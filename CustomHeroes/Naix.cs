using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Models;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Core.Preload;



namespace WarcraftPlugin.Classes
{

    internal class Naix : WarcraftClass
    {
        public override string DisplayName => "Naix";
        public override Color DefaultColor => Color.AntiqueWhite;
        private const uint IN_DUCK = 1 << 2; // Defines the crouch input
        private readonly Dictionary<CCSPlayerController, SmokeSupplyEffect> activeEffects = new();
        private readonly int _MovementSpeedMult = 10;
        private Dictionary<ulong, Vector> lastSmokePositions = new Dictionary<ulong, Vector>();
        private Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer> smokeTimers = new();
        private bool isAlive = false;
        private bool canUseUltimate = true;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Smoke Supply", "Spawn with a smoke grenade and gain up to 0/1/2/3/4 additional smokes"),
            new WarcraftAbility("Consume", "If you kill an enemy while crouched, you teleport to the place you killed him, refill your ammo and gain 5/10/15/25/35 health"),
            new WarcraftAbility("Acrobatics", "Gain 10/20/30/40/50% movement speed and the ability to jump further."),
            new WarcraftCooldownAbility("Detonate", "Detonate a grenade in the middle of the last smoke you threw", 6f)
        ];

        public override void Register()
        {
            HookEvent<EventSmokegrenadeDetonate>(SmokegrenadeDetonate);
            HookEvent<EventPlayerHurtOther>(PlayerKill);
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerJump>(PlayerJump);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            if (Player == null || Player.PlayerPawn?.Value == null)
            {
                Console.WriteLine("[ERROR] PlayerSpawn triggered but player is STILL NULL after delay! Aborting.");
                return;
            }

            if (WarcraftPlayer.GetAbilityLevel(2) > 0)
            {
                WarcraftPlugin.Instance.AddTimer(0.2f, () => {
                    if (WarcraftPlayer.GetAbilityLevel(2) > 0)
                    {
                        var pawn = Player.PlayerPawn.Value;
                        pawn.VelocityModifier = 1 + 0.12f * WarcraftPlayer.GetAbilityLevel(2);
                    }
                });

            }


            // Ensuring previous effect is removed before adding a new one
            if (activeEffects.TryGetValue(Player, out var existingEffect))
            {
                existingEffect.Destroy();
                activeEffects.Remove(Player);
            }

            // Apply Smoke Supply Effect and track it
            var effect = new SmokeSupplyEffect(Player);
            activeEffects[Player] = effect;
            effect.Start();
            canUseUltimate = true;
            isAlive = true;
        }

        private void PlayerDeath(EventPlayerDeath @event)
        {
            Console.WriteLine("Player died, preventing ultimate usage.");
            isAlive = false;
            canUseUltimate = false;

        }

