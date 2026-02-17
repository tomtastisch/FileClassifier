' ============================================================================
' FILE: FileType.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.Collections.Immutable

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Unveränderliches Wertobjekt, das einen aufgelösten Dateityp einschließlich Metadaten beschreibt.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         SSOT-Regel: Instanzen werden zentral über <c>FileTypeRegistry</c> aufgebaut.
    '''     </para>
    '''     <para>
    '''         Konsumenten erhalten ein stabiles, read-only API-Modell mit kanonischer Endung, MIME und Aliasmenge.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class FileType
        ''' <summary>Enum-Schlüssel des Typs.</summary>
        Public ReadOnly Property Kind As FileKind

        ''' <summary>Kanonische Endung inklusive Punkt, bei Unknown leer.</summary>
        Public ReadOnly Property CanonicalExtension As String

        ''' <summary>Kanonischer MIME-Typ als Metadatum, kann leer sein.</summary>
        Public ReadOnly Property Mime As String

        ''' <summary>
        '''     Kennzeichnet, ob der Typ in der aktuellen Policy erlaubt ist.
        ''' </summary>
        Public ReadOnly Property Allowed As Boolean

        ''' <summary>
        '''     Normalisierte Alias-Liste (ohne führenden Punkt, dedupliziert).
        ''' </summary>
        Public ReadOnly Property Aliases As ImmutableArray(Of String)

        Friend Sub New(kind As FileKind, canonicalExtension As String, mime As String, allowed As Boolean,
                       aliases As IEnumerable(Of String))
            Dim dedup As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim n As String
            Dim orderedAliases As List(Of String)

            Me.Kind = kind
            Me.CanonicalExtension = If(String.IsNullOrWhiteSpace(canonicalExtension), String.Empty, canonicalExtension)
            Me.Mime = If(mime, String.Empty)
            Me.Allowed = allowed

            If aliases IsNot Nothing Then
                For Each a In aliases
                    n = FileTypeRegistry.NormalizeAlias(a)
                    If n.Length > 0 Then dedup.Add(n)
                Next
            End If

            If dedup.Count = 0 Then
                Me.Aliases = ImmutableArray(Of String).Empty
            Else
                orderedAliases = dedup.ToList()
                orderedAliases.Sort(StringComparer.Ordinal)
                Me.Aliases = orderedAliases.ToImmutableArray()
            End If
        End Sub

        ''' <summary>
        '''     Liefert die textuelle Repräsentation des Dateityps auf Basis des Enum-Schlüssels.
        ''' </summary>
        ''' <returns>String-Repräsentation des Feldes <see cref="Kind"/>.</returns>
        Public Overrides Function ToString() As String
            Return Kind.ToString()
        End Function
    End Class
End Namespace
