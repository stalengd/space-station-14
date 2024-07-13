using Content.Shared.SS220.CultYogg;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.CultYogg;

public sealed class CultYoggSystem : SharedCultYoggSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, GetStatusIconsEvent>(GetCultYoggIcon);
    }
    private void GetCultYoggIcon(Entity<CultYoggComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
