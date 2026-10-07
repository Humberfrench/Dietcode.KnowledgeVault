namespace Dietcode.KnowledgeVault.Application.Configuration;

public sealed class VaultOptions
{
    public const string SectionName = "Vault";
    public string RootPath { get; set; } = string.Empty;
    public string[] AllowedExtensions { get; set; } = [".md"];
    public long MaxFileSizeBytes { get; set; } = 2 * 1024 * 1024;

    public void NormalizeExtensions()
    {
        AllowedExtensions = (AllowedExtensions ?? [])
            .Select(extension => (extension ?? string.Empty).Trim().ToLowerInvariant())
            .Select(extension => extension.Length > 0 && !extension.StartsWith('.') ? "." + extension : extension)
            .Distinct(StringComparer.Ordinal).ToArray();
    }
}