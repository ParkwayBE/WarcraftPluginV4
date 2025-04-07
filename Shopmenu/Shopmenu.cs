using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Menu;

namespace WarcraftPlugin.Core
{
    public class ShopMenu
    {
        private readonly WarcraftPlugin _plugin;
        private static Dictionary<CCSPlayerController, Action<string>> pendingInputs = new();
        private static Random rng = new();

        public ShopMenu(WarcraftPlugin plugin)
        {
            _plugin = plugin;
            _plugin.AddCommandListener("say", OnPlayerChat);
        }

        public HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
        {
            var message = info.GetArg(1).ToLower();
            if (player == null) return HookResult.Continue;

            if (pendingInputs.TryGetValue(player, out var action))
            {
                action.Invoke(message);
                pendingInputs.Remove(player);
                return HookResult.Handled;
            }

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

            // Set static item costs (Placeholder 1 - 16)
            int[] itemCosts = new int[]
            {
        800, 1300, 2200, 3100,
        900, 1600, 2400, 3300,
        1000, 1800, 2600, 3500,
        1100, 2000, 2800, 3900
            };

            for (int i = 0; i < 4; i++)
            {
                var menu = MenuManagerExtra.CreateMenu($"Shop Page {i + 1}/4", 5);
                menu.Category = "Shop";

                for (int j = 1; j <= 4; j++)
                {
                    int itemIndex = i * 4 + j;
                    int cost = itemCosts[itemIndex - 1];
                    string itemName = $"Placeholder {itemIndex} - ${cost}";

                    menu.Add(itemName, null, (pl, opt) =>
                    {
                        var moneyService = pl.InGameMoneyServices;
                        if (moneyService == null) return;

                        int currentMoney = moneyService.Account;

                        if (currentMoney < cost)
                        {
                            pl.PrintToChat($" {ChatColors.Red}✖ You can't afford this item. It costs ${cost}.");
                        }
                        else
                        {
                            moneyService.Account = Math.Max(0, currentMoney - cost);
                            pl.PrintToChat($" {ChatColors.Green}✔ You bought {itemName} for ${cost}!");
                        }
                    });
                }

                pages.Add(menu);
            }

            MenuManagerExtra.OpenMainMenuExtra(player, pages);
        }

    }

    // === Placeholder Item Classes (Future logic per item goes here) ===
    public class ShopItem1 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem2 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem3 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem4 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem5 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem6 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem7 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem8 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem9 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem10 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem11 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem12 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem13 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem14 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem15 { public void Apply(CCSPlayerController player) { } }
    public class ShopItem16 { public void Apply(CCSPlayerController player) { } }
}