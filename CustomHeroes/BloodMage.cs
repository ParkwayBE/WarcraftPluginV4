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
    public class BloodMage : WarcraftClass
    {
        public override string DisplayName => "Blood Mage";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Flame Strike", "Calls down flames to damage enemies when hitting an enemy."),
            new WarcraftAbility("Banish", "Obscure your target's screen, Enemies close to the target take additional damage for the next 5-10 seconds."),
            new WarcraftAbility("Siphon Mana", "5-25% to siphon money from your target"),
            new WarcraftCooldownAbility("Phoenix", "If you activated your ultimate in the last 10 seconds when you die. You will respawn yourself and up to 2 teammates.! ", 8f)
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

        private void PlayerDeath(EventPlayerDeath death)
        {
            // TODO: When you die, check for recently activated ult, if so --> respawn yourself and up to 2 teammates
        }

        private void Ultimate()
        {
            // TODO: 
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO: Chance to spawn a molotov at the feet of the target. Flamestrike
            // TODO: Stealing money skill over here
        }

    }
}