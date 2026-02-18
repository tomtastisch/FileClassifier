' ============================================================================
' FILE: FileTypeDetector.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq
Imports System.Diagnostics.CodeAnalysis

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Öffentliche Haupt-API zur inhaltsbasierten Dateityp-Erkennung, Archivvalidierung und sicheren Extraktion.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Verantwortung: Die Klasse stellt die konsumierbare Erkennungs- und Archivschnittstelle bereit und kapselt
    '''         die fail-closed Entscheidungspfade für Datei- und Byte-Eingaben.
    '''     </para>
    '''     <para>
    '''         Invarianten:
    '''         1) Fehler- und Unsicherheitsfälle liefern deterministisch <see cref="FileKind.Unknown"/> bzw. <c>False</c>.
    '''         2) Signatur- und Typauflösung erfolgt zentral über <c>FileTypeRegistry</c> (SSOT).
    '''         3) Dateiendungen sind nur nachgelagerte Policy und nie Primärsignal.
    '''         4) Archive werden vor Refinement und Extraktion durch Sicherheits-Gates validiert.
    '''     </para>
    '''     <para>
    '''         Ein-/Ausgaben: Eingaben sind Dateipfade oder Byte-Payloads; Ausgaben sind typisierte
    '''         <see cref="FileType"/>-/Detail-Objekte oder boolesche Validierungsentscheidungen.
    '''     </para>
    '''     <para>
    '''         Nebenwirkungen: Dateisystemzugriffe (Lesen/Extraktion) sowie Protokollierung über den konfigurierten Logger.
    '''     </para>
    '''     <para>
    '''         Threading/Security: Die Instanz ist zustandslos; Sicherheitsgrenzen zu Größen, Archivstruktur und Traversal
    '''         werden über die konfigurierten Guard-Komponenten fail-closed erzwungen.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class FileTypeDetector
        Private Const ReasonUnknown As String = "Unknown"
        Private Const ReasonFileNotFound As String = "FileNotFound"
        Private Const ReasonInvalidLength As String = "InvalidLength"
        Private Const ReasonFileTooLarge As String = "FileTooLarge"
        Private Const ReasonException As String = "Exception"
        Private Const ReasonExceptionUnauthorizedAccess As String = "ExceptionUnauthorizedAccess"
        Private Const ReasonExceptionSecurity As String = "ExceptionSecurity"
        Private Const ReasonExceptionIO As String = "ExceptionIO"
        Private Const ReasonExceptionInvalidData As String = "ExceptionInvalidData"
        Private Const ReasonExceptionNotSupported As String = "ExceptionNotSupported"
        Private Const ReasonExceptionArgument As String = "ExceptionArgument"
        Private Const ReasonExtensionMismatch As String = "ExtensionMismatch"
        Private Const ReasonHeaderUnknown As String = "HeaderUnknown"
        Private Const ReasonHeaderMatch As String = "HeaderMatch"
        Private Const ReasonArchiveGateFailed As String = "ArchiveGateFailed"
        Private Const ReasonArchiveStructuredRefined As String = "ArchiveStructuredRefined"
        Private Const ReasonArchiveRefined As String = "ArchiveRefined"
        Private Const ReasonArchiveGeneric As String = "ArchiveGeneric"
        Private Const ReasonOfficeBinaryRefined As String = "OfficeBinaryRefined"

        ''' <summary>
        '''     Setzt globale Default-Optionen als Snapshot.
        ''' </summary>
        ''' <param name="opt">Quelloptionen für den globalen Snapshot.</param>
        Friend Shared Sub SetDefaultOptions(opt As FileTypeProjectOptions)

            FileTypeOptions.SetSnapshot(opt)
        End Sub

        ''' <summary>
        '''     Liefert einen Snapshot der aktuellen Default-Optionen.
        ''' </summary>
        ''' <returns>Unabhängige Kopie der globalen Optionen.</returns>
        Friend Shared Function GetDefaultOptions() As FileTypeProjectOptions

            Return FileTypeOptions.GetSnapshot()
        End Function

        ''' <summary>
        '''     Liest eine Datei begrenzt in den Arbeitsspeicher ein.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Guard-Clauses für Pfadgültigkeit und Dateiexistenz.
        '''         2) Größenprüfung gegen <c>MaxBytes</c>.
        '''         3) Begrenztes Streaming in den Arbeitsspeicher.
        '''     </para>
        '''     <para>
        '''         Fail-Closed: Bei Verstoß oder Ausnahme wird deterministisch ein leeres Byte-Array zurückgegeben.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Dateipfad der Quelldatei.</param>
        ''' <returns>Gelesene Bytes oder ein leeres Array bei Fehlern bzw. Regelverletzungen.</returns>
        Public Shared Function ReadFileSafe _
            (
                path As String
            ) As Byte()

            Dim opt As FileTypeProjectOptions = GetDefaultOptions()
            Dim fi As FileInfo

            ' Guard-Clauses: Pfad und Dateiexistenz.
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                Return Array.Empty(Of Byte)()
            End If

            Try
                ' Größenprüfung: Datei muss innerhalb der konfigurierten Grenzen liegen.
                fi = New FileInfo(path)
                If fi.Length < 0 OrElse fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu groß ({fi.Length} > {opt.MaxBytes}).")
                    Return Array.Empty(Of Byte)()
                End If

                ' Bounded-Read: Sequenzielles Streaming mit harter Obergrenze.
                Using _
                    fs As _
                        New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Using ms As New MemoryStream(CInt(Math.Min(fi.Length, Integer.MaxValue)))
                        StreamBounds.CopyBounded(fs, ms, opt.MaxBytes)
                        Return ms.ToArray()
                    End Using
                End Using

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return LogReadFileSafeFailure(opt, ex)
            End Try
        End Function

        ''' <summary>
        '''     Erkennt den Dateityp anhand eines Dateipfads ohne Endungsprüfung.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit <c>verifyExtension:=False</c>.
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu klassifizierenden Datei.</param>
        ''' <returns>Erkannter Typ oder <see cref="FileKind.Unknown"/> bei Fehlern.</returns>
        Public Function Detect _
            (
                path As String
            ) As FileType

            Return Detect(path, verifyExtension:=False)
        End Function

        ''' <summary>
        '''     Erkennt den Dateityp anhand eines Dateipfads mit optionaler Endungsprüfung.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Entscheidungspfad:
        '''         1) Header-/Registry-Erkennung (SSOT),
        '''         2) Archiv-Gate und optionales OOXML-Refinement,
        '''         3) optionale Endungs-Policy.
        '''     </para>
        '''     <para>
        '''         Bei aktivierter Endungsprüfung wird ein Mismatch fail-closed als <see cref="FileKind.Unknown"/> bewertet.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu klassifizierenden Datei.</param>
        ''' <param name="verifyExtension"><c>True</c> erzwingt die fail-closed Endungsprüfung nach Inhaltsdetektion.</param>
        ''' <returns>Erkannter Typ oder <see cref="FileKind.Unknown"/> bei Mismatch oder Fehlern.</returns>
        <SuppressMessage("Performance", "CA1822:Mark members as static", Justification:="Public instance API; changing to Shared would be a breaking API change.")>
        Public Function Detect _
            (
                path As String,
                verifyExtension As Boolean
            ) As FileType

            Dim detected = DetectPathCore(path)
            Return ApplyExtensionPolicy(path, detected, verifyExtension)
        End Function

        ''' <summary>
        '''     Liefert ein detailliertes, auditierbares Detektionsergebnis ohne Endungsprüfung.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit <c>verifyExtension:=False</c>.
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu klassifizierenden Datei.</param>
        ''' <returns>Detailliertes Detektionsergebnis inklusive Reason-Code und Trace-Flags.</returns>
        Public Function DetectDetailed _
            (
                path As String
            ) As DetectionDetail

            Return DetectDetailed(path, verifyExtension:=False)
        End Function

        ''' <summary>
        '''     Liefert ein detailliertes, auditierbares Detektionsergebnis inklusive optionaler Endungs-Policy.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Inhaltsdetektion mit Trace-Erfassung,
        '''         2) optionale Endungsprüfung,
        '''         3) Ausgabe als <see cref="DetectionDetail"/> inklusive Nachvollziehbarkeitsflags.
        '''     </para>
        '''     <para>
        '''         Bei Endungs-Mismatch wird fail-closed auf <see cref="FileKind.Unknown"/> gesetzt und der Reason-Code angepasst.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu klassifizierenden Datei.</param>
        ''' <param name="verifyExtension"><c>True</c> aktiviert die Endungsprüfung nach Inhaltsdetektion.</param>
        ''' <returns>Detailliertes Detektionsergebnis mit typisiertem Trace-Kontext.</returns>
        ''' <example>
        '''     <code language="vb">
        ''' Dim detector As New FileTypeDetector()
        ''' Dim detail As DetectionDetail = detector.DetectDetailed("beleg.docx", verifyExtension:=True)
        ''' If detail.Detected.Kind = FileKind.Unknown Then
        '''     Console.WriteLine(detail.ReasonCode)
        ''' End If
        '''     </code>
        ''' </example>
        <SuppressMessage("Performance", "CA1822:Mark members as static", Justification:="Public instance API; changing to Shared would be a breaking API change.")>
        Public Function DetectDetailed _
            (
                path As String,
                verifyExtension As Boolean
            ) As DetectionDetail

            Dim opt = GetDefaultOptions()
            Dim trace As DetectionTrace = DetectionTrace.Empty

            ' Inhaltsdetektion: Primärentscheidung auf Basis Header/Container.
            Dim detected As FileType = DetectPathCoreWithTrace(path, opt, trace)

            ' Endungs-Policy: optionaler, nachgelagerter Konsistenzcheck.
            Dim extensionOk = True
            If verifyExtension Then
                extensionOk = ExtensionMatchesKind(path, detected.Kind)
                If Not extensionOk Then
                    detected = UnknownType()
                    trace.ReasonCode = ReasonExtensionMismatch
                End If
            End If

            ' Ergebnisaufbau: auditierbares Detailobjekt für Konsumenten.
            Return New DetectionDetail(
                detected,
                trace.ReasonCode,
                trace.UsedZipContentCheck,
                trace.UsedStructuredRefinement,
                verifyExtension AndAlso extensionOk)
        End Function

        ''' <summary>
        '''     Prüft, ob die Dateiendung zum inhaltsbasiert erkannten Typ passt.
        ''' </summary>
        ''' <remarks>
        '''     Fehlende Endung wird als neutral bewertet; unbekannter erkannter Typ führt fail-closed zu <c>False</c>.
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu klassifizierenden Datei.</param>
        ''' <returns><c>True</c> bei passender oder fehlender Endung; sonst <c>False</c>.</returns>
        Public Function DetectAndVerifyExtension _
            (
                path As String
            ) As Boolean

            Dim detected = Detect(path)
            Return ExtensionMatchesKind(path, detected.Kind)
        End Function

        ''' <summary>
        '''     Prüft fail-closed, ob eine Datei ein sicheres, extrahierbares Archiv repräsentiert.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Guard-Clauses für Pfad und Existenz,
        '''         2) Containerbeschreibung,
        '''         3) Safety-Gate gegen die konfigurierten Archivgrenzen.
        '''     </para>
        '''     <para>
        '''         Fehlerpfade sind fail-closed und liefern deterministisch <c>False</c>.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu validierenden Archivdatei.</param>
        ''' <returns><c>True</c>, wenn Typprüfung und Safety-Gate für ein extrahierbares Archiv bestehen; sonst <c>False</c>.</returns>
        Public Shared Function TryValidateArchive _
            (
                path As String
            ) As Boolean

            Dim opt As FileTypeProjectOptions = GetDefaultOptions()
            Dim descriptor As ArchiveDescriptor = ArchiveDescriptor.UnknownDescriptor()
            Dim detected As FileType

            ' Guard-Clauses: Pfad und Dateiexistenz.
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return False
            detected = DetectPathCore(path)
            If Not IsArchiveContainerKind(detected.Kind) Then Return False

            Try
                ' describe -> safety gate.
                Using _
                    fs As _
                        New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    If Not ArchiveTypeResolver.TryDescribeStream(fs, opt, descriptor) Then Return False
                    If fs.CanSeek Then fs.Position = 0
                    Return ArchiveSafetyGate.IsArchiveSafeStream(fs, opt, descriptor, depth:=0)
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                Return False
            End Try
        End Function

        ''' <summary>
        '''     Führt die Pfad-Detektion ohne Endungs-Policy aus und liefert nur das inhaltsbasierte Ergebnis.
        ''' </summary>
        ''' <param name="path">Pfad zur zu erkennenden Datei.</param>
        ''' <returns>Erkannter Typ oder <see cref="FileKind.Unknown"/>.</returns>
        Private Shared Function DetectPathCore(
                path As String
            ) As FileType

            Dim opt = GetDefaultOptions()
            Dim trace As DetectionTrace = DetectionTrace.Empty
            Return DetectPathCoreWithTrace(path, opt, trace)
        End Function

        ''' <summary>
        '''     Kernpfad für inhaltsbasierte Dateityperkennung inklusive Trace-Erfassung.
        ''' </summary>
        ''' <remarks>
        '''     Die Funktion kapselt File-Guards, Header-Lesen und die zentrale Header-/Archiv-Auflösung.
        '''     Fehlerpfade bleiben fail-closed und setzen den passenden Reason-Code.
        ''' </remarks>
        ''' <param name="path">Dateipfad der Quelldatei.</param>
        ''' <param name="opt">Options-Snapshot für Größen- und Sicherheitsgrenzen.</param>
        ''' <param name="trace">Rückkanal für auditierbare Entscheidungsinformationen.</param>
        ''' <returns>Erkannter Typ oder <see cref="FileKind.Unknown"/>.</returns>
        Private Shared Function DetectPathCoreWithTrace(
                path As String,
                opt As FileTypeProjectOptions,
                ByRef trace As DetectionTrace
            ) As FileType

            Dim fi As FileInfo
            Dim header As Byte()

            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                trace.ReasonCode = ReasonFileNotFound
                Return UnknownType()
            End If

            Try
                fi = New FileInfo(path)
                If fi.Length < 0 Then
                    trace.ReasonCode = ReasonInvalidLength
                    Return UnknownType()
                End If

                If fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu groß ({fi.Length} > {opt.MaxBytes}).")
                    trace.ReasonCode = ReasonFileTooLarge
                    Return UnknownType()
                End If

                Using fs As New FileStream(
                        path, FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        InternalIoDefaults.FileStreamBufferSize,
                        FileOptions.SequentialScan
                    )

                    header = ReadHeader(fs, opt.SniffBytes, opt.MaxBytes)
                    Return ResolveByHeaderForPath(header, opt, trace, fs)

                End Using

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return LogDetectFailure(opt, trace, ex)
            End Try
        End Function

        ''' <summary>
        '''     Erkennt den Dateityp anhand von In-Memory-Daten.
        ''' </summary>
        ''' <remarks>
        '''     Die Operation ist rein speicherbasiert und unterliegt denselben Größen- und Sicherheitsregeln wie die Pfadvariante.
        ''' </remarks>
        ''' <param name="data">Zu prüfende Nutzdaten.</param>
        ''' <returns>Erkannter Typ oder <see cref="FileKind.Unknown"/> bei Fehlern.</returns>
        <SuppressMessage("Performance", "CA1822:Mark members as static", Justification:="Public instance API; changing to Shared would be a breaking API change.")>
        Public Function Detect _
            (
                data As Byte()
            ) As FileType

            Dim opt = GetDefaultOptions()
            Return DetectInternalBytes(data, opt)
        End Function

        ''' <summary>
        '''     Führt eine deterministische Typprüfung als Convenience-API aus.
        ''' </summary>
        ''' <remarks>
        '''     Ergebnis basiert vollständig auf der inhaltsbasierten Detektion ohne Endungsprüfung.
        ''' </remarks>
        ''' <param name="data">Zu prüfende Nutzdaten.</param>
        ''' <param name="kind">Erwarteter Dateityp.</param>
        ''' <returns><c>True</c> bei Typgleichheit, sonst <c>False</c>.</returns>
        Public Function IsOfType _
            (
                data As Byte(),
                kind As FileKind
            ) As Boolean

            Return Detect(data).Kind = kind
        End Function

        ''' <summary>
        '''     Entpackt ein Archiv deterministisch und fail-closed in ein neues Zielverzeichnis.
        '''     Sicherheitsregeln (Traversal/Limits/Nesting) sind immer aktiv.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Guard-Clauses und optionale Vorprüfung des Quelltyps,
        '''         2) Laden der Payload über <see cref="ReadFileSafe(String)"/>,
        '''         3) sichere Persistenz via <see cref="FileMaterializer"/> mit <c>secureExtract:=True</c>.
        '''     </para>
        '''     <para>
        '''         Nebenwirkungen: Zielverzeichnis wird bei Erfolg erstellt bzw. beschrieben.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Pfad zur Archivdatei.</param>
        ''' <param name="destinationDirectory">Leeres, noch nicht existierendes Zielverzeichnis.</param>
        ''' <param name="verifyBeforeExtract"><c>True</c> aktiviert zusätzlich eine vollständige Vorvalidierung über <see cref="TryValidateArchive(String)"/>.</param>
        ''' <returns><c>True</c> bei erfolgreichem, atomarem Entpacken; sonst <c>False</c>.</returns>
        Public Function ExtractArchiveSafe _
            (
                path As String,
                destinationDirectory As String,
                verifyBeforeExtract As Boolean
            ) As Boolean

            Dim opt As FileTypeProjectOptions = GetDefaultOptions()
            Dim payload As Byte()
            If Not CanExtractArchivePath(path, verifyBeforeExtract, opt) Then Return False

            Try
                payload = ReadFileSafe(path)

                If payload.Length = 0 Then Return False

                Return FileMaterializer.Persist(
                        payload,
                        destinationDirectory,
                        overwrite:=False,
                        secureExtract:=True
                    )

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return LogArchiveExtractFailure(opt, ex)
            End Try
        End Function

        ''' <summary>
        '''     Extrahiert Archiv-Inhalte sicher in Memory und gibt sie als wiederverwendbare Objekte zurück.
        '''     Es erfolgt keine persistente Speicherung; Fehler liefern fail-closed eine leere Liste.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Guard-Clauses und optionale Vorprüfung,
        '''         2) sequenzielles Lesen des Quellarchivs,
        '''         3) sichere In-Memory-Extraktion.
        '''     </para>
        '''     <para>
        '''         Die Methode erzeugt keine persistente Dateisystem-Nebenwirkung und liefert bei Fehlern eine leere Liste.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Pfad zur Archivdatei.</param>
        ''' <param name="verifyBeforeExtract"><c>True</c> aktiviert zusätzlich eine vollständige Vorvalidierung über <see cref="TryValidateArchive(String)"/>.</param>
        ''' <returns>Read-only Liste extrahierter Einträge oder leer bei Fehler.</returns>
        Public Function ExtractArchiveSafeToMemory _
            (
                path As String,
                verifyBeforeExtract As Boolean
            ) _
            As IReadOnlyList(Of ZipExtractedEntry)

            Dim opt = GetDefaultOptions()
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If Not CanExtractArchivePath(path, verifyBeforeExtract, opt) Then Return emptyResult

            Try
                Using fs As New FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        InternalIoDefaults.FileStreamBufferSize,
                        FileOptions.SequentialScan
                    )
                    Return ArchiveExtractor.TryExtractArchiveStreamToMemory(fs, opt)

                End Using

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return LogArchiveExtractFailure(opt, ex, emptyResult)
            End Try
        End Function

        ''' <summary>
        '''     Byte-basierte Detektion mit denselben Sicherheitsgrenzen wie die Pfadvariante.
        ''' </summary>
        ''' <param name="data">Zu detektierende Nutzdaten.</param>
        ''' <param name="opt">Options-Snapshot mit Maximalgrenzen.</param>
        ''' <returns>Erkannter Typ oder <see cref="FileKind.Unknown"/>.</returns>
        Private Shared Function DetectInternalBytes(
                data As Byte(),
                opt As FileTypeProjectOptions
            ) As FileType

            Dim trace As DetectionTrace = DetectionTrace.Empty

            If data Is Nothing OrElse data.Length = 0 Then Return UnknownType()

            If CLng(data.Length) > opt.MaxBytes Then
                LogGuard.Warn(opt.Logger, $"[Detect] Daten zu groß ({data.Length} > {opt.MaxBytes}).")
                Return UnknownType()
            End If

            Try
                Return ResolveByHeaderForBytes(data, opt, trace, data)

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return LogDetectFailure(opt, ex)
            End Try
        End Function

        ''' <summary>
        '''     Entscheidungslogik für die Pfad-Variante.
        ''' </summary>
        Private Shared Function ResolveByHeaderForPath(
                header As Byte(),
                opt As FileTypeProjectOptions,
                ByRef trace As DetectionTrace,
                fs As FileStream
            ) As FileType

            Return ResolveByHeaderCommon(
                header,
                opt,
                trace,
                tryDescribe:=Function()
                                 Return TryDescribeArchiveStreamDescriptor(fs, opt)
                             End Function,
                tryValidate:=Function(descriptor)
                                 Return ValidateArchiveStreamRaw(fs, opt, descriptor)
                             End Function,
                tryRefine:=Function()
                               Return OpenXmlRefiner.TryRefineStream(fs)
                           End Function,
                tryRefineLegacyOffice:=Function()
                                           Return LegacyOfficeBinaryRefiner.TryRefineStream(
                                               fs, ResolveLegacyOfficeProbeBytes(opt))
                                       End Function)
        End Function

        ''' <summary>
        '''     Entscheidungslogik für die Byte-Variante.
        ''' </summary>
        Private Shared Function ResolveByHeaderForBytes(
                                                 header As Byte(),
                                                 opt As FileTypeProjectOptions,
                                                 ByRef trace As DetectionTrace,
                                                 data As Byte()
                                                 ) As FileType

            Return ResolveByHeaderCommon(
                header,
                opt,
                trace,
                tryDescribe:=Function()
                                 Return TryDescribeArchiveBytesDescriptor(data, opt)
                             End Function,
                tryValidate:=Function(descriptor)
                                 Return ValidateArchiveBytesRaw(data, opt, descriptor)
                             End Function,
                tryRefine:=Function()
                               Using ms = CreateReadOnlyMemoryStream(data)
                                   Return OpenXmlRefiner.TryRefineStream(ms)
                               End Using
                           End Function,
                tryRefineLegacyOffice:=Function()
                                           Return LegacyOfficeBinaryRefiner.TryRefineBytes(data)
                                       End Function)
        End Function

        ''' <summary>
        '''     Zentraler Entscheidungsfluss für Header-/Archiv-/Refinement-Pfade.
        ''' </summary>
        ''' <remarks>
        '''     Reihenfolge:
        '''     1) direkte Header-Matches,
        '''     2) Legacy-Office-Refinement (OLE),
        '''     3) Archiv-Beschreibung + Safety-Gate,
        '''     4) optionales strukturiertes ZIP-Refinement.
        ''' </remarks>
        ''' <param name="header">Gelesene Header-Bytes.</param>
        ''' <param name="opt">Options-Snapshot.</param>
        ''' <param name="trace">Audit-Trace.</param>
        ''' <param name="tryDescribe">Archiv-Descriptor-Factory.</param>
        ''' <param name="tryValidate">Archiv-Safety-Validator.</param>
        ''' <param name="tryRefine">ZIP-basiertes Office/OpenDocument-Refinement.</param>
        ''' <param name="tryRefineLegacyOffice">OLE-basiertes Legacy-Office-Refinement.</param>
        ''' <returns>Erkannter Dateityp oder <see cref="FileKind.Unknown"/>.</returns>
        Private Shared Function ResolveByHeaderCommon(
                                               header As Byte(),
                                               opt As FileTypeProjectOptions,
                                               ByRef trace As DetectionTrace,
                                               tryDescribe As Func(Of ArchiveDescriptor),
                                               tryValidate As Func(Of ArchiveDescriptor, Boolean),
                                               tryRefine As Func(Of FileType),
                                               tryRefineLegacyOffice As Func(Of FileType)
                                               ) As FileType

            Dim magicKind As FileKind
            Dim descriptor As ArchiveDescriptor
            Dim legacyOfficeType As FileType

            If header Is Nothing OrElse header.Length = 0 Then
                trace.ReasonCode = ReasonHeaderUnknown
                Return UnknownType()
            End If

            magicKind = FileTypeRegistry.DetectByMagic(header)
            If magicKind <> FileKind.Unknown AndAlso magicKind <> FileKind.Zip Then
                trace.ReasonCode = ReasonHeaderMatch
                Return FileTypeRegistry.Resolve(magicKind)
            End If

            If magicKind = FileKind.Unknown AndAlso LegacyOfficeBinaryRefiner.IsOleCompoundHeader(header) Then
                legacyOfficeType = tryRefineLegacyOffice()
                If legacyOfficeType.Kind <> FileKind.Unknown Then
                    trace.ReasonCode = ReasonOfficeBinaryRefined
                    Return legacyOfficeType
                End If
            End If

            If magicKind = FileKind.Zip Then
                descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip)
            Else
                descriptor = tryDescribe()
                If descriptor Is Nothing Then
                    trace.ReasonCode = ReasonHeaderUnknown
                    Return UnknownType()
                End If
            End If

            trace.UsedZipContentCheck = True
            If Not tryValidate(descriptor) Then
                LogGuard.Warn(opt.Logger, "[Detect] Archive-Gate verletzt.")
                trace.ReasonCode = ReasonArchiveGateFailed
                Return UnknownType()
            End If

            Return ResolveAfterArchiveGate(magicKind, opt, trace, tryRefine)
        End Function

        ''' <summary>
        '''     Ermittelt die maximale Probegröße für Legacy-OLE-Refinement.
        ''' </summary>
        ''' <param name="opt">Options-Snapshot oder <c>Nothing</c>.</param>
        ''' <returns>Probegröße in Byte, defensiv begrenzt auf 1 MiB.</returns>
        Private Shared Function ResolveLegacyOfficeProbeBytes(opt As FileTypeProjectOptions) As Integer
            Dim maxProbe As Long

            If opt Is Nothing Then Return 1048576

            maxProbe = Math.Min(opt.MaxBytes, 1048576L)
            If maxProbe <= 0 Then Return 1048576
            Return CInt(maxProbe)
        End Function

        Private Shared Function TryDescribeArchiveStreamDescriptor(
                                                                   fs As FileStream,
                                                                   opt As FileTypeProjectOptions
                                                                   ) As ArchiveDescriptor

            Dim descriptor As ArchiveDescriptor = ArchiveDescriptor.UnknownDescriptor()

            If Not ArchiveTypeResolver.TryDescribeStream(fs, opt, descriptor) Then Return Nothing
            Return descriptor
        End Function

        Private Shared Function TryDescribeArchiveBytesDescriptor(
                                                                  data As Byte(),
                                                                  opt As FileTypeProjectOptions
                                                                  ) As ArchiveDescriptor

            Dim descriptor As ArchiveDescriptor = ArchiveDescriptor.UnknownDescriptor()

            If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return Nothing
            Return descriptor
        End Function

        Private Shared Function ValidateArchiveStreamRaw(
                fs As FileStream,
                opt As FileTypeProjectOptions,
                descriptor As ArchiveDescriptor
            ) As Boolean

            If fs Is Nothing OrElse Not fs.CanRead Then Return False
            If fs.CanSeek Then fs.Position = 0
            Return ArchiveSafetyGate.IsArchiveSafeStream(fs, opt, descriptor, depth:=0)
        End Function

        Private Shared Function ValidateArchiveBytesRaw(
                data As Byte(),
                opt As FileTypeProjectOptions,
                descriptor As ArchiveDescriptor
            ) As Boolean

            Return ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor)
        End Function

        Private Shared Function ResolveAfterArchiveGate(
                magicKind As FileKind,
                opt As FileTypeProjectOptions,
                ByRef trace As DetectionTrace,
                tryRefine As Func(Of FileType)
            ) As FileType

            Dim refined As FileType

            If magicKind <> FileKind.Zip Then
                trace.ReasonCode = ReasonArchiveGeneric
                Return FileTypeRegistry.Resolve(FileKind.Zip)
            End If

            refined = tryRefine()
            Return FinalizeArchiveDetection(refined, opt, trace)
        End Function

        Private Shared Function FinalizeArchiveDetection(
                refined As FileType,
                opt As FileTypeProjectOptions,
                ByRef trace As DetectionTrace
            ) As FileType

            If refined.Kind <> FileKind.Unknown Then
                WarnIfNoDirectContentDetection(refined.Kind, opt)
                trace.UsedStructuredRefinement =
                    (refined.Kind = FileKind.Docx OrElse refined.Kind = FileKind.Xlsx OrElse
                     refined.Kind = FileKind.Pptx)
                trace.ReasonCode =
                    If(trace.UsedStructuredRefinement, ReasonArchiveStructuredRefined, ReasonArchiveRefined)
                Return refined
            End If

            trace.ReasonCode = ReasonArchiveGeneric
            Return FileTypeRegistry.Resolve(FileKind.Zip)
        End Function

        Private Function CanExtractArchivePath(
                path As String,
                verifyBeforeExtract As Boolean,
                opt As FileTypeProjectOptions
            ) As Boolean

            Dim detected As FileType

            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[ArchiveExtract] Quelldatei fehlt.")
                Return False
            End If

            detected = Detect(path)
            If Not IsArchiveContainerKind(detected.Kind) Then
                LogGuard.Warn(opt.Logger, $"[ArchiveExtract] Kein extrahierbarer Archivtyp ({detected.Kind}).")
                Return False
            End If

            If verifyBeforeExtract Then
                If Not TryValidateArchive(path) Then
                    LogGuard.Warn(opt.Logger, "[ArchiveExtract] Vorvalidierung fehlgeschlagen.")
                    Return False
                End If
            End If

            Return True
        End Function

        ''' <summary>
        '''     Wendet optional die Endungs-Policy auf ein inhaltsbasiertes Detektionsergebnis an.
        ''' </summary>
        ''' <param name="path">Dateipfad der Quelldatei.</param>
        ''' <param name="detected">Inhaltsbasiert erkannter Typ.</param>
        ''' <param name="verifyExtension"><c>True</c> aktiviert die Endungsprüfung.</param>
        ''' <returns>Detektierter Typ oder <see cref="FileKind.Unknown"/> bei aktivem Mismatch.</returns>
        Private Shared Function ApplyExtensionPolicy(path As String, detected As FileType, verifyExtension As Boolean) _
            As FileType

            If Not verifyExtension Then Return detected
            If ExtensionMatchesKind(path, detected.Kind) Then Return detected
            Return UnknownType()
        End Function

        ''' <summary>
        '''     Prüft, ob ein erkannter Typ ein tatsächlich extrahierbarer Archivcontainer ist.
        ''' </summary>
        ''' <param name="kind">Erkannter Dateityp.</param>
        ''' <returns><c>True</c> für extrahierbare Archivtypen, sonst <c>False</c>.</returns>
        Private Shared Function IsArchiveContainerKind(kind As FileKind) As Boolean

            Return kind = FileKind.Zip
        End Function

        Private Shared Sub WarnIfNoDirectContentDetection(kind As FileKind, opt As FileTypeProjectOptions)

            If kind = FileKind.Unknown Then Return
            If FileTypeRegistry.HasDirectContentDetection(kind) Then Return
            LogGuard.Warn(opt.Logger,
                          $"[Detect] Keine direkte Content-Erkennung für Typ '{kind _
                             }'. Ergebnis stammt aus Fallback/Refinement.")
        End Sub

        Private Shared Function ExtensionMatchesKind(path As String, detectedKind As FileKind) As Boolean

            Dim ext As String = IO.Path.GetExtension(If(path, String.Empty))
            Dim normalizedExt As String
            Dim detectedType As FileType

            If String.IsNullOrWhiteSpace(ext) Then Return True

            If detectedKind = FileKind.Unknown Then Return False

            normalizedExt = FileTypeRegistry.NormalizeAlias(ext)
            detectedType = FileTypeRegistry.Resolve(detectedKind)

            If normalizedExt = FileTypeRegistry.NormalizeAlias(detectedType.CanonicalExtension) Then
                Return True
            End If

            If Not detectedType.Aliases.IsDefault AndAlso
               detectedType.Aliases.Any(Function(a) String.Equals(a, normalizedExt, StringComparison.OrdinalIgnoreCase)) Then
                Return True
            End If

            Return False
        End Function

        Private Shared Function ReadHeader(input As FileStream, sniffBytes As Integer, maxBytes As Long) As Byte()

            Dim want As Integer
            Dim take As Integer
            Dim off As Integer
            Dim n As Integer
            Dim buf As Byte()
            Dim exact As Byte()

            Try
                If input Is Nothing OrElse Not input.CanRead Then Return Array.Empty(Of Byte)()
                If maxBytes <= 0 Then Return Array.Empty(Of Byte)()
                If input.CanSeek Then
                    If input.Length <= 0 OrElse input.Length > maxBytes Then Return Array.Empty(Of Byte)()
                    input.Position = 0
                End If

                want = sniffBytes
                If want <= 0 Then want = InternalIoDefaults.DefaultSniffBytes
                take = want
                If input.CanSeek Then
                    take = CInt(Math.Min(input.Length, want))
                End If
                If take <= 0 Then Return Array.Empty(Of Byte)()

                buf = New Byte(take - 1) {}
                off = 0
                While off < take
                    n = input.Read(buf, off, take - off)
                    If n <= 0 Then Exit While
                    off += n
                End While

                If off <= 0 Then Return Array.Empty(Of Byte)()
                If off < take Then
                    exact = New Byte(off - 1) {}
                    Array.Copy(buf, exact, off)
                    Return exact
                End If

                Return buf
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                Return Array.Empty(Of Byte)()
            End Try
        End Function

        Private Shared Function UnknownType() As FileType

            Return FileTypeRegistry.Resolve(FileKind.Unknown)
        End Function

        Private Shared Function LogReadFileSafeFailure(opt As FileTypeProjectOptions, ex As Exception) As Byte()

            LogGuard.Error(opt.Logger, "[Detect] ReadFileSafe Fehler.", ex)
            Return Array.Empty(Of Byte)()
        End Function

        Private Shared Function LogDetectFailure(opt As FileTypeProjectOptions, ex As Exception) As FileType

            LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
            Return UnknownType()
        End Function

        Private Shared Function LogDetectFailure(opt As FileTypeProjectOptions, ByRef trace As DetectionTrace,
                                                 ex As Exception) As FileType

            LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
            trace.ReasonCode = ExceptionToReasonCode(ex)
            Return UnknownType()
        End Function

        Private Shared Function ExceptionToReasonCode(ex As Exception) As String

            If ex Is Nothing Then Return ReasonException

            If TypeOf ex Is UnauthorizedAccessException Then Return ReasonExceptionUnauthorizedAccess
            If TypeOf ex Is Security.SecurityException Then Return ReasonExceptionSecurity
            If TypeOf ex Is IOException Then Return ReasonExceptionIO
            If TypeOf ex Is InvalidDataException Then Return ReasonExceptionInvalidData
            If TypeOf ex Is NotSupportedException Then Return ReasonExceptionNotSupported
            If TypeOf ex Is ArgumentException Then Return ReasonExceptionArgument

            Return ReasonException
        End Function

        Private Shared Function LogArchiveExtractFailure(opt As FileTypeProjectOptions, ex As Exception) As Boolean

            LogGuard.Error(opt.Logger, "[ArchiveExtract] Ausnahme, fail-closed.", ex)
            Return False
        End Function

        Private Shared Function LogArchiveExtractFailure(opt As FileTypeProjectOptions, ex As Exception,
                                                         emptyResult As IReadOnlyList(Of ZipExtractedEntry)) _
            As IReadOnlyList(Of ZipExtractedEntry)

            LogGuard.Error(opt.Logger, "[ArchiveExtract] Ausnahme, fail-closed.", ex)
            Return emptyResult
        End Function

        Private Shared Function CreateReadOnlyMemoryStream(data As Byte()) As MemoryStream

            Return New MemoryStream(data, 0, data.Length, writable:=False, publiclyVisible:=False)
        End Function

        ''' <summary>
        '''     Interner, unveränderlicher Datenträger <c>DetectionTrace</c> für strukturierte Verarbeitungsschritte.
        ''' </summary>
        Private Structure DetectionTrace
            Friend ReasonCode As String
            Friend UsedZipContentCheck As Boolean
            Friend UsedStructuredRefinement As Boolean

            Friend Shared ReadOnly Property Empty As DetectionTrace
                Get
                    Return New DetectionTrace With {.ReasonCode = ReasonUnknown}
                End Get
            End Property
        End Structure
    End Class
End Namespace
