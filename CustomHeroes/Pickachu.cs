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
                if (player.PlayerPawn != null && player.PlayerPawn.Value != null)
                {
                    particle.SetParent(player.PlayerPawn.Value);
                }
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
            private float _lastChatTime;
            public int _chargeStacks;
            private Vector _lastPosition;
            private readonly int _maxCharge = 100;

            public int ChargeStacks => _chargeStacks;

            public ChargeWhileMovingEffect(CCSPlayerController owner)
                : base(owner, duration: 9999f, destroyOnDeath: true, destroyOnRoundEnd: true)
            {
            }

            public override void OnStart()
            {
                if (Owner == null || Owner.PlayerPawn == null || Owner.PlayerPawn.Value == null) return;

                var origin = Owner.PlayerPawn.Value.AbsOrigin.Clone();
                _lastPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();

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
                if (Owner == null || Owner.PlayerPawn == null || !Owner.IsAlive()) return;

                var currentPosition = Owner.PlayerPawn.Value.AbsOrigin;
                var diff = currentPosition - _lastPosition;
                float distanceMoved = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;

                bool isMoving = distanceMoved > 4f;

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

                _lastPosition = currentPosition;

                if (_chargeStacks >= 10)
                {
                    int tier = _chargeStacks / 10;
                    float buffMultiplier = tier * 0.1f;
                    Owner.PlayerPawn.Value.VelocityModifier = 1.0f + (buffMultiplier / 2f);
                }
            }

        }
        internal class ElectricShockEffect : WarcraftEffect
        {
            private readonly CCSPlayerController Attacker;
            private readonly int Damage;
            private int TicksRemaining = 3;

            public ElectricShockEffect(CCSPlayerController attacker, CCSPlayerController victim, int damage)
                : base(victim, duration: 4.5f, destroyOnDeath: true, destroyOnRoundEnd: true)
            {
                Attacker = attacker;
                Damage = damage;
            }

            public override void OnStart()
            {
                Console.WriteLine($"[Thunderbolt] Starting ElectricShockEffect on {Owner.PlayerName}");
                ApplyDamage();
            }

            public override void OnTick()
            {
                TicksRemaining--;
                ApplyDamage();

                if (TicksRemaining <= 0)
                    Destroy();
            }

            public override void OnFinish()
            {
                // needs to be here
                Console.WriteLine($"[Thunderbolt] ElectricShockEffect ended for {Owner?.PlayerName}");
            }

            private void ApplyDamage()
            {
                if (Owner == null || !Owner.IsValid || !Owner.IsAlive()) return;
                if (Attacker == null || !Attacker.IsValid || !Attacker.IsAlive()) return;

                SkillFunctions.DealRawDamage(Attacker, Owner, Damage);

                if (Owner.PlayerPawn?.Value != null)
                {
                    var pos = Owner.PlayerPawn.Value.AbsOrigin;
                    Warcraft.SpawnParticle(pos, "particles/ambient_fx/ambient_sparks_core.vpcf", 2f);
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
            new ElectricShockEffect(attacker, victim, electricDamage).Start();
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