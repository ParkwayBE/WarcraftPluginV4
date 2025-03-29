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

                // ✅ Roll chance
                int roll = Random.Shared.Next(1, 101);
                if (roll > _chancePercent)
                    return HookResult.Continue;

                // ✅ Calculate reduction
                int originalDamage = @event.DmgHealth;
                int reducedDamage = (int)(originalDamage * (1f - _reductionPercent));

                // ✅ Don't show message unless damage was actually reduced
                if (reducedDamage < originalDamage)
                {
                    @event.DmgHealth = reducedDamage;
                    Owner.PrintToChat($" \x04[Evasion] Evaded {originalDamage - reducedDamage} damage! (Roll: {roll}/{_chancePercent})");
                    Console.WriteLine($"[Evasion] Roll: {roll}, Reduced: {originalDamage} → {reducedDamage}");
                }

                return HookResult.Continue;
            });

            Console.WriteLine($"[SetEvasion] {Owner.PlayerName} gained {_chancePercent}% evasion ({_reductionPercent * 100}% reduction).");
        }


        public override void OnTick()
        {
            // just needs to be here.
        }

        public override void OnFinish() { } // Evasion ends when effect ends or race switches
    }
}
