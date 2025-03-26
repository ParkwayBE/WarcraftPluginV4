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
    public class NightElves : WarcraftClass
    {
        public override string DisplayName => "Night Elves";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Evasion", "Gain up to 75% invisibility"),
        new WarcraftAbility("Thorns Aura", "Gain up to 90 bonus starting health"),
        new WarcraftAbility("Trueshot Aura", "5-25% to freeze your target for 1-3 seconds"),
        new WarcraftCooldownAbility("Root", " Root nearby players for 3 seconds! ", 8f)
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
        }



        private void Ultimate()
        {
            // TODO: Root and effect
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO: Extra damage
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            // TODO:Evasion
        }

    }
}