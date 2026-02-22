' ============================================================================
' FILE: FileMaterializer.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.IO

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Materialisiert Byte-Payloads fail-closed in ein Dateiziel oder extrahiert Archive sicher in ein Zielverzeichnis.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Verantwortung: Die Klasse kapselt den deterministischen Persistenzpfad für Rohbytes und den sicheren
    '''         Extraktionspfad für Archivpayloads.
    '''     </para>
    '''     <para>
    '''         Invarianten:
    '''         1) Eingaben werden vor jeder I/O-Operation auf Null, Größenlimits und Zielpfad-Eignung geprüft.
    '''         2) Bei aktivem <c>secureExtract</c> wird vor der Extraktion stets ein Sicherheits-Gate ausgeführt.
    '''         3) Fehlerpfade sind fail-closed und liefern <c>False</c>.
    '''     </para>
    '''     <para>
    '''         Nebenwirkungen: Dateisystemzugriffe (Verzeichniserstellung, Dateischreiben, optionale Archivextraktion)
    '''         sowie Warn-/Fehlerprotokollierung über den konfigurierten Logger.
    '''     </para>
    '''     <para>
    '''         Security/Compliance: Pfadnormalisierung, Archiv-Sicherheitsprüfung und atomare Zielvorbereitung begrenzen
    '''         Traversal-, Overwrite- und Ressourcenrisiken im Sinne einer fail-closed Verarbeitung.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class FileMaterializer

        Private Const MaterializationModeReject As Integer = 0
        Private Const MaterializationModePersistRaw As Integer = 1
        Private Const MaterializationModeExtractArchive As Integer = 2

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Persistiert Rohbytes in den angegebenen Zielpfad mit Standardverhalten ohne Überschreiben und ohne sichere Extraktion.
        ''' </summary>
        ''' <remarks>
        '''     Diese Überladung delegiert vollständig auf die Vollüberladung mit <c>overwrite:=False</c> und
        '''     <c>secureExtract:=False</c>.
        ''' </remarks>
        ''' <param name="data">Zu persistierende Nutzdaten.</param>
        ''' <param name="destinationPath">Dateisystemziel für die Materialisierung.</param>
        ''' <returns><c>True</c> bei erfolgreicher Persistierung; andernfalls <c>False</c>.</returns>
        ''' <example>
        '''     <code language="vb">
        ''' Dim payload As Byte() = File.ReadAllBytes("input.bin")
        ''' Dim ok As Boolean = FileMaterializer.Persist(payload, "output.bin")
        '''     </code>
        ''' </example>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String
            ) As Boolean

            Return Persist(data, destinationPath, overwrite:=False, secureExtract:=False)

        End Function

        ''' <summary>
        '''     Persistiert Rohbytes in den angegebenen Zielpfad und steuert explizit das Überschreibverhalten.
        ''' </summary>
        ''' <remarks>
        '''     Diese Überladung delegiert vollständig auf die Vollüberladung mit <c>secureExtract:=False</c>.
        ''' </remarks>
        ''' <param name="data">Zu persistierende Nutzdaten.</param>
        ''' <param name="destinationPath">Dateisystemziel für die Materialisierung.</param>
        ''' <param name="overwrite"><c>True</c>, um ein bestehendes Ziel gemäß Zielpfad-Policy zu ersetzen; sonst <c>False</c>.</param>
        ''' <returns><c>True</c> bei erfolgreicher Persistierung; andernfalls <c>False</c>.</returns>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String,
                overwrite As Boolean
            ) As Boolean

            Return Persist(data, destinationPath, overwrite, secureExtract:=False)
        End Function

        ''' <summary>
        '''     Führt die vollständige Materialisierungslogik für Rohdaten und optional sichere Archivextraktion aus.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Guard-Clauses für Daten, Größenlimits und Zielpfad.
        '''         2) Pfadnormalisierung auf absoluten Zielpfad.
        '''         3) Optionaler <c>secureExtract</c>-Pfad mit Typbeschreibung, Safety-Gate und Extraktion.
        '''         4) Raw-Fallback als Bytepersistenz.
        '''     </para>
        '''     <para>
        '''         Fail-Closed: Jeder validierte Fehlerpfad führt zu <c>False</c>; es wird keine partielle Erfolgsmeldung erzeugt.
        '''     </para>
        ''' </remarks>
        ''' <param name="data">Zu materialisierende Nutzdaten.</param>
        ''' <param name="destinationPath">Datei- oder Verzeichnisziel abhängig vom Verarbeitungspfad.</param>
        ''' <param name="overwrite"><c>True</c>, um ein vorhandenes Ziel gemäß Zielpfad-Policy zu ersetzen.</param>
        ''' <param name="secureExtract"><c>True</c>, um Archivpayloads sicher validieren und
        ''' extrahieren zu können; sonst Rohpersistenz.</param>
        ''' <returns><c>True</c> bei erfolgreicher Materialisierung; andernfalls <c>False</c>.</returns>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String,
                overwrite As Boolean,
                secureExtract As Boolean
            ) As Boolean

            Dim opt                          As FileTypeProjectOptions = FileTypeOptions.GetSnapshot()
            Dim destinationFull              As String                 = String.Empty
            Dim descriptor                   As ArchiveDescriptor      = Nothing
            Dim isPayloadWithinLimit         As Boolean                = False
            Dim archiveDescribeSucceeded     As Boolean                = False
            Dim archiveSafetyPassed          As Boolean                = False
            Dim archiveSignatureCandidate    As Boolean                = False
            Dim delegatedMaterializationMode As Integer                = MaterializationModePersistRaw
            Dim materializationMode          As Integer                = MaterializationModePersistRaw

            ' Guard-Clauses: Null-, Größen- und Zielpfadprüfung.
            If data Is Nothing Then Return False

            If CsCoreRuntimeBridge.TryIsPayloadWithinMaxBytes(data.Length, opt.MaxBytes, isPayloadWithinLimit) Then
                If Not isPayloadWithinLimit Then
                    LogGuard.Warn(opt.Logger, $"[Materialize] Daten zu groß ({data.Length} > {opt.MaxBytes}).")
                    Return False
                End If
            Else
                If CLng(data.Length) > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Materialize] Daten zu groß ({data.Length} > {opt.MaxBytes}).")
                    Return False
                End If
            End If

            If String.IsNullOrWhiteSpace(destinationPath) Then Return False

            ' Pfadnormalisierung: Absoluten Zielpfad auflösen.
            If Not PathResolutionGuard.TryGetFullPath(
                    destinationPath,
                    opt,
                    "[Materialize] Ungültiger Zielpfad",
                    warnLevel:=True,
                    destinationFull
                ) Then
                Return False
            End If

            ' Secure-Extract-Branch: describe -> safety gate -> extract.
            If secureExtract Then
                archiveDescribeSucceeded = ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor)
                If archiveDescribeSucceeded Then
                    archiveSafetyPassed = ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor)
                Else
                    archiveSignatureCandidate = ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(data)
                End If
            End If

            materializationMode = ComputeLocalMaterializationMode(
                secureExtract:=secureExtract,
                archiveDescribeSucceeded:=archiveDescribeSucceeded,
                archiveSafetyPassed:=archiveSafetyPassed,
                archiveSignatureCandidate:=archiveSignatureCandidate
            )

            If CsCoreRuntimeBridge.TryDecideMaterializationMode(
                secureExtract:=secureExtract,
                archiveDescribeSucceeded:=archiveDescribeSucceeded,
                archiveSafetyPassed:=archiveSafetyPassed,
                archiveSignatureCandidate:=archiveSignatureCandidate,
                mode:=delegatedMaterializationMode
            ) Then
                If delegatedMaterializationMode <> materializationMode Then
                    LogGuard.Warn(
                        opt.Logger,
                        $"[Materialize] CSCore-Modusabweichung ({delegatedMaterializationMode} != {materializationMode}); lokaler Fail-Closed-Modus aktiv."
                    )
                Else
                    materializationMode = delegatedMaterializationMode
                End If
            End If

            Return MaterializeByMode(
                mode:=materializationMode,
                data:=data,
                destinationFull:=destinationFull,
                overwrite:=overwrite,
                opt:=opt,
                descriptor:=descriptor,
                archiveDescribeSucceeded:=archiveDescribeSucceeded,
                archiveSafetyPassed:=archiveSafetyPassed,
                archiveSignatureCandidate:=archiveSignatureCandidate
            )
        End Function

        Private Shared Function ComputeLocalMaterializationMode _
            (
                secureExtract As Boolean,
                archiveDescribeSucceeded As Boolean,
                archiveSafetyPassed As Boolean,
                archiveSignatureCandidate As Boolean
            ) As Integer

            If Not secureExtract Then
                Return MaterializationModePersistRaw
            End If

            If archiveDescribeSucceeded Then
                If archiveSafetyPassed Then
                    Return MaterializationModeExtractArchive
                End If

                Return MaterializationModeReject
            End If

            If archiveSignatureCandidate Then
                Return MaterializationModeReject
            End If

            Return MaterializationModePersistRaw
        End Function

        Private Shared Function MaterializeByMode _
            (
                mode As Integer,
                data As Byte(),
                destinationFull As String,
                overwrite As Boolean,
                opt As FileTypeProjectOptions,
                descriptor As ArchiveDescriptor,
                archiveDescribeSucceeded As Boolean,
                archiveSafetyPassed As Boolean,
                archiveSignatureCandidate As Boolean
            ) As Boolean

            Select Case mode
                Case MaterializationModeExtractArchive
                    If Not archiveDescribeSucceeded OrElse descriptor Is Nothing Then
                        LogGuard.Warn(opt.Logger, "[Materialize] Archivbeschreibung fehlt.")
                        Return False
                    End If

                    If Not archiveSafetyPassed Then
                        LogGuard.Warn(opt.Logger, "[Materialize] Archiv-Validierung fehlgeschlagen.")
                        Return False
                    End If

                    Return MaterializeArchiveBytes(data, destinationFull, overwrite, opt, descriptor)

                Case MaterializationModeReject
                    If archiveDescribeSucceeded AndAlso Not archiveSafetyPassed Then
                        LogGuard.Warn(opt.Logger, "[Materialize] Archiv-Validierung fehlgeschlagen.")
                        Return False
                    End If

                    If Not archiveDescribeSucceeded AndAlso archiveSignatureCandidate Then
                        LogGuard.Warn(opt.Logger, "[Materialize] Archiv kann nicht gelesen werden.")
                        Return False
                    End If

                    LogGuard.Warn(opt.Logger, "[Materialize] Materialisierungsmodus wurde fail-closed abgelehnt.")
                    Return False

                Case MaterializationModePersistRaw
                    Return MaterializeRawBytes(data, destinationFull, overwrite, opt)

                Case Else
                    LogGuard.Warn(opt.Logger, $"[Materialize] Unbekannter Materialisierungsmodus: {mode}.")
                    Return False
            End Select
        End Function

        Private Shared Function MaterializeRawBytes _
            (
                data As Byte(),
                destinationFull As String,
                overwrite As Boolean,
                opt As FileTypeProjectOptions
            ) As Boolean

            Dim parent As String

            Try
                If Not DestinationPathGuard.PrepareMaterializationTarget(destinationFull, overwrite, opt) Then _
                    Return False

                parent = Path.GetDirectoryName(destinationFull)
                If String.IsNullOrWhiteSpace(parent) Then Return False
                Directory.CreateDirectory(parent)

                Using _
                    fs As _
                        New FileStream(destinationFull, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    fs.Write(data, 0, data.Length)
                End Using

                Return True

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse _
                TypeOf ex Is Security.SecurityException OrElse _
                TypeOf ex Is IOException OrElse _
                TypeOf ex Is InvalidDataException OrElse _
                TypeOf ex Is NotSupportedException OrElse _
                TypeOf ex Is ArgumentException

                LogGuard.Error(opt.Logger, "[Materialize] Byte-Persistenz fehlgeschlagen.", ex)
                Return False
            End Try
        End Function

        Private Shared Function MaterializeArchiveBytes _
            (
                data As Byte(),
                destinationFull As String,
                overwrite As Boolean,
                opt As FileTypeProjectOptions,
                descriptor As ArchiveDescriptor
            ) As Boolean

            Try
                If Not DestinationPathGuard.PrepareMaterializationTarget(destinationFull, overwrite, opt) Then _
                    Return False

                Using ms As New MemoryStream(data, writable:=False)
                    Return ArchiveExtractor.TryExtractArchiveStream(ms, destinationFull, opt, descriptor)
                End Using

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse _
                TypeOf ex Is Security.SecurityException OrElse _
                TypeOf ex Is IOException OrElse _
                TypeOf ex Is InvalidDataException OrElse _
                TypeOf ex Is NotSupportedException OrElse _
                TypeOf ex Is ArgumentException

                LogGuard.Error(opt.Logger, "[Materialize] Archiv-Extraktion fehlgeschlagen.", ex)
                Return False
            End Try
        End Function
    End Class
End Namespace
