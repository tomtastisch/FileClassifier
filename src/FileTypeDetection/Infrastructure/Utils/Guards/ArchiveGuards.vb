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
        Private Sub New()
        End Sub

        Friend Shared Function HasContent(data As Byte()) As Boolean

            Return data IsNot Nothing AndAlso data.Length > 0
        End Function
    End Class

    ''' <summary>
    '''     Sicherheits-Gate für Archive-Container.
    ''' </summary>
    Friend NotInheritable Class ArchiveSafetyGate
        Private Sub New()
        End Sub

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
        Private Sub New()
        End Sub

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
        Private Sub New()
        End Sub

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
        Private Sub New()
        End Sub

        Friend Shared Function IsSafeArchivePayload _
            (
                data As Byte(),
                opt As FileTypeProjectOptions
            ) As Boolean

            Dim descriptor As ArchiveDescriptor = ArchiveDescriptor.UnknownDescriptor()

            Return TryDescribeSafeArchivePayload(data, opt, descriptor)
        End Function

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
        Private Sub New()
        End Sub

        Friend Shared Function TryNormalizeRelativePath _
            (
                rawPath As String,
                allowDirectoryMarker As Boolean,
                ByRef normalizedPath As String,
                ByRef isDirectory As Boolean
            ) As Boolean

            Dim safe As String
            Dim trimmed As String
            Dim segments As String()

            normalizedPath = String.Empty
            isDirectory = False

            safe = If(rawPath, String.Empty).Trim()
            If safe.Length = 0 Then Return False
            If safe.Contains(ChrW(0)) Then Return False

            safe = safe.Replace("\"c, "/"c)
            If Path.IsPathRooted(safe) Then Return False
            safe = safe.TrimStart("/"c)
            If safe.Length = 0 Then Return False

            trimmed = safe.TrimEnd("/"c)
            If trimmed.Length = 0 Then
                If Not allowDirectoryMarker Then Return False
                normalizedPath = safe
                isDirectory = True
                Return True
            End If

            segments = trimmed.Split("/"c)
            For Each seg In segments
                If seg.Length = 0 Then Return False
                If seg = "." OrElse seg = ".." Then Return False
            Next

            If safe.Length <> trimmed.Length AndAlso Not allowDirectoryMarker Then
                Return False
            End If

            normalizedPath = If(allowDirectoryMarker, safe, trimmed)
            isDirectory = allowDirectoryMarker AndAlso safe.Length <> trimmed.Length
            Return True
        End Function
    End Class

End Namespace
