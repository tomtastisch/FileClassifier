Option Strict On
Option Explicit On

Imports System.Collections.Immutable
Imports System.Linq

Namespace FileTypeDetection
    ''' <summary>
    '''     Zentrale Registry als SSOT fuer Typmetadaten, Alias-Aufloesung und Magic-Patterns.
    '''     Regeln:
    '''     - Neue Typen werden primaer ueber FileKind erweitert.
    '''     - Metadaten werden deterministisch aus FileKind + zentralen Overrides aufgebaut.
    '''     - Unknown ist immer als fail-closed Fallback vorhanden.
    ''' </summary>
    Friend NotInheritable Class FileTypeRegistry
        Private Sub New()
        End Sub

        Friend Shared ReadOnly TypesByKind As ImmutableDictionary(Of FileKind, FileType)
        Friend Shared ReadOnly KindByAlias As ImmutableDictionary(Of String, FileKind)

        Private Shared ReadOnly ExtensionOverrides As ImmutableDictionary(Of FileKind, String) =
                                    ImmutableDictionary.CreateRange(Of FileKind, String)(
                                        {New KeyValuePair(Of FileKind, String)(FileKind.Jpeg, ".jpg")})

        Private Shared ReadOnly AliasOverrides As ImmutableDictionary(Of FileKind, ImmutableArray(Of String)) =
                                    ImmutableDictionary.CreateRange(Of FileKind, ImmutableArray(Of String))(
                                        { _
                                            New KeyValuePair(Of FileKind, ImmutableArray(Of String))(FileKind.Jpeg,
                                                                                                     ImmutableArray.
                                                                                                        Create("jpe")),
                                            New KeyValuePair(Of FileKind, ImmutableArray(Of String))(FileKind.Zip,
                                                                                                     ImmutableArray.
                                                                                                        Create("tar",
                                                                                                               "tgz",
                                                                                                               "gz",
                                                                                                               "gzip",
                                                                                                               "bz2",
                                                                                                               "bzip2",
                                                                                                               "xz",
                                                                                                               "7z",
                                                                                                               "zz",
                                                                                                               "rar"))
                                        })

        Private Shared ReadOnly _
            MagicPatternCatalog As ImmutableDictionary(Of FileKind, ImmutableArray(Of MagicPattern)) =
                BuildMagicPatternCatalog()

        Private Shared ReadOnly MagicRules As ImmutableArray(Of MagicRule)

        Shared Sub New()
            Dim definitions = BuildDefinitionsFromEnum()
            TypesByKind = BuildTypes(definitions)
            KindByAlias = BuildAliasMap(TypesByKind)
            MagicRules = BuildMagicRules(definitions)
        End Sub

        Private Shared Function BuildDefinitionsFromEnum() As ImmutableArray(Of FileTypeDefinition)
            Dim b = ImmutableArray.CreateBuilder(Of FileTypeDefinition)()

            For Each kind In OrderedKinds()
                If kind = FileKind.Unknown Then Continue For

                Dim canonicalExtension = GetCanonicalExtension(kind)
                Dim aliases = BuildAliases(kind, canonicalExtension)
                Dim magicPatterns = GetMagicPatterns(kind)

                b.Add(New FileTypeDefinition(kind, canonicalExtension, aliases, magicPatterns))
            Next

            Return b.ToImmutable()
        End Function

        Private Shared Function OrderedKinds() As ImmutableArray(Of FileKind)
            Return [Enum].
                GetValues(Of FileKind)().
                OrderBy(Function(kind) CInt(kind)).
                ToImmutableArray()
        End Function

        Private Shared Function GetCanonicalExtension(kind As FileKind) As String
            Dim overrideExt As String = Nothing
            If ExtensionOverrides.TryGetValue(kind, overrideExt) Then
                Return overrideExt
            End If

            Return "." & NormalizeAlias(kind.ToString())
        End Function

        Private Shared Function BuildAliases(kind As FileKind, canonicalExtension As String) As String()
            Dim aliases As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            Dim extAlias = NormalizeAlias(canonicalExtension)
            If extAlias.Length > 0 Then aliases.Add(extAlias)

            Dim enumAlias = NormalizeAlias(kind.ToString())
            If enumAlias.Length > 0 Then aliases.Add(enumAlias)

            Dim additional As ImmutableArray(Of String) = ImmutableArray(Of String).Empty
            If AliasOverrides.TryGetValue(kind, additional) Then
                additional.
                    Select(Function(item) NormalizeAlias(item)).
                    Where(Function(normalized) normalized.Length > 0).
                    ToList().
                    ForEach(Sub(normalized) aliases.Add(normalized))
            End If

            Dim orderedAliases = aliases.ToList()
            orderedAliases.Sort(StringComparer.Ordinal)
            Return orderedAliases.ToArray()
        End Function

        Private Shared Function GetMagicPatterns(kind As FileKind) As ImmutableArray(Of MagicPattern)
            Dim patterns As ImmutableArray(Of MagicPattern) = ImmutableArray(Of MagicPattern).Empty
            If MagicPatternCatalog.TryGetValue(kind, patterns) Then
                Return patterns
            End If

            Return ImmutableArray(Of MagicPattern).Empty
        End Function

        Private Shared Function BuildTypes(definitions As ImmutableArray(Of FileTypeDefinition)) _
            As ImmutableDictionary(Of FileKind, FileType)
            Dim b = ImmutableDictionary.CreateBuilder(Of FileKind, FileType)()

            b(FileKind.Unknown) = New FileType(FileKind.Unknown, Nothing, Nothing, False,
                                               ImmutableArray(Of String).Empty)

            For Each d In definitions
                b(d.Kind) = New FileType(d.Kind, d.CanonicalExtension, MimeProvider.GetMime(d.CanonicalExtension), True,
                                         d.Aliases)
            Next

            Return b.ToImmutable()
        End Function

        Friend Shared Function DetectByMagic(header As Byte()) As FileKind
            If header Is Nothing OrElse header.Length = 0 Then Return FileKind.Unknown

            Dim match = MagicRules.
                SelectMany(Function(rule) rule.Patterns.
                    Select(Function(pattern) New With {.Rule = rule, .Pattern = pattern})).
                FirstOrDefault(Function(item)
                                   Dim segments = item.Pattern.Segments
                                   Return segments.All(Function(segment) HasSegment(header, segment))
                               End Function)

            Return If(match Is Nothing, FileKind.Unknown, match.Rule.Kind)
        End Function

        Friend Shared Function HasDirectHeaderDetection(kind As FileKind) As Boolean
            If kind = FileKind.Unknown Then Return False
            Dim patterns As ImmutableArray(Of MagicPattern) = ImmutableArray(Of MagicPattern).Empty
            Return MagicPatternCatalog.TryGetValue(kind, patterns) AndAlso Not patterns.IsDefaultOrEmpty
        End Function

        Friend Shared Function HasStructuredContainerDetection(kind As FileKind) As Boolean
            Return kind = FileKind.Docx OrElse
                   kind = FileKind.Xlsx OrElse
                   kind = FileKind.Pptx
        End Function

        Friend Shared Function HasDirectContentDetection(kind As FileKind) As Boolean
            Return HasDirectHeaderDetection(kind) OrElse HasStructuredContainerDetection(kind)
        End Function

        Friend Shared Function KindsWithoutDirectContentDetection() As ImmutableArray(Of FileKind)
            Return OrderedKinds().
                Where(Function(kind) kind <> FileKind.Unknown).
                Where(Function(kind) Not HasDirectContentDetection(kind)).
                ToImmutableArray()
        End Function

        Private Shared Function BuildMagicRules(definitions As ImmutableArray(Of FileTypeDefinition)) _
            As ImmutableArray(Of MagicRule)
            Return definitions.
                Where(Function(definition) Not definition.MagicPatterns.IsDefaultOrEmpty).
                Select(Function(definition) New MagicRule(definition.Kind, definition.MagicPatterns)).
                ToImmutableArray()
        End Function

        Private Shared Function BuildMagicPatternCatalog() _
            As ImmutableDictionary(Of FileKind, ImmutableArray(Of MagicPattern))
            Dim b = ImmutableDictionary.CreateBuilder(Of FileKind, ImmutableArray(Of MagicPattern))()

            b(FileKind.Pdf) = ImmutableArray.Create(
                Pattern(Prefix(0, &H25, &H50, &H44, &H46, &H2D)))

            b(FileKind.Png) = ImmutableArray.Create(
                Pattern(Prefix(0, &H89, &H50, &H4E, &H47, &HD, &HA, &H1A, &HA)))

            b(FileKind.Jpeg) = ImmutableArray.Create(
                Pattern(Prefix(0, &HFF, &HD8, &HFF)))

            b(FileKind.Gif) = ImmutableArray.Create(
                Pattern(Prefix(0, &H47, &H49, &H46, &H38, &H37, &H61)),
                Pattern(Prefix(0, &H47, &H49, &H46, &H38, &H39, &H61)))

            b(FileKind.Webp) = ImmutableArray.Create(
                Pattern(Prefix(0, &H52, &H49, &H46, &H46), Prefix(8, &H57, &H45, &H42, &H50)))

            b(FileKind.Zip) = ImmutableArray.Create(
                Pattern(Prefix(0, &H50, &H4B, &H3, &H4)),
                Pattern(Prefix(0, &H50, &H4B, &H5, &H6)),
                Pattern(Prefix(0, &H50, &H4B, &H7, &H8)))

            Return b.ToImmutable()
        End Function

        Private Shared Function Pattern(ParamArray segments As MagicSegment()) As MagicPattern
            Return New MagicPattern(ImmutableArray.Create(segments))
        End Function

        Private Shared Function Prefix(offset As Integer, ParamArray bytesValue As Byte()) As MagicSegment
            Return New MagicSegment(offset, ImmutableArray.Create(bytesValue))
        End Function

        Private Shared Function HasSegment(data As Byte(), segment As MagicSegment) As Boolean
            If data Is Nothing Then Return False
            If segment.Offset < 0 Then Return False
            If segment.Bytes.IsDefaultOrEmpty Then Return False

            Dim endPos = segment.Offset + segment.Bytes.Length
            If endPos < 0 OrElse data.Length < endPos Then Return False

            For i = 0 To segment.Bytes.Length - 1
                If data(segment.Offset + i) <> segment.Bytes(i) Then Return False
            Next
            Return True
        End Function

        Private Shared Function BuildAliasMap(types As ImmutableDictionary(Of FileKind, FileType)) _
            As ImmutableDictionary(Of String, FileKind)
            If types Is Nothing Then Return ImmutableDictionary(Of String, FileKind).Empty

            Dim entries = types.
                Where(Function(kv) kv.Value IsNot Nothing).
                Where(Function(kv) Not kv.Value.Aliases.IsDefault AndAlso kv.Value.Aliases.Length > 0).
                SelectMany(Function(kv) kv.Value.Aliases.
                    Select(Function(aliasValue) New With {
                        .Kind = kv.Key,
                        .Normalized = NormalizeAlias(aliasValue)
                    })).
                Where(Function(item) item.Normalized.Length > 0).
                ToList()

            Return entries.
                Aggregate(ImmutableDictionary.CreateBuilder(Of String, FileKind)(StringComparer.OrdinalIgnoreCase),
                          Function(builder, entry)
                              builder(entry.Normalized) = entry.Kind
                              Return builder
                          End Function).
                ToImmutable()
        End Function

        Friend Shared Function NormalizeAlias(raw As String) As String
            Dim s = If(raw, String.Empty).Trim()
            If s.StartsWith("."c) Then s = s.Substring(1)
            Return s.ToLowerInvariant()
        End Function

        ''' <summary>
        '''     Liefert den zugeordneten FileType fuer einen Enumwert.
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <returns>Registrierter Typ oder Unknown.</returns>
        Friend Shared Function Resolve(kind As FileKind) As FileType
            Dim t As FileType = Nothing
            If TypesByKind.TryGetValue(kind, t) AndAlso t IsNot Nothing Then Return t
            Return TypesByKind(FileKind.Unknown)
        End Function

        ''' <summary>
        '''     Liefert den zugeordneten FileType fuer einen Aliaswert.
        ''' </summary>
        ''' <param name="aliasKey">Alias mit oder ohne fuehrenden Punkt.</param>
        ''' <returns>Registrierter Typ oder Unknown.</returns>
        Friend Shared Function ResolveByAlias(aliasKey As String) As FileType
            Dim k = FileKind.Unknown
            If KindByAlias.TryGetValue(NormalizeAlias(aliasKey), k) Then
                Return Resolve(k)
            End If
            Return Resolve(FileKind.Unknown)
        End Function

        Private Structure FileTypeDefinition
            Friend ReadOnly Kind As FileKind
            Friend ReadOnly CanonicalExtension As String
            Friend ReadOnly Aliases As String()
            Friend ReadOnly MagicPatterns As ImmutableArray(Of MagicPattern)

            Friend Sub New(kind As FileKind, canonicalExtension As String, aliases As String(),
                           magicPatterns As ImmutableArray(Of MagicPattern))
                Me.Kind = kind
                Me.CanonicalExtension = canonicalExtension
                Me.Aliases = aliases
                Me.MagicPatterns = magicPatterns
            End Sub
        End Structure

        Private Structure MagicRule
            Friend ReadOnly Kind As FileKind
            Friend ReadOnly Patterns As ImmutableArray(Of MagicPattern)

            Friend Sub New(kind As FileKind, patterns As ImmutableArray(Of MagicPattern))
                Me.Kind = kind
                Me.Patterns = patterns
            End Sub
        End Structure

        Private Structure MagicPattern
            Friend ReadOnly Segments As ImmutableArray(Of MagicSegment)

            Friend Sub New(segments As ImmutableArray(Of MagicSegment))
                Me.Segments = segments
            End Sub
        End Structure

        Private Structure MagicSegment
            Friend ReadOnly Offset As Integer
            Friend ReadOnly Bytes As ImmutableArray(Of Byte)

            Friend Sub New(offset As Integer, bytes As ImmutableArray(Of Byte))
                Me.Offset = offset
                Me.Bytes = bytes
            End Sub
        End Structure
    End Class
End Namespace
