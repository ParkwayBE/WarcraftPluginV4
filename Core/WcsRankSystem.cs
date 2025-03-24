using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using WarcraftPlugin.Core;
using System.Linq;

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
            // Hook into chat commands
            _plugin.AddCommand("say", "Chat command handler", OnChatCommand);

            // Wait until WarcraftPlugin is fully initialized before accessing the database
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
            if (msg == "!rank" || msg == "!wcsrank")
            {
                ShowPlayerRank(player);
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
            int maxLevelPerRace = 16;
            int maxTotalLevel = allClassData.Count * maxLevelPerRace;

            player.PrintToChat($"[WCS] Your total level across all races is {totalLevel} / {maxTotalLevel}.");
        }
    }
}
