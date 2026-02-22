<!-- LANG_SWITCH:BEGIN -->
[DE](001_HMAC_KEY_SETUP.MD) | [EN](../../1_en/secure/001_HMAC_KEY_SETUP.MD)
<!-- LANG_SWITCH:END -->

# HMAC-Key Setup (FILECLASSIFIER_HMAC_KEY_B64)

## Zweck
Die optionalen HMAC-SHA256-Digests, die von `EvidenceHashing` erzeugt werden, liefern Authentizitaet und Tamper-Evidence fuer Hash-Evidence.
Sie sind nicht "besseres Hashing" als SHA-256, sondern keyed Digests, die einen geteilten Secret-Key voraussetzen.

## Key-Format (SSOT)
- Environment-Variable: `FILECLASSIFIER_HMAC_KEY_B64`
- Wert: Base64-codierte Key-Bytes
- Empfehlung: 32 random Bytes (256-bit)

Wichtig:
- Den Key nicht loggen.
- Den Key nicht in git committen.
- Den Key wie jedes andere Secret behandeln.

## Key-Generierung
Erzeuge 32 random Bytes und Base64-codiere sie.

Beispiel (Linux/macOS):
```bash
python3 - <<'PY'
import base64, os
print(base64.b64encode(os.urandom(32)).decode('ascii'))
PY
```

Beispiel (PowerShell):
```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

## FILECLASSIFIER_HMAC_KEY_B64 setzen
### Windows (PowerShell)
Fuer die aktuelle Session:
```powershell
$env:FILECLASSIFIER_HMAC_KEY_B64 = "<base64>"
```

Persistent fuer den aktuellen User:
```powershell
[System.Environment]::SetEnvironmentVariable("FILECLASSIFIER_HMAC_KEY_B64", "<base64>", "User")
```

Persistent maschinenweit (erfordert erhoehte Rechte):
```powershell
[System.Environment]::SetEnvironmentVariable("FILECLASSIFIER_HMAC_KEY_B64", "<base64>", "Machine")
```

### Linux (bash/zsh)
Fuer die aktuelle Shell:
```bash
export FILECLASSIFIER_HMAC_KEY_B64="<base64>"
```

Persistent fuer den User (Beispiel):
- Export-Zeile in `~/.bashrc`, `~/.zshrc` oder ein dediziertes Profilfile eintragen, das von der Shell gesourced wird.

Optionales systemd-Service-Beispiel:
- In der Unit-Datei:
```ini
Environment=FILECLASSIFIER_HMAC_KEY_B64=<base64>
```

### macOS (zsh)
Fuer die aktuelle Shell:
```bash
export FILECLASSIFIER_HMAC_KEY_B64="<base64>"
```

Optionen fuer ein persistentes User-Setup:
- Shell-Profil: in `~/.zshrc` eintragen (Dateirechte restriktiv halten).
- Keychain-Ansatz: Secret in Keychain speichern und beim Prozessstart injizieren.
- CI-freundlich: im Environment des aufrufenden Prozesses setzen (GitHub Actions, launchd, etc.).

## GitHub Actions Secret
Den Base64-Wert als GitHub Repository Secret speichern:
- Secret-Name: `FILECLASSIFIER_HMAC_KEY_B64`
