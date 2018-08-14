using System;

namespace Ship_Game
{
	public sealed class AnomalyManager : IDisposable
	{
		public BatchRemovalCollection<Anomaly> AnomaliesList = new BatchRemovalCollection<Anomaly>();

	    public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AnomalyManager() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            AnomaliesList?.Dispose(ref AnomaliesList);
        }
    }
}