using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class DwarvenSniper : WarcraftClass
    {
        public override string DisplayName => "Dwarven Sniper";
        public override Color DefaultColor => Color.GreenYellow;

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

                // Determine weapon chance
                int awpChance = Math.Clamp(10 + (abilityLevel - 1) * 10, 10, 50); // Level 1 = 10%, Level 5 = 50%
                int roll = Random.Shared.Next(1, 101);
                string weaponToGive = (roll <= awpChance) ? "weapon_awp" : "weapon_ssg08";

                Console.WriteLine($"[Dwarven Supplies] Rolled {roll} → Giving {weaponToGive}");

                var pawn = Player.PlayerPawn.Value;
                var activeWeaponName = pawn.WeaponServices?.ActiveWeapon?.Value?.DesignerName;

                if (activeWeaponName != "weapon_ssg08" && activeWeaponName != "weapon_awp")
                {
                    Player.GiveNamedItem(weaponToGive);
                }
            });
        }


        private void PlayerHurt(EventPlayerHurt @event)
        {
            // Ensure Player is not null 
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
            // TODO: Eagle Eye : Increased damage with scoped weapons
            if (@event.Weapon == "weapon_ssg08" || @event.Weapon == "weapon_awp")
            {
                var damageBonus = WarcraftPlayer.GetAbilityLevel(0) * 12;
                @event.AddBonusDamage(damageBonus);
            }
        }

    }
}