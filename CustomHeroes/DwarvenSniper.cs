using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using static WarcraftPlugin.Classes.Naix;


namespace WarcraftPlugin.Classes
{
    public class DwarvenSniper : WarcraftClass
    {
        public override string DisplayName => "Dwarven Sniper";
        public override Color DefaultColor => Color.GreenYellow;
        private readonly Dictionary<CCSPlayerController, SmokeSupplyEffect> activeEffects = new();


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
                var effect = new SmokeSupplyEffect(Player);
                activeEffects[Player] = effect;
                effect.Start();
            });

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

                maxGrenades = WarcraftPlayer.GetAbilityLevel(0);
                Console.WriteLine($"[DEBUG] Retrieved ability level: {maxGrenades}");

                if (maxGrenades < 1)
                {
                    Console.WriteLine("[INFO] Player has no Smoke Supply ability, skipping smoke grenade assignment.");
                    return;
                }

                Console.WriteLine($"[INFO] Grenade Supply Effect Activated - Ability Level: {maxGrenades}");

                // Remove existing smokes to prevent unintended stacking
                RemoveGrenades("weapon_hegrenade");

                //Start with 1 smoke
                Console.WriteLine("[INFO] Granting initial smoke grenade.");
                Owner.GiveNamedItem("weapon_hegrenade");
                maxGrenades = 1;
            }

            public void GiveGrenadeIfNeeded()
            {
                if (grenadesGiven >= maxGrenades)
                {
                    Console.WriteLine($"[INFO] {Owner.PlayerName} has already received the max number of smokes ({maxGrenades}).");
                    return;
                }

                Console.WriteLine($"[INFO] {Owner.PlayerName} has no smokes. Giving another one.");
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

            int evasionChance = abilityLevel * 7;

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
            StartCooldown(3); // Index 3 = Ultimate
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

    }
}