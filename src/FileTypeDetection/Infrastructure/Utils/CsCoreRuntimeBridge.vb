' ============================================================================
' FILE: CsCoreRuntimeBridge.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
'
' Kontext:
' - Runtime-Bruecke fuer optionale Delegation auf C#-CSCore Utilities.
' - Fail-closed: Falls CSCore nicht aufloesbar ist, bleibt VB-Fallback aktiv.
' ============================================================================

Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Runtime.ExceptionServices

Namespace Global.Tomtastisch.FileClassifier.Infrastructure.Utils

    ''' <summary>
    '''     <b>Zweck:</b><br/>
    '''     Runtime-Bruecke zwischen VB-Core und optionalem CSCore-Utility-Layer.
    ''' </summary>
    ''' <remarks>
    '''     <b>Fail-Closed:</b><br/>
    '''     - Ist die CSCore-Assembly oder ein erwarteter Typ/Member nicht verfuegbar,
    '''       liefert die Bruecke deterministisch <c>False</c> und der VB-Fallback bleibt aktiv.
    ''' </remarks>
    <System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>
    Friend NotInheritable Class CsCoreRuntimeBridge

        Private Const CsCoreAssemblySimpleName As String = "FileClassifier.CSCore"
        Private Const CsCoreAssemblyFileName As String = "FileClassifier.CSCore.dll"
        Private Const EnumUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.EnumUtility"
        Private Const IterableUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.IterableUtility"
        Private Const GuardUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.GuardUtility"
        Private Const ExceptionFilterUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.ExceptionFilterUtility"
        Private Const HashNormalizationUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.HashNormalizationUtility"
        Private Const MaterializationUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.MaterializationUtility"
        Private Const ProjectOptionsUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.ProjectOptionsUtility"
        Private Const DetectionPolicyUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.DetectionPolicyUtility"
        Private Const OfficePolicyUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.OfficePolicyUtility"
        Private Const EvidencePolicyUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.EvidencePolicyUtility"
        Private Const ArchivePathPolicyUtilityTypeName As String = "Tomtastisch.FileClassifier.CSCore.Utilities.ArchivePathPolicyUtility"

        Private Const OperationGetEnumValues As String = "GetEnumValues"
        Private Const OperationCloneArray As String = "CloneArray"
        Private Const OperationNotNull As String = "NotNull"
        Private Const OperationRequireLength As String = "RequireLength"
        Private Const OperationRequireEnumDefined As String = "RequireEnumDefined"
        Private Const OperationArchiveValidationFilter As String = "ArchiveValidationFilter"
        Private Const OperationPathNormalizationFilter As String = "PathNormalizationFilter"
        Private Const OperationPathResolutionFilter As String = "PathResolutionFilter"
        Private Const OperationLoggerWriteFilter As String = "LoggerWriteFilter"
        Private Const OperationNormalizeDigest As String = "NormalizeDigest"
        Private Const OperationCreateEmptyDigestParts As String = "CreateEmptyDigestParts"
        Private Const OperationCoalesceMaterializedFileName As String = "CoalesceMaterializedFileName"
        Private Const OperationNormalizeMaterializedFileName As String = "NormalizeMaterializedFileName"
        Private Const OperationIsPayloadWithinMaxBytes As String = "IsPayloadWithinMaxBytes"
        Private Const OperationDecideMaterializationMode As String = "DecideMaterializationMode"
        Private Const OperationNormalizeProjectOptionsValues As String = "NormalizeProjectOptionsValues"
        Private Const OperationExceptionToReasonCode As String = "ExceptionToReasonCode"
        Private Const OperationIsExtensionMatch As String = "IsExtensionMatch"
        Private Const OperationBuildSummaryValues As String = "BuildSummaryValues"
        Private Const OperationResolveOpenDocumentMimeKindKey As String = "ResolveOpenDocumentMimeKindKey"
        Private Const OperationResolveArchivePackageKindKey As String = "ResolveArchivePackageKindKey"
        Private Const OperationResolveLegacyMarkerKindKey As String = "ResolveLegacyMarkerKindKey"
        Private Const OperationNormalizeEvidenceLabel As String = "NormalizeEvidenceLabel"
        Private Const OperationAppendNoteIfAny As String = "AppendNoteIfAny"
        Private Const OperationResolveHmacKeyFromEnvironment As String = "ResolveHmacKeyFromEnvironment"
        Private Const OperationNormalizeArchiveRelativePath As String = "NormalizeArchiveRelativePath"
        Private Const OperationIsRootPath As String = "IsRootPath"
        Private Const ExpectedSummaryValuesCount As Integer = 4
        Private Const ExpectedHmacResolutionValuesCount As Integer = 3
        Private Const ExpectedArchivePathValuesCount As Integer = 3
        Private Const ExpectedNormalizeProjectOptionsValuesCount As Integer = 14
        Private Const ExpectedEmptyDigestPartCount As Integer = 6

        Private Shared ReadOnly SyncRoot As New Object()
        Private Shared ReadOnly EnumGetValuesClosedMethodCache As New ConcurrentDictionary(Of Type, MethodInfo)()
        Private Shared ReadOnly CloneArrayClosedMethodCache As New ConcurrentDictionary(Of Type, MethodInfo)()

        Private Shared ReadOnly DelegatedCounterByOperation As New ConcurrentDictionary(Of String, Long)()
        Private Shared ReadOnly FallbackCounterByOperation As New ConcurrentDictionary(Of String, Long)()

        Private Shared _isInitialized As Boolean
        Private Shared _isAvailable As Boolean

        Private Shared EnumGetValuesMethodDefinition As MethodInfo
        Private Shared CloneArrayMethodDefinition As MethodInfo
        Private Shared GuardNotNullMethod As MethodInfo
        Private Shared GuardRequireLengthMethod As MethodInfo
        Private Shared GuardRequireEnumDefinedMethod As MethodInfo
        Private Shared IsArchiveValidationExceptionMethod As MethodInfo
        Private Shared IsPathNormalizationExceptionMethod As MethodInfo
        Private Shared IsPathResolutionExceptionMethod As MethodInfo
        Private Shared IsLoggerWriteExceptionMethod As MethodInfo
        Private Shared NormalizeDigestMethod As MethodInfo
        Private Shared CreateEmptyDigestPartsMethod As MethodInfo
        Private Shared CoalesceMaterializedFileNameMethod As MethodInfo
        Private Shared NormalizeMaterializedFileNameMethod As MethodInfo
        Private Shared IsPayloadWithinMaxBytesMethod As MethodInfo
        Private Shared DecideMaterializationModeMethod As MethodInfo
        Private Shared NormalizeProjectOptionsValuesMethod As MethodInfo
        Private Shared ExceptionToReasonCodeMethod As MethodInfo
        Private Shared IsExtensionMatchMethod As MethodInfo
        Private Shared BuildSummaryValuesMethod As MethodInfo
        Private Shared ResolveOpenDocumentMimeKindKeyMethod As MethodInfo
        Private Shared ResolveArchivePackageKindKeyMethod As MethodInfo
        Private Shared ResolveLegacyMarkerKindKeyMethod As MethodInfo
        Private Shared NormalizeEvidenceLabelMethod As MethodInfo
        Private Shared AppendNoteIfAnyMethod As MethodInfo
        Private Shared ResolveHmacKeyFromEnvironmentMethod As MethodInfo
        Private Shared NormalizeArchiveRelativePathMethod As MethodInfo
        Private Shared IsRootPathMethod As MethodInfo

        ''' <summary>
        '''     <b>Hinweis:</b><br/>
        '''     Verhindert Instanziierung; zustandsloses Utility.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     <b>Rueckgabe:</b><br/>
        '''     Laufzeitindikator fuer Unit-Tests und Diagnostik.
        ''' </summary>
        Friend Shared ReadOnly Property IsCsCoreAvailable As Boolean
            Get
                EnsureInitialized()
                Return _isAvailable
            End Get
        End Property

        ''' <summary>
        '''     <b>Aktion:</b><br/>
        '''     Setzt die Telemetry-Zaehler zurueck (nur fuer Tests/Diagnostik).
        ''' </summary>
        Friend Shared Sub ResetTelemetry()

            DelegatedCounterByOperation.Clear()
            FallbackCounterByOperation.Clear()
        End Sub

        ''' <summary>
        '''     <b>Rueckgabe:</b><br/>
        '''     Liefert eine unveraenderliche Telemetry-Sicht fuer Audit/Tests.
        ''' </summary>
        Friend Shared Function GetTelemetrySnapshot() As CsCoreRuntimeBridgeTelemetrySnapshot

            Dim delegatedCopy As New Dictionary(Of String, Long)(StringComparer.Ordinal)
            Dim fallbackCopy  As New Dictionary(Of String, Long)(StringComparer.Ordinal)
            Dim pair          As KeyValuePair(Of String, Long)

            EnsureInitialized()

            For Each pair In DelegatedCounterByOperation
                delegatedCopy(pair.Key) = pair.Value
            Next

            For Each pair In FallbackCounterByOperation
                fallbackCopy(pair.Key) = pair.Value
            Next

            Return New CsCoreRuntimeBridgeTelemetrySnapshot(
                isCsCoreAvailable:=_isAvailable,
                delegatedByOperation:=delegatedCopy,
                fallbackByOperation:=fallbackCopy
            )
        End Function

        Friend Shared Function TryGetEnumValues(Of TEnum As Structure) _
            (
                sortOrder As Integer,
                fromIndex As Nullable(Of Integer),
                toIndex As Nullable(Of Integer),
                ByRef values() As TEnum
            ) As Boolean

            Dim enumType As Type       = GetType(TEnum)
            Dim method   As MethodInfo = Nothing
            Dim result   As Object     = Nothing

            values = Nothing
            If Not TryEnsureCsCoreAvailable(OperationGetEnumValues) Then
                Return False
            End If

            If Not enumType.IsEnum Then
                RecordFallback(OperationGetEnumValues)
                Return False
            End If

            If Not TryGetClosedGenericMethod(
                EnumGetValuesMethodDefinition,
                enumType,
                EnumGetValuesClosedMethodCache,
                method
            ) Then
                RecordFallback(OperationGetEnumValues)
                Return False
            End If

            If Not TryPrepareInvocation(OperationGetEnumValues, method) Then
                Return False
            End If

            If Not TryInvokeForFallback(
                OperationGetEnumValues,
                method,
                New Object() {sortOrder, fromIndex, toIndex},
                result
            ) Then
                Return False
            End If

            If Not TypeOf result Is TEnum() Then
                RecordFallback(OperationGetEnumValues)
                Return False
            End If

            values = CType(result, TEnum())
            Return True
        End Function

        Friend Shared Function TryCloneArray(Of T) _
            (
                source() As T,
                ByRef cloned() As T
            ) As Boolean

            Dim method As MethodInfo = Nothing
            Dim result As Object     = Nothing

            cloned = Nothing
            If Not TryEnsureCsCoreAvailable(OperationCloneArray) Then
                Return False
            End If

            If Not TryGetClosedGenericMethod(
                CloneArrayMethodDefinition,
                GetType(T),
                CloneArrayClosedMethodCache,
                method
            ) Then
                RecordFallback(OperationCloneArray)
                Return False
            End If

            If Not TryPrepareInvocation(OperationCloneArray, method) Then
                Return False
            End If

            If Not TryInvokeForFallback(
                OperationCloneArray,
                method,
                New Object() {source},
                result
            ) Then
                Return False
            End If

            If result Is Nothing Then
                cloned = Nothing
                Return True
            End If

            If Not TypeOf result Is T() Then
                RecordFallback(OperationCloneArray)
                Return False
            End If

            cloned = CType(result, T())
            Return True
        End Function

        Friend Shared Function TryNotNull _
            (
                value As Object,
                paramName As String
            ) As Boolean

            If Not TryPrepareInvocation(OperationNotNull, GuardNotNullMethod) Then
                Return False
            End If

            InvokeWithUnwrappedInnerException(
                GuardNotNullMethod,
                New Object() {value, paramName}
            )

            Return True
        End Function

        Friend Shared Function TryRequireLength _
            (
                value As Array,
                expectedLength As Integer,
                paramName As String
            ) As Boolean

            If Not TryPrepareInvocation(OperationRequireLength, GuardRequireLengthMethod) Then
                Return False
            End If

            InvokeWithUnwrappedInnerException(
                GuardRequireLengthMethod,
                New Object() {value, expectedLength, paramName}
            )

            Return True
        End Function

        Friend Shared Function TryRequireEnumDefined _
            (
                enumType As Type,
                value As Object,
                paramName As String
            ) As Boolean

            If Not TryPrepareInvocation(OperationRequireEnumDefined, GuardRequireEnumDefinedMethod) Then
                Return False
            End If

            InvokeWithUnwrappedInnerException(
                GuardRequireEnumDefinedMethod,
                New Object() {enumType, value, paramName}
            )

            Return True
        End Function

        Friend Shared Function TryIsArchiveValidationException _
            (
                ex As Exception,
                ByRef isMatch As Boolean
            ) As Boolean

            isMatch = False

            If Not TryPrepareInvocation(OperationArchiveValidationFilter, IsArchiveValidationExceptionMethod) Then
                Return False
            End If

            Return TryInvokeBooleanForFallback(
                OperationArchiveValidationFilter,
                IsArchiveValidationExceptionMethod,
                New Object() {ex},
                isMatch
            )
        End Function

        Friend Shared Function TryIsPathNormalizationException _
            (
                ex As Exception,
                ByRef isMatch As Boolean
            ) As Boolean

            isMatch = False

            If Not TryPrepareInvocation(OperationPathNormalizationFilter, IsPathNormalizationExceptionMethod) Then
                Return False
            End If

            Return TryInvokeBooleanForFallback(
                OperationPathNormalizationFilter,
                IsPathNormalizationExceptionMethod,
                New Object() {ex},
                isMatch
            )
        End Function

        Friend Shared Function TryIsPathResolutionException _
            (
                ex As Exception,
                ByRef isMatch As Boolean
            ) As Boolean

            isMatch = False

            If Not TryPrepareInvocation(OperationPathResolutionFilter, IsPathResolutionExceptionMethod) Then
                Return False
            End If

            Return TryInvokeBooleanForFallback(
                OperationPathResolutionFilter,
                IsPathResolutionExceptionMethod,
                New Object() {ex},
                isMatch
            )
        End Function

        Friend Shared Function TryIsLoggerWriteException _
            (
                ex As Exception,
                ByRef isMatch As Boolean
            ) As Boolean

            isMatch = False

            If Not TryPrepareInvocation(OperationLoggerWriteFilter, IsLoggerWriteExceptionMethod) Then
                Return False
            End If

            Return TryInvokeBooleanForFallback(
                OperationLoggerWriteFilter,
                IsLoggerWriteExceptionMethod,
                New Object() {ex},
                isMatch
            )
        End Function

        Friend Shared Function TryNormalizeDigest _
            (
                value As String,
                ByRef normalized As String
            ) As Boolean

            normalized = Nothing
            If Not TryPrepareInvocation(OperationNormalizeDigest, NormalizeDigestMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationNormalizeDigest,
                NormalizeDigestMethod,
                New Object() {value},
                normalized
            )
        End Function

        Friend Shared Function TryGetEmptyDigestParts _
            (
                ByRef parts() As String
            ) As Boolean

            parts = Nothing
            If Not TryPrepareInvocation(OperationCreateEmptyDigestParts, CreateEmptyDigestPartsMethod) Then
                Return False
            End If

            Return TryInvokeStringArrayForFallback(
                OperationCreateEmptyDigestParts,
                CreateEmptyDigestPartsMethod,
                New Object() {},
                ExpectedEmptyDigestPartCount,
                parts
            )
        End Function

        Friend Shared Function TryCoalesceMaterializedFileName _
            (
                value As String,
                ByRef coalesced As String
            ) As Boolean

            coalesced = Nothing
            If Not TryPrepareInvocation(OperationCoalesceMaterializedFileName, CoalesceMaterializedFileNameMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationCoalesceMaterializedFileName,
                CoalesceMaterializedFileNameMethod,
                New Object() {value},
                coalesced
            )
        End Function

        Friend Shared Function TryNormalizeMaterializedFileName _
            (
                candidate As String,
                ByRef normalized As String
            ) As Boolean

            normalized = Nothing
            If Not TryPrepareInvocation(OperationNormalizeMaterializedFileName, NormalizeMaterializedFileNameMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationNormalizeMaterializedFileName,
                NormalizeMaterializedFileNameMethod,
                New Object() {candidate},
                normalized
            )
        End Function

        Friend Shared Function TryIsPayloadWithinMaxBytes _
            (
                payloadLength As Integer,
                maxBytes As Long,
                ByRef isWithinLimit As Boolean
            ) As Boolean

            isWithinLimit = False

            If Not TryPrepareInvocation(OperationIsPayloadWithinMaxBytes, IsPayloadWithinMaxBytesMethod) Then
                Return False
            End If

            Return TryInvokeBooleanForFallback(
                OperationIsPayloadWithinMaxBytes,
                IsPayloadWithinMaxBytesMethod,
                New Object() {payloadLength, maxBytes},
                isWithinLimit
            )
        End Function

        Friend Shared Function TryDecideMaterializationMode _
            (
                secureExtract As Boolean,
                archiveDescribeSucceeded As Boolean,
                archiveSafetyPassed As Boolean,
                archiveSignatureCandidate As Boolean,
                ByRef mode As Integer
            ) As Boolean

            mode = 0

            If Not TryPrepareInvocation(OperationDecideMaterializationMode, DecideMaterializationModeMethod) Then
                Return False
            End If

            Return TryInvokeIntegerForFallback(
                OperationDecideMaterializationMode,
                DecideMaterializationModeMethod,
                New Object() {secureExtract, archiveDescribeSucceeded, archiveSafetyPassed, archiveSignatureCandidate},
                mode
            )
        End Function

        Friend Shared Function TryNormalizeProjectOptionsValues _
            (
                headerOnlyNonZip As Boolean,
                maxBytes As Long,
                sniffBytes As Integer,
                maxZipEntries As Integer,
                maxZipTotalUncompressedBytes As Long,
                maxZipEntryUncompressedBytes As Long,
                maxZipCompressionRatio As Integer,
                maxZipNestingDepth As Integer,
                maxZipNestedBytes As Long,
                rejectArchiveLinks As Boolean,
                allowUnknownArchiveEntrySize As Boolean,
                hashIncludePayloadCopies As Boolean,
                hashIncludeFastHash As Boolean,
                hashIncludeSecureHash As Boolean,
                hashMaterializedFileName As String,
                ByRef normalizedValues() As Object
            ) As Boolean

            normalizedValues = Nothing

            If Not TryPrepareInvocation(OperationNormalizeProjectOptionsValues, NormalizeProjectOptionsValuesMethod) Then
                Return False
            End If

            Return TryInvokeObjectArrayForFallback(
                OperationNormalizeProjectOptionsValues,
                NormalizeProjectOptionsValuesMethod,
                New Object() {
                    headerOnlyNonZip,
                    maxBytes,
                    sniffBytes,
                    maxZipEntries,
                    maxZipTotalUncompressedBytes,
                    maxZipEntryUncompressedBytes,
                    maxZipCompressionRatio,
                    maxZipNestingDepth,
                    maxZipNestedBytes,
                    rejectArchiveLinks,
                    allowUnknownArchiveEntrySize,
                    hashIncludePayloadCopies,
                    hashIncludeFastHash,
                    hashIncludeSecureHash,
                    hashMaterializedFileName
                },
                ExpectedNormalizeProjectOptionsValuesCount,
                normalizedValues
            )
        End Function

        Friend Shared Function TryExceptionToReasonCode _
            (
                ex As Exception,
                ByRef reasonCode As String
            ) As Boolean

            reasonCode = Nothing

            If Not TryPrepareInvocation(OperationExceptionToReasonCode, ExceptionToReasonCodeMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationExceptionToReasonCode,
                ExceptionToReasonCodeMethod,
                New Object() {ex},
                reasonCode
            )
        End Function

        Friend Shared Function TryIsExtensionMatch _
            (
                path As String,
                detectedIsUnknown As Boolean,
                canonicalExtension As String,
                aliases() As String,
                mimeType As String,
                headerBytes As Integer,
                ByRef isMatch As Boolean
            ) As Boolean

            isMatch = False

            If Not TryPrepareInvocation(OperationIsExtensionMatch, IsExtensionMatchMethod) Then
                Return False
            End If

            Return TryInvokeBooleanForFallback(
                OperationIsExtensionMatch,
                IsExtensionMatchMethod,
                New Object() {
                    path,
                    detectedIsUnknown,
                    canonicalExtension,
                    aliases,
                    mimeType,
                    headerBytes
                },
                isMatch
            )
        End Function

        Friend Shared Function TryBuildSummaryValues _
            (
                canonicalExtension As String,
                mimeType As String,
                headerBytes As Integer,
                ByRef summaryValues() As Object
            ) As Boolean

            summaryValues = Nothing

            If Not TryPrepareInvocation(OperationBuildSummaryValues, BuildSummaryValuesMethod) Then
                Return False
            End If

            Return TryInvokeObjectArrayForFallback(
                OperationBuildSummaryValues,
                BuildSummaryValuesMethod,
                New Object() {
                    canonicalExtension,
                    mimeType,
                    headerBytes
                },
                ExpectedSummaryValuesCount,
                summaryValues
            )
        End Function

        Friend Shared Function TryResolveOpenDocumentMimeKindKey _
            (
                normalizedMime As String,
                ByRef kindKey As String
            ) As Boolean

            kindKey = Nothing

            If Not TryPrepareInvocation(OperationResolveOpenDocumentMimeKindKey, ResolveOpenDocumentMimeKindKeyMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationResolveOpenDocumentMimeKindKey,
                ResolveOpenDocumentMimeKindKeyMethod,
                New Object() {
                    normalizedMime
                },
                kindKey
            )
        End Function

        Friend Shared Function TryResolveArchivePackageKindKey _
            (
                hasContentTypes As Boolean,
                hasDocxMarker As Boolean,
                hasXlsxMarker As Boolean,
                hasPptxMarker As Boolean,
                openDocumentKindKey As String,
                hasOpenDocumentConflict As Boolean,
                ByRef kindKey As String
            ) As Boolean

            kindKey = Nothing

            If Not TryPrepareInvocation(OperationResolveArchivePackageKindKey, ResolveArchivePackageKindKeyMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationResolveArchivePackageKindKey,
                ResolveArchivePackageKindKeyMethod,
                New Object() {
                    hasContentTypes,
                    hasDocxMarker,
                    hasXlsxMarker,
                    hasPptxMarker,
                    openDocumentKindKey,
                    hasOpenDocumentConflict
                },
                kindKey
            )
        End Function

        Friend Shared Function TryResolveLegacyMarkerKindKey _
            (
                hasWord As Boolean,
                hasExcel As Boolean,
                hasPowerPoint As Boolean,
                ByRef kindKey As String
            ) As Boolean

            kindKey = Nothing

            If Not TryPrepareInvocation(OperationResolveLegacyMarkerKindKey, ResolveLegacyMarkerKindKeyMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationResolveLegacyMarkerKindKey,
                ResolveLegacyMarkerKindKeyMethod,
                New Object() {
                    hasWord,
                    hasExcel,
                    hasPowerPoint
                },
                kindKey
            )
        End Function

        Friend Shared Function TryNormalizeEvidenceLabel _
            (
                label As String,
                defaultLabel As String,
                ByRef normalizedLabel As String
            ) As Boolean

            normalizedLabel = Nothing

            If Not TryPrepareInvocation(OperationNormalizeEvidenceLabel, NormalizeEvidenceLabelMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationNormalizeEvidenceLabel,
                NormalizeEvidenceLabelMethod,
                New Object() {
                    label,
                    defaultLabel
                },
                normalizedLabel
            )
        End Function

        Friend Shared Function TryAppendNoteIfAny _
            (
                baseNotes As String,
                toAppend As String,
                ByRef combinedNotes As String
            ) As Boolean

            combinedNotes = Nothing

            If Not TryPrepareInvocation(OperationAppendNoteIfAny, AppendNoteIfAnyMethod) Then
                Return False
            End If

            Return TryInvokeStringForFallback(
                OperationAppendNoteIfAny,
                AppendNoteIfAnyMethod,
                New Object() {
                    baseNotes,
                    toAppend
                },
                combinedNotes
            )
        End Function

        Friend Shared Function TryResolveHmacKeyFromEnvironment _
            (
                environmentVariableName As String,
                ByRef isResolved As Boolean,
                ByRef key As Byte(),
                ByRef note As String
            ) As Boolean

            Dim values() As Object = Nothing

            isResolved = False
            key = Array.Empty(Of Byte)()
            note = String.Empty

            If Not TryPrepareInvocation(OperationResolveHmacKeyFromEnvironment, ResolveHmacKeyFromEnvironmentMethod) Then
                Return False
            End If

            If Not TryInvokeObjectArrayForFallback(
                OperationResolveHmacKeyFromEnvironment,
                ResolveHmacKeyFromEnvironmentMethod,
                New Object() {
                    environmentVariableName
                },
                ExpectedHmacResolutionValuesCount,
                values
            ) Then
                Return False
            End If

            Try
                ' Striktes Tuple-Shape: [Boolean, Byte(), String]
                isResolved = CBool(values(0))
                key = If(CType(values(1), Byte()), Array.Empty(Of Byte)())
                note = If(CStr(values(2)), String.Empty)
                Return True
            Catch ex As Exception When _
                TypeOf ex Is InvalidCastException OrElse
                TypeOf ex Is NullReferenceException OrElse
                TypeOf ex Is IndexOutOfRangeException
                isResolved = False
                key = Array.Empty(Of Byte)()
                note = String.Empty
                RecordFallback(OperationResolveHmacKeyFromEnvironment)
                Return False
            End Try
        End Function

        Friend Shared Function TryNormalizeArchiveRelativePath _
            (
                rawPath As String,
                allowDirectoryMarker As Boolean,
                ByRef isValid As Boolean,
                ByRef normalizedPath As String,
                ByRef isDirectory As Boolean
            ) As Boolean

            Dim values() As Object = Nothing

            isValid = False
            normalizedPath = String.Empty
            isDirectory = False

            If Not TryPrepareInvocation(OperationNormalizeArchiveRelativePath, NormalizeArchiveRelativePathMethod) Then
                Return False
            End If

            If Not TryInvokeObjectArrayForFallback(
                OperationNormalizeArchiveRelativePath,
                NormalizeArchiveRelativePathMethod,
                New Object() {
                    rawPath,
                    allowDirectoryMarker
                },
                ExpectedArchivePathValuesCount,
                values
            ) Then
                Return False
            End If

            Try
                ' Striktes Tuple-Shape: [Boolean, String, Boolean]
                isValid = CBool(values(0))
                normalizedPath = If(CStr(values(1)), String.Empty)
                isDirectory = CBool(values(2))
                Return True
            Catch ex As Exception When _
                TypeOf ex Is InvalidCastException OrElse
                TypeOf ex Is NullReferenceException OrElse
                TypeOf ex Is IndexOutOfRangeException
                isValid = False
                normalizedPath = String.Empty
                isDirectory = False
                RecordFallback(OperationNormalizeArchiveRelativePath)
                Return False
            End Try
        End Function

        Friend Shared Function TryIsRootPath _
            (
                destinationFull As String,
                ByRef isRootPath As Boolean
            ) As Boolean

            isRootPath = False

            If Not TryPrepareInvocation(OperationIsRootPath, IsRootPathMethod) Then
                Return False
            End If

            Return TryInvokeBooleanForFallback(
                OperationIsRootPath,
                IsRootPathMethod,
                New Object() {
                    destinationFull
                },
                isRootPath
            )
        End Function

        Private Shared Sub EnsureInitialized()

            If _isInitialized Then Return

            SyncLock SyncRoot
                If _isInitialized Then Return

                Try
                    Dim csCoreAssembly                          As Assembly = ResolveCsCoreAssembly()
                    Dim enumUtilityType                         As Type
                    Dim iterableUtilityType                     As Type
                    Dim guardUtilityType                        As Type
                    Dim exceptionFilterUtilityType              As Type
                    Dim hashNormalizationUtilityType            As Type
                    Dim materializationUtilityType              As Type
                    Dim projectOptionsUtilityType               As Type
                    Dim detectionPolicyUtilityType              As Type
                    Dim officePolicyUtilityType                 As Type
                    Dim evidencePolicyUtilityType               As Type
                    Dim archivePathPolicyUtilityType            As Type
                    Dim expectedGuardParameterTypes()           As Type
                    Dim expectedExceptionFilterParameterTypes() As Type
                    Dim expectedHashStringParameterTypes()      As Type

                    If csCoreAssembly Is Nothing Then
                        _isAvailable = False
                        Return
                    End If

                    enumUtilityType = csCoreAssembly.GetType(EnumUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    iterableUtilityType = csCoreAssembly.GetType(IterableUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    guardUtilityType = csCoreAssembly.GetType(GuardUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    exceptionFilterUtilityType = csCoreAssembly.GetType(ExceptionFilterUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    hashNormalizationUtilityType = csCoreAssembly.GetType(HashNormalizationUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    materializationUtilityType = csCoreAssembly.GetType(MaterializationUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    projectOptionsUtilityType = csCoreAssembly.GetType(ProjectOptionsUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    detectionPolicyUtilityType = csCoreAssembly.GetType(DetectionPolicyUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    officePolicyUtilityType = csCoreAssembly.GetType(OfficePolicyUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    evidencePolicyUtilityType = csCoreAssembly.GetType(EvidencePolicyUtilityTypeName, throwOnError:=False, ignoreCase:=False)
                    archivePathPolicyUtilityType = csCoreAssembly.GetType(ArchivePathPolicyUtilityTypeName, throwOnError:=False, ignoreCase:=False)

                    If enumUtilityType Is Nothing OrElse
                        iterableUtilityType Is Nothing OrElse
                        guardUtilityType Is Nothing OrElse
                        exceptionFilterUtilityType Is Nothing Then
                        _isAvailable = False
                        Return
                    End If

                    EnumGetValuesMethodDefinition = FindGenericMethodByName(enumUtilityType, "GetValues", parameterCount:=3)
                    CloneArrayMethodDefinition = FindGenericMethodByName(iterableUtilityType, "CloneArray", parameterCount:=1)

                    expectedGuardParameterTypes = New Type() {GetType(Object), GetType(String)}
                    GuardNotNullMethod = FindPublicStaticMethod(
                        guardUtilityType,
                        "NotNull",
                        expectedGuardParameterTypes
                    )

                    expectedGuardParameterTypes = New Type() {GetType(Array), GetType(Integer), GetType(String)}
                    GuardRequireLengthMethod = FindPublicStaticMethod(
                        guardUtilityType,
                        "RequireLength",
                        expectedGuardParameterTypes
                    )

                    expectedGuardParameterTypes = New Type() {GetType(Type), GetType(Object), GetType(String)}
                    GuardRequireEnumDefinedMethod = FindPublicStaticMethod(
                        guardUtilityType,
                        "RequireEnumDefined",
                        expectedGuardParameterTypes
                    )

                    expectedExceptionFilterParameterTypes = New Type() {GetType(Exception)}
                    IsArchiveValidationExceptionMethod = FindPublicStaticMethod(
                        exceptionFilterUtilityType,
                        "IsArchiveValidationException",
                        expectedExceptionFilterParameterTypes
                    )
                    IsPathNormalizationExceptionMethod = FindPublicStaticMethod(
                        exceptionFilterUtilityType,
                        "IsPathNormalizationException",
                        expectedExceptionFilterParameterTypes
                    )
                    IsPathResolutionExceptionMethod = FindPublicStaticMethod(
                        exceptionFilterUtilityType,
                        "IsPathResolutionException",
                        expectedExceptionFilterParameterTypes
                    )
                    IsLoggerWriteExceptionMethod = FindPublicStaticMethod(
                        exceptionFilterUtilityType,
                        "IsLoggerWriteException",
                        expectedExceptionFilterParameterTypes
                    )

                    expectedHashStringParameterTypes = New Type() {GetType(String)}
                    If hashNormalizationUtilityType IsNot Nothing Then
                        NormalizeDigestMethod = FindPublicStaticMethod(
                            hashNormalizationUtilityType,
                            "NormalizeDigest",
                            expectedHashStringParameterTypes
                        )
                        CreateEmptyDigestPartsMethod = FindPublicStaticMethod(
                            hashNormalizationUtilityType,
                            "CreateEmptyDigestParts",
                            parameterTypes:=Type.EmptyTypes
                        )
                        CoalesceMaterializedFileNameMethod = FindPublicStaticMethod(
                            hashNormalizationUtilityType,
                            "CoalesceMaterializedFileName",
                            expectedHashStringParameterTypes
                        )
                        NormalizeMaterializedFileNameMethod = FindPublicStaticMethod(
                            hashNormalizationUtilityType,
                            "NormalizeMaterializedFileName",
                            expectedHashStringParameterTypes
                        )
                    End If

                    If materializationUtilityType IsNot Nothing Then
                        IsPayloadWithinMaxBytesMethod = FindPublicStaticMethod(
                            materializationUtilityType,
                            "IsPayloadWithinMaxBytes",
                            New Type() {GetType(Integer), GetType(Long)}
                        )
                        DecideMaterializationModeMethod = FindPublicStaticMethod(
                            materializationUtilityType,
                            "DecideMode",
                            New Type() {GetType(Boolean), GetType(Boolean), GetType(Boolean), GetType(Boolean)}
                        )
                    End If

                    If projectOptionsUtilityType IsNot Nothing Then
                        NormalizeProjectOptionsValuesMethod = FindPublicStaticMethod(
                            projectOptionsUtilityType,
                            "NormalizeSnapshotValues",
                            New Type() {
                                GetType(Boolean),
                                GetType(Long),
                                GetType(Integer),
                                GetType(Integer),
                                GetType(Long),
                                GetType(Long),
                                GetType(Integer),
                                GetType(Integer),
                                GetType(Long),
                                GetType(Boolean),
                                GetType(Boolean),
                                GetType(Boolean),
                                GetType(Boolean),
                                GetType(Boolean),
                                GetType(String)
                            }
                        )
                    End If

                    If detectionPolicyUtilityType IsNot Nothing Then
                        ExceptionToReasonCodeMethod = FindPublicStaticMethod(
                            detectionPolicyUtilityType,
                            "ExceptionToReasonCode",
                            New Type() {
                                GetType(Exception)
                            }
                        )

                        IsExtensionMatchMethod = FindPublicStaticMethod(
                            detectionPolicyUtilityType,
                            "IsExtensionMatch",
                            New Type() {
                                GetType(String),
                                GetType(Boolean),
                                GetType(String),
                                GetType(String()),
                                GetType(String),
                                GetType(Integer)
                            }
                        )

                        BuildSummaryValuesMethod = FindPublicStaticMethod(
                            detectionPolicyUtilityType,
                            "BuildSummaryValues",
                            New Type() {
                                GetType(String),
                                GetType(String),
                                GetType(Integer)
                            }
                        )
                    End If

                    If officePolicyUtilityType IsNot Nothing Then
                        ResolveOpenDocumentMimeKindKeyMethod = FindPublicStaticMethod(
                            officePolicyUtilityType,
                            "ResolveOpenDocumentMimeKindKey",
                            New Type() {
                                GetType(String)
                            }
                        )

                        ResolveArchivePackageKindKeyMethod = FindPublicStaticMethod(
                            officePolicyUtilityType,
                            "ResolveArchivePackageKindKey",
                            New Type() {
                                GetType(Boolean),
                                GetType(Boolean),
                                GetType(Boolean),
                                GetType(Boolean),
                                GetType(String),
                                GetType(Boolean)
                            }
                        )

                        ResolveLegacyMarkerKindKeyMethod = FindPublicStaticMethod(
                            officePolicyUtilityType,
                            "ResolveLegacyMarkerKindKey",
                            New Type() {
                                GetType(Boolean),
                                GetType(Boolean),
                                GetType(Boolean)
                            }
                        )
                    End If

                    If evidencePolicyUtilityType IsNot Nothing Then
                        NormalizeEvidenceLabelMethod = FindPublicStaticMethod(
                            evidencePolicyUtilityType,
                            "NormalizeLabel",
                            New Type() {
                                GetType(String),
                                GetType(String)
                            }
                        )

                        AppendNoteIfAnyMethod = FindPublicStaticMethod(
                            evidencePolicyUtilityType,
                            "AppendNoteIfAny",
                            New Type() {
                                GetType(String),
                                GetType(String)
                            }
                        )

                        ResolveHmacKeyFromEnvironmentMethod = FindPublicStaticMethod(
                            evidencePolicyUtilityType,
                            "ResolveHmacKeyFromEnvironment",
                            New Type() {
                                GetType(String)
                            }
                        )
                    End If

                    If archivePathPolicyUtilityType IsNot Nothing Then
                        NormalizeArchiveRelativePathMethod = FindPublicStaticMethod(
                            archivePathPolicyUtilityType,
                            "NormalizeRelativePath",
                            New Type() {
                                GetType(String),
                                GetType(Boolean)
                            }
                        )

                        IsRootPathMethod = FindPublicStaticMethod(
                            archivePathPolicyUtilityType,
                            "IsRootPath",
                            New Type() {
                                GetType(String)
                            }
                        )
                    End If

                    _isAvailable =
                        EnumGetValuesMethodDefinition IsNot Nothing AndAlso
                        CloneArrayMethodDefinition IsNot Nothing AndAlso
                        GuardNotNullMethod IsNot Nothing AndAlso
                        GuardRequireLengthMethod IsNot Nothing AndAlso
                        GuardRequireEnumDefinedMethod IsNot Nothing AndAlso
                        IsArchiveValidationExceptionMethod IsNot Nothing AndAlso
                        IsPathNormalizationExceptionMethod IsNot Nothing AndAlso
                        IsPathResolutionExceptionMethod IsNot Nothing AndAlso
                        IsLoggerWriteExceptionMethod IsNot Nothing
                Catch ex As Exception
                    _isAvailable = False
                Finally
                    _isInitialized = True
                End Try
            End SyncLock
        End Sub

        Private Shared Function ResolveCsCoreAssembly() As Assembly

            Dim loadedAssemblies() As Assembly
            Dim loadedAssembly     As Assembly
            Dim loadedName         As String
            Dim resolvedAssembly   As Assembly
            Dim assemblyDirectory  As String
            Dim assemblyPath       As String
            Dim baseDirectoryPath  As String

            resolvedAssembly = Nothing
            loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            For Each loadedAssembly In loadedAssemblies
                loadedName = loadedAssembly.GetName().Name
                If String.Equals(loadedName, CsCoreAssemblySimpleName, StringComparison.Ordinal) Then
                    Return loadedAssembly
                End If
            Next

            Try
                Return Assembly.Load(New AssemblyName(CsCoreAssemblySimpleName))
            Catch ex As Exception When _
                TypeOf ex Is IO.FileNotFoundException OrElse
                TypeOf ex Is IO.FileLoadException OrElse
                TypeOf ex Is BadImageFormatException
                ' Optionales Runtime-Modul: Fallback auf Dateisuche in vertrauenswürdigen Basisverzeichnissen.
            End Try

            assemblyDirectory = IO.Path.GetDirectoryName(GetType(CsCoreRuntimeBridge).Assembly.Location)
            If Not String.IsNullOrWhiteSpace(assemblyDirectory) Then
                assemblyPath = IO.Path.Combine(assemblyDirectory, CsCoreAssemblyFileName)
                If TryLoadAssemblyFromPath(assemblyPath, resolvedAssembly) Then
                    Return resolvedAssembly
                End If
            End If

            baseDirectoryPath = AppContext.BaseDirectory
            If Not String.IsNullOrWhiteSpace(baseDirectoryPath) Then
                assemblyPath = IO.Path.Combine(baseDirectoryPath, CsCoreAssemblyFileName)
                If TryLoadAssemblyFromPath(assemblyPath, resolvedAssembly) Then
                    Return resolvedAssembly
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function TryLoadAssemblyFromPath _
            (
                assemblyPath As String,
                ByRef resolvedAssembly As Assembly
            ) As Boolean

            Dim rawAssembly() As Byte

            resolvedAssembly = Nothing
            If String.IsNullOrWhiteSpace(assemblyPath) Then Return False
            If Not IO.File.Exists(assemblyPath) Then Return False

            Try
                rawAssembly = IO.File.ReadAllBytes(assemblyPath)
                If rawAssembly.Length = 0 Then Return False

                resolvedAssembly = Assembly.Load(rawAssembly)
                Return resolvedAssembly IsNot Nothing
            Catch ex As Exception When _
                TypeOf ex Is IO.IOException OrElse
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is BadImageFormatException OrElse
                TypeOf ex Is IO.FileLoadException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is System.Security.SecurityException
                Return False
            End Try
        End Function

        Private Shared Function FindGenericMethodByName _
            (
                targetType As Type,
                methodName As String,
                parameterCount As Integer
            ) As MethodInfo

            Dim methods()    As MethodInfo
            Dim method       As MethodInfo
            Dim parameters() As ParameterInfo

            methods = targetType.GetMethods(BindingFlags.Public Or BindingFlags.Static)
            For Each method In methods
                If Not String.Equals(method.Name, methodName, StringComparison.Ordinal) Then Continue For
                If Not method.IsGenericMethodDefinition Then Continue For

                parameters = method.GetParameters()
                If parameters.Length = parameterCount Then
                    Return method
                End If
            Next

            Return Nothing
        End Function

        Private Shared Function FindPublicStaticMethod _
            (
                targetType As Type,
                methodName As String,
                parameterTypes() As Type
            ) As MethodInfo

            Return targetType.GetMethod(
                methodName,
                BindingFlags.Public Or BindingFlags.Static,
                binder:=Nothing,
                types:=parameterTypes,
                modifiers:=Nothing
            )
        End Function

        ''' <summary>
        '''     <b>Pruefung:</b><br/>
        '''     Stellt sicher, dass CSCore initialisiert und als verfuegbar markiert ist.
        ''' </summary>
        Private Shared Function TryEnsureCsCoreAvailable _
            (
                operationName As String
            ) As Boolean

            EnsureInitialized()
            If _isAvailable Then
                Return True
            End If

            RecordFallback(operationName)
            Return False
        End Function

        ''' <summary>
        '''     <b>Pruefung:</b><br/>
        '''     Fuehrt Verfuegbarkeits- und Methodenguard zusammen und zaehlt Delegation.
        ''' </summary>
        Private Shared Function TryPrepareInvocation _
            (
                operationName As String,
                method As MethodInfo
            ) As Boolean

            If Not TryEnsureCsCoreAvailable(operationName) Then
                Return False
            End If

            If method Is Nothing Then
                RecordFallback(operationName)
                Return False
            End If

            RecordDelegated(operationName)
            Return True
        End Function

        ''' <summary>
        '''     <b>Typisierte Rueckgabe:</b><br/>
        '''     Invoked Ergebnis muss String sein; sonst fail-closed Fallback.
        ''' </summary>
        Private Shared Function TryInvokeStringForFallback _
            (
                operationName As String,
                method As MethodInfo,
                arguments() As Object,
                ByRef value As String
            ) As Boolean

            Dim result As Object = Nothing

            value = Nothing
            If Not TryInvokeForFallback(operationName, method, arguments, result) Then
                Return False
            End If

            If Not TypeOf result Is String Then
                RecordFallback(operationName)
                Return False
            End If

            value = CStr(result)
            Return True
        End Function

        ''' <summary>
        '''     <b>Typisierte Rueckgabe:</b><br/>
        '''     Invoked Ergebnis muss Integer sein; sonst fail-closed Fallback.
        ''' </summary>
        Private Shared Function TryInvokeIntegerForFallback _
            (
                operationName As String,
                method As MethodInfo,
                arguments() As Object,
                ByRef value As Integer
            ) As Boolean

            Dim result As Object = Nothing

            value = 0
            If Not TryInvokeForFallback(operationName, method, arguments, result) Then
                Return False
            End If

            If Not TypeOf result Is Integer Then
                RecordFallback(operationName)
                Return False
            End If

            value = CInt(result)
            Return True
        End Function

        ''' <summary>
        '''     <b>Shape-Guard:</b><br/>
        '''     Invoked Ergebnis muss <c>Object()</c> mit exakt erwarteter Laenge liefern.
        ''' </summary>
        Private Shared Function TryInvokeObjectArrayForFallback _
            (
                operationName As String,
                method As MethodInfo,
                arguments() As Object,
                expectedLength As Integer,
                ByRef values() As Object
            ) As Boolean

            Dim result As Object = Nothing

            values = Nothing
            If Not TryInvokeForFallback(operationName, method, arguments, result) Then
                Return False
            End If

            If Not TypeOf result Is Object() Then
                RecordFallback(operationName)
                Return False
            End If

            values = CType(result, Object())
            If values.Length <> expectedLength Then
                values = Nothing
                RecordFallback(operationName)
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        '''     <b>Shape-Guard:</b><br/>
        '''     Invoked Ergebnis muss <c>String()</c> mit exakt erwarteter Laenge liefern.
        ''' </summary>
        Private Shared Function TryInvokeStringArrayForFallback _
            (
                operationName As String,
                method As MethodInfo,
                arguments() As Object,
                expectedLength As Integer,
                ByRef values() As String
            ) As Boolean

            Dim result As Object = Nothing

            values = Nothing
            If Not TryInvokeForFallback(operationName, method, arguments, result) Then
                Return False
            End If

            If Not TypeOf result Is String() Then
                RecordFallback(operationName)
                Return False
            End If

            values = CType(result, String())
            If values.Length <> expectedLength Then
                values = Nothing
                RecordFallback(operationName)
                Return False
            End If

            Return True
        End Function

        Private Shared Function TryGetClosedGenericMethod _
            (
                genericMethodDefinition As MethodInfo,
                genericArgument As Type,
                cache As ConcurrentDictionary(Of Type, MethodInfo),
                ByRef closedMethod As MethodInfo
            ) As Boolean

            closedMethod = Nothing
            If genericMethodDefinition Is Nothing Then Return False
            If genericArgument Is Nothing Then Return False

            Try
                closedMethod = cache.GetOrAdd(genericArgument, Function(type As Type) genericMethodDefinition.MakeGenericMethod(type))
                Return closedMethod IsNot Nothing
            Catch ex As Exception When _
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is NotSupportedException
                closedMethod = Nothing
                Return False
            End Try
        End Function

        Private Shared Function InvokeWithUnwrappedInnerException _
            (
                method As MethodInfo,
                arguments() As Object
            ) As Object

            Try
                Return method.Invoke(Nothing, arguments)
            Catch ex As TargetInvocationException
                RethrowInnerException(ex)
            End Try

            Throw New InvalidOperationException("Unerreichbarer Codepfad nach Behandlung von TargetInvocationException erreicht.")
        End Function

        Private Shared Function TryInvokeForFallback _
            (
                operationName As String,
                method As MethodInfo,
                arguments() As Object,
                ByRef result As Object
            ) As Boolean

            result = Nothing

            Try
                result = InvokeWithUnwrappedInnerException(method, arguments)
                Return True
            Catch ex As Exception When _
                TypeOf ex Is TargetException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is MethodAccessException OrElse
                TypeOf ex Is MemberAccessException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is NullReferenceException OrElse
                TypeOf ex Is IndexOutOfRangeException OrElse
                TypeOf ex Is InvalidCastException
                RecordFallback(operationName)
                Return False
            End Try
        End Function

        Private Shared Function TryInvokeBooleanForFallback _
            (
                operationName As String,
                method As MethodInfo,
                arguments() As Object,
                ByRef value As Boolean
            ) As Boolean

            Dim result As Object = Nothing

            value = False
            If Not TryInvokeForFallback(operationName, method, arguments, result) Then
                Return False
            End If

            If Not TypeOf result Is Boolean Then
                RecordFallback(operationName)
                Return False
            End If

            value = CBool(result)
            Return True
        End Function

        Private Shared Sub RecordDelegated _
            (
                operationName As String
            )

            DelegatedCounterByOperation.AddOrUpdate(
                operationName,
                addValueFactory:=Function(key As String) 1L,
                updateValueFactory:=Function(key As String, currentValue As Long) currentValue + 1L
            )
        End Sub

        Private Shared Sub RecordFallback _
            (
                operationName As String
            )

            FallbackCounterByOperation.AddOrUpdate(
                operationName,
                addValueFactory:=Function(key As String) 1L,
                updateValueFactory:=Function(key As String, currentValue As Long) currentValue + 1L
            )
        End Sub

        Private Shared Sub RethrowInnerException _
            (
                ex As TargetInvocationException
            )

            If ex.InnerException IsNot Nothing Then
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw()
            End If

            Throw ex
        End Sub
    End Class

    Friend NotInheritable Class CsCoreRuntimeBridgeTelemetrySnapshot

        Private ReadOnly _delegatedByOperation As Dictionary(Of String, Long)
        Private ReadOnly _fallbackByOperation As Dictionary(Of String, Long)

        Friend Sub New _
            (
                isCsCoreAvailable As Boolean,
                delegatedByOperation As Dictionary(Of String, Long),
                fallbackByOperation As Dictionary(Of String, Long)
            )

            Dim pair As KeyValuePair(Of String, Long)

            Me.IsCsCoreAvailable = isCsCoreAvailable
            _delegatedByOperation = New Dictionary(Of String, Long)(StringComparer.Ordinal)
            _fallbackByOperation = New Dictionary(Of String, Long)(StringComparer.Ordinal)

            For Each pair In delegatedByOperation
                _delegatedByOperation(pair.Key) = pair.Value
                TotalDelegated += pair.Value
            Next

            For Each pair In fallbackByOperation
                _fallbackByOperation(pair.Key) = pair.Value
                TotalFallback += pair.Value
            Next
        End Sub

        Friend ReadOnly Property IsCsCoreAvailable As Boolean

        Friend ReadOnly Property TotalDelegated As Long

        Friend ReadOnly Property TotalFallback As Long

        Friend Function GetDelegatedCount _
            (
                operationName As String
            ) As Long

            Dim value As Long

            If _delegatedByOperation.TryGetValue(operationName, value) Then
                Return value
            End If

            Return 0
        End Function

        Friend Function GetFallbackCount _
            (
                operationName As String
            ) As Long

            Dim value As Long

            If _fallbackByOperation.TryGetValue(operationName, value) Then
                Return value
            End If

            Return 0
        End Function

        Friend Function GetTotalCount _
            (
                operationName As String
            ) As Long

            Return GetDelegatedCount(operationName) + GetFallbackCount(operationName)
        End Function
    End Class

End Namespace
