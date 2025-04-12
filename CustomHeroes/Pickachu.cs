using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using g3;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.Classes
{
    public class Pickachu : WarcraftClass
    {
        public override string DisplayName => "Pickachu";
        public override Color DefaultColor => Color.Yellow;
        private Dictionary<ulong, ChargeWhileMovingEffect> _chargeEffects = new();
        private readonly Dictionary<ulong, float> _shockCooldowns = new();




        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Static body", "After getting hit you have a chance to paralyze your attacker. "),
            new WarcraftAbility("Thunderbolt", "Chance to deal bonus electric damage, potentially Paralyzing your target."),
            new WarcraftAbility("Charge", "Moving is Charging, Charge is increasing your movement speed and evasion based on how long you've been Charging."),
            new WarcraftCooldownAbility("Volt Tackle","Consume all Charges to deal damage to nearby players, more Charges equals more damage and range.", 3f, true)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookEvent<EventPlayerPing>(OnPlayerPing);


            HookAbility(3, Ultimate);
        }

        private void OnPlayerPing(EventPlayerPing ping)
        {
            SkillFunctions.HandleTeleportPing(Player, ping.X, ping.Y, ping.Z, maxDistance: 400f);
        }



        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            var player = spawn.Userid;
            if (player == null || !player.IsValid || !player.IsAlive()) return;

            WarcraftPlugin.Instance.AddTimer(0.2f, () =>
            {
                if (player == null || !player.IsValid || !player.IsAlive()) return;

                var effect = new ChargeWhileMovingEffect(player);
                _chargeEffects[player.SteamID] = effect;
                effect.Start();

            });
        }
        private void PlayerDeath(EventPlayerDeath death)
        {
            var player = death.Userid;
            if (player == null || !player.IsValid) return;

            if (_chargeEffects.TryGetValue(player.SteamID, out var effect))
            {
                effect.Destroy();
                _chargeEffects.Remove(player.SteamID);
            }
        }


        private void Ultimate()
        {
            if (WarcraftPlayer.GetAbilityLevel(3) <= 0)
                return;

            var caster = Player;
            if (caster == null || !caster.IsValid || !caster.IsAlive()) return;

            if (!_chargeEffects.TryGetValue(caster.SteamID, out var effect)) return;
            int damage = effect.ChargeStacks;
            int ActualDamage = damage / 2;

            SkillFunctions.TeleportUltimate(caster, 200f);
            caster.PrintToChat($" {ChatColors.LightPurple}⚡ Volt Tackle initiated!");

            WarcraftPlugin.Instance.AddTimer(0.3f, () =>
            {
                var casterPos = caster.PlayerPawn?.Value?.AbsOrigin;
                if (casterPos == null || !caster.IsValid || !caster.IsAlive()) return;

                float radius = 200f;
                bool hitSomething = false;
                HashSet<ulong> alreadyHit = new();

                foreach (var player in Utilities.GetPlayers())
                {
                    if (player == null || !player.IsValid || !player.IsAlive() || player.PlayerPawn?.Value == null)
                        continue;

                    if (player == caster || player.TeamNum == caster.TeamNum)
                        continue;

                    var pos = player.PlayerPawn.Value.AbsOrigin;
                    var diff = pos - casterPos;
                    float distSq = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;

                    if (distSq > radius * radius)
                        continue;

                    var wcTarget = player.GetWarcraftPlayer();
                    if (wcTarget != null && wcTarget.HasUltimateImmunity)
                    {
                        caster.PrintToCenter($" {ChatColors.Red}⛔{ChatColors.Default} Target has {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default}!");
                        player.PrintToCenter($" {ChatColors.Green}🛡️{ChatColors.Default} Your {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default} blocked {ChatColors.LightPurple}Volt Tackle{ChatColors.Default}!");
                        continue;
                    }

                    if (alreadyHit.Contains(player.SteamID))
                        continue;
                    alreadyHit.Add(player.SteamID);


                    hitSomething = true;
                    SkillFunctions.DealRawDamage(caster, player, ActualDamage);
                    caster.PrintToChat($" {ChatColors.Green}[Volt Tackle]{ChatColors.Default} Dealt {ChatColors.LightPurple}{damage}{ChatColors.Default} damage to {ChatColors.Yellow}{player.PlayerName}{ChatColors.Default}.");

                    var lightningPos = Warcraft.EyePosition(player);
                    var particle = Warcraft.SpawnParticle(lightningPos, "particles/ui/status_levels/ui_status_level7_lightning.vpcf", 2.0f);
                    if (player.PlayerPawn?.Value != null)
                    {
                        particle.SetParent(player.PlayerPawn.Value);
                    }

                    var raisedPos = pos + new Vector(0, 0, 30);
                    Warcraft.SpawnParticle(raisedPos, "particles/generic_fx/fx_electricspark_glow.vpcf", 2f);
                    Warcraft.SpawnParticle(casterPos, "particles/explosions_fx/bumpmine_detonate_sparks.vpcf", 2f);
                }

                if (!hitSomething)
                {
                    int CrashDamage = (int)(30 + (damage / 100f) * 70);
                    caster.PrintToChat($" {ChatColors.Red}⚠️ No targets hit! You shocked yourself for {CrashDamage}!");
                    SkillFunctions.DealRawDamage(caster, caster, CrashDamage);
                    effect._chargeStacks = 0;
                    StartCooldown(3);
                }

                effect._chargeStacks = 0;
                StartCooldown(3);
            });
        }

        internal class ChargeWhileMovingEffect : WarcraftEffect
        {
            private float _lastChatTime;
            public int _chargeStacks;
            private Vector _lastPosition;
            private readonly int _maxCharge = 100;

            public int ChargeStacks => _chargeStacks;

            public ChargeWhileMovingEffect(CCSPlayerController owner)
                : base(owner, duration: 9999f, destroyOnDeath: true, destroyOnRoundEnd: true, onTickInterval: 1f)
            {
            }

            private void CheckForMovementAndAddCharge()
            {
                if (Owner == null || Owner.PlayerPawn?.Value == null || !Owner.IsAlive())
                    return;

                var currentPosition = CopyPosition(Owner.PlayerPawn.Value.AbsOrigin);
                var diff = currentPosition - _lastPosition;
                float movedDist = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;

                bool isMoving = movedDist > 4f;
                if (!isMoving)
                {
                    _lastPosition = currentPosition;
                    return;
                }

                _chargeStacks = Math.Min(_chargeStacks + 2, _maxCharge);

                float now = Server.CurrentTime;
                if (now - _lastChatTime > 2f)
                {
                    Owner.PrintToCenter($"Charge: {_chargeStacks}/100");
                    _lastChatTime = now;
                }

                int tier = Math.Min(_chargeStacks / 10, 10);
                float buffMultiplier = tier * 0.1f;
                float newSpeed = 1.0f + (buffMultiplier / 2f);
                Owner.PlayerPawn.Value.VelocityModifier = Math.Min(newSpeed, 1.6f);


                _lastPosition = currentPosition;
            }

            public override void OnStart()
            {
                if (Owner?.PlayerPawn?.Value == null) return;
                _lastPosition = CopyPosition(Owner.PlayerPawn.Value.AbsOrigin);
            }

            public override void OnTick()
            {
                try
                {
                    CheckForMovementAndAddCharge();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChargeSystem] OnTick crashed: {ex.Message}");
                }
            }

            public override void OnFinish()
            {
                if (Owner?.IsValid == true)
                {
                    Owner.PrintToChat($" {ChatColors.Green}[ChargeSystem]{ChatColors.Default} Stopped charging.");
                }
                _chargeStacks = 0;
            }

            private Vector CopyPosition(Vector pos)
            {
                return new Vector(pos.X, pos.Y, pos.Z);
            }
        }


        internal class ElectricShockEffect : WarcraftEffect
        {
            private readonly CCSPlayerController _target;
            private readonly CCSPlayerController _attacker;
            private readonly int _ticks;
            private readonly int _damagePerTick;
            private int _currentTick;

            public ElectricShockEffect(CCSPlayerController attacker, CCSPlayerController target, int ticks, int damagePerTick)
                : base(attacker, ticks * 0.8f, onTickInterval: 1.1f)
            {
                _attacker = attacker;
                _target = target;
                _ticks = ticks;
                _damagePerTick = damagePerTick;
            }

            public override void OnStart()
            {
                SpawnElectricEffect(_target);
            }

            public override void OnTick()
            {
                if (_target == null || !_target.IsAlive() || _currentTick >= _ticks) return;

                _target.TakeDamage(_damagePerTick, _attacker);
                SpawnElectricEffect(_target);
                _currentTick++;
            }

            public override void OnFinish()
            {
            }

            private void SpawnElectricEffect(CCSPlayerController player)
            {
                var pos = player.PlayerPawn?.Value?.AbsOrigin;
                if (pos != null)
                {
                    Warcraft.SpawnParticle(pos, "particles/ambient_fx/ambient_sparks_core.vpcf", 2f);
                }
            }
        }


        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive())
                return;

            if (attacker.TeamNum == victim.TeamNum)
                return;

            int level = WarcraftPlayer.GetAbilityLevel(1);
            if (level <= 0)
                return;

            float currentTime = Server.CurrentTime;
            if (_shockCooldowns.TryGetValue(victim.SteamID, out var lastShockTime))
            {
                if (currentTime - lastShockTime < 1f)
                    return;
            }

            _shockCooldowns[victim.SteamID] = currentTime;

            int ticks = 3 + (level / 2);
            int damagePerTick = Math.Max(1, level);

            float shockChance = 0.25f * (level / 5f);
            if (Random.Shared.NextDouble() <= shockChance)
            {
                new ElectricShockEffect(attacker, victim, ticks, damagePerTick).Start();
            }

            float selfZapChance = 0.12f * (level / 5f);
            if (Random.Shared.NextDouble() <= selfZapChance)
            {
                new FireDelayAndFreezeEffect(victim, duration: 1.0f, fireDelaySeconds: 1.0f, debugPrint: true).Start();
                victim.PrintToChat($" {ChatColors.Red} You got paralyzed by {attacker.PlayerName}");
                attacker.PrintToChat($" {ChatColors.Green} You {ChatColors.Default}{ChatColors.LightPurple}paralyzed{ChatColors.Default}{ChatColors.Green} {victim.PlayerName}{ChatColors.Default}"); // 

            }
        }


        private void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if (attacker == null || victim == null || !attacker.IsValid || !victim.IsValid) return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel <= 0) return;

            int level = WarcraftPlayer.GetAbilityLevel(1);
            if (level <= 0) return;

            float selfZapChance = 0.12f * (level / 5f);

            if (Random.Shared.NextDouble() <= selfZapChance)
            {
                new FireDelayAndFreezeEffect(attacker, duration: 1.0f, fireDelaySeconds: 1.0f, debugPrint: true).Start();
                attacker.PrintToChat($" {ChatColors.Red} You got paralyzed by {victim.PlayerName}");
                attacker.PrintToChat($" {ChatColors.Green} You {ChatColors.Default}{ChatColors.LightPurple}paralyzed{ChatColors.Default}{ChatColors.Green} {victim.PlayerName}{ChatColors.Default}");

            }

        }



        internal class FireDelayAndFreezeEffect : WarcraftEffect
        {
            private readonly float _fireDelaySeconds;
            private readonly bool _debugPrint;
            private int _originalFireTick = -1;

            public FireDelayAndFreezeEffect(CCSPlayerController owner, float duration = 2.0f, float fireDelaySeconds = 2.0f, bool debugPrint = false)
                : base(owner, duration, destroyOnDeath: true, destroyOnRoundEnd: true, onTickInterval: 0.5f)
            {
                _fireDelaySeconds = fireDelaySeconds;
                _debugPrint = debugPrint;
            }

            public override void OnStart()
            {
                if (Owner?.PlayerPawn?.Value == null || !Owner.IsValid || !Owner.IsAlive()) return;

                Owner.DisableMovement();

                var weapon = Owner.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value;
                if (weapon != null)
                {
                    int currentTick = Server.TickCount;
                    int delayTicks = (int)(_fireDelaySeconds * 20); // ~20 ticks/sec
                    _originalFireTick = weapon.NextPrimaryAttackTick;

                    weapon.NextPrimaryAttackTick = currentTick + delayTicks;

                    if (_debugPrint)
                    {
                        Owner.PrintToChat($" {ChatColors.Red}[FireDelay] 🔥 Weapon delay applied for {_fireDelaySeconds:F1}s");
                    }
                }
            }

            public override void OnFinish()
            {
                if (Owner?.PlayerPawn?.Value == null || !Owner.IsValid) return;

                Owner.EnableMovement();

                if (_debugPrint)
                {
                    Owner.PrintToChat($" {ChatColors.Green}[FireDelay] ✅ Movement re-enabled");
                }
            }

            public override void OnTick()
            {
                Owner.PrintToChat("OnTick Called for effect");
                Warcraft.SpawnParticle(Owner.PlayerPawn.Value.AbsOrigin, "particles/screen_fx/ghost_screenglow.vpcf", 2f);
                Warcraft.SpawnParticle(Owner.PlayerPawn.Value.AbsOrigin, "particles/screen_fx/ghost_screenglow_warp_loop.vpcf", 2f);

            }
        }





        public static List<Vector3d> CreateSphereAroundPoint(Vector point, double radius, int numLatitudeSegments = 10, int numLongitudeSegments = 10)
        {
            var vertices = new List<Vector3d>();

            for (int lat = 0; lat <= numLatitudeSegments; lat++)
            {
                double theta = lat * Math.PI / numLatitudeSegments;
                double sinTheta = Math.Sin(theta);
                double cosTheta = Math.Cos(theta);

                for (int lon = 0; lon <= numLongitudeSegments; lon++)
                {
                    double phi = lon * 2 * Math.PI / numLongitudeSegments;
                    double sinPhi = Math.Sin(phi);
                    double cosPhi = Math.Cos(phi);

                    double x = cosPhi * sinTheta;
                    double y = cosTheta;
                    double z = sinPhi * sinTheta;

                    vertices.Add(new Vector3d(x * radius, y * radius, z * radius) + point.ToVector3d());
                }
            }

            return vertices;
        }

        public static void DrawLaserSphere(Vector center, float radius, float duration = 3f)
        {
            var points = CreateSphereAroundPoint(center, radius);

            int latSegments = 10;
            int lonSegments = 10;

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int index1 = lat * (lonSegments + 1) + lon;
                    int index2 = index1 + 1;
                    int index3 = index1 + (lonSegments + 1);

                    if (index2 < points.Count)
                    {
                        Warcraft.DrawLaserBetween(points[index1].ToVector(), points[index2].ToVector(), Color.Yellow, duration, 1.2f);
                    }

                    if (index3 < points.Count)
                    {
                        Warcraft.DrawLaserBetween(points[index1].ToVector(), points[index3].ToVector(), Color.Yellow, duration, 1.2f);
                    }
                }
            }
        }

    }
}