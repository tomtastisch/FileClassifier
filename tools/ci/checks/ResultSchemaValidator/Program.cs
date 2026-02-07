using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

var argsList = args.ToList();
string? schemaPath = null;
string? resultPath = null;
for (var i = 0; i < argsList.Count; i++)
{
    if (argsList[i] == "--schema" && i + 1 < argsList.Count)
    {
        schemaPath = argsList[++i];
        continue;
    }

    if (argsList[i] == "--result" && i + 1 < argsList.Count)
    {
        resultPath = argsList[++i];
    }
}

if (string.IsNullOrWhiteSpace(schemaPath) || string.IsNullOrWhiteSpace(resultPath))
{
    Console.Error.WriteLine("Usage: ResultSchemaValidator --schema <path> --result <path>");
    return 2;
}

if (!File.Exists(schemaPath))
{
    Console.Error.WriteLine($"Schema file missing: {schemaPath}");
    return 1;
}

if (!File.Exists(resultPath))
{
    Console.Error.WriteLine($"Result file missing: {resultPath}");
    return 1;
}

JsonDocument schemaDoc;
try
{
    schemaDoc = JsonDocument.Parse(File.ReadAllText(schemaPath));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Schema JSON parse failed: {ex.Message}");
    return 1;
}

var schemaRoot = schemaDoc.RootElement;
var requiredProperties = new List<string>();
if (schemaRoot.TryGetProperty("required", out var requiredEl) && requiredEl.ValueKind == JsonValueKind.Array)
{
    foreach (var item in requiredEl.EnumerateArray())
    {
        var value = item.GetString();
        if (!string.IsNullOrWhiteSpace(value))
        {
            requiredProperties.Add(value);
        }
    }
}

if (requiredProperties.Count == 0)
{
    Console.Error.WriteLine("Schema does not define required properties");
    return 1;
}

int expectedSchemaVersion = 1;
var allowedStatuses = new HashSet<string>(StringComparer.Ordinal);
if (schemaRoot.TryGetProperty("properties", out var propsEl) && propsEl.ValueKind == JsonValueKind.Object)
{
    if (propsEl.TryGetProperty("schema_version", out var schemaVersionEl) &&
        schemaVersionEl.TryGetProperty("const", out var schemaVersionConst) &&
        schemaVersionConst.ValueKind == JsonValueKind.Number &&
        schemaVersionConst.TryGetInt32(out var parsedSchemaVersion))
    {
        expectedSchemaVersion = parsedSchemaVersion;
    }

    if (propsEl.TryGetProperty("status", out var statusSchemaEl) &&
        statusSchemaEl.TryGetProperty("enum", out var statusEnumEl) &&
        statusEnumEl.ValueKind == JsonValueKind.Array)
    {
        foreach (var item in statusEnumEl.EnumerateArray())
        {
            var status = item.GetString();
            if (!string.IsNullOrWhiteSpace(status))
            {
                allowedStatuses.Add(status);
            }
        }
    }
}

if (allowedStatuses.Count == 0)
{
    allowedStatuses.UnionWith(new[] { "pass", "warn", "fail" });
}

try
{
    using var doc = JsonDocument.Parse(File.ReadAllText(resultPath));
    var root = doc.RootElement;
    var errors = new List<string>();

    void RequireProperty(string name)
    {
        if (!root.TryGetProperty(name, out _))
        {
            errors.Add($"Missing property: {name}");
        }
    }

    foreach (var property in requiredProperties)
    {
        RequireProperty(property);
    }

    if (root.TryGetProperty("schema_version", out var schemaVersion))
    {
        if (schemaVersion.ValueKind != JsonValueKind.Number ||
            !schemaVersion.TryGetInt32(out var schemaInt) ||
            schemaInt != expectedSchemaVersion)
        {
            errors.Add($"schema_version must be integer {expectedSchemaVersion}");
        }
    }

    if (root.TryGetProperty("status", out var statusEl))
    {
        var status = statusEl.GetString();
        if (status is null || !allowedStatuses.Contains(status))
        {
            errors.Add($"status must be one of {string.Join("|", allowedStatuses.OrderBy(s => s, StringComparer.Ordinal))}");
        }
    }

    if (root.TryGetProperty("timing", out var timingEl))
    {
        if (timingEl.ValueKind != JsonValueKind.Object)
        {
            errors.Add("timing must be an object");
        }
        else
        {
            if (!timingEl.TryGetProperty("started_at", out var startedAt) || !IsIsoUtc(startedAt.GetString()))
            {
                errors.Add("timing.started_at must be ISO-8601 UTC");
            }

            if (!timingEl.TryGetProperty("finished_at", out var finishedAt) || !IsIsoUtc(finishedAt.GetString()))
            {
                errors.Add("timing.finished_at must be ISO-8601 UTC");
            }

            if (!timingEl.TryGetProperty("duration_ms", out var durationMs) || durationMs.ValueKind != JsonValueKind.Number || !durationMs.TryGetInt64(out var ms) || ms < 0)
            {
                errors.Add("timing.duration_ms must be non-negative integer");
            }
        }
    }

    if (root.TryGetProperty("rule_violations", out var violationsEl))
    {
        if (violationsEl.ValueKind != JsonValueKind.Array)
        {
            errors.Add("rule_violations must be an array");
        }
        else
        {
            foreach (var item in violationsEl.EnumerateArray())
            {
                if (!item.TryGetProperty("rule_id", out var ruleIdEl) || !Regex.IsMatch(ruleIdEl.GetString() ?? string.Empty, "^CI-[A-Z0-9_-]+-[0-9]{3}$"))
                {
                    errors.Add("rule_violations[].rule_id invalid");
                }

                if (!item.TryGetProperty("severity", out var severityEl) || severityEl.GetString() is not ("warn" or "fail"))
                {
                    errors.Add("rule_violations[].severity invalid");
                }

                if (!item.TryGetProperty("message", out var messageEl) || string.IsNullOrWhiteSpace(messageEl.GetString()))
                {
                    errors.Add("rule_violations[].message missing");
                }

                if (!item.TryGetProperty("evidence_paths", out var evidenceEl) || evidenceEl.ValueKind != JsonValueKind.Array)
                {
                    errors.Add("rule_violations[].evidence_paths must be array");
                }
                else if (severityEl.GetString() == "fail" && evidenceEl.GetArrayLength() < 1)
                {
                    errors.Add("rule_violations[].evidence_paths must contain at least one item for fail severity");
                }
            }
        }
    }

    if (errors.Count > 0)
    {
        foreach (var err in errors)
        {
            Console.Error.WriteLine(err);
        }

        return 1;
    }

    Console.WriteLine($"Result schema validation passed: {resultPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Result JSON parse failed: {ex.Message}");
    return 1;
}

static bool IsIsoUtc(string? value)
{
    if (string.IsNullOrWhiteSpace(value) || !value.EndsWith("Z", StringComparison.Ordinal))
    {
        return false;
    }

    return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _);
}
