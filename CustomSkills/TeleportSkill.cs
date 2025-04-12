using System;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Helpers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.CustomSkills
{
    public static class TeleportSkill
    {
        public static void Execute(CCSPlayerController player, float maxDistance)
        {
            player.ExecuteClientCommandFromServer("player_ping");
        }

        public static void HandlePing(CCSPlayerController player, float pingX, float pingY, float pingZ, float maxDistance = 1000f)
        {
            if (player?.PlayerPawn?.Value == null) return;

            var origin = player.PlayerPawn.Value.AbsOrigin;
            var pingTarget = new Vector(pingX, pingY, pingZ);

            var direction = new Vector(pingX - origin.X, pingY - origin.Y, pingZ - origin.Z);
            float distance = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y + direction.Z * direction.Z);

            if (distance == 0)
                return;

            var offset = 40f;
            var zOffset = 60f;

            var normalized = new Vector(direction.X / distance, direction.Y / distance, direction.Z / distance);
            var adjustedTarget = new Vector(
                pingX + normalized.X * offset,
                pingY + normalized.Y * offset,
                pingZ + normalized.Z * zOffset
            );

            var finalDirection = new Vector(adjustedTarget.X - origin.X, adjustedTarget.Y - origin.Y, adjustedTarget.Z - origin.Z);
            float finalDistance = MathF.Sqrt(finalDirection.X * finalDirection.X + finalDirection.Y * finalDirection.Y + finalDirection.Z * finalDirection.Z);

            if (finalDistance > maxDistance)
            {
                float scale = maxDistance / finalDistance;
                finalDirection = new Vector(finalDirection.X * scale, finalDirection.Y * scale, finalDirection.Z * scale);
            }

            var finalTarget = new Vector(origin.X + finalDirection.X, origin.Y + finalDirection.Y, origin.Z + finalDirection.Z);

            player.PlayLocalSound("sounds/weapons/fx/nearmiss/bulletltor06.vsnd");

            var from = origin.Clone().Add(z: 20);
            Warcraft.SpawnParticle(from, "particles/ui/ui_electric_exp_glow.vpcf", 3);
            Warcraft.SpawnParticle(origin, "particles/explosions_fx/explosion_smokegrenade_distort.vpcf", 2);

            player.PlayerPawn.Value.Teleport(finalTarget, player.PlayerPawn.Value.AbsRotation, new Vector());

            var to = player.PlayerPawn.Value.AbsOrigin;
            Warcraft.SpawnParticle(to.Clone().Add(z: 20), "particles/ui/ui_electric_exp_glow.vpcf", 3);
            Warcraft.SpawnParticle(to, "particles/explosions_fx/explosion_smokegrenade_distort.vpcf", 2);
        }

    }
}
