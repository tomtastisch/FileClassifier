using System.IO;

namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Geteilte Ausnahmefiltermengen für <c>fail-closed</c>-Schutzpfade und Protokollierungspfade.
/// </summary>
public static class ExceptionFilterUtility
{
    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Prüft, ob eine Ausnahme zur Archivvalidierungsmenge gehört.
    /// </summary>
    public static bool IsArchiveValidationException(Exception? ex)
    {
        return ex is UnauthorizedAccessException or
            System.Security.SecurityException or
            IOException or
            InvalidDataException or
            NotSupportedException or
            ArgumentException or
            InvalidOperationException or
            ObjectDisposedException;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Prüft, ob eine Ausnahme zur Pfadnormalisierungsmenge gehört.
    /// </summary>
    public static bool IsPathNormalizationException(Exception? ex)
    {
        return ex is UnauthorizedAccessException or
            System.Security.SecurityException or
            IOException or
            NotSupportedException or
            ArgumentException;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Prüft, ob eine Ausnahme zur Pfadauflösungsmenge gehört.
    /// </summary>
    public static bool IsPathResolutionException(Exception? ex)
    {
        return IsPathNormalizationException(ex) ||
            ex is PathTooLongException;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Prüft, ob eine Ausnahme zur Logger-Schreibmenge gehört.
    /// </summary>
    public static bool IsLoggerWriteException(Exception? ex)
    {
        return ex is InvalidOperationException or
            ObjectDisposedException or
            FormatException or
            ArgumentException;
    }
}
