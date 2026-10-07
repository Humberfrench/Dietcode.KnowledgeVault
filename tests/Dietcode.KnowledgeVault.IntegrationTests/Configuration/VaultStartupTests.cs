using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Configuration;
using Dietcode.KnowledgeVault.Domain.Policies;
using Dietcode.KnowledgeVault.Server.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dietcode.KnowledgeVault.IntegrationTests.Configuration;

public sealed class VaultStartupTests
{
    private static WebApplication CreateHost(Dictionary<string, string?> values)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(values);
        builder.WebHost.UseSetting("urls", "http://127.0.0.1:0");
        builder.Services.AddVault(builder.Configuration);
        return builder.Build();
    }

    private static Dictionary<string, string?> Configuration(string root) => new()
    {
        ["Vault:RootPath"] = root,
        ["Vault:AllowedExtensions:0"] = " MD ",
        ["Vault:MaxFileSizeBytes"] = "1024"
    };

    [Fact]
    public async Task StartsWithValidConfigurationAndRegistersConfiguredServices()
    {
        using var vault = new TemporaryVault();
        await using var app = CreateHost(Configuration(vault.Root));
        await app.StartAsync();
        try
        {
            Assert.Equal(vault.Root, app.Services.GetRequiredService<IVaultPathResolver>().Resolve());
            Assert.Equal(1024, app.Services.GetRequiredService<NoteSizePolicy>().MaximumBytes);
            Assert.Equal(new[] { ".md" }, app.Services.GetRequiredService<IOptions<VaultOptions>>().Value.AllowedExtensions);
        }
        finally { await app.StopAsync(); }
    }

    [Theory]
    [InlineData("Vault:RootPath", "", "RootPath")]
    [InlineData("Vault:RootPath", "relative/path", "RootPath")]
    [InlineData("Vault:MaxFileSizeBytes", "0", "MaxFileSizeBytes")]
    [InlineData("Vault:MaxFileSizeBytes", "-1", "MaxFileSizeBytes")]
    [InlineData("Vault:AllowedExtensions:0", ".txt", "AllowedExtensions")]
    [InlineData("Vault:AllowedExtensions:0", "", "AllowedExtensions")]
    [InlineData("Vault:AllowedExtensions:0", "../md", "AllowedExtensions")]
    public async Task FailsDuringStartupWithExplicitConfigurationError(string key, string value, string field)
    {
        using var vault = new TemporaryVault();
        var values = Configuration(vault.Root);
        values[key] = value;
        await using var app = CreateHost(values);
        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
        Assert.Contains(field, error.Message);
    }

    [Fact]
    public async Task MissingSectionFailsBeforeServing()
    {
        await using var app = CreateHost([]);
        await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
    }

    [Fact]
    public async Task MissingDirectoryIsNotCreated()
    {
        using var vault = new TemporaryVault();
        var missing = Path.Combine(vault.Parent, "missing");
        await using var app = CreateHost(Configuration(missing));
        await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public async Task FileCannotBeConfiguredAsRoot()
    {
        using var vault = new TemporaryVault();
        var file = Path.Combine(vault.Root, "note.md");
        File.WriteAllText(file, "# Test");
        await using var app = CreateHost(Configuration(file));
        await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
    }

    [Fact]
    public async Task LinkedRootFailsAtStartup()
    {
        using var vault = new TemporaryVault();
        var link = Path.Combine(vault.Parent, "link");
        vault.CreateDirectoryLink(link, vault.Root);
        await using var app = CreateHost(Configuration(link));
        await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
    }
}
