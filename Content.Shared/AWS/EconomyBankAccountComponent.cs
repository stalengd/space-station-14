using Robust.Shared.Prototypes;
using Content.Shared.Store;

namespace Content.Shared.AW.Economy
{
    [RegisterComponent]
    public sealed partial class EconomyBankAccountComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ProtoId<CurrencyPrototype> AllowCurrency;
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ProtoId<EconomyAccountIdPrototype> AccountIdByProto;

        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ulong Balance = 0;
        [ViewVariables(VVAccess.ReadWrite), DataField(required: false)]
        public ulong Penalty = 0;
        [ViewVariables(VVAccess.ReadWrite), DataField()]
        public string AccountId = "NO VALUE";
        [ViewVariables(VVAccess.ReadWrite), DataField(required: false)]
        public string AccountName = "";
        [ViewVariables(VVAccess.ReadWrite), DataField(required: false)]
        public bool Blocked = false;
    }
}