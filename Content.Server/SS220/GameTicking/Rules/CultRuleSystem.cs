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
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Zombies;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
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

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class CultRuleSystem : GameRuleSystem<CultRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        //SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        //SubscribeLocalEvent<PendingZombieComponent, PukeShroomSelfActionEvent>(PukeShroom);
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
    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        foreach (var zombie in EntityQuery<ZombieRuleComponent>())
        {
            // This is just the general condition thing used for determining the win/lose text
            var fraction = GetInfectedFraction(true, true);

            if (fraction <= 0)
                ev.AddLine(Loc.GetString("zombie-round-end-amount-none"));
            else if (fraction <= 0.25)
                ev.AddLine(Loc.GetString("zombie-round-end-amount-low"));
            else if (fraction <= 0.5)
                ev.AddLine(Loc.GetString("zombie-round-end-amount-medium", ("percent", Math.Round((fraction * 100), 2).ToString(CultureInfo.InvariantCulture))));
            else if (fraction < 1)
                ev.AddLine(Loc.GetString("zombie-round-end-amount-high", ("percent", Math.Round((fraction * 100), 2).ToString(CultureInfo.InvariantCulture))));
            else
                ev.AddLine(Loc.GetString("zombie-round-end-amount-all"));

            ev.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", zombie.InitialInfectedNames.Count)));
            foreach (var player in zombie.InitialInfectedNames)
            {
                ev.AddLine(Loc.GetString("zombie-round-end-user-was-initial",
                    ("name", player.Key),
                    ("username", player.Value)));
            }

            var healthy = GetHealthyHumans();
            // Gets a bunch of the living players and displays them if they're under a threshold.
            // InitialInfected is used for the threshold because it scales with the player count well.
            if (healthy.Count <= 0 || healthy.Count > 2 * zombie.InitialInfectedNames.Count)
                continue;
            ev.AddLine("");
            ev.AddLine(Loc.GetString("zombie-round-end-survivor-count", ("count", healthy.Count)));
            foreach (var survivor in healthy)
            {
                var meta = MetaData(survivor);
                var username = string.Empty;
                if (_mindSystem.TryGetMind(survivor, out _, out var mind) && mind.Session != null)
                {
                    username = mind.Session.Name;
                }

                ev.AddLine(Loc.GetString("zombie-round-end-user-was-survivor",
                    ("name", meta.EntityName),
                    ("username", username)));
            }
        }
    }
}
