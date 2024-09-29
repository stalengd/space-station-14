// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Calculator;

namespace Content.Server.SS220.Calculator;

public sealed class CalculatorSystem : SharedCalculatorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SetCalculatorStateMessage>(OnCalculatorStateMessage);
        SubscribeLocalEvent<CalculatorComponent, BoundUIOpenedEvent>(OnCalculatorUIOpened);
    }

    protected override void OnChanged(Entity<CalculatorComponent> calculator)
    {
        Dirty(calculator);
    }

    private void OnCalculatorStateMessage(SetCalculatorStateMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;
        if (!TryGetEntity(message.Calculator, out var calculatorEntityOrNone) || calculatorEntityOrNone is not { } calculatorEntity)
            return;
        if (!TryComp<CalculatorComponent>(calculatorEntity, out var calculator))
            return;
        if (player != calculator.LastUser) // Ultimate anti-hack system here
            return;
        calculator.State = message.State;
        OnChanged((calculatorEntity, calculator));
    }

    private void OnCalculatorUIOpened(Entity<CalculatorComponent> calculator, ref BoundUIOpenedEvent args)
    {
        calculator.Comp.LastUser = args.Actor;
    }
}
