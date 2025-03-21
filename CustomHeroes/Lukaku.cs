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
using static g3.RoundRectGenerator;
using CounterStrikeSharp.API.Modules.Entities;


namespace WarcraftPlugin.Classes
{
    public class Lukaku : WarcraftClass
    {
        public override string DisplayName => "Lukaku";

        private readonly WarcraftPlugin _plugin;
        public override Color DefaultColor => Color.CadetBlue;

        FootBall football;
        int footballsRemaining = 10;
        bool footballIsSpawned = false;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("DoSomething", "Quick"),
            new WarcraftAbility("Monkey Agility", "Increases movement speed and evasion."),
            new WarcraftAbility("Primal Roar", "Emits a roar when killing an enemy that stuns nearby enemies."),
            new WarcraftCooldownAbility("Ballr", "Balls", 0.5f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventWeaponFire>(PlayerShoot);
            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            footballsRemaining = 10;
            //WarcraftPlugin.Instance.AddTimer(0.2f, () => {Player.PlayLocalSound("sounds/ambient/misc/techno_overpass.vsnd"); });
            Player.PrintToChat("you have 3 footballs");
        }

        private void Ultimate()
        {
            Console.WriteLine("pressed ult");
            if (!footballIsSpawned)
            {
                Console.WriteLine("You don't have a football");
                if (footballsRemaining > 0)
                {
                    Console.WriteLine("decreasing available balls and spawn the football");
                    football = new FootBall(Player);
                    footballIsSpawned = true;
                    footballsRemaining--;
                    StartCooldown(3);
                }

            }
            else
            {
                Console.WriteLine("Already have a ball,");
                football.StayPut(Player);
                footballIsSpawned = false;
                WarcraftPlugin.Instance.AddTimer(15f, () =>
                {
                    football.DestroyBall();
                });

                StartCooldown(3);
            }

        }

        private void PlayerShoot(EventWeaponFire @event)
        {
            // _plugin.AdminPanel.OpenAdminPanel(Player);

        }
        /*
        private void FootballUse()
        {
            int maxSeconds = 5;
            DateTime startTime = DateTime.Now;
            footBalls.Add(new FootBall(Player, Player.PlayerPawn.Value.AbsOrigin));
            footBalls[0].Activate();
            Player.PrintToChat("You have used a ball");
            

            while ((DateTime.Now - startTime).TotalSeconds < maxSeconds)
            {
                footBalls[0].UpdateLocation(Player.PlayerPawn.Value.AbsOrigin);
            }
        }
        */
        
        

    }

}









