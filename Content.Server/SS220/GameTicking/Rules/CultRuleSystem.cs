// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.SS220.GameTicking.Rules.Components;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Server.SS220.Roles;
using Content.Server.Zombies;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Server.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Content.Server.Antag;
using Content.Shared.SS220.Cult;
using Content.Shared.CombatMode.Pacification;
using Content.Server.Objectives;
using Content.Server.PDA.Ringer;
using Content.Server.Traitor.Uplink;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Text;
using Content.Server.GameTicking.Components;

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

    public bool MakeCultist(EntityUid cultist, CultRuleComponent component, bool giveObjectives = true)
    {
        //Grab the mind if it wasnt provided
        if (!_mindSystem.TryGetMind(cultist, out var mindId, out var mind))
            return false;

        _antagSelection.SendBriefing(cultist, Loc.GetString("traitor-role-greeting"), null, component.GreetSoundNotification);

        component.CultistMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(cultist, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(cultist, component.CultFaction);

        _entityManager.AddComponent<CultComponent>(cultist);
        _entityManager.AddComponent<ZombieImmuneComponent>(cultist);//they are practically mushrooms

        //ToDo Give list of sacrificial

        return true;
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, TraitorRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = _antag.GetAntagMindEntityUids(uid);

        args.AgentName = Loc.GetString("traitor-round-end-agent-name");
    }
}
