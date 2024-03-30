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

    [DataField, AutoNetworkedField]
    public EntityUid? PukeShroomActionEntity;

    /// <summary>
    /// Wheter the Cultist is currently in physical form or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool PhysicalForm = false;

    /// <summary>
    /// Sound played whilepuking MiGoShroom
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier PukeSound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_gate_close.ogg", new()
    {
        MaxDistance = 7
    });
}
