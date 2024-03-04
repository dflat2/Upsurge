using System;
using System.Collections.Generic;

namespace MCGalaxy.Games
{
    internal sealed class ParkourData
    {
        public bool Finished = false;
        public bool VisibilityToggled = false;

        public List<Checkpoint> Checkpoints = new List<Checkpoint>();
        public DateTime FinishedTime;

        public UInt16 laps = 0;     // Specifically for circular parkour

        /* TotalRoundsFinished = total rounds finished across all time
         * MaxRoundsFinished = maximum streak across all time
         * CurrentRoundsFinished = current streak of rounds finished
         */
        public int TotalRounds;
        public int TotalRoundsFinished, MaxRoundsFinished, CurrentRoundsFinished;
        public int BronzeMedals, SilverMedals, GoldMedals;
        public int AuthorTimes;             // Total author times beaten

        // Called when the round ends
        public void ResetState()
        {
            Finished = false;
            FinishedTime = DateTime.MinValue;
            Checkpoints.Clear();
            VisibilityToggled = false;  // Note that players are "respawned" when the new map is sent
        }
    }
}