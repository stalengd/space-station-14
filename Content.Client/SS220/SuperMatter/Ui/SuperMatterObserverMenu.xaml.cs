// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SuperMatter.Functions;
using FancyWindow = Content.Client.UserInterface.Controls.FancyWindow;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Client.UserInterface.Controls;
using Content.Shared.SS220.SuperMatter.Ui;
using Content.Client.SS220.UserInterface.PlotFigure;
using Content.Client.SS220.SuperMatter.Observer;
using System.Numerics;
using System.Linq;
using Robust.Client.Graphics;
using Content.Shared.Atmos;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.SS220.SuperMatter.Ui;

[GenerateTypedNameReferences]
public sealed partial class SuperMatterObserverMenu : FancyWindow
{
    [Dependency] private readonly ILocalizationManager _localization = default!;

    public event Action<BaseButton.ButtonEventArgs, SuperMatterObserverComponent>? OnServerButtonPressed;
    public event Action<BaseButton.ButtonEventArgs, int>? OnCrystalButtonPressed;

    public SuperMatterObserverComponent? Observer;
    public int? CrystalKey;
    // Defines how much data point about the past we will show
    public const int MAX_DATA_LENGTH = 180;

    public SuperMatterObserverMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        PlotValueOverTime.SetLabels(_localization.GetString("smObserver-plotXLabel-integrity"), _localization.GetString("smObserver-plotYLabel-integrity"), _localization.GetString("smObserver-plotTitle-integrity"));

