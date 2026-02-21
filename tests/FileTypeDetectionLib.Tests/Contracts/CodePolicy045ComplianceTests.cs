using System.Text.RegularExpressions;

namespace FileTypeDetectionLib.Tests.Contracts;

[Trait("Category", "Governance")]
public sealed class CodePolicy045ComplianceTests
{
    private static readonly Regex NamespaceRegex = new(@"^\s*Namespace\s+",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex PublicTypeRegex = new(@"^\s*Public\s+(?:NotInheritable\s+)?(?:Class|Enum|Structure|Module|Interface)\s+",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex ForbiddenCatchPseudoFilterRegex = new(@"Catch\s+\w+\s+As\s+Exception\s+When\s+TypeOf\s+\w+\s+Is\s+Exception",
        RegexOptions.CultureInvariant);

    [Fact]
    public void VbFiles_UnderSrcFileTypeDetection_ComplyWithCore045LayoutRules()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sourceRoot = Path.Combine(repoRoot, "src", "FileTypeDetection");
        Assert.True(Directory.Exists(sourceRoot), $"Source root missing: {sourceRoot}");

        var files = Directory.GetFiles(sourceRoot, "*.vb", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                               StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                               StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);

            Assert.Contains("' FILE:", content, StringComparison.Ordinal);
            Assert.Contains("INTERNE POLICY", content, StringComparison.Ordinal);
            Assert.Contains("Option Strict On", content, StringComparison.Ordinal);
            Assert.Contains("Option Explicit On", content, StringComparison.Ordinal);
            Assert.True(NamespaceRegex.IsMatch(content), $"Missing namespace declaration: {file}");

            var fileIndex = content.IndexOf("' FILE:", StringComparison.Ordinal);
            var strictIndex = content.IndexOf("Option Strict On", StringComparison.Ordinal);
            var explicitIndex = content.IndexOf("Option Explicit On", StringComparison.Ordinal);
            var namespaceIndex = NamespaceRegex.Match(content).Index;

            Assert.True(fileIndex >= 0 && strictIndex > fileIndex,
                $"Policy 045 order violated ('FILE' before Option Strict): {file}");
            Assert.True(explicitIndex > strictIndex,
                $"Policy 045 order violated (Option Explicit after Option Strict): {file}");
            Assert.True(namespaceIndex > explicitIndex,
                $"Policy 045 order violated (Namespace after options): {file}");

            Assert.False(ForbiddenCatchPseudoFilterRegex.IsMatch(content),
                $"Policy 045 violation (forbidden catch pseudo-filter): {file}");

            if (PublicTypeRegex.IsMatch(content))
            {
                Assert.Contains("''' <summary>", content, StringComparison.Ordinal);
            }
        }
    }
}
