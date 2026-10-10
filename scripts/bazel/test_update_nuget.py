"""Dependency-graph behavior against independently restored lock records."""

import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("update_nuget", Path(__file__).with_name("update-nuget.py"))
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)


def lock(framework, version="1.0.0", content_hash="abc", dependencies=None):
    return {"dependencies": {framework: {"Package": {
        "type": "Direct", "resolved": version, "contentHash": content_hash,
        "dependencies": dependencies or {},
    }}}}


class NugetGraphTests(unittest.TestCase):
    def test_preserves_framework_specific_dependency_edges(self):
        record, = generator.packages([lock("netstandard2.1"), lock("net8.0", dependencies={"Dependency": "2.0.0"})])
        self.assertEqual(record["dependencies"], {"netstandard2.1": [], "net8.0": ["Dependency"]})
        self.assertEqual(record["sha512"], "sha512-abc")

    def test_nuget_framework_names_use_bazel_target_framework_monikers(self):
        record, = generator.packages([lock(".NETStandard,Version=v2.1"), lock(".NETCoreApp,Version=v8.0")])
        self.assertEqual(set(record["dependencies"]), {"netstandard2.1", "net8.0"})

    def test_conflicting_version_or_hash_requires_explicit_resolution(self):
        for conflicting in (lock("net8.0", version="2.0.0"), lock("net8.0", content_hash="different")):
            with self.subTest(conflicting=conflicting), self.assertRaisesRegex(ValueError, "Conflicting"):
                generator.packages([lock("netstandard2.1"), conflicting])

    def test_archive_hash_is_distinct_but_tied_to_locked_identity(self):
        records = generator.packages([lock("net8.0")])
        generator.apply_archive_records(records, {"package": {"version": "1.0.0", "contentHash": "abc", "sha512": "sha512-signed-zip"}})
        self.assertEqual(records[0]["sha512"], "sha512-signed-zip")
        for archives in ({}, {"package": {"version": "1.0.0", "contentHash": "different", "sha512": "sha512-signed-zip"}}):
            with self.subTest(archives=archives), self.assertRaises(ValueError):
                generator.apply_archive_records(generator.packages([lock("net8.0")]), archives)

    def test_source_project_edges_are_not_downloaded_as_packages(self):
        self.assertEqual(generator.packages([{"dependencies": {"net8.0": {"Source": {"type": "Project"}}}}]), [])
