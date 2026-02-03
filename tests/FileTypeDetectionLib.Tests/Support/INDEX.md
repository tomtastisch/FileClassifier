# Index - Support

## 1. Purpose
Gemeinsame Test-Helfer fuer stabilen, deterministischen Testbetrieb.

## 2. Inputs
- Testzustand, Ressourcen, Options-Snapshots

## 3. Outputs
- reproduzierbare Testkontexte und BDD-Konsolenausgabe

## 4. Failure Modes / Guarantees
- Testisolation via Scope/State-Helfer

## 5. Verification & Evidence
- `DetectorOptionsScope.cs`
- `BddConsoleHooks.cs`
