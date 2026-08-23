#!/usr/bin/env sh
set -eu

base_url=${1:-}
test -n "$base_url" || {
  echo "Usage: smoke.sh https://appointment.example.com" >&2
  exit 2
}

attempt=1
while [ "$attempt" -le 30 ]; do
  if curl --silent --show-error --fail --max-time 10 \
      "$base_url/health/live" >/dev/null \
    && curl --silent --show-error --fail --max-time 10 \
      "$base_url/health/ready" >/dev/null \
    && curl --silent --show-error --fail --max-time 10 \
      "$base_url/api/v1/system/status" >/dev/null; then
    echo "Deployment smoke passed: $base_url"
    exit 0
  fi

  attempt=$((attempt + 1))
  sleep 5
done

echo "Deployment smoke failed after 30 attempts: $base_url" >&2
exit 1
