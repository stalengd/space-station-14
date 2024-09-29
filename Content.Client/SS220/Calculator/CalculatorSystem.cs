// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Calculator;

namespace Content.Client.SS220.Calculator;

public sealed class CalculatorSystem : SharedCalculatorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CalculatorComponent, BoundUIClosedEvent>(OnUserInterfaceClosed);
    }

    protected override void OnChanged(Entity<CalculatorComponent> calculator)
    {
        // No automatic state sync
    }

    private void OnUserInterfaceClosed(Entity<CalculatorComponent> calculator, ref BoundUIClosedEvent args)
    {
        RaiseNetworkEvent(new SetCalculatorStateMessage()
        {
            Calculator = GetNetEntity(calculator),
            State = calculator.Comp.State,
        });
    }
}
