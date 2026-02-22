namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Defensiv ausgelegte Hilfsmethoden für Aufzählungssequenzen.
/// </summary>
public static class IterableUtility
{
    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Liefert eine geklonte Array-Instanz oder <c>null</c>, wenn die Quelle <c>null</c> ist.
    /// </summary>
    public static T[]? CloneArray<T>(T[]? source)
    {
        if (source is null)
        {
            return null;
        }

        return (T[])source.Clone();
    }
}
