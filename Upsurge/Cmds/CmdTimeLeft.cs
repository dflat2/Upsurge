using MCGalaxy.Games;
using System;

namespace MCGalaxy
{
    public sealed class CmdTimeLeft : Command2
    {
        public override string name { get { return "TimeLeft"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }

        public override void Use(Player p, string message)
        {
            string format = @"mm\:ss\.ff";
            TimeSpan timeLeft = ParkourGame.Instance.RoundEnd - DateTime.Now;
            string timeStamp = timeLeft.ToString(format).TrimStart('0').TrimStart(':');
            if (timeLeft >= TimeSpan.Zero)
            {
                p.Message("Time left on parkour: " + timeStamp + " seconds.");
            } else
            {
                p.Message("Time left on parkour: 0.00 seconds.");
            }
        }

        public override void Help(Player p)
        {
            p.Message(@"/TimeLeft");
            p.Message("Shows the time left on the parkour round.");
        }
    }
}
