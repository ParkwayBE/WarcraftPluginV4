using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class HumanAlliance : WarcraftClass
    {
        public override string DisplayName => "Human Alliance";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Invisibility", "Gain up to 75% invisibility"),
            new WarcraftAbility("Devotion Aura", "Gain up to 90 bonus starting health"),
            new WarcraftAbility("Bash", "5-25% to freeze your target for 1-3 seconds"),
            new WarcraftCooldownAbility("Teleport", " Teleport where you aim at! ", 8f, false)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerPing>(OnPlayerPing);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookAbility(3, Ultimate);
        }

        public static void BonusHealth(CCSPlayerController player, int amount)
        {
            var HealthEffect = new SetBonusHealth(player, amount);
            HealthEffect.Start();
        }

        public static void Invisibility(CCSPlayerController player, float duration, int amount)
        {
            var InvisEffect = new SetInvisibility(player, duration, amount);
            InvisEffect.Start();
        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);

                int DevotionAura = abilityLevel * 18;

                float invisPercent = abilityLevel * 15f;
                int alpha = 100; //(int)(255f * (1f - (invisPercent / 100f)));

                BonusHealth(Player, DevotionAura);

                Invisibility(Player, 999f, alpha);
                Console.WriteLine($"[Invisibility] Level {abilityLevel} → alpha: {alpha}");



                WarcraftPlayer.HasUltimateImmunity = true;
            });

            if (Player?.PlayerPawn?.Value == null) return;
            ResetCooldowns();
        }



        private void Ultimate()
        {
            SkillFunctions.TeleportUltimate(Player);
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            if (@event.Attacker == null || @event.Userid == null) return;

            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (!attacker.IsValid || !victim.IsValid || attacker.UserId == victim.UserId)
                return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            int ChanceInPercent = 5 * abilityLevel;
            float duration = (6f * abilityLevel) / 10f;
            SkillFunctions.FreezePlayer(attacker, victim, ChanceInPercent, duration);
        }

        private void OnPlayerPing(EventPlayerPing ping)
        {
            SkillFunctions.HandleTeleportPing(Player, ping.X, ping.Y, ping.Z);
        }

    }
}
