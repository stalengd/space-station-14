// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Client.Player;

namespace Content.Client.SS220.CultYogg;

public sealed class ShowCultYoggIconsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [ValidatePrototypeId<FactionIconPrototype>]
    private const string JobIconForNoId = "JobIconNoId";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowCultYoggIconsComponent, GetStatusIconsEvent>(OnGetCultistsIconsEvent);
        SubscribeLocalEvent<CultYoggSacrificialComponent, GetStatusIconsEvent>(OnGetSacraficialIconsEvent);
    }

    private void OnGetCultistsIconsEvent(Entity<ShowCultYoggIconsComponent> uid, ref GetStatusIconsEvent ev)
    {
        var iconId = JobIconForNoId;

        if (TryComp<ShowCultYoggIconsComponent>(uid, out var cultComp))
        {
            iconId = cultComp.StatusIcon;
        }

        if (_prototype.TryIndex<FactionIconPrototype>(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
        else
            Log.Error($"Invalid faction icon prototype: {iconPrototype}");
    }
    private void OnGetSacraficialIconsEvent(Entity<CultYoggSacrificialComponent> uid, ref GetStatusIconsEvent ev)
    {
        var viewer = _playerManager.LocalSession?.AttachedEntity;
        if (viewer == uid)
            return;

        var iconId = JobIconForNoId;

        if (TryComp<CultYoggSacrificialComponent>(uid, out var sacrComp))
        {
            iconId = sacrComp.StatusIcon;
        }

        if (_prototype.TryIndex<FactionIconPrototype>(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
        else
            Log.Error($"Invalid faction icon prototype: {iconPrototype}");
    }
}
