using Riok.Mapperly.Abstractions;
using Tomtastisch.FileClassifier.CSCore.Model;

namespace Tomtastisch.FileClassifier.CSCore.Mapping;

/// <summary>
/// <b>Zweck:</b><br/>
/// Kompilierzeit-Projektionszuordnung für Transformationen von Detektionsmodellen.
/// </summary>
[Mapper]
public static partial class FileDetectionMapper
{
    /// <summary>
    /// <b>Zuordnung:</b><br/>
    /// Projiziert ein Detektionssignal in eine normalisierte Detektionszusammenfassung.
    /// </summary>
    public static partial DetectionSummary ToSummary(DetectionSignal signal);
}
