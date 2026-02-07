using System.Text.Json;

var argsList = args.ToList();
string? sarifPath = null;
for (var i = 0; i < argsList.Count; i++)
{
    if (argsList[i] == "--sarif" && i + 1 < argsList.Count)
    {
        sarifPath = argsList[++i];
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
    using var doc = JsonDocument.Parse(File.ReadAllText(sarifPath));
    if (!doc.RootElement.TryGetProperty("runs", out var runs) || runs.ValueKind != JsonValueKind.Array)
    {
        Console.Error.WriteLine("CI-QODANA-003: SARIF missing runs[] array");
        return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"CI-QODANA-003: SARIF invalid JSON: {ex.Message}");
    return 1;
}

Console.WriteLine("Qodana contract validation passed.");
return 0;
