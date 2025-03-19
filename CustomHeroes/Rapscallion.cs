using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Events.ExtendedEvents;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using WarcraftPlugin.Models;
using System;
using System.Collections.Generic;
using System.Drawing;



namespace WarcraftPlugin.Classes
{
    public class Rapscallion : WarcraftClass
    {
        private VanishEffect? activeVanishEffect = null;
        private readonly int _MedkitHealthMultiplier = 20;
        public override string DisplayName => "Rapscallion";
        private const int _StackCooldown = 20;
        public override Color DefaultColor => Color.White;
        private CCSPlayer_ItemServices? itemServices;
        private CCSPlayerController? player;
        private int movementStacks = 0;
        private bool isAlive = false;
        private SpeedOverTimeEffect? activeSpeedEffect;
        private bool hasSpawned = false;



        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Ninja skills", "Gain health and invisibility when planting or defusing a bomb"),
            new WarcraftAbility("Agility", "Up to 40% evasion and 180% movement speed"),
            new WarcraftAbility("Unseen Blade", "Additional knife damage"),
            new WarcraftCooldownAbility("Vanish", "Teleport, invis, freeze with reactivation to undo", 8f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventBombBeginplant>(BombBeginPlant);
            HookEvent<EventBombBegindefuse>(BombBeginDefuse);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventItemPickup>(OnItemPickup);
            HookEvent<EventRoundStart>(RoundStart);
            HookEvent<EventRoundEnd>(RoundEnd);
            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            Console.WriteLine("[DEBUG] PlayerSpawn triggered!");

            if (Player == null)
            {
                Console.WriteLine("ERROR: player is NULL at the start of PlayerSpawn! Assigning reference.");
            }

            if (Player == null)
            {
                Console.WriteLine("ERROR: Player is still NULL after delay! Skipping spawn setup.");
                return;
            }

            Console.WriteLine($"[INFO] Player assigned: {Player.PlayerName}");

            if (Player?.PlayerPawn?.Value == null)
            {
                Console.WriteLine("ERROR: PlayerPawn is NULL even after setting player!");
                return;
            }

            Console.WriteLine("[INFO] Running delayed spawn setup...");

            activeSpeedEffect?.Stop();
            activeVanishEffect = null;

            float duration = 88f;
            float tickRate = 11f;
            activeSpeedEffect = new SpeedOverTimeEffect(player, duration, tickRate);
            activeSpeedEffect.Start();

            movementStacks = 0;
            isAlive = true;

            // Prevent weapon pickups
            WarcraftPlugin.Instance.AddTimer(0.1f, () =>
            {
                if (Player?.PlayerPawn?.Value != null)
                {
                    Console.WriteLine($"[INFO] Preventing weapon pickup for {Player.PlayerName}");
                    SetWeaponPickupState(Player, true);
                }
            });

            if (Player?.PlayerPawn?.Value != null)
            {
                // Drop C4 before removing weapons
                player.DropWeaponByDesignerName("weapon_c4");
                Console.WriteLine("[INFO] Removing the bomb.");
            }

            Player.RemoveWeapons();

            // Remove weapons again after 1 second to make sure there's no gimmicky timings with the assignment of the starter pistol by server
            WarcraftPlugin.Instance.AddTimer(1.0f, () =>
            {
                if (Player?.PlayerPawn?.Value != null)
                {
                    Console.WriteLine($"[INFO] Removing weapons again to prevent starter pistol issue.");
                    Player.RemoveWeapons();
                    Player.PlayerPawn.Value.VelocityModifier = 1.0f;

                }

            });
        }

        private void ResetUltimate()
        {
            Console.WriteLine("[INFO] Resetting Ultimate!");

            activeVanishEffect = null;
            canUseUltimate = true; // ✅ Ensure the ability can be used again

            if (Player?.PlayerPawn?.Value != null)
            {
                SetUltimateVisibility(Player);

                // ✅ Temporarily allow pickups
                SetWeaponPickupState(Player, false);

                // ✅ Give the knife
                Console.WriteLine("Granting the knife...");
                Player.GiveNamedItem("weapon_knife");

                // ✅ Delay and give C4 or defuse kit
                WarcraftPlugin.Instance.AddTimer(0.1f, () =>
                {
                    if (Player.TeamNum == (byte)CsTeam.Terrorist)
                    {
                        Console.WriteLine("Granting C4 to the player...");
                        Player.GiveNamedItem("weapon_c4");
                    }
                    else if (Player.TeamNum == (byte)CsTeam.CounterTerrorist)
                    {
                        Console.WriteLine("Granting defuse kit to the player...");
                        var itemServices = new CCSPlayer_ItemServices(Player.PlayerPawn.Value.ItemServices.Handle);
                        itemServices.HasDefuser = true;
                    }
                });

                // ✅ Delay re-enabling `PreventWeaponPickup` slightly longer to prevent bomb dropping
                WarcraftPlugin.Instance.AddTimer(0.4f, () =>
                {
                    Console.WriteLine("Re-enabling weapon pickup prevention (delayed for C4).");
                    SetWeaponPickupState(Player, true);
                });

                // ✅ NEW: Start the cooldown AFTER everything has reset
                StartCooldown(3); // Cooldown of 3 seconds (adjust as needed)
                Console.WriteLine("[INFO] Ultimate cooldown started!");
            }

            Console.WriteLine("[INFO] Ultimate has been fully reset.");
        }






