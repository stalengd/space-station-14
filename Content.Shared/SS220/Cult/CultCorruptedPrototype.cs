using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Cult
{
    /// <summary>
    ///     Prototype representing a tag in YAML.
    ///     Meant to only have an ID property, as that is the only thing that
    ///     gets saved in TagComponent.
    /// </summary>
    [Prototype("corrupted")]

    [Serializable, NetSerializable]
    public sealed partial class CultCorruptedPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("corruptedEntity")]
        public string? Start { get; private set; }

        [DataField("result")]
        public string? Result { get; private set; }
    }
}
