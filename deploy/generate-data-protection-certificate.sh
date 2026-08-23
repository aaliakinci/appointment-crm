#!/usr/bin/env sh
set -eu

target_directory=${1:-}
password_file=${2:-}
test -n "$target_directory" && test -n "$password_file" || {
  echo "Usage: generate-data-protection-certificate.sh <secret-directory> <password-file>" >&2
  exit 2
}
test -s "$password_file" || {
  echo "The certificate password file is missing or empty." >&2
  exit 1
}

certificate="$target_directory/data-protection.pfx"
test ! -e "$certificate" || {
  echo "Refusing to overwrite existing certificate: $certificate" >&2
  exit 1
}

umask 077
mkdir -p "$target_directory"
temporary_directory=$(mktemp -d)
trap 'rm -rf "$temporary_directory"' EXIT HUP INT TERM

openssl req -x509 -newkey rsa:3072 -sha256 -nodes \
  -subj "/CN=Appointment CRM Data Protection" \
  -days 3650 \
  -keyout "$temporary_directory/key.pem" \
  -out "$temporary_directory/certificate.pem"
openssl pkcs12 -export \
  -inkey "$temporary_directory/key.pem" \
  -in "$temporary_directory/certificate.pem" \
  -out "$certificate" \
  -passout "file:$password_file"

chmod 600 "$certificate"
echo "$certificate"
