using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;


namespace WarcraftPlugin.Classes
{
    public class OrcishHorde : WarcraftClass
    {
        public override string DisplayName => "Orcish Horde";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Critical Strike", "up to 35% to deal double damage."),
            new WarcraftAbility("Reincarnation", "Gain up to 100% chance to respawn once after dying"),
            new WarcraftAbility("Critical Grenade", "up to 100% chance to deal double damage"),
            new WarcraftCooldownAbility("Chain Lightning", "Strike a nearby enemy with lightning.", 8f, true)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                BonusHealth(Player, 9999);
            });
        }

        public static void BonusHealth(CCSPlayerController player, int amount)
        {
            var HealthEffect = new SetBonusHealth(player, amount);
            HealthEffect.Start();
        }


        private void Ultimate()
        {
            Console.WriteLine("[OrcishHorde] Ultimate activated");

            var caster = Player;
            var wcCaster = WarcraftPlayer;

            if (caster?.PlayerPawn?.Value == null)
            {
                Console.WriteLine("[OrcishHorde] Caster has no PlayerPawn.");
                return;
            }

            var casterPos = caster.PlayerPawn.Value.AbsOrigin;

            if (casterPos.X == 0 && casterPos.Y == 0 && casterPos.Z == 0)
            {
                Console.WriteLine("[OrcishHorde] Caster position invalid (0,0,0).");
                return;
            }

            float radius = 1500f;
            int damage = 30;
            var potentialTargets = new List<CCSPlayerController>();

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player == caster || !player.IsAlive())
                    continue;

                if (player.TeamNum == caster.TeamNum)
                    continue;

                if (player.PlayerPawn?.Value == null)
                {
                    Console.WriteLine($"[OrcishHorde] Skipped {player.PlayerName} — no PlayerPawn.");
                    continue;
                }

                var targetPos = player.PlayerPawn.Value.AbsOrigin;

                if (targetPos.X == 0 && targetPos.Y == 0 && targetPos.Z == 0)
                {
                    Console.WriteLine($"[OrcishHorde] Skipped {player.PlayerName} — position is zero.");
                    continue;
                }

                var diff = targetPos - casterPos;
                float distanceSq = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;

                Console.WriteLine($"[OrcishHorde] Checking {player.PlayerName} - Dist²: {distanceSq}");

                if (distanceSq <= radius * radius)
                {
                    potentialTargets.Add(player);
                }
            }

            Console.WriteLine($"[OrcishHorde] Found {potentialTargets.Count} potential targets");

            if (potentialTargets.Count == 0)
            {
                caster.PrintToCenter("⚡ No enemies nearby for Chain Lightning!");
                return;
            }

            var target = potentialTargets
                    .OrderBy(p =>
                    {
                        var pos = p.PlayerPawn?.Value?.AbsOrigin ?? default;
                        var diff = pos - casterPos;
                        return diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;
                    })
                    .First();

            var wcTarget = target.GetWarcraftPlayer();

            if (wcTarget == null)
            {
                Console.WriteLine($"[OrcishHorde] Target {target.PlayerName} has no WarcraftPlayer data (probably a bot). Continuing...");
            }
            else if (wcTarget.HasUltimateImmunity)
            {
                caster.PrintToCenter("⛔ Target is immune to ultimates!");
                target.PrintToCenter("🛡️ Your Ultimate Immunity blocked Chain Lightning!");
                Console.WriteLine($"[OrcishHorde] Target {target.PlayerName} had immunity.");
                return;
            }


            SkillFunctions.DealRawDamage(caster, target, damage);
            Warcraft.DrawLaserBetween(caster.EyePosition(), target.EyePosition(), Color.LightBlue, 2f);

            Console.WriteLine($"[OrcishHorde] Dealt {damage} damage to {target.PlayerName}");

            StartCooldown(3);
        }












        private void PlayerDeath(EventPlayerDeath death)
        {
            // reincarnation skill
        }
        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // Extra damage
            // Extra Damage with nades
        }

    }
}