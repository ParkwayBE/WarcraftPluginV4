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
            var victimPawn = _target.PlayerPawn.Value;

            // Half their speed
            _originalSpeed = _target.PlayerPawn.Value.VelocityModifier;
            victimPawn.VelocityModifier = victimPawn.VelocityModifier / 2;
            victimPawn.SetColor(Color.BlueViolet);

            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/weapons/cs_weapon_fx/weapon_sensorgren_glowring.vpcf", 0.4f);
                WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                {
                    Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/weapons/cs_weapon_fx/weapon_molotov_fp_wick.vpcf", 0.4f); 
                    WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                    {
                        Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/water_impact/water_splash_01_ripple_rings_secondary.vpcf", 0.4f); 
                        WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                        {
                            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/water_impact/water_impact_bubbles_1.vpcf", 0.4f);
                            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                            {
                                Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/water_fx/waterfall_anubis.vpcf", 0.4f); 
                                WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                {
                                    Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/water_fx/water_wake_fast_ripple_sides.vpcf", 0.4f);
                                    WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                    {
                                        Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/water_fx/water_wake_fast_ripple_rings_secondary.vpcf", 0.4f); 
                                        WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                        {
                                            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/water_fx/water_wake_slow.vpcf", 0.4f); 
                                            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                            {
                                                Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/unified_weapon_fx/weapon_muzzleflash_basic_spark.vpcf", 0.4f);
                                                WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                                {
                                                    Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/unified_weapon_fx/uweapon_muzzleflash_subm_spark.vpcf", 0.4f); 
                                                });
                                            });
                                        });
                                    });
                                });
                            });
                        });
                    });
                });
            });
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
            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/ambient_fx/snow_blizzard.vpcf", 0.4f);
        }
    }
}