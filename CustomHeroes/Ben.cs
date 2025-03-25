using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;


namespace WarcraftPlugin.Classes
{
    public class Ben : WarcraftClass
    {
        public override string DisplayName => "Ben";


        public override Color DefaultColor => Color.CadetBlue;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("DoSomething", "Quick"),
            new WarcraftAbility("Monkey Agility", "Increases movement speed and evasion."),
            new WarcraftAbility("Primal Roar", "Emits a roar when killing an enemy that stuns nearby enemies."),
            new WarcraftCooldownAbility("Jungle Fury", "Temporarily increases attack speed and damage.", 60f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventWeaponFire>(PlayerShoot);
            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {

        }

        private void Ultimate()
        {
            Console.WriteLine("Ben used ultimate!");
        }

        private void PlayerShoot(EventWeaponFire @event)
        {


        }


    }
}









