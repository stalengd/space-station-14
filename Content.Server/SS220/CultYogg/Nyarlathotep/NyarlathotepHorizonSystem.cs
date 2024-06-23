using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Ghost;
using Content.Server.Administration.Logs;
using Content.Server.Station.Components;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using Content.Server.SS220.CultYogg.Nyarlathotep.EntitySystems;
using Content.Server.SS220.CultYogg.Nyarlathotep.Events;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.CultYogg;


namespace Content.Server.SS220.CultYogg.Nyarlathotep;

public sealed class NyarlathotepHorizonSystem : SharedNyarlathotepHorizonSystem
{
    #region Dependencies
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    #endregion Dependencies

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapGridComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<GhostComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<StationDataComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<MobStateComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsumeMobs);
        SubscribeLocalEvent<NyarlathotepHorizonComponent, MapInitEvent>(OnHorizonMapInit);
        SubscribeLocalEvent<NyarlathotepHorizonComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<NyarlathotepHorizonComponent, EntGotInsertedIntoContainerMessage>(OnNyarlathotepHorizonContained);
        SubscribeLocalEvent<NyarlathotepHorizonContainedEvent>(OnNyarlathotepHorizonContained);
        SubscribeLocalEvent<ContainerManagerComponent, NyarlathotepHorizonConsumedEntityEvent>(OnContainerConsumed);

        var vvHandle = Vvm.GetTypeHandler<NyarlathotepHorizonComponent>();
        vvHandle.AddPath(nameof(NyarlathotepHorizonComponent.TargetConsumePeriod), (_, comp) => comp.TargetConsumePeriod, SetConsumePeriod);
    }

    private void OnHorizonMapInit(EntityUid uid, NyarlathotepHorizonComponent component, MapInitEvent args)
    {
        component.NextConsumeWaveTime = _timing.CurTime;
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<NyarlathotepHorizonComponent>();
        vvHandle.RemovePath(nameof(NyarlathotepHorizonComponent.TargetConsumePeriod));

        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NyarlathotepHorizonComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var NyarlathotepHorizon, out var xform))
        {
            var curTime = _timing.CurTime;
            if (NyarlathotepHorizon.NextConsumeWaveTime <= curTime)
                Update(uid, NyarlathotepHorizon, xform);
        }
    }

    public void Update(EntityUid uid, NyarlathotepHorizonComponent? NyarlathotepHorizon = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref NyarlathotepHorizon))
            return;

        NyarlathotepHorizon.NextConsumeWaveTime += NyarlathotepHorizon.TargetConsumePeriod;

        if (!Resolve(uid, ref xform))
            return;

        // Handle singularities some admin smited into a locker.
        if (_containerSystem.TryGetContainingContainer(uid, out var container, transform: xform)
        && !AttemptConsumeEntity(uid, container.Owner, NyarlathotepHorizon))
        {
            // Locker is indestructible. Consume everything else in the locker instead of magically teleporting out.
            ConsumeEntitiesInContainer(uid, container, NyarlathotepHorizon, container);
            return;
        }

        if (NyarlathotepHorizon.Radius > 0.0f)
            ConsumeEverythingInRange(uid, NyarlathotepHorizon.Radius, xform, NyarlathotepHorizon);
    }

    #region Consume

    #region Consume Entities

    public void ConsumeEntity(EntityUid hungry, EntityUid morsel, NyarlathotepHorizonComponent NyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        if (!EntityManager.IsQueuedForDeletion(morsel) // I saw it log twice a few times for some reason?
        && (HasComp<MindContainerComponent>(morsel)
            || _tagSystem.HasTag(morsel, "HighRiskItem")))
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.Extreme, $"{ToPrettyString(morsel)} entered the event horizon of {ToPrettyString(hungry)} and was deleted");
        }

        EntityManager.QueueDeleteEntity(morsel);
        var evSelf = new NyarlathotepConsumedByEventHorizonEvent(morsel, hungry, NyarlathotepHorizon, outerContainer);
        var evEaten = new NyarlathotepHorizonConsumedEntityEvent(morsel, hungry, NyarlathotepHorizon, outerContainer);
        RaiseLocalEvent(hungry, ref evSelf);
        RaiseLocalEvent(morsel, ref evEaten);
    }

    public bool AttemptConsumeEntity(EntityUid hungry, EntityUid morsel, NyarlathotepHorizonComponent NyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        if (!CanConsumeEntity(hungry, morsel, NyarlathotepHorizon))
            return false;

        ConsumeEntity(hungry, morsel, NyarlathotepHorizon, outerContainer);
        return true;
    }

    public bool CanConsumeEntity(EntityUid hungry, EntityUid uid, NyarlathotepHorizonComponent NyarlathotepHorizon)
    {
        var ev = new NyarlathotepHorizonAttemptConsumeEntityEvent(uid, hungry, NyarlathotepHorizon);
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }

    public void ConsumeEntitiesInRange(EntityUid uid, float range, TransformComponent? xform = null, NyarlathotepHorizonComponent? NyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref xform, ref NyarlathotepHorizon))
            return;

        var range2 = range * range;
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var epicenter = _xformSystem.GetWorldPosition(xform, xformQuery);
        foreach (var entity in _lookup.GetEntitiesInRange(_xformSystem.GetMapCoordinates(uid, xform), range, flags: LookupFlags.Uncontained))
        {
            if (entity == uid)
                continue;
            if (!xformQuery.TryGetComponent(entity, out var entityXform))
                continue;

            var displacement = _xformSystem.GetWorldPosition(entityXform, xformQuery) - epicenter;
            if (displacement.LengthSquared() > range2)
                continue;

            AttemptConsumeEntity(uid, entity, NyarlathotepHorizon);
        }
    }

    public void ConsumeEntitiesInContainer(EntityUid hungry, BaseContainer container, NyarlathotepHorizonComponent NyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        List<EntityUid> immune = new();

        foreach (var entity in container.ContainedEntities)
        {
            if (entity == hungry || !AttemptConsumeEntity(hungry, entity, NyarlathotepHorizon, outerContainer))
                immune.Add(entity);
        }

        if (outerContainer == container || immune.Count <= 0)
            return;
        foreach (var entity in immune)
        {
            var target_container = outerContainer;
            while (target_container != null)
            {
                if (_containerSystem.Insert(entity, target_container))
                    break;

                _containerSystem.TryGetContainingContainer(target_container.Owner, out target_container);
            }
            if (target_container == null)
                _xformSystem.AttachToGridOrMap(entity);
        }
    }

    #endregion Consume Entities
    public void ConsumeEverythingInRange(EntityUid uid, float range, TransformComponent? xform = null, NyarlathotepHorizonComponent? NyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref xform, ref NyarlathotepHorizon))
            return;

        if (NyarlathotepHorizon.ConsumeEntities)
            ConsumeEntitiesInRange(uid, range, xform, NyarlathotepHorizon);
    }

    #endregion Consume

    #region Getters/Setters

    public void SetConsumePeriod(EntityUid uid, TimeSpan value, NyarlathotepHorizonComponent? NyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref NyarlathotepHorizon))
            return;

        if (MathHelper.CloseTo(NyarlathotepHorizon.TargetConsumePeriod.TotalSeconds, value.TotalSeconds))
            return;

        var diff = (value - NyarlathotepHorizon.TargetConsumePeriod);
        NyarlathotepHorizon.TargetConsumePeriod = value;
        NyarlathotepHorizon.NextConsumeWaveTime += diff;

        var curTime = _timing.CurTime;
        if (NyarlathotepHorizon.NextConsumeWaveTime < curTime)
            Update(uid, NyarlathotepHorizon);
    }

    #endregion Getters/Setters

    #region Event Handlers

    protected override bool PreventCollide(EntityUid uid, NyarlathotepHorizonComponent comp, ref PreventCollideEvent args)
    {
        if (base.PreventCollide(uid, comp, ref args) || args.Cancelled)
            return true;

        // If we can eat it we don't want to bounce off of it. If we can't eat it we want to bounce off of it (containment fields).
        args.Cancelled = args.OurFixture.Hard && CanConsumeEntity(uid, args.OtherEntity, comp);
        return false;
    }

    public void PreventConsumeMobs<TComp>(EntityUid uid, TComp comp, ref NyarlathotepHorizonAttemptConsumeEntityEvent args)
    {
        PreventConsume(uid, comp, ref args);
        if (_mob.IsAlive(args.entity) && !HasComp<MiGoComponent>(args.entity))
        {
            DamageSpecifier damage = new();
            damage.DamageDict.Add("Cold", 100);//Надо решить какой тип урона
            _damageable.TryChangeDamage(uid, damage, true);
            if (HasComp<NyarlathotepTargetComponent>(uid))
            {
                EntityManager.RemoveComponent(uid, EntityManager.GetComponent<NyarlathotepTargetComponent>(uid));
            }
        }
    }
    public static void PreventConsume<TComp>(EntityUid uid, TComp comp, ref NyarlathotepHorizonAttemptConsumeEntityEvent args)
    {
        if (!args.Cancelled)
            args.Cancelled = true;
    }

    private void OnStartCollide(EntityUid uid, NyarlathotepHorizonComponent comp, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != comp.ConsumerFixtureId)
            return;
        if (args.OurFixtureId != comp.ConsumerFixtureId)
            return;

        AttemptConsumeEntity(uid, args.OtherEntity, comp);
    }
    private void OnNyarlathotepHorizonContained(EntityUid uid, NyarlathotepHorizonComponent comp, EntGotInsertedIntoContainerMessage args)
    {
        // Delegates processing an event until all queued events have been processed.
        QueueLocalEvent(new NyarlathotepHorizonContainedEvent(uid, comp, args));
    }

    private void OnNyarlathotepHorizonContained(NyarlathotepHorizonContainedEvent args)
    {
        var uid = args.Entity;
        if (!EntityManager.EntityExists(uid))
            return;
        var comp = args.NyarlathotepHorizon;

        var containerEntity = args.Args.Container.Owner;
        if (!EntityManager.EntityExists(containerEntity))
            return;
        if (AttemptConsumeEntity(uid, containerEntity, comp))
            return; // If we consume the entity we also consume everything in the containers it has.

        ConsumeEntitiesInContainer(uid, args.Args.Container, comp, args.Args.Container);
    }

    private void OnContainerConsumed(EntityUid uid, ContainerManagerComponent comp, ref NyarlathotepHorizonConsumedEntityEvent args)
    {
        var drop_container = args.Container;
        if (drop_container is null)
            _containerSystem.TryGetContainingContainer(uid, out drop_container);

        foreach (var container in comp.GetAllContainers())
        {
            ConsumeEntitiesInContainer(args.NyarlathotepHorizonUid, container, args.NyarlathotepHorizon, drop_container);
        }
    }
    #endregion Event Handlers
}
