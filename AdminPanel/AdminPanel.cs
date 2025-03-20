using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using WarcraftPlugin.Helpers;


namespace WarcraftPlugin.Core
{


    internal class AdminPanel
    {
        private readonly WarcraftPlugin _plugin;
        Database _db;
        public AdminPanel(WarcraftPlugin plugin, Database db)
        {
            _plugin = plugin;
            _db = db;
        }

        public void OpenAdminPanel(CCSPlayerController player)
        {
            var wcPlayer = _plugin.GetWcPlayer(player);
            if (wcPlayer == null) return;
            var playerP = wcPlayer.GetPlayer();
            int role = _db.GetPlayerRole(playerP);
            if (role == 0)
            {
                player.PrintToChat("You are a player, You cant use admin panel");
            }
            if (role == 1)
            {
                player.PrintToChat("You are an admin, the panel will open soon ;)");
            }
            if (role == 9009)
            {
                player.PrintToChat("Nah, no roles for u");
            }

        }
        public void ChangeRole(CCSPlayerController player, int role)
        {
            _db.ChangePlayerRole(player, role);
        }

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
