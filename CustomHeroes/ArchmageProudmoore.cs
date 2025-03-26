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
    public class ArchmageProudmoore : WarcraftClass
    {
        public override string DisplayName => "Archmage Proudmoore";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Blizzard", "Chance to slow your target and obscure his vision ."),
            new WarcraftAbility("Water Elemental", "When you kill a player you have a chance to revive a teammate as a Water Elemental."),
            new WarcraftAbility("Brilliance Aura", "You and up to two random allies have a chance to block some ultimates."),
            new WarcraftCooldownAbility("Flight","Conjure a spell that allows you to fly.", 1f)
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
            // TODO: Give player and up to 2 allies a chance to block certain ultimates from damaging or firing off? to be determined
        }

        private void Ultimate()
        {
            // TODO: Flight on toggle ultimate ON/OFF use Movetype fly
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO: Chance to slow target and obscure his vision
            // TODO: Chance to revive an ally as a water elemental, 250 health and only a knife.
        }

    }
}