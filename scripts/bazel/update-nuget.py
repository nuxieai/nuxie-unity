#!/usr/bin/env python3
"""Translate the independently restored .NET package locks into Bazel imports."""

import argparse
import base64
import hashlib
import os
import json
import re
from pathlib import Path
import subprocess


def target_framework(framework):
    match = re.fullmatch(r"\.(NETStandard|NETCoreApp),Version=v([0-9]+\.[0-9]+)", framework)
    if match:
        return ("netstandard" if match[1] == "NETStandard" else "net") + match[2]
    return framework


def packages(locks):
    merged = {}
    for lock in locks:
        for framework, dependencies in lock["dependencies"].items():
            framework = target_framework(framework)
            for name, package in dependencies.items():
                if package["type"] == "Project":
                    continue
                key = name.lower()
                version = package["resolved"]
                integrity = "sha512-" + package["contentHash"]
                record = merged.setdefault(key, {
                    "name": name, "id": name, "version": version,
                    "sha512": integrity,
                    "sources": ["https://api.nuget.org/v3/index.json"],
                    "dependencies": {}, "targeting_pack_overrides": [], "framework_list": [],
                })
                if (record["version"], record["sha512"]) != (version, integrity):
                    raise ValueError("Conflicting locked NuGet identity: " + name)
                record["dependencies"][framework] = sorted(package.get("dependencies", {}))
    return [merged[name] for name in sorted(merged)]



def archive_records(records, package_root):
    """Use NuGet's own content-hash calculation as the oracle for raw ZIPs."""
    result = {}
    for record in records:
        name, version = record["name"].lower(), record["version"]
        archive = package_root / name / version / (name + "." + version + ".nupkg")
        verification = subprocess.run(["dotnet", "nuget", "verify", str(archive), "--all"], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT).stdout
        # Certificate policy can reject historic signed packages even when
        # their immutable content equals the independent restored lock. The
        # exact content hash, rather than certificate validity, is this seam.
        expected = record["sha512"].removeprefix("sha512-")
        if "Content hash: " + expected not in verification.splitlines():
            raise ValueError("NuGet content verification differs from the restored lock: " + name)
        result[name] = {"version": version, "contentHash": expected,
                        "sha512": "sha512-" + base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode()}
    return result


def apply_archive_records(records, archives):
    if set(archives) != {record["name"].lower() for record in records}:
        raise ValueError("NuGet archive inventory differs from the package locks; refresh with --update-archives")
    for record in records:
        archive = archives[record["name"].lower()]
        if (archive["version"], "sha512-" + archive["contentHash"]) != (record["version"], record["sha512"]):
            raise ValueError("NuGet archive identity differs from the restored lock: " + record["name"])
        record["sha512"] = archive["sha512"]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--restore", action="store_true", help="Refresh the owning .NET/NuGet locks before emitting the graph")
    parser.add_argument("--update-archives", action="store_true", help="Record ZIP checksums using dotnet 10+ content-hash verification")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    if args.check and (args.restore or args.update_archives):
        parser.error("--check cannot be combined with refresh options")
    if args.restore:
        projects = [root / "dotnet" / project / (project + ".csproj")
                    for project in ("Nuxie.Unity.Core", "Nuxie.Unity.Core.Tests")]
        projects.append(root / "scripts/bazel/Runner.csproj")
        for project in projects:
            subprocess.run(["dotnet", "restore", str(project), "--use-lock-file", "--force-evaluate", "--nologo"], cwd=root, check=True)
    locks = [json.loads((root / "dotnet" / project / "packages.lock.json").read_text())
             for project in ("Nuxie.Unity.Core", "Nuxie.Unity.Core.Tests")]
    locks.append(json.loads((root / "scripts/bazel/runner.packages.lock.json").read_text()))
    records = packages(locks)
    archive_lock = root / "scripts/bazel/nuget-archives.json"
    if args.update_archives or args.restore:
        package_root = Path(os.environ.get("NUGET_PACKAGES", str(Path.home() / ".nuget/packages")))
        archive_lock.write_text(json.dumps(archive_records(records, package_root), indent=2) + "\n")
    apply_archive_records(records, json.loads(archive_lock.read_text()))
    for record in records:
        for framework, deps in record["dependencies"].items():
            for dep in deps:
                if dep.lower() not in {p["name"].lower() for p in records}:
                    raise ValueError(f"Missing {framework} transitive package: {dep}")
    contents = """# Generated from the owning .NET packages.lock.json files.
load("@rules_dotnet//dotnet:defs.bzl", "nuget_repo")

def _impl(_ctx):
    nuget_repo(name = "unity_nuget", packages = %s)

unity_nuget = module_extension(implementation = _impl)
""" % json.dumps(records, indent=4)
    output = root / "scripts/bazel/nuget.bzl"
    if args.check:
        if not output.is_file() or output.read_text() != contents:
            raise SystemExit("NuGet Bazel graph is stale; run scripts/bazel/update-nuget.py")
    else:
        output.write_text(contents)
    print(f"NuGet Bazel graph: {len(records)} locked packages")


if __name__ == "__main__":
    main()
