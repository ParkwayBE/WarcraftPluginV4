using System;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Helpers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;



namespace WarcraftPlugin.Core
{
    public enum HitGroup
    {
        Generic = 0,
        Head = 1,
        Chest = 2,
        Stomach = 3,
        LeftArm = 4,
        RightArm = 5,
        LeftLeg = 6,
        RightLeg = 7,
        Gear = 10
    }

    public class GlobalBuffs
    {
        private readonly WarcraftPlugin _plugin;

        public GlobalBuffs(WarcraftPlugin plugin)
        {
            _plugin = plugin;

            // Hook global events
            _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            _plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            _plugin.RegisterEventHandler<EventPlayerJump>(OnPlayerJump);
            _plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            _plugin.RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
            _plugin.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
            _plugin.RegisterEventHandler<EventWeaponFire>(OnWeaponFire);



        }

        // 🧠 SECTION 1: Manual Global Buffs

        private HookResult OnSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!player.IsValid || !player.IsAlive() || player.PlayerPawn?.Value == null)
                return HookResult.Continue;

            var wcPlayer = _plugin.GetWcPlayer(player);
            if (wcPlayer == null) return HookResult.Continue;

            if (wcPlayer.RespawnQueued)
            {
                wcPlayer.RespawnQueued = false;

                var location = wcPlayer.RespawnLocation;
                _plugin.AddTimer(0.2f, () =>
                {
                    if (player.IsValid && player.PlayerPawn?.Value != null)
                    {
                        player.PlayerPawn.Value.Teleport(location);
                        player.PrintToChat($" {ChatColors.Green}✔ You have been resurrected at your ally's location!");
                    }
                });
            }

