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
            if (attacker == null || !attacker.IsValid || !attacker.IsAlive())
                return;

            if (!Warcraft.RollDice(1, 100 / chancePercent))
                return;

            int healAmount = (int)(damageDealt * (healPercent / 100f));
            var pawn = attacker.PlayerPawn.Value;
            var currentHealth = pawn.Health;

            if (currentHealth < 200)
            {
                int newHealth = Math.Min(currentHealth + healAmount, 200);
                pawn.Health = newHealth;  // ✅ Directly set HP
                attacker.PrintToChat($"[Vampiric Touch] You leeched {healAmount} health.");

                Warcraft.SpawnParticle(pawn.AbsOrigin.Clone().Add(z: 40), "particles/blood_impact/blood_impact_basic.vpcf", 0.6f);
                Warcraft.SpawnParticle(pawn.AbsOrigin.Clone().Add(z: 50), "particles/ui/ui_playerhealthbuff_red.vpcf", 0.4f);
            }
        }
    }
}

