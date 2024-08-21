// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server.SS220.CultYogg;

public sealed class CultYoggMiGoReplacementSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        _playerManager.PlayerStatusChanged += OnPlayerChange;
    }
    
    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerChange;
    }
        
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }
    
    private void OnPlayerChange(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Disconnected:
                if (e.Session.AttachedEntity is null || !HasComp<MindContainerComponent>(e.Session.AttachedEntity) || !HasComp<BodyComponent>(e.Session.AttachedEntity))
                {
                    break;
                }

                if (!_preferencesManager.TryGetCachedPreferences(e.Session.UserId, out var preferences)|| preferences.SelectedCharacter is not HumanoidCharacterProfile humanoidPreferences)
                {
                    break;
                }
                _entityEnteredSSDTimes[(e.Session.AttachedEntity.Value, e.Session.UserId)] = (_gameTiming.CurTime, humanoidPreferences.TeleportAfkToCryoStorage);
                break;
            case SessionStatus.Connected:
                if (_entityEnteredSSDTimes.TryFirstOrNull(item => item.Key.Item2 == e.Session.UserId, out var item))
                {
                    _entityEnteredSSDTimes.Remove(item.Value.Key);
                }

                break;
        }
    }
}
