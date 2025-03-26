using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Numerics;

namespace WarcraftPlugin.Classes
{
    public class Charizard : WarcraftClass
    {
        public override string DisplayName => "Charizard";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Flamethrower", "Chance to burn enemies for up to 5 seconds. "),
            new WarcraftAbility("Wing Attack", "Your attacks have a chance to push the target back."),
            new WarcraftAbility("Sunny Day", "Enemies shooting you have a chance to get blinded and miss instead."),
            new WarcraftCooldownAbility("Overheat","After charging for a short amount of time, unleash a massive explosion of energy.", 1f)
        ];

        /* 
        TODO: Special Idea:
            - Charizard deals double damage against Venusaur and receives half damage from Venusaur
            - Venusaur deals double damage against Blastoise and receives half damage from Blastoise
            - Blastoise deals double damage against Charizard and receives half damage from Charizard
        */

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            // TODO: Module : 
        }
        private void Ultimate()
        {
            // TODO: Overheat: Use big sfx explosion, to deal damage in a massive radius
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            // TODO: Sunny Day : Blind enemies that hit you, evade damage;  both on %
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO:  Wing Attack  : chance to push enemies back
            // TODO:  Flamethrower  : Chance to burn enemies
        }

    }
}