namespace Dietcode.KnowledgeVault.Application.Abstractions;

public interface IVaultPathResolver
{
    /// <summary>Resolves a literal relative path. Null means the root. Does not create or open files.</summary>
    string Resolve(string? relativePath = null);
}