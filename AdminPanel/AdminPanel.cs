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
            admins.Add("76561198024206738");
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

                // Create multiple columns of menus
                var leftMenu = MenuManagerExtra.CreateMenu("Left Side Menu", 5);
                var middleMenu = MenuManagerExtra.CreateMenu("Middle Menu", 5);
                var rightMenu = MenuManagerExtra.CreateMenu("Right Side Menu", 5);

                // List of menus for navigation
                List<Menu.Menu> menus = new() { leftMenu, middleMenu, rightMenu };

                // Left Menu Options
                leftMenu.Add("→ Next", null, (pl, opt) =>
                {
                    MenuManagerExtra.SwitchMenu(pl, true); // Move to the right menu
                });
                leftMenu.Add("Spawn Dummy", null, (pl, opt) =>
                {
                    DummyBotManager.SpawnOrResetDummy(pl);
                });
                leftMenu.Add("Freeze Bots", null, (pl, opt) =>
                {
                    pl.ExecuteClientCommandFromServer($"css_freeze @bots");
                });
                

                // Middle Menu Options
                middleMenu.Add("Admin Options", null, (pl, opt) =>
                {
                    pl.PrintToChat("Admin options selected.");
                });
                middleMenu.Add("← Back", null, (pl, opt) =>
                {
                    MenuManagerExtra.SwitchMenu(pl, false); // Move to the left menu
                });
                middleMenu.Add("→ Next", null, (pl, opt) =>
                {
                    MenuManagerExtra.SwitchMenu(pl,  true);
                });
                

                // Right Menu Options
                rightMenu.Add("Kill Player Menu", null, (pl, opt) =>
                {
                    var killSubMenu = MenuManager.CreateMenu("Kill a player", 5);
                    foreach (var targetPlayer in Players)
                    {
                        killSubMenu.Add(targetPlayer.PlayerName, null, (pl, opt) =>
                        {
                            pl.ExecuteClientCommandFromServer($"css_slay #{targetPlayer.SteamID}");
                        });
                    }
                    MenuManagerExtra.OpenMainMenu(pl, new List<Menu.Menu> { killSubMenu }); // Only single menu here
                });
                rightMenu.Add("← Back", null, (pl, opt) =>
                {
                    MenuManagerExtra.SwitchMenu(pl, false); // Move to the left menu
                });
                rightMenu.Add("XP Menu", null, (pl, opt) =>
                {
                    var xpSubMenu = MenuManager.CreateMenu("XP Menu", 5);
                    xpSubMenu.Add("All Players", null, (pl, opt) =>
                    {
                        RequestInput(pl, (mess) =>
                        {
                            int num = int.Parse(mess);
                            if (num > 0)
                            {
                                foreach (var targetPlayer in Players)
                                {
                                    _plugin.XpSystem.AddXp(targetPlayer, num);
                                }
                            }
                        });
                    });
                    MenuManagerExtra.OpenMainMenu(pl, new List<Menu.Menu> { xpSubMenu });
                });

                

                // Open the multi-column menu for the player
                MenuManagerExtra.OpenMainMenu(player, menus);

                /*
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
                    player.ExecuteClientCommandFromServer($"css_freeze @bots");
                });
                classMenu.Add("unFreeze bots", null, (pl, opt) =>
                {
                    player.ExecuteClientCommandFromServer($"css_unfreeze @bots");
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
                    //all players
                    saySubMenu.Add("All", null, async (selectedAdmin, opt2) =>
                    {
                        RequestInput(player, (mess) =>
                        {
                            int num = int.Parse(mess);
                            if (num > 0)
                            {
                                foreach (var targetPlayer in Players)
                                {
                                    _plugin.XpSystem.AddXp(targetPlayer, num);
                                }
                            }
                        });
                        
                    });
                    //each player solo
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
             */

            }
            if (role == 9009)
            {
                player.PrintToChat("Nah, no roles for u");
            }
        
        }
        void openMenu(CCSPlayerController player)
        {

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
namespace WarcraftPlugin.Menu
{
    internal static class MenuManagerExtra
    {
        private static Dictionary<int, int> PlayerMenuColumn = new();
        public static Dictionary<int, List<Menu>> PlayerMenus = new();

        internal static void OpenMainMenu(CCSPlayerController player, List<Menu> menus, int selectedOptionIndex = 0)
        {
            if (player == null || menus == null || menus.Count == 0)
                return;

            if (!PlayerMenus.ContainsKey(player.Slot))
                PlayerMenus[player.Slot] = new List<Menu>();

            // Store available menus for the player
            PlayerMenus[player.Slot] = menus;

            if (!PlayerMenuColumn.ContainsKey(player.Slot))
                PlayerMenuColumn[player.Slot] = 0;

            int column = PlayerMenuColumn[player.Slot];
            column = Math.Clamp(column, 0, menus.Count - 1);
            PlayerMenuColumn[player.Slot] = column;

            MenuAPI.Players[player.Slot].OpenMainMenu(menus[column], selectedOptionIndex);
        }

        internal static void CloseMenu(CCSPlayerController player)
        {
            if (player == null)
                return;
            MenuAPI.Players[player.Slot].OpenMainMenu(null);
        }

        internal static void CloseSubMenu(CCSPlayerController player)
        {
            if (player == null)
                return;
            MenuAPI.Players[player.Slot].CloseSubMenu();
        }

        internal static void CloseAllSubMenus(CCSPlayerController player)
        {
            if (player == null)
                return;
            MenuAPI.Players[player.Slot].CloseAllSubMenus();
        }

        internal static void OpenSubMenu(CCSPlayerController player, Menu menu)
        {
            if (player == null)
                return;
            MenuAPI.Players[player.Slot].OpenSubMenu(menu);
        }

        internal static Menu CreateMenu(string title = "", int resultsBeforePaging = 4)
        {
            return new Menu
            {
                Title = title,
                ResultsBeforePaging = resultsBeforePaging,
            };
        }

        internal static void SwitchMenu(CCSPlayerController player, bool moveRight)
        {
            if (player == null || !PlayerMenus.ContainsKey(player.Slot) || PlayerMenus[player.Slot].Count < 2)
                return;

            List<Menu> menus = PlayerMenus[player.Slot];

            if (!PlayerMenuColumn.ContainsKey(player.Slot))
                PlayerMenuColumn[player.Slot] = 0;

            int column = PlayerMenuColumn[player.Slot];
            column = moveRight ? Math.Min(column + 1, menus.Count - 1) : Math.Max(column - 1, 0);

            PlayerMenuColumn[player.Slot] = column;

            // Use MenuAPI directly to open the selected menu
            MenuAPI.Players[player.Slot].OpenMainMenu(menus[column]);
        }
    }
}