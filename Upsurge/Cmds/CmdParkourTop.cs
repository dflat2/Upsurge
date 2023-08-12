using System;
using System.Collections.Generic;
using MCGalaxy.SQL;

namespace MCGalaxy
{
    public class CmdParkourTop : Command2
    {
        public override string name { get { return "ParkourTop"; } }
        public override string shortcut { get { return "PTop"; } }
        public override bool SuperUseable { get { return false; } }
        public override string type { get { return CommandTypes.Information; } }
        public override void Use(Player p, string message)
        {
            string level = p.level.name;
            bool all = false;
            int x = 0;

            string[] args = message.SplitSpaces();

            // Ugly ass parser
            if (message == "")
            {
                x = 1;
            }
            else if (message.ToLower() == "all")
            {
                all = true;
            }
            else if (LevelInfo.AllMapNames().CaselessContains(message.ToLower()))
            {
                x = 1;
                level = message.ToLower();
            }
            else if (int.TryParse(message, out x))
            {
                if (x < 1 || x > 100)
                {
                    p.Message("x must be between 1 and 100");
                    Help(p); return;
                }
            }
            else if (args.Length == 2)
            {
                if (int.TryParse(args[1], out x) && LevelInfo.AllMapNames().CaselessContains(args[0].ToLower()))
                {
                    if (x < 1 || x > 100)
                    {
                        p.Message("x must be between 1 and 100");
                        Help(p); return;
                    }
                    level = args[0].ToLower();
                }
                else if (args[1].ToLower() == "all" && LevelInfo.AllMapNames().CaselessContains(args[0].ToLower()))
                {
                    level = args[0].ToLower();
                    all = true;
                }
                else
                {
                    p.Message("Number invalid or map doesn't exist");
                    Help(p); return;
                }
            }
            else
            {
                Help(p); return;
            }


            List<string[]> rows = Database.GetRows("OrderedFinishTimes", "player,Checkpoint,FinishTimeMS", "WHERE map=@0 ORDER BY mapRank", level);
            if (all)
            {
                int index = 1;
                foreach (string[] str in rows)
                {
                    string checkpoint;
                    if (str[1] == "65535")  // uint.max
                    {
                        checkpoint = "Finish, ";
                    }
                    else
                    {
                        checkpoint = "Checkpoint " + str[1] + ", ";
                    }
                    p.Message((index).ToString()
                        + ") "
                        + str[0]
                        + ": "
                        + checkpoint
                        + TimeSpan.FromMilliseconds(double.Parse(str[2])).ToString(@"mm\:ss\:ff"));
                    index++;
                }
            }
            else
            {
                for (int i = x - 1; i < Math.Min(x + 10 - 1, rows.Count); i++)
                {
                    string checkpoint;
                    if (rows[i][1] == "65535")
                    {
                        checkpoint = "Finish, ";
                    }
                    else
                    {
                        checkpoint = "Checkpoint " + rows[i][1] + ", ";
                    }
                    p.Message((i + 1).ToString()
                        + ") "
                        + rows[i][0]
                        + ": "
                        + checkpoint
                        + TimeSpan.FromMilliseconds(double.Parse(rows[i][2])).ToString(@"mm\:ss\:ff"));
                }
            }
        }

        public override void Help(Player p)
        {
            p.Message("/ParkourTop [map] [x]");
            p.Message("Gives the best scores in the range x -> x + 10 for the given map");
            p.Message(@"Use ""/ParkourTop [map] all"" to see all top 100 players on the map");
        }
    }
}