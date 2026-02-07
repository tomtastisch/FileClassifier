using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

var argsList = args.ToList();
string? checkId = null;
string? repoRoot = null;
string? outDir = null;

for (var i = 0; i < argsList.Count; i++)
{
    if (argsList[i] == "--check-id" && i + 1 < argsList.Count)
    {
        checkId = argsList[++i];
        continue;
    }

    if (argsList[i] == "--repo-root" && i + 1 < argsList.Count)
    {
        repoRoot = argsList[++i];
        continue;
    }

    if (argsList[i] == "--out-dir" && i + 1 < argsList.Count)
    {
        outDir = argsList[++i];
    }
}

if (string.IsNullOrWhiteSpace(checkId) || string.IsNullOrWhiteSpace(repoRoot) || string.IsNullOrWhiteSpace(outDir))
{
    Console.Error.WriteLine("Usage: PolicyRunner --check-id <id> --repo-root <path> --out-dir <path>");
    return 2;
}

var repoRootPath = Path.GetFullPath(repoRoot);
var outDirPath = Path.GetFullPath(outDir);
Directory.CreateDirectory(outDirPath);

var rawLogPath = Path.Combine(outDirPath, "raw.log");
var summaryPath = Path.Combine(outDirPath, "summary.md");
var resultPath = Path.Combine(outDirPath, "result.json");

var startedAt = DateTimeOffset.UtcNow;
var startedAtText = startedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
var startMs = startedAt.ToUnixTimeMilliseconds();

var logger = new List<string>();
var violations = new List<Violation>();
var matchedRuleCount = 0;

var rulesDir = Path.Combine(repoRootPath, "tools", "ci", "policies", "rules");
var rulesSchemaPath = Path.Combine(repoRootPath, "tools", "ci", "policies", "schema", "rules.schema.json");
var resultSchemaPath = Path.Combine(repoRootPath, "tools", "ci", "schema", "result.schema.json");

logger.Add($"POLICY_RUNNER|check_id={checkId}");
logger.Add($"POLICY_RUNNER|rules_dir={Rel(repoRootPath, rulesDir)}");
logger.Add($"POLICY_RUNNER|rules_schema={Rel(repoRootPath, rulesSchemaPath)}");
logger.Add($"POLICY_RUNNER|result_schema={Rel(repoRootPath, resultSchemaPath)}");
logger.Add($"POLICY_RUNNER|out_dir={Rel(repoRootPath, outDirPath)}");

if (!File.Exists(rulesSchemaPath))
{
    AddPolicyFailure("rules schema missing", Rel(repoRootPath, rulesSchemaPath));
    return FinalizeAndExit();
}

if (!File.Exists(resultSchemaPath))
{
    AddPolicyFailure("result schema missing", Rel(repoRootPath, resultSchemaPath));
    return FinalizeAndExit();
}

if (!Directory.Exists(rulesDir))
{
    AddPolicyFailure("rules directory missing", Rel(repoRootPath, rulesDir));
    return FinalizeAndExit();
}

var schemaDocument = LoadRulesSchema(rulesSchemaPath);
if (!schemaDocument.Ok)
{
    AddPolicyFailure(schemaDocument.Error ?? "failed to parse rules schema", Rel(repoRootPath, rulesSchemaPath));
    return FinalizeAndExit();
}

var yamlFiles = Directory.GetFiles(rulesDir, "*.yaml", SearchOption.TopDirectoryOnly)
    .OrderBy(static x => x, StringComparer.Ordinal)
    .ToList();

if (yamlFiles.Count == 0)
{
    AddPolicyFailure("no policy files found in rules directory", Rel(repoRootPath, rulesDir));
    return FinalizeAndExit();
}

var allRules = new List<PolicyRule>();
foreach (var yamlFile in yamlFiles)
{
    logger.Add($"RULE_FILE|path={Rel(repoRootPath, yamlFile)}");
    var loaded = LoadRulesFromYaml(yamlFile, schemaDocument.SchemaVersion);
    if (!loaded.Ok)
    {
        AddPolicyFailure(loaded.Error ?? "failed to load policy file", Rel(repoRootPath, yamlFile));
        continue;
    }

    foreach (var parseError in loaded.ValidationErrors)
    {
        AddPolicyFailure(parseError, Rel(repoRootPath, yamlFile));
    }

    allRules.AddRange(loaded.Rules);
}

