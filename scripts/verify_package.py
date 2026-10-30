#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Pack Astrea.Components and verify consumption from clean Blazor Web App and WebAssembly apps.

This is a release-engineering check, not a unit test. It uses the local package
source and a temporary NuGet cache so it does not accidentally consume a cached
Astrea.Components package.

Usage: python scripts/verify_package.py
"""

import os
import re
import shutil
import signal
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
PACKAGES = ROOT / "artifacts" / "packages"
PROJECT = ROOT / "src" / "Astrea.Components" / "Astrea.Components.csproj"
STATIC_ASSETS = (
    "astrea-tree.js",
    "astrea-tree.css",
    "astrea-resource-table.js",
    "astrea-resource-table.css",
)


def fail(message: str) -> None:
    print(f"PACKAGE PROBE FAILED: {message}")
    raise SystemExit(1)


def run(command: list[str], cwd: Path = ROOT, env: dict[str, str] | None = None) -> None:
    print("+", " ".join(str(part) for part in command))
    subprocess.run(command, cwd=cwd, env=env, check=True)


def project_version() -> str:
    match = re.search(r"<Version>([^<]+)</Version>", PROJECT.read_text(encoding="utf-8"))
    if not match:
        fail("Astrea.Components.csproj has no <Version>")
    return match.group(1)


def pack(version: str) -> Path:
    shutil.rmtree(PACKAGES, ignore_errors=True)
    PACKAGES.mkdir(parents=True, exist_ok=True)
    run(["dotnet", "pack", str(PROJECT), "-c", "Release", "-o", str(PACKAGES), "--nologo"])

    package = PACKAGES / f"Astrea.Components.{version}.nupkg"
    if not package.exists():
        fail(f"expected package not found: {package}")

    required = {
        "lib/net10.0/Astrea.Components.dll",
        "README.md",
        "LICENSE",
        "CHANGELOG.md",
        *(f"staticwebassets/{asset}" for asset in STATIC_ASSETS),
    }
    with zipfile.ZipFile(package) as archive:
        names = set(archive.namelist())
        missing = required - names
        if missing:
            fail(f"package is missing entries: {sorted(missing)}")
        legacy = sorted(name for name in names if name.startswith("lib/net6.0/"))
        if legacy:
            fail(f"package must not contain net6.0 assets: {legacy}")
        nuspec = archive.read("Astrea.Components.nuspec").decode("utf-8")
        for expected in (
            f"<version>{version}</version>",
            "<license type=\"expression\">MIT</license>",
            "<readme>README.md</readme>",
            "HestiaLab/Astrea",
        ):
            if expected not in nuspec:
                fail(f"nuspec is missing {expected!r}")

    print(f"Package contents verified: {package}")
    return package


def write_nuget_config(folder: Path, package_dir: Path) -> None:
    (folder / "nuget.config").write_text(
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        "<configuration>\n"
        "  <packageSources>\n"
        "    <clear />\n"
        f"    <add key=\"local-astrea\" value=\"{escape(package_dir.as_posix())}\" />\n"
        "    <add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />\n"
        "  </packageSources>\n"
        "</configuration>\n",
        encoding="utf-8",
    )


def replace_project_reference(csproj: Path, version: str) -> None:
    text = csproj.read_text(encoding="utf-8")
    text = re.sub(r"\s*<ProjectReference[^>]*/>", "", text)
    text = text.replace(
        "</Project>",
        "  <ItemGroup>\n"
        f"    <PackageReference Include=\"Astrea.Components\" Version=\"{version}\" />\n"
        "  </ItemGroup>\n"
        "</Project>",
    )
    csproj.write_text(text, encoding="utf-8")


def copy_app(source: Path, destination: Path, version: str, package_dir: Path) -> Path:
    shutil.copytree(source, destination)
    csproj = destination / f"{source.name}.csproj"
    if not csproj.exists():
        fail(f"copied app has no project file: {csproj}")
    replace_project_reference(csproj, version)
    write_nuget_config(destination, package_dir)
    shutil.copy2(ROOT / "global.json", destination / "global.json")
    return csproj


def free_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


def wait_for_http(url: str, process: subprocess.Popen, timeout: float = 40, log_path: Path | None = None) -> str:
    deadline = time.time() + timeout
    while time.time() < deadline:
        if process.poll() is not None:
            details = ""
            if log_path is not None and log_path.exists():
                details = "\n" + log_path.read_text(encoding="utf-8", errors="replace")[-4000:]
            fail(f"server process exited with code {process.returncode}{details}")
        try:
            with urllib.request.urlopen(url, timeout=2) as response:
                if response.status == 200:
                    return response.read().decode("utf-8")
        except OSError:
            time.sleep(0.5)
    fail(f"server did not become ready: {url}")
    return ""


def stop_process(process: subprocess.Popen) -> None:
    if process.poll() is not None:
        return
    if os.name == "nt":
        subprocess.run(
            ["taskkill", "/F", "/T", "/PID", str(process.pid)],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=False,
        )
    else:
        os.killpg(os.getpgid(process.pid), signal.SIGTERM)
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()


def verify_server_app(temp: Path, version: str, package_dir: Path, nuget_cache: Path) -> None:
    app = temp / "server"
    csproj = copy_app(ROOT / "tests" / "Astrea.ServerSmoke", app, version, package_dir)
    env = {**os.environ, "NUGET_PACKAGES": str(nuget_cache), "ASPNETCORE_ENVIRONMENT": "Development"}
    run(["dotnet", "build", str(csproj), "-c", "Debug", "--nologo"], cwd=app, env=env)

    port = free_port()
    url = f"http://127.0.0.1:{port}"
    log_path = temp / "server-probe.log"
    with log_path.open("w", encoding="utf-8") as log_file:
        process = subprocess.Popen(
            ["dotnet", "run", "--project", str(csproj), "--no-build", "-c", "Debug", "--urls", url],
            cwd=app,
            env=env,
            stdout=log_file,
            stderr=subprocess.STDOUT,
            start_new_session=os.name != "nt",
        )
        try:
            html = wait_for_http(url + "/", process, log_path=log_path)
            if "server-tree" not in html or "Server root" not in html:
                fail("server app did not render the packaged Tree component")
            for asset in ("astrea-tree.css", "astrea-resource-table.js"):
                with urllib.request.urlopen(f"{url}/_content/Astrea.Components/{asset}", timeout=5) as response:
                    if response.status != 200 or not response.read(1):
                        fail(f"server app did not serve packaged asset {asset}")
        finally:
            stop_process(process)

    print("Blazor Web App package consumption verified")


def verify_wasm_app(temp: Path, version: str, package_dir: Path, nuget_cache: Path) -> None:
    app = temp / "wasm"
    csproj = copy_app(ROOT / "src" / "Astrea.Demo", app, version, package_dir)
    env = {**os.environ, "NUGET_PACKAGES": str(nuget_cache)}
    publish = temp / "wasm-publish"
    run(["dotnet", "publish", str(csproj), "-c", "Release", "-o", str(publish), "--nologo"], cwd=app, env=env)

    for asset in STATIC_ASSETS:
        packaged = publish / "wwwroot" / "_content" / "Astrea.Components" / asset
        if not packaged.exists() or packaged.stat().st_size == 0:
            fail(f"WebAssembly publish is missing packaged asset {packaged}")

    print("Blazor WebAssembly package consumption verified")


def main() -> None:
    version = project_version()
    package = pack(version)
    package_dir = package.parent

    with tempfile.TemporaryDirectory(prefix="astrea-package-probe-") as temp_name:
        temp = Path(temp_name)
        nuget_cache = temp / "nuget-packages"
        nuget_cache.mkdir()
        verify_server_app(temp, version, package_dir, nuget_cache)
        verify_wasm_app(temp, version, package_dir, nuget_cache)

    print(f"PACKAGE PROBE PASSED（Astrea.Components {version}）")


if __name__ == "__main__":
    main()
