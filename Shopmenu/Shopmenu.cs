using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
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

    // === Example Functional Item ===
    public class ShopItem1 : IShopItem
    {
        public string Name => "Speed Boots";
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

    // === Health Boost (persistent) ===
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
                Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");

                player.PrintToChat($" {ChatColors.Green}+50 HP applied!");
                return true;
            }
            return false;
        }

        public void ResetEffect(CCSPlayerController player) { }
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

                var health = player.PlayerPawn.Value.Health;
                if (health < 200)
                {
                    player.PlayerPawn.Value.Health = Math.Min(health + 2, 200);
                    Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");

                }

                // Re-schedule the timer
                regenTimers[player] = WarcraftPlugin.Instance.AddTimer(1.0f, RepeatRegen);
            }

            regenTimers[player] = WarcraftPlugin.Instance.AddTimer(1.0f, RepeatRegen);
            player.PrintToChat($" {ChatColors.Green}✔ You feel rejuvenated... (+2 HP/sec up to 200)");
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



    // === Placeholder Stubs ===
    public class ShopItem4 : IShopItem { public string Name => "Placeholder 4"; public int Cost => 3100; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem5 : IShopItem { public string Name => "Placeholder 5"; public int Cost => 900; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem6 : IShopItem { public string Name => "Placeholder 6"; public int Cost => 1600; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem7 : IShopItem { public string Name => "Placeholder 7"; public int Cost => 2400; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem8 : IShopItem { public string Name => "Placeholder 8"; public int Cost => 3300; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem9 : IShopItem { public string Name => "Placeholder 9"; public int Cost => 1000; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem10 : IShopItem { public string Name => "Placeholder 10"; public int Cost => 1800; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem11 : IShopItem { public string Name => "Placeholder 11"; public int Cost => 2600; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem12 : IShopItem { public string Name => "Placeholder 12"; public int Cost => 3500; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem13 : IShopItem { public string Name => "Placeholder 13"; public int Cost => 1100; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem15 : IShopItem { public string Name => "Placeholder 15"; public int Cost => 2800; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
    public class ShopItem16 : IShopItem { public string Name => "Placeholder 16"; public int Cost => 3900; public bool IsPersistent => false; public bool Apply(CCSPlayerController player) => true; public void ResetEffect(CCSPlayerController player) { } }
}
