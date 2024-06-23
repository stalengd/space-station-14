using Content.Server.Beam;
using Content.Server.Beam.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Lightning;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using Content.Shared.SS220.CultYogg;

namespace Content.Server.SS220.CultYogg.Nyarlathotep;

public sealed class NyarlathotepSystem : EntitySystem
{
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NyarlathotepComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(EntityUid uid, NyarlathotepComponent component, ComponentRemove args)
    {
        if (!TryComp<BeamComponent>(uid, out var lightningBeam) || !TryComp<BeamComponent>(lightningBeam.VirtualBeamController, out var beamController))
        {
            return;
        }

        beamController.CreatedBeams.Remove(uid);
    }


    public void SearchNearNyarlathotep(EntityUid user, float range)
    {
        foreach (var target in _entityLookupSystem.GetComponentsInRange<MobStateComponent>(_transform.GetMapCoordinates(user), range))
        {
            if(!HasComp<MiGoComponent>(target.Owner) && !HasComp<NyarlathotepTargetComponent>(target.Owner) && _mobStateSystem.IsAlive(target.Owner))
            {
                EntityManager.AddComponent(target.Owner, new NyarlathotepTargetComponent());
            }
        }
    }
}
