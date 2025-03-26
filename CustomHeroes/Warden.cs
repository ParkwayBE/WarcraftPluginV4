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
    public class Warden : WarcraftClass
    {
        public override string DisplayName => "Warden"; // TODO: knife only race
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Sharp End", "Chance for your attacks to deal bleed damage"),
            new WarcraftAbility("Mercy or Revenge", "Upon dying you have a 50% to either deal damage to the player that killed you or healing him but reviving yourself. Max 1 time per round"),
            new WarcraftAbility("Fan Of Knives", "Your knife attacks are throwing knife attacks."),
            new WarcraftCooldownAbility("Eternal Darkness","Engulf all nearby players in darkness and slow them.", 1f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);

            HookAbility(3, Ultimate);
        }

        private void PlayerDeath(EventPlayerDeath death)
        {
            // TODO: 50% chance to either revive the player and heal the killer. Or Damage the killer
        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            // TODO: Knife only: restrict the player to using his knife
            // TODO: Fan Of Knive: Code a throwing knife ability hint: Author made a race that lets it rain knives, might be worth taking a look
        }

        private void Ultimate()
        {
            // TODO: Ring of power : Blind enemies with darkness
            // TODO: Ring of power : Slow enemies
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO: Sharp End : Your knife and throwing knife attacks deal big bleed damage
        }

    }
}