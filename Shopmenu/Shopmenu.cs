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


namespace WarcraftPlugin.Core
{
    #region
    public class ShopMenu
    {
        private readonly WarcraftPlugin _plugin;
        private static readonly Dictionary<CCSPlayerController, HashSet<string>> purchasesThisRound = new();
        private static readonly Dictionary<CCSPlayerController, List<IShopItem>> roundBoundItems = new();

        public ShopMenu(WarcraftPlugin plugin)
        {
            _plugin = plugin;
            _plugin.AddCommandListener("say", OnPlayerChat);

            _plugin.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                foreach (var (player, items) in roundBoundItems)
                {
                    foreach (var item in items)
                    {
                        item.ResetEffect(player);
                    }
                }

                purchasesThisRound.Clear();
                roundBoundItems.Clear();

                return HookResult.Continue;
            });

        }

        public HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
        {
            var message = info.GetArg(1).ToLower();
            if (player == null) return HookResult.Continue;

            if (message == "shop" || message == "shopmenu")
            {
                OpenShopMenu(player);
                return HookResult.Handled;
            }

            return HookResult.Continue;
        }

        private void OpenShopMenu(CCSPlayerController player)
        {
            List<Menu.Menu> pages = new();

            for (int i = 0; i < 4; i++)
            {
                var menu = MenuManagerExtra.CreateMenu($"Shop Page {i + 1}/4", 6);
                menu.Category = "Shop";

                for (int j = 1; j <= 5; j++)
                {
                    int itemIndex = i * 5 + j;
                    var item = GetShopItem(itemIndex);
                    string itemName = $"{item.Name} - ${item.Cost}";

                    menu.Add(itemName, null, (pl, opt) =>
                    {
                        var moneyService = pl.InGameMoneyServices;
                        if (moneyService == null) return;

                        int currentMoney = moneyService.Account;

                        // Check if already purchased this item
                        if (!purchasesThisRound.TryGetValue(pl, out var boughtItems))
                        {
                            boughtItems = new HashSet<string>();
                            purchasesThisRound[pl] = boughtItems;
                        }

                        if (boughtItems.Contains(item.Name))
                        {
                            pl.PrintToChat($" {ChatColors.Red}✖ You already bought {item.Name} this round.");
                            return;
                        }

                        if (currentMoney < item.Cost)
                        {
                            pl.PrintToChat($" {ChatColors.Red}✖ You can't afford {item.Name}. It costs ${item.Cost}.");
                            return;
                        }

                        bool success = item.Apply(pl);
                        if (!success)
                        {
                            // Block purchase if Apply failed (e.g., due to race restriction)
                            return;
                        }

                        // Deduct money & confirm purchase
                        moneyService.Account = Math.Max(0, currentMoney - item.Cost);
                        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
                        player.PlayLocalSound("sounds/common/talk.vsnd");



                        // ____________________________
                        Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");
                        //------------------------------

                        pl.PrintToChat($" {ChatColors.Green}✔ You bought {item.Name} for ${item.Cost}!");
                        boughtItems.Add(item.Name);

                        if (!item.IsPersistent)
                        {
                            if (!roundBoundItems.ContainsKey(pl))
                                roundBoundItems[pl] = new List<IShopItem>();
                            roundBoundItems[pl].Add(item);
                        }
                    });
                }

                pages.Add(menu);
            }

            MenuManagerExtra.OpenMainMenuExtra(player, pages);
        }

        private IShopItem GetShopItem(int index)
        {
            return index switch
            {
                1 => new ShopItem1(),
                2 => new ShopItem2(),
                3 => new ShopItem3(),
                4 => new ShopItem4(),
                5 => new ShopItem5(),
                6 => new ShopItem6(),
                7 => new ShopItem7(),
                8 => new ShopItem8(),
                9 => new ShopItem9(),
                10 => new ShopItem10(),
                11 => new ShopItem11(),
                12 => new ShopItem12(),
                13 => new ShopItem13(),
                14 => new ShopItem14(),
                15 => new ShopItem15(),
                16 => new ShopItem16(),
                17 => new ShopItem17(),
                18 => new ShopItem18(),
                19 => new ShopItem19(),
                20 => new ShopItem20(),
                _ => new ShopItem1()
            };

        }
    }

    public interface IShopItem
    {
        string Name { get; }
        int Cost { get; }
        bool IsPersistent { get; }
        bool Apply(CCSPlayerController player);
        void ResetEffect(CCSPlayerController player);
    }
    #endregion

    #region Shopmenu item classes
    public class ShopItem1 : IShopItem
    {
        public string Name => "Boots of speed";
        public int Cost => 2600;
        public bool IsPersistent => false;

        private readonly HashSet<string> restrictedRaces = new()
        {
            "undead_scourge",
            "laser_light_show"
        };

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            var playerClass = wcPlayer.GetClass();
            if (playerClass == null) return false;

            var race = playerClass.InternalName;

            if (restrictedRaces.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) already has movement buffs.");
                return false;
            }

            player.PlayerPawn.Value.VelocityModifier += 0.25f;
            player.PrintToChat($" {ChatColors.Green}✔ Speed Boots equipped! (+25% movement speed)");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            if (player.IsValid && player.PlayerPawn?.Value != null)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
            }
        }
    }
    public class ShopItem2 : IShopItem
    {
        public string Name => "Ring of Regen";
        public int Cost => 3500;
        public bool IsPersistent => false; // so it cleans on round end

        private readonly Dictionary<CCSPlayerController, Timer> regenTimers = new();

        public bool Apply(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value == null || !player.IsValid) return false;

            void RepeatRegen()
            {
                if (!player.IsValid || !player.IsAlive() || player.PlayerPawn?.Value == null) return;

                int currentHp = player.PlayerPawn.Value.Health;
                if (currentHp < 200)
                {
                    // Heal for 2
                    player.PlayerPawn.Value.Health = Math.Min(currentHp + 2, 200);
                    Server.NextFrame(() => Utilities.SetStateChanged(player.PlayerPawn.Value!, "CBaseEntity", "m_iHealth"));

                }

                // Reschedule the timer
                regenTimers[player] = WarcraftPlugin.Instance.AddTimer(1.0f, RepeatRegen);
            }

            regenTimers[player] = WarcraftPlugin.Instance.AddTimer(1.0f, RepeatRegen);
            player.PrintToChat($"{ChatColors.Green}✔ Regeneration active! (+2 HP/sec)");
            return true;
        }



        public void ResetEffect(CCSPlayerController player)
        {
            if (regenTimers.TryGetValue(player, out var timer))
            {
                timer.Kill();
                regenTimers.Remove(player);
                player.PrintToChat($" {ChatColors.Red}✖ Ring of Regeneration faded away.");
            }
        }
    }
    public class ShopItem3 : IShopItem
    {
        public string Name => "Necklace of Immunity";
        public int Cost => 2500;
        public bool IsPersistent => false;

        private readonly HashSet<string> restrictedRaces = new()
        {
            "archmage_proudmoore",
            "crypt_lord"
        };

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            var race = wcPlayer.GetClass().InternalName;

            if (restrictedRaces.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) is restricted from buying this item.");
                return false;
            }

            wcPlayer.HasUltimateImmunity = true;
            player.PrintToChat($" {ChatColors.Green}✔ You are now immune to ultimates this round.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return;

            wcPlayer.HasUltimateImmunity = false;
            player.PrintToChat($" {ChatColors.Red}✖ Your ultimate immunity has worn off.");
        }
    }
    public class ShopItem4 : IShopItem
    {
        public string Name => "Grand Exp Tome";
        public int Cost => 5000;
        public bool IsPersistent => true; // XP is permanent
        private const int xpToGive = 300;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            plugin.XpSystem.AddXp(player, xpToGive);

            var wcPlayer = plugin.GetWcPlayer(player);
            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            player.PrintToChat($"{ChatColors.Green}✔ You gained {xpToGive} XP from the Grand Tome of Experience!");
            player.PrintToChat($"{ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");


            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // Nothing to reset — XP gain is permanent
        }
    }
    public class ShopItem5 : IShopItem
    {
        public string Name => "Massive Exp Tome";
        public int Cost => 10000;
        public bool IsPersistent => true;
        private const int xpToGive = 600;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            plugin.XpSystem.AddXp(player, xpToGive);

            var wcPlayer = plugin.GetWcPlayer(player);
            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            player.PrintToChat($"{ChatColors.Green}✔ You gained {xpToGive} XP from the Grand Tome of Experience!");
            player.PrintToChat($"{ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");


            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // Nothing to reset — XP gain is permanent
        }
    }
    public class ShopItem6 : IShopItem
    {
        public string Name => "Gambling Exp Tome";
        public int Cost => 10000;
        public bool IsPersistent => true;

        private const int xpToGiveMin = 100;
        private const int xpToGiveMax = 900;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            var random = new Random();
            int xpToGive = random.Next(xpToGiveMin, xpToGiveMax + 1);
            var wcPlayer = plugin.GetWcPlayer(player);
            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            // Roll for GOLD bonus
            int roll = random.Next(1, 431); // 1 in 430 chance
            bool isGold = roll == 1;

            if (isGold)
            {
                xpToGive += 1000;
                Utilities.GetPlayers().ForEach(p =>
                {
                    p.PrintToChat($" {ChatColors.Gold}✨ {player.PlayerName} rolled a GOLD CASE in the XP shop and gained +1000 bonus XP! ✨");

                });
                player.PrintToChat($"{ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");
            }

            plugin.XpSystem.AddXp(player, xpToGive);

            player.PrintToChat($" {ChatColors.Green}🎲 You gained {xpToGive} XP from the Gambling Tome of Experience!");
            player.PrintToChat($"{ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");
            if (isGold)
            {
                player.PrintToChat($" {ChatColors.Gold}💛 You wasted your knife luck for this round...");
            }

            return true;
        }


        public void ResetEffect(CCSPlayerController player)
        {
            // Nothing to reset — XP gain is permanent
        }
    }
    public class ShopItem7 : IShopItem
    {
        public string Name => "Exp Tome";
        public int Cost => 1000;
        public bool IsPersistent => true;
        private const int xpToGive = 50;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            plugin.XpSystem.AddXp(player, xpToGive);

            var wcPlayer = plugin.GetWcPlayer(player);
            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            player.PrintToChat($"{ChatColors.Green}✔ You gained {xpToGive} XP from the Grand Tome of Experience!");
            player.PrintToChat($"{ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");


            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // Nothing to reset — XP gain is permanent
        }
    }
    public class ShopItem8 : IShopItem
    {
        public string Name => "Feather Boots";
        public int Cost => 3100;
        public bool IsPersistent => false;

        private readonly HashSet<string> restrictedRaces = new()
        {
            "undead_scourge"
        };

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null || player.PlayerPawn?.Value == null) return false;

            // Optional: Restrict by race
            if (restrictedRaces.Contains(wcPlayer.GetClass().InternalName))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot wear Feather Boots.");
                return false;
            }

            // ✅ Your actual effect goes here
            player.PlayerPawn.Value.GravityScale = 0.75f; // Example: reduce gravity for higher jumps

            player.PrintToChat($" {ChatColors.Green}✔ Feather Boots equipped! Gravity reduced.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value != null)
            {
                player.PlayerPawn.Value.GravityScale = 1.0f; // Reset gravity to normal
                player.PrintToChat($" {ChatColors.Default}✖ Feather Boots have worn off.");
            }
        }

    }
    public class ShopItem9 : IShopItem
    {
        public string Name => "Longjump";
        public int Cost => 4000;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null || player.PlayerPawn?.Value == null) return false;

            wcPlayer.HasLongjumpBoots = true;
            player.PrintToChat($"{ChatColors.Green}✔ Longjump Boots equipped. Press jump to leap forward!");

            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return;

            wcPlayer.HasLongjumpBoots = false;
        }
    }
    public class ShopItem10 : IShopItem
    {
        public string Name => "Cloak of invisibility";
        public int Cost => 1800;
        public bool IsPersistent => false;

        private readonly HashSet<string> restrictedRaces = new()
        {
            "human_alliance"
        };

        public static void Invisibility(CCSPlayerController player, float duration, int amount)
        {
            var InvisEffect = new SetInvisibility(player, duration, amount);
            InvisEffect.Start();
        }

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            var race = wcPlayer.GetClass().InternalName;
            if (restrictedRaces.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) already has invisibility buffs.");
                return false;
            }

            Invisibility(player, 999f, 150);
            player.PrintToChat($" {ChatColors.Green}✔ Cloak of Invisibility equipped.");
            return true;
        }


        public void ResetEffect(CCSPlayerController player)
        {
            Invisibility(player, 999f, 255);
        }
    }
    public class ShopItem11 : IShopItem
    {
        public string Name => "Orb of Slow";
        public int Cost => 2800;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            wcPlayer.HasOrbOfSlow = true;
            player.PrintToChat($" {ChatColors.Green}✔ Orb of Slow equipped! You now have a chance to slow enemies on hit.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return;

            wcPlayer.HasOrbOfSlow = false;
        }
    }
    public class ShopItem12 : IShopItem
    {
        public string Name => "FMJ Bullets";
        public int Cost => 2800;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            wcPlayer.HasArmorPiercingRounds = true;
            player.PrintToChat($" {ChatColors.Green}✔ Orb of Slow equipped! You now have a chance to slow enemies on hit.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return;

            wcPlayer.HasArmorPiercingRounds = false;
        }
    }
    public class ShopItem13 : IShopItem
    {
        public string Name => "Disguise";
        public int Cost => 1400;
        public bool IsPersistent => false;

        private readonly string ctModel = "models/player/custom_player/legacy/ctm_fbi.vmdl";
        private readonly string tModel = "models/player/custom_player/legacy/tm_leet.vmdl";


        public bool Apply(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value == null || !player.IsValid) return false;

            var modelToApply = player.TeamNum switch
            {
                2 => ctModel, // Terrorist gets disguised as CT
                3 => tModel,  // CT gets disguised as T
                _ => null
            };

            if (modelToApply == null)
            {
                player.PrintToChat($" {ChatColors.Red}✖ Could not apply disguise.");
                return false;
            }

            player.PlayerPawn.Value.SetModel(modelToApply);
            player.PrintToChat($" {ChatColors.Green}✔ You are now disguised as the enemy!");

            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // Let CS2 reset model on death or round start naturally
        }
    }
    public class ShopItem14 : IShopItem
    {
        public string Name => "Periapt of Health";
        public int Cost => 2400;
        public bool IsPersistent => true;

        private readonly HashSet<string> restrictedRaces = new()
        {
            "human_alliance",
            "laser_light_show"
        };

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            var race = wcPlayer.GetClass().InternalName;
            if (restrictedRaces.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) is restricted from buying this item.");
                return false;
            }

            if (player.PlayerPawn?.Value != null)
            {
                player.PlayerPawn.Value.Health += 50;
                Server.NextFrame(() => Utilities.SetStateChanged(player.PlayerPawn.Value!, "CBaseEntity", "m_iHealth"));
                player.PrintToChat($" {ChatColors.Green}+50 HP applied!");
                return true;
            }
            return false;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }
    public class ShopItem15 : IShopItem
    {
        public string Name => "Gift of Experience";
        public int Cost => 4000;
        public bool IsPersistent => true;

        private const int xpToGive = 300;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            var players = Utilities.GetPlayers()
                .Where(p => p.IsValid && p != player && p.IsBot == false)
                .ToList();

            if (players.Count == 0)
            {
                player.PrintToChat($" {ChatColors.Red}✖ No teammates found to gift XP to.");
                return false;
            }

            var random = new Random();
            var chosenTeammate = players[random.Next(players.Count)];

            plugin.XpSystem.AddXp(chosenTeammate, xpToGive);

            var wcPlayer = plugin.GetWcPlayer(chosenTeammate);
            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            player.PrintToChat($" {ChatColors.Green}✔ You have given {xpToGive} XP to {chosenTeammate.PlayerName} !");
            chosenTeammate.PrintToChat($" {ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");

            chosenTeammate.PrintToChat($" {ChatColors.Gold}✨ {player.PlayerName} has gifted you {xpToGive} XP!");

            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // XP is permanent — no reset needed
        }
    }
    public class ShopItem16 : IShopItem
    {
        public string Name => "Scroll of Resurrection";
        public int Cost => 5000;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            if (player == null || player.PlayerPawn?.Value == null || player.IsAlive())
            {
                player.PrintToChat($" {ChatColors.Red}✖ You must be dead to use the Scroll of Resurrection!");
                return false;
            }

            var allies = Utilities.GetPlayers()
                .Where(p => p != player && p.TeamNum == player.TeamNum && p.IsAlive())
                .ToList();

            if (allies.Count == 0)
            {
                player.PrintToChat($" {ChatColors.Red}✖ No living teammates to anchor your resurrection.");
                return false;
            }

            var random = new Random();
            var anchor = allies[random.Next(allies.Count)];

            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            wcPlayer.RespawnQueued = true;
            wcPlayer.RespawnLocation = anchor.PlayerPawn.Value.AbsOrigin;
            wcPlayer.RespawnTriggerTime = Server.CurrentTime + 3f;

            player.PrintToChat($" {ChatColors.Gold}⏳ Channeling resurrection... You will respawn in 3 seconds!");

            return true;
        }




        public void ResetEffect(CCSPlayerController player) { }
    }
    public class ShopItem17 : IShopItem
    {
        public string Name => "Gloves of Warmth";
        public int Cost => 2800;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            wcPlayer.HasGlovesOfWarmth = true;
            player.GiveNamedItem("weapon_hegrenade");
            player.PrintToChat($"{ChatColors.Green}✔ Gloves of Warmth equipped!");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer != null)
                wcPlayer.HasGlovesOfWarmth = false;
        }
    }
    public class ShopItem18 : IShopItem
    {
        public string Name => "Mask of Death";
        public int Cost => 1900;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            wcPlayer.HasMaskOfDeath = true;
            player.PrintToChat($"{ChatColors.Green}✔ Mask of Death equipped. You may reveal enemies!");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer != null)
                wcPlayer.HasMaskOfDeath = false;
        }
    }
    public class ShopItem19 : IShopItem
    {
        public string Name => "Helm of Excellence";
        public int Cost => 3000;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            wcPlayer.HasHelmOfExcellence = true;
            player.PrintToChat($"{ChatColors.Green}✔ Helm of Excellence equipped. Headshots hurt less!");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer != null)
                wcPlayer.HasHelmOfExcellence = false;
        }
    }
    public class ShopItem20 : IShopItem
    {
        public string Name => "Orb of Reflection";
        public int Cost => 2800;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            wcPlayer.HasOrbOfReflection = true;
            player.PrintToChat($"{ChatColors.Green}✔ Orb of Reflection equipped! Some of the damage you take will be returned.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer != null)
                wcPlayer.HasOrbOfReflection = false;
        }
    }

    #endregion
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///                                                                                                   END OF SHOPMENU CODE , BELOW YOU WILL FIND THE CODE FOR FLAGS ALLOWING GLOBAL BUFFS/ ITEM USAGE                                                                        ///
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public enum HitGroup
    {
        Generic = 0,
        Head = 1,
        Chest = 2,
        Stomach = 3,
        LeftArm = 4,
        RightArm = 5,
        LeftLeg = 6,
        RightLeg = 7,
        Gear = 10
    }

    #region Global Buffs related to shopmenu
    public class GlobalBuffs
    {
        private readonly WarcraftPlugin _plugin;

        public GlobalBuffs(WarcraftPlugin plugin)
        {
            _plugin = plugin;

            // Hook global events
            _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            _plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            _plugin.RegisterEventHandler<EventPlayerJump>(OnPlayerJump);
            _plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            _plugin.RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
            _plugin.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
            _plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            _plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        }

        // 🧠 SECTION 1: Manual Global Buffs

        private HookResult OnSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!player.IsValid || !player.IsAlive() || player.PlayerPawn?.Value == null)
                return HookResult.Continue;

            var wcPlayer = _plugin.GetWcPlayer(player);
            if (wcPlayer == null) return HookResult.Continue;

            if (wcPlayer.RespawnQueued)
            {
                wcPlayer.RespawnQueued = false;

                var location = wcPlayer.RespawnLocation;
                _plugin.AddTimer(0.2f, () =>
                {
                    if (player.IsValid && player.PlayerPawn?.Value != null)
                    {
                        player.PlayerPawn.Value.Teleport(location);
                        player.PrintToChat($" {ChatColors.Green}✔ You have been resurrected at your ally's location!");
                    }
                });
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var victim = @event.Userid;
            if (!victim.IsValid || victim.PlayerPawn?.Value == null)
                return HookResult.Continue;

            var wcVictim = _plugin.GetWcPlayer(victim);
            if (wcVictim == null) return HookResult.Continue;

            // 🧠 Reset player-specific buffs
            wcVictim.HasOrbOfSlow = false;
            wcVictim.HasArmorPiercingRounds = false;
            wcVictim.HasMaskOfDeath = false;
            wcVictim.HasHelmOfExcellence = false;
            wcVictim.HasGlovesOfWarmth = false;
            wcVictim.HasLongjumpBoots = false;
            wcVictim.HasOrbOfReflection = false;
            wcVictim.HasDamageReflection = false;
            wcVictim.ChameleonOffensive = false;
            wcVictim.ChameleonDefensive = false;
            wcVictim.HasUltimateImmunity = false;
            wcVictim.RespawnQueued = false;

            // 🧠 Track death in stats
            WarcraftPlugin.Instance.GetDatabase().RegisterDeath(victim, wcVictim.className);

            // 🧠 Track kill (if valid attacker)
            var attacker = @event.Attacker;
            if (attacker != null && attacker.IsValid && attacker != victim)
            {
                var wcAttacker = _plugin.GetWcPlayer(attacker);
                if (wcAttacker != null)
                {
                    WarcraftPlugin.Instance.GetDatabase().RegisterKill(attacker, wcAttacker.className);
                }
            }

            return HookResult.Continue;
        }



        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || player.PlayerPawn?.Value == null)
                    continue;

                player.PlayerPawn.Value.Health += 50;
            }

            _plugin.AddTimer(1.0f, RepeatRespawnCheck, TimerFlags.REPEAT);


            return HookResult.Continue;
        }
        private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!player.IsValid || player.PlayerPawn?.Value == null || !player.IsAlive())
                return HookResult.Continue;

            var wcPlayer = _plugin.GetWcPlayer(player);
            if (wcPlayer != null && wcPlayer.HasGlovesOfWarmth)
            {
                Server.NextFrame(() =>
                {
                    _plugin.AddTimer(5f, () =>
                    {
                        if (!player.IsValid || player.PlayerPawn?.Value == null || !player.IsAlive())
                            return;

                        string[] grenades = {
                            "weapon_hegrenade",
                            "weapon_flashbang",
                            "weapon_incgrenade",
                            "weapon_decoy"
                        };

                        string randomGrenade = grenades[Random.Shared.Next(grenades.Length)];
                        player.GiveNamedItem(randomGrenade);

                        player.PrintToChat($" {ChatColors.Green}You received a random grenade: {randomGrenade.Replace("weapon_", "").ToUpper()}!");
                    });
                });
            }

            return HookResult.Continue;
        }


        private void RepeatRespawnCheck()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var wcPlayer = _plugin.GetWcPlayer(player);
                if (wcPlayer == null || !wcPlayer.RespawnQueued) continue;

                if (Server.CurrentTime >= wcPlayer.RespawnTriggerTime)
                {
                    wcPlayer.RespawnQueued = false;

                    if (!player.IsValid || player.IsAlive()) continue;

                    player.Respawn();
                    var targetLocation = wcPlayer.RespawnLocation;
                    _plugin.AddTimer(0.2f, () =>
                    {
                        if (player.IsValid && player.PlayerPawn?.Value != null)
                        {
                            player.PlayerPawn.Value.Teleport(targetLocation);
                            player.PrintToChat($"{ChatColors.Green}✔ You have been resurrected at your ally’s location!");
                        }
                    });
                }
            }
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || player.PlayerPawn?.Value == null)
                    continue;

                var wcPlayer = _plugin.GetWcPlayer(player);
                if (wcPlayer == null) continue;

                // Safety reset: remove ultimate immunity
                wcPlayer.HasOrbOfSlow = false;
                wcPlayer.HasArmorPiercingRounds = false;
                wcPlayer.HasMaskOfDeath = false;
                wcPlayer.HasHelmOfExcellence = false;
                wcPlayer.HasGlovesOfWarmth = false;
                wcPlayer.HasLongjumpBoots = false;
                wcPlayer.HasOrbOfReflection = false;
                wcPlayer.HasDamageReflection = false;
                wcPlayer.ChameleonOffensive = false;
                wcPlayer.ChameleonDefensive = false;
                wcPlayer.HasUltimateImmunity = false;

            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var disconPlayer = @event.Userid;
            if (!disconPlayer.IsValid || disconPlayer.PlayerPawn?.Value == null)
                return HookResult.Continue;

            var wcDcPlayer = _plugin.GetWcPlayer(disconPlayer);
            if (wcDcPlayer == null) return HookResult.Continue;

            // Reset player-specific buffs
            wcDcPlayer.HasOrbOfSlow = false;
            wcDcPlayer.HasArmorPiercingRounds = false;
            wcDcPlayer.HasMaskOfDeath = false;
            wcDcPlayer.HasHelmOfExcellence = false;
            wcDcPlayer.HasGlovesOfWarmth = false;
            wcDcPlayer.HasLongjumpBoots = false;
            wcDcPlayer.HasOrbOfReflection = false;
            wcDcPlayer.HasDamageReflection = false;
            wcDcPlayer.ChameleonOffensive = false;
            wcDcPlayer.ChameleonDefensive = false;
            wcDcPlayer.HasUltimateImmunity = false;

            return HookResult.Continue;
        }



        ///

        // SECTION 2: Shop & Debuff Effects
        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || attacker == victim || !attacker.IsValid || !victim.IsValid)
                return HookResult.Continue;

            if (attacker.TeamNum == victim.TeamNum)
                return HookResult.Continue;

            var wcAttacker = WarcraftPlugin.Instance.GetWcPlayer(attacker);
            var wcVictim = WarcraftPlugin.Instance.GetWcPlayer(victim);

            if (wcAttacker == null) return HookResult.Continue;

            if (wcAttacker.HasOrbOfSlow)
            {
                SkillFunctions.SlowTarget(attacker, victim, 25, 3f); // 25% chance to slow for 3s
            }


            if (wcAttacker.HasArmorPiercingRounds)
            {
                SkillFunctions.DealRawDamage(attacker, victim, 5);
                attacker.PrintToCenter("You dealt 5 additional damage with each hit");
            }

            if (wcAttacker.HasMaskOfDeath && Random.Shared.Next(100) < 20)
            {
                if (wcVictim != null)
                {
                    wcVictim.HasUltimateImmunity = false;
                    victim.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255));
                    victim.PrintToChat($" {ChatColors.Red}✖ Your invisibility and immunity were stripped!");
                }
            }

            if (wcVictim != null && wcVictim.HasHelmOfExcellence && @event.Hitgroup == (int)HitGroup.Head)
            {
                int DmgDealt = @event.DmgHealth;
                int DmgReduction = (int)(DmgDealt * 0.65f);
                SkillFunctions.SetBonusHealth(victim, DmgReduction);
                victim.PrintToCenter($" {ChatColors.Green}🛡️ Helm of Excellence absorbed some of the damage!");
                if (victim != null && victim.IsValid && victim.PlayerPawn != null && victim.PlayerPawn.IsValid)
                {
                    {
                        try
                        {
                            if (victim != null && victim.PlayerPawn != null && victim.PlayerPawn.IsValid)
                            {
                                Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Exception in HelmOfExcellence NextFrame: {ex}");
                        }
                    }
                }

            }

            if (wcVictim != null && attacker != null && wcVictim.HasOrbOfReflection && attacker.IsValid && attacker.IsAlive())
            {
                float now = Server.CurrentTime;
                if (now - wcVictim.LastReflectionTime > 1.0f)
                {
                    wcVictim.LastReflectionTime = now;

                    int reflected = (int)(@event.DmgHealth * 0.25f);
                    if (reflected > 0)
                    {
                        SkillFunctions.DealRawDamage(victim, attacker, reflected);
                        attacker.PrintToChat($" {ChatColors.Red}⚡ You were struck by reflected damage!");
                        victim.PrintToChat($" {ChatColors.Green}✔ Orb of Reflection struck your attacker for {reflected} damage!");
                    }
                }
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            return HookResult.Continue;



        }

        private HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player?.PlayerPawn?.Value == null || !player.IsValid) return HookResult.Continue;

            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null || !wcPlayer.HasLongjumpBoots) return HookResult.Continue;

            WarcraftPlugin.Instance.AddTimer(0.05f, () =>
            {
                var directionAngle = player.PlayerPawn.Value.EyeAngles;
                var directionVec = new Vector();
                NativeAPI.AngleVectors(directionAngle.Handle, directionVec.Handle, nint.Zero, nint.Zero);

                if (directionVec.Z < 0.55f)
                    directionVec.Z = 0.55f;



                directionVec *= 520;
                player.PlayerPawn.Value.AbsVelocity.X = directionVec.X;
                player.PlayerPawn.Value.AbsVelocity.Y = directionVec.Y;
                player.PlayerPawn.Value.AbsVelocity.Z = directionVec.Z;
            });

            WarcraftPlugin.Instance.AddTimer(0.05f, () =>
            {
                new SetGravityEffect(player, 70f, 5f).Start();
            });

            return HookResult.Continue;
        }
    }
}
#endregion