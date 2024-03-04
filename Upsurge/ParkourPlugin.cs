using MCGalaxy.Commands.Building;
using MCGalaxy.Commands.Fun;
using MCGalaxy.Games;
using MCGalaxy.SQL;

namespace MCGalaxy
{
    public class ParkourPlugin: Plugin
    {
        public override string creator { get { return "Opapinguin"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string name { get { return "Parkour"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdParkour());

            /*******************
             * DATABASE TABLES *
             *******************/
            if (!Database.TableExists("Rounds"))
            {
                Database.CreateTable("Rounds", new ColumnDesc[] {
                new ColumnDesc("ID", ColumnType.UInt32),
                new ColumnDesc("map", ColumnType.VarChar)});
            }

            if (!Database.TableExists("FinishTimes"))
            {
                Database.CreateTable("FinishTimes", new ColumnDesc[] {
                new ColumnDesc("ID", ColumnType.Integer),
                new ColumnDesc("Player", ColumnType.VarChar),
                new ColumnDesc("FinishTimeMS", ColumnType.Integer),
                new ColumnDesc("Checkpoint", ColumnType.Int8)});
            }

            /*******************
             * DATABASE VIEWS *
             *******************/

            // Best times orders by player, then map, a person's best time and checkpoint on that map
            Database.Execute("CREATE VIEW IF NOT EXISTS BestTimes " +
            "AS SELECT player," +
            "map," +
            "Checkpoint," +
            "FinishTimeMS FROM " +
            "(SELECT row_number() OVER(PARTITION BY player,map ORDER BY Checkpoint DESC, FinishTimeMS ASC) AS rn,* FROM (" +
            "SELECT * FROM Rounds INNER JOIN FinishTimes ON Rounds.ID = FinishTimes.ID" +
            ")) a " +
            "WHERE a.rn = 1;");

            // Ordered finish times sorts by map, then by players who got the best time
            Database.Execute("CREATE VIEW IF NOT EXISTS OrderedFinishTimes " +
                "AS SELECT * FROM(" +
                "SELECT map," +
                "player," +
                "Checkpoint," +
                "FinishTimeMS," +
                "row_number() OVER(PARTITION BY Rounds.map ORDER BY Checkpoint DESC, FinishTimeMS ASC) AS mapRank " +
                "FROM FinishTimes INNER JOIN Rounds ON FinishTimes.ID = Rounds.ID) ranks " +
                "where mapRank <= 100");

            // Round winners sorts by rounds (round ID and associated map) and shows the top 3 players in the round
            Database.Execute("CREATE VIEW IF NOT EXISTS RoundWinners " +
                "AS SELECT * FROM(" +
                "SELECT Rounds.ID," +
                "map," +
                "player," +
                "Checkpoint," +
                "FinishTimeMS," +
                "row_number() OVER(PARTITION BY Rounds.ID ORDER BY Rounds.ID ASC, Checkpoint DESC, FinishTimeMS ASC) AS playerRank " +
                "FROM FinishTimes INNER JOIN Rounds ON FinishTimes.ID = Rounds.ID) ranks " +
                "where playerRank <= 3");

            /************
             * COMMANDS *
             ************/

            Command.Register(new CmdParkourCheckpoint());       // Create checkpoint blocks
            Command.Register(new CmdParkourFinish());           // Create finish blocks
            Command.Register(new CmdParkourGate());             // Create gate blocks

            Command.Register(new CmdPlayerBest());              // Player-specific best stats
            Command.Register(new CmdParkourTop());              // Map-specific best stats

            Command.Register(new CmdRate());                    // Rating maps
            Command.Register(new CmdReplay());                  // Run replayer
            Command.Register(new CmdStopReplayers());           // Stop replayers (they can definitely crowd your view a bit)
            Command.Register(new CmdToggleVisibility());        // Toggle other players' visibility
            Command.Register(new CmdParkourReveal());           // Toggle parkour blocks for visibility
            Command.Register(new CmdRemoveBestTime());
            Command.Register(new CmdRemoveAllTimes());
            Command.Register(new CmdTimeLeft());


            // Load the configuration and autostart if turned on
            ParkourGame.Config.Load();
            ParkourGame.Instance.AutoStart();
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Parkour"));

            Command.Unregister(Command.Find("ParkourCheckpoint"));
            Command.Unregister(Command.Find("ParkourFinish"));
            Command.Unregister(Command.Find("ParkourGate"));

            Command.Unregister(Command.Find("PlayerBest"));
            Command.Unregister(Command.Find("ParkourTop"));

            Command.Unregister(Command.Find("Rate"));
            Command.Unregister(Command.Find("Replay"));
            Command.Unregister(Command.Find("StopReplayers"));
            Command.Unregister(Command.Find("ToggleVisibility"));
            Command.Unregister(Command.Find("ParkourReveal"));
            Command.Unregister(Command.Find("RemoveBestTime"));
            Command.Unregister(Command.Find("RemoveAllTimes"));
            Command.Unregister(Command.Find("TimeLeft"));

            ParkourGame.Instance.End();
            IGame.RunningGames.Remove(ParkourGame.Instance);
        }
    }
}
