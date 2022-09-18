using MCGalaxy.DB;

namespace MCGalaxy.Commands.Fun
{
    public sealed class CmdReplay : Command2
    {
        public override string name { get { return "Replay"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string name)
        {
            if (name == "")
            {
                Help(p); return;
            }

            if (PlayerDB.FindName(name) == null)
            {
                p.Message("Could not find player");
                return;
            }
            else
            {
                PlayerBot bot = new PlayerBot(name, p.level);
                if (p.level.Bots.Count + p.level.players.Count >= Server.Config.MaxBotsPerLevel)
                {
                    p.Message("Reached maximum number of bots allowed on this map."); return;
                }

                bot.SetInitialPos(p.Pos);
                bot.SetYawPitch(p.Rot.RotY, 0);

                PlayerBot.Add(bot, false);

                Replayer replay = new Replayer(p, bot, p.level.name);
                replay.StartReplayer();
            }
        }

        public override void Help(Player p)
        {
            p.Message(@"/Replay [player]");
            p.Message("Replays someone's run recorder of the current map.");
        }
    }
}
