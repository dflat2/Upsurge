using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using MCGalaxy.Blocks.Extended;
using MCGalaxy.Maths;
using MCGalaxy.SQL;

namespace MCGalaxy.Games
{
    public sealed partial class ParkourGame : RoundsGame
    {
        protected List<Player> DoRoundCountdown(int delay)
        {
            while (true)
            {
                RoundStart = DateTime.UtcNow.AddSeconds(delay);
                if (!Running) return null;

                DoCountdown("&4Starting in &f{0} &4seconds", delay, 10);
                if (!Running) return null;

                List<Player> players = GetPlayers();
                if (players.Count >= 1) return players;
            }
        }

        // Initializes a round, but doesn't yet start it
        protected override void DoRound()
        {
            if (!Running) return;
            List<Player> players = DoRoundCountdown(10);
            if (players == null) return;

            if (!Running) return;
            RoundInProgress = true;
            StartRound(players);

            if (!Running) return;
            DoCoreGame();
        }

        void StartRound(List<Player> players)
        {
            TimeSpan duration = Map.Config.RoundTime;
            Map.Message("This round will last for &a" + duration.Shorten(true, true));
            RoundStart = DateTime.UtcNow;

            Player[] online = PlayerInfo.Online.Items;
            // Just to be safe
            foreach (Player pl in online)
            {
                if (pl.level != Map) continue;
                ParkourData data = Get(pl);

                data.ResetState();
                ToggleParkourGates(pl, true);
            }
            RoundEnd = DateTime.UtcNow.Add(duration).AddSeconds(1);
        }

        
        void ToggleParkourGates(Player p, bool on)
        {
            // List of arrays of the form [x, y, z], each message block coordinates with the word /parkourgate in them
            List<String[]> MsgBlocks = Database.GetRows("Messages" + p.level.name, "X, Y, Z", "WHERE lower(Message)=\"/parkourgate\"");
            
            // Turn the gates off (into air)
            if (RoundInProgress && on)
            {
                foreach (String[] s in MsgBlocks)
                {
                    p.SendBlockchange(ushort.Parse(s[0]), ushort.Parse(s[1]), ushort.Parse(s[2]), Block.Air);
                }
            } else
            {
                // Turn the gates on (into whatever it was)
                foreach (String[] s in MsgBlocks)
                {
                    p.RevertBlock(ushort.Parse(s[0]), ushort.Parse(s[1]), ushort.Parse(s[2]));
                }
            }
        }
        

        void DoCoreGame()
        {
            Player[] finished = Finished.Items;
            string lastTimeLeft = null;
            int lastCountdown = -1;

            while ((!AllFinished() || finished.Length == 0) && Running && RoundInProgress)
            {
                // Round ended with some players not finished yet
                int seconds = (int)(RoundEnd - DateTime.UtcNow).TotalSeconds;
                if (seconds <= 0)
                {
                    MessageMap(CpeMessageType.Announcement, ""); return;
                }

                if (seconds <= 5 && seconds != lastCountdown)
                {
                    string suffix = seconds == 1 ? " &4second" : " &4seconds";
                    MessageMap(CpeMessageType.Announcement,
                               "&4Ending in &f" + seconds + suffix);
                    lastCountdown = seconds;
                }

                // Update the round time left shown in the top right
                string timeLeft = GetTimeLeft(seconds);
                if (lastTimeLeft != timeLeft)
                {
                    UpdateAllStatus1();
                    lastTimeLeft = timeLeft;
                }

                Thread.Sleep(200);
                finished = Finished.Items;
            }
        }


        public override void EndRound()
        {
            if (ParkourGame.Instance.Map.Extras.Contains("replayers"))
            {
                foreach (Replayer R in ((List<Replayer>)ParkourGame.Instance.Map.Extras["replayers"]))
                {
                    R.StopReplayer();
                }
                ((List<Replayer>)(ParkourGame.Instance.Map.Extras["replayers"])).Clear();
            }

            if (!RoundInProgress) return;
            RoundInProgress = false;

            foreach (Player p in GetPlayers())
            {
                ParkourData data = Get(p);
                ((Stopwatch)(p.Extras["stopwatch"])).StopTimer();
                ((RunRecorder)(p.Extras["runrecorder"])).StopRecorder();
            }

            var sortedWinners = GetSortedWinners();

            AnnounceWinners(sortedWinners);
            UpdateMapStats();
            UpdatePlayerStats(sortedWinners);
            UpdatePlayerAwards(sortedWinners);
            UpdateRoundRecorder(sortedWinners);
            UpdateDatabase(sortedWinners);

            RoundEnd = DateTime.MinValue;
            UpdateAllStatus1();

            if (!Running) return;
            Map.Message("&aThe game has ended!");

            Map.Config.RoundsPlayed++;

            Map.SaveSettings();

            Finished.Clear();
        }

