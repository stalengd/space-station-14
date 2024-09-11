// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Server.SS220.GameTicking.Rules;
using Robust.Shared.GameObjects;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Revolutionary.Components;
using Content.Shared.SS220.CultYogg;
using Content.Shared.StatusEffect;
using Content.Shared.Zombies;
using Content.Shared.Mind;
using Content.Server.Bible.Components;
using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using System.ComponentModel;

namespace Content.Server.SS220.CultYogg;

public sealed partial class MiGoSystem : SharedMiGoSystem
{

    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementEvent>(MiGoEnslave);


        SubscribeLocalEvent<MiGoComponent, MiGoEnslaveDoAfterEvent>(MiGoEnslaveOnDoAfter);
    }
    #region Enslave
    private void MiGoEnslave(Entity<MiGoComponent> uid, ref MiGoEnslavementEvent args)
    {
        if (args.Handled)
            return;

        if (!_mind.TryGetMind(args.Target, out var mindId, out var mind))
        {
            //_popup.PopupEntity(Loc.GetString("cult-yogg-no-mind"), args.Target, uid); // commenting cause its spamming sevral times
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

        if (!_statusEffectsSystem.HasStatusEffect(args.Target, uid.Comp.RequiedEffect))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-should-eat-shroom"), args.Target, uid);
            return;
        }

        if (HasComp<BibleUserComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-cant-be-a-priest"), args.Target, uid);
            return;
        }

        if (HasComp<CultYoggSacrificialComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-is-sacraficial"), args.Target, uid);
            return;
        }
        //ToDo Remove all holy water

        var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3), new MiGoEnslaveDoAfterEvent(), uid, args.Target)//ToDo estimate time for Enslave
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
    private void MiGoEnslaveOnDoAfter(Entity<MiGoComponent> uid, ref MiGoEnslaveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        var ev = new CultYoggEnslavedEvent(args.Target);
        RaiseLocalEvent(uid, ref ev, true);

        _statusEffectsSystem.TryRemoveStatusEffect(args.Target.Value, uid.Comp.RequiedEffect); //Remove Rave

        args.Handled = true;
    }
    #endregion
}
