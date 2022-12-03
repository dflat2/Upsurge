using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using MCGalaxy;
using MCGalaxy.Games;

public class Replayer
{
    public static int replayAccuracy = 10;

    private System.Timers.Timer aTimer;
    readonly PlayerBot p;
    readonly Player owner;
    readonly string level;
    string[] runCoords;
    int runCoordsSize = 0;
    int index = 0;
    readonly bool showPing;

    public Replayer(Player owner, PlayerBot p, string level, bool showPing)
    {
        this.p = p;
        this.level = level;
        this.owner = owner;
        this.showPing = showPing;
    }

    public void StartReplayer()
    {
        if (GetRunCoords() && (ParkourGame.Instance.RoundInProgress == true || p.level != ParkourGame.Instance.Map))
        {
            if (!p.level.Extras.Contains("replayers"))
            {
                p.level.Extras["replayers"] = new List<Replayer>();
            }

            ((List<Replayer>)(p.level.Extras["replayers"])).Add(this);
            aTimer = new System.Timers.Timer(Replayer.replayAccuracy);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            aTimer.Start();
        }
        else
        {
            StopReplayer();
        }
    }

    private void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        if (index >= runCoordsSize - 3)
        {
            StopReplayer();
        }
        else
        {
            string[] Pcoords = runCoords[index].Split(',');
            Position pos = new Position(ushort.Parse(Pcoords[0]), ushort.Parse(Pcoords[1]), ushort.Parse(Pcoords[2]));
            p.Pos = pos;

            string[] Rcoords = runCoords[index + 1].Split(',');
            Orientation rot = new Orientation(byte.Parse(Rcoords[1]), byte.Parse(Rcoords[3]));
            rot.RotX = byte.Parse(Rcoords[0]);
            rot.RotZ = byte.Parse(Rcoords[2]);
            p.Rot = rot;

            string ping = runCoords[index + 2];
            if (showPing)
            {
                owner.SendCpeMessage(CpeMessageType.Status3, ping);
            }
            index += 3;
        }
    }

    public bool GetRunCoords()
    {
        if (!File.Exists(String.Format("RunRecorder/{0}+{1}.txt", p.name, level)))
        {
            owner.Message("Couldn't find runrecorder file. Did this player ever play this level?");
            return false;
        }
        else
        {
            runCoords = File.ReadAllLines(String.Format("RunRecorder/{0}+{1}.txt", p.name, level));
            runCoordsSize = runCoords.Length;
            return true;
        }
    }

    public void StopReplayer()
    {
        if (aTimer != null)
        {
            aTimer.Elapsed -= OnTimedEvent;
            aTimer.Stop();
            aTimer.Dispose();
        }
        if (p != null)
        {
            PlayerBot.Remove(p, false);
        }
        if (showPing)
        {
            owner.SendCpeMessage(CpeMessageType.Status3, "");
        }
    }

    ~Replayer()
    {
        StopReplayer();
    }
}