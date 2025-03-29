using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;




namespace WarcraftPlugin.Classes
{
    public class DwarvenSniper : WarcraftClass
    {
        public override string DisplayName => "Dwarven Sniper";
        public override Color DefaultColor => Color.GreenYellow;
        private readonly Dictionary<CCSPlayerController, GrenadeSupplyEffect> activeEffects = new();
        private float evasionMultiplier = 1.0f;
        private bool impaleOnSight = false;
        private bool impaleTriggered = false;




        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Eagle eye", "Increased damage with scoped weapons."),
            new WarcraftAbility("Dwarven Genes", "Evasion and increased health"),
            new WarcraftAbility("Supplies", "Occasionally grants a grenade and chance to spawn with a Scout or AWP"),
            new WarcraftCooldownAbility("Ring of power","For the next 5 seconds you double your evasion and the first player to look at you gets impaled.", 1f) // TODO: If not possible to code this skill then adapt it, but stay on the theme.
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            HookEvent<EventGrenadeThrown>(GrenadeThrown);
            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
                if (abilityLevel <= 0)
                    return;

                SkillFunctions.SetBonusHealth(Player, 9999); // TEMP TESTING

                int awpChance = abilityLevel * 10; // Level 1 = 10%, Level 5 = 50%
                int roll = Random.Shared.Next(100);

                string weaponToGive = (roll < awpChance) ? "weapon_awp" : "weapon_ssg08";
                Console.WriteLine($"[Dwarven Supplies] Rolled {roll} vs {awpChance} → Giving {weaponToGive}");

                var pawn = Player.PlayerPawn.Value;
                var activeWeaponName = pawn.WeaponServices?.ActiveWeapon?.Value?.DesignerName;

                if (activeWeaponName != "weapon_ssg08" && activeWeaponName != "weapon_awp")
                {
                    Player.GiveNamedItem(weaponToGive);
                }

                if (activeEffects.TryGetValue(Player, out var existingEffect))
                {
                    existingEffect.Destroy();
                    activeEffects.Remove(Player);
                }

