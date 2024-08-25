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
using Content.Shared.SSDIndicator;
using Robust.Shared.Player;

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
        SubscribeLocalEvent<MiGoReplacementComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MiGoReplacementComponent, PlayerDetachedEvent>(OnPlayerDetached);
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

    private void OnPlayerAttached(Entity<MiGoReplacementComponent> ent, ref PlayerAttachedEvent args)
    {
        CheckTimerConditions(ent, ent.Comp);
    }
    private void OnPlayerDetached(Entity<MiGoReplacementComponent> ent, ref PlayerDetachedEvent args)
    {
        CheckTimerConditions(ent, ent.Comp);
    }
    private void RemoveReplacement(EntityUid uid, MiGoReplacementComponent replComp, MiGoComponent migoComp)
    {
        StopTimer(replComp);
        _migoSystem.MarkupForReplacement(uid, migoComp, false);
    }
    private void OnMindAdded(Entity<MiGoReplacementComponent> ent, ref MindAddedMessage args)
    {
        CheckTimerConditions(ent, ent.Comp);
    }
    private void OnMindRemoved(Entity<MiGoReplacementComponent> ent, ref MindRemovedMessage args)
    {
        CheckTimerConditions(ent, ent.Comp);
    }
    private void CheckTimerConditions(EntityUid uid, MiGoReplacementComponent replComp) 
    {
        if (_mobState.IsDead(uid)) //if you are dead = timer
            StartTimer(replComp);

        if (!TryComp<MindContainerComponent>(uid, out var mindComp) && (mindComp == null))
            return;

        if (mindComp.Mind == null) // if you ghosted = timer
            StartTimer(replComp);

        _mind.TryGetMind(uid, out var mind, out var userMidComp);

        if (userMidComp == null)
            return;

        if (userMidComp.Session == null) // if you left = timer
            StartTimer(replComp);

        if (TryComp<MiGoComponent>(uid, out var migoComp))
            RemoveReplacement(uid, replComp, migoComp);
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
            //if timer is on count it
            if (!replaceComp.ShouldBeCounted)
                continue;

            replaceComp.ReplacementTimer += frameTime;

            if (replaceComp.ReplacementTimer >= replaceComp.BeforeReplacemetTime)
            {
                _migoSystem.MarkupForReplacement(uid, migoComp, true);
            }
        }
    }
}
