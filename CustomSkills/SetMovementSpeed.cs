using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// File: CustomSkills/MovementSpeed.cs

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Core.Effects;

namespace WarcraftPlugin.CustomSkills
{
    public class SetMovementSpeed : WarcraftEffect
    {
        private readonly float _speedMultiplier;

        public SetMovementSpeed(CCSPlayerController owner, float duration, float speedMultiplier)
            : base(owner, duration)
        {
            _speedMultiplier = speedMultiplier;
        }

        public override void OnStart()
        {
            WarcraftPlugin.Instance.AddTimer(0.5f, () =>
            {
                Owner.PrintToChat($"[TEST] Speed set to {_speedMultiplier}x for {Duration}s");
                var pawn = Owner.PlayerPawn.Value;
                pawn.VelocityModifier = 1f + 0.1f * _speedMultiplier;
            });
            

        }

        public override void OnFinish()
        {
            Owner.PlayerPawn.Value.VelocityModifier = 1.0f;
            Owner.PrintToChat("[TEST] Speed returned to normal.");
        }

        public override void OnTick()
        {/*       */ }
    }

    public static class SkillFunctions
    {
        public static void MovementSpeed(CCSPlayerController player, float amount, float duration)
        {
            new SetMovementSpeed(player, duration, amount).Start();
        }
    }
}
