using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using MCGalaxy;

public class RunRecorder
{
    private System.Timers.Timer aTimer;
    readonly Player p;
    List<string> runCoords = new List<string>();
    public RunRecorder(Player p)
    {
        this.p = p;
    }

    public void StartRecorder()
    {
        aTimer = new System.Timers.Timer(Replayer.replayAccuracy);
        aTimer.Elapsed += OnTimedEvent;
        aTimer.AutoReset = true;
        aTimer.Enabled = true;
        aTimer.Start();
    }

    private void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        Position P = p.Pos;
        Orientation R = p.Rot;
        runCoords.Add(String.Format("{0}, {1}, {2}", P.X.ToString(), P.Y.ToString(), P.Z.ToString()));
        runCoords.Add(String.Format("{0}, {1}, {2}, {3}", R.RotX.ToString(), R.RotY.ToString(), R.RotZ.ToString(), R.HeadX.ToString()));
        runCoords.Add(p.Session.Ping.Format());
    }

    public void Save()
    {
        if (!Directory.Exists("RunRecorder"))
        {
            Directory.CreateDirectory("RunRecorder");
        }
        File.WriteAllLines(String.Format("RunRecorder/{0}{1}.txt", p.name, p.level.name), runCoords);
    }

    public void StopRecorder()
    {
        if (aTimer != null)
        {
            aTimer.Elapsed -= OnTimedEvent;
            aTimer.Stop();
            aTimer.Dispose();
        }
    }

    ~RunRecorder()
    {
        StopRecorder();
    }
}