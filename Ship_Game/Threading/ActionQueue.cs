using System;
using System.Threading;

namespace Ship_Game.Threading
{
    /// <summary>
    /// This is a generalized work queue.
    ///
    /// Any kind of work can be submitted to the queue and it will be processed in background
    /// 
    /// </summary>
    public class ActionQueue 
    {
        Thread Worker;
        int ActionsProcessedThisTurn;
        readonly AutoResetEvent ActionsAvailable = new AutoResetEvent(false);
        readonly Array<Action> WorkingActions = new Array<Action>();
        readonly Array<Action> PendingActions = new Array<Action>();
        public readonly AggregatePerfTimer Perf = new AggregatePerfTimer();

        public void Stop()
        {
            Thread dying = Worker;
            Worker = null;
            PendingActions.Clear();
            ActionsAvailable.Set(); // signal the thread
            dying?.Join(250); // wait for merge
        }

        Exception ThreadException;
        int StillProcessingCounter;
        
        public ActionQueue()
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
            Perf.Start();

            ActionsProcessedThisTurn = 0;

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

            Perf.Stop();
        }
    }
}