using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
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
        private readonly Dictionary<CCSPlayerController, float> _hexCooldowns = new();

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Healing Wave", "You and your teammates gain additional health on spawn."),
            new WarcraftAbility("Hex", "6-30% chance to remove all bonushealth, bonus speed and invisibility from your target."),
            new WarcraftAbility("Serpent Ward", "Place a ward that damages and slows nearby enemies."),
            new WarcraftCooldownAbility("Big Bad Voodoo", "Become immune to all damage for the next 0.6-3 seconds", 30f, false)
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
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(0);
            if (abilityLevel <= 0) return;

            int bonusHealth = 6 * abilityLevel;

            if (Player.IsAlive())
            {
                int newHp = Player.PlayerPawn.Value.Health + bonusHealth;
                Player.SetHp(newHp);
                Player.PrintToChat($" {ChatColors.Green}Healing Wave{ChatColors.Default} : You gained {bonusHealth} bonus {ChatColors.LightPurple}health{ChatColors.Default}.");
            }

            if (WarcraftPlayer.GetAbilityLevel(2) > 0)
            {
                var decoy = new CDecoyGrenade(Player.GiveNamedItem("weapon_decoy"));
                decoy.AttributeManager.Item.CustomName = Localizer["ShadowHunter.ability.2"];
            }

            foreach (var teammate in Utilities.GetPlayers().Where(p => p.IsValid && p.TeamNum == Player.TeamNum && p != Player))
            {
                if (!teammate.IsAlive()) continue;

                int newHp = teammate.PlayerPawn.Value.Health + bonusHealth;
                teammate.SetHp(newHp);
                teammate.PrintToChat($" {ChatColors.Green}Healing Wave{ChatColors.Default} : {Player.PlayerName} healed you for {bonusHealth} {ChatColors.LightPurple}health {ChatColors.Default}!");
            }

            if (Player?.PlayerPawn?.Value == null) return;
            ResetCooldowns();
        }
        private void DecoyStart(EventDecoyStarted grenade)
        {
            if (WarcraftPlayer.GetAbilityLevel(2) <= 0)
                return;

            // Remove the actual decoy grenade so it doesn't distract/confuse players
            Utilities.GetEntityFromIndex<CDecoyProjectile>(grenade.Entityid)?.RemoveIfValid();

            // 🔥 Destroy any existing wards first
            foreach (var existingWard in activeWards)
                existingWard.Destroy();
            activeWards.Clear();

            // Create and place the new ward
            var origin = new Vector(grenade.X, grenade.Y, grenade.Z);
            var newWard = new SerpentWardEffect(Player, origin);
            newWard.Start();
            activeWards.Add(newWard);

            Player.PrintToChat($" {ChatColors.Green}Serpent Ward{ChatColors.Default} : Ward placed!");
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
            private readonly float _radius = 125f;
            private readonly int beamCount = 8;
            private float _rotationAngle = 0f;

            private readonly float _damageInterval = 0.7f;
            private readonly int _damage = 14;
            private Timer? _damageTimer;
            private Timer? _beamRotationTimer;
            private readonly CCSPlayerController _owner;
            private readonly List<CBeam> _beams = new();
            private int _rotationStep = 0;
            private readonly int _beamCount = 4;


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

                    WarcraftPlugin.Instance.AddTimer(1.0f, () => beam.RemoveIfValid());
                }

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
                        int hp = player.PlayerPawn.Value.Health;
                        SkillFunctions.DealRawDamage(_owner, player, _damage, KillFeedIcon.breachcharge);

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
                return;
            }

            float duration = 3f;

            _godModeActive = true;
            _ultimateActive = true;
            Player.PlayerPawn.Value.SetColor(Color.IndianRed);

            Player.PrintToChat($" {ChatColors.Green} Big Bad Voodoo {ChatColors.Default}:You are invincible for {duration} seconds!");

            WarcraftPlugin.Instance.AddTimer(duration, () =>
            {
                _godModeActive = false;
                _ultimateActive = false;
                Player.PrintToChat($" {ChatColors.Green} Big Bad Voodoo {ChatColors.Default}: Your invincibility has ended.");
                Player.PlayLocalSound("sounds/weapons/hkp2000/hkp2000_sliderelease.vsnd");
                Player.PlayerPawn.Value.SetColor(Color.White);
            });

            StartCooldown(3);
        }

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel <= 0) return;

            int chancePercent = 5 + (abilityLevel * 5);
            if (!Warcraft.RollDice(chancePercent, 100))
                return;

            var target = @event.Userid;
            if (!target.IsValid || !target.IsAlive() || Player.TeamNum == target.TeamNum)
                return;

            // Prevent multiple triggers on the same target
            float cooldownDuration = 2.0f;
            float currentTime = Server.CurrentTime;

            if (_hexCooldowns.TryGetValue(target, out float lastHexTime))
            {
                if (currentTime - lastHexTime < cooldownDuration)
                    return;
            }

            _hexCooldowns[target] = currentTime;

            // Remove buffs
            target.PlayerPawn.Value.VelocityModifier = 1f;
            if (target.PlayerPawn.Value.Health > 100)
            {
                target.SetHp(99);
            }

            target.PlayerPawn.Value.SetColor(Color.White);

            target.PrintToChat($" {ChatColors.Red}Hex{ChatColors.Default}: Your buffs have been removed by {Player.PlayerName}!");
            Player.PrintToChat($" {ChatColors.Green}Hex{ChatColors.Default}: Successfully removed buffs from {target.PlayerName}.");
        }



        private void PlayerHurt(EventPlayerHurt @event)
        {
            if (!@event.Userid.IsAlive() || @event.Userid.UserId != Player.UserId) return;

            if (_godModeActive)
            {
                @event.IgnoreDamage();
            }
        }
    }
}

