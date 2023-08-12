using System;
using System.Collections.Generic;
using MCGalaxy.DB;
using MCGalaxy.SQL;

namespace MCGalaxy
{
    public class CmdPlayerBest : Command2
    {
        public override string name { get { return "PlayerBest"; } }
        public override bool SuperUseable { get { return false; } }
        public override string type { get { return CommandTypes.Information; } }
        public override void Use(Player p, string message)
        {
            List<string[]> rows;
            string plName = p.name;
            string level = p.level.name;

            string[] args = message.SplitSpaces();

            // Ugly parser as always
            if (message == "")
            {
                rows = Database.GetRows("BestTimes", "map,Checkpoint,FinishTimeMS", "WHERE Player=@0 AND map=@1", plName, p.level.name);
            }
            else if (args.Length == 1)
            {
                if (args[0] == "all")
                {
                    rows = Database.GetRows("BestTimes", "map,Checkpoint,FinishTimeMS", "WHERE Player=@0", plName);
                }
                else if (LevelInfo.AllMapNames().CaselessContains(args[0].ToLower()))
                {
                    level = args[0].ToLower();
                    rows = Database.GetRows("BestTimes", "map,Checkpoint,FinishTimeMS", "WHERE Player=@0 AND map=@1", plName, level);
                }
                else
                {
                    p.Message("Level not found."); return;
                }
            }
            else if (args.Length == 2)
            {
                if (PlayerDB.FindName(args[0]) == null)
                {
                    p.Message("Could not find player");
                    return;
                }


                plName = args[0];   // This is safe
                if (args[1].ToLower() == "all")
                {
                    rows = Database.GetRows("BestTimes", "map,Checkpoint,FinishTimeMS", "WHERE Player=@0", plName);
                }
                else if (LevelInfo.AllMapNames().CaselessContains(args[1].ToLower()))
                {
                    level = args[1].ToLower();
                    rows = Database.GetRows("BestTimes", "map,Checkpoint,FinishTimeMS", "WHERE Player=@0 AND map=@1", plName, level);
                }
                else
                {
                    p.Message("Level not found."); return;
                }
            }
            else
            {
                Help(p); return;
            }

            p.Message("Scores for " + plName + ":");
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

                p.Message(str[0]
                        + ": "
                        + checkpoint
                        + TimeSpan.FromMilliseconds(double.Parse(str[2])).ToString(@"mm\:ss\:ff"));
            }
        }

        public override void Help(Player p)
        {
            p.Message("/PlayerBest [player] [map]");
            p.Message("Gives the best run for [player] on [map]");
            p.Message(@"Use ""/PlayerBest [player] all"" to get all scores");
        }
    }
}