if (HasPolicyFailure())
{
    return FinalizeAndExit();
}

var duplicateRuleIds = allRules
    .GroupBy(rule => rule.RuleId, StringComparer.Ordinal)
    .Where(group => group.Count() > 1)
    .OrderBy(group => group.Key, StringComparer.Ordinal)
    .ToList();

foreach (var duplicate in duplicateRuleIds)
{
    var evidence = duplicate
        .Select(rule => Rel(repoRootPath, rule.SourcePath))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(static x => x, StringComparer.Ordinal)
        .ToList();
    AddRuleViolation("CI-POLICY-001", "fail", $"duplicate rule_id detected: {duplicate.Key}", evidence);
}

if (HasPolicyFailure())
{
    return FinalizeAndExit();
}

var matchingRules = allRules
    .Where(rule => rule.AppliesTo.Any(applies => string.Equals(applies, checkId, StringComparison.Ordinal)))
    .OrderBy(rule => rule.RuleId, StringComparer.Ordinal)
    .ToList();

matchedRuleCount = matchingRules.Count;
logger.Add($"RULES|loaded={allRules.Count}|matching={matchingRules.Count}");

foreach (var rule in matchingRules)
{
    EvaluateRule(rule);
}

return FinalizeAndExit();

void EvaluateRule(PolicyRule rule)
{
    switch (rule.PolicyType)
    {
        case "artifact_contract":
            EvaluateArtifactContractRule(rule);
            break;
        case "shell_continue_on_error":
        case "shell_or_true":
        case "shell_set_plus_e":
            EvaluateRegexLineRule(rule);
            break;
        case "shell_run_block_max_lines":
            EvaluateRunBlockMaxLinesRule(rule);
            break;
        case "docs_drift":
            EvaluateDocsDriftRule(rule);
            break;
        default:
            AddPolicyFailure($"unsupported policy_type '{rule.PolicyType}'", "tools/ci/policies/rules");
            break;
    }
}

void EvaluateArtifactContractRule(PolicyRule rule)
{
    if (rule.Params.RequiredArtifacts.Count == 0)
    {
        AddPolicyFailure($"{rule.RuleId}: params.required_artifacts must be non-empty", "tools/ci/policies/rules");
        return;
    }

    if (rule.Params.CheckIds.Count == 0)
    {
        AddPolicyFailure($"{rule.RuleId}: params.check_ids must be non-empty", "tools/ci/policies/rules");
        return;
    }

    var configuredArtifactRoot = string.IsNullOrWhiteSpace(rule.Params.ArtifactRoot)
        ? "artifacts/ci"
        : rule.Params.ArtifactRoot!;
    var rootDir = TryResolvePathUnderRepo(configuredArtifactRoot, $"{rule.RuleId}:artifact_root");
    if (rootDir is null)
    {
        return;
    }

    var checkIds = rule.Params.CheckIds.OrderBy(static x => x, StringComparer.Ordinal).ToList();
    var requiredArtifacts = rule.Params.RequiredArtifacts.OrderBy(static x => x, StringComparer.Ordinal).ToList();

    logger.Add($"EVAL|rule_id={rule.RuleId}|type={rule.PolicyType}|root={Rel(repoRootPath, rootDir)}");

    foreach (var evaluatedCheckId in checkIds)
    {
        foreach (var requiredArtifact in requiredArtifacts)
        {
            var targetPath = Path.Combine(rootDir, evaluatedCheckId, requiredArtifact);
            var relTargetPath = Rel(repoRootPath, targetPath);
            if (!File.Exists(targetPath))
            {
                AddRuleViolation(rule.RuleId, rule.Severity, $"missing required artifact {relTargetPath}", new[] { relTargetPath });
                continue;
            }

            if (string.Equals(requiredArtifact, "result.json", StringComparison.Ordinal))
            {
                ValidateResultSchema(targetPath, relTargetPath);
            }
        }
    }
}

