using Content.Shared.Item;
using Content.Shared.SS220.CultYogg.Spikegun.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.SS220.CultYogg.Spikegun.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Network;

namespace Content.Server.SS220.CultYogg.Spikegun.Systems;

public abstract class SpikegunSystem : SharedSpikegunSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        //SubscribeLocalEvent<SpikegunComponent, TakeAmmoEvent>(OnUpdateAmmoCounter);

    }
}
