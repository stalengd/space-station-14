using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.CultYogg;
using Content.Client.Overlays;

namespace Content.Client.SS220.CultYogg;

public sealed class ShowCultYoggIconsSystem : EquipmentHudSystem<ShowCultYoggIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string JobIconForNoId = "JobIconNoId";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowCultYoggIconsComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeLocalEvent<CultYoggSacrificialComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent2);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, ShowCultYoggIconsComponent _, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        var iconId = JobIconForNoId;

        if (TryComp<ShowCultYoggIconsComponent>(uid, out var cultComp))
        {
            iconId = cultComp.StatusIcon;
        }

        if (_prototype.TryIndex<StatusIconPrototype>(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
        else
            Log.Error($"Invalid job icon prototype: {iconPrototype}");
    }
    private void OnGetStatusIconsEvent2(EntityUid uid, CultYoggSacrificialComponent _, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        var iconId = JobIconForNoId;

        if (TryComp<CultYoggSacrificialComponent>(uid, out var sacrComp))
        {
            iconId = sacrComp.StatusIcon;
        }

        if (_prototype.TryIndex<StatusIconPrototype>(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
        else
            Log.Error($"Invalid job icon prototype: {iconPrototype}");
    }
}
