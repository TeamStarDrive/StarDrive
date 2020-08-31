using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Ship_Game.Gameplay;
using Ship_Game.Utils;

namespace Ship_Game.Threading
{
    /// <summary>
    /// There are two sync points to the main game thread.
    /// 1. the spacemanager quadtree. QuadTree Finds must not run during quadtree update.
    ///     locked by public GameThreadLocker
    /// 2. moving actions to be processed to the processing queue.
    ///     locked by private ActionProcessorLock
    ///     used by method MoveItemsToThread
    /// To avoid locking on the SpaceManager update try to run MoveItemsToThread just after Universe_SpaceManager Update.
    /// </summary>
    public class ActionPool 
    {
        Thread Worker;
        static readonly Array<Action> EmptyArray   = new Array<Action>(0);
        int DefaultAccumulatorSize                 = 10;
        readonly AutoResetEvent ActionsAvailable   = new AutoResetEvent(false);
        Array<Action> ActionsBeingProcessed        = EmptyArray;
        Array<Action> ActionAccumulator;
        public SafeQueue<Action> RunOnEmpireThread = new SafeQueue<Action>();
        bool Initialized;

        public int ActionsProcessedThisTurn {get; private set;}
        public float AvgActionsProcessed {get; private set;}
        public bool IsProcessing {get; private set;}

        public PerfTimer ProcessTime = new PerfTimer();

        public void Kill()
        {
            Thread dieing = Worker;
            Worker = null;
            dieing.Join(250);
            ActionAccumulator = null;
        }
        
        public void Add(Action itemToThread) => ActionAccumulator.Add(itemToThread);

        Exception ThreadException  = null;
        int StillProcessingCounter = 0;
        
        public ActionPool()
        {
            Worker            = new Thread(ProcessQueuedItems);
            ActionAccumulator = new Array<Action>(DefaultAccumulatorSize);
        }

        public void ExecutePendingEmpireThreadActions()
        {
            while (RunOnEmpireThread.NotEmpty)
            {
                RunOnEmpireThread.Dequeue().Invoke();
            }
        }

        public void EnqueueItemForEmpireThread(Action item) => RunOnEmpireThread.Enqueue(item);

        /// <summary>
        /// Run On game thread
        /// this will steal the Actions Accumulated and run them on the game thread. 
        /// </summary>
        public void ManualUpdate()
        {
            for (int i = 0; i < ActionAccumulator.Count; i++)
            {
                var action = ActionAccumulator[i];
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            ActionAccumulator = new Array<Action>(DefaultAccumulatorSize);
            ExecutePendingEmpireThreadActions();
        }

        /// <summary>
        /// Run after SpaceManager update is done. Run Only from thread generating actions adds
        /// moves all queued actions to thread processing pool. 
        /// </summary>
        public ThreadState MoveItemsToThread()
        {
            ExecutePendingEmpireThreadActions();
            if (ThreadException != null)
            {
                Log.Error(ThreadException, $"ActionProcessor Threw in parallel foreach");
                ThreadException = null;
            }

            if (ActionsBeingProcessed.NotEmpty)
            {
                StillProcessingCounter++;
                if (Empire.Universe?.DebugWin != null && StillProcessingCounter > 0)
                    Log.Warning($"Action Pool Unable to process all items. Processed: " +
                                $"{ActionsProcessedThisTurn} Waiting {ActionAccumulator.Count} " +
                                $"TurnsInProcess: {StillProcessingCounter} " +
                                $" ThreadState {Worker?.ThreadState}");
                return Worker?.ThreadState ?? ThreadState.Stopped;
            }

            if (ActionsBeingProcessed.IsEmpty && (ActionAccumulator.NotEmpty))
            {
                StillProcessingCounter = 0;
                ActionsBeingProcessed  = new Array<Action>(ActionAccumulator);
                ActionAccumulator      = new Array<Action>(10);
                ActionsAvailable.Set();
            }
            return Worker?.ThreadState ?? ThreadState.Stopped;
        }

        public void Initialize() 
        {
            if (Worker == null || Worker.ThreadState == ThreadState.Stopped)
            {
                Log.Warning($"Async data collector lost its thread? what'd you do!?");
                Worker      = new Thread(ProcessQueuedItems); 
                Worker.Name = "ActionPool";
                Initialized = false;
            }
            if (!Initialized)
                Worker.Start();
            Initialized = true;
        }

        void ProcessQueuedItems()
        {
            while (Worker != null)
            {
                // wait for ActionsBeingProcessed to be populated
                if (!ActionsAvailable.WaitOne(100/*ms*/))
                    continue; // TIMEOUT

                if (ActionsBeingProcessed.Count == 0)
                    continue;

                ProcessTime.Start();
                IsProcessing = true;

                int processedLastTurn = ActionsProcessedThisTurn;
                ActionsProcessedThisTurn = 0;

                lock (UniverseScreen.SpaceManager.LockSpaceManager)
                {
                    for (int i = 0; i < ActionsBeingProcessed.Count; i++)
                    {
                        var action = ActionsBeingProcessed[i];
                        try
                        {
                            action?.Invoke();
                            ActionsProcessedThisTurn++;
                        }
                        catch (Exception ex)
                        {
                            ThreadException = ex;
                        }
                    }
                }

                ActionsBeingProcessed = EmptyArray;
                AvgActionsProcessed   = (ActionsProcessedThisTurn + processedLastTurn) / 2f;
                IsProcessing          = false;
                ProcessTime.Stop();
            }
        }
    }
}