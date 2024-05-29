using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Cult
{
    /// <summary>
    ///  Resecpie for corruption of animals
    /// </summary>
    [Prototype("corruptedAnimals")]

    [Serializable, NetSerializable]
    public sealed partial class CultCorruptedAnimalsPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("corruptedAnimal")]
        public string? Start { get; private set; }

        [DataField("result")]
        public string? Result { get; private set; }
    }
}
