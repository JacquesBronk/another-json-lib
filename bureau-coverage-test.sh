#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_ROOT"
rm -f bureau-junit.trx bureau-junit.xml
dotnet test tests/AnotherJsonLib.Tests/AnotherJsonLib.Tests.csproj --logger "trx;LogFileName=bureau-junit.trx" --results-directory .
trx2junit bureau-junit.trx
test -f bureau-junit.xml
grep -q '\[E-01\]' bureau-junit.xml
echo "OK: bureau-junit.xml has [E-01] natively (no rename)"
