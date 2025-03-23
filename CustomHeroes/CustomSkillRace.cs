using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;



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
            WarcraftPlugin.Instance.AddTimer(0.5f, () =>
            {
                BonusMovementSpeedh(Player, 6f, 999f);
                BonusHealth(Player, 8880);
                Invisibility(Player, 20f, 100);
            });
            
            //logging purposes below
            var pawn = Player.PlayerPawn.Value;
            var NewMovementSpeed = pawn.VelocityModifier;
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

            // Slowing effect
            // SkillFunctions.FreezePlayer(attacker, victim, 50, 3.5f); // 25% chance to freeze for 1.5 seconds



            // Lifesteal effect
            SkillFunctions.LeechHealth(attacker, victim, 50, 50f, @event.DmgHealth);// Player - ChancePercent - healPercent - int DamageDealt
            

        }



        private void OnPlayerPing(EventPlayerPing ping)
        {
            SkillFunctions.HandleTeleportPing(Player, ping.X, ping.Y, ping.Z);
        }
    }
}