using System.Text.Json;
using System.Text.Json.Nodes;

var nonBlockingHighRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    // Style/noise findings that are not security/reliability regressions for CI gate purposes.
    "CheckNamespace",
    "RedundantQualifier",
    "RedundantSuppressNullableWarningExpression",
    "UnusedImportClause",
    "UnusedMember.Local",
    "VBWarnings__BC42309"
};

var argsList = args.ToList();
string? sarifPath = null;
string? filteredSarifOutPath = null;
for (var i = 0; i < argsList.Count; i++)
{
    if (argsList[i] == "--sarif" && i + 1 < argsList.Count)
    {
        sarifPath = argsList[++i];
    }
    else if (argsList[i] == "--filtered-sarif-out" && i + 1 < argsList.Count)
    {
        filteredSarifOutPath = argsList[++i];
    }
}

if (string.IsNullOrWhiteSpace(sarifPath))
{
    Console.Error.WriteLine("Usage: QodanaContractValidator --sarif <path>");
    return 2;
}

var token = Environment.GetEnvironmentVariable("QODANA_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("CI-QODANA-001: QODANA_TOKEN missing");
    return 1;
}

if (!File.Exists(sarifPath))
{
    Console.Error.WriteLine($"CI-QODANA-002: SARIF missing at {sarifPath}");
    return 1;
}

try
{
    var sarifText = File.ReadAllText(sarifPath);
    using var doc = JsonDocument.Parse(sarifText);
    if (!doc.RootElement.TryGetProperty("runs", out var runs) || runs.ValueKind != JsonValueKind.Array)
    {
        Console.Error.WriteLine("CI-QODANA-003: SARIF missing runs[] array");
        return 1;
    }

    var findings = new List<Finding>();
    foreach (var run in runs.EnumerateArray())
    {
        if (!run.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            continue;
        }

        foreach (var result in results.EnumerateArray())
        {
            var severity = ExtractSeverity(result);
            var ruleId = ExtractString(result, "ruleId") ?? "UNKNOWN";
            var message = ExtractMessage(result) ?? "no-message";
            var location = ExtractPrimaryLocation(result) ?? "unknown:0";
            findings.Add(new Finding(severity, ruleId, message, location));
        }
    }

    var grouped = findings
        .GroupBy(static finding => finding.Severity, StringComparer.OrdinalIgnoreCase)
        .OrderBy(group => SeverityRank(group.Key))
        .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
        .ToList();

    foreach (var group in grouped)
    {
        Console.WriteLine($"QODANA_COUNT|severity={group.Key}|count={group.Count()}");
    }

    var blockingFindings = findings
        .Where(finding => SeverityRank(finding.Severity) >= SeverityRank("High"))
        .Where(finding => !nonBlockingHighRuleIds.Contains(finding.RuleId))
        .OrderBy(finding => finding.Location, StringComparer.Ordinal)
        .ThenBy(finding => finding.RuleId, StringComparer.Ordinal)
        .ThenBy(finding => finding.Message, StringComparer.Ordinal)
        .ToList();

    var ignoredHighFindings = findings
        .Where(finding => SeverityRank(finding.Severity) >= SeverityRank("High"))
        .Where(finding => nonBlockingHighRuleIds.Contains(finding.RuleId))
        .ToList();

    if (ignoredHighFindings.Count > 0)
    {
        foreach (var groupedIgnored in ignoredHighFindings
                     .GroupBy(finding => finding.RuleId, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"QODANA_IGNORED|rule_id={groupedIgnored.Key}|count={groupedIgnored.Count()}");
        }
    }

    foreach (var finding in blockingFindings.Take(20))
    {
        Console.Error.WriteLine($"QODANA_FINDING|severity={finding.Severity}|rule_id={finding.RuleId}|location={finding.Location}|message={finding.Message}");
    }

    if (blockingFindings.Count > 0)
    {
        Console.Error.WriteLine($"CI-QODANA-004: blocking findings detected at severity High+ ({blockingFindings.Count})");
        return 1;
    }

    if (!string.IsNullOrWhiteSpace(filteredSarifOutPath))
    {
        if (!TryWriteFilteredSarif(sarifText, filteredSarifOutPath!, nonBlockingHighRuleIds))
        {
            Console.Error.WriteLine("CI-QODANA-003: unable to write filtered SARIF output");
            return 1;
        }
    }

    var ideaLogPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sarifPath)) ?? ".", "log", "idea.log");
    if (File.Exists(ideaLogPath))
    {
        var toolsetErrors = File.ReadLines(ideaLogPath)
            .Where(static line => line.Contains("Rider toolset and environment errors", StringComparison.Ordinal))
            .OrderBy(static line => line, StringComparer.Ordinal)
            .ToList();

        foreach (var line in toolsetErrors.Take(20))
        {
            Console.Error.WriteLine($"QODANA_TOOL_ERROR|{line}");
        }

        if (toolsetErrors.Count > 0)
        {
            Console.Error.WriteLine($"CI-QODANA-WARN-005: qodana toolset/environment errors detected ({toolsetErrors.Count})");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"CI-QODANA-003: SARIF invalid JSON: {ex.Message}");
    return 1;
}

