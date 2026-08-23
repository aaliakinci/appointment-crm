#!/usr/bin/env sh
set -eu

environment_name=${1:-}
release_id=${2:-}
api_image=${3:-}
web_image=${4:-}
ops_image=${5:-}
version=${6:-}
schema_compatible=${7:-false}

case "$environment_name" in
  staging|production) ;;
  *) echo "Environment must be staging or production." >&2; exit 2 ;;
esac
printf '%s\n' "$release_id" | grep -Eq '^[a-f0-9]{40}$' || {
  echo "Release id must be a full Git commit SHA." >&2
  exit 2
}
printf '%s\n' "$version" | grep -Eq '^[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.-]+)?$' || {
  echo "Version is invalid: $version" >&2
  exit 2
}
case "$schema_compatible" in true|false) ;; *) exit 2 ;; esac

deploy_host=${APPOINTMENTCRM_DEPLOY_HOST:?deploy host is required}
deploy_user=${APPOINTMENTCRM_DEPLOY_USER:?deploy user is required}
deploy_root=${APPOINTMENTCRM_DEPLOY_ROOT:-/opt/appointment-crm}
printf '%s\n' "$deploy_root" | grep -Eq '^/[A-Za-z0-9._/-]+$' || {
  echo "Deploy root contains unsupported characters." >&2
  exit 2
}
case "$deploy_root" in *..*) exit 2 ;; esac

temporary_directory=$(mktemp -d)
trap 'rm -rf "$temporary_directory"' EXIT HUP INT TERM
mkdir -p "$temporary_directory/bundle"
mkdir -p "$temporary_directory/bundle/deploy/proxy"
cp \
  deploy/compose.operations.yaml \
  deploy/compose.release.yaml \
  deploy/release.sh \
  deploy/smoke.sh \
  "$temporary_directory/bundle/deploy/"
cp deploy/proxy/Caddyfile "$temporary_directory/bundle/deploy/proxy/Caddyfile"
printf '%s\n' \
  "APPOINTMENTCRM_RELEASE_ID=$release_id" \
  "APPOINTMENTCRM_VERSION=$version" \
  "APPOINTMENTCRM_API_IMAGE=$api_image" \
  "APPOINTMENTCRM_WEB_IMAGE=$web_image" \
  "APPOINTMENTCRM_OPS_IMAGE=$ops_image" \
  "APPOINTMENTCRM_SCHEMA_COMPATIBLE_WITH_PREVIOUS=$schema_compatible" \
  > "$temporary_directory/bundle/deploy/release.env"
tar -C "$temporary_directory/bundle" -czf "$temporary_directory/release.tar.gz" .

target="$deploy_user@$deploy_host"
remote_release="$deploy_root/releases/$release_id"
ssh "$target" sh -s -- "$remote_release" <<'REMOTE_PREPARE'
set -eu
remote_release=$1
mkdir -p "$remote_release"
REMOTE_PREPARE
scp "$temporary_directory/release.tar.gz" "$target:$remote_release/release.tar.gz"
ssh "$target" sh -s -- "$deploy_root" "$release_id" "$environment_name" <<'REMOTE_DEPLOY'
set -eu
deploy_root=$1
release_id=$2
environment_name=$3
remote_release="$deploy_root/releases/$release_id"
tar -C "$remote_release" -xzf "$remote_release/release.tar.gz"
rm -f "$remote_release/release.tar.gz"
runtime_file="$deploy_root/shared/$environment_name/runtime.env"
state_directory="$deploy_root/environments/$environment_name/state"
test -f "$runtime_file"
current_release="$deploy_root/environments/$environment_name/current"
if [ "$environment_name" = production ] \
  && [ -x "$current_release/deploy/release.sh" ] \
  && [ -f "$state_directory/current.env" ]; then
  "$current_release/deploy/release.sh" backup \
    "$runtime_file" \
    "$state_directory/current.env" \
    "$state_directory"
fi
"$remote_release/deploy/release.sh" promote \
  "$runtime_file" \
  "$remote_release/deploy/release.env" \
  "$state_directory"
mkdir -p "$deploy_root/environments/$environment_name"
ln -sfn "$remote_release" "$deploy_root/environments/$environment_name/current"
REMOTE_DEPLOY
