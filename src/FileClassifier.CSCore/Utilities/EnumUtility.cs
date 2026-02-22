using System.Globalization;

namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Deterministische Enum-Hilfsmethoden mit Sortierung und Bereichsschnitt.
/// </summary>
public static class EnumUtility
{
    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Liefert Enum-Werte mit optionaler Sortierung und Index-Ausschnittbildung.
    /// </summary>
    public static TEnum[] GetValues<TEnum>(int sortOrder, int? fromIndex, int? toIndex)
        where TEnum : struct, Enum
    {
        var values = (TEnum[])Enum.GetValues(typeof(TEnum));
        var count = values.Length;
        if (count == 0)
        {
            return Array.Empty<TEnum>();
        }

        if (sortOrder != 0)
        {
            var keys = new long[count];
            for (var i = 0; i < count; i++)
            {
                keys[i] = Convert.ToInt64(values[i], CultureInfo.InvariantCulture);
            }

            Array.Sort(keys, values);

            if (sortOrder == 2)
            {
                Array.Reverse(values);
            }
        }

        var maxIndex = count - 1;
        var effectiveTo = toIndex ?? maxIndex;
        effectiveTo = Math.Min(Math.Max(effectiveTo, 0), maxIndex);

        var effectiveMaxFrom = toIndex.HasValue ? effectiveTo : maxIndex;
        var effectiveFrom = fromIndex ?? 0;
        effectiveFrom = Math.Min(Math.Max(effectiveFrom, 0), effectiveMaxFrom);

        var length = (effectiveTo - effectiveFrom) + 1;
        var result = new TEnum[length];
        Array.Copy(values, effectiveFrom, result, 0, length);
        return result;
    }
}
