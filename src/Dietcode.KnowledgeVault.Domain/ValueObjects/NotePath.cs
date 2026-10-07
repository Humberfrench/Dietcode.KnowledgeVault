namespace Dietcode.KnowledgeVault.Domain.ValueObjects;

public sealed record NotePath
{
    public VaultPath RelativePath { get; }
    public string Value => RelativePath.Value;

    public NotePath(string value)
    {
        RelativePath = new VaultPath(value);
        if (!Value.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            Value.Split('/')[^1].Length <= 3)
            throw new ArgumentException("A note must have a name and the .md extension.", nameof(value));
    }

    public override string ToString() => Value;
}
