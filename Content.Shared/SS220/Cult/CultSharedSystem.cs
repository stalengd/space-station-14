// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Actions;

namespace Content.Shared.SS220.Cult;

public abstract class SharedCultSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<CultComponent, CultPukeShroomEvent>(PukeAction);
        SubscribeLocalEvent<CultComponent, CultCorruptItemEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultComponent, CultAscendingEvent>(AscendingAction);
    }

    protected virtual void OnCompInit(EntityUid uid, CultComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.PukeShroomActionEntity, comp.PukeShroomAction);
        _actions.AddAction(uid, ref comp.CorruptItemActionEntity, comp.CorruptItemAction);
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
            //_popupSystem.PopupEntity(Loc.GetString("cult-corrupt-not-foun"), args.Args.Target.Value, args.Args.User);
            return;
        }

        /* ToDo Hastable
         if(!(args.Targer in List))
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-corrupt-not-foun"), args.Args.Target.Value, args.Args.User);
            return;
        }
         */

        var coords = Transform(args.Target).Coordinates;

        var corruptedEntity = Spawn("FoodSnackMREBrownieOpen", coords);

        _entityManager.AddComponent<CorruptedComponent>(corruptedEntity);//ToDo save previuos form

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(uid)} used corrupt on {ToPrettyString(args.Target)} and made {ToPrettyString(corruptedEntity)}");

        //Delete previous entity
        _entityManager.DeleteEntity(args.Target);
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
