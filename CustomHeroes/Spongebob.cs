using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class Spongebob : WarcraftClass
    {
        public override string DisplayName => "Spongebob";
        public override Color DefaultColor => Color.Yellow;
        private bool isUltimateActive = false;
        private const uint IN_DUCK = 1 << 2;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Yummy Yummy", "5%-25% chance to refill your clip when getting shot."),
            new WarcraftAbility("Shock Absorber", "20-100% chance to reduce incoming headshot damage by 20%-60%."),
            new WarcraftAbility("Slippery Sponge", "Double your movement speed for 3s when crouching. 10second cooldown and max 3 uses per round."),
            new WarcraftCooldownAbility("Ultimate Sponge","Become a sponge for 3s during which all incoming damage is converted to health. Max HP: 200.", 25f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurt>(PlayerHurt);

            HookAbility(3, Ultimate);
        }

        public class SlipperySpongeSpeedBoostEffect : WarcraftEffect
        {
            private float originalSpeed;
            private bool hasActivatedSpeed = false;
            private int usesRemaining = 3;

            private readonly int abilityLevel;

            private static readonly Dictionary<ulong, float> Cooldowns = new();

            public SlipperySpongeSpeedBoostEffect(CCSPlayerController player, float duration = 5.0f)
                : base(player, duration)
            {
                var wp = player.GetWarcraftPlayer();
                abilityLevel = wp.GetAbilityLevel(2); 
            }

            public override void OnStart()
            {
                originalSpeed = Owner.PlayerPawn.Value.VelocityModifier;
            }

            public override void OnTick()
            {
                var movementServices = Owner.PlayerPawn.Value.MovementServices;
                bool isDucking = (movementServices.Buttons.ButtonStates[0] & IN_DUCK) != 0;
                ulong steamId = Owner.SteamID;

                if (isDucking && !hasActivatedSpeed && abilityLevel > 0 && usesRemaining > 0)
                {
                    float currentTime = Server.CurrentTime;

                    if (!Cooldowns.TryGetValue(steamId, out var readyTime) || currentTime >= readyTime)
                    {
                        float cooldown = 15f - (abilityLevel - 1);
                        Cooldowns[steamId] = currentTime + cooldown;

                        float speed = 2.0f + 0.1f * (abilityLevel - 1);
                        Owner.PlayerPawn.Value.VelocityModifier = speed;
                        hasActivatedSpeed = true;
                        usesRemaining--;

                        Owner.PrintToChat($"{ChatColors.Green}[WCS] {ChatColors.Default}Slippery Sponge activated! ({3 - usesRemaining}/3 used)");
                    }
                }
            }


            public override void OnFinish()
            {
                Owner.PlayerPawn.Value.VelocityModifier = originalSpeed;
            }

            public void ResetUses()
            {
                usesRemaining = 3;
            }

        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);

            if (abilityLevel > 0)
            {
                var effect = new SlipperySpongeSpeedBoostEffect(Player, 999F);
                effect.ResetUses();
                effect.Start();
            }
        }
        private void Ultimate()
        {
            if (!Player.IsAlive()) return;

            isUltimateActive = true;
            Player.PlayerPawn.Value.SetColor(Color.Green);
            StartCooldown(3);

            Player.PrintToChat($" {ChatColors.Green}[WCS] {ChatColors.Default}You have become a sponge for 2 seconds!");

            WarcraftPlugin.Instance.AddTimer(3.0f, () =>
            {
                isUltimateActive = false;
                Player.PrintToChat($" {ChatColors.Green}[WCS] {ChatColors.Default}Sponge mode ended.");
                Player.PlayerPawn.Value.SetColor(Color.Yellow);
            });
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if (attacker == null || victim == null || !victim.IsValid || !attacker.IsValid || !victim.IsAlive()) return;

            var wp = victim.GetWarcraftPlayer();
            int level = wp.GetAbilityLevel(0);

            if (isUltimateActive)
            {
                int newHp = Math.Min(victim.Health + @event.DmgHealth, 200);
                victim.SetHp(newHp);
                @event.IgnoreDamage();
                return;
            }

            int ghostLevel = wp.GetAbilityLevel(1);
            if (@event.Hitgroup == (int)HitGroup.Head)
            {
                int[] chanceLevels = { 20, 40, 60, 80, 90, 100 };
                int[] reductionPercent = { 20, 30, 40, 50, 55, 60 };
                int chance = chanceLevels[Math.Clamp(ghostLevel - 1, 0, 5)];
                int reduceByPercent = reductionPercent[Math.Clamp(ghostLevel - 1, 0, 5)];

                if (Random.Shared.Next(0, 100) < chance)
                {
                    float multiplier = (100f - reduceByPercent) / 100f;
                    @event.DmgHealth = (int)(@event.DmgHealth * multiplier);
                }
            }

            int yummyChance = new[] { 5, 10, 15, 20, 25, 30 }[Math.Clamp(level, 0, 5)];
            if (@event.DmgHealth > 0 && Random.Shared.Next(0, 100) < yummyChance)
            {
                var activeWeapon = victim.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value;
                if (activeWeapon != null)
                {
                    activeWeapon.Clip1 = activeWeapon.GetVData<CBasePlayerWeaponVData>().MaxClip1;
                    Console.WriteLine($"[INFO] {victim.PlayerName}'s ammo refilled to max ({activeWeapon.Clip1})!");
                }
            }
        }

    }

    private enum HitGroup
    {
        Generic = 0,
        Head = 1,
        Chest = 2,
        Stomach = 3,
        LeftArm = 4,
        RightArm = 5,
        LeftLeg = 6,
        RightLeg = 7,
        Gear = 10
    }

}