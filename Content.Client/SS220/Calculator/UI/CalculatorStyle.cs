// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.StyleTools;
using Content.Client.SS220.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Calculator.UI;

public sealed class CalculatorStyle : QuickStyle
{
    protected override void CreateRules()
    {
        Builder
            .Element<PanelContainer>()
            .Class("CalculatorPanel")
            .Prop(PanelContainer.StylePropertyPanel,
                StrechedStyleBoxTexture(Tex("/Textures/SS220/Interface/Calculator/calculator-body.png")));

        Builder
            .Element<Label>()
            .Class("CalculatorDisplay")
            .Prop(Label.StylePropertyFont, SpriteFont("CalculatorDisplayFont"));

        AddButtonStyle("CalculatorButtonDigit1", "/Textures/SS220/Interface/Calculator/buttons.rsi", "1");
        AddButtonStyle("CalculatorButtonDigit2", "/Textures/SS220/Interface/Calculator/buttons.rsi", "2");
        AddButtonStyle("CalculatorButtonDigit3", "/Textures/SS220/Interface/Calculator/buttons.rsi", "3");
        AddButtonStyle("CalculatorButtonDigit4", "/Textures/SS220/Interface/Calculator/buttons.rsi", "4");
        AddButtonStyle("CalculatorButtonDigit5", "/Textures/SS220/Interface/Calculator/buttons.rsi", "5");
        AddButtonStyle("CalculatorButtonDigit6", "/Textures/SS220/Interface/Calculator/buttons.rsi", "6");
        AddButtonStyle("CalculatorButtonDigit7", "/Textures/SS220/Interface/Calculator/buttons.rsi", "7");
        AddButtonStyle("CalculatorButtonDigit8", "/Textures/SS220/Interface/Calculator/buttons.rsi", "8");
        AddButtonStyle("CalculatorButtonDigit9", "/Textures/SS220/Interface/Calculator/buttons.rsi", "9");
        AddButtonStyle("CalculatorButtonDot", "/Textures/SS220/Interface/Calculator/buttons.rsi", "dot");
        AddButtonStyle("CalculatorButtonPlus", "/Textures/SS220/Interface/Calculator/buttons.rsi", "plus");
        AddButtonStyle("CalculatorButtonMinus", "/Textures/SS220/Interface/Calculator/buttons.rsi", "minus");
        AddButtonStyle("CalculatorButtonMultiply", "/Textures/SS220/Interface/Calculator/buttons.rsi", "multiply");
        AddButtonStyle("CalculatorButtonDivide", "/Textures/SS220/Interface/Calculator/buttons.rsi", "divide");
        AddButtonStyle("CalculatorButtonClear", "/Textures/SS220/Interface/Calculator/buttons.rsi", "c");
        AddButtonStyle("CalculatorButtonClearEntry", "/Textures/SS220/Interface/Calculator/buttons.rsi", "ce");
        AddButtonStyle("CalculatorButtonClose", "/Textures/SS220/Interface/Calculator/buttons.rsi", "power");

        AddButtonStyle("CalculatorButtonDigit0", "/Textures/SS220/Interface/Calculator/buttons-long.rsi", "0");
        AddButtonStyle("CalculatorButtonEquals", "/Textures/SS220/Interface/Calculator/buttons-long.rsi", "equals");
    }

    private void AddButtonStyle(string className, string rsiPath, string state)
    {
        Builder
            .Element<SpriteButton>()
                .Class(className)
                .Prop(SpriteButton.StylePropertySprite, Sprite(rsiPath, state))
            .Element<SpriteButton>()
                .Class(className)
                .Pseudo(SpriteButton.StylePseudoClassPressed)
                .Prop(SpriteButton.StylePropertySprite, Sprite(rsiPath, $"{state}_pressed"));
    }
}
