using Content.Server.Objectives.Systems;

namespace Content.Server.SS220.Objectives;

/// <summary>
/// Requires that a target completes half of their objectives.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(CultYoggSummonProgressConditionSystem))]
public sealed partial class CultYoggSummonProgressConditionComponent : Component
{
}
