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
using System.Linq;
using WarcraftPlugin.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Helpers;
namespace WarcraftPlugin.Classes
{
    public class Venusaur : WarcraftClass
    {
        public override string DisplayName => "Venusaur";
        public override Color DefaultColor => Color.GreenYellow;
        private bool AcurracyDrop = false;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Mega drain", "Heal for a percentage of the damage dealt. "),
            new WarcraftAbility("Vine Snare", "Chance to root enemies. Rooted enemies have lower accuracy."),
            new WarcraftAbility("Solar Beam", "Your attacks have a chance to fire a solar beam after a short delay."),
            new WarcraftCooldownAbility("Leech Seed","Place a seed underneath the player. The first time a player is too close to the seed, root them and drain health from them.", 1f)
        ];

        /* 
        TODO: Special Idea:
            - Charizard deals double damage against Venusaur and receives half damage from Venusaur
            - Venusaur deals double damage against Blastoise and receives half damage from Blastoise
            - Blastoise deals double damage against Charizard and receives half damage from Charizard
        */


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
        }
        private void Ultimate()
        {
            // TODO: Leech seed: Place a totemlike "seed" that acts like a ward that damages nearby players, 
            // heals you wherever you are (if not dead) and roots the first player that gets too close to the seed.

            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO:  Solar Beam  : chance to spawn a solar beam on your target, it's a delayed beam of energy that lands on the spot where the enemy was when the skill activates
            // TODO:  Vine Snare  : Chance to root enemies + rooted enemies have lower accuracy
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;
            var victimName = Warcraft.GetRealPlayerName(victim);

            if (victimName.Contains("Blastoise"))
            {
                var damageDealt = @event.DmgHealth;
                int bonusDamage = damageDealt / 5;
                SkillFunctions.DealRawDamage(attacker, victim, bonusDamage);
            }

            // Lifedrain effect
            var abilityLevel = WarcraftPlayer.GetAbilityLevel(0);
            if (abilityLevel < 1) return;

            float healPercent = abilityLevel * 2;
            SkillFunctions.LeechHealth(attacker, victim, 100, healPercent, @event.DmgHealth); // 100% chance ot heal up to 10%

            // FREEZE EFFECT , TODO: move freeze visual effect to be race internal instead of inside the freeze function.
            var abilityLevel2 = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel2 < 1) return;

            int freezeChance = abilityLevel2 * 2;
            SkillFunctions.FreezePlayer(attacker, victim, freezeChance, 1.5f);
            AcurracyDrop = true;
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                AcurracyDrop = false;
            });
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;
            var attackerName = Warcraft.GetRealPlayerName(attacker);

            if (attackerName.Contains("Blastoise"))
            {
                var dmgTaken = @event.DmgHealth;
                int DmgNegate = dmgTaken / 5;
                SkillFunctions.SetBonusHealth(victim, DmgNegate);
            }

            if (AcurracyDrop)
            {
                HandleEvasion(@event);
            }
            else return;
        }

        private void HandleEvasion(EventPlayerHurt @event)
        {
            if (Player == null) return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel == 0) return;

            int evasionChance = abilityLevel * 10;

            var roll = Random.Shared.Next(100);
            if (roll < evasionChance)
            {
                @event.IgnoreDamage();
                Player.PrintToChat($" {ChatColors.Default}Vine Snare{ChatColors.Default} : Your enemy is missing his shots.");
            }
        }

    }
}