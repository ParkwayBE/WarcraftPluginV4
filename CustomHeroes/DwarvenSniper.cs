using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.CustomSkills;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;



namespace WarcraftPlugin.Classes
{
    public class DwarvenSniper : WarcraftClass
    {
        public override string DisplayName => "Dwarven Sniper";
        public override Color DefaultColor => Color.GreenYellow;
        private readonly Dictionary<CCSPlayerController, GrenadeSupplyEffect> activeEffects = new();
        private float evasionMultiplier = 1.0f;
        private bool impaleOnSight = false;
        private bool impaleTriggered = false;
        private bool hasUsedUltimate = false;




        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("Eagle eye", "Increased damage with scoped weapons."),
            new WarcraftAbility("Dwarven Genes", "Evasion and increased health"),
            new WarcraftAbility("Supplies", "Occasionally grants a grenade and chance to spawn with a Scout or AWP"),
            new WarcraftCooldownAbility("Ring of power","For the next 5 seconds you double your evasion and the first player to look at you gets impaled.", 30f, false)
        ];

        public override void Register()
        {
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventPlayerHurtOther>(PlayerHurtOther);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            HookEvent<EventGrenadeThrown>(GrenadeThrown);
            HookAbility(3, Ultimate);
        }


        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                int abilityLevel2 = WarcraftPlayer.GetAbilityLevel(2);

                int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
                if (abilityLevel <= 0)
                    return;

                var increaseHealth = abilityLevel * 15;
                SkillFunctions.SetBonusHealth(Player, increaseHealth);

                int awpChance = abilityLevel * 10;
                int roll = Random.Shared.Next(100);

                string weaponToGive = (roll < awpChance) ? "weapon_awp" : "weapon_ssg08";
                Console.WriteLine($" {ChatColors.Green}Dwarven Supplies{ChatColors.Default} Rolled {roll} vs {awpChance} → Giving {weaponToGive}");

                var pawn = Player.PlayerPawn.Value;
                var activeWeaponName = pawn.WeaponServices?.ActiveWeapon?.Value?.DesignerName;

                if (abilityLevel2 <= 0)
                    return;

                if (activeWeaponName != "weapon_ssg08" && activeWeaponName != "weapon_awp")
                {
                    Player.GiveNamedItem(weaponToGive);
                }

                if (activeEffects.TryGetValue(Player, out var existingEffect))
                {
                    existingEffect.Destroy();
                    activeEffects.Remove(Player);
                }

                ResetCooldowns();

