using System;
using System.Collections.Generic;
using System.Drawing;
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
        bool hitSomething = false;

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
                StartCooldown(3);
            });
        }

        public static void BonusHealth(CCSPlayerController player, int amount)
        {
            var HealthEffect = new SetBonusHealth(player, amount);
            HealthEffect.Start();
        }


        private void Ultimate()
        {
            if (WarcraftPlayer.GetAbilityLevel(3) <= 0)
                return;

            Console.WriteLine("[OrcishHorde] Ultimate activated");

            var caster = Player;
            var casterPos = caster.PlayerPawn?.Value?.AbsOrigin;
            if (casterPos == null) return;

            int maxBounces = 3;
            float bounceRadius = 1500f;
            int damage = 30;
            float delayBetweenBounces = 0.3f;

            var hitPlayers = new HashSet<CCSPlayerController>();
            var bounceTargets = new List<CCSPlayerController> { caster };
            bool hitSomething = false; // ✅ Track if any valid target was hit

            void DoBounce(int bounceIndex)
            {
                if (bounceIndex >= maxBounces)
                {
                    caster.PrintToCenter("⚡ Chain Lightning ended.");
                    return;
                }

                var last = bounceTargets[bounceTargets.Count - 1];
                var lastPos = last.PlayerPawn?.Value?.AbsOrigin;
                if (lastPos == null) return;

                CCSPlayerController? closest = null;
                float closestDistSq = float.MaxValue;

                foreach (var player in Utilities.GetPlayers())
                {
                    if (player == null || !player.IsValid || !player.IsAlive() || player.PlayerPawn?.Value == null)
                        continue;

                    if (player == last || player == caster || player.TeamNum == caster.TeamNum)
                        continue;

                    if (hitPlayers.Contains(player))
                        continue;

                    var pos = player.PlayerPawn.Value.AbsOrigin;
                    var diff = pos - lastPos;
                    float distSq = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;

                    if (distSq <= bounceRadius * bounceRadius && distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closest = player;
                    }
                }

                if (closest == null)
                {
                    caster.PrintToCenter("⚡ Chain Lightning ended.");
                    Console.WriteLine($"[OrcishHorde] No more valid targets after bounce #{bounceIndex + 1}");
                    return;
                }

                var wcTarget = closest.GetWarcraftPlayer();
                if (wcTarget != null && wcTarget.HasUltimateImmunity)
                {
                    caster.PrintToCenter("⛔ Target is immune to ultimates!");
                    closest.PrintToCenter("🛡️ Your Ultimate Immunity blocked Chain Lightning!");
                    Console.WriteLine($"[OrcishHorde] Target {closest.PlayerName} had immunity. Skipping.");
                    return;
                }

                // ✅ SUCCESS — we hit someone
                hitSomething = true;

                hitPlayers.Add(closest);
                bounceTargets.Add(closest);

                SkillFunctions.DealRawDamage(caster, closest, damage);
                Warcraft.DrawLaserBetween(last.EyePosition(), closest.EyePosition(), Color.Cyan, 2.0f);

                Console.WriteLine($"[OrcishHorde] Chain Lightning bounced to {closest.PlayerName} (bounce #{bounceIndex + 1})");

                WarcraftPlugin.Instance.AddTimer(delayBetweenBounces, () => DoBounce(bounceIndex + 1));
            }

            // Start chain
            DoBounce(0);

            // ✅ Only start cooldown if something was hit
            if (hitSomething)
            {
                StartCooldown(3);
            }
            else
            {
                caster.PrintToCenter("⚠️ No valid targets for Chain Lightning — no cooldown used.");
            }
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