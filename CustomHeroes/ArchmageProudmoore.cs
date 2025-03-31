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

namespace WarcraftPlugin.Classes
{
    public class ArchmageProudmoore : WarcraftClass
    {
        public override string DisplayName => "Archmage Proudmoore";
        public override Color DefaultColor => Color.GreenYellow;
        private bool UltimateToggle = false;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Blizzard", "Chance to slow your target and obscure his vision ."),
            new WarcraftAbility("Water Elemental", "When you kill a player you have a chance to revive a teammate as a Water Elemental."),
            new WarcraftAbility("Brilliance Aura", "You and up to two random allies have a chance to block some ultimates."),
            new WarcraftCooldownAbility("Flight","Conjure a spell that allows you to fly.", 1f, false)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            UltimateToggle = false;

            int level = WarcraftPlayer.GetAbilityLevel(2);
            if (level <= 0) return;

            int chancePercent = 30 + (level * 10);
            int maxAllies = 2;

            // Grant to self
            if (Warcraft.RollDice(chancePercent, 100))
            {
                WarcraftPlayer.HasUltimateImmunity = true;
                Player.PrintToChat("🛡️ Brilliance Aura: You gained Ultimate Immunity for 120 seconds!");

                WarcraftPlugin.Instance.AddTimer(120f, () =>
                {
                    if (Player.IsValid)
                    {
                        WarcraftPlayer.HasUltimateImmunity = false;
                        Player.PrintToChat("⚠️ Your Ultimate Immunity has worn off.");
                    }
                });
            }

            // Grant to up to 2 random teammates
            var teammates = Utilities.GetPlayers().Where(p =>
                p != Player && p.IsValid && p.IsAlive() && p.TeamNum == Player.TeamNum).OrderBy(_ => Guid.NewGuid()).Take(maxAllies);

            foreach (var ally in teammates)
            {
                if (Warcraft.RollDice(chancePercent, 100))
                {
                    var wcAlly = ally.GetWarcraftPlayer();
                    if (wcAlly != null)
                    {
                        wcAlly.HasUltimateImmunity = true;
                        ally.PrintToChat("🛡️ Brilliance Aura: You received Ultimate Immunity for 120 seconds!");
                        Player.PrintToChat($"✨ Brilliance Aura: {ally.PlayerName} gained immunity!");

                        WarcraftPlugin.Instance.AddTimer(160f, () =>
                        {
                            if (ally.IsValid)
                            {
                                wcAlly.HasUltimateImmunity = false;
                                ally.PrintToChat("⚠️ Your Ultimate Immunity has worn off.");
                            }
                        });
                    }
                }
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
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(3);
            float duration = abilityLevel * 5f;

            Player.PrintToChat($" \x07[Flight] You are now flying for {duration} seconds!");

            // Enable flight mode
            Player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_FLY;

            // Optional: Give a small upward push


            var pawn = Player.PlayerPawn.Value;
            pawn.VelocityModifier = 1f + 0.5f;
            var velocity = pawn.AbsVelocity;
            pawn.GravityScale = 0f;
            velocity.Z = 5;

            // Reset after duration
            WarcraftPlugin.Instance.AddTimer(duration, () =>
            {
                if (!Player.IsValid || !Player.IsAlive()) return;

                Player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                Player.PlayerPawn.Value.GravityScale = 1.0f;
                Player.PrintToChat(" \x07[Flight] Your flight has ended.");
            });
        }





        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            SkillFunctions.SlowTarget(attacker, victim, 25, 3.5f);

            if (@event.DmgHealth >= victim.PlayerPawn.Value.Health)
            {
                int level = WarcraftPlayer.GetAbilityLevel(1);
                if (level <= 0) return;

                int chancePercent = level * 10;
                if (!Warcraft.RollDice(chancePercent, 100)) return;

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

                Player.PrintToChat($"🧊 Water Elemental: You revived {revived.PlayerName} with {bonusHp} HP!");
                revived.PrintToChat("💧 You were revived as a Water Elemental with only a knife!");

                WarcraftPlugin.Instance.AddTimer(0.2f, () =>
                {
                    if (revived?.IsValid == true && revived.PlayerPawn?.Value != null)
                    {
                        revived.PlayerPawn.Value.Teleport(deathPosition, null, null);
                        var allowedWeapons = new List<string> { "weapon_knife" };
                        SkillFunctions.RestrictWeapons(revived, allowedWeapons, 30f);
                        revived.PrintToCenter("💧 You were summoned at the site of death!");
                    }
                });

            }

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