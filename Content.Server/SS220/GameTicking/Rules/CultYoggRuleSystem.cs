// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Rules;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Zombies;
using Content.Server.Mind;
using Content.Server.Antag;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Server.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.Administration;

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class CultYoggRuleSystem : GameRuleSystem<CultYoggRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<MiGoComponent, CultYoggEnslavedEvent>(MiGoEnslave);//cant receive this shit
    }

    /// <summary>
    /// Used to generate sacraficials
    /// </summary>
    protected override void Started(EntityUid uid, CultYoggRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        //AssignCaptain(uid, component);
        AssignHead(uid, component);
        //AssignRegular(uid, component);
    }
    private void AssignSacriaficials(EntityUid uid, CultYoggRuleComponent component)
    {
        var allHumans = GetAliveHumans();

    }

    public List<EntityUid> GetAliveHumans()//maybe add here sacraficials and cultists filter
    {
        var mindQuery = EntityQuery<MindComponent>();

        var allHumans = new List<EntityUid>();
        // HumanoidAppearanceComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out var mc, out var mobState, out _))
        {
            // the player needs to have a mind and not be the excluded one
            if (mc.Mind == null)
                continue;

            // the player has to be alive
            if (_mobState.IsAlive(uid, mobState))
                allHumans.Add(mc.Mind.Value);
        }

        return allHumans;
    }
    private void AssignHead(EntityUid uid, CultYoggRuleComponent component)
    {
        var mindQuery = EntityQuery<MindComponent>();

        var allHumans = new List<EntityUid>();

        if (allHumans.Count == 0)
            return;

        // HumanoidAppearanceComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uids, out var mc, out var mobState, out _))
        {
            // the player needs to have a mind and not be the excluded one
            if (mc.Mind == null || !HasComp<CultYoggComponent>(uid) || !HasComp<CultYoggSacrificialComponent>(uid))
                continue;

            // the player has to be alive
            if (_mobState.IsAlive(uid, mobState))
                allHumans.Add(mc.Mind.Value);
        }

        var allHeads = new List<EntityUid>();
        foreach (var mind in allHumans)
        {
            // RequireAdminNotify used as a cheap way to check for command department
            if (_job.MindTryGetJob(mind, out _, out var prototype) && prototype.RequireAdminNotify)
                allHeads.Add(mind);
        }

        if (allHeads.Count == 0)
        {
            //should call pick random guy function
            return;
        }

        SetTarget(_random.Pick(allHeads), component);
    }

    private void SetTarget(EntityUid uid, CultYoggRuleComponent component)
    {
        //component.SacreficialsMinds.Add(uid); // maybe it should be mind
        //ToDo add target component on it
    }

    private void AfterEntitySelected(Entity<CultYoggRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, ent);
    }

    /// <summary>
    /// If MiGo enslaves somebody -- will call this
    /// </summary>
    /// <param name="args.User">MiGo</param>
    /// <param name="args.Target">Target of enslavement</param>
    private void MiGoEnslave(Entity<MiGoComponent> uid, ref CultYoggEnslavedEvent args)
    {
        if (args.Target == null)
            return;

        GetCultGamerule(out var gameRuleEntity, out var gameRule);

        if (gameRule == null)
            return;

        MakeCultist((EntityUid) args.Target, gameRule);

        //args.Handled = true;
    }

    //it isn't working
    private void GetCultGamerule(out EntityUid? ruleEntity, out CultYoggRuleComponent? component)
    {
        List<GameRuleInfo> _gameRulesList = new();
        _gameRulesList = _gameTicker.GetAddedGameRules().Select(gr => new GameRuleInfo(GetNetEntity(gr), MetaData(gr).EntityPrototype?.ID ?? string.Empty)).ToList();

        var gameRu = _gameTicker.GetAddedGameRules();

        var gameRules = _gameTicker.GetActiveGameRules().GetEnumerator();
        ruleEntity = null;
        while (gameRules.MoveNext())
        {
            if (!HasComp<CultYoggRuleComponent>(gameRules.Current))
                continue;

            ruleEntity = gameRules.Current;
            break;
        }

        TryComp(ruleEntity, out component);
    }

    public bool MakeCultist(EntityUid uid, CultYoggRuleComponent component, bool initial = true)
    {
        //Grab the mind if it wasnt provided
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;

        if (HasComp<CultYoggSacrificialComponent>(uid))//targets can't be cultists
            return false;

        _antagSelection.SendBriefing(uid, Loc.GetString("cult-yogg-role-greeting"), null, component.GreetSoundNotification);

        component.CultistMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(uid, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(uid, component.CultYoggFaction);

        _entityManager.AddComponent<CultYoggComponent>(uid);
        _entityManager.AddComponent <ShowCultYoggIconsComponent>(uid);//icons of cultists and sacraficials
        _entityManager.AddComponent<ZombieImmuneComponent>(uid);//they are practically mushrooms

        return true;
    }

    /// <summary>
    /// EndTect copypasted from zombies. Hasn't finished.
    /// </summary>
    protected override void AppendRoundEndText(EntityUid uid, CultYoggRuleComponent component, GameRuleComponent gameRule,
    ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (component.Summoned)
        {
            args.AddLine(Loc.GetString("cult-yogg-round-end-amount-win"));
        }
        else
        {
            var fraction = GetCultistsFraction();
            if (fraction <= 0)
                args.AddLine(Loc.GetString("cult-yogg-round-end-amount-none"));
            else if (fraction <= 2)
                args.AddLine(Loc.GetString("cult-yogg-round-end-amount-low"));
            else if (fraction < 12)
                args.AddLine(Loc.GetString("cult-yogg-round-end-amount-medium"));
            else
                args.AddLine(Loc.GetString("cult-yogg-round-end-amount-high"));
        }

        args.AddLine(Loc.GetString("cult-yogg-round-end-initial-count", ("initialCount", component.InitialCultistsNames.Count)));

        var antags = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", antags.Count)));
        foreach (var (_, data, entName) in antags)
        {
            args.AddLine(Loc.GetString("cult-yogg-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }
    }
    private float GetCultistsFraction()//надо учесть МиГо
    {
        int cultistsCount = 0;
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, CultYoggComponent, MobStateComponent>();
        while (query.MoveNext(out _, out _, out _, out var mob))
        {
            if (mob.CurrentState == MobState.Dead)
                continue;
            cultistsCount++;
        }

        return cultistsCount;
    }
}

[ByRefEvent, Serializable]
public record struct CultYoggEnslavedEvent(EntityUid? Target);
