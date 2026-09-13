#!/usr/bin/env python3
"""Local Unity SDK PR gate. Requires a licensed Editor and both mobile modules."""
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parent.parent
for command in [
    ['dotnet', 'test', 'dotnet/Nuxie.Unity.slnx', '--nologo'],
    ['dotnet', 'build', 'dotnet/Nuxie.Unity.Core/Nuxie.Unity.Core.csproj', '--nologo'],
    ['python3', 'scripts/prepare-native.py'],
    ['python3', 'scripts/check-ios.py'],
    ['python3', 'scripts/check-unity.py', '--packed'],
    ['python3', 'scripts/pack.py'],
]:
    print('+ ' + ' '.join(command), flush=True)
    subprocess.run(command, cwd=root, check=True)
