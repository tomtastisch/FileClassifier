using System;
using System.IO;

namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Deterministische Archiv-/Pfad-Richtlinienhelfer für die VB-Laufzeit-Brücke.
/// </summary>
public static class ArchivePathPolicyUtility
{
    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Normalisiert und validiert relative Archiveintragspfade.<br/>
    /// Rückgabeformat: [istGültig(bool), normalisierterPfad(string), istVerzeichnis(bool)].
    /// </summary>
    public static object[] NormalizeRelativePath(string? rawPath, bool allowDirectoryMarker)
    {
        if (!TryPrepareRelativePath(rawPath, out var preparedPath))
        {
            return [false, string.Empty, false];
        }

        var trimmed = preparedPath.TrimEnd('/');
        if (trimmed.Length == 0)
        {
            if (!allowDirectoryMarker)
            {
                return [false, string.Empty, false];
            }

            return [true, preparedPath, true];
        }

        if (!HasOnlyAllowedPathSegments(trimmed))
        {
            return [false, string.Empty, false];
        }

        if (preparedPath.Length != trimmed.Length && !allowDirectoryMarker)
        {
            return [false, string.Empty, false];
        }

        var normalizedPath = allowDirectoryMarker ? preparedPath : trimmed;
        var isDirectory = allowDirectoryMarker && preparedPath.Length != trimmed.Length;
        return [true, normalizedPath, isDirectory];
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Ermittelt, ob ein Zielpfad auf das Dateisystem-Wurzelverzeichnis zeigt.
    /// </summary>
    public static bool IsRootPath(string? destinationFull)
    {
        if (string.IsNullOrWhiteSpace(destinationFull))
        {
            return false;
        }

        var destinationPath = (destinationFull ?? string.Empty).Trim();
        string? rootPath;
        try
        {
            rootPath = Path.GetPathRoot(destinationPath);
        }
        catch (Exception ex) when (ExceptionFilterUtility.IsPathNormalizationException(ex))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        return string.Equals(
            destinationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryPrepareRelativePath(string? rawPath, out string preparedPath)
    {
        preparedPath = (rawPath ?? string.Empty).Trim();
        if (preparedPath.Length == 0)
        {
            return false;
        }

        if (preparedPath.Contains('\0'))
        {
            return false;
        }

        if (Path.IsPathRooted(preparedPath))
        {
            return false;
        }

        preparedPath = preparedPath.Replace('\\', '/').TrimStart('/');
        return preparedPath.Length > 0;
    }

    private static bool HasOnlyAllowedPathSegments(string pathValue)
    {
        var segments = pathValue.Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (segment.Length == 0)
            {
                return false;
            }

            if (string.Equals(segment, ".", StringComparison.Ordinal) ||
                string.Equals(segment, "..", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
