using System;
using System.Collections.Generic;
using System.Linq;
using MCGalaxy.SQL;

namespace MCGalaxy.Games
{
    public sealed partial class ParkourGame : RoundsGame
    {
        public static ParkourConfig Config = new ParkourConfig();
        public override string GameName { get { return "Parkour"; } }
        public override RoundsGameConfig GetConfig() { return Config; }

        protected override string WelcomeMessage => "&TParkour &Sis running! Type &T/Parkour go &Sto join.";

        private static readonly ParkourGame instance = new ParkourGame();
        static ParkourGame()
        {
        }

        private ParkourGame()
        {
            Picker = new LevelPicker();
        }

        public static ParkourGame Instance
        {
            get
            {
                return instance;
            }
        }

        public DateTime RoundEnd = DateTime.UtcNow;
        public VolatileArray<Player> Finished = new VolatileArray<Player>();

        const string ParkourExtrasKey = "789MCG_PARKOUR_DATA123";
        // Get all parkour data from player p

        internal static ParkourData Get(Player p)
        {
            ParkourData data = TryGet(p);
            if (data != null) return data;
            data = new ParkourData();

            // Database stuff
            ParkourStats s = LoadStats(p.name);
            data.MaxRoundsFinished = s.MaxRoundsFinished; data.TotalRoundsFinished = s.TotalRounds;
            data.BronzeMedals = s.BronzeMedals; data.SilverMedals = s.SilverMedals; data.GoldMedals = s.GoldMedals;

            p.Extras[ParkourExtrasKey] = data;   // Don't really need the key like that, as plugins will not access it

            return data;
        }

        internal static ParkourData TryGet(Player p)
        {
            object data; p.Extras.TryGet(ParkourExtrasKey, out data); return (ParkourData)data;
        }

        public override void UpdateMapConfig() { }

        // Needed this to use this publicly for e.g. toggleVisibility
        public List<Player> PublicGetPlayers()
        {
            return GetPlayers();
        }

        protected override List<Player> GetPlayers()
        {
            Player[] players = PlayerInfo.Online.Items;
            List<Player> playing = new List<Player>();

            foreach (Player pl in players)
            {
                if (pl.level != Map || pl.Game.Referee) continue;
                playing.Add(pl);
            }
            return playing;
        }

        // Gets players who actually crossed a checkpoint
        public List<Player> GetPlayersStarted()
        {
            Player[] players = PlayerInfo.Online.Items;
            List<Player> playersStarted = new List<Player>();

            foreach (Player pl in players)
            {
                ParkourData data = Get(pl);
                if (pl.level != Map || pl.Game.Referee || !(data.Checkpoints.Count > 1)) continue;
                playersStarted.Add(pl);
            }
            return playersStarted;
        }

        public override void OutputStatus(Player p)
        {
            p.Message("{0} out of {1} players have finished", Finished.Count, GetPlayers().Count);
        }

        public override void Start(Player p, string map, int rounds)
        {
            // Parkour starts on current map by default
            if (!p.IsSuper && map.Length == 0) map = p.level.name;
            base.Start(p, map, rounds);
        }

        protected override void StartGame()
        {
            if (!Database.TableExists("ParkourStats"))
            {
                Database.CreateTable("ParkourStats", parkourTable);
            }
            HookStats();
        }

        protected override void EndGame()
        {
            RoundEnd = DateTime.MinValue;
            UnhookStats();

            Finished.Clear();

            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.level != Map) continue;
                ParkourData data = Get(pl);

