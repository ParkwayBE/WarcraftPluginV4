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
            new WarcraftAbility("Banish", "Obscure your target's screen, Enemies close to the target Have their ultimate immunity removed."), // TODO : STILL HAVE TO CODE THIS
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

            Player.PrintToCenter($" {ChatColors.Green}🔥 Phoenix ready!{ChatColors.Default} If you die in the next 10 seconds, you will rise again.");
            StartCooldown(3); // Index 3 = Ultimate
        }

        private void PlayerDeath(EventPlayerDeath death)
        {
            var victim = death.Userid;
            if (victim == null || !_phoenixActivationTime.TryGetValue(victim, out float lastUsedTime))
                return;

            WarcraftPlugin.Instance.AddTimer(2f, () =>
            {
                if (Server.CurrentTime - lastUsedTime <= 10f)
                {
                    // Respawn self
                    victim.Respawn();
                    victim.PrintToChat($" {ChatColors.Green}🔥 Phoenix{ChatColors.Default} triggered! You have returned from death.");

                    // Respawn up to 2 dead teammates
                    var teammates = Utilities.GetPlayers()
                        .Where(p => p != victim && p.TeamNum == victim.TeamNum && !p.IsAlive())
                        .Take(2)
                        .ToList();

                    foreach (var ally in teammates)
                    {
                        ally.Respawn();
                        ally.PrintToChat($" {ChatColors.LightPurple}🔥 You were revived by Phoenix!");
                    }

                    _phoenixActivationTime.Remove(victim);
                }
                else
                {
                    Console.WriteLine($"[WCS] Phoenix trigger window missed for {victim.PlayerName}");
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
                        Warcraft.SpawnParticle(enemy.PlayerPawn.Value.AbsOrigin, "particles/burning_fx/barrel_burning_engine_fire.vpcf", 1f);

                    }
                }
            }

            public override void OnFinish() { }
        }

        private class BanishEffect : WarcraftEffect
        {
            private readonly Vector _center;
            private readonly int _radius;
            private readonly int _duration;
            private readonly CCSPlayerController _victim;

            public BanishEffect(CCSPlayerController owner, Vector center, int radius, float duration, CCSPlayerController victim)
    : base(owner, duration)
            {
                _center = center;
                _radius = radius;
                _victim = victim;
            }


            public override void OnStart()
            {
                if (_victim.IsValid)
                {
                    // Blind Effect

                    int banishLevel = Owner.GetWarcraftPlayer()?.GetAbilityLevel(1) ?? 0;
                    if (banishLevel > 0)
                    {
                        _victim.Blind(_duration, Color.DarkRed);
                        Owner.PrintToChat($" {ChatColors.Green}Banish{ChatColors.Default}: You blinded {_victim.PlayerName}.");

                    }

                }
            }

            public override void OnTick()
            {
                foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && p.IsAlive() && p.TeamNum != Owner.TeamNum))
                {
                    if ((player.PlayerPawn.Value.AbsOrigin - _center).Length() < _radius)
                    {
                        var wcPlayer = player.GetWarcraftPlayer();
                        if (wcPlayer != null && wcPlayer.HasUltimateImmunity)
                        {
                            wcPlayer.HasUltimateImmunity = false;
                            player.PrintToChat($"{ChatColors.Red}❌ Your ultimate immunity has been stripped!");
                            Owner.PrintToChat($"{ChatColors.LightPurple}You removed ultimate immunity from {player.PlayerName}.");
                        }
                        else
                        {
                            Owner.PrintToChat("There were no players with immunity to strip.");
                        }
                    }
                }
            }

            public override void OnFinish()
            {
                // 
            }
        }


        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || victim == null || attacker == victim || attacker.TeamNum == victim.TeamNum) return;
            int banishLevel = WarcraftPlayer.GetAbilityLevel(1);
            var origin = victim.PlayerPawn.Value.AbsOrigin;
            origin.Z += 10;
            int flameLevel = WarcraftPlayer.GetAbilityLevel(0);
            int radius = 150;
            int damage = flameLevel;
            int ticks = 5;
            float tickInterval = 0.5f;
            float duration = 1f + (0.1f * banishLevel);

            //Flamestrike
            if (flameLevel > 0 && Random.Shared.Next(100) < 20 + flameLevel * 5) // 25–45% chance
            {
                new FlameStrikeEffect(attacker, origin, radius, damage, ticks, tickInterval).Start();
                attacker.PrintToChat($" {ChatColors.Orange}🔥 Flame Strike! Burning enemies at {victim.PlayerName}'s location.");
            }

            // Banish
            if (banishLevel > 0 && Random.Shared.Next(100) < 20 + banishLevel * 2) // 20-30% chance
            {
                new BanishEffect(attacker, origin, radius, duration, victim).Start();
                attacker.PrintToChat($" {ChatColors.Orange}Banish{ChatColors.Default}: Removing ultimate immunity.");
            }

            // --- Siphon Mana ---
            int siphonLevel = WarcraftPlayer.GetAbilityLevel(2); // Ability 2: Siphon Mana
            if (siphonLevel > 0 && Random.Shared.Next(100) < 5 + siphonLevel * 5) // 10-30% chance
            {
                var attackerMoneyService = attacker.InGameMoneyServices;
                var victimMoneyService = victim.InGameMoneyServices;

                if (attackerMoneyService != null && victimMoneyService != null)
                {
                    int stealAmount = 100 * siphonLevel;

                    int victimMoney = victimMoneyService.Account;
                    int attackerMoney = attackerMoneyService.Account;

                    victimMoneyService.Account = Math.Max(0, victimMoney - stealAmount);
                    attackerMoneyService.Account = Math.Min(16000, attackerMoney + stealAmount);

                    attacker.PrintToChat($" {ChatColors.Green}💰 You siphoned ${stealAmount} {ChatColors.Default}from {victim.PlayerName}!");
                    victim.PrintToChat($" {ChatColors.Red}💸 {attacker.PlayerName} {ChatColors.Default}siphoned ${stealAmount} from you!");
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
        }
    }

}