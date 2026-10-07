using Dietcode.KnowledgeVault.Application.Models;

namespace Dietcode.KnowledgeVault.Application.Abstractions;

public interface IVaultService
{
    Task<IReadOnlyCollection<NoteInfo>> ListAsync(string? folder = null, CancellationToken cancellationToken = default);
    Task<NoteContent> ReadAsync(string path, CancellationToken cancellationToken = default);
    Task<NoteInfo> GetInfoAsync(string path, CancellationToken cancellationToken = default);
}
