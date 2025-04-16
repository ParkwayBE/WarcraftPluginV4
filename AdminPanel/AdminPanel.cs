using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Menu;
using CSVector = CounterStrikeSharp.API.Modules.Utils.Vector;


namespace WarcraftPlugin.Core
{
    public class AdminPanel
    {
        private readonly WarcraftPlugin _plugin;
        List<String> admins = new List<String>();
        private static Dictionary<CCSPlayerController, Action<string>> pendingInputs = new Dictionary<CCSPlayerController, Action<string>>();
        private List<ulong> _mutedPlayers = new List<ulong>();
        Database _db;
        private Timer? _waitForWcPluginTimer;

        public AdminPanel(WarcraftPlugin plugin)
        {
            _plugin = plugin;
            admins.Add("76561198061919153");
            admins.Add("76561198024206738");
            _plugin.AddCommand("adm", "opens admin panel", OpenAdminPanel);
            //_plugin.AddCommand("say", "for mutes", CheckIfMuted);
            //_plugin.RegisterEventHandler<EventPlayerChat>(OnPlayerChat, HookMode.Pre);
            //_plugin.RegisterEventHandler<EventPlayerChat>(OnPlayerChat2, HookMode.Pre);
            _plugin.AddCommandListener("say", OnPlayerChat2);
            _waitForWcPluginTimer = _plugin.AddTimer(1.0f, WaitForWarcraftPlugin, TimerFlags.REPEAT);
        }

