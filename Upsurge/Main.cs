using System;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Commands.Building;
using MCGalaxy.Commands.Fun;
using MCGalaxy.Games;
using MCGalaxy.SQL;

namespace MCGalaxy
{
    public class ParkourPlugin: Plugin
    {
        public override string creator { get { return "Opapinguin"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.2"; } }
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

            ParkourGame.Instance.End();
            IGame.RunningGames.Remove(ParkourGame.Instance);
        }
    }

    public sealed class CmdParkour : RoundsGameCmd
    {
        public override string name { get { return "Parkour"; } }
        protected override RoundsGame Game { get { return ParkourGame.Instance; } }
        public override CommandAlias[] Aliases
        {
            get { return new[] { new CommandAlias("PG"), new CommandAlias("RoundTime", "set roundtime") }; }
        }
        public override CommandPerm[] ExtraPerms
        {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can manage parkour") }; }
        }

        protected override void HandleSet(Player p, RoundsGame game, string[] args)
        {
            ParkourConfig cfg = ParkourGame.Config;
            string prop = args[1];
            LevelConfig lCfg = p.level.Config;

            if (prop.CaselessEq("map"))
            {
                p.Message("Pillaring allowed: &b" + lCfg.Pillaring);
                p.Message("Build type: &b" + lCfg.BuildType);
                p.Message("Round time: &b{0}" + lCfg.RoundTime.Shorten(true, true));
                //p.Message("Author time: &b{0}" + lCfg.AuthorTime.Shorten(true, true));
                return;
            }
            if (args.Length < 3) { Help(p, "set"); return; }

            if (prop.CaselessEq("maxmove"))
            {
                if (!CommandParser.GetReal(p, args[2], "Max move distance", ref cfg.MaxMoveDist, 0, 4)) return;
                p.Message("Set max move distance to &a" + cfg.MaxMoveDist + " &Sblocks apart");

                cfg.Save(); return;
            }
            else if (prop.CaselessEq("pillaring"))
            {
                if (!CommandParser.GetBool(p, args[2], ref lCfg.Pillaring)) return;

                p.Message("Set pillaring allowed to &b" + lCfg.Pillaring);
                game.UpdateAllStatus2();
            }
            else if (prop.CaselessEq("build"))
            {
                if (!CommandParser.GetEnum(p, args[2], "Build type", ref lCfg.BuildType)) return;
                p.level.UpdateBlockPermissions();

                p.Message("Set build type to &b" + lCfg.BuildType);
                game.UpdateAllStatus2();
            }
            else if (prop.CaselessEq("roundtime"))
            {
                if (!ParseTimespan(p, "round time", args, ref lCfg.RoundTime)) return;
            }
            //else if (prop.CaselessEq("authortime"))
            //{
                //if (!ParseTimespan(p, "author time", args, ref lCfg.AuthorTime)) return;
            //}
            else
            {
                Help(p, "set"); return;
            }
            p.level.SaveSettings();
        }

        static bool ParseTimespan(Player p, string arg, string[] args, ref TimeSpan span)
        {
            if (!CommandParser.GetTimespan(p, args[2], ref span, "set " + arg + " to", "m")) return false;
            p.Message("Set {0} to &b{1}", arg, span.Shorten(true));
            return true;
        }

        public override void Help(Player p, string message)
        {
            if (message.CaselessEq("set"))
            {
                p.Message("&T/Help Parkour game &H- Views help for game settings");
                p.Message("&T/Help Parkour map &H- Views help for per-map settings");
            }
            else if (message.CaselessEq("game"))
            {
                p.Message("&T/Parkour set hitbox [distance]");
                p.Message("&HSets furthest apart players can be before they are considered touching.");
                p.Message("&T/Parkour set maxmove [distance]");
                p.Message("&HSets largest distance players can move in a tick " +
                               "before they are considered speedhacking.");
            }
            else if (message.CaselessEq("map"))
            {
                p.Message("&T/Parkour set map &H-Views map settings");
                p.Message("&T/Parkour set pillaring [yes/no]");
                p.Message("&HSets whether players are allowed to pillar");
                p.Message("&T/Parkour set build [normal/modifyonly/nomodify]");
                p.Message("&HSets build type of the map");
                p.Message("&T/Parkour set roundtime [timespan]");
                p.Message("&HSets how long a round is");
                //p.Message("&T/Parkour set authortime [timespan]");
                p.Message("&HSets the author (challenge) time");
            }
            else
            {
                base.Help(p, message);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Parkour start <map> &H- Starts Parkour");
            p.Message("&T/Parkour stop &H- Stops Parkour");
            p.Message("&T/Parkour end &H- Ends current round of Parkour");
            p.Message("&T/Parkour add/remove &H- Adds/removes current map from map list");
            p.Message("&T/Parkour set [property] &H- Sets a property. See &T/Help Parkour set");
            p.Message("&T/Parkour status &H- Outputs current status of Parkour");
            p.Message("&T/Parkour go &H- Moves you to the current Parkour map");
        }
    }
}
