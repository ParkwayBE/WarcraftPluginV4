using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
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

                        if (currentMoney < item.Cost)
                        {
                            pl.PrintToChat($" {ChatColors.Red}✖ You can't afford this item. It costs ${item.Cost}.");
                        }
                        else
                        {
                            moneyService.Account = Math.Max(0, currentMoney - item.Cost);

                            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iAccount");

                            item.Apply(pl);
                            pl.PrintToChat($" {ChatColors.Green}✔ You bought {item.Name} for ${item.Cost}!");
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
                14 => new ShopItem14(), // Functional item (+50 HP)
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
        void Apply(CCSPlayerController player);
    }

    // === Example Placeholder Items ===
    public class ShopItem1 : IShopItem { public string Name => "Placeholder 1"; public int Cost => 800; public void Apply(CCSPlayerController player) { } }
    public class ShopItem2 : IShopItem { public string Name => "Placeholder 2"; public int Cost => 1300; public void Apply(CCSPlayerController player) { } }
    public class ShopItem3 : IShopItem { public string Name => "Placeholder 3"; public int Cost => 2200; public void Apply(CCSPlayerController player) { } }
    public class ShopItem4 : IShopItem { public string Name => "Placeholder 4"; public int Cost => 3100; public void Apply(CCSPlayerController player) { } }
    public class ShopItem5 : IShopItem { public string Name => "Placeholder 5"; public int Cost => 900; public void Apply(CCSPlayerController player) { } }
    public class ShopItem6 : IShopItem { public string Name => "Placeholder 6"; public int Cost => 1600; public void Apply(CCSPlayerController player) { } }
    public class ShopItem7 : IShopItem { public string Name => "Placeholder 7"; public int Cost => 2400; public void Apply(CCSPlayerController player) { } }
    public class ShopItem8 : IShopItem { public string Name => "Placeholder 8"; public int Cost => 3300; public void Apply(CCSPlayerController player) { } }
    public class ShopItem9 : IShopItem { public string Name => "Placeholder 9"; public int Cost => 1000; public void Apply(CCSPlayerController player) { } }
    public class ShopItem10 : IShopItem { public string Name => "Placeholder 10"; public int Cost => 1800; public void Apply(CCSPlayerController player) { } }
    public class ShopItem11 : IShopItem { public string Name => "Placeholder 11"; public int Cost => 2600; public void Apply(CCSPlayerController player) { } }
    public class ShopItem12 : IShopItem { public string Name => "Placeholder 12"; public int Cost => 3500; public void Apply(CCSPlayerController player) { } }
    public class ShopItem13 : IShopItem { public string Name => "Placeholder 13"; public int Cost => 1100; public void Apply(CCSPlayerController player) { } }

    // === Functional Example ===
    public class ShopItem14 : IShopItem
    {
        public string Name => "Vitality Boost";
        public int Cost => 2000;

        public void Apply(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value != null)
            {
                player.PlayerPawn.Value.Health += 50;
                player.PrintToChat($"{ChatColors.Green}+50 HP applied!");
            }
        }
    }

    public class ShopItem15 : IShopItem { public string Name => "Placeholder 15"; public int Cost => 2800; public void Apply(CCSPlayerController player) { } }
    public class ShopItem16 : IShopItem { public string Name => "Placeholder 16"; public int Cost => 3900; public void Apply(CCSPlayerController player) { } }
}
