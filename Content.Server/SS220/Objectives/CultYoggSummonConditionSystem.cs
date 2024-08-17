// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.Objectives.Components;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.SS220.CultYogg.Components;

namespace Content.Server.SS220.Objectives;

/// <summary>
/// Handle amount of sacrafices 
/// </summary>
public sealed class CultYoggSummonConditionSystem : EntitySystem
{
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggSummonConditionComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<CultYoggSummonConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);

        SubscribeLocalEvent<CultYoggSummonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }
    //check if gamerule was rewritten
    private void OnInit(Entity<CultYoggSummonConditionComponent> ent, ref ComponentInit args)
    {
        _cultRule.GetCultGameRuleComp(out var ruleComp);

        if (ruleComp is null)
            return;

        ent.Comp.reqSacrAmount = ruleComp.ReqAmountOfMiGo;
    }

    private void OnAfterAssign(Entity<CultYoggSummonConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {


        string title = "Призовите темного бога, принеся в жертву:";

        var query = EntityQueryEnumerator<CultYoggSacrificialComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            var targetName = "Unknown";
            if (TryComp<MindComponent>(uid, out var mind) && mind.CharacterName != null)
            {
                targetName = mind.CharacterName;
            }

            var jobName = _job.MindTryGetJobName(uid);

            title += "\n" + targetName + " в должности " + jobName;
        }
        /*
         _cultRule.GetCultGameRuleComp(out var ruleComp);

        if (ruleComp is null)
            return;
        foreach (var target in ruleComp.SacraficialsList)
        {
            var targetName = "Unknown";
            if (TryComp<MindComponent>(target, out var mind) && mind.CharacterName != null)
            {
                targetName = mind.CharacterName;
            }

            var jobName = _job.MindTryGetJobName(target);

            title += "\n" + targetName + " в должности " + jobName;
        }
        */

        _metaData.SetEntityName(ent, title, args.Meta);
    }
    private void OnGetProgress(Entity<CultYoggSummonConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        _cultRule.GetCultGameRuleComp(out var ruleComp);

        if (ruleComp is null)
            return;

        args.Progress = ruleComp.AmountOfSacrifices / ent.Comp.reqSacrAmount;
    }
}
