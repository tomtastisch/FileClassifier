' ============================================================================
' FILE: DestinationPathGuard.vb
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
    '''     Gemeinsame Zielpfad-Policy für Materialisierung und Archiv-Extraktion.
    ''' </summary>
    Friend Interface IDestinationPathPolicy
        ''' <summary>
        '''     Bereitet ein Materialisierungsziel vor und beachtet Overwrite-Policy.
        ''' </summary>
        Function PrepareMaterializationTarget _
            (
                destinationFull As String,
                overwrite As Boolean,
                opt As FileTypeProjectOptions
            ) As Boolean

        ''' <summary>
        '''     Prüft, ob ein neues Extraktionsziel erstellt werden darf.
        ''' </summary>
        Function ValidateNewExtractionTarget _
            (
                destinationFull As String,
                opt As FileTypeProjectOptions
            ) As Boolean

        ''' <summary>
        '''     Prüft, ob ein Zielpfad auf ein Root-Verzeichnis zeigt.
        ''' </summary>
        Function IsRootPath _
            (
                destinationFull As String
            ) As Boolean
    End Interface

    ''' <summary>
    '''     Standardimplementierung der internen Zielpfad-Policy.
    ''' </summary>
    Friend NotInheritable Class DefaultDestinationPathPolicy
        Implements IDestinationPathPolicy

        Friend Shared ReadOnly Instance As IDestinationPathPolicy = _
            New DefaultDestinationPathPolicy()

        ''' <summary>
        '''     Verhindert direkte Instanziierung; Zugriff ausschließlich über <see cref="Instance"/>.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Bereitet ein Materialisierungsziel vor (Root-Schutz, Overwrite-Handling).
        ''' </summary>
        ''' <param name="destinationFull">Vollqualifizierter Zielpfad.</param>
        ''' <param name="overwrite">Steuert, ob bestehende Ziele gelöscht werden dürfen.</param>
        ''' <param name="opt">Laufzeitoptionen inklusive Logger.</param>
        Public Function PrepareMaterializationTarget _
            (
                destinationFull As String,
                overwrite As Boolean,
                opt As FileTypeProjectOptions
            ) As Boolean Implements IDestinationPathPolicy.PrepareMaterializationTarget

            If IsRootPath(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel darf kein Root-Verzeichnis sein.")
                Return False
            End If

            If Not TryDeleteExistingTarget(destinationFull, overwrite) Then Return False

            Return True
        End Function

        ''' <summary>
        '''     Validiert ein neues Extraktionsziel ohne bestehende Kollisionen.
        ''' </summary>
        ''' <param name="destinationFull">Vollqualifizierter Zielpfad.</param>
        ''' <param name="opt">Laufzeitoptionen inklusive Logger.</param>
        Public Function ValidateNewExtractionTarget _
            (
                destinationFull As String,
                opt As FileTypeProjectOptions
            ) As Boolean Implements IDestinationPathPolicy.ValidateNewExtractionTarget

            Dim parent As String

            If IsRootPath(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel darf kein Root-Verzeichnis sein.")
                Return False
            End If

            If File.Exists(destinationFull) OrElse Directory.Exists(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel existiert bereits.")
                Return False
            End If

            parent = Path.GetDirectoryName(destinationFull)
            If String.IsNullOrWhiteSpace(parent) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel ohne gültigen Parent.")
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        '''     Ermittelt, ob der Zielpfad auf ein Root-Verzeichnis zeigt.
        ''' </summary>
        ''' <param name="destinationFull">Zu prüfender Zielpfad.</param>
        Public Function IsRootPath _
            (
                destinationFull As String
            ) As Boolean Implements IDestinationPathPolicy.IsRootPath

            Dim rootPath         As String
            Dim isRootFromCsCore As Boolean = False

            If String.IsNullOrWhiteSpace(destinationFull) Then Return False

            If CsCoreRuntimeBridge.TryIsRootPath(destinationFull, isRootFromCsCore) Then
                Return isRootFromCsCore
            End If

            Try
                rootPath = Path.GetPathRoot(destinationFull)
            Catch ex As Exception When ExceptionFilterGuard.IsPathNormalizationException(ex)
                Return False
            End Try

            If String.IsNullOrWhiteSpace(rootPath) Then Return False

            Return String.Equals(
                destinationFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        '''     Löscht ein bestehendes Ziel abhängig von der Overwrite-Einstellung.
        ''' </summary>
        ''' <param name="destinationFull">Vollqualifizierter Zielpfad.</param>
        ''' <param name="overwrite">Steuert, ob Löschung erlaubt ist.</param>
        Private Shared Function TryDeleteExistingTarget _
            (
                destinationFull As String,
                overwrite As Boolean
            ) As Boolean

            Dim existsAsFile      As Boolean
            Dim existsAsDirectory As Boolean

            existsAsFile = File.Exists(destinationFull)
            existsAsDirectory = Directory.Exists(destinationFull)

            If Not existsAsFile AndAlso Not existsAsDirectory Then Return True
            If Not overwrite Then Return False

            If existsAsFile Then
                File.Delete(destinationFull)
                Return True
            End If

            Directory.Delete(destinationFull, recursive:=True)
            Return True
        End Function
    End Class

    Friend NotInheritable Class DestinationPathGuard
        Private Shared ReadOnly Policy As IDestinationPathPolicy = _
            DefaultDestinationPathPolicy.Instance

        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Delegiert die Materialisierungsziel-Validierung an die aktive Policy.
        ''' </summary>
        Friend Shared Function PrepareMaterializationTarget _
            (
                destinationFull As String,
                overwrite As Boolean,
                opt As FileTypeProjectOptions
            ) As Boolean

            Return Policy.PrepareMaterializationTarget(destinationFull, overwrite, opt)
        End Function

        ''' <summary>
        '''     Delegiert die Extraktionsziel-Validierung an die aktive Policy.
        ''' </summary>
        Friend Shared Function ValidateNewExtractionTarget _
            (
                destinationFull As String,
                opt As FileTypeProjectOptions
            ) _
            As Boolean

            Return Policy.ValidateNewExtractionTarget(destinationFull, opt)
        End Function

        ''' <summary>
        '''     Delegiert die Root-Pfad-Prüfung an die aktive Policy.
        ''' </summary>
        Friend Shared Function IsRootPath _
            (
                destinationFull As String
            ) As Boolean

            Return Policy.IsRootPath(destinationFull)
        End Function
    End Class

End Namespace