        IOrderedEnumerable<KeyValuePair<string, Tuple<TimeSpan, UInt16>>> GetSortedWinners()
        {
            // <player name, <the time to get to the finish/furthest checkpoint, the furthest checkpoint reached (+infty for finish blocks)>>
            Dictionary<string, Tuple<TimeSpan, UInt16>> Winners = new Dictionary<string, Tuple<TimeSpan, UInt16>>();

            // Create the winners dictionary first
            foreach (Player p in GetPlayersStarted())
            {
                ParkourData data = Get(p);

                // Get the furthest checkpoint reached
                Checkpoint bestCheckpoint = new Checkpoint(0, DateTime.MinValue);

                foreach (Checkpoint checkpoint in data.Checkpoints)
                {
                    if (bestCheckpoint.num < checkpoint.num)
                    {
                        bestCheckpoint = checkpoint;
                    }
                }
                if (data.Finished)
                {
                    Winners.Add(p.name, Tuple.Create(data.FinishedTime - ((Stopwatch)(p.Extras["stopwatch"])).getStartTime(), UInt16.MaxValue));
                }
                else
                {
                    Winners.Add(p.name, Tuple.Create(bestCheckpoint.time - ((Stopwatch)(p.Extras["stopwatch"])).getStartTime(), bestCheckpoint.num));
                }
            }

            // Sort the dictionary by checkpoint number first, then that checkpoint's time
            var sortedWinners = from entry in Winners orderby entry.Value.Item2 descending, entry.Value.Item1 ascending select entry;
            return sortedWinners;
        }


        void AnnounceWinners(IOrderedEnumerable<KeyValuePair<string, Tuple<TimeSpan, UInt16>>> sortedWinners)
        {
            int place = 1; // 1st, 2nd, 3rd etc. place
            bool areThereWinners = GetPlayersStarted().Any();
            if (areThereWinners) { Map.Message("And the winners are:"); }

            foreach (KeyValuePair<string, Tuple<TimeSpan, UInt16>> entry in sortedWinners)
            {
                TimeSpan RunningTime = entry.Value.Item1;
                string format = @"mm\:ss\.ff";
                string timeStamp = RunningTime.ToString(format).TrimStart('0').TrimStart(':');

                if (entry.Value.Item2 == UInt16.MaxValue)
                {
                    Map.Message(String.Format("{0}. {1} [{2}] - {3}", place.ToString(), entry.Key, "Finish", RunningTime.ToString(format)));
                }
                else
                {
                    Map.Message(String.Format("{0}. {1} [Checkpoint {2}] - {3}", place.ToString(), entry.Key, entry.Value.Item2, RunningTime.ToString(format)));
                }
                place++;
            }
        }

        void UpdateMapStats()
        {
            //Map.SetTotalPeopleWhoFinished(Map.TotalPeopleWhoFinished + Finished.Count);
            //Map.SetTotalPeopleWhoPlayed(Map.TotalPeopleWhoPlayed + GetPlayersStarted().Count);
            Map.Config.SaveFor(Map.name);
        }

