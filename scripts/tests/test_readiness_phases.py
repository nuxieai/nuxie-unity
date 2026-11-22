import importlib.util
from pathlib import Path
import sys
import unittest

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'scripts'))
spec=importlib.util.spec_from_file_location('unity_readiness',ROOT/'scripts/check.py')
check=importlib.util.module_from_spec(spec);spec.loader.exec_module(check)

class ReadinessPhaseTests(unittest.TestCase):
    def test_portable_managed_checks_do_not_start_editor_or_mobile_builds(self):
        calls=[];check.check('portable',run=calls.append)
        self.assertEqual(calls,[['python3','scripts/bazel/sdk.py','test'],['python3','scripts/bazel/sdk.py','build']])

    def test_default_local_gate_retains_every_original_command_once(self):
        calls=[];check.check(run=calls.append)
        self.assertEqual(calls,[['python3','scripts/bazel/sdk.py','test'],['python3','scripts/bazel/sdk.py','build'],['python3','scripts/prepare-native.py'],['python3','scripts/check-ios.py'],['python3','scripts/check-unity.py','--packed'],['python3','scripts/pack.py']])

    def test_independently_placed_editor_phase_prepares_its_package_input(self):
        calls=[];check.check('engine',run=calls.append)
        self.assertEqual(calls,[['python3','scripts/prepare-native.py'],['python3','scripts/check-unity.py','--packed'],['python3','scripts/pack.py']])
