using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CounterStrikeSharp.API.Modules.Memory;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using System.Drawing;
using WarcraftPlugin.Core.Effects;
using System.Collections.Generic;
using WarcraftPlugin.Events.ExtendedEvents;
using System;
using System.Reflection;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using WarcraftPlugin.Core;


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
          
            HookEvent<EventWeaponFire>(PlayerShoot);
            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            //WarcraftPlugin.Instance.AdminPanel.OpenAdminPanel(Player);
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









