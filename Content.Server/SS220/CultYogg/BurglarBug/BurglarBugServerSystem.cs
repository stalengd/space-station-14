using Content.Server.Administration.Logs;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Sticky.Components;
using Content.Server.Sticky.Events;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Sticky;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.BurglarBug;

public sealed class BurglarBugServerSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BurglarBugComponent, TriggerEvent>(HandleTrigger);
        SubscribeLocalEvent<BurglarBugComponent, UseInHandEvent>(OnActivate);
        SubscribeLocalEvent<BurglarBugComponent, AttemptEntityStickEvent>(OnStick);
        SubscribeLocalEvent<BurglarBugComponent, StickyDoAfterEvent>(OnBreak);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BurglarBugComponent>();
        while (query.MoveNext(out var uid, out var bugComponent))
        {
            if (bugComponent.DoorOpenTime == null || bugComponent.DoorOpenTime > _gameTiming.CurTime)
                continue;

            OpenDoor(uid, bugComponent);
        }
    }
    private void OpenDoor(EntityUid uid, BurglarBugComponent component)
    {
            if (!EntityManager.TryGetComponent<StickyComponent>(uid, out var stickyComponent))
                return;

            if (!EntityManager.TryGetComponent<DoorComponent>(stickyComponent.StuckTo,
                    out var doorComponent))
                return;

            if(doorComponent.State == DoorState.Closed)
                _doorSystem.StartOpening(doorComponent.Owner, doorComponent);

            if (!EntityManager.TryGetComponent<DoorBoltComponent>(stickyComponent.StuckTo,
                    out var doorBoltComponent))
                return;
            _doorSystem.SetBoltsDown((doorBoltComponent.Owner, doorBoltComponent), true);

            if (!EntityManager.TryGetComponent<AccessReaderComponent>(stickyComponent.StuckTo,
                    out var accessReaderComponent))
                return;
            accessReaderComponent.AccessLists.Clear();

            AfterUsed((uid, component));
        }
    private void OnBreak(Entity<BurglarBugComponent> entity, ref StickyDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            if (!EntityManager.TryGetComponent<AccessReaderComponent>(entity.Comp.Door, out var accessReaderComponent))
                return;
            accessReaderComponent.AccessLists = new List<HashSet<ProtoId<AccessLevelPrototype>>> (entity.Comp.AccessLists);
            entity.Comp.Activated = false;
        }
        else
        {
            entity.Comp.DoorOpenTime = _gameTiming.CurTime + TimeSpan.FromSeconds(entity.Comp.TimeToOpen);
        }
    }
    private void OnActivate(Entity<BurglarBugComponent> entity, ref UseInHandEvent args)
    {
        entity.Comp.Activated = true;
    }

    private void OnStick(Entity<BurglarBugComponent> entity, ref AttemptEntityStickEvent args)
    {
        if (entity.Comp.OpenedDoorStickPopupCancellation != null)
        {
            if (EntityManager.TryGetComponent<DoorComponent>(args.Target,
                    out var doorComponent) &&  doorComponent.State != DoorState.Closed)
            {
                args.Cancelled = true;
                RaiseLocalEvent(entity.Owner, new DroppedEvent(args.User), true);
                var msg = Loc.GetString(entity.Comp.OpenedDoorStickPopupCancellation);
                _popupSystem.PopupEntity(msg, args.User, PopupType.MediumCaution);
                _handsSystem.TryDrop(args.User);
                _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(args.User):actor} has droped the item {ToPrettyString(entity.Owner):item}");
                return;
            }

        }
        if (!entity.Comp.Activated)
        {
            if (entity.Comp.NotActivatedStickPopupCancellation != null)
            {
                var msg = Loc.GetString(entity.Comp.NotActivatedStickPopupCancellation);
                _popupSystem.PopupEntity(msg, args.User, PopupType.MediumCaution);
            }
            args.Cancelled = true;
            return;
        }
        if (!_access.GetMainAccessReader(args.Target, out var accessReaderComponent))
                return;
        entity.Comp.Door = accessReaderComponent.Owner;
        entity.Comp.AccessLists = new List<HashSet<ProtoId<AccessLevelPrototype>>>(accessReaderComponent.AccessLists);
        accessReaderComponent.AccessLists.Clear();
        _access.SetAccesses(accessReaderComponent.Owner, accessReaderComponent, ["Some hard code"]);
    }

    private void HandleTrigger(Entity<BurglarBugComponent> entity, ref TriggerEvent args)
    {
        var (uid, component) = entity;

        if (!EntityManager.TryGetComponent<StickyComponent>(uid, out var stickyComponent))
            return;

        if (stickyComponent.StuckTo == null)
        {
            var damage = new DamageSpecifier(component.Damage);
            foreach (var target
                     in _entityLookupSystem.GetComponentsInRange<MobStateComponent>(
                         _transform.GetMapCoordinates(entity.Owner), component.DamageRange))
            {
                _damageable.TryChangeDamage(target.Owner, damage, component.IgnoreResistances);
            }
            AfterUsed(entity);
        }
    }

    private void AfterUsed(Entity<BurglarBugComponent> entity)
    {
        EntityManager.DeleteEntity(entity);
    }
}
