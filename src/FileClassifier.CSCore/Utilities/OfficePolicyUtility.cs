namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Deterministische Richtlinienentscheidungen für OpenXML/OpenDocument und historische Office-Markerauflösung.
/// </summary>
public static class OfficePolicyUtility
{
    public const string KindUnknown = "Unknown";
    public const string KindDoc = "Doc";
    public const string KindXls = "Xls";
    public const string KindPpt = "Ppt";

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Löst einen OpenDocument-MIME-Wert in gruppierte Office-Kind-Schlüssel auf.
    /// </summary>
    public static string ResolveOpenDocumentMimeKindKey(string? normalizedMime)
    {
        var mime = (normalizedMime ?? string.Empty).Trim().ToLowerInvariant();

        return mime switch
        {
            "application/vnd.oasis.opendocument.text" => KindDoc,
            "application/vnd.oasis.opendocument.text-template" => KindDoc,
            "application/vnd.oasis.opendocument.spreadsheet" => KindXls,
            "application/vnd.oasis.opendocument.spreadsheet-template" => KindXls,
            "application/vnd.oasis.opendocument.presentation" => KindPpt,
            "application/vnd.oasis.opendocument.presentation-template" => KindPpt,
            _ => KindUnknown
        };
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Löst Archivpaket-Markersignale auf gruppierte Office-Kind-Schlüssel auf.
    /// </summary>
    public static string ResolveArchivePackageKindKey(
        bool hasContentTypes,
        bool hasDocxMarker,
        bool hasXlsxMarker,
        bool hasPptxMarker,
        string? openDocumentKindKey,
        bool hasOpenDocumentConflict)
    {
        var normalizedOpenDocumentKind = NormalizeKindKey(openDocumentKindKey);

        if (hasContentTypes)
        {
            var structuredMarkerCount = 0;
            if (hasDocxMarker)
            {
                structuredMarkerCount++;
            }

            if (hasXlsxMarker)
            {
                structuredMarkerCount++;
            }

            if (hasPptxMarker)
            {
                structuredMarkerCount++;
            }

            if (structuredMarkerCount > 1)
            {
                return KindUnknown;
            }

            if (!string.Equals(normalizedOpenDocumentKind, KindUnknown, StringComparison.Ordinal))
            {
                return KindUnknown;
            }

            if (hasDocxMarker)
            {
                return KindDoc;
            }

            if (hasXlsxMarker)
            {
                return KindXls;
            }

            if (hasPptxMarker)
            {
                return KindPpt;
            }
        }

        if (hasOpenDocumentConflict)
        {
            return KindUnknown;
        }

        return normalizedOpenDocumentKind;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Löst historische OLE-Markersignale auf gruppierte Office-Kind-Schlüssel auf.
    /// </summary>
    public static string ResolveLegacyMarkerKindKey(bool hasWord, bool hasExcel, bool hasPowerPoint)
    {
        var markerCount = 0;
        if (hasWord)
        {
            markerCount++;
        }

        if (hasExcel)
        {
            markerCount++;
        }

        if (hasPowerPoint)
        {
            markerCount++;
        }

        if (markerCount != 1)
        {
            return KindUnknown;
        }

        if (hasWord)
        {
            return KindDoc;
        }

        if (hasExcel)
        {
            return KindXls;
        }

        if (hasPowerPoint)
        {
            return KindPpt;
        }

        return KindUnknown;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Normalisiert Kind-Schlüssel auf bekannte Werte und fällt im Sinne von <c>fail-closed</c> auf <c>Unknown</c> zurück.
    /// </summary>
    public static string NormalizeKindKey(string? kindKey)
    {
        var key = (kindKey ?? string.Empty).Trim();

        if (string.Equals(key, KindDoc, StringComparison.OrdinalIgnoreCase))
        {
            return KindDoc;
        }

        if (string.Equals(key, KindXls, StringComparison.OrdinalIgnoreCase))
        {
            return KindXls;
        }

        if (string.Equals(key, KindPpt, StringComparison.OrdinalIgnoreCase))
        {
            return KindPpt;
        }

        return KindUnknown;
    }
}
