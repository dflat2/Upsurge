using System;
using MCGalaxy.Commands;
using MCGalaxy.Commands.Fun;
using MCGalaxy.Games;

namespace MCGalaxy
{
    public sealed class CmdParkour : RoundsGameCmd
    {
        public override string name { get { return "Parkour"; } }
        protected override RoundsGame Game { get { return ParkourGame.Instance; } }
        public override CommandAlias[] Aliases
        {
            get { return new[] { new CommandAlias("PG"), new CommandAlias("RoundTime", "set roundtime") }; }
        }
        public override CommandPerm[] ExtraPerms
        {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can manage parkour") }; }
        }

        protected override void HandleSet(Player p, RoundsGame game, string[] args)
        {
            ParkourConfig cfg = ParkourGame.Config;
            string prop = args[1];
            LevelConfig lCfg = p.level.Config;

            if (prop.CaselessEq("map"))
            {
                p.Message("Pillaring allowed: &b" + lCfg.Pillaring);
                p.Message("Build type: &b" + lCfg.BuildType);
                p.Message("Round time: &b{0}" + lCfg.RoundTime.Shorten(true, true));
                //p.Message("Author time: &b{0}" + lCfg.AuthorTime.Shorten(true, true));
                return;
            }
            if (args.Length < 3) { Help(p, "set"); return; }

            if (prop.CaselessEq("maxmove"))
            {
                if (!CommandParser.GetReal(p, args[2], "Max move distance", ref cfg.MaxMoveDist, 0, 4)) return;
                p.Message("Set max move distance to &a" + cfg.MaxMoveDist + " &Sblocks apart");

                cfg.Save(); return;
            }
            else if (prop.CaselessEq("pillaring"))
            {
                if (!CommandParser.GetBool(p, args[2], ref lCfg.Pillaring)) return;

                p.Message("Set pillaring allowed to &b" + lCfg.Pillaring);
                game.UpdateAllStatus2();
            }
            else if (prop.CaselessEq("build"))
            {
                if (!CommandParser.GetEnum(p, args[2], "Build type", ref lCfg.BuildType)) return;
                p.level.UpdateBlockPermissions();

                p.Message("Set build type to &b" + lCfg.BuildType);
                game.UpdateAllStatus2();
            }
            else if (prop.CaselessEq("roundtime"))
            {
                if (!ParseTimespan(p, "round time", args, ref lCfg.RoundTime)) return;
            }
            else if (prop.CaselessEq("authortime"))
            {
                //if (!ParseTimespan(p, "author time", args, ref lCfg.AuthorTime)) return;
            }
            else
            {
                Help(p, "set"); return;
            }
            p.level.SaveSettings();
        }

        static bool ParseTimespan(Player p, string arg, string[] args, ref TimeSpan span)
        {
            if (!CommandParser.GetTimespan(p, args[2], ref span, "set " + arg + " to", "m")) return false;
            p.Message("Set {0} to &b{1}", arg, span.Shorten(true));
            return true;
        }

        public override void Help(Player p, string message)
        {
            if (message.CaselessEq("set"))
            {
                p.Message("&T/Help Parkour game &H- Views help for game settings");
                p.Message("&T/Help Parkour map &H- Views help for per-map settings");
            }
            else if (message.CaselessEq("game"))
            {
                p.Message("&T/Parkour set hitbox [distance]");
                p.Message("&HSets furthest apart players can be before they are considered touching.");
                p.Message("&T/Parkour set maxmove [distance]");
                p.Message("&HSets largest distance players can move in a tick " +
                               "before they are considered speedhacking.");
            }
            else if (message.CaselessEq("map"))
            {
                p.Message("&T/Parkour set map &H-Views map settings");
                p.Message("&T/Parkour set pillaring [yes/no]");
                p.Message("&HSets whether players are allowed to pillar");
                p.Message("&T/Parkour set build [normal/modifyonly/nomodify]");
                p.Message("&HSets build type of the map");
                p.Message("&T/Parkour set roundtime [timespan]");
                p.Message("&HSets how long a round is");
                //p.Message("&T/Parkour set authortime [timespan]");
                p.Message("&HSets the author (challenge) time");
            }
            else
            {
                base.Help(p, message);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Parkour start <map> &H- Starts Parkour");
            p.Message("&T/Parkour stop &H- Stops Parkour");
            p.Message("&T/Parkour end &H- Ends current round of Parkour");
            p.Message("&T/Parkour add/remove &H- Adds/removes current map from map list");
            p.Message("&T/Parkour set [property] &H- Sets a property. See &T/Help Parkour set");
            p.Message("&T/Parkour status &H- Outputs current status of Parkour");
            p.Message("&T/Parkour go &H- Moves you to the current Parkour map");
        }
    }
}
