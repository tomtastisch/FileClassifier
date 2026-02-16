<!-- LANG_SWITCH:BEGIN -->
[DE](001_HMAC_KEY_SETUP.MD) | [EN](101_HMAC_KEY_SETUP.MD)
<!-- LANG_SWITCH:END -->

# HMAC Key Setup (FILECLASSIFIER_HMAC_KEY_B64)

## Purpose
The optional HMAC-SHA256 digests produced by `EvidenceHashing` provide authenticity and tamper-evidence for hash evidence.
They are not "better hashing" than SHA-256; they are keyed digests that require a shared secret key.

## Key Format (SSOT)
- Environment variable name: `FILECLASSIFIER_HMAC_KEY_B64`
- Value: Base64-encoded key bytes
- Recommendation: 32 random bytes (256-bit)

Important:
- Do not log the key.
- Do not commit the key to git.
- Treat the key like any other secret.

## Key Generation
Generate 32 random bytes and Base64-encode them.

Example (Linux/macOS):
```bash
python3 - <<'PY'
import base64, os
print(base64.b64encode(os.urandom(32)).decode('ascii'))
PY
```

Example (PowerShell):
```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

## Setting FILECLASSIFIER_HMAC_KEY_B64
### Windows (PowerShell)
Set for current session:
```powershell
$env:FILECLASSIFIER_HMAC_KEY_B64 = "<base64>"
```

Persist for current user:
```powershell
[System.Environment]::SetEnvironmentVariable("FILECLASSIFIER_HMAC_KEY_B64", "<base64>", "User")
```

Persist machine-wide (requires elevated permissions):
```powershell
[System.Environment]::SetEnvironmentVariable("FILECLASSIFIER_HMAC_KEY_B64", "<base64>", "Machine")
```

### Linux (bash/zsh)
Set for current shell:
```bash
export FILECLASSIFIER_HMAC_KEY_B64="<base64>"
```

Persist for your user (example):
- Add the export line to `~/.bashrc`, `~/.zshrc`, or a dedicated profile file sourced by your shell.

Optional systemd service example:
- Add to your unit file:
```ini
Environment=FILECLASSIFIER_HMAC_KEY_B64=<base64>
```

### macOS (zsh)
Set for current shell:
```bash
export FILECLASSIFIER_HMAC_KEY_B64="<base64>"
```

Persistent user setup options:
- Shell profile: add to `~/.zshrc` (ensure file permissions are restrictive).
- Keychain-backed approach: store the secret in Keychain and inject it at process start.
- CI-friendly approach: set it in the environment of the calling process (GitHub Actions, launchd, etc.).

## GitHub Actions Secret
Store the Base64 value as a GitHub repository secret:
- Secret name: `FILECLASSIFIER_HMAC_KEY_B64`

Then expose it as an environment variable in the workflow step that runs hashing.

## Key Rotation
Recommended rotation approach:
1. Generate a new key and roll it out to producers and verifiers in a coordinated window.
2. During rotation, allow verification using both the old and new key if you need to validate existing evidence.
3. After rotation completes, remove the old key.

Warning:
- Never print key material in logs.
- Keep build/test output free of secrets.
