using Robust.Shared.Prototypes;

namespace Content.Shared.AW.Economy
{
    [Prototype("economyAccountIdPrototype")]
    public sealed partial class EconomyAccountIdPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;
        [DataField(required: false)]
        public string? Descriptior;
        [DataField(required: false)]
        public string Prefix = "";
        [DataField(required: false)]
        public uint Strik = 4;
        [DataField(required: false)]
        public uint NumbersPerStrik = 4;
    }
}