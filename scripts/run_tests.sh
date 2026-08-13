#!/usr/bin/env bash
# Astrea 项目验证入口（Hermes 识别的 canonical verify command）。
# 封装：build + test + format + SRS 一致性，全绿才算通过。
set -uo pipefail
cd "$(dirname "$0")/.." || exit 1

echo "== 1/4 build =="
dotnet build Astrea.sln

echo "== 2/4 test =="
dotnet test Astrea.sln --no-build

echo "== 3/4 format =="
dotnet format Astrea.sln --verify-no-changes --no-restore

echo "== 4/4 SRS =="
python3 scripts/verify_srs_consistency.py
