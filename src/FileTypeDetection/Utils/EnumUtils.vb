' ============================================================================
' FILE: EnumUtils.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
'
' Kontext:
' - Allgemeine Enum-Helfer (Java-ähnliches values()).
' - Liefert Enum-Werte als typisiertes Array, optional sortiert und optional als Index-Range.
' ============================================================================

Option Strict On
Option Explicit On

Imports System

Namespace Global.Tomtastisch.FileClassifier.Utils

    ''' <summary>
    '''     Utility-Funktionen für Enum-Typen (values()).
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Zweck:
    '''         - Liefert Enum-Werte als typisiertes Array ohne LINQ.
    '''         - Optional: Sortierung nach numerischem Enum-Wert.
    '''         - Optional: Index-Range (from/to) mit deterministischem Clamping.
    '''     </para>
    '''     <para>
    '''         Nicht-Ziele:
    '''         - Keine zustandsbehaftete Logik.
    '''         - Keine Abhängigkeiten auf Projektdienste (I/O, Logger, Policy-Engine).
    '''         - Keine Reflection-Features außer <see cref="System.Enum.GetValues(Type)"/>.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class EnumUtils

        Private Sub New()
        End Sub


        ' =====================================================================
        ' Internal (Projekt-intern): Sortier-Optionen
        ' =====================================================================

        Friend Enum EnumSortOrder
            None = 0
            Ascending = 1
            Descending = 2
        End Enum


        ' =====================================================================
        ' Public API
        ' =====================================================================

        ''' <summary>
        '''     Liefert alle Werte eines Enum-Typs als typisiertes Array.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Werte werden über <see cref="System.Enum.GetValues(Type)"/> geladen,
        '''         2) Ausgabe erfolgt als typisiertes Array <c>TEnum()</c>.
        '''     </para>
        '''     <para>
        '''         Hinweis:
        '''         - Diese öffentliche Überladung liefert bewusst die gesamte Menge ohne Sortierung und ohne Range.
        '''         - Erweiterte Optionen (Sortierung/Range) sind projektintern gehalten.
        '''     </para>
        ''' </remarks>
        ''' <typeparam name="TEnum">Enum-Typ.</typeparam>
        ''' <returns>Enum-Werte als Array.</returns>
        ''' <exception cref="ArgumentException">Wird ausgelöst, wenn <typeparamref name="TEnum"/> kein Enum ist.</exception>
        ''' <example>
        '''     <code language="vb">
        ''' ' Beispiel: alle Werte ohne Sortierung/Range
        ''' Dim values() As ExampleSlot = EnumUtils.GetValues(Of ExampleSlot)()
        ''' For Each v As ExampleSlot In values
        '''     Console.WriteLine(v)
        ''' Next
        '''     </code>
        ''' </example>
        Public Shared Function GetValues(Of TEnum As Structure)() As TEnum()

            Return GetValues(Of TEnum)(EnumSortOrder.None, fromIndex:=Nothing, toIndex:=Nothing)

        End Function


        ' =====================================================================
        ' Internal API (Projekt-intern): Sortierung + Range
        ' =====================================================================

        ''' <summary>
        '''     Liefert Enum-Werte als typisiertes Array (optional sortiert) und optional als Index-Range.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Werte werden über <see cref="System.Enum.GetValues(Type)"/> geladen,
        '''         2) optional: Sortierung nach numerischem Enum-Wert,
        '''         3) Range wird deterministisch geklemmt,
        '''         4) Ausgabe erfolgt als Slice über <see cref="System.Array.Copy(System.Array, Integer, System.Array, Integer, Integer)"/>.
        '''     </para>
        '''     <para>
        '''         Range-Semantik (0-basiert, inklusive):
        '''         - Keine Angabe: gesamte Menge.
        '''         - Nur <paramref name="fromIndex"/>: von <paramref name="fromIndex"/> bis letztes Element.
        '''         - Nur <paramref name="toIndex"/>: von 0 bis <paramref name="toIndex"/>.
        '''         - Beide: von <paramref name="fromIndex"/> bis <paramref name="toIndex"/>.
        '''     </para>
        '''     <para>
        '''         Clamping-Regeln (deterministisch):
        '''         1) <paramref name="toIndex"/> wird zuerst geklemmt:
        '''            - <paramref name="toIndex"/> &lt; 0     =&gt; 0
        '''            - <paramref name="toIndex"/> &gt; max   =&gt; max
        '''            - <paramref name="toIndex"/> Nothing  =&gt; max
        '''         2) <paramref name="fromIndex"/> wird danach geklemmt:
        '''            - <paramref name="fromIndex"/> Nothing =&gt; 0
        '''            - <paramref name="fromIndex"/> &lt; 0    =&gt; 0
        '''            - <paramref name="fromIndex"/> &gt; max2 =&gt; max2
        '''              wobei max2 = geklemmter <paramref name="toIndex"/> (falls gesetzt), sonst max.
        '''     </para>
        '''     <para>
        '''         Fail-Closed:
        '''         - Ist <typeparamref name="TEnum"/> kein Enum, wird eine <see cref="ArgumentException"/> ausgelöst.
        '''         - Leere Enums liefern ein leeres Array.
        '''     </para>
        ''' </remarks>
        ''' <typeparam name="TEnum">Enum-Typ.</typeparam>
        ''' <param name="sortOrder">
        '''     Sortierreihenfolge:
        '''     - <see cref="EnumSortOrder.None"/>: keine Sortierung (Originalreihenfolge von <see cref="System.Enum.GetValues(Type)"/>).
        '''     - <see cref="EnumSortOrder.Ascending"/>: aufsteigend nach numerischem Enum-Wert.
        '''     - <see cref="EnumSortOrder.Descending"/>: absteigend nach numerischem Enum-Wert.
        ''' </param>
        ''' <param name="fromIndex">Startindex (0-basiert, inklusive); Nothing bedeutet 0.</param>
        ''' <param name="toIndex">Endindex (0-basiert, inklusive); Nothing bedeutet max.</param>
        ''' <returns>Enum-Werte als Array (ggf. sortiert und/oder gefiltert).</returns>
        ''' <exception cref="ArgumentException">Wird ausgelöst, wenn <typeparamref name="TEnum"/> kein Enum ist.</exception>
        ''' <example>
        '''     <code language="vb">
        ''' ' Aufsteigend sortiert, dann Slice der Indizes 1..3 (inklusive)
        ''' Dim slice() As ExampleSlot = EnumUtils.GetValues(Of ExampleSlot)(
        '''     sortOrder:=EnumUtils.EnumSortOrder.Ascending,
        '''     fromIndex:=1,
        '''     toIndex:=3
        ''' )
        '''     </code>
        ''' </example>
        Friend Shared Function GetValues(Of TEnum As Structure) _
            (
                Optional sortOrder As EnumSortOrder = EnumSortOrder.None,
                Optional fromIndex As Nullable(Of Integer) = Nothing,
                Optional toIndex As Nullable(Of Integer) = Nothing
            ) As TEnum()

            ' Deklarationsblock
            Dim enumType            As Type      = GetType(TEnum)
            Dim raw                 As Array     = Nothing

            Dim values()            As TEnum     = Nothing
            Dim keys()              As Long      = Nothing

            Dim i                   As Integer
            Dim count               As Integer   = 0
            Dim maxIndex            As Integer   = 0

            Dim effectiveTo         As Integer   = 0
            Dim effectiveMaxFrom    As Integer   = 0
            Dim effectiveFrom       As Integer   = 0

            Dim length              As Integer   = 0
            Dim result()            As TEnum     = Nothing


            ' -----------------------------------------------------------------
            ' Guard-Clauses
            ' -----------------------------------------------------------------
            If Not enumType.IsEnum Then
                Throw New ArgumentException("TEnum muss ein Enum-Typ sein.", NameOf(TEnum))
            End If


            ' -----------------------------------------------------------------
            ' Werte laden
            ' -----------------------------------------------------------------
            raw = [Enum].GetValues(enumType)

            count = raw.Length
            If count = 0 Then
                Return New TEnum() {}
            End If

            maxIndex = count - 1

            values = New TEnum(count - 1) {}
            For i = 0 To count - 1
                values(i) = CType(raw.GetValue(i), TEnum)
            Next


            ' -----------------------------------------------------------------
            ' Optional: Sortierung (Keys nur bei Bedarf)
            ' -----------------------------------------------------------------
            If sortOrder <> EnumSortOrder.None Then

                keys = New Long(count - 1) {}
                For i = 0 To count - 1
                    keys(i) = Convert.ToInt64(values(i))
                Next

                Array.Sort(keys, values)

                If sortOrder = EnumSortOrder.Descending Then
                    Array.Reverse(values)
                End If

            End If


            ' -----------------------------------------------------------------
            ' Range: toIndex zuerst clampen, dann fromIndex
            ' -----------------------------------------------------------------
            effectiveTo = If(toIndex.HasValue, toIndex.Value, maxIndex)
            effectiveTo = Math.Min(Math.Max(effectiveTo, 0), maxIndex)

            effectiveMaxFrom = If(toIndex.HasValue, effectiveTo, maxIndex)

            effectiveFrom = If(fromIndex.HasValue, fromIndex.Value, 0)
            effectiveFrom = Math.Min(Math.Max(effectiveFrom, 0), effectiveMaxFrom)


            ' -----------------------------------------------------------------
            ' Slice kopieren (inklusive)
            ' -----------------------------------------------------------------
            length = (effectiveTo - effectiveFrom) + 1
            result = New TEnum(length - 1) {}

            Array.Copy(values, effectiveFrom, result, 0, length)

            Return result

        End Function

    End Class

End Namespace
