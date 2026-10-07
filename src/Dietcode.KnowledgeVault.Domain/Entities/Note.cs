using Dietcode.KnowledgeVault.Domain.Policies;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Domain.Entities;

/// <summary>An immutable Markdown document, including any original frontmatter, without rewriting it.</summary>
public sealed class Note
{
    public NotePath Path { get; }
    public string Content { get; }
    private readonly NoteSizePolicy sizePolicy;

    public Note(NotePath path, string content, NoteSizePolicy? sizePolicy = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        this.sizePolicy = sizePolicy ?? new NoteSizePolicy();
        this.sizePolicy.Validate(content);
        Path = path;
        Content = content;
    }

    public Note ReplaceContent(string content) => new(Path, content, sizePolicy);

    /// <summary>Appends exactly the supplied text; the caller controls Markdown separators.</summary>
    public Note AppendContent(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new Note(Path, string.Concat(Content, content), sizePolicy);
    }
}
