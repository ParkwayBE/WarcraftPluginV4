using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using WarcraftPlugin.Core.Effects;
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
        private readonly List<SerpentWardEffect> activeWards = new();



        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Healing Wave", "You and your teammates gain additional health on spawn."),
            new WarcraftAbility("Hex", "6-30% chance to remove all bonushealth, bonus speed and invisibility from your target."),
            new WarcraftAbility("Serpent Ward", "Place a ward that damages and slows nearby enemies."),
            new WarcraftCooldownAbility("Big Bad Voodoo", "Become immune to all damage for the next 0.6-3 seconds", 8f, false)
        ];

        public override void Register()
        {
            HookEvent<EventRoundStart>(RoundStart);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            HookEvent<EventDecoyStarted>(DecoyStart, HookMode.Post);
            HookEvent<EventRoundEnd>(OnRoundEnd);

            HookAbility(3, Ultimate);
        }

        private void RoundStart(EventRoundStart @event)
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0); // Healing Wave
            if (abilityLevel <= 0) return;

            int bonusHealth = 6 * abilityLevel;

            // Heal self
            if (Player.IsAlive())
            {
                int newHp = Player.PlayerPawn.Value.Health + bonusHealth;
                Player.SetHp(newHp);
                Player.PrintToChat($" \x04[Healing Wave] You gained {bonusHealth} bonus HP from a Shadow Hunter.");
            }

            if (WarcraftPlayer.GetAbilityLevel(2) > 0)
            {
                var decoy = new CDecoyGrenade(Player.GiveNamedItem("weapon_decoy"));
                decoy.AttributeManager.Item.CustomName = Localizer["ShadowHunter.ability.2"];
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
        private void DecoyStart(EventDecoyStarted grenade)
        {
            if (WarcraftPlayer.GetAbilityLevel(2) <= 0)
                return;

            Utilities.GetEntityFromIndex<CDecoyProjectile>(grenade.Entityid)?.RemoveIfValid();

            var origin = new Vector(grenade.X, grenade.Y, grenade.Z);
            var ward = new SerpentWardEffect(origin);
            ward.Start();
            activeWards.Add(ward);

            Player.PrintToChat("\x04[Serpent Ward] Ward placed!");
        }

        private void OnRoundEnd(EventRoundEnd @event)
        {
            foreach (var ward in activeWards)
                ward.Destroy();
            activeWards.Clear();
        }

        internal class SerpentWardEffect : WarcraftEffect
        {
            private readonly Vector _origin;
            private readonly float _radius = 200f;
            private readonly float _damageInterval = 0.5f;
            private readonly int _damage = 5;
            private Timer? _damageTimer;

            public SerpentWardEffect(Vector origin)
                : base(null, duration: float.MaxValue, destroyOnDeath: false, destroyOnRoundEnd: true)
            {
                _origin = origin;
            }

            public override void OnStart()
            {
                Console.WriteLine("[SerpentWard] Ward activated at " + _origin);

                // Beam goes up from ward
                Vector beamEnd = _origin.Clone();
                beamEnd.Z += 200;
                Warcraft.DrawLaserBetween(_origin, beamEnd, Color.Red, duration: 15.0f);

                // Damage loop
                _damageTimer = WarcraftPlugin.Instance.AddTimer(_damageInterval, ApplyWardEffect, TimerFlags.REPEAT);
            }

            private void ApplyWardEffect()
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!player.IsValid || player.PlayerPawn?.Value == null || !player.IsAlive())
                        continue;

                    var pos = player.PlayerPawn.Value.AbsOrigin;
                    var dx = pos.X - _origin.X;
                    var dy = pos.Y - _origin.Y;
                    var dz = pos.Z - _origin.Z;

                    float distanceSq = dx * dx + dy * dy + dz * dz;

                    if (distanceSq <= _radius * _radius)
                    {
                        int hp = player.PlayerPawn.Value.Health;
                        if (hp <= _damage)
                        {
                            player.CommitSuicide(true, true);
                            Console.WriteLine($"[SerpentWard] {player.PlayerName} was killed by the Ward!");
                        }
                        else
                        {
                            player.SetHp(hp - _damage);
                            Console.WriteLine($"[SerpentWard] {player.PlayerName} took {_damage} damage from the Ward.");
                        }
                    }
                }
            }

            public override void OnFinish()
            {
                _damageTimer?.Kill();
            }

            public override void OnTick() { }
        }


        private void Ultimate()
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(3);
            float duration = 0.6f + (abilityLevel * 0.5f);

            _godModeActive = true;
            Player.PrintToChat($" \x07[Big Bad Voodoo] \x02You are invincible for {duration} seconds!");

            WarcraftPlugin.Instance.AddTimer(duration, () =>
            {
                _godModeActive = false;
                Player.PrintToChat(" \x07[Big Bad Voodoo] \x02Your invincibility has ended.");
            });
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel <= 0) return;

            float chance = 0.05f + (abilityLevel * 0.05f);
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

