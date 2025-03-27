using CounterStrikeSharp.API.Core;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;

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
            new WarcraftCooldownAbility("Teleport", " Teleport where you aim at! ", 8f)
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
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                int DevotionAura = abilityLevel * 18;
                int InvisPercent = abilityLevel * 15;
                BonusHealth(Player, DevotionAura);
                Invisibility(Player, 999f, InvisPercent);
                Player.SendInfo($"You gained {DevotionAura} health and became {InvisPercent / 255}% invisible.");

            });
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
            attacker.SendInfo($"You froze your victim for  {duration} seconds.");
            victim.SendInfo($"You got frozen for {duration} seconds.");


        }

        private void OnPlayerPing(EventPlayerPing ping)
        {
            SkillFunctions.HandleTeleportPing(Player, ping.X, ping.Y, ping.Z);
        }

    }
}