using System;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Hosting;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Core.Preload;
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
            _target.PrintToChat(" \x07[Freeze] You are slowed!");
            var victimPawn = _target.PlayerPawn.Value;

            // Half their speed
            _originalSpeed = _target.PlayerPawn.Value.VelocityModifier;
            victimPawn.VelocityModifier = victimPawn.VelocityModifier / 2;
            victimPawn.SetColor(Color.BlueViolet);

            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/entity/path_particle_c4_wires.vpcf", 0.4f);
                Owner.PrintToChat("path_particle_c4_wires");
                WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                {
                    Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/ui/rank_carepackage_recieve_hit.vpcf", 0.4f);
                    Owner.PrintToChat("rank_carepackage_recieve_hit");
                    WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                    {
                        Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/inferno_fx/firework_crate_ground_primary_01.vpcf", 0.4f);
                        Owner.PrintToChat("firework_crate_ground_primary_01");
                        WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                        {
                            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/water_impact/water_impact_bubbles_1.vpcf", 0.4f);
                            Owner.PrintToChat("water_impact_bubbles_1");
                            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                            {
                                Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/water_fx/waterfall_anubis.vpcf", 0.4f);
                                Owner.PrintToChat("waterfall_anubis");
                                WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                {
                                    Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/explosions_fx/explosion_child_core09b_1k.vpcf", 0.4f);
                                    Owner.PrintToChat("explosion_child_core09b_1k");
                                    WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                    {
                                        Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/explosions_fx/explosion_c4_interior_distort01b.vpcf", 0.4f);
                                        Owner.PrintToChat("explosion_c4_interior_distort01b");
                                        WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                        {
                                            Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/ambient_fx/env_sparks_directional.vpcf", 0.4f);
                                            Owner.PrintToChat("water_wake_slow");
                                            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                            {
                                                Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/maps/de_dust/dust_devil.vpcf", 0.4f);
                                                Owner.PrintToChat("dust_devil");
                                                WarcraftPlugin.Instance.AddTimer(1.5f, () =>
                                                {
                                                    Warcraft.SpawnParticle(victimPawn.AbsOrigin.Clone().Add(z: 75), "particles/inventory_items/crate_outward_groundsmoke.vpcf", 0.4f);
                                                    Owner.PrintToChat("crate_outward_groundsmoke");
                                                    // 
                                                    // 1 works, 0 doesn't work.
                                                    // Testing Push
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
