using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using WarcraftPlugin.Menu;
using WarcraftPlugin.Menu.WarcraftMenu;

namespace WarcraftPlugin.Core
{


    public class AdminPanel
    {
        private readonly WarcraftPlugin _plugin;
        List<String> admins = new List<String>();
        private static Dictionary<CCSPlayerController, Action<string>> pendingInputs = new Dictionary<CCSPlayerController, Action<string>>();


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
                //Kill option
                classMenu.Add("Kill player menu", null, (pl, opt) =>
                {
                    var killSubMenu = MenuManager.CreateMenu("Kill a player", 5);
                    foreach (var targetPlayer in Players)
                    {
                        killSubMenu.Add(targetPlayer.PlayerName, null, (pl, opt) =>
                        {
                            //player.ExecuteClientCommand($"css_slay #{targetPlayer.SteamID.ToString()}");
                            
                            player.ExecuteClientCommandFromServer($"css_slay #{targetPlayer.SteamID.ToString()}");

                        });
                    }
                    killSubMenu.Add("Back", null, (p, opt2) =>
                    {
                        MenuManager.OpenMainMenu(p, classMenu);
                    });
                    MenuManager.OpenMainMenu(pl, killSubMenu);
                });

                //Say Option
                classMenu.Add("Say menu", null, (pl, opt) =>
                {
                    var killSubMenu = MenuManager.CreateMenu("Say to a player", 5);
                    foreach (var targetPlayer in Players)
                    {
                        killSubMenu.Add(targetPlayer.PlayerName, null, (pl, opt) =>
                        {

                            RequestInput(pl, targetPlayer);
  

                        });
                    }
                    killSubMenu.Add("Back", null, (p, opt2) =>
                    {
                        MenuManager.OpenMainMenu(p, classMenu);
                    });
                    MenuManager.OpenMainMenu(pl, killSubMenu);
                });

                MenuManager.OpenMainMenu(player, classMenu);
            }
            if (role == 9009)
            {
                player.PrintToChat("Nah, no roles for u");
            }

        }
        [GameEventHandler]
        public HookResult OnPlayerChat(EventPlayerChat ev)
        {
            var player = Utilities.GetPlayerFromUserid(ev.Userid);
            var message = ev.Text.Trim();

            if (player == null) return HookResult.Continue;

            if (pendingInputs.TryGetValue(player, out var action))
            {
                action.Invoke(message);  // Process stored action
                pendingInputs.Remove(player); // Remove from pending inputs
                return HookResult.Handled; // Block message from showing in chat
            }

            return HookResult.Continue; // Allow normal chat behavior
        }
        private void RequestInput(CCSPlayerController admin, CCSPlayerController target)
        {
            admin.PrintToChat("Type the message in chat for the selected player");
            if (pendingInputs.ContainsKey(admin))
            {
                pendingInputs.Remove(admin);
            }


            pendingInputs[admin] = (message) =>
            {
                target.PrintToChat($"{message}");
            };
        }

        public void ChangeRole(CCSPlayerController player, int role)
        {
            //_db.ChangePlayerRole(player, role);
        }
        
        public HookResult PlayerSpawnHandler(EventPlayerSpawn @event, GameEventInfo info)
        {
            Console.WriteLine($"Player Spawned: {@event.Userid}");

          return HookResult.Continue;
         }
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