        private void WaitForWarcraftPlugin()
        {
            if (WarcraftPlugin.Instance == null)
            {
                Server.PrintToConsole("[WCS Rank] ❌ Waiting for WarcraftPlugin.Instance...");
                return;
            }

            _db = WarcraftPlugin.Instance.GetDatabase();

            if (_db == null)
            {
                Server.PrintToConsole("[WCS Rank] ❌ WarcraftPlugin.Instance loaded but GetDatabase() returned null.");
                return;
            }

            Server.PrintToConsole("[WCS Rank] ✅ WarcraftPlugin successfully linked. Rank system is ready!");
            _waitForWcPluginTimer?.Kill();
            _waitForWcPluginTimer = null;
            LoadMutedPlayers();
        }
        //private HookResult CheckIfMuted
        private void LoadMutedPlayers()
        {
            Database db = _plugin.GetDatabase();
            var mutedPlayers = db.GetAllMutedPlayers();
            if (mutedPlayers == null)
            {
                return;
            }
            if (mutedPlayers.Count == 0)
            {
                return;
            }
            _mutedPlayers.Clear();
            foreach (var mutedPlayer in mutedPlayers)
            {

                _mutedPlayers.Add(mutedPlayer);
            }
        }
        private void MutePlayer(CCSPlayerController player)
        {
            Database db = _plugin.GetDatabase();
            db.MutePlayer(player);
        }
        private void UnmutePlayer(CCSPlayerController player)
        {
            Database db = _plugin.GetDatabase();
            db.UnmutePlayer(player);
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
                var leftMenu = MenuManagerExtra.CreateMenu("Admin Page 1 of 3", 5);
                var middleMenu = MenuManagerExtra.CreateMenu("Admin Page 2 of 3", 5);
                var rightMenu = MenuManagerExtra.CreateMenu("Admin Page 3 of 3", 5);
                leftMenu.Category = "Admin";
                middleMenu.Category = "Admin";
                rightMenu.Category = "Admin";
                // List of menus for navigation
                List<Menu.Menu> menus = new() { leftMenu, middleMenu, rightMenu };

                // Left Menu Options
                leftMenu.Add("Spawn Dummy", null, (pl, opt) =>
                {
                    DummyBotManager.SpawnOrResetDummy(pl);
                    player.PrintToChat($" {ChatColors.Red}{player.PlayerName}[ADMIN] {ChatColors.Default} Has spawned a dummy");
                });
                leftMenu.Add("Freeze Bots", null, (pl, opt) =>
                {
                    pl.ExecuteClientCommandFromServer($"css_freeze @bots");
                    foreach (var plyr in Players)
                    {
                        plyr.PrintToChat($" {ChatColors.Red}{player.PlayerName}[ADMIN] {ChatColors.Default} Has frozen all bots");
                    }

                });
                leftMenu.Add("Unfreeze Bots", null, (pl, opt) =>
                {
                    pl.ExecuteClientCommandFromServer($"css_unfreeze @bots");
                    foreach (var plyr in Players)
                    {
                        plyr.PrintToChat($" {ChatColors.Red}{player.PlayerName}[ADMIN] {ChatColors.Default} Has unfrozen all bots");
                    }

                });


                // Middle Menu Options
                middleMenu.Add("message to a player", null, (pl, opt) =>
                {
                    var messageSubMenu = MenuManager.CreateMenu("choose a player to message", 5);
                    foreach (var targetPlayer in Players)
                    {
                        messageSubMenu.Add(targetPlayer.PlayerName, null, (selectedAdmin, opt2) =>
                        {
                            RequestInput(player, (mess) =>
                            {
                                targetPlayer.PrintToChat($" {ChatColors.Default}Message from {ChatColors.Red}{player.PlayerName}[ADMIN]{ChatColors.Default} {mess}");
                            });

                        });
                    }

                    MenuManagerExtra.OpenMainMenuExtra(pl, new List<Menu.Menu> { messageSubMenu });
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
                            targetPlayer.PrintToChat($" {ChatColors.Default}Killed by {ChatColors.Red}[ADMIN]{player.PlayerName}");
                        });
                    }
                    // Add the submenu to the existing list of menus for the player
                    MenuManagerExtra.OpenMainMenuExtra(pl, new List<Menu.Menu> { killSubMenu });
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
                                    targetPlayer.PrintToChat($" {ChatColors.Red}{player.PlayerName}[ADMIN]{ChatColors.Default} gave you {num} Xp");
                                }
                            }
                        });
                    });
                    // Add the submenu to the existing list of menus for the player
                    MenuManagerExtra.OpenMainMenuExtra(pl, new List<Menu.Menu> { xpSubMenu });
                });
                rightMenu.Add("Mute Menu", null, (pl, opt) =>
                {
                    var muteSubMenu = MenuManager.CreateMenu("Select a player to mute", 5);
                    LoadMutedPlayers();
                    var mutedPlayers = _mutedPlayers;
                    foreach (var targetPlayer in Players)
                    {
                        if (!mutedPlayers.Contains(targetPlayer.SteamID))
                        {
                            muteSubMenu.Add(targetPlayer.PlayerName, null, (pl, opt) =>
                            {
                                MutePlayer(targetPlayer);
                                LoadMutedPlayers();
                            });
                        }
                    }
                    var unmuteSubMenu = MenuManager.CreateMenu("Select a player to umute", 5);
                    foreach (var mutedPlayer in _mutedPlayers)
                    {
                        muteSubMenu.Add(mutedPlayer.ToString(), null, (pl, opt) =>
                        {
                            foreach (var player in Players)
                            {
                                if (player.SteamID == mutedPlayer)
                                {
                                    muteSubMenu.Add(player.PlayerName, null, (pl, opt) =>
                                    {
                                        UnmutePlayer(player);
                                        LoadMutedPlayers();
                                    });
                                }
                            }

                        });
                        MenuManagerExtra.OpenMainMenuExtra(pl, new List<Menu.Menu> { muteSubMenu });
                    }
                });

                // Open the multi-column menu for the player
                MenuManagerExtra.OpenMainMenuExtra(player, menus); // This will open the left, middle, and right men


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

        /*
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
        } */
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

    public static class DummyBotManager
    {
        private static readonly Dictionary<int, CCSPlayerController> DummyTracking = new();

        public static Dictionary<int, CCSPlayerController> GetAllTrackedDummies()
        {
            return DummyTracking;
        }

        public static void SpawnOrResetDummy(CCSPlayerController owner)
        {
            if (owner == null || !owner.IsValid || !owner.IsAlive() || owner.PlayerPawn?.Value == null)
            {
                Console.WriteLine("[Dummy] Command issued by invalid or dead player.");
                return;
            }

            var enemyTeam = owner.TeamNum == (byte)CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

            var dummy = Utilities.GetPlayers()
                .FirstOrDefault(p => p.IsBot && p.IsValid && p.TeamNum == (byte)enemyTeam && p.PlayerPawn?.Value != null && p.IsAlive());

            if (dummy == null)
            {
                owner.PrintToChat(" \x07[Dummy] No valid bot found on the enemy team.");
                return;
            }

            // Track this dummy
            DummyTracking[owner.Slot] = dummy;

            // Position in front of player
            var forward = owner.PlayerPawn.Value.EyeAngles.ToForward();
            var spawnPos = owner.EyePosition() + forward * 100;
            dummy.PlayerPawn.Value.Teleport(spawnPos, new QAngle(), new CSVector());

            // Give health
            BonusHealth(dummy, 9999);

            // Strip weapons
            foreach (var weapon in dummy.PlayerPawn.Value.WeaponServices.MyWeapons)
            {
                if (weapon.IsValid)
                {
                    weapon.Value.Remove();
                }
            }

            // Optional cosmetic
            dummy.PlayerPawn.Value.SetColor(Color.Gray);
            owner.PrintToChat(" \x04[Dummy] Dummy bot has been moved in front of you for testing.");
        }




        public static void BonusHealth(CCSPlayerController dummy, int amount)
        {
            Console.WriteLine($"[DEBUG] Giving bonus health to: {dummy.PlayerName}");

            var healthEffect = new SetBonusHealth(dummy, amount);
            healthEffect.Start();
        }


        public static void MonitorDummyHealth()
        {
            foreach (var entry in DummyTracking)
            {
                var dummy = entry.Value;
                if (dummy == null || !dummy.IsValid || dummy.PlayerPawn?.Value == null)
                    continue;

                var hp = dummy.PlayerPawn.Value.Health;
                if (hp <= 100)
                {
                    int newHealth = dummy.Health + 5000;
                    dummy.SetHp(newHealth);
                    dummy.PrintToChat(" \x07[Dummy] You cannot die. Testing mode active.");
                    var tester = Utilities.GetPlayerFromSlot(entry.Key);
                    tester?.PrintToChat(" \x06[Dummy] Your test dummy was low hp and got healed.");
                }
            }
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


        internal static void OpenMainMenuExtra(CCSPlayerController player, List<Menu> menus, int selectedOptionIndex = 0)
        {

            if (player == null || menus == null || menus.Count == 0)
                return;

            if (!PlayerMenus.ContainsKey(player.Slot))
                PlayerMenus[player.Slot] = new List<Menu>();


            PlayerMenus[player.Slot] = menus;

            if (!PlayerMenuColumn.ContainsKey(player.Slot))
                PlayerMenuColumn[player.Slot] = 0;

            int column = PlayerMenuColumn[player.Slot];
            column = Math.Clamp(column, 0, menus.Count - 1);
            PlayerMenuColumn[player.Slot] = column;

            MenuAPI.Players[player.Slot].OpenMainMenu(menus[0], selectedOptionIndex);
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


            if (player == null || !PlayerMenus.ContainsKey(player.Slot))
                return;

            List<Menu> allMenus = PlayerMenus[player.Slot];
            if (allMenus.Count < 2)
                return;

            if (!PlayerMenuColumn.ContainsKey(player.Slot))
                PlayerMenuColumn[player.Slot] = 0;

            int currentColumn = PlayerMenuColumn[player.Slot];
            Menu currentMenu = MenuAPI.Players[player.Slot].MainMenu;

            // Filter menus: Only switch within the same category
            List<Menu> filteredMenus = allMenus.Where(m => m.Category == currentMenu.Category).ToList();

            if (filteredMenus.Count < 2)
                return;

            int index = filteredMenus.IndexOf(currentMenu);
            if (index == -1)
                return;

            int newIndex = moveRight ? index + 1 : index - 1;
            if (newIndex < 0 || newIndex >= filteredMenus.Count)
                return;

            PlayerMenuColumn[player.Slot] = newIndex;
            MenuAPI.Players[player.Slot].OpenMainMenu(filteredMenus[newIndex]);

        }
    }
}