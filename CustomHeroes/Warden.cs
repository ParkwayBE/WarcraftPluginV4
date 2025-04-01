using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class Warden : WarcraftClass
    {
        private bool _hasUsedRevive = false;

        public override string DisplayName => "Warden";
        public override Color DefaultColor => Color.GreenYellow;
        private float _lastThrowTime = 0f;
        private const float ThrowCooldown = 1.5f; // seconds between throws


        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Sharp End", "Chance for your attacks to deal bleed damage"),
            new WarcraftAbility("Mercy or Revenge", "50% chance to either revive or punish your killer."),
            new WarcraftAbility("Fan Of Knives", "Your knife attacks are throwing knife attacks."),
            new WarcraftCooldownAbility("Eternal Darkness", "Blind and slow nearby enemies. Gain speed per enemy hit.", 25f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            // HookEvent<EventPlayerShoot>(PlayerShoot);
            HookAbility(3, Ultimate);

            WarcraftPlugin.Instance.AddTimer(0.05f, () => CheckThrowingKnifeLoop());

        }

        private float TimeSinceMapStart()
        {
            return (float)Server.EngineTime;
        }


        private void CheckThrowingKnifeLoop()
        {
            CheckThrowingKnife();
            WarcraftPlugin.Instance.AddTimer(0.05f, () => CheckThrowingKnifeLoop()); // Re-loop every 50ms
        }


        private void CheckThrowingKnife()
        {
            if (Player == null || !Player.IsValid || !Player.IsAlive()) return;

            var weapon = Player.PlayerPawn.Value.WeaponServices?.ActiveWeapon.Value;
            if (weapon == null || weapon.DesignerName != "weapon_knife") return;

            // Detect right-click
            ulong buttons = Player.PlayerPawn.Value.MovementServices.Buttons.ButtonStates[0];
            bool isRightClick = (buttons & (ulong)PlayerButtons.Attack2) != 0;

            if (!isRightClick) return;

            // Use server tick time (approx fallback)
            float now = TimeSinceMapStart();

            if (now - _lastThrowTime < ThrowCooldown) return;

            _lastThrowTime = now;

            if (WarcraftPlayer.GetAbilityLevel(2) > 0 && IsAbilityReady(2))
            {
                Player.PrintToChat($"{ChatColors.Red}DEBUG{ChatColors.Default} Fan of Knives THROW triggered!");
                new ThrowingKnifeEffect(Player).Start();
            }
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            _hasUsedRevive = false;

            // Restrict to knife only
            var allowedWeapons = new List<string> { "weapon_knife" };
            SkillFunctions.RestrictWeapons(Player, allowedWeapons, 999f);
        }


        /*
        private void PlayerShoot(EventPlayerShoot shoot)
        {
            Player.PrintToChat($"{ChatColors.Red}DEBUG{ChatColors.Default} Player firing his knife");
            if (WarcraftPlayer.GetAbilityLevel(2) <= 0) return; // Fan of Knives
            if (Player == null || !Player.IsValid || !Player.IsAlive()) return;

            var activeWeapon = Player.PlayerPawn.Value.WeaponServices?.ActiveWeapon.Value;
            if (activeWeapon == null || activeWeapon.DesignerName != "weapon_knife") return;

            Player.PrintToChat($"{ChatColors.Red}DEBUG{ChatColors.Default} Player is valid and holding a knife");

            ulong buttons = Player.PlayerPawn.Value.MovementServices.Buttons.ButtonStates[0];
            bool isRightClick = (buttons & (ulong)PlayerButtons.Attack2) != 0;

            Player.PrintToChat($"{ChatColors.Red}DEBUG{ChatColors.Default} Player is using RMB");

            if (!isRightClick) return;
            if (!IsAbilityReady(2)) return;
            new ThrowingKnifeEffect(Player).Start();
        } */


        private class ThrowingKnifeEffect : WarcraftEffect
        {
            private Vector _position;
            private Vector _direction;
            private float _speed = 1200f;
            private float _travelTime;
            private float _maxDistance = 1500f;
            private CDynamicProp _visualKnife;

            public ThrowingKnifeEffect(CCSPlayerController owner)
                : base(owner, 10f) // Keep alive for testing; we'll control duration manually
            {
            }

            public override void OnStart()
            {
                // Calculate spawn position & direction
                _position = Owner.CalculatePositionInFront(60, Owner.EyeHeight());
                _direction = Owner.PlayerPawn.Value.EyeAngles.ToForward();

                // Create the visual knife
                _visualKnife = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
                _visualKnife.SetModel("models/weapons/w_knife_gg.vmdl");
                _visualKnife.SetScale(1.0f); // Make it visible

                _visualKnife.Teleport(_position, new QAngle(), new Vector());
                _visualKnife.DispatchSpawn();

                // Optional: future visual boost (particles, light aura)
                Owner.PrintToChat($"{ChatColors.Red}DEBUG{ChatColors.Default} ThrowingKnife OnStart effect called");
            }


            public override void OnTick()
            {
                float deltaTime = 0.02f; // assuming ~50 ticks/sec
                _travelTime += deltaTime;

                // Move knife forward
                _position += _direction * (_speed * deltaTime);
                _visualKnife?.Teleport(_position, new QAngle(z: _travelTime * 720), new Vector());

                // DEBUG: Show movement visually
                Owner.PrintToChat($"DEBUG Knife Pos: {_position.X:F0}, {_position.Y:F0}, {_position.Z:F0}");

                // Optional: particle trail
                Warcraft.SpawnParticle(_position, "particles/tracer/tracer_flyby.vpcf", 0.2f);

                // Stop effect if we hit max range
                float traveled = (_position - Owner.PlayerPawn.Value.AbsOrigin).Length();
                if (traveled >= _maxDistance)
                {
                    Owner.PrintToChat("DEBUG Knife reached max distance");
                    this.Destroy();
                }
            }

            public override void OnFinish()
            {
                _visualKnife?.RemoveIfValid();
                Owner.PrintToChat($"{ChatColors.Red}DEBUG{ChatColors.Default} ThrowingKnife effect removed.");
            }
        }




        private void PlayerDeath(EventPlayerDeath death)
        {
            if (_hasUsedRevive || WarcraftPlayer.GetAbilityLevel(1) <= 0)
                return;

            var killer = death.Attacker;
            if (killer == null || !killer.IsValid || killer.TeamNum == Player.TeamNum) return;

            var roll = Random.Shared.Next(2); // 0 or 1
            var level = WarcraftPlayer.GetAbilityLevel(1);
            _hasUsedRevive = true;

            if (roll == 0)
            {
                // Revenge: damage killer
                int damage = 20 + (level * 6);
                SkillFunctions.DealRawDamage(Player, killer, damage);
                Player.PrintToChat($"{ChatColors.Red}☠️ Mercy Denied{ChatColors.Default}: You damaged your killer for {damage} HP!");
            }
            else
            {
                // Mercy: heal killer, revive self
                killer.SetHp(killer.PlayerPawn.Value.Health + 70);
                var teammates = Utilities.GetPlayers().Where(p => p.TeamNum == Player.TeamNum && p != Player && p.IsAlive()).ToList();
                var revivePosition = teammates.Count > 0
                    ? teammates[Random.Shared.Next(teammates.Count)].PlayerPawn.Value.AbsOrigin
                    : Player.PlayerPawn.Value.AbsOrigin;

                WarcraftPlugin.Instance.AddTimer(2.0f, () =>
                {
                    Player.Respawn();
                    Player.SetHp(100);
                    Player.PlayerPawn.Value.Teleport(revivePosition, null, new Vector());
                    Player.PrintToChat($"{ChatColors.Blue}🔄 Mercy Given{ChatColors.Default}: You were revived by fate.");
                });
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

                new BleedEffect(attacker, victim, totalTicks, damagePerTick).Start();
                attacker.PrintToChat($"{ChatColors.Red}🩸 Sharp End{ChatColors.Default}: You inflicted bleeding!");
            }

            // TODO: Fan of Knives - throwing knife logic (RMB detection or alternate trigger)
        }

        private void Ultimate()
        {
            if (!Player.IsAlive()) return;

            int affected = 0;
            float radius = 800f;
            float slowAmount = 0.7f;
            float selfSpeedBoost = 0.1f;

            foreach (var enemy in Utilities.GetPlayers())
            {
                if (!enemy.IsAlive() || enemy.TeamNum == Player.TeamNum) continue;

                var dist = (enemy.PlayerPawn.Value.AbsOrigin - Player.PlayerPawn.Value.AbsOrigin).Length();
                if (dist > radius) continue;

                enemy.Blind(5f, Color.Black);
                SkillFunctions.MovementSpeed(enemy, slowAmount, 5f); // Slow
                affected++;
            }

            if (affected > 0)
            {
                float boost = Math.Min(affected * selfSpeedBoost, 1.5f);
                SkillFunctions.MovementSpeed(Player, 1 + boost, 5f);
                Player.PrintToChat($"{ChatColors.Green}🌑 Eternal Darkness{ChatColors.Default}: Drained {affected} enemies. Gained +{(boost * 100):F0}% speed!");
                StartCooldown(3);
            }
            else
            {
                Player.PrintToChat($"{ChatColors.LightRed}No enemies found for Eternal Darkness.");
            }
        }

        private class BleedEffect : WarcraftEffect
        {
            private readonly CCSPlayerController _target;
            private readonly int _ticks;
            private readonly int _damage;
            private int _currentTick;

            public BleedEffect(CCSPlayerController owner, CCSPlayerController target, int ticks, int damage)
                : base(owner, ticks * 0.5f)
            {
                _target = target;
                _ticks = ticks;
                _damage = damage;
            }
            public override void OnStart()
            {
                Owner.PrintToChat($"{ChatColors.Red}DEBUG{ChatColors.Default} BLEED EFFECT called");
                // needs to be here
            }

            public override void OnTick()
            {
                if (_currentTick >= _ticks || !_target.IsAlive()) return;

                SkillFunctions.DealRawDamage(Owner, _target, _damage);
                _currentTick++;
                Warcraft.SpawnParticle(_target.PlayerPawn.Value.AbsOrigin.With(z: 70), "particles/blood_impact/blood_impact_blade.vpcf", 0.3f);
            }

            public override void OnFinish() { }
        }
    }
}
