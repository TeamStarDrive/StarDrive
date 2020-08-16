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
        public Object Locker;
        public void Add(Action itemToThread) => ActionToBeThreaded.Enqueue(itemToThread);

        public ActionPool()
        {
            Locker = new object();
        }

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
                lock (Empire.Universe.DataCollectorLocker)
                {
                    var action = ActionToBeThreaded.Dequeue();
                    action?.Invoke();
                }     
            }
        }
    }
}