        void UpdatePlayerStats(IOrderedEnumerable<KeyValuePair<string, Tuple<TimeSpan, UInt16>>> sortedWinners)
        {
            int index = 0;
            int numberOfPlayers = GetPlayersStarted().Count;

            foreach (KeyValuePair<string, Tuple<TimeSpan, UInt16>> entry in sortedWinners)
            {
                Player p = PlayerInfo.FindExact(entry.Key.ToLower());
                ParkourData data = Get(p);
                string finishString = "";
                string authorTimeString = "";

                int award = numberOfPlayers - index + 3 - 1;    // First prize gets the most, second a bit less etc.
                if (data.Finished)
                {
                    finishString = " and reaching the finish";
                    award += 5;
                }
                //if (data.Finished && (data.FinishedTime - ((Stopwatch)(p.Extras["stopwatch"])).getStartTime() < (TimeSpan)p.level.Extras["authortime"]))
                //{
                //    authorTimeString = " and beating the author time";
                //    award += 5;
                //}

                p.Message("You received &a" + award.ToString() + " &3" + Server.Config.Currency +
                               " &Sfor finishing this round" + finishString + authorTimeString + "!");
                p.SetMoney(p.money + award);

                if (data.Finished)
                {
                    data.CurrentRoundsFinished += 1;
                    data.TotalRoundsFinished += 1;
                    data.MaxRoundsFinished = Math.Max(data.CurrentRoundsFinished, data.MaxRoundsFinished);
                }
                else
                {
                    data.CurrentRoundsFinished = 0;
                }
                data.TotalRounds += 1;

                // We don't update medal statistics if there's too few players, against farming
                if (numberOfPlayers >= 3)
                {
                    switch (index)
                    {
                        case 0:
                            data.GoldMedals += 1;
                            break;
                        case 1:
                            data.SilverMedals += 1;
                            break;
                        case 2:
                            data.BronzeMedals += 1;
                            break;
                    }
                }

                //if (data.Finished && (data.FinishedTime - ((Stopwatch)(p.Extras["stopwatch"])).getStartTime() < (TimeSpan)(p.level.Extras["authortime"])))
                //{
                //    data.AuthorTimes += 1;
                //}

                p.SetPrefix();

                SaveStats(p);

                index++;
            }
        }

        void UpdatePlayerAwards(IOrderedEnumerable<KeyValuePair<string, Tuple<TimeSpan, UInt16>>> sortedWinners)
        {
            foreach (KeyValuePair<string, Tuple<TimeSpan, UInt16>> entry in sortedWinners)
            {
                Player p = PlayerInfo.FindExact(entry.Key.ToLower());
                ParkourAwards.Award(p);
            }
        }

        void UpdateRoundRecorder(IOrderedEnumerable<KeyValuePair<string, Tuple<TimeSpan, UInt16>>> sortedWinners)
        {
            foreach (KeyValuePair<string, Tuple<TimeSpan, UInt16>> entry in sortedWinners)
            {
                Player p = PlayerInfo.FindExact(entry.Key.ToLower());
                List<string[]> playerBest = Database.GetRows("BestTimes", "Checkpoint,FinishTimeMS", "WHERE Player=@0 AND map=@1", p.name, p.level.name);
                if (playerBest.Count == 0)
                {
                    ((RunRecorder)(p.Extras["runrecorder"])).Save();   // Means this is their first time running
                    continue;
                }

                // If the checkpoint reached now is worse than the best checkpoint skip ahead
                if (entry.Value.Item2 < UInt16.Parse(playerBest[0][0]) && entry.Value.Item2 != UInt16.MaxValue)
                {
                    continue;
                }

                // Finally, if the time is better than what came before AND the checkpoint is better save the run

                if (entry.Value.Item1 < TimeSpan.FromMilliseconds(Double.Parse(playerBest[0][1])))
                {
                    p.Message("You've achieved a new all-time personal record!");
                    ((RunRecorder)(p.Extras["runrecorder"])).Save();
                }
            }
        }

        void UpdateDatabase(IOrderedEnumerable<KeyValuePair<string, Tuple<TimeSpan, ushort>>> sortedWinners)
        {
            uint roundID = RoundID.GetRoundID();
            Database.AddRow("Rounds", "ID, map", roundID, Map.name);
            foreach (KeyValuePair<string, Tuple<TimeSpan, UInt16>> entry in sortedWinners)
            {
                Player p = PlayerInfo.FindExact(entry.Key.ToLower());
                Database.AddRow("FinishTimes", "ID, Player, FinishTimeMS, Checkpoint", roundID, p.name, entry.Value.Item1.TotalMilliseconds, entry.Value.Item2);
            }
            RoundID.IncrementRoundID();
        }
    }
}