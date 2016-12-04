using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

public class SingleGlobalInstance : IDisposable
{
	private Mutex Mutex;

	public SingleGlobalInstance()
	{
		try
		{
            string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly()
                                .GetCustomAttributes(typeof(GuidAttribute), false)[0]).Value;

            Mutex = new Mutex(true, $"Global\\{{{appGuid}}}", out bool newMutexCreated);
            if (!newMutexCreated)
                throw new Exception("An instance of StarDrive is already running");
		}
		catch (AbandonedMutexException)
		{
		}
	}

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SingleGlobalInstance() { Dispose(false); }

    protected void Dispose(bool disposing)
    {
        Mutex?.Dispose();
        Mutex = null;
    }
}