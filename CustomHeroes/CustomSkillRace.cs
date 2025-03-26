using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector; // ✅ Aliased Vector
using System.Numerics;

namespace WarcraftPlugin.Classes
{
    public class CustomSkillRace : WarcraftClass
    {
        public override string DisplayName => "CustomSkillRace";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("TEST MOVEMENT SPEED", "STEST"),
            new WarcraftAbility("TEST HEALTH", "TEST"),
            new WarcraftAbility("TEST INVISIBILITY", "TEST"),
            new WarcraftCooldownAbility("TEST TELEPORT", " TEST ", 5f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerPing>(OnPlayerPing);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookAbility(3, Ultimate);
        }

        public static void BonusMovementSpeedh(CCSPlayerController player, float amount, float duration)
        {
            var SpeedEffect = new SetMovementSpeed(player, amount, duration);
            SpeedEffect.Start();
        }

        public static void BonusHealth(CCSPlayerController player, int amount)
        {
            var HealthEffect = new SetBonusHealth(player, amount);
            HealthEffect.Start();
        }

        public static void Invisibility(CCSPlayerController player, float duration, int amount)
        {
            var InvisEffect = new SetInvisibility(player, duration, amount);
            InvisEffect.Start();
        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            Console.WriteLine("CustomSkillRace has spawned!");

            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                BonusMovementSpeedh(Player, 6f, 999f);
                BonusHealth(Player, 8880);
                Invisibility(Player, 20f, 100);

                FancySpawnEffect.DrawSpawnTriangle(Player);
            });

            if (Player.PlayerPawn == null || !Player.PlayerPawn.IsValid)
                return;

            var NewMovementSpeed = Player.PlayerPawn.Value.VelocityModifier;
            Console.WriteLine($"You have {NewMovementSpeed} Speed");
        }



        private void Ultimate()
        {
            SkillFunctions.TeleportUltimate(Player);
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            if (@event.Attacker == null || @event.Userid == null) return;

            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (!attacker.IsValid || !victim.IsValid || attacker.UserId == victim.UserId)
                return;

            SkillFunctions.SlowTarget(attacker, victim, 50, 3.5f);
        }

        private void OnPlayerPing(EventPlayerPing ping)
        {
            SkillFunctions.HandleTeleportPing(Player, ping.X, ping.Y, ping.Z);
        }


        public static class FancySpawnEffect // testing custom effects using DrawLaserBetween
        {
            public static void DrawSpawnTriangle(CCSPlayerController player)
            {
                var pawn = player.PlayerPawn?.Value;
                if (pawn == null || !pawn.IsValid) return;

                var origin = pawn.AbsOrigin;
                var angles = pawn.EyeAngles;

                // Basic forward vector calculation (no cross product math)
                float yaw = angles.Y * (float)Math.PI / 180f;

                // Get approximate forward and right vectors
                Vector forward = new Vector((float)Math.Cos(yaw), (float)Math.Sin(yaw), 0f);
                Vector right = new Vector(-forward.Y, forward.X, 0f); // 90 degrees rotated

                // Triangle points (all in game Vector)
                Vector pointA = origin + forward * 50f;
                Vector pointB = origin - forward * 50f + right * 25f;
                Vector pointC = origin - forward * 50f - right * 25f;

                // Draw triangle lasers
                DrawLaserBetween(pointA, pointB, Color.Cyan, 2f);
                DrawLaserBetween(pointB, pointC, Color.Cyan, 2f);
                DrawLaserBetween(pointC, pointA, Color.Cyan, 2f);
            }

            public static CBeam DrawLaserBetween(Vector startPos, Vector endPos, Color? color = null, float duration = 1f, float width = 2f)
            {
                var beam = Utilities.CreateEntityByName<CBeam>("beam");
                if (beam == null) return null;

                beam.Render = color ?? Color.Red;
                beam.Width = width;

                beam.Teleport(startPos, new QAngle(), new Vector());

                // Use reflection hack to set EndPos
                typeof(CBeam).GetProperty("EndPos")?.SetValue(beam, endPos);

                beam.DispatchSpawn();

                WarcraftPlugin.Instance.AddTimer(duration, () =>
                {
                    if (beam.IsValid)
                        beam.Remove();
                });

                return beam;
            }
        }
    }
}