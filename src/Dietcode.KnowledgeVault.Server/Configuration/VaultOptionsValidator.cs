using Dietcode.KnowledgeVault.Application.Configuration;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Infrastructure.FileSystem;
using Microsoft.Extensions.Options;

namespace Dietcode.KnowledgeVault.Server.Configuration;

public sealed class VaultOptionsValidator : IValidateOptions<VaultOptions>
{
    public ValidateOptionsResult Validate(string? name, VaultOptions options)
    {
        var errors = new List<string>();
        if (options.MaxFileSizeBytes <= 0)
            errors.Add("Vault:MaxFileSizeBytes must be greater than zero.");
        if (options.AllowedExtensions is not { Length: > 0 } ||
            options.AllowedExtensions.Any(extension => extension != ".md"))
            errors.Add("Vault:AllowedExtensions must contain only .md in V1.");
        try { _ = new VaultPathResolver(options.RootPath); }
        catch (Exception exception) when (exception is VaultPathException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            errors.Add("Vault:RootPath must be an existing absolute directory without symbolic links or junctions.");
        }
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}