using Dietcode.KnowledgeVault.Application.Configuration;

namespace Dietcode.KnowledgeVault.UnitTests.Configuration;

public sealed class VaultOptionsTests
{
    [Fact]
    public void NormalizesCaseWhitespaceDotAndDuplicates()
    {
        var options = new VaultOptions { AllowedExtensions = [" MD ", ".MD", ".md"] };
        options.NormalizeExtensions();
        Assert.Equal(new[] { ".md" }, options.AllowedExtensions);
    }

    [Fact]
    public void DoesNotSilentlyAcceptEmptyExtensions()
    {
        var options = new VaultOptions { AllowedExtensions = [" "] };
        options.NormalizeExtensions();
        Assert.Equal(new[] { "" }, options.AllowedExtensions);
    }

    [Fact]
    public void MissingExtensionsStayEmptyForValidation()
    {
        var options = new VaultOptions { AllowedExtensions = null! };
        options.NormalizeExtensions();
        Assert.Empty(options.AllowedExtensions);
    }
}
