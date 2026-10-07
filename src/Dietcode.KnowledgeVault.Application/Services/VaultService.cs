using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Application.Models;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Application.Services;

public sealed class VaultService(INoteReader reader) : IVaultService
{
    public Task<NoteContent> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return reader.ReadAsync(Validate(path), cancellationToken);
    }

    public Task<NoteInfo> GetInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return reader.GetInfoAsync(Validate(path), cancellationToken);
    }

    private static NotePath Validate(string path)
    {
        try { _ = new VaultPath(path); }
        catch (ArgumentException exception)
        {
            throw new VaultPathException("Supply a valid relative note path.", exception);
        }
        try { return new NotePath(path); }
        catch (ArgumentException exception)
        {
            throw new NoteReadException(NoteReadError.ExtensionNotAllowed, path, exception);
        }
    }
}
