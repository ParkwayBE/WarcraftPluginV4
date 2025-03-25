using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;

namespace WarcraftPlugin.Menu;

internal class Menu
{
    internal string Title { get; set; } = "";
    internal int ResultsBeforePaging { get; set; }
    internal LinkedList<MenuOption> Options { get; set; } = new();
    internal LinkedListNode<MenuOption> Prev { get; set; } = null;

    internal LinkedListNode<MenuOption> Add(string display, string subDisplay, Action<CCSPlayerController, MenuOption> onChoice, Action<CCSPlayerController, MenuOption> onSelect = null)
    {
        if (Options == null)
            Options = new();
        MenuOption newOption = new MenuOption
        {
            OptionDisplay = display,
            SubOptionDisplay = subDisplay,
            OnChoose = onChoice,
            OnSelect = onSelect,
            Index = Options.Count,
            Parent = this
        };
        return Options.AddLast(newOption);
    }
}
