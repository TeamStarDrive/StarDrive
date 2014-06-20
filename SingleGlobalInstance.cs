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
		if (this.hasHandle && this.mutex != null)
		{
			this.mutex.ReleaseMutex();
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