            return HookResult.Continue;
        }


        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || player.PlayerPawn?.Value == null)
                    continue;

                player.PlayerPawn.Value.Health += 50;
            }

            _plugin.AddTimer(1.0f, RepeatRespawnCheck, TimerFlags.REPEAT);


            return HookResult.Continue;
        }
        private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!player.IsValid || player.PlayerPawn?.Value == null || !player.IsAlive())
                return HookResult.Continue;

            var wcPlayer = _plugin.GetWcPlayer(player);
            if (wcPlayer.HasGlovesOfWarmth)
            {
                Server.NextFrame(() =>
                {
                    _plugin.AddTimer(5f, () =>
                    {
                        if (!player.IsValid || player.PlayerPawn?.Value == null || !player.IsAlive())
                            return;

                        string[] grenades = {
                            "weapon_hegrenade",
                            "weapon_flashbang",
                            "weapon_incgrenade",
                            "weapon_decoy"
                        };

                        string randomGrenade = grenades[Random.Shared.Next(grenades.Length)];
                        player.GiveNamedItem(randomGrenade);

                        player.PrintToChat($" {ChatColors.Green}You received a random grenade: {randomGrenade.Replace("weapon_", "").ToUpper()}!");
                    });
                });
            }

            return HookResult.Continue;
        }


        private void RepeatRespawnCheck()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var wcPlayer = _plugin.GetWcPlayer(player);
                if (wcPlayer == null || !wcPlayer.RespawnQueued) continue;

                if (Server.CurrentTime >= wcPlayer.RespawnTriggerTime)
                {
                    wcPlayer.RespawnQueued = false;

                    if (!player.IsValid || player.IsAlive()) continue;

                    player.Respawn();

                    // Delay teleport safely
                    var targetLocation = wcPlayer.RespawnLocation;
                    _plugin.AddTimer(0.2f, () =>
                    {
                        if (player.IsValid && player.PlayerPawn?.Value != null)
                        {
                            player.PlayerPawn.Value.Teleport(targetLocation);
                            player.PrintToChat($"{ChatColors.Green}✔ You have been resurrected at your ally’s location!");
                        }
                    });
                }
            }
        }


        private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            var attacker = @event.Userid;
            if (!attacker.IsValid || attacker.PlayerPawn?.Value == null || !attacker.IsAlive())
                return HookResult.Continue;

            var wcAttacker = _plugin.GetWcPlayer(attacker);
            if (wcAttacker == null || !wcAttacker.HasArmorPiercingRounds)
                return HookResult.Continue;

            // Do a ray trace to get where the shot would land
            Vector start = Warcraft.EyePosition(attacker);
            Vector lookDirection = attacker.PlayerPawn.Value.EyeAngles.ToForward();
            Vector end = start + (lookDirection * 4096); // Long range shot

            var traceResult = RayTracer.Trace(start, end, true);

            if (traceResult != default)
            {
                end = traceResult;
            }

            end.Z += 20f;

            Warcraft.DrawLaserBetween(start, end, Color.White, 0.15f, 0.8f);

            return HookResult.Continue;
        }


        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (!p.IsValid || p.PlayerPawn?.Value == null) continue;

                var wcPlayer = _plugin.GetWcPlayer(p);
                if (wcPlayer == null) continue;

                // Put clearing functions underneath to clear certain effects from players at round end
            }

            return HookResult.Continue;
        }



        ///

        // SECTION 2: Shop & Debuff Effects
        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || attacker == victim || !attacker.IsValid || !victim.IsValid)
                return HookResult.Continue;

            var wcAttacker = WarcraftPlugin.Instance.GetWcPlayer(attacker);
            var wcVictim = WarcraftPlugin.Instance.GetWcPlayer(victim);

            if (wcAttacker == null) return HookResult.Continue;

            // Orb of Slow effect
            if (wcAttacker.HasOrbOfSlow)
            {
                SkillFunctions.SlowTarget(attacker, victim, 25, 3f); // 25% chance to slow for 3s
            }


            if (wcAttacker.HasArmorPiercingRounds)
            {
                SkillFunctions.DealRawDamage(attacker, victim, 5);
                attacker.PrintToCenter("You dealt 5 additional damage with each hit");
            }

            if (wcAttacker.HasMaskOfDeath && Random.Shared.Next(100) < 20)
            {
                if (wcVictim != null)
                {
                    wcVictim.HasUltimateImmunity = false;
                    victim.PlayerPawn.Value.SetColor(Color.FromArgb(255, 255, 255, 255));
                    victim.PrintToChat($" {ChatColors.Red}✖ Your invisibility and immunity were stripped!");
                }
            }

            if (wcVictim != null && wcVictim.HasHelmOfExcellence && @event.Hitgroup == (int)HitGroup.Head)
            {
                @event.DmgHealth = (int)(@event.DmgHealth * 0.65f);
                victim.PrintToCenter($" {ChatColors.Green}🛡️ Helm of Excellence absorbed some of the damage!");
            }

            if (wcVictim.HasOrbOfReflection && attacker.IsValid && attacker.IsAlive())
            {
                float now = Server.CurrentTime;
                if (now - wcVictim.LastReflectionTime > 1.0f)
                {
                    wcVictim.LastReflectionTime = now;

                    int reflected = (int)(@event.DmgHealth * 0.25f);
                    if (reflected > 0)
                    {
                        SkillFunctions.DealRawDamage(victim, attacker, reflected);
                        attacker.PrintToChat($"{ChatColors.Red}⚡ You were struck by reflected damage!");
                        victim.PrintToChat($"{ChatColors.Green}✔ Orb of Reflection struck your attacker for {reflected} damage!");
                    }
                }
            }



            /////////////////////////////////////////////////////////////////////////////////////////////////////
            return HookResult.Continue;



        }

        private HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player?.PlayerPawn?.Value == null || !player.IsValid) return HookResult.Continue;

            var wcPlayer = WarcraftPlugin.Instance.GetWcPlayer(player);
            if (wcPlayer == null || !wcPlayer.HasLongjumpBoots) return HookResult.Continue;

            // Apply forward force
            WarcraftPlugin.Instance.AddTimer(0.05f, () =>
            {
                var directionAngle = player.PlayerPawn.Value.EyeAngles;
                var directionVec = new Vector();
                NativeAPI.AngleVectors(directionAngle.Handle, directionVec.Handle, nint.Zero, nint.Zero);

                if (directionVec.Z < 0.55f)
                    directionVec.Z = 0.55f;



                directionVec *= 520; // fixed forward push
                player.PlayerPawn.Value.AbsVelocity.X = directionVec.X;
                player.PlayerPawn.Value.AbsVelocity.Y = directionVec.Y;
                player.PlayerPawn.Value.AbsVelocity.Z = directionVec.Z;
            });

            // Apply reduced gravity for 5 seconds
            WarcraftPlugin.Instance.AddTimer(0.05f, () =>
            {
                new SetGravityEffect(player, 70f, 5f).Start();
            });

            return HookResult.Continue;
        }
    }
}
