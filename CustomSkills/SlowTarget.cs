using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Helpers;
using System;
using WarcraftPlugin.Core.Preload;
using Microsoft.Extensions.Hosting;

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
            _target.PrintToChat(" \x07[Freeze] You are slowed!");
            var pawn = _target.PlayerPawn.Value;

            // Half their speed
            _originalSpeed = _target.PlayerPawn.Value.VelocityModifier;
            pawn.VelocityModifier = pawn.VelocityModifier / 2;
            pawn.SetColor(Color.BlueViolet);
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 100), "particles/generic_fx/fx_electricspark_longtrail.vpcf", 0.4f);
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
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 50), "particles/generic_fx/fx_electricspark_longtrail.vpcf", 0.4f);
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/generic_fx/fx_electricspark_longtrail.vpcf", 0.4f);
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 100), "particles/generic_fx/fx_electricspark_longtrail.vpcf", 0.4f);
        }
    }
}