<!-- LANG_SWITCH:BEGIN -->
[DE](011_SECURITY_BENCHMARK.MD) | [EN](111_SECURITY_BENCHMARK.MD)
<!-- LANG_SWITCH:END -->

# Security-Policy-Benchmark (Stand: 2026-02-13)

## 1. Ziel und Geltungsbereich
Vergleich der Security-Policy-Reife von `tomtastisch/FileClassifier` (PR-Branch `tomtastisch-patch-1`) mit verbreiteten .NET-Open-Source-Repositories anhand nachweisbarer GitHub- und Repository-Fakten.
Dieser Benchmark ist ein Snapshot vor dem Merge in `main` (Stand 2026-02-13).

Verglichene Repositories:
- `tomtastisch/FileClassifier`
- `dotnet/runtime`
- `dotnet/aspnetcore`
- `JamesNK/Newtonsoft.Json`
- `DapperLib/Dapper`
- `serilog/serilog`
- `AutoMapper/AutoMapper`
- `App-vNext/Polly`
- `FluentValidation/FluentValidation`
- `xunit/xunit`
- `NLog/NLog`

## 2. Methodik (nur faktenbasiert)
Erhoben über GitHub API und lokale Dateiinspektion:
- Vorhandensein `SECURITY.md` (FileClassifier: repo-root `SECURITY.md`; andere Repos ggf. alternativ `.github/SECURITY.md`)
- Status `private-vulnerability-reporting`
- Sichtbare `security_and_analysis`-Felder (`dependabot_security_updates`, `secret_scanning`)
- Vorhandensein `.github/dependabot.yml`
- Vorhandensein von Workflow-Dateien mit `codeql` im Dateinamen
- Inhaltsmerkmale der `SECURITY.md`: Support-Tabelle, Reporting, SLA-Zeitangaben, Safe Harbor, ISO/IEC 29147/30111, koordinierte Offenlegung

## 3. Ergebnis A - Plattform-/Repository-Merkmale
| Repository | SECURITY.md | Private Vulnerability Reporting | Dependabot Security Updates | Secret Scanning | CodeQL Workflow-Datei | dependabot.yml |
|---|---|---|---|---|---|---|
| tomtastisch/FileClassifier | nein (Snapshot vor Merge in `main`) | true | enabled | enabled | nein | nein |
| dotnet/runtime | ja | false_or_unavailable | unknown | unknown | nein | ja |
| dotnet/aspnetcore | ja | false_or_unavailable | unknown | unknown | nein | ja |
| JamesNK/Newtonsoft.Json | nein | false_or_unavailable | unknown | unknown | ja | nein |
| DapperLib/Dapper | nein | false_or_unavailable | unknown | unknown | nein | nein |
| serilog/serilog | ja | true | unknown | unknown | nein | nein |
| AutoMapper/AutoMapper | nein | true | unknown | unknown | nein | nein |
| App-vNext/Polly | ja | true | unknown | unknown | nein | ja |
| FluentValidation/FluentValidation | nein | false_or_unavailable | unknown | unknown | nein | nein |
| xunit/xunit | nein | false_or_unavailable | unknown | unknown | nein | nein |
| NLog/NLog | ja | true | unknown | unknown | nein | ja |

Hinweis:
- `unknown` bedeutet: Feld wird über GitHub API für das betrachtete Fremdrepo nicht veröffentlicht bzw. nicht sichtbar geliefert.
- `false_or_unavailable` bedeutet: API liefert für den Endpunkt keine aktive Konfiguration.

## 4. Ergebnis B - SECURITY.md Inhaltsvergleich
| Repository | Support-Versionen | Reporting-Abschnitt | SLA-Zeitwerte | Safe Harbor | ISO/IEC 29147/30111 | Koordinierte Offenlegung |
|---|---|---|---|---|---|---|
| dotnet/runtime | ja | ja | ja | nein | nein | nein |
| dotnet/aspnetcore | ja | ja | ja | nein | nein | nein |
| serilog/serilog | ja | ja | nein | nein | nein | nein |
| App-vNext/Polly | nein | ja | ja | nein | nein | nein |
| NLog/NLog | ja | ja | nein | nein | nein | nein |
| tomtastisch/FileClassifier (lokal, PR) | ja | ja | ja | ja | ja | ja |

## 5. Interpretation (faktennah)
- Die neue `SECURITY.md` von FileClassifier ist inhaltlich umfangreicher als die untersuchten Vergleichsdateien (insbesondere Safe Harbor, ISO/IEC-Orientierung, koordinierte Offenlegung).
- Snapshot-Hinweis: Zum Erhebungszeitpunkt lag die Datei im PR-Branch `tomtastisch-patch-1` und noch nicht in `main`.
- Repo-seitige Sicherheitsmechanismen sind für FileClassifier nachweisbar aktiv:
  - Private Vulnerability Reporting (`enabled=true`)
  - Dependabot Security Updates (`enabled`)
  - Secret Scanning (`enabled`)
  - CI-Sicherheitsgate `security-nuget` (lokal mit `pass` verifiziert)

## 6. Rest-Gaps für "zertifizierungsnahe" Einsatzfähigkeit
Diese Punkte sind ausserhalb einer einzelnen `SECURITY.md`, aber notwendig für belastbare Audit-/Zertifizierungsfähigkeit:
- formaler SDLC-Nachweis (dokumentierte Rollen, Freigaben, Change-Management)
- Threat-Modeling-Artefakte mit regelmäßiger Aktualisierung
- Incident-Response-Runbooks mit Uebungen und Nachweisen
- SBOM/Provenance-Strategie für Releases (inkl. Aufbewahrung)
- formaler Nachweis für Produktionsbetriebskontrollen beim Betreiber

## 7. Reproduzierbarkeit
Verwendete Kommandos (Auszug):
Alle Kommandos sind für die Ausführung im Repository-Root gedacht.
```bash
REPO="<owner>/<repo>"
gh api "repos/$REPO"
gh api "repos/$REPO/private-vulnerability-reporting"
gh api "repos/$REPO/contents/SECURITY.md"
gh api "repos/$REPO/contents/.github/SECURITY.md"
gh api "repos/$REPO/contents/.github/workflows"
gh api "repos/$REPO/contents/.github/dependabot.yml"
```

Lokale Verifikation für FileClassifier:
```bash
python3 tools/check-docs.py
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/ci/bin/run.sh security-nuget
```
