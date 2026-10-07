using Dietcode.KnowledgeVault.Application.Models;

namespace Dietcode.KnowledgeVault.Application.Abstractions;

public interface IVaultService
{
    Task<NoteContent> ReadAsync(string path, CancellationToken cancellationToken = default);
    Task<NoteInfo> GetInfoAsync(string path, CancellationToken cancellationToken = default);
}
