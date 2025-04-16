using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Menu;

namespace WarcraftPlugin.Core
{
    public class WcsRankSystem
    {
        private Database? _database;
        private Timer? _waitForWcPluginTimer;
        private BasePlugin _plugin;

        public WcsRankSystem(BasePlugin plugin)
        {
            _plugin = plugin;
        }

        public void Initialize()
        {
            _plugin.AddCommand("say", "Chat command handler", OnChatCommand);
            _waitForWcPluginTimer = _plugin.AddTimer(1.0f, WaitForWarcraftPlugin, TimerFlags.REPEAT);
            _plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);

        }

        private void WaitForWarcraftPlugin()
        {
            if (WarcraftPlugin.Instance == null)
            {
                Server.PrintToConsole("[WCS Rank] ❌ Waiting for WarcraftPlugin.Instance...");
                return;
            }

            _database = WarcraftPlugin.Instance.GetDatabase();

            if (_database == null)
            {
                Server.PrintToConsole("[WCS Rank] ❌ WarcraftPlugin.Instance loaded but GetDatabase() returned null.");
                return;
            }

            Server.PrintToConsole("[WCS Rank] ✅ WarcraftPlugin successfully linked. Rank system is ready!");
            _waitForWcPluginTimer?.Kill();
            _waitForWcPluginTimer = null;
        }


