// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;
using Content.Shared.Antag;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;


namespace Content.Shared.SS220.Cult;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCultSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class CultComponent : Component, IAntagStatusIconComponent
{
    /// ABILITIES ///
    [DataField]
    public EntProtoId PukeShroomAction = "ActionCultPukeShroom";

    [DataField]
    public EntProtoId AscendingAction = "ActionCultAscending";

    [DataField]
    public EntProtoId CorruptItemAction = "ActionCultCorruptItem";

    [DataField]
    public EntProtoId CorruptItemInHandAction = "ActionCultCorruptItemInHand";

    [DataField, AutoNetworkedField]
    public EntityUid? PukeShroomActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? CorruptItemActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? CorruptItemInHandActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? AscendingActionEntity;

    /// <summary>
    /// Sound played while puking MiGoShroom
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier PukeSound = new SoundPathSpecifier("/Audio/SS220/Cult/puke.ogg", new()
    {
        MaxDistance = 3
    });

    [ViewVariables, DataField, AutoNetworkedField]
    public string PukedEntity = "FoodMi'GomyceteCult"; //what will be puked out

    [ViewVariables, DataField, AutoNetworkedField]
    public string PukedLiquid = "PuddleVomit"; //maybe should be special liquid?

    /// <summary>
    /// Entity the cultist will ascend into
    /// </summary>
    public string AscendedEntity = "MiGoCult";

    public int ConsumedShrooms = 0; //buffer

    public const int NeededForAscended = 3;//How many shrooms need to be consumed before ascension

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current hunger of the mob doing micoz
    /// </summary>

    [ViewVariables, DataField, AutoNetworkedField]
    public float HungerCost = 5f;

    /// <summary>
    /// The role prototype of the zombie antag role
    /// </summary>
    [DataField("cultRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string CultRoleId = "Cultist";

    [DataField("cultStatusIcon")]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "CultFaction";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;
}
