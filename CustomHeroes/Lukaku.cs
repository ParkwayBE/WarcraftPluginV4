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

        FootBaller footBaller;
        int footballsRemaining = 10;
        bool canUseUlt = false;
        bool footballIsSpawned = false;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("DoSomething", "Quick"),
            new WarcraftAbility("Monkey Agility", "Increases movement speed and evasion."),
            new WarcraftAbility("Primal Roar", "Emits a roar when killing an enemy that stuns nearby enemies."),
            new WarcraftCooldownAbility("Ballr", "Balls", 0.3f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            if(footBaller != null)
            {
                footBaller = null;
            }
            footballsRemaining = 10;
            Player.PrintToChat("you have 3 footballs");
            footBaller = new FootBaller(Player, 20);
            canUseUlt = true;
        }

        private void Ultimate()
        {
            Console.WriteLine("pressed ult");
            if (!footballIsSpawned)
            {
                if (footballsRemaining > 0)
                {
                    footBaller.ActivateBall();
                    footballIsSpawned = true;
                    StartCooldown(3);
                }

            }
            else
            {
                footBaller.ServeBall();
                footballIsSpawned = false;

                StartCooldown(3);
            }

        }
        private void PlayerDeath(EventPlayerDeath dead)
        {
            canUseUlt = false;

        }

    }

}









