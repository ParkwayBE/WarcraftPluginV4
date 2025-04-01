using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.Classes
{
    public class LaserLightShow : WarcraftClass
    {
        public override string DisplayName => "Laser Light Show";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Module R", "Increased Movement speed and health on spawn."),
            new WarcraftAbility("Module G", "Your attacks have a chance to deal bonus damage."),
            new WarcraftAbility("Module B", "Your attacks can chain through enemies."),
            new WarcraftCooldownAbility("Disintigrate","Upon activation: After a brief delay fire a beam of energy damaging all players that are too close.", 1f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            WarcraftPlugin.Instance.AddTimer(0.8f, () =>
            {
                int abilityLevel = WarcraftPlayer.GetAbilityLevel(0);
                if (abilityLevel < 1) return;

                float speedMultiplier = 1 + (0.1f * abilityLevel);
                int bonushealth = abilityLevel * 15;

                SkillFunctions.MovementSpeed(Player, speedMultiplier, 999f);
                SkillFunctions.SetBonusHealth(Player, bonushealth);
                new RGBColorCycleEffect(Player, 999f).Start();




                var origin = Player.PlayerPawn.Value.AbsOrigin;
                float radius = 50f;
                int laserCount = 32;
                float laserDuration = 3f;
                float laserUpdateInterval = 0.5f;
                int updates = (int)(laserDuration / laserUpdateInterval);

                List<(Vector start, Vector end)> laserPositions = new();

                for (int i = 0; i < laserCount; i++)
                {
                    float angle = (float)(i * (2 * Math.PI / laserCount));
                    float x = origin.X + radius * (float)Math.Cos(angle);
                    float y = origin.Y + radius * (float)Math.Sin(angle);
                    float zStart = origin.Z;
                    float zEnd = origin.Z + 200f;

                    laserPositions.Add((new Vector(x, y, zStart), new Vector(x, y, zEnd)));
                }

                void DrawColorCycle(int currentTick)
                {
                    if (currentTick >= updates)
                        return;

                    foreach (var (start, end) in laserPositions)
                    {
                        var color = Color.FromArgb(Random.Shared.Next(256), Random.Shared.Next(256), Random.Shared.Next(256));
                        Warcraft.DrawLaserBetween(start, end, color, laserUpdateInterval + 0.1f, width: 1.2f);
                    }

                    WarcraftPlugin.Instance.AddTimer(laserUpdateInterval, () => DrawColorCycle(currentTick + 1));
                }
                DrawColorCycle(0);
            });
        }



        private void Ultimate()
        {
            int abilityLevel0 = WarcraftPlayer.GetAbilityLevel(0);
            int abilityLevel1 = WarcraftPlayer.GetAbilityLevel(1);
            int abilityLevel2 = WarcraftPlayer.GetAbilityLevel(2);
            int AbilityLevelMult = abilityLevel0 * abilityLevel1 * abilityLevel2;
            float radius = 900f + AbilityLevelMult;

            var eyePos = Player.EyePosition();
            eyePos.Z += 30f; // Raise slightly above eye level
            var forward = Player.PlayerPawn.Value.EyeAngles.ToForward();
            var targetPos = eyePos + forward * 1000f;

            // New rainbow triple beam
            Color[] beamColors = { Color.Red, Color.Green, Color.Blue };
            Vector[] offsets = {
                new Vector(5f, 0, 0),
                new Vector(-5f, 0, 0),
                new Vector(0, 5f, 0)
            };

            foreach (var offset in offsets)
            {
                var beamStart = eyePos + offset;
                var jitteredEnd = targetPos + new Vector(0, 0, Random.Shared.Next(-10, 10));

                Warcraft.DrawLaserBetween(beamStart, jitteredEnd, beamColors[Array.IndexOf(offsets, offset)], duration: 1.5f, width: 4f);
            }


            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                // Spawn explosion at target
                Warcraft.SpawnExplosion(targetPos, (AbilityLevelMult - 50f), radius, Player, KillFeedIcon.prop_exploding_barrel);

                // Radial blast lasers

                int beamCount = 32;
                float angleStep = 360f / beamCount;

                for (int i = 0; i < beamCount; i++)
                {
                    // Random direction using unit sphere
                    double theta = Random.Shared.NextDouble() * 2 * Math.PI;
                    double phi = Math.Acos(2 * Random.Shared.NextDouble() - 1);
                    float x = (float)(Math.Sin(phi) * Math.Cos(theta));
                    float y = (float)(Math.Sin(phi) * Math.Sin(theta));
                    float z = (float)Math.Cos(phi);

                    var dir = new Vector(x, y, z);
                    var end = targetPos + dir * radius;

                    var color = Color.FromArgb(Random.Shared.Next(256), Random.Shared.Next(256), Random.Shared.Next(256));
                    Warcraft.DrawLaserBetween(targetPos, end, color, duration: 2.5f, width: 2f);
                }
            });

            Player.PrintToChat($" {ChatColors.Green}Disintigrate{ChatColors.Green} Ultimate activated!");
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel <= 0) return;

            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (!attacker.IsValid || !victim.IsValid || !victim.IsAlive()) return;

            var damage = @event.DmgHealth;
            if (damage <= 0) return;

            // ---------------------
            // V-Shape Laser Effect
            // ---------------------
            Vector beamStart = attacker.PlayerPawn.Value.AbsOrigin;
            beamStart.Z += 20; // Start at feet

            Vector endBase = victim.PlayerPawn.Value.AbsOrigin;
            float baseZ = endBase.Z + 30f;

            var rand = new Random();

            // Tier setup: 3 levels of lasers (1, 3, 5 = 9 total)
            int[] tierCounts = { 0, 1, 2 }; // Number of lasers left/right from center per tier
            float zStep = 10f;
            float xStep = 10f;
            float yCurve = 6f;

            // Grouped colors
            List<Color> tierColors = new()
    {
        Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256)),
        Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256)),
        Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256))
    };

            for (int tier = 0; tier < tierCounts.Length; tier++)
            {
                int count = tierCounts[tier];
                float tierZ = baseZ + (tier * zStep);

                for (int i = -count; i <= count; i++)
                {
                    float xOffset = i * xStep;
                    float yOffset = -Math.Abs(i) * yCurve;

                    Vector beamEnd = new Vector(
                        endBase.X + xOffset,
                        endBase.Y + yOffset,
                        tierZ + rand.Next(-2, 3) // tiny jitter
                    );

                    Warcraft.DrawLaserBetween(
                        beamStart,
                        beamEnd,
                        tierColors[tier],
                        duration: 1.5f,
                        width: 1f
                    );
                }
            }

            // ---------------------
            // Chain Through Logic
            // ---------------------
            Vector victimPos = victim.PlayerPawn.Value.AbsOrigin;
            Vector forward = attacker.PlayerPawn.Value.EyeAngles.ToForward();

            foreach (var target in Utilities.GetPlayers())
            {
                if (target == victim || target == attacker) continue;
                if (!target.IsValid || !target.IsAlive() || target.TeamNum == attacker.TeamNum) continue;

                Vector targetPos = target.PlayerPawn.Value.AbsOrigin;
                Vector toTarget = targetPos - victimPos;

                if (toTarget.Length() > 300f) continue;

                Vector toTargetNorm = toTarget / toTarget.Length();
                float dot = Vector3.Dot(forward.ToVector3(), toTargetNorm.ToVector3());
                if (dot < 0.90f) continue;

                // Piercing laser from victim to collateral
                var pierceColor = Color.FromArgb(
                    Random.Shared.Next(100, 256),
                    Random.Shared.Next(100, 256),
                    Random.Shared.Next(100, 256)
                );

                Warcraft.DrawLaserBetween(
                    Warcraft.EyePosition(victim, -10),
                    Warcraft.EyePosition(target, -10),
                    pierceColor
                );

                // Apply collateral damage
                int collateralDamage = (int)(damage * 0.5f);
                @event.AddBonusDamage(collateralDamage);

                target.PrintToChat($" {ChatColors.Red}Module B{ChatColors.Default} You were hit through {victim.PlayerName}!");
                attacker.PrintToChat($" {ChatColors.Green}Module B{ChatColors.Default}: {target.PlayerName} was pierced for {collateralDamage} damage!");
            }
        }
        public class RGBColorCycleEffect : WarcraftEffect
        {
            private float _hue = 0f;

            public RGBColorCycleEffect(CCSPlayerController owner, float duration)
                : base(owner, duration)
            {
            }

            public override void OnStart()
            {
            }

            public override void OnTick()
            {
                if (Owner?.PlayerPawn?.Value == null || !Owner.IsAlive()) return;

                // Convert hue (0-360) to RGB
                var rgb = ColorFromHue(_hue);
                Owner.PlayerPawn.Value.SetColor(rgb);

                // Increase hue
                _hue += 5f;
                if (_hue >= 360f) _hue = 0f;
            }

            public override void OnFinish()
            {
                if (Owner?.PlayerPawn?.Value != null)
                {
                    Owner.PlayerPawn.Value.SetColor(Color.White);
                }
            }

            private Color ColorFromHue(float hue)
            {
                float s = 1f;
                float v = 1f;
                int hi = (int)(hue / 60f) % 6;
                float f = (hue / 60f) - (int)(hue / 60f);

                float p = v * (1 - s);
                float q = v * (1 - f * s);
                float t = v * (1 - (1 - f) * s);

                float r = 0, g = 0, b = 0;

                switch (hi)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    case 5: r = v; g = p; b = q; break;
                }

                return Color.FromArgb(
                    (int)(r * 255),
                    (int)(g * 255),
                    (int)(b * 255)
                );
            }
        }
    }
}
