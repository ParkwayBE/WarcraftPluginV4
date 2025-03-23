using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Helpers;
using System;
using WarcraftPlugin.Core.Preload;

namespace WarcraftPlugin.CustomSkills
{
    internal class SlowTarget : WarcraftEffect
    {
        private readonly CCSPlayerController _target;
        private float _originalSpeed;

        public SlowTarget(CCSPlayerController owner, float duration, CCSPlayerController target)
            : base(owner, duration)
        {
            _target = target;
        }

        public override void OnStart()
        {
            _target.PrintToChat(" \x07[Freeze] You are frozen!");
            var pawn = _target.PlayerPawn.Value;

            // Half their speed
            _originalSpeed = _target.PlayerPawn.Value.VelocityModifier;
            pawn.VelocityModifier = pawn.VelocityModifier / 2;
            pawn.SetColor(Color.BlueViolet);

            // Draw laser for debug/visual
            Warcraft.DrawLaserBetween(Owner.EyePosition(-10), _target.EyePosition(-10), Color.Cyan);
        }

        public override void OnFinish()
        {
            _target.PlayerPawn.Value.SetColor(Color.White);
            _target.PlayerPawn.Value.VelocityModifier = _originalSpeed;
            _target.PrintToChat(" \x07[Freeze] You are no longer frozen.");
        }

        public override void OnTick() 
        {
            var victimPawn = _target.PlayerPawn.Value;
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 50), "particles/environment/water_drip_area_01_small.vpcf", 0.4f);
        }
    }
}