#!/usr/bin/env bash
set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
JUNIT_FILE="$REPO_ROOT/bureau-junit.xml"

dotnet test "$REPO_ROOT/tests/AnotherJsonLib.Tests/AnotherJsonLib.Tests.csproj" \
  --logger "junit;LogFilePath=$JUNIT_FILE"

if [ ! -f "$JUNIT_FILE" ]; then
  echo "ERROR: bureau-junit.xml not found at $JUNIT_FILE" >&2
  exit 1
fi

# JunitXml.TestLogger emits the C# method name; patch each EARS probe entry
# to its [ID] display name so the coverage gate can match the bracketed token.
sed -i 's|name="LibraryAssemblyLoadsAndJsonObjectRoundTrips"|name="[E-01] library assembly loads and a JsonObject round-trips"|g' "$JUNIT_FILE"

echo "OK: bureau-junit.xml exists at $JUNIT_FILE"
