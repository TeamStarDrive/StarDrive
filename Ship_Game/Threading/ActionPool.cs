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
        Array<Action> ActionProcessor              = EmptyArray;
        Array<Action> ActionAccumulator;
        bool Initialized;
        int DefaultAccumulatorSize                 = 1000;
        readonly AutoResetEvent ActionsAvailable            = new AutoResetEvent(false);
        
        public int ActionsProcessedThisTurn {get; private set;}
        public float AvgActionsProcessed {get; private set;}
        public bool IsProcessing {get; private set;}

        public PerfTimer ProcessTime = new PerfTimer();
        
        public void Add(Action itemToThread) => ActionAccumulator.Add(itemToThread);

        Exception ThreadException = null;

        int StillProcessingCounter = 0;
        
        public ActionPool()
        {
            Worker            = new Thread(ProcessQueuedItems);
            ActionAccumulator = new Array<Action>(DefaultAccumulatorSize);
        }

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
        }

        /// <summary>
        /// Run after SpaceManager update is done. Run Only from thread generating actions adds
        /// moves all queued actions to thread processing pool. 
        /// </summary>
        public ThreadState MoveItemsToThread()
        {
            if (ThreadException != null)
            {
                Log.Error(ThreadException, $"ActionProcessor Threw in parallel foreach");
                ThreadException = null;
            }

            if (ActionProcessor.NotEmpty)
            {
                ActionsAvailable.Set();
                StillProcessingCounter++;
                if (Empire.Universe?.DebugWin != null && StillProcessingCounter > 5)
                    Log.Warning($"Action Pool Unable to process all items. Processed: " +
                                $"{ActionsProcessedThisTurn} Waiting {ActionAccumulator.Count} " +
                                $"TurnsInProcess: {StillProcessingCounter} " +
                                $" ThreadState {Worker?.ThreadState}");
                return Worker?.ThreadState ?? ThreadState.Stopped;
            }

            if (ActionProcessor.IsEmpty && ActionAccumulator.Count > 100)// && --MoveTimeDelay < 0)
            {
                StillProcessingCounter = 0;

                ActionProcessor = new Array<Action>(ActionAccumulator);
                ActionAccumulator = new Array<Action>(1000);
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
            Array<Action> localActionQueue;
            while (true)
            {
                ActionsAvailable.WaitOne(50);

                if (ActionProcessor.Count == 0) continue;

                ProcessTime.Start();
                IsProcessing = true;
                int processedLastTurn = ActionsProcessedThisTurn;
                ActionsProcessedThisTurn = 0;

                localActionQueue = new Array<Action>(ActionProcessor);

                Parallel.ForEach(localActionQueue, action =>
                {
                    try
                    {
                        action?.Invoke();
                        ActionsProcessedThisTurn++;
                    }
                    catch (Exception ex)
                    {
                        ThreadException = ex;
                    }
                });
                ActionProcessor = EmptyArray;
                AvgActionsProcessed = (ActionsProcessedThisTurn + processedLastTurn) / 2f;
                IsProcessing = false;
                ProcessTime.Stop();
            }
        }
    }
}