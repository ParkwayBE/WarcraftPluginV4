using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CounterStrikeSharp.API.Modules.Memory;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using System.Drawing;
using WarcraftPlugin.Core.Effects;
using System.Collections.Generic;
using WarcraftPlugin.Events.ExtendedEvents;
using System;
using System.Reflection;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using WarcraftPlugin.Core;
using WarcraftPlugin.Summons;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using System.Linq;


namespace WarcraftPlugin.Classes
{
    public class Wraithstalker : WarcraftClass
    {
        public override string DisplayName => "Wraithstalker";

        private readonly WarcraftPlugin _plugin;
        public override Color DefaultColor => Color.CadetBlue;
        private bool canUseUltimate = true;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Assimilation", "On Kill: Movement speed and reduced gravity for 3/5/7/9/10 seconds, this also grants a blindstack max 2, which blinds an enemy when he spots you."),
            new WarcraftAbility("Phantom Cloak", "Standing still for  2.5 - 0.5 seconds makes you invisible and your next shot deals bonus damage."),
            new WarcraftAbility("Shadowstrike", "After you exited Phantom Cloak your next hit will cause bonus damage and grant you a guaranteed skull"),
            new WarcraftCooldownAbility("Marked for prey", "Scan the area where you are looking, highlight enemies close for x seconds and slow them down. Killing a marked target grants a skull. Skulls give you lasting benefits untill mapchange.", 5f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);

            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            
        }

        public static void SetGlowOnEntity(CBaseEntity? entity, Color GlowColor)
        {
            if (entity == null || !entity.IsValid)
                return;

            CDynamicProp Glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic")!;
            Glow.Spawnflags = 256;
            Glow.Render = Color.Transparent;
            Glow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(Glow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            Glow.SetModel(entity.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
            Glow.DispatchSpawn();
            Glow.Glow.GlowColorOverride = GlowColor;
            Glow.Glow.GlowRange = 1000;
            Glow.Glow.GlowRangeMin = 0;
            Glow.Glow.GlowTeam = -1; // -1 = Both, 2 = T, 3 = CT
            Glow.Glow.GlowType = 3;
            Glow.Glow.GlowTime = 8;

            Glow.Teleport(entity.AbsOrigin, entity.AbsRotation, entity.AbsVelocity);
            Glow.AcceptInput("SetParent", entity, Glow, "!activator");
        }

        internal class GlowEffect : WarcraftEffect
        {
            private readonly Color _glowColor;

            public GlowEffect(CCSPlayerController owner, Color glowColor, float duration, float onTickInterval)
                : base(owner, duration: duration, onTickInterval: onTickInterval)
            {
                _glowColor = glowColor;
            }

            public override void OnStart()
            {
                SetGlowOnEntity(Owner.PlayerPawn.Value, _glowColor);
            }

            public override void OnTick()
            {
                // Repeat the glow effect at every tick
                SetGlowOnEntity(Owner.PlayerPawn.Value, _glowColor);
            }

            public override void OnFinish()
            {
                // Optionally clear glow when finished
            }
        }

        private void FindAndLogNearbyPlayers(float radius)
        {
            // Get the player's current position and look direction
            var playerPosition = Player.PlayerPawn.Value.AbsOrigin;  // Current position
            var lookDirection = Player.PlayerPawn.Value.EyeAngles;   // Direction we're looking in

            // Offset the position in the direction we're looking
            var offsetVector = new Vector();
            NativeAPI.AngleVectors(lookDirection.Handle, offsetVector.Handle, nint.Zero, nint.Zero);
            offsetVector *= 200.0f; // Move 200 units forward in the look direction
            var targetPosition = playerPosition + offsetVector;

            // Iterate through all players and find those within the radius
            var players = Utilities.GetPlayers();
            foreach (var otherPlayer in players)
            {
                if (!otherPlayer.IsAlive() || otherPlayer.UserId == Player.UserId)
                    continue;

                // Calculate the squared distance between the target position and the other player's position
                var otherPlayerPosition = otherPlayer.PlayerPawn.Value.AbsOrigin;
                var distanceVector = targetPosition - otherPlayerPosition;
                var distanceSquared = distanceVector.X * distanceVector.X + distanceVector.Y * distanceVector.Y + distanceVector.Z * distanceVector.Z;

                // Compare squared distance to squared radius for performance
                if (distanceSquared <= radius * radius)
                {
                    // If within radius, print the player's name
                    var playerName = otherPlayer.GetRealPlayerName(); // Use the extension method
                    Console.WriteLine($"Player found: {playerName}");
                }
            }
        }


        private void Ultimate()
        {
            // Trigger the glow effect (if needed)
            var duration = 7.0f;
            var tickRate = 0.02f;
            /* new GlowEffect(Player, Color.Red, duration, tickRate).Start(); // UNCOMMENT THIS LINE TO ENABLE GLOW EFFECT AGAIN */

            // Find and log nearby players
            FindAndLogNearbyPlayers(500f);

            // Start cooldown for the ultimate ability
            StartCooldown(3);
        }




        private void PlayerShoot(EventWeaponFire @event)
        {
           

        }


    }
}









