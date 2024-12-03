// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Content.Shared.SS220.CultYogg.Corruption;
using Content.Shared.Inventory.Events;

namespace Content.Server.SS220.CultYogg.Corruption;

public sealed class CultYoggCocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggCocoonComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CultYoggWeaponComponent, DroppedEvent>(OnUnequip);
    }
    private void OnUseInHand(Entity<CultYoggCocoonComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var coords = Transform(args.User).Coordinates;
        var newEnt = Spawn(ent.Comp.Item, coords);

        if (TryComp<CultYoggCorruptedComponent>(ent, out var corruptComp))
        {
            var comp = EnsureComp<CultYoggCorruptedComponent>(newEnt);
            comp.SoftDeletedOriginalEntity = corruptComp.SoftDeletedOriginalEntity;
            comp.Recipe = corruptComp.Recipe;
        }
        EntityManager.DeleteEntity(ent);
        _hands.PickupOrDrop(args.User, newEnt);
        if (ent.Comp.Sound != null)
        {
            // The entity is often deleted, so play the sound at its position rather than parenting
            //var coordinates = Transform(ent).Coordinates;
            _audio.PlayPredicted(ent.Comp.Sound, args.User, args.User);
        }

        args.Handled = true;
    }
    private void OnUnequip(Entity<CultYoggWeaponComponent> ent, ref DroppedEvent args)
    {
        ;
    }
}
