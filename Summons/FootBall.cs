using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        internal Vector Position { get; set; } = new(70, -70, 90);

        internal FootBall(CCSPlayerController owner, Vector position)
        {
            _owner = owner;
            Position = position;
 
        }

        public void Activate()
        {
            Deactivate();
            _ball = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            _ball.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");

            _ballProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            _ballProp.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            _ballProp.DispatchSpawn();

            _ballProp.SetParent(_ball, new Vector(0, 0, 0));
            _ball.Teleport(_owner.CalculatePositionInFront(Position), _owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));

        }
        private void Deactivate()
        {
            _ball?.RemoveIfValid();
        }
    }

}
