#!/bin/bash
# Checks a RestDraft linux-x64 tarball (for example a downloaded release asset) in WSL.
#
# Usage, from Windows:
#   wsl.exe -d Ubuntu-24.04 -- bash /mnt/c/Vibe/RestDraft/scripts/verify-release-wsl.sh /mnt/c/path/RestDraft-linux-x64.tar.gz
#
# What it does: verifies the checksum when SHA256SUMS.txt is next to the tarball, extracts into a
# new folder in the WSL home folder (not /mnt/c: the Windows drive keeps no file permissions),
# opens gedit first so WSLg is warm, starts the app, and checks the layout, the executable bit,
# data/app.db, the log, and that nothing was written to ~/.net/RestDraft.
# Exit code: 0 when every check passed, 1 otherwise.
#
# Environment: WAIT_SECONDS (default 360) is how long to wait for data/app.db.

TARBALL_ARG="$1"
WAIT_SECONDS="${WAIT_SECONDS:-360}"

if [ -z "$TARBALL_ARG" ]; then
  echo "Usage: verify-release-wsl.sh <path to RestDraft-linux-x64.tar.gz>"
  exit 1
fi

# Accept both a Linux path and a Windows path (C:\...).
case "$TARBALL_ARG" in
  [A-Za-z]:*) TARBALL="$(wslpath -u "$TARBALL_ARG")" ;;
  *) TARBALL="$TARBALL_ARG" ;;
esac
if [ ! -f "$TARBALL" ]; then
  echo "Tarball not found: $TARBALL"
  exit 1
fi

WORK="$HOME/restdraft-verify"
DIR="$WORK/RestDraft-linux-x64"
FAILURES=0
APP_PID=""
GEDIT_PID=""

# /proc/uptime is used for timings because the WSL clock can jump.
uptime_seconds() {
  cut -d' ' -f1 /proc/uptime
}

report() {
  local result="$1"
  local text="$2"
  if [ "$result" != "PASS" ]; then
    FAILURES=$((FAILURES + 1))
  fi
  echo "[$result] $text"
}

check() {
  local text="$1"
  shift
  if "$@" >/dev/null 2>&1; then
    report PASS "$text"
  else
    report FAIL "$text"
  fi
}

stop_processes() {
  if [ -n "$APP_PID" ]; then
    kill "$APP_PID" 2>/dev/null
    sleep 2
    kill -9 "$APP_PID" 2>/dev/null
  fi
  if [ -n "$GEDIT_PID" ]; then
    kill "$GEDIT_PID" 2>/dev/null
  fi
}

cleanup() {
  stop_processes
  rm -rf "$WORK"
}
trap cleanup EXIT

net_folder_entries() {
  find "$HOME/.net/RestDraft" 2>/dev/null | wc -l
}

has_one_top_level_folder() {
  [ "$(tar -tzf "$TARBALL" | cut -d/ -f1 | sort -u)" = "RestDraft-linux-x64" ]
}

only_expected_files_are_executable() {
  local executables
  executables="$(tar -tvzf "$TARBALL" | awk '$1 ~ /^-..x/ {print $NF}' | sort | tr '\n' ' ')"
  [ "$executables" = "RestDraft-linux-x64/RestDraft RestDraft-linux-x64/createdump " ]
}

# 1. Checksum, when the checksum file is next to the tarball.
SUMS="$(dirname "$TARBALL")/SHA256SUMS.txt"
if [ -f "$SUMS" ]; then
  check "sha256sum -c SHA256SUMS.txt (this tarball)" bash -c "cd '$(dirname "$TARBALL")' && sha256sum -c --ignore-missing SHA256SUMS.txt"
else
  echo "[SKIP] no SHA256SUMS.txt next to the tarball"
fi

# 2. Layout and permissions, read from the archive itself.
check "the archive has one top-level folder RestDraft-linux-x64/" has_one_top_level_folder
check "only RestDraft and createdump are executable" only_expected_files_are_executable

# 3. Warm up WSLg. The first GUI window after WSL starts can take minutes, see portability-checklist.md.
if command -v gedit >/dev/null 2>&1; then
  echo "[$(uptime_seconds)] starting gedit to warm up WSLg"
  nohup gedit >/dev/null 2>&1 &
  GEDIT_PID=$!
  sleep 20
else
  echo "[SKIP] gedit is not installed; the first start can be slow while WSLg warms up"
fi

# 4. Extract and start.
NET_BEFORE="$(net_folder_entries)"
rm -rf "$WORK"
mkdir -p "$WORK"
tar -xzf "$TARBALL" -C "$WORK"
echo "[$(uptime_seconds)] extracted $(find "$DIR" -type f | wc -l) files, starting RestDraft"
"$DIR/RestDraft" >"$WORK/output.txt" 2>&1 &
APP_PID=$!

for _ in $(seq 1 "$WAIT_SECONDS"); do
  if [ -f "$DIR/data/app.db" ]; then
    break
  fi
  sleep 1
done
sleep 5

# 5. Results.
check "data/app.db was created inside the program folder" test -f "$DIR/data/app.db"
check "a log file was created in data/logs" bash -c "ls '$DIR'/data/logs/*.log"
check "the app is still running after start" kill -0 "$APP_PID"
check "nothing was written to ~/.net/RestDraft" test "$(net_folder_entries)" = "$NET_BEFORE"

echo "--- log"
cat "$DIR"/data/logs/*.log 2>/dev/null | head -5
echo "--- stdout and stderr of the app"
head -20 "$WORK/output.txt"

echo "[$(uptime_seconds)] done, $FAILURES failure(s)"
if [ "$FAILURES" -gt 0 ]; then
  exit 1
fi
exit 0
