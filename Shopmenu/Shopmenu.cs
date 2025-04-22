using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Menu;

namespace WarcraftPlugin.Core
{
    public static class ShopItemRestrictions // TO DO: Update this with the correct blacklist
    {
        public static readonly Dictionary<Type, HashSet<string>> RaceBlacklist = new()
        {
            {
                typeof(BootsOfSpeed), new()
                {
                "undead_scourge", "laser_light_show"
                }
            },
            { typeof(RingOfRegen), new() { "undead_scourge" } },
            { typeof(NecklaceOfImmunity), new() { "undead_scourge" } },
            { typeof(FeatherBoots), new() { "undead_scourge" } },
            { typeof(LongjumpBoots), new() { "undead_scourge" } },
            { typeof(CloakOfInvisibility), new() { "undead_scourge" } },
            { typeof(OrbOfSlow), new() { "undead_scourge" } },
            { typeof(DisguiseKit), new() { "undead_scourge" } },
            { typeof(GlovesOfWarmth), new() { "undead_scourge" } },
            { typeof(MaskOfDeath), new() { "undead_scourge" } },
            { typeof(HelmOfExcellence), new() { "undead_scourge" } },
            { typeof(OrbOfReflection), new() { "undead_scourge" } },
            { typeof(FmjBullets), new() { "undead_scourge" } },
        };
    }
    public class ResurrectionInfo
    {
        public Vector RespawnLocation { get; set; }
        public float RespawnTriggerTime { get; set; }
    }

    public static class ResurrectionManager
    {
        public static readonly Dictionary<CCSPlayerController, ResurrectionInfo> ResurrectionQueue = new();

    }

    public static class InventoryManagement
    {
        public static readonly Dictionary<CCSPlayerController, List<IShopItem>> PersistentInventories = new();
    }
    public class ShopMenu
    {
        private readonly WarcraftPlugin _plugin;
        public static readonly Dictionary<CCSPlayerController, List<IShopItem>> Inventories = new();

        public ShopMenu(WarcraftPlugin plugin)
        {
            _plugin = plugin;
            _plugin.AddCommandListener("say", OnPlayerChat);
            _plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            _plugin.RegisterEventHandler<EventPlayerJump>(OnPlayerJump);
            _plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            _plugin.RegisterEventHandler<EventPlayerHurtOther>(OnPlayerHurtOther);
            _plugin.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
            _plugin.RegisterEventHandler<EventPlayerDeath>(OnDeath);
            _plugin.RegisterEventHandler<EventPlayerDisconnect>(OnDisconnect);
            _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);

            StartResurrectionWatcher(); // Updated .dll file
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            foreach (var (player, items) in Inventories)
            {
                foreach (var item in items)
                    item.ResetEffect(player);
            }

            Inventories.Clear();
            return HookResult.Continue;
        }
        private HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            if (@event == null || @event.Userid == null || !@event.Userid.IsValid)
                return HookResult.Continue;

            var player = @event.Userid;

            if (!player.IsValid || player.PlayerPawn?.Value == null)
                return HookResult.Continue;

