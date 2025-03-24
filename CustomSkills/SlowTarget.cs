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
            _target.PrintToChat(" \x07[Slow] You are slowed!");
            var pawn = _target.PlayerPawn.Value;

            _originalSpeed = pawn.VelocityModifier;

            float newSpeed = Math.Max(_originalSpeed * 0.5f, 0.4f);

            pawn.VelocityModifier = newSpeed;
            pawn.SetColor(Color.BlueViolet);
        }


        public override void OnFinish()
        {
            _target.PlayerPawn.Value.SetColor(Color.White);
            _target.PlayerPawn.Value.VelocityModifier = _originalSpeed;
            _target.PrintToChat(" \x07[Freeze] You are no longer slowed.");
        }

        public override void OnTick() 
        {
            var victimPawn = _target.PlayerPawn.Value;
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 50), "particles/environment/water_drip_area_01_small.vpcf", 0.4f);
        }
    }
}