// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt



using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.SS220.Buckle;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg;

public abstract class SharedCultYoggAltarSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultYoggAltarComponent, BuckleAttemptEvent>(OnBuckleAttempt);
    }

    private void OnBuckleAttempt(Entity<CultYoggAltarComponent> ent, ref BuckleAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.UserEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (!HasComp<CultYoggSacrificialComponent>(args.UserEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<BuckleComponent>(args.UserEntity, out var buckleComp) && buckleComp.Buckled)
        {
            args.Cancelled = true;
            return;
        }

    }

    protected void UpdateAppearance(EntityUid uid, CultYoggAltarComponent? altarComp = null,
    AppearanceComponent? appearanceComp = null)
    {
        if (!Resolve(uid, ref altarComp))
            return;

        if (!Resolve(uid, ref appearanceComp))
            return;

        _appearance.SetData(uid, CultYoggAltarComponent.CultYoggAltarVisuals.Sacrificed, altarComp.Used, appearanceComp);
    }
}
