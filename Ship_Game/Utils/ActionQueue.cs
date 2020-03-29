using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Utils;

namespace Ship_Game.Utils
{
    public class ActionQueue
    {
        readonly Queue FunctionsQueue;

        public bool IsEmpty => FunctionsQueue.Count == 0;
        public bool NotEmpty => FunctionsQueue.Count > 0;

        public ActionQueue()
        {
            FunctionsQueue = new Queue();
        }

        public bool Contains(Action action) => FunctionsQueue.Contains(action);

        public Action Pop() => FunctionsQueue.Dequeue() as Action;

        public void Add(Action function)
        {
            FunctionsQueue.Enqueue(function);
        }

        public void Clear()
        {
            FunctionsQueue.Clear();
        }

        public void InvokeQueuedActions()
        {
            while(NotEmpty)
            {
                Pop().Invoke();
            }
        }
    }
}
