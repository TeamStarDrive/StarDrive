using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ship_Game.Gameplay;
using Ship_Game.Utils;

namespace Ship_Game.Threading
{
    public class ActionPool 
    {
        readonly SafeQueue<Action> ActionToBeThreaded = new SafeQueue<Action>();
        Thread Worker;
        public Object Locker;
        public void Add(Action itemToThread) => ActionToBeThreaded.Enqueue(itemToThread);
        bool Initialized = false;
        public ActionPool()
        {
            Locker = new object();
            Worker = new Thread(ProcessQueuedItems);
        }

        public void ManualUpdate(bool runTillEmpty = false)
        {
            if (runTillEmpty && !Initialized)
            {
                while (ActionToBeThreaded.Count > 0)
                {
                    var action = ActionToBeThreaded.Dequeue();
                    lock (Empire.Universe.DataCollectorLocker)
                    {
                        try
                        {
                            action.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"action failed in ActionPool");
                        }
                    }
                }
            }
        }

        public void Initialize() 
        {
            if (Worker == null || Worker.ThreadState == ThreadState.Stopped)
            {
                Log.Warning($"Async data collector lost its thread? what'd you do!?");
                Worker = new Thread(ProcessQueuedItems); 
                Initialized = false;
            }
            if (!Initialized)
                Worker.Start();
            Initialized = true;
        }

        void ProcessQueuedItems()
        {
            while (true)
            {
                if (ActionToBeThreaded.Count > 0)
                {
                    var actions =ActionToBeThreaded.TakeAll();
                    lock (Locker)
                        Parallel.ForEach(actions, action =>
                        {
                            action?.Invoke();
                        });
                }
                Thread.Sleep(300);
            }
        }
    }
}