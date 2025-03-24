using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using WarcraftPlugin.Core; // Make sure this is correct depending on where your Database.cs lives
using WarcraftPlugin.Models;
using System.Linq;

public class WcsRankPlugin : BasePlugin
{
    public override string ModuleName => "WcsRankPlugin";
    public override string ModuleVersion => "1.0.0";

    private Database? _database;

    public override void Load(bool hotReload)
    {
        AddCommand("say", "Chat command handler", OnChatCommand);

        // Reference the WCS Database instance
        _database = WarcraftPlugin.WarcraftPlugin.Instance.GetDatabase();
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
