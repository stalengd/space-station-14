// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.EUI;
using Robust.Shared.Player;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Enums;

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

    public override void Initialize()
    {
        base.Initialize();
        _playerManager.PlayerStatusChanged += OnPlayerChange;

        SubscribeLocalEvent<MiGoReplacementComponent, MobStateChangedEvent>(OnMobState);
    }
    
    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerChange;
    }

    private void StartTimer(MiGoReplacementComponent comp)
    {
        comp.ShouldBeCounted = true;
        comp.ReplacementTimer = 0;
    }

    private void StopTimer(MiGoReplacementComponent comp)
    {
        comp.ShouldBeCounted = false;
        comp.ReplacementTimer = 0;
        comp.MayBeReplaced = false;
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
        var query = EntityQueryEnumerator<MiGoReplacementComponent>();
        while (query.MoveNext(out var uid, out var repl))
        {
            if (!repl.ShouldBeCounted)
                continue;

            repl.ReplacementTimer += frameTime;

            if (repl.ReplacementTimer >= repl.BeforeReplacemetTime)
            {
                repl.MayBeReplaced = true;
            }

        }
    }

    private void OnPlayerChange(object? sender, SessionStatusEventArgs e) //ToDo maybe rewrite it in dictionary us TeleportAFKtoCryoSystem
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Disconnected:
                if (e.Session.AttachedEntity is null
                    || !HasComp<MindContainerComponent>(e.Session.AttachedEntity)
                    || !HasComp<BodyComponent>(e.Session.AttachedEntity))
                {
                    break;
                }

                if (TryComp<MiGoReplacementComponent>(e.Session.AttachedEntity, out var compRelp1))
                    StartTimer(compRelp1);
                break;

            case SessionStatus.Connected:
                if(TryComp<MiGoReplacementComponent>(e.Session.AttachedEntity, out var compRelp2))
                    StopTimer(compRelp2);
                break;
        }
    }
}