                Player.GiveNamedItem("weapon_hegrenade");
                var effect = new GrenadeSupplyEffect(Player);
                activeEffects[Player] = effect;
                effect.Start();
            });

        }

        internal void GrenadeThrown(EventGrenadeThrown @event)
        {
            if (activeEffects.TryGetValue(Player, out var effect))
            {
                effect.GiveGrenadeIfNeeded();
            }
        }


        internal class GrenadeSupplyEffect(CCSPlayerController owner) : WarcraftEffect(owner)
        {
            private int grenadesGiven = 0;
            private int maxGrenades = 0;
            private WarcraftPlayer WarcraftPlayer;

            public override void OnStart()
            {
                Console.WriteLine("[DEBUG] GrenadeSupplyEffect OnStart() triggered.");

                if (Owner == null || Owner.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("[ERROR] GrenadeSupplyEffect started but Owner is NULL! Aborting.");
                    return;
                }

                WarcraftPlayer = Owner.GetWarcraftPlayer();
                if (WarcraftPlayer == null)
                {
                    Console.WriteLine("[ERROR] Failed to retrieve WarcraftPlayer.");
                    return;
                }

                maxGrenades = WarcraftPlayer.GetAbilityLevel(2);
                Console.WriteLine($"[DEBUG] Retrieved ability level: {maxGrenades}");

                if (maxGrenades < 1)
                {
                    Console.WriteLine("[INFO] Player has no Grenade Supply ability, skipping grenade assignment.");
                    return;
                }

                Console.WriteLine($"[INFO] Grenade Supply Effect Activated - Ability Level: {maxGrenades}");

                RemoveGrenades("weapon_hegrenade");

                Console.WriteLine("[INFO] Granting initial grenade.");
                Owner.GiveNamedItem("weapon_hegrenade");
                maxGrenades = 4;
            }

            public void GiveGrenadeIfNeeded()
            {
                if (grenadesGiven >= maxGrenades)
                {
                    Console.WriteLine($"[INFO] {Owner.PlayerName} has already received the max number of grenades ({maxGrenades}).");
                    return;
                }

                Console.WriteLine($"[INFO] {Owner.PlayerName} has no grenades. Giving another one.");
                Owner.GiveNamedItem("weapon_hegrenade");
                grenadesGiven++;
            }


            public override void OnFinish()
            {
                Console.WriteLine($"[INFO] No more free grenades for {Owner.PlayerName} this round.");
            }

            private void RemoveGrenades(string grenadeName)
            {
                var grenades = Owner.PlayerPawn.Value.WeaponServices.MyWeapons;

                foreach (var grenade in grenades)
                {
                    if (grenade.Value.DesignerName == grenadeName)
                    {
                        Console.WriteLine($"[INFO] Removing existing {grenadeName} from {Owner.PlayerName}");
                        Owner.DropWeaponByDesignerName(grenadeName);
                    }
                }
            }

            public override void OnTick() { }

        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            if (Player == null) return;
            HandleEvasion(@event);
        }

        private void HandleEvasion(EventPlayerHurt @event)
        {
            if (Player == null) return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel == 0) return;

            int baseChance = abilityLevel * 7;
            int evasionChance = (int)(baseChance * evasionMultiplier);

            var roll = Random.Shared.Next(100);
            if (roll < evasionChance)
            {
                Console.WriteLine($"Evasion triggered! Chance: {evasionChance}% (Roll: {roll})");
                @event.IgnoreDamage();

                Player.PrintToChat("You evaded a hit.");
            }
        }

        private void Ultimate()
        {
            // TODO: Ring of power : Double evasion
            // TODO: Ring of power : Attempt to code the impale skill
            if (Player == null) return;
            evasionMultiplier = 2.0f;
            Player.PrintToChat(" \x06[Ultimate] Your evasion has been doubled for 5 seconds!");

            WarcraftPlugin.Instance.AddTimer(5.0f, () =>
            {
                if (Player == null) return;
                evasionMultiplier = 1.0f;
                Player.PrintToChat(" \x06[Ultimate] Your evasion boost has ended.");
            });

            ActivateImpaleUltimate();

            StartCooldown(3); // Index 3 = Ultimate
        }

        private HashSet<CCSPlayerController> impaleTriggeredPlayers = new();

        private void ActivateImpaleUltimate()
        {

            impaleTriggeredPlayers.Clear();

            Player.PrintToChat(" \x05[Ultimate] Impale activated! Anyone who sees you will be launched!");

            WarcraftPlugin.Instance.AddTimer(5.0f, () =>
            {
                impaleTriggeredPlayers.Clear();
                Player.PrintToChat(" \x05[Ultimate] Impale has ended.");
            });
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            if (@event.Weapon == "ssg08" || @event.Weapon == "awp")
            {
                var damageBonus = WarcraftPlayer.GetAbilityLevel(0) * 8;
                @event.AddBonusDamage(damageBonus);
                Console.WriteLine($"Dealt {damageBonus} extra damage with a scoped weapon.");
            }
        }

        [GameEventHandler]
        public HookResult OnSpottedByEnemy(EventSpottedByEnemy e)
        {
            if (!impaleOnSight || Player == null || !Player.IsValid)
                return HookResult.Continue;

            var enemy = e.UserId;
            if (enemy == null || !enemy.IsValid || enemy == Player || enemy.TeamNum == Player.TeamNum)
                return HookResult.Continue;

            if (impaleTriggeredPlayers.Contains(enemy)) return HookResult.Continue;

            // Apply impale effect
            if (enemy.PlayerPawn?.Value != null)
            {
                enemy.PlayerPawn.Value.Teleport(null, null, new Vector(0, 0, 500));
                enemy.PrintToChat(" \x07[Impale] You looked at the wrong dwarf!");
                Player.PrintToChat($" \x04[Impale] {enemy.PlayerName} was launched for looking at you!");
                impaleTriggeredPlayers.Add(enemy);
            }

            return HookResult.Continue;
        }



    }
}