void EvaluateRegexLineRule(PolicyRule rule)
{
    if (string.IsNullOrWhiteSpace(rule.Params.RegexPattern))
    {
        AddPolicyFailure($"{rule.RuleId}: params.regex_pattern must be set", "tools/ci/policies/rules");
        return;
    }

    if (rule.Params.ScanPaths.Count == 0)
    {
        AddPolicyFailure($"{rule.RuleId}: params.scan_paths must be non-empty", "tools/ci/policies/rules");
        return;
    }

    Regex regex;
    try
    {
        regex = new Regex(rule.Params.RegexPattern, RegexOptions.CultureInvariant);
    }
    catch (Exception ex)
    {
        AddPolicyFailure($"{rule.RuleId}: invalid params.regex_pattern ({ex.Message})", "tools/ci/policies/rules");
        return;
    }

    var files = CollectFiles(rule.Params.ScanPaths);
    logger.Add($"EVAL|rule_id={rule.RuleId}|type={rule.PolicyType}|files={files.Count}");

    foreach (var file in files)
    {
        var lines = File.ReadAllLines(file);
        for (var i = 0; i < lines.Length; i++)
        {
            if (!regex.IsMatch(lines[i]))
            {
                continue;
            }

            var evidence = $"{Rel(repoRootPath, file)}:{i + 1}";
            AddRuleViolation(rule.RuleId, rule.Severity, rule.Description, new[] { evidence });
        }
    }
}

void EvaluateRunBlockMaxLinesRule(PolicyRule rule)
{
    if (rule.Params.MaxInlineRunLines is null || rule.Params.MaxInlineRunLines < 1)
    {
        AddPolicyFailure($"{rule.RuleId}: params.max_inline_run_lines must be >= 1", "tools/ci/policies/rules");
        return;
    }

    if (rule.Params.ScanPaths.Count == 0)
    {
        AddPolicyFailure($"{rule.RuleId}: params.scan_paths must be non-empty", "tools/ci/policies/rules");
        return;
    }

    var max = rule.Params.MaxInlineRunLines.Value;
    var files = CollectFiles(rule.Params.ScanPaths)
        .Where(static f => f.EndsWith(".yml", StringComparison.Ordinal) || f.EndsWith(".yaml", StringComparison.Ordinal))
        .ToList();

    logger.Add($"EVAL|rule_id={rule.RuleId}|type={rule.PolicyType}|files={files.Count}|max={max}");

    foreach (var file in files)
    {
        var lines = File.ReadAllLines(file);

        var inRun = false;
        var count = 0;
        var startLine = 0;
        var runIndent = 0;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var lineNumber = index + 1;

            if (!inRun)
            {
                if (Regex.IsMatch(line, @"^\s*run:\s*\|\s*$", RegexOptions.CultureInvariant))
                {
                    inRun = true;
                    count = 0;
                    startLine = lineNumber;
                    runIndent = LeadingSpaces(line);
                }

                continue;
            }

            var currIndent = LeadingSpaces(line);
            var isBlank = string.IsNullOrWhiteSpace(line);
            if (!isBlank && currIndent <= runIndent)
            {
                if (count > max)
                {
                    AddRuleViolation(
                        rule.RuleId,
                        rule.Severity,
                        $"{rule.Description} ({max})",
                        new[]
                        {
                            $"{Rel(repoRootPath, file)}:{startLine}",
                            $"{Rel(repoRootPath, file)}:lines={count}"
                        });
                }

                inRun = false;
                count = 0;
                startLine = 0;
                runIndent = 0;

                if (Regex.IsMatch(line, @"^\s*run:\s*\|\s*$", RegexOptions.CultureInvariant))
                {
                    inRun = true;
                    startLine = lineNumber;
                    runIndent = LeadingSpaces(line);
                }

                continue;
            }

            count++;
        }

        if (inRun && count > max)
        {
            AddRuleViolation(
                rule.RuleId,
                rule.Severity,
                $"{rule.Description} ({max})",
                new[]
                {
                    $"{Rel(repoRootPath, file)}:{startLine}",
                    $"{Rel(repoRootPath, file)}:lines={count}"
                });
        }
    }
}

