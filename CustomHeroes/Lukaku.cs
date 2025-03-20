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
using WarcraftPlugin.Summons;


namespace WarcraftPlugin.Classes
{
    public class Lukaku : WarcraftClass
    {
        public override string DisplayName => "Lukaku";

        private readonly WarcraftPlugin _plugin;
        public override Color DefaultColor => Color.CadetBlue;

        List<FootBall> footBalls = new List<FootBall>();


        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("DoSomething", "Quick"),
            new WarcraftAbility("Monkey Agility", "Increases movement speed and evasion."),
            new WarcraftAbility("Primal Roar", "Emits a roar when killing an enemy that stuns nearby enemies."),
            new WarcraftCooldownAbility("Ballr", "Balls", 60f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventWeaponFire>(PlayerShoot);
            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            footBalls.Add(new FootBall(Player, Player.PlayerPawn.Value.AbsOrigin));
            footBalls.Add(new FootBall(Player, Player.PlayerPawn.Value.AbsOrigin));
            footBalls.Add(new FootBall(Player, Player.PlayerPawn.Value.AbsOrigin));
            //WarcraftPlugin.Instance.AddTimer(0.2f, () => {Player.PlayLocalSound("sounds/ambient/misc/techno_overpass.vsnd"); });
            Player.PrintToChat("you have 3 footballs");
        }

        private void Ultimate()
        {
            if (footBalls.Count > 0)
            {
                Player.PrintToChat("Lukaku has used a ball!");
                footBalls[0].Activate();
                footBalls.RemoveAt(footBalls.Count - 1);
            }


            StartCooldown(3);
        }

        private void PlayerShoot(EventWeaponFire @event)
        {
            // _plugin.AdminPanel.OpenAdminPanel(Player);

        }


    }
}









