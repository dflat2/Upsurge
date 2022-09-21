using MCGalaxy.Games;
using MCGalaxy.Maths;
using System;
using MCGalaxy.DB;
using System.Collections.Generic;

namespace MCGalaxy.Commands.Fun
{
    public sealed class CmdStopReplayers : Command2
    {
        public override string name { get { return "StopReplayers"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string name)
        {
            if (!p.level.Extras.Contains("replayers")) { return; }  // no replayers on the map or replayers extra never initialized

            int count = ((List<Replayer>)(p.level.Extras["replayers"])).Count;

            foreach (Replayer R in ((List<Replayer>)(p.level.Extras["replayers"])))
            {
                R.StopReplayer();
            }

            ((List<Replayer>)(p.level.Extras["replayers"])).Clear();

            p.Message("Removed " + count + " replay bots");
        }

        public override void Help(Player p)
        {
            p.Message(@"/Replay [player]");
            p.Message("Deletes all replay bots on the level");
        }
    }
}
