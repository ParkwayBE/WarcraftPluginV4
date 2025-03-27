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
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
        }



        private void Ultimate()
        {
            Console.WriteLine("[Ultimate] Chain Lightning triggered.");

            var caster = Player;
            var wcCaster = caster.GetWarcraftPlayer();

            if (caster?.PlayerPawn?.Value == null)
            {
                Console.WriteLine("[Ultimate] PlayerPawn was null.");
                return;
            }

            float radius = 500f;
            int damage = 30;
            bool playerFound = false;

            var origin = caster.PlayerPawn.Value.AbsOrigin;
            var potentialTargets = new List<CCSPlayerController>();

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || !player.IsAlive() || player == caster)
                    continue;

                if (player.TeamNum == caster.TeamNum)
                    continue;

                if (player.PlayerPawn?.Value == null)
                    continue;

                var otherPos = player.PlayerPawn.Value.AbsOrigin;
                var diff = origin - otherPos;
                float distanceSquared = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;

                Console.WriteLine($"[Ultimate] Checking {player.PlayerName}, dist² = {distanceSquared}");

                if (distanceSquared <= radius * radius)
                {
                    playerFound = true;
                    potentialTargets.Add(player);
                    Console.WriteLine($"[Ultimate] Valid target: {player.PlayerName}");

                    // Visual laser to show connection
                    Warcraft.DrawLaserBetween(origin, otherPos, Color.Orange, 1.5f);
                }
            }

            if (!playerFound || potentialTargets.Count == 0)
            {
                Console.WriteLine("[Ultimate] No valid players found nearby.");
                caster.PrintToCenter("⚡ No enemies nearby for Chain Lightning!");
                return;
            }

            var target = potentialTargets[new Random().Next(potentialTargets.Count)];
            var wcTarget = target.GetWarcraftPlayer();

            Console.WriteLine($"[Ultimate] Selected target: {target.PlayerName}");

            Warcraft.DrawLaserBetween(origin, target.PlayerPawn.Value.AbsOrigin, Color.Cyan, 3.0f);

            if (wcTarget != null && wcTarget.HasUltimateImmunity)
            {
                caster.PrintToCenter("⛔ Target is immune to ultimates!");
                target.PrintToCenter("🛡️ Your Ultimate Immunity blocked Chain Lightning!");
                Console.WriteLine($"[Ultimate] {target.PlayerName} blocked the ult with immunity.");
                return;
            }

            Console.WriteLine($"[Ultimate] Dealing {damage} damage to {target.PlayerName}");
            SkillFunctions.DealRawDamage(caster, target, damage);
            StartCooldown(3);
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