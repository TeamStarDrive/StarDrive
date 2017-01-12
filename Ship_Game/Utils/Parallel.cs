using System;
using System.Threading;

namespace Ship_Game
{
    public delegate void RangeAction(int start, int end);

    public class ParallelTask : IDisposable
    {
        private AutoResetEvent EvtNewTask = new AutoResetEvent(false);
        private AutoResetEvent EvtEndTask = new AutoResetEvent(false);
        private Thread Thread;
        private RangeAction Task;
        private int LoopStart;
        private int LoopEnd;
        public bool Running => Task != null;
        private volatile bool Killed;

        public ParallelTask(int index)
        {
            Thread = new Thread(Run){ Name = "ParallelTask_"+(index+1) };
        }
        public void Start(int start, int end, RangeAction taskBody)
        {
            if (Task != null)
                throw new InvalidOperationException("ParallelTask is still running");
            Task      = taskBody;
            LoopStart = start;
            LoopEnd   = end;
            EvtNewTask.Set();
            if (!Thread.IsAlive)
                Thread.Start();
        }
        public void Wait()
        {
            while (Task != null)
            {
                // if Wait() times out and worker already stopped, we have a major malfunction here
                if (!EvtEndTask.WaitOne(1000) && Task == null)
                {
                    Log.Error("ParallelTask.Wait timed out while waiting. Potential deadlock issue");
                    return;
                }
            }
        }
        private void Run()
        {
            while (!Killed)
            {
                EvtNewTask.WaitOne();
                if (Task == null)
                    continue;
                try
                {
                    Task(LoopStart, LoopEnd);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ParallelTask unhandled exception");
                }
                Task = null;
                EvtEndTask.Set();
            }
        }
        public void Dispose()
        {
            Destructor();
            GC.SuppressFinalize(this);
        }
        private void Destructor()
        {
            if (EvtNewTask == null)
                return;
            Killed = true;
            Task   = null;
            EvtNewTask.Set();
            Thread.Join(100);

            EvtNewTask.Dispose();
            EvtEndTask.Dispose();
            EvtNewTask = null;
            EvtEndTask = null;
            Thread     = null;
        }
        ~ParallelTask() { Destructor(); }
    }

    public static class Parallel
    {
        private static readonly Array<ParallelTask> Pool = new Array<ParallelTask>();

        public static int PoolSize => Pool.Count;

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

        /// <summary>
        /// Several times faster than System.Threading.Tasks.Parallel.For,
        /// will utilize all cores of the CPU at default affinity.
        /// The range is properly partitioned before threads to avoid false sharing
        /// and process the items in batched to avoid delegate callback overhead
        /// </summary>
        /// <param name="rangeStart">Start of the range (inclusive)</param>
        /// <param name="rangeEnd">End of the range (exclusive)</param>
        /// <param name="body">delegate void RangeAction(int start, int end)</param>
        public static void For(int rangeStart, int rangeEnd, RangeAction body)
        {
            if (rangeStart >= rangeEnd)
                return; // no work done on empty ranges

            int range = rangeEnd - rangeStart;
            int cores = Math.Min(range, Environment.ProcessorCount);
            int len = range / cores;

            // this can happen if the target CPU only has 1 core, or if the list has 1 item
            if (cores == 1)
            {
                body(rangeStart, rangeEnd);
                return;
            }

            var tasks = new ParallelTask[cores];
            lock (Pool)
            {
                int poolIndex = 0;
                int start = rangeStart;
                for (int i = 0; i < cores; ++i, start += len)
                {
                    int end = (i == cores - 1) ? rangeEnd : start + len;

                    ParallelTask task = NextTask(ref poolIndex);
                    tasks[i] = task;
                    task.Start(start, end, body);
                }
            }
            for (int i = 0; i < tasks.Length; ++i)
                tasks[i].Wait();
        }

        public static void For(int rangeLength, RangeAction body)
        {
            For(0, rangeLength, body);
        }
    }
}
