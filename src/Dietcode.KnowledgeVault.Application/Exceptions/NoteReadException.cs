namespace Dietcode.KnowledgeVault.Application.Exceptions;

public enum NoteReadError
{
    NotFound,
    ExtensionNotAllowed,
    InvalidEncoding,
    AccessDenied,
    Unavailable
}

public sealed class NoteReadException(NoteReadError error, string relativePath, Exception? innerException = null)
    : Exception($"Cannot read note '{relativePath}': {error}.", innerException)
{
    public NoteReadError Error { get; } = error;
    public string RelativePath { get; } = relativePath;
}
