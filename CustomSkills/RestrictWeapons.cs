using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using WarcraftPlugin.Core.Effects;
using System.Collections.Generic;

namespace WarcraftPlugin.CustomSkills
{
    public class RestrictWeaponsEffect : WarcraftEffect
    {
        private readonly List<string> _restrictedWeapons;

        public RestrictWeaponsEffect(CCSPlayerController owner, float duration, List<string> restrictedWeapons)
            : base(owner, duration)
        {
            _restrictedWeapons = restrictedWeapons;
        }

        public override void OnStart()
        {
            Owner.PrintToChat("[RestrictWeapons] Weapon pickup restrictions active.");
            Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = true;
        }

        public override void OnTick()
        {
            var activeWeapon = Owner.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value;
            if (activeWeapon == null)
                return;

            var weaponName = activeWeapon.DesignerName;

            if (_restrictedWeapons.Contains(weaponName))
            {
                Owner.DropActiveWeapon();
                Owner.PrintToChat($"[RestrictWeapons] You cannot use {weaponName}!");
            }
        }


        public override void OnFinish()
        {
            Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;
            Owner.PrintToChat("[RestrictWeapons] Weapon pickup restrictions expired.");
        }
    }
}
