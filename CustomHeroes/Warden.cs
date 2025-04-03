using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using g3;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;




namespace WarcraftPlugin.Classes
{
    public class Warden : WarcraftClass
    {
        private bool _hasUsedRevive = false;

        public override string DisplayName => "Warden";
        public override Color DefaultColor => Color.GreenYellow;
        private float _lastThrowTime = 0f;
        private const float ThrowCooldown = 1.5f; // seconds between throws
        public CPhysicsPropMultiplayer _ball;
        public CDynamicProp _ballProp;
        private ThrowingKnifeEffect throwingKnifeEffect;

        public override List<string> PreloadResources => new()
        {
            "models/props_gameplay/football.vmdl",
            "CustomModels/ThrowingKnife/knife.vmdl_c"
        };

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
            HookEvent<EventWeaponFire>(WeaponFire);

            HookAbility(3, Ultimate);

        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            _hasUsedRevive = false;

            // Restrict to knife only
            var allowedWeapons = new List<string> { "weapon_knife" };
            // SkillFunctions.RestrictWeapons(Player, allowedWeapons, 999f);
        }


        private void WeaponFire(EventWeaponFire @event)
        {
            var shooter = @event.Userid; // Uselmess comment

            if (shooter == null || !shooter.IsValid)
            {
                Console.WriteLine("[WCS] WeaponFire: shooter was null or invalid.");
                return;
            }

            Console.WriteLine($"[WCS] WeaponFire triggered by: {shooter.PlayerName} ({shooter.SteamID})");

            var weaponName = @event.Weapon?.ToLower() ?? "unknown";
            Console.WriteLine($"[WCS] Weapon used: {weaponName}");

            if (shooter != Player)
            {
                Console.WriteLine("[WCS] WeaponFire: Shooter is not our class's player, skipping.");
                return;
            }

            Console.WriteLine("[WCS] Launching ThrowingKnifeEffect...");
            SpawnBall(Player);
        }
        private void SpawnBall(CCSPlayerController owner)
        {
            var grenade = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");

            // Set custom model BEFORE dispatching spawn
            grenade.SetModel("models/tools/bullet_hit_marker.vmdl");
            grenade.SetScale(0.4f); // Optional: scale down for visual size

            // Positioning
            var distance = 60;
            var height = 75;
            var speed = 3000f;

            var spawnPos = owner.CalculatePositionInFront(distance, height);
            var direction = owner.CalculateVelocityAwayFromPlayer((int)speed);

            // Teleport and apply direction
            grenade.Teleport(spawnPos, owner.PlayerPawn.Value.EyeAngles, direction);
            grenade.DispatchSpawn();

            grenade.SetModel("models/tools/bullet_hit_marker.vmdl");


            // Visual tweaks
            grenade.SetColor(Color.FromArgb(255, 200, 50, 50)); // Slightly red
            grenade.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PROJECTILE;
            grenade.Collision.SolidFlags = 12;
            grenade.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

            // Set the owner for killfeed / damage logic
            Schema.SetSchemaValue(grenade.Handle, "CBaseGrenade", "m_hThrower", owner.PlayerPawn.Raw);

            // Start the knife logic
            throwingKnifeEffect = new ThrowingKnifeEffect(owner, grenade);
        }




        private class ThrowingKnifeEffect : WarcraftEffect
        {
            private CHEGrenadeProjectile _projectile;
            private readonly float _damage = 25f;
            private readonly float _checkInterval = 0.02f;
            private Box3d _hitbox;

            public ThrowingKnifeEffect(CCSPlayerController owner, CHEGrenadeProjectile projectile)
                : base(owner, onTickInterval: 0.02f)
            {
                _projectile = projectile;
            }

            public override void OnStart()
            {
                // Could spawn particles here if desired
            }

            public override void OnTick()
            {
                if (!_projectile.IsValid) return;

                // Create a hitbox around the projectile
                var center = _projectile.AbsOrigin;
                _hitbox = Warcraft.CreateBoxAroundPoint(center, 40, 40, 40); // Adjust size as needed

                foreach (var player in Utilities.GetPlayers())
                {
                    if (!player.IsValid || !player.PawnIsAlive || player.TeamNum == Owner.TeamNum || player == Owner)
                        continue;

                    var hitPoint = player.PlayerPawn.Value.AbsOrigin.Clone().Add(z: 40); // Mid-chest
                    if (_hitbox.Contains(hitPoint))
                    {
                        SkillFunctions.DealRawDamage(Owner, player, (int)_damage);
                        Owner.PrintToChat($"{ChatColors.Lime}🔪 You hit {player.PlayerName} with a throwing knife!");
                        _projectile.RemoveIfValid();
                        Destroy(); // End the effect
                        return;
                    }
                }
            }

            public override void OnFinish()
            {
                _projectile?.RemoveIfValid();
            }
        }


        private void PlayerDeath(EventPlayerDeath death)
        {
            var attacker = death.Attacker;

            if (_hasUsedRevive || WarcraftPlayer.GetAbilityLevel(1) <= 0)
                return;


            if (attacker == null || !attacker.IsValid || attacker.TeamNum == Player.TeamNum) return;

            var roll = Random.Shared.Next(2); // 0 or 1
            var level = WarcraftPlayer.GetAbilityLevel(1);
            _hasUsedRevive = true;

            if (roll == 0)
            {
                // Revenge: damage killer
                int damage = 20 + (level * 6);
                SkillFunctions.DealRawDamage(Player, attacker, damage);
                Player.PrintToChat($"{ChatColors.Red}☠️ Mercy Denied{ChatColors.Default}: You damaged your killer for {damage} HP!");
            }
            else
            {
                // Mercy: heal killer, revive self
                attacker.SetHp(attacker.PlayerPawn.Value.Health + 70);
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