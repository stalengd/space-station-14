// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System;
using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Cult;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Actions;
using Content.Server.Polymorph.Systems;
using Content.Shared.Popups;

namespace Content.Server.SS220.Cult;

public sealed class CultSystem : SharedCultSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultComponent, CultCorruptItemEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultComponent, CultAscendingEvent>(AscendingAction);
    }
    protected override void OnCompInit(EntityUid uid, CultComponent comp, ComponentStartup args)
    {
        base.OnCompInit(uid, comp, args);

        _actions.AddAction(uid, ref comp.PukeShroomActionEntity, comp.PukeShroomAction);
        _actions.AddAction(uid, ref comp.CorruptItemActionEntity, comp.CorruptItemAction);
        _actions.AddAction(uid, ref comp.AscendingActionEntity, comp.AscendingAction);
    }

    private void CorruptItemAction(EntityUid uid, CultComponent comp, CultCorruptItemEvent args)//ToDo some list of corruption
    {
        /*
        if (!EntityManager.TryGetComponent(player, out HandsComponent? handsComponent))
        {
            _popupSystem.PopupEntity(Loc.GetString("wires-component-ui-on-receive-message-no-hands"), uid, player);
            return;
        }

        var activeHand = handsComponent.ActiveHand;
        */

        _polymorphSystem.PolymorphEntity(args.Target, "AdminBreadSmite");
    }

    private void AscendingAction(EntityUid uid, CultComponent comp, CultAscendingEvent args)
    {
        _polymorphSystem.PolymorphEntity(uid, "МиГо");//надо добавить взрыв оригинального тела.
    }
}
