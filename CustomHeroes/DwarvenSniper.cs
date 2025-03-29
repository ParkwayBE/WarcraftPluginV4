using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class DwarvenSniper : WarcraftClass
    {
        public override string DisplayName => "Dwarven Sniper";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Eagle eye", "Increased damage with scoped weapons."),
            new WarcraftAbility("Dwarven Genes", "Evasion and increased health"),
            new WarcraftAbility("Supplies", "Occasionally grants a grenade and chance to spawn with a Scout or AWP"),
            new WarcraftCooldownAbility("Ring of power","For the next 5 seconds you double your evasion and the first player to look at you gets impaled.", 1f) // TODO: If not possible to code this skill then adapt it, but stay on the theme.
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
                SkillFunctions.SetBonusHealth(Player, 9999);
                SkillFunctions.SetEvasion(Player, 50, 1.0f); // 50% chance to evade 100% of the dmg
                // TODO: Dwarven Genes: Increased health
                // TODO: Supplies: Occasionally grants a grenade and chance to spawn with either scout or awp, Maybe 50/50 at level 5 going down to 10/90 in favor of the scout at level 1
            });
        }

        private void Ultimate()
        {
            // TODO: Ring of power : Double evasion
            // TODO: Ring of power : Attempt to code the impale skill
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO: Eagle Eye : Increased damage with scoped weapons
        }

    }
}