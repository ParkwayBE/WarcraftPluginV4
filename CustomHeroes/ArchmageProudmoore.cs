using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class ArchmageProudmoore : WarcraftClass
    {
        public override string DisplayName => "Archmage Proudmoore";
        public override Color DefaultColor => Color.GreenYellow;
        private bool UltimateToggle = false;
        private readonly Dictionary<CCSPlayerController, Timer> _immunityTimers = new();


        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Blizzard", "Chance to slow your target and obscure his vision ."),
            new WarcraftAbility("Water Elemental", "When you kill a player you have a chance to revive a teammate as a Water Elemental."),
            new WarcraftAbility("Brilliance Aura", "You and up to two random allies have a chance to block some ultimates."),
            new WarcraftCooldownAbility("Flight","Conjure a spell that allows you to fly.", 2f, false) 
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventRoundEnd>(OnRoundEnd);
            HookEvent<EventPlayerDisconnect>(OnPlayerDisconnect);

            HookAbility(3, Ultimate);
        }


        private void OnPlayerDisconnect(EventPlayerDisconnect evt)
        {
            foreach (var timer in _immunityTimers.Values)
            {
                timer?.Kill();
            }
            _immunityTimers.Clear();
        }


        private void OnRoundEnd(EventRoundEnd evt)
        {
            foreach (var timer in _immunityTimers.Values)
            {
                timer?.Kill();
            }
            _immunityTimers.Clear();
        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            UltimateToggle = false;

            if (_immunityTimers.ContainsKey(Player))
            {
                _immunityTimers[Player].Kill();
                _immunityTimers.Remove(Player);
            }
            WarcraftPlayer.HasUltimateImmunity = false;

            int level = WarcraftPlayer.GetAbilityLevel(2);
            if (level <= 0) return;

            int chancePercent = 30 + (level * 10);
            int maxAllies = 2;

            // Roll for self immunity
            if (Warcraft.RollDice(chancePercent, 100))
            {
                WarcraftPlayer.HasUltimateImmunity = true;
                Player.PrintToChat($" {ChatColors.Green}✨ Brilliance Aura{ChatColors.Default}: You gained {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default}!");

                // Apply new immunity timer
                var selfTimer = WarcraftPlugin.Instance.AddTimer(160f, () =>
                {
                    if (Player.IsValid)
                    {
                        WarcraftPlayer.HasUltimateImmunity = false;
                        Player.PrintToChat($" {ChatColors.Green}⚠️{ChatColors.Default} Your {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default} has worn off.");
                    }
                });
                _immunityTimers[Player] = selfTimer;
            }

            // Grant to up to 2 teammates
            var teammates = Utilities.GetPlayers().Where(p =>
                p != Player && p.IsValid && p.IsAlive() && p.TeamNum == Player.TeamNum)
                .OrderBy(_ => Guid.NewGuid()).Take(maxAllies);

            foreach (var ally in teammates)
            {
                if (!Warcraft.RollDice(chancePercent, 100)) continue;

                var wcAlly = ally.GetWarcraftPlayer();
                if (wcAlly == null) continue;

                // Kill old timer if exists
                if (_immunityTimers.ContainsKey(ally))
                {
                    _immunityTimers[ally].Kill();
                    _immunityTimers.Remove(ally);
                }

                wcAlly.HasUltimateImmunity = true;
                ally.PrintToChat($" {ChatColors.Blue}🛡️ Brilliance Aura{ChatColors.Default}: You received {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default}!");
                Player.PrintToChat($" {ChatColors.Green}✨ Brilliance Aura{ChatColors.Default}: {ally.PlayerName} gained {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default}!");

                // Apply new immunity timer
                var allyTimer = WarcraftPlugin.Instance.AddTimer(160f, () =>
                {
                    if (ally.IsValid)
                    {
                        wcAlly.HasUltimateImmunity = false;
                        ally.PrintToChat($" {ChatColors.Blue}⚠️ {ChatColors.Default}Your {ChatColors.LightPurple}Ultimate Immunity{ChatColors.Default} has worn off.");
                    }
                });
                _immunityTimers[ally] = allyTimer;
            }
        }

        private static void SetMoveType(CCSPlayerPawn pawn, MoveType_t moveType)
        {
            if (pawn == null) return;
            pawn.MoveType = moveType;
            pawn.ActualMoveType = moveType;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
        private void Ultimate()
        {
            if (!Player.IsValid || !Player.IsAlive()) return;

            var pawn = Player.PlayerPawn.Value;
            if (pawn == null) return;

            if (!UltimateToggle)
            {
                // Enable flying
                UltimateToggle = true;
                SetMoveType(pawn, MoveType_t.MOVETYPE_FLYGRAVITY);
                pawn.GravityScale = 0f;
                pawn.VelocityModifier = 1.5f;

                var velocity = pawn.AbsVelocity;
                velocity.Z = 200;
                pawn.Teleport(null, null, velocity);
            }
            else
            {
                // Disable flying
                UltimateToggle = false;
                SetMoveType(pawn, MoveType_t.MOVETYPE_WALK);
                pawn.GravityScale = 1f;
                pawn.VelocityModifier = 1f;
            }

            StartCooldown(3);
        }
        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0);

            int level = WarcraftPlayer.GetAbilityLevel(1);


            int BlindPercent = abilityLevel * 2;
            int chancePercent = level * 10;
            float blindTime = abilityLevel / 2f;


            if (!Warcraft.RollDice(BlindPercent, 100))
            {
                if (level <= 0) return;
                victim.Blind(blindTime, Color.WhiteSmoke);
            }

            if (!Warcraft.RollDice(chancePercent, 100)) return;

            if (level <= 0) return;

            SkillFunctions.SlowTarget(attacker, victim, 25, 3.5f);

            if (victim.PlayerPawn?.Value?.Health > 0) return;



            // Find a dead teammate
            var deadTeammates = Utilities.GetPlayers().Where(p =>
                p.TeamNum == Player.TeamNum && !p.IsAlive() && p != Player).ToList();

            if (deadTeammates.Count == 0) return;

            var revived = deadTeammates[Random.Shared.Next(deadTeammates.Count)];
            revived.Respawn();
            int bonusHp = 80 + (level * 10);
            var deathPosition = victim.PlayerPawn.Value.AbsOrigin;

            SkillFunctions.SetBonusHealth(revived, bonusHp);
            revived.RemoveWeapons();
            revived.GiveNamedItem("weapon_knife");
            revived.PlayerPawn.Value.SetColor(Color.Blue);

            Player.PrintToChat($" {ChatColors.Green}🧊 Water Elemental{ChatColors.Default}: You revived {revived.PlayerName} with {bonusHp} HP!");

            WarcraftPlugin.Instance.AddTimer(0.2f, () =>
            {
                if (revived?.IsValid == true && revived.PlayerPawn?.Value != null)
                {
                    revived.PlayerPawn.Value.Teleport(deathPosition, null, null);
                    var allowedWeapons = new List<string> { "weapon_knife" };
                    SkillFunctions.RestrictWeapons(revived, allowedWeapons, 999f);
                    revived.PrintToCenter($" {ChatColors.Blue}🧊 Water Elemental{ChatColors.Default} You were summoned by {attacker.PlayerName}!");
                }
            });
        }

    }
}



/*
 * 
 * 
 * 
 * 
 * POTENTIAL ULT FOR VAGABOND OR RAPSCALLION WORKS PERFECT MIDAIR

 
 private void Ultimate()
        {
            var pawn = Player.PlayerPawn.Value;
            if (pawn == null || !Player.IsAlive()) return;

            if (UltimateToggle)
            {
                // Toggle off: return to normal movement
                SetMoveType(pawn, MoveType_t.MOVETYPE_WALK);
                pawn.Teleport(null, null, new Vector(0, 0, 0)); // Stop movement
                UltimateToggle = false;
                Player.PrintToChat("🪂 You returned to the ground.");
            }
            else
            {
                // Toggle on: enable flying
                SetMoveType(pawn, MoveType_t.MOVETYPE_FLY);
                UltimateToggle = true;
                Player.PrintToChat("🕊️ You are now flying!");
            }

            StartCooldown(3);
        }
 
 
 
 * 
 * 
 * 
 * 
 */