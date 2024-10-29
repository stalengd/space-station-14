// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using System.Text;
using Content.Client.Stylesheets;
using Content.Client.SS220.UserInterface.Utility;
using Content.Shared.SS220.Calculator;
using Content.Shared.SS220.Input;
using Content.Shared.SS220.Utility;
using Robust.Client.AutoGenerated;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Animations;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;
using Robust.Shared.Random;

using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client.SS220.Calculator.UI;

[GenerateTypedNameReferences]
public sealed partial class CalculatorMenu : BaseWindow
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Animatable]
    public Vector2 BodyOffset
    {
        get => new(MainContainer.Margin.Left, -MainContainer.Margin.Top);
        set => MainContainer.Margin = new Thickness(value.X, -value.Y, -value.X, value.Y);
    }

    private readonly CalculatorStyle _style;
    private readonly ButtonsListenerCollection _buttons = new();
    private readonly TimeSpan _buttonPressedDuration = TimeSpan.FromSeconds(0.1);
    private readonly TimeSpan _minLifetime = TimeSpan.FromSeconds(0.5);
    private StringBuffer _numberTextBuffer;
    private CalculatorBoundUserInterface? _owner;
    private bool _isClosedWithButton;
    private TimeSpan _lifeTimer;

    private const string ANIMATION_OPEN = "Open";
    private const string ANIMATION_CLOSE = "Close";
    private const string ANIMATION_PRESS = "Press";

    public CalculatorMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _style = new();
        Stylesheet = _style.Create(IoCManager.Resolve<IStylesheetManager>().SheetSpace, _resourceCache);
    }

    public CalculatorMenu(CalculatorBoundUserInterface owner) : this()
    {
        _owner = owner;

        _buttons.ListenButton(ButtonDigit0, _ => _owner.OnDigitPressed(0));
        _buttons.ListenButton(ButtonDigit1, _ => _owner.OnDigitPressed(1));
        _buttons.ListenButton(ButtonDigit2, _ => _owner.OnDigitPressed(2));
        _buttons.ListenButton(ButtonDigit3, _ => _owner.OnDigitPressed(3));
        _buttons.ListenButton(ButtonDigit4, _ => _owner.OnDigitPressed(4));
        _buttons.ListenButton(ButtonDigit5, _ => _owner.OnDigitPressed(5));
        _buttons.ListenButton(ButtonDigit6, _ => _owner.OnDigitPressed(6));
        _buttons.ListenButton(ButtonDigit7, _ => _owner.OnDigitPressed(7));
        _buttons.ListenButton(ButtonDigit8, _ => _owner.OnDigitPressed(8));
        _buttons.ListenButton(ButtonDigit9, _ => _owner.OnDigitPressed(9));
        _buttons.ListenButton(ButtonDot, _ => _owner.OnDotPressed());
        _buttons.ListenButton(ButtonPlus, _ => _owner.OnAddPressed());
        _buttons.ListenButton(ButtonMinus, _ => _owner.OnSubtractPressed());
        _buttons.ListenButton(ButtonMultiply, _ => _owner.OnMultiplyPressed());
        _buttons.ListenButton(ButtonDivide, _ => _owner.OnDividePressed());
        _buttons.ListenButton(ButtonClear, _ => _owner.OnClearPressed());
        _buttons.ListenButton(ButtonClearEntry, _ => _owner.OnClearEntryPressed());
        _buttons.ListenButton(ButtonEquals, _ => _owner.OnEqualsPressed());
        _buttons.ListenButton(ButtonClose, _ =>
        {
            _isClosedWithButton = true;
            Close();
        });

        _buttons.ListenAllButtons(OnButtonPressed);
    }

    protected override void Opened()
    {
        var builder = CommandBinds.Builder;
        BindButtonToKey(builder, KeyFunctions220.CalculatorType0, ButtonDigit0);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType1, ButtonDigit1);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType2, ButtonDigit2);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType3, ButtonDigit3);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType4, ButtonDigit4);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType5, ButtonDigit5);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType6, ButtonDigit6);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType7, ButtonDigit7);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType8, ButtonDigit8);
        BindButtonToKey(builder, KeyFunctions220.CalculatorType9, ButtonDigit9);
        BindButtonToKey(builder, KeyFunctions220.CalculatorTypeAdd, ButtonPlus);
        BindButtonToKey(builder, KeyFunctions220.CalculatorTypeSubtract, ButtonMinus);
        BindButtonToKey(builder, KeyFunctions220.CalculatorTypeMultiply, ButtonMultiply);
        BindButtonToKey(builder, KeyFunctions220.CalculatorTypeDivide, ButtonDivide);
        BindButtonToKey(builder, KeyFunctions220.CalculatorTypeDot, ButtonDot);
        BindButtonToKey(builder, KeyFunctions220.CalculatorEnter, ButtonEquals);
        BindButtonToKey(builder, KeyFunctions220.CalculatorClear, ButtonClear);
        BindButtonToKey(builder, EngineKeyFunctions.CloseModals, ButtonClose);
        builder.Register<CalculatorMenu>();

        PlayAnimation(_style.OpenAnimation, ANIMATION_OPEN);
    }

    public override void Close()
    {
        // Yeah, this system will not carry 2 windows well, but I dont care
        CommandBinds.Unregister<CalculatorMenu>();
        // This is mainly to filter out windows that
        // will be created and closed rapidly after closing window (robust bug or soomething).
        if (_lifeTimer < _minLifetime)
        {
            base.Close();
            return;
        }
        // Press UI button even if closed via Esc or other means,
        // return cos pressing close button will lead us back here again. 
        if (!_isClosedWithButton)
        {
            _buttons.PressButton(ButtonClose, _buttonPressedDuration);
            return;
        }
        // We want to really close window only when close animation completes.
        AnimationCompleted += animKey =>
        {
            if (animKey != ANIMATION_CLOSE)
                return;
            // Now we right in FrameUpdate() method execution and can not just close
            // window because that will break controls iteration, so defer it.
            UserInterfaceManager.DeferAction(() =>
            {
                base.Close();
            });
        };

        if (HasRunningAnimation(ANIMATION_CLOSE))
            return;
        PlayAnimation(_style.CloseAnimation, ANIMATION_CLOSE);
    }

    public void SetNumber(decimal number, byte? fractionLength)
    {
        var builder = _numberTextBuffer.BeginFormat();
        WriteNumber(builder, number, fractionLength);
        _numberTextBuffer.EndFormat();
        NumberDisplayLabel.TextMemory = _numberTextBuffer;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        var deltaSpan = TimeSpan.FromSeconds(args.DeltaSeconds);
        _lifeTimer += deltaSpan;
        _buttons.Update(deltaSpan);
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        return DragMode.Move;
    }

    private void BindButtonToKey(CommandBinds.BindingsBuilder builder, BoundKeyFunction function, BaseButton uiButton)
    {
        builder.Bind(function, new PointerInputCmdHandler((in PointerInputCmdArgs args) =>
        {
            if (args.State != BoundKeyState.Down)
                return false;
            _buttons.PressButton(uiButton, _buttonPressedDuration, new BaseButton.ButtonEventArgs(uiButton,
                new GUIBoundKeyEventArgs(function, args.State, args.ScreenCoordinates, true, Vector2.Zero, Vector2.Zero)));
            return true;
        }, false, true));
    }

    private static void WriteNumber(StringBuilder builder, decimal number, byte? fractionLength)
    {
        var intPart = DecimalMath.CastToIntOrDefault(number);
        var decPart = DecimalMath.CastToIntOrDefault((Math.Abs(number) - Math.Abs(intPart)) * DecimalMath.GetPowerOfTen(number.Scale));

        if (intPart == 0 && decimal.IsNegative(number))
        {
            builder.Append('-');
        }
        builder.Append(intPart);
        if (!fractionLength.HasValue)
            return;
        builder.Append('.');
        if (fractionLength.Value == 0)
            return;
        var decPartLength = DecimalMath.GetDecimalLength(decPart);
        for (var i = 0; i < number.Scale - decPartLength; i++)
        {
            builder.Append('0');
        }
        if (decPart > 0)
            builder.Append(decPart);
        for (var i = 0; i < fractionLength.GetValueOrDefault(0) - number.Scale; i++)
        {
            builder.Append('0');
        }
    }

    private void OnButtonPressed(BaseButton.ButtonEventArgs? args)
    {
        _owner?.OnButtonPressed();

        if (HasRunningAnimation(ANIMATION_CLOSE))
            return;

        Vector2 offset;
        if (args?.Button is { } button)
        {
            var localCenter = button.Rect.Center;
            var relativeNormalized = localCenter / MainContainer.Size * 2f - Vector2.One;
            offset = relativeNormalized * new Vector2(3, 0) + new Vector2(0, _random.Next(-5, -3));
        }
        else
        {
            offset = _random.NextVector2Box(-2, -7, 2, -5);
        }
        StopAnimation(ANIMATION_PRESS);
        PlayAnimation(_style.PressAnimation(offset), ANIMATION_PRESS);
    }
}