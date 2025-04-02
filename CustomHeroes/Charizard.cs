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
    public class Charizard : WarcraftClass
    {
        public override string DisplayName => "Charizard";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Flamethrower", "Chance to burn enemies for up to 5 seconds. "),
            new WarcraftAbility("Wing Attack", "Your attacks have a chance to push the target back."),
            new WarcraftAbility("Sunny Day", "Enemies shooting you have a chance to get blinded and miss instead."),
            new WarcraftCooldownAbility("Overheat","After charging for a short amount of time, unleash a massive explosion of energy.", 1f)
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

            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            // TODO: Module : 
        }
        private void Ultimate()
        {
            // TODO: Overheat: Use big sfx explosion, to deal damage in a massive radius
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            // TODO: Sunny Day : Blind enemies that hit you, evade damage;  both on %
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker.Contains("Blastoise"))
            {


            }
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;

            // Sharp End: Bleed effect
            int level = WarcraftPlayer.GetAbilityLevel(0);
            if (level > 0 && @event.Weapon == "knife" && Warcraft.RollDice(level * 10, 100))
            {
                int totalTicks = level; // 5–10 ticks
                int damagePerTick = 2 + (level / 2); // 2–4 damage

                new BurnEffect(attacker, victim, totalTicks, damagePerTick).Start();
                attacker.PrintToChat($"{ChatColors.Red} Flamethrower{ChatColors.Default}: You inflicted burn!");
            }

            var victimName = Warcraft.GetRealPlayerName(victim);

            if (victimName.Contains("Venusaur"))
            {
                var damageDealt = @event.DmgHealth;
                int bonusDamage = damageDealt / 5;
                SkillFunctions.DealRawDamage(attacker, victim, bonusDamage);

            }



            // TODO: Wing attack
        }

        private class BurnEffect : WarcraftEffect
        {
            private readonly CCSPlayerController _target;
            private readonly int _ticks;
            private readonly int _damage;
            private int _currentTick;

            public BurnEffect(CCSPlayerController owner, CCSPlayerController target, int ticks, int damage)
                : base(owner, ticks * 0.5f)
            {
                _target = target;
                _ticks = ticks;
                _damage = damage;
            }
            public override void OnStart()
            {
                Owner.PrintToChat($"{ChatColors.Red}DEBUG{ChatColors.Default} BURN EFFECT called");
                // needs to be here
            }

            public override void OnTick()
            {
                if (_currentTick >= _ticks || !_target.IsAlive()) return;

                SkillFunctions.DealRawDamage(Owner, _target, _damage);
                _currentTick++;
            }

            public override void OnFinish() { }
        }

    }
}