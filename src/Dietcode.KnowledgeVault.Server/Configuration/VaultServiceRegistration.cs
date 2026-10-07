using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Configuration;
using Dietcode.KnowledgeVault.Domain.Policies;
using Dietcode.KnowledgeVault.Infrastructure.FileSystem;
using Microsoft.Extensions.Options;

namespace Dietcode.KnowledgeVault.Server.Configuration;

public static class VaultServiceRegistration
{
    public static IServiceCollection AddVault(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<VaultOptions>, VaultOptionsValidator>();
        services.AddOptions<VaultOptions>()
            .Bind(configuration.GetSection(VaultOptions.SectionName))
            .PostConfigure(options => options.NormalizeExtensions())
            .ValidateOnStart();
        services.AddSingleton<IVaultPathResolver>(provider => new VaultPathResolver(
            provider.GetRequiredService<IOptions<VaultOptions>>().Value.RootPath));
        services.AddSingleton(provider => new NoteSizePolicy(
            provider.GetRequiredService<IOptions<VaultOptions>>().Value.MaxFileSizeBytes));
        services.AddSingleton<INoteReader>(provider => new FileSystemNoteReader(
            provider.GetRequiredService<IVaultPathResolver>(),
            provider.GetRequiredService<IOptions<VaultOptions>>().Value.MaxFileSizeBytes));
        services.AddSingleton<IVaultService, Dietcode.KnowledgeVault.Application.Services.VaultService>();
        return services;
    }
}