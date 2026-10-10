#!/usr/bin/env python3
"""Compile the Android bridge directly and stage the pinned UPM Maven repository."""
from pathlib import Path
import hashlib
import json
import sys
import tempfile

root = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(root / 'scripts/bazel'))
from sdk import android_build, copy_maven, publish_tree
from bridge_artifacts import publish_maven

artifact, manifest = android_build()
pin = json.loads((root / 'NATIVE-PINS.json').read_text())['android']['revision']
with tempfile.TemporaryDirectory(prefix='upm-maven-', dir=root / '.native') as temporary:
    maven = Path(temporary) / 'maven'
    copy_maven(manifest, maven)
    publish_maven(artifact, maven / 'ai/nuxie/nuxie-unity-bridge/0.2.0', '0.2.0', pin)
    publish_tree(maven, root / 'Artifacts~/maven')
checksums = {str(path.relative_to(root / 'Artifacts~')): hashlib.sha256(path.read_bytes()).hexdigest()
             for path in sorted((root / 'Artifacts~/maven').rglob('*')) if path.is_file()}
(root / 'Artifacts~/checksums.json').write_text(json.dumps(checksums, indent=2) + '\n')
print('Prepared direct Bazel Android products for UPM packing.')