void EvaluateDocsDriftRule(PolicyRule rule)
{
    if (rule.Params.DocsPaths.Count == 0)
    {
        AddPolicyFailure($"{rule.RuleId}: params.docs_paths must be non-empty", "tools/ci/policies/rules");
        return;
    }

    if (rule.Params.ForbiddenPatterns.Count == 0)
    {
        AddPolicyFailure($"{rule.RuleId}: params.forbidden_patterns must be non-empty", "tools/ci/policies/rules");
        return;
    }

    var docsFiles = CollectFiles(rule.Params.DocsPaths)
        .Where(static f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        .OrderBy(static f => f, StringComparer.Ordinal)
        .ToList();

    var allowedSet = new HashSet<string>(
        rule.Params.AllowedPaths.Select(path => Rel(repoRootPath, Path.Combine(repoRootPath, path))),
        StringComparer.Ordinal);

    logger.Add($"EVAL|rule_id={rule.RuleId}|type={rule.PolicyType}|docs_files={docsFiles.Count}");

    var compiledPatterns = new List<Regex>();
    foreach (var pattern in rule.Params.ForbiddenPatterns.OrderBy(static p => p, StringComparer.Ordinal))
    {
        try
        {
            compiledPatterns.Add(new Regex(pattern, RegexOptions.CultureInvariant));
        }
        catch (Exception ex)
        {
            AddPolicyFailure($"{rule.RuleId}: invalid forbidden pattern '{pattern}' ({ex.Message})", "tools/ci/policies/rules");
            return;
        }
    }

    foreach (var file in docsFiles)
    {
        var rel = Rel(repoRootPath, file);
        if (allowedSet.Contains(rel))
        {
            continue;
        }

        var lines = File.ReadAllLines(file);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            foreach (var pattern in compiledPatterns)
            {
                if (!pattern.IsMatch(line))
                {
                    continue;
                }

                AddRuleViolation(rule.RuleId, rule.Severity, rule.Description, new[] { $"{rel}:{i + 1}" });
                break;
            }
        }
    }
}

void ValidateResultSchema(string resultPathAbsolute, string resultPathRelative)
{
    var validatorDllPath = Path.Combine(
        repoRootPath,
        "tools",
        "ci",
        "checks",
        "ResultSchemaValidator",
        "bin",
        "Release",
        "net10.0",
        "ResultSchemaValidator.dll");

    if (!File.Exists(validatorDllPath))
    {
        AddPolicyFailure("result schema validator missing", Rel(repoRootPath, validatorDllPath));
        return;
    }

    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }
    };

    process.StartInfo.ArgumentList.Add(validatorDllPath);
    process.StartInfo.ArgumentList.Add("--schema");
    process.StartInfo.ArgumentList.Add(resultSchemaPath);
    process.StartInfo.ArgumentList.Add("--result");
    process.StartInfo.ArgumentList.Add(resultPathAbsolute);

    process.Start();
    var stdoutTask = process.StandardOutput.ReadToEndAsync();
    var stderrTask = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    Task.WaitAll(stdoutTask, stderrTask);
    var stdout = stdoutTask.Result;
    var stderr = stderrTask.Result;

    if (!string.IsNullOrWhiteSpace(stdout))
    {
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            logger.Add($"SCHEMA_STDOUT|{line.TrimEnd()}");
        }
    }

    if (!string.IsNullOrWhiteSpace(stderr))
    {
        foreach (var line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            logger.Add($"SCHEMA_STDERR|{line.TrimEnd()}");
        }
    }

    if (process.ExitCode != 0)
    {
        AddRuleViolation("CI-SCHEMA-001", "fail", $"result.json schema validation failed for {resultPathRelative}", new[] { resultPathRelative });
    }
}

List<string> CollectFiles(IReadOnlyList<string> configuredPaths)
{
    var files = new SortedSet<string>(StringComparer.Ordinal);

    foreach (var configuredPath in configuredPaths.OrderBy(static p => p, StringComparer.Ordinal))
    {
        var fullPath = TryResolvePathUnderRepo(configuredPath, "collect_files");
        if (fullPath is null)
        {
            continue;
        }

        if (File.Exists(fullPath))
        {
            files.Add(Path.GetFullPath(fullPath));
            continue;
        }

        if (Directory.Exists(fullPath))
        {
            foreach (var file in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories).OrderBy(static f => f, StringComparer.Ordinal))
            {
                files.Add(Path.GetFullPath(file));
            }

            continue;
        }

        AddPolicyFailure($"configured path missing: {configuredPath}", configuredPath);
    }

    return files.ToList();
}

