using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Helpers;
using static g3.RoundRectGenerator;
using static WarcraftPlugin.Summons.FootBall;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.Summons
{
    public class FootBall
    {
        private CPhysicsPropMultiplayer _ball;
        private CDynamicProp _ballProp;
        private CCSPlayerController _owner;
        private Vector posInfrontOfPlayer;
        private Vector changedLocation;
        private FootballHitSystem hitSystem;
        private FootballAimSystem aimSystem;

        //public Vector Position { get; set; } = new(70, -70, 90);

        public FootBall(CCSPlayerController owner)
        {
            _owner = owner;
 
        }

        public void Activate(CCSPlayerController owner)
        {
            Deactivate();
            _ball = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            _ball.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            _ball.DispatchSpawn();

            _ballProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            _ballProp.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            _ballProp.DispatchSpawn();

            //_ballProp.SetParent(_ball, new Vector(0, 0, 0));
            _ballProp.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = 0.8f;
            var distance = 60;
            var height = 10;
            posInfrontOfPlayer = _owner.CalculatePositionInFront(distance, height);
            _ballProp.Teleport(posInfrontOfPlayer, _owner.PlayerPawn.Value.V_angle, new Vector(0,1,1));
            UpdateBall(owner);
        }
        private void Deactivate()
        {
            _ball?.RemoveIfValid();
        }
        public void UpdateLocation(CCSPlayerController owner)
        {
            var distance = 60;
            var height = 10;
            posInfrontOfPlayer = owner.CalculatePositionInFront(distance, height);
            owner.PrintToChat($"Updating Ball Position: {posInfrontOfPlayer}");
            _ballProp.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            
        }
        public void StayPut(CCSPlayerController owner)
        {
            StopUpdateBall();
            var distance = 60;
            var height = 10;
            posInfrontOfPlayer = owner.CalculatePositionInFront(distance, height);
            _ballProp.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            _ball.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            _ballProp.SetParent(_ball);
            TraceHits(owner);


        }

        public void TraceHits(CCSPlayerController owner)
        {
            hitSystem = new FootballHitSystem(owner, 0.01f, _ball);
            hitSystem.Start();
        }
        public void UpdateBall(CCSPlayerController owner)
        {
            aimSystem = new FootballAimSystem(owner, 0.01f, this);
            aimSystem.Start();
        }
        public void DestroyBall()
        {
            hitSystem.Destroy();
        }
        public void StopUpdateBall()
        {
            aimSystem.Destroy();
        }
        internal class FootballHitSystem(CCSPlayerController owner, float onTickInterval, CPhysicsPropMultiplayer ball) : WarcraftEffect(owner, onTickInterval: onTickInterval)
        {
           
            public override void OnStart()
            {
               
            }
            public override void OnTick()
            {
                //var ballBox = ball.CollisionBox();
                Vector vec = new Vector(ball.AbsOrigin.X, ball.AbsOrigin.Y, ball.AbsOrigin.Z);
                
                var box = Warcraft.CreateBoxAroundPoint(vec, 50, 50, 50);
                //owner.PrintToChat($"Ball Box x: {box.Center.x} | y: {box.Center.z}");
                var players = Utilities.GetPlayers();
                var playersInBox = players.Where(x => x.PawnIsAlive && box.Contains(x.PlayerPawn.Value.AbsOrigin));

                if (playersInBox.Any())
                {
                    foreach ( var player in playersInBox)
                    {
                        owner.PrintToChat($"Hit {player.PlayerName}");  
                    }
                }
            }
            public override void OnFinish() { }
        }

        internal class FootballAimSystem(CCSPlayerController owner, float onTickInterval, FootBall ball) : WarcraftEffect(owner, onTickInterval: onTickInterval)
        {
            public override void OnStart()
            {
                owner.PrintToChat("football Aim sytsem start");
                owner.PrintToChat("You have used a ball");
            }
            public override void OnTick()
            {
                owner.PrintToChat("tick");
                ball.UpdateLocation(Owner);
            }
            public override void OnFinish() { }
        }
    }

}
