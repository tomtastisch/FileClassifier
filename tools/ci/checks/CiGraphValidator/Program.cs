using System.Text.Json;
using YamlDotNet.RepresentationModel;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: CiGraphValidator <workflow.yml> <expected.json>");
    return 2;
}

var workflowPath = args[0];
var expectedPath = args[1];
if (!File.Exists(workflowPath) || !File.Exists(expectedPath))
{
    Console.Error.WriteLine("Workflow or expected file missing.");
    return 1;
}

var expected = JsonDocument.Parse(File.ReadAllText(expectedPath)).RootElement;
var allowedJobs = expected.GetProperty("allowed_jobs").EnumerateArray().Select(x => x.GetString()!).ToHashSet();
var requiredEdges = expected.GetProperty("required_needs_edges").EnumerateArray().Select(x =>
    (From: x.GetProperty("from").GetString()!, To: x.GetProperty("to").GetString()!)).ToList();

var yaml = new YamlStream();
yaml.Load(new StringReader(File.ReadAllText(workflowPath)));
var root = (YamlMappingNode)yaml.Documents[0].RootNode;
if (!root.Children.TryGetValue(new YamlScalarNode("jobs"), out var jobsNodeRaw))
{
    Console.Error.WriteLine("No jobs node in workflow.");
    return 1;
}

var jobsNode = (YamlMappingNode)jobsNodeRaw;
var jobNeeds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

foreach (var child in jobsNode.Children)
{
    var jobId = ((YamlScalarNode)child.Key).Value ?? string.Empty;
    var map = child.Value as YamlMappingNode;
    var needs = new HashSet<string>(StringComparer.Ordinal);
    if (map != null && map.Children.TryGetValue(new YamlScalarNode("needs"), out var needsNode))
    {
        switch (needsNode)
        {
            case YamlScalarNode scalar when !string.IsNullOrWhiteSpace(scalar.Value):
                needs.Add(scalar.Value!);
                break;
            case YamlSequenceNode seq:
                foreach (var item in seq.Children.OfType<YamlScalarNode>())
                {
                    if (!string.IsNullOrWhiteSpace(item.Value)) needs.Add(item.Value!);
                }
                break;
        }
    }

    jobNeeds[jobId] = needs;
}

var errors = new List<string>();

foreach (var allowed in allowedJobs)
{
    if (!jobNeeds.ContainsKey(allowed))
    {
        errors.Add($"Missing allowed job: {allowed}");
    }
}

foreach (var present in jobNeeds.Keys)
{
    if (!allowedJobs.Contains(present))
    {
        errors.Add($"Unexpected job found: {present}");
    }
}

foreach (var edge in requiredEdges)
{
    if (!jobNeeds.TryGetValue(edge.From, out var needs) || !needs.Contains(edge.To))
    {
        errors.Add($"Missing required edge: {edge.From} -> {edge.To}");
    }
}

if (errors.Count > 0)
{
    foreach (var e in errors) Console.Error.WriteLine(e);
    return 1;
}

Console.WriteLine("CI graph validation passed.");
return 0;
