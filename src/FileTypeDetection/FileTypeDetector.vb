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
    '''         Sicherheits- und Architekturprinzipien:
    '''         1) fail-closed: Fehlerpfade liefern deterministisch <see cref="FileKind.Unknown"/>.
    '''         2) SSOT: Signatur- und Typwissen wird zentral aus <c>FileTypeRegistry</c> aufgelöst.
    '''         3) Dateiendungen sind Metadaten; Primärentscheidung basiert auf Inhalt.
    '''         4) Archive durchlaufen Sicherheits-Gates und optionales strukturiertes Refinement.
    '''     </para>
    '''     <para>
    '''         Nebenwirkungen: Dateisystemzugriffe (Lesen/Extraktion) und Protokollierung über den konfigurierten Logger.
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
        '''     Die Methode erzwingt Größenlimits und liefert fail-closed ein leeres Byte-Array bei Fehlern.
        ''' </remarks>
        ''' <param name="path">Dateipfad der Quelldatei.</param>
        ''' <returns>Gelesene Bytes oder ein leeres Array bei Fehlern bzw. Regelverletzungen.</returns>
        ''' <exception cref="UnauthorizedAccessException">Kann bei Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="Security.SecurityException">Kann bei sicherheitsrelevantem Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="IOException">Kann bei I/O-Zugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="InvalidDataException">Kann bei ungültigen Datenzuständen intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="NotSupportedException">Kann bei nicht unterstützten Pfadformaten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="ArgumentException">Kann bei ungültigen Argumentzuständen intern auftreten und wird fail-closed behandelt.</exception>
        Public Shared Function ReadFileSafe _
            (
                path As String
            ) As Byte()

            Dim opt = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                Return Array.Empty(Of Byte)()
            End If

            Try
                Dim fi As New FileInfo(path)
                If fi.Length < 0 OrElse fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu gross ({fi.Length} > {opt.MaxBytes}).")
                    Return Array.Empty(Of Byte)()
                End If

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
        '''     Erkennt den Dateityp anhand eines Dateipfads.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit Endungsprüfung deaktiviert.
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
        '''     Entscheidungspfad:
        '''     1) Header-/Registry-Erkennung (SSOT),
        '''     2) Archiv-Gate und optionales OOXML-Refinement,
        '''     3) optionale Endungs-Policy.
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu klassifizierenden Datei.</param>
        ''' <param name="verifyExtension"><c>True</c> erzwingt die fail-closed Endungsprüfung nach Inhaltsdetektion.</param>
        ''' <returns>Erkannter Typ oder <see cref="FileKind.Unknown"/> bei Mismatch oder Fehlern.</returns>
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
        '''     Bei Endungs-Mismatch wird fail-closed auf <see cref="FileKind.Unknown"/> gesetzt und der Reason-Code angepasst.
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu klassifizierenden Datei.</param>
        ''' <param name="verifyExtension"><c>True</c> aktiviert die Endungsprüfung nach Inhaltsdetektion.</param>
        ''' <returns>Detailliertes Detektionsergebnis mit typisiertem Trace-Kontext.</returns>
        ' ReSharper disable once MemberCanBeMadeStatic.Global
        <SuppressMessage("Performance", "CA1822:Mark members as static", Justification:="Public instance API; changing to Shared would be a breaking API change.")>
        Public Function DetectDetailed _
            (
                path As String,
                verifyExtension As Boolean
            ) As DetectionDetail

            Dim opt = GetDefaultOptions()
            Dim trace As DetectionTrace = DetectionTrace.Empty

            Dim detected As FileType = DetectPathCoreWithTrace(path, opt, trace)
            Dim extensionOk = True
            If verifyExtension Then
                extensionOk = ExtensionMatchesKind(path, detected.Kind)
                If Not extensionOk Then
                    detected = UnknownType()
                    trace.ReasonCode = ReasonExtensionMismatch
                End If
            End If

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
        '''     Prüft fail-closed, ob eine Datei einen sicheren Archiv-Container repräsentiert.
        ''' </summary>
        ''' <remarks>
        '''     Die Methode beschreibt den Containertyp und validiert den Archivinhalt gegen Sicherheitsgrenzen.
        ''' </remarks>
        ''' <param name="path">Dateipfad der zu validierenden Archivdatei.</param>
        ''' <returns><c>True</c>, wenn der Container valide und sicher ist; andernfalls <c>False</c>.</returns>
        Public Shared Function TryValidateArchive _
            (
                path As String
            ) As Boolean

            Dim opt = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return False

            Try
                Using _
                    fs As _
                        New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Dim descriptor As ArchiveDescriptor = ArchiveDescriptor.UnknownDescriptor()
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
                TypeOf ex Is ArgumentException
                Return False
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Shared Function DetectPathCore(path As String) As FileType
            Dim opt = GetDefaultOptions()
            Dim trace As DetectionTrace = DetectionTrace.Empty
            Return DetectPathCoreWithTrace(path, opt, trace)
        End Function

        Private Shared Function DetectPathCoreWithTrace(path As String, opt As FileTypeProjectOptions,
                                                        ByRef trace As DetectionTrace) As FileType
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                trace.ReasonCode = ReasonFileNotFound
                Return UnknownType()
            End If

            Try
                Dim fi As New FileInfo(path)
                If fi.Length < 0 Then
                    trace.ReasonCode = ReasonInvalidLength
                    Return UnknownType()
                End If
                If fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu gross ({fi.Length} > {opt.MaxBytes}).")
                    trace.ReasonCode = ReasonFileTooLarge
                    Return UnknownType()
                End If

                Using _
                    fs As _
                        New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Dim header = ReadHeader(fs, opt.SniffBytes, opt.MaxBytes)
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
        ' ReSharper disable once MemberCanBeMadeStatic.Global
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
        '''     Ergebnis basiert vollständig auf der inhaltsbasierten Detektion.
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
        '''     Persistenz erfolgt über <see cref="FileMaterializer"/> mit aktivem Sicherheitsmodus.
        ''' </remarks>
        ''' <param name="path">Pfad zur Archivdatei.</param>
        ''' <param name="destinationDirectory">Leeres, noch nicht existierendes Zielverzeichnis.</param>
        ''' <param name="verifyBeforeExtract"><c>True</c> aktiviert eine vorgelagerte Typprüfung über <c>Detect(path)</c>.</param>
        ''' <returns><c>True</c> bei erfolgreichem, atomarem Entpacken; sonst <c>False</c>.</returns>
        ''' <exception cref="UnauthorizedAccessException">Kann bei Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="Security.SecurityException">Kann bei sicherheitsrelevantem Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="IOException">Kann bei I/O-Zugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="InvalidDataException">Kann bei ungültigen Archivdaten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="NotSupportedException">Kann bei nicht unterstützten Pfadformaten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="ArgumentException">Kann bei ungültigen Argumentzuständen intern auftreten und wird fail-closed behandelt.</exception>
        Public Function ExtractArchiveSafe _
            (
                path As String,
                destinationDirectory As String,
                verifyBeforeExtract As Boolean
            ) As Boolean
            Dim opt = GetDefaultOptions()
            If Not CanExtractArchivePath(path, verifyBeforeExtract, opt) Then Return False

            Try
                Dim payload = ReadFileSafe(path)
                If payload.Length = 0 Then Return False
                Return _
                    FileMaterializer.Persist(payload, destinationDirectory, overwrite:=False, secureExtract:=True)
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
        '''     Die Methode liefert keine Dateisystem-Nebenwirkungen und gibt fail-closed eine leere Liste zurück.
        ''' </remarks>
        ''' <param name="path">Pfad zur Archivdatei.</param>
        ''' <param name="verifyBeforeExtract"><c>True</c> aktiviert eine vorgelagerte Typprüfung über <c>Detect(path)</c>.</param>
        ''' <returns>Read-only Liste extrahierter Einträge oder leer bei Fehler.</returns>
        ''' <exception cref="UnauthorizedAccessException">Kann bei Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="Security.SecurityException">Kann bei sicherheitsrelevantem Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="IOException">Kann bei I/O-Zugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="InvalidDataException">Kann bei ungültigen Archivdaten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="NotSupportedException">Kann bei nicht unterstützten Pfadformaten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="ArgumentException">Kann bei ungültigen Argumentzuständen intern auftreten und wird fail-closed behandelt.</exception>
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
                Using _
                    fs As _
                        New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
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

        Private Shared Function DetectInternalBytes(data As Byte(), opt As FileTypeProjectOptions) As FileType
            If data Is Nothing OrElse data.Length = 0 Then Return UnknownType()
            If CLng(data.Length) > opt.MaxBytes Then
                LogGuard.Warn(opt.Logger, $"[Detect] Daten zu gross ({data.Length} > {opt.MaxBytes}).")
                Return UnknownType()
            End If

            Try
                Dim trace As DetectionTrace = DetectionTrace.Empty
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
                                 Dim descriptor As ArchiveDescriptor = ArchiveDescriptor.UnknownDescriptor()
                                 If Not ArchiveTypeResolver.TryDescribeStream(fs, opt, descriptor) Then Return Nothing
                                 Return descriptor
                             End Function,
                tryValidate:=Function(descriptor)
                                 Return ValidateArchiveStreamRaw(fs, opt, descriptor)
                             End Function,
                tryRefine:=Function()
                               Return OpenXmlRefiner.TryRefineStream(fs)
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
                                 Dim descriptor As ArchiveDescriptor = ArchiveDescriptor.UnknownDescriptor()
                                 If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return Nothing
                                 Return descriptor
                             End Function,
                tryValidate:=Function(descriptor)
                                 Return ValidateArchiveBytesRaw(data, opt, descriptor)
                             End Function,
                tryRefine:=Function()
                               Using ms = CreateReadOnlyMemoryStream(data)
                                   Return OpenXmlRefiner.TryRefineStream(ms)
                               End Using
                           End Function)
        End Function

        Private Shared Function ResolveByHeaderCommon(
                                               header As Byte(),
                                               opt As FileTypeProjectOptions,
                                               ByRef trace As DetectionTrace,
                                               tryDescribe As Func(Of ArchiveDescriptor),
                                               tryValidate As Func(Of ArchiveDescriptor, Boolean),
                                               tryRefine As Func(Of FileType)
                                               ) As FileType
            If header Is Nothing OrElse header.Length = 0 Then
                trace.ReasonCode = ReasonHeaderUnknown
                Return UnknownType()
            End If

            Dim magicKind = FileTypeRegistry.DetectByMagic(header)
            If magicKind <> FileKind.Unknown AndAlso magicKind <> FileKind.Zip Then
                trace.ReasonCode = ReasonHeaderMatch
                Return FileTypeRegistry.Resolve(magicKind)
            End If

            Dim descriptor As ArchiveDescriptor
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
            If magicKind <> FileKind.Zip Then
                trace.ReasonCode = ReasonArchiveGeneric
                Return FileTypeRegistry.Resolve(FileKind.Zip)
            End If

            Dim refined = tryRefine()
            Return FinalizeArchiveDetection(refined, opt, trace)
        End Function

        Private Shared Function FinalizeArchiveDetection(refined As FileType, opt As FileTypeProjectOptions,
                                                  ByRef trace As DetectionTrace) As FileType
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

        Private Function CanExtractArchivePath(path As String, verifyBeforeExtract As Boolean,
                                               opt As FileTypeProjectOptions) As Boolean
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[ArchiveExtract] Quelldatei fehlt.")
                Return False
            End If

            If verifyBeforeExtract Then
                Dim detected = Detect(path)
                If Not IsArchiveContainerKind(detected.Kind) Then
                    LogGuard.Warn(opt.Logger, $"[ArchiveExtract] Vorpruefung fehlgeschlagen ({detected.Kind}).")
                    Return False
                End If
            End If

            Return True
        End Function

        Private Shared Function ApplyExtensionPolicy(path As String, detected As FileType, verifyExtension As Boolean) _
            As FileType
            If Not verifyExtension Then Return detected
            If ExtensionMatchesKind(path, detected.Kind) Then Return detected
            Return UnknownType()
        End Function

        Private Shared Function IsArchiveContainerKind(kind As FileKind) As Boolean
            Return kind = FileKind.Zip OrElse
                   kind = FileKind.Docx OrElse
                   kind = FileKind.Xlsx OrElse
                   kind = FileKind.Pptx
        End Function

        Private Shared Sub WarnIfNoDirectContentDetection(kind As FileKind, opt As FileTypeProjectOptions)
            If kind = FileKind.Unknown Then Return
            If FileTypeRegistry.HasDirectContentDetection(kind) Then Return
            LogGuard.Warn(opt.Logger,
                          $"[Detect] Keine direkte Content-Erkennung fuer Typ '{kind _
                             }'. Ergebnis stammt aus Fallback/Refinement.")
        End Sub

        Private Shared Function ExtensionMatchesKind(path As String, detectedKind As FileKind) As Boolean
            Dim ext = IO.Path.GetExtension(If(path, String.Empty))
            If String.IsNullOrWhiteSpace(ext) Then Return True

            If detectedKind = FileKind.Unknown Then Return False

            Dim normalizedExt = FileTypeRegistry.NormalizeAlias(ext)
            Dim detectedType = FileTypeRegistry.Resolve(detectedKind)

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
            Try
                If input Is Nothing OrElse Not input.CanRead Then Return Array.Empty(Of Byte)()
                If maxBytes <= 0 Then Return Array.Empty(Of Byte)()
                If input.CanSeek Then
                    If input.Length <= 0 OrElse input.Length > maxBytes Then Return Array.Empty(Of Byte)()
                    input.Position = 0
                End If

                Dim want As Integer = sniffBytes
                If want <= 0 Then want = InternalIoDefaults.DefaultSniffBytes
                Dim take As Integer = want
                If input.CanSeek Then
                    take = CInt(Math.Min(input.Length, want))
                End If
                If take <= 0 Then Return Array.Empty(Of Byte)()

                Dim buf(take - 1) As Byte
                Dim off = 0
                While off < take
                    Dim n = input.Read(buf, off, take - off)
                    If n <= 0 Then Exit While
                    off += n
                End While

                If off <= 0 Then Return Array.Empty(Of Byte)()
                If off < take Then
                    Dim exact(off - 1) As Byte
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
                TypeOf ex Is ArgumentException
                Return Array.Empty(Of Byte)()
            Catch ex As Exception
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
