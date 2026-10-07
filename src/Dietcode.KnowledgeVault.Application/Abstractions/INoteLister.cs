using Dietcode.KnowledgeVault.Application.Models;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Application.Abstractions;

public interface INoteLister
{
    Task<IReadOnlyCollection<NoteInfo>> ListAsync(VaultPath? folder, CancellationToken cancellationToken);
}
