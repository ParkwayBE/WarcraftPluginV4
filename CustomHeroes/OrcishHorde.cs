using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
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
        private Vector? lastSpawnPosition = null;
        private Vector? lastDeathPosition = null;
        private bool hasReincarnated = false;
        private static readonly Random _rng = new();


        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Critical Strike", "up to 35% to deal double damage."),
            new WarcraftAbility("Reincarnation", "Gain up to 100% chance to respawn once after dying"),
            new WarcraftAbility("Critical Grenade", "up to 100% chance to deal double damage"),
            new WarcraftCooldownAbility("Chain Lightning", "Strike a nearby enemy with lightning.", 32f, true)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDeath>(OnDeath);
            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                ResetCooldowns();

                if (Player?.PlayerPawn?.Value == null) return;

                lastSpawnPosition = Player.PlayerPawn.Value.AbsOrigin.Clone();
                hasReincarnated = false;
            });
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
            bool hitSomething = false;

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
                    return;
                }

                hitSomething = true;
                hitPlayers.Add(closest);
                bounceTargets.Add(closest);
                SkillFunctions.DealRawDamage(caster, closest, damage);
                var lightningPos = Warcraft.EyePosition(closest);
                var particle = Warcraft.SpawnParticle(lightningPos, "particles/ui/status_levels/ui_status_level7_lightning.vpcf", 2.0f);
                particle.SetParent(closest.PlayerPawn.Value);
                WarcraftPlugin.Instance.AddTimer(delayBetweenBounces, () => DoBounce(bounceIndex + 1));
            }
            DoBounce(0);
            if (hitSomething)
            {
                StartCooldown(3);
            }
            else
            {
                caster.PrintToCenter("⚠️ No valid targets for Chain Lightning — no cooldown used.");
            }
        }


        private void OnDeath(EventPlayerDeath death)
        {
            if (Player?.PlayerPawn?.Value == null || hasReincarnated)
                return;

            int level = WarcraftPlayer.GetAbilityLevel(1);
            if (level == 0) return;

            lastDeathPosition = Player.PlayerPawn.Value.AbsOrigin.Clone();

            float chance = level * 0.2f;

            if (_rng.NextDouble() <= chance)
            {
                hasReincarnated = true;

                WarcraftPlugin.Instance.AddTimer(2f, () =>
                {
                    Player.PrintToChat(" \x06[Reincarnation] You have been revived!");
                    Player.Respawn();
                    Player.SetHp(100);

                    Vector spawnPoint;

                    if (lastSpawnPosition == null && lastDeathPosition == null)
                    {
                        spawnPoint = Player.PlayerPawn.Value.AbsOrigin;
                    }
                    else
                    {
                        spawnPoint = _rng.Next(0, 2) == 0
                            ? lastSpawnPosition ?? lastDeathPosition!
                            : lastDeathPosition ?? lastSpawnPosition!;

                    }

                    Player.PlayerPawn.Value.Teleport(spawnPoint, new QAngle(), new Vector());
                    Warcraft.SpawnParticle(spawnPoint, "particles/ui/status_levels/ui_status_level_7_energycirc.vpcf", 4f);
                    Player.PlayLocalSound("sounds/ambient/atmosphere/cs_cable_rattle02.vsnd");
                });
            }
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || attacker.PlayerPawn?.Value == null)
                return;

            var wcPlayer = attacker.GetWarcraftPlayer();
            if (wcPlayer == null) return;

            var damageDealt = @event.DmgHealth;

            if (@event.Weapon == "weapon_grenade")
            {
                int nadeLevel = wcPlayer.GetAbilityLevel(2);
                if (nadeLevel == 0) return;

                int chancePercent = Math.Min(nadeLevel * 20, 100);
                int roll = new Random().Next(1, 101);

                Console.WriteLine($"[GrenadeCrit] Rolled {roll} vs {chancePercent}");

                if (roll <= chancePercent)
                {
                    int bonus = damageDealt / 2;
                    int total = damageDealt + bonus;
                    @event.AddBonusDamage(total);
                    attacker.PrintToChat($"🔥 Critical grenade hit! Dealt {total} damage.");
                }
            }

            else
            {
                int normalLevel = wcPlayer.GetAbilityLevel(1);
                if (normalLevel == 0) return;

                int chancePercent = Math.Min(normalLevel * 7, 35);
                int roll = new Random().Next(1, 101);
                Console.WriteLine($"[Crit] Rolled {roll} vs {chancePercent}");

                if (roll <= chancePercent)
                {
                    int bonus = (damageDealt / 4) + (damageDealt / 2);
                    @event.AddBonusDamage(bonus);
                    attacker.PrintToChat($"⚡ Critical hit! Dealt {bonus} bonus damage.");
                }
            }
        }

    }
}