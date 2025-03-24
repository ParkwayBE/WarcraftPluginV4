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

            var allClassData = _database.LoadClassInformationFromDatabase(player);

            if (allClassData == null || allClassData.Count == 0)
            {
                player.PrintToChat("[WCS] You don't have any race data yet.");
                return;
            }

            int totalLevel = allClassData.Sum(race => race.CurrentLevel);

            // Get leaderboard rank
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

            int classCount = WarcraftPlugin.Instance.classManager.GetAllClasses().Count();
            int maxLevelPerRace = 16;
            int maxTotalLevel = classCount * maxLevelPerRace;

            // Set line width for consistent alignment
            const int lineWidth = 40;

            // Raw label/value strings
            string label1 = "Total Level:";
            string value1 = $"{totalLevel} / {maxTotalLevel}";

            string label2 = "Races Trained:";
            string value2 = $"{allClassData.Count} / {classCount}";

            string label3 = "Leaderboard Rank:";
            string value3 = $"#{rank}";

            // Build spaced-out strings BEFORE adding color
            string line1Raw = label1.PadRight(lineWidth - value1.Length) + value1;
            string line2Raw = label2.PadRight(lineWidth - value2.Length) + value2;
            string line3Raw = label3.PadRight(lineWidth - value3.Length) + value3;

            // Add color formatting after spacing
            string line1 = $" \x04{line1Raw.Substring(0, label1.Length)}\x01{line1Raw.Substring(label1.Length)}";
            string line2 = $" \x04{line2Raw.Substring(0, label2.Length)}\x01{line2Raw.Substring(label2.Length)}";
            string line3 = $" \x04{line3Raw.Substring(0, label3.Length)}\x01{line3Raw.Substring(label3.Length)}";

            // Send to chat
            player.PrintToChat(" \x0B★ \x06Your WCS Rank Summary \x0B★");
            player.PrintToChat(line1);
            player.PrintToChat(line2);
            player.PrintToChat(line3);
            player.PrintToChat("────────────────────────────────────────");
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
                string paddedName = name.Length > 24 ? name.Substring(0, 24) : name.PadRight(24);
                string paddedLevel = row.TotalLevel.ToString().PadLeft(4); // aligns right (e.g., " 208")

                player.PrintToChat($"{emoji} \x09{paddedName} \x01– \x07{paddedLevel} levels");

                rank++;
            }

            player.PrintToChat("────────────────────────────");
        }

    }
}
