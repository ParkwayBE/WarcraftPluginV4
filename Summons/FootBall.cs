using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using static g3.RoundRectGenerator;
using static WarcraftPlugin.Summons.FootBaller;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.Summons
{
    public class Football
    {
        public CPhysicsPropMultiplayer _ball;
        private CDynamicProp _ballProp;
        private CCSPlayerController _owner;
        public FootballHitSystem hitSystem;
        public bool isActive = false;

        public Football(CCSPlayerController owner) 
        {
            _owner = owner;
        }
        public void Activate(CCSPlayerController owner)
        { 
            isActive = true;
            _ball = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            _ball.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            _ball.DispatchSpawn();

            _ballProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            _ballProp.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            _ballProp.DispatchSpawn();


            _ballProp.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = 0.8f;
           // var distance = 60;
           //var height = 10;
            //Vector posInfrontOfPlayer = _owner.CalculatePositionInFront(distance, height);
            //_ballProp.Teleport(posInfrontOfPlayer, _owner.PlayerPawn.Value.V_angle, new Vector(0, 1, 1));
            //UpdateBall(owner);
        }
       
        public void UpdateLocation(CCSPlayerController owner)
        {
            var distance = 60;
            var height = 30;
            Vector posInfrontOfPlayer = owner.CalculatePositionInFront(distance, height);
            owner.PrintToChat($"Updating Ball Position: {posInfrontOfPlayer}");
            _ballProp.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));

        }
        public void UpdateLocation(Vector pos)
        {
            _ball.Teleport(pos);
        }
        public void StayPut(CCSPlayerController owner)
        {
            if (_ball != null)
            {
                //StopUpdateBall();
                var distance = 60;
                var height = 30;
                Vector posInfrontOfPlayer = owner.CalculatePositionInFront(distance, height);
                _ballProp.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
                _ball.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
                _ballProp.SetParent(_ball);
                TraceHits(owner);
                var power = 2300;
                Vector velocity = _owner.CalculateVelocityAwayFromPlayer(power);
                _ball.Teleport(null, null, velocity);

            }
        }

        public void TraceHits(CCSPlayerController owner)
        {
            hitSystem = new FootballHitSystem(owner, 0.01f, this);
            hitSystem.Start();
            WarcraftPlugin.Instance.AddTimer(10f, () =>
            {
                StopTraceHits();
            });
        }
        public void StopTraceHits()
        {
            hitSystem.Destroy();
        }


    }
    public class FootBaller
    {
        
        private CCSPlayerController _owner;
        private FootballHitSystem hitSystem;
        private FootballAimSystem aimSystem;
        public List<Football> footballs = new List<Football>();
        public List<Football> extraBalls = new List<Football>();
        private Football lastAddedBall;

        public FootBaller(CCSPlayerController owner, int balls)
        {
            _owner = owner;
        }
        public void ActivateBall()
        {
            lastAddedBall = new Football(_owner);
            lastAddedBall.isActive = true;
            lastAddedBall.Activate(_owner);

            footballs.Add(lastAddedBall);
   
           
            UpdateBallWithAimSystem(_owner, lastAddedBall);
            
        }
        
        public void UpdateBallWithAimSystem(CCSPlayerController owner, Football ball)
        {
            aimSystem = new FootballAimSystem(owner, 0.01f, ball);
            aimSystem.Start();
            
        }
        public void ServeBall()
        {
            aimSystem.Destroy();
            
            lastAddedBall.TraceHits(_owner);
            lastAddedBall.StayPut(_owner);
        }
        public void DestroyBall()
        {
            

        }
        public void StopUpdateBall()
        {
            
            
        }
        


    }
    public class FootballAimSystem(CCSPlayerController owner, float onTickInterval, Football ball) : WarcraftEffect(owner, onTickInterval: onTickInterval)
    {
        public override void OnStart()
        {
            owner.PrintToChat("football Aim sytsem start!");
            owner.PrintToChat("You have used a balll");
        }
        public override void OnTick()
        {
            owner.PrintToChat("tick");
            ball.UpdateLocation(Owner);
        }
        public override void OnFinish() { }
    }

    public class FootballHitSystem(CCSPlayerController owner, float onTickInterval, Football football) : WarcraftEffect(owner, onTickInterval: onTickInterval)
    {

        public override void OnStart()
        {

        }
        public override void OnTick()
        {

            if (football._ball != null)
            {
                Vector vec = new Vector(football._ball.AbsOrigin.X, football._ball.AbsOrigin.Y, football._ball.AbsOrigin.Z);

                var box = Warcraft.CreateBoxAroundPoint(vec, 100, 100, 100);
                var players = Utilities.GetPlayers();
                var playersInBox = players.Where(x => x.PawnIsAlive && box.Contains(x.PlayerPawn.Value.AbsOrigin));

                if (playersInBox.Any())
                {
                    foreach (var player in playersInBox)
                    {
                        owner.PrintToChat($"Hit {player.PlayerName}");
                        if (player.DesignerName != owner.DesignerName)
                        {
                            Warcraft.TakeDamage(player, 900000, owner, inflictor: owner);
                            Football fb = new Football(owner);
                            fb.UpdateLocation(owner);
                        }
                    }
                }
            }


        }
        public override void OnFinish() { }
    }
}