        private void Ultimate()
        {
            Console.WriteLine("[INFO] Ultimate ability activated!");

            if (Player == null || Player.PlayerPawn?.Value == null)
            {
                Console.WriteLine("ERROR: Player or PlayerPawn is NULL in Ultimate! Aborting.");
                return;
            }

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

            if (activeVanishEffect == null) // ✅ First Activation: Vanish & Dash
            {
                Console.WriteLine("Dashing and becoming invisible...");

                activeVanishEffect = new VanishEffect(this, Player);
                activeVanishEffect.Start();

                ApplyDashForce(Player);
            }
            else // ✅ Second Activation: Return to Normal
            {
                Console.WriteLine("Ultimate recast! Becoming visible again...");
                activeVanishEffect.EndEarly();

                // ✅ Reset Ultimate AFTER giving the player their weapons back
                WarcraftPlugin.Instance.AddTimer(0.1f, () =>
                {
                    ResetUltimate();
                });
            }

            // ✅ Start cooldown
            canUseUltimate = false;
            WarcraftPlugin.Instance.AddTimer(1.0f, () => canUseUltimate = true);
        }









        private void RoundStart(EventRoundStart @event)
        {
            if (Player != null)
            {
                Player.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = true;
                Console.WriteLine("ROUND HAS STARTED");
                // player.GiveNamedItem("weapon_knife"); // This isn't working yet

                // Stop any existing speed effect
                activeSpeedEffect?.Stop();
                activeSpeedEffect = null;

                // Start the SpeedOverTimeEffect
                float duration = 88f; // Duration, currently 88 seconds, 1 stack every 11 seconds, total seconds roundtime: 135. Result Can play with max stacks up to 30% of the round 
                float onTickInterval = 11f;
                activeSpeedEffect = new SpeedOverTimeEffect(Player, duration, onTickInterval);
                activeSpeedEffect.Start();


                WarcraftPlugin.Instance.AddTimer(0.1f, () =>
                {
                    if (Player?.PlayerPawn?.Value != null)
                    {
                        Console.WriteLine("[INFO] Re-enabling weapon pickup prevention at round start.");
                        SetWeaponPickupState(Player, true);
                    }
                });
            }
        }



        private void RoundEnd(EventRoundEnd @event)
        {
            if (Player != null)
            {
                activeSpeedEffect?.Stop();
                activeSpeedEffect = null;
                if (Player?.PlayerPawn?.Value != null)
                {
                    Console.WriteLine("[INFO] Re-enabling weapon pickup prevention at round start.");
                    SetWeaponPickupState(Player, true);
                }
            }
        }



        private bool hasC4Granted = false; // Tracks if the C4 was granted during the ultimate

        private void OnItemPickup(EventItemPickup @event)
        {
            if (@event.Userid == null) return;

            
            var pawn = Player.PlayerPawn?.Value;
            if (pawn == null) return;

            string weaponName = @event.Item.Replace("weapon_", "").Replace("item_", "");

            bool isT = Player.TeamNum == (byte)CsTeam.Terrorist;
            bool isCT = Player.TeamNum == (byte)CsTeam.CounterTerrorist;

            Console.WriteLine($"[DEBUG] {Player.PlayerName} attempted to pick up {weaponName}.");

            bool isAllowedWeapon = weaponName == "knife" ||
                                   (isT && weaponName == "c4") ||
                                   (isCT && weaponName == "defuser");

            if (isAllowedWeapon)
            {
                Console.WriteLine($"[ALLOW] {Player.PlayerName} picked up {weaponName}.");
                SetWeaponPickupState(Player, false);

                // Check if the bomb was picked up
                if (hasC4Granted && weaponName == "c4")
                {
                    Console.WriteLine("[INFO] C4 successfully picked up. Re-enabling weapon pickup prevention.");
                    hasC4Granted = false;
                    ReEnablePickupPrevention(Player);
                }
            }
            else
            {
                Console.WriteLine($"[BLOCK] {Player.PlayerName} is trying to pick up {weaponName}. Preventing pickup.");
                SetWeaponPickupState(Player, false);
            }
        }

