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
using System.Reflection.Emit;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Numerics;



namespace WarcraftPlugin.Classes
{
    public class Wraithstalker : WarcraftClass
    {
        public override string DisplayName => "Wraithstalker";

        private readonly WarcraftPlugin _plugin;
        public override Color DefaultColor => Color.CadetBlue;
        private bool canUseUltimate = true;
        private bool _CanUseCloakEffect = true;
        private readonly Dictionary<int, PhantomCloakEffect> activeCloakEffects = new();
        private PhantomCloakEffect cloakEffect;
        private static Dictionary<ulong, int> skullTracker = new();
        private const int MaxSkulls = 40;



        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Assimilation", "On Kill: Gain a Skull, skulls last untill the end of the game and give various bonusstats on spawn."),
            new WarcraftAbility("Phantom Cloak", "Standing still for  2.5 - 0.5 seconds makes you invisible and your next shot deals bonus damage."),
            new WarcraftAbility("Shadowstrike", "After you exited Phantom Cloak your next hit will cause bonus damage and grant you a guaranteed skull"),
            new WarcraftCooldownAbility("Marked for prey", "Scan the area where you are looking, highlight enemies close for x seconds and slow them down. Killing a marked target grants a skull. Skulls give you lasting benefits untill mapchange.", 5f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerDeath>(OnPlayerDeath);
            HookEvent<EventRoundEnd>(OnRoundEnd);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDisconnect>(PlayerDisconnect);
            HookEvent<EventPlayerConnect>(OnPlayerConnect);
            HookEvent<EventPlayerJump>(PlayerJump);

            HookAbility(3, Ultimate);
        }



        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            if (@event.Attacker == null || @event.Userid == null)
                return;

            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (!attacker.IsValid || !victim.IsValid)
                return;

            if (attacker.PlayerPawn?.Value == null || victim.PlayerPawn?.Value == null)
                return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0);

            if (cloakEffect._AdditionalDamage)
            {
                Console.WriteLine("You dealt extra damage!");
                int bonusDamage = WarcraftPlayer.GetAbilityLevel(1) * 10;

                // Apply bonus damage
                @event.AddBonusDamage(bonusDamage);

                // Notify victim
                @event.Userid?.PrintToChat($"\x07[Wraithstalker] You received {bonusDamage} bonus damage from the shadows!");

                // Disable further bonus until recloaked
                cloakEffect._AdditionalDamage = false;
            }

