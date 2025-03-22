using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;

namespace WarcraftPlugin.CustomSkills
{
    public static class SkillFunctions
    {
        public static void MovementSpeed(CCSPlayerController player, float amount, float duration)
        {
            new SetMovementSpeed(player, duration, amount).Start();
        }
        public static void SetBonusHealth(CCSPlayerController player, int amount)
        {
            new SetBonusHealth(player, amount).Start();
        }
        public static void SetInvisibility(CCSPlayerController player, float duration)
        {
            new SetInvisibility(player, duration).Start();
        }
    }
}
