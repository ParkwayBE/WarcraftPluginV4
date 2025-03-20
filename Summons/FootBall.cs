using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Helpers;
using static g3.RoundRectGenerator;
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

        //public Vector Position { get; set; } = new(70, -70, 90);

        public FootBall(CCSPlayerController owner)
        {
            _owner = owner;
 
        }

        public void Activate()
        {
            Deactivate();
            _ball = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            _ball.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            _ball.DispatchSpawn();

            _ballProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            _ballProp.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            _ballProp.DispatchSpawn();

            _ballProp.SetParent(_ball, new Vector(0, 0, 0));
            _ballProp.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = 0.8f;
            var distance = 60;
            var height = 10;
            posInfrontOfPlayer = _owner.CalculatePositionInFront(distance, height);
            _ballProp.Teleport(posInfrontOfPlayer, _owner.PlayerPawn.Value.V_angle, new Vector(0,1,1));
            
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
            var distance = 60;
            var height = 10;
            posInfrontOfPlayer = owner.CalculatePositionInFront(distance, height);
            _ball.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            //_ballProp.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            //_ballProp.SetParent(_ball);
            
        }
    }

}
