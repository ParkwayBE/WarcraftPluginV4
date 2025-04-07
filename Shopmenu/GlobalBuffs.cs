using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.CustomSkills;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;


namespace WarcraftPlugin.Core
{
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

        }

        // 🧠 SECTION 1: Manual Global Buffs

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || player.PlayerPawn?.Value == null)
                    continue;

                player.PlayerPawn.Value.Health += 50;
            }

            return HookResult.Continue;
        }


        // 🧠 SECTION 2: Shop & Debuff Effects
        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker == null || victim == null || attacker == victim || !attacker.IsValid || !victim.IsValid)
                return HookResult.Continue;

            var wcAttacker = WarcraftPlugin.Instance.GetWcPlayer(attacker);
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






            // Add more shop-related flags here (lifesteal, orb of fire, etc.)

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

                if (directionVec.Z < 0.475f)
                    directionVec.Z = 0.475f;

                directionVec *= 620; // fixed forward push
                player.PlayerPawn.Value.AbsVelocity.X = directionVec.X;
                player.PlayerPawn.Value.AbsVelocity.Y = directionVec.Y;
                player.PlayerPawn.Value.AbsVelocity.Z = directionVec.Z;
            });

            // Apply reduced gravity for 5 seconds
            WarcraftPlugin.Instance.AddTimer(0.05f, () =>
            {
                new SetGravityEffect(player, 0.6f, 5f).Start();
            });

            return HookResult.Continue;
        }
    }
}
