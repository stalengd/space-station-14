using Content.Shared.Construction.Components;
using JetBrains.Annotations;

namespace Content.Client.SS220.CultYogg.UI
{
    [UsedImplicitly]
    public sealed class MiGoErectBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private MiGoErectLawMenu? _menu;
        private EntityUid _owner;
        //private List<SiliconLaw>? _laws;

        public MiGoErectBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _owner = owner;
        }

        protected override void Open()
        {
            base.Open();

            _menu = new();

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void Open()
        {
            base.Open();

            _menu = new();

            _menu.OnClose += Close;
            _menu.OpenCentered();
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

            if (state is not MiGoErectLawBuiState msg)
                return;

            _menu?.Update(_owner, msg);
        }
    }
}
