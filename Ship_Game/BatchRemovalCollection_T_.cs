using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class BatchRemovalCollection<T> : List<T>
	{
		public List<T> pendingRemovals;

		public BatchRemovalCollection()
		{
			this.pendingRemovals = new List<T>();
		}

		public void ApplyPendingRemovals()
		{
			for (int i = 0; i < this.pendingRemovals.Count; i++)
			{
				base.Remove(this.pendingRemovals[i]);
			}
			this.pendingRemovals.Clear();
		}

		public void QueuePendingRemoval(T item)
		{
			this.pendingRemovals.Add(item);
		}
	}
}