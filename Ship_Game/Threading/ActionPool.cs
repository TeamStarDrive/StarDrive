using System;
using System.Threading;
using Ship_Game.Utils;

namespace Ship_Game.Threading
{
    public class ActionPool 
    {
        readonly SafeQueue<Action> ActionToBeThreaded = new SafeQueue<Action>();
        TaskResult ActionThread;
        Thread Worker;
        EventWaitHandle WorkerGate;

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
            var passToThread = new Array<Action>();
            while (ActionToBeThreaded.NotEmpty)
            {
                passToThread.Add(ActionToBeThreaded.Dequeue());
            }

            if (passToThread.NotEmpty)
            {
                Parallel.ForEach(passToThread, i => i.Invoke());
            }
        }

    }
}