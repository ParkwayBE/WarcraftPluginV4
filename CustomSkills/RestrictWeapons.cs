using CounterStrikeSharp.API.Core;
using System.Collections.Generic;
using System;

public static class RestrictWeapons
{
    // Dictionary to store allowed weapons per player
    private static readonly Dictionary<ulong, HashSet<string>> _allowedWeapons = new();

    public static void Handle(CCSPlayerController player, string context, List<string> allowedWeapons)
    {
        /* 
        if (player == null || !player.IsValid || player.PlayerPawn?.Value == null)
            return;

        var steamId = player.SteamID;

        switch (context.ToLower())
        {
            case "spawn":
                _allowedWeapons[steamId] = new HashSet<string>(allowedWeapons);
                player.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;
                break;

            case "pickup":
                if (!_allowedWeapons.TryGetValue(steamId, out var allowed))
                    return;

                var activeWeapon = player.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value;
                var weaponName = activeWeapon?.DesignerName;

                if (string.IsNullOrEmpty(weaponName))
                    return;

                if (!allowed.Contains(weaponName))
                {
                    Console.WriteLine($"[RestrictWeapons] Blocking weapon pickup: {weaponName}");
                    player.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = true;
                    player.DropActiveWeapon(); // optional
                }
                else
                {
                    player.PlayerPawn.Value.WeaponServices.PreventWeaponPickup = false;
                }
                break;

            case "cleanup":
                _allowedWeapons.Remove(steamId);
                break;
        
        
          }
        */
    }
}
