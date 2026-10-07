using Microsoft.Win32.SafeHandles;

namespace Dietcode.KnowledgeVault.Infrastructure.FileSystem;

/// <summary>Holds each ancestor against rename/delete while a new note is staged and published.</summary>
internal sealed class WindowsDirectoryChainLease : IDisposable
{
    private readonly List<SafeFileHandle> handles = [];

    public static WindowsDirectoryChainLease Open(string directory)
    {
        var lease = new WindowsDirectoryChainLease();
        try
        {
            var root = Path.GetPathRoot(directory)!;
            var current = root;
            lease.handles.Add(WindowsDirectoryLease.Open(current));
            foreach (var segment in directory[root.Length..].Split(Path.DirectorySeparatorChar,
                         StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                lease.handles.Add(WindowsDirectoryLease.Open(current));
            }
            return lease;
        }
        catch { lease.Dispose(); throw; }
    }

    public void Dispose()
    {
        for (var index = handles.Count - 1; index >= 0; index--)
            handles[index].Dispose();
    }
}
