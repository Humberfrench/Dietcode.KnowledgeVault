using Dietcode.KnowledgeVault.Application.Exceptions;

namespace Dietcode.KnowledgeVault.Infrastructure.FileSystem;

internal static class VaultPathGuard
{
    public static string ValidateRoot(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Path.IsPathFullyQualified(rootPath))
            throw new VaultPathException("Vault:RootPath must be an absolute directory path.");
        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        RejectReparsePoints(fullPath);
        if (!Directory.Exists(fullPath))
            throw new VaultPathException("Vault:RootPath must be an existing accessible directory; automatic creation is disabled.");
        return fullPath;
    }

    // Walk from the volume root, including ancestors of VaultRoot. Never follow a reparse point.
    public static void RejectReparsePoints(string fullPath)
    {
        var root = Path.GetPathRoot(fullPath)!;
        var current = root;
        var segments = fullPath[root.Length..].Split(Path.DirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries);
        Check(current, segments.Length > 0);
        for (var index = 0; index < segments.Length; index++)
        {
            current = Path.Combine(current, segments[index]);
            if (!Check(current, index < segments.Length - 1))
                break; // Missing descendants are allowed for future creation, without creating anything.
        }
    }

    private static bool Check(string path, bool mustBeDirectory)
    {
        FileAttributes attributes;
        try { attributes = File.GetAttributes(path); }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new VaultPathException("Symbolic links, junctions and other reparse points are not allowed in Vault paths.");
        if (mustBeDirectory && (attributes & FileAttributes.Directory) == 0)
            throw new VaultPathException("An intermediate path component is not a directory.");
        return true;
    }
}