                data.ResetState();
            }
        }

        public override void PlayerJoinedGame(Player p)
        {
            bool announce = false;
            HandleJoinedLevel(p, Map, Map, ref announce);
        }
        public override void PlayerLeftGame(Player p)
        {
            if (Finished.Contains(p))
            {
                Finished.Remove(p);
            }

            if (p.Extras.Contains("stopwatch"))
            {
                if (p.Extras["stopwatch"] != null)
                {
                    ((Stopwatch)(p.Extras["stopwatch"])).StopTimer();
                }
            }
            if (p.Extras.Contains("runrecorder"))
            {
                if (p.Extras["runrecorder"] != null)
                {
                    ((RunRecorder)(p.Extras["runrecorder"])).StopRecorder();
                }
            }

            p.SendCpeMessage(CpeMessageType.BottomRight1, "");
            p.SendCpeMessage(CpeMessageType.BottomRight2, "");
            p.SendCpeMessage(CpeMessageType.BottomRight3, "");
        }

        // Called when a player crosses a checkpoint
        public void CrossedCheckPoint(Player p, UInt16 num, bool respawn, UInt16 RespawnX, UInt16 RespawnY, UInt16 RespawnZ)
        {
            if (p.Game.Referee) return;

            ParkourData data = Get(p);

            // Initialize stopwatch and runrecorder if it's the first checkpoint
            if (data.Checkpoints.Count() == 0)
            {
                if (!p.Extras.Contains("stopwatch")) {
                    p.Extras["stopwatch"] = new Stopwatch(p);
                }

                if (!p.Extras.Contains("runrecorder"))
                {
                    p.Extras["runrecorder"] = new RunRecorder(p);
                }

                ((Stopwatch)(p.Extras["stopwatch"])).StartTimer(DateTime.UtcNow);
                ((RunRecorder)(p.Extras["runrecorder"])).StartRecorder();
            }

            // Check if checkpoint wasn't already reached
            foreach (Checkpoint checkpoint in data.Checkpoints)
            {
                if (checkpoint.num == num)
                {
                    return; // This means the checkpoint was already reached, so ignore it
                    // TODO: Make this compatible with circular maps
                }
            }

            Checkpoint newCheckpoint = new Checkpoint(num, DateTime.UtcNow);
            data.Checkpoints.Add(newCheckpoint);
            p.Message(String.Format("You reached checkpoint {0}!", num));

            if (respawn == true)
            {
                p.useCheckpointSpawn = true;
                p.checkpointX = RespawnX; p.checkpointY = (ushort)(RespawnY + 1); p.checkpointZ = RespawnZ;
                p.checkpointRotX = p.Rot.RotY; p.checkpointRotY = p.Rot.HeadX;

                Position pos = new Position((int)RespawnX, (int)RespawnY, (int)RespawnZ);
                if (p.Supports(CpeExt.SetSpawnpoint))
                {
                    p.Session.SendSetSpawnpoint(pos, p.Rot);
                }
                else
                {
                    p.SendPos(Entities.SelfID, pos, p.Rot);
                    Entities.Spawn(p, p);
                }
                p.Message("Spawnpoint updated");
            }
        }

        public void CrossedFinish(Player p)
        {
            if (p.Game.Referee) return;

            ParkourData data = Get(p);
            if (!data.Finished)
            {
                data.FinishedTime = DateTime.UtcNow;
                p.Message(String.Format("You reached the finish!"));

                ((Stopwatch)(p.Extras["stopwatch"])).StopTimer();
                ((RunRecorder)(p.Extras["runrecorder"])).StopRecorder();

                TimeSpan pTime = data.FinishedTime - ((Stopwatch)(p.Extras["stopwatch"])).GetStartTime();
                string format = @"mm\:ss\.ff";
                string timeStamp = pTime.ToString(format).TrimStart('0').TrimStart(':');

                p.Message(String.Format("You finished with a time of {0}", timeStamp));

                data.Finished = true;
                Finished.Add(p);
                UpdateAllStatus1();
            }

            if (AllFinished())
            {
                EndRound();
            }
        }

        // TODO: This won't work quite well with circular parkour
        public bool AllFinished()
        {
            foreach (Player p in GetPlayers())
            {
                ParkourData data = Get(p);

                // Ignore players who have not activated any checkpoint
                if (!(data.Checkpoints.Count > 1))
                {
                    continue;
                }

                if (!data.Finished)
                {
                    return false;
                }
            }
            return true;
        }

        static string GetTimeLeft(int seconds)
        {
            if (seconds < 0) return "";
            if (seconds <= 10) return "10s left";
            if (seconds <= 30) return "30s left";
            if (seconds <= 60) return "1m left";
            return ((seconds + 59) / 60) + "m left";
        }

        public bool SetQueuedLevel(Player p, string name)
        {
            string map = Matcher.FindMaps(p, name);
            if (map == null) return false;

            p.Message(map + " was queued.");
            Picker.QueuedMap = map.ToLower();

            if (Map != null) Map.Message(map + " was queued as the next map.");
            return true;
        }
        protected override string FormatStatus1(Player p)
        {
            int left = (int)(RoundEnd - DateTime.UtcNow).TotalSeconds;
            string timespan = GetTimeLeft(left);

            string format = timespan.Length == 0 ? "{0} players finished (map: {1})" :
                "{0} players finished (map: {1}) - {2}";
            return string.Format(format, Finished.Count, Map.MapName, timespan);
        }

        // Usually reserved for map type
        protected override string FormatStatus2(Player p)
        {
            return "";
        }

        // Ditto... Usually reserved for money info
        protected override string FormatStatus3(Player p)
        {
            return "";
        }

    }
}