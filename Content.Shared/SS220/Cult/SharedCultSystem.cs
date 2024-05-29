// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Nutrition.EntitySystems;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared.SS220.Cult;

public abstract class SharedCultSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<CultComponent, CultPukeShroomEvent>(PukeAction);
        SubscribeLocalEvent<CultComponent, CultCorruptItemEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultComponent, CultCorruptItemInHandEvent>(CorruptItemInHandAction);
        SubscribeLocalEvent<CultComponent, CultAscendingEvent>(AscendingAction);
        SubscribeLocalEvent<CultComponent, CultCorruptDoAfterEvent>(CorruptOnDoAfter);
    }

    protected virtual void OnCompInit(EntityUid uid, CultComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.PukeShroomActionEntity, comp.PukeShroomAction);
        _actions.AddAction(uid, ref comp.CorruptItemActionEntity, comp.CorruptItemAction);
        _actions.AddAction(uid, ref comp.CorruptItemInHandActionEntity, comp.CorruptItemInHandAction);
        _actions.AddAction(uid, ref comp.AscendingActionEntity, comp.AscendingAction);
    }

    private void PukeAction(EntityUid uid, CultComponent comp, CultPukeShroomEvent args)
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
    private void CorruptItemAction(EntityUid uid, CultComponent comp, CultCorruptItemEvent args)//ToDo some list of corruption
    {
        if (args.Handled)
            return;

        if (!CheckForCorruption(args.Target, out var corruption))
        {
            //_popup.PopupClient(Loc.GetString("cult-corrupt-no-protod"), uid, PopupType.SmallCaution);
            _popup.PopupEntity(Loc.GetString("cult-corrupt-no-proto"), uid);
            return;
        }

        if (_entityManager.HasComponent<CorruptedComponent>(args.Target))
        {
            //_popup.PopupCursor(Loc.GetString("cult-corrupt-already-corrupted"), PopupType.SmallCaution); //somehow isn't working
            _popup.PopupEntity(Loc.GetString("cult-corrupt-already-corrupted"), args.Target, uid);
            return;
        }


        var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3), new CultCorruptDoAfterEvent(corruption, false), uid, args.Target)//ToDo estimate time for corruption
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };


        _doAfter.TryStartDoAfter(doafterArgs);

        args.Handled = true;
    }
    private void CorruptItemInHandAction(EntityUid uid, CultComponent comp, CultCorruptItemInHandEvent args)//ToDo some list of corruption
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

        if (!CheckForCorruption((EntityUid) handItem, out var corruption))
        {
            //_popup.PopupClient(Loc.GetString("cult-corrupt-no-protod"), uid, PopupType.SmallCaution);
            _popup.PopupEntity(Loc.GetString("cult-corrupt-no-proto"), uid);
            return;
        }

        var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3), new CultCorruptDoAfterEvent(corruption, true), uid, handItem)//ToDo estimate time for corruption
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = false,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(doafterArgs);

        args.Handled = true;
    }
    private bool CheckForCorruption(EntityUid uid, [NotNullWhen(true)] out CultCorruptedPrototype? corruption)//if item in list of corrupted
    {
        var idOfEnity = _entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype!.ID;

        foreach (var entProto in _prototypeManager.EnumeratePrototypes<CultCorruptedPrototype>())//idk if it isn't shitcode
        {
            if (idOfEnity == entProto.ID)
            {
                corruption = entProto;
                return true;
            }
        }
        corruption = null;
        return false;
    }
    private void CorruptOnDoAfter(EntityUid uid, CultComponent component, CultCorruptDoAfterEvent args)//DoAfter for corruption
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        if (_net.IsClient)
            return;

        if (args.Proto == null)
            return;

        var coords = Transform((EntityUid) args.Target).Coordinates;

        var corruptedEntity = Spawn(args.Proto.Result, coords);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(uid)} used corrupt on {ToPrettyString(args.Target)} and made {ToPrettyString(corruptedEntity)}");

        //ToDo if object is a storage, it should drop all its items

        //Every corrupted entity should have this  entity at start
        _entityManager.AddComponent<CorruptedComponent>(corruptedEntity);//ToDo save previuos form here, so delete it when you do all the corrupted list
        if (!_entityManager.TryGetComponent<CorruptedComponent>(corruptedEntity, out var corrupted))
            return;

        corrupted.PreviousForm = "";

        //Delete previous entity
        _entityManager.DeleteEntity(args.Target);


        if (args.InHand)
            _hands.PickupOrDrop(uid, corruptedEntity);

        args.Handled = true;
    }
    private void AscendingAction(EntityUid uid, CultComponent comp, CultAscendingEvent args)
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
