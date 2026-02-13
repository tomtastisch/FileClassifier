# Security-Policy Benchmark (Stand: 2026-02-13)

## 1. Ziel und Scope
Vergleich der Security-Policy-Reife von `tomtastisch/FileClassifier` (PR-Branch `tomtastisch-patch-1`) mit verbreiteten .NET-Open-Source-Repositories anhand nachweisbarer GitHub- und Repository-Fakten.
Dieser Benchmark ist ein Snapshot vor Merge in `main` (Stand 2026-02-13).

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
Erhoben via GitHub API und lokale Dateiinspektion:
- Vorhandensein `SECURITY.md` (`SECURITY.md` oder `.github/SECURITY.md`)
- Status `private-vulnerability-reporting`
- Sichtbare `security_and_analysis`-Felder (`dependabot_security_updates`, `secret_scanning`)
- Vorhandensein `.github/dependabot.yml`
- Vorhandensein von Workflow-Dateien mit `codeql` im Dateinamen
- Inhaltsmerkmale der `SECURITY.md`: Support-Tabelle, Reporting, SLA-Zeitangaben, Safe Harbor, ISO/IEC 29147/30111, koordinierte Offenlegung

## 3. Ergebnis A - Plattform-/Repo-Merkmale
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
- `unknown` bedeutet: Feld wird ueber GitHub API fuer das betrachtete Fremdrepo nicht veroeffentlicht bzw. nicht sichtbar geliefert.
- `false_or_unavailable` bedeutet: API liefert fuer den Endpunkt keine aktive Konfiguration.

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
- Repo-seitige Sicherheitsmechanismen sind fuer FileClassifier nachweisbar aktiv:
  - Private Vulnerability Reporting (`enabled=true`)
  - Dependabot Security Updates (`enabled`)
  - Secret Scanning (`enabled`)
  - CI-Sicherheitsgate `security-nuget` (lokal mit `pass` verifiziert)

## 6. Rest-Gaps fuer "zertifizierungsnahe" Einsatzfaehigkeit
Diese Punkte sind ausserhalb einer einzelnen `SECURITY.md`, aber notwendig fuer belastbare Audit-/Zertifizierungsfaehigkeit:
- formaler SDLC-Nachweis (dokumentierte Rollen, Freigaben, Change-Management)
- Threat-Modeling-Artefakte mit regelmaessiger Aktualisierung
- Incident-Response-Runbooks mit Uebungen und Nachweisen
- SBOM/Provenance-Strategie fuer Releases (inkl. Aufbewahrung)
- formaler Nachweis fuer Produktionsbetriebskontrollen beim Betreiber

## 7. Reproduzierbarkeit
Verwendete Kommandos (Auszug):
```bash
gh api 'repos/<owner>/<repo>'
gh api 'repos/<owner>/<repo>/private-vulnerability-reporting'
gh api 'repos/<owner>/<repo>/contents/SECURITY.md'
gh api 'repos/<owner>/<repo>/contents/.github/SECURITY.md'
gh api 'repos/<owner>/<repo>/contents/.github/workflows'
gh api 'repos/<owner>/<repo>/contents/.github/dependabot.yml'
```

Lokale Verifikation fuer FileClassifier:
```bash
python3 tools/check-docs.py
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/ci/bin/run.sh security-nuget
```
