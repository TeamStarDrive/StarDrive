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
        void SetResult(object value, Exception e);
        bool Wait(int millisecondTimeout);
        void CancelAndWait(int millisecondTimeout);
    }

    public class TaskResult : ITaskResult
    {
        public Exception Error { get; private set; }
        public bool IsComplete { get; private set; }
        // @note Task has to check this value itself and cancel manually
        public bool IsCancelRequested { get; private set; }
        readonly ManualResetEvent Finished = new ManualResetEvent(false);

        void ITaskResult.SetResult(object value, Exception e)
        {
            Error = e;
            IsComplete = true;
            Finished.Set();
        }

        // wait until task has finished
        public bool Wait(int millisecondTimeout = -1)
        {
            if (!IsComplete)
                Finished.WaitOne(millisecondTimeout);
            if (Error != null)
                throw Error;
            return IsComplete;
        }

        public void CancelAndWait(int millisecondTimeout = -1)
        {
            IsCancelRequested = true;
            Wait(millisecondTimeout);
        }
    }

    public class TaskResult<T> : ITaskResult
    {
        public Exception Error { get; private set; }
        public T Result { get; private set; }
        public bool IsComplete { get; private set; }
        // @note Task has to check this value itself and cancel manually
        public bool IsCancelRequested { get; private set; }
        readonly ManualResetEvent Finished = new ManualResetEvent(false);

        void ITaskResult.SetResult(object value, Exception e)
        {
            Error = e;
            Result = (T)value;
            IsComplete = true;
            Finished.Set();
        }

        // Wait until task has finished
        // @return TRUE if task was completed
        public bool Wait(int millisecondTimeout = -1)
        {
            if (!IsComplete)
                Finished.WaitOne(millisecondTimeout);
            if (Error != null)
                throw Error;
            return IsComplete;
        }

        public void CancelAndWait(int millisecondTimeout = -1)
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
        volatile bool Disposed;
        readonly string Name;

        public ParallelTask(int index)
        {
            Name = "ParallelTask_"+(index+1);
            Thread = new Thread(Run){ Name = Name };
        }
        public bool HasTasksToExecute()
        {
            return RangeTask != null || VoidTask != null || ResultTask != null;
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
            if (HasTasksToExecute())
                throw new InvalidOperationException("ParallelTask is still running");
            RangeTask = taskBody;
            LoopStart = start;
            LoopEnd   = end;
            TriggerTaskStart();
        }
        public void Start(Action taskBody, ITaskResult result)
        {
            if (HasTasksToExecute())
                throw new InvalidOperationException("ParallelTask is still running");
            VoidTask = taskBody;
            Result = result;
            TriggerTaskStart();
        }
        public void Start(Func<object> taskBody, ITaskResult result)
        {
            if (HasTasksToExecute())
                throw new InvalidOperationException("ParallelTask is still running");
            ResultTask = taskBody;
            Result = result;
            TriggerTaskStart();
        }
        public Exception Wait()
        {
            while (HasTasksToExecute() && Thread != null)
            {
                if (EvtEndTask.WaitOne(1000))
                    continue;
                if (Thread == null)
                    Log.Warning("ParallelTask wait timed out after 1000ms but the task was already killed. This is a bug in ParallelTask kill.");
            }
            if (Error == null)
                return null;
            Exception ex = Error; // propagate captured exceptions
            Error = null;
            return ex;
        }
        void SetResult(object value, Exception e)
        {
            ITaskResult result = Result;
            if (result == null) return;
            Result = null; // so if SetResult fails, we don't crash twice
            result.SetResult(value, e);
        }
        void Run()
        {
            while (!Disposed)
            {
                IdleTimer = Stopwatch.StartNew();
                EvtNewTask.WaitOne(5000);
                if (!HasTasksToExecute())
                {
                    lock (KillSync)  // lock before deciding to kill thread
                    {
                        if (IdleTimer.ElapsedMilliseconds > 5000)
                        {
                            Thread = null; // Die!
                            EvtEndTask.Set();
                            Log.Info(ConsoleColor.DarkGray, $"Auto-Kill {Name}");
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
                        SetResult(null, null);
                    }
                    else if (ResultTask != null)
                    {
                        object value = ResultTask.Invoke();
                        SetResult(value, null);
                    }
                }
                catch (Exception ex)
                {
                    if (RangeTask == null) // don't log ex for RangeTasks
                        Log.Warning($"{Name} caught unhandled exception: {ex}");
                    Error = ex;
                    SetResult(null, ex);
                }
                finally
                {
                    RangeTask  = null;
                    VoidTask   = null;
                    ResultTask = null;
                }
                if (Disposed)
                    break;
                EvtEndTask.Set();
            }
            Log.Info(ConsoleColor.DarkGray, $"Dispose-Killed {Name}");
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
            Disposed   = true;
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

        static bool InitThreadPool()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => ClearPool();
            return true;
        }

        public static int PoolSize => Pool.Count;

        static ParallelTask NextTask(ref int poolIndex)
        {
            lock (Pool)
            {
                for (; poolIndex < Pool.Count; ++poolIndex)
                {
                    ParallelTask task = Pool[poolIndex];
                    if (!task.HasTasksToExecute()) return task;
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
                    try
                    {
                        var query = new ManagementObjectSearcher("Select NumberOfCores from Win32_Processor");
                        var results = query.Get();
                        if (results.Count > 0)
                        {
                            foreach (var item in results)
                            {
                                PhysicalCoreCount = (int)(uint)item["NumberOfCores"];
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Query NumPhysicalCores failed: {e.Message}");
                    }

                    if (PhysicalCoreCount == 0)
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

            int poolIndex = 0;
            int start = rangeStart;
            for (int i = 0; i < cores; ++i, start += len)
            {
                int end = (i == cores - 1) ? rangeEnd : start + len;

                ParallelTask task = NextTask(ref poolIndex);
                tasks[i] = task;
                task.Start(start, end, body);
            }

            Exception ex = null; // only store a single exception
            for (int i = 0; i < tasks.Length; ++i)
            {
                ParallelTask task = tasks[i];
                Exception e = task.Wait();
                if (e != null && ex == null) ex = e;
            }

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
