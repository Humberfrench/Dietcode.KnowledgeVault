using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Microsoft.Win32.SafeHandles;

namespace Dietcode.KnowledgeVault.Infrastructure.FileSystem;

/// <summary>Checks the actual opened object before content is read. The supported host is Windows/IIS.</summary>
internal static class WindowsOpenedFileGuard
{
    public static void Validate(SafeFileHandle handle, string expectedPath)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Secure Vault file reads currently require Windows.");

        var buffer = new StringBuilder(512);
        var length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, 0);
        if (length == 0)
            throw new IOException("Cannot verify the opened file path.", new Win32Exception(Marshal.GetLastWin32Error()));
        if (length >= buffer.Capacity)
        {
            buffer = new StringBuilder(checked((int)length + 1));
            length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, 0);
            if (length == 0 || length >= buffer.Capacity)
                throw new IOException("Cannot verify the opened file path.");
        }

        var actual = buffer.ToString();
        if (actual.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            actual = @"\\" + actual[8..];
        else if (actual.StartsWith(@"\\?\", StringComparison.Ordinal))
            actual = actual[4..];

        if (!string.Equals(Path.GetFullPath(actual), expectedPath, StringComparison.OrdinalIgnoreCase))
            throw new VaultPathException("The opened file does not match the validated Vault path.");

        if (!GetFileInformationByHandle(handle, out var info))
            throw new IOException("Cannot verify the opened file attributes.", new Win32Exception(Marshal.GetLastWin32Error()));
        if ((info.Attributes & (uint)(FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0 ||
            info.NumberOfLinks != 1)
            throw new VaultPathException("Only regular files with a single filesystem link are allowed.");
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle file, StringBuilder path, uint length, uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileInformation info);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }
}
