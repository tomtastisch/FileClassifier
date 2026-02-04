Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text.Json

Namespace FileTypeDetection

    ''' <summary>
    ''' Zentrale, globale Optionsverwaltung als JSON-Schnittstelle.
    ''' </summary>
    Public NotInheritable Class FileTypeOptions
        Private Shared ReadOnly _optionsLock As New Object()
        Private Shared _currentOptions As FileTypeDetectorOptions = FileTypeDetectorOptions.DefaultOptions()

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Laedt globale Optionen aus JSON.
        ''' Nicht gesetzte Felder bleiben auf Default-Werten.
        ''' </summary>
        Public Shared Function LoadOptions(json As String) As Boolean
            If String.IsNullOrWhiteSpace(json) Then Return False

            Dim defaults = FileTypeDetectorOptions.DefaultOptions()
            Dim headerOnlyNonZip = defaults.HeaderOnlyNonZip
            Dim maxBytes = defaults.MaxBytes
            Dim sniffBytes = defaults.SniffBytes
            Dim maxZipEntries = defaults.MaxZipEntries
            Dim maxZipTotalUncompressedBytes = defaults.MaxZipTotalUncompressedBytes
            Dim maxZipEntryUncompressedBytes = defaults.MaxZipEntryUncompressedBytes
            Dim maxZipCompressionRatio = defaults.MaxZipCompressionRatio
            Dim maxZipNestingDepth = defaults.MaxZipNestingDepth
            Dim maxZipNestedBytes = defaults.MaxZipNestedBytes
            Dim logger = defaults.Logger

            Try
                Using doc = JsonDocument.Parse(json, New JsonDocumentOptions With {.AllowTrailingCommas = True})
                    If doc.RootElement.ValueKind <> JsonValueKind.Object Then
                        LogGuard.Warn(logger, "[Config] Root muss ein JSON-Objekt sein.")
                        Return False
                    End If

                    For Each p In doc.RootElement.EnumerateObject()
                        Select Case p.Name.ToLowerInvariant()
                            Case "headeronlynonzip" : headerOnlyNonZip = ParseBoolean(p.Value, headerOnlyNonZip, p.Name, logger)
                            Case "maxbytes" : maxBytes = ParsePositiveLong(p.Value, maxBytes, p.Name, logger)
                            Case "sniffbytes" : sniffBytes = ParsePositiveInt(p.Value, sniffBytes, p.Name, logger)
                            Case "maxzipentries" : maxZipEntries = ParsePositiveInt(p.Value, maxZipEntries, p.Name, logger)
                            Case "maxziptotaluncompressedbytes" : maxZipTotalUncompressedBytes = ParsePositiveLong(p.Value, maxZipTotalUncompressedBytes, p.Name, logger)
                            Case "maxzipentryuncompressedbytes" : maxZipEntryUncompressedBytes = ParsePositiveLong(p.Value, maxZipEntryUncompressedBytes, p.Name, logger)
                            Case "maxzipcompressionratio" : maxZipCompressionRatio = ParseNonNegativeInt(p.Value, maxZipCompressionRatio, p.Name, logger)
                            Case "maxzipnestingdepth" : maxZipNestingDepth = ParseNonNegativeInt(p.Value, maxZipNestingDepth, p.Name, logger)
                            Case "maxzipnestedbytes" : maxZipNestedBytes = ParsePositiveLong(p.Value, maxZipNestedBytes, p.Name, logger)
                            Case Else
                                LogGuard.Warn(logger, $"[Config] Unbekannter Schluessel '{p.Name}' ignoriert.")
                        End Select
                    Next
                End Using

                Dim nextOptions = New FileTypeDetectorOptions(headerOnlyNonZip) With {
                    .MaxBytes = maxBytes,
                    .SniffBytes = sniffBytes,
                    .MaxZipEntries = maxZipEntries,
                    .MaxZipTotalUncompressedBytes = maxZipTotalUncompressedBytes,
                    .MaxZipEntryUncompressedBytes = maxZipEntryUncompressedBytes,
                    .MaxZipCompressionRatio = maxZipCompressionRatio,
                    .MaxZipNestingDepth = maxZipNestingDepth,
                    .MaxZipNestedBytes = maxZipNestedBytes,
                    .Logger = logger
                }
                SetSnapshot(nextOptions)
                Return True
            Catch ex As Exception
                LogGuard.Warn(GetSnapshot().Logger, $"[Config] Parse-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Liefert die aktuell gesetzten globalen Optionen als JSON.
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
                {"maxZipNestedBytes", opt.MaxZipNestedBytes}
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

        Friend Shared Function GetSnapshot() As FileTypeDetectorOptions
            SyncLock _optionsLock
                Return Snapshot(_currentOptions)
            End SyncLock
        End Function

        Friend Shared Sub SetSnapshot(opt As FileTypeDetectorOptions)
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

        Private Shared Function ParsePositiveInt(el As JsonElement, fallback As Integer, name As String, logger As Microsoft.Extensions.Logging.ILogger) As Integer
            Dim v = SafeInt(el, fallback)
            If v > 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParseNonNegativeInt(el As JsonElement, fallback As Integer, name As String, logger As Microsoft.Extensions.Logging.ILogger) As Integer
            Dim v = SafeInt(el, fallback)
            If v >= 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParsePositiveLong(el As JsonElement, fallback As Long, name As String, logger As Microsoft.Extensions.Logging.ILogger) As Long
            Dim v = SafeLong(el, fallback)
            If v > 0 Then Return v
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function ParseBoolean(el As JsonElement, fallback As Boolean, name As String, logger As Microsoft.Extensions.Logging.ILogger) As Boolean
            If el.ValueKind = JsonValueKind.True Then Return True
            If el.ValueKind = JsonValueKind.False Then Return False
            LogGuard.Warn(logger, $"[Config] Ungueltiger Wert fuer '{name}', fallback={fallback}.")
            Return fallback
        End Function

        Private Shared Function Snapshot(opt As FileTypeDetectorOptions) As FileTypeDetectorOptions
            If opt Is Nothing Then Return FileTypeDetectorOptions.DefaultOptions()
            Return opt.Clone()
        End Function
    End Class

End Namespace
