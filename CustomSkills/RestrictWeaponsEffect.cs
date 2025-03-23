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
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                return;

            // Delay to avoid early-round weapon replication issues
            WarcraftPlugin.Instance.AddTimer(0.3f, () =>
            {
                Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;

                DropAllWeaponsExceptAllowed();

                WarcraftPlugin.Instance.AddTimer(0.2f, () =>
                {
                    GiveAllowedWeapons();
                    Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = true;
                });
            });
        }

        public override void OnTick()
        {
            if (!Owner.IsValid || Owner.PlayerPawn?.Value == null)
                return;

            var activeWeapon = Owner.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value;
            var weaponName = activeWeapon?.DesignerName;

            if (string.IsNullOrEmpty(weaponName) || _allowedWeapons.Contains(weaponName))
                return;

            Console.WriteLine($"[RestrictWeaponsEffect] Disallowed weapon detected: {weaponName}");

            Owner.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;

            DropAllWeaponsExceptAllowed();

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

        private void DropAllWeaponsExceptAllowed()
        {
            var pawn = Owner.PlayerPawn.Value;
            var weapons = pawn.WeaponServices.MyWeapons;
            if (weapons == null) return;

            for (int i = weapons.Count - 1; i >= 0; i--)
            {
                var weapon = weapons[i].Value;
                if (weapon == null || !_allowedWeapons.Contains(weapon.DesignerName))
                {
                    Console.WriteLine($"[RestrictWeaponsEffect] Dropping disallowed weapon: {weapon?.DesignerName}");
                    DropWeaponByDesignerName(Owner, weapon?.DesignerName ?? "");
                }
            }
        }

        private void DropWeaponByDesignerName(CCSPlayerController player, string weaponName)
        {
            if (player == null || !player.IsValid || string.IsNullOrEmpty(weaponName)) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || !player.PawnIsAlive || pawn.WeaponServices == null) return;

            var matchedWeapon = pawn.WeaponServices.MyWeapons
                .FirstOrDefault(x => x.Value?.DesignerName == weaponName);

            if (matchedWeapon != null && matchedWeapon.IsValid)
            {
                pawn.WeaponServices.ActiveWeapon.Raw = matchedWeapon.Raw;
                player.DropActiveWeapon();
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
