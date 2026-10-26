#!/usr/bin/env python3
"""Execute the shipped Swift bridge against genuine pinned native SDK products."""
from pathlib import Path
import subprocess
import sys
import sys
from pool import dispatch
status = dispatch('check-ios', sys.argv[1:])
if status is not None: raise SystemExit(status)

root = Path(__file__).resolve().parent.parent
subprocess.run([sys.executable, str(root / 'scripts/bazel/ios_simulator.py'), '--',
                sys.executable, str(root / 'scripts/bazel/sdk.py'), 'test-ios'], cwd=root, check=True)
