using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
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
            Console.WriteLine("ATTEMPTING TO VERIFY USER IN ONSTART.");
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                return;

            Console.WriteLine("USER IS VALID.");
            WarcraftPlugin.Instance.AddTimer(0.3f, () =>
            {
                Console.WriteLine("Disabling weaponpickup prevention.");
                Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;

                Console.WriteLine("ATTEMPTING TO REMOVE WEAPONS.");
                RemoveAllWeapons();

                WarcraftPlugin.Instance.AddTimer(0.2f, () =>
                {
                    Console.WriteLine("ATTEMPTING TO GIVE ALLOWED WEAPONS.");
                    GiveAllowedWeapons();
                    Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = true;
                    Console.WriteLine("WEAPON PREVENTION ENABLED.");
                });
            });
        }

        public override void OnTick()
        {
            Console.WriteLine("ONTICK IS WORKING AND ATTEMPTING TO VERIFY USER.");
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                return;

            Console.WriteLine("ONTICK HAS VERIFIED THE USER.");
            var activeWeapon = Owner.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value;
            var weaponName = activeWeapon?.DesignerName;

            if (string.IsNullOrEmpty(weaponName) || _allowedWeapons.Contains(weaponName))
                return;

            Console.WriteLine($"[RestrictWeaponsEffect] Disallowed weapon detected: {weaponName}");

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
            var toRemove = inventory
                .Select(h => h.Value)
                .Where(e => e != null)
                .ToList(); // clone list to avoid mutation during iteration

            foreach (var weapon in toRemove)
            {
                weapon.Remove();
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
