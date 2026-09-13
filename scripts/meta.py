#!/usr/bin/env python3
"""Create stable GUIDs for new package assets; preserve all existing GUIDs."""
from pathlib import Path
import uuid
root = Path(__file__).resolve().parent.parent
for directory in ['Runtime','Editor','Tests','Samples~/SdkLab','ExampleProject/Assets']:
    base = root / directory
    if not base.exists(): continue
    for path in [base, *base.rglob('*')]:
        if path.suffix == '.meta' or any(p.startswith('.') for p in path.relative_to(root).parts): continue
        meta = Path(str(path)+'.meta')
        if meta.exists(): continue
        guid = uuid.uuid5(uuid.NAMESPACE_URL, 'https://nuxie.ai/unity/'+str(path.relative_to(root))).hex
        text = 'fileFormatVersion: 2\nguid: '+guid+'\n'
        if path.is_dir(): text += 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        elif path.suffix == '.cs': text += 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        else: text += 'DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        meta.write_text('\n'.join(line.rstrip() for line in text.splitlines()) + '\n')
