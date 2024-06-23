using Robust.Shared.GameStates;
using Content.Server.SS220.CultYogg.Nyarlathotep.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Components;

[Access(friends: typeof(SharedNyarlathotepHorizonSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class NyarlathotepHorizonComponent : Component
{
    [DataField("radius")]
    public float Radius;

    [DataField]
    public bool ConsumeEntities = true;

    [DataField("consumerFixtureId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ConsumerFixtureId;

    [DataField("colliderFixtureId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ColliderFixtureId;

    [DataField("consumePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TargetConsumePeriod = TimeSpan.FromSeconds(0.5);

    [ViewVariables(VVAccess.ReadOnly), DataField("nextConsumeWaveTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextConsumeWaveTime;
}
