using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Helpers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.Summons
{
    public class FootBall
    {
        private CPhysicsPropMultiplayer _ball;
        private CDynamicProp _ballProp;
        private CCSPlayerController _owner;

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
            Vector changedLocation = new Vector (_owner.PlayerPawn.Value.AbsOrigin.X+=0.1f, _owner.PlayerPawn.Value.AbsOrigin.Y, _owner.PlayerPawn.Value.AbsOrigin.Z+=0.1f);
            _ball.Teleport(changedLocation, _owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));

        }
        private void Deactivate()
        {
            _ball?.RemoveIfValid();
        }
    }

}
