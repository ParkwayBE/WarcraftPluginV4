using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class OrcishHorde : WarcraftClass
    {
        public override string DisplayName => "Orcish Horde";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Critical Strike", "up to 35% to deal double damage."),
            new WarcraftAbility("Reincarnation", "Gain up to 100% chance to respawn once after dying"),
            new WarcraftAbility("Critical Grenade", "up to 100% chance to deal double damage"),
            new WarcraftCooldownAbility("Chain Lightning", "Strike a nearby enemy with lightning.", 8f, true)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
        }



        private void Ultimate()
        {
            var caster = Player;
            var wcCaster = caster.GetWarcraftPlayer();
            float radius = 500f;
            int damage = 30;

            if (caster?.PlayerPawn?.Value == null)
                return;

            var origin = caster.PlayerPawn.Value.AbsOrigin;
            var potentialTargets = new List<CCSPlayerController>();

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot || player == caster)
                    continue;

                if (player.TeamNum == caster.TeamNum)
                    continue;

                if (player.PlayerPawn?.Value == null)
                    continue;

                float distance = (player.PlayerPawn.Value.AbsOrigin - origin).Length();

                // Debug beam to all potential enemies
                Warcraft.DrawLaserBetween(origin, player.PlayerPawn.Value.AbsOrigin, Color.Yellow, 1.0f);

                if (distance <= radius)
                {
                    potentialTargets.Add(player);
                }
            }

            if (potentialTargets.Count == 0)
            {
                caster.PrintToCenter("⚡ No enemies nearby for Chain Lightning!");
                return;
            }

            var random = new Random();
            var target = potentialTargets[random.Next(potentialTargets.Count)];
            var wcTarget = target.GetWarcraftPlayer();

            // Beam to chosen target
            Warcraft.DrawLaserBetween(origin, target.PlayerPawn.Value.AbsOrigin, Color.Cyan, 2.0f);

            if (wcTarget != null && wcTarget.HasUltimateImmunity)
            {
                //caster.SendInfo("⛔ Target is immune to ultimates!");
                //target.SendInfo("🛡️ Your Ultimate Immunity blocked Chain Lightning!");
                return;
            }

            SkillFunctions.DealRawDamage(caster, target, damage);
            StartCooldown(3); // Index 3 = Ultimate
        }





        private void PlayerDeath(EventPlayerDeath death)
        {
            // reincarnation skill
        }
        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // Extra damage
            // Extra Damage with nades
        }

    }
}