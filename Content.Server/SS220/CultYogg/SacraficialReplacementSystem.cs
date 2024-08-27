// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.EUI;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Player;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Timing;
using Content.Server.Body.Components;
using Robust.Shared.Network;
using System.Linq;

namespace Content.Server.SS220.CultYogg;

public sealed partial class SacraficialReplacementSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<(EntityUid, NetUserId), (TimeSpan, bool)> _entityEnteredSSDTimes = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggSacrificialComponent, BeingGibbedEvent>(OnBeingGibbed);
        //SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerAttachedEvent>(OnPlayerAttached);
        //SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerDetachedEvent>(OnPlayerDetached);

        _playerManager.PlayerStatusChanged -= OnPlayerChange;
    }
    private void OnBeingGibbed(Entity<CultYoggSacrificialComponent> ent, ref BeingGibbedEvent args)
    {
        if (ent.Comp.WasSacraficed) // if it died when it was sacraficed -- everything is ok
            return;

        if (!_mind.TryGetMind(ent, out var mind, out var userMidComp))
            return;

        int tier = ent.Comp.Tier;//ToDo send this tier to a gamerule

        //remove all components
        RemComp<CultYoggSacrificialComponent>(ent);
        RemComp<CultYoggSacrificialMindComponent>(mind);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var pair in _entityEnteredSSDTimes.Where(uid => HasComp<MindContainerComponent>(uid.Key.Item1)))
        {
            if (pair.Value.Item2)
                _entityEnteredSSDTimes.Remove(pair.Key);
        }
    }
}
