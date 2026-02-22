namespace Tomtastisch.FileClassifier.CSCore.Model;

/// <summary>
/// <b>Zweck:</b><br/>
/// Unveränderliche Momentaufnahme für deterministische Hash-Optionen.
/// </summary>
public sealed record HashOptionsSnapshot
{
    public bool IncludePayloadCopies { get; init; }
    public bool IncludeFastHash { get; init; } = true;
    public bool IncludeSecureHash { get; init; }
    public string MaterializedFileName { get; init; } = "deterministic-roundtrip.bin";
}
