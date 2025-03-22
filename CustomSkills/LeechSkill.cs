using System;
using System.Numerics;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Helpers;

namespace WarcraftPlugin.CustomSkills
{
    public static class LeechSkill
    {
        public static void LeechHealth(CCSPlayerController attacker, int chancePercent, float healPercent, int damageDealt)
        {
            // Validation checks
            if (attacker == null || !attacker.IsValid || !attacker.IsAlive())
                return;

            // RollDice(chance, outOf)
            if (!Warcraft.RollDice(1, 100 / chancePercent))
                return;

            int healAmount = (int)(damageDealt * (healPercent / 100f));
            var currentHealth = attacker.PlayerPawn.Value.Health;

            if (currentHealth < 200) // Optional cap
            {
                int newHealth = Math.Min(currentHealth + healAmount, 200);
                attacker.SetHp(newHealth);
                attacker.PrintToChat($"[Vampiric Touch] You leeched {healAmount} health.");

                // Vampiric visual effects
                Warcraft.SpawnParticle(attacker.PlayerPawn.Value.AbsOrigin.Clone().Add(z: 40), "particles/blood_impact/blood_impact_basic.vpcf", 0.6f);
                Warcraft.SpawnParticle(attacker.PlayerPawn.Value.AbsOrigin.Clone().Add(z: 50), "particles/ui/ui_playerhealthbuff_red.vpcf", 0.4f);
            }
        }
    }
}

