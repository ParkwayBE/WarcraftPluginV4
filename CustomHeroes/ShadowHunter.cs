using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using g3;
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

            // Heal teammates
            foreach (var teammate in Utilities.GetPlayers().Where(p => p.IsValid && p.TeamNum == Player.TeamNum && p != Player))
            {
                if (!teammate.IsAlive()) continue;

                int newHp = teammate.PlayerPawn.Value.Health + bonusHealth;
                teammate.SetHp(newHp);
                teammate.PrintToChat($" \x04[Healing Wave] {Player.PlayerName} healed you for {bonusHealth} HP!");

                if (WarcraftPlayer.GetAbilityLevel(2) > 0)
                {
                    var decoy = new CDecoyGrenade(Player.GiveNamedItem("weapon_decoy"));
                    decoy.AttributeManager.Item.CustomName = Localizer["ShadowHunter.ability.2"];
                }
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
            private readonly Vector origin;
            private CBaseEntity? beamEntity;
            private Box3d _auraZone;

            public SerpentWardEffect(CCSPlayerController owner, Vector origin)
                : base(owner, duration: 999f, onTickInterval: 0.5f)
            {
                this.origin = origin;
            }

            public override void OnStart()
            {
                // Draw the vertical red beam
                beamEntity = Warcraft.DrawLaserBetween(origin, origin.With(z: origin.Z + 400), Color.Red, Duration);

                // Define a damage zone
                _auraZone = Warcraft.CreateBoxAroundPoint(origin, 200, 200, 200);
                //_auraZone.Show(30); // optional debug
            }

            public override void OnTick()
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!player.IsAlive() || player.TeamNum == Owner.TeamNum || player.PlayerPawn?.Value == null)
                        continue;

                    if (_auraZone.Contains(player.PlayerPawn.Value.AbsOrigin))
                    {
                        player.TakeDamage(3, Owner, KillFeedIcon.tripwirefire);
                        player.PlayerPawn.Value.VelocityModifier = 0.7f;
                        player.PlayerPawn.Value.MovementServices.Maxspeed = 180;

                        Warcraft.SpawnParticle(player.EyePosition(), "particles/blood_impact/blood_impact_basic.vpcf");
                    }
                }
            }

            public override void OnFinish()
            {
                beamEntity?.RemoveIfValid();
            }
        }




        private void Ultimate()
        {
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(3);
            float duration = 0.6f + (abilityLevel * 0.5f);

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

