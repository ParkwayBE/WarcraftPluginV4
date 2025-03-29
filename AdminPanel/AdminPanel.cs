using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using WarcraftPlugin.Menu;
using WarcraftPlugin.Menu.WarcraftMenu;

namespace WarcraftPlugin.Core
{


    public class AdminPanel
    {
        private readonly WarcraftPlugin _plugin;
        List<String> admins = new List<String>();


        public AdminPanel(WarcraftPlugin plugin)
        {
            _plugin = plugin;
            admins.Add("76561198061919153");
            _plugin.AddCommand("adminPanel", "opens admin panel", OpenAdminPanel);


        }

        public void OpenAdminPanel(CCSPlayerController? player, CommandInfo commandInfo)
        {
            

            if (!admins.Contains(player.SteamID.ToString()))
            {
                return;
            }
            
            var wcPlayer = _plugin.GetWcPlayer(player);
            if (wcPlayer == null) return;
            var playerP = wcPlayer.GetPlayer();
       
            var role = 1;
            var message = "hallo mannekes";

            //int role = _db.GetPlayerRole(playerP);
            if (role == 0)
            {
                player.PrintToChat("You are a player, You cant use admin panel");
            }
            if (role == 1)
            {
                playerP.PrintToChat("You are an admin, the panel will open soon ;)");
                Console.WriteLine("admin panel test");
                var Players = Utilities.GetPlayers();
                var classMenu = MenuManager.CreateMenu(@$"<font color='lightgrey' class='{FontSizes.FontSizeM}'>
                    {player.SteamID.ToString()}'s Admin Menu</font><br>
                    <font color='grey'>select an option</font> ", 5);
                foreach (var targetPlayer in Players) // Changed the loop variable to 'targetPlayer'
                {
                    classMenu.Add(targetPlayer.PlayerName, null, (pl, opt) => // Use 'pl' for the callback parameter
                    {
                        // This will now correctly print to the 'targetPlayer' chat, not the player who opened the menu
                        targetPlayer.PrintToChat($"{targetPlayer.PlayerName} I see you");
                    });
                }
                //playerP.PrintToCenterHtml("<font color='#FFFFFF'>AAAADDMIN PANEL</font>");

                classMenu.Add("Close Menu", null, (p, opt) =>
                {
                    MenuManager.CloseMenu(player);
                });
                MenuManager.OpenMainMenu(player, classMenu);
            }
            if (role == 9009)
            {
                player.PrintToChat("Nah, no roles for u");
            }

        }

        public void ChangeRole(CCSPlayerController player, int role)
        {
            //_db.ChangePlayerRole(player, role);
        }
        //[GameEventHandler]
        //public HookResult PlayerSpawnHandler(EventPlayerSpawn @event, GameEventInfo info)
        //{
        //  Console.WriteLine($"Player Spawned: {@event.Userid}");

        // Send a message to all clients


        //var player = @event.Userid;
        //var player = Utilities.GetPlayerFromUserid(@event.Userid);
        // var player = @event.Userid;
        //  var message = "adminPanel";

        //  player.ExecuteClientCommand($"say adminPanel");

        //  return HookResult.Continue;
        // }
    }
}
namespace CounterStrikeSharp.API.Core
{
    public partial class CCSPlayerControllerExtra : CCSPlayerController
    {
        public int Role;
        public CCSPlayerControllerExtra(nint index)
        : base(index)
        {
            this.Role = 0;
        }

        public int GetRole() { return this.Role; }
    }


}
