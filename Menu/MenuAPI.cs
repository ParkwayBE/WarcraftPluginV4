using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WarcraftPlugin.Menu;

internal static class MenuAPI
{

    internal static readonly Dictionary<int, MenuPlayer> Players = [];

    internal static void Load(BasePlugin plugin, bool hotReload)
    {
        plugin.RegisterEventHandler<EventPlayerActivate>((@event, info) =>
        {
            if (@event.Userid != null)
                Players[@event.Userid.Slot] = new MenuPlayer
                {
                    player = @event.Userid,
                    Buttons = 0
                };
            return HookResult.Continue;
        });

        plugin.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (@event.Userid != null) Players.Remove(@event.Userid.Slot);
            return HookResult.Continue;
        });

        plugin.RegisterListener<Listeners.OnTick>(OnTick);

        if (hotReload)
            foreach (var pl in Utilities.GetPlayers())
            {
                Players[pl.Slot] = new MenuPlayer
                {
                    player = pl,
                    Buttons = pl.Buttons
                };
            }
    }

    internal static void OnTick()
    {

        foreach (var player in Players.Values.Where(p => p.MainMenu != null))
        {
            if ((player.Buttons & PlayerButtons.Forward) == 0 && (player.player.Buttons & PlayerButtons.Forward) != 0)
            {
                Console.WriteLine("menu foward");
                player.ScrollUp();
            }
            else if ((player.Buttons & PlayerButtons.Back) == 0 && (player.player.Buttons & PlayerButtons.Back) != 0)
            {
                player.ScrollDown();
            }
            else if ((player.Buttons & PlayerButtons.Jump) == 0 && (player.player.Buttons & PlayerButtons.Jump) != 0)
            {
                player.Choose();
            }
            else if ((player.Buttons & PlayerButtons.Use) == 0 && (player.player.Buttons & PlayerButtons.Use) != 0)
            {
                player.Choose();
            }
            else if ((player.Buttons & PlayerButtons.Left) == 0 && (player.player.Buttons & PlayerButtons.Left) != 0)
            {
                Console.WriteLine("menu left");
                player.ScrollLeft();
            }
            else if ((player.Buttons & PlayerButtons.Right) == 0 && (player.player.Buttons & PlayerButtons.Right) != 0)
            {
                Console.WriteLine("menu right");
                player.ScrollRight();
            }

            if (((long)player.player.Buttons & 8589934592) == 8589934592)
            {
                player.OpenMainMenu(null);
            }

            player.Buttons = player.player.Buttons;
            if (player.CenterHtml != "")
                Server.NextFrame(() =>
                player.player.PrintToCenterHtml(player.CenterHtml)
            );
        }
    }


}
