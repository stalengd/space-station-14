// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.Bible;

[RegisterComponent]
/// <summary>
/// Adds ability to perform exorcism with this component on item. 
/// </summary>
public sealed partial class ExorcismPerformerComponent : Component
{
    [DataField("requiresBibleUser")]
    public bool RequiresBibleUser = true;

    [DataField("exorcismAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ExorcismAction = "ActionExorcism";
    [DataField("exorcismActionEntity")]
    public EntityUid? ExorcismActionEntity;

    [DataField("messageLengthMin")]
    public int MessageLengthMin = 60;
    [DataField("messageLengthMax")]
    public int MessageLengthMax = 120;

    [DataField("range")]
    public float Range = 10f;

    [DataField("lightEffectDuration")]
    public float LightEffectDurationSeconds = 1f;

    [DataField("lightBehaviourId")]
    public string LightBehaviourId = "exorcism_performance";
}
