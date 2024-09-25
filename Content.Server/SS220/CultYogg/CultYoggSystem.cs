// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Server.Humanoid;
using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Shared.Timing;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Content.Server.SS220.DarkForces.Saint.Reagent;
using Robust.Shared.Network;

namespace Content.Server.SS220.CultYogg;

public sealed class CultYoggSystem : SharedCultYoggSystem
{

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrinked);
        SubscribeLocalEvent<CultYoggComponent, ChangeCultYoggStageEvent>(UpdateStage);

    }

    private void UpdateStage(Entity<CultYoggComponent> entity, ref ChangeCultYoggStageEvent args)
    {
        Log.Error("AAAAAAAA");
        if (!HasComp<CultYoggComponent>(entity))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(entity, out var huAp))
            return;

        switch (entity.Comp.CurrentStage)
        {
            case 0:
                return;
            case 1:
                entity.Comp.PreviousEyeColor = new Color(huAp.EyeColor.R,huAp.EyeColor.G,huAp.EyeColor.B,huAp.EyeColor.A);
                huAp.EyeColor = Color.Green;
                break;
            case 2:
                if (!_prototype.HasIndex<MarkingPrototype>("CultStage-Halo"))
                {
                    Log.Error("CultStage-Halo marking doesn't exist");
                    return;
                }

                if (!huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Special))
                {
                    huAp.MarkingSet.Markings.Add(MarkingCategories.Special, new List<Marking>([new Marking("CultStage-Halo", colorCount:1)]));
                    Dirty(entity.Owner, huAp);
                }
                else
                {
                    _humanoidAppearance.SetMarkingId(entity.Owner,
                        MarkingCategories.Special,
                        0,
                        "CultStage-Halo",
                        huAp);
                }

                var newMarkingId = $"CultStage-{huAp.Species}";

                if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
                {
                    Log.Error($"{newMarkingId} marking doesn't exist");
                    return;
                }

                if (!huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Tail) &&
                    newMarkingId != "CultStage-Halo")
                {
                    if(huAp.MarkingSet.Markings[MarkingCategories.Tail].FirstOrDefault() != null)
                        entity.Comp.PreviousTail = huAp.MarkingSet.Markings[MarkingCategories.Tail].FirstOrDefault();
                    _humanoidAppearance.SetMarkingId(entity.Owner,
                        MarkingCategories.Tail,
                        0,
                        newMarkingId,
                        huAp);
                }
                break;
            case 3:
                //Here will be logic here to turn player into a migo
                break;
            default:
                Log.Error("Something went wrong with CultYogg stages");
                break;
        }
        Dirty(entity.Owner, huAp);
    }

    private void DeleteVisual(Entity<CultYoggComponent> entity)
    {
        if (!HasComp<CultYoggComponent>(entity))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(entity, out var huAp))
            return;
        huAp.EyeColor = entity.Comp.PreviousEyeColor;
        if (huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Special))
        {
            huAp.MarkingSet.Markings.Remove(MarkingCategories.Special);
        }

        if (!huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Tail) &&
            entity.Comp.PreviousTail != null)
        {
            _humanoidAppearance.SetMarkingId(entity.Owner,
                MarkingCategories.Tail,
                0,
                entity.Comp.PreviousTail.MarkingId,
                huAp);
        }
        Dirty(entity.Owner, huAp);
    }

    #region Ascending
    public void ModifyEatenShrooms(EntityUid uid, CultYoggComponent comp)//idk if it is canser or no, will be like that for a time
    {
        comp.ConsumedShrooms++; //Add shroom to buffer
        if (comp.ConsumedShrooms < comp.AmountShroomsToAscend) // if its not enough to ascend go next
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
        if (_actions.AddAction(uid, ref comp.AscendingActionEntity, out var act, comp.AscendingAction) && act.UseDelay != null)
        {
            var start = _timing.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(comp.AscendingActionEntity.Value, start, end);

            comp.IsAscending = true;
        }
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
        var query = EntityQueryEnumerator<CultYoggComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IsAscending) //ToDo set as checking status effect

                return true;
        }
        return false;
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
