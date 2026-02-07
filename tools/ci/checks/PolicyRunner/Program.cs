using System.Text.Json;
using System.Diagnostics;
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
var startMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

var logger = new List<string>();
var violations = new List<Violation>();

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
    var loaded = LoadRulesFromYaml(repoRootPath, yamlFile, schemaDocument.SchemaVersion);
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

var matchingRules = allRules
    .Where(rule => rule.AppliesTo.Any(applies => string.Equals(applies, checkId, StringComparison.Ordinal)))
    .OrderBy(rule => rule.RuleId, StringComparer.Ordinal)
    .ToList();

logger.Add($"RULES|loaded={allRules.Count}|matching={matchingRules.Count}");

foreach (var rule in matchingRules)
{
    EvaluateArtifactContractRule(rule);
}

return FinalizeAndExit();

void EvaluateArtifactContractRule(PolicyRule rule)
{
    var rootDir = string.IsNullOrWhiteSpace(rule.Params.ArtifactRoot)
        ? Path.Combine(repoRootPath, "artifacts", "ci")
        : Path.Combine(repoRootPath, rule.Params.ArtifactRoot);

    var checkIds = rule.Params.CheckIds.OrderBy(static x => x, StringComparer.Ordinal).ToList();
    var requiredArtifacts = rule.Params.RequiredArtifacts.OrderBy(static x => x, StringComparer.Ordinal).ToList();

    logger.Add($"EVAL|rule_id={rule.RuleId}|root={Rel(repoRootPath, rootDir)}");
    logger.Add($"EVAL|rule_id={rule.RuleId}|check_ids={string.Join(",", checkIds)}");
    logger.Add($"EVAL|rule_id={rule.RuleId}|required_artifacts={string.Join(",", requiredArtifacts)}");

    foreach (var evaluatedCheckId in checkIds)
    {
        foreach (var requiredArtifact in requiredArtifacts)
        {
            var targetPath = Path.Combine(rootDir, evaluatedCheckId, requiredArtifact);
            var relTargetPath = Rel(repoRootPath, targetPath);
            if (!File.Exists(targetPath))
            {
                violations.Add(new Violation(
                    rule.RuleId,
                    rule.Severity,
                    $"missing required artifact {relTargetPath}",
                    new[] { relTargetPath }));
                logger.Add($"VIOLATION|rule_id={rule.RuleId}|path={relTargetPath}");
                continue;
            }

            if (string.Equals(requiredArtifact, "result.json", StringComparison.Ordinal))
            {
                ValidateResultSchema(targetPath, relTargetPath);
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
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();

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
        violations.Add(new Violation(
            "CI-SCHEMA-001",
            "fail",
            $"result.json schema validation failed for {resultPathRelative}",
            new[] { resultPathRelative }));
        logger.Add($"VIOLATION|rule_id=CI-SCHEMA-001|path={resultPathRelative}");
    }
}

bool HasPolicyFailure()
{
    return violations.Any(v => string.Equals(v.RuleId, "CI-POLICY-001", StringComparison.Ordinal));
}

void AddPolicyFailure(string message, string evidencePath)
{
    violations.Add(new Violation(
        "CI-POLICY-001",
        "fail",
        message,
        new[] { evidencePath }));
    logger.Add($"VIOLATION|rule_id=CI-POLICY-001|path={evidencePath}|message={message}");
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
        $"Rules evaluated: {normalizedViolations.Select(v => v.RuleId).Distinct(StringComparer.Ordinal).Count()}",
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

static (bool Ok, List<PolicyRule> Rules, List<string> ValidationErrors, string? Error) LoadRulesFromYaml(string repoRootPath, string yamlPath, int schemaVersion)
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
                .Where(key => key is not ("rule_id" or "severity" or "title" or "description" or "applies_to" or "params"))
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToList();
            if (unknownKeys.Count > 0)
            {
                validationErrors.Add($"rules[{index}] has unknown keys: {string.Join(",", unknownKeys)}");
            }

            var ruleId = GetScalar(ruleMap, "rule_id");
            var severity = GetScalar(ruleMap, "severity");
            var title = GetScalar(ruleMap, "title");
            var description = GetScalar(ruleMap, "description");
            var appliesTo = GetStringSequence(ruleMap, "applies_to");
            var paramsNode = GetNode(ruleMap, "params") as YamlMappingNode;

            if (string.IsNullOrWhiteSpace(ruleId) || !System.Text.RegularExpressions.Regex.IsMatch(ruleId, "^CI-[A-Z0-9_-]+-[0-9]{3}$"))
            {
                validationErrors.Add($"rules[{index}].rule_id invalid");
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
                .Where(key => key is not ("required_artifacts" or "check_ids" or "artifact_root"))
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToList();
            if (unknownParamKeys.Count > 0)
            {
                validationErrors.Add($"rules[{index}].params has unknown keys: {string.Join(",", unknownParamKeys)}");
            }

            var requiredArtifacts = GetStringSequence(paramsNode, "required_artifacts");
            var checkIds = GetStringSequence(paramsNode, "check_ids");
            var artifactRoot = GetScalar(paramsNode, "artifact_root");

            if (requiredArtifacts.Count == 0)
            {
                validationErrors.Add($"rules[{index}].params.required_artifacts must be non-empty");
            }

            if (checkIds.Count == 0)
            {
                validationErrors.Add($"rules[{index}].params.check_ids must be non-empty");
            }

            rules.Add(new PolicyRule(
                ruleId ?? string.Empty,
                severity ?? "fail",
                title ?? string.Empty,
                description ?? string.Empty,
                appliesTo.OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                new RuleParams(
                    requiredArtifacts.OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                    checkIds.OrderBy(static x => x, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToList(),
                    artifactRoot)));
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
    string? ArtifactRoot);

internal sealed record PolicyRule(
    string RuleId,
    string Severity,
    string Title,
    string Description,
    IReadOnlyList<string> AppliesTo,
    RuleParams Params);

internal sealed record Violation(
    string RuleId,
    string Severity,
    string Message,
    IReadOnlyList<string> EvidencePaths);
