using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Helpers;

namespace WarcraftPlugin.CustomSkills
{
    internal class FreezePlayerEffect : WarcraftEffect
    {
        private readonly CCSPlayerController _target;

        public FreezePlayerEffect(CCSPlayerController owner, float duration, CCSPlayerController target)
            : base(owner, duration)
        {
            _target = target;
        }

        public override void OnStart()
        {
            _target.PrintToChat(" \x07[Freeze] You are frozen!");
            var pawn = _target.PlayerPawn.Value;
            pawn.SetColor(Color.Cyan);

            // Draw laser for debug/visual
            Warcraft.DrawLaserBetween(Owner.EyePosition(-10), _target.EyePosition(-10), Color.Cyan);
            _target.DisableMovement();
        }

        public override void OnFinish()
        {
            _target.PlayerPawn.Value.SetColor(Color.White);
            _target.PrintToChat(" \x07[Freeze] You are no longer frozen.");
            _target.EnableMovement();
        }

        public override void OnTick() { }
    }
}