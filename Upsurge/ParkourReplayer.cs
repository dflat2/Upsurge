using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Timers;
using MCGalaxy;
using MCGalaxy.Games;

public class Replayer
{
    public static int replayAccuracy = 10;

    private System.Timers.Timer aTimer;
    PlayerBot p;
    Player owner;
    string level;
    string[] runCoords;
    int runCoordsSize = 0;
    int index = 0;

    public Replayer(Player owner, PlayerBot p, string level)
    {
        this.p = p;
        this.level = level;
        this.owner = owner;
    }

    public class ElapsedEventReceiver : ISynchronizeInvoke
    {
        private Thread m_Thread;
        private BlockingCollection<Message> m_Queue = new BlockingCollection<Message>();

        public ElapsedEventReceiver()
        {
            m_Thread = new Thread(Run);
            m_Thread.Priority = ThreadPriority.Lowest;
            m_Thread.IsBackground = true;
            m_Thread.Start();
        }

        private void Run()
        {
            while (true)
            {
                Message message = m_Queue.Take();
                message.Return = message.Method.DynamicInvoke(message.Args);
                message.Finished.Set();
            }
        }

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            Message message = new Message();
            message.Method = method;
            message.Args = args;
            m_Queue.Add(message);
            return message;
        }

        public object EndInvoke(IAsyncResult result)
        {
            Message message = result as Message;
            if (message != null)
            {
                message.Finished.WaitOne();
                return message.Return;
            }
            throw new ArgumentException("result");
        }

        public object Invoke(Delegate method, object[] args)
        {
            Message message = new Message();
            message.Method = method;
            message.Args = args;
            m_Queue.Add(message);
            message.Finished.WaitOne();
            return message.Return;
        }

        public bool InvokeRequired
        {
            get { return Thread.CurrentThread != m_Thread; }
        }

        private class Message : IAsyncResult
        {
            public Delegate Method;
            public object[] Args;
            public object Return;
            public object State;
            public ManualResetEvent Finished = new ManualResetEvent(false);

            public object AsyncState
            {
                get { return State; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return Finished; }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public bool IsCompleted
            {
                get { return Finished.WaitOne(0); }
            }
        }
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
            ElapsedEventReceiver eventReceiver = new ElapsedEventReceiver();
            aTimer = new System.Timers.Timer(Replayer.replayAccuracy);
            aTimer.SynchronizingObject = eventReceiver;
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
        if (index >= runCoordsSize - 2)
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
            owner.SendCpeMessage(CpeMessageType.Status3, ping);

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
            aTimer.Stop();
            aTimer.Dispose();
        }
        if (p != null)
        {
            PlayerBot.Remove(p, false);
        }
        owner.SendCpeMessage(CpeMessageType.Status3, "");
    }

    ~Replayer()
    {
        StopReplayer();
    }
}