Console.WriteLine("Qodana contract validation passed.");
return 0;

static int SeverityRank(string severity)
{
    return severity.ToLowerInvariant() switch
    {
        "critical" => 4,
        "high" => 3,
        "moderate" => 2,
        "medium" => 2,
        "low" => 1,
        _ => 0
    };
}

static string ExtractSeverity(JsonElement result)
{
    if (result.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
    {
        var qodanaSeverity = ExtractString(properties, "qodanaSeverity");
        if (!string.IsNullOrWhiteSpace(qodanaSeverity))
        {
            return qodanaSeverity!;
        }
    }

    var level = ExtractString(result, "level");
    return level?.ToLowerInvariant() switch
    {
        "error" => "High",
        "warning" => "Moderate",
        "note" => "Low",
        _ => "Unknown"
    };
}

static string? ExtractMessage(JsonElement result)
{
    if (!result.TryGetProperty("message", out var messageNode) || messageNode.ValueKind != JsonValueKind.Object)
    {
        return null;
    }

    return ExtractString(messageNode, "text");
}

static string? ExtractPrimaryLocation(JsonElement result)
{
    if (!result.TryGetProperty("locations", out var locations) || locations.ValueKind != JsonValueKind.Array)
    {
        return null;
    }

    foreach (var location in locations.EnumerateArray())
    {
        if (!location.TryGetProperty("physicalLocation", out var physicalLocation) || physicalLocation.ValueKind != JsonValueKind.Object)
        {
            continue;
        }

        var file = "unknown";
        if (physicalLocation.TryGetProperty("artifactLocation", out var artifactLocation) && artifactLocation.ValueKind == JsonValueKind.Object)
        {
            file = ExtractString(artifactLocation, "uri") ?? file;
        }

        var line = 0;
        if (physicalLocation.TryGetProperty("region", out var region) && region.ValueKind == JsonValueKind.Object)
        {
            if (region.TryGetProperty("startLine", out var startLine) && startLine.ValueKind == JsonValueKind.Number)
            {
                line = startLine.GetInt32();
            }
        }

        return $"{file}:{line}";
    }

    return null;
}

static string? ExtractString(JsonElement node, string property)
{
    if (!node.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
    {
        return null;
    }

    return value.GetString();
}

static bool TryWriteFilteredSarif(string sarifText, string outputPath, ISet<string> nonBlockingHighRuleIds)
{
    JsonNode? rootNode;
    try
    {
        rootNode = JsonNode.Parse(sarifText);
    }
    catch
    {
        return false;
    }

    if (rootNode is not JsonObject rootObject)
    {
        return false;
    }

    if (rootObject["runs"] is not JsonArray runsArray)
    {
        return false;
    }

    foreach (var runNode in runsArray)
    {
        if (runNode is not JsonObject runObject)
        {
            continue;
        }

        if (runObject["results"] is not JsonArray resultsArray)
        {
            continue;
        }

        var filteredResults = new JsonArray();
        foreach (var resultNode in resultsArray)
        {
            if (resultNode is not JsonObject resultObject)
            {
                continue;
            }

            var severity = ExtractSeverityFromNode(resultObject);
            var ruleId = resultObject["ruleId"]?.GetValue<string>() ?? "UNKNOWN";

            var isBlocking = SeverityRank(severity) >= SeverityRank("High") &&
                             !nonBlockingHighRuleIds.Contains(ruleId);
            if (isBlocking)
            {
                filteredResults.Add(resultObject.DeepClone());
            }
        }

        runObject["results"] = filteredResults;
    }

    var outputDirectory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    File.WriteAllText(outputPath, rootObject.ToJsonString(new JsonSerializerOptions
    {
        WriteIndented = false
    }));
    Console.WriteLine($"QODANA_FILTERED_SARIF|path={outputPath}");
    return true;
}

static string ExtractSeverityFromNode(JsonObject resultObject)
{
    if (resultObject["properties"] is JsonObject propertiesObject)
    {
        var qodanaSeverity = propertiesObject["qodanaSeverity"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(qodanaSeverity))
        {
            return qodanaSeverity!;
        }
    }

    var level = resultObject["level"]?.GetValue<string>();
    return level?.ToLowerInvariant() switch
    {
        "error" => "High",
        "warning" => "Moderate",
        "note" => "Low",
        _ => "Unknown"
    };
}

internal sealed record Finding(string Severity, string RuleId, string Message, string Location);
