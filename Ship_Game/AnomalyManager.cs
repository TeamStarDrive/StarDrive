using System;

namespace Ship_Game
{
	public sealed class AnomalyManager : IDisposable
	{
		public BatchRemovalCollection<Anomaly> AnomaliesList = new BatchRemovalCollection<Anomaly>();

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public AnomalyManager()
		{
		}
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AnomalyManager() { Dispose(false); }

        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.AnomaliesList != null)
                        this.AnomaliesList.Dispose();

                }
                this.AnomaliesList = null;
                this.disposed = true;
            }
        }
    }
}