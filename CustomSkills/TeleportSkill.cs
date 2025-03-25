using System;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Helpers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.CustomSkills
{
    public static class TeleportSkill
    {
        public static void Execute(CCSPlayerController player)
        {
            // Ping hack to get target location (assumes player_ping is hooked in race file)
            player.ExecuteClientCommandFromServer("player_ping");
        }

        public static void HandlePing(CCSPlayerController player, float pingX, float pingY, float pingZ)
        {
            var offset = 40f;
            var Zoffset = 60f;
            var origin = player.PlayerPawn.Value.AbsOrigin;

            float deltaX = origin.X - pingX;
            float deltaY = origin.Y - pingY;
            float deltaZ = origin.Z - pingZ;
            float distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

            float newX = pingX + deltaX / distance * offset;
            float newY = pingY + deltaY / distance * offset;
            float newZ = pingZ + deltaZ / distance * Zoffset;

            // Effects + teleport
            player.PlayLocalSound("sounds/weapons/fx/nearmiss/bulletltor06.vsnd");
            var from = origin.Clone().Add(z: 20);
            Warcraft.SpawnParticle(from, "particles/ui/ui_electric_exp_glow.vpcf", 3);
            Warcraft.SpawnParticle(origin, "particles/explosions_fx/explosion_smokegrenade_distort.vpcf", 2);

            player.PlayerPawn.Value.Teleport(new Vector(newX, newY, newZ), player.PlayerPawn.Value.AbsRotation, new Vector());

            var to = player.PlayerPawn.Value.AbsOrigin;
            Warcraft.SpawnParticle(to.Clone().Add(z: 20), "particles/ui/ui_electric_exp_glow.vpcf", 3);
            Warcraft.SpawnParticle(to, "particles/explosions_fx/explosion_smokegrenade_distort.vpcf", 2);
        }
    }
}
