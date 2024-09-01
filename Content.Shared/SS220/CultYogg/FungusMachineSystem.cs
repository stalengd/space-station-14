using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.FungusMachineSystem;

public abstract partial class SharedFungusMachineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FungusMachineComponent, ComponentInit>(OnComponentInit);
    }

    protected virtual void OnComponentInit(EntityUid uid, FungusMachineComponent component, ComponentInit args)
    {
        RestockInventoryFromPrototype(uid, component);
    }

    public void RestockInventoryFromPrototype(EntityUid uid, FungusMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (!_prototypeManager.TryIndex(component.PackPrototypeId, out FungusMachineInventoryPrototype? packPrototype))
            return;

        AddInventoryFromPrototype(uid, packPrototype.StartingInventory, component);
    }

    public List<FungusMachineInventoryEntry> GetInventory(EntityUid uid, FungusMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();
        var inventory = new List<FungusMachineInventoryEntry>(component.Inventory.Values);
        return inventory;
    }

    private void AddInventoryFromPrototype(EntityUid uid, Dictionary<string, uint>? entries, FungusMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component) || entries == null)
        {
            return;
        }

        Dictionary<string, FungusMachineInventoryEntry> inventory;
        inventory = component.Inventory;

        foreach (var (id, amount) in entries)
        {
            if (!_prototypeManager.HasIndex<EntityPrototype>(id))
                continue;

            var restock = amount;
            inventory.Add(id, new FungusMachineInventoryEntry(id, restock));
        }
    }
}


[NetSerializable, Serializable]
public sealed class FungusMachineInterfaceState : BoundUserInterfaceState
{
    public List<FungusMachineInventoryEntry> Inventory;

    public FungusMachineInterfaceState(List<FungusMachineInventoryEntry> inventory)
    {
        Inventory = inventory;
    }
}

[Serializable, NetSerializable]
public sealed class FungusSelectedID : BoundUserInterfaceMessage
{
    public readonly string ID;
    public FungusSelectedID( string id)
    {
        ID = id;
    }
}

[Serializable, NetSerializable]
public enum FungusMachineUiKey
{
    Key,
}
