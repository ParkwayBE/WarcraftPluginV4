using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Numerics;

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

            var potentialTargets = new List<CCSPlayerController>();

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot || player == caster)
                    continue;

                // Skip same team members
                if (player.TeamNum == caster.TeamNum)
                    continue;

                var distance = (player.PlayerPawn.Value.AbsOrigin - caster.PlayerPawn.Value.AbsOrigin).Length();
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

            if (wcTarget != null && wcTarget.HasUltimateImmunity)
            {
                caster.PrintToCenter("⛔ Target is immune to ultimates!");
                target.PrintToCenter("🛡️ Your Ultimate Immunity blocked Chain Lightning!");
                return;
            }

            // Use your helper to deal raw damage
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