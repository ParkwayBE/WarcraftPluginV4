using System;
using System.Collections.Generic;
using System.Drawing;
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
            new WarcraftCooldownAbility("Locust Swarm","Steal up to 50hp from a random enemy player", 1f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            // TODO: Carrion Beetles: Ultimate immunity
        }

        private void Ultimate()
        {
            // TODO: Locust Swarm : Steal up to 50 health from a random enemy
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            if (WarcraftPlayer.GetAbilityLevel(0) > 0)
            {
                SkillFunctions.ImpaleTarget(Player, @event.Userid, 600f);
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
                Vector pushForce = direction * 300.0f; // Adjust force strength here
                attacker.PlayerPawn.Value.Teleport(null, null, pushForce);
            }

            SkillFunctions.DealRawDamage(Player, attacker, reflectDamage);


            attacker.PrintToCenter($"☠️ You took {reflectDamage} reflected damage from Spiked Carapace!");
            Player.PrintToCenter($"🛡️ Spiked Carapace reflected {reflectDamage} damage to {attacker.PlayerName}.");
        }


    }
}