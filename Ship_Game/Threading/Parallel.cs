using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ship_Game
{
    public delegate void RangeAction(int start, int end);

    public interface ITaskResult
    {
        bool IsComplete { get; }
        Exception Error { get; }
        void SetResult(object value, Exception e);
        // Wait until the task is complete, use -1 to wait forever
        bool Wait(int millisecondTimeout);
        // Wait until the task is complete, Exceptions are not rethrown, @see Error
        bool WaitNoThrow(int millisecondTimeout);
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

        // Wait until task has finished
        // NOTE: Throws any unhandled exceptions caught from the task
        // @param millisecondTimeout -1 to wait forever
        // @return TRUE if task was completed
        public bool Wait(int millisecondTimeout = -1)
        {
            if (!IsComplete)
                Finished.WaitOne(millisecondTimeout);
            if (Error != null)
                throw Error;
            return IsComplete;
        }
        
        // Wait until the task is complete, Exceptions are not rethrown, @see Error
        public bool WaitNoThrow(int millisecondTimeout = -1)
        {
            if (!IsComplete)
                Finished.WaitOne(millisecondTimeout);
            return IsComplete;
        }

        // Request for Cancel and call Wait()
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
        // NOTE: Throws any unhandled exceptions caught from the task
        // @return TRUE if task was completed
        public bool Wait(int millisecondTimeout = -1)
        {
            if (!IsComplete)
                Finished.WaitOne(millisecondTimeout);
            if (Error != null)
                throw Error;
            return IsComplete;
        }
        
        // Wait until the task is complete, Exceptions are not rethrown, @see Error
        public bool WaitNoThrow(int millisecondTimeout = -1)
        {
            if (!IsComplete)
                Finished.WaitOne(millisecondTimeout);
            return IsComplete;
        }
        
        // Request for Cancel and call Wait()
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
        public void Start(int start, int end, RangeAction taskBody, ITaskResult result)
        {
            if (HasTasksToExecute())
                throw new InvalidOperationException("ParallelTask is still running");
            RangeTask = taskBody;
            LoopStart = start;
            LoopEnd   = end;
            Result    = result;
            TriggerTaskStart();
        }
        public void Start(Action taskBody, ITaskResult result)
        {
            if (HasTasksToExecute())
                throw new InvalidOperationException("ParallelTask is still running");
            VoidTask = taskBody;
            Result   = result;
            TriggerTaskStart();
        }
        public void Start(Func<object> taskBody, ITaskResult result)
        {
            if (HasTasksToExecute())
                throw new InvalidOperationException("ParallelTask is still running");
            ResultTask = taskBody;
            Result     = result;
            TriggerTaskStart();
        }
        void SetResult(object value, Exception e)
        {
            ITaskResult result = Result;
            if (result == null)
                return;
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
                    if (RangeTask != null)
                    {
                        RangeTask(LoopStart, LoopEnd);
                        SetResult(null, null);
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
                    var currentThread = Thread.CurrentThread;
                    ex.Data["Thread"] = currentThread.Name;
                    ex.Data["ThreadId"] = currentThread.ManagedThreadId;
                    Log.Warning($"{Name} caught unhandled exception: {ex}");
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
        }
        ~ParallelTask() { Destructor(); }
    }

    public static class Parallel
    {
        public static void ClearPool()
        {
            lock (Pool)
            {
                Pool.ClearAndDispose();
            }
        }

        static readonly Array<ParallelTask> Pool = new Array<ParallelTask>(32);
        public static readonly int NumPhysicalCores = InitThreadPool();
        
        [DllImport("SDNative.dll")]
        static extern int GetPhysicalCPUCoreCount();

        static int InitThreadPool()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => ClearPool();
            int cores = GetPhysicalCPUCoreCount();
            return cores;
        }

        public static int PoolSize { get { lock (Pool) { return Pool.Count; } } }

        // NOTE: This must be called in a `lock (Pool)` context,
        //       Because there is a small race-condition between getting idle task and starting it
        static ParallelTask FindOrSpawnTask(ref int poolIndex)
        {
            for (; poolIndex < Pool.Count; ++poolIndex)
            {
                ParallelTask task = Pool[poolIndex];
                if (!task.HasTasksToExecute())
                    return task;
            }
            var newTask = new ParallelTask(Pool.Count);
            Pool.Add(newTask);
            return newTask;
        }

        static TaskResult StartRangeTask(ref int poolIndex, int start, int end,  RangeAction body)
        {
            var result = new TaskResult();
            lock (Pool)
            {
                ParallelTask task = FindOrSpawnTask(ref poolIndex);
                task.Start(start, end, body, result);
            }
            return result;
        }

        // Maximum parallelism allowed by System settings
        // Unlimited Parallelism: <= 0
        // Single Threaded: == 1
        // Limited Parallelism: > 1
        public static int MaxParallelism
        {
            get
            {
                if (GlobalStats.MaxParallelism <= 0)
                    return NumPhysicalCores;
                if (GlobalStats.MaxParallelism == 1)
                    return 1;
                return Math.Min(GlobalStats.MaxParallelism, NumPhysicalCores);
            }
        }

        /// <summary>
        /// Several times faster than System.Threading.Tasks.Parallel.For,
        /// will utilize all cores of the CPU at default affinity.
        /// Ranges are properly partitioned to avoid false sharing
        /// and process the items in batched to avoid delegate callback overhead
        /// 
        /// In case of ParallelTasks encountering an exception, the first captured exception
        /// will be thrown once ALL loop branches are complete.
        /// </summary>
        /// <exception cref="ThreadStateException">NESTED Parallel.For loops are forbidden!</exception>
        /// <param name="rangeStart">Start of the range (inclusive)</param>
        /// <param name="rangeEnd">End of the range (exclusive)</param>
        /// <param name="body">delegate void RangeAction(int start, int end)</param>
        /// <param name="maxParallelism">maximum parallel tasks, -1: use MaxParallelism property</param>
        /// <example>
        /// Parallel.For(0, arr.Length, (start, end) =&gt;
        /// {
        ///     for (int i = start; i &lt; end; i++)
        ///     {
        ///         var elem = arr[i];
        ///     }
        /// });
        /// </example>
        public static void For(int rangeStart, int rangeEnd, RangeAction body, int maxParallelism = -1)
        {
            if (rangeStart >= rangeEnd)
                return; // no work done on empty ranges

            maxParallelism = (maxParallelism <= 0) ? MaxParallelism : Math.Min(maxParallelism, MaxParallelism);

            int range = rangeEnd - rangeStart;
            int cores = Math.Min(range, maxParallelism);
            int len = range / cores;

            // this can happen if the target CPU only has 1 core, or if the list has 1 item
            if (cores <= 1)
            {
                body(rangeStart, rangeEnd);
                return;
            }

            var results = new TaskResult[cores];

            int poolIndex = 0;
            int start = rangeStart;
            int step = range / cores;
            lock (Pool)
            {
                for (int i = 0; i < cores; ++i, start += step)
                {
                    int end = (i == cores - 1) ? rangeEnd : start + len;
                    results[i] = StartRangeTask(ref poolIndex, start, end, body);
                }
            }

            Exception ex = null; // only store a single exception
            for (int i = 0; i < results.Length; ++i)
            {
                TaskResult result = results[i];
                result.WaitNoThrow();
                if (ex == null && result.Error != null)
                    ex = result.Error;
            }

            // from the first ParallelTask that threw an exception:
            if (ex != null)
                throw ex;
        }

        public static void For(int rangeLength, RangeAction body, int maxParallelism = -1)
        {
            For(0, rangeLength, body, maxParallelism);
        }

        // Iterates in Parallel over each element in the list
        // Only use when a single item takes significant amount of time
        public static void ForEach<T>(IReadOnlyList<T> list, Action<T> body)
        {
            For(0, list.Count, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                    body(list[i]);
            });
        }

        // Runs Parallel select over each element in the List
        // Each item T yields a new item of type U
        public static U[] Select<T, U>(IReadOnlyList<T> list, Func<T, U> body)
        {
            var results = new U[list.Count];
            For(0, list.Count, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                    results[i] = body(list[i]);
            });
            return results;
        }

        public static TaskResult Run(Action action)
        {
            int poolIndex = 0;
            var result = new TaskResult();
            lock (Pool)
            {
                ParallelTask task = FindOrSpawnTask(ref poolIndex);
                task.Start(action, result);
            }
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

        public static TaskResult Run(IReadOnlyList<Action> actions)
        {
            return Run(() =>
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    var action = actions[i];
                    action.Invoke();
                }
            });
        }

        public static TaskResult<T> Run<T>(Func<T> asyncFunc)
        {
            object AsyncFunc()
            {
                return asyncFunc();
            }
            int poolIndex = 0;
            var result = new TaskResult<T>();
            lock (Pool)
            {
                ParallelTask task = FindOrSpawnTask(ref poolIndex);
                task.Start(AsyncFunc, result);
            }
            return result;
        }
    }
}
