using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Timers;
using MCGalaxy;

public class Stopwatch
{
    static int timerAccuracy = 47;

    string format = @"mm\:ss\.ff";
    private System.Timers.Timer aTimer;
    DateTime startTime;
    Player p;
    public Stopwatch(Player p)
    {
        this.p = p;
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

    public void StartTimer(DateTime startTime) // startTime can be the round start, OR when a player joined the map
    {
        ElapsedEventReceiver eventReceiver = new ElapsedEventReceiver();
        this.startTime = startTime;
        aTimer = new System.Timers.Timer(Stopwatch.timerAccuracy);
        aTimer.SynchronizingObject = eventReceiver;
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
            aTimer.Stop();
            aTimer.Dispose();
        }
        p.SendCpeMessage(CpeMessageType.BottomRight2, "");
    }

    public DateTime getStartTime()
    {
        return this.startTime;
    }

    ~Stopwatch()
    {
        StopTimer();
    }
}