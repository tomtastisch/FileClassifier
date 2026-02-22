namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Zentralisierte deterministische Entscheidungen für Nutzlast-Materialisierungspfade.
/// </summary>
public static class MaterializationUtility
{
    public const int Reject = 0;
    public const int PersistRaw = 1;
    public const int ExtractArchive = 2;

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Validiert, ob die Nutzlastlänge innerhalb konfigurierter Byte-Grenzen bleibt.
    /// </summary>
    public static bool IsPayloadWithinMaxBytes(int payloadLength, long maxBytes)
    {
        return payloadLength >= 0 && payloadLength <= maxBytes;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Berechnet den Materialisierungsmodus basierend auf Extraktionsrichtlinie und Archiv-Sicherheitsergebnis.
    /// </summary>
    public static int DecideMode(
        bool secureExtract,
        bool archiveDescribeSucceeded,
        bool archiveSafetyPassed,
        bool archiveSignatureCandidate)
    {
        if (!secureExtract)
        {
            return PersistRaw;
        }

        if (archiveDescribeSucceeded)
        {
            return archiveSafetyPassed ? ExtractArchive : Reject;
        }

        if (archiveSignatureCandidate)
        {
            return Reject;
        }

        return PersistRaw;
    }
}
