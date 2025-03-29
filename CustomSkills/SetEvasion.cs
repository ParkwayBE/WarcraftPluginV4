using System;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Helpers;

namespace WarcraftPlugin.CustomSkills
{
    public class SetEvasion : WarcraftEffect
    {
        private readonly int _chancePercent;
        private readonly float _reductionPercent;

        public SetEvasion(CCSPlayerController owner, int chancePercent, float reductionPercent, float duration = 999f)
            : base(owner, duration)
        {
            _chancePercent = chancePercent;
            _reductionPercent = reductionPercent;
        }

        public override void OnStart()
        {
            WarcraftPlugin.Instance.RegisterEventHandler<EventPlayerHurt>((@event, info) =>
            {
                if (@event.Userid != Owner || !Owner.IsValid || !Owner.IsAlive())
                    return HookResult.Continue;

                if (!Warcraft.RollDice(_chancePercent, 100))
                    return HookResult.Continue;

                // Reduce damage
                int originalDamage = @event.DmgHealth;
                int reducedDamage = (int)(originalDamage * (1f - _reductionPercent));

                @event.DmgHealth = reducedDamage;
                Console.WriteLine($"[Evasion] Rolled successful evasion. Original: {originalDamage}, Reduced: {reducedDamage}");

                Owner.PrintToChat($" \x04[Evasion] You evaded {originalDamage - reducedDamage} damage!");
                return HookResult.Continue;
            });

            Console.WriteLine($"[SetEvasion] {Owner.PlayerName} now has {_chancePercent}% evasion with {_reductionPercent * 100}% damage reduction.");
        }

        public override void OnTick()
        {
            // just needs to be here.
        }

        public override void OnFinish() { } // Evasion ends when effect ends or race switches
    }
}