        private void EnforcePreventWeaponPickup(CCSPlayerController Player)
        {
            if (Player?.PlayerPawn?.Value != null)
            {
                SetWeaponPickupState(Player, false);
            }
        }

        private void PlayerDeath(EventPlayerDeath @event)
        {
            Console.WriteLine("[DEBUG] PlayerDeath triggered!");

            if (Player == null)
            {
                Console.WriteLine("ERROR: Player is NULL during death event! This should not happen.");
                return;
            }

            if (!Player.IsValid)
            {
                Console.WriteLine("ERROR: Player is INVALID during death event! Aborting.");
                return;
            }

            Console.WriteLine($"[INFO] {Player.PlayerName} has died!");

            isAlive = false;
            ResetAgility();
            movementStacks = 0; // Reset stacks on death

            if (Player?.PlayerPawn?.Value != null)
            {
                Console.WriteLine($"[INFO] Removing weapon pickup prevention for {player.PlayerName}");
                SetWeaponPickupState(Player, false);
            }
            else
            {
                Console.WriteLine("WARNING: PlayerPawn.Value is NULL during death! Skipping weapon pickup modification.");
            }

            // Stop any active speed effect
            activeSpeedEffect?.Stop();
            activeSpeedEffect = null;

            // Clear vanish effect on death
            if (activeVanishEffect != null)
            {
                Console.WriteLine("[INFO] Clearing Vanish effect due to death.");
                activeVanishEffect.EndEarly();
                activeVanishEffect = null;
            }
        }





        private void PlayerHurt(EventPlayerHurt @event)
        {
            if (Player == null) return;
            HandleEvasion(@event);
        }


        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            if (@event.Weapon == "knife")
            {
                var damageBonus = WarcraftPlayer.GetAbilityLevel(2) * 12;
                @event.AddBonusDamage(damageBonus);
                Player.PrintToChat($"Damage bonus, dealt {damageBonus} extra damage!");
            }
        }

        private void HandleEvasion(EventPlayerHurt @event)
        {
            if (Player == null || !isAlive) return;

            int evasionChance = Math.Clamp(8 + (movementStacks * 4), 10, 40);

            var roll = Random.Shared.Next(100);
            if (roll < evasionChance)
            {
                Console.WriteLine($"Evasion triggered! Chance: {evasionChance}% (Roll: {roll})");
                @event.IgnoreDamage();

                // Provide user feedback
                Player.PrintToChat("Agility saved you! You evaded the attack.");
                Player.EmitSound("Weapon_M4A1.Silenced"); // Don't know which sound I'm gonna be using here yet
            }
        }

        internal class SpeedOverTimeEffect : WarcraftEffect
        {
            private int movementStacks = 0;
            private const int MaxStacks = 8;
            private const float BaseSpeedMultiplier = 1f;
            private const float StackIncrement = 0.1f; // 10% speed increase per stack

            public SpeedOverTimeEffect(CCSPlayerController owner, float duration, float onTickInterval)
                : base(owner, duration, onTickInterval: onTickInterval) { }

            public override void OnStart()
            {
                if (Owner?.PlayerPawn?.Value != null)
                {
                    Owner.PlayerPawn.Value.VelocityModifier = BaseSpeedMultiplier;
                    Console.WriteLine("SpeedOverTimeEffect started: Speed reset to base.");
                }
                else
                {
                    Console.WriteLine("ERROR: Owner or PlayerPawn is NULL during OnStart!");
                }
            }

            public override void OnTick()
            {
                if (Owner?.PlayerPawn?.Value == null || movementStacks >= MaxStacks) return;

                movementStacks++;
                float newSpeedMultiplier = BaseSpeedMultiplier + (StackIncrement * movementStacks);

                if (Owner.PlayerPawn.Value.VelocityModifier != null)
                {
                    Owner.PlayerPawn.Value.VelocityModifier = newSpeedMultiplier;
                    Console.WriteLine($"Speed increased: {newSpeedMultiplier:P2} (Stacks: {movementStacks})");
                }
                else
                {
                    Console.WriteLine("ERROR: VelocityModifier is NULL during OnTick!");
                }
            }

            public override void OnFinish()
            {
                if (Owner?.PlayerPawn?.Value != null)
                {
                    Owner.PlayerPawn.Value.VelocityModifier = BaseSpeedMultiplier;
                    Console.WriteLine("SpeedOverTimeEffect finished: Speed reset to base.");
                }
                else
                {
                    Console.WriteLine("ERROR: Owner or PlayerPawn is NULL during OnFinish!");
                }
            }

