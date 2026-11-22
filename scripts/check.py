#!/usr/bin/env python3
"""Readiness phases retain every oracle and place only engine/Apple work on a Mac."""
import argparse
from pathlib import Path
import subprocess

from pool import CHECK_PHASES, dispatch

ROOT = Path(__file__).resolve().parent.parent


def check(phase=None, run=None):
    if run is None:
        run = lambda command: subprocess.run(command, cwd=ROOT, check=True)
    commands = {
        'portable': [['python3', 'scripts/bazel/sdk.py', 'test'], ['python3', 'scripts/bazel/sdk.py', 'build']],
        'android': [['python3', 'scripts/prepare-native.py']],
        'ios': [['python3', 'scripts/check-ios.py']],
        # Packing requires the Android Maven repository in this request's workspace.
        'engine': [['python3', 'scripts/prepare-native.py'], ['python3', 'scripts/check-unity.py', '--packed'], ['python3', 'scripts/pack.py']],
    }
    for selected in ([phase] if phase else CHECK_PHASES):
        selected_commands = commands[selected]
        if selected == 'engine' and phase is None: selected_commands = selected_commands[1:]
        for command in selected_commands:
            print('+ ' + ' '.join(command), flush=True)
            run(command)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--phase', choices=CHECK_PHASES)
    phase = parser.parse_args().phase
    status = dispatch('check', ['--phase', phase] if phase else [])
    if status is not None: return status
    check(phase)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
