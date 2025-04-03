using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
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
        public CHEGrenadeProjectile _ball;
        public CDynamicProp _ballProp;

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
            // Create grenade (physics)
            var grenade = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");
            if (!grenade.IsValid) return;

            var spawnPos = owner.CalculatePositionInFront(60, 75);
            var velocity = owner.CalculateVelocityAwayFromPlayer((int)2000f);

            grenade.SetModel("models/tools/bullet_hit_marker.vmdl");
            grenade.SetScale(0.001f); // tiny scale to "hide"
            grenade.Teleport(spawnPos, owner.PlayerPawn.Value.V_angle, velocity);
            grenade.DispatchSpawn();

            grenade.SetColor(Color.FromArgb(0, 45, 25, 25));

            // Create visible knife model
            var knifeProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (!knifeProp.IsValid) return;

            knifeProp.SetModel("models/tools/bullet_hit_marker.vmdl");
            knifeProp.SetScale(0.8f);
            knifeProp.Teleport(spawnPos, new QAngle(-90f, owner.PlayerPawn.Value.V_angle.Y + 180f, 90f), null);
            knifeProp.SetParent(grenade); // follow the grenade

            // Setup for proper collision
            grenade.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_INTERACTIVE_DEBRIS;
            Schema.SetSchemaValue(grenade.Handle, "CBaseEntity", "m_flElasticity", 0.0f);
            Schema.SetSchemaValue(grenade.Handle, "CBaseEntity", "m_flFriction", 1.0f);
            grenade.Collision.SolidFlags = 12;
            grenade.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

            Schema.SetSchemaValue(grenade.Handle, "CBaseGrenade", "m_hThrower", owner.PlayerPawn.Raw);

            // Start hit tracking
            var effect = new ThrowingKnifeHitSystem(owner, grenade, knifeProp);
            effect.Start();

            // Cleanup after 2 seconds
            WarcraftPlugin.Instance.AddTimer(2f, () =>
            {
                grenade.RemoveIfValid();
                knifeProp.RemoveIfValid();
            });
        }

        private class ThrowingKnifeHitSystem : WarcraftEffect
        {
            private readonly CHEGrenadeProjectile _grenade;
            private readonly CDynamicProp _visual;
            private readonly float _damage = 200f;
            private readonly float _radius = 45f;
            private bool _hasHit = false;

            public ThrowingKnifeHitSystem(CCSPlayerController owner, CHEGrenadeProjectile grenade, CDynamicProp visual)
                : base(owner, onTickInterval: 0.02f)
            {
                _grenade = grenade;
                _visual = visual;
            }

            public override void OnStart()
            {
                //
                //
            }

            public override void OnTick()
            {
                if (_hasHit || !_grenade.IsValid) return;

                foreach (var player in Utilities.GetPlayers())
                {
                    if (!player.IsAlive() || player.TeamNum == Owner.TeamNum || player == Owner)
                        continue;

                    float distance = (_grenade.AbsOrigin - player.PlayerPawn.Value.AbsOrigin).Length();
                    if (distance <= _radius)
                    {
                        player.TakeDamage(_damage, Owner);
                        Warcraft.SpawnParticle(player.AbsOrigin.With(z: 70), "particles/blood_impact/blood_impact_basic.vpcf");

                        _hasHit = true;
                        _grenade.RemoveIfValid();
                        _visual.RemoveIfValid();
                        break;
                    }
                }
            }

            public override void OnFinish()
            {
                _grenade?.RemoveIfValid();
                _visual?.RemoveIfValid();
            }
        }




        private void PlayerDeath(EventPlayerDeath death)
        {
            var attacker = death.Userid;

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

            // TODO: Fan of Knives 
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





/*
 * SEMI WORKING COPY
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 private void SpawnBall(CCSPlayerController owner)
        {
            // Step 1: Spawn the grenade
            var grenade = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");
            if (!grenade.IsValid) return;

            var speed = 2000;
            Vector velocity = Player.CalculateVelocityAwayFromPlayer(speed);
            Vector spawnPos = owner.CalculatePositionInFront(60, 75);

            // Move grenade off-screen to hide it visually, but let it do physics
            Vector grenadeSpawn = new Vector(-9999f, -9999f, -9999f);
            grenade.Teleport(grenadeSpawn, new QAngle(), new Vector());
            grenade.DispatchSpawn();

            // Step 2: Spawn the visible knife model
            var knifeModel = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (!knifeModel.IsValid) return;

            knifeModel.SetModel("models/tools/bullet_hit_marker.vmdl");
            knifeModel.SetScale(0.8f);

            // Flip model to face forward
            var angle = new QAngle(-90f, owner.PlayerPawn.Value.V_angle.Y + 180f, 90f);
            knifeModel.Teleport(spawnPos, angle, null);

            // Parent to grenade for motion
            knifeModel.SetParent(grenade);

            // Step 3: Apply velocity after spawn
            grenade.Teleport(null, null, velocity);
            // This affects the physics grenade (real hitbox and movement)
            grenade.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PROJECTILE;
            grenade.Collision.SolidFlags = 12;
            grenade.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

            // Optional: tells engine who threw it (used in killfeed/damage attribution)
            Schema.SetSchemaValue(grenade.Handle, "CBaseGrenade", "m_hThrower", owner.PlayerPawn.Raw);


            //Cleanup
            WarcraftPlugin.Instance.AddTimer(0.6f, () => { knifeModel?.RemoveIfValid(); });
        }


 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 


private void SpawnBall(CCSPlayerController owner)
        {
            var _ball = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");
            if (!_ball.IsValid) return;


            _ball.SetModel("models/tools/bullet_hit_marker.vmdl");


            var SpeedInSpawnBall = 2000;
            Vector velocity = Player.CalculateVelocityAwayFromPlayer(SpeedInSpawnBall);
            var angle = new QAngle(-90f, owner.PlayerPawn.Value.V_angle.Y, 0);



            var distance = 60;
            var height = 75;

            Vector posInfrontOfPlayer = owner.CalculatePositionInFront(distance, height);

            _ball.Teleport(posInfrontOfPlayer, angle, velocity);
            _ball.DispatchSpawn();
            _ball.SetModel("models/tools/bullet_hit_marker.vmdl");
            _ball.SetScale(0.8f);
        }




 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 *
 



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
        public CHEGrenadeProjectile _ball;
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
            var _ball = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");
            if (!_ball.IsValid) return;

            var SpeedInSpawnBall = 3500f;
            var distance = 60;
            var height = 75;

            Vector posInfrontOfPlayer = owner.CalculatePositionInFront(distance, height);

            _ball.Teleport(posInfrontOfPlayer, owner.PlayerPawn.Value.V_angle, new Vector(nint.Zero));
            _ball.DispatchSpawn();
            _ball.SetModel("models/tools/bullet_hit_marker.vmdl");
            _ball.SetScale(0.8f);

            

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
    private CHEGrenadeProjectile _knife;


    public ThrowingKnifeEffect(CCSPlayerController owner, CHEGrenadeProjectile _ball)
: base(owner)
    {
        _owner = owner;
        _knife = _ball;
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


    public override void OnStart()
    {
        UpdateLocation();
    }

    public override void OnTick()
    {
        // WIP
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

    // TODO: Fan of Knives 
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




 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */