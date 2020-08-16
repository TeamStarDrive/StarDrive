using System;
using System.Threading;
using Ship_Game.Gameplay;
using Ship_Game.Utils;

namespace Ship_Game.Threading
{
    public class ActionPool 
    {
        readonly SafeQueue<Action> ActionToBeThreaded = new SafeQueue<Action>();
        Thread Worker;
        public ScopedReadLock ThreadLock => ActionToBeThreaded.AcquireReadLock() ;
        public void Add(Action itemToThread) => ActionToBeThreaded.Enqueue(itemToThread);

        public void Update()
        {
            if (Worker?.ThreadState != ThreadState.Running)
            {
                //Log.Warning("ActionPool thread not running");
                Worker = new Thread(ProcessQueuedItems);
                Worker.IsBackground = true;
                Worker.Start();
            }
        }

        void ProcessQueuedItems()
        {
            while (ActionToBeThreaded.NotEmpty)
            {
                  ActionToBeThreaded.Dequeue().Invoke();
            }
        }

    }
}