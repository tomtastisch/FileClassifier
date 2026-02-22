' ============================================================================
' FILE: ArchiveGuards.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports Tomtastisch.FileClassifier

Namespace Global.Tomtastisch.FileClassifier.Infrastructure.Utils

    ''' <summary>
    '''     Zentrale Byte-Array-Guards für konsistente Null-/Leer-Prüfungen.
    ''' </summary>
    Friend NotInheritable Class ByteArrayGuard
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Prüft, ob ein Byte-Array gesetzt ist und mindestens ein Byte enthält.
        ''' </summary>
        ''' <param name="data">Zu prüfende Bytefolge.</param>
        ''' <returns><c>True</c>, wenn die Bytefolge nicht leer ist.</returns>
        Friend Shared Function HasContent(data As Byte()) As Boolean

            Return data IsNot Nothing AndAlso data.Length > 0
        End Function
    End Class

    ''' <summary>
    '''     Sicherheits-Gate für Archive-Container.
    ''' </summary>
    Friend NotInheritable Class ArchiveSafetyGate
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Validiert einen Archivpayload auf Bytebasis gegen Größen- und Strukturregeln.
        ''' </summary>
        ''' <param name="data">Zu prüfender Archivpayload.</param>
        ''' <param name="opt">Laufzeitoptionen inklusive Sicherheitsgrenzen.</param>
        ''' <param name="descriptor">Vorab ermittelter Archivdeskriptor.</param>
        ''' <returns><c>True</c>, wenn der Payload die Sicherheitsprüfungen besteht.</returns>
        Friend Shared Function IsArchiveSafeBytes _
            (
                data As Byte(),
                opt As FileTypeProjectOptions,
                descriptor As ArchiveDescriptor
            ) As Boolean

            If Not ByteArrayGuard.HasContent(data) Then Return False
            If opt Is Nothing Then Return False
            If descriptor Is Nothing OrElse
                descriptor.ContainerType = ArchiveContainerType.Unknown Then Return False

            Try
                Using ms As New MemoryStream(data, writable:=False)
                    Return IsArchiveSafeStream(ms, opt, descriptor, depth:=0)
                End Using
            Catch ex As Exception When ExceptionFilterGuard.IsArchiveValidationException(ex)
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Bytes-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        '''     Validiert einen Archivstream gegen Größen-, Tiefen- und Strukturregeln.
        ''' </summary>
        ''' <param name="stream">Zu prüfender Stream.</param>
        ''' <param name="opt">Laufzeitoptionen inklusive Sicherheitsgrenzen.</param>
        ''' <param name="descriptor">Vorab ermittelter Archivdeskriptor.</param>
        ''' <param name="depth">Aktuelle Verschachtelungstiefe.</param>
        ''' <returns><c>True</c>, wenn der Stream die Sicherheitsprüfungen besteht.</returns>
        Friend Shared Function IsArchiveSafeStream _
            (
                stream As Stream,
                opt As FileTypeProjectOptions,
                descriptor As ArchiveDescriptor,
                depth As Integer
            ) As Boolean

            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            Return ArchiveProcessingEngine.ValidateArchiveStream(stream, opt, depth, descriptor)
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Guards für signaturbasierte Archiv-Byte-Payloads.
    ''' </summary>
    Friend NotInheritable Class ArchiveSignaturePayloadGuard
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Prüft, ob ein Payload anhand der Magic-Erkennung als Archivkandidat gilt.
        ''' </summary>
        ''' <param name="data">Zu prüfender Payload.</param>
        ''' <returns><c>True</c> bei positivem Signaturkandidaten.</returns>
        Friend Shared Function IsArchiveSignatureCandidate _
            (
                data As Byte()
            ) As Boolean

            If Not ByteArrayGuard.HasContent(data) Then Return False
            Return FileTypeRegistry.DetectByMagic(data) = FileKind.Zip
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Policy-Prüfung für Link-Entries in Archiven.
    ''' </summary>
    Friend NotInheritable Class ArchiveLinkGuard
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Prüft, ob ein Link-Entry gemäß Policy verworfen werden muss.
        ''' </summary>
        ''' <param name="opt">Laufzeitoptionen inklusive Link-Policy.</param>
        ''' <param name="linkTarget">Linkziel aus dem Archiveintrag.</param>
        ''' <param name="logPrefix">Präfix für Protokollmeldungen.</param>
        ''' <param name="logWhenRejected">Steuert, ob bei Verwerfung geloggt wird.</param>
        ''' <returns><c>True</c>, wenn der Eintrag verworfen werden muss.</returns>
        Friend Shared Function IsRejectedLink _
            (
                opt As FileTypeProjectOptions,
                linkTarget As String,
                logPrefix As String,
                logWhenRejected As Boolean
            ) As Boolean

            If opt Is Nothing Then Return True

            If opt.RejectArchiveLinks AndAlso Not String.IsNullOrWhiteSpace(linkTarget) Then
                If logWhenRejected Then
                    LogGuard.Warn(opt.Logger, $"{logPrefix} Link-Entry ist nicht erlaubt.")
                End If

                Return True
            End If

            Return False
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Guards für beliebige Archive-Byte-Payloads.
    ''' </summary>
    Friend NotInheritable Class ArchivePayloadGuard
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Prüft einen Byte-Payload als vollständigen „sicheren Archivpayload“.
        ''' </summary>
        ''' <param name="data">Zu prüfender Payload.</param>
        ''' <param name="opt">Laufzeitoptionen inklusive Sicherheitsgrenzen.</param>
        ''' <returns><c>True</c>, wenn Beschreibung und Sicherheitsprüfung erfolgreich sind.</returns>
        Friend Shared Function IsSafeArchivePayload _
            (
                data As Byte(),
                opt As FileTypeProjectOptions
            ) As Boolean

            Dim descriptor As ArchiveDescriptor = ArchiveDescriptor.UnknownDescriptor()

            Return TryDescribeSafeArchivePayload(data, opt, descriptor)
        End Function

        ''' <summary>
        '''     Beschreibt und validiert einen Archive-Payload in einem Schritt.
        ''' </summary>
        ''' <param name="data">Zu prüfender Payload.</param>
        ''' <param name="opt">Laufzeitoptionen inklusive Sicherheitsgrenzen.</param>
        ''' <param name="descriptor">Ausgabeparameter für den ermittelten Deskriptor.</param>
        ''' <returns><c>True</c>, wenn Deskriptor und Sicherheitsprüfung erfolgreich sind.</returns>
        Friend Shared Function TryDescribeSafeArchivePayload _
            (
                data As Byte(),
                opt As FileTypeProjectOptions,
                ByRef descriptor As ArchiveDescriptor
            ) As Boolean

            descriptor = ArchiveDescriptor.UnknownDescriptor()

            If Not ByteArrayGuard.HasContent(data) Then Return False
            If opt Is Nothing Then Return False
            If CLng(data.Length) > opt.MaxBytes Then Return False

            If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return False
            Return ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor)
        End Function

    End Class

    ''' <summary>
    '''     Gemeinsame Normalisierung für relative Archiv-Entry-Pfade.
    ''' </summary>
    Friend NotInheritable Class ArchiveEntryPathPolicy
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Normalisiert und validiert relative Archivpfade deterministisch.
        ''' </summary>
        ''' <param name="rawPath">Rohpfad aus dem Archiveintrag.</param>
        ''' <param name="allowDirectoryMarker">Erlaubt abschließenden Verzeichnismarker.</param>
        ''' <param name="normalizedPath">Ausgabeparameter für den normalisierten Pfad.</param>
        ''' <param name="isDirectory">Ausgabeparameter für Verzeichniskennzeichnung.</param>
        ''' <returns><c>True</c>, wenn der Pfad sicher und gültig ist.</returns>
        Friend Shared Function TryNormalizeRelativePath _
            (
                rawPath As String,
                allowDirectoryMarker As Boolean,
                ByRef normalizedPath As String,
                ByRef isDirectory As Boolean
            ) As Boolean

            Dim safe                  As String  = String.Empty
            Dim trimmed               As String
            Dim isValidFromCsCore     As Boolean = False
            Dim normalizedFromCsCore  As String  = String.Empty
            Dim isDirectoryFromCsCore As Boolean = False

            normalizedPath = String.Empty
            isDirectory = False

            If CsCoreRuntimeBridge.TryNormalizeArchiveRelativePath(
                    rawPath:=rawPath,
                    allowDirectoryMarker:=allowDirectoryMarker,
                    isValid:=isValidFromCsCore,
                    normalizedPath:=normalizedFromCsCore,
                    isDirectory:=isDirectoryFromCsCore
                ) Then
                normalizedPath = normalizedFromCsCore
                isDirectory = isDirectoryFromCsCore
                Return isValidFromCsCore
            End If

            If Not TryPrepareRelativePath(rawPath, safe) Then Return False

            trimmed = safe.TrimEnd("/"c)
            If trimmed.Length = 0 Then
                If Not allowDirectoryMarker Then Return False
                normalizedPath = safe
                isDirectory = True
                Return True
            End If

            If Not HasOnlyAllowedPathSegments(trimmed) Then Return False

            If safe.Length <> trimmed.Length AndAlso Not allowDirectoryMarker Then
                Return False
            End If

            normalizedPath = If(allowDirectoryMarker, safe, trimmed)
            isDirectory = allowDirectoryMarker AndAlso safe.Length <> trimmed.Length
            Return True
        End Function

        ''' <summary>
        '''     Führt Vorprüfung und Grundnormalisierung eines relativen Archivpfades aus.
        ''' </summary>
        ''' <param name="rawPath">Rohpfad aus dem Archiveintrag.</param>
        ''' <param name="preparedPath">Ausgabeparameter für den vorbereiteten Pfad.</param>
        ''' <returns><c>True</c>, wenn der Pfad grundsätzlich verwendbar ist.</returns>
        Private Shared Function TryPrepareRelativePath _
            (
                rawPath As String,
                ByRef preparedPath As String
            ) As Boolean

            preparedPath = If(rawPath, String.Empty).Trim()
            If preparedPath.Length = 0 Then Return False
            If preparedPath.Contains(ChrW(0)) Then Return False
            If Path.IsPathRooted(preparedPath) Then Return False

            preparedPath = preparedPath.Replace("\"c, "/"c).TrimStart("/"c)
            If preparedPath.Length = 0 Then Return False

            Return True
        End Function

        ''' <summary>
        '''     Prüft, ob alle Pfadsegmente zulässig sind (keine leeren Segmente, kein `.`/`..`).
        ''' </summary>
        ''' <param name="pathValue">Zu prüfender relativer Pfad.</param>
        Private Shared Function HasOnlyAllowedPathSegments(pathValue As String) As Boolean

            Dim segments As String()

            segments = pathValue.Split("/"c)
            For Each seg In segments
                If seg.Length = 0 Then Return False
                If seg = "." OrElse seg = ".." Then Return False
            Next

            Return True
        End Function
    End Class

End Namespace