        private void PlayerJump(EventPlayerJump @event)
        {
            if (WarcraftPlayer.GetAbilityLevel(2) > 0)
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

                    if (directionVec.Z < 0.475f)
                    {
                        directionVec.Z = 0.475f;
                    }

                    directionVec *= 575; // Adjust force if needed
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

        private void SmokegrenadeDetonate(EventSmokegrenadeDetonate detonate)
        {
            Console.WriteLine("[DEBUG] SmokeDetonate triggered!");

            if (detonate.Userid == null || !detonate.Userid.IsValid)
            {
                Console.WriteLine("[ERROR] SmokeDetonate event triggered, but Userid is NULL or invalid!");
                return;
            }

            var player = detonate.Userid;
            if (player == null || player.PlayerPawn?.Value == null)
            {
                Console.WriteLine("[ERROR] Player is NULL in SmokeDetonate! Aborting.");
                return;
            }

            lastSmokePositions[player.SteamID] = new Vector(detonate.X, detonate.Y, detonate.Z);

            if (activeEffects.TryGetValue(player, out var effect))
            {
                effect.GiveSmokeIfNeeded();
            }
            SetUltimateAvailability(true, player);
        }

        private void SetUltimateAvailability(bool availability, CCSPlayerController player)
        {
            var playerId = player.SteamID;
            // Cancel any existing timer for this player
            if (smokeTimers.TryGetValue(player, out var existingTimer))
            {
                Console.WriteLine("[INFO] Cancelling existing timer.");
                existingTimer.Kill();
                smokeTimers.Remove(player);
            }

            if (availability)
            {
                canUseUltimate = true;

                var timer = WarcraftPlugin.Instance.AddTimer(20f, () =>
                {
                    canUseUltimate = false;
                    smokeTimers.Remove(player);
                });

                smokeTimers[player] = timer;
            }
            else
            {
                canUseUltimate = false;
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

        int repetitionCount = 0;
        int maxRepetitions = 3;
        float delayBetweenRepetitions = 1.0f; // Matches the particle duration

        void SpawnParticles()
        {
            // EFFECT CODE
            float offset = 70.0f; // Adjust the offset as needed
            float particleDuration = 120.0f;
            float particleDuration2 = 40.0f;
            string redCircleParticle = "particles/weapons/cs_weapon_fx/weapon_sensorgren_detonate.vpcf";
            string redCircleParticle2 = "particles/inferno_fx/explosion_incend_air_core.vpcf";

            var basePosition = Player.PlayerPawn.Value.AbsOrigin.Clone();
            basePosition.Z += 50; // Raise all particles above the ground

            // Spawn particle 1 (center)
            var particle1 = Warcraft.SpawnParticle(basePosition, redCircleParticle, particleDuration);
            particle1.SetParent(Player.PlayerPawn.Value);

            // Spawn particle 2 (offset slightly in X)
            var particle2Position = basePosition.Clone();
            particle2Position.X += offset;
            var particle2 = Warcraft.SpawnParticle(particle2Position, redCircleParticle, particleDuration);
            particle2.SetParent(Player.PlayerPawn.Value);

            // Spawn particle 3 (offset slightly in Y)
            var particle3Position = basePosition.Clone();
            particle3Position.Y += offset;
            var particle3 = Warcraft.SpawnParticle(particle3Position, redCircleParticle, particleDuration);
            particle3.SetParent(Player.PlayerPawn.Value);

            // particle 4 
            var particle4Position = basePosition.Clone();
            var particle4 = Warcraft.SpawnParticle(particle4Position, redCircleParticle2, particleDuration2);
            particle4.SetParent(Player.PlayerPawn.Value);

            // END EFFECT CODE

            repetitionCount++;
            if (repetitionCount < maxRepetitions)
            {
                WarcraftPlugin.Instance.AddTimer(delayBetweenRepetitions, SpawnParticles);
            }
        }


        private void PlayerKill(EventPlayerHurtOther @event)
        {
            var killer = @event.Attacker;
            var victim = @event.Userid;

            if (killer == null || victim == null)
            {
                Console.WriteLine("[ERROR] PlayerKill failed: Killer or victim is NULL!");
                return;
            }

            if (killer != Player)
            {
                Console.WriteLine($"[INFO] {killer.PlayerName} is not Naix. Ignoring kill event.");
                return;
            }

            if (victim.PlayerPawn?.Value?.Health > 0)
            {
                return;
            }
            // Check if the player is crouching
            var movementServices = killer.PlayerPawn.Value.MovementServices;
            if ((movementServices.Buttons.ButtonStates[0] & IN_DUCK) == 0)
            {
                return;
            }

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel < 1)
            {
                Console.WriteLine($"[INFO] {killer.PlayerName} does not have Consume unlocked. Aborting.");
                return;
            }
            Console.WriteLine($"[DEBUG] {killer.PlayerName} has Consume at Level {abilityLevel}.");
            Console.WriteLine($"[DEBUG] Current Health: {killer.Health}");
            Console.WriteLine($"[DEBUG] Heal Amount: {abilityLevel} * 7 = {abilityLevel * 7}");

            int currentHealth = killer.PlayerPawn.Value.Health;

            int healAmount = abilityLevel * 7;
            int newHealth = currentHealth + healAmount;
            killer.SetHp(newHealth);

            Player.PrintToCenter($"[INFO] {killer.PlayerName} healed for {healAmount} HP (new total: {newHealth}).");

            if (victim.PlayerPawn?.Value != null)
            {
                Vector victimPos = victim.PlayerPawn.Value.AbsOrigin;
                killer.PlayerPawn.Value.Teleport(victimPos, killer.PlayerPawn.Value.AbsRotation, new Vector());
                Console.WriteLine($"[INFO] {killer.PlayerName} teleported to {victim.PlayerName}'s last position.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Could not retrieve victim's position for teleport!");
            }

            var activeWeapon = killer.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value;
            if (activeWeapon != null)
            {
                activeWeapon.Clip1 = activeWeapon.GetVData<CBasePlayerWeaponVData>().MaxClip1;
                Console.WriteLine($"[INFO] {killer.PlayerName}'s ammo refilled to max ({activeWeapon.Clip1})!");
            }
            else
            {
                Console.WriteLine("[ERROR] No active weapon found, could not refill ammo!");
            }
            killer.PlayerPawn.Value.SetColor(Color.Red);
            SpawnParticles();
            killer.PlayLocalSound("sounds/ambient/ambience/rainscapes/thunder_close02.vsnd");
            victim.PlayLocalSound("sounds/ambient/ambience/rainscapes/thunder_close02.vsnd");


            WarcraftPlugin.Instance.AddTimer(3.0f, () =>
            {
                if (killer.PlayerPawn?.Value != null && killer.PlayerPawn.Value.IsValid)
                {
                    killer.PlayerPawn.Value.SetColor(Color.AntiqueWhite);
                    repetitionCount = 0;
                }
            });

            Console.WriteLine($"[SUCCESS] {killer.PlayerName} successfully used Consume on {victim.PlayerName}!");
        }

        internal class SmokeSupplyEffect(CCSPlayerController owner) : WarcraftEffect(owner)
        {
            private int smokesGiven = 0;
            private int maxSmokes = 0;
            private WarcraftPlayer WarcraftPlayer;

            public override void OnStart()
            {
                Console.WriteLine("[DEBUG] SmokeSupplyEffect OnStart() triggered.");

                if (Owner == null || Owner.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("[ERROR] SmokeSupplyEffect started but Owner is NULL! Aborting.");
                    return;
                }

                WarcraftPlayer = Owner.GetWarcraftPlayer();
                if (WarcraftPlayer == null)
                {
                    Console.WriteLine("[ERROR] Failed to retrieve WarcraftPlayer.");
                    return;
                }

                maxSmokes = WarcraftPlayer.GetAbilityLevel(0);
                Console.WriteLine($"[DEBUG] Retrieved ability level: {maxSmokes}");

                if (maxSmokes < 1)
                {
                    Console.WriteLine("[INFO] Player has no Smoke Supply ability, skipping smoke grenade assignment.");
                    return;
                }

                Console.WriteLine($"[INFO] Smoke Supply Effect Activated - Ability Level: {maxSmokes}");

                // Remove existing smokes to prevent unintended stacking
                RemoveGrenades("weapon_smokegrenade");

                //Start with 1 smoke
                Console.WriteLine("[INFO] Granting initial smoke grenade.");
                Owner.GiveNamedItem("weapon_smokegrenade");
                smokesGiven = 1; 
            }

            public void GiveSmokeIfNeeded()
            {
                if (smokesGiven >= maxSmokes)
                {
                    Console.WriteLine($"[INFO] {Owner.PlayerName} has already received the max number of smokes ({maxSmokes}).");
                    return;
                }

                Console.WriteLine($"[INFO] {Owner.PlayerName} has no smokes. Giving another one.");
                Owner.GiveNamedItem("weapon_smokegrenade");
                smokesGiven++;
            }


            public override void OnFinish()
            {
                Console.WriteLine($"[INFO] No more free smokes for {Owner.PlayerName} this round.");
            }

            private void RemoveGrenades(string grenadeName)
            {
                var grenades = Owner.PlayerPawn.Value.WeaponServices.MyWeapons;

                foreach (var grenade in grenades)
                {
                    if (grenade.Value.DesignerName == grenadeName)
                    {
                        Console.WriteLine($"[INFO] Removing existing {grenadeName} from {Owner.PlayerName}");
                        Owner.DropWeaponByDesignerName(grenadeName);
                    }
                }
            }

            public override void OnTick() { }

        }

        private void Ultimate()
        {
            if (WarcraftPlayer.GetAbilityLevel(3) < 1 || !IsAbilityReady(3))
            {
                Console.WriteLine("[INFO] Ultimate cannot be used (ability level too low or on cooldown).");
                return;
            }

            if (!canUseUltimate)
            {
                Console.WriteLine("[INFO] Ultimate is on cooldown!");
                return;
            }

            if (!lastSmokePositions.TryGetValue(Player.SteamID, out Vector lastSmokePosition))
            {
                Console.WriteLine("[ERROR] No stored smoke grenade position for this player! Aborting ultimate.");
                return;
            }

            var explosionPosition = lastSmokePosition.With(z: lastSmokePosition.Z + 10f);

            if (explosionPosition == null)
            {
                Console.WriteLine("[ERROR] Explosion position is NULL! Aborting.");
                return;
            }

            Warcraft.SpawnExplosion(
                pos: explosionPosition,
                damage: 50f + (WarcraftPlayer.GetAbilityLevel(3) * 10f), 
                radius: 350f, // Tweak for explosion radius
                attacker: Player,
                killFeedIcon: KillFeedIcon.prop_exploding_barrel
            );

            Console.WriteLine($"[INFO] Explosion triggered at {explosionPosition} with {50f + (WarcraftPlayer.GetAbilityLevel(3) * 10f)} damage!");

            canUseUltimate = false;
            WarcraftPlugin.Instance.AddTimer(6.0f, () => canUseUltimate = true); // Reset after 6 seconds
            StartCooldown(3);
        }


    }
}