using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Decals;
using Content.Client.Hands.Systems;
using Content.Client.Rotation;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Photography;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Client.Photography;

public sealed partial class PhotoVisualizer : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    private SharedTransformSystem _transform = default!;
    private InventorySystem _inventory = default!;
    private HandsSystem _hands = default!;
    private DecalSystem _decal = default!;
    private EyeSystem _eye = default!;
    private ISawmill _sawmill = Logger.GetSawmill("photo-visualizer");

    private Dictionary<string, PhotoVisualisation> _currentlyVisualized = new();

    public override void Initialize()
    {
        base.Initialize();
        IoCManager.InjectDependencies(this);

        _eye = _sysMan.GetEntitySystem<EyeSystem>();
        _hands = _sysMan.GetEntitySystem<HandsSystem>();
        _decal = _sysMan.GetEntitySystem<DecalSystem>();
        _inventory = _sysMan.GetEntitySystem<InventorySystem>();
        _transform = _sysMan.GetEntitySystem<SharedTransformSystem>();

        InitializeCache();
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

        var mapId = _mapMan.CreateMap();
        _mapMan.AddUninitializedMap(mapId);
        var origin = new MapCoordinates(Vector2.Zero, mapId);

        var camera = Spawn(null, origin);

        eye = EnsureComp<EyeComponent>(camera);
        _eye.SetZoom(camera, Vector2.One, eye);
        //Align with grid by subtracting grid angle (I have no idea why, but it works)
        _eye.SetRotation(camera, -data.CameraRotation, eye);

        var cameraXform = EnsureComp<TransformComponent>(camera);
        _transform.SetWorldPosition(cameraXform, data.CameraPosition);

        // Add grids
        foreach (var gridDesc in data.Grids)
        {
            var grid = _mapMan.CreateGrid(mapId);
            var gridXform = EnsureComp<TransformComponent>(grid.Owner);

            _transform.SetWorldPositionRotation(gridXform, gridDesc.Position, gridDesc.Rotation);

            foreach (var (indices, tileType) in gridDesc.Tiles)
            {
                var tile = new Tile(tileType);
                grid.SetTile(indices, tile);
            }

            foreach (var decal in gridDesc.Decals)
            {
                //Todo: fuck i can't add decals clientside
            }
        }

        // Add entities
        foreach (var entityDesc in data.Entities)
        {
            var worldPos = entityDesc.Position;

            // Make sure entities are parented to grids
            EntityUid parent;
            if (_mapMan.TryFindGridAt(mapId, entityDesc.Position, out var gridUid, out _))
            {
                parent = gridUid;
            }
            else if (_mapMan.GetMapEntityId(mapId) is { Valid: true } mapEnt)
            {
                parent = mapEnt;
            }
            else
            {
                continue;
            }

            EntityCoordinates coords = new(parent, _transform.GetInvWorldMatrix(parent).Transform(worldPos));

            var entity = Spawn(entityDesc.PrototypeId, coords);
            var xform = EnsureComp<TransformComponent>(entity);

            _transform.SetWorldRotation(xform, entityDesc.Rotation);

            if (TryComp<RotationVisualsComponent>(entity, out var rotationVisualsComp))
                rotationVisualsComp.AnimationTime = 0;

            // Handle appearance state
            if (entityDesc.Appearance is not null)
            {
                var appearanceComp = EnsureComp<AppearanceComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Appearance, null);
                EntityManager.EventBus.RaiseComponentEvent(appearanceComp, ref ev);
            }

            // TODO: deduplicate
            // Handle humanoid appearance state
            if (entityDesc.HumanoidAppearance is not null)
            {
                var humanoidAppearanceComp = EnsureComp<HumanoidAppearanceComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.HumanoidAppearance, null);
                EntityManager.EventBus.RaiseComponentEvent(humanoidAppearanceComp, ref ev);
            }

            // Handle point light state
            if (entityDesc.PointLight is not null)
            {
                var pointLightComp = EnsureComp<PointLightComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.PointLight, null);
                EntityManager.EventBus.RaiseComponentEvent(pointLightComp, ref ev);
            }

            // Handle occluder state
            if (entityDesc.Occluder is not null)
            {
                var occluderComp = EnsureComp<OccluderComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Occluder, null);
                EntityManager.EventBus.RaiseComponentEvent(occluderComp, ref ev);
            }

            // Handle damageable state
            if (entityDesc.Damageable is not null)
            {
                var damageableComp = EnsureComp<DamageableComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Damageable, null);
                EntityManager.EventBus.RaiseComponentEvent(damageableComp, ref ev);
            }

            // Handle hands state
            if (entityDesc.Hands is not null)
            {
                var handsComp = EnsureComp<HandsComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Hands, null);
                EntityManager.EventBus.RaiseComponentEvent(handsComp, ref ev);
            }

            // Handle inventory
            if (entityDesc.Inventory is not null)
            {
                var inventoryComp = EnsureComp<InventoryComponent>(entity);
                foreach (var slotEntry in entityDesc.Inventory)
                {
                    if (_inventory.TryUnequip(entity, slotEntry.Key, out var item, true, true, false, inventoryComp))
                        QueueDel(item);
                    _inventory.SpawnItemInSlot(entity, slotEntry.Key, slotEntry.Value, true, true, inventoryComp);
                }
            }

            // Handle hands
            if (entityDesc.HandsContents is not null)
            {
                var handsComp = EnsureComp<HandsComponent>(entity);

                foreach (var handEntry in entityDesc.HandsContents)
                {
                    if (!_hands.TryGetHand(entity, handEntry.Key, out var hand, handsComp))
                        continue;

                    var inhandEntity = EntityManager.SpawnEntity(handEntry.Value, coords);
                    if (!_hands.TryPickup(entity, inhandEntity, hand, false, false, handsComp))
                        QueueDel(inhandEntity);
                }
            }
        }

        var photoVisDesc = new PhotoVisualisation(mapId, camera, origin, eye);
        _currentlyVisualized.Add(data.Id, photoVisDesc);
        _sawmill.Debug("Created map for visualization of the photo " + data.Id);

        return true;
    }

    public void DisposeVisualization(string id)
    {
        if (!_currentlyVisualized.TryGetValue(id, out var photoVisualisation))
            return;

        _mapMan.DeleteMap(photoVisualisation.MapId);
        _currentlyVisualized.Remove(id);
    }
}

public sealed class PhotoVisualisation
{
    public readonly MapId MapId;
    public readonly EntityUid Camera;
    public readonly EyeComponent Eye;
    public readonly MapCoordinates Origin;

    public PhotoVisualisation(MapId mapId, EntityUid camera, MapCoordinates origin, EyeComponent eye)
    {
        MapId = mapId;
        Origin = origin;
        Camera = camera;
        Eye = eye;
    }
}
