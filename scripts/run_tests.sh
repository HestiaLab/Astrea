#!/usr/bin/env bash
# Astrea 项目验证入口（Hermes 识别的 canonical verify command）。
# 封装：build + test + format + SRS 一致性，全绿才算通过。
set -uo pipefail
cd "$(dirname "$0")/.." || exit 1

echo "== 1/5 build =="
dotnet build Astrea.sln

echo "== 2/5 test =="
dotnet test Astrea.sln --no-build

echo "== 3/5 format =="
dotnet format Astrea.sln --verify-no-changes --no-restore

echo "== 4/5 SRS =="
python3 scripts/verify_srs_consistency.py

echo "== 5/5 JS 单元测试 =="
node --test tests/Astrea.JsTests/astrea-tree.test.mjs
