using System.IO;
using Tomtastisch.FileClassifier.CSCore.Mapping;
using Tomtastisch.FileClassifier.CSCore.Model;

namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Zentralisierte Detektions-Richtlinienhelfer für Endungsprüfung, <c>ReasonCode</c>-Zuordnung<br/>
/// und deterministische Projektionswerte der VB-Laufzeit-Brücke.
/// </summary>
public static class DetectionPolicyUtility
{
    public const string ReasonException = "Exception";
    public const string ReasonExceptionUnauthorizedAccess = "ExceptionUnauthorizedAccess";
    public const string ReasonExceptionSecurity = "ExceptionSecurity";
    public const string ReasonExceptionIO = "ExceptionIO";
    public const string ReasonExceptionInvalidData = "ExceptionInvalidData";
    public const string ReasonExceptionNotSupported = "ExceptionNotSupported";
    public const string ReasonExceptionArgument = "ExceptionArgument";

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Mappt Laufzeitausnahmen auf deterministische <c>ReasonCode</c>-Werte.
    /// </summary>
    public static string ExceptionToReasonCode(Exception? ex)
    {
        if (ex is null)
        {
            return ReasonException;
        }

        return ex switch
        {
            UnauthorizedAccessException => ReasonExceptionUnauthorizedAccess,
            System.Security.SecurityException => ReasonExceptionSecurity,
            IOException => ReasonExceptionIO,
            InvalidDataException => ReasonExceptionInvalidData,
            NotSupportedException => ReasonExceptionNotSupported,
            ArgumentException => ReasonExceptionArgument,
            _ => ReasonException
        };
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Prüft Endungskompatibilität gegen kanonische Endung und Aliasliste.
    /// </summary>
    public static bool IsExtensionMatch(
        string? path,
        bool detectedIsUnknown,
        string? canonicalExtension,
        string[]? aliases,
        string? mimeType,
        int headerBytes)
    {
        var summary = BuildSummary(canonicalExtension, mimeType, headerBytes);

        var ext = Path.GetExtension(path ?? string.Empty);
        if (string.IsNullOrWhiteSpace(ext))
        {
            return true;
        }

        if (detectedIsUnknown)
        {
            return false;
        }

        var normalizedExt = NormalizeAlias(ext);
        var normalizedCanonical = NormalizeAlias(summary.CanonicalExtension);
        if (string.Equals(normalizedExt, normalizedCanonical, StringComparison.Ordinal))
        {
            return true;
        }

        if (aliases is null || aliases.Length == 0)
        {
            return false;
        }

        for (var index = 0; index < aliases.Length; index++)
        {
            if (string.Equals(NormalizeAlias(aliases[index]), normalizedExt, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Baut deterministische Zusammenfassungswerte, um manuellen Zuordnungscode in VB zu vermeiden.
    /// </summary>
    public static object[] BuildSummaryValues(string? canonicalExtension, string? mimeType, int headerBytes)
    {
        var summary = BuildSummary(canonicalExtension, mimeType, headerBytes);

        return
        [
            summary.CanonicalExtension,
            summary.MimeType,
            summary.HeaderBytes,
            summary.HasStructuredMime
        ];
    }

    private static DetectionSummary BuildSummary(string? canonicalExtension, string? mimeType, int headerBytes)
    {
        var signal = new DetectionSignal(canonicalExtension ?? string.Empty, mimeType ?? string.Empty, headerBytes);
        return FileDetectionMapper.ToSummary(signal);
    }

    private static string NormalizeAlias(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (value[0] == '.')
        {
            value = value.Substring(1);
        }

        return value.ToLowerInvariant();
    }
}
