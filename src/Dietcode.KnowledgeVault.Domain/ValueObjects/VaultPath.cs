namespace Dietcode.KnowledgeVault.Domain.ValueObjects;

/// <summary>A logical relative path. Physical containment and links must be checked by infrastructure.</summary>
public sealed record VaultPath
{
    public string Value { get; }

    public VaultPath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Replace('\\', '/');
        var segments = normalized.Split('/');
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment) || segment is "." or ".." ||
                segment.EndsWith('.') || segment.EndsWith(' ') ||
                segment.Any(c => char.IsControl(c) || "<>:\"|?*".Contains(c)))
                throw new ArgumentException("Use a relative path with valid folder and file names.", nameof(value));

            var stem = segment.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (stem is "CON" or "PRN" or "AUX" or "NUL" ||
                (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) &&
                 "123456789¹²³".Contains(stem[3])))
                throw new ArgumentException("Reserved Windows device names are not allowed.", nameof(value));
        }
        Value = normalized;
    }

    public override string ToString() => Value;
}
