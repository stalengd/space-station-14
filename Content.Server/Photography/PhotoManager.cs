using Content.Shared.Photography;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Photography;

public sealed class PhotoManager : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    private EntityLookupSystem _entityLookup = default!;

    private float _pvsRange = 10;
    private EntityQuery<MapGridComponent> _gridQuery = default!;
    private ISawmill _sawmill = Logger.GetSawmill("photo-manager");

    public override void Initialize()
    {
        base.Initialize();
        IoCManager.InjectDependencies(this);

        _configurationManager.OnValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged, true);
        _pvsRange = _configurationManager.GetCVar(CVars.NetMaxUpdateRange);
        _entityLookup = EntityManager.System<EntityLookupSystem>();
        _gridQuery = EntityManager.GetEntityQuery<MapGridComponent>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _photos.Clear();
        _configurationManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged);
    }

    private void OnPvsRangeChanged(float value) => _pvsRange = value;

    private Dictionary<string, PhotoData> _photos = new();

    public string? TryCapture(MapCoordinates coords, Vector2i captureSize)
    {
        var id = Guid.NewGuid().ToString();
        var data = new PhotoData(id, captureSize);

        var ent_count = 0;
        foreach (var entity in _entityLookup.GetEntitiesInRange(coords, _pvsRange))
        {
            var protoId = MetaData(entity).EntityPrototype?.ID;
            if (protoId is null)
                continue;

            // No grids here
            if (_gridQuery.HasComponent(entity))
                continue;

            AppearanceComponentState? appearanceState = null;
            if (TryComp<AppearanceComponent>(entity, out var appearance))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, appearance, null, GameTick.Zero);
                if (maybe_state is AppearanceComponentState state)
                {
                    appearanceState = state;
                }
            }

            var ent_data = new EntityData(protoId, appearanceState);
            data.AddEntity(ent_data);

            ent_count++;
        }

        if (!_photos.TryAdd(id, data)) //sorry i'm paranoidal
            return null;

        _sawmill.Debug("Photo taken! Entity count: " + ent_count + " ID: " + id);

        return id;
    }
}
