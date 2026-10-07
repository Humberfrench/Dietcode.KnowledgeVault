using System.Diagnostics;

namespace Dietcode.KnowledgeVault.IntegrationTests;

internal sealed class TemporaryVault : IDisposable
{
    public string Parent { get; } = Path.Combine(Path.GetTempPath(), "KnowledgeVaultTests", Guid.NewGuid().ToString("N"));
    public string Root { get; }
    public string Outside { get; }
    private readonly List<string> links = [];

    public TemporaryVault()
    {
        Root = Directory.CreateDirectory(Path.Combine(Parent, "Vault")).FullName;
        Outside = Directory.CreateDirectory(Path.Combine(Parent, "Vault-backup")).FullName;
    }

    public void CreateDirectoryLink(string link, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            // Junctions require no developer mode or symbolic-link privilege.
            var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            foreach (var argument in new[] { "/c", "mklink", "/J", link, target })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new IOException("Could not create test junction: " + output + error);
        }
        else
            Directory.CreateSymbolicLink(link, target);
        links.Add(link);
    }

    public void Dispose()
    {
        // Remove each link itself before recursive cleanup, never traverse its target.
        foreach (var link in links)
            Directory.Delete(link);
        var allowedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "KnowledgeVaultTests"))
            + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(Parent).StartsWith(allowedParent, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test cleanup escaped the temporary directory.");
        Directory.Delete(Parent, recursive: true);
    }
}
