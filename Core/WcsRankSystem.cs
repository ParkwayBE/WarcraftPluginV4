using System;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using WarcraftPlugin.Helpers;

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
            else if (msg is "!dummy" or "!spawn_dummy")
            {
                DummyBotManager.SpawnDummyBot(player);
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

            int classCount = WarcraftPlugin.Instance.classManager.GetAllClasses().Count();
            int maxLevelPerRace = 16;
            int maxTotalLevel = classCount * maxLevelPerRace;

            // 👇 Full-width target column for alignment (change if needed)
            const int valueColumnStart = 36;

            // Build raw lines
            string line1 = BuildRankLine("Total Level:", $"{totalLevel} / {maxTotalLevel}", valueColumnStart);
            string line2 = BuildRankLine("Races Trained:", $"{allClassData.Count} / {classCount}", valueColumnStart);
            string line3 = BuildRankLine("Leaderboard Rank:", $"#{rank}", valueColumnStart);

            // Print all
            player.PrintToChat(" \x0B★ \x06Your WCS Rank Summary ★");
            player.PrintToChat(line1);
            player.PrintToChat(line2);
            player.PrintToChat(line3);
            player.PrintToChat("────────────────────────────────────────");
        }

        // ✅ Helper method
        private string BuildRankLine(string label, string value, int valueStartColumn)
        {
            int space = Math.Max(1, valueStartColumn - label.Length);
            return $" \x04{label}{new string(' ', space)}\x01{value}";
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

    public static class DummyBotManager
    {
        public static void SpawnDummyBot(CCSPlayerController owner)
        {
            var eyePos = owner.EyePosition();
            var forward = owner.PlayerPawn.Value.EyeAngles.ToForward();
            var spawnPos = eyePos + forward * 100;

            Server.ExecuteCommand("bot_add_t");

            WarcraftPlugin.Instance.AddTimer(0.5f, () =>
            {
                var bot = Utilities.GetPlayers().LastOrDefault(p =>
                    p != null &&
                    p.IsBot &&
                    p.IsValid &&
                    p != owner &&
                    p.TeamNum == (byte)CsTeam.Terrorist &&
                    p.PlayerPawn?.Value != null);

                if (bot == null)
                {
                    owner.PrintToChat(" \x07[Dummy] Failed to spawn bot.");
                    return;
                }

                bot.PlayerPawn.Value.Teleport(spawnPos, new QAngle(), new Vector());

                bot.PlayerPawn.Value.MaxHealth = 9999;
                bot.PlayerPawn.Value.Health = 9999;

                bot.PlayerPawn.Value.Speed = 0.0f;
                bot.PlayerPawn.Value.VelocityModifier = 0f;

                bot.PlayerPawn.Value.SetColor(Color.Gray);
                bot.PlayerName = "TrainingDummy";

                bot.PrintToChat(" \x06[Dummy] You are now a training dummy.");
                owner.PrintToChat($" \x04[Dummy] Spawned dummy: {bot.PlayerName}");
            });
        }
    }

    public static class AngleExtensions
    {
        public static Vector ToForward(this QAngle angle)
        {
            float pitch = angle.X * (float)(Math.PI / 180.0);
            float yaw = angle.Y * (float)(Math.PI / 180.0);

            float x = (float)(Math.Cos(pitch) * Math.Cos(yaw));
            float y = (float)(Math.Cos(pitch) * Math.Sin(yaw));
            float z = (float)-Math.Sin(pitch);

            return new Vector(x, y, z);
        }
    }

}
