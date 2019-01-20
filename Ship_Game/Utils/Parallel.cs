using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading;

namespace Ship_Game
{
    public delegate void RangeAction(int start, int end);

    public interface ITaskResult
    {
        bool IsComplete { get; }
        void SetResult(object value);
    }

    public class TaskResult : ITaskResult
    {
        public bool IsComplete { get; private set; }
        // @note Task has to check this value itself and cancel manually
        public bool IsCancelRequested { get; private set; }
        readonly ManualResetEvent Finished = new ManualResetEvent(false);

        void ITaskResult.SetResult(object value)
        {
            IsComplete = true;
            Finished.Set();
        }

        // wait until task has finished
        public void Wait(int millisecondTimeout=-1)
        {
            if (!IsComplete)
                Finished.WaitOne(millisecondTimeout);
        }

        public void CancelAndWait(int millisecondTimeout=-1)
        {
            IsCancelRequested = true;
            Wait(millisecondTimeout);
        }
    }

    public class TaskResult<T> : ITaskResult
    {
        public T Result { get; private set; }
        public bool IsComplete { get; private set; }
        // @note Task has to check this value itself and cancel manually
        public bool IsCancelRequested { get; private set; }
        readonly ManualResetEvent Finished = new ManualResetEvent(false);

        void ITaskResult.SetResult(object value)
        {
            Result = (T)value;
            IsComplete = true;
            Finished.Set();
        }

        // wait until task has finished
        public void Wait(int millisecondTimeout=-1)
        {
            if (!IsComplete)
                Finished.WaitOne(millisecondTimeout);
        }

        public void CancelAndWait(int millisecondTimeout=-1)
        {
            IsCancelRequested = true;
            Wait(millisecondTimeout);
        }
    }

    public class ParallelTask : IDisposable
    {
        AutoResetEvent EvtNewTask = new AutoResetEvent(false);
        AutoResetEvent EvtEndTask = new AutoResetEvent(false);
        readonly object KillSync = new object();
        Thread Thread;
        Action VoidTask;
        Func<object> ResultTask;
        ITaskResult Result;
        RangeAction RangeTask;
        int LoopStart, LoopEnd;
        Stopwatch IdleTimer;
        Exception Error;
        volatile bool Killed;
        readonly string Name;

        public bool Running => RangeTask != null || VoidTask != null || ResultTask != null;
        public int ThreadId => Thread.ManagedThreadId;

