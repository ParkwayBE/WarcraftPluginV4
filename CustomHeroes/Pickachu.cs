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

namespace WarcraftPlugin.Classes
{
    public class Pickachu : WarcraftClass
    {
        public override string DisplayName => "Pickachu";
        public override Color DefaultColor => Color.Yellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Static body", "After getting hit you have a chance to paralyze your attacker. "),
            new WarcraftAbility("Thunderbolt", "Chance to deal bonus electric damage, potentially Paralyzing your target."),
            new WarcraftAbility("Charge", "Moving is Charging, Charge is increasing your movement speed and evasion based on how long you've been Charging."),
            new WarcraftCooldownAbility("Volt Tackle","Consume all Charges to deal damage to nearby players, more Charges equals more damage and range.", 1f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            // TODO: Charge : Gain charges while moving, granting movement speed and Evasion based on how many charges
        }
        private void Ultimate()
        {
            // TODO: Volt Tackle: Consume all charges to deal electrical damage to all nearby players
            // More charges = bigger range and bigger damage

            StartCooldown(3); // Index 3 = Ultimate
        }

        internal class ChargeWhileMovingEffect : WarcraftEffect
        {
            private Vector _previousPosition;
            private Timer? _chargeTimer;
            private int _chargeStacks;
            private readonly int _maxCharge = 100;

            public int ChargeStacks => _chargeStacks;

            public ChargeWhileMovingEffect(CCSPlayerController owner)
                : base(owner, duration: float.MaxValue, destroyOnDeath: true, destroyOnRoundEnd: true)
            {
            }

            public override void OnStart()
            {
                Console.WriteLine("[ChargeSystem] Started charging while moving.");

                _previousPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();

                _chargeTimer = WarcraftPlugin.Instance.AddTimer(1.0f, () =>
                {
                    var currentPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();

                    bool isMoving = !_previousPosition.Equals(currentPosition);

                    if (isMoving)
                    {
                        if (_chargeStacks < _maxCharge)
                        {
                            _chargeStacks += 5; // Gain 5 stacks per second
                            if (_chargeStacks > _maxCharge)
                                _chargeStacks = _maxCharge;

                            Console.WriteLine($"[ChargeSystem] Gained charge: {_chargeStacks}/100");
                        }
                    }

                    _previousPosition = currentPosition;
                }, TimerFlags.REPEAT);
            }

            public override void OnFinish()
            {
                Console.WriteLine("[ChargeSystem] Stopped charging.");
                _chargeTimer?.Kill();
                _chargeStacks = 0;
            }

            public override void OnTick()
            {
                if (_chargeStacks < 10) return;

                int tier = _chargeStacks / 10; // ranges: 10–19 = 1, 20–29 = 2, ..., 90–100 = 9 or 10
                float buffMultiplier = tier * 0.1f; // 0.1 → 0.2 → ... → 1.0

                // Apply the buff once per tick (you can cap max if needed)
                ApplyChargeBuff(buffMultiplier);
            }

            private void ApplyChargeBuff(float multiplier)
            {
                // Example: modify movement speed
                var pawn = Owner.PlayerPawn.Value;
                pawn.VelocityModifier = 1.0f + (multiplier / 2f);

                // Example: increase damage (you'd hook this into your damage dealing logic elsewhere)
                Console.WriteLine($"[ChargeSystem] Buff active: +{(int)(multiplier * 100)}%");
            }

        }




        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO:  Thunderbolt  : Deal additional electrical damage and chance to paralyze target
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            var electricDamage = abilityLevel;
            @event.AddBonusDamage(electricDamage);
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                @event.AddBonusDamage(electricDamage);
            });
            WarcraftPlugin.Instance.AddTimer(3f, () =>
            {
                @event.AddBonusDamage(electricDamage);
                var victimPos = victim.PlayerPawn.Value.AbsOrigin;
                Warcraft.SpawnParticle(victimPos, "particles/ui/hud/ui_transitions_tests_lin_a.vpcf", 2f); // TODO: Add electric effect on victim

            });


        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            // TODO:  Static body : Chance to paralyze your attacker
        }

    }
}