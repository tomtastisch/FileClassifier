' ============================================================================
' FILE: FileTypeOptions.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Verwaltet die globalen Bibliotheksoptionen als thread-sicheren Snapshot mit JSON-Ein- und Ausgabe.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Verantwortung: Die Klasse bildet die öffentliche Konfigurationsschnittstelle für Konsumenten und kapselt
    '''         Validierung, Normalisierung und Snapshot-Aktualisierung.
    '''     </para>
    '''     <para>
    '''         Nebenwirkungen: Änderungen wirken global auf nachfolgende Operationen der Bibliothek.
    '''         Fehlerhafte Konfigurationen werden fail-closed verworfen.
    '''     </para>
    '''     <para>
    '''         Threading: Lese- und Schreibzugriffe auf den aktuellen Snapshot sind über ein zentrales Lock synchronisiert.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class FileTypeOptions
        Private Shared ReadOnly OptionsLock As New Object()
        Private Shared _currentOptions As FileTypeProjectOptions = FileTypeProjectOptions.DefaultOptions()

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Lädt globale Optionen aus einem JSON-Dokument und ersetzt den aktuellen Snapshot atomar bei Erfolg.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Nicht gesetzte Felder bleiben auf normierten Default-Werten. Unbekannte Schlüssel werden ignoriert
        '''         und protokolliert.
        '''     </para>
        '''     <para>
        '''         Fail-Closed: Bei Parse- oder Validierungsfehlern bleibt der bisherige Snapshot unverändert und die
        '''         Methode liefert <c>False</c>.
        '''     </para>
        ''' </remarks>
        ''' <param name="json">JSON-Konfiguration mit unterstützten Optionsfeldern.</param>
        ''' <returns><c>True</c>, wenn ein gültiger Snapshot gesetzt wurde; andernfalls <c>False</c>.</returns>
        ''' <exception cref="System.Text.Json.JsonException">Kann bei ungültiger JSON-Struktur intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="ArgumentException">Kann bei ungültigen Argumentzuständen intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="NotSupportedException">Kann bei nicht unterstützter JSON-Konstellation intern auftreten und wird fail-closed behandelt.</exception>
        Public Shared Function LoadOptions _
            (
                json As String
            ) As Boolean
            If String.IsNullOrWhiteSpace(json) Then Return False

            Dim defaults = FileTypeProjectOptions.DefaultOptions()
            Dim headerOnlyNonZip = defaults.HeaderOnlyNonZip
            Dim maxBytes = defaults.MaxBytes
            Dim sniffBytes = defaults.SniffBytes
            Dim maxZipEntries = defaults.MaxZipEntries
            Dim maxZipTotalUncompressedBytes = defaults.MaxZipTotalUncompressedBytes
            Dim maxZipEntryUncompressedBytes = defaults.MaxZipEntryUncompressedBytes
            Dim maxZipCompressionRatio = defaults.MaxZipCompressionRatio
            Dim maxZipNestingDepth = defaults.MaxZipNestingDepth
            Dim maxZipNestedBytes = defaults.MaxZipNestedBytes
            Dim rejectArchiveLinks = defaults.RejectArchiveLinks
            Dim allowUnknownArchiveEntrySize = defaults.AllowUnknownArchiveEntrySize
            Dim hashIncludePayloadCopies = defaults.DeterministicHash.IncludePayloadCopies
            Dim hashIncludeFastHash = defaults.DeterministicHash.IncludeFastHash
            Dim hashIncludeSecureHash = defaults.DeterministicHash.IncludeSecureHash
            Dim hashMaterializedFileName = defaults.DeterministicHash.MaterializedFileName
            Dim logger = defaults.Logger

            Try
                Using doc = Text.Json.JsonDocument.Parse(
                    json,
                    New Text.Json.JsonDocumentOptions With {.AllowTrailingCommas = True}
                )
                    If doc.RootElement.ValueKind <> Text.Json.JsonValueKind.Object Then
                        LogGuard.Warn(logger, "[Config] Root muss ein JSON-Objekt sein.")
                        Return False
                    End If

                    For Each p In doc.RootElement.EnumerateObject()
                        Select Case p.Name.ToLowerInvariant()
                            Case "headeronlynonzip" _
                                : headerOnlyNonZip = ParseBoolean(
                                    p.Value, headerOnlyNonZip, p.Name, logger)
                            Case "maxbytes" : maxBytes = ParsePositiveLong(p.Value, maxBytes, p.Name, logger)
                            Case "sniffbytes" : sniffBytes = ParsePositiveInt(p.Value, sniffBytes, p.Name, logger)
                            Case "maxzipentries" _
                                : maxZipEntries = ParsePositiveInt(p.Value, maxZipEntries, p.Name, logger)
                            Case "maxziptotaluncompressedbytes" _
                                : maxZipTotalUncompressedBytes = ParsePositiveLong(p.Value, maxZipTotalUncompressedBytes,
                                                                                   p.Name, logger)
                            Case "maxzipentryuncompressedbytes" _
                                : maxZipEntryUncompressedBytes = ParsePositiveLong(p.Value, maxZipEntryUncompressedBytes,
                                                                                   p.Name, logger)
                            Case "maxzipcompressionratio" _
                                : maxZipCompressionRatio = ParseNonNegativeInt(p.Value, maxZipCompressionRatio, p.Name,
                                                                               logger)
                            Case "maxzipnestingdepth" _
                                : maxZipNestingDepth = ParseNonNegativeInt(p.Value, maxZipNestingDepth, p.Name, logger)
                            Case "maxzipnestedbytes" _
                                : maxZipNestedBytes = ParsePositiveLong(p.Value, maxZipNestedBytes, p.Name, logger)
                            Case "rejectarchivelinks" _
                                : rejectArchiveLinks = ParseBoolean(p.Value, rejectArchiveLinks, p.Name, logger)
                            Case "allowunknownarchiveentrysize" _
                                : allowUnknownArchiveEntrySize = ParseBoolean(p.Value, allowUnknownArchiveEntrySize,
                                                                              p.Name, logger)
                            Case "deterministichash", "deterministichashoptions"
                                TryParseHashOptions(
                                    p.Value,
                                    hashIncludePayloadCopies,
                                    hashIncludeFastHash,
                                    hashIncludeSecureHash,
                                    hashMaterializedFileName,
                                    logger)
                            Case "deterministichashincludepayloadcopies"
                                hashIncludePayloadCopies = ParseBoolean(p.Value, hashIncludePayloadCopies, p.Name,
                                                                        logger)
                            Case "deterministichashincludefasthash"
                                hashIncludeFastHash = ParseBoolean(p.Value, hashIncludeFastHash, p.Name, logger)
                            Case "deterministichashincludesecurehash"
                                hashIncludeSecureHash = ParseBoolean(p.Value, hashIncludeSecureHash, p.Name, logger)
                            Case "deterministichashmaterializedfilename"
                                hashMaterializedFileName = ParseString(p.Value, hashMaterializedFileName, p.Name, logger)
                            Case Else
                                LogGuard.Warn(logger, $"[Config] Unbekannter Schluessel '{p.Name}' ignoriert.")
                        End Select
                    Next
                End Using

                Dim nextHashOptions = New HashOptions With {
                        .IncludePayloadCopies = hashIncludePayloadCopies,
                        .IncludeFastHash = hashIncludeFastHash,
                        .IncludeSecureHash = hashIncludeSecureHash,
                        .MaterializedFileName = hashMaterializedFileName
                        }

                Dim nextOptions = New FileTypeProjectOptions(headerOnlyNonZip) With {
                        .MaxBytes = maxBytes,
                        .SniffBytes = sniffBytes,
                        .MaxZipEntries = maxZipEntries,
                        .MaxZipTotalUncompressedBytes = maxZipTotalUncompressedBytes,
                        .MaxZipEntryUncompressedBytes = maxZipEntryUncompressedBytes,
                        .MaxZipCompressionRatio = maxZipCompressionRatio,
                        .MaxZipNestingDepth = maxZipNestingDepth,
                        .MaxZipNestedBytes = maxZipNestedBytes,
                        .RejectArchiveLinks = rejectArchiveLinks,
                        .AllowUnknownArchiveEntrySize = allowUnknownArchiveEntrySize,
                        .Logger = logger,
                        .DeterministicHash = nextHashOptions
                        }
                nextOptions.NormalizeInPlace()
                SetSnapshot(nextOptions)
                Return True
            Catch ex As Exception When _
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is Text.Json.JsonException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is FormatException
                LogGuard.Warn(GetSnapshot().Logger, $"[Config] Parse-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        '''     Liefert den aktuell gesetzten globalen Options-Snapshot als JSON-Dokument.
        ''' </summary>
        ''' <remarks>
        '''     Ausgabe dient der auditierbaren Repräsentation der effektiven Laufzeitkonfiguration.
        ''' </remarks>
        ''' <returns>JSON-Serialisierung der normierten Optionen.</returns>
        Public Shared Function GetOptions() As String
            Dim opt = GetSnapshot()
            Dim dto As New Dictionary(Of String, Object) From {
                    {"headerOnlyNonZip", opt.HeaderOnlyNonZip},
                    {"maxBytes", opt.MaxBytes},
                    {"sniffBytes", opt.SniffBytes},
                    {"maxZipEntries", opt.MaxZipEntries},
                    {"maxZipTotalUncompressedBytes", opt.MaxZipTotalUncompressedBytes},
                    {"maxZipEntryUncompressedBytes", opt.MaxZipEntryUncompressedBytes},
                    {"maxZipCompressionRatio", opt.MaxZipCompressionRatio},
                    {"maxZipNestingDepth", opt.MaxZipNestingDepth},
                    {"maxZipNestedBytes", opt.MaxZipNestedBytes},
                    {"rejectArchiveLinks", opt.RejectArchiveLinks},
                    {"allowUnknownArchiveEntrySize", opt.AllowUnknownArchiveEntrySize},
                    {"deterministicHash", New Dictionary(Of String, Object) From {
                    {"includePayloadCopies", opt.DeterministicHash.IncludePayloadCopies},
                    {"includeFastHash", opt.DeterministicHash.IncludeFastHash},
                    {"includeSecureHash", opt.DeterministicHash.IncludeSecureHash},
                    {"materializedFileName", opt.DeterministicHash.MaterializedFileName}
                    }}
                    }
            Return Text.Json.JsonSerializer.Serialize(dto)
        End Function

        Friend Shared Function LoadOptionsFromPath(path As String) As Boolean
            If String.IsNullOrWhiteSpace(path) OrElse Not IO.File.Exists(path) Then Return False
            If Not path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) Then Return False

            Try
                Return LoadOptions(IO.File.ReadAllText(path))
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IO.IOException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return False
            Catch ex As Exception
                Return False
            End Try
        End Function

        Friend Shared Function GetSnapshot() As FileTypeProjectOptions
            SyncLock OptionsLock
                Return Snapshot(_currentOptions)
            End SyncLock
        End Function

        Friend Shared Sub SetSnapshot(opt As FileTypeProjectOptions)
            SyncLock OptionsLock
                _currentOptions = Snapshot(opt)
            End SyncLock
        End Sub

        Private Shared Function SafeInt(el As Text.Json.JsonElement, fallback As Integer) As Integer
            Dim v As Integer
            If el.ValueKind = Text.Json.JsonValueKind.Number AndAlso el.TryGetInt32(v) Then Return v
            Return fallback
        End Function

        Private Shared Function SafeLong(el As Text.Json.JsonElement, fallback As Long) As Long
            Dim v As Long
            If el.ValueKind = Text.Json.JsonValueKind.Number AndAlso el.TryGetInt64(v) Then Return v
            Return fallback
        End Function

        Private Shared Function ParsePositiveInt(el As Text.Json.JsonElement, fallback As Integer,
                                                 name As String,
                                                 logger As Microsoft.Extensions.Logging.ILogger) As Integer
            Dim v = SafeInt(el, fallback)
            If v > 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParseNonNegativeInt(el As Text.Json.JsonElement, fallback As Integer,
                                                    name As String,
                                                    logger As Microsoft.Extensions.Logging.ILogger) As Integer
            Dim v = SafeInt(el, fallback)
            If v >= 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParsePositiveLong(el As Text.Json.JsonElement, fallback As Long,
                                                  name As String,
                                                  logger As Microsoft.Extensions.Logging.ILogger) _
            As Long
            Dim v = SafeLong(el, fallback)
            If v > 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParseBoolean(el As Text.Json.JsonElement, fallback As Boolean,
                                             name As String,
                                             logger As Microsoft.Extensions.Logging.ILogger) _
            As Boolean
            If el.ValueKind = Text.Json.JsonValueKind.True Then Return True
            If el.ValueKind = Text.Json.JsonValueKind.False Then Return False
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParseString(el As Text.Json.JsonElement, fallback As String,
                                            name As String,
                                            logger As Microsoft.Extensions.Logging.ILogger) _
            As String
            If el.ValueKind = Text.Json.JsonValueKind.String Then
                Dim value = el.GetString()
                If value IsNot Nothing Then Return value
            End If
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Sub TryParseHashOptions(
                                                            el As Text.Json.JsonElement,
                                                            ByRef includePayloadCopies As Boolean,
                                                            ByRef includeFastHash As Boolean,
                                                            ByRef includeSecureHash As Boolean,
                                                            ByRef materializedFileName As String,
                                                            logger As Microsoft.Extensions.Logging.ILogger)

            If el.ValueKind <> Text.Json.JsonValueKind.Object Then
                LogGuard.Warn(logger, "[Config] 'deterministicHash' muss ein JSON-Objekt sein.")
                Return
            End If

            For Each p In el.EnumerateObject()
                Select Case p.Name.ToLowerInvariant()
                    Case "includepayloadcopies"
                        includePayloadCopies = ParseBoolean(
                            p.Value,
                            includePayloadCopies,
                            $"deterministicHash.{p.Name}",
                            logger
                        )
                    Case "includefasthash"
                        includeFastHash = ParseBoolean(
                            p.Value,
                            includeFastHash,
                            $"deterministicHash.{p.Name}",
                            logger
                        )
                    Case "includesecurehash"
                        includeSecureHash = ParseBoolean(
                            p.Value,
                            includeSecureHash,
                            $"deterministicHash.{p.Name}",
                            logger
                        )
                    Case "materializedfilename"
                        materializedFileName = ParseString(
                            p.Value,
                            materializedFileName,
                            $"deterministicHash.{p.Name}",
                            logger
                        )

                    Case Else
                        LogGuard.Warn(logger, $"[Config] Unbekannter Schluessel 'deterministicHash.{p.Name}' ignoriert.")
                End Select
            Next
        End Sub

        Private Shared Function Snapshot(opt As FileTypeProjectOptions) As FileTypeProjectOptions
            If opt Is Nothing Then Return FileTypeProjectOptions.DefaultOptions()
            Dim snap = opt.Clone()
            snap.NormalizeInPlace()
            Return snap
        End Function
    End Class
End Namespace
