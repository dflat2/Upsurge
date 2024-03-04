namespace MCGalaxy.Commands.Fun
{

    public class CmdRate : Command2
    {
        public override string name { get { return "Rate"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            int oldRating = int.MaxValue;
            if (message == "")
            {
                Help(p); return;
            }
            if (!int.TryParse(message, out int rating))
            {
                p.Message("Not a valid input");
                return;
            }

            if (rating > 5 || rating < 1)
            {
                p.Message("Not a valid input");
                return;
            }

            string path = "extra/Ratings";
            if (CheckIsAuthor(p))
            {
                p.Message("Cannot rate this map as you are an author of it."); return;
            }

            // Initialize path directory if it does not exist
            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}

            PlayerExtList levelList = PlayerExtList.Load(path + "/" + p.level.name + ".txt"); // Automatically creates the file as well

            if (levelList.Contains(p.truename))
            {
                oldRating = int.Parse(levelList.FindData(p.truename));
            }

            levelList.Update(p.truename, rating.ToString());

            if (oldRating == int.MaxValue)  // Not rated before
            {
                p.level.Config.Likes += rating;
                p.level.Config.Dislikes += 5 - rating;
                p.SetMoney(p.money + 5);
                p.Message("Thank you for voting! You received 5 " + Server.Config.Currency);
            }
            else
            {   // bit of trickery but this works out to the right answer
                p.level.Config.Likes += rating - oldRating;
                p.level.Config.Dislikes += oldRating - rating;
                p.Message("Thank you for rating this map!");
            }

            levelList.Save();
            p.level.SaveSettings();
        }
        protected static bool CheckIsAuthor(Player p)
        {
            string[] authors = p.level.Config.Authors.SplitComma();
            return authors.CaselessContains(p.name);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Rate [num]");
            p.Message("&HRates a map from 1 to 5.");
        }
    }
}