string? TryResolvePathUnderRepo(string configuredPath, string context)
{
    if (Path.IsPathRooted(configuredPath))
    {
        AddPolicyFailure($"{context}: configured path must be relative", configuredPath);
        return null;
    }

    var fullPath = Path.GetFullPath(Path.Combine(repoRootPath, configuredPath));
    var repoRootWithSep = repoRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    var fullPathWithSep = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (!fullPathWithSep.StartsWith(repoRootWithSep, StringComparison.Ordinal))
    {
        AddPolicyFailure($"{context}: configured path escapes repository root", configuredPath);
        return null;
    }

    return fullPath;
}

void AddRuleViolation(string ruleId, string severity, string message, IReadOnlyList<string> evidence)
{
    violations.Add(new Violation(
        ruleId,
        severity,
        message,
        evidence.OrderBy(static p => p, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList()));
    logger.Add($"VIOLATION|rule_id={ruleId}|message={message}");
}

bool HasPolicyFailure()
{
    return violations.Any(v => string.Equals(v.RuleId, "CI-POLICY-001", StringComparison.Ordinal));
}

void AddPolicyFailure(string message, string evidencePath)
{
    AddRuleViolation("CI-POLICY-001", "fail", message, new[] { evidencePath });
}

int FinalizeAndExit()
{
    var finishedAt = DateTimeOffset.UtcNow;
    var finishedAtText = finishedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
    var durationMs = Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startMs);

    var normalizedViolations = violations
        .Select(v => new Violation(
            v.RuleId,
            v.Severity,
            v.Message,
            v.EvidencePaths.OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList()))
        .OrderBy(v => v.RuleId, StringComparer.Ordinal)
        .ThenBy(v => v.EvidencePaths.FirstOrDefault() ?? string.Empty, StringComparer.Ordinal)
        .ThenBy(v => v.Message, StringComparer.Ordinal)
        .ToList();

    var status = "pass";
    if (normalizedViolations.Any(v => string.Equals(v.Severity, "fail", StringComparison.Ordinal)))
    {
        status = "fail";
    }
    else if (normalizedViolations.Count > 0)
    {
        status = "warn";
    }

    var allEvidence = normalizedViolations
        .SelectMany(v => v.EvidencePaths)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(static x => x, StringComparer.Ordinal)
        .ToList();

    var result = new
    {
        schema_version = 1,
        check_id = checkId,
        status,
        rule_violations = normalizedViolations.Select(v => new
        {
            rule_id = v.RuleId,
            severity = v.Severity,
            message = v.Message,
            evidence_paths = v.EvidencePaths
        }),
        evidence_paths = allEvidence,
        artifacts = new[]
        {
            Rel(repoRootPath, rawLogPath),
            Rel(repoRootPath, summaryPath),
            Rel(repoRootPath, resultPath)
        },
        timing = new
        {
            started_at = startedAtText,
            finished_at = finishedAtText,
            duration_ms = durationMs
        }
    };

    File.WriteAllLines(rawLogPath, logger.OrderBy(static line => line, StringComparer.Ordinal));

    var summaryLines = new List<string>
    {
        $"PolicyRunner check '{checkId}' completed.",
        $"Rules matched: {matchedRuleCount}",
        $"Rules with violations: {normalizedViolations.Select(v => v.RuleId).Distinct(StringComparer.Ordinal).Count()}",
        $"Violations: {normalizedViolations.Count}",
        $"Status: {status}"
    };

    foreach (var violation in normalizedViolations)
    {
        summaryLines.Add($"- {violation.RuleId} [{violation.Severity}] {violation.Message}");
    }

    File.WriteAllLines(summaryPath, summaryLines);

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
    File.WriteAllText(resultPath, JsonSerializer.Serialize(result, jsonOptions));

    return string.Equals(status, "fail", StringComparison.Ordinal) ? 1 : 0;
}

static string Rel(string repoRootPath, string path)
{
    return Path.GetRelativePath(repoRootPath, path).Replace('\\', '/');
}

static int LeadingSpaces(string input)
{
    var count = 0;
    while (count < input.Length && input[count] == ' ')
    {
        count++;
    }

    return count;
}

