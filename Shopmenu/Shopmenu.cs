using System;
using System.Collections.Generic;
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
                var menu = MenuManagerExtra.CreateMenu($"Shop Page {i + 1}/4", 5);
                menu.Category = "Shop";

                for (int j = 1; j <= 4; j++)
                {
                    int itemIndex = i * 4 + j;
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

    public class ShopItem1 : IShopItem
    {
        public string Name => "Boots of speed";
        public int Cost => 1600;
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

            var race = wcPlayer.GetClass().InternalName;
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
        public string Name => "Ring of Regeneration";
        public int Cost => 2500;
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
        public int Cost => 3000;
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
        public string Name => "Grand Tome of Experience";
        public int Cost => 5000;
        public bool IsPersistent => true; // XP is permanent
        private const int xpToGive = 300;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            plugin.XpSystem.AddXp(player, xpToGive);
            player.PrintToChat($"{ChatColors.Green}✔ You gained {xpToGive} XP from the Grand Tome of Experience!");

            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // Nothing to reset — XP gain is permanent
        }
    }

    public class ShopItem5 : IShopItem
    {
        public string Name => "Massive Tome of Experience";
        public int Cost => 10000;
        public bool IsPersistent => true;
        private const int xpToGive = 600;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            plugin.XpSystem.AddXp(player, xpToGive);
            player.PrintToChat($" {ChatColors.Green}✔ You gained {xpToGive} XP from the Grand Tome of Experience!");

            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // Nothing to reset — XP gain is permanent
        }
    }

    public class ShopItem6 : IShopItem
    {
        public string Name => "Gambling Tome of Experience";
        public int Cost => 1; // SET TO 10.000
        public bool IsPersistent => true;

        private const int xpToGiveMin = 100;
        private const int xpToGiveMax = 900;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            var random = new Random();
            int xpToGive = random.Next(xpToGiveMin, xpToGiveMax + 1);

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
            }

            plugin.XpSystem.AddXp(player, xpToGive);

            player.PrintToChat($" {ChatColors.Green}🎲 You gained {xpToGive} XP from the Gambling Tome of Experience!");
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
        public string Name => "Tome of Experience";
        public int Cost => 1000;
        public bool IsPersistent => true;
        private const int xpToGive = 50;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            plugin.XpSystem.AddXp(player, xpToGive);
            player.PrintToChat($" {ChatColors.Green}✔ You gained {xpToGive} XP from the Grand Tome of Experience!");

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
        public int Cost => 1000;
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

            Invisibility(player, 999f, 175);
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
        public string Name => "Armor piercing rounds";
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
        public int Cost => 1100;
        public bool IsPersistent => false;

        private readonly string ctModel = "characters/models/ctm_heavy/ctm_heavy.vmdl";
        private readonly string tModel = "characters/models/tm_phoenix_heavy/tm_phoenix_heavy.vmdl";

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
                player.PrintToChat($"{ChatColors.Red}✖ Could not apply disguise.");
                return false;
            }

            player.PlayerPawn.Value.SetModel(modelToApply);
            player.PrintToChat($"{ChatColors.Green}✔ You are now disguised as the enemy!");

            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // Let CS2 reset model on death or round start naturally
        }
    }


    public class ShopItem14 : IShopItem
    {
        public string Name => "Vitality Boost";
        public int Cost => 2000;
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
        public int Cost => 10;
        public bool IsPersistent => true;

        private const int xpToGive = 200;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            if (plugin == null) return false;

            var teammates = Utilities.GetPlayers()
                .Where(p => p.IsValid && p != player && p.TeamNum == player.TeamNum && p.IsBot == false)
                .ToList();

            if (teammates.Count == 0)
            {
                player.PrintToChat($"{ChatColors.Red}✖ No teammates found to gift XP to.");
                return false;
            }

            var random = new Random();
            var chosenTeammate = teammates[random.Next(teammates.Count)];

            plugin.XpSystem.AddXp(chosenTeammate, xpToGive);
            player.PrintToChat($"{ChatColors.Green}✔ You gifted {xpToGive} XP to {chosenTeammate.PlayerName}!");
            chosenTeammate.PrintToChat($"{ChatColors.Gold}✨ {player.PlayerName} has gifted you {xpToGive} XP!");

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
        public int Cost => 3900;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            if (player == null || player.PlayerPawn?.Value == null || player.IsAlive())
            {
                player.PrintToChat($"{ChatColors.Red}✖ You must be dead to use the Scroll of Resurrection!");
                return false;
            }

            // Get a random living ally
            var allies = Utilities.GetPlayers()
                .Where(p => p != player && p.TeamNum == player.TeamNum && p.IsAlive())
                .ToList();

            if (allies.Count == 0)
            {
                player.PrintToChat($"{ChatColors.Red}✖ No living teammates to anchor your resurrection.");
                return false;
            }

            var random = new Random();
            var anchor = allies[random.Next(allies.Count)];
            var resurrectionPosition = anchor.PlayerPawn.Value.AbsOrigin;

            player.PrintToChat($"{ChatColors.Gold}⏳ Channeling resurrection... You will respawn in 3 seconds!");

            WarcraftPlugin.Instance.AddTimer(3.0f, () =>
            {
                if (!player.IsValid || player.IsAlive()) return;

                player.Respawn();
                player.PlayerPawn.Value.Teleport(resurrectionPosition);
                player.PrintToChat($"{ChatColors.Green}✔ You have been resurrected at {anchor.PlayerName}'s former position!");
            });

            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }
}
