using System.Numerics;
using Content.Shared.Photography;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Photography;

public sealed class PhotoManager : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _conf = default!;
    private EntityLookupSystem _entityLookup = default!;
    private SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private EntityQuery<MapGridComponent> _gridQuery = default!;
    private EntityQueryEnumerator<MapGridComponent, TransformComponent> _gridQueryEnumerator = default!;
    private Dictionary<string, PhotoData> _photos = new();
    private float _pvsRange = 10;
    private ISawmill _sawmill = Logger.GetSawmill("photo-manager");

    public override void Initialize()
    {
        base.Initialize();
        IoCManager.InjectDependencies(this);

        _conf.OnValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged, true);
        _pvsRange = _conf.GetCVar(CVars.NetMaxUpdateRange);

        _transform = EntityManager.System<SharedTransformSystem>();
        _entityLookup = EntityManager.System<EntityLookupSystem>();
        _gridQuery = EntityManager.GetEntityQuery<MapGridComponent>();
        _gridQueryEnumerator = EntityManager.EntityQueryEnumerator<MapGridComponent, TransformComponent>();

        SubscribeNetworkEvent<PhotoDataRequest>(OnPhotoDataRequest);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _photos.Clear();
        _conf.UnsubValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged);
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

    public string? TryCapture(TransformComponent cameraXform, Vector2i captureSize)
    {
        var id = Guid.NewGuid().ToString();
        var cameraCoords = cameraXform.MapPosition;
        var cameraWorldPos = _transform.GetWorldPosition(cameraXform);

        var radius = 10;
        var range = new Vector2(radius, radius);
        var worldArea = new Box2(cameraWorldPos - range, cameraWorldPos + range);

        var data = new PhotoData(id, captureSize, _transform.GetWorldPosition(cameraXform));

        // Get entities in range
        var ent_count = 0;
        foreach (var entity in _entityLookup.GetEntitiesInRange(cameraCoords, radius, LookupFlags.Uncontained))
        {
            var protoId = MetaData(entity).EntityPrototype?.ID;
            if (protoId is null)
                continue;

            // No grids here
            if (_gridQuery.HasComponent(entity))
                continue;

            if (!TryComp<TransformComponent>(entity, out var entXform))
                continue;

            var posrot = _transform.GetWorldPositionRotation(entXform);

            AppearanceComponentState? appearanceState = null;
            if (TryComp<AppearanceComponent>(entity, out var appearance))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, appearance, null, GameTick.Zero);
                if (maybe_state is AppearanceComponentState state)
                {
                    appearanceState = state;
                }
            }

            var ent_data = new PhotoEntityData(protoId, posrot, appearanceState);
            data.Entities.Add(ent_data);

            ent_count++;
        }

        // Get grids in range
        var grid_count = 0;
        var intersectingGrids = _mapManager.FindGridsIntersecting(cameraXform.MapID, worldArea);
        foreach (var grid in intersectingGrids)
        {
            grid_count++;

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

            data.Grids.Add(gridData);
        }

        _photos.Add(id, data);
        _sawmill.Debug("Photo taken! Entity count: " + ent_count + ", Grid count: " + grid_count + ", ID: " + id);

        return id;
    }
}
