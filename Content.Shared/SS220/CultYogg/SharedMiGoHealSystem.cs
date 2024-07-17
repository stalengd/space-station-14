using Content.Shared.StatusEffect;

namespace Content.Shared.SS220.CultYogg;

public abstract class SharedMiGoHealSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string EffectkKey = "MiGoHeal";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
    public void TryApplyMiGoHeal(EntityUid uid, float time, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, EffectkKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<RaveComponent>(uid, EffectkKey, TimeSpan.FromSeconds(time), true, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, EffectkKey, TimeSpan.FromSeconds(time), status);
        }
    }

    public void TryRemoveMiGoHeal(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, EffectkKey);
    }
    public void TryRemoveMiGoHealTime(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, EffectkKey, TimeSpan.FromSeconds(timeRemoved));
    }

}
