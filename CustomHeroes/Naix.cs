using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Models;
using WarcraftPlugin.Events.ExtendedEvents;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;


namespace WarcraftPlugin.Classes
{

    internal class Naix : WarcraftClass
    {
        public override string DisplayName => "Naix";
        public override Color DefaultColor => Color.GreenYellow;
        private const uint IN_DUCK = 1 << 2; // Defines the crouch input
        private readonly Dictionary<CCSPlayerController, SmokeSupplyEffect> activeEffects = new();
        private readonly int _MovementSpeedMult = 10;
        private Dictionary<ulong, Vector> lastSmokePositions = new Dictionary<ulong, Vector>();

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Smoke Supply", "Spawn with a smoke grenade and gain up to 0/1/2/3/4 additional smokes"),
            new WarcraftAbility("Consume", "If you kill an enemy while crouched, you teleport to the place you killed him, refill your ammo and gain 5/10/15/25/35 health"),
            new WarcraftAbility("Acrobatics", "Gain 10/20/30/40/50% movement speed and the ability to jump further."),
            new WarcraftCooldownAbility("Detonate", "Detonate a grenade in the middle of the last smoke you threw", 6f)
        ];

        public override void Register()
        {
            Console.WriteLine("[INFO] Registering Naix hooks...");
            HookEvent<EventSmokegrenadeDetonate>(OnSmokeDetonate);
            HookEvent<EventPlayerHurtOther>(PlayerKill);
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerJump>(PlayerJump);
            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            if (Player == null || Player.PlayerPawn?.Value == null)
            {
                Console.WriteLine("[ERROR] PlayerSpawn triggered but player is STILL NULL after delay! Aborting.");
                return;
            }

            Console.WriteLine("[INFO] Naix has spawned!");

            if (WarcraftPlayer.GetAbilityLevel(2) > 0)
            {
                Player.PlayerPawn.Value.VelocityModifier += WarcraftPlayer.GetAbilityLevel(2) * _MovementSpeedMult;
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

                Console.WriteLine($"[DEBUG] {Player.PlayerName} jumped! Applying longjump effect...");
                WarcraftPlugin.Instance.AddTimer(0.05f, () =>
                {
                    var directionAngle = Player.PlayerPawn.Value.EyeAngles;
                    var directionVec = new Vector();
                    NativeAPI.AngleVectors(directionAngle.Handle, directionVec.Handle, nint.Zero, nint.Zero);

                    if (directionVec.Z < 0.500f)
                    {
                        directionVec.Z = 0.500f;
                    }

                    directionVec *= 500; // Adjust force if needed
                    Player.PlayerPawn.Value.AbsVelocity.X = directionVec.X;
                    Player.PlayerPawn.Value.AbsVelocity.Y = directionVec.Y;
                    Player.PlayerPawn.Value.AbsVelocity.Z = directionVec.Z;

                    Console.WriteLine($"[INFO] Applied longjump force after delay: X:{directionVec.X}, Y:{directionVec.Y}, Z:{directionVec.Z}");
                });
                WarcraftPlugin.Instance.AddTimer(0.05f, () =>
                {
                    Console.WriteLine("[INFO] Applying reduced gravity after delay.");
                    new SetGravityEffect(Player, 0.7f, 3f).Start();
                });
            }
        }



        private void OnSmokeDetonate(EventSmokegrenadeDetonate detonate)
        {
            Console.WriteLine("[DEBUG] OnSmokeDetonate triggered!");

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

            // Log all potential position values
            Console.WriteLine($"[DEBUG] SmokeDetonate Position - X: {detonate.X}, Y: {detonate.Y}, Z: {detonate.Z}");

            // Ensure we have a valid position
            Vector smokePosition = new Vector(detonate.X, detonate.Y, detonate.Z);
            if (smokePosition == null)
            {
                Console.WriteLine("[ERROR] Failed to retrieve smoke grenade position!");
                return;
            }

            // ✅ Store the last smoke grenade position
            lastSmokePositions[player.SteamID] = smokePosition;
            Console.WriteLine($"[DEBUG] Stored smoke position for {player.PlayerName}: {smokePosition}");
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

                Console.WriteLine($"[INFO] {Owner.PlayerName} gravity set to {_gravity}.");
                Owner.PlayerPawn.Value.GravityScale = _gravity;
            }

            public override void OnFinish()
            {
                if (Owner?.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("ERROR: Owner or PlayerPawn is NULL in SetGravityEffect OnFinish!");
                    return;
                }

                Console.WriteLine($"[INFO] {Owner.PlayerName} gravity restored to normal.");
                Owner.PlayerPawn.Value.GravityScale = 1.0f; // Reset to default gravity
            }

