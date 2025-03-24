using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using WarcraftPlugin.Core;
using System.Linq;
using System.Collections.Generic;
using Dapper;

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
                player.PrintToChat("[WCS] Rank system is currently unavailable.");
                return;
            }

            var allClassData = _database.LoadClassInformationFromDatabase(player)
                .ToDictionary(x => x.RaceName, x => x);

            var allClasses = WarcraftPlugin.Instance.classManager.GetAllClasses();

            int totalLevel = 0;
            int maxLevelPerRace = WarcraftPlugin.MaxLevel;
            int classCount = allClasses.Count();

            foreach (var warcraftClass in allClasses)
            {
                if (allClassData.TryGetValue(warcraftClass.InternalName, out var classInfo))
                {
                    totalLevel += classInfo.CurrentLevel;
                }
                else
                {
                    totalLevel += 1;
                }
            }

            int maxTotalLevel = classCount * maxLevelPerRace;
            player.PrintToChat($"[WCS] Your total level across all races is {totalLevel} / {maxTotalLevel}.");
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

            player.PrintToChat(" \x0B★ \x06WCS Leaderboard — Top 10 Players \x0B★");

            int rank = 1;
            foreach (var row in results)
            {
                string? name = _database?.GetPlayerName(row.SteamId.ToString()) ?? $"SteamID: {row.SteamId}";
                string emoji = rank switch
                {
                    1 => "★",
                    2 => "☆",
                    3 => "○",
                    _ => $"#{rank}"
                };


                string color = row.SteamId == player.SteamID ? "\x10" : "\x09"; // highlight if it's the local player
                player.PrintToChat($"{emoji} {color}{name} \x01– \x07{row.TotalLevel} levels");
                rank++;
            }

            player.PrintToChat("────────────────────────────");
        }

    }
}
