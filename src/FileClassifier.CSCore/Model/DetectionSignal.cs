namespace Tomtastisch.FileClassifier.CSCore.Model;

/// <summary>
/// <b>Zweck:</b><br/>
/// Eingabesignal für deterministische Detektionsprojektionen.
/// </summary>
public sealed record DetectionSignal
{
    /// <summary>
    /// <b>Feld:</b><br/>
    /// Kanonischer Endungskandidat als Projektionseingabe.
    /// </summary>
    public string CanonicalExtension { get; init; }

    /// <summary>
    /// <b>Feld:</b><br/>
    /// MIME-Typ-Kandidat als Projektionseingabe.
    /// </summary>
    public string MimeType { get; init; }

    /// <summary>
    /// <b>Feld:</b><br/>
    /// Anzahl Header-Bytes im Projektionskontext.
    /// </summary>
    public int HeaderBytes { get; init; }

    /// <summary>
    /// <b>Konstruktor:</b><br/>
    /// Erstellt ein normalisiertes Detektionssignal.
    /// </summary>
    public DetectionSignal(string canonicalExtension, string mimeType, int headerBytes)
    {
        CanonicalExtension = canonicalExtension ?? string.Empty;
        MimeType = mimeType ?? string.Empty;
        HeaderBytes = headerBytes;
    }
}
