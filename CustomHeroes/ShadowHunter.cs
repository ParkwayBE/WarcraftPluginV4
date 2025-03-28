using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace WarcraftPlugin.Classes
{
    public class ShadowHunter : WarcraftClass
    {
        public override string DisplayName => "Shadow Hunter";
        public override Color DefaultColor => Color.GreenYellow;
        private bool _godModeActive = false;
        private readonly List<CCSPlayerController> _slowedPlayers = new();


        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Healing Wave", "You and your teammates gain additional health on spawn."),
            new WarcraftAbility("Hex", "6-30% chance to remove all bonushealth, bonus speed and invisibility from your target."),
            new WarcraftAbility("Serpent Ward", "Place a ward that damages and slows nearby enemies."),
            new WarcraftCooldownAbility("Big Bad Voodoo", "Become immune to all damage for the next 0.6-3 seconds", 8f)
        ];

        public override void Register()
        {
            HookEvent<EventRoundStart>(RoundStart);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            HookEvent<EventFlashbangDetonate>(OnWardDetonate);

            HookAbility(3, Ultimate);
        }

        private void RoundStart(EventRoundStart @event)
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0); // Healing Wave
            if (abilityLevel <= 0) return;

            int bonusHealth = 5 * abilityLevel;

            // Heal self
            if (Player.IsAlive())
            {
                int newHp = Player.PlayerPawn.Value.Health + bonusHealth;
                Player.SetHp(newHp);
                Player.PrintToChat($" \x04[Healing Wave] You gained {bonusHealth} bonus HP from a Shadow Hunter.");
                Player.GiveNamedItem("weapon_flashbang");
                Player.GiveNamedItem("weapon_flashbang");
            }

            // Heal teammates
            foreach (var teammate in Utilities.GetPlayers().Where(p => p.IsValid && p.TeamNum == Player.TeamNum && p != Player))
            {
                if (!teammate.IsAlive()) continue;

                int newHp = teammate.PlayerPawn.Value.Health + bonusHealth;
                teammate.SetHp(newHp);
                teammate.PrintToChat($" \x04[Healing Wave] {Player.PlayerName} healed you for {bonusHealth} HP!");
            }
        }

        private void OnWardDetonate(EventFlashbangDetonate flashbang)
        {
            if (flashbang.Userid != Player || !Player.IsValid) return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);
            if (abilityLevel <= 0) return;

            Vector origin = flashbang.Userid.PlayerPawn.Value.AbsOrigin;
            float radius = 250f;
            float damagePerTick = 4f + abilityLevel;
            float slowFactor = 0.7f;
            float duration = 5f + abilityLevel;
            float tickRate = 1f;

            Player.PrintToChat($" \x07[Serpent Ward] Ward activated!");

            // Visual effect: red beam
            Vector top = origin + new Vector(0, 0, 200);
            Warcraft.DrawLaserBetween(origin, top, Color.Red, duration, 2f);

            int ticks = (int)(duration / tickRate);

            void TickWard()
            {
                if (ticks-- <= 0)
                {
                    foreach (var p in _slowedPlayers)
                    {
                        if (p.IsValid && p.IsAlive())
                            p.PlayerPawn.Value.VelocityModifier = 1f;
                    }
                    _slowedPlayers.Clear();
                    return;
                }

                foreach (var target in Utilities.GetPlayers().Where(p => p.IsValid && p.IsAlive() && p.TeamNum != Player.TeamNum))
                {
                    var pos = target.PlayerPawn.Value.AbsOrigin;
                    var diff = pos - origin;
                    if (diff.Length() < radius)
                    {
                        int newHp = target.PlayerPawn.Value.Health - (int)damagePerTick;
                        target.SetHp(newHp);
                        target.PlayerPawn.Value.VelocityModifier = slowFactor;

                        if (!_slowedPlayers.Contains(target)) _slowedPlayers.Add(target);

                        target.PrintToChat(" \x07[Serpent Ward] You are being damaged and slowed!");
                    }
                }

                WarcraftPlugin.Instance.AddTimer(tickRate, TickWard);
            }

            TickWard(); // Start the first tick
        }

        private void Ultimate()
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(3);
            float duration = abilityLevel / 2f;

            _godModeActive = true;
            Player.PrintToChat($" \x07[GodMode] You are invincible for {duration} seconds!");

            WarcraftPlugin.Instance.AddTimer(duration, () =>
            {
                _godModeActive = false;
                Player.PrintToChat(" \x07[GodMode] Your invincibility has ended.");
            });
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel <= 0) return;

            float chance = 0.05f + (abilityLevel * 0.05f); // 6% → 30%
            if (Random.Shared.NextDouble() > chance) return;

            var target = @event.Userid;
            if (!target.IsValid || !target.IsAlive()) return;

            // Remove buffs
            target.PlayerPawn.Value.VelocityModifier = 1f;
            if (target.PlayerPawn.Value.Health > 100) target.SetHp(100);
            target.PlayerPawn.Value.SetColor(Color.White);

            target.PrintToChat($" \x07[Hexed] Your buffs have been removed by {Player.PlayerName}!");
            Player.PrintToChat(" \x04[Hex] Successfully removed buffs from your target.");
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            if (!@event.Userid.IsAlive() || @event.Userid.UserId != Player.UserId) return;

            if (_godModeActive)
            {
                @event.IgnoreDamage();
                Player.PrintToChat(" \x07[GodMode] Damage blocked!");
            }
        }
    }
}