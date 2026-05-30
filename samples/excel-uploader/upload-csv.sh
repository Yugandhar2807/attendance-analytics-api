#!/usr/bin/env bash
# =============================================================================
# upload-csv.sh — Submit a CSV of punches to the API via Mode 2 (UI upload).
#
# Usage:
#   ./upload-csv.sh http://localhost:5080 tenant-a ../synthetic-data/tenant-a-punches.csv
# =============================================================================
set -euo pipefail

if [ "$#" -ne 3 ]; then
    echo "Usage: $0 <api-base> <tenant-id> <csv-path>"
    exit 1
fi

API_BASE="$1"
TENANT_ID="$2"
CSV_PATH="$3"

if [ ! -f "$CSV_PATH" ]; then
    echo "CSV not found: $CSV_PATH"
    exit 1
fi

echo "Uploading $CSV_PATH to tenant $TENANT_ID..."

curl -fS -X POST \
    -H "X-Tenant-Id: $TENANT_ID" \
    -F "file=@${CSV_PATH};type=text/csv" \
    "${API_BASE}/api/v1/punches/batch" \
    | jq .
