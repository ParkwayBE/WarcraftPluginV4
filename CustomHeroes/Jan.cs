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


namespace WarcraftPlugin.Classes
{
    public class Jan : WarcraftClass
    { 
        public override string DisplayName => "Jan";


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

            Player.RemoveWeapons();
            WarcraftPlugin.Instance.AddTimer(0.1f, () => {
                Player.GiveNamedItem("weapon_knife");
                Player.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;
            });
        }

        private void Ultimate()
        {
            Console.WriteLine("Jan used ultimate!");
        }

  


    }
}









