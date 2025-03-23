using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Core.Effects;

namespace WarcraftPlugin.CustomSkills
{
    public class RestrictWeaponsEffect : WarcraftEffect
    {
        private readonly HashSet<string> _restrictedWeapons;
        private float _cooldownTimer;

        public RestrictWeaponsEffect(CCSPlayerController owner, float duration, IEnumerable<string> restrictedWeapons)
            : base(owner, duration, onTickInterval: 0.2f)
        {
            _restrictedWeapons = new HashSet<string>(restrictedWeapons);
        }

        public override void OnStart()
        {
            Console.WriteLine("[RestrictWeapons] Effect started.");
            Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;
        }

        public override void OnTick()
        {
            var pawn = Owner.PlayerPawn.Value;
            var weapon = pawn.WeaponServices?.ActiveWeapon?.Value;

            if (weapon == null)
                return;

            var weaponName = weapon.DesignerName;
            if (_restrictedWeapons.Contains(weaponName))
            {
                Owner.PrintToChat($"[RestrictWeapons] {weaponName} is restricted and will be dropped!");
                Owner.DropActiveWeapon();

                // Prevent pickup for a short moment to avoid immediately grabbing it again
                pawn.WeaponServices.PreventWeaponPickup = true;

                WarcraftPlugin.Instance.AddTimer(1.0f, () =>
                {
                    if (pawn.IsValid)
                    {
                        pawn.WeaponServices.PreventWeaponPickup = false;
                    }
                });
            }
        }

        public override void OnFinish()
        {
            Console.WriteLine("[RestrictWeapons] Effect ended. Resetting weapon pickup.");
            Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;
        }
    }
}
