namespace Dietcode.KnowledgeVault.Application.Exceptions;

public enum NoteListError { FolderNotFound, NotDirectory, AccessDenied, Unavailable }

public sealed class NoteListException(NoteListError error, string? folder, Exception? innerException = null)
    : Exception($"Cannot list folder '{folder ?? "/"}': {error}.", innerException)
{
    public NoteListError Error { get; } = error;
    public string? Folder { get; } = folder;
}
