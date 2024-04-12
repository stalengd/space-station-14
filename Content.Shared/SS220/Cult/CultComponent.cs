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

    [DataField, AutoNetworkedField]
    public EntityUid? PukeShroomActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? CorruptItemActionEntity;

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
    public string PukedEntity = "FoodMi'GomyceteCult"; //what we will puke out

    [ViewVariables, DataField, AutoNetworkedField]
    public string PukedLiquid = "PuddleVomit"; //maybe should be special liquid?

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
