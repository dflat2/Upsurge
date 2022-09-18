using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Timers;
using MCGalaxy;
using MCGalaxy.Maths;

public class RunRecorder
{
    private System.Timers.Timer aTimer;
    Player p;
    List<string> runCoords = new List<string>();
    public RunRecorder(Player p)
    {
        this.p = p;
    }

    private class ElapsedEventReceiver : ISynchronizeInvoke
    {
        private Thread m_Thread;
        private BlockingCollection<Message> m_Queue = new BlockingCollection<Message>();

        public ElapsedEventReceiver()
        {
            m_Thread = new Thread(Run);
            m_Thread.Priority = ThreadPriority.BelowNormal;
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

    public void StartRecorder()
    {
        ElapsedEventReceiver eventReceiver = new ElapsedEventReceiver();
        aTimer = new System.Timers.Timer(Replayer.replayAccuracy);
        aTimer.SynchronizingObject = eventReceiver;
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
        File.WriteAllLines(String.Format("RunRecorder/{0}+{1}.txt", p.name, p.level.name), runCoords);
    }

    public void StopRecorder()
    {
        if (aTimer != null)
        {
            aTimer.Stop();
            aTimer.Dispose();
        }
    }

    ~RunRecorder()
    {
        StopRecorder();
    }
}