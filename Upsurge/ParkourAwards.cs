using MCGalaxy;
using MCGalaxy.Games;
using MCGalaxy.Modules.Awards;
using System;
using System.Linq;

namespace MCGalaxy
{
    public class ParkourAwards
    {
        public static void Award(Player p)
        {
            ParkourData data = ParkourGame.Get(p);
            if (p.level != ParkourGame.Instance.Map || p.Game.Referee || !(data.Checkpoints.Count > 1)) return;

            if (data.BronzeMedals >= 50)
            {
                AwardPlayer(p, "Bronze Collector");
            }

            if (data.SilverMedals >= 50)
            {
                AwardPlayer(p, "Silver Collector");
            }

            if (data.GoldMedals >= 50)
            {
                AwardPlayer(p, "Gold Collector");
            }

            if (data.TotalRoundsFinished >= 100)
            {
                AwardPlayer(p, "Finisher");
            }

            if (data.CurrentRoundsFinished >= 3)
            {
                AwardPlayer(p, "Streak");
            }

            if (p.money > 500)
            {
                AwardPlayer(p, "Baby you're a rich man!");
            }

            //if (data.Finished && (data.FinishedTime - ((Stopwatch)p.Extras["stopwatch"]).getStartTime() < (TimeSpan)(p.level.Extras["authortime"])))
            //{
            //    AwardPlayer(p, "The student becomes the master");
            //}

            if (data.Finished && (data.FinishedTime - ((Stopwatch)p.Extras["stopwatch"]).GetStartTime() < TimeSpan.FromSeconds(1)))
            {
                AwardPlayer(p, "Just in time");
            }

            if (data.AuthorTimes >= 10)
            {
                AwardPlayer(p, "The master becomes the grandmaster");
            }
        }

        private static void AwardPlayer(Player p, string award)
        {
            if (PlayerAwards.Give(p.name, award))
            {
                Chat.MessageGlobal("{0} &Swas awarded: &b{1}", p.name, award);
                PlayerAwards.Save();
            }
        }

    }
}