using Content.Shared.Item;
using Content.Shared.SS220.CultYogg.Spikegun.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.SS220.CultYogg.Spikegun.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Item;
using Content.Shared.Clothing;
using Content.Shared.Actions;
using System.Linq;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.Spikegun.Systems;


public abstract class SharedSpikegunSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedItemSystem Item = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;




    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpikegunComponent, SpikegunServerEvent>(OnUpdateAmmoCounter);
    }

    private void OnUpdateAmmoCounter(Entity<SpikegunComponent> entity, ref SpikegunServerEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (TryComp<BasicEntityAmmoProviderComponent>(entity.Owner, out BasicEntityAmmoProviderComponent? comp)
            && TryComp<ItemComponent>(entity.Owner, out var item))
        {
            if (comp.Count < 1)
            {
                Log.Info("State before: " + item.HeldPrefix);
                Item.SetHeldPrefix(entity.Owner,  null , component: item);
                Appearance.SetData(entity.Owner, ToggleVisuals.Toggled, false);
                Log.Info("Changed to noammo");
                Log.Info("State after: " + item.HeldPrefix);
            }
            else
            {
                Log.Info("State before: " + item.HeldPrefix);
                Item.SetHeldPrefix(entity.Owner, "fullammo", component: item);
                Appearance.SetData(entity.Owner, ToggleVisuals.Toggled, true);
                Log.Info("Changed to fullammo");
                Log.Info("State after: " + item.HeldPrefix);
            }
            Dirty(entity.Owner, entity.Comp);
        }
    }

}

public sealed partial class SpikegunServerEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public enum ToggleVisuals : byte
{
    Toggled,
    Layer
}
