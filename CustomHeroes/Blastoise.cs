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
    public class Blastoise : WarcraftClass
    {
        public override string DisplayName => "Blastoise";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Shell Armor", "You gain up to 100 armor on spawn and you block up to 80% of the damage from behind"),
            new WarcraftAbility("Rain dance", "Enemies that hit you have a chance to get hit by a rogue wave."),
            new WarcraftAbility("Water Pulse", "Your attacks have a chance to confuse the target dealing extra damage and lowering the targets accuracy."),
            new WarcraftCooldownAbility("Aqua Jet","Blink forward", 1f) // TODO: should be short distance but maybe additional benefits like shooting bubbles at nearby players
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
            // TODO: Shell Armor : gain armor
        }
        private void Ultimate()
        {
            // TODO: Aqua Jet: Dash short distance and maybe additional effect, no idea? look skill description note
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            // TODO:  Water Pulse  : chance to confuse enemies 
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;
            var victimName = Warcraft.GetRealPlayerName(victim);

            if (victimName.Contains("Charizard"))
            {
                var damageDealt = @event.DmgHealth;
                int bonusDamage = damageDealt / 5;
                SkillFunctions.DealRawDamage(attacker, victim, bonusDamage);
            }
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            // TODO: Rain Dance: Chance to get hit by rogue wave
            // TODO: Shell armor: block up to 80% of the damage dealt from behind 
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            if (attacker == null || victim == null || !attacker.IsAlive() || !victim.IsAlive()) return;

            var attackerName = Warcraft.GetRealPlayerName(attacker);

            if (attackerName.Contains("Charizard"))
            {
                var dmgTaken = @event.DmgHealth;
                int DmgNegate = dmgTaken / 5;
                SkillFunctions.SetBonusHealth(victim, DmgNegate);
            }

            if (WarcraftPlayer.GetAbilityLevel(1) > 0) Backstab(@event);

        }

        private void Backstab(EventPlayerHurt eventPlayerHurtOther)
        {
            var attackerAngle = eventPlayerHurtOther.Attacker.PlayerPawn.Value.EyeAngles.Y;
            var victimAngle = eventPlayerHurtOther.Userid.PlayerPawn.Value.EyeAngles.Y;

            if (Math.Abs(attackerAngle - victimAngle) <= 50)
            {
                var BackstabDmgNegation = WarcraftPlayer.GetAbilityLevel(0) * 10;
                SkillFunctions.SetBonusHealth(eventPlayerHurtOther.Userid, BackstabDmgNegation);
                Warcraft.SpawnParticle(eventPlayerHurtOther.Userid.PlayerPawn.Value.AbsOrigin.Clone().Add(z: 85), "particles/overhead_icon_fx/radio_voice_flash.vpcf", 1);
            }
        }

    }
}