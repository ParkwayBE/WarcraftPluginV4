using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Helpers;


namespace WarcraftPlugin.CustomSkills
{
    public static class SkillFunctions
    {
        public static void MovementSpeed(CCSPlayerController player, float amount, float duration)
        {
            new SetMovementSpeed(player, duration, amount).Start();
        }
        public static void SetBonusHealth(CCSPlayerController player, int amount)
        {
            new SetBonusHealth(player, amount).Start();
        }
        public static void SetInvisibility(CCSPlayerController player, float duration, int alpha)
        {
            new SetInvisibility(player, duration, alpha).Start();
        }
        // Teleport Skill
        public static void TeleportUltimate(CCSPlayerController player)
        {
            TeleportSkill.Execute(player);
        }

        public static void HandleTeleportPing(CCSPlayerController player, float x, float y, float z)
        {
            TeleportSkill.HandlePing(player, x, y, z);
        }
        // end Teleport skill

        public static void FreezePlayer(CCSPlayerController attacker, CCSPlayerController target, int chancePercent, float duration)
        {
            if (!target.IsAlive() || !attacker.IsAlive())
                return;

            if (Warcraft.RollDice(100, chancePercent)) // simple roll: 1 in `chance`
            {
                int roll = new Random().Next(1, 101);
                Console.WriteLine($"[Freeze] Rolled: {roll} vs Chance: {chancePercent}");

                if (roll <= chancePercent)
                {
                    Console.WriteLine("[Freeze] Freeze applied!");
                    new FreezePlayerEffect(attacker, duration, target).Start();
                }

            }
        }

        public static void LeechHealth(CCSPlayerController attacker, CCSPlayerController victim, int chancePercent, float healPercent, int damageDealt)
        {
            LeechSkill.LeechHealth(attacker, victim, chancePercent, healPercent, damageDealt);
        }

        public static void SlowTarget(CCSPlayerController attacker, CCSPlayerController target, int chancePercent, float duration)
        {
            if (!target.IsAlive() || !attacker.IsAlive())
                return;

            if (Warcraft.RollDice(100, chancePercent)) // simple roll: 1 in `chance`
            {
                int roll = new Random().Next(1, 101);
                Console.WriteLine($"[Freeze] Rolled: {roll} vs Chance: {chancePercent}");

                if (roll <= chancePercent)
                {
                    Console.WriteLine("[Freeze] Slow applied!");
                    new SlowTarget(attacker, duration, target).Start();
                }

            }
        }

        public static void RestrictWeapons(CCSPlayerController player, List<string> allowedWeapons, float duration = 999f)
        {
            new RestrictWeaponsEffect(player, duration, allowedWeapons).Start();
        }
    }
}
