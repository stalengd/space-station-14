using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles acsending to MiGo task
/// </summary>
public sealed class MiGoAliveConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

         SubscribeLocalEvent<MiGoAliveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        //SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnPersonAssigned);

        //SubscribeLocalEvent<PickRandomHeadComponent, ObjectiveAssignedEvent>(OnHeadAssigned);
    }
    private void OnGetProgress(Entity<MiGoAliveConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        int migoCount = 0;
        var query = EntityQueryEnumerator<MiGoComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var _, out var mobStateComp))
        {
            if (mobStateComp.CurrentState == MobState.Dead)
                continue;
            migoCount++;
        }

        args.Progress = migoCount / 3;
    }
}
