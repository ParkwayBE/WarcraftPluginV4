using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using WarcraftPlugin.Models;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CounterStrikeSharp.API.Modules.Memory;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using System.Drawing;
using WarcraftPlugin.Core.Effects;
using System.Collections.Generic;
using WarcraftPlugin.Events.ExtendedEvents;
using System;
using System.Reflection;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using WarcraftPlugin.Core;
using WarcraftPlugin.Summons;


namespace WarcraftPlugin.Classes
{
    public class Wraithstalker : WarcraftClass
    {
        public override string DisplayName => "Wraithstalker";

        private readonly WarcraftPlugin _plugin;
        public override Color DefaultColor => Color.CadetBlue;

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Assimilation", "On Kill: Movement speed and reduced gravity for 3/5/7/9/10 seconds, this also grants a blindstack max 2, which blinds an enemy when he spots you."),
            new WarcraftAbility("Phantom Cloak", "Standing still for  2.5 - 0.5 seconds makes you invisible and your next shot deals bonus damage."),
            new WarcraftAbility("Shadowstrike", "After you exited Phantom Cloak your next hit will cause bonus damage and grant you a guaranteed skull"),
            new WarcraftCooldownAbility("Marked for prey", "Scan the area where you are looking, highlight enemies close for x seconds and slow them down. Killing a marked target grants a skull. Skulls give you lasting benefits untill mapchange.", 5f)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);

            HookAbility(3, Ultimate);
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            
        }

        public void StartGlow(CCSPlayerController player, Color color, int duration)
        {
            var pawn = player.PlayerPawn.Value;

            // Create a new prop_dynamic entity for the glow
            var glowProp = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
            if (glowProp == null) return;

            // Assign a model and configure it
            glowProp.SetModel("characters\\models\\tm_leet\\tm_leet_variantb.vmdl");  // Example model path
            glowProp.Spawnflags = 256U;
            glowProp.RenderMode = RenderMode_t.kRenderGlow;
            glowProp.Glow.GlowColorOverride = color;
            glowProp.Glow.GlowRangeMin = 3;
            glowProp.Glow.GlowRange = 8000;
            glowProp.Glow.GlowType = 3;

            // Attach the prop to the player
            glowProp.Teleport(pawn.AbsOrigin, pawn.AbsRotation, new Vector(0, 0, 0));
            glowProp.AcceptInput("FollowEntity", caller: glowProp, activator: pawn, value: "!activator");

            // Spawn the prop
            glowProp.DispatchSpawn();

            // Set a timer to remove the glow prop after the specified duration
            if (duration > 0)
            {
                WarcraftPlugin.Instance.AddTimer(duration, () =>
                {
                    glowProp?.Remove();
                });
            }
        }



        private void Ultimate()
        {
           StartGlow(Player, Color.Blue, 5);
           StartCooldown(3);
        }

        private void PlayerShoot(EventWeaponFire @event)
        {
           

        }


    }
}









