// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Rules;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Zombies;
using Content.Server.Mind;
using Content.Server.Antag;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
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
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.SS220.Telepathy;
using Content.Server.SS220.CultYogg.Nyarlathotep;
using Content.Server.Station.Systems;
using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.SS220.CultYogg;
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using static Content.Shared.SS220.CultYogg.EntitySystems.SharedCultYoggSystem;

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
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    private List<List<String>> SacraficialTiers = new();
    public TimeSpan DefaultShuttleArriving { get; set; } = TimeSpan.FromSeconds(85);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<CultYoggEnslavedEvent>(MiGoEnslave);//cant receive this shit
        //SubscribeLocalEvent<CultYoggComponent, CleansedEvent>();

        SubscribeLocalEvent<SacraficialReplacementEvent>(SacraficialReplacement);

        SubscribeLocalEvent<CultYoggSummonedEvent>(OnGodSummoned);
    }



    #region Sacreficials picking
    /// <summary>
    /// Used to generate sacraficials at the start of the gamerule
    /// </summary>
    protected override void Started(EntityUid uid, CultYoggRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        GenerateJobsList(component);

        SetSacraficials(component);
    }

    //Filling list of jobs fot better range
    private void GenerateJobsList(CultYoggRuleComponent comp)
    {
        List<string> firstTier = comp.FirstTierJobs;//just captain as main target

        List<string> secondTier = new();//heads

        if (!_proto.TryIndex<DepartmentPrototype>(comp.SecondTierDepartament, out var commandList))
            return;

        foreach (ProtoId<JobPrototype> role in commandList.Roles)
        {
            if (firstTier.Contains(role.Id))
                continue;

            secondTier.Add(role.Id);
        }

        List<string> thirdTier = new();//everybody else except heads

        foreach (var departament in _proto.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (comp.BannedDepartaents.Contains(departament.ID))
                continue;

            if (departament.ID == comp.SecondTierDepartament)
                continue;

            foreach (ProtoId<JobPrototype> role in departament.Roles)
            {
                if (firstTier.Contains(role.Id))
                    continue;

                if (secondTier.Contains(role.Id))
                    continue;

                thirdTier.Add(role.Id);
            }
        }

        SacraficialTiers.Add(firstTier);
        SacraficialTiers.Add(secondTier);
        SacraficialTiers.Add(thirdTier);
    }

    private void SetSacraficials(CultYoggRuleComponent component)
    {
        var allHumans = GetAliveHumans();

        if (allHumans is null)
            return;

        for (int i = 0; i < SacraficialTiers.Count; i++)
        {
            SetSacraficeTarget(component, PickFromTierPerson(allHumans, i), i);
        }
    }

    public EntityUid? PickFromTierPerson(List<EntityUid> allHumans, int tier)//ToDo wierd naming
    {
        if (tier >= SacraficialTiers.Count)
            return null;

        var allSuitable = new List<EntityUid>();

        foreach (var mind in allHumans)
        {
            // RequireAdminNotify used as a cheap way to check for command department
            if (!_job.MindTryGetJob(mind, out _, out var prototype))
                continue;

            if (HasComp<CultYoggSacrificialMindComponent>(mind))//shouldn't be already a target
                continue;

            if (SacraficialTiers[tier].Contains(prototype.ID))
                allSuitable.Add(mind);
        }

        if (allSuitable.Count == 0)
        {
            return PickFromTierPerson(allHumans, ++tier);
        }

        return _random.Pick(allSuitable);
    }

    private void SetSacraficeTarget(CultYoggRuleComponent component, EntityUid? uid, int tier)
    {
        if (uid is null)
            return;

        if (!TryComp<MindComponent>(uid, out var mind))
            return;

        if (mind.Session is null)
            return;

        if (mind.Session.AttachedEntity is null)
            return;

        EnsureComp<CultYoggSacrificialMindComponent>(uid.Value); //ToDo figure out do i need this?

        var sacrComp = EnsureComp<CultYoggSacrificialComponent>(mind.Session.AttachedEntity.Value);

        sacrComp.Tier = tier;

        component.SacraficialsList.Add(uid.Value);
    }

    private List<EntityUid> GetAliveHumans()//maybe add here sacraficials and cultists filter
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
    #endregion

    #region Sacraficials Events

    private void SacraficialReplacement(ref SacraficialReplacementEvent args)
    {
        GetCultGameRule(out var cultRuleEnt, out var cultRuleComp);

        if (cultRuleComp == null)
            return;

        if (!TryComp<CultYoggSacrificialComponent>(args.Entity, out var sacrComp))
            return;

        if (sacrComp.WasSacraficed)
            return;

        SetNewSacraficial(cultRuleComp, sacrComp.Tier);

        RemComp<CultYoggSacrificialComponent>(args.Entity);

        if (!_mindSystem.TryGetMind(args.Player, out var mindUid, out var mind))
            return;

        if (mindUid == null)
            return;

        cultRuleComp.SacraficialsList.Remove(mindUid.Value);

        RemComp<CultYoggSacrificialMindComponent>(mindUid.Value);
    }
    private void SetNewSacraficial(CultYoggRuleComponent comp, int tier)
    {
        var allHumans = GetAliveHumans();

        if (allHumans is null)
            return;

        SetSacraficeTarget(comp, PickFromTierPerson(allHumans, tier), tier);
    }
    #endregion

    #region Enslaving
    /// <summary>
    /// If MiGo enslaves somebody -- will call this
    /// </summary>
    /// <param name="args.Target">Target of enslavement</param>
    private void MiGoEnslave(ref CultYoggEnslavedEvent args)
    {
        if (args.Target == null)
            return;

        GetCultGameRule(out var cultRuleEnt, out var cultRuleComp);

        if (cultRuleComp == null)
            return;

        MakeCultist(args.Target.Value, cultRuleComp, false);
    }
    #endregion

    #region Cultists making
    private void AfterEntitySelected(Entity<CultYoggRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, ent);//ToDo not quite shure if entiry is a body or a mind
    }

    public bool MakeCultist(EntityUid uid, CultYoggRuleComponent component, bool initial = true)
    {
        //Grab the mind if it wasnt provided
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;

        //ToDo remove this when you make a JobRequirement
        if (HasComp<CultYoggSacrificialComponent>(uid))//targets can't be cultists
            return false;

        _antagSelection.SendBriefing(uid, Loc.GetString("cult-yogg-role-greeting"), null, component.GreetSoundNotification);

        if (initial)
            component.InitialCultistMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(uid, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(uid, component.CultYoggFaction);

        var CultistComp = EnsureComp<CultYoggComponent>(uid);
        //ToDo CultYoggComponent -- set current amount of sacrafaces for visualisation of stage

        //Add telepathy
        var telepathy = EnsureComp<TelepathyComponent>(uid);
        telepathy.CanSend = false;
        telepathy.TelepathyChannelPrototype = component.Channel;

        AddComp<ShowCultYoggIconsComponent>(uid);//icons of cultists and sacraficials
        AddComp<ZombieImmuneComponent>(uid);//they are practically mushrooms

        return true;
    }
    #endregion

    #region RoundEnding
    private void OnGodSummoned(ref CultYoggSummonedEvent args)
    {
        foreach (var station in _station.GetStations())
        {
            _chat.DispatchStationAnnouncement(station, Loc.GetString("cult-yogg-shuttle-call"), colorOverride: Color.Crimson);
        }
        _roundEnd.RequestRoundEnd(DefaultShuttleArriving, null);

        GetCultGameRule(out var cultRuleEnt, out var cultRuleComp);

        if (cultRuleEnt == null || cultRuleComp == null)
            return;

        cultRuleComp.Summoned = true;//Win EndText

        var ev = new CultYoggForceAscendingEvent();

        //Event hadn't raised on Cultists even with broadcast, so i made this
        var query = EntityQueryEnumerator<CultYoggComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            RaiseLocalEvent(ent, ref ev);//Make all cultists MiGo
        }
    }

    #endregion

    #region EndText
    /// <summary>
    /// EndText copypasted from zombies. Hasn't finished.
    /// </summary>
    protected override void AppendRoundEndText(EntityUid uid, CultYoggRuleComponent component, GameRuleComponent gameRule,
    ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);


        bool summoned = false;

        var query = EntityQueryEnumerator<NyarlathotepSearchTargetsComponent>();//ToDo maybe some altrenative

        if (component.Summoned)
        {
            args.AddLine(Loc.GetString("cult-yogg-round-end-win"));
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
        //args.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", antags.Count))); // ToDo Should we add this?
        foreach (var (mind, data, entName) in antags)
        {
            if (component.InitialCultistMinds.Contains(mind))
                continue;

            args.AddLine(Loc.GetString("cult-yogg-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }
    }
    private float GetCultistsFraction()
    {
        int cultistsCount = 0;
        var queryCultists = EntityQueryEnumerator<HumanoidAppearanceComponent, CultYoggComponent, MobStateComponent>();
        while (queryCultists.MoveNext(out _, out _, out _, out var mob))
        {
            if (mob.CurrentState == MobState.Dead)
                continue;
            cultistsCount++;
        }

        var queryMiGo = EntityQueryEnumerator<MiGoComponent, MobStateComponent>();
        while (queryMiGo.MoveNext(out _, out _,  out var mob))
        {
            if (mob.CurrentState == MobState.Dead)
                continue;
            cultistsCount++;
        }

        return cultistsCount;
    }
    #endregion

    public void GetCultGameRule(out EntityUid? ent, out CultYoggRuleComponent? comp)
    {
        comp = null;
        ent = null;
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var cultComp, out _))
        {
            comp = cultComp;
            ent = uid;
        }
    }
}
