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
    public class Venusaur : WarcraftClass
    {
        public override string DisplayName => "Venusaur";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Mega drain", "Heal for a percentage of the damage dealt. "),
            new WarcraftAbility("Vine Snare", "Chance to root enemies. Rooted enemies have lower accuracy."),
            new WarcraftAbility("Solar Beam", "Your attacks have a chance to fire a solar beam after a short delay."),
            new WarcraftCooldownAbility("Leech Seed","Place a seed underneath the player. The first time a player is too close to the seed, root them and drain health from them.", 1f)
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
            // TODO: Leech seed: Place a totemlike "seed" that acts like a ward that damages nearby players, 
            // heals you wherever you are (if not dead) and roots the first player that gets too close to the seed.

            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO:  Solar Beam  : chance to spawn a solar beam on your target, it's a delayed beam of energy that lands on the spot where the enemy was when the skill activates
            // TODO:  Vine Snare  : Chance to root enemies + rooted enemies have lower accuracy
            // TODO:  Mega Drain  : heal for x percent of damage dealt.
        }

    }
}