using System.Numerics;
using Content.Server.Decals;
using Content.Server.Hands.Systems;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Photography;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Photography;

public sealed class PhotoManager : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _map = default!;
    private EntityLookupSystem _entityLookup = default!;
    private SharedTransformSystem _transform = default!;
    private InventorySystem _inventory = default!;
    private HandsSystem _hands = default!;
    private DecalSystem _decal = default!;

    private EntityQuery<MapGridComponent> _gridQuery = default!;
    private Dictionary<string, PhotoData> _photos = new();
    private ISawmill _sawmill = Logger.GetSawmill("photo-manager");

    private float _pvsRange = 10;
    public const float MAX_PHOTO_RADIUS = 20;

    public override void Initialize()
    {
        base.Initialize();
        IoCManager.InjectDependencies(this);

        _cfg.OnValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged, true);
        _pvsRange = _cfg.GetCVar(CVars.NetMaxUpdateRange);

        _transform = _sysMan.GetEntitySystem<SharedTransformSystem>();
        _entityLookup = _sysMan.GetEntitySystem<EntityLookupSystem>();
        _inventory = _sysMan.GetEntitySystem<InventorySystem>();
        _hands = _sysMan.GetEntitySystem<HandsSystem>();
        _decal = _sysMan.GetEntitySystem<DecalSystem>();

        _gridQuery = EntityManager.GetEntityQuery<MapGridComponent>();

        SubscribeNetworkEvent<PhotoDataRequest>(OnPhotoDataRequest);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _photos.Clear();
        _cfg.UnsubValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged);
    }

    public void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _photos.Clear();
    }

    private void OnPvsRangeChanged(float value) => _pvsRange = value;

    private void OnPhotoDataRequest(PhotoDataRequest message, EntitySessionEventArgs eventArgs)
    {
        var sender = eventArgs.SenderSession;

        if (!_photos.TryGetValue(message.Id, out var photoData))
        {
            _sawmill.Warning("Player " + sender.Name + " requested data of a photo with ID " + message.Id + " but it doesn't exist!");
        }

        var ev = new PhotoDataRequestResponse(photoData, message.Id);
        RaiseNetworkEvent(ev, sender);
    }

    public string? TryCapture(MapCoordinates focusCoords, Angle cameraRotation, Vector2i captureSize)
    {
        var id = Guid.NewGuid().ToString();
        var focusWorldPos = focusCoords.Position;

        var radius = MathF.Min(_pvsRange, MAX_PHOTO_RADIUS); //cap because scary
        var range = new Vector2(radius, radius);
        var worldArea = new Box2(focusWorldPos - range, focusWorldPos + range);

        var data = new PhotoData(id, captureSize, focusWorldPos, cameraRotation);

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

            var (position, rotation) = _transform.GetWorldPositionRotation(entXform);

            // TODO: deduplicate
            // Appearance state
            AppearanceComponentState? appearanceState = null;
            if (TryComp<AppearanceComponent>(entity, out var appearance))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, appearance, null, GameTick.Zero);
                if (maybe_state is AppearanceComponentState state)
                {
                    appearanceState = state;
                }
            }

            // Humanoid appearance state
            HumanoidAppearanceState? humanoidAppearanceState = null;
            if (TryComp<HumanoidAppearanceComponent>(entity, out var humanoidAppearance))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, humanoidAppearance, null, GameTick.Zero);
                if (maybe_state is HumanoidAppearanceState state)
                {
                    humanoidAppearanceState = state;
                }
            }

            // Point light state
            PointLightComponentState? pointLightState = null;
            if (TryComp<PointLightComponent>(entity, out var pointLight))
            {
                // not networked, have to do it like this otherwise crashes in debug
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

            // Occluder state
            OccluderComponent.OccluderComponentState? occluderState = null;
            if (TryComp<OccluderComponent>(entity, out var occluder))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, occluder, null, GameTick.Zero);
                if (maybe_state is OccluderComponent.OccluderComponentState state)
                {
                    occluderState = state;
                }
            }

            // Damageable state
            DamageableComponentState? damageableState = null;
            if (TryComp<DamageableComponent>(entity, out var damageable))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, damageable, null, GameTick.Zero);
                if (maybe_state is DamageableComponentState state)
                {
                    damageableState = state;
                }
            }

            // Hands state
            HandsComponentState? handsState = null;
            if (TryComp<HandsComponent>(entity, out var handsComp))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, handsComp, null, GameTick.Zero);
                if (maybe_state is HandsComponentState state)
                {
                    handsState = state;
                }
            }

            // Inventory & Hands
            Dictionary<string, string>? inventory = null;
            Dictionary<string, string>? hands = null;

            var haveHands = handsComp != null;
            if (haveHands)
                hands = new();

            var haveInventory = TryComp(entity, out InventoryComponent? inventoryComp);
            if (haveInventory)
                inventory = new();

            if (haveHands || haveInventory)
            {
                foreach (var item in _inventory.GetHandOrInventoryEntities(entity))
                {
                    var proto = MetaData(item).EntityPrototype?.ID;
                    if (proto is null)
                        continue;

                    if (haveInventory && _inventory.TryGetContainingSlot(item, out var slot))
                    {
                        inventory!.Add(slot.Name, proto);
                    }
                    else if (haveHands && _hands.IsHolding(entity, item, out var hand, handsComp))
                    {
                        foreach (var handEntry in handsComp!.Hands)
                        {
                            if (handEntry.Value != hand)
                                continue;

                            hands!.Add(handEntry.Key, proto);
                            break;
                        }
                    }
                }
            }

            var ent_data = new PhotoEntityData(protoId, position, rotation)
            {
                Appearance = appearanceState,
                HumanoidAppearance = humanoidAppearanceState,
                PointLight = pointLightState,
                Occluder = occluderState,
                Damageable = damageableState,
                Hands = handsState,
                Inventory = inventory,
                HandsContents = hands
            };
            data.Entities.Add(ent_data);
        }

        // Get grids in range
        var intersectingGrids = _map.FindGridsIntersecting(focusCoords.MapId, worldArea);
        foreach (var grid in intersectingGrids)
        {
            if (!TryComp<TransformComponent>(grid.Owner, out var gridXform))
                continue;

            var gridPosRot = _transform.GetWorldPositionRotation(gridXform);
            var gridData = new PhotoGridData(gridPosRot.WorldPosition, gridPosRot.WorldRotation);
            foreach (var tile in grid.GetTilesIntersecting(worldArea))
            {
                var indices = tile.GridIndices;
                var tileType = tile.Tile.TypeId;
                gridData.Tiles.Add((indices, tileType));
            }

            foreach (var decal in _decal.GetDecalsIntersecting(grid.Owner, worldArea))
            {
                gridData.Decals.Add(decal.Decal);
            }

            data.Grids.Add(gridData);
        }

        _photos.Add(id, data);
        _sawmill.Debug("Photo taken! Entity count: " + data.Entities.Count + ", Grid count: " + data.Grids.Count + ", ID: " + id);

        return id;
    }
}
