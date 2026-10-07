using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Domain.Exceptions;

public sealed class NoteVersionConflictException(NoteVersion expected, NoteVersion actual)
    : Exception("The note has changed. Read it again before updating.")
{
    public NoteVersion Expected { get; } = expected;
    public NoteVersion Actual { get; } = actual;
}
