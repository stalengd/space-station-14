using Content.Shared.SS220.CultYogg;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.CultYogg;

public sealed class CultYoggSacrificialSystem : SharedCultYoggSacrificialSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggSacrificialComponent, GetStatusIconsEvent>(GetSacraficialIcon);
    }
    private void GetSacraficialIcon(Entity<CultYoggSacrificialComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
