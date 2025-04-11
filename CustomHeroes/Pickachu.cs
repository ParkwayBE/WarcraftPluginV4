using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
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


        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Static body", "After getting hit you have a chance to paralyze your attacker. "),
            new WarcraftAbility("Thunderbolt", "Chance to deal bonus electric damage, potentially Paralyzing your target."),
            new WarcraftAbility("Charge", "Moving is Charging, Charge is increasing your movement speed and evasion based on how long you've been Charging."),
            new WarcraftCooldownAbility("Volt Tackle","Consume all Charges to deal damage to nearby players, more Charges equals more damage and range.", 1f, true)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            HookEvent<EventPlayerDeath>(PlayerDeath);

            HookAbility(3, Ultimate);
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
                effect.Start(); // ✅ now it's tracked before starting

            });
        }
        private void PlayerDeath(EventPlayerDeath death)
        {
            var player = death.Userid;
            if (player == null || !player.IsValid) return;

            if (_chargeEffects.TryGetValue(player.SteamID, out var effect))
            {
                effect.Destroy(); // ✅ cleanly stops OnTick
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

            // Begin Volt Tackle — TELEPORT first
            SkillFunctions.TeleportUltimate(caster);
            caster.PrintToChat($" {ChatColors.LightPurple}⚡ Volt Tackle initiated!");

            // After teleport, do the AoE check (short delay to ensure teleport finished)
            WarcraftPlugin.Instance.AddTimer(0.3f, () =>
            {
                var casterPos = caster.PlayerPawn?.Value?.AbsOrigin;
                if (casterPos == null || !caster.IsValid || !caster.IsAlive()) return;

                float radius = 1500f;
                bool hitSomething = false;

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

                    // ✅ Hit confirmed
                    hitSomething = true;
                    SkillFunctions.DealRawDamage(caster, player, damage);
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

                // No targets? Deal damage to self!
                if (!hitSomething)
                {
                    caster.PrintToChat($" {ChatColors.Red}⚠️ No targets hit! You shocked yourself for {damage}!");
                    SkillFunctions.DealRawDamage(caster, caster, damage);
                }

                // Always clear charges and trigger cooldown
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
                    Owner.PrintToCenter($" {ChatColors.Green}[ChargeSystem]{ChatColors.Default} Charge: {ChatColors.LightYellow}{_chargeStacks}/100");
                    _lastChatTime = now;
                }

                int tier = Math.Min(_chargeStacks / 10, 10);
                float buffMultiplier = tier * 0.1f;
                Owner.PlayerPawn.Value.VelocityModifier = 1.0f + (buffMultiplier / 2f);

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
                : base(attacker, ticks * 0.8f, onTickInterval: 0.8f) // Slightly slower than Bleed
            {
                _attacker = attacker;
                _target = target;
                _ticks = ticks;
                _damagePerTick = damagePerTick;
            }

            public override void OnStart()
            {
                Console.WriteLine($"[Thunderbolt] ElectricShockEffect STARTED on {_target?.PlayerName}");
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
                Console.WriteLine($"[Thunderbolt] ElectricShockEffect ENDED on {_target?.PlayerName}");
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

            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;

            int level = WarcraftPlayer.GetAbilityLevel(1);
            if (level <= 0) return;

            int ticks = 3 + (level / 2); // scale with level
            int damagePerTick = Math.Max(1, level); // avoid 0

            new ElectricShockEffect(attacker, victim, ticks, damagePerTick).Start();
        }


        private void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if (attacker == null || victim == null || !attacker.IsValid || !victim.IsValid) return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel <= 0) return;

            // Defensive paralyze code here Static body
        }
    }
}