using Dietcode.KnowledgeVault.Domain.Exceptions;

namespace Dietcode.KnowledgeVault.Domain.ValueObjects;

/// <summary>SHA-256 token supplied by storage; the domain does not read or hash files.</summary>
public sealed record NoteVersion
{
    public string Value { get; }

    public NoteVersion(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 64 || !value.All(Uri.IsHexDigit))
            throw new ArgumentException("A version must be a 64-character SHA-256 hexadecimal token.", nameof(value));
        Value = value.ToUpperInvariant();
    }

    public void EnsureMatches(NoteVersion actual)
    {
        ArgumentNullException.ThrowIfNull(actual);
        if (this != actual)
            throw new NoteVersionConflictException(this, actual);
    }

    public override string ToString() => Value;
}