        public ParallelTask(int index)
        {
            Name = "ParallelTask_"+(index+1);
            Thread = new Thread(Run){ Name = Name };
        }
        void TriggerTaskStart()
        {
            lock (KillSync) // we need to sync here, because background thread can auto-terminate
            {
                EvtNewTask.Set();
                if (Thread == null)
                    Thread = new Thread(Run) { Name = Name };
                if (!Thread.IsAlive)
                    Thread.Start();
            }
        }
        public void Start(int start, int end, RangeAction taskBody)
        {
            if (Running)
                throw new InvalidOperationException("ParallelTask is still running");
            RangeTask = taskBody;
            LoopStart = start;
            LoopEnd   = end;
            TriggerTaskStart();
        }
        public void Start(Action taskBody, ITaskResult result)
        {
            if (Running)
                throw new InvalidOperationException("ParallelTask is still running");
            VoidTask = taskBody;
            Result = result;
            TriggerTaskStart();
        }
        public void Start(Func<object> taskBody, ITaskResult result)
        {
            if (Running)
                throw new InvalidOperationException("ParallelTask is still running");
            ResultTask = taskBody;
            Result = result;
            TriggerTaskStart();
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
        void SetResultValue(object value)
        {
            ITaskResult result = Result;
            if (result == null) return;
            Result = null; // so if SetResult fails, we don't crash twice
            result.SetResult(value);
        }
        void Run()
        {
            while (!Killed)
            {
                IdleTimer = Stopwatch.StartNew();
                EvtNewTask.WaitOne(5000);
                if (!Running) { // no tasks
                    lock (KillSync) { // lock before deciding to kill thread
                        if (IdleTimer.ElapsedMilliseconds > 5000) {
                            Thread = null; // Die!
                            return;
                        }
                    }
                    continue;
                }
                try
                {
                    Error = null;
                    if (RangeTask != null)
                    {
                        RangeTask(LoopStart, LoopEnd);
                    }
                    else if (VoidTask != null)
                    {
                        VoidTask.Invoke();
                        SetResultValue(null);
                    }
                    else if (ResultTask != null)
                    {
                        object value = ResultTask.Invoke();
                        SetResultValue(value);
                    }
                }
                catch (Exception ex)
                {
                    Error = ex;
                    SetResultValue(null);
                }
                RangeTask = null;
                VoidTask  = null;
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
        void Destructor() // Manually controlling result lifetimes
        {
            if (EvtNewTask == null)
                return;
            Killed     = true;
            RangeTask  = null;
            VoidTask   = null;
            ResultTask = null;
            Result     = null;
            EvtNewTask.Set();
            Thread?.Join(100);

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

        static readonly Array<ParallelTask> Pool = new Array<ParallelTask>(32);
        static readonly bool Initialized = InitThreadPool();
        static readonly Map<int, bool> PForProtected = new Map<int, bool>();

        static bool InitThreadPool()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => ClearPool();
            return true;
        }

        /// <summary>TRUE if another Parallel.For loop is already running on THIS thread </summary>
        public static bool ShouldNotLaunchPFor 
            => PForProtected.TryGetValue(Thread.CurrentThread.ManagedThreadId, out bool running) && running;

        static void MarkProtected(int threadId, bool @protected) => PForProtected[threadId] = @protected;

        public static int PoolSize => Pool.Count;

        static ParallelTask NextTask(ref int poolIndex)
        {
            lock (Pool)
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
        }

        static int PhysicalCoreCount;
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
        /// Parallel.For(0, arr.Length, (start, end) =&gt;
        /// {
        ///     for (int i = start; i &lt; end; i++)
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
            int cores = Math.Min(range, parallelism <= 0 ? NumPhysicalCores : Math.Min(NumPhysicalCores, parallelism));
            int len = range / cores;

            // this can happen if the target CPU only has 1 core, or if the list has 1 item
            if (cores == 1)
            {
                body(rangeStart, rangeEnd);
                return;
            }

            var tasks = new ParallelTask[cores];
            lock (PForProtected)
            {
                // Rationale: if you nest parallel for loops, you will spawn a huge number of threads
                // and thus killing off any performance gains. For example on a 6-core cpu it would spawn
                // 6*6 = 36 threads !!. Adjust your algorithms to prevent parallel loop nesting.
                if (ShouldNotLaunchPFor)
                    throw new ThreadStateException("Another Parallel.For loop is already running. Nested Parallel.For loops are forbidden");
                MarkProtected(Thread.CurrentThread.ManagedThreadId, true);
            }

            int poolIndex = 0;
            int start = rangeStart;
            for (int i = 0; i < cores; ++i, start += len)
            {
                int end = (i == cores - 1) ? rangeEnd : start + len;

                ParallelTask task = NextTask(ref poolIndex);
                tasks[i] = task;
                task.Start(start, end, body);
                MarkProtected(task.ThreadId, true); // mark PFor sub-tasks as protected
            }

            Exception ex = null; // only store a single exception
            for (int i = 0; i < tasks.Length; ++i)
            {
                ParallelTask task = tasks[i];
                Exception e = task.Wait();
                lock (PForProtected) MarkProtected(task.ThreadId, false);
                if (e != null && ex == null) ex = e;
            }
            lock (PForProtected) MarkProtected(Thread.CurrentThread.ManagedThreadId, false);

            // from the first ParallelTask that threw an exception:
            if (ex != null)
                throw ex;
        }

        public static void For(int rangeLength, RangeAction body, int parallelism = 0)
        {
            For(0, rangeLength, body, parallelism);
        }

        public static void ForEach<T>(IReadOnlyList<T> list, Action<T> body)
        {
            For(0, list.Count, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                    body(list[i]);
            });
        }

        public static TaskResult Run(Action action)
        {
            int poolIndex = 0;
            ParallelTask task = NextTask(ref poolIndex);
            var result = new TaskResult();
            task.Start(action, result);
            return result;
        }

        // runs a single background thread and loops over the items
        public static TaskResult Run<T>(IReadOnlyList<T> list, Action<T> body)
        {
            return Run(() =>
            {
                foreach (T item in list)
                    body(item);
            });
        }

        public static TaskResult<T> Run<T>(Func<T> asyncFunc)
        {
            int poolIndex = 0;
            ParallelTask task = NextTask(ref poolIndex);
            var result = new TaskResult<T>();

            object AsyncFunc()
            {
                return asyncFunc();
            }
            task.Start(AsyncFunc, result);
            return result;
        }
    }
}
