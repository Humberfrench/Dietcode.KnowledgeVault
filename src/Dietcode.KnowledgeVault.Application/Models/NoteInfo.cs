namespace Dietcode.KnowledgeVault.Application.Models;

public sealed record NoteInfo(
    string Name,
    string RelativePath,
    string Extension,
    long Size,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
