using System.Collections.Generic;
using System.IO;
using MCGalaxy.Config;

namespace MCGalaxy.Games
{
    public sealed class ParkourConfig : RoundsGameConfig
    {
        [ConfigFloat("parkour-max-move-distance", "Parkour", 1.5625f)]
        public float MaxMoveDist = 1.5625f;
        [ConfigString("finished-tablist-group", "Parkour", "&cFinished")]
        public string ParkourTabListGroup = "&cFinished";

        static ConfigElement[] cfg;
        public override bool AllowAutoload { get { return true; } }
        protected override string GameName { get { return "Parkour"; } }

        public ParkourConfig() { 
            Path = "properties/parkour.properties";
        }

        public override void Save()
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(ParkourConfig));

            using (StreamWriter w = new StreamWriter(Path))
            {
                ConfigElement.Serialise(cfg, w, this);
            }
        }

        public override void Load()
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(ParkourConfig));
            PropertiesFile.Read(Path, ProcessConfigLine);
        }

        void ProcessConfigLine(string key, string value)
        {
            if (key.CaselessEq("parkour-levels-list"))
            {
                Maps = new List<string>(value.SplitComma());
            }
            else
            {
                ConfigElement.Parse(cfg, this, key, value);
            }
        }
    }
}
