using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CounterStrikeSharp.API.Modules.Memory;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using System.Drawing;
using WarcraftPlugin.Core.Effects;
using System.Collections.Generic;
using WarcraftPlugin.Events.ExtendedEvents;
using System;
using System.Reflection;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using WarcraftPlugin.Core;
using WarcraftPlugin.Summons;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CounterStrikeSharp.API.Modules.Timers;
using System.Reflection.Emit;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Numerics;
using WarcraftPlugin.CustomSkills;
using CounterStrikeSharp.API.Modules.Commands;

namespace WarcraftPlugin.Classes
{
    public class Pickachu : WarcraftClass
    {
        public override string DisplayName => "Pickachu";
        public override Color DefaultColor => Color.Yellow;
        private Dictionary<ulong, ChargeWhileMovingEffect> _chargeEffects = new();
        private static readonly Dictionary<ulong, ParalyzeEffect> _activeParalyzeEffects = new();


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
            if (Player == null || !Player.IsAlive())
                return;
            var Playerpos = Player.PlayerPawn.Value.AbsOrigin;
            if (!_chargeEffects.TryGetValue(Player.SteamID, out var effect))
                return;

            int stackDamage = effect.ChargeStacks;
            float radius = 15 * effect.ChargeStacks;

            foreach (var target in Utilities.GetPlayers())
            {
                if (!target.IsAlive() || target == Player || target.TeamNum == Player.TeamNum)
                    continue;

                var wcTarget = target.GetWarcraftPlayer();
                if (wcTarget == null)
                {
                    Console.WriteLine($"[VoltTackle] Skipping target {target.PlayerName} (no WarcraftPlayer)");
                    continue;
                }

                if (wcTarget.HasUltimateImmunity)
                {
                    Player.PrintToCenter($" {ChatColors.Red}⛔{ChatColors.Default} Target has {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default}!");
                    target.PrintToCenter($" {ChatColors.Green}🛡️{ChatColors.Default} Your {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default} blocked {ChatColors.LightPurple}Volt Tackle{ChatColors.Default}!");
                    continue;
                }

                var pos = target.PlayerPawn.Value.AbsOrigin;
                var self = Player.PlayerPawn.Value.AbsOrigin;

                float dist = (pos - self).Length();
                if (dist > radius)
                    continue;

                // Apply damage and effects
                SkillFunctions.DealRawDamage(Player, wcTarget.Player, stackDamage);
                Warcraft.SpawnParticle(pos, "particles/generic_fx/fx_electricspark_glow.vpcf", 2f);
            }

            effect._chargeStacks = 0;
            Warcraft.SpawnParticle(Playerpos, "particles/explosions_fx/bumpmine_detonate_sparks.vpcf", 2f);

            StartCooldown(3);
        }

        internal class ChargeWhileMovingEffect : WarcraftEffect
        {
            private Vector _previousPosition;
            private Timer? _chargeTimer;
            public int _chargeStacks;
            private readonly int _maxCharge = 100;

            public int ChargeStacks => _chargeStacks;

            public ChargeWhileMovingEffect(CCSPlayerController owner)
                : base(owner, duration: float.MaxValue, destroyOnDeath: true, destroyOnRoundEnd: true)
            {

            }

            public override void OnStart()
            {
                Console.WriteLine("[ChargeSystem] Started charging while moving.");
                if (Owner == null) return;
                _previousPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();
                _chargeTimer = WarcraftPlugin.Instance.AddTimer(1.0f, () =>
                {
                    var currentPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();
                    bool isMoving = !_previousPosition.Equals(currentPosition);

                    if (isMoving && _chargeStacks < _maxCharge)
                    {
                        _chargeStacks += 2;
                        if (_chargeStacks > _maxCharge) _chargeStacks = _maxCharge;
                        Owner.PrintToChat($" {ChatColors.Green}[ChargeSystem]{ChatColors.Default} Gained charge: {ChatColors.LightYellow}{_chargeStacks}/100");
                    }
                    _previousPosition = currentPosition;
                }, TimerFlags.REPEAT);

            }

