using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;


namespace WarcraftPlugin.Classes
{
    public class BloodMage : WarcraftClass
    {
        public override string DisplayName => "Blood Mage";
        public override Color DefaultColor => Color.GreenYellow;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Flame Strike", "Call down flames when hitting an enemy."),
            new WarcraftAbility("Banish", "Obscure your target's screen, Enemies close to the target take additional damage for the next 5-10 seconds."),
            new WarcraftAbility("Siphon Mana", "5-25% to siphon money from your target. You deal an additional point of damage for every 1000 dollars you have."),
            new WarcraftCooldownAbility("Phoenix", "If you activated your ultimate in the last 10 seconds when you die. You will respawn yourself and up to 2 teammates!", 8f, false)
        ];

        private readonly Dictionary<CCSPlayerController, float> _phoenixActivationTime = new();

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookAbility(3, Ultimate);
        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // Placeholder: Could restore state if needed
        }

        private void Ultimate()
        {
            if (Player == null || !Player.IsValid) return;

            _phoenixActivationTime[Player] = Server.CurrentTime;

            Player.PrintToCenter($"{ChatColors.Orange}🔥 Phoenix ready! If you die in the next 10 seconds, you will rise again.");
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerDeath(EventPlayerDeath death)
        {
            if (Player == null || !_phoenixActivationTime.TryGetValue(Player, out float lastUsedTime))
                return;

            WarcraftPlugin.Instance.AddTimer(2f, () =>
            {


                if (Server.CurrentTime - lastUsedTime <= 10f)
                {
                    // Respawn self
                    Player.Respawn();
                    Player.PrintToChat($"{ChatColors.LightRed}🔥 Phoenix triggered! You have returned from death.");

                    // Respawn up to 2 teammates
                    var teammates = Utilities.GetPlayers()
                        .Where(p => p != Player && p.TeamNum == Player.TeamNum && !p.IsAlive())
                        .Take(2)
                        .ToList();

                    foreach (var ally in teammates)
                    {
                        ally.Respawn();
                        ally.PrintToChat($"{ChatColors.LightPurple}🔥 You were revived by Phoenix!");
                    }

                    _phoenixActivationTime.Remove(Player);
                }
            });
        }


        private class FlameStrikeEffect : WarcraftEffect
        {
            private readonly Vector _center;
            private readonly int _radius;
            private readonly int _damage;
            private int _tick = 0;
            private readonly int _maxTicks;

            public FlameStrikeEffect(CCSPlayerController owner, Vector center, int radius, int damage, int ticks, float interval)
    : base(owner, ticks * interval)
            {
                _center = center;
                _radius = radius;
                _damage = damage;
                _maxTicks = ticks;
                OnTickInterval = interval;
            }

            public override void OnStart()
            {
            }

            public override void OnTick()
            {
                _tick++;
                foreach (var enemy in Utilities.GetPlayers().Where(p => p.IsAlive() && p.TeamNum != Owner.TeamNum))
                {
                    if ((enemy.PlayerPawn.Value.AbsOrigin - _center).Length() < _radius)
                    {
                        SkillFunctions.DealRawDamage(Owner, enemy, _damage);
                        enemy.PrintToChat($"{ChatColors.Red}🔥 You're burning!");
                        Warcraft.SpawnParticle(Owner.PlayerPawn.Value.AbsOrigin, "particles/burning_fx/barrel_burning_engine_fire.vpcf", 1f);

                    }
                }
            }

            public override void OnFinish() { }
        }



        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || victim == null || attacker == victim) return;

            //Flamestrike
            int flameLevel = WarcraftPlayer.GetAbilityLevel(0);
            if (flameLevel > 0 && Random.Shared.Next(100) < 20 + flameLevel * 5) // 25–45% chance
            {
                var origin = victim.PlayerPawn.Value.AbsOrigin;
                origin.Z += 10;

                int radius = 150;
                int damage = 5 + flameLevel * 5; // Scales with level
                int ticks = 5;
                float tickInterval = 0.5f;

                new FlameStrikeEffect(attacker, origin, radius, damage, ticks, tickInterval).Start();

                attacker.PrintToChat($"{ChatColors.Orange}🔥 Flame Strike! Burning enemies at {victim.PlayerName}'s location.");
            }





            // --- Siphon Mana ---
            int siphonLevel = WarcraftPlayer.GetAbilityLevel(2); // Ability 2: Siphon Mana
            if (siphonLevel > 0 && Random.Shared.Next(100) < 5 + siphonLevel * 5) // 10-30% chance
            {
                var attackerMoneyService = attacker.InGameMoneyServices;
                var victimMoneyService = victim.InGameMoneyServices;

                if (attackerMoneyService != null && victimMoneyService != null)
                {
                    int stealAmount = 100 + 100 * siphonLevel;

                    int victimMoney = victimMoneyService.Account;
                    int attackerMoney = attackerMoneyService.Account;

                    victimMoneyService.Account = Math.Max(0, victimMoney - stealAmount);
                    attackerMoneyService.Account = Math.Min(16000, attackerMoney + stealAmount);

                    attacker.PrintToChat($" {ChatColors.Green}💰 You siphoned ${stealAmount} from {victim.PlayerName}!");
                    victim.PrintToChat($" {ChatColors.Red}💸 {attacker.PlayerName} siphoned ${stealAmount} from you!");
                }

            }
            // Insert after siphon or flame logic in PlayerHurtOther

            var attackerMoneyService2 = attacker.InGameMoneyServices;
            if (attackerMoneyService2 != null)
            {
                int currentMoney = attackerMoneyService2.Account;
                int bonusDamage = currentMoney / 1000; // scale 1 per 1k

                if (bonusDamage > 0)
                {
                    @event.AddBonusDamage(bonusDamage);
                    attacker.PrintToChat($" {ChatColors.Orange}💸 Siphon Mana: +{bonusDamage} bonus damage from your ${currentMoney}!");
                }
            }



            // --- Banish (logic stub) ---
            int banishLevel = WarcraftPlayer.GetAbilityLevel(1); // Ability 1: Banish
            if (banishLevel > 0)
            {
                // TO DO: Add visual + AoE splash around victim for the next 5–10s
                // Suggestion: Freeze screen + slow + bonus damage flag
            }
        }
    }

}