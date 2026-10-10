#!/usr/bin/env python3
"""Translate the independently restored .NET package locks into Bazel imports."""

import argparse
import json
from pathlib import Path
import subprocess


def packages(locks):
    merged = {}
    for lock in locks:
        for framework, dependencies in lock["dependencies"].items():
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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--restore", action="store_true", help="Refresh the owning .NET/NuGet locks before emitting the graph")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    if args.check and args.restore:
        parser.error("--check and --restore cannot be combined")
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
