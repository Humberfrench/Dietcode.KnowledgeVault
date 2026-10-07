using Dietcode.KnowledgeVault.Application.Models;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Application.Abstractions;

/// <summary>Read-only storage port; no filesystem or transport types cross this boundary.</summary>
public interface INoteReader
{
    Task<NoteContent> ReadAsync(NotePath path, CancellationToken cancellationToken);
    Task<NoteInfo> GetInfoAsync(NotePath path, CancellationToken cancellationToken);
}
