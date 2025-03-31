// File: CustomSkills/MovementSpeed.cs

using System;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;

namespace WarcraftPlugin.CustomSkills
{
    public class SetMovementSpeed : WarcraftEffect
    {
        private readonly float _speedMultiplier;

        public SetMovementSpeed(CCSPlayerController owner, float speedMultiplier, float duration)
            : base(owner, duration)
        {
            _speedMultiplier = speedMultiplier;
        }

        public override void OnStart()
        {
            var pawn = Owner.PlayerPawn.Value;
            pawn.VelocityModifier = 1.0f;

            float speed = 1f + 0.1f * _speedMultiplier;
            pawn.VelocityModifier = Math.Clamp(speed, 1f, 2.5f);

        }

        public override void OnFinish()
        {
            Owner.PlayerPawn.Value.VelocityModifier = 1.0f;
        }

        public override void OnTick()
        {/*       */ }
    }
}