                Player.GiveNamedItem("weapon_hegrenade");
                var effect = new GrenadeSupplyEffect(Player);
                activeEffects[Player] = effect;
                effect.Start();
            });
        }

        internal void GrenadeThrown(EventGrenadeThrown @event)
        {
            if (activeEffects.TryGetValue(Player, out var effect))
            {
                effect.GiveGrenadeIfNeeded();
            }
        }


        internal class GrenadeSupplyEffect(CCSPlayerController owner) : WarcraftEffect(owner)
        {
            private int grenadesGiven = 0;
            private int maxGrenades = 0;
            private WarcraftPlayer WarcraftPlayer;

            public override void OnStart()
            {

                if (Owner == null || Owner.PlayerPawn?.Value == null)
                {
                    Console.WriteLine("[ERROR] GrenadeSupplyEffect started but Owner is NULL! Aborting.");
                    return;
                }

                WarcraftPlayer = Owner.GetWarcraftPlayer();
                if (WarcraftPlayer == null)
                {
                    return;
                }

                maxGrenades = WarcraftPlayer.GetAbilityLevel(2);

                if (maxGrenades < 1)
                {
                    return;
                }

                RemoveGrenades("weapon_hegrenade");
                Owner.GiveNamedItem("weapon_hegrenade");
                maxGrenades = 4;
            }

            public void GiveGrenadeIfNeeded()
            {
                if (grenadesGiven >= maxGrenades)
                {
                    return;
                }
                Owner.GiveNamedItem("weapon_hegrenade");
                grenadesGiven++;
            }


            public override void OnFinish()
            { /* */ }

            private void RemoveGrenades(string grenadeName)
            {
                var grenades = Owner.PlayerPawn.Value.WeaponServices.MyWeapons;

                foreach (var grenade in grenades)
                {
                    if (grenade.Value.DesignerName == grenadeName)
                    {
                        Owner.DropWeaponByDesignerName(grenadeName);
                    }
                }
            }

            public override void OnTick() { }

        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            if (Player == null || !Player.IsValid || !Player.IsAlive())
                return;

            HandleEvasion(@event);

            if (impaleOnSight && @event.Attacker != null && @event.Attacker.IsValid)
            {
                var attacker = @event.Attacker;

                if (attacker != Player && attacker.TeamNum != Player.TeamNum && !impaleTriggeredPlayers.Contains(attacker))
                {
                    // Apply impale effect
                    attacker.PlayerPawn.Value.Teleport(null, null, new Vector(0, 0, 500));
                    Warcraft.SpawnParticle(attacker.PlayerPawn.Value.AbsOrigin.With(z: attacker.PlayerPawn.Value.AbsOrigin.Z + 60), "particles/ui/status_levels/ui_status_level__gen_glow.vpcf");
                    attacker.PrintToChat($" {ChatColors.Red}Ring Of Power{ChatColors.Default}: Dwarven Sniper {ChatColors.LightPurple}impaled{ChatColors.Default} you!");

                    impaleTriggeredPlayers.Add(attacker);
                }
            }
        }


        private void HandleEvasion(EventPlayerHurt @event)
        {
            if (Player == null) return;

            int abilityLevel = WarcraftPlayer.GetAbilityLevel(1);
            if (abilityLevel == 0) return;

            int baseChance = abilityLevel * 7;
            int evasionChance = (int)(baseChance * evasionMultiplier);

            var roll = Random.Shared.Next(100);
            if (roll < evasionChance)
            {
                @event.IgnoreDamage();
                Player.PrintToChat($" {ChatColors.Default}Dwarven Genes{ChatColors.Default} : You evaded a hit.");
            }
        }

        private void Ultimate()
        {
            if (Player == null) return;
            evasionMultiplier = 2.0f;
            hasUsedUltimate = true;
            Player.PrintToCenter($" {ChatColors.Green}Ring Of Power{ChatColors.Default}: Your{ChatColors.LightPurple} evasion{ChatColors.Default} has been doubled for 7 seconds!");

            WarcraftPlugin.Instance.AddTimer(7.0f, () =>
            {
                if (Player == null) return;
                evasionMultiplier = 1.0f;
                Player.PrintToCenter($" {ChatColors.Green}Ring Of Power{ChatColors.Default}: Your {ChatColors.LightPurple} evasion{ChatColors.Default} boost has ended.");
            });

            ActivateImpaleUltimate();

            StartCooldown(3);
        }

        private HashSet<CCSPlayerController> impaleTriggeredPlayers = new();

        private void ActivateImpaleUltimate()
        {
            impaleTriggeredPlayers.Clear();
            impaleOnSight = true;


            WarcraftPlugin.Instance.AddTimer(7.0f, () =>
            {
                impaleOnSight = false;
                impaleTriggeredPlayers.Clear();
                hasUsedUltimate = false;
            });
        }


        private void PlayerHurtOther(EventPlayerHurtOther @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (attacker.TeamNum == victim.TeamNum)
                return;

            if (@event.Weapon == "ssg08" || @event.Weapon == "awp")
            {
                var damageBonus = WarcraftPlayer.GetAbilityLevel(0) * 8;
                @event.AddBonusDamage(damageBonus);
                Warcraft.SpawnParticle(victim.PlayerPawn.Value.AbsOrigin.With(z: victim.PlayerPawn.Value.AbsOrigin.Z + 60), "particles/ui/hud/ui_transitions_tests_lin_a.vpcf");
            }
        }
    }
}