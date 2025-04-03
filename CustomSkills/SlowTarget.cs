using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Helpers;

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
            var victimPawn = _target.PlayerPawn.Value;

            // Half their speed
            _originalSpeed = _target.PlayerPawn.Value.VelocityModifier;
            victimPawn.VelocityModifier = victimPawn.VelocityModifier / 2;
            victimPawn.SetColor(Color.BlueViolet);
        }


        public override void OnFinish()
        {
            _target.PlayerPawn.Value.SetColor(Color.White);
            _target.PlayerPawn.Value.VelocityModifier = _originalSpeed;
        }

        public override void OnTick()
        {
            var victimPawn = _target.PlayerPawn.Value;
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/ambient_fx/snow_blizzard.vpcf", 0.4f);
        }
    }
}