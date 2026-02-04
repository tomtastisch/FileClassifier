# API-Referenz (DE) - FileTypeDetection

## 1. Zweck
Diese Referenz beschreibt die **vollstaendige oeffentliche API** sowie den empfohlenen Einsatz je Szenario.
Ergaenzende normorientierte Spezifikation: [DIN_SPECIFICATION_DE.md](./DIN_SPECIFICATION_DE.md).

## 2. Entscheidungs-Matrix (welche Methode wann)
| Ziel | Empfohlene API | Warum |
|---|---|---|
| Dateityp einer Datei bestimmen | `FileTypeDetector.Detect(path)` | Einfachster fail-closed Einstieg |
| Dateityp bestimmen und Endung erzwingen | `FileTypeDetector.Detect(path, verifyExtension:=True)` | Konflikte werden zu `Unknown` normalisiert |
| Upload/Message-Bytes pruefen | `FileTypeDetector.Detect(data)` | Kein Dateisystemzugriff notwendig |
| Auditierbares Ergebnis inkl. Grundcode | `FileTypeDetector.DetectDetailed(...)` | `ReasonCode` + Policy-Flags |
| Nur ZIP-Sicherheit pruefen (ohne Extraktion) | `FileTypeDetector.TryValidateZip(path)` oder `ZipProcessing.TryValidate(...)` | Gate-only, keine Seiteneffekte |
| ZIP sicher auf Platte entpacken | `FileTypeDetector.ExtractZipSafe(...)` oder `ZipProcessing.ExtractToDirectory(...)` | Traversal-/Bomb-Schutz |
| ZIP sicher in Memory entpacken | `FileTypeDetector.ExtractZipSafeToMemory(...)` oder `ZipProcessing.TryExtractToMemory(...)` | Kein Persistieren auf Disk |
| Byte-Payload persistieren (optional ZIP->Disk) | `FileMaterializer.Persist(...)` | ein Einstieg fuer rohe Bytes und sichere ZIP-Extraktion |

## 3. Oeffentliche API - Detailtabelle
### 3.1 `FileTypeDetector` (Instanz + Shared)
| Symbol | Signatur | Input | Output | Einsatzzeitpunkt | Verhalten/Trigger |
|---|---|---|---|---|---|
| Datei sicher lesen | `ReadFileSafe(path)` | Datei-Pfad | `Byte()` | Vorverarbeitung fuer Byte-Pipelines | Groessenlimit erzwungen, Fehler => leeres Array |
| Detect (Pfad) | `Detect(path)` | Datei-Pfad | `FileType` | Standardfall | Header -> ZIP-Gate/Refiner -> fail-closed |
| Detect (Pfad + Policy) | `Detect(path, verifyExtension)` | Datei-Pfad + Bool | `FileType` | stricte Endungspolitik | Endungskonflikt => `Unknown` |
| DetectDetailed | `DetectDetailed(path)` | Datei-Pfad | `DetectionDetail` | Audit/UX/Telemetry | liefert `ReasonCode` und Flags |
| DetectDetailed + Policy | `DetectDetailed(path, verifyExtension)` | Datei-Pfad + Bool | `DetectionDetail` | Audit bei Endungsregeln | dokumentiert Mismatch explizit |
| Endungscheck | `DetectAndVerifyExtension(path)` | Datei-Pfad | `Boolean` | schnelles Policy-Gate | `False` bei Mismatch oder `Unknown` |
| ZIP-Validierung | `TryValidateZip(path)` | Datei-Pfad | `Boolean` | nur Sicherheitspruefung | kein Extract, nur Gate-Entscheid |
| Detect (Bytes) | `Detect(data)` | Payload | `FileType` | Upload/Queue/Event | gleiche Sicherheitslogik wie Pfad-Variante |
| Typvergleich | `IsOfType(data, kind)` | Payload + `FileKind` | `Boolean` | Guard-Checks in Workflows | intern auf `Detect(data)` aufgebaut |
| ZIP->Disk | `ExtractZipSafe(path, destination, verifyBeforeExtract)` | Pfad + Ziel + Bool | `Boolean` | kontrollierte Extraktion | Ziel darf nicht existieren, atomares Stage-Move |
| ZIP->Memory | `ExtractZipSafeToMemory(path, verifyBeforeExtract)` | Pfad + Bool | `IReadOnlyList(Of ZipExtractedEntry)` | sichere In-Memory-Verarbeitung | bei Fehler: leere Liste |

### 3.2 `ZipProcessing` (statische Fassade)
| Symbol | Signatur | Input | Output | Einsatzzeitpunkt | Verhalten/Trigger |
|---|---|---|---|---|---|
| ZIP validieren (Pfad) | `TryValidate(path)` | Datei-Pfad | `Boolean` | ZIP-Gate als Utility | delegiert an `FileTypeDetector.TryValidateZip` |
| ZIP validieren (Bytes) | `TryValidate(data)` | Payload | `Boolean` | Upload-Gate ohne Dateisystem | Header muss ZIP sein, dann Gate |
| ZIP->Disk | `ExtractToDirectory(path, destination, verifyBeforeExtract)` | Pfad + Ziel + Bool | `Boolean` | einfache statische Nutzung | delegiert auf fail-closed Extract |
| ZIP->Memory (Pfad) | `ExtractToMemory(path, verifyBeforeExtract)` | Pfad + Bool | `IReadOnlyList(Of ZipExtractedEntry)` | statische API fuer Leser | delegiert auf fail-closed Extract |
| ZIP->Memory (Bytes) | `TryExtractToMemory(data)` | Payload | `IReadOnlyList(Of ZipExtractedEntry)` | API/Queue ohne Temp-Datei | validiert zuerst, dann extrahiert |

