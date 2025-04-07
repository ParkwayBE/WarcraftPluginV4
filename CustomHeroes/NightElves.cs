using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class NightElves : WarcraftClass
    {
        public override string DisplayName => "Night Elves";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Evasion", "Gain up to 30% evasion"),
            new WarcraftAbility("Thorns Aura", "Gain up to 50% chance to reflect 25% of the damage taken."),
            new WarcraftAbility("Trueshot Aura", "Deal up to 20% additional damage"),
            new WarcraftCooldownAbility("Root", "Root up to 4 nearby enemies for 3 seconds!", 25f, true)
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
            // No passive spawn logic for now
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            if (Player == null || !Player.IsAlive())
                return;

            int evasionLevel = WarcraftPlayer.GetAbilityLevel(0);
            int reflectLevel = WarcraftPlayer.GetAbilityLevel(1);
            var attacker = @event.Attacker;

            // Evasion
            if (evasionLevel > 0)
            {
                int chance = evasionLevel * 6; // up to 30%
                int roll = Random.Shared.Next(100);

                if (roll < chance)
                {
                    @event.IgnoreDamage();
                    Player.PrintToChat($" {ChatColors.Green}Evasion{ChatColors.Default}: You evaded a hit!");
                    return;
                }
            }

            // Thorns Aura
            if (attacker != null && attacker.IsValid && attacker.IsAlive() && reflectLevel > 0)
            {
                int reflectChance = reflectLevel * 10;
                int roll = Random.Shared.Next(100);

                if (roll < reflectChance)
                {
                    int reflectAmount = (int)(@event.DmgHealth * 0.25f);
                    attacker.SetHp(attacker.PlayerPawn.Value.Health - reflectAmount);
                    attacker.PrintToChat($"🗡 You were hurt by {ChatColors.LightPurple}Thorns Aura{ChatColors.Default}!");
                    Player.PrintToChat($"🌿 Your {ChatColors.LightPurple}Thorns Aura{ChatColors.Default} reflected damage!");
                }
            }
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive())
                return;

            if (attacker.TeamNum == victim.TeamNum)
                return;

            int level = WarcraftPlayer.GetAbilityLevel(2); // Trueshot Aura
            if (level <= 0)
                return;

            int bonusDamage = (int)(@event.DmgHealth * (level * 0.04f)); // Up to 20%
            @event.AddBonusDamage(bonusDamage);

            attacker.PrintToChat($" {ChatColors.Green}🎯 Trueshot Aura{ChatColors.Default}: +{bonusDamage} bonus damage!");
        }

        private void Ultimate()
        {
            if (Player == null || !Player.IsAlive())
                return;

            int affected = 0;
            float radius = 1000f;
            int maxTargets = 4;
            int rootDuration = 3;


            foreach (var target in Utilities.GetPlayers())
            {
                if (!target.IsAlive() || target == Player || target.TeamNum == Player.TeamNum)
                    continue;

                var wcTarget = target.GetWarcraftPlayer();
                if (wcTarget != null && wcTarget.HasUltimateImmunity)
                {
                    Player.PrintToCenter($" {ChatColors.Red}⛔{ChatColors.Default} Target has {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default}!");
                    target.PrintToCenter($" {ChatColors.Green}🛡️{ChatColors.Default} Your {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default} blocked {ChatColors.LightPurple}Root{ChatColors.Default}!");
                    continue;
                }


                var pos = target.PlayerPawn.Value.AbsOrigin;
                var self = Player.PlayerPawn.Value.AbsOrigin;

                float dist = (pos - self).Length();
                if (dist > radius)
                    continue;

                new FreezePlayerEffect(Player, rootDuration, target).Start();
                affected++;
                if (affected >= maxTargets)
                    break;
            }

            if (affected > 0)
            {
                Player.PrintToChat($" {ChatColors.Lime}🌱 Root{ChatColors.Default}: Immobilized {affected} enemy{(affected > 1 ? " players" : "")}.");
                StartCooldown(3);
            }
            else
            {
                Player.PrintToChat($" {ChatColors.LightRed}Root failed: No enemies nearby.");
                CooldownManager.StartCooldown(WarcraftPlayer, 3, 5f);
            }
        }
    }
}
