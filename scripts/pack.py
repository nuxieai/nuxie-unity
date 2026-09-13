#!/usr/bin/env python3
"""Create a UPM archive containing the verified native Maven repository."""
from pathlib import Path
import hashlib
import json
import tarfile
import shutil

root = Path(__file__).resolve().parent.parent
artifacts = root / 'Artifacts~'
manifest = json.loads((root / 'package.json').read_text())
checksums = json.loads((artifacts / 'checksums.json').read_text())
actual = {str(p.relative_to(artifacts)) for p in (artifacts / 'maven').rglob('*') if p.is_file()}
if actual != set(checksums):
    raise SystemExit('Native artifacts differ from their manifest. Run scripts/prepare-native.py.')
for name, expected in checksums.items():
    path = artifacts / name
    if not path.resolve().is_relative_to(artifacts.resolve()) or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
        raise SystemExit('Native artifact integrity failure: ' + name)
if not any(name.endswith('.aar') and '/nuxie-unity-bridge/' in name for name in actual):
    raise SystemExit('Missing native bridge AAR')
paths = []
for name in ['package.json', 'NATIVE-PINS.json', 'README.md', 'Runtime', 'Editor', 'Tests', 'Samples~', 'Documentation~', 'Artifacts~']:
    path = root / name
    if not path.exists():
        raise SystemExit('Missing package content: ' + name)
    paths.extend([path, *path.rglob('*')] if path.is_dir() else [path])
    meta = Path(str(path) + '.meta')
    if meta.exists(): paths.append(meta)
for path in paths:
    if path.is_symlink(): raise SystemExit('Package symlinks are not supported: ' + str(path))
    relative = path.relative_to(root)
    if relative.parts[0] in ('Runtime', 'Editor', 'Tests') and path.suffix != '.meta' and not Path(str(path) + '.meta').exists():
        raise SystemExit('Missing Unity metadata: ' + str(relative))
staging = root / '.native/package'
if staging.exists(): shutil.rmtree(staging)
for path in sorted(set(paths)):
    target = staging / path.relative_to(root)
    if path.is_dir(): target.mkdir(parents=True, exist_ok=True)
    else:
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path,target)
output = root / (manifest['name'] + '-' + manifest['version'] + '.tgz')
with tarfile.open(output, 'w:gz') as archive:
    for path in sorted(set(paths)):
        archive.add(path, arcname='package/' + str(path.relative_to(root)), recursive=False)
print(output)
print('SHA256 ' + hashlib.sha256(output.read_bytes()).hexdigest())
