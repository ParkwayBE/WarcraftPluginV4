using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Helpers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;



namespace WarcraftPlugin.CustomSkills
{
    public static class SkillFunctions
    {
        public static void MovementSpeed(CCSPlayerController player, float amount, float duration)
        {
            new SetMovementSpeed(player, amount, duration).Start();
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
            if (!target.IsAlive() || !attacker.IsAlive() || attacker.TeamNum == target.TeamNum)
                return;

            if (Warcraft.RollDice(100, chancePercent)) // simple roll: 1 in `chance`
            {
                int roll = new Random().Next(1, 101);

                if (roll <= chancePercent)
                {
                    Console.WriteLine("[Freeze] Freeze applied!");
                    new FreezePlayerEffect(attacker, duration, target).Start();
                }

            }
        }

        public static void ExplodeOnDeathSkill(CCSPlayerController player, float radius, float damage)
        {
            new ExplodeOnDeathEffect(player, radius, damage).Start();
        }

        public static void SetPlayerGravity(CCSPlayerController player, float gravityPercent, float duration)
        {
            new SetGravityEffect(player, gravityPercent, duration).Start();
        }

        public static void LeechHealth(CCSPlayerController attacker, CCSPlayerController victim, int chancePercent, float healPercent, int damageDealt)
        {
            if (!victim.IsAlive() || !attacker.IsAlive() || attacker.TeamNum == victim.TeamNum)
                return;

            LeechSkill.LeechHealth(attacker, victim, chancePercent, healPercent, damageDealt);
        }

        public static void SlowTarget(CCSPlayerController attacker, CCSPlayerController target, int chancePercent, float duration)
        {
            if (!target.IsAlive() || !attacker.IsAlive() || attacker.TeamNum == target.TeamNum)
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

        public static void DealRawDamage(CCSPlayerController attacker, CCSPlayerController victim, int damage)
        {
            if (victim == null || !victim.IsValid || victim.PlayerPawn?.Value == null) return;

            if (!victim.IsAlive() || !attacker.IsAlive() || attacker.TeamNum == victim.TeamNum)
                return;

            int newHealth = victim.PlayerPawn.Value.Health - damage;

            if (newHealth <= 0)
            {
                victim.CommitSuicide(true, true); // Kills the player properly
                Console.WriteLine($"[Chain Lightning] {attacker.PlayerName} killed {victim.PlayerName} with the final zap!");
            }
            else
            {
                victim.SetHp(newHealth);
                Console.WriteLine($"[Chain Lightning] {attacker.PlayerName} dealt {damage} to {victim.PlayerName} (new HP: {newHealth})");
            }

            attacker.PrintToCenter($"⚡ You dealt {damage} damage to {victim.PlayerName}!");
            victim.PrintToCenter($"⚡ You were hit by {attacker.PlayerName}'s Chain Lightning!");
        }

        public static void ImpaleTarget(CCSPlayerController attacker, CCSPlayerController victim, float force = 300f)
        {
            if (victim == null || !victim.IsValid || victim.PlayerPawn?.Value == null) return;

            if (!victim.IsAlive() || !attacker.IsAlive() || attacker.TeamNum == victim.TeamNum)
                return;

            var launchVelocity = new Vector(0, 0, force);
            victim.PlayerPawn.Value.Teleport(null, null, launchVelocity);

        }


        public static void RestrictWeapons(CCSPlayerController player, List<string> allowedWeapons, float duration = 999f)
        {
            new RestrictWeaponsEffect(player, duration, allowedWeapons).Start();
        }

        public static Vector Normalize(Vector vec)
        {
            float length = MathF.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
            if (length == 0) return new Vector(0, 0, 0);
            return new Vector(vec.X / length, vec.Y / length, vec.Z / length);
        }
    }
}