        private void OnChatCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid) return;

            var msg = commandInfo.GetArg(1).ToLower();

            if (msg is "!rank" or "!wcsrank" or "rank" or "wcsrank")
            {
                ShowPlayerRank(player);
            }
            else if (msg is "!top" or "!wcstop" or "top" or "wcstop" or "top10")
            {
                ShowTop10InChat(player);
            }
        }

        private void ShowPlayerRank(CCSPlayerController player)
        {
            if (_database == null)
            {
                player.PrintToChat(" {ChatColors.Red}[WCS] Rank system is currently unavailable.");
                return;
            }

            var allClassData = _database.LoadClassInformationFromDatabase(player);

            if (allClassData == null || allClassData.Count == 0)
            {
                player.PrintToChat(" {ChatColors.Gray}[WCS] You don't have any race data yet.");
                return;
            }

            int totalLevel = allClassData.Sum(race => race.CurrentLevel);

            // Leaderboard rank
            var connection = typeof(Database)
                .GetField("_connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_database) as Microsoft.Data.Sqlite.SqliteConnection;

            int rank = -1;
            if (connection != null)
            {
                rank = connection.ExecuteScalar<int>(
                    @"SELECT COUNT(*) + 1 FROM (
                SELECT steamid, SUM(currentLevel) as totalLevel 
                FROM raceinformation 
                GROUP BY steamid 
                HAVING totalLevel > @PlayerTotal
            );", new { PlayerTotal = totalLevel });
            }

            // Extra stats
            var stats = connection?.Query<(string Race, int Kills, int Deaths)>(
                @"SELECT race, kills, deaths FROM playerstats WHERE steamid = @steamid;",
                new { steamid = player.SteamID })?.ToList() ?? new List<(string, int, int)>();

            int totalKills = stats.Sum(s => s.Kills);
            int totalDeaths = stats.Sum(s => s.Deaths);
            double kdRatio = totalDeaths > 0 ? (double)totalKills / totalDeaths : totalKills;
            string mostPlayedRace = stats.OrderByDescending(s => s.Kills).FirstOrDefault().Race ?? "N/A";
            int mostPlayedKills = stats.OrderByDescending(s => s.Kills).FirstOrDefault().Kills;

            int classCount = WarcraftPlugin.Instance.classManager.GetAllClasses().Count();
            int maxLevelPerRace = 16;
            int maxTotalLevel = classCount * maxLevelPerRace;

            string kdColor;

            if (kdRatio < 1.0)
                kdColor = ChatColors.Red.ToString();
            else if (kdRatio < 1.5)
                kdColor = ChatColors.Yellow.ToString();
            else
                kdColor = ChatColors.Green.ToString();



            // Display
            player.PrintToChat($" {ChatColors.Red}★{ChatColors.Default} {ChatColors.LightYellow}Your WCS Rank Summary{ChatColors.Default} {ChatColors.Red}★{ChatColors.Default}");
            player.PrintToChat($" {ChatColors.Grey}Total Level:{ChatColors.Green} {totalLevel} / {maxTotalLevel}");
            player.PrintToChat($" {ChatColors.Grey}Races Trained:{ChatColors.Green} {allClassData.Count} / {classCount}");
            player.PrintToChat($" {ChatColors.Grey}Leaderboard Rank:{ChatColors.Green} #{rank}");
            player.PrintToChat("─────────────────────────────");
            player.PrintToChat($" {ChatColors.Grey}Total Kills:{ChatColors.White} {totalKills}");
            player.PrintToChat($" {ChatColors.Grey}Total Deaths:{ChatColors.White} {totalDeaths}");
            player.PrintToChat($" {ChatColors.Grey}K/D Ratio:{kdColor} {kdRatio:0.00}");
            player.PrintToChat($" {ChatColors.Grey}Most Played:{ChatColors.White} {mostPlayedRace} ({mostPlayedKills} kills)");
            player.PrintToChat("─────────────────────────────");
        }

        private void ShowPlayerStatsMenu(CCSPlayerController viewer, ulong steamId)
        {
            if (_database == null) return;

            var connection = typeof(Database)
                .GetField("_connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_database) as Microsoft.Data.Sqlite.SqliteConnection;

            if (connection == null) return;

            string name = _database.GetPlayerName(steamId.ToString()) ?? $"SteamID: {steamId}";

            // Get all race stats
            var stats = connection.Query<(string Race, int Kills, int Deaths)>(
                @"SELECT race, kills, deaths FROM playerstats WHERE steamid = @steamid;",
                new { steamid = steamId }).ToList();

            // Get total level
            int totalLevel = connection.ExecuteScalar<int>(
                @"SELECT SUM(currentLevel) FROM raceinformation WHERE steamid = @steamid;",
                new { steamid = steamId });

            int totalKills = stats.Sum(s => s.Kills);
            int totalDeaths = stats.Sum(s => s.Deaths);
            double kdRatio = totalDeaths > 0 ? (double)totalKills / totalDeaths : totalKills;

            string mostPlayedRace = stats.OrderByDescending(s => s.Kills).FirstOrDefault().Race ?? "N/A";
            int mostPlayedKills = stats.OrderByDescending(s => s.Kills).FirstOrDefault().Kills;

            var menu = MenuManagerExtra.CreateMenu($"<font color='#D4AF37'>{name}'s WCS Stats</font>", 6);
            menu.Category = $"Player Stats";

            menu.Add($" <font color='#A0A0A0'>Total Level:</font><font color='#FFFFFF'> {totalLevel}</font>", null, null);
            menu.Add($" <font color='#A0A0A0'>Total Kills:</font><font color='#FFFFFF'> {totalKills}</font>", null, null);
            menu.Add($" <font color='#A0A0A0'>Total Deaths:</font><font color='#FFFFFF'> {totalDeaths}</font>", null, null);
            menu.Add($" <font color='#A0A0A0'>K/D Ratio:</font><font color='#FFFFFF'> {kdRatio:0.00}</font>", null, null);
            menu.Add($" <font color='#A0A0A0'>Most Played:</font><font color='#FFFFFF'> {mostPlayedRace} ({mostPlayedKills} kills)</font>", null, null);
            menu.Add(" <font color='#FF6666'>↩ Return to Top10 Menu</font>", null, (pl, _) => ShowTop10InChat(pl));

            MenuManagerExtra.OpenMainMenuExtra(viewer, new List<Menu.Menu> { menu });
        }



        private void ShowTop10InChat(CCSPlayerController player)
        {
            if (_database == null)
            {
                player.PrintToChat("[WCS] Rank system is currently unavailable.");
                return;
            }

            var connection = typeof(Database)
                .GetField("_connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_database) as Microsoft.Data.Sqlite.SqliteConnection;

            if (connection == null)
            {
                player.PrintToChat("[WCS] Couldn't access database connection.");
                return;
            }

            var results = connection.Query<(ulong SteamId, int TotalLevel)>(
                @"SELECT steamid, SUM(currentLevel) AS TotalLevel 
          FROM raceinformation 
          GROUP BY steamid 
          ORDER BY TotalLevel DESC 
          LIMIT 10;").ToList();

            if (results.Count == 0)
            {
                player.PrintToChat("[WCS] No player rank data found.");
                return;
            }

            var pages = new List<Menu.Menu>();

            for (int pageIndex = 0; pageIndex < 2; pageIndex++)
            {
                var menu = MenuManagerExtra.CreateMenu($"Leaderboard Page {pageIndex + 1}/2", 6);
                menu.Category = "Top10";

                for (int i = 0; i < 5; i++)
                {
                    int idx = pageIndex * 5 + i;
                    if (idx >= results.Count)
                        break;

                    var row = results[idx];
                    string playerName = _database.GetPlayerName(row.SteamId.ToString()) ?? $"SteamID: {row.SteamId}";
                    string label;
                    string color;
                    string emoji;

                    switch (idx)
                    {
                        case 0:
                            color = "\x06"; emoji = "🥇"; break;
                        case 1:
                            color = "\x0B"; emoji = "🥈"; break;
                        case 2:
                            color = "\x0E"; emoji = "🥉"; break;
                        default:
                            color = "\x01"; emoji = "🔹"; break;
                    }

                    label = $"{color}{emoji} #{idx + 1} - {playerName} ({row.TotalLevel} lvl)";

                    menu.Add(label, null, (pl, opt) =>
                    {
                        ShowPlayerStatsMenu(pl, row.SteamId);
                    });
                }

                pages.Add(menu);
            }

            MenuManagerExtra.OpenMainMenuExtra(player, pages);
        }


        private HookResult OnPlayerHurt(EventPlayerHurt e, GameEventInfo info)
        {
            if (e == null || e.Userid == null || !e.Userid.IsValid)
                return HookResult.Continue;

            var victim = e.Userid;

            foreach (var dummy in DummyBotManager.GetAllTrackedDummies())
            {
                if (dummy.Value == victim && victim.IsValid && victim.IsAlive())
                {
                    var currentHp = victim.PlayerPawn.Value.Health;
                    var newHp = Math.Max(1, currentHp - e.DmgHealth);

                    // Console log
                    var attacker = e.Attacker;
                    var name = attacker?.PlayerName ?? "Unknown";
                    Console.WriteLine($"[Dummy] {name} dealt {e.DmgHealth} damage — HP: {currentHp} → {newHp}");
                }
            }

            return HookResult.Continue;
        }

    }








}
