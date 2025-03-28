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
            BonusHealth(Player, 9999);

        }

        public static void BonusHealth(CCSPlayerController player, int amount)
        {
            var HealthEffect = new SetBonusHealth(player, amount);
            HealthEffect.Start();
        }


        private void Ultimate()
        {
            Console.WriteLine("[OrcishHorde] Ultimate activated");

            var caster = Player;
            var wcCaster = WarcraftPlayer;
            float radius = 500f;
            int damage = 30;

            var potentialTargets = new List<CCSPlayerController>();

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot || player == caster)
                    continue;

                if (player.TeamNum == caster.TeamNum || player.PlayerPawn?.Value == null)
                    continue;

                var diff = player.PlayerPawn.Value.AbsOrigin - caster.PlayerPawn.Value.AbsOrigin;
                float distanceSq = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;

                if (distanceSq <= radius * radius)
                {
                    potentialTargets.Add(player);
                }
            }

            Console.WriteLine($"[OrcishHorde] Found {potentialTargets.Count} potential targets");

            if (potentialTargets.Count == 0)
            {
                caster.PrintToCenter("⚡ No enemies nearby for Chain Lightning!");
                return;
            }

            var target = potentialTargets[Random.Shared.Next(potentialTargets.Count)];
            var wcTarget = target.GetWarcraftPlayer();

            if (wcTarget.HasUltimateImmunity)
            {
                caster.PrintToCenter("⛔ Target is immune to ultimates!");
                target.PrintToCenter("🛡️ Your Ultimate Immunity blocked Chain Lightning!");
                Console.WriteLine($"[OrcishHorde] Target {target.PlayerName} had immunity.");
                return;
            }

            SkillFunctions.DealRawDamage(caster, target, damage);

            // Optional beam
            Warcraft.DrawLaserBetween(caster.EyePosition(), target.EyePosition(), Color.LightBlue, 2f);

            Console.WriteLine($"[OrcishHorde] Dealt {damage} damage to {target.PlayerName}");

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