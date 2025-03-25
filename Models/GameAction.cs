using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace WarcraftPlugin.Models
{
    internal class GameAction
    {
        public Type EventType { get; set; }
        public Action<GameEvent> Handler { get; set; }
        public HookMode HookMode { get; set; }
    }
}
