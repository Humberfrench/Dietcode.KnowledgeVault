namespace Dietcode.KnowledgeVault.Domain.Models;

/// <summary>Optional frontmatter fields. Taxonomy is open and serialization belongs to infrastructure.</summary>
public sealed class NoteMetadata
{
    public string? Id { get; }
    public string? Title { get; }
    public string? Category { get; }
    public string? Project { get; }
    public string? Type { get; }
    public DateOnly? Created { get; }
    public DateOnly? Updated { get; }
    public IReadOnlyList<string> Tags { get; }

    public NoteMetadata(string? id = null, string? title = null, string? category = null,
        string? project = null, string? type = null, DateOnly? created = null,
        DateOnly? updated = null, IEnumerable<string>? tags = null)
    {
        if (created.HasValue && updated.HasValue && updated.Value < created.Value)
            throw new ArgumentException("Updated date cannot precede the creation date.", nameof(updated));
        var tagArray = tags?.ToArray() ?? [];
        if (tagArray.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Tags cannot be empty.", nameof(tags));
        Id = id;
        Title = title;
        Category = category;
        Project = project;
        Type = type;
        Created = created;
        Updated = updated;
        Tags = Array.AsReadOnly(tagArray);
    }
}
