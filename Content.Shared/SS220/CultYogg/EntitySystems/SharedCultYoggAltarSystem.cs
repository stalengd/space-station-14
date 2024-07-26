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
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultYoggAltarComponent, BuckleAttemptEvent>(OnBuckleAttempt);
    }

    private void OnBuckleAttempt(Entity<CultYoggAltarComponent> ent, ref BuckleAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.UserEntity))
            args.Cancelled = true;

        if (!HasComp<CultYoggSacrificialComponent>(args.UserEntity))
            args.Cancelled = true;

        if (args.BuckledEntity == args.UserEntity)
            args.Cancelled = true;

    }
}
