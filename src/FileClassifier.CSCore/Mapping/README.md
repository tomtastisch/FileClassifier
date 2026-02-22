# CSCore Mapping Modul

## 1. Zweck
Compile-time Mapping zwischen CSCore-Modellen zur Reduktion von manuellem Mapping-Boilerplate.

## 2. Inhalt
- `FileDetectionMapper.cs`: `DetectionSignal -> DetectionSummary`.
- `ProjectOptionsSnapshotMapper.cs`: Clone-Projektionen fuer Snapshot-Modelle.

## 3. Designregeln
- Mapping wird ueber `Riok.Mapperly` source-generated.
- Keine runtime-reflection Mapper.
- Fail-fast bei ungueltigen Mapping-Definitionen bereits zur Build-Zeit.

## 4. Verweise
- [CSCore Root](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.CSCore/README.md)
- [Utilities Layer](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.CSCore/Utilities/README.md)
