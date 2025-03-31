using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Helpers;

namespace WarcraftPlugin.CustomSkills
{
    public static class LeechSkill
    {
        public static void LeechHealth(CCSPlayerController attacker, CCSPlayerController victim, int chancePercent, float healPercent, int damageDealt)
        {
            if (attacker == null || !attacker.IsValid || !attacker.IsAlive())
                return;


            if (!Warcraft.RollDice(chancePercent, 100))
                return;

            int currentHealth = attacker.PlayerPawn.Value.Health;

            int healAmount = (int)(damageDealt * (healPercent / 100f));
            var pawn = attacker.PlayerPawn.Value;
            var victimPawn = victim.PlayerPawn.Value;


            int newHealth = currentHealth + healAmount;

            attacker.SetHp(newHealth);

            Console.WriteLine($"You have leeched health for {healAmount} And you now have {newHealth}");

            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            attacker.PrintToChat($"{ChatColors.Green} Vampiric Touch{ChatColors.Default} : You leeched {healAmount} {ChatColors.LightPurple}health{ChatColors.Default}.");
            Warcraft.SpawnParticle(pawn.AbsOrigin.Clone().Add(z: 40), "particles/blood_impact/blood_impact_basic.vpcf", 0.6f);
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 50), "particles/weapons/cs_weapon_fx/weapon_sensorgren_detonate.vpcf", 0.4f);

        }
    }
}

