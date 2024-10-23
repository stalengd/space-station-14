// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Rotation;
using Content.Shared.SS220.Photography;
using Content.Shared.StatusEffect;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Photography;

public sealed partial class PhotoVisualizer : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly EyeSystem _eye = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly LightTreeSystem _lightTree = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;

    private ISawmill _sawmill = Logger.GetSawmill("photo-visualizer");
    private Dictionary<string, PhotoVisualisation> _currentlyVisualized = new();
    private MapId? _reservedMap;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PhotoReservedMapMessage>(OnMapReserved);
        InitializeCache();

        // Request a map, just in case it was already created
        // Needed for mid-round joins
        var ev = new PhotoRequestMapMessage();
        RaiseNetworkEvent(ev);
    }

    private void OnMapReserved(PhotoReservedMapMessage args)
    {
        _sawmill.Debug($"Reserved map received. MapId: {args.Map}");
        _reservedMap = args.Map;
    }

    public bool TryGetVisualization(string id, [NotNullWhen(true)] out EyeComponent? eye)
    {
        if (!_photoDataCache.TryGetValue(id, out var data))
        {
            eye = null;
            return false;
        }

        return TryGetVisualization(data, out eye);
    }

    public bool TryGetVisualization(PhotoData data, [NotNullWhen(true)] out EyeComponent? eye)
    {
        if (_currentlyVisualized.TryGetValue(data.Id, out var existingVisualization))
        {
            eye = existingVisualization.Eye;
            return true;
        }

        if (!_reservedMap.HasValue || !_mapMan.MapExists(_reservedMap.Value))
        {
            _sawmill.Debug($"Visualisation has not been loaded. Reason: No reserved map!");
            eye = null;
            return false;
        }

        var origin = new MapCoordinates(Vector2.Zero, _reservedMap.Value);
        var entities = new List<EntityUid>(data.Entities.Count + data.Grids.Count + 1);
        List<EntityUid> grids = new(data.Grids.Count);

        // Add grids
        for (var i = 0; i < data.Grids.Count; i++)
        {
            var gridDesc = data.Grids[i];
            var grid = _mapMan.CreateGrid(_reservedMap.Value);
            var gridEnt = grid.Owner;
            var gridXform = EnsureComp<TransformComponent>(gridEnt);
            entities.Add(gridEnt);
            grids.Add(gridEnt);

            _transform.SetParent(gridEnt, gridXform, _mapMan.GetMapEntityId(_reservedMap.Value));
            _transform.SetLocalRotationNoLerp(gridEnt, gridDesc.Rotation, gridXform);
            _transform.SetLocalPositionNoLerp(gridEnt, gridDesc.Position, gridXform);

            foreach (var (indices, tileType) in gridDesc.Tiles)
            {
                var tile = new Tile(tileType);
                _map.SetTile(gridEnt, grid, indices, tile);
            }

            // Handle decal state
            if (gridDesc.DecalGridState is not null)
            {
                var decalGrid = EnsureComp<DecalGridComponent>(gridEnt);
                var ev = new ComponentHandleState(gridDesc.DecalGridState, null);
                RaiseLocalEvent(gridEnt, ref ev);
            }
        }


        // Create and setup camera
        var camera = Spawn(null, origin);
        entities.Add(camera);

        var cameraXform = EnsureComp<TransformComponent>(camera);
        _transform.SetWorldPosition(cameraXform, data.CameraPosition);

        eye = EnsureComp<EyeComponent>(camera);
        _eye.SetZoom(camera, Vector2.One, eye);
        //Align with grid by subtracting grid angle (I have no idea why, but it works)
        _eye.SetRotation(camera, -data.CameraRotation, eye);


        // Add entities
        foreach (var entityDesc in data.Entities)
        {
            // Make sure entity transforms are parented
            EntityUid parent;

            if (entityDesc.GridIndex.HasValue && grids.TryGetValue(entityDesc.GridIndex.Value, out var maybeParent))
            {
                parent = maybeParent;
            }
            else if (_mapMan.GetMapEntityId(_reservedMap.Value) is { Valid: true } mapEnt)
            {
                parent = mapEnt;
            }
            else
            {
                continue;
            }

            EntityCoordinates coords = new(parent, entityDesc.Position);

            var entity = Spawn(null, coords);
            entities.Add(entity);

            var xform = EnsureComp<TransformComponent>(entity);
            _transform.SetLocalRotationNoLerp(entity, entityDesc.Rotation, xform);

            var spriteComp = EnsureComp<SpriteComponent>(entity);

            //if (!TryComp(entity, out SpriteComponent? spriteComp))
            //{
            //    continue;
            //}

            spriteComp.GranularLayersRendering = entityDesc.Sprite.GranularLayersRendering;
            spriteComp.Visible = entityDesc.Sprite.Visible;
            spriteComp.DrawDepth = entityDesc.Sprite.DrawDepth;
            spriteComp.Scale = entityDesc.Sprite.Scale;
            spriteComp.Rotation = entityDesc.Sprite.Rotation;
            spriteComp.Offset = entityDesc.Sprite.Offset;
            spriteComp.Color = entityDesc.Sprite.Color;
            spriteComp.BaseRSI = _resourceCache.GetResource<RSIResource>(entityDesc.Sprite.BaseRSI).RSI;
            spriteComp.GetScreenTexture = entityDesc.Sprite.GetScreenTexture;
            spriteComp.RaiseShaderEvent = entityDesc.Sprite.RaiseShaderEvent;

            while (spriteComp.LayerExists(0, logError: false))
            {
                spriteComp.RemoveLayer(0);
            }
            foreach (var layerData in entityDesc.Sprite.Layers)
            {
                var layerProto = new PrototypeLayerData()
                {
                    Shader = layerData.Shader,
                    RsiPath = layerData.RsiPath,
                    State = layerData.RsiState,
                    Rotation = layerData.Rotation,
                    Scale = layerData.Scale,
                    Offset = layerData.Offset,
                    Visible = layerData.Visible,
                    Color = layerData.Color,
                    RenderingStrategy = layerData.RenderingStrategy,
                    Cycle = layerData.Cycle,
                    CopyToShaderParameters = layerData.CopyToShaderParameters,
                };
                var layerIndex = spriteComp.AddLayer(layerProto);
                if (!spriteComp.TryGetLayer(layerIndex, out var layer))
                    continue;
                layer.SetAnimationTime(layerData.AnimationTime);
                layer.DirOffset = (SpriteComponent.DirectionOffset)(byte)layerData.DirOffset;
            }
            foreach (var (layerKeySerialized, layerIndex) in entityDesc.Sprite.LayerMap)
            {
                object key;
                if (_reflectionManager.TryParseEnumReference(layerKeySerialized, out var enumKey, false))
                {
                    key = enumKey;
                }
                else
                {
                    key = layerKeySerialized;
                }
                spriteComp.LayerMapSet(key, layerIndex);
            }

            if (entityDesc.PointLight is not null)
            {
                var pointLightComp = EnsureComp<PointLightComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.PointLight, null);
                EntityManager.EventBus.RaiseComponentEvent(entity, pointLightComp, ev);
            }

            if (entityDesc.Occluder is not null)
            {
                var occluderComp = EnsureComp<OccluderComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Occluder, null);
                EntityManager.EventBus.RaiseComponentEvent(entity, occluderComp, ev);
            }
        }

        var photoVisDesc = new PhotoVisualisation(_reservedMap.Value, camera, origin, eye, entities, data.PhotoSize);
        _currentlyVisualized.Add(data.Id, photoVisDesc);
        _sawmill.Debug("Created map for visualization of the photo " + data.Id);

        return true;
    }

    public void DisposeVisualization(string id)
    {
        if (!_currentlyVisualized.TryGetValue(id, out var photoVisualisation))
            return;

        //_mapMan.DeleteMap(photoVisualisation.MapId);
        _currentlyVisualized.Remove(id);
        foreach (var entity in photoVisualisation.Entities)
        {
            EntityManager.DeleteEntity(entity);
        }
    }
}

public sealed class PhotoVisualisation
{
    public readonly MapId MapId;
    public readonly EntityUid Camera;
    public readonly EyeComponent Eye;
    public readonly MapCoordinates Origin;
    public readonly List<EntityUid> Entities;
    public readonly float Size;

    public PhotoVisualisation(MapId mapId, EntityUid camera, MapCoordinates origin, EyeComponent eye, List<EntityUid> entities, float size)
    {
        MapId = mapId;
        Origin = origin;
        Camera = camera;
        Eye = eye;
        Entities = entities;
        Size = size;
    }
}
