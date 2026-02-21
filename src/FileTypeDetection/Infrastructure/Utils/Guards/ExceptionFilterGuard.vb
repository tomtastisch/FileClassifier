' ============================================================================
' FILE: ExceptionFilterGuard.vb
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

Namespace Global.Tomtastisch.FileClassifier.Infrastructure.Utils

    ''' <summary>
    '''     SSOT für wiederkehrende Exception-Filter in Guard-Klassen.
    ''' </summary>
    ''' <remarks>
    '''     Diese Utility kapselt Catch-Filter-Sets deterministisch, um
    '''     Duplikate zu vermeiden und die Filter-Semantik zentral auditierbar zu halten.
    ''' </remarks>
    Friend NotInheritable Class ExceptionFilterGuard
        Private Sub New()
        End Sub

        Friend Shared Function IsArchiveValidationException(ex As Exception) As Boolean

            Return TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
        End Function

        Friend Shared Function IsPathNormalizationException(ex As Exception) As Boolean

            Return TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
        End Function

        Friend Shared Function IsPathResolutionException(ex As Exception) As Boolean

            Return IsPathNormalizationException(ex) OrElse
                TypeOf ex Is PathTooLongException
        End Function

        Friend Shared Function IsLoggerWriteException(ex As Exception) As Boolean

            Return TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException OrElse
                TypeOf ex Is FormatException OrElse
                TypeOf ex Is ArgumentException
        End Function
    End Class

End Namespace
