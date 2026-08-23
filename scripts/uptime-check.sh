#!/usr/bin/env sh
set -eu

base_url=${1:-}
maximum_seconds=${APPOINTMENTCRM_UPTIME_MAX_SECONDS:-2}
test -n "$base_url" || {
  echo "Usage: uptime-check.sh https://appointment.example.com" >&2
  exit 2
}

check() {
  endpoint=$1
  elapsed=$(curl --silent --show-error --fail \
    --max-time 10 \
    --output /dev/null \
    --write-out '%{time_total}' \
    "$base_url$endpoint")
  awk -v elapsed="$elapsed" -v maximum="$maximum_seconds" \
    'BEGIN { if ((elapsed + 0) > (maximum + 0)) exit 1 }' || {
      echo "$endpoint exceeded ${maximum_seconds}s (actual ${elapsed}s)." >&2
      exit 1
    }
  echo "$endpoint ${elapsed}s"
}

check /health/live
check /health/ready
