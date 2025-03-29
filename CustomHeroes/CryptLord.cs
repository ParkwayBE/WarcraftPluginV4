using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class CryptLord : WarcraftClass
    {
        public override string DisplayName => "Crypt Lord";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Impale", "Chance to impale your targets."),
            new WarcraftAbility("Spiked Carapace", "Knife attacks reflect up to 200% damage back."),
            new WarcraftAbility("Carrion Beetles", "Beetles will save you from most ultimates up to 100% succesrate."),
            new WarcraftCooldownAbility("Locust Swarm","Steal up to 50hp from a random enemy player", 1f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            // TODO: Carrion Beetles: Ultimate immunity
        }

        private void Ultimate()
        {
            // TODO: Locust Swarm : Steal up to 50 health from a random enemy
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO:  Impale : chance to push enemy up and reducing his gravity temp for the effect
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            // TODO: Spiked Carapace : Reflect knife damage skill.
        }

    }
}