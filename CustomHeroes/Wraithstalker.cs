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
            new WarcraftCooldownAbility("Marked for prey", "Scan the area where you are looking, highlight enemies close for x seconds and slow them down. Killing a marked target grants a skull. Skulls give you lasting benefits untill mapchange.", 60f)
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

            // Retrieve the player's current model name
            var modelName = pawn.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName;

            // Create relay and glow model entities
            var relayModel = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
            var glowModel = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");

            if (relayModel == null || glowModel == null) return;

            // Set up the relay model
            relayModel.SetModel(modelName);
            relayModel.Spawnflags = 256U;
            relayModel.RenderMode = RenderMode_t.kRenderNone;

            // Set up the glow model
            glowModel.SetModel(modelName);
            glowModel.Spawnflags = 256U;
            glowModel.Glow.GlowColorOverride = color;
            glowModel.Glow.GlowRange = 5000;
            glowModel.Glow.GlowType = 3;
            glowModel.RenderMode = RenderMode_t.kRenderGlow;

            // Spawn the entities
            relayModel.DispatchSpawn();
            glowModel.DispatchSpawn();

            // Attach the models
            relayModel.AcceptInput("FollowEntity", pawn, relayModel, "!activator");
            glowModel.AcceptInput("FollowEntity", relayModel, glowModel, "!activator");

            // Set a timer to clean up the glow effect
            if (duration > 0)
            {
                WarcraftPlugin.Instance.AddTimer(duration, () =>
                {
                    // Remove the glow and relay models after the duration
                    glowModel?.RemoveIfValid();
                    relayModel?.RemoveIfValid();
                });
            }
        }



        private void Ultimate()
        {
           StartGlow(Player, Color.Blue, 20);
           StartCooldown(3);
        }

        private void PlayerShoot(EventWeaponFire @event)
        {
           

        }


    }
}









