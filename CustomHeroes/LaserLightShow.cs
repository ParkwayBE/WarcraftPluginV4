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
    public class LaserLightShow : WarcraftClass
    {
        public override string DisplayName => "Laser Light Show";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Module R", "Increased Movement speed and health on spawn."),
            new WarcraftAbility("Module G", "Your attacks have a chance to deal bonus damage."),
            new WarcraftAbility("Module B", "Your attacks can chain through enemies."),
            new WarcraftCooldownAbility("Disintigrate","Upon activation: After a brief delay fire a beam of energy damaging all players that are too close.", 1f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            // TODO: Module R: Grant movement speed and health based on level, start a loop to loop through the RGB colors for the playermodel.
        }

        private void Ultimate()
        {
            // TODO: Disintegrate: DrawLaserBetween multiple in a circle shaped pattern maybe , --->
            // --->  different colors, after a brief delay create an explosion at the location of the end of the laser.
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO:  Module G : chance to deal bonus damage on hits
            // TODO:  Module B : Chance to make attacks chain to nearby players.
        }

    }
}