            if (victim.PlayerPawn?.Value != null)
            {
                int remainingHealth = victim.PlayerPawn.Value.Health - @event.DmgHealth;

                if (remainingHealth <= 0)
                {
                    if (!skullTracker.ContainsKey(attacker.SteamID))
                        skullTracker[attacker.SteamID] = 0;

                    skullTracker[attacker.SteamID]++;
                    attacker.PrintToChat($"\x07[Wraithstalker] Skull claimed! Total skulls: {skullTracker[attacker.SteamID]}");
                }
            }

        }

        private void PlayerJump(EventPlayerJump @event)
        {
            int skulls = GetSkullCount(Player);
            if (WarcraftPlayer.GetAbilityLevel(0) > 0)
            {
                if (Player == null || Player.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("ERROR: Player or PlayerPawn is NULL in PlayerJump!");
                    return;
                }

                WarcraftPlugin.Instance.AddTimer(0.05f, () =>
                {
                    var directionAngle = Player.PlayerPawn.Value.EyeAngles;
                    var directionVec = new Vector();
                    NativeAPI.AngleVectors(directionAngle.Handle, directionVec.Handle, nint.Zero, nint.Zero);

                    if (directionVec.Z < 0.275f)
                    {
                        directionVec.Z = 0.275f;
                    }
                    int ScalingLongJump = Math.Max(150, 15 * skulls);
                    directionVec *= ScalingLongJump; // Adjust force if needed
                    Player.PlayerPawn.Value.AbsVelocity.X = directionVec.X;
                    Player.PlayerPawn.Value.AbsVelocity.Y = directionVec.Y;
                    Player.PlayerPawn.Value.AbsVelocity.Z = directionVec.Z;
                });
                WarcraftPlugin.Instance.AddTimer(0.05f, () =>
                {
                    Console.WriteLine("[INFO] Applying reduced gravity after delay.");
                    new SetGravityEffect(Player, 0.5f, 3f).Start();
                });
            }
        }

        internal class SetGravityEffect(CCSPlayerController owner, float gravity, float duration)
    : WarcraftEffect(owner, duration)
        {
            private readonly float _gravity = gravity;

            public override void OnStart()
            {
                if (Owner?.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("ERROR: Owner or PlayerPawn is NULL in SetGravityEffect!");
                    return;
                }
                Owner.PlayerPawn.Value.GravityScale = _gravity;
            }

            public override void OnFinish()
            {
                if (Owner?.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("ERROR: Owner or PlayerPawn is NULL in SetGravityEffect OnFinish!");
                    return;
                }
                Owner.PlayerPawn.Value.GravityScale = 1.0f; // Reset to default gravity
            }

            public override void OnTick() { /* */ }
        }


        private void ResetSkullsForPlayer(CCSPlayerController player)
        {
            if (player == null || !player.IsValid)
                return;

            if (skullTracker.ContainsKey(player.SteamID))
            {
                Console.WriteLine($"[Skulls] Resetting skulls for {player.PlayerName} ({player.SteamID})");
                skullTracker[player.SteamID] = 0;
            }
        }

        public void OnPlayerConnect(EventPlayerConnect @event)
        {
            Console.WriteLine("resetting Skulls");
            var player = @event.Userid;
            ResetSkullsForPlayer(player);
        }


        private void PlayerDisconnect(EventPlayerDisconnect @event)
        {
            var player = @event.Userid;
            ResetSkullsForPlayer(player);
        }



        private int GetSkullCount(CCSPlayerController player)
        {
            return skullTracker.TryGetValue(player.SteamID, out var count) ? Math.Min(count, MaxSkulls) : 0;
        }

        private void ApplySkullBonuses(CCSPlayerController player)
        {
            int skulls = GetSkullCount(player);
            int bonusHealth = skulls * 2;

            // Default speed (260 is CS2 default walk speed)

            int currentHealth = Player.PlayerPawn.Value.Health;
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0);
            int healAmount = abilityLevel * 1;
            int newHealth = currentHealth + skulls * healAmount;
            Player.SetHp(newHealth);

            var pawn = Player.PlayerPawn.Value;
            const float SkullSpeedDivisor = 66.666f;
            float bonusSpeed = Math.Min(0.6f, skulls / SkullSpeedDivisor);
            pawn.VelocityModifier = 1f + bonusSpeed;


            player.PrintToChat($"\x07[Wraithstalker] You have {skulls} skull(s). (+{bonusHealth} HP, +{skulls}% speed)");

        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            var playerId = Player.Slot;
            var level = WarcraftPlayer.GetAbilityLevel(1);
            cloakEffect = new PhantomCloakEffect(Player, level);
            RemoveCloakEffect(); // <- this line replaces RemovePhantomCloakEffect()
            
            cloakEffect.Start();
            activeCloakEffects[Player.Slot] = cloakEffect;

            WarcraftPlugin.Instance.AddTimer(0.2f, () =>
            {
                ApplySkullBonuses(Player);
            });
        }


        private void OnPlayerDeath(EventPlayerDeath death)
        {
            RemoveCloakEffect();
        }

        private void OnRoundEnd(EventRoundEnd round)
        {
            RemoveCloakEffect();
        }

        private void RemoveCloakEffect()
        {
            int playerId = Player.Slot;

            if (activeCloakEffects.TryGetValue(playerId, out var effect))
            {
                effect.Destroy();
                activeCloakEffects.Remove(playerId);
            }
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
            { }
        }


        internal class PhantomCloakEffect : WarcraftEffect
        {
            private Vector _previousPosition;
            private Vector _currentPosition;
            private Timer? _positionComparisonTimer;
            private bool _isCloaked;
            private readonly int _abilityLevel;
            public bool _AdditionalDamage = false;

            public PhantomCloakEffect(CCSPlayerController owner, int abilityLevel)
                : base(owner, duration: float.MaxValue, destroyOnDeath: true, destroyOnRoundEnd: true)
            {
                _abilityLevel = abilityLevel;
            }

            public override void OnStart()
            {
                Console.WriteLine("[PhantomCloak] OnStart is called");

                _previousPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();
                _currentPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();

                _positionComparisonTimer = WarcraftPlugin.Instance.AddTimer(1.0f, () =>
                {
                    _previousPosition = _currentPosition.Clone();
                    _currentPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();

                    //Console.WriteLine("[PhantomCloak] Comparing positions:");
                    //Console.WriteLine($"   Previous: {_previousPosition}");
                    //Console.WriteLine($"   Current:  {_currentPosition}");

                    if (_previousPosition.X == _currentPosition.X &&
                        _previousPosition.Y == _currentPosition.Y &&
                        _previousPosition.Z == _currentPosition.Z)
                    {
                        if (!_isCloaked)
                        {
                            EnableCloak();
                            _isCloaked = true;
                            _AdditionalDamage = false;
                        }
                    }
                    else
                    {
                        if (_isCloaked)
                        {
                            _AdditionalDamage = true;
                            DisableCloak();
                            _isCloaked = false;
                            Console.WriteLine("[Wraithstalker] Additional damage for the next 7 seconds for your first hit");
                            WarcraftPlugin.Instance.AddTimer(7.0f, () =>
                            {
                                _AdditionalDamage = false;
                                Console.WriteLine("[Wraithstalker] Additional damage expired.");
                            });
                        }
                    }
                }, TimerFlags.REPEAT);
            }

            public override void OnFinish()
            {
                Console.WriteLine("[PhantomCloak] OnFinish called.");
                _positionComparisonTimer?.Kill();
                if (_isCloaked)
                {
                    DisableCloak();
                    _isCloaked = false;
                }
            }

            private void EnableCloak()
            {
                int alpha = 100 + (5 - _abilityLevel) * 20; // L5 = 100, L1 = 180
                Owner.PlayerPawn.Value.SetColor(Color.FromArgb(alpha, 255, 255, 255));
                Console.WriteLine($"[PhantomCloak] Cloak enabled (alpha={alpha}).");
            }

            private void DisableCloak()
            {
                Owner.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255));
                Console.WriteLine("[PhantomCloak] Cloak disabled.");
            }

            public override void OnTick() { } 
        }








        private void Ultimate()
        {
            // NearbyPlayers(1000f, 1000f); // Radius, forward offset UNCOMMENT TO REGAIN MARKED ULT
            StartCooldown(3);
        }




        private void PlayerShoot(EventWeaponFire @event)
        {
            // 

        }


    }
}