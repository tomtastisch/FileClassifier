# Incident Response Runbook

## 1. Trigger Events
- private vulnerability report submitted via GitHub security reporting
- CI security gate failure with exploitable impact (`security-nuget`, provenance verification, or equivalent)
- credible external report of active exploitation

## 2. Roles
- Incident owner: repository maintainer on duty
- Technical triage: maintainer with code-area ownership
- Release owner: maintainer with release permissions
- Communications owner: maintainer coordinating disclosure/advisory text

## 3. SLA And Time Targets
- Intake acknowledgement: within 5 business days (per `SECURITY.md`)
- Triage decision target: as early as possible after reproducibility is confirmed
- Fix and disclosure timeline: risk- and complexity-based, coordinated with reporter

## 4. Severity Model (Operational)
- Sev-1: confirmed critical exploitability or active exploitation
- Sev-2: high impact with realistic exploitation path
- Sev-3: medium/low impact or hard-to-exploit conditions

## 5. Procedure
1. Intake
- Confirm report channel and classify duplicate/new case.
- Create internal tracking entry with timestamp and reporter context.

2. Triage
- Reproduce in isolated environment using minimal PoC.
- Determine affected versions and impact surface.

3. Containment
- Define temporary mitigations (config/workaround/documentation guardrails) if fix is not immediate.
- Avoid public issue disclosure before coordinated plan is agreed.

4. Remediation
- Implement minimal-risk patch in focused commit(s).
- Add or adapt regression tests for exploit path.

5. Verification
- Run security-relevant checks and document exact command outputs:
```bash
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/ci/bin/run.sh security-nuget
bash tools/audit/verify-security-claims.sh
```

6. Release And Disclosure
- Prepare advisory/release notes with affected/fixed versions.
- Publish coordinated disclosure once fix availability is confirmed.

7. Closure
- Record post-incident summary, root cause, and follow-up tasks.
- Link fix commit(s), tests, and evidence artifacts.

## 6. Evidence Checklist
- timeline with UTC timestamps
- severity assignment and rationale
- affected version range and fixed version
- reproduction steps and verification commands
- commit SHAs and PR references
- advisory/disclosure reference

## 7. Evidence Storage
- CI artifacts: `artifacts/ci/*`
- claim evidence: `artifacts/ci/security-claims-evidence/*`
- package/release evidence: `artifacts/nuget/*` when available

## 8. Non-Goals
- This runbook is not legal advice.
- This runbook does not claim formal certification or regulatory coverage by itself.
