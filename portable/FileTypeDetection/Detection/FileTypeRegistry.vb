Option Strict On
Option Explicit On

Imports System.Collections.Immutable

Namespace FileTypeDetection

    ''' <summary>
    ''' Zentrale Registry als SSOT fuer Typmetadaten und Alias-Aufloesung.
    '''
    ''' Regeln:
    ''' - Neue Typen nur hier registrieren.
    ''' - Aufloesung ist deterministisch und case-insensitive.
    ''' - Unknown ist immer als fail-closed Fallback vorhanden.
    ''' </summary>
    Friend NotInheritable Class FileTypeRegistry
        Private Sub New()
        End Sub

        Friend Shared ReadOnly TypesByKind As ImmutableDictionary(Of FileKind, FileType)
        Friend Shared ReadOnly KindByAlias As ImmutableDictionary(Of String, FileKind)

        Shared Sub New()
            TypesByKind = BuildTypes()
            KindByAlias = BuildAliasMap(TypesByKind)
        End Sub

        Private Shared Function BuildTypes() As ImmutableDictionary(Of FileKind, FileType)
            Dim mimeProvider As MimeProvider = MimeProvider.Instance
            Dim b = ImmutableDictionary.CreateBuilder(Of FileKind, FileType)()

            b(FileKind.Unknown) = New FileType(FileKind.Unknown, Nothing, Nothing, False, ImmutableArray(Of String).Empty)

            For Each d In KnownTypeDefinitions()
                b(d.kind) = New FileType(d.kind, d.extension, mimeProvider.GetMime(d.extension), True, d.aliases)
            Next

            Return b.ToImmutable()
        End Function

        ''' <summary>
        ''' Kanonische Typdefinitionen in einer Liste (SSOT fuer Metadaten-Initialisierung).
        ''' </summary>
        Private Shared Function KnownTypeDefinitions() As (kind As FileKind, extension As String, aliases As String())()
            Return {
                (FileKind.Pdf, ".pdf", {"pdf"}),
                (FileKind.Png, ".png", {"png"}),
                (FileKind.Jpeg, ".jpg", {"jpg", "jpeg", "jpe"}),
                (FileKind.Gif, ".gif", {"gif"}),
                (FileKind.Webp, ".webp", {"webp"}),
                (FileKind.Zip, ".zip", {"zip"}),
                (FileKind.Docx, ".docx", {"docx"}),
                (FileKind.Xlsx, ".xlsx", {"xlsx"}),
                (FileKind.Pptx, ".pptx", {"pptx"})
            }
        End Function

        Private Shared Function BuildAliasMap(types As ImmutableDictionary(Of FileKind, FileType)) As ImmutableDictionary(Of String, FileKind)
            If types Is Nothing Then Return ImmutableDictionary(Of String, FileKind).Empty

            Dim b = ImmutableDictionary.CreateBuilder(Of String, FileKind)(StringComparer.OrdinalIgnoreCase)
            For Each kv In types
                Dim t = kv.Value
                If t Is Nothing Then Continue For
                If t.Aliases.IsDefault OrElse t.Aliases.Length = 0 Then Continue For

                For Each a In t.Aliases
                    Dim n = NormalizeAlias(a)
                    If n.Length = 0 Then Continue For
                    b(n) = kv.Key
                Next
            Next

            Return b.ToImmutable()
        End Function

        Friend Shared Function NormalizeAlias(raw As String) As String
            Dim s = If(raw, String.Empty).Trim()
            If s.StartsWith(".", StringComparison.Ordinal) Then s = s.Substring(1)
            Return s.ToLowerInvariant()
        End Function

        ''' <summary>
        ''' Liefert den zugeordneten FileType fuer einen Enumwert.
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <returns>Registrierter Typ oder Unknown.</returns>
        Friend Shared Function Resolve(kind As FileKind) As FileType
            Dim t As FileType = Nothing
            If TypesByKind.TryGetValue(kind, t) AndAlso t IsNot Nothing Then Return t
            Return TypesByKind(FileKind.Unknown)
        End Function

        ''' <summary>
        ''' Liefert den zugeordneten FileType fuer einen Aliaswert.
        ''' </summary>
        ''' <param name="aliasKey">Alias mit oder ohne fuehrenden Punkt.</param>
        ''' <returns>Registrierter Typ oder Unknown.</returns>
        Friend Shared Function ResolveByAlias(aliasKey As String) As FileType
            Dim k As FileKind = FileKind.Unknown
            If KindByAlias.TryGetValue(NormalizeAlias(aliasKey), k) Then
                Return Resolve(k)
            End If
            Return Resolve(FileKind.Unknown)
        End Function

    End Class

End Namespace
