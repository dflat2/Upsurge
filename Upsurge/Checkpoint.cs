using System;

namespace MCGalaxy.Games
{
    /// <summary>
    ///  Basic data associated with every player
    /// </summary>
    internal sealed class Checkpoint
    {
        public UInt16 num; // Whether it's the nth checkpoint
        public DateTime time;  // When it was reached
        public Checkpoint(UInt16 num, DateTime time)
        {
            this.num = num;
            this.time = time;
        }
    }
}