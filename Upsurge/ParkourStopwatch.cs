using System;
using System.Timers;
using MCGalaxy;
using MCGalaxy.Games;

namespace MCGalaxy
{
    public class Stopwatch
    {
        static readonly int timerAccuracy = 47;
        readonly string format = @"mm\:ss\.ff";
        private System.Timers.Timer aTimer;
        DateTime startTime;
        readonly Player p;

        public Stopwatch(Player p)
        {
            this.p = p;
        }

        public void StartTimer(DateTime startTime) // startTime can be the round start, OR when a player joined the map
        {
            this.startTime = startTime;
            aTimer = new System.Timers.Timer(Stopwatch.timerAccuracy);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            aTimer.Start();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            TimeSpan interval = e.SignalTime - this.startTime;
            p.SendCpeMessage(CpeMessageType.BottomRight2, String.Format("&eCurrent Time: &c{0}",
                interval.ToString(format)
                .TrimStart('0')
                .TrimStart(':')
                .Replace('.', ':')));
        }

        public void StopTimer()
        {
            if (aTimer != null)
            {
                aTimer.Elapsed -= OnTimedEvent;
                aTimer.Stop();
                aTimer.Dispose();
            }
            p.SendCpeMessage(CpeMessageType.BottomRight2, "");
        }

        public DateTime GetStartTime()
        {
            return this.startTime;
        }

        ~Stopwatch()
        {
            StopTimer();
        }
    }
}
