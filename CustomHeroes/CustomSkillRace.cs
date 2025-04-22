using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Models;



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
            HookAbility(3, Ultimate);
        }

        private void Ultimate()
        {
            StartCooldown(3); // Index 3 = Ultimate
        }


    }
}