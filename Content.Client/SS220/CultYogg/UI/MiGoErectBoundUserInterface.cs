// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using JetBrains.Annotations;
using Content.Shared.SS220.CultYogg;

namespace Content.Client.SS220.CultYogg.UI;

[UsedImplicitly]
public sealed class MiGoErectBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MiGoErectMenu? _menu;
    private EntityUid _owner;

    public MiGoErectBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(this);

        _menu.OnClose += Close;
        _menu.OpenCenteredLeft();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MiGoErectBuiState msg)
            return;

        _menu?.Update(_owner, msg);
    }

    public void OnBuildingSelect(CultYoggBuildingPrototype building)
    {
        SendMessage(new MiGoErectBuildingSelectedMessage()
        {
            BuildingId = building.ID,
        });
    }
}
