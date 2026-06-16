#!/usr/bin/env bash
set -euo pipefail

dotnet publish ModelTest/ModelTest.csproj \
  -c Release \
  -r win-x86 \
  --self-contained false \
  -o publish/win-x86-framework
