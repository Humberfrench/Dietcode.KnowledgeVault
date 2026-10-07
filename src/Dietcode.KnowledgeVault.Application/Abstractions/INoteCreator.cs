using Dietcode.KnowledgeVault.Application.Models;
using Dietcode.KnowledgeVault.Domain.Entities;

namespace Dietcode.KnowledgeVault.Application.Abstractions;

public interface INoteCreator
{
    Task<NoteInfo> CreateAsync(Note note, CancellationToken cancellationToken);
}
