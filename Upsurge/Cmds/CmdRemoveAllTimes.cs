using MCGalaxy.DB;
using MCGalaxy.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCGalaxy
{
    class CmdRemoveAllTimes : Command2
    {
        public override string name { get { return "RemoveAllTimes"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override CommandEnable Enabled { get { return CommandEnable.Always; } }
        public override bool SuperUseable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string player)
        {
            if (player == "")
            {
                Help(p);
                return;
            }

            if (PlayerDB.FindName(player) == null)
            {
                p.Message("Could not find player");
                return;
            }

            Database.Execute(String.Format("DELETE FROM FinishTimes WHERE Player=\"{0}+\"", player));

            p.Message("Successfully finished removing all parkour data for player " + player);
        }

        public override void Help(Player p)
        {
            p.Message("&T/RemoveAllTimes [player]");
            p.Message("&HRemoves all parkour data associated with a player");
        }
    }
}
