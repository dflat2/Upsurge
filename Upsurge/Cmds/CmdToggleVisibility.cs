using MCGalaxy.Blocks;
using MCGalaxy.Games;
using MCGalaxy.Maths;
using BlockID = System.UInt16;

namespace MCGalaxy.Commands.Fun
{
    public sealed class CmdToggleVisibility : Command2
    {
        public override string name { get { return "ToggleVisibility"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override string shortcut { get { return "TV"; } }

        public override void Use(Player p, string message)
        {
            AABB bb = p.ModelBB.OffsetPosition(p.Pos);

            if (CheckNearWall(p, bb)) {
                // Stupid teleportation into a wall thing
                p.Message("Anti-exploit: don't stand too close to a wall");
                return;
            }

            ParkourData data_ = ParkourGame.Get(p);
            data_.VisibilityToggled = !data_.VisibilityToggled;

            if (data_.VisibilityToggled)
            {
                foreach (Player pl in ParkourGame.Instance.PublicGetPlayers())
                {
                    Entities.Despawn(p, pl);
                }
                p.Message("Players are now invisible");
            }
            else
            {
                foreach (Player pl in ParkourGame.Instance.PublicGetPlayers())
                {
                    Entities.Spawn(p, pl);
                }
                p.Message("Players are now visible");
            }
        }

        public override void Help(Player p)
        {
            p.Message("Toggles other players' visibility in parkour.");
        }

        private bool CheckNearWall(Player p, AABB bb)
        {
            Vec3S32 min = bb.BlockMin, max = bb.BlockMax;
            Level level = p.level;

            Logger.Log(LogType.ConsoleMessage, "test");

            for (int y = min.Y; y <= max.Y; y++)
                for (int z = min.Z - 2; z <= max.Z + 2; z++)
                    for (int x = min.X - 2; x <= max.X + 2; x++)
                    {
                        ushort xP = (ushort)x, yP = (ushort)y, zP = (ushort)z;

                        BlockID block = level.GetBlock(xP, yP, zP);

                        AABB blockBB = Block.BlockAABB(block, level).Offset(x * 32, y * 32, z * 32);

                        if (!AABB.Intersects(ref bb, ref blockBB)) continue;

                        // Some blocks will cause death of players
                        if (level.CollideType(block) != CollideType.WalkThrough) return true;
                    }
            return false;
        }
    }
}