            if (ShopMenu.Inventories.TryGetValue(player, out var items) && items.Any(i => i is LongjumpBoots))
            {
                _plugin.AddTimer(0.05f, () =>
                {
                    if (!player.IsValid || player.PlayerPawn?.Value == null)
                        return;

                    var angle = player.PlayerPawn.Value.EyeAngles;
                    var forward = new Vector();
                    NativeAPI.AngleVectors(angle.Handle, forward.Handle, nint.Zero, nint.Zero);

                    if (forward.Z < 0.55f)
                        forward.Z = 0.55f;

                    forward *= 520;

                    var pawn = player.PlayerPawn.Value;
                    pawn.AbsVelocity.X = forward.X;
                    pawn.AbsVelocity.Y = forward.Y;
                    pawn.AbsVelocity.Z = forward.Z;
                });

                _plugin.AddTimer(0.05f, () =>
                {
                    var pawn = player.PlayerPawn.Value;
                    float originalGravity = pawn.GravityScale;
                    pawn.GravityScale = 0.7f;

                    _plugin.AddTimer(5f, () =>
                    {
                        if (player.IsValid && player.PlayerPawn?.Value != null)
                        {
                            player.PlayerPawn.Value.GravityScale = originalGravity;
                        }
                    });
                });
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {

            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || attacker == victim)
                return HookResult.Continue;

            // --- Orb of Slow

            if (ShopMenu.Inventories.TryGetValue(attacker, out var attackerItems) && attackerItems.Any(item => item is OrbOfSlow))
            {
                var pawn = victim.PlayerPawn?.Value;
                if (pawn == null) return HookResult.Continue;

                var originalSpeed = pawn.VelocityModifier;
                pawn.VelocityModifier = originalSpeed / 2f;
                pawn.SetColor(Color.BlueViolet);

                _plugin.AddTimer(3f, () =>
                {
                    if (victim.IsValid && victim.PlayerPawn?.Value != null)
                    {
                        victim.PlayerPawn.Value.VelocityModifier = originalSpeed;
                        victim.PlayerPawn.Value.SetColor(Color.White);
                    }
                });

            }

            // --- Mask of Death 
            if (attackerItems != null && attackerItems.Any(item => item is MaskOfDeath) && Random.Shared.Next(100) < 20)
            {
                if (victim.PlayerPawn?.Value != null)
                {
                    victim.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255));
                    victim.PrintToChat($" {ChatColors.Red}✖ Your invisibility and immunity were stripped!");
                }
            }

            // --- Helm of Excellence 
            if (@event.Hitgroup == (int)HitGroup.Head && ShopMenu.Inventories.TryGetValue(victim, out var victimItems) && victimItems.Any(item => item is HelmOfExcellence))
            {
                int dmg = @event.DmgHealth;
                int reduced = (int)(dmg * 0.65f);

                if (victim.PlayerPawn?.Value != null)
                {
                    int currentHp = victim.PlayerPawn.Value.Health;
                    int newHp = currentHp + (dmg - reduced);
                    victim.PlayerPawn.Value.Health = newHp;

                    victim.PrintToCenter("🛡️ Helm of Excellence absorbed damage!");
                    Server.NextFrame(() => Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth"));
                }

            }

            // --- Orb of Reflection 
            victimItems = null;
            InventoryManagement.PersistentInventories.TryGetValue(victim, out victimItems);

            if (victimItems != null && victimItems.Any(item => item is OrbOfReflection) && attacker.IsValid && attacker.IsAlive() && attacker.PlayerPawn?.Value != null)
            {
                int reflected = (int)(@event.DmgHealth * 0.25f);
                if (reflected > 0)
                {
                    attacker.PlayerPawn.Value.Health -= reflected;

                    Server.NextFrame(() =>
                    {
                        if (attacker.PlayerPawn?.Value != null)
                            Utilities.SetStateChanged(attacker.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                    });

                    attacker.PrintToChat($" {ChatColors.Red}⚡ You were struck by reflected damage!");
                    victim.PrintToChat($" {ChatColors.Green}✔ Orb of Reflection struck your attacker for {reflected} damage!");
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            if (@event == null || @event.Userid == null || !@event.Userid.IsValid)
                return HookResult.Continue;

            var player = @event.Userid;

            if (!player.IsValid || player.PlayerPawn?.Value == null) return HookResult.Continue;

            _plugin.AddTimer(0.2f, () =>
            {
                if (!player.IsValid || player.PlayerPawn?.Value == null) return;
                Console.WriteLine("Player has spawned"); // Remove this when done
            });

            return HookResult.Continue;
        }
        private HookResult OnPlayerHurtOther(EventPlayerHurtOther @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || attacker == victim)
                return HookResult.Continue;

            ShopMenu.Inventories.TryGetValue(attacker, out var attackerItems);

            if (attackerItems != null && attackerItems.Any(item => item is FmjBullets))
            {
                @event.AddBonusDamage(5);
                attacker.PrintToCenter("You dealt 5 additional damage with FMJ bullets!");
            }

            return HookResult.Continue;
        }
        private HookResult OnDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var player = @event.Userid;

            if (@event == null || player == null || !player.IsValid)
                return HookResult.Continue;


            if (!player.IsValid || player.PlayerPawn?.Value == null) return HookResult.Continue;

            ShopMenu.Inventories.Remove(player);
            InventoryManagement.PersistentInventories.Remove(player);
            ResurrectionManager.ResurrectionQueue.Remove(player);

            return HookResult.Continue;
        }
        private HookResult OnDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!player.IsValid) return HookResult.Continue;

            ShopMenu.Inventories.Remove(player);
            InventoryManagement.PersistentInventories.Remove(player);
            ResurrectionManager.ResurrectionQueue.Remove(player);

            return HookResult.Continue;
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {

            foreach (KeyValuePair<CCSPlayerController, List<IShopItem>> entry in InventoryManagement.PersistentInventories)
            {
                var player = entry.Key;
                var items = entry.Value;

                if (!player.IsValid() || player.PlayerPawn?.Value == null) continue;

                _plugin.AddTimer(0.2f, () =>
                {
                    foreach (var item in items)
                        item.Apply(player);
                });
            }

            return HookResult.Continue;
        }

        private IShopItem GetShopItem(int index)
        {
            switch (index)
            {
                case 1: return new BootsOfSpeed();
                case 2: return new RingOfRegen();
                case 3: return new NecklaceOfImmunity();
                case 4: return new GrandExpTome();
                case 5: return new MassiveExpTome();
                case 6: return new GamblingExpTome();
                case 7: return new SmallExpTome();
                case 8: return new FeatherBoots();
                case 9: return new LongjumpBoots();
                case 10: return new CloakOfInvisibility();
                case 11: return new OrbOfSlow();
                case 12: return new FmjBullets();
                case 13: return new DisguiseKit();
                case 14: return new PeriaptOfHealth();
                case 15: return new GiftOfExp();
                case 16: return new ScrollOfResurrection();
                case 17: return new GlovesOfWarmth();
                case 18: return new MaskOfDeath();
                case 19: return new HelmOfExcellence();
                case 20: return new OrbOfReflection();

                default:
                    Console.WriteLine($"[WCS] ⚠ Invalid shop item index requested: {index}");
                    return new BootsOfSpeed(); // Safe fallback
            }
        }


        private void StartResurrectionWatcher()
        {
            _plugin.AddTimer(1f, () =>
            {
                float now = Server.CurrentTime;

                if (now < 10f) // Prevent startup crashes
                {
                    StartResurrectionWatcher();
                    return;
                }

                if (ResurrectionManager.ResurrectionQueue.Count == 0)
                {
                    StartResurrectionWatcher(); // Keep timer alive
                    return;
                }

                var toRespawn = ResurrectionManager.ResurrectionQueue
                    .Where(kvp => kvp.Value.RespawnTriggerTime <= now)
                    .ToList();

                foreach (var (player, info) in toRespawn)
                {
                    if (player == null || !player.IsValid || player.IsAlive())
                    {
                        ResurrectionManager.ResurrectionQueue.Remove(player);
                        continue;
                    }

                    if (player.PlayerPawn?.Value == null)
                    {
                        ResurrectionManager.ResurrectionQueue.Remove(player);
                        continue;
                    }

                    if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || player.IsAlive())
                    {
                        ResurrectionManager.ResurrectionQueue.Remove(player);
                        continue;
                    }
                    player.Respawn();

                    _plugin.AddTimer(0.8f, () =>
                    {
                        if (player.IsValid && player.PlayerPawn?.Value != null)
                        {
                            player.PlayerPawn.Value.Teleport(info.RespawnLocation);
                            player.PrintToChat($" {ChatColors.Green}✔ You have been resurrected at your ally’s location!");
                        }
                    });

                    ResurrectionManager.ResurrectionQueue.Remove(player);
                }

                StartResurrectionWatcher();
            });
        }





        private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
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
                        var money = pl.InGameMoneyServices;
                        if (money == null || !pl.IsValid()) return;

                        int currentMoney = money.Account;

                        if (!Inventories.TryGetValue(pl, out var ownedRoundItems))
                        {
                            ownedRoundItems = new List<IShopItem>();
                            Inventories[pl] = ownedRoundItems;
                        }

                        if (!InventoryManagement.PersistentInventories.TryGetValue(pl, out var persistentItems))
                        {
                            persistentItems = new List<IShopItem>();
                            InventoryManagement.PersistentInventories[pl] = persistentItems;
                        }


                        bool alreadyOwned = item.IsPersistent
                            ? persistentItems.Any(i => i.GetType() == item.GetType())
                            : ownedRoundItems.Any(i => i.GetType() == item.GetType());

                        if (alreadyOwned)
                        {
                            pl.PrintToChat($" {ChatColors.Red}✖ You already own {item.Name}{(item.IsPersistent ? " permanently" : " this round")}.");
                            return;
                        }

                        if (currentMoney < item.Cost)
                        {
                            pl.PrintToChat($" {ChatColors.Red}✖ Not enough money for {item.Name} (${item.Cost}).");
                            return;
                        }

                        if (!item.Apply(pl))
                        {
                            Console.WriteLine("Item failed to apply");
                            return;
                        }

                        money.Account -= item.Cost;
                        Utilities.SetStateChanged(pl, "CCSPlayerController", "m_pInGameMoneyServices");

                        if (item.IsPersistent)
                        {
                            persistentItems.Add(item);
                        }
                        else
                        {
                            ownedRoundItems.Add(item);
                        }

                        pl.PlayLocalSound("sounds/common/talk.vsnd"); // update with a different sound, Currently using this for ultimate cd aswell.
                        pl.PrintToChat($" {ChatColors.Green}✔ You bought {item.Name} for ${item.Cost}!");
                    });
                }

                pages.Add(menu);
            }

            MenuManagerExtra.OpenMainMenuExtra(player, pages);
        }

    }

    public interface IShopItem
    {
        string Name { get; }
        int Cost { get; }
        bool IsPersistent { get; } // To DO : Change some items to be persistant through roundEnd and can be brought over to the next round without having to repurchase
        bool Apply(CCSPlayerController player);
        void ResetEffect(CCSPlayerController player);
    }


    public static class ShopItemRegistry
    {
        public static List<IShopItem> GetAllItems() => new List<IShopItem>
        {
            new BootsOfSpeed(),
            new RingOfRegen(),
            new NecklaceOfImmunity(),
            new GrandExpTome(),
            new MassiveExpTome(),
            new GamblingExpTome(),
            new SmallExpTome(),
            new FeatherBoots(),
            new LongjumpBoots(),
            new CloakOfInvisibility(),
            new OrbOfSlow(),
            new FmjBullets(),
            new DisguiseKit(),
            new PeriaptOfHealth(),
            new GiftOfExp(),
            new ScrollOfResurrection(),
            new GlovesOfWarmth(),
            new MaskOfDeath(),
            new HelmOfExcellence(),
            new OrbOfReflection()
        };
    }

    public class PeriaptOfHealth : IShopItem
    {
        public string Name => "Periapt of Health";
        public int Cost => 2400;
        public bool IsPersistent => false;
        public bool Apply(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value == null) return false;
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }
            player.PlayerPawn.Value.Health += 50;
            Server.NextFrame(() => Utilities.SetStateChanged(player.PlayerPawn.Value!, "CBaseEntity", "m_iHealth"));
            player.PrintToChat($" {ChatColors.Green}+50 Health granted.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }

    public class BootsOfSpeed : IShopItem
    {
        public string Name => "Boots of Speed";
        public int Cost => 2600;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null || player.PlayerPawn?.Value == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            player.PlayerPawn.Value.VelocityModifier += 0.25f;
            player.PrintToChat($" {ChatColors.Green}✔ Speed Boots equipped! (+25% movement speed)");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            if (player.IsValid && player.PlayerPawn?.Value != null)
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
        }
    }


    public class RingOfRegen : IShopItem
    {
        public string Name => "Ring of Regen";
        public int Cost => 3500;
        public bool IsPersistent => false;

        private readonly Dictionary<CCSPlayerController, Timer> regenTimers = new();

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null || !player.IsValid || player.PlayerPawn?.Value == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            void RegenTick()
            {
                if (!player.IsValid || !player.IsAlive() || player.PlayerPawn?.Value == null) return;

                int currentHp = player.PlayerPawn.Value.Health;
                if (currentHp < 200)
                {
                    player.PlayerPawn.Value.Health = Math.Min(currentHp + 2, 200);
                    Server.NextFrame(() => Utilities.SetStateChanged(player.PlayerPawn.Value!, "CBaseEntity", "m_iHealth"));
                }

                regenTimers[player] = WarcraftPlugin.Instance.AddTimer(1.0f, RegenTick);
            }

            regenTimers[player] = WarcraftPlugin.Instance.AddTimer(1.0f, RegenTick);
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


    public class NecklaceOfImmunity : IShopItem // DISABLED 
    {
        public string Name => "[Disabled-Item]";
        public int Cost => 2500;
        public bool IsPersistent => false;
        private readonly HashSet<string> restrictedRaces = new()
        {
            "undead_scourge"
        };

        public bool Apply(CCSPlayerController player)
        {
            player.PrintToChat($" {ChatColors.Red}✖ Necklace of Immunity is currently disabled.");
            return false; // Disabling Necklace of Immunity for now
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }


            //wcPlayer.HasUltimateImmunity = true;
            player.PrintToChat($" {ChatColors.Green}✔ You are now immune to ultimates this round.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer != null)
            {
                //wcPlayer.HasUltimateImmunity = false;
                player.PrintToChat($" {ChatColors.Red}✖ Your ultimate immunity has worn off.");
            }
        }
    }

    public class GrandExpTome : IShopItem
    {
        public string Name => "Grand Exp Tome";
        public int Cost => 5000;
        public bool IsPersistent => false;
        private const int xpToGive = 300;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            WarcraftPlugin.Instance.XpSystem.AddXp(player, xpToGive);

            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            player.PrintToChat($" {ChatColors.Green}✔ You gained {xpToGive} XP from the Grand Tome of Experience!");
            player.PrintToChat($" {ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }

    public class MassiveExpTome : IShopItem
    {
        public string Name => "Massive Exp Tome";
        public int Cost => 10000;
        public bool IsPersistent => false;
        private const int xpToGive = 600;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            plugin.XpSystem.AddXp(player, xpToGive);

            var wcPlayer = plugin.GetWcPlayer(player);
            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            player.PrintToChat($"{ChatColors.Green}✔ You gained {xpToGive} XP from the Massive Tome of Experience!");
            player.PrintToChat($"{ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }

    public class GamblingExpTome : IShopItem
    {
        public string Name => "Gambling Exp Tome";
        public int Cost => 10000;
        public bool IsPersistent => false;
        private const int xpToGiveMin = 100;
        private const int xpToGiveMax = 900;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            var wcPlayer = plugin.GetWcPlayer(player);
            if (wcPlayer == null) return false;

            var random = new Random();
            int xpToGive = random.Next(xpToGiveMin, xpToGiveMax + 1);

            int roll = random.Next(1, 431);
            bool isGold = roll == 1;

            if (isGold)
            {
                xpToGive += 1000;
                foreach (var p in Utilities.GetPlayers())
                {
                    p.PrintToChat($" {ChatColors.Gold}✨ {player.PlayerName} rolled a GOLD CASE and gained +1000 bonus XP!");
                }
            }

            plugin.XpSystem.AddXp(player, xpToGive);

            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            player.PrintToChat($" {ChatColors.Green}🎲 You gained {xpToGive} XP from the Gambling Tome of Experience!");
            player.PrintToChat($" {ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");

            if (isGold)
            {
                player.PrintToChat($" {ChatColors.Gold}💛 You wasted your knife luck on this purchase...");
            }

            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }


    public class SmallExpTome : IShopItem
    {
        public string Name => "Exp Tome";
        public int Cost => 1000;
        public bool IsPersistent => false;
        private const int xpToGive = 50;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;
            plugin.XpSystem.AddXp(player, xpToGive);

            var wcPlayer = plugin.GetWcPlayer(player);
            int curXp = wcPlayer.currentXp;
            int maxXp = wcPlayer.amountToLevel;
            int level = wcPlayer.GetLevel();

            player.PrintToChat($" {ChatColors.Green}✔ You gained {xpToGive} XP from the Tome of Experience!");
            player.PrintToChat($" {ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }

    public class GiftOfExp : IShopItem
    {
        public string Name => "Gift of Experience";
        public int Cost => 4000;
        public bool IsPersistent => false;
        private const int xpToGive = 300;

        public bool Apply(CCSPlayerController player)
        {
            var plugin = WarcraftPlugin.Instance;

            var teammates = Utilities.GetPlayers()
                .Where(p => p.IsValid && p != player && !p.IsBot && p.TeamNum == player.TeamNum)
                .ToList();

            if (teammates.Count == 0)
            {
                player.PrintToChat($" {ChatColors.Red}✖ No teammates found to gift XP to.");
                return false;
            }

            var random = new Random();
            var chosen = teammates[random.Next(teammates.Count)];

            plugin.XpSystem.AddXp(chosen, xpToGive);

            var wcChosen = plugin.GetWcPlayer(chosen);
            int curXp = wcChosen.currentXp;
            int maxXp = wcChosen.amountToLevel;
            int level = wcChosen.GetLevel();

            player.PrintToChat($" {ChatColors.Green}✔ You gifted {xpToGive} XP to {chosen.PlayerName}!");
            chosen.PrintToChat($" {ChatColors.Gold}✨ {player.PlayerName} has gifted you {xpToGive} XP!");
            chosen.PrintToChat($" {ChatColors.Default}📘 You are now Level {level} ({curXp}/{maxXp} XP)");

            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }

    public class FeatherBoots : IShopItem
    {
        public string Name => "Feather Boots";
        public int Cost => 3100;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }


            player.PlayerPawn.Value.GravityScale = 0.65f;
            player.PrintToChat($" {ChatColors.Green}✔ Feather Boots equipped! Gravity reduced.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value != null)
            {
                player.PlayerPawn.Value.GravityScale = 1.0f;
                player.PrintToChat($" {ChatColors.Default}✖ Feather Boots have worn off.");
            }
        }
    }

    public class LongjumpBoots : IShopItem
    {
        public string Name => "Longjump Boots";
        public int Cost => 4000;
        public bool IsPersistent => false;


        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            player.PrintToChat($" {ChatColors.Green}✔ Longjump Boots equipped. Press jump to leap forward!");
            return true;
        }


        public void ResetEffect(CCSPlayerController player) { }
    }

    public class CloakOfInvisibility : IShopItem
    {
        public string Name => "Cloak of Invisibility";
        public int Cost => 1800;
        public bool IsPersistent => false;

        public static void Invisibility(CCSPlayerController player, float duration, int amount)
        {
            if (player?.PlayerPawn?.Value == null) return;

            var currentColor = player.PlayerPawn.Value.Render;
            var newColor = Color.FromArgb(
                Math.Clamp(170, 0, 255), // TO DO :Change 170 to correct level of invis
                currentColor.R,
                currentColor.G,
                currentColor.B
            );

            player.PlayerPawn.Value.Render = newColor;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
        }

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
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

    public class OrbOfSlow : IShopItem
    {
        public string Name => "Orb of Slow";
        public int Cost => 2800;
        public bool IsPersistent => false;


        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            player.PrintToChat($" {ChatColors.Green}✔ Orb of Slow equipped! You now have a chance to slow enemies on hit.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }


    public class DisguiseKit : IShopItem
    {
        public string Name => "Disguise";
        public int Cost => 1400;
        public bool IsPersistent => false;


        private readonly string ctModel = "models/player/custom_player/legacy/ctm_fbi.vmdl"; // TO DO: Fix proper player model
        private readonly string tModel = "models/player/custom_player/legacy/tm_leet.vmdl"; // TO DO: Fix proper player model

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            var model = player.TeamNum switch
            {
                2 => ctModel,
                3 => tModel,
                _ => null
            };

            if (model == null) return false;

            player.PlayerPawn.Value.SetModel(model);
            player.PrintToChat($" {ChatColors.Green}✔ You are now disguised as the enemy!");
            return true;
        }

        public void ResetEffect(CCSPlayerController player)
        {
            // Let the game naturally reset model on round end/death
        }
    }

    public class ScrollOfResurrection : IShopItem
    {
        public string Name => "Scroll of Resurrection";
        public int Cost => 5000;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;
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



            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }


            var random = new Random();
            var anchor = allies[random.Next(allies.Count)];

            ResurrectionManager.ResurrectionQueue[player] = new ResurrectionInfo
            {
                RespawnLocation = anchor.PlayerPawn.Value.AbsOrigin,
                RespawnTriggerTime = Server.CurrentTime + 3f
            };

            player.PrintToChat($" {ChatColors.Gold}⏳ Channeling resurrection... You will respawn in 3 seconds!");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }


    public class GlovesOfWarmth : IShopItem
    {
        public string Name => "Gloves of Warmth";
        public int Cost => 2800;
        public bool IsPersistent => false;
        private static readonly Dictionary<CCSPlayerController, Timer> GrenadeTimers = new();

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }


            player.GiveNamedItem("weapon_hegrenade");
            player.PrintToChat($"{ChatColors.Green}✔ Gloves of Warmth equipped!");

            StartRegenLoop(player);
            return true;
        }

        private void StartRegenLoop(CCSPlayerController player)
        {
            if (GrenadeTimers.ContainsKey(player))
                GrenadeTimers[player]?.Kill();

            GrenadeTimers[player] = WarcraftPlugin.Instance.AddTimer(1.0f, () =>
            {
                if (!player.IsValid || player.PlayerPawn?.Value == null || !player.IsAlive())
                    return;

                var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
                if (weapons == null) return;

                bool hasGrenade = weapons.Any(w =>
                    w.Value?.DesignerName.Contains("hegrenade") == true ||
                    w.Value?.DesignerName.Contains("flashbang") == true ||
                    w.Value?.DesignerName.Contains("decoy") == true ||
                    w.Value?.DesignerName.Contains("incgrenade") == true);

                if (!hasGrenade)
                {
                    var grenades = new[] {
                    "weapon_hegrenade",
                    "weapon_flashbang",
                    "weapon_decoy",
                    "weapon_incgrenade"
                };

                    string selected = grenades[Random.Shared.Next(grenades.Length)];
                    player.GiveNamedItem(selected);
                    player.PrintToChat($" {ChatColors.Green}🧤 Gloves of Warmth: You received a new {selected.Replace("weapon_", "").ToUpper()}!");
                }
                StartRegenLoop(player);
            });
        }

        public void ResetEffect(CCSPlayerController player)
        {
            if (GrenadeTimers.TryGetValue(player, out var timer))
            {
                timer.Kill();
                GrenadeTimers.Remove(player);
            }
        }
    }


    public class MaskOfDeath : IShopItem
    {
        public string Name => "Mask of Death";
        public int Cost => 1900;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            player.PrintToChat($" {ChatColors.Green}✔ Mask of Death equipped. You may reveal enemies!");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }


    public class HelmOfExcellence : IShopItem
    {
        public string Name => "Helm of Excellence";
        public int Cost => 3000;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            player.PrintToChat($" {ChatColors.Green}✔ Helm of Excellence equipped. Headshots hurt less!");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }

    public class OrbOfReflection : IShopItem
    {
        public string Name => "Orb of Reflection";
        public int Cost => 2800;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            player.PrintToChat($" {ChatColors.Green}✔ Orb of Reflection equipped! Some damage will be returned to attackers.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }


    public class FmjBullets : IShopItem
    {
        public string Name => "FMJ Bullets";
        public int Cost => 2800;
        public bool IsPersistent => false;

        public bool Apply(CCSPlayerController player)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer?.GetClass() == null) return false;

            string race = wcPlayer.GetClass().InternalName;

            if (ShopItemRestrictions.RaceBlacklist.TryGetValue(GetType(), out var blacklist) &&
                blacklist.Contains(race))
            {
                player.PrintToChat($" {ChatColors.Red}✖ Your race ({wcPlayer.GetClass().DisplayName}) cannot use this item.");
                return false;
            }

            player.PrintToChat($" {ChatColors.Green}✔ FMJ Bullets equipped! Bonus armor-piercing damage enabled.");
            return true;
        }

        public void ResetEffect(CCSPlayerController player) { }
    }
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
}