using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

public class SingleGlobalInstance : IDisposable
{
	public bool hasHandle;

	private Mutex mutex;

    //adding for thread safe Dispose because class uses unmanaged resources 
    private bool disposed;

	public SingleGlobalInstance(int TimeOut)
	{
		this.InitMutex();
		try
		{
			if (TimeOut > 0)
			{
				this.hasHandle = this.mutex.WaitOne(TimeOut, false);
			}
			else
			{
				this.hasHandle = this.mutex.WaitOne(-1, false);
			}
			if (!this.hasHandle)
			{
				throw new Exception("An instance of StarDrive is already running");
			}
		}
		catch (AbandonedMutexException)
		{
			this.hasHandle = true;
		}
	}

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                if (this.mutex != null)
                    this.mutex.Dispose();
            }
            this.disposed = true;
            if (this.hasHandle && this.mutex != null)
            {
                this.mutex.ReleaseMutex();
            }
            this.mutex = null;
        }
    }

	private void InitMutex()
	{
		string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
		string mutexId = string.Format("Global\\{{{0}}}", appGuid);
		this.mutex = new Mutex(false, mutexId);
		MutexAccessRule allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
		MutexSecurity securitySettings = new MutexSecurity();
		securitySettings.AddAccessRule(allowEveryoneRule);
		this.mutex.SetAccessControl(securitySettings);
	}
}