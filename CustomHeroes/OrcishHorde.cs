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
    public class OrcishHorde : WarcraftClass
    {
        public override string DisplayName => "Orcish Horde";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Critical Strike", "up to 35% to deal double damage."),
            new WarcraftAbility("Reincarnation", "Gain up to 100% chance to respawn once after dying"),
            new WarcraftAbility("Critical Grenade", "up to 100% chance to deal double damage"),
            new WarcraftCooldownAbility("Chain Lightning", " Fire a chainlightning to a nearby target, it may bounce up to 3 times. ", 8f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
        }



        private void Ultimate()
        {
            // Chain lightning
            StartCooldown(3); // Index 3 = Ultimate
        }


        private void PlayerDeath(EventPlayerDeath death)
        {
            // reincarnation skill
        }
        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // Extra damage
            // Extra Damage with nades
        }

    }
}