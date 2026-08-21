#!/usr/bin/env bash

set -euo pipefail

repo="${1:-aaliakinci/appointment_crm}"

command -v gh >/dev/null
gh auth status --hostname github.com >/dev/null

label() {
  local name="$1"
  local color="$2"
  local description="$3"

  gh label create "$name" \
    --repo "$repo" \
    --color "$color" \
    --description "$description" \
    --force
}

milestone() {
  local title="$1"
  local description="$2"
  local number

  number="$(
    gh api --paginate "repos/${repo}/milestones?state=all&per_page=100" \
      --jq ".[] | select(.title == \"${title}\") | .number" \
      | head -n 1
  )"

  if [[ -z "$number" ]]; then
    gh api --method POST "repos/${repo}/milestones" \
      -f title="$title" \
      -f description="$description" \
      --silent
  else
    gh api --method PATCH "repos/${repo}/milestones/${number}" \
      -f title="$title" \
      -f description="$description" \
      -f state="open" \
      --silent
  fi
}

gh repo edit "$repo" \
  --description "Multi-tenant appointment CRM built with ASP.NET Core, PostgreSQL, React, and Lily UI." \
  --enable-issues \
  --add-topic appointment-crm \
  --add-topic aspnet-core \
  --add-topic multi-tenant \
  --add-topic postgresql \
  --add-topic react \
  --add-topic typescript

label "phase:0" "5319e7" "Repository bootstrap and decisions"
label "phase:1" "5319e7" "Working technical skeleton"
label "phase:2" "5319e7" "Identity, authorization, and tenancy"
label "phase:3" "5319e7" "Customer, service, and employee modules"
label "phase:4" "5319e7" "Working hours and availability"
label "phase:5" "5319e7" "Appointment lifecycle and concurrency"
label "phase:6" "5319e7" "Operations UX and reporting"
label "phase:7" "5319e7" "Async work, observability, and hardening"
label "phase:8" "5319e7" "Release quality gate"
label "phase:9" "5319e7" "Deployment and demo operations"
label "phase:10" "5319e7" "Portfolio packaging and v1.0"

label "area:api" "1d76db" "ASP.NET Core API and contracts"
label "area:web" "1d76db" "React frontend and Lily UI"
label "area:data" "1d76db" "PostgreSQL, EF Core, migrations, and Redis"
label "area:identity" "1d76db" "Authentication, sessions, and permissions"
label "area:tenancy" "b60205" "Tenant boundary and isolation"
label "area:appointments" "b60205" "Availability, lifecycle, and concurrency"
label "area:platform" "1d76db" "CI/CD, containers, deployment, and telemetry"
label "area:docs" "1d76db" "Documentation and portfolio material"

label "type:feature" "0e8a16" "New product behavior"
label "type:bug" "d73a4a" "Reproducible defect"
label "type:test" "0e8a16" "Test evidence or infrastructure"
label "type:docs" "0075ca" "Documentation-only outcome"
label "type:chore" "cfd3d7" "Maintenance or repository work"
label "type:spike" "fbca04" "Time-boxed uncertainty reduction"
label "priority:p0" "b60205" "Release blocker"
label "priority:p1" "d93f0b" "Required for portfolio v1"
label "priority:p2" "fbca04" "v1.1 candidate or time permitting"
label "security" "b60205" "Security-sensitive; report unsafe details privately"

milestone "v0.1.0 — Working skeleton" "One-command local startup and green CI."
milestone "v0.2.0 — Secure tenant foundation" "Tenant-isolation and authorization tests."
milestone "v0.3.0 — Business setup" "Customer, service, employee, and availability vertical slices."
milestone "v0.4.0 — Appointment core" "Database-proven concurrency conflict behavior."
milestone "v0.5.0 — Feature complete" "Operational UI, reporting, audit, outbox, and E2E smoke."
milestone "v1.0.0 — Portfolio release" "Live demo, operational evidence, and complete documentation."

gh api --method PUT "repos/${repo}/branches/main/protection" --input - --silent <<'JSON'
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["Backend", "Frontend", "Containers"]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": {
    "dismiss_stale_reviews": false,
    "require_code_owner_reviews": false,
    "required_approving_review_count": 0,
    "require_last_push_approval": false
  },
  "restrictions": null,
  "required_linear_history": true,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "block_creations": false,
  "required_conversation_resolution": true,
  "lock_branch": false,
  "allow_fork_syncing": true
}
JSON

printf 'GitHub governance synchronized for %s\n' "$repo"
