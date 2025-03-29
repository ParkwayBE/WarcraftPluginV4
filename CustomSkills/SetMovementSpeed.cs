// File: CustomSkills/MovementSpeed.cs

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
            Owner.PrintToChat($"[MS] Speed set to {_speedMultiplier}x for {Duration}s");
            var pawn = Owner.PlayerPawn.Value;
            pawn.VelocityModifier = 1f + 0.1f * _speedMultiplier;
        }

        public override void OnFinish()
        {
            Owner.PlayerPawn.Value.VelocityModifier = 1.0f;
            Owner.PrintToChat("[MS] Speed returned to normal.");
        }

        public override void OnTick()
        {/*       */ }
    }
}
