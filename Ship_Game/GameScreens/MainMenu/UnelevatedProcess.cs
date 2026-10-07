using System;
using System.Runtime.InteropServices;
using System.Text;
#pragma warning disable CA1060

namespace Ship_Game.GameScreens.MainMenu;

/// <summary>
/// Starts a process without admin rights from an elevated process, using the token of the desktop shell
/// (the documented Windows UAC sample approach). A plain Process.Start from an elevated process stays elevated.
/// </summary>
internal static class UnelevatedProcess
{
    /// <returns>false if the process could not be started this way, e.g. when this process is not elevated</returns>
    public static bool TryStart(string exePath, string arguments, string workingDirectory)
    {
        IntPtr shellProcess = IntPtr.Zero, shellToken = IntPtr.Zero, primaryToken = IntPtr.Zero;
        try
        {
            EnablePrivilege("SeIncreaseQuotaPrivilege");

            IntPtr shellWindow = GetShellWindow();
            if (shellWindow == IntPtr.Zero)
                return false;
            GetWindowThreadProcessId(shellWindow, out uint shellProcessId);
            if (shellProcessId == 0)
                return false;

            shellProcess = OpenProcess(PROCESS_QUERY_INFORMATION, false, shellProcessId);
            if (shellProcess == IntPtr.Zero || !OpenProcessToken(shellProcess, TOKEN_DUPLICATE, out shellToken))
                return false;

            const uint access = TOKEN_QUERY | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID;
            if (!DuplicateTokenEx(shellToken, access, IntPtr.Zero, SecurityImpersonation, TokenPrimary, out primaryToken))
                return false;

            var startup = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };
            var commandLine = new StringBuilder(arguments.Length > 0 ? $"\"{exePath}\" {arguments}" : $"\"{exePath}\"");
            if (!CreateProcessWithTokenW(primaryToken, 0, exePath, commandLine, 0, IntPtr.Zero, workingDirectory,
                                         ref startup, out PROCESS_INFORMATION info))
                return false;

            AllowSetForegroundWindow(info.dwProcessId);
            CloseHandle(info.hProcess);
            CloseHandle(info.hThread);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (primaryToken != IntPtr.Zero) CloseHandle(primaryToken);
            if (shellToken != IntPtr.Zero) CloseHandle(shellToken);
            if (shellProcess != IntPtr.Zero) CloseHandle(shellProcess);
        }
    }

    static void EnablePrivilege(string privilege)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr token))
            return;
        try
        {
            if (!LookupPrivilegeValue(null, privilege, out LUID luid))
                return;
            var privileges = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
            AdjustTokenPrivileges(token, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            CloseHandle(token);
        }
    }

    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
    const uint TOKEN_DUPLICATE = 0x0002;
    const uint TOKEN_QUERY = 0x0008;
    const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    const uint TOKEN_ADJUST_DEFAULT = 0x0080;
    const uint TOKEN_ADJUST_SESSIONID = 0x0100;
    const uint SE_PRIVILEGE_ENABLED = 0x0002;
    const int SecurityImpersonation = 2;
    const int TokenPrimary = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX, dwY, dwXSize, dwYSize;
        public int dwXCountChars, dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    static extern bool AllowSetForegroundWindow(int processId);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool LookupPrivilegeValue(string systemName, string name, out LUID luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TOKEN_PRIVILEGES newState,
                                             uint bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool DuplicateTokenEx(IntPtr existingToken, uint desiredAccess, IntPtr tokenAttributes,
                                        int impersonationLevel, int tokenType, out IntPtr newToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcessWithTokenW(IntPtr token, uint logonFlags, string applicationName, StringBuilder commandLine,
                                               uint creationFlags, IntPtr environment, string currentDirectory,
                                               ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);
}
