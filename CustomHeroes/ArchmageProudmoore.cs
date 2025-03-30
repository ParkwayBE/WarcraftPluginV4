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
            new WarcraftCooldownAbility("Flight","Conjure a spell that allows you to fly.", 1f)
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

                        WarcraftPlugin.Instance.AddTimer(120f, () =>
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


        private void Ultimate()
        {
            var pawn = Player.PlayerPawn.Value;
            if (pawn == null || !Player.IsAlive()) return;

            if (UltimateToggle)
            {
                // Toggle off: return to normal movement
                pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                pawn.Teleport(null, null, new Vector(0, 0, 0)); // Reset velocity safely
                UltimateToggle = false;
                Player.PrintToChat("🪂 You returned to the ground.");
            }
            else
            {
                // Toggle on: flight
                pawn.MoveType = MoveType_t.MOVETYPE_FLY;
                UltimateToggle = true;
                Player.PrintToChat("🕊️ You are now flying!");
            }

            StartCooldown(3); // Apply cooldown after activation
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
                SkillFunctions.SetBonusHealth(revived, bonusHp);
                revived.RemoveWeapons();
                revived.GiveNamedItem("weapon_knife");
                revived.PlayerPawn.Value.SetColor(Color.Blue);

                Player.PrintToChat($"🧊 Water Elemental: You revived {revived.PlayerName} with {bonusHp} HP!");
                revived.PrintToChat("💧 You were revived as a Water Elemental with only a knife!");
            }

        }

    }
}