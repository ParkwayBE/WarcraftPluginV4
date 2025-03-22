using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Core.Effects;


namespace WarcraftPlugin.CustomSkills
{
    public class SetBonusHealth : WarcraftEffect
    {
        private readonly int _bonusHealth;

        public SetBonusHealth(CCSPlayerController owner, int bonusHealth)
            : base(owner, duration: 0f) // zero duration = permanent until race swap or respawn
        {
            _bonusHealth = bonusHealth;
        }

        public override void OnStart()
        {
            var pawn = Owner.PlayerPawn.Value;

            // Set health if alive
            if (pawn != null && Owner.IsAlive())
            {
                int newHealth = pawn.Health + _bonusHealth;
                Owner.SetHp(newHealth);
                Owner.PrintToChat($"[TEST] You gained {_bonusHealth} bonus health.");
            }
        }

        public override void OnFinish() { } // Not used unless you want to remove HP somehow
        public override void OnTick() { }   // Not needed for passive effect
    }
}
