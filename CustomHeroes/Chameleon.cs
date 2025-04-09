using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;

namespace WarcraftPlugin.Classes
{
    public class Chameleon : WarcraftClass
    {
        public override string DisplayName => "Chameleon";
        public override Color DefaultColor => Color.White;
        private ChameleonCloakEffect? cloakEffect;


        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Adapt to Environment", "Gain a randomized mix of buffs on spawn."),
            new WarcraftAbility("Improvise", "Gain a randomized offensive effect when hitting a player."),
            new WarcraftAbility("Cloak", "After standing still for 1.5 seconds you gain partial invisibility untill you move or shoot."),
            new WarcraftCooldownAbility("Tongue lash", "Lash your tongue at an enemy foe, damaging him and pulling him closer.", 2f, false)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerDeath>(PlayerDeath);
            HookEvent<EventRoundEnd>(RoundEnd);
            HookEvent<EventWeaponFire>(OnWeaponFire);

            HookAbility(3, Ultimate);
        }

        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // Delay to ensure pawn is ready
            WarcraftPlugin.Instance.AddTimer(0.1f, () =>
            {
                if (!Player.IsValid || Player.PlayerPawn?.Value == null || !Player.IsAlive())
                    return;

                var random = new Random();
                int AbilityLevel = WarcraftPlayer.GetAbilityLevel(0);

                cloakEffect = new ChameleonCloakEffect(Player, WarcraftPlayer.GetAbilityLevel(2));
                cloakEffect.Start();

                // List of possible effects
                var effects = new List<Action<CCSPlayerController>>
{
                // 🎲 Effect 1: Bonus Health
                p =>
                {
                    Console.WriteLine("🎲 Effect 1 triggered: Bonus health");
                    SkillFunctions.SetBonusHealth(p, 15 * AbilityLevel);
                },

                // 🎲 Effect 2: Bonus Movement Speed
                p =>
                {
                    Console.WriteLine("🎲 Effect 2 triggered: Bonus movement speed");
                    SkillFunctions.SetMovementSpeed(p, 0.1f * AbilityLevel, 999f);
                },

                // 🎲 Effect 3: Team Health Buff
                p =>
                {
                    Console.WriteLine("🎲 Effect 3 triggered: Bonus health for the entire team");
                    foreach (var teammate in Utilities.GetPlayers().Where(t => t.TeamNum == p.TeamNum && t.IsAlive()))
                    {
                        SkillFunctions.SetBonusHealth(teammate, 10 + 5 * AbilityLevel);
                        teammate.PrintToChat($"{ChatColors.Green}⛑️ Chameleon granted you bonus health!");
                    }
                },

                // 🎲 Effect 4: Ult Immunity for self + 2 allies
                p =>
                {
                    Console.WriteLine("🎲 Effect 4 triggered: Ultimate immunity for self and 2 allies");

                    var self = WarcraftPlugin.Instance.GetWcPlayer(p);
                    if (self != null)
                        self.HasUltimateImmunity = true;

                    var allies = Utilities.GetPlayers().Where(t => t.TeamNum == p.TeamNum && t != p && t.IsAlive()).OrderBy(_ => Guid.NewGuid()).Take(2);
                    foreach (var ally in allies)
                    {
                        var wcAlly = WarcraftPlugin.Instance.GetWcPlayer(ally);
                        if (wcAlly != null)
                        {
                            wcAlly.HasUltimateImmunity = true;
                            ally.PrintToChat($"{ChatColors.Lime}🛡️ You received temporary ultimate immunity from a Chameleon!");
                        }
                    }
                },

               // 🎲 Effect 5: Reduced gravity + long jump
                p =>
                {
                    Console.WriteLine("🎲 Effect 5 triggered: Reduced gravity and longjump");
                    SkillFunctions.SetGravity(p, 75f, 999f); // 3 seconds duration
                    SkillFunctions.ApplyForwardBoost(p, 100f); // gentle boost
                },


                // 🎲 Effect 6: Juggernaut Mode
                p =>
                {
                    Console.WriteLine("🎲 Effect 6 triggered: Juggernaut mode");
                    SkillFunctions.SetBonusHealth(p, 75);
                    SkillFunctions.SetMovementSpeed(p, -0.15f, 999f);                },

                // 🎲 Effect 7: Random weapon loadout
                p =>
                {
                    Console.WriteLine("🎲 Effect 7 triggered: Random rifle + nades");
                    string[] rifles = { "weapon_ak47", "weapon_m4a1", "weapon_famas", "weapon_galilar" };
                    string selected = rifles[Random.Shared.Next(rifles.Length)];
                    p.GiveNamedItem(selected);
                    p.GiveNamedItem("weapon_hegrenade");
                    p.GiveNamedItem("weapon_smokegrenade");
                    p.PrintToChat($"{ChatColors.Gold}🎁 You received a {selected.Replace("weapon_", "").ToUpper()} and grenades!");
                },

                // 🎲 Effect 8: Defensive cloak upgrade
                p =>
                {
                    Console.WriteLine("🎲 Effect 8 triggered: Upgraded cloak");
                    var wc = WarcraftPlugin.Instance.GetWcPlayer(p);
                    if (wc != null) wc.ChameleonDefensive = true;
                },

                // 🎲 Effect 9: Offensive boost
                p =>
                {
                    Console.WriteLine("🎲 Effect 9 triggered: Offensive skills double in efficiency");
                    var wc = WarcraftPlugin.Instance.GetWcPlayer(p);
                    if (wc != null) wc.ChameleonOffensive = true;
                },

                // 🎲 Effect 10: Reflect Damage (temporary)
                p =>
                {
                    Console.WriteLine("🎲 Effect 10 triggered: 25% chance to reflect incoming damage");
                    var wc = WarcraftPlugin.Instance.GetWcPlayer(p);
                    if (wc != null) wc.HasDamageReflection = true;
                    p.PrintToChat($"{ChatColors.LightBlue}⚔️ You are temporarily reflecting 25% of damage!");
                }
            };


                // Shuffle for unique selections
                var shuffled = effects.OrderBy(_ => random.Next()).ToList();

                // Always trigger one
                shuffled[0](Player);

                // 15% chance for second effect
                if (random.Next(100) < 25)
                {
                    shuffled[1](Player);
                    Player.PrintToChat($" {ChatColors.Gold}✨ Lucky! You received a second random effect!");
                }
            });
        }

        private void PlayerDeath(EventPlayerDeath death)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(Player);
            if (wcPlayer != null)
            {
                wcPlayer.ChameleonOffensive = false;
                wcPlayer.ChameleonDefensive = false;
                cloakEffect?.Destroy();
                cloakEffect = null;

            }

        }
        private void RoundEnd(EventRoundEnd @event)
        {
            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(Player);
            if (wcPlayer != null)
            {
                wcPlayer.ChameleonOffensive = false;
                wcPlayer.ChameleonDefensive = false;
                cloakEffect?.Destroy();
                cloakEffect = null;

            }

        }

        private void OnWeaponFire(EventWeaponFire @event)
        {
            if (cloakEffect != null)
            {
                cloakEffect.Destroy();
                cloakEffect = null;

                // Restart after 5 seconds (or however long you want)
                WarcraftPlugin.Instance.AddTimer(5f, () =>
                {
                    if (Player.IsValid && Player.IsAlive())
                    {
                        cloakEffect = new ChameleonCloakEffect(Player, WarcraftPlayer.GetAbilityLevel(2));
                        cloakEffect.Start();
                        Console.WriteLine($"[Cloak] {Player.PlayerName}'s cloak restarted after delay.");
                    }
                });
            }
        }


        private readonly Dictionary<ulong, float> lastEffectTime = new();

        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || !attacker.IsValid || !victim.IsValid || attacker == victim)
                return;

            int level = WarcraftPlayer.GetAbilityLevel(1);
            if (level <= 0) return;

            var now = Server.CurrentTime;
            var steamId = attacker.SteamID;
            if (lastEffectTime.TryGetValue(steamId, out var lastTime) && now - lastTime < 1.0f)
                return;

            lastEffectTime[steamId] = now;

            var rand = new Random();
            int effect = rand.Next(6); // 0–5

            Vector start = Warcraft.EyePosition(attacker);
            Vector end = Warcraft.EyePosition(victim);
            Color laserColor = Color.White;

            switch (effect)
            {
                case 0: // Slow
                    SkillFunctions.SlowTarget(attacker, victim, 100, 0.5f);
                    attacker.PrintToChat($" {ChatColors.Green}🎯 You slowed your enemy!");
                    laserColor = Color.Blue;
                    break;

                case 1: // Lifesteal
                    float damageDealt = @event.DmgHealth;
                    SkillFunctions.LeechHealth(attacker, victim, 100, damageDealt, level); // level used as multiplier or dummy param
                    attacker.PrintToChat($" {ChatColors.Green}🧛 You leeched {damageDealt * 0.5f:0} HP!");
                    laserColor = Color.HotPink;
                    break;

                case 2: // Remove Invisibility
                    SkillFunctions.SetInvisibility(victim, 0f, 255);
                    attacker.PrintToChat($" {ChatColors.Orange}👀 You revealed your enemy!");
                    laserColor = Color.Orange;
                    break;

                case 3: // Freeze
                    SkillFunctions.SetMovementSpeed(victim, -1.0f, 0.5f); // freeze
                    attacker.PrintToChat($" {ChatColors.Blue}❄️ You froze your enemy!");
                    laserColor = Color.Cyan;
                    break;

                case 4: // Float
                    SkillFunctions.SetGravity(victim, 0.0f, 0.5f);
                    Vector upward = new(0, 0, 200f);
                    victim.PlayerPawn.Value.Teleport(null, null, upward);
                    attacker.PrintToChat($" {ChatColors.LightBlue}🌪️ You made your enemy float!");
                    laserColor = Color.LightBlue;
                    break;

                case 5: // Restrict weapons
                    var allowedWeapons = new List<string>
                        {
                            "weapon_knife",
                            "weapon_c4",
                            "weapon_glock",
                            "weapon_hkp2000",
                            "weapon_usp_silencer",
                            "weapon_p250",
                            "weapon_tec9",
                            "weapon_fiveseven",
                            "weapon_cz75a",
                            "weapon_deagle",
                            "weapon_revolver",
                            "weapon_elite"
                        };

                    SkillFunctions.RestrictWeapons(victim, allowedWeapons, 0.5f);
                    attacker.PrintToChat($" {ChatColors.Red}💥 You restricted your enemy's weapons!");
                    laserColor = Color.Red;
                    break;
            }

            // 🎯 Draw effect laser
            Warcraft.DrawLaserBetween(start, end, laserColor, duration: 0.3f, width: 1.5f);
        }

        [GameEventHandler]
        public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            if (@event.Userid != Player) return HookResult.Continue;

            cloakEffect?.BreakCloakFromWeaponFire();
            return HookResult.Continue;
        }



        public class ChameleonCloakEffect : WarcraftEffect
        {
            private Vector _previousPosition;
            private Vector _currentPosition;
            private CounterStrikeSharp.API.Modules.Timers.Timer? _positionComparisonTimer;
            private bool _isCloaked;
            private readonly int _abilityLevel;

            public ChameleonCloakEffect(CCSPlayerController owner, int abilityLevel)
                : base(owner, duration: float.MaxValue, destroyOnDeath: true, destroyOnRoundEnd: true)
            {
                _abilityLevel = abilityLevel;
            }

            public override void OnStart()
            {
                Console.WriteLine("[ChameleonCloak] Cloak effect started.");

                _previousPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();
                _currentPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();

                _positionComparisonTimer = WarcraftPlugin.Instance.AddTimer(1.0f, () =>
                {
                    _previousPosition = _currentPosition.Clone();
                    _currentPosition = Owner.PlayerPawn.Value.AbsOrigin.Clone();

                    if (_previousPosition.X == _currentPosition.X &&
                        _previousPosition.Y == _currentPosition.Y &&
                        _previousPosition.Z == _currentPosition.Z)
                    {
                        if (!_isCloaked)
                        {
                            EnableCloak();
                            _isCloaked = true;
                            Owner.PlayLocalSound("sounds/physics/fruit/fruit_impact_02.vsnd");
                        }
                    }
                    else
                    {
                        if (_isCloaked)
                        {
                            DisableCloak();
                            _isCloaked = false;
                            Console.WriteLine("[ChameleonCloak] Cloak disabled due to movement.");
                        }
                    }
                }, TimerFlags.REPEAT);
            }

            public override void OnFinish()
            {
                Console.WriteLine("[ChameleonCloak] Cloak effect finished.");
                _positionComparisonTimer?.Kill();
                if (_isCloaked)
                {
                    DisableCloak();
                    _isCloaked = false;
                }
            }

            public void BreakCloakFromWeaponFire()
            {
                if (_isCloaked)
                {
                    DisableCloak();
                    _isCloaked = false;
                    Console.WriteLine("[ChameleonCloak] Cloak disabled due to weapon fire.");
                }
            }

            private void EnableCloak()
            {
                var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(Owner);
                bool upgraded = wcPlayer?.ChameleonDefensive == true;

                int alpha = upgraded ? 100 : 175;
                Owner.PlayerPawn.Value.SetColor(Color.FromArgb(alpha, 255, 255, 255));
                Console.WriteLine($"[ChameleonCloak] Cloak enabled (alpha={alpha}).");
            }

            private void DisableCloak()
            {
                Owner.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255));
                Console.WriteLine("[ChameleonCloak] Cloak reset to full visibility.");
            }

            public override void OnTick() { }
        }


        public static Vector Normalize(Vector v)
        {
            float length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            return length > 0 ? new Vector(v.X / length, v.Y / length, v.Z / length) : new Vector();
        }


        private void Ultimate()
        {
            if (!Player.IsValid || !Player.IsAlive())
                return;

            var eyePos = Warcraft.EyePosition(Player);
            var viewDirection = Player.PlayerPawn.Value.EyeAngles.ToForward();
            Vector targetPoint = eyePos + viewDirection * 800f;
            Vector hitPosition = RayTracer.Trace(eyePos, targetPoint, true);

            CCSPlayerController? targetPlayer = null;
            float closestDistance = 1200f;

            foreach (var other in Utilities.GetPlayers())
            {
                if (!other.IsValid || !other.IsAlive() || other.TeamNum == Player.TeamNum || other == Player)
                    continue;

                var distance = (other.PlayerPawn.Value.AbsOrigin - hitPosition).Length();
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetPlayer = other;
                }
            }

            if (targetPlayer == null)
            {
                Player.PrintToChat($"{ChatColors.Red}❌ No enemy found where you lashed!");
                return;
            }

            // Apply the pull effect 7 times with delay
            for (int i = 0; i < 7; i++)
            {
                float delay = 0.3f * i;
                var targetCopy = targetPlayer; // Capture loop variable correctly
                WarcraftPlugin.Instance.AddTimer(delay, () => PullTarget(targetCopy));
            }


            // Optional one-time effects
            SkillFunctions.DealRawDamage(Player, targetPlayer, 25);
            Player.EmitSound("knife_hit3.vsnd");
            targetPlayer.EmitSound("knife_hit1.vsnd");
            Player.PrintToChat($" {ChatColors.Green}👅 You lashed {targetPlayer.PlayerName}!");

            StartCooldown(3);
        }


        private void PullTarget(CCSPlayerController targetPlayer)
        {
            if (!Player.IsValid || !Player.IsAlive() || !targetPlayer.IsValid || !targetPlayer.IsAlive())
                return;

            Vector pullDirection = Player.PlayerPawn.Value.AbsOrigin - targetPlayer.PlayerPawn.Value.AbsOrigin;
            Vector pullForce = Chameleon.Normalize(pullDirection) * 750f;
            pullForce.Z += 80f;

            targetPlayer.PlayerPawn.Value.Teleport(null, null, pullForce);
            Warcraft.DrawLaserBetween(targetPlayer.PlayerPawn.Value.AbsOrigin, Player.PlayerPawn.Value.AbsOrigin, Color.Purple, 0.1f, 1.5f);
        }





    }

}
