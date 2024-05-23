// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<CultComponent, CultPukeShroomEvent>(PukeAction);
        SubscribeLocalEvent<CultComponent, CultCorruptItemEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultComponent, CultCorruptItemInHandEvent>(CorruptItemInHandAction);
        SubscribeLocalEvent<CultComponent, CultAscendingEvent>(AscendingAction);
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
        _entityManager.SpawnEntity(comp.PukedLiquid, Transform(uid).Coordinates);
        var shroom = _entityManager.SpawnEntity(comp.PukedEntity, Transform(uid).Coordinates);
        _audio.PlayPredicted(comp.PukeSound, uid, shroom);
    }
    private void CorruptItemAction(EntityUid uid, CultComponent comp, CultCorruptItemEvent args)//ToDo some list of corruption
    {
        if (_entityManager.HasComponent<CorruptedComponent>(args.Target))
        {
            //_popup.PopupCursor(Loc.GetString("cult-corrupt-already-corrupted"), PopupType.SmallCaution); //somehow isn't working
            _popup.PopupEntity(Loc.GetString("cult-corrupt-already-corrupted"), args.Target, uid);
            return;
        }
        /* ToDo Hastable
         if(!(args.Targer in List))
        {
        }

            _popupSystem.PopupEntity(Loc.GetString("cult-corrupt-not-found"), args.Args.Target.Value, args.Args.User);
            return;
        }
         */

        var coords = Transform(args.Target).Coordinates;

        var corruptedEntity = Spawn("FoodSnackMREBrownieOpen", coords);

        _entityManager.AddComponent<CorruptedComponent>(corruptedEntity);//ToDo save previuos form here

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(uid)} used corrupt on {ToPrettyString(args.Target)} and made {ToPrettyString(corruptedEntity)}");

        //Delete previous entity
        _entityManager.DeleteEntity(args.Target);
    }
    private void CorruptItemInHandAction(EntityUid uid, CultComponent comp, CultCorruptItemInHandEvent args)//ToDo some list of corruption
    {
        if (!_entityManager.TryGetComponent<HandsComponent>(uid, out var hands))
            return;

        if (hands.ActiveHand == null)
            return;

        var handItem = hands.ActiveHand.HeldEntity;

        if (handItem == null)
            return;

        if (_entityManager.HasComponent<CorruptedComponent>(handItem))
        {
            //_popup.PopupClient(Loc.GetString("cult-corrupt-already-corrupted"), uid, PopupType.SmallCaution);
            _popup.PopupEntity(Loc.GetString("cult-corrupt-already-corrupted"), uid);
            return;
        }

        /* ToDo Hastable
          if(!(args.Targer in List))
         {
             _popupSystem.PopupEntity(Loc.GetString("cult-corrupt-not-found"), args.Args.Target.Value, args.Args.User);
             return;
         }
         */

        var coords = Transform(uid).Coordinates;

        var corruptedEntity = Spawn("FoodSnackMREBrownieOpen", coords);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(uid)} used corrupt on {ToPrettyString(handItem)} and made {ToPrettyString(corruptedEntity)}");

        //Delete previous entity
        _entityManager.DeleteEntity(handItem);

        _entityManager.AddComponent<CorruptedComponent>(corruptedEntity);//ToDo save previuos form here

        _hands.PickupOrDrop(uid, corruptedEntity);
    }
    private void AscendingAction(EntityUid uid, CultComponent comp, CultAscendingEvent args)
    {
        /* idk what is this
        if (!_timing.IsFirstTimePredicted)
            return;
        */

        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition("MiGoCult", Transform(uid).Coordinates);


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
