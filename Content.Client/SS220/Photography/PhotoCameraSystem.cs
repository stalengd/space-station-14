using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.SS220.Photography;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Client.GameObjects;
using Content.Client.Hands.Systems;
using Content.Client.IdentityManagement;
using Robust.Shared.Reflection;

namespace Content.Client.SS220.Photography;

public sealed class PhotoCameraSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _mapSys = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;

    private ISawmill _sawmill = Logger.GetSawmill("photo-manager");
    private EntityQuery<MapGridComponent> _gridQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = EntityManager.GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<PhotoCameraComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(Entity<PhotoCameraComponent> camera, ref UseInHandEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var xform = Transform(args.User);

        Angle cameraRotation;

        if (xform.GridUid is { } gridToAllignWith)
            cameraRotation = _transform.GetWorldRotation(gridToAllignWith);
        else
            cameraRotation = _transform.GetWorldRotation(xform);

        var photo = TryCapture(_transform.GetMapCoordinates(camera.Owner), cameraRotation, 10, out var id, out var seenObjects);
        RaiseNetworkEvent(new PhotoTakeRequest(GetNetEntity(camera.Owner), photo));
    }

    public PhotoData? TryCapture(
        MapCoordinates focusCoords,
        Angle cameraRotation,
        float captureSize,
        [NotNullWhen(true)] out string? id,
        [NotNullWhen(true)] out List<string>? seenObjects)
    {
        id = Guid.NewGuid().ToString();
        seenObjects = new();
        var focusWorldPos = focusCoords.Position;
        var captureSizeSquareHalf = (captureSize * captureSize) / 2; //for optimization purposes

        var radius = MathF.Min(10, 10); //cap because scary
        var range = new Vector2(radius, radius);
        var worldArea = new Box2(focusWorldPos - range, focusWorldPos + range);

        var data = new PhotoData(id, captureSize, focusWorldPos, cameraRotation);

        // Get grids in range
        var intersectingGrids = _mapMan.FindGridsIntersecting(focusCoords.MapId, worldArea);
        Dictionary<EntityUid, int> gridIdMap = new();

        foreach (var grid in intersectingGrids)
        {
            var gridUid = grid.Owner;

            if (!TryComp<TransformComponent>(gridUid, out var gridXform))
                continue;

            var gridPosRot = _transform.GetWorldPositionRotation(gridXform);
            var gridData = new PhotoGridData(gridPosRot.WorldPosition, gridPosRot.WorldRotation);
            foreach (var tile in _mapSys.GetTilesIntersecting(gridUid, grid, worldArea, true))
            {
                var indices = tile.GridIndices;
                var tileType = tile.Tile.TypeId;
                gridData.Tiles.Add((indices, tileType));
            }

            if (TryComp<DecalGridComponent>(gridUid, out var decalGrid))
            {
                var (_, _, matrix) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
                var localPos = Vector2.Transform(focusWorldPos, matrix);
                var localAABB = new Box2(localPos - range, localPos + range);
                var chunkDict = new Dictionary<Vector2i, DecalGridComponent.DecalChunk>();
                var chunks = new ChunkIndicesEnumerator(localAABB, SharedDecalSystem.ChunkSize);
                var chunkCollection = decalGrid.ChunkCollection.ChunkCollection;

                while (chunks.MoveNext(out var chunkOrigin))
                {
                    if (!chunkCollection.TryGetValue(chunkOrigin.Value, out var chunk))
                        continue;

                    chunkDict.Add(chunkOrigin.Value, chunk);
                }

                gridData.DecalGridState = new DecalGridState(chunkDict);
            }

            gridIdMap.Add(gridUid, data.Grids.Count);
            data.Grids.Add(gridData);
        }

        // Get entities in range
        foreach (var entity in _entityLookup.GetEntitiesInRange(focusCoords, radius, LookupFlags.Uncontained))
        {
            var protoId = MetaData(entity).EntityPrototype?.ID;
            if (protoId is null)
                continue;

            // No grids here
            if (_gridQuery.HasComponent(entity))
                continue;

            if (!TryComp<TransformComponent>(entity, out var entXform))
                continue;

            if (!TryComp<SpriteComponent>(entity, out var sprite))
            {
                continue;
            }

            Vector2 position;
            Angle rotation;
            int? gridKey = null;

            if (entXform.GridUid is { } gridUid && gridIdMap.TryGetValue(gridUid, out var gridKeyMaybe))
            {
                gridKey = gridKeyMaybe;
                position = entXform.LocalPosition;
                rotation = entXform.LocalRotation;
            }
            else
                (position, rotation) = _transform.GetWorldPositionRotation(entXform);

            SpriteState spriteState = new();
            spriteState.GranularLayersRendering = sprite.GranularLayersRendering;
            spriteState.Visible = sprite.Visible;
            spriteState.DrawDepth = sprite.DrawDepth;
            spriteState.Scale = sprite.Scale;
            spriteState.Rotation = sprite.Rotation;
            spriteState.Offset = sprite.Offset;
            spriteState.Color = sprite.Color;
            spriteState.BaseRSI = sprite.BaseRSI?.Path ?? ResPath.Empty;
            spriteState.GetScreenTexture = sprite.GetScreenTexture;
            spriteState.RaiseShaderEvent = sprite.RaiseShaderEvent;
            spriteState.RenderOrder = sprite.RenderOrder;

            foreach (var layerBase in sprite.AllLayers)
            {
                if (layerBase is not SpriteComponent.Layer layer)
                    continue;
                var layerState = new SpriteState.Layer()
                {
                    DirOffset = (SpriteState.DirectionOffset)(int)layer.DirOffset,
                    Shader = layer.ShaderPrototype,
                    RsiPath = layer.RSI?.Path.CanonPath,
                    RsiState = layer.State.Name,
                    Rotation = layer.Rotation,
                    Scale = layer.Scale,
                    Offset = layer.Offset,
                    Visible = layer.Visible,
                    Color = layer.Color,
                    AnimationTime = layer.AnimationTime,
                    RenderingStrategy = layer.RenderingStrategy,
                    Cycle = layer.Cycle,
                };
                if (layer.CopyToShaderParameters is { } copyParams && copyParams.LayerKey is { } layerKey)
                {
                    var layerIndex = sprite.LayerMapGet(layerKey);
                    var layerKeySerialized = layerKey switch
                    {
                        Enum e => _reflectionManager.GetEnumReference(e),
                        _ => layerKey.ToString(),
                    };
                    if (layerKeySerialized is { })
                    {
                        spriteState.LayerMap[layerKeySerialized] = layerIndex;
                        layerState.CopyToShaderParameters = new()
                        {
                            LayerKey = layerKeySerialized,
                            ParameterTexture = copyParams.ParameterTexture,
                            ParameterUV = copyParams.ParameterUV,
                        };
                    }
                }
                spriteState.Layers.Add(layerState);
            }

            PointLightComponentState? pointLightState = null;
            if (TryComp<PointLightComponent>(entity, out var pointLight))
            {
                pointLightState = new PointLightComponentState()
                {
                    Color = pointLight.Color,
                    Energy = pointLight.Energy,
                    Softness = pointLight.Softness,
                    CastShadows = pointLight.CastShadows,
                    Enabled = pointLight.Enabled,
                    Radius = pointLight.Radius,
                    Offset = pointLight.Offset
                };
            }

            OccluderComponent.OccluderComponentState? occluderState = null;
            if (TryComp<OccluderComponent>(entity, out var occluder))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, occluder, null, GameTick.Zero);
                if (maybe_state is OccluderComponent.OccluderComponentState state)
                {
                    occluderState = state;
                }
            }

            var ent_data = new PhotoEntityData(protoId, position, rotation)
            {
                GridIndex = gridKey,
                Sprite = spriteState,
                PointLight = pointLightState,
                Occluder = occluderState,
            };
            data.Entities.Add(ent_data);
        }
        _sawmill.Debug("Photo taken! Entity count: " + data.Entities.Count + ", Grid count: " + data.Grids.Count + ", ID: " + id);

        return data;
    }
}
