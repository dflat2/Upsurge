/*
    Copyright 2015 MCGalaxy

    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.osedu.org/licenses/ECL-2.0
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using System.Threading;

namespace MCGalaxy.Games
{
    public class ParkourLevelPicker : LevelPicker
    {
        public new string QueuedMap;
        public new List<string> RecentMaps = new List<string>();
        public new int VoteTime = 20;
        public new bool Voting;

        internal string Candidate1 = "", Candidate2 = "", Candidate3 = "";
        internal int Votes1, Votes2, Votes3;
        const int minMaps = 3;

        public new void AddRecentMap(string map)
        {
            if (RecentMaps.Count >= 20)
                RecentMaps.RemoveAt(0);
            RecentMaps.Add(map);
        }

        public new void Clear()
        {
            QueuedMap = null;
            RecentMaps.Clear();
        }

        public new string ChooseNextLevel(RoundsGame game)
        {
            if (QueuedMap != null) return QueuedMap;

            try
            {
                List<string> maps = GetCandidateMaps(game);
                if (maps == null) return null;

                RemoveRecentLevels(maps);
                Votes1 = 0; Votes2 = 0; Votes3 = 0;

                Random r = new Random();
                Candidate1 = GetRandomMap(r, maps);
                Candidate2 = GetRandomMap(r, maps);
                Candidate3 = GetRandomMap(r, maps);

                if (!game.Running) return null;
                DoLevelVote(game);
                if (!game.Running) return null;

                return NextLevel(r, maps);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return null;
            }
        }

        void RemoveRecentLevels(List<string> maps)
        {
            // Try to avoid recently played levels, avoiding most recent
            List<string> recent = RecentMaps;
            for (int i = recent.Count - 1; i >= 0; i--)
            {
                if (maps.Count > minMaps) maps.CaselessRemove(recent[i]);
            }

            // Try to avoid maps voted last round if possible
            if (maps.Count > minMaps) maps.CaselessRemove(Candidate1);
            if (maps.Count > minMaps) maps.CaselessRemove(Candidate2);
            if (maps.Count > minMaps) maps.CaselessRemove(Candidate3);
        }

        void DoLevelVote(IGame game)
        {
            Voting = true;
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.level != game.Map) continue;
                SendVoteMessage(pl);
            }

            VoteCountdown(game);
            Voting = false;
        }

        void VoteCountdown(IGame game)
        {
            // Show message for non-CPE clients
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.level != game.Map || pl.Supports(CpeExt.MessageTypes)) continue;
                pl.Message("You have " + VoteTime + " seconds to vote for the next map");
            }

            Level map = game.Map;
            for (int i = 0; i < VoteTime; i++)
            {
                players = PlayerInfo.Online.Items;
                if (!game.Running) break;

                foreach (Player pl in players)
                {
                    if (pl.level != map || !pl.Supports(CpeExt.MessageTypes)) continue;
                    string timeLeft = "&e" + (VoteTime - i) + "s &Sleft to vote";
                    pl.SendCpeMessage(CpeMessageType.BottomRight1, timeLeft);
                }
                Thread.Sleep(1000);
            }

            players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.level == map) ResetVoteMessage(pl);
            }
        }

        string NextLevel(Random r, List<string> levels)
        {
            Player[] online = PlayerInfo.Online.Items;
            foreach (Player pl in online) pl.voted = false;

            if (Votes3 > Votes1 && Votes3 > Votes2)
            {
                return Candidate3;
            }
            else if (Votes1 >= Votes2)
            {
                return Candidate1;
            }
            else
            {
                return Candidate2;
            }
        }

        internal static string GetRandomMap(Random r, List<string> maps)
        {
            int i = r.Next(0, maps.Count);
            string map = maps[i];
            maps.RemoveAt(i);
            return map;
        }

        public new virtual List<string> GetCandidateMaps(RoundsGame game)
        {
            List<string> maps = new List<string>(game.GetConfig().Maps);
            if (maps.Count < minMaps)
            {
                Logger.Log(LogType.Warning, "You must have 3 or more maps configured to change levels in " + game.GameName);
                return null;
            }
            return maps;
        }

        public new void SendVoteMessage(Player p)
        {
            const string line1 = "&eLevel vote - type &a1&e, &b2&e or &c3";
            string line2 = "&a" + Candidate1 + "&e, &b" + Candidate2 + "&e, &c" + Candidate3;

            if (p.Supports(CpeExt.MessageTypes))
            {
                p.SendCpeMessage(CpeMessageType.BottomRight3, line1);
                p.SendCpeMessage(CpeMessageType.BottomRight2, line2);
            }
            else
            {
                p.Message(line1);
                p.Message(line2);
            }
        }

        public new void ResetVoteMessage(Player p)
        {
            p.SendCpeMessage(CpeMessageType.BottomRight3, "");
            p.SendCpeMessage(CpeMessageType.BottomRight2, "");
            p.SendCpeMessage(CpeMessageType.BottomRight1, "");
        }

        public new bool HandlesMessage(Player p, string message)
        {
            if (!Voting) return false;

            return
                CheckVote(message, p, "1", "one", ref Votes1, ref Votes2, ref Votes3) ||
                CheckVote(message, p, "2", "two", ref Votes1, ref Votes2, ref Votes3) ||
                CheckVote(message, p, "3", "three", ref Votes1, ref Votes2, ref Votes3);
        }

        internal static bool CheckVote(string msg, Player p, string a, string b, ref int votes1, ref int votes2, ref int votes3)
        {
            if (!(msg.CaselessEq(a) || msg.CaselessEq(b))) return false;
            p.Message("&aThanks for voting!");
            p.voted = true;

            if (!p.Extras.Contains("vote"))
            {
                p.Extras["vote"] = (int)0;
            }
            // Subtract from the previous vote
            switch ((int)(p.Extras["vote"]))
            {
                case 0:     // Not voted yet (default p.vote value)
                    break;
                case 1:
                    votes1--;
                    break;
                case 2:
                    votes2--;
                    break;
                case 3:
                    votes3--;
                    break;
            }

            // Add to current vote
            p.Extras["vote"] = int.Parse(a);
            switch ((int)(p.Extras["vote"]))
            {
                case 1:
                    votes1++;
                    break;
                case 2:
                    votes2++;
                    break;
                case 3:
                    votes3++;
                    break;
            }
            return true;
        }
    }
}
