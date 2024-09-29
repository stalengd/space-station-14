// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Calculator;

[Serializable, NetSerializable]
public enum CalculatorUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CalculatorBoundUIState : BoundUserInterfaceState
{
    public CalculatorState State;
}

[Serializable, NetSerializable]
public sealed class SetCalculatorStateMessage : EntityEventArgs
{
    public NetEntity Calculator;
    public CalculatorState State;
}
