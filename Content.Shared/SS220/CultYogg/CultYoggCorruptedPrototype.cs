// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg
{
    /// <summary>
    ///     Recipes for corruption
    /// </summary>
    [Prototype("corrupted")]

    [Serializable, NetSerializable]
    public sealed partial class CultYoggCorruptedPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("corruptedEntity")]
        public string? Start { get; private set; }

        [DataField("result")]
        public string? Result { get; private set; }

        [DataField("corruptionReverseEffect")]
        public string? CorruptionReverseEffect { get; private set; }
    }
}
