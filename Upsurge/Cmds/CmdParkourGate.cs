namespace MCGalaxy.Commands.Building
{
    public sealed class CmdParkourGate : Command2
    {
        public override string name { get { return "ParkourGate"; } }
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
        }

        public override void Help(Player p)
        {
            p.Message(@"""/ParkourGate"" creates a parkour gate block");
            p.Message(@"Hint: use ""/t mb air /ParkourGate"" to quickly place gate blocks");
        }
    }
}
