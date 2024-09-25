// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Shared.Timing;
using Content.Shared.SS220.CultYogg;
using Content.Shared.Mind;

namespace Content.Server.SS220.CultYogg;

public sealed class CultYoggSystem : SharedCultYoggSystem
{

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrinked);
        SubscribeLocalEvent<CultYoggComponent, CultYoggForceAscendingEvent>(ForcedAcsending);
        SubscribeLocalEvent<CultYoggComponent, CultYoggAscendingEvent>(AscendingAction);

    }
    #region Ascending
    private void AscendingAction(Entity<CultYoggComponent> uid, ref CultYoggAscendingEvent args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(uid.Comp.AscendedEntity, Transform(uid).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(uid, out var body))
            _body.GibBody(uid, body: body);
    }
    private void ForcedAcsending(Entity<CultYoggComponent> uid, ref CultYoggForceAscendingEvent args)
    {

        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(uid.Comp.AscendedEntity, Transform(uid).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(uid, out var body))
            _body.GibBody(uid, body: body);
    }
    #endregion

    #region Ascending
    public void ModifyEatenShrooms(EntityUid uid, CultYoggComponent comp)//idk if it is canser or no, will be like that for a time
    {
        comp.ConsumedShrooms++; //Add shroom to buffer
        if (comp.ConsumedShrooms <= comp.AmountShroomsToAscend) // if its not enough to ascend
            return;

        if (!AcsendingCultistCheck())//to prevent becaming MiGo at the same time
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-have-acsending"), uid, uid);
            return;
        }

        if (!AvaliableMiGoCheck() && !TryReplaceMiGo())//if amount of migo < required amount of migo or have 1 to replace
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-migo-full"), uid, uid);
            return;
        }

        //Maybe in later version we will detiriorate the body and add some kind of effects

        //IDK how to check if he already has this action, so i did this markup
        EnsureComp<AcsendingComponent>(uid);
    }

    //Check for avaliable amoiunt of MiGo or gib MiGo to replace
    private bool AvaliableMiGoCheck()
    {
        //Check number of MiGo in gamerule
        _cultRule.GetCultGameRule(out var ruleComp);

        if (ruleComp is null)
            return false;

        int reqMiGo = ruleComp.ReqAmountOfMiGo;

        var query = EntityQueryEnumerator<MiGoComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            reqMiGo--;
        }

        if (reqMiGo > 0)
            return true;

        return false;
    }
    private bool TryReplaceMiGo()
    {
        //if any MiGo needs to be replaced add here
        List<EntityUid> migoOnDelete = new();

        var query = EntityQueryEnumerator<MiGoComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.MayBeReplaced)
                migoOnDelete.Add(uid);
        }

        if ((migoOnDelete.Count != 0) && TryComp<BodyComponent>(migoOnDelete[0], out var body)) //ToDo check for cancer coding
        {
            _body.GibBody(migoOnDelete[0], body: body);
            return true;
        }

        return false;
    }
    private bool AcsendingCultistCheck()//if anybody else is acsending
    {
        var query = EntityQueryEnumerator<CultYoggComponent, AcsendingComponent>();
        while (query.MoveNext(out var uid, out var comp, out var acsComp))
        {
            return false;
        }
        return true;
    }
    #endregion
    private void OnSaintWaterDrinked(Entity<CultYoggComponent> uid, ref OnSaintWaterDrinkEvent args)
    {
        EnsureComp<CultYoggCleansedComponent>(uid, out var cleansedComp);
        cleansedComp.AmountOfHolyWater += args.SaintWaterAmount;

        if (cleansedComp.AmountOfHolyWater >= cleansedComp.AmountToCleance)
            RemComp<CultYoggComponent>(uid);

        cleansedComp.CleansingDecayEventTime = _timing.CurTime + cleansedComp.BeforeDeclinesTime; //setting timer, when cleansing will be removed
    }
}
[ByRefEvent, Serializable]
public record struct CultYoggForceAscendingEvent;
