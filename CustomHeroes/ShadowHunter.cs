using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
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
        private bool _ultimateActive = false;



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
            var ward = new SerpentWardEffect(Player, origin);

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
            private readonly float _radius = 50f;
            private readonly int beamCount = 8;
            private float _rotationAngle = 0f;

            private readonly float _damageInterval = 0.7f;
            private readonly int _damage = 8;
            private Timer? _damageTimer;
            private Timer? _beamRotationTimer;
            private readonly CCSPlayerController _owner;
            private readonly List<CBeam> _beams = new();
            private int _rotationStep = 0;
            private readonly int _beamCount = 4; // Number of beams around the ward

            public SerpentWardEffect(CCSPlayerController owner, Vector origin)
                : base(owner, duration: float.MaxValue, destroyOnDeath: false, destroyOnRoundEnd: true)
            {
                _origin = origin;
                _owner = owner;
            }

            public override void OnStart()
            {
                Console.WriteLine($"[SerpentWard] Ward activated at {_origin}");

                Color beamColor = _owner.TeamNum == 2 ? Color.Red : Color.Cyan;

                // Create beams around the ward
                float radiusOffset = _radius * 0.75f;
                for (int i = 0; i < _beamCount; i++)
                {
                    float angle = (float)(2 * Math.PI * i / _beamCount);
                    var offset = new Vector(
                        radiusOffset * (float)Math.Cos(angle),
                        radiusOffset * (float)Math.Sin(angle),
                        0f
                    );

                    Vector start = _origin + offset;
                    Vector end = start.Clone();
                    end.Z += 200;

                    var beam = Warcraft.DrawLaserBetween(start, end, beamColor, duration: 240f, width: 10f);
                    _beams.Add(beam);
                }

                // Start rotation effect
                _beamRotationTimer = WarcraftPlugin.Instance.AddTimer(0.8f, RotateBeams, TimerFlags.REPEAT);

                // Start damage loop
                _damageTimer = WarcraftPlugin.Instance.AddTimer(_damageInterval, ApplyWardEffect, TimerFlags.REPEAT);
            }

            private void RotateBeams()
            {
                if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                    return;

                Vector origin = _origin;
                float step = (float)(2 * Math.PI / beamCount);
                float radius = _radius;
                float angleOffset = _rotationAngle;

                Color beamColor = Owner.TeamNum == (byte)CsTeam.Terrorist ? Color.Red : Color.LightBlue;
                Color secondaryColor = Owner.TeamNum == (byte)CsTeam.Terrorist ? Color.Orange : Color.Cyan;

                for (int i = 0; i < beamCount; i++)
                {
                    float angle = step * i + angleOffset;
                    float xOffset = (float)(Math.Cos(angle) * radius);
                    float yOffset = (float)(Math.Sin(angle) * radius);

                    Vector start = origin + new Vector(xOffset, yOffset, 0);
                    Vector end = start.Clone();
                    end.Z += 250;

                    Color thisColor = (i % 2 == 0) ? beamColor : secondaryColor;

                    var beam = Utilities.CreateEntityByName<CBeam>("beam");
                    if (beam == null) continue;

                    beam.Render = thisColor;
                    beam.Width = 12;
                    beam.Teleport(start, new QAngle(), new Vector());

                    // ✅ Set end position piece by piece
                    beam.EndPos.X = end.X;
                    beam.EndPos.Y = end.Y;
                    beam.EndPos.Z = end.Z;

                    beam.DispatchSpawn();



                    // Let each beam stay alive long enough to overlap with the next
                    WarcraftPlugin.Instance.AddTimer(1.0f, () => beam.RemoveIfValid());
                }

                // Rotate slowly
                _rotationAngle += 0.2f;
            }




            private void ApplyWardEffect()
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!player.IsValid || player.PlayerPawn?.Value == null || !player.IsAlive())
                        continue;

                    if (player.TeamNum == _owner.TeamNum)
                        continue;

                    var pos = player.PlayerPawn.Value.AbsOrigin;
                    var dx = pos.X - _origin.X;
                    var dy = pos.Y - _origin.Y;
                    var dz = pos.Z - _origin.Z;

                    float distanceSq = dx * dx + dy * dy + dz * dz;
                    if (distanceSq <= _radius * _radius)
                    {
                        player.EmitSound("common/talk.vsnd");
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
                _beamRotationTimer?.Kill();
                foreach (var beam in _beams)
                    WarcraftPlugin.Instance.AddTimer(1.5f, () => beam.RemoveIfValid());
            }

            public override void OnTick() { }
        }







        private void Ultimate()
        {
            if (WarcraftPlayer.GetAbilityLevel(3) <= 0)
                return;

            if (_ultimateActive)
            {
                Player.PrintToChat(" \x07[Big Bad Voodoo] You're already invincible!");
                return;
            }

            float duration = 3f;

            _godModeActive = true;
            _ultimateActive = true;

            Player.PrintToChat($" \x07[Big Bad Voodoo] \x02You are invincible for {duration:F1} seconds!");

            WarcraftPlugin.Instance.AddTimer(duration, () =>
            {
                _godModeActive = false;
                _ultimateActive = false;
                Player.PrintToChat(" \x07[Big Bad Voodoo] \x02Your invincibility has ended.");
            });

            StartCooldown(3);
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

