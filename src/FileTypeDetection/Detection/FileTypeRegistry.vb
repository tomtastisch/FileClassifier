' ============================================================================
' FILE: FileTypeRegistry.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.Collections.Immutable
Imports System.Linq

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Zentrale Registry als SSOT für Typmetadaten, Alias-Auflösung und Magic-Patterns.
    '''     Regeln:
    '''     - Neue Typen werden primär über FileKind erweitert.
    '''     - Metadaten werden deterministisch aus FileKind + zentralen Overrides aufgebaut.
    '''     - Unknown ist immer als fail-closed Fallback vorhanden.
    ''' </summary>
    Friend NotInheritable Class FileTypeRegistry
        Private Sub New()
        End Sub

        ''' <summary>
        '''     SSOT: Ordnet jedem <see cref="FileKind"/> den zugehörigen <see cref="FileType"/> zu.
        '''     Der Eintrag <see cref="FileKind.Unknown"/> ist immer vorhanden (fail-closed).
        ''' </summary>
        Friend Shared ReadOnly TypesByKind As ImmutableDictionary(Of FileKind, FileType)

        ''' <summary>
        '''     Alias-Index: Ordnet normalisierte Aliaswerte (z.B. Endungen ohne Punkt) einem <see cref="FileKind"/> zu.
        '''     Die Normalisierung erfolgt über <see cref="NormalizeAlias"/>.
        ''' </summary>
        Friend Shared ReadOnly KindByAlias As ImmutableDictionary(Of String, FileKind)


        ''' <summary>
        '''     Canonical-Extension Overrides (SSOT). Wird für einzelne Typen genutzt,
        '''     wenn der Enumname nicht der gewünschten Canonical-Extension entspricht.
        ''' </summary>
        Private Shared ReadOnly ExtensionOverrides As ImmutableDictionary(Of FileKind, String) =
            FileTypeRegistryConfig.ExtensionOverrides

        ''' <summary>
        '''     Zusätzliche Aliaswerte pro <see cref="FileKind"/> (SSOT). Diese Werte ergänzen die automatisch
        '''     abgeleiteten Aliases (Enumname + Canonical-Extension) und werden deterministisch normalisiert.
        ''' </summary>
        Private Shared ReadOnly AliasOverrides As ImmutableDictionary(Of FileKind, ImmutableArray(Of String)) =
            FileTypeRegistryConfig.AliasOverrides

        ''' <summary>
        '''     Cache der deterministisch sortierten Enumwerte (<see cref="FileKind"/>).
        '''     Vermeidet wiederholte Reflection/Sortierung in Hotpaths.
        ''' </summary>
        Private Shared ReadOnly OrderedKindsCache As ImmutableArray(Of FileKind) = BuildOrderedKinds()

        ''' <summary>
        '''     Katalog von Magic-Patterns pro <see cref="FileKind"/>.
        '''     Die Datenquelle ist die zentrale Konfiguration <c>FileTypeRegistryConfig</c>.
        ''' </summary>
        Private Shared ReadOnly _
            MagicPatternCatalog As ImmutableDictionary(Of FileKind, ImmutableArray(Of MagicPattern)) =
                FileTypeRegistryConfig.MagicPatternCatalog

        ''' <summary>
        '''     Aus <see cref="FileTypeDefinition"/> abgeleitete Regeln für die Magic-Erkennung.
        '''     Enthält ausschließlich Einträge mit mindestens einem Magic-Pattern.
        ''' </summary>
        Private Shared ReadOnly MagicRules As ImmutableArray(Of MagicRule)


        ''' <summary>
        '''     Initialisiert die Registry deterministisch aus <see cref="FileKind"/> und den zentralen Overrides.
        '''     Reihenfolge: Definitionen bauen, Typen ableiten, Aliasindex erzeugen, Magic-Regeln ableiten.
        ''' </summary>
        Shared Sub New()
            Dim definitions = BuildDefinitionsFromEnum()
            TypesByKind = BuildTypes(definitions)
            KindByAlias = BuildAliasMap(TypesByKind)
            MagicRules = BuildMagicRules(definitions)
        End Sub

        ''' <summary>
        '''     Erzeugt die vollständige Menge an <see cref="FileTypeDefinition"/> aus der Enumquelle.
        '''     <see cref="FileKind.Unknown"/> wird bewusst ausgeschlossen, da Unknown als separater fail-closed Typ geführt wird.
        ''' </summary>
        ''' <returns>Unveränderliche Liste aller Definitionsobjekte (ohne Unknown).</returns>
        Private Shared Function BuildDefinitionsFromEnum() As ImmutableArray(Of FileTypeDefinition)
            Dim b = ImmutableArray.CreateBuilder(Of FileTypeDefinition)()
            Dim canonicalExtension As String
            Dim aliases As String()
            Dim magicPatterns As ImmutableArray(Of MagicPattern)

            For Each kind In OrderedKindsCache
                If kind = FileKind.Unknown Then Continue For

                canonicalExtension = GetCanonicalExtension(kind)
                aliases = BuildAliases(kind, canonicalExtension)
                magicPatterns = GetMagicPatterns(kind)

                b.Add(New FileTypeDefinition(kind, canonicalExtension, aliases, magicPatterns))
            Next

            Return b.ToImmutable()
        End Function

        ''' <summary>
        '''     Liefert die deterministisch sortierten Enumwerte (<see cref="FileKind"/>) aus dem Cache.
        ''' </summary>
        ''' <returns>Sortierte Liste aller Enumwerte.</returns>
        Private Shared Function OrderedKinds() As ImmutableArray(Of FileKind)
            Return OrderedKindsCache
        End Function

        ''' <summary>
        '''     Baut den Cache der sortierten Enumwerte (<see cref="FileKind"/>) einmalig über Reflection.
        ''' </summary>
        ''' <returns>Sortierte Liste aller Enumwerte.</returns>
        Private Shared Function BuildOrderedKinds() As ImmutableArray(Of FileKind)
            Dim values = [Enum].GetValues(GetType(FileKind)).Cast(Of FileKind)()
            Return values.
                OrderBy(Function(kind) CInt(kind)).
                ToImmutableArray()
        End Function

        ''' <summary>
        '''     Bestimmt die Canonical-Extension für einen Typ.
        '''     Priorität: Override &gt; Enumname (normalisiert) als <c>"." + alias</c>.
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <returns>Canonical-Extension inklusive führendem Punkt.</returns>
        Private Shared Function GetCanonicalExtension(kind As FileKind) As String
            Dim overrideExt As String = Nothing
            If ExtensionOverrides.TryGetValue(kind, overrideExt) Then
                Return overrideExt
            End If

            Return "." & NormalizeAlias(kind.ToString())
        End Function

        ''' <summary>
        '''     Baut die vollständige Aliasliste für einen Typ.
        '''     Enthält Canonical-Extension, Enumalias sowie zusätzliche Overrides.
        '''     Ergebnis ist deterministisch sortiert und ohne Duplikate.
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <param name="canonicalExtension">Canonical-Extension inklusive führendem Punkt.</param>
        ''' <returns>Sortierte Aliasliste (ohne führende Punkte, kleingeschrieben).</returns>
        Private Shared Function BuildAliases(kind As FileKind, canonicalExtension As String) As String()
            Dim aliases As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim extAlias As String
            Dim enumAlias As String
            Dim additional As ImmutableArray(Of String) = ImmutableArray(Of String).Empty
            Dim orderedAliases As List(Of String)

            extAlias = NormalizeAlias(canonicalExtension)
            If extAlias.Length > 0 Then aliases.Add(extAlias)

            enumAlias = NormalizeAlias(kind.ToString())
            If enumAlias.Length > 0 Then aliases.Add(enumAlias)

            If AliasOverrides.TryGetValue(kind, additional) Then
                For Each rawAlias In additional
                    Dim normalized = NormalizeAlias(rawAlias)
                    If normalized.Length > 0 Then aliases.Add(normalized)
                Next
            End If

            orderedAliases = aliases.ToList()
            orderedAliases.Sort(StringComparer.Ordinal)
            Return orderedAliases.ToArray()
        End Function

        ''' <summary>
        '''     Liefert die Magic-Patterns für einen Typ aus dem Katalog.
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <returns>Magic-Patterns oder ein leeres Array.</returns>
        Private Shared Function GetMagicPatterns(kind As FileKind) As ImmutableArray(Of MagicPattern)
            Dim patterns As ImmutableArray(Of MagicPattern) = ImmutableArray(Of MagicPattern).Empty
            If MagicPatternCatalog.TryGetValue(kind, patterns) Then
                Return patterns
            End If

            Return ImmutableArray(Of MagicPattern).Empty
        End Function

        ''' <summary>
        '''     Erzeugt die Typ-Registry (<see cref="TypesByKind"/>) aus den Definitionsobjekten.
        '''     Unknown wird als eigener, fail-closed Eintrag hinzugefügt.
        ''' </summary>
        ''' <param name="definitions">Definitionsobjekte (ohne Unknown).</param>
        ''' <returns>Unveränderliches Dictionary mit Einträgen für alle Typen inklusive Unknown.</returns>
        Private Shared Function BuildTypes(definitions As ImmutableArray(Of FileTypeDefinition)) _
            As ImmutableDictionary(Of FileKind, FileType)
            Dim b = ImmutableDictionary.CreateBuilder(Of FileKind, FileType)()

            b(FileKind.Unknown) = CreateUnknownType()

            For Each d In definitions
                b(d.Kind) = New FileType(
                    d.Kind,
                    d.CanonicalExtension,
                    MimeProvider.GetMime(d.CanonicalExtension),
                    True,
                    d.Aliases
                )
            Next

            Return b.ToImmutable()
        End Function

        ''' <summary>
        '''     Erzeugt den fail-closed <see cref="FileType"/> für <see cref="FileKind.Unknown"/>.
        ''' </summary>
        ''' <returns>Unknown-Typ ohne Extension und ohne MIME.</returns>
        Private Shared Function CreateUnknownType() As FileType
            Return New FileType(FileKind.Unknown,
                                Nothing,
                                Nothing,
                                False,
                                ImmutableArray(Of String).Empty)
        End Function


        ''' <summary>
        '''     Bestimmt den Typ anhand von Magic-Patterns in einem Header-Bytearray.
        '''     Die Auswertung erfolgt deterministisch in Regelreihenfolge; erster Treffer gewinnt.
        ''' </summary>
        ''' <param name="header">Dateiheader (mindestens so lang wie die benötigten Segmente).</param>
        ''' <returns>Erkannter <see cref="FileKind"/> oder <see cref="FileKind.Unknown"/>.</returns>
        Friend Shared Function DetectByMagic(header As Byte()) As FileKind
            Dim rule As MagicRule
            Dim patterns As ImmutableArray(Of MagicPattern)

            If header Is Nothing OrElse header.Length = 0 Then Return FileKind.Unknown

            For i = 0 To MagicRules.Length - 1
                rule = MagicRules(i)
                patterns = rule.Patterns

                For j = 0 To patterns.Length - 1
                    If MatchesPattern(header, patterns(j)) Then
                        Return rule.Kind
                    End If
                Next
            Next

            Return FileKind.Unknown
        End Function

        ''' <summary>
        '''     Prüft, ob ein Magic-Pattern vollständig gegen den Header matcht.
        ''' </summary>
        ''' <param name="header">Headerdaten.</param>
        ''' <param name="pattern">Pattern mit Segmenten.</param>
        ''' <returns><c>True</c>, wenn alle Segmente matchen.</returns>
        Private Shared Function MatchesPattern(header As Byte(), pattern As MagicPattern) As Boolean
            Dim segments As ImmutableArray(Of MagicSegment) = pattern.Segments

            If segments.IsDefaultOrEmpty Then Return False

            For i = 0 To segments.Length - 1
                If Not HasSegment(header, segments(i)) Then Return False
            Next

            Return True
        End Function

        ''' <summary>
        '''     Prüft, ob für einen Typ mindestens ein Magic-Pattern für direkte Header-Erkennung hinterlegt ist.
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <returns><c>True</c> bei vorhandenem Patternkatalogeintrag.</returns>
        Friend Shared Function HasDirectHeaderDetection(kind As FileKind) As Boolean
            Dim patterns As ImmutableArray(Of MagicPattern) = ImmutableArray(Of MagicPattern).Empty

            If kind = FileKind.Unknown Then Return False
            Return MagicPatternCatalog.TryGetValue(kind, patterns) AndAlso Not patterns.IsDefaultOrEmpty
        End Function

        ''' <summary>
        '''     Prüft, ob ein Typ zusätzlich über strukturierte Container-Erkennung klassifiziert wird.
        '''     Diese Klassifikation ist unabhängig von direkten Header-Signaturen.
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <returns><c>True</c>, wenn strukturierte Container-Erkennung aktiv ist.</returns>
        Friend Shared Function HasStructuredContainerDetection(kind As FileKind) As Boolean
            Return kind = FileKind.Doc OrElse
                   kind = FileKind.Xls OrElse
                   kind = FileKind.Ppt
        End Function

        ''' <summary>
        '''     Prüft, ob der Typ eine direkte Inhalts-/Header-Erkennung besitzt
        '''     (Magic-Header oder strukturierte Container-Erkennung).
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <returns><c>True</c>, wenn Content-Detection verfügbar ist.</returns>
        Friend Shared Function HasDirectContentDetection(kind As FileKind) As Boolean
            Return HasDirectHeaderDetection(kind) OrElse HasStructuredContainerDetection(kind)
        End Function

        ''' <summary>
        '''     Liefert alle Typen, die keine direkte Content-Detection besitzen.
        '''     Unknown ist ausgeschlossen.
        ''' </summary>
        ''' <returns>Liste der Typen ohne direkte Content-Detection.</returns>
        Friend Shared Function KindsWithoutDirectContentDetection() As ImmutableArray(Of FileKind)
            Return OrderedKinds().
                Where(Function(kind) kind <> FileKind.Unknown).
                Where(Function(kind) Not HasDirectContentDetection(kind)).
                ToImmutableArray()
        End Function

        ''' <summary>
        '''     Baut die Magic-Regeln aus den Definitionsobjekten.
        '''     Es werden ausschließlich Definitionsobjekte mit mindestens einem Magic-Pattern berücksichtigt.
        ''' </summary>
        ''' <param name="definitions">Definitionsobjekte (ohne Unknown).</param>
        ''' <returns>Unveränderliche Liste der Magic-Regeln.</returns>
        Private Shared Function BuildMagicRules(definitions As ImmutableArray(Of FileTypeDefinition)) _
            As ImmutableArray(Of MagicRule)
            Return definitions.
                Where(Function(definition) Not definition.MagicPatterns.IsDefaultOrEmpty).
                Select(Function(definition) New MagicRule(definition.Kind, definition.MagicPatterns)).
                ToImmutableArray()
        End Function

        ''' <summary>
        '''     Prüft, ob ein einzelnes Segment am angegebenen Offset innerhalb der Daten exakt matcht.
        '''     Fail-closed: Bei ungültigen Parametern oder zu kurzen Daten wird <c>False</c> geliefert.
        ''' </summary>
        ''' <param name="data">Headerdaten.</param>
        ''' <param name="segment">Segmentdefinition.</param>
        ''' <returns><c>True</c> bei exaktem Match.</returns>
        Private Shared Function HasSegment(data As Byte(), segment As MagicSegment) As Boolean
            Dim endPos As Integer

            If data Is Nothing Then Return False
            If segment.Offset < 0 Then Return False
            If segment.Bytes.IsDefaultOrEmpty Then Return False

            endPos = segment.Offset + segment.Bytes.Length
            If endPos < 0 OrElse data.Length < endPos Then Return False

            For i = 0 To segment.Bytes.Length - 1
                If data(segment.Offset + i) <> segment.Bytes(i) Then Return False
            Next
            Return True
        End Function

        ''' <summary>
        '''     Erzeugt den Aliasindex (<see cref="KindByAlias"/>) aus der Typ-Registry.
        '''     Aliases werden normalisiert; spätere Einträge überschreiben frühere deterministisch.
        ''' </summary>
        ''' <param name="types">Typ-Registry.</param>
        ''' <returns>Unveränderliches Dictionary Alias-&gt;Kind.</returns>
        Private Shared Function BuildAliasMap(types As ImmutableDictionary(Of FileKind, FileType)) _
            As ImmutableDictionary(Of String, FileKind)
            Dim builder As ImmutableDictionary(Of String, FileKind).Builder
            Dim kind As FileKind
            Dim t As FileType = Nothing

            If types Is Nothing Then Return ImmutableDictionary(Of String, FileKind).Empty

            builder = ImmutableDictionary.CreateBuilder(Of String, FileKind)(StringComparer.OrdinalIgnoreCase)

            For Each kind In OrderedKindsCache
                If Not types.TryGetValue(kind, t) Then Continue For
                If t Is Nothing Then Continue For
                If t.Aliases.IsDefaultOrEmpty Then Continue For

                For i = 0 To t.Aliases.Length - 1
                    Dim aliasKey = NormalizeAlias(t.Aliases(i))
                    If aliasKey.Length = 0 Then Continue For

                    builder(aliasKey) = kind
                Next
            Next

            Return builder.ToImmutable()
        End Function

        ''' <summary>
        '''     Normalisiert einen Aliaswert deterministisch.
        '''     Entfernt führende Punkte, trimmt Whitespace und wandelt in Kleinbuchstaben (Invariant) um.
        ''' </summary>
        ''' <param name="raw">Rohwert, z.B. ".PDF" oder " pdf ".</param>
        ''' <returns>Normalisierter Alias ohne Punkt oder leerer String.</returns>
        Friend Shared Function NormalizeAlias(raw As String) As String
            Dim s As String = If(raw, String.Empty).Trim()

            If s.Length = 0 Then Return String.Empty
            If s(0) = "."c Then s = s.Substring(1)

            Return s.ToLowerInvariant()
        End Function

        ''' <summary>
        '''     Liefert den zugeordneten FileType für einen Enumwert.
        ''' </summary>
        ''' <param name="kind">Enumwert des Typs.</param>
        ''' <returns>Registrierter Typ oder Unknown.</returns>
        Friend Shared Function Resolve(kind As FileKind) As FileType
            Dim t As FileType = Nothing
            If TypesByKind.TryGetValue(kind, t) AndAlso t IsNot Nothing Then Return t
            Return TypesByKind(FileKind.Unknown)
        End Function

        ''' <summary>
        '''     Liefert den zugeordneten FileType für einen Aliaswert.
        ''' </summary>
        ''' <param name="aliasKey">Alias mit oder ohne führenden Punkt.</param>
        ''' <returns>Registrierter Typ oder Unknown.</returns>
        Friend Shared Function ResolveByAlias(aliasKey As String) As FileType
            Dim k = FileKind.Unknown
            If KindByAlias.TryGetValue(NormalizeAlias(aliasKey), k) Then
                Return Resolve(k)
            End If
            Return Resolve(FileKind.Unknown)
        End Function

        ''' <summary>
        '''     Interner, unveränderlicher Datenträger <c>FileTypeDefinition</c> für strukturierte Verarbeitungsschritte.
        ''' </summary>
        Friend Structure FileTypeDefinition
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

        ''' <summary>
        '''     Interner, unveränderlicher Datenträger <c>MagicRule</c> für strukturierte Verarbeitungsschritte.
        ''' </summary>
        Friend Structure MagicRule
            Friend ReadOnly Kind As FileKind
            Friend ReadOnly Patterns As ImmutableArray(Of MagicPattern)

            Friend Sub New(kind As FileKind, patterns As ImmutableArray(Of MagicPattern))
                Me.Kind = kind
                Me.Patterns = patterns
            End Sub
        End Structure

        ''' <summary>
        '''     Interner, unveränderlicher Datenträger <c>MagicPattern</c> für strukturierte Verarbeitungsschritte.
        ''' </summary>
        Friend Structure MagicPattern
            Friend ReadOnly Segments As ImmutableArray(Of MagicSegment)

            Friend Sub New(segments As ImmutableArray(Of MagicSegment))
                Me.Segments = segments
            End Sub
        End Structure

        ''' <summary>
        '''     Interner, unveränderlicher Datenträger <c>MagicSegment</c> für strukturierte Verarbeitungsschritte.
        ''' </summary>
        Friend Structure MagicSegment
            Friend ReadOnly Offset As Integer
            Friend ReadOnly Bytes As ImmutableArray(Of Byte)

            Friend Sub New(offset As Integer, bytes As ImmutableArray(Of Byte))
                Me.Offset = offset
                Me.Bytes = bytes
            End Sub
        End Structure
    End Class
End Namespace
