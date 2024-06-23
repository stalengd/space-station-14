using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

using Content.Shared.Ghost;
using Robust.Shared.Physics;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.EntitySystems;

public abstract class SharedNyarlathotepHorizonSystem : EntitySystem
{

    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly IViewVariablesManager Vvm = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NyarlathotepHorizonComponent, ComponentStartup>(OnNyarlathotepHorizonStartup);
        SubscribeLocalEvent<NyarlathotepHorizonComponent, PreventCollideEvent>(OnPreventCollide);

        var vvHandle = Vvm.GetTypeHandler<NyarlathotepHorizonComponent>();
        vvHandle.AddPath(nameof(NyarlathotepHorizonComponent.Radius), (_, comp) => comp.Radius, (uid, value, comp) => SetRadius(uid, value, NyarlathotepHorizon: comp));
        vvHandle.AddPath(nameof(NyarlathotepHorizonComponent.ColliderFixtureId), (_, comp) => comp.ColliderFixtureId, (uid, value, comp) => SetColliderFixtureId(uid, value, NyarlathotepHorizon: comp));
        vvHandle.AddPath(nameof(NyarlathotepHorizonComponent.ConsumerFixtureId), (_, comp) => comp.ConsumerFixtureId, (uid, value, comp) => SetConsumerFixtureId(uid, value, NyarlathotepHorizon: comp));
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<NyarlathotepHorizonComponent>();
        vvHandle.RemovePath(nameof(NyarlathotepHorizonComponent.Radius));
        vvHandle.RemovePath(nameof(NyarlathotepHorizonComponent.ColliderFixtureId));
        vvHandle.RemovePath(nameof(NyarlathotepHorizonComponent.ConsumerFixtureId));

        base.Shutdown();
    }
    public void SetRadius(EntityUid uid, float value, bool updateFixture = true, NyarlathotepHorizonComponent? NyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref NyarlathotepHorizon))
            return;

        var oldValue = NyarlathotepHorizon.Radius;
        if (value == oldValue)
            return;

        NyarlathotepHorizon.Radius = value;
        Dirty(uid, NyarlathotepHorizon);
        if (updateFixture)
            UpdateNyarlathotepHorizonFixture(uid, NyarlathotepHorizon: NyarlathotepHorizon);
    }

    public void SetColliderFixtureId(EntityUid uid, string? value, bool updateFixture = true, NyarlathotepHorizonComponent? NyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref NyarlathotepHorizon))
            return;

        var oldValue = NyarlathotepHorizon.ColliderFixtureId;
        if (value == oldValue)
            return;

        NyarlathotepHorizon.ColliderFixtureId = value;
        Dirty(uid, NyarlathotepHorizon);
        if (updateFixture)
            UpdateNyarlathotepHorizonFixture(uid, NyarlathotepHorizon: NyarlathotepHorizon);
    }

    public void SetConsumerFixtureId(EntityUid uid, string? value, bool updateFixture = true, NyarlathotepHorizonComponent? NyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref NyarlathotepHorizon))
            return;

        var oldValue = NyarlathotepHorizon.ConsumerFixtureId;
        if (value == oldValue)
            return;

        NyarlathotepHorizon.ConsumerFixtureId = value;
        Dirty(uid, NyarlathotepHorizon);
        if (updateFixture)
            UpdateNyarlathotepHorizonFixture(uid, NyarlathotepHorizon: NyarlathotepHorizon);
    }

    public void UpdateNyarlathotepHorizonFixture(EntityUid uid, FixturesComponent? fixtures = null, NyarlathotepHorizonComponent? NyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref NyarlathotepHorizon))
            return;

        var consumerId = NyarlathotepHorizon.ConsumerFixtureId;
        var colliderId = NyarlathotepHorizon.ColliderFixtureId;
        if (consumerId == null || colliderId == null
        || !Resolve(uid, ref fixtures, logMissing: false))
            return;

        var consumer = _fixtures.GetFixtureOrNull(uid, consumerId, fixtures);
        if (consumer != null)
        {
            _physics.SetRadius(uid, consumerId, consumer, consumer.Shape, NyarlathotepHorizon.Radius, fixtures);
            _physics.SetHard(uid, consumer, false, fixtures);
        }

        var collider = _fixtures.GetFixtureOrNull(uid, colliderId, fixtures);
        if (collider != null)
        {
            _physics.SetRadius(uid, colliderId, collider, collider.Shape, NyarlathotepHorizon.Radius, fixtures);
            _physics.SetHard(uid, collider, false, fixtures);
        }

        EntityManager.Dirty(uid, fixtures);
    }
    private void OnNyarlathotepHorizonStartup(Entity<NyarlathotepHorizonComponent> comp, ref ComponentStartup args)
    {
        UpdateNyarlathotepHorizonFixture(comp.Owner, NyarlathotepHorizon: comp.Comp);
    }
    private void OnPreventCollide(Entity<NyarlathotepHorizonComponent> comp, ref PreventCollideEvent args)
    {
        if (!args.Cancelled)
            PreventCollide(comp, ref args);
    }
    protected virtual bool PreventCollide(Entity<NyarlathotepHorizonComponent> comp, ref PreventCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        if (HasComp<MapGridComponent>(otherUid) ||
            HasComp<GhostComponent>(otherUid))
        {
            args.Cancelled = true;
            return true;
        }

        return false;
    }
}
