namespace Dietcode.KnowledgeVault.Application.Exceptions;

public enum NoteCreateError { AlreadyExists, ParentNotFound, ExtensionNotAllowed, InvalidEncoding, AccessDenied, Unavailable }

public sealed class NoteCreateException(NoteCreateError error, string relativePath, Exception? innerException = null)
    : Exception($"Cannot create note '{relativePath}': {error}.", innerException)
{
    public NoteCreateError Error { get; } = error;
    public string RelativePath { get; } = relativePath;
}
