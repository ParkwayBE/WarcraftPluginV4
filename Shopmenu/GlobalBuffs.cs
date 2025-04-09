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
            _plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
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

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var victim = @event.Userid;
            if (!victim.IsValid || victim.PlayerPawn?.Value == null)
                return HookResult.Continue;

            var wcVictim = _plugin.GetWcPlayer(victim);
            if (wcVictim == null) return HookResult.Continue;

            // 🧠 Reset player-specific buffs
            wcVictim.HasOrbOfSlow = false;
            wcVictim.HasArmorPiercingRounds = false;
            wcVictim.HasMaskOfDeath = false;
            wcVictim.HasHelmOfExcellence = false;
            wcVictim.HasGlovesOfWarmth = false;
            wcVictim.HasLongjumpBoots = false;
            wcVictim.HasOrbOfReflection = false;
            wcVictim.HasDamageReflection = false;
            wcVictim.ChameleonOffensive = false;
            wcVictim.ChameleonDefensive = false;
            wcVictim.HasUltimateImmunity = false;
            wcVictim.RespawnQueued = false;

            // 🧠 Track death in stats
            WarcraftPlugin.Instance.GetDatabase().RegisterDeath(victim, wcVictim.className);

            // 🧠 Track kill (if valid attacker)
            var attacker = @event.Attacker;
            if (attacker != null && attacker.IsValid && attacker != victim)
            {
                var wcAttacker = _plugin.GetWcPlayer(attacker);
                if (wcAttacker != null)
                {
                    WarcraftPlugin.Instance.GetDatabase().RegisterKill(attacker, wcAttacker.className);
                }
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

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || player.PlayerPawn?.Value == null)
                    continue;

                var wcPlayer = _plugin.GetWcPlayer(player);
                if (wcPlayer == null) continue;

                // Safety reset: remove ultimate immunity
                wcPlayer.HasOrbOfSlow = false;
                wcPlayer.HasArmorPiercingRounds = false;
                wcPlayer.HasMaskOfDeath = false;
                wcPlayer.HasHelmOfExcellence = false;
                wcPlayer.HasGlovesOfWarmth = false;
                wcPlayer.HasLongjumpBoots = false;
                wcPlayer.HasOrbOfReflection = false;
                wcPlayer.HasDamageReflection = false;
                wcPlayer.ChameleonOffensive = false;
                wcPlayer.ChameleonDefensive = false;
                wcPlayer.HasUltimateImmunity = false;

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
                int DmgDealt = @event.DmgHealth;
                int DmgReduction = (int)(DmgDealt * 0.65f);
                SkillFunctions.SetBonusHealth(victim, DmgReduction);
                victim.PrintToCenter($" {ChatColors.Green}🛡️ Helm of Excellence absorbed some of the damage!");
                if (victim != null && victim.IsValid && victim.PlayerPawn != null && victim.PlayerPawn.IsValid)
                {
                    Server.NextFrame(() =>
                    {
                        if (victim.PlayerPawn != null && victim.PlayerPawn.IsValid)
                        {
                            Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                        }
                    });
                }

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
                        attacker.PrintToChat($" {ChatColors.Red}⚡ You were struck by reflected damage!");
                        victim.PrintToChat($" {ChatColors.Green}✔ Orb of Reflection struck your attacker for {reflected} damage!");
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

            WarcraftPlugin.Instance.AddTimer(0.05f, () =>
            {
                var directionAngle = player.PlayerPawn.Value.EyeAngles;
                var directionVec = new Vector();
                NativeAPI.AngleVectors(directionAngle.Handle, directionVec.Handle, nint.Zero, nint.Zero);

                if (directionVec.Z < 0.55f)
                    directionVec.Z = 0.55f;



                directionVec *= 520;
                player.PlayerPawn.Value.AbsVelocity.X = directionVec.X;
                player.PlayerPawn.Value.AbsVelocity.Y = directionVec.Y;
                player.PlayerPawn.Value.AbsVelocity.Z = directionVec.Z;
            });

            WarcraftPlugin.Instance.AddTimer(0.05f, () =>
            {
                new SetGravityEffect(player, 70f, 5f).Start();
            });

            return HookResult.Continue;
        }
    }
}