### 3.3 `FileMaterializer` (statische Fassade)
| Symbol | Signatur | Input | Output | Einsatzzeitpunkt | Verhalten/Trigger |
|---|---|---|---|---|---|
| Persist (Default) | `Persist(data, destinationPath)` | Payload + Zielpfad | `Boolean` | Standardfall fuer Byte->Datei | schreibt fail-closed direkt auf Disk |
| Persist (Overwrite) | `Persist(data, destinationPath, overwrite)` | Payload + Zielpfad + Bool | `Boolean` | explizite Overwrite-Policy | `overwrite=False` verhindert Ueberschreiben |
| Persist (Full) | `Persist(data, destinationPath, overwrite, secureExtract)` | Payload + Zielpfad + 2 Bool | `Boolean` | ZIP-Bytes optional sicher entpacken | bei `secureExtract=True` + ZIP: Validierung und sichere Extraktion statt Raw-Write; `MaxBytes` und Root-Pfadschutz sind aktiv |

### 3.4 `FileTypeOptions` (statische Fassade)
| Symbol | Signatur | Input | Output | Einsatzzeitpunkt | Verhalten/Trigger |
|---|---|---|---|---|---|
| Optionen laden | `LoadOptions(json)` | JSON-String | `Boolean` | Startup/Runtime-Rekonfiguration | partielle JSON-Werte ueberschreiben Defaults; unbekannte Keys werden ignoriert; ungueltige Grenzwerte fallen auf Defaults zurueck |
| Optionen lesen | `GetOptions()` | - | `String` (JSON) | Audit/Diagnose/API-Ausgabe | liefert aktuellen globalen Snapshot als JSON |

### 3.5 `FileTypeSecurityBaseline`
| Symbol | Signatur | Zweck |
|---|---|---|
| Baseline anwenden | `ApplyDeterministicDefaults()` | setzt konservative globale Security-Grenzen |

## 4. Interne Kernpfade (Lesefuehrung)
| Interner Pfad | Datei | Bedeutung |
|---|---|---|
| Header/Typ-SSOT | `Detection/FileTypeRegistry.vb` | Aliase, Canonical Extensions, Magic-Patterns |
| ZIP-Gate/Bounds/Refiner | `Infrastructure/CoreInternals.vb` | sicherheitskritische Guards |
| ZIP-Engine/Extractor | `Infrastructure/ZipInternals.vb` | deterministische ZIP-Iteration + Extraktion |

## 5. Verwendungsbeispiele (C#)
### 5.1 Minimal: Typ bestimmen
```csharp
using FileTypeDetection;

var detector = new FileTypeDetector();
var t = detector.Detect("/data/upload.bin");
Console.WriteLine($"Kind={t.Kind}, Allowed={t.Allowed}");
```

### 5.2 Auditierbare Entscheidung mit Endungs-Policy
```csharp
using FileTypeDetection;

var detector = new FileTypeDetector();
var detail = detector.DetectDetailed("/data/invoice.pdf", verifyExtension: true);

Console.WriteLine($"Kind={detail.DetectedType.Kind}");
Console.WriteLine($"Reason={detail.ReasonCode}");
Console.WriteLine($"ZipCheck={detail.UsedZipContentCheck}");
Console.WriteLine($"ExtensionVerified={detail.ExtensionVerified}");
```

### 5.3 ZIP sicher in Memory (Byte-Input)
```csharp
using FileTypeDetection;

byte[] payload = File.ReadAllBytes("/data/archive.zip");
if (!ZipProcessing.TryValidate(payload))
{
    throw new InvalidOperationException("Unsicheres ZIP.");
}

var entries = ZipProcessing.TryExtractToMemory(payload);
foreach (var e in entries)
{
    Console.WriteLine($"{e.RelativePath} -> {e.Content.Length} bytes");
}
```

### 5.4 Konservative Baseline aktivieren
```csharp
using FileTypeDetection;

FileTypeSecurityBaseline.ApplyDeterministicDefaults();
```

## 6. Verwendungsbeispiele (VB)
```vb
Imports FileTypeDetection

FileTypeSecurityBaseline.ApplyDeterministicDefaults()

Dim detector As New FileTypeDetector()
Dim detail = detector.DetectDetailed("/data/sample.docx", verifyExtension:=True)
Console.WriteLine($"{detail.DetectedType.Kind} / {detail.ReasonCode}")
```

## 7. API-Stabilitaet und Versionierung
- Oeffentliche Einstiegspunkte liegen im Modul-Root (`FileTypeDetector.vb`, `ZipProcessing.vb`, `FileMaterializer.vb`, `FileTypeOptions.vb`).
- Sicherheitskritische Low-Level-Bausteine bleiben intern (`Friend`) in `Infrastructure/`.
- Neue Use-Cases bevorzugt als **additive** Methoden in den Root-APIs.

## 8. Abhaengigkeiten (ZIP/Logging)
- ZIP-Verarbeitung basiert auf `System.IO.Compression` (BCL, kein zusaetzliches NuGet erforderlich).
- `FileMaterializer` nutzt zusaetzlich `SharpCompress` fuer defensiven ZIP-Lesbarkeits-Check bei `secureExtract=True`.
- Logging basiert auf `Microsoft.Extensions.Logging` (via `FrameworkReference Microsoft.AspNetCore.App`).
