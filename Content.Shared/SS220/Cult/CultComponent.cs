// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Cult;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCultSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class CultComponent : Component
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
    public SoundSpecifier PukeSound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_gate_open.ogg", new()
    {
        MaxDistance = 3
    });

    [ViewVariables, DataField, AutoNetworkedField]
    public string PukedEntity = "FoodMi'GomyceteCult"; //what we will puke out

    [ViewVariables, DataField, AutoNetworkedField]
    public string PukedLiquid = "PuddleVomit"; //maybe should be special liquid?
}
