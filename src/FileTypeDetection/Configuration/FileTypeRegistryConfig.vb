' ============================================================================
' FILE: FileTypeRegistryConfig.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
'
' SSOT CONFIG (verbindlich)
' - AliasGroups: zentrale “Wildcard”-Semantik (FileKind.* steht für viele Aliaswerte)
' - AliasOverrides: Kind -> AliasGroup
' - ExtensionOverrides: Canonical-Extension Overrides
' - MagicPatternCatalog: zentrale Magic-Signaturen
' ============================================================================

Option Strict On
Option Explicit On

Imports System.Collections.Immutable

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Zentrale Konfiguration (SSOT) für <see cref="FileTypeRegistry"/>.
    '''     Definiert:
    '''     - Canonical-Extension Overrides
    '''     - Aliasgruppen (Wildcard-Semantik)
    '''     - Mapping <see cref="FileKind"/> -&gt; Aliasgruppe
    '''     - Magic-Pattern-Katalog
    ''' </summary>
    ''' <remarks>
    '''     Diese Konfiguration enthält ausschließlich statische Daten und deterministische Builder.
    '''     Normalisierung, Deduplikation und Sortierung erfolgen in <see cref="FileTypeRegistry"/>.
    ''' </remarks>
    Friend Module FileTypeRegistryConfig

        ''' <summary>
        '''     Canonical-Extension Overrides (SSOT).
        '''     Wird genutzt, wenn die Canonical-Extension nicht aus dem Enum-Namen abgeleitet werden soll.
        ''' </summary>
        Friend ReadOnly ExtensionOverrides _
                            As ImmutableDictionary(Of FileKind, String) = _
                            BuildExtensionOverrides()

        ''' <summary>
        '''     Aliasgruppen (SSOT) zur Abbildung der Wildcard-Semantik.
        '''     Gruppen fassen gleichartige Aliaswerte zusammen (z.B. Archive, Office-Container).
        ''' </summary>
        Friend ReadOnly AliasGroups _
                            As ImmutableDictionary(Of String, ImmutableArray(Of String)) = _
                            BuildAliasGroups()

        ''' <summary>
        '''     Mapping <see cref="FileKind"/> -&gt; Aliasgruppe (SSOT).
        '''     Die Werte ergänzen die automatisch abgeleiteten Aliases (Enumname + Canonical-Extension).
        ''' </summary>
        Friend ReadOnly AliasOverrides _
                            As ImmutableDictionary(Of FileKind, ImmutableArray(Of String)) = _
                            BuildAliasOverrides()

        ''' <summary>
        '''     Magic-Pattern-Katalog (SSOT) für direkte Header-Erkennung.
        '''     Enthält pro <see cref="FileKind"/> eine Liste von Patterns; ein Pattern besteht aus Segmenten.
        ''' </summary>
        Friend ReadOnly MagicPatternCatalog _
                            As ImmutableDictionary(Of FileKind, ImmutableArray(Of FileTypeRegistry.MagicPattern)) =
                            BuildMagicPatternCatalog()

        ''' <summary>
        '''     Erstellt ein unveränderliches Aliasarray aus variablen Stringwerten.
        ''' </summary>
        ''' <param name="values">Aliaswerte in Rohform.</param>
        ''' <returns>Unveränderliches Array der Aliaswerte.</returns>
        Private Function A _
            (
                ParamArray values As String()
            ) As ImmutableArray(Of String)

            Return ImmutableArray.Create(values)
        End Function

        ''' <summary>
        '''     Liefert die Aliasgruppe für einen Gruppennamen.
        '''     Fail-closed: Unbekannte Gruppen liefern ein leeres Array.
        ''' </summary>
        ''' <param name="name">Gruppenname (case-insensitive).</param>
        ''' <returns>Aliasgruppe oder leeres Array.</returns>
        Private Function GetGroup _
            (
                name As String _
             ) As ImmutableArray(Of String)

            Dim values As ImmutableArray(Of String) = ImmutableArray(Of String).Empty
            If AliasGroups.TryGetValue(name, values) Then Return values
            Return ImmutableArray(Of String).Empty
        End Function

        ''' <summary>
        '''     Baut die Canonical-Extension Overrides deterministisch.
        ''' </summary>
        ''' <returns>Unveränderliches Dictionary Kind-&gt;Extension.</returns>
        Private Function BuildExtensionOverrides() As ImmutableDictionary(Of FileKind, String)
            Dim b = ImmutableDictionary.CreateBuilder(Of FileKind, String)()

            b(FileKind.Jpeg) = ".jpg"

            Return b.ToImmutable()
        End Function

        ''' <summary>
        '''     Baut die Aliasgruppen deterministisch.
        '''     Gruppen sind fachliche Wildcards und werden in <see cref="BuildAliasOverrides"/> referenziert.
        ''' </summary>
        ''' <returns>Unveränderliches Dictionary Gruppenname-&gt;Aliasliste.</returns>
        Private Function BuildAliasGroups() As ImmutableDictionary(Of String, ImmutableArray(Of String))
            Dim b = ImmutableDictionary.CreateBuilder(Of String, ImmutableArray(Of String))(StringComparer.OrdinalIgnoreCase)

            ' Wildcard-Semantik (Gruppen):
            ' - ARCHIVE: alle Archive/Container, die über FileKind.Zip normalisiert werden.
            ' - OFFICE_*: Office/ähnliche Container (Doc/Xls/Ppt), deren Content/Container-Detection separat läuft.

            b("JPEG") = A("jpe")

            b("ARCHIVE") = A(
                "tar", "tgz", "gz", "gzip",
                "bz2", "bzip2",
                "xz",
                "7z", "zz", "rar")

            b("OFFICE_DOC") = A(
                "doc", "docx", "docm", "docb",
                "dot", "dotm", "dotx",
                "odt", "ott")

            b("OFFICE_XLS") = A(
                "xls", "xlsx", "xlsm", "xlsb",
                "xlt", "xltm", "xltx", "xltb",
                "xlam", "xla",
                "ods", "ots")

            b("OFFICE_PPT") = A(
                "ppt", "pptx", "pptm",
                "pot", "potm", "potx",
                "pps", "ppsm", "ppsx",
                "odp", "otp")

            Return b.ToImmutable()
        End Function

        ''' <summary>
        '''     Baut das Mapping <see cref="FileKind"/> -&gt; Aliasgruppe deterministisch.
        ''' </summary>
        ''' <returns>Unveränderliches Dictionary Kind-&gt;Aliasliste.</returns>
        Private Function BuildAliasOverrides() As ImmutableDictionary(Of FileKind, ImmutableArray(Of String))
            Dim b = ImmutableDictionary.CreateBuilder(Of FileKind, ImmutableArray(Of String))()

            b(FileKind.Jpeg) = GetGroup("JPEG")
            b(FileKind.Zip) = GetGroup("ARCHIVE")
            b(FileKind.Doc) = GetGroup("OFFICE_DOC")
            b(FileKind.Xls) = GetGroup("OFFICE_XLS")
            b(FileKind.Ppt) = GetGroup("OFFICE_PPT")

            Return b.ToImmutable()
        End Function

        ''' <summary>
        '''     Erstellt ein <see cref="FileTypeRegistry.MagicPattern"/> aus Segmenten.
        ''' </summary>
        ''' <param name="segments">Segmente, die gemeinsam matchen müssen.</param>
        ''' <returns>Magic-Pattern.</returns>
        Private Function Pattern _
            (
                ParamArray segments As FileTypeRegistry.MagicSegment()
             ) As FileTypeRegistry.MagicPattern

            Return New FileTypeRegistry.MagicPattern(ImmutableArray.Create(segments))
        End Function

        ''' <summary>
        '''     Erstellt ein <see cref="FileTypeRegistry.MagicSegment"/>, das eine Bytefolge ab einem festen Offset erwartet.
        ''' </summary>
        ''' <param name="offset">Startoffset im Header.</param>
        ''' <param name="bytesValue">Erwartete Bytefolge.</param>
        ''' <returns>Magic-Segment.</returns>
        Private Function Prefix _
            (
                offset As Integer,
                ParamArray bytesValue As Byte()
            ) As FileTypeRegistry.MagicSegment

            Return New FileTypeRegistry.MagicSegment(offset, ImmutableArray.Create(bytesValue))
        End Function

        ''' <summary>
        '''     Baut den Magic-Pattern-Katalog deterministisch.
        '''     Einträge sind ausschließlich direkte Header-Signaturen (kein Container-Parsing).
        ''' </summary>
        ''' <returns>Unveränderliches Dictionary Kind-&gt;Magic-Patterns.</returns>
        Private Function BuildMagicPatternCatalog _
            () As ImmutableDictionary(Of FileKind, ImmutableArray(Of FileTypeRegistry.MagicPattern))

            Dim b = ImmutableDictionary.CreateBuilder(Of FileKind, ImmutableArray(Of FileTypeRegistry.MagicPattern))()

            b(FileKind.Pdf) = ImmutableArray.Create(
                Pattern(Prefix(0, &H25, &H50, &H44, &H46, &H2D))
            )

            b(FileKind.Png) = ImmutableArray.Create(
                Pattern(Prefix(0, &H89, &H50, &H4E, &H47, &HD, &HA, &H1A, &HA))
            )

            b(FileKind.Jpeg) = ImmutableArray.Create(
                Pattern(Prefix(0, &HFF, &HD8, &HFF))
            )

            b(FileKind.Gif) = ImmutableArray.Create(
                Pattern(Prefix(0, &H47, &H49, &H46, &H38, &H37, &H61)),
                Pattern(Prefix(0, &H47, &H49, &H46, &H38, &H39, &H61))
            )

            b(FileKind.Webp) = ImmutableArray.Create(
                Pattern(
                    Prefix(0, &H52, &H49, &H46, &H46),
                        Prefix(8, &H57, &H45, &H42, &H50)
                    )
                )

            b(FileKind.Zip) = ImmutableArray.Create(
                Pattern(Prefix(0, &H50, &H4B, &H3, &H4)),
                Pattern(Prefix(0, &H50, &H4B, &H5, &H6)),
                Pattern(Prefix(0, &H50, &H4B, &H7, &H8))
            )

            Return b.ToImmutable()
        End Function

    End Module
End Namespace
