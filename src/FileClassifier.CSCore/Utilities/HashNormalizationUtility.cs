using System.IO;

namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Zentralisierte Normalisierungsregeln für Hash- und Materialisierungsmetadaten.
/// </summary>
public static class HashNormalizationUtility
{
    public const string DefaultMaterializedFileName = "deterministic-roundtrip.bin";
    public const int EmptyDigestPartCount = 6;

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Normalisiert Hashwerte auf eine kleingeschriebene, kulturinvariante Darstellung.
    /// </summary>
    public static string NormalizeDigest(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Erzeugt einen leeren Hashteil-Vektor mit fester Größe.
    /// </summary>
    public static string[] CreateEmptyDigestParts()
    {
        return
        [
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty
        ];
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Koalesziert einen potenziell <c>null</c>en Materialisierungsdateinamen.
    /// </summary>
    public static string CoalesceMaterializedFileName(string? value)
    {
        return value ?? string.Empty;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Normalisiert einen Materialisierungsdateinamen auf ein sicheres, deterministisches Dateitoken.
    /// </summary>
    public static string NormalizeMaterializedFileName(string? candidate)
    {
        var normalized = (candidate ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultMaterializedFileName;
        }

        try
        {
            normalized = Path.GetFileName(normalized);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or
            System.Security.SecurityException or
            IOException or
            NotSupportedException or
            ArgumentException)
        {
            return DefaultMaterializedFileName;
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultMaterializedFileName;
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            if (normalized.IndexOf(invalidChar) >= 0)
            {
                return DefaultMaterializedFileName;
            }
        }

        return normalized;
    }
}
