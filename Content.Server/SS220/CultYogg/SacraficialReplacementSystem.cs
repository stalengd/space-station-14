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
using Content.Shared.Actions;
using System.Collections.Generic;
using JetBrains.FormatRipper.Elf;
using Content.Shared.Clothing;

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

    private Dictionary<(EntityUid, NetUserId), TimeSpan> _entityEnteredSSDTimes = new();

    private TimeSpan _BeforeReplacementCooldown = TimeSpan.FromSeconds(300);//ToDo set timer

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }
    private void OnPlayerAttached(Entity<CultYoggSacrificialComponent> ent, ref PlayerAttachedEvent args)
    {
        _entityEnteredSSDTimes.Remove((ent, args.Player.UserId));
    }

    private void OnPlayerDetached(Entity<CultYoggSacrificialComponent> ent, ref PlayerDetachedEvent args)
    {
        _entityEnteredSSDTimes.Add((ent, args.Player.UserId), _timing.CurTime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var pair in _entityEnteredSSDTimes)
        {
            if (_timing.CurTime < pair.Value + _BeforeReplacementCooldown)
                continue;

            var ev = new SacraficialReplacementEvent(pair.Key.Item1, pair.Key.Item2);
            RaiseLocalEvent(pair.Key.Item1, ref ev, true);

            _entityEnteredSSDTimes.Remove(pair.Key);
        }
    }
}

[ByRefEvent, Serializable]
public sealed class SacraficialReplacementEvent : EntityEventArgs
{
    public readonly EntityUid Entity;
    public readonly NetUserId Player;

    public SacraficialReplacementEvent(EntityUid entity, NetUserId player)
    {
        Entity = entity;
        Player = player;
    }
}
