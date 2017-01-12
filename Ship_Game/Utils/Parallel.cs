using System;
using System.Threading;

namespace Ship_Game
{
    public delegate void RangeAction(int start, int end);

    public class ParallelTask : IDisposable
    {
        public AutoResetEvent Event = new AutoResetEvent(true);
        private Thread Thread;
        private RangeAction Task;
        private int LoopStart;
        private int LoopEnd;
        public bool Running { get; private set; }
        private volatile bool Killed;

        public ParallelTask(int index)
        {
            Thread = new Thread(Run){ Name = "ParallelTask_"+index };
        }

        public void Start(int start, int end, RangeAction taskBody)
        {
            Task      = taskBody;
            LoopStart = start;
            LoopEnd   = end;
            Running   = true;
            Event.Set();
            if (!Thread.IsAlive)
                Thread.Start();
        }

        public void Wait()
        {
            while (Running) Event.WaitOne();
        }

        private void Run()
        {
            while (!Killed)
            {
                Event.WaitOne();
                if (Task == null)
                    continue;
                try
                {
                    RangeAction task = Task;
                    Task = null;
                    task(LoopStart, LoopEnd);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ParallelTask unhandled exception");
                }
                Running = false;
                Event.Set();
            }
        }

        public void Dispose()
        {
            Destructor();
            GC.SuppressFinalize(this);
        }
        private void Destructor()
        {
            if (Event == null)
                return;
            Killed = true;
            Event.Set();
            Thread.Join(100);

            Event?.Dispose();
            Event  = null;
            Thread = null;
            Task   = null;
        }
        ~ParallelTask() { Destructor(); }
    }

    public static class Parallel
    {
        private static readonly Array<ParallelTask> Pool = new Array<ParallelTask>();

        private static ParallelTask NextTask(ref int poolIndex)
        {
            for (; poolIndex < Pool.Count; ++poolIndex)
            {
                ParallelTask task = Pool[poolIndex];
                if (!task.Running) return task;
            }
            var newTask = new ParallelTask(Pool.Count);
            Pool.Add(newTask);
            return newTask;
        }

        public static void For(int rangeStart, int rangeEnd, RangeAction body)
        {
            int range = rangeEnd - rangeStart;
            int cores = Math.Min(range, Environment.ProcessorCount);
            int len = range / cores;

            var tasks = new ParallelTask[cores];
            lock (Pool)
            {
                int poolIndex = 0;
                for (int i = 0; i < cores; ++i)
                {
                    int start = i * len;
                    int end = i == cores - 1 ? rangeEnd : start + len;

                    ParallelTask task = NextTask(ref poolIndex);
                    tasks[i] = task;
                    task.Start(start, end, body);
                }
            }
            for (int i = 0; i < tasks.Length; ++i)
                tasks[i].Wait();
        }
    }
}