            public void Stop()
            {
                Duration = 0;
                this.OnFinish();
            }


        }


        private void ResetAgility()
        {
            Console.WriteLine("Resetting agility state...");

            movementStacks = 0;

            if (Player?.PlayerPawn?.Value != null)
            {
                Player.PlayerPawn.Value.VelocityModifier = 1.0f; // Reset to normal speed
            }
        }


        private bool canUseUltimate = true; // Prevents spam activation
        private bool isInvisible = false;

        internal class VanishEffect(Rapscallion parent, CCSPlayerController owner) : WarcraftEffect(owner)
        {
            private readonly Rapscallion _parent = parent;
            public bool IsActive { get; private set; } = true;

            public override void OnStart()
            {
                if (Owner?.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("ERROR: Owner or PlayerPawn is NULL during Ultimate activation!");
                    return;
                }

                Console.WriteLine($"{Owner.PlayerName} activated Vanish!");

                _parent.SetUltimateInvisibility(Owner);

                // ✅ Drop C4 before removing weapons
                if (Owner.TeamNum == (byte)CsTeam.Terrorist)
                {
                    Console.WriteLine("[INFO] Dropping C4...");
                    Owner.DropWeaponByDesignerName("weapon_c4");
                }

                // ✅ Remove ALL weapons (including knife)
                Console.WriteLine("[INFO] Removing all weapons...");
                _parent.ClearWeaponsForTeam(Owner);

                // ✅ Allow pickups for a very short duration while weapons are removed
                _parent.SetWeaponPickupState(Owner, false);


                // ✅ Ensure pickup prevention is re-enabled after 0.3s
                WarcraftPlugin.Instance.AddTimer(0.3f, () =>
                {
                    if (Owner?.PlayerPawn?.Value != null)
                    {
                        Console.WriteLine("[INFO] Re-enabling weapon pickup prevention after invisibility.");
                        _parent.SetWeaponPickupState(Owner, true);

                    }
                });
            }

            public override void OnFinish()
            {
                if (Owner?.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("ERROR: Owner or PlayerPawn is NULL during Ultimate deactivation!");
                    return;
                }

                Console.WriteLine($"{Owner.PlayerName} reappeared!");

                _parent.SetUltimateVisibility(Owner);

                IsActive = false;
            }

            public void EndEarly()
            {
                Console.WriteLine($"[DEBUG] Manually ending Vanish for {Owner.PlayerName}");
                OnFinish();
            }

            public override void OnTick() {/* Required override but not used */}
        }


        private void ClearWeaponsForTeam(CCSPlayerController Player)
        {
            if (Player?.PlayerPawn?.Value == null) return;

            // Step 1: Remove the C4 explicitly if it exists
            Console.WriteLine("Attempting to remove C4 specifically...");
            Player.GiveNamedItem("weapon_knife");
            WarcraftPlugin.Instance.AddTimer(0.1f, () =>
            {
                Console.WriteLine("Removing all weapons, including potential C4...");
                Player.RemoveWeapons();
            });

            // Step 2: Handle defuse kit for CTs
            if (Player.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                itemServices.HasDefuser = false;
                Console.WriteLine("Defuse kit removed for Counter-Terrorist.");
            }

            // Step 3: Re-enable pickup prevention to block further weapon pickups
            WarcraftPlugin.Instance.AddTimer(0.2f, () =>
            {
                Console.WriteLine("Re-enabling weapon pickup prevention...");
                SetWeaponPickupState(Player, true);
            });
        }


        private void ReEnablePickupPrevention(CCSPlayerController Player)
        {
            if (Player?.PlayerPawn?.Value != null)
            {
                WarcraftPlugin.Instance.AddTimer(0.1f, () =>
                {
                    SetWeaponPickupState(Player, true);
                    Console.WriteLine("Weapon pickup prevention re-enabled.");
                });
            }
        }


        private void ReactivateUltimate(CCSPlayerController Player)
        {
            if (Player == null || Player.PlayerPawn?.Value == null) return;

            // ✅ Temporarily allow pickups
            SetWeaponPickupState(Player, false);

            // ✅ Give the knife
            GiveKnife(Player);

            // ✅ Delay and give the bomb or defuse kit
            WarcraftPlugin.Instance.AddTimer(0.2f, () =>
            {
                if (Player.TeamNum == (byte)CsTeam.Terrorist)
                {
                    Console.WriteLine("Giving C4 to the player.");
                    Player.GiveNamedItem("weapon_c4");
                }
                else if (Player.TeamNum == (byte)CsTeam.CounterTerrorist)
                {
                    Console.WriteLine("Giving defuse kit to the player.");
                    itemServices.HasDefuser = true;
                }
            });

            // ✅ Re-enable pickup prevention after 0.3s
            WarcraftPlugin.Instance.AddTimer(0.3f, () =>
            {
                SetWeaponPickupState(Player, true);
            });
        }

