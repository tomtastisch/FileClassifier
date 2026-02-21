' ============================================================================
' FILE: IterableUtils.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
'
' Kontext:
' - Minimale Array-/Iterable-Helfer (Defensive Copies).
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier.Utils

    ''' <summary>
    '''     Utility-Funktionen für defensive Kopien (Array-basierte Rückgaben).
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Zweck:
    '''         - Verhindert, dass interne Arrays über Public API als Referenz nach außen geleakt werden.
    '''         - Unterstützt defensive Copies bei Rückgaben und Snapshots.
    '''     </para>
    '''     <para>
    '''         Fail-Closed:
    '''         - <c>Nothing</c> bleibt <c>Nothing</c>; es findet keine implizite Erzeugung leerer Arrays statt.
    '''     </para>
    ''' </remarks>
    Friend NotInheritable Class IterableUtils

        Private Sub New()
        End Sub


        ' =====================================================================
        ' Public API (Shared; Utility, stateless)
        ' =====================================================================

        ''' <summary>
        '''     Erstellt eine defensive Kopie von <paramref name="source"/>.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Nullprüfung,
        '''         2) Kopie via <see cref="System.Array.Clone"/> (shallow copy),
        '''         3) Rückgabe als typisiertes Array.
        '''     </para>
        '''     <para>
        '''         Hinweis:
        '''         - Bei Referenztypen werden die Referenzen kopiert (shallow copy), nicht die Objekte selbst.
        '''     </para>
        ''' </remarks>
        ''' <typeparam name="T">Elementtyp.</typeparam>
        ''' <param name="source">Quelle; <c>Nothing</c> bleibt <c>Nothing</c>.</param>
        ''' <returns>Defensive Kopie oder <c>Nothing</c>.</returns>
        Public Shared Function CloneArray(Of T)(source As T()) As T()

            ' Deklarationsblock
            Dim copy() As T

            If source Is Nothing Then Return Nothing

            copy = CType(source.Clone(), T())
            Return copy

        End Function

    End Class

End Namespace
