using System;
using System.Drawing;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Hosting;
using WarcraftPlugin.Core.Preload;
using WarcraftPlugin.Helpers;

namespace WarcraftPlugin.CustomSkills
{
    public static class LeechSkill
    {
        public static void LeechHealth(CCSPlayerController attacker, CCSPlayerController victim, int chancePercent, float healPercent, int damageDealt)
        {
            Console.WriteLine("PlayerHurtOther Event has succesfully triggered LeechHealth");
            if (attacker == null || !attacker.IsValid || !attacker.IsAlive())
                return;

            Console.WriteLine("Player is not null and play is valid and alive");
            
            if (!Warcraft.RollDice(chancePercent, 100))
                return;


            Console.WriteLine("Player is about to heal some health from lifesteal");
            int currentHealth = attacker.PlayerPawn.Value.Health;

            int healAmount = (int)(damageDealt * (healPercent / 100f));
            var pawn = attacker.PlayerPawn.Value;
            var victimPawn = victim.PlayerPawn.Value;


            int newHealth = currentHealth + healAmount;

            // ✅ Apply new health
            // pawn.Health = newHealth;
            attacker.SetHp(newHealth);

            Console.WriteLine($"You have leeched health for {healAmount} And you now have {newHealth}");

            // ✅ Notify engine of health change
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            // ✅ Feedback
            attacker.PrintToChat($"[Vampiric Touch] You leeched {healAmount} health.");
            Warcraft.SpawnParticle(pawn.AbsOrigin.Clone().Add(z: 40), "particles/blood_impact/blood_impact_basic.vpcf", 0.6f);
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 50), "particles/environment/directionallight_glow01.vpcf", 0.4f);

        }
    }
}

