using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Application.Models;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Application.Services;

public sealed class VaultService(INoteReader reader, INoteLister lister) : IVaultService
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

    public Task<IReadOnlyCollection<NoteInfo>> ListAsync(string? folder = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        VaultPath? path = null;
        if (folder is not null)
        {
            try { path = new VaultPath(folder); }
            catch (ArgumentException exception)
            {
                throw new VaultPathException("Supply a valid relative folder, or null for the root.", exception);
            }
        }
        return lister.ListAsync(path, cancellationToken);
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
