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
                effect.Start();
                _chargeEffects[player.SteamID] = effect;
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

            ChargeWhileMovingEffect effect;
            if (!_chargeEffects.TryGetValue(caster.SteamID, out effect))
                return;


            float radius = 1500f;
            int damage = effect.ChargeStacks;
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

                hitSomething = true;
                SkillFunctions.DealRawDamage(caster, player, damage);
                caster.PrintToChat($" {ChatColors.Green}[Volt Tackle]{ChatColors.Default} Dealt {ChatColors.LightPurple}{damage}{ChatColors.Default} damage to {ChatColors.Yellow}{player.PlayerName}{ChatColors.Default}.");

                var lightningPos = Warcraft.EyePosition(player);
                var particle = Warcraft.SpawnParticle(lightningPos, "particles/ui/status_levels/ui_status_level7_lightning.vpcf", 2.0f);
                particle.SetParent(player.PlayerPawn.Value);
                var raisedPos = pos + new Vector(0, 0, 30);
                Warcraft.SpawnParticle(raisedPos, "particles/generic_fx/fx_electricspark_glow.vpcf", 2f);
                Warcraft.SpawnParticle(casterPos, "particles/explosions_fx/bumpmine_detonate_sparks.vpcf", 2f);
            }

            if (hitSomething)
            {
                StartCooldown(3);
                effect._chargeStacks = 0;
            }
            else
            {
                caster.PrintToCenter($" {ChatColors.Default}⚠️{ChatColors.Default} No valid targets for {ChatColors.LightPurple}Volt Tackle{ChatColors.Default} — Pickachu kept going and crashed.");
                effect._chargeStacks = 0;
                StartCooldown(3);
            }
        }
        internal class ChargeWhileMovingEffect : WarcraftEffect
        {
            private Vector _previousPosition;
            private float _lastChatTime;
            public int _chargeStacks;
            private readonly int _maxCharge = 100;

            public int ChargeStacks => _chargeStacks;

            public ChargeWhileMovingEffect(CCSPlayerController owner)
                : base(owner, duration: float.MaxValue, destroyOnDeath: true, destroyOnRoundEnd: true)
            {
            }

            public override void OnStart()
            {
                if (Owner == null || Owner.PlayerPawn == null || Owner.PlayerPawn.Value == null) return;

                _previousPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();
                Console.WriteLine($"[ChargeSystem] Charging started for {Owner.PlayerName}");
            }

            public override void OnFinish()
            {
                if (Owner != null && Owner.IsValid)
                {
                    Owner.PrintToChat($" {ChatColors.Green}[ChargeSystem]{ChatColors.Default} Stopped charging.");
                }

                _chargeStacks = 0;
            }

            public override void OnTick()
            {
                if (Owner == null || Owner.PlayerPawn == null || Owner.PlayerPawn.Value == null || !Owner.IsAlive()) return;

                var currentPosition = Owner.PlayerPawn.Value.AbsOrigin;

                // ✅ Better movement check: require 2+ units of movement
                Vector diff = currentPosition - _previousPosition;
                float distanceMoved = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;
                bool isMoving = distanceMoved > 4f; // 2 units squared

                if (isMoving && _chargeStacks < _maxCharge)
                {
                    _chargeStacks += 2;
                    if (_chargeStacks > _maxCharge) _chargeStacks = _maxCharge;

                    float now = Server.CurrentTime;
                    if (now - _lastChatTime > 2f)
                    {
                        Owner.PrintToChat($" {ChatColors.Green}[ChargeSystem]{ChatColors.Default} Gained charge: {ChatColors.LightYellow}{_chargeStacks}/100");
                        _lastChatTime = now;
                    }
                }

                _previousPosition = currentPosition;

                // Apply passive buff if charged
                if (_chargeStacks >= 10)
                {
                    int tier = _chargeStacks / 10;
                    float buffMultiplier = tier * 0.1f;
                    var pawn = Owner.PlayerPawn.Value;
                    pawn.VelocityModifier = 1.0f + (buffMultiplier / 2f);
                }
            }

        }


        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            var electricDamage = abilityLevel;
            var victimPos = victim.PlayerPawn.Value.AbsOrigin;
            @event.AddBonusDamage(electricDamage);
            Warcraft.SpawnParticle(victimPos, "particles/ambient_fx/ambient_sparks_core.vpcf", 2f);

            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;
                @event.AddBonusDamage(electricDamage);
                Warcraft.SpawnParticle(victimPos, "particles/ambient_fx/ambient_sparks_core.vpcf", 2f);
            });

            float chance = 0.02f * abilityLevel;
            if (Random.Shared.NextDouble() <= chance)
            {
                if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;
                // Paralyze code needs to be here
                Console.WriteLine($"[Thunderbolt] Paralyzing victim from Thunderbolt.");
            }

            WarcraftPlugin.Instance.AddTimer(3f, () =>
            {
                if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;
                @event.AddBonusDamage(electricDamage);
                Warcraft.SpawnParticle(victimPos, "particles/ambient_fx/ambient_sparks_core.vpcf", 2f);
            });
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