// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Server.SS220.GameTicking.Rules;
using Robust.Shared.GameObjects;

namespace Content.Server.SS220.CultYogg;

public sealed partial class MiGoSystem : SharedMiGoSystem
{

    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    public override void Initialize()
    {
        base.Initialize();

        //UpdateEnslavementCharges(Entity < MiGoComponent > uid)
    }
    //Do not know how to set this on shared, cause cant call server system from shared
    private void UpdateEnslavementCharges(Entity<MiGoComponent> uid)
    {
        _cultRule.GetCultGameRule(out var cultRuleEnt, out var cultRuleComp);

        if (cultRuleComp == null)
            return;

        if (!TryComp<EntityTargetActionComponent>(uid.Comp.MiGoEnslavementActionEntity, out var actComp))
            return;

        actComp.Charges = 0;
    }
}