static (bool Ok, int SchemaVersion, string? Error) LoadRulesSchema(string schemaPath)
{
    try
    {
        using var schemaDoc = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var root = schemaDoc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return (false, 0, "rules schema root must be an object");
        }

        if (!root.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return (false, 0, "rules schema missing properties object");
        }

        if (!properties.TryGetProperty("rules_schema_version", out var versionProperty) ||
            !versionProperty.TryGetProperty("const", out var versionConst) ||
            !versionConst.TryGetInt32(out var schemaVersion))
        {
            return (false, 0, "rules schema missing properties.rules_schema_version.const");
        }

        return (true, schemaVersion, null);
    }
    catch (Exception ex)
    {
        return (false, 0, $"rules schema parse failed: {ex.Message}");
    }
}

static (bool Ok, List<PolicyRule> Rules, List<string> ValidationErrors, string? Error) LoadRulesFromYaml(string yamlPath, int schemaVersion)
{
    try
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(File.ReadAllText(yamlPath)));
        if (yaml.Documents.Count != 1)
        {
            return (false, new List<PolicyRule>(), new List<string>(), "yaml must contain exactly one document");
        }

        if (yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            return (false, new List<PolicyRule>(), new List<string>(), "yaml root must be mapping");
        }

        var validationErrors = new List<string>();
        var unknownRootKeys = root.Children.Keys
            .OfType<YamlScalarNode>()
            .Select(static x => x.Value ?? string.Empty)
            .Where(key => key is not ("rules_schema_version" or "rules"))
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
        if (unknownRootKeys.Count > 0)
        {
            validationErrors.Add($"root has unknown keys: {string.Join(",", unknownRootKeys)}");
        }

        var versionText = GetScalar(root, "rules_schema_version");
        if (!int.TryParse(versionText, out var parsedVersion) || parsedVersion != schemaVersion)
        {
            validationErrors.Add($"rules_schema_version must equal {schemaVersion}");
        }

        var rulesNode = GetNode(root, "rules");
        if (rulesNode is not YamlSequenceNode rulesSequence || rulesSequence.Children.Count == 0)
        {
            validationErrors.Add("rules must be a non-empty sequence");
            return (true, new List<PolicyRule>(), validationErrors, null);
        }

        var rules = new List<PolicyRule>();
        for (var index = 0; index < rulesSequence.Children.Count; index++)
        {
            if (rulesSequence.Children[index] is not YamlMappingNode ruleMap)
            {
                validationErrors.Add($"rules[{index}] must be an object");
                continue;
            }

            var unknownKeys = ruleMap.Children.Keys
                .OfType<YamlScalarNode>()
                .Select(static x => x.Value ?? string.Empty)
                .Where(key => key is not ("rule_id" or "policy_type" or "severity" or "title" or "description" or "applies_to" or "params"))
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToList();
            if (unknownKeys.Count > 0)
            {
                validationErrors.Add($"rules[{index}] has unknown keys: {string.Join(",", unknownKeys)}");
            }

            var ruleId = GetScalar(ruleMap, "rule_id") ?? string.Empty;
            var policyType = GetScalar(ruleMap, "policy_type") ?? string.Empty;
            var severity = GetScalar(ruleMap, "severity") ?? string.Empty;
            var title = GetScalar(ruleMap, "title") ?? string.Empty;
            var description = GetScalar(ruleMap, "description") ?? string.Empty;
            var appliesTo = GetStringSequence(ruleMap, "applies_to");
            var paramsNode = GetNode(ruleMap, "params") as YamlMappingNode;

            if (string.IsNullOrWhiteSpace(ruleId) || !Regex.IsMatch(ruleId, "^CI-[A-Z0-9_-]+-[0-9]{3}$", RegexOptions.CultureInvariant))
            {
                validationErrors.Add($"rules[{index}].rule_id invalid");
            }

            if (policyType is not (
                "artifact_contract" or
                "shell_continue_on_error" or
                "shell_or_true" or
                "shell_set_plus_e" or
                "shell_run_block_max_lines" or
                "docs_drift"))
            {
                validationErrors.Add($"rules[{index}].policy_type invalid");
            }

            if (severity is not ("warn" or "fail"))
            {
                validationErrors.Add($"rules[{index}].severity invalid");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                validationErrors.Add($"rules[{index}].title missing");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                validationErrors.Add($"rules[{index}].description missing");
            }

            if (appliesTo.Count == 0)
            {
                validationErrors.Add($"rules[{index}].applies_to must be non-empty");
            }

            if (paramsNode is null)
            {
                validationErrors.Add($"rules[{index}].params missing");
                continue;
            }

            var unknownParamKeys = paramsNode.Children.Keys
                .OfType<YamlScalarNode>()
                .Select(static x => x.Value ?? string.Empty)
                .Where(key => key is not (
                    "required_artifacts" or
                    "check_ids" or
                    "artifact_root" or
                    "scan_paths" or
                    "regex_pattern" or
                    "max_inline_run_lines" or
                    "docs_paths" or
                    "forbidden_patterns" or
                    "allowed_paths"))
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToList();

            if (unknownParamKeys.Count > 0)
            {
                validationErrors.Add($"rules[{index}].params has unknown keys: {string.Join(",", unknownParamKeys)}");
            }

            var parsedParams = new RuleParams(
                RequiredArtifacts: GetStringSequence(paramsNode, "required_artifacts").OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                CheckIds: GetStringSequence(paramsNode, "check_ids").OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                ArtifactRoot: GetScalar(paramsNode, "artifact_root"),
                ScanPaths: GetStringSequence(paramsNode, "scan_paths").OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                RegexPattern: GetScalar(paramsNode, "regex_pattern"),
                MaxInlineRunLines: GetIntScalar(paramsNode, "max_inline_run_lines"),
                DocsPaths: GetStringSequence(paramsNode, "docs_paths").OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                ForbiddenPatterns: GetStringSequence(paramsNode, "forbidden_patterns").OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                AllowedPaths: GetStringSequence(paramsNode, "allowed_paths").OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList());

            rules.Add(new PolicyRule(
                RuleId: ruleId,
                PolicyType: policyType,
                Severity: severity,
                Title: title,
                Description: description,
                AppliesTo: appliesTo.OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                Params: parsedParams,
                SourcePath: yamlPath));
        }

        return (true, rules, validationErrors, null);
    }
    catch (Exception ex)
    {
        return (false, new List<PolicyRule>(), new List<string>(), $"yaml parse failed: {ex.Message}");
    }
}

