using System.Text;
using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Domain.Exceptions;
using Dietcode.KnowledgeVault.Server.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dietcode.KnowledgeVault.IntegrationTests.FileSystem;

public sealed class NoteListTests
{
    private static ServiceProvider Provider(TemporaryVault vault, long limit = 2097152)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vault:RootPath"] = vault.Root,
            ["Vault:MaxFileSizeBytes"] = limit.ToString()
        }).Build();
        return new ServiceCollection().AddVault(configuration).BuildServiceProvider();
    }

    [Fact]
    public async Task RootListsOnlyMarkdownAtCurrentLevelInStableOrder()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Root, "z.MD"), "ação", new UTF8Encoding(false));
        await File.WriteAllTextAsync(Path.Combine(vault.Root, "A.md"), "# A");
        await File.WriteAllTextAsync(Path.Combine(vault.Root, "ignore.txt"), "not a note");
        Directory.CreateDirectory(Path.Combine(vault.Root, "folder.md"));
        var nested = Directory.CreateDirectory(Path.Combine(vault.Root, "nested")).FullName;
        await File.WriteAllTextAsync(Path.Combine(nested, "child.md"), "# Child");
        using var provider = Provider(vault);
        var service = provider.GetRequiredService<IVaultService>();
        var result = (await service.ListAsync()).ToArray();
        Assert.Equal(new[] { "A.md", "z.MD" }, result.Select(info => info.RelativePath));
        Assert.Equal(await service.GetInfoAsync("z.MD"), result[1]);
    }

    [Fact]
    public async Task SpecificFolderNormalizesSeparatorsAndPreservesUnicode()
    {
        using var vault = new TemporaryVault();
        var folder = Directory.CreateDirectory(Path.Combine(vault.Root, "Trabalho", "TAG")).FullName;
        await File.WriteAllTextAsync(Path.Combine(folder, "Liquidação.md"), "# Nota");
        using var provider = Provider(vault);
        var service = provider.GetRequiredService<IVaultService>();
        var result = Assert.Single(await service.ListAsync(@"Trabalho\TAG"));
        Assert.Equal("Trabalho/TAG/Liquidação.md", result.RelativePath);
    }

    [Fact]
    public async Task EmptyRootAndFolderReturnEmptyCollections()
    {
        using var vault = new TemporaryVault();
        Directory.CreateDirectory(Path.Combine(vault.Root, "empty"));
        using var provider = Provider(vault);
        var service = provider.GetRequiredService<IVaultService>();
        Assert.Empty(await service.ListAsync());
        Assert.Empty(await service.ListAsync("empty"));
    }

    [Fact]
    public async Task MissingFolderReturnsKnownErrorWithoutCreatingIt()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        var error = await Assert.ThrowsAsync<NoteListException>(() =>
            provider.GetRequiredService<IVaultService>().ListAsync("missing"));
        Assert.Equal(NoteListError.FolderNotFound, error.Error);
        Assert.False(Directory.Exists(Path.Combine(vault.Root, "missing")));
    }

    [Fact]
    public async Task FileCannotBeListedAsDirectory()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Root, "note.md"), "# Note");
        using var provider = Provider(vault);
        var error = await Assert.ThrowsAsync<NoteListException>(() =>
            provider.GetRequiredService<IVaultService>().ListAsync("note.md"));
        Assert.Equal(NoteListError.NotDirectory, error.Error);
    }

    [Theory]
    [InlineData("../Vault-backup")]
    [InlineData(@"..\Vault-backup")]
    [InlineData(@"C:\Windows")]
    [InlineData(@"\\server\share")]
    [InlineData("%2e%2e")]
    [InlineData("")]
    [InlineData(".")]
    public async Task RejectsUnsafeFolders(string folder)
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        await Assert.ThrowsAsync<VaultPathException>(() =>
            provider.GetRequiredService<IVaultService>().ListAsync(folder));
    }

    [Fact]
    public async Task DoesNotFollowChildJunctionAndRejectsListingItDirectly()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Outside, "secret.md"), "# Outside");
        vault.CreateDirectoryLink(Path.Combine(vault.Root, "link"), vault.Outside);
        using var provider = Provider(vault);
        var service = provider.GetRequiredService<IVaultService>();
        Assert.Empty(await service.ListAsync());
        await Assert.ThrowsAsync<VaultPathException>(() => service.ListAsync("link"));
    }

    [Fact]
    public async Task MetadataListingDoesNotDecodeContents()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllBytesAsync(Path.Combine(vault.Root, "note.md"), [0xFF, 0xFE]);
        using var provider = Provider(vault);
        var result = Assert.Single(await provider.GetRequiredService<IVaultService>().ListAsync());
        Assert.Equal(2, result.Size);
    }

    [Fact]
    public async Task OversizedNoteFailsInsteadOfReturningPartialListing()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Root, "note.md"), "12345", new UTF8Encoding(false));
        using var provider = Provider(vault, 4);
        await Assert.ThrowsAsync<NoteSizeExceededException>(() =>
            provider.GetRequiredService<IVaultService>().ListAsync());
    }

    [Fact]
    public async Task RejectsJunctionIntroducedBetweenResolutionAndDirectoryOpen()
    {
        using var vault = new TemporaryVault();
        var inner = new Dietcode.KnowledgeVault.Infrastructure.FileSystem.VaultPathResolver(vault.Root);
        var resolver = new RedirectingResolver(inner,
            () => vault.CreateDirectoryLink(Path.Combine(vault.Root, "folder"), vault.Outside));
        var reader = new Dietcode.KnowledgeVault.Infrastructure.FileSystem.FileSystemNoteReader(inner, 2097152);
        var lister = new Dietcode.KnowledgeVault.Infrastructure.FileSystem.FileSystemNoteLister(resolver, reader);
        await Assert.ThrowsAsync<VaultPathException>(() =>
            lister.ListAsync(new Dietcode.KnowledgeVault.Domain.ValueObjects.VaultPath("folder"), default));
    }

    private sealed class RedirectingResolver(IVaultPathResolver inner, Action redirect) : IVaultPathResolver
    {
        public string Resolve(string? relativePath = null)
        {
            var path = inner.Resolve(relativePath);
            redirect();
            return path;
        }
    }

    [Fact]
    public async Task PreCancelledRequestDoesNotEnumerate()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<IVaultService>().ListAsync(cancellationToken: cancellation.Token));
    }
}
