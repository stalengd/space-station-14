using Content.Server.Lightning;
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Nyarlathotep;

public sealed class NyarlathotepTargetSearcherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NyarlathotepSystem _nyarlathotep = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NyarlathotepSearchTargetsComponent, MapInitEvent>(OnSearchMapInit);
    }

    private void OnSearchMapInit(EntityUid uid, NyarlathotepSearchTargetsComponent component, ref MapInitEvent args)
    {
        component.NextSearchTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.SearchMaxInterval);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NyarlathotepSearchTargetsComponent>();
        while (query.MoveNext(out var uid, out var targetSearcher))
        {
            if (targetSearcher.NextSearchTime > _gameTiming.CurTime)
                continue;

            TargetSearch(uid, targetSearcher);
            var delay = TimeSpan.FromSeconds(_random.NextFloat(targetSearcher.SearchMinInterval, targetSearcher.SearchMaxInterval));
            targetSearcher.NextSearchTime += delay;
        }
    }

    private void TargetSearch(EntityUid uid, NyarlathotepSearchTargetsComponent component)
    {
        _nyarlathotep.SearchNearNyarlathotep(uid, component.SearchRange);
    }
}
