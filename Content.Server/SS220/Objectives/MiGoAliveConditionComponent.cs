using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Component for a Acsension task for cultists
/// </summary>
[RegisterComponent, Access(typeof(MiGoAliveConditionSystem))]
public sealed partial class MiGoAliveConditionComponent : Component
{
}
