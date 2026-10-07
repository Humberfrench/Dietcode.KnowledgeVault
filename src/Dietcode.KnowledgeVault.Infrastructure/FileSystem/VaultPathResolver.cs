using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Infrastructure.FileSystem;

public sealed class VaultPathResolver : IVaultPathResolver
{
    private readonly string rootPath;
    private readonly string rootPrefix;
    private readonly StringComparison comparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public VaultPathResolver(string rootPath)
    {
        this.rootPath = VaultPathGuard.ValidateRoot(rootPath);
        rootPrefix = Path.EndsInDirectorySeparator(this.rootPath)
            ? this.rootPath : this.rootPath + Path.DirectorySeparatorChar;
    }

    public string Resolve(string? relativePath = null)
    {
        try
        {
            // Check again in case a directory has been replaced since startup.
            VaultPathGuard.ValidateRoot(rootPath);
            if (relativePath is null)
                return rootPath;
            // Paths are literal, never URL-decoded. Reject percent escapes to prevent later double decoding.
            if (relativePath.Contains('%'))
                throw new VaultPathException("Percent-encoded paths are not supported; supply a literal relative path.");
            var path = new VaultPath(relativePath);
            var fullPath = Path.GetFullPath(Path.Combine(rootPath,
                path.Value.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(rootPrefix, comparison))
                throw new VaultPathException("The requested path is outside VaultRoot.");
            VaultPathGuard.RejectReparsePoints(fullPath);
            return fullPath;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            throw new VaultPathException("The Vault path is invalid or inaccessible.", exception);
        }
    }
}