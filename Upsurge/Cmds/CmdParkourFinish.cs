using MCGalaxy.Games;

namespace MCGalaxy.Commands.Building
{
    public sealed class CmdParkourFinish : Command2
    {
        public override string name { get { return "ParkourFinish"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string args, CommandData data)
        {
            if (data.Context != CommandContext.MessageBlock)
            {
                p.Message("&cThis command can only be used in a message block!"); return;
            }

            if (!ParkourGame.Instance.RoundInProgress)
            {
                return;
            }
            else
            {
                ParkourGame.Instance.CrossedFinish(p);
            }
        }

        public override void Help(Player p)
        {
            p.Message(@"""/ParkourFinish"" creates a parkour finish block");
            p.Message(@"Hint: use ""/t mb air /ParkourFinish"" to quickly place finish blocks");
        }
    }
}
