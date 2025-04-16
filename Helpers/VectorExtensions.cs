using System;
using System.Numerics;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.Helpers
{
    public static class VectorExtensions
    {
        public static Vector3 ToVector3(this Vector v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vector ToForward(this QAngle angle)
        {
            float pitch = angle.X * (float)(Math.PI / 180.0);
            float yaw = angle.Y * (float)(Math.PI / 180.0);

            float x = (float)(Math.Cos(pitch) * Math.Cos(yaw));
            float y = (float)(Math.Cos(pitch) * Math.Sin(yaw));
            float z = (float)-Math.Sin(pitch);

            return new Vector(x, y, z);
        }
    }
}
