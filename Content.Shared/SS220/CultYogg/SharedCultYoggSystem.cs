// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.SS220.CultYogg;

public abstract class SharedCultYoggSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly SharedCultYoggCorruptedSystem _cultYoggCorruptedSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<CultYoggComponent, CultYoggPukeShroomEvent>(PukeAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemInHandEvent>(CorruptItemInHandAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggAscendingEvent>(AscendingAction);
    }

    protected virtual void OnCompInit(EntityUid uid, CultYoggComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.PukeShroomActionEntity, comp.PukeShroomAction);
        _actions.AddAction(uid, ref comp.CorruptItemActionEntity, comp.CorruptItemAction);
        _actions.AddAction(uid, ref comp.CorruptItemInHandActionEntity, comp.CorruptItemInHandAction);
        _actions.AddAction(uid, ref comp.AscendingActionEntity, comp.AscendingAction);
    }

    private void PukeAction(EntityUid uid, CultYoggComponent comp, CultYoggPukeShroomEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsClient) // Have to do this because spawning stuff in shared is CBT.
            return;

        _entityManager.SpawnEntity(comp.PukedLiquid, Transform(uid).Coordinates);
        var shroom = _entityManager.SpawnEntity(comp.PukedEntity, Transform(uid).Coordinates);
        _audio.PlayPredicted(comp.PukeSound, uid, shroom);

        _hungerSystem.ModifyHunger(uid, -comp.HungerCost);

        /*
        if (TryComp<HungerComponent>(uid, out var hungerComp) // A check, just incase the doafter is somehow performed when the entity is not in the right hunger state.
        && _hungerSystem.IsHungerBelowState(uid, comp.MinHungerThreshold, hungerComp.CurrentHunger - comp.HungerCost, hungerComp))
        {
            _popupSystem.PopupClient(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }
        */

        args.Handled = true;

        //SharedSericultureSystem watch ref for staf here
    }

    private void CorruptItemAction(EntityUid uid, CultYoggComponent comp, CultYoggCorruptItemEvent args)
    {
        if (args.Handled)
            return;

        if (_cultYoggCorruptedSystem.IsCorrupted(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-already-corrupted"), args.Target, uid);
            return;
        }

        if (!_cultYoggCorruptedSystem.TryCorruptContinuously(uid, args.Target, false))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-no-proto"), uid);
            return;
        }
        args.Handled = true;
    }

    private void CorruptItemInHandAction(EntityUid uid, CultYoggComponent comp, CultYoggCorruptItemInHandEvent args)
    {
        if (args.Handled)
            return;
        if (!_entityManager.TryGetComponent<HandsComponent>(uid, out var hands))
            return;
        if (hands.ActiveHand == null)
            return;

        var handItem = hands.ActiveHand.HeldEntity;
        if (handItem == null)
            return;

        if (_cultYoggCorruptedSystem.IsCorrupted(handItem.Value))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-already-corrupted"), handItem.Value, uid);
            return;
        }

        if (!_cultYoggCorruptedSystem.TryCorruptContinuously(uid, handItem.Value, true))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-no-proto"), uid);
            return;
        }
        args.Handled = true;
    }

    private void AscendingAction(EntityUid uid, CultYoggComponent comp, CultYoggAscendingEvent args)
    {
        /* idk what is this
        if (!_timing.IsFirstTimePredicted)
            return;
        */

        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(comp.AscendedEntity, Transform(uid).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(uid, out var body))
        {
            _body.GibBody(uid, body: body);
        }
    }
}
