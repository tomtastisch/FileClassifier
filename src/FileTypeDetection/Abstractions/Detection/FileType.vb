Option Strict On
Option Explicit On

Imports System.Collections.Immutable

Namespace FileTypeDetection
    ''' <summary>
    '''     Unveränderliches Wertobjekt für einen Dateityp.
    '''     SSOT-Regel:
    '''     - Instanzen werden zentral in FileTypeRegistry aufgebaut.
    '''     - Aufrufer sollen keine ad-hoc FileType-Objekte erstellen.
    ''' </summary>
    Public NotInheritable Class FileType
        ''' <summary>Enum-Schluessel des Typs.</summary>
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
        '''     Normalisierte Alias-Liste (ohne fuehrenden Punkt, dedupliziert).
        ''' </summary>
        Public ReadOnly Property Aliases As ImmutableArray(Of String)

        Friend Sub New(kind As FileKind, canonicalExtension As String, mime As String, allowed As Boolean,
                       aliases As IEnumerable(Of String))
            Me.Kind = kind
            Me.CanonicalExtension = If(String.IsNullOrWhiteSpace(canonicalExtension), String.Empty, canonicalExtension)
            Me.Mime = If(mime, String.Empty)
            Me.Allowed = allowed

            Dim dedup = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If aliases IsNot Nothing Then
                For Each a In aliases
                    Dim n = FileTypeRegistry.NormalizeAlias(a)
                    If n.Length > 0 Then dedup.Add(n)
                Next
            End If

            If dedup.Count = 0 Then
                Me.Aliases = ImmutableArray (Of String).Empty
            Else
                Dim orderedAliases = dedup.ToList()
                orderedAliases.Sort(StringComparer.Ordinal)
                Me.Aliases = orderedAliases.ToImmutableArray()
            End If
        End Sub

        Public Overrides Function ToString() As String
            Return Kind.ToString()
        End Function
    End Class
End Namespace
