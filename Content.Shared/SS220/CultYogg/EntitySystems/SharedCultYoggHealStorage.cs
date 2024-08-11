// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedCultYoggHealStorageSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
        // mb very bold
        SubscribeLocalEvent<CultYoggHealStorageComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<CultYoggHealStorageComponent, EntInsertedIntoContainerMessage>(GotInserted);
        SubscribeLocalEvent<CultYoggHealStorageComponent, EntGotRemovedFromContainerMessage>(GotRemoved);
        SubscribeLocalEvent<CultYoggHealStorageComponent, EntityTerminatingEvent>(GotTerminated);
    }

    private void GotTerminated(Entity<CultYoggHealStorageComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.InsertedEnt == null)
            return;

        RemComp<CultYoggHealComponent>(ent.Comp.InsertedEnt.Value);
    }

    private void OnInsertAttempt(Entity<CultYoggHealStorageComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        args.AssumeEmpty = true;

        if (!HasComp<MobStateComponent>(ent) || !HasComp<DamageableComponent>(ent))
        {
            args.Cancel();
            return;
        }

        if (!HasComp<CultYoggComponent>(ent))
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cult-yogg-heal-only-cultists"), ent, ent);

            args.Cancel();
            return;
        }
    }

    private void GotRemoved(Entity<CultYoggHealStorageComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RemComp<CultYoggHealComponent>(args.Entity);
    }

    private void GotInserted(Entity<CultYoggHealStorageComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        ent.Comp.InsertedEnt = args.Entity;
        EnsureComp<CultYoggHealComponent>(args.Entity);
    }
}