        ColorState.EvalFunctionOnMeshgrid(GetIntegrityDamageMap);
        ColorState.SetLabels(_localization.GetString("smObserver-plotXLabel-colorState"), _localization.GetString("smObserver-plotYLabel-colorState"), _localization.GetString("smObserver-plotTitle-colorState"));
        InitGasRatioBars();
    }
    public void LoadState(List<Entity<SuperMatterObserverComponent>> observerEntities)
    {
        ServerNavigationBar.RemoveAllChildren();
        foreach (var (observerUid, observerComp) in observerEntities)
        {
            var serverButton = new ServerButton
            {
                Text = observerUid.ToString(),
                StyleBoxOverride = new StyleBoxFlat(Color.DarkGray),
                ObserverComponent = observerComp,
                Margin = new Thickness(2, 0, 2, 0),
                ToggleMode = true,
                StyleClasses = { "OpenBoth" }
            };

            serverButton.OnPressed += args =>
            {
                OnServerButtonPressed?.Invoke(args, serverButton.ObserverComponent);
            };
            ServerNavigationBar.AddChild(serverButton);
        }
    }
    public void LoadCrystal()
    {
        CrystalNavigationBar.RemoveAllChildren();
        if (Observer == null)
            return;
        foreach (var (crystalKey, name) in Observer.Names)
        {
            var crystalButton = new CrystalButton
            {
                Text = name,
                StyleBoxOverride = new StyleBoxFlat(Color.DarkGray),
                CrystalKey = crystalKey,
                ToggleMode = true,
                Margin = new Thickness(2, 0, 2, 0),
                StyleClasses = { "OpenBoth" }
            };

            crystalButton.OnPressed += args =>
            {
                OnCrystalButtonPressed?.Invoke(args, crystalButton.CrystalKey);
            };
            CrystalNavigationBar.AddChild(crystalButton);
        }
    }
    public void LoadCachedData()
    {
        if (Observer == null
            || CrystalKey == null)
            return;

        if (!Observer.Names.ContainsKey(CrystalKey.Value))
            return;

        PlotValueOverTime.LoadPlot2DTimePoints(new PlotPoints2D(MAX_DATA_LENGTH, Observer.Integrities[CrystalKey.Value],
                                                        -1f, Observer.Integrities[CrystalKey.Value].Count));
        ColorState.LoadMovingPoint(new Vector2(Observer.Matters[CrystalKey.Value].Last().Value, Observer.InternalEnergy[CrystalKey.Value].Last().Value),
                                     new Vector2(Observer.Matters[CrystalKey.Value].Last().Derv, Observer.InternalEnergy[CrystalKey.Value].Last().Derv));
    }
    public void UpdateState(SuperMatterObserverUpdateState msg)
    {
        if (Observer == null
            || CrystalKey == null)
        {
            SetMessageDataToTextInfo();
            return;
        }
        if (msg.Id != CrystalKey)
            return;

        PlotValueOverTime.AddPointToPlot(new Vector2(PlotValueOverTime.GetLastAddedPointX() + msg.UpdateDelay, msg.Integrity));
        ColorState.LoadMovingPoint(new Vector2(msg.Matter.Value, msg.InternalEnergy.Value), new Vector2(msg.Matter.Derivative, msg.InternalEnergy.Derivative));
        SetMessageDataToTextInfo(msg);
        UpdateGasRatioBars(msg.GasRatios);
    }

    private void SetMessageDataToTextInfo(SuperMatterObserverUpdateState msg)
    {
        NameLabel.SetMessage(_localization.GetString("smObserver-name", ("name", msg.Name)));
        IntegrityLabel.SetMessage(_localization.GetString("smObserver-integrity", ("value", msg.Integrity.ToString("N2"))));
        PressureLabel.SetMessage(_localization.GetString("smObserver-pressure", ("value", msg.Pressure.ToString("N2"))));
        TemperatureLabel.SetMessage(_localization.GetString("smObserver-temperature", ("value", msg.Temperature.ToString("N2"))));
        MatterLabel.SetMessage(_localization.GetString("smObserver-matter", ("value", msg.Matter.Value.ToString("N2"))));
        InternalEnergyLabel.SetMessage(_localization.GetString("smObserver-internalEnergy", ("value", msg.InternalEnergy.Value.ToString("N2"))));
        DelamStatus.SetMessage(_localization.GetString("smObserver-delamStatus", ("status", _localization.GetString(msg.Delaminate.Delaminates.ToString()))));
        MolesAmount.SetMessage(_localization.GetString("smObserver-molesAmount", ("value", msg.TotalMoles.ToString("N2"))));
    }
    private void SetMessageDataToTextInfo()
    {
        var noneString = _localization.GetString("smObserver-none-value");
        NameLabel.SetMessage(_localization.GetString("smObserver-name", ("name", noneString)));
        IntegrityLabel.SetMessage(_localization.GetString("smObserver-integrity", ("value", noneString)));
        PressureLabel.SetMessage(_localization.GetString("smObserver-pressure", ("value", noneString)));
        TemperatureLabel.SetMessage(_localization.GetString("smObserver-temperature", ("value", noneString)));
        MatterLabel.SetMessage(_localization.GetString("smObserver-matter", ("value", noneString)));
        InternalEnergyLabel.SetMessage(_localization.GetString("smObserver-internalEnergy", ("value", noneString)));
        DelamStatus.SetMessage(_localization.GetString("smObserver-delamStatus", ("status", noneString)));
        MolesAmount.SetMessage(_localization.GetString("smObserver-molesAmount", ("value", noneString)));
    }
    private float GetIntegrityDamageMap(float matter, float internalEnergy)
    {
        return SuperMatterFunctions.EnergyToMatterDamageFactorFunction(internalEnergy
                - SuperMatterFunctions.SafeInternalEnergyToMatterFunction(matter / SuperMatterFunctions.MatterNondimensionalization),
            matter / SuperMatterFunctions.MatterNondimensionalization);
    }
    private void InitGasRatioBars()
    {
        foreach (var gas in Enum.GetValues<Gas>())
        {
            var gasBar = new SuperMatterObserverGasBar();
            gasBar.Initialize(gas);
            GasContainer.AddChild(gasBar);
        }
    }
    private void UpdateGasRatioBars(Dictionary<Gas, float> gasRatios)
    {
        foreach (var child in GasContainer.Children)
        {
            if (child is not SuperMatterObserverGasBar gasBar)
                return;

            gasBar.UpdateBar(gasRatios[gasBar.GasId]);
        }
    }

    private sealed class ServerButton : Button
    {
        public SuperMatterObserverComponent? ObserverComponent;
    }
    private sealed class CrystalButton : Button
    {
        public int CrystalKey;
    }
}

