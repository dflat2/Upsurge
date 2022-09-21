using System;
using MCGalaxy.Games;

namespace MCGalaxy.Commands.Building
{
    public sealed class CmdParkourCheckpoint : Command2
    {
        public override string name { get { return "ParkourCheckpoint"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (data.Context != CommandContext.MessageBlock)
            {
                p.Message("&cThis command can only be used in a message block!"); return;
            }

            string[] args = message.SplitSpaces();
            UInt16 checkpointNumber = 0;
            UInt16 respawnX = (ushort)data.MBCoords.X;
            UInt16 respawnY = (ushort)data.MBCoords.Y;
            UInt16 respawnZ = (ushort)data.MBCoords.Z;
            bool respawn = false;

            // Command Parser
            switch (args.Length)
            {
                case 1: //Either "parkourcheckpoint show" or "parkourcheckpoint [num]" but "parkourcheckpoint" is also considered length 1
                    if (args[0] == "")
                    {
                        Help(p);
                        return;
                    }
                    else if (UInt16.TryParse(args[0], out checkpointNumber))
                    {
                        if (checkpointNumber == 0)
                        {
                            p.Message("0 is a reserved number. Do not use");
                            return;
                        }
                    }
                    break;
                case 2: //parkourcheckpoint [num] respawn
                    if (UInt16.TryParse(args[0], out checkpointNumber) && args[1] == "respawn")
                    {
                        respawn = true;
                    }
                    else
                    {
                        Help(p);
                        return;
                    }
                    if (checkpointNumber == 0)
                    {
                        p.Message("0 is a reserved number. Do not use");
                        return;
                    }
                    break;
                case 5: //parkourcheckpoint [num] respawn [x] [y] [z]
                    if (UInt16.TryParse(args[0], out checkpointNumber)
                        && args[1] == "respawn"
                        && UInt16.TryParse(args[2], out respawnX)
                        && UInt16.TryParse(args[3], out respawnY)
                        && UInt16.TryParse(args[4], out respawnZ))
                    {
                        respawn = true;
                    }
                    else
                    {
                        Help(p);
                        return;
                    }
                    if (checkpointNumber == 0)
                    {
                        p.Message("0 is a reserved number. Do not use");
                        return;
                    }
                    break;
                default:
                    Help(p);
                    return;
            }

            if (!ParkourGame.Instance.RoundInProgress || p.level != ParkourGame.Instance.Map)
            {
                return;
            } else
            {
                ParkourGame.Instance.CrossedCheckPoint(p, checkpointNumber, respawn, respawnX, respawnY, respawnZ);
            }
        }

        public override void Help(Player p)
        {
            p.Message(@"""/ParkourCheckpoint [value]"" creates a checkpoint block");
            p.Message("Value must be a natural number, 0 not included");
            p.Message(@"Use ""/ParkourCheckpoint show"" to toggle show mode");
            p.Message(@"""/ParkourCheckpoint [value] respawn"" creates a respawn checkpoint block with the spawn set at the block's location");
            p.Message(@"""/ParkourCheckpoint [value] respawn [x] [y] [z]"" creates a respawn checkpoint block with the spawn at (x,y,z)");
            p.Message(@"Hint: use ""/t mb air /ParkourCheckpoint"" to quickly place checkpoints");
        }
    }
}
