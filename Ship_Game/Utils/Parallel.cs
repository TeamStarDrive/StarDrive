using System;
using System.Management;
using System.Threading;

namespace Ship_Game
{
    public delegate void RangeAction(int start, int end);

    public class ParallelTask : IDisposable
    {
        private AutoResetEvent EvtNewTask = new AutoResetEvent(false);
        private AutoResetEvent EvtEndTask = new AutoResetEvent(false);
        private Thread Thread;
        private Action SimpleTask;
        private RangeAction RangeTask;
        private int LoopStart;
        private int LoopEnd;
        public bool Running => RangeTask != null || SimpleTask != null;
        private Exception Error;
        private volatile bool Killed;

        public ParallelTask(int index)
        {
            Thread = new Thread(Run){ Name = "ParallelTask_"+(index+1) };
        }
        public void Start(int start, int end, RangeAction taskBody)
        {
            if (Running)
                throw new InvalidOperationException("ParallelTask is still running");
            RangeTask = taskBody;
            LoopStart = start;
            LoopEnd   = end;
            EvtNewTask.Set();
            if (!Thread.IsAlive)
                Thread.Start();
        }
        public void Start(Action taskBody)
        {
            if (Running)
                throw new InvalidOperationException("ParallelTask is still running");
            SimpleTask = taskBody;
            EvtNewTask.Set();
            if (!Thread.IsAlive)
                Thread.Start();
        }
        public Exception Wait()
        {
            while (Running)
            {
                EvtEndTask.WaitOne();
            }
            if (Error == null)
                return null;
            Exception ex = Error; // propagate captured exceptions
            Error = null;
            return ex;
        }
        private void Run()
        {
            while (!Killed)
            {
                EvtNewTask.WaitOne();
                if (!Running)
                    continue;
                try
                {
                    Error = null;
                    if (RangeTask != null)
                        RangeTask(LoopStart, LoopEnd);
                    else
                        SimpleTask?.Invoke();
                }
                catch (Exception ex)
                {
                    Error = ex;
                }
                RangeTask  = null;
                SimpleTask = null;
                if (Killed)
                    break;
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
            RangeTask  = null;
            SimpleTask = null;
            EvtNewTask.Set();
            Thread.Join(100);

            EvtNewTask.Dispose();
            EvtEndTask.Dispose();
            EvtNewTask = null;
            EvtEndTask = null;
            Thread     = null;
            Error      = null;
        }
        ~ParallelTask() { Destructor(); }
    }

    public static class Parallel
    {
        public static void ClearPool()
        {
            lock (Pool) Pool.ClearAndDispose();
        }

        private static readonly Array<ParallelTask> Pool = new Array<ParallelTask>();
        private static readonly bool Initialized = InitThreadPool();

        private static bool InitThreadPool()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => ClearPool();
            return true;
        }

        /// <summary>TRUE if another Parallel.For loop is already running </summary>
        public static bool Running { get; private set; }
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

        private static int PhysicalCoreCount;
        public static int NumPhysicalCores
        {
            get
            {
                if (PhysicalCoreCount == 0)
                {
                    var results = new ManagementObjectSearcher("Select NumberOfCores from Win32_Processor").Get();
                    if (results.Count > 0)
                    {
                        foreach (var item in results)
                        {
                            PhysicalCoreCount = (int)(uint)item["NumberOfCores"];
                            break;
                        }
                    }
                    else
                    {
                        // Query failed, so assume HT or SMT is enabled
                        PhysicalCoreCount = Environment.ProcessorCount / 2;
                    }
                }
                return PhysicalCoreCount;
            }
        }

        /// <summary>
        /// Several times faster than System.Threading.Tasks.Parallel.For,
        /// will utilize all cores of the CPU at default affinity.
        /// Ranges are properly partitioned to avoid false sharing
        /// and process the items in batched to avoid delegate callback overhead
        /// 
        /// Parallel.For loops are forbidden (ThreadStateException)
        /// 
        /// In case of ParallelTasks encountering an exception, the first captured exception
        /// will be thrown once ALL loop branches are complete.
        /// </summary>
        /// <exception cref="ThreadStateException">NESTED Parallel.For loops are forbidden!</exception>
        /// <param name="rangeStart">Start of the range (inclusive)</param>
        /// <param name="rangeEnd">End of the range (exclusive)</param>
        /// <param name="body">delegate void RangeAction(int start, int end)</param>
        /// <param name="parallelism">Number of threads to spawn. By default number of physical cores is used.</param>
        /// <example>
        /// Parallel.For(0, arr.Length, (start, end) =>
        /// {
        ///     for (int i = start; i < end; i++)
        ///     {
        ///         var elem = arr[i];
        ///     }
        /// });
        /// </example>
        public static void For(int rangeStart, int rangeEnd, RangeAction body, int parallelism = 0)
        {
            if (rangeStart >= rangeEnd)
                return; // no work done on empty ranges

            int range = rangeEnd - rangeStart;
            int cores = Math.Min(range, Math.Max(NumPhysicalCores, parallelism));
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
                // Rationale: if you nest parallel for loops, you will spawn a huge number of threads
                // and thus killing off any performance gains. For example on a 6-core cpu it would spawn
                // 6*6 = 36 threads !!. Adjust your algorithms to prevent parellel loop nesting.
                if (Running)
                    throw new ThreadStateException("Another Parallel.For loop is already running. Nested Parallel.For loops are forbidden");
                Running = true;
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

            Exception ex = null; // only store a single exception
            for (int i = 0; i < tasks.Length; ++i)
            {
                Exception e = tasks[i].Wait();
                if (e != null && ex == null) ex = e;
            }
            lock (Pool) Running = false;

            // from the first ParallelTask that threw an exception:
            if (ex != null)
                throw ex;
        }

        public static void For(int rangeLength, RangeAction body, int parallelism = 0)
        {
            For(0, rangeLength, body, parallelism);
        }

        public static ParallelTask Run(Action action)
        {
            int poolIndex = 0;
            ParallelTask task = NextTask(ref poolIndex);
            task.Start(action);
            return task;
        }
    }
}
