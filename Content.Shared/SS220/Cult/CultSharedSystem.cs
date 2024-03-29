// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Cult;

public abstract class SharedCultSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        // actions
        SubscribeLocalEvent<CultComponent, CultAstralEvent>(AstralAction);
        SubscribeLocalEvent<CultComponent, CultPukeShroomEvent>(Puke);
    }
    private void AstralAction(EntityUid uid, CultComponent comp, CultAstralEvent args)
    {
        GoToAstral(uid, comp);
    }
    protected void GoToAstral(EntityUid uid, CultComponent comp)
    {

    }

    private void Puke(EntityUid uid, CultComponent comp, CultPukeShroomEvent args)
    {
        _audio.PlayPredicted(comp.PukeSound, uid, uid);
    }

}
