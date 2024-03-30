// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.SS220.GameTicking.Rules;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.GameTicking.Events;
using Content.Server.SS220.GameTicking.Rules;
using System.Globalization;
using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.SS220.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Zombies;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Server.Mind;
using Content.Shared.Mobs;
using Content.Server.NPC.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Antag;

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class CultRuleSystem : GameRuleSystem<CultRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        //SubscribeLocalEvent<PendingZombieComponent, PukeShroomSelfActionEvent>(PukeShroom);
    }

    //Set min players on game rule
    protected override void Added(EntityUid uid, CultRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        gameRule.MinPlayers = _cfg.GetCVar(CCVars.TraitorMinPlayers);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<ZombieRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var minPlayers = _cfg.GetCVar(CCVars.ZombieMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("cult-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length),
                    ("minimumPlayers", minPlayers)));
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("cult-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    /// <summary>
    /// Start this game rule manually
    /// </summary>
    public CultRuleComponent StartGameRule()
    {
        var comp = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (comp == null)
        {
            GameTicker.StartGameRule("Cult", out var ruleEntity);
            comp = Comp<CultRuleComponent>(ruleEntity);
        }

        return comp;
    }
    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {

    }

    public void MakeCultistAdmin(EntityUid entity)
    {
        var cultRule = StartGameRule();
        MakeCultist(entity, cultRule);
    }
    public bool MakeCultist(EntityUid cultist, CultRuleComponent component)
    {
        //Grab the mind if it wasnt provided
        if (!_mindSystem.TryGetMind(cultist, out var mindId, out var mind))
            return false;

        if (HasComp<CultistRoleComponent>(mindId))
        {
            Log.Error($"Player {mind.CharacterName} is already a cultist.");
            return false;
        }

        if (HasComp<TraitorRoleComponent>(mindId))
        {
            Log.Error($"Player {mind.CharacterName} is a traitor.");
            return false;
        }

        if (HasComp<ZombieRoleComponent>(mindId))
        {
            Log.Error($"Player {mind.CharacterName} is a zombie.");
            return false;
        }

        _antagSelection.SendBriefing(cultist, "Хей", null, component.GreetSoundNotification);

        component.CultistMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(cultist, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(cultist, component.CultFaction);

        return true;
    }
}
