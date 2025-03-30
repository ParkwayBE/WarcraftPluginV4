using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;



namespace WarcraftPlugin.Classes
{
    public class CryptLord : WarcraftClass
    {
        public override string DisplayName => "Crypt Lord";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Impale", "Chance to impale your targets."),
            new WarcraftAbility("Spiked Carapace", "Knife attacks reflect up to 200% damage back."),
            new WarcraftAbility("Carrion Beetles", "Beetles will save you from most ultimates up to 100% succesrate."),
            new WarcraftCooldownAbility("Locust Swarm","Steal up to 50hp from a random enemy player", 25f, true)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookEvent<EventPlayerDisconnect>(PlayerDisconnect);

            HookAbility(3, Ultimate);
        }

        private void PlayerDisconnect(EventPlayerDisconnect @event)
        {
            WarcraftPlayer.HasUltimateImmunity = false;
        }

        private void PlayerDeath(EventPlayerDeath @event)
        {
            WarcraftPlayer.HasUltimateImmunity = false;
        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            ResetCooldowns();
            if (WarcraftPlayer.GetAbilityLevel(2) > 0)
            {
                if (Player == null) return;

                int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
                if (abilityLevel == 0) return;

                int UltimateImmunityChance = abilityLevel * 20;

                var roll = Random.Shared.Next(100);
                if (roll < UltimateImmunityChance)
                {
                    WarcraftPlayer.HasUltimateImmunity = true;
                    Player.PrintToChat("You gained ultimate immunity for this round.");
                }


            }
        }

        private void Ultimate()
        {
            var enemies = Utilities.GetPlayers().Where(p =>
        p != Player &&
        p.TeamNum != Player.TeamNum &&
        p.IsValid && p.IsAlive()).ToList();

            if (enemies.Count == 0)
            {
                Player.PrintToCenter("⚠️ No valid enemies to target!");
                return;
            }

            var randomEnemy = enemies[Random.Shared.Next(enemies.Count)];
            var wcTarget = randomEnemy.GetWarcraftPlayer();

            if (wcTarget != null && wcTarget.HasUltimateImmunity)
            {
                Player.PrintToCenter("⛔ Target is immune to ultimates!");
                randomEnemy.PrintToCenter("🛡️ Your Ultimate Immunity blocked the effect!");
                return;
            }

            // Deal raw damage
            SkillFunctions.DealRawDamage(Player, randomEnemy, 40);

            var VictimLoc = randomEnemy.PlayerPawn.Value.AbsOrigin;
            var AttackerLoc = Player.PlayerPawn.Value.AbsOrigin;
            Warcraft.SpawnParticle(VictimLoc, "particles/ui/hud/ui_mvp_winner_alt_a.vpcf", 4f);
            Warcraft.SpawnParticle(AttackerLoc, "particles/ui/ammohealthcenter/ui_hud_kill_streaks_spectator_4.vpcf", 4f);

            // Heal caster
            var currentHealth = Player.PlayerPawn.Value.Health;
            Player.SetHp(currentHealth + 40);

            // Feedback
            Player.PrintToCenter($"💉 You drained 40 health from {randomEnemy.PlayerName}!");
            randomEnemy.PrintToCenter($" You were hit by {Player.PlayerName}'s ultimate!");


            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            if (Player == null) return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0);
            if (abilityLevel <= 0) return;
            int chancePercent = abilityLevel * 3;
            int roll = new Random().Next(1, 101);
            Console.WriteLine($"[Impale] Rolled {roll} vs {chancePercent}");

            if (roll <= chancePercent)
            {
                var target = @event.Userid;
                if (target == null || !target.IsValid || !target.IsAlive()) return;

                SkillFunctions.ImpaleTarget(Player, target, 600f);
                Player.PrintToChat("You impaled an enemy.");
            }
        }



        private void PlayerHurt(EventPlayerHurt @event)
        {
            if (Player == null || !Player.IsValid) return;

            var attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || attacker == Player) return;

            var victim = @event.Userid;
            if (victim == null || !victim.IsValid || victim != Player) return;

            var weaponName = attacker.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value?.DesignerName;
            if (weaponName != "weapon_knife") return;

            int level = WarcraftPlayer.GetAbilityLevel(1);
            if (level <= 0) return;

            int damageDealt = @event.DmgHealth;

            float reflectMultiplier = 0.4f * level;
            int reflectDamage = (int)(damageDealt * reflectMultiplier);
            var Victimpawn = victim.PlayerPawn.Value;

            int newHealth = Victimpawn.Health + damageDealt;
            victim.SetHp(newHealth);

            Vector direction = attacker.PlayerPawn.Value.AbsOrigin - Player.PlayerPawn.Value.AbsOrigin;
            float length = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y + direction.Z * direction.Z);

            if (length != 0)
            {
                direction = new Vector(direction.X / length, direction.Y / length, direction.Z / length);
                Vector pushForce = direction * 500.0f; // Adjust force strength here
                attacker.PlayerPawn.Value.Teleport(null, null, pushForce);
            }

            SkillFunctions.DealRawDamage(Player, attacker, reflectDamage);


            attacker.PrintToCenter($"☠️ You took {reflectDamage} reflected damage from Spiked Carapace!");
            Player.PrintToCenter($"🛡️ Spiked Carapace reflected {reflectDamage} damage to {attacker.PlayerName}.");
        }


    }
}