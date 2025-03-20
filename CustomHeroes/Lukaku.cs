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


namespace WarcraftPlugin.Classes
{
    public class Lukaku : WarcraftClass
    {
        public override string DisplayName => "Lukaku";

        private readonly WarcraftPlugin _plugin;
        public override Color DefaultColor => Color.CadetBlue;

        List<FootBall> footBalls = new List<FootBall>();
        int footballsRemaining = 0;

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
            footballsRemaining = 3;
            //WarcraftPlugin.Instance.AddTimer(0.2f, () => {Player.PlayLocalSound("sounds/ambient/misc/techno_overpass.vsnd"); });
            Player.PrintToChat("you have 3 footballs");
        }

        private void Ultimate()
        {
            if (footballsRemaining > 0)
            {
                footballsRemaining--;
                FootballSystem fbs = new FootballSystem(Player, 0.3f, footBalls);
                fbs.FinishOnDestroy = true;
                fbs.Start();
                WarcraftPlugin.Instance.AddTimer(20f, () =>
                {
                    fbs.Destroy();
                });
                Player.PrintToChat("end ult");
            }


            StartCooldown(3);
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
        internal class FootballSystem(CCSPlayerController owner, float onTickInterval, List<FootBall> footBalls) : WarcraftEffect(owner, onTickInterval: onTickInterval)
        {
            public override void OnStart()
            {
                owner.PrintToChat("football sytsem start");
                footBalls.Add(new FootBall(owner, owner.PlayerPawn.Value.AbsOrigin));
                footBalls[0].Activate();
                owner.PrintToChat("You have used a ball");
            }
            public override void OnTick()
            {
                owner.PrintToChat("tick");
                footBalls[0].UpdateLocation(owner.PlayerPawn.Value.AbsOrigin);
         
            }
            public override void OnFinish() { }
        }

    }

}









