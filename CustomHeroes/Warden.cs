using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
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
            "models/props_gameplay/football.vmdl"
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
            _ball = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            _ball.SetModel("models/props/de_aztec/hr_aztec/aztec_archaeology/aztec_archaeology_tools_shovel_01.vmdl");
            _ball.DispatchSpawn();

            _ballProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            _ballProp.SetModel("models/props/de_aztec/hr_aztec/aztec_archaeology/aztec_archaeology_tools_shovel_01.vmdl");
            _ballProp.DispatchSpawn();

            var distance = 60;
            var height = 75;
            var SpeedInSpawnBall = 3500f;

            Vector posInfrontOfPlayer = owner.CalculatePositionInFront(distance, height);

            _ballProp.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            _ball.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            _ballProp.SetParent(_ball);

            Vector velocity = owner.CalculateVelocityAwayFromPlayer((int)SpeedInSpawnBall);
            _ball.Teleport(null, null, velocity);


            throwingKnifeEffect = new ThrowingKnifeEffect(owner, _ball);
        }



        private class ThrowingKnifeEffect : WarcraftEffect
        {
            private CPhysicsPropMultiplayer? _prop;
            private CDynamicProp _ballprop;
            private Vector _direction;
            private float _speed = 2500f;
            private float _travelled = 0f;
            private float _maxDistance = 2500f;
            private float _tickInterval = 0.02f;
            private readonly float _damage = 25f;
            private Box3d _hitbox;
            private CCSPlayerController _owner;
            private CPhysicsPropMultiplayer _knife;

            public ThrowingKnifeEffect(CCSPlayerController owner, CPhysicsPropMultiplayer knife) : base(owner, onTickInterval: 0.01f)
            {
                _owner = owner;
                _knife = knife;
            }

            public void UpdateLocation()
            {
                var distance = 60;
                var height = 30;

                Vector velocity = _owner.CalculateVelocityAwayFromPlayer((int)_speed);


                Vector posInfrontOfPlayer = _owner.CalculatePositionInFront(distance, height);
                _owner.PrintToChat($"Updating Ball Position: {posInfrontOfPlayer}");
                _knife.Teleport(posInfrontOfPlayer, _owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
                _knife.Teleport(null, null, velocity);

            }

            Vector Normalize(Vector v)
            {
                var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
                if (length == 0) return new Vector(0, 0, 0);
                return new Vector(v.X / length, v.Y / length, v.Z / length);
            }


            public override void OnStart()
            {
                UpdateLocation();
            }

            public override void OnTick()
            {


                // Update hitbox each tick to match prop's current position
                /*
                var center = _prop.AbsOrigin;
                _hitbox = Warcraft.CreateBoxAroundPoint(center, 50, 50, 50);  // Width, depth, height

                // Check for collision
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!player.IsValid || !player.PawnIsAlive || player.TeamNum == Owner.TeamNum || player == Owner)
                        continue;

                    var targetPos = player.PlayerPawn.Value.AbsOrigin.Clone().Add(z: 40);  // Mid-chest height
                    if (_hitbox.Contains(targetPos))
                    {
                        SkillFunctions.DealRawDamage(Owner, player, (int)_damage);
                        Owner.PrintToChat($"{ChatColors.Lime}🔪 You hit {player.PlayerName} with a throwing knife!");
                        Destroy();
                        return;
                    }
                }
                */
            }

            public override void OnFinish()
            {
                Owner.PrintToChat("[WCS] OnFinish Throwing knife effect triggered...");
                _prop?.RemoveIfValid();
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