            public override void OnTick() { /* Not needed */ }
        }



        private void PlayerKill(EventPlayerHurtOther @event)
        {
            Console.WriteLine("[DEBUG] PlayerHurtOther event triggered! Checking for lethal hit...");

            var killer = @event.Attacker;
            var victim = @event.Userid;

            Console.WriteLine($"[DEBUG] Attacker: {killer?.PlayerName ?? "NULL"}, Victim: {victim?.PlayerName ?? "NULL"}");

            if (killer == null || victim == null)
            {
                Console.WriteLine("[ERROR] PlayerKill failed: Killer or victim is NULL!");
                return;
            }

            // ✅ Ensure that Naix (YOU) is the attacker
            if (killer != Player)
            {
                Console.WriteLine($"[INFO] {killer.PlayerName} is not Naix. Ignoring kill event.");
                return;
            }

            // ✅ Ensure that the victim is actually dead
            if (victim.PlayerPawn?.Value?.Health > 0)
            {
                Console.WriteLine($"[INFO] {victim.PlayerName} is still alive ({victim.PlayerPawn.Value.Health} HP). Consume aborted.");
                return;
            }

            Console.WriteLine("[INFO] PlayerHurtOther detected a fatal hit, treating as PlayerKill.");

            Console.WriteLine($"[DEBUG] {killer.PlayerName} killed {victim.PlayerName}. Checking conditions...");

            // ✅ Check if the player is crouching using `IN_DUCK`
            var movementServices = killer.PlayerPawn.Value.MovementServices;
            if ((movementServices.Buttons.ButtonStates[0] & IN_DUCK) == 0)
            {
                Console.WriteLine($"[INFO] {killer.PlayerName} is NOT crouching. Consume aborted.");
                return;
            }
            Console.WriteLine($"[DEBUG] {killer.PlayerName} is crouching. Consume activated!");

            // ✅ Ensure Naix has the ability
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel < 1)
            {
                Console.WriteLine($"[INFO] {killer.PlayerName} does not have Consume unlocked. Aborting.");
                return;
            }
            Console.WriteLine($"[DEBUG] {killer.PlayerName} has Consume at Level {abilityLevel}.");

            // ✅ Debug Logs for Health Before Applying
            Console.WriteLine($"[DEBUG] Current Health: {killer.Health}");
            Console.WriteLine($"[DEBUG] Heal Amount: {abilityLevel} * 7 = {abilityLevel * 7}");

            // ✅ Get current HP
            int currentHealth = killer.PlayerPawn.Value.Health;

            // ✅ Calculate new HP
            int healAmount = abilityLevel * 7;
            int newHealth = currentHealth + healAmount;

            // ✅ Apply the health increase
            killer.SetHp(newHealth);

            Console.WriteLine($"[INFO] {killer.PlayerName} healed for {healAmount} HP (new total: {newHealth}).");




            // ✅ Teleport Naix to victim's last position
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

            // ✅ Refill Ammo Properly
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

            Console.WriteLine($"[SUCCESS] {killer.PlayerName} successfully used Consume on {victim.PlayerName}!");
        }


        private void SmokegrenadeDetonate(EventSmokegrenadeDetonate detonate)
        {
            var player = detonate.Userid;
            if (!activeEffects.TryGetValue(player, out var effect)) return;

            // ✅ Grant another smoke if below ability cap
            effect.GiveSmokeIfNeeded();
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

                // ✅ Retrieve WarcraftPlayer correctly
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

                // ✅ Remove existing smokes to prevent unintended stacking
                RemoveGrenades("weapon_smokegrenade");

                // ✅ Start with 1 smoke
                Console.WriteLine("[INFO] Granting initial smoke grenade.");
                Owner.GiveNamedItem("weapon_smokegrenade");
                smokesGiven = 1; // Track that we have given 1 smoke
            }


            // ✅ Called by `Naix` when a smoke detonates
            public void GiveSmokeIfNeeded()
            {
                Console.WriteLine($"[DEBUG] Checking if {Owner.PlayerName} needs a smoke.");

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
                Console.WriteLine($"[INFO] Smoke Supply Effect Finished for {Owner.PlayerName}");
            }

            // ✅ Helper function to remove old smokes
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

            // ✅ Required by WarcraftEffect (empty implementation)
            public override void OnTick() { }

        }

        private bool canUseUltimate = true; // ✅ Add this at the class level

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

            Console.WriteLine($"[INFO] Spawning explosion at last smoke position: {lastSmokePosition}");

            // ✅ Adjust explosion position slightly above ground
            var explosionPosition = lastSmokePosition.With(z: lastSmokePosition.Z + 10f);

            // ✅ Use predefined SpawnExplosion function
            Warcraft.SpawnExplosion(
                pos: explosionPosition,
                damage: 50f + (WarcraftPlayer.GetAbilityLevel(3) * 10f), // ✅ Damage scales with ability level
                radius: 250f,
                attacker: Player,
                killFeedIcon: KillFeedIcon.prop_exploding_barrel
            );


            Console.WriteLine($"[INFO] Explosion triggered at {explosionPosition} with {50f + (WarcraftPlayer.GetAbilityLevel(3) * 10f)} damage!");

            // ✅ Start Ultimate Cooldown
            canUseUltimate = false;
            WarcraftPlugin.Instance.AddTimer(6.0f, () => canUseUltimate = true); // Reset after 6 seconds
            StartCooldown(3);
        }

    }
}


