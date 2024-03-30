using Content.Server.NPC.Components;
using Content.Shared.Dataset;
using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(CultRuleSystem))]
public sealed partial class CultRuleComponent : Component
{
    public readonly List<EntityUid> CultistMinds = new();

    [DataField]
    public ProtoId<AntagPrototype> CultPrototypeId = "Cultist";

    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> CultFaction = "Cult";

    [DataField]
    public ProtoId<WeightedRandomPrototype> ObjectiveGroup = "CultObjectiveGroups";

    public int TotalTraitors => CultistMinds.Count;
    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToStart = 1,
        Started = 2,
    }

    /// <summary>
    /// Current state of the rule
    /// </summary>
    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;

    /// <summary>
    /// When should traitors be selected and the announcement made
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? AnnounceAt;

    /// <summary>
    ///     Path to cultist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/SS220/Ambience/Antag/cult_start.ogg");
}
