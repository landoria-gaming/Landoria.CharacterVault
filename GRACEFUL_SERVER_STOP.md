# Graceful server stop and restart

This optional feature works on a dedicated server. CharacterVault watches for
`character_vault.drp` in the Valheim process working directory. The file must
contain the current Valheim process ID. A valid request asks connected players
to save, waits up to 90 seconds for the server commits, then starts Valheim's
normal world save and shutdown. If the timeout expires, the server disconnects
players whose character save was not committed and continues shutting down.

Example Linux systemd stop helper:

```bash
#!/usr/bin/env bash
set -u

readonly valheim_pid="${1:-}"
readonly working_directory="${2:-}"

if [[ ! "$valheim_pid" =~ ^[0-9]+$ ]] || (( valheim_pid <= 1 )); then
  exit 0
fi
if [[ ! -d "$working_directory" ]] || ! kill -0 "$valheim_pid" 2>/dev/null; then
  exit 0
fi

readonly exit_file="$working_directory/character_vault.drp"
readonly temporary_exit_file="$exit_file.$$"
trap 'rm -f -- "$temporary_exit_file"' EXIT HUP INT TERM
printf '%s\n' "$valheim_pid" >"$temporary_exit_file"
mv -f -- "$temporary_exit_file" "$exit_file"
trap - EXIT HUP INT TERM

while kill -0 "$valheim_pid" 2>/dev/null; do
  sleep 1
done
```

Configure the service with the same working directory used to start Valheim.
`$MAINPID` must be the Valheim server process itself, not a parent shell or
wrapper process:

```ini
[Service]
WorkingDirectory=/path/to/server-working-directory
ExecStop=/path/to/character-vault-stop.sh $MAINPID /path/to/server-working-directory
TimeoutStopSec=120
```

Make the helper executable and reload systemd. Atomic rename prevents the plugin
from reading a partial request, and the 120-second service timeout accommodates
the 90-second character-save limit plus vanilla shutdown.
