Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
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

            Dim nextOptions = FileTypeDetectorOptions.DefaultOptions()

            Try
                Using doc = JsonDocument.Parse(json, New JsonDocumentOptions With {.AllowTrailingCommas = True})
                    For Each p In doc.RootElement.EnumerateObject()
                        Select Case p.Name.ToLowerInvariant()
                            Case "maxbytes" : nextOptions.MaxBytes = SafeLong(p.Value, nextOptions.MaxBytes)
                            Case "sniffbytes" : nextOptions.SniffBytes = SafeInt(p.Value, nextOptions.SniffBytes)
                            Case "maxzipentries" : nextOptions.MaxZipEntries = SafeInt(p.Value, nextOptions.MaxZipEntries)
                            Case "maxziptotaluncompressedbytes" : nextOptions.MaxZipTotalUncompressedBytes = SafeLong(p.Value, nextOptions.MaxZipTotalUncompressedBytes)
                            Case "maxzipentryuncompressedbytes" : nextOptions.MaxZipEntryUncompressedBytes = SafeLong(p.Value, nextOptions.MaxZipEntryUncompressedBytes)
                            Case "maxzipcompressionratio" : nextOptions.MaxZipCompressionRatio = SafeInt(p.Value, nextOptions.MaxZipCompressionRatio)
                            Case "maxzipnestingdepth" : nextOptions.MaxZipNestingDepth = SafeInt(p.Value, nextOptions.MaxZipNestingDepth)
                            Case "maxzipnestedbytes" : nextOptions.MaxZipNestedBytes = SafeLong(p.Value, nextOptions.MaxZipNestedBytes)
                            Case Else
                                LogGuard.Warn(nextOptions.Logger, $"[Config] Unbekannter Schluessel '{p.Name}' ignoriert.")
                        End Select
                    Next
                End Using

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

        Private Shared Function Snapshot(opt As FileTypeDetectorOptions) As FileTypeDetectorOptions
            If opt Is Nothing Then Return FileTypeDetectorOptions.DefaultOptions()
            Return opt.Clone()
        End Function
    End Class

End Namespace
