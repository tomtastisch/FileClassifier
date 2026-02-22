namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Schutz-Hilfsmethoden für deterministische Argumentvalidierung.
/// </summary>
public static class GuardUtility
{
    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Wirft <see cref="ArgumentNullException"/>, wenn der Wert <c>null</c> ist.
    /// </summary>
    public static void NotNull(object? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Erzwingt einen exakten Array-Längenvertrag.
    /// </summary>
    public static void RequireLength(Array? value, int expectedLength, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        var actualLength = value.Length;
        if (actualLength != expectedLength)
        {
            throw new ArgumentException(
                $"Expected length {expectedLength}, but was {actualLength}.",
                paramName);
        }
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Erzwingt, dass der Wert für den angegebenen Enum-Typ definiert ist.
    /// </summary>
    public static void RequireEnumDefined(Type? enumType, object? value, string paramName)
    {
        if (enumType is null)
        {
            throw new ArgumentNullException(nameof(enumType));
        }

        if (!enumType.IsEnum)
        {
            throw new ArgumentException("enumType muss ein Enum-Typ sein.", nameof(enumType));
        }

        var isDefined = Enum.IsDefined(enumType, value!);
        if (!isDefined)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }
}
