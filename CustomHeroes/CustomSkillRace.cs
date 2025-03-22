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

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            Console.WriteLine("CustomSkillRace has spawned!");
            SkillFunctions.MovementSpeed(Player, 5f, 20f);
            var NewMovementSpeed = pawn.VelocityModifier;
            Console.WriteLine($"You have {NewMovementSpeed} Speed");
        }

        private void Ultimate()
        {
            Console.WriteLine("CustomSkillRace used ultimate!");
        }
    }
}