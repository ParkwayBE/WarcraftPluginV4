using System.Numerics;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;



namespace WarcraftPlugin.CustomSkills
{
    public class ExplodeOnDeathEffect : WarcraftEffect
    {
        private readonly float Radius;
        private readonly float Damage;

        public ExplodeOnDeathEffect(CCSPlayerController owner, float radius, float damage)
            : base(owner)
        {
            Radius = radius;
            Damage = damage;
        }

        public override void OnStart()
        {
            if (Owner?.PlayerPawn?.Value == null) return;

            var explosionOrigin = Owner.PlayerPawn.Value.AbsOrigin;

            Warcraft.SpawnExplosion(
                pos: explosionOrigin,
                damage: Damage,
                radius: Radius,
                attacker: Owner,
                killFeedIcon: KillFeedIcon.prop_exploding_barrel
            );
        }

        public override void OnTick() { }

        public override void OnFinish() { }
    }

    public static class VectorExtensions
    {
        public static Vector3 ToVector3(this CounterStrikeSharp.API.Modules.Utils.Vector v)
            => new Vector3(v.X, v.Y, v.Z);
    }
}