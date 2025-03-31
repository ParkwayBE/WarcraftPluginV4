using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Models;
using WarcraftPlugin.Summons;


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
            HookEvent<EventPlayerDisconnect>(PlayerDisconnect);
            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            if (footBaller != null)
            {
                footBaller = null;
            }
            footballsRemaining = 10;
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
            footBaller.DestroyBallSytsems();

        }

        private void PlayerDisconnect(EventPlayerDisconnect @event)
        {
            footBaller.DestroyBallSytsems();
        }


    }

}









