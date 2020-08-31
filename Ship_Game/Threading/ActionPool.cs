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
        readonly AutoResetEvent ActionsAvailable = new AutoResetEvent(false);
        readonly Array<Action> WorkingActions = new Array<Action>();
        readonly Array<Action> PendingActions = new Array<Action>();

        public int ActionsProcessedThisTurn {get; private set;}

        public PerfTimer ProcessTime = new PerfTimer();

        public void Kill()
        {
            Thread dying = Worker;
            Worker = null;
            PendingActions.Clear();
            ActionsAvailable.Set(); // signal the thread
            dying.Join(250); // wait for merge
        }

        Exception ThreadException  = null;
        int StillProcessingCounter = 0;
        
        public ActionPool()
        {
        }

        /// <summary>
        /// Run On game thread
        /// this will steal the Actions Accumulated and run them on the game thread. 
        /// </summary>
        public void ManualUpdate()
        {
            ProcessActions(PendingActions);

            if (ThreadException != null)
                Log.Error(ThreadException);
        }

        /// <summary>
        /// Run after SpaceManager update is done.
        /// </summary>
        public void SubmitWork(Action work)
        {
            PendingActions.Add(work);

            if (ThreadException != null)
            {
                Log.Error(ThreadException, $"ActionProcessor Exception: {ThreadException}");
                ThreadException = null;
            }

            if (WorkingActions.NotEmpty)
            {
                StillProcessingCounter++;
                if (Empire.Universe?.DebugWin != null && StillProcessingCounter > 0)
                {
                    Log.Warning($"Action Pool Unable to process all items. Processed: " +
                                $"{ActionsProcessedThisTurn} Waiting {PendingActions.Count} " +
                                $"TurnsInProcess: {StillProcessingCounter} " +
                                $" ThreadState {Worker?.ThreadState}");
                }
                return;
            }

            // restart the worker if needed
            if (Worker == null || Worker.ThreadState == ThreadState.Stopped)
            {
                Worker = new Thread(ActionPoolThread);
                Worker.Name = "ActionPool";
                Worker.Start();
            }

            if (WorkingActions.IsEmpty && PendingActions.NotEmpty)
            {
                StillProcessingCounter = 0;

                // move pending items to working
                WorkingActions.AddRange(PendingActions);
                PendingActions.Clear();

                ActionsAvailable.Set(); // GO
            }
        }

        void ActionPoolThread()
        {
            while (Worker != null)
            {
                // wait for ActionsBeingProcessed to be populated
                if (!ActionsAvailable.WaitOne(100/*ms*/))
                    continue; // TIMEOUT

                if (WorkingActions.NotEmpty)
                {
                    ProcessActions(WorkingActions);
                }
            }
        }


        // NOTE: actions will be cleared
        void ProcessActions(Array<Action> actions)
        {
            ProcessTime.Start();

            int processedLastTurn = ActionsProcessedThisTurn;
            ActionsProcessedThisTurn = 0;

            lock (UniverseScreen.SpaceManager.LockSpaceManager)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    try
                    {
                        actions[i].Invoke();
                        ActionsProcessedThisTurn++;
                    }
                    catch (Exception ex)
                    {
                        ThreadException = ex;
                    }
                }
                actions.Clear();
            }

            ProcessTime.Stop();
        }
    }
}