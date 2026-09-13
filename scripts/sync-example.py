#!/usr/bin/env python3
"""Samples~/SdkLab is canonical; refresh the example's imported sample files."""
from pathlib import Path
import shutil
root = Path(__file__).resolve().parent.parent
source = root / 'Samples~/SdkLab'
target = root / 'ExampleProject/Assets/SdkLab'
shutil.copytree(source, target, dirs_exist_ok=True)
