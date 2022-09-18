using MCGalaxy.Games;
using MCGalaxy.SQL;
using System;
using System.Collections.Generic;

namespace MCGalaxy.Commands.Building
{
    public sealed class CmdParkourReveal : Command2
    {
        public override string name { get { return "ParkourReveal"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string args)
        {
            // List of arrays of the form [x, y, z], each message block coordinates with the word /parkourgate in them
            List<String[]> GateBlocks = Database.GetRows("Messages" + p.level.name, "X, Y, Z", "WHERE lower(Message)=\"/parkourgate\"");
            List<string[]> CheckpointBlocks = Database.GetRows("Messages" + p.level.name, "X, Y, Z", "WHERE (lower(Message) LIKE \"/parkourcheckpoint%\")");
            List<string[]> FinishBlocks = Database.GetRows("Messages" + p.level.name, "X, Y, Z", "WHERE (lower(Message) LIKE \"/parkourfinish%\")");

            // Turn the gates off (into air)
            if (p.Extras.GetBoolean("parkourreveal", false))
            {
                // Turn blocks into their respective colors
                foreach (String[] s in GateBlocks)
                {
                    p.SendBlockchange(ushort.Parse(s[0]), ushort.Parse(s[1]), ushort.Parse(s[2]), Block.Green);
                }
                foreach (String[] s in CheckpointBlocks)
                {
                    p.SendBlockchange(ushort.Parse(s[0]), ushort.Parse(s[1]), ushort.Parse(s[2]), Block.Red);
                }
                foreach (String[] s in FinishBlocks)
                {
                    p.SendBlockchange(ushort.Parse(s[0]), ushort.Parse(s[1]), ushort.Parse(s[2]), Block.Blue);
                }

                p.Extras["parkourreveal"] = false;
            } else
            {
                // Turn blocks back to normal
                foreach (String[] s in GateBlocks)
                {
                    p.RevertBlock(ushort.Parse(s[0]), ushort.Parse(s[1]), ushort.Parse(s[2]));
                }
                foreach (String[] s in CheckpointBlocks)
                {
                    p.RevertBlock(ushort.Parse(s[0]), ushort.Parse(s[1]), ushort.Parse(s[2]));
                }
                foreach (String[] s in FinishBlocks)
                {
                    p.RevertBlock(ushort.Parse(s[0]), ushort.Parse(s[1]), ushort.Parse(s[2]));
                }

                p.Extras["parkourreveal"] = true;
            }
        }

        public override void Help(Player p)
        {
            p.Message(@"""/ParkourReveal"" reveals all special parkour blocks");
        }
    }
}
