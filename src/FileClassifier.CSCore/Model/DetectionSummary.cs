namespace Tomtastisch.FileClassifier.CSCore.Model;

/// <summary>
/// <b>Zweck:</b><br/>
/// Normalisierte Zusammenfassungsprojektion für detektionsbezogenen Prüfkontext.
/// </summary>
public sealed record DetectionSummary
{
    /// <summary>
    /// <b>Feld:</b><br/>
    /// Kanonische Endung aus dem Quellsignal.
    /// </summary>
    public string CanonicalExtension { get; init; }

    /// <summary>
    /// <b>Feld:</b><br/>
    /// MIME-Typ aus dem Quellsignal.
    /// </summary>
    public string MimeType { get; init; }

    /// <summary>
    /// <b>Feld:</b><br/>
    /// Header-Byte-Anzahl aus dem Quellsignal.
    /// </summary>
    public int HeaderBytes { get; init; }

    /// <summary>
    /// <b>Hinweis:</b><br/>
    /// Kennzeichnet, ob der MIME-Typ eine strukturierte Haupt-/Untertyp-Form besitzt.
    /// </summary>
    public bool HasStructuredMime => MimeType.IndexOf('/') >= 0;

    /// <summary>
    /// <b>Konstruktor:</b><br/>
    /// Erstellt eine normalisierte Detektionszusammenfassung.
    /// </summary>
    public DetectionSummary(string canonicalExtension, string mimeType, int headerBytes)
    {
        CanonicalExtension = canonicalExtension ?? string.Empty;
        MimeType = mimeType ?? string.Empty;
        HeaderBytes = headerBytes;
    }
}
