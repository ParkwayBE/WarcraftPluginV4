using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
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
                // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
                SkillFunctions.SetBonusHealth(Player, 9999);

                // TODO: Dwarven Genes: Increased health
                // TODO: Supplies: Occasionally grants a grenade and chance to spawn with either scout or awp, Maybe 50/50 at level 5 going down to 10/90 in favor of the scout at level 1
            });
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            if (Player == null || !Player.IsValid || !Player.IsAlive())
                return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1); // Example: Dwarven Genes
            if (abilityLevel <= 0) return;

            int chancePercent = abilityLevel * 10; // Level 5 → 35% chance
            int roll = Random.Shared.Next(1, 101);

            Console.WriteLine($"[Evasion] Rolled {roll} vs {chancePercent}");

            if (roll <= chancePercent)
            {
                int originalDamage = @event.DmgHealth;
                int reducedDamage = (int)(originalDamage * (1f - 1.0f)); // 100% damage negation
                reducedDamage = Math.Max(0, reducedDamage);

                @event.DmgHealth = reducedDamage;

                Player.PrintToChat($" \x04[Evasion] Evaded {originalDamage} damage! (Roll: {roll}/{chancePercent})");
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
        }

    }
}