            public override void OnFinish()
            {
                Owner.PrintToChat($" {ChatColors.Green}[ChargeSystem]{ChatColors.Default} Stopped charging.");
                _chargeTimer?.Kill();
                _chargeStacks = 0;
            }

            public override void OnTick()
            {
                if (Owner == null) return;
                if (_chargeStacks < 10) return;

                int tier = _chargeStacks / 10;
                float buffMultiplier = tier * 0.1f;
                ApplyChargeBuff(buffMultiplier);
            }

            private void ApplyChargeBuff(float multiplier)
            {
                // Example: modify movement speed
                var pawn = Owner.PlayerPawn.Value;
                if (Owner == null) return;
                pawn.VelocityModifier = 1.0f + (multiplier / 2f);
                Owner.PrintToChat($"[ChargeSystem] Buff active: +{(int)(multiplier * 100)}%");
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
                if (victim == null) return;
                @event.AddBonusDamage(electricDamage);
                Warcraft.SpawnParticle(victimPos, "particles/ambient_fx/ambient_sparks_core.vpcf", 2f);
            });

            float chance = 0.02f * abilityLevel;
            if (Random.Shared.NextDouble() <= chance)
            {
                new ParalyzeEffect(victim, 1.0f).Start();
                Console.WriteLine($"[Thunderbolt] Paralyzing victim from Thunderbolt.");
            }

            WarcraftPlugin.Instance.AddTimer(3f, () =>
            {
                if (victim == null) return;
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

            float chance = (0.1f * abilityLevel) / 2f;
            if (Random.Shared.NextDouble() <= chance)
            {
                new ParalyzeEffect(attacker, 0.5f).Start();
                Console.WriteLine($"[StaticBody] Paralyzing attacker for hitting Pickachu.");
            }
        }



        public class ParalyzeEffect : WarcraftEffect
        {
            private Timer? _fireDelayTimer;
            public ParalyzeEffect(CCSPlayerController owner, float duration = 2.0f)
                : base(owner, duration, destroyOnDeath: false, destroyOnRoundEnd: false)
            {
            }

            public override void OnStart()
            {
                if (Owner == null || Owner.PlayerPawn == null || !Owner.IsValid) return;

                ulong steamId = Owner.SteamID;
                if (_activeParalyzeEffects.ContainsKey(steamId))
                {
                    Console.WriteLine($"[ParalyzeEffect] Skipping: {Owner.PlayerName} already has an active ParalyzeEffect.");
                    Destroy();
                    return;
                }

                _activeParalyzeEffects[steamId] = this;

                FreezeInput(Owner, true);
                Owner.PrintToCenter($"{ChatColors.Red}⚡ Paralyzed! ⚡");
                Warcraft.SpawnParticle(Owner.PlayerPawn.Value.AbsOrigin, "particles/ambient_fx/ambient_sparks_core.vpcf", 2f);

                _fireDelayTimer = WarcraftPlugin.Instance.AddTimer(0.25f, () =>
                {
                    if (Owner == null || Owner.PlayerPawn == null || !Owner.IsValid) return;

                    var weapon = Owner.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value;
                    if (weapon != null)
                    {
                        int currentTick = Server.TickCount;
                        int durationTicks = (int)(Duration * 20);

                        if (weapon.NextPrimaryAttackTick <= currentTick)
                        {
                            weapon.NextPrimaryAttackTick = currentTick + durationTicks;
                        }
                    }
                }, TimerFlags.REPEAT);
            }
            public override void OnTick()
            {
                // needs to be here
            }

            public override void OnFinish()
            {
                if (Owner == null || Owner.PlayerPawn == null || !Owner.IsValid) return;

                FreezeInput(Owner, false);
                _fireDelayTimer?.Kill();
                _activeParalyzeEffects.Remove(Owner.SteamID);
            }
        }

        private static void FreezeInput(CCSPlayerController player, bool freeze)
        {
            if (player == null || !player.IsValid || player.PlayerPawn == null) return;

            if (freeze)
            {
                player.DisableMovement();
            }
            else
            {
                player.EnableMovement();
            }
        }

    }
}