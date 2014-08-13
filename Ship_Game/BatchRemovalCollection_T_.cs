using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Ship_Game
{
	public class BatchRemovalCollection<T> :   List<T>
	{
		//public List<T> pendingRemovals;
        public ConcurrentStack<T> pendingRemovals;

		public BatchRemovalCollection()
		{
			//this.pendingRemovals = new List<T>();
            this.pendingRemovals = new ConcurrentStack<T>();
		}

		public void ApplyPendingRemovals()
		{
			//for (int i = 0; i < this.pendingRemovals.Count; i++)
            //foreach(T i in this.pendingRemovals)
            
           while(!this.pendingRemovals.IsEmpty)
			{
				T result;
            this.pendingRemovals.TryPop( out result) ; //out T result);
               //base.Remove(this.pendingRemovals(i));
                base.  Remove(result);
                
			}
			//this.pendingRemovals=new ConcurrentBag<T>();
		}

		public void QueuePendingRemoval(T item)
		{
			this.pendingRemovals.Push(item);
		}
	}
}