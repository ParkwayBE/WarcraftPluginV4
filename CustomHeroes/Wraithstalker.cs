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
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CounterStrikeSharp.API.Modules.Timers;



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
            var level = WarcraftPlayer.GetAbilityLevel(1);
            if (level > 0)
                new PhantomCloakEffect(Player, level).Start();
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

        bool playerFound = false;
        private void NearbyPlayers(float radius, float forwardOffset)
        {
            bool playerFound = false;

            // Get the player's current position and eye direction
            var playerPosition = Player.PlayerPawn.Value.AbsOrigin;
            var eyeAngles = Player.PlayerPawn.Value.EyeAngles;

            // Convert angles to directional vector
            var forwardVector = new Vector();
            NativeAPI.AngleVectors(eyeAngles.Handle, forwardVector.Handle, nint.Zero, nint.Zero);
            forwardVector *= forwardOffset; // OFFSET NOT WORKING YET , FIX IT

            // Calculate the forward offset position
            var scanOrigin = playerPosition + forwardVector;

            var players = Utilities.GetPlayers();

            foreach (var otherPlayer in players)
            {
                if (!otherPlayer.IsAlive() || otherPlayer.UserId == Player.UserId)
                    continue;

                // Skip teammates
                if (Player.TeamNum == otherPlayer.TeamNum)
                    continue;

                var otherPlayerPosition = otherPlayer.PlayerPawn.Value.AbsOrigin;
                var distanceVector = scanOrigin - otherPlayerPosition;
                var distanceSquared = distanceVector.X * distanceVector.X + distanceVector.Y * distanceVector.Y + distanceVector.Z * distanceVector.Z;

                float doubleRadius = 2 * radius;

                if (distanceSquared <= doubleRadius * doubleRadius)
                {
                    playerFound = true;

                    // Trigger the glow effect
                    var duration = 7.0f;
                    var tickRate = 0.02f;
                    new GlowEffect(otherPlayer, Color.Red, duration, tickRate).Start();
                    otherPlayer.PrintToChat("You have been MARKED");
                    new UltimateSlowEffect(otherPlayer, 5.0f, 130f).Start();
                }
            }

            if (!playerFound)
            {
                Console.WriteLine("No enemies found in the scan direction and radius.");
            }
        }


        internal class UltimateSlowEffect : WarcraftEffect
        {
            private readonly float _slowAmount;
            private float _originalSpeed;

            public UltimateSlowEffect(CCSPlayerController owner, float duration, float slowAmount)
                : base(owner, duration: duration)
            {
                _slowAmount = slowAmount;
            }

            public override void OnStart()
            {
                if (Owner.PlayerPawn.Value == null)
                    return;

                // Store original speed
                _originalSpeed = Owner.PlayerPawn.Value.MovementServices.Maxspeed;

                // Reduce speed (clamp to prevent negative values)
                Owner.PlayerPawn.Value.MovementServices.Maxspeed = Math.Max(10, _originalSpeed - _slowAmount);
                

                // Debug log
                Console.WriteLine($"[DEBUG] {Owner.PlayerName} is slowed for {Duration} seconds! New speed: {Owner.PlayerPawn.Value.MovementServices.Maxspeed}");
            }

            public override void OnFinish()
            {
                if (Owner.PlayerPawn.Value == null)
                    return;

                // Restore original speed
                Owner.PlayerPawn.Value.MovementServices.Maxspeed = _originalSpeed;

                // Debug log
                Console.WriteLine($"[DEBUG] {Owner.PlayerName} slow effect ended. Speed restored to {Owner.PlayerPawn.Value.MovementServices.Maxspeed}");
            }

            public override void OnTick()
            {            }
        }

        internal class PhantomCloakEffect : WarcraftEffect
        {
            private float _stillTime;
            private readonly float _requiredStillTime;
            private bool _isCloaked;

            public PhantomCloakEffect(CCSPlayerController owner, int abilityLevel)
                : base(owner, duration: float.MaxValue, onTickInterval: 0.1f)
            {
                _requiredStillTime = Math.Max(0.5f, 3.0f - (abilityLevel * 0.5f));
            }

            public override void OnStart()
            {
                _stillTime = 0f;
                _isCloaked = false;
                Console.WriteLine($"[PhantomCloak] Required stand still time set to {_requiredStillTime} seconds.");
            }

            public override void OnTick()
            {
                var velocity = Owner.PlayerPawn.Value.AbsVelocity;
                // var isMoving = Math.Abs(velocity.X) > 0.1f || Math.Abs(velocity.Y) > 0.1f || Math.Abs(velocity.Z) > 0.1f;
                Console.WriteLine($"[PhantomCloak] Current velocity: {velocity} seconds.");

                if (!isMoving)
                {
                    _stillTime += OnTickInterval;
                    Console.WriteLine($"[PhantomCloak] Standing still for {_stillTime:0.00}/{_requiredStillTime}s");

                    if (!_isCloaked && _stillTime >= _requiredStillTime)
                    {
                        ApplyCloak();
                        _isCloaked = true;
                    }
                }
                else
                {
                    if (_isCloaked)
                    {
                        RemoveCloak();
                        _isCloaked = false;
                    }

                    if (_stillTime > 0f)
                    {
                        Console.WriteLine("[PhantomCloak] Movement detected, resetting timer.");
                    }

                    _stillTime = 0f;
                }
            }

            private void ApplyCloak()
            {
                int level = Owner.GetWarcraftPlayer().GetAbilityLevel(1);
                int alpha = Math.Max(130, 255 - (level * 25));
                Owner.PlayerPawn.Value.SetColor(Color.FromArgb(alpha, 255, 255, 255));
                Console.WriteLine("[PhantomCloak] Cloak applied!");
            }

            private void RemoveCloak()
            {
                Owner.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255));
                Console.WriteLine("[PhantomCloak] Cloak removed.");
            }

            public override void OnFinish()
            {
                if (_isCloaked)
                    RemoveCloak();

                Console.WriteLine("[PhantomCloak] Effect ended.");
            }
        }




        private void Ultimate()
        {
            // Find and log nearby players
            // NearbyPlayers(1000f, 1000f); // Radius, forward offset UNCOMMENT TO REGAIN MARKED ULT

            // Start cooldown for the ultimate ability
            StartCooldown(3);
        }




        private void PlayerShoot(EventWeaponFire @event)
        {
           // 

        }


    }
}









