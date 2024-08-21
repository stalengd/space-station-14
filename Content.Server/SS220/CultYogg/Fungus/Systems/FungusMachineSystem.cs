using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Content.Shared.SS220.CultYogg.FungusMachineSystem;

namespace Content.Server.SS220.CultYogg.Fungus.Systems
{
    public sealed class FungusMachineSystem : SharedFungusMachineSystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            Subs.BuiEvents<FungusMachineComponent>(FungusMachineUiKey.Key, subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            });
        }


        protected override void OnComponentInit(EntityUid uid, FungusMachineComponent component, ComponentInit args)
        {
            base.OnComponentInit(uid, component, args);

            component.Container = _containerSystem.EnsureContainer<Container>(uid, FungusMachineComponent.ContainerId);
        }

        private void OnBoundUIOpened(EntityUid uid, FungusMachineComponent component, BoundUIOpenedEvent args)
        {
            UpdateFungusMachineInterfaceState(uid, component);
        }

        private void UpdateFungusMachineInterfaceState(EntityUid uid, FungusMachineComponent component)
        {
            var state = new FungusMachineInterfaceState(GetInventory(uid, component));

            _userInterfaceSystem.SetUiState(uid, FungusMachineUiKey.Key, state);
        }

        private FungusMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, FungusMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return null;

            return component.Inventory.GetValueOrDefault(entryId);
        }
    }
}

