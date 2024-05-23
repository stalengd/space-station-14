// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Rules;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Chat.Managers;
using Content.Server.Zombies;
using Content.Server.Mind;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Content.Server.Antag;
using Content.Shared.SS220.Cult;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;
using Content.Server.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Mindshield.Components;

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class CultRuleSystem : GameRuleSystem<CultRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);

        //SubscribeLocalEvent<CultRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
        //SubscribeLocalEvent<CultRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
    }

    private void AfterEntitySelected(Entity<CultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, ent);
    }

    public bool MakeCultist(EntityUid uid, CultRuleComponent component, bool giveObjectives = true)
    {
        //Grab the mind if it wasnt provided
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;

        _antagSelection.SendBriefing(uid, Loc.GetString("traitor-role-greeting"), null, component.GreetSoundNotification);

        component.CultistMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(uid, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(uid, component.CultFaction);

        _entityManager.AddComponent<CultComponent>(uid);
        _entityManager.AddComponent<ZombieImmuneComponent>(uid);//they are practically mushrooms

        //ToDo Give list of sacrificial

        return true;
    }

    public bool TryMakeCultist(EntityUid uid, CultRuleComponent component)
        //needed 4 enslavement
        //maybe looc into RevolutionaryRuleSystem
        //maybe it should be an event
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;

        if (HasComp<RevolutionaryComponent>(uid) ||
            HasComp<MindShieldComponent>(uid) ||
            !HasComp<HumanoidAppearanceComponent>(uid) ||
            !_mobState.IsAlive(uid) ||
            HasComp<ZombieComponent>(uid))
        {
            return false;
        }

        //ToDo in sacrificial list, or possible sacrificial
        /*
        if(uid in)
        */

        MakeCultist(uid, component);

        return true;
    }

    protected override void AppendRoundEndText(EntityUid uid, CultRuleComponent component, GameRuleComponent gameRule,
    ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (component.Summoned)
        {
            args.AddLine(Loc.GetString("cult-round-end-amount-win"));
        }
        else
        {
            var fraction = GetCultistsFraction();
            if (fraction <= 0)
                args.AddLine(Loc.GetString("cult-round-end-amount-none"));
            else if (fraction <= 2)
                args.AddLine(Loc.GetString("cult-round-end-amount-low"));
            else if (fraction < 12)
                args.AddLine(Loc.GetString("cult-round-end-amount-medium"));
            else
                args.AddLine(Loc.GetString("cult-round-end-amount-high"));
        }

        args.AddLine(Loc.GetString("cult-round-end-initial-count", ("initialCount", component.InitialCultistsNames.Count)));

        var antags = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", antags.Count)));
        foreach (var (_, data, entName) in antags)
        {
            args.AddLine(Loc.GetString("cult-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }
    }
    private float GetCultistsFraction()//надо учесть МиГо
    {
        int cultistsCount = 0;
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, CultComponent, MobStateComponent>();
        while (query.MoveNext(out _, out _, out _, out var mob))
        {
            if (mob.CurrentState == MobState.Dead)
                continue;
            cultistsCount++;
        }

        return cultistsCount;
    }
}
