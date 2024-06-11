// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Zombies;
using Content.Shared.Mind;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Content.Shared.Tag;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.SS220.CultYogg;

public abstract class SharedMiGoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;

    //[Dependency] private readonly CultYoggRuleSystem _cultYoggRule = default!; //maybe use this for enslavement

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementEvent>(MiGoEnslave);
        SubscribeLocalEvent<MiGoComponent, MiGoAstralEvent>(MiGoAstral);
        SubscribeLocalEvent<MiGoComponent, MiGoHealEvent>(MiGoHeal);
        SubscribeLocalEvent<MiGoComponent, MiGoErectEvent>(MiGoErect);
        SubscribeLocalEvent<MiGoComponent, MiGoSacrificeEvent>(MiGoSacrifice);
        //SubscribeLocalEvent<MiGoComponent, MiGoEnslavetDoAfterEvent>(MiGoEnslaveOnDoAfter);
    }

    protected virtual void OnCompInit(EntityUid uid, MiGoComponent comp, ComponentStartup args)
    {

        _actions.AddAction(uid, ref comp.MiGoEnslavementActionEntity, comp.MiGoEnslavementAction);
        _actions.AddAction(uid, ref comp.MiGoAstralActionEntity, comp.MiGoAstralAction);

    }

    private void MiGoEnslave(EntityUid uid, MiGoComponent comp, MiGoEnslavementEvent args)
    {
        if (args.Handled)
            return;

        //maybe look into RevolutionaryRuleSystem
        if (!_mind.TryGetMind(args.Target, out var mindId, out var mind))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-no-mind"), args.Target, uid);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-must-be-human"), args.Target, uid);
            return;
        }

        if (!_mobState.IsAlive(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-must-be-alive"), args.Target, uid);
            return;
        }

        if (HasComp<RevolutionaryComponent>(args.Target) || HasComp<MindShieldComponent>(args.Target) || HasComp<ZombieComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-another-fraction"), args.Target, uid);
            return;
        }

        /*
        if(HasComp<SacrificialComponent>(uid))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-is-sacraficial"), args.Target, uid);
            return;
        }
         */
        var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3), new MiGoEnslavetDoAfterEvent(), uid, args.Target)//ToDo estimate time for Enslave
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = false,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(doafterArgs);

        args.Handled = true;
    }
    private void MiGoEnslaveOnDoAfter(EntityUid uid, MiGoComponent comp, MiGoEnslavetDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        //ToDo Remove clients effects

        args.Handled = true;
    }

    private void MiGoAstral(EntityUid uid, MiGoComponent comp, MiGoAstralEvent args)
    {
        if (comp.PhysicalForm)
        {
            ChangeForm(uid, comp, false);
        }
        else
        {
            ChangeForm(uid, comp, true);
        }

        //ToDo https://github.com/TheArturZh/space-station-14/blob/b0ee614751216474ddbeabab970b3ab505f63845/Content.Shared/SS220/DarkReaper/DarkReaperSharedSystem.cs#L4
    }
    public virtual void ChangeForm(EntityUid uid, MiGoComponent comp, bool isMaterial)
    {
        comp.PhysicalForm = isMaterial;

        if (TryComp<FixturesComponent>(uid, out var fixturesComp))
        {
            if (fixturesComp.Fixtures.TryGetValue("fix1", out var fixture))
            {
                var mask = (int) (isMaterial ? CollisionGroup.MobMask : CollisionGroup.GhostImpassable);
                var layer = (int) (isMaterial ? CollisionGroup.MobLayer : CollisionGroup.None);
                _physics.SetCollisionMask(uid, "fix1", fixture, mask);
                _physics.SetCollisionLayer(uid, "fix1", fixture, layer);
            }
        }

        if (isMaterial)
        {
            _physics.SetBodyType(uid, BodyType.KinematicController);
            _tag.AddTag(uid, "DoorBumpOpener");
        }
        else
        {
            _physics.SetBodyType(uid, BodyType.Static);
            _tag.RemoveTag(uid, "DoorBumpOpener");
            comp.MaterializedStart = null;
        }

        UpdateMovementSpeed(uid, comp);

        Dirty(uid, comp);
    }

    private void UpdateMovementSpeed(EntityUid uid, MiGoComponent comp)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var modifComp))
            return;

        var speed = comp.PhysicalForm ? comp.MaterialMovementSpeed : comp.UnMaterialMovementSpeed;
        _speedModifier.ChangeBaseSpeed(uid, speed, speed, modifComp.Acceleration, modifComp);
    }
    private void MiGoHeal(EntityUid uid, MiGoComponent comp, MiGoHealEvent args)
    {
        if (args.Handled)
            return;

        if (!_mind.TryGetMind(args.Target, out var mindId, out var mind))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-yogg-no-mind"), args.Target, uid);
            return;
        }

        if (!HasComp<CultYoggComponent>(args.Target) || !HasComp<MiGoComponent>(args.Target))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-yogg-heal-only-cultists"), args.Target, uid);
            return;
        }

        //ToDo find way to heal

        args.Handled = true;
    }
    private void MiGoErect(EntityUid uid, MiGoComponent comp, MiGoErectEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        //_userInterface.TryToggleUi(uid, SiliconLawsUiKey.Key, actor.PlayerSession);
    }
    private void MiGoSacrifice(EntityUid uid, MiGoComponent comp, MiGoSacrificeEvent args)
    {

    }
}
