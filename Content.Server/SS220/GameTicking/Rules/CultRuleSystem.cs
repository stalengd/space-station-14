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
using Content.Shared.SS220.Cult;

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

    private int StartedCultists => _cfg.GetCVar(CCVars.CultStartedCultists);
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

        gameRule.MinPlayers = _cfg.GetCVar(CCVars.CultMinPlayers);
    }
    protected override void Started(EntityUid uid, CultRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
    }

    protected override void ActiveTick(EntityUid uid, CultRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus < CultRuleComponent.SelectionState.Started && component.AnnounceAt < _timing.CurTime)
        {
            DoCultStart(component);
            component.SelectionStatus = CultRuleComponent.SelectionState.Started;
        }
    }

    private void DoCultStart(CultRuleComponent component)
    {
        var eligiblePlayers = _antagSelection.GetEligiblePlayers(_playerManager.Sessions, component.CultPrototypeId);

        if (eligiblePlayers.Count == 0)
            return;

        // should we do calculation about amount of cultists?
        //var cultistsToSelect = _antagSelection.CalculateAntagCount(_playerManager.PlayerCount, PlayersPerTraitor, StartedCultists); 

        //var selectedCultists = _antagSelection.ChooseAntags(cultistsToSelect, eligiblePlayers);
        var selectedCultists = _antagSelection.ChooseAntags(StartedCultists, eligiblePlayers);// started amount is 3

        MakeCultist(selectedCultists, component);
        //MakeSacraficials();//to do
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
    public bool MakeCultist(List<EntityUid> cultist, CultRuleComponent component, bool giveUplink = true, bool giveObjectives = true)
    {
        foreach (var traitor in cultist)
        {
            MakeCultist(cultist, component, giveUplink, giveObjectives);
        }

        return true;
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

        _antagSelection.SendBriefing(cultist, Loc.GetString("traitor-role-greeting"), null, component.GreetSoundNotification); //доработать и добавить перечисление жертв, как в GenerateBriefing

        component.CultistMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(cultist, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(cultist, component.CultFaction);

        _entityManager.AddComponent<CultComponent>(cultist);

        return true;
    }
}
