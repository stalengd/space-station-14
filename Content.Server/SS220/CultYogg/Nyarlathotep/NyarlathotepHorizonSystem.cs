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

    private void OnHorizonMapInit(Entity<NyarlathotepHorizonComponent> component, ref MapInitEvent args)
    {
        component.Comp.NextConsumeWaveTime = _timing.CurTime;
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

    public void Update(EntityUid uid, NyarlathotepHorizonComponent? nyarlathotepHorizon = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref nyarlathotepHorizon))
            return;

        nyarlathotepHorizon.NextConsumeWaveTime += nyarlathotepHorizon.TargetConsumePeriod;

        if (!Resolve(uid, ref xform))
            return;

        // Handle singularities some admin smited into a locker.
        if (_containerSystem.TryGetContainingContainer(uid, out var container, transform: xform)
        && !AttemptConsumeEntity(uid, container.Owner, nyarlathotepHorizon))
        {
            // Locker is indestructible. Consume everything else in the locker instead of magically teleporting out.
            ConsumeEntitiesInContainer(uid, container, nyarlathotepHorizon, container);
            return;
        }

        if (nyarlathotepHorizon.Radius > 0.0f)
            ConsumeEverythingInRange(uid, nyarlathotepHorizon.Radius, xform, nyarlathotepHorizon);
    }

    #region Consume

    #region Consume Entities

    public void ConsumeEntity(EntityUid nyarlathotep, EntityUid entityToConsume, NyarlathotepHorizonComponent nyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        if (!EntityManager.IsQueuedForDeletion(entityToConsume) // I saw it log twice a few times for some reason?
        && (HasComp<MindContainerComponent>(entityToConsume)
            || _tagSystem.HasTag(entityToConsume, "HighRiskItem")))
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.Extreme, $"{ToPrettyString(entityToConsume)} entered the event horizon of {ToPrettyString(nyarlathotep)} and was deleted");
        }

        EntityManager.QueueDeleteEntity(entityToConsume);
        var evSelf = new NyarlathotepConsumedByEventHorizonEvent(entityToConsume, nyarlathotep, nyarlathotepHorizon, outerContainer);
        var evEaten = new NyarlathotepHorizonConsumedEntityEvent(entityToConsume, nyarlathotep, nyarlathotepHorizon, outerContainer);
        RaiseLocalEvent(nyarlathotep, ref evSelf);
        RaiseLocalEvent(entityToConsume, ref evEaten);
    }

    public bool AttemptConsumeEntity(EntityUid nyarlathotep, EntityUid entityToConsume, NyarlathotepHorizonComponent nyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        if (!CanConsumeEntity(nyarlathotep, entityToConsume, nyarlathotepHorizon))
            return false;

        ConsumeEntity(nyarlathotep, entityToConsume, nyarlathotepHorizon, outerContainer);
        return true;
    }

    public bool CanConsumeEntity(EntityUid nyarlathotep, EntityUid entityToConsume, NyarlathotepHorizonComponent nyarlathotepHorizon)
    {
        var ev = new NyarlathotepHorizonAttemptConsumeEntityEvent(entityToConsume, nyarlathotep, nyarlathotepHorizon);
        RaiseLocalEvent(entityToConsume, ref ev);
        return !ev.Cancelled;
    }

    public void ConsumeEntitiesInRange(EntityUid nyarlathotep, float range, TransformComponent? xform = null, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(nyarlathotep, ref xform, ref nyarlathotepHorizon))
            return;

        var range2 = range * range;
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var epicenter = _xformSystem.GetWorldPosition(xform, xformQuery);
        foreach (var entity in _lookup.GetEntitiesInRange(_xformSystem.GetMapCoordinates(nyarlathotep, xform), range, flags: LookupFlags.Uncontained))
        {
            if (entity == nyarlathotep)
                continue;
            if (!xformQuery.TryGetComponent(entity, out var entityXform))
                continue;

            var displacement = _xformSystem.GetWorldPosition(entityXform, xformQuery) - epicenter;
            if (displacement.LengthSquared() > range2)
                continue;

            AttemptConsumeEntity(nyarlathotep, entity, nyarlathotepHorizon);
        }
    }

    public void ConsumeEntitiesInContainer(EntityUid nyarlathotep, BaseContainer container, NyarlathotepHorizonComponent nyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        List<EntityUid> immune = new();

        foreach (var entity in container.ContainedEntities)
        {
            if (entity == nyarlathotep || !AttemptConsumeEntity(nyarlathotep, entity, nyarlathotepHorizon, outerContainer))
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
    public void ConsumeEverythingInRange(EntityUid nyarlathotep, float range, TransformComponent? xform = null, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(nyarlathotep, ref xform, ref nyarlathotepHorizon))
            return;

        if (nyarlathotepHorizon.ConsumeEntities)
            ConsumeEntitiesInRange(nyarlathotep, range, xform, nyarlathotepHorizon);
    }

    #endregion Consume

    #region Getters/Setters

    public void SetConsumePeriod(EntityUid nyarlathotep, TimeSpan value, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(nyarlathotep, ref nyarlathotepHorizon))
            return;

        if (MathHelper.CloseTo(nyarlathotepHorizon.TargetConsumePeriod.TotalSeconds, value.TotalSeconds))
            return;

        var diff = (value - nyarlathotepHorizon.TargetConsumePeriod);
        nyarlathotepHorizon.TargetConsumePeriod = value;
        nyarlathotepHorizon.NextConsumeWaveTime += diff;

        var curTime = _timing.CurTime;
        if (nyarlathotepHorizon.NextConsumeWaveTime < curTime)
            Update(nyarlathotep, nyarlathotepHorizon);
    }

    #endregion Getters/Setters

    #region Event Handlers

    protected override bool PreventCollide(Entity<NyarlathotepHorizonComponent> comp, ref PreventCollideEvent args)
    {
        if (base.PreventCollide(comp, ref args) || args.Cancelled)
            return true;

        args.Cancelled = args.OurFixture.Hard && CanConsumeEntity(comp.Owner, args.OtherEntity, comp.Comp);
        return false;
    }

    public void PreventConsumeMobs(Entity<MobStateComponent> comp, ref NyarlathotepHorizonAttemptConsumeEntityEvent args)
    {
        PreventConsume(comp.Owner, comp.Comp, ref args);
        if (_mob.IsAlive(args.entity) && !HasComp<MiGoComponent>(args.entity))
        {
            DamageSpecifier damage = new();
            damage.DamageDict.Add("Cold", 100);//Надо решить какой тип урона
            _damageable.TryChangeDamage(comp.Owner, damage, true);
            if (HasComp<NyarlathotepTargetComponent>(comp.Owner))
            {
                EntityManager.RemoveComponent(comp.Owner, EntityManager.GetComponent<NyarlathotepTargetComponent>(comp.Owner));
            }
        }
    }
    public static void PreventConsume<TComp>(EntityUid uid, TComp comp, ref NyarlathotepHorizonAttemptConsumeEntityEvent args)
    {
        if (!args.Cancelled)
            args.Cancelled = true;
    }

    private void OnStartCollide(Entity<NyarlathotepHorizonComponent> comp, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != comp.Comp.ConsumerFixtureId)
            return;
        if (args.OurFixtureId != comp.Comp.ConsumerFixtureId)
            return;

        AttemptConsumeEntity(comp.Owner, args.OtherEntity, comp.Comp);
    }
    private void OnNyarlathotepHorizonContained(EntityUid uid, NyarlathotepHorizonComponent comp, EntGotInsertedIntoContainerMessage args)
    {
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

    private void OnContainerConsumed(Entity<ContainerManagerComponent> comp, ref NyarlathotepHorizonConsumedEntityEvent args)
    {
        var drop_container = args.Container;
        if (drop_container is null)
            _containerSystem.TryGetContainingContainer(comp.Owner, out drop_container);

        foreach (var container in comp.Comp.GetAllContainers())
        {
            ConsumeEntitiesInContainer(args.NyarlathotepHorizonUid, container, args.NyarlathotepHorizon, drop_container);
        }
    }
    #endregion Event Handlers
}
