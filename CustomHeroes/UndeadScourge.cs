using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class UndeadScourge : WarcraftClass
    {
        public override string DisplayName => "Undead Scourge";
        public override Color DefaultColor => Color.White;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Lifesteal", "6%-21% of your damage dealt is gained as health. This may overheal."),
            new WarcraftAbility("Levitation", "Gain reduced gravity 10%-50%"),
            new WarcraftAbility("Unholy Aura", "Increased movement speed on spawn."),
            new WarcraftAbility("Suicide bomber", "When killed, the player explodes dealing massive damage around him.")
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDeath>(OnPlayerDeath);
        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            int level = WarcraftPlayer.GetAbilityLevel(1); // Assuming slot 0 is the gravity ability
            if (level <= 0) return;

            float gravityPercent = 100f - (level * 10f); // 90% to 50%
            float duration = 999f; // untill death/round end/disconnect


            int level2 = WarcraftPlayer.GetAbilityLevel(2); // Or the correct slot for this skill
            if (level <= 0) return;
            float speedMultiplier = level2; // 1 = +10%, 5 = +50%


            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                BonusMovementSpeed(Player, speedMultiplier, duration);
                SkillFunctions.SetPlayerGravity(Player, gravityPercent, duration);
                Player.PrintToChat($"Your gravity is now {gravityPercent} and gained {speedMultiplier} bonus movement speed!");

            });
        }

        public static void BonusMovementSpeed(CCSPlayerController player, float amount, float duration)
        {
            var SpeedEffect = new SetMovementSpeed(player, amount, duration);
            SpeedEffect.Start();
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || victim == null || !attacker.IsValid || !victim.IsValid || attacker == victim)
                return;

            int chance = 100;
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0);
            float healPercent = 0.03f + (abilityLevel * 0.03f);
            int damage = @event.DmgHealth;

            SkillFunctions.LeechHealth(attacker, victim, chance, healPercent, damage);
        }


        private void OnPlayerDeath(EventPlayerDeath death)
        {
            if (death.Userid != Player || !Player.IsValid) return;

            if (Player.PlayerPawn?.Value == null) return;

            float radius = 250f;
            float damage = 100f;

            SkillFunctions.ExplodeOnDeathSkill(Player, radius, damage);
            Player.PrintToChat($"You dealt {damage} damage around you!");
        }
    }
}