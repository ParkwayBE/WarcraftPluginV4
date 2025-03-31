using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using g3;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using WarcraftPlugin.Core;
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
            Activate(owner);
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


            //_ballProp.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = 0.8f;
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
            Vector posInfrontOfPlayer = _owner.CalculatePositionInFront(distance, height);
            _owner.PrintToChat($"Updating Ball Position: {posInfrontOfPlayer}");
            _ballProp.Teleport(posInfrontOfPlayer, _owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));

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
            hitSystem = new FootballHitSystem(_owner, 0.01f, this);
            hitSystem.Start();
            WarcraftPlugin.Instance.AddTimer(10f, () =>
            {
                StopTraceHits();
            });
        }
        public void StopTraceHits()
        {
            if (hitSystem != null)
            {
                hitSystem.FinishOnDestroy = true;
                hitSystem.Destroy();
            }
        }




    }
    public class FootBaller
    {
        
        private CCSPlayerController _owner;
        private FootballHitSystem hitSystem;
        private FootballAimSystem aimSystem;
        public List<Football> footballs;
        public List<Football> extraBalls = new List<Football>();
        private Football lastAddedBall;
        private FootballTurnAroundPlayerSystem tAroundPlayerSys;

        public FootBaller(CCSPlayerController owner, int balls)
        {
            footballs = new List<Football>(){
                new Football(owner),
                new Football(owner),
                new Football(owner),
            };
            _owner = owner;
        }


        public void TurnBallsAroundPlayer(CCSPlayerController owner)
        {
            if(tAroundPlayerSys != null)
            {
                tAroundPlayerSys.Destroy();
            }
            if (footballs != null)
            {
                tAroundPlayerSys = new FootballTurnAroundPlayerSystem(owner, 0.01f, footballs);
                tAroundPlayerSys.Start();
            }

        }
        public void ActivateBall()
        {
            lastAddedBall = new Football(_owner);
            lastAddedBall.isActive = true;

            footballs.Add(lastAddedBall);
   
           
            UpdateBallWithAimSystem(_owner, lastAddedBall);
            
        }
        
        public void UpdateBallWithAimSystem(CCSPlayerController owner, Football ball)
        {
            aimSystem = new FootballAimSystem(owner, 0.01f, ball);
            aimSystem.FinishOnDestroy = true;
            aimSystem.Start();
            
        }
        public void ServeBall()
        {
            if (aimSystem != null)
            {
                aimSystem.Destroy();
            }
            lastAddedBall.TraceHits(_owner);
            lastAddedBall.StayPut(_owner);
        }
        public void DestroyBallSytsems()
        {
            if (aimSystem != null)
            {
                aimSystem.Destroy();
            }
            for (int i = 0; i < footballs.Count; i++)
            {
                footballs[i].StopTraceHits();
                if (footballs[i]._ball != null)
                {
                    footballs[i]._ball.RemoveIfValid();
                }
            }

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

    public class FootballTurnAroundPlayerSystem(CCSPlayerController owner, float onTickInterval, List<Football> balls) : WarcraftEffect(owner, onTickInterval: onTickInterval)
    {
        public override void OnStart()
        {
            owner.PrintToChat("footballs turns around player system started");

            float angleIncrement = MathF.PI * 2 / balls.Count;
            for (int i = 0; i < balls.Count; i++)
            {
                // Calculate the angle for this object
                float angle = i * angleIncrement;

                // Calculate the position of the object in a circle
                Vector objectPosition = new Vector
                {
                    X = Owner.AbsOrigin.X + 50f * MathF.Cos(angle),
                    Y = Owner.AbsOrigin.Y + 50f * MathF.Sin(angle),
                    Z = Owner.AbsOrigin.Z // Maintain the same Z level for flat placement
                };

                // Log the object's new position or move it in the game

                balls[i].UpdateLocation(owner.AbsOrigin);
            }
        }
        public override void OnTick()
        {
            float angleIncrement = MathF.PI * 2 / balls.Count;
            for (int i = 0; i < balls.Count; i++)
            {
                // Calculate the angle for this object
                float angle = i * angleIncrement;

                // Calculate the position of the object in a circle
                Vector3 objectPosition = new Vector3
                {
                    X = Owner.AbsOrigin.X + 50f * MathF.Cos(angle),
                    Y = Owner.AbsOrigin.Y + 50f * MathF.Sin(angle),
                    Z = Owner.AbsOrigin.Z // Maintain the same Z level for flat placement
                };

                // Log the object's new position or move it in the game

                //balls[i]._ball.Teleport(new Vector(objectPosition.X, objectPosition.Y, objectPosition.Z),owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            }
        }
        public override void OnFinish() { }
    }

    public class FootballHitSystem(CCSPlayerController owner, float onTickInterval, Football football) : WarcraftEffect(owner, onTickInterval: onTickInterval)
    {
        public List<CCSPlayerController> players;
        Football fb;
        public override void OnStart()
        {
            players = Utilities.GetPlayers();
        }
        public override void OnTick()
        {

            if (football._ball != null)
            {
                Vector vec = new Vector(football._ball.AbsOrigin.X, football._ball.AbsOrigin.Y, football._ball.AbsOrigin.Z);

                var box = Warcraft.CreateBoxAroundPoint(vec, 100, 100, 100);
                var playersInBox = players.Where(x => x.PawnIsAlive && box.Contains(x.PlayerPawn.Value.AbsOrigin));

                if (playersInBox.Any())
                {
                    foreach (var player in playersInBox)
                    {
                        owner.PrintToChat($"Hit {player.PlayerName}");
                        if (player.PlayerName != owner.PlayerName)
                        {
                            Warcraft.TakeDamage(player, 900000, owner, killFeedIcon: KillFeedIcon.snowball, inflictor: owner);
                            //fb = new Football(owner);
                            //fb.UpdateLocation(owner);
                        }
                    }
                }
            }


        }
        public override void OnFinish() { }
    }
}