static YamlNode? GetNode(YamlMappingNode mappingNode, string key)
{
    return mappingNode.Children.TryGetValue(new YamlScalarNode(key), out var value) ? value : null;
}

static string? GetScalar(YamlMappingNode mappingNode, string key)
{
    return GetNode(mappingNode, key) is YamlScalarNode scalar ? scalar.Value : null;
}

static int? GetIntScalar(YamlMappingNode mappingNode, string key)
{
    if (!int.TryParse(GetScalar(mappingNode, key), out var parsed))
    {
        return null;
    }

    return parsed;
}

static List<string> GetStringSequence(YamlMappingNode mappingNode, string key)
{
    var values = new List<string>();
    if (GetNode(mappingNode, key) is not YamlSequenceNode sequence)
    {
        return values;
    }

    foreach (var item in sequence.Children.OfType<YamlScalarNode>())
    {
        if (!string.IsNullOrWhiteSpace(item.Value))
        {
            values.Add(item.Value!);
        }
    }

    return values;
}

internal sealed record RuleParams(
    IReadOnlyList<string> RequiredArtifacts,
    IReadOnlyList<string> CheckIds,
    string? ArtifactRoot,
    IReadOnlyList<string> ScanPaths,
    string? RegexPattern,
    int? MaxInlineRunLines,
    IReadOnlyList<string> DocsPaths,
    IReadOnlyList<string> ForbiddenPatterns,
    IReadOnlyList<string> AllowedPaths);

internal sealed record PolicyRule(
    string RuleId,
    string PolicyType,
    string Severity,
    string Title,
    string Description,
    IReadOnlyList<string> AppliesTo,
    RuleParams Params,
    string SourcePath);

internal sealed record Violation(
    string RuleId,
    string Severity,
    string Message,
    IReadOnlyList<string> EvidencePaths);
