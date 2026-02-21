# Sicherheitsrichtlinie (SECURITY.md)

## 1. Zweck und Geltungsbereich
Diese Richtlinie beschreibt die Meldung, Bearbeitung und koordinierte Offenlegung von
Sicherheitsluecken im Repository `tomtastisch/FileClassifier`.

Die Ausgestaltung ist an bewaehrten Prozessen orientiert, insbesondere:
- ISO/IEC 29147 (Vulnerability Disclosure)
- ISO/IEC 30111 (Vulnerability Handling Processes)

Hinweis: Diese Richtlinie ist ein operatives Projekt-Policy-Dokument und kein
Zertifizierungs- oder Rechtsgutachten.

## 2. Unterstuetzte Versionen (Security Fixes)
Security-Fixes werden nur fuer den aktuell unterstuetzten Major bereitgestellt.

| Version | Security-Support |
| ------- | ---------------- |
| 6.x     | Ja               |
| < 6.0   | Nein             |

## 3. Meldung einer Sicherheitsluecke
Bitte melde Sicherheitsluecken **nicht** ueber oeffentliche Issues.

Primarer Meldeweg:
- GitHub Private Vulnerability Reporting / Security Advisory:
  [Repository Security](https://github.com/tomtastisch/FileClassifier/security)
  (dort "Report a vulnerability" verwenden)

Wenn die Plattform technisch nicht verfuegbar ist, bitte einen Issue ohne technische
Exploit-Details erstellen und auf vertraulichen Kontakt hinweisen.

## 4. Erforderliche Angaben in der Meldung
Bitte liefere nach Moeglichkeit:
- betroffene Version(en) und Umgebung
- klare Reproduktionsschritte
- erwartetes vs. tatsaechliches Verhalten
- Impact-Einschaetzung (Vertraulichkeit, Integritaet, Verfuegbarkeit)
- Proof-of-Concept in minimaler, sicherer Form
- bekannte Mitigations/Workarounds

## 5. Prozess und Reaktionszeiten (kompaktes SLA)
- Eingangsbestaetigung: in der Regel innerhalb von **5 Werktagen**
- Triage und Priorisierung: risikobasiert (Schweregrad, Ausnutzbarkeit, Reichweite)
- Behebungsplanung und Kommunikation: nach Risiko, Komplexitaet und Release-Zyklus

Es besteht kein Anspruch auf sofortige Behebung; wir arbeiten risikoorientiert und
koordinieren die Kommunikation transparent im Advisory-Prozess.

## 6. Safe Harbor fuer gutglaeubige Sicherheitsforschung
Wir begruessen verantwortungsvolle, gutglaeubige Forschung innerhalb folgender Leitplanken:
- keine absichtliche Datenexfiltration, Datenveraenderung oder dauerhafte Stoerung
- keine Denial-of-Service-Tests oder Lastspitzen gegen Produktions-/fremde Systeme
- kein Social Engineering, kein Phishing, keine physische Angriffe
- keine automatisierten Massen-Scans ohne vorherige Abstimmung
- nur notwendige, minimale Testtiefe zur Nachweisfuehrung
- unverzuegliche vertrauliche Meldung bei Fund

Wenn du in gutem Glauben und im Rahmen dieser Leitplanken handelst, betrachten wir das
als verantwortungsvolle Forschung und streben eine kooperative Loesung an.

## 7. Koordinierte Offenlegung
Wir verfolgen koordiniertes Disclosure:
- Oeffentliche Details erst nach verfuegbarem Fix oder abgestimmter Mitigation
- Zeitfenster werden fallbezogen zwischen Maintainern und meldender Person abgestimmt
- Credits werden auf Wunsch im Advisory genannt

## 8. Nicht unterstuetzte Meldungskanaele
- Oeffentliche GitHub Issues/Discussions fuer ungepatchte Schwachstellen
- Veroeffentlichung von Exploit-Details vor abgestimmter Offenlegung

## 9. Nachweisbarkeit und Einsatz in sicherheitsrelevanten Umgebungen
Dieses Repository trifft **keine** Aussage ueber formale Zertifizierung (z. B. ISO 27001,
IEC 62443, Common Criteria) des Produkts oder eines Betreiber-ISMS.

Der Einsatz in sicherheitsrelevanten oder systemkritischen Architekturen ist nur
verantwortbar, wenn die betreibende Organisation zusaetzliche, eigene Kontrollen
nachweisbar umsetzt (z. B. Threat Modeling, Haertung, Betriebsmonitoring, Incident Response,
Schluesselmanagement, Netzwerksegmentierung, Backup/Restore-Tests, Change-Management).

Nachweisbare, repo-seitige Sicherheitsmechanismen (Stand dieses Projekts):
- Security-Vulnerability-Meldeweg via GitHub Repository Security
- CI-Gate `security-nuget` fuer Vulnerability- und Deprecation-Scans
- Branch-Protection mit festen Required Contexts auf `main`
- Release-Publish via OIDC Trusted Publishing fuer NuGet (kein statischer API-Key im
  regulaeren Publish-Pfad)

Empfohlener Mindestnachweis vor Produktiveinsatz:
```bash
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/ci/bin/run.sh security-nuget
python3 tools/check-docs.py
```

Optional fuer erweiterten Nachweis:
```bash
bash tools/ci/bin/run.sh tests-bdd-coverage
bash tools/ci/bin/run.sh api-contract
bash tools/ci/bin/run.sh pack
bash tools/ci/bin/run.sh consumer-smoke
bash tools/ci/bin/run.sh package-backed-tests
```

Vielen Dank fuer verantwortungsvolle Meldungen und die Unterstuetzung der
Sicherheit von FileClassifier.