        private void SetWeaponPickupState(CCSPlayerController Player, bool state)
        {
            if (Player?.PlayerPawn?.Value == null) return;

            Console.WriteLine($"[INFO] Setting PreventWeaponPickup to {state} for {Player.PlayerName}");
            Player.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = state;
        }


        private void GiveKnife(CCSPlayerController Player)
        {
            Console.WriteLine("Giving knife to the player.");
            Player.GiveNamedItem("weapon_knife");
        }


        private void ApplyDashForce(CCSPlayerController Player)
        {
            if (Player == null || Player.PlayerPawn?.Value == null)
            {
                Console.WriteLine("ERROR: Player or PlayerPawn is NULL in ApplyDashForce!");
                return;
            }

            var directionAngle = Player.PlayerPawn.Value.EyeAngles; // getting the diretion for the dash
            var directionVec = new Vector();
            NativeAPI.AngleVectors(directionAngle.Handle, directionVec.Handle, nint.Zero, nint.Zero);

            if (directionVec.Z < 0.375)
            {
                directionVec.Z = 0.375f;
            }
            directionVec *= 1800; // The force for the Dash, adjust if the dash is too powerfull/weak
            Player.PlayerPawn.Value.AbsVelocity.X = directionVec.X;
            Player.PlayerPawn.Value.AbsVelocity.Y = directionVec.Y;
            Player.PlayerPawn.Value.AbsVelocity.Z = directionVec.Z;
        }

        private void SetUltimateInvisibility(CCSPlayerController Player)
        {
            isInvisible = true;
            Player.PrintToCenter("You have become invisible!");
            Player.PlayerPawn.Value.SetColor(Color.FromArgb(0, 255, 255, 255)); // Fully invisible
            Player.DisableMovement();
        }

        private void SetUltimateVisibility(CCSPlayerController Player)
        {
            isInvisible = false;
            Player.PrintToCenter("You are now visible!");
            Player.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255)); // Fully visible
            Player.EnableMovement();

        }

        private void HandleBombEvent(CCSPlayerController Player)
        {
            if (Player == null)
            {
                Console.WriteLine("ERROR: Player is NULL! Maybe they haven't spawned yet?");
                return;
            }

            if (Player.PlayerPawn == null || Player.PlayerPawn.Value == null)
            {
                Console.WriteLine("ERROR: PlayerPawn or PlayerPawn.Value is NULL!");
                return;
            }

            if (WarcraftPlayer == null)
            {
                Console.WriteLine("ERROR: WarcraftPlayer is NULL!");
                return;
            }
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0);


            int newHealth = 100 + abilityLevel * _MedkitHealthMultiplier;
            Console.WriteLine($"Setting player health to {newHealth}");

            Player.SetHp(newHealth);
            Player.PlayerPawn.Value.MaxHealth = Player.PlayerPawn.Value.Health;

            // Make the playr invisible for 5 seconds for plant/ defuse actions
            Console.WriteLine("Making player invisible!");
            SetInvisible(Player, 5);

            Console.WriteLine("Popping smoke");
            var smoke = Warcraft.SpawnSmoke(Player.PlayerPawn.Value.AbsOrigin.Clone().Add(z: 5), Player.PlayerPawn.Value, Color.Black);
            smoke.SpawnTime = 0;
            smoke.Teleport(velocity: Vector.Zero);
        }

        private void BombBeginPlant(EventBombBeginplant @event)
        {
            HandleBombEvent(Player);
        }

        private void BombBeginDefuse(EventBombBegindefuse @event)
        {
            HandleBombEvent(Player);
        }

        private void SetInvisible(CCSPlayerController player, float duration)
        {
            new Invisibility(Player, duration).Start();
        }
    }

    public class Invisibility(CCSPlayerController owner, float duration) : WarcraftEffect(owner, duration)
    {
        public override void OnStart()
        {
            Owner.PrintToCenter("You are now invisible!");
            Owner.PlayerPawn.Value.SetColor(Color.FromArgb(0, 255, 255, 255));
        }

        public override void OnTick() { }

        public override void OnFinish()
        {
            Owner.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255));
            Owner.PrintToCenter("You are now visible!");
        }
    }

}
