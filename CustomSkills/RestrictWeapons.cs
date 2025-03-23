using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using WarcraftPlugin.Core.Effects;

namespace WarcraftPlugin.CustomSkills
{
    public class RestrictWeaponsEffect : WarcraftEffect
    {
        private readonly List<string> _allowedWeapons;

        public RestrictWeaponsEffect(CCSPlayerController owner, float duration, List<string> allowedWeapons)
            : base(owner, duration)
        {
            _allowedWeapons = allowedWeapons;
        }

        public override void OnStart()
        {
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                return;

            Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;

            // Remove weapons first
            RemoveAllWeapons();

            // Delay weapon re-granting
            WarcraftPlugin.Instance.AddTimer(0.2f, () =>
            {
                GiveAllowedWeapons();
                Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = true;
            });
        }

        public override void OnTick()
        {
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                return;

            var activeWeapon = Owner.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value;
            var currentWeapon = activeWeapon?.DesignerName;

            if (string.IsNullOrEmpty(currentWeapon) || _allowedWeapons.Contains(currentWeapon))
                return;

            // Illegal weapon detected
            Console.WriteLine($"[RestrictWeaponsEffect] Player has disallowed weapon: {currentWeapon}, resetting...");

            Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;

            RemoveAllWeapons();

            WarcraftPlugin.Instance.AddTimer(0.2f, () =>
            {
                GiveAllowedWeapons();
                Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = true;
            });
        }

        public override void OnFinish()
        {
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                return;

            Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;
        }

        private void RemoveAllWeapons()
        {
            var inventory = Owner.PlayerPawn.Value.WeaponServices.MyWeapons;
            foreach (var weaponHandle in inventory)
            {
                var entity = weaponHandle.Value;
                entity?.Remove();
            }
        }

        private void GiveAllowedWeapons()
        {
            foreach (var weapon in _allowedWeapons)
            {
                Owner.GiveNamedItem(weapon);
            }
        }
    }
}
