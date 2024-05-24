// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Zombies;
using Content.Shared.Mind;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Content.Shared.Popups;

namespace Content.Shared.SS220.Cult;

public abstract class SharedMiGoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementEvent>(Enslave);
        SubscribeLocalEvent<MiGoComponent, MiGoAstralEvent>(MiGoAstral);
        SubscribeLocalEvent<MiGoComponent, MiGoHealEvent>(MiGoHeal);
        SubscribeLocalEvent<MiGoComponent, MiGoErectEvent>(MiGoErect);
        SubscribeLocalEvent<MiGoComponent, MiGoSacrificeEvent>(MiGoSacrifice);
    }

    protected virtual void OnCompInit(EntityUid uid, MiGoComponent comp, ComponentStartup args)
    {

        _actions.AddAction(uid, ref comp.MiGoEnslavementActionEntity, comp.MiGoEnslavementAction);
        _actions.AddAction(uid, ref comp.MiGoAstralActionEntity, comp.MiGoAstralAction);

    }

    private void Enslave(EntityUid uid, MiGoComponent comp, MiGoEnslavementEvent args)
    {
        //maybe look into RevolutionaryRuleSystem
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-enslave-no-mind"), args.Target, uid);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(uid))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-enslave-must-be-human"), args.Target, uid);
            return;
        }

        if (!_mobState.IsAlive(uid))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-enslave-must-be-alive"), args.Target, uid);
            return;
        }

        if (HasComp<RevolutionaryComponent>(uid) || HasComp<MindShieldComponent>(uid) || HasComp<ZombieComponent>(uid))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-enslave-another-fraction"), args.Target, uid);
            return;
        }

        /*
        if(HasComp<SacrificialComponent>(uid))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-enslave-is-sacraficial"), args.Target, uid);
            return;
        }
         */

    }

    private void MiGoAstral(EntityUid uid, MiGoComponent comp, MiGoAstralEvent args)
    {
        //ToDo https://github.com/TheArturZh/space-station-14/blob/b0ee614751216474ddbeabab970b3ab505f63845/Content.Shared/SS220/DarkReaper/DarkReaperSharedSystem.cs#L4
    }
    private void MiGoHeal(EntityUid uid, MiGoComponent comp, MiGoHealEvent args)
    {

    }
    private void MiGoErect(EntityUid uid, MiGoComponent comp, MiGoErectEvent args)
    {

    }
    private void MiGoSacrifice(EntityUid uid, MiGoComponent comp, MiGoSacrificeEvent args)
    {

    }
}
