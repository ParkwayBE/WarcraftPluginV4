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

            int maxBounces = 3;
            float bounceRadius = 1500f;
            int damage = 30;
            float bounceDelay = 0.3f;

            var caster = Player;
            var casterPos = caster.PlayerPawn.Value.AbsOrigin;
            var hitPlayers = new HashSet<CCSPlayerController> { caster };
            var lastTarget = caster;

            void Bounce(int bounceCount)
            {
                CCSPlayerController? nextTarget = null;
                float closestDistanceSq = float.MaxValue;

                foreach (var player in Utilities.GetPlayers())
                {
                    if (player == null || !player.IsValid || player.TeamNum == caster.TeamNum || player.PlayerPawn?.Value == null || hitPlayers.Contains(player))
                        continue;

                    var diff = player.PlayerPawn.Value.AbsOrigin - lastTarget.PlayerPawn.Value.AbsOrigin;
                    float distSq = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;

                    if (distSq <= bounceRadius * bounceRadius && distSq < closestDistanceSq)
                    {
                        closestDistanceSq = distSq;
                        nextTarget = player;
                    }
                }

                if (nextTarget == null)
                {
                    Console.WriteLine("[OrcishHorde] No more valid targets.");
                    return;
                }

                var wcTarget = nextTarget.GetWarcraftPlayer();
                if (wcTarget != null && wcTarget.HasUltimateImmunity)
                {
                    caster.PrintToCenter("⛔ Target is immune to ultimates!");
                    nextTarget.PrintToCenter("🛡️ Your Ultimate Immunity blocked Chain Lightning!");
                    Console.WriteLine($"[OrcishHorde] Target {nextTarget.PlayerName} had immunity.");
                    return;
                }

                SkillFunctions.DealRawDamage(caster, nextTarget, damage);
                hitPlayers.Add(nextTarget);

                // Visual: lightning laser + glow effect
                Warcraft.DrawLaserBetween(lastTarget.EyePosition(), nextTarget.EyePosition(), Color.Cyan, 1.5f);
                nextTarget.PlayerPawn.Value.SetColor(Color.FromArgb(255, 150, 255, 255));
                WarcraftPlugin.Instance.AddTimer(0.5f, () =>
                {
                    nextTarget.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255));
                });

                Console.WriteLine($"[OrcishHorde] Chain Lightning bounced to {nextTarget.PlayerName} (bounce #{bounceCount + 1})");
                lastTarget = nextTarget;

                if (bounceCount + 1 < maxBounces)
                {
                    WarcraftPlugin.Instance.AddTimer(bounceDelay, () => Bounce(bounceCount + 1));
                }
            }

            WarcraftPlugin.Instance.AddTimer(0.0f, () => Bounce(0));
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