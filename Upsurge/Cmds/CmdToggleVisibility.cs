using MCGalaxy.Games;
using MCGalaxy.Maths;
using System;

namespace MCGalaxy.Commands.Fun
{
    public sealed class CmdToggleVisibility : Command2
    {
        public override string name { get { return "ToggleVisibility"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override string shortcut { get { return "TV"; } }

        public override void Use(Player p, string message)
        {
            Vec3F32 P = p.Pos.ToVec3F32();

            if (Math.Abs(0.5f - P.X + Math.Truncate(P.X)) > 0.2
                || Math.Abs(0.5f - P.Y + Math.Truncate(P.Y)) > 0.2
                || Math.Abs(0.5f - P.Z + Math.Truncate(P.Z)) > 0.2)
            {
                // Stupid teleportation into a wall thing
                p.Message("Anti-exploit: you have to stand near the middle of a block to toggle visibility");
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
    }
}
