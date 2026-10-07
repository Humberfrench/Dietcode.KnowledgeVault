using Dietcode.KnowledgeVault.Application.Models;

namespace Dietcode.KnowledgeVault.Application.Abstractions;

public interface IVaultService
{
    Task<NoteInfo> CreateAsync(string path, string content, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NoteInfo>> ListAsync(string? folder = null, CancellationToken cancellationToken = default);
    Task<NoteContent> ReadAsync(string path, CancellationToken cancellationToken = default);
    Task<NoteInfo> GetInfoAsync(string path, CancellationToken cancellationToken = default);
}
