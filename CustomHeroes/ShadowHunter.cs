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
    public class ShadowHunter : WarcraftClass
    {
        public override string DisplayName => "Shadow Hunter";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Healing Wave", "You and your teammates gain additional health on spawn."),
            new WarcraftAbility("Hex", "6-30% chance to remove all bonushealth, bonus speed and invisibility from your target."),
            new WarcraftAbility("Serpent Ward", "Place a ward that damages and slows nearby enemies."),
            new WarcraftCooldownAbility("Big Bad Voodoo","Become immune to all damage for the next 0.6-3seconds", 8f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);


            // TODO: Find how we can do the Serpent Ward ability, we might have to take a look at how ranger gets his beartrap.

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            // TODO: Increase all allies' health by up to 30
        }

        private void Ultimate()
        {
            // TODO: Ultimate god mode, max 3 seconds
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO: Hex : Chance to completely remove all playerspawn buffs a player might've gotten
        }

    }
}