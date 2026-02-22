using Riok.Mapperly.Abstractions;
using Tomtastisch.FileClassifier.CSCore.Model;

namespace Tomtastisch.FileClassifier.CSCore.Mapping;

/// <summary>
/// <b>Zweck:</b><br/>
/// Kompilierzeit-Zuordnung für das Klonen unveränderlicher Options-Momentaufnahmen.
/// </summary>
[Mapper]
public static partial class ProjectOptionsSnapshotMapper
{
    /// <summary>
    /// <b>Zuordnung:</b><br/>
    /// Klont eine Projektoptions-Momentaufnahme.
    /// </summary>
    public static partial ProjectOptionsSnapshot Clone(ProjectOptionsSnapshot source);

    /// <summary>
    /// <b>Zuordnung:</b><br/>
    /// Klont eine Hashoptions-Momentaufnahme.
    /// </summary>
    public static partial HashOptionsSnapshot Clone(HashOptionsSnapshot source);
}
