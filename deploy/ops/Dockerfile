FROM alpine:3.24.1@sha256:28bd5fe8b56d1bd048e5babf5b10710ebe0bae67db86916198a6eec434943f8b

ARG VERSION=0.0.0-local
ARG VCS_REF=unknown

LABEL org.opencontainers.image.title="Appointment CRM Database Operations" \
    org.opencontainers.image.version="$VERSION" \
    org.opencontainers.image.revision="$VCS_REF" \
    org.opencontainers.image.source="https://github.com/aaliakinci/appointment_crm"

RUN apk add --no-cache \
    ca-certificates=20260611-r0 \
    openssl=3.5.7-r0 \
    postgresql18-client=18.6-r0 \
    && addgroup -S -g 1654 app \
    && adduser -S -D -H -u 1654 -G app app

COPY --chmod=0555 deploy/ops/backup.sh /usr/local/bin/appointment-crm-backup
COPY --chmod=0555 deploy/ops/restore-rehearsal.sh /usr/local/bin/appointment-crm-restore-rehearsal

USER 1654:1654
