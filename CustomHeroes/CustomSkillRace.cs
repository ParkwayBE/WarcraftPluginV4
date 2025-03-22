using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using WarcraftPlugin.CustomSkills;


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
            new WarcraftCooldownAbility("TEST TELEPORT", " TEST ", 60f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);

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

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            Console.WriteLine("CustomSkillRace has spawned!");
            BonusMovementSpeedh(Player, 6f, 20f);
            BonusHealth(Player, 80);
            //logging purposes below
            var pawn = Player.PlayerPawn.Value;
            var NewMovementSpeed = pawn.VelocityModifier;
            Console.WriteLine($"You have {NewMovementSpeed} Speed");
        }

        private void Ultimate()
        {
            Console.WriteLine("CustomSkillRace used ultimate!");
        }
    }
}