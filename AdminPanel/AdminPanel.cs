using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace WarcraftPlugin.Core
{


    public class AdminPanel
    {
        private readonly WarcraftPlugin _plugin;

        public AdminPanel(WarcraftPlugin plugin)
        {
            _plugin = plugin;
            _plugin.AddCommand("say", "adminPanel", OpenAdminPanel);

        }

        public void OpenAdminPanel(CCSPlayerController? player, CommandInfo commandInfo)
        {

            var wcPlayer = _plugin.GetWcPlayer(player);
            if (wcPlayer == null) return;
            var playerP = wcPlayer.GetPlayer();
            var role = 1;
            var message = "hallo mannekes";

            //int role = _db.GetPlayerRole(playerP);
            if (role == 0)
            {
                player.PrintToChat("You are a player, You cant use admin panel");
            }
            if (role == 1)
            {
                playerP.PrintToChat("You are an admin, the panel will open soon ;)");
                //player.ExecuteClientCommand($"panorama.RunScript(\"global.CSSharp_MessageEvent({{message: '{message}'}})\");");
                //string command = $"DispatchEvent('CSSharp_MessageEvent', {{ message: '{message}' }});";
                //player.ExecuteClientCommand("GameEvents.SendCustomGameEvent('CSSharp_MessageEvent', { message: 'Hello from CSSharp!' });");
                //player.ExecuteClientCommandFromServer(command);
                Console.WriteLine("admin panel test");
                //Server.ExecuteCommand(command);
                playerP.PrintToCenterHtml("<font color='#FFFFFF'>\" + message + \"</font>");

            }
            if (role == 9009)
            {
                player.PrintToChat("Nah, no roles for u");
            }

        }
        public void ChangeRole(CCSPlayerController player, int role)
        {
            //_db.ChangePlayerRole(player, role);
        }
        //[GameEventHandler]
        //public HookResult PlayerSpawnHandler(EventPlayerSpawn @event, GameEventInfo info)
        //{
        //  Console.WriteLine($"Player Spawned: {@event.Userid}");

        // Send a message to all clients


        //var player = @event.Userid;
        //var player = Utilities.GetPlayerFromUserid(@event.Userid);
        // var player = @event.Userid;
        //  var message = "adminPanel";

        //  player.ExecuteClientCommand($"say adminPanel");

        //  return HookResult.Continue;
        // }
    }
}
namespace CounterStrikeSharp.API.Core
{
    public partial class CCSPlayerControllerExtra : CCSPlayerController
    {
        public int Role;
        public CCSPlayerControllerExtra(nint index)
        : base(index)
        {
            this.Role = 0;
        }

        public int GetRole() { return this.Role; }
    }


}
