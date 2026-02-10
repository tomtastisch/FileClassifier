Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text.Json
Imports Microsoft.Extensions.Logging

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Zentrale, globale Optionsverwaltung als JSON-Schnittstelle.
    ''' </summary>
    Public NotInheritable Class FileTypeOptions
        Private Shared ReadOnly _optionsLock As New Object()
        Private Shared _currentOptions As FileTypeProjectOptions = FileTypeProjectOptions.DefaultOptions()

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Laedt globale Optionen aus JSON.
        '''     Nicht gesetzte Felder bleiben auf Default-Werten.
        ''' </summary>
        Public Shared Function LoadOptions(json As String) As Boolean
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
            Dim hashMaterializedFileName = defaults.DeterministicHash.MaterializedFileName
            Dim logger = defaults.Logger

            Try
                Using doc = JsonDocument.Parse(json, New JsonDocumentOptions With {.AllowTrailingCommas = True})
                    If doc.RootElement.ValueKind <> JsonValueKind.Object Then
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
                                TryParseDeterministicHashOptions(
                                    p.Value,
                                    hashIncludePayloadCopies,
                                    hashIncludeFastHash,
                                    hashMaterializedFileName,
                                    logger)
                            Case "deterministichashincludepayloadcopies"
                                hashIncludePayloadCopies = ParseBoolean(p.Value, hashIncludePayloadCopies, p.Name,
                                                                        logger)
                            Case "deterministichashincludefasthash"
                                hashIncludeFastHash = ParseBoolean(p.Value, hashIncludeFastHash, p.Name, logger)
                            Case "deterministichashmaterializedfilename"
                                hashMaterializedFileName = ParseString(p.Value, hashMaterializedFileName, p.Name, logger)
                            Case Else
                                LogGuard.Warn(logger, $"[Config] Unbekannter Schluessel '{p.Name}' ignoriert.")
                        End Select
                    Next
                End Using

                Dim nextHashOptions = New DeterministicHashOptions With {
                        .IncludePayloadCopies = hashIncludePayloadCopies,
                        .IncludeFastHash = hashIncludeFastHash,
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
            Catch ex As Exception
                LogGuard.Warn(GetSnapshot().Logger, $"[Config] Parse-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        '''     Liefert die aktuell gesetzten globalen Optionen als JSON.
        ''' </summary>
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
                    {"materializedFileName", opt.DeterministicHash.MaterializedFileName}
                    }}
                    }
            Return JsonSerializer.Serialize(dto)
        End Function

        Friend Shared Function LoadOptionsFromPath(path As String) As Boolean
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return False
            If Not path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) Then Return False

            Try
                Return LoadOptions(File.ReadAllText(path))
            Catch
                Return False
            End Try
        End Function

        Friend Shared Function GetSnapshot() As FileTypeProjectOptions
            SyncLock _optionsLock
                Return Snapshot(_currentOptions)
            End SyncLock
        End Function

        Friend Shared Sub SetSnapshot(opt As FileTypeProjectOptions)
            SyncLock _optionsLock
                _currentOptions = Snapshot(opt)
            End SyncLock
        End Sub

        Private Shared Function SafeInt(el As JsonElement, fallback As Integer) As Integer
            Dim v As Integer
            If el.ValueKind = JsonValueKind.Number AndAlso el.TryGetInt32(v) Then Return v
            Return fallback
        End Function

        Private Shared Function SafeLong(el As JsonElement, fallback As Long) As Long
            Dim v As Long
            If el.ValueKind = JsonValueKind.Number AndAlso el.TryGetInt64(v) Then Return v
            Return fallback
        End Function

        Private Shared Function ParsePositiveInt(el As JsonElement, fallback As Integer, name As String,
                                                 logger As ILogger) As Integer
            Dim v = SafeInt(el, fallback)
            If v > 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParseNonNegativeInt(el As JsonElement, fallback As Integer, name As String,
                                                    logger As ILogger) As Integer
            Dim v = SafeInt(el, fallback)
            If v >= 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParsePositiveLong(el As JsonElement, fallback As Long, name As String, logger As ILogger) _
            As Long
            Dim v = SafeLong(el, fallback)
            If v > 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParseBoolean(el As JsonElement, fallback As Boolean, name As String, logger As ILogger) _
            As Boolean
            If el.ValueKind = JsonValueKind.True Then Return True
            If el.ValueKind = JsonValueKind.False Then Return False
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParseString(el As JsonElement, fallback As String, name As String, logger As ILogger) _
            As String
            If el.ValueKind = JsonValueKind.String Then
                Dim value = el.GetString()
                If value IsNot Nothing Then Return value
            End If
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Sub TryParseDeterministicHashOptions(
                                                            el As JsonElement,
                                                            ByRef includePayloadCopies As Boolean,
                                                            ByRef includeFastHash As Boolean,
                                                            ByRef materializedFileName As String,
                                                            logger As ILogger)

            If el.ValueKind <> JsonValueKind.Object Then
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
