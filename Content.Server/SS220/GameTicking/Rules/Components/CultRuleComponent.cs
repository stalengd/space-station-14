// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Dataset;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(CultRuleSystem))]
public sealed partial class CultRuleComponent : Component
{
    [DataField]
    public Dictionary<string, string> InitialCultistsNames = new();//Who was cultist on the gamestart.

    [DataField]
    public Dictionary<string, string> CultistsNames = new();

    public readonly List<EntityUid> CultistMinds = new();

    [DataField]
    public ProtoId<AntagPrototype> CultPrototypeId = "Cult";

    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> CultFaction = "Cult";

    [DataField]
    public ProtoId<WeightedRandomPrototype> ObjectiveGroup = "CultObjectiveGroups";

    [DataField]
    public bool Summoned = false;

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
    /// When should cultists be selected and the announcement made
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? AnnounceAt;

    /// <summary>
    ///     Path to cultist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/SS220/Ambience/Antag/cult_start.ogg");
}
