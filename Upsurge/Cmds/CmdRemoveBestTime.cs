using MCGalaxy.DB;
using MCGalaxy.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCGalaxy
{
    class CmdRemoveBestTime : Command2
    {
        public override string name { get { return "RemoveBestTime"; } }
        public override string type { get { return CommandTypes.Games; } }
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

            Database.Execute(String.Format("DELETE FROM FinishTimes " +
                "WHERE(ID, FinishTimeMS, Checkpoint) IN " +
                "(SELECT ID, MIN(FinishTimeMS), Checkpoint FROM FinishTimes " +
                "NATURAL JOIN ROUNDS " +
                "WHERE Player = \"{0}+\" AND Map = \"{1}\" " +
                "GROUP BY ID " +
                "HAVING Checkpoint=MAX(Checkpoint))", player, p.level.name));

            p.Message("Successfully finished removing best time data for player " + player + " on level " + p.level.name);
        }

        public override void Help(Player p)
        {
            p.Message("&T/RemoveBestTime [player]");
            p.Message("&HRemoves the best time associated with a player on the current map");
        }
    }
}
