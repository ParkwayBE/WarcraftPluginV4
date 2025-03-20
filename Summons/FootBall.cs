using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Helpers;
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

        public Vector Position { get; set; } = new(70, -70, 90);

        public FootBall(CCSPlayerController owner, Vector position)
        {
            _owner = owner;
            Position = position;
 
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
            _ballProp.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = 1;
            posInfrontOfPlayer = _owner.CalculatePositionInFront(Position);
            changedLocation = new Vector (posInfrontOfPlayer.X+=100f, posInfrontOfPlayer.Y, posInfrontOfPlayer.Z += 80f);
            _ball.Teleport(changedLocation, _owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            
        }
        private void Deactivate()
        {
            _ball?.RemoveIfValid();
        }
        public void UpdateLocation(Vector position)
        {
            var distance = 60;
            var height = 60;
            posInfrontOfPlayer = _owner.CalculatePositionInFront(distance, height);
            _ball.Teleport(changedLocation, _owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
           
        }
    }

}
