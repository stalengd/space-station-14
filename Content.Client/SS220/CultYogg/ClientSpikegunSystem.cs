using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.SS220.CultYogg.Spikegun.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.SS220.CultYogg.Spikegun.Components;
using static Content.Client.Weapons.Ranged.Systems.GunSystem;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.GameObjects;
using System.Linq;

namespace Content.Client.SS220.CultYogg;

public sealed class ClientSpikegunSystem : SharedSpikegunSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpikegunComponent, UpdateAmmoCounterEvent>(OnUpdateAmmoCounter);

    }

    private void OnUpdateAmmoCounter(Entity<SpikegunComponent> entity, ref UpdateAmmoCounterEvent args)
    {
        RaiseLocalEvent(entity.Owner, new SpikegunServerEvent());
    }
}

