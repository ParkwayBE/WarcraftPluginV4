using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Helpers;

namespace WarcraftPlugin.CustomSkills
{
    public class SetInvisibility : WarcraftEffect
    {
        int amount;
        public SetInvisibility(CCSPlayerController owner, float duration, int amount)
            : base(owner, duration: duration)
        {
            this.amount = amount;
        }

        public override void OnStart()
        {
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null || !Owner.IsAlive())
                return;

            Owner.PlayerPawn.Value.SetColor(Color.FromArgb(amount, 255, 255, 255)); // mostly invisible
        }

        public override void OnFinish()
        {
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                return;

            Owner.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255)); // restore visibility
        }

        public override void OnTick() { } // Not needed
    }
}
