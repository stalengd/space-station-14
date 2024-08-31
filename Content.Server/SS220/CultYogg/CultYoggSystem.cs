// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Shared.Timing;
using System;

namespace Content.Server.SS220.CultYogg;

public sealed class CultYoggSystem : SharedCultYoggSystem
{

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
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
            var start = _gameTiming.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(comp.AscendingActionEntity.Value, start, end);

            comp.IsAscending = true;
        }
    }

    //Check for avaliable amoiunt of MiGo or gib MiGo to replace
    private bool AvaliableMiGoCheck()
    {
        //Check number of MiGo in gamerule
        _cultRule.GetCultGameRuleComp(out var ruleComp);

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
            if (comp.IsAscending)
                return true;
        }
        return false;
    }
    #endregion
}
