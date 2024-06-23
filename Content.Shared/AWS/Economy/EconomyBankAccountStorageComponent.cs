namespace Content.Shared.AW.Economy
{
    [RegisterComponent]
    public sealed partial class EconomyBankAccountStorageComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField()]
        public List<EconomyBankAccountComponent> Accounts = new();
    }
}