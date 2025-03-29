using System;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;

namespace WarcraftPlugin.CustomSkills
{
    internal class SetGravityEffect : WarcraftEffect
    {
        private readonly float _gravity;

        public SetGravityEffect(CCSPlayerController owner, float gravityPercent, float duration)
            : base(owner, duration)
        {
            // Convert percentage to scale. Example: 80% = 0.8f
            _gravity = gravityPercent / 100f;
        }

        public override void OnStart()
        {
            if (Owner?.PlayerPawn?.Value == null)
            {
                Console.WriteLine("ERROR: Owner or PlayerPawn is NULL in SetGravityEffect OnStart!");
                return;
            }

            Owner.PlayerPawn.Value.GravityScale = _gravity;
        }

        public override void OnFinish()
        {
            if (Owner?.PlayerPawn?.Value == null)
            {
                Console.WriteLine("ERROR: Owner or PlayerPawn is NULL in SetGravityEffect OnFinish!");
                return;
            }

            Owner.PlayerPawn.Value.GravityScale = 1.0f; // Reset to normal gravity
        }

        public override void OnTick() { }
    }
}