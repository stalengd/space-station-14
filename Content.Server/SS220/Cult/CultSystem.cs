// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Cult;
using Robust.Shared.Prototypes;
using Content.Server.Actions;
using Content.Server.Polymorph.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using Content.Server.Mind;
using Robust.Shared.Audio.Systems;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;

namespace Content.Server.SS220.Cult;

public sealed class CultSystem : SharedCultSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
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
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = EntityManager.SpawnAtPosition("MiGoCult", Transform(uid).Coordinates);


        // Move the mind if there is one and it's supposed to be transferred
        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, migo, mind: mind);


        //ToDo set Migo special name
        //_metaData.SetEntityName(uid, GetTitle(target.Value, comp.Title), MetaData(uid));

        //Gib original body
        if (TryComp<BodyComponent>(uid, out var body))
        {
            _bodySystem.GibBody(uid, body: body);
        }
    }
}
