// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.EUI;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Network.Messages;
using Content.Shared.PAI;

namespace Content.Server.SS220.CultYogg;

public sealed class MiGoReplacementSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MiGoSystem _migoSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoReplacementComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<MiGoReplacementComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<MiGoReplacementComponent, MindRemovedMessage>(OnMindRemoved);
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    private void StartTimer(MiGoReplacementComponent comp)
    {
        if (comp.ShouldBeCounted)
            return;

        comp.ShouldBeCounted = true;
        comp.ReplacementTimer = 0;
    }

    private void StopTimer(MiGoReplacementComponent comp)
    {
        comp.ShouldBeCounted = false;
        comp.ReplacementTimer = 0;
    }

    private void RemoveReplacement(EntityUid uid, MiGoReplacementComponent replComp, MiGoComponent migoComp)
    {
        StopTimer(replComp);
        _migoSystem.MarkupForReplacement(uid, migoComp, false);
    }
    private void OnMindAdded(Entity<MiGoReplacementComponent> uid, ref MobStateChangedEvent args)
    {
        ;
    }
    private void OnMindRemoved(Entity<MiGoReplacementComponent> uid, ref MobStateChangedEvent args)
    {
        ;
    }

    ///<summary>
    /// On death removes active comps and gives genetic damage to prevent cloning, reduce this to allow cloning.
    ///</summary>
    private void OnMobState(Entity<MiGoReplacementComponent> uid, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            StartTimer(uid.Comp);
        else
            StopTimer(uid.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MindContainerComponent, MiGoReplacementComponent, MiGoComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var mc, out var replaceComp, out var migoComp, out var mobState))
        {
            //check if it has mind
            if (!mc.HasMind)
                StartTimer(replaceComp);

            //if timer is on count it
            if (!replaceComp.ShouldBeCounted)
                continue;

            //in case somebody was resurected while he isn't in a body
            //case we dont have any event for acquiring mind
            if (mc.HasMind && _mobState.IsAlive(uid, mobState))
            {
                RemoveReplacement(uid, replaceComp, migoComp);
                continue;
            }

            replaceComp.ReplacementTimer += frameTime;

            if (replaceComp.ReplacementTimer >= replaceComp.BeforeReplacemetTime)
            {
                _migoSystem.MarkupForReplacement(uid, migoComp, true);
            }

        }
    }
}
