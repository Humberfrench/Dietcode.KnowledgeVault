using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Dietcode.KnowledgeVault.Infrastructure.FileSystem;

internal static class WindowsDirectoryLease
{
    public static SafeFileHandle Open(string path)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Secure Vault listing currently requires Windows.");
        // FILE_LIST_DIRECTORY, FILE_SHARE_READ | FILE_SHARE_WRITE, OPEN_EXISTING,
        // FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT. No delete sharing.
        var handle = CreateFile(path, 1, 3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw error switch
            {
                2 or 3 => new DirectoryNotFoundException("Vault folder was not found."),
                5 => new UnauthorizedAccessException("Vault folder access denied."),
                _ => new IOException("Cannot open Vault folder.", new Win32Exception(error))
            };
        }
        try
        {
            WindowsOpenedFileGuard.Validate(handle, path, directory: true);
            return handle;
        }
        catch { handle.Dispose(); throw; }
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share,
        IntPtr security, uint creation, uint flags, IntPtr template);
}
