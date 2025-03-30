using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using WarcraftPlugin.Helpers;
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
            _plugin.AddCommand("adm", "opens admin panel", OpenAdminPanel);
            _plugin.RegisterEventHandler<EventPlayerChat>(OnPlayerChat, HookMode.Pre);
            //_plugin.RegisterEventHandler<EventPlayerChat>(OnPlayerChat2, HookMode.Pre);
            _plugin.AddCommandListener("say", OnPlayerChat2);
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
                //spawn dummy
                classMenu.Add("Spawn Dummy", null, (pl, opt) =>
                {
                    DummyBotManager.SpawnOrResetDummy(player);
                });

                //Freeze option
                classMenu.Add("Freeze bots", null, (pl, opt) =>
                {
                    player.ExecuteClientCommandFromServer($"css_freeze @bot");
                });

                //Kill option
                classMenu.Add("Kill player menu", null, (pl, opt) =>
                {
                    var killSubMenu = MenuManager.CreateMenu("Kill a player", 5);
                    foreach (var targetPlayer in Players)
                    {
                        killSubMenu.Add(targetPlayer.PlayerName, null, (pl, opt) =>
                        {
                            //player.ExecuteClientCommand($"css_slay #{targetPlayer.SteamID.ToString()}");
                            if (!targetPlayer.IsBot)
                            {

                                player.ExecuteClientCommandFromServer($"css_slay #{targetPlayer.SteamID.ToString()}");
                            }
                            else
                            {
                                player.ExecuteClientCommandFromServer($"css_slay {targetPlayer.GetRealPlayerName()}");
                            }
                        
                            

                        });
                    }
                    killSubMenu.Add("Back", null, (p, opt2) =>
                    {
                        MenuManager.OpenMainMenu(p, classMenu);
                    });
                    MenuManager.OpenMainMenu(pl, killSubMenu);
                });

                //Say Option
                classMenu.Add("Say menu", null, (admin, opt) => // Rename `pl` to `admin`
                {
                    var saySubMenu = MenuManager.CreateMenu("Say to a player", 5);

                    foreach (var targetPlayer in Players)
                    {
                        saySubMenu.Add(targetPlayer.PlayerName, null, (selectedAdmin, opt2) => // Rename `pl` to `selectedAdmin`
                        {


                            RequestInput(player, (mess) =>
                            {
                                targetPlayer.PrintToChat($"{mess}");
                            });
                          
                        });
                    }

                    saySubMenu.Add("Back", null, (selectedAdmin, opt3) =>
                    {
                        MenuManager.OpenMainMenu(selectedAdmin, classMenu);
                    });

                    MenuManager.OpenMainMenu(admin, saySubMenu);
                });
                //AddXp Option
                classMenu.Add("XP Menu", null, (admin, opt) => // Rename `pl` to `admin`
                {
                    var saySubMenu = MenuManager.CreateMenu("Add xp to player", 5);

                    foreach (var targetPlayer in Players)
                    {
                        saySubMenu.Add(targetPlayer.PlayerName, null, async (selectedAdmin, opt2) => // Rename `pl` to `selectedAdmin`
                        {
                            RequestInput(player, (mess) =>
                            {
                                int num = int.Parse(mess);
                                if (num > 0)
                                {
                                    _plugin.XpSystem.AddXp(targetPlayer, num);
                                }
                            });
                        });
                    }

                    saySubMenu.Add("Back", null, (selectedAdmin, opt3) =>
                    {
                        MenuManager.OpenMainMenu(selectedAdmin, classMenu);
                    });

                    MenuManager.OpenMainMenu(admin, saySubMenu);
                });

                MenuManager.OpenMainMenu(player, classMenu);
            }
            if (role == 9009)
            {
                player.PrintToChat("Nah, no roles for u");
            }

        }
        public HookResult OnPlayerChat(EventPlayerChat ev, GameEventInfo info)
        {
           var player = Utilities.GetPlayerFromUserid(ev.Userid);
           var message = ev.Text.Trim();

        
          if (player == null) return HookResult.Continue;
        
           if (pendingInputs.TryGetValue(player, out var action))
           {
             action.Invoke(message);  // Process stored action
             pendingInputs.Remove(player); // Remove from pending inputs
         
             info.DontBroadcast = true;
             return HookResult.Stop;
           
            }

            return HookResult.Continue;
        }
        public HookResult OnPlayerChat2(CCSPlayerController? player, CommandInfo info)
        {
            //var player = Utilities.GetPlayerFromUserid(ev.Userid);
            //var message = ev.Text.Trim();
            var message = info.GetArg(1);
            Console.WriteLine($"message is : {message}");
            if (player == null) return HookResult.Continue;
         
            if (pendingInputs.TryGetValue(player, out var action))
            {
                action.Invoke(message);  // Process stored action
                pendingInputs.Remove(player);
                return HookResult.Handled;
            }

                return HookResult.Continue; // Allow other chat messages to continue
        }


        private void RequestInput(CCSPlayerController admin, Action<string> callback)
        {
            admin.PrintToChat("write in chat to add argument");
            if (pendingInputs.ContainsKey(admin))
            {
                pendingInputs.Remove(admin);
            }
            var mess = "";

            pendingInputs[admin] = (message) =>
            {
                mess = message;
                callback(mess);
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
