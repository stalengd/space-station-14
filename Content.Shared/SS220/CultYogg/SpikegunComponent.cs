using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.CultYogg.Spikegun.Systems;


namespace Content.Shared.SS220.CultYogg.Spikegun.Components;

[RegisterComponent, NetworkedComponent(), AutoGenerateComponentState]
[Access(typeof(SharedSpikegunSystem))]
public sealed partial class SpikegunComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleSpikegun";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField("on"), AutoNetworkedField]
    public bool On;
}
