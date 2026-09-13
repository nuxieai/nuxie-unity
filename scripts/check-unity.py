#!/usr/bin/env python3
"""Run real Unity tests and IL2CPP exports; retain evidence under .native/unity-check."""
from pathlib import Path
import argparse
import os
import json
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser()
parser.add_argument('--editor', default=os.environ.get('UNITY_EDITOR'))
parser.add_argument('--tests-only', action='store_true')
parser.add_argument('--packed', action='store_true', help='Resolve the actual UPM tarball while checking; restore the source manifest afterward')
args = parser.parse_args()
version = (root / 'ExampleProject/ProjectSettings/ProjectVersion.txt').read_text().split(':', 1)[1].strip().splitlines()[0]
editor = Path(args.editor) if args.editor else Path('/Applications/Unity/Hub/Editor') / version / 'Unity.app/Contents/MacOS/Unity'
if editor.suffix == '.app': editor = editor / 'Contents/MacOS/Unity'
if not editor.is_file(): raise SystemExit('Set UNITY_EDITOR to a licensed Unity Editor executable.')
evidence = root / '.native/unity-check'
evidence.mkdir(parents=True, exist_ok=True)
subprocess.run(['python3', str(root / 'scripts/pack.py')], check=True)
base = [str(editor), '-batchmode', '-projectPath', str(root / 'ExampleProject')]
manifest = root / 'ExampleProject/Packages/manifest.json'
lock = manifest.with_name('packages-lock.json')
saved = {p: p.read_bytes() if p.exists() else None for p in [manifest,lock]}
try:
    if args.packed:
        package = json.loads((root / 'package.json').read_text())
        settings = json.loads(manifest.read_text())
        settings['dependencies']['com.nuxie.unity'] = 'file:../../' + package['name'] + '-' + package['version'] + '.tgz'
        manifest.write_text(json.dumps(settings,indent=2) + '\n')
    for mode in ['EditMode', 'PlayMode']:
        result = evidence / (mode + '.xml')
        result.unlink(missing_ok=True)
        subprocess.run(base + ['-runTests', '-testPlatform', mode, '-testResults', str(result), '-logFile', str(evidence / (mode + '.log'))], check=True)
        if not result.exists(): raise SystemExit('Unity did not produce test results for ' + mode)
        report = ET.parse(result).getroot()
        if int(report.get('total', '0')) == 0 or report.get('result') != 'Passed':
            raise SystemExit('Unity test suite failed or ran no tests: ' + mode)
    if not args.tests_only:
        for target, method in [('iOS', 'Ios'), ('iOS', 'IosSimulator'), ('Android', 'Android')]:
            subprocess.run(base + ['-quit', '-buildTarget', target, '-executeMethod', 'Nuxie.Unity.Example.LabBuild.' + method, '-logFile', str(evidence / (method + '.log'))], check=True)
finally:
    if args.packed:
        for path, content in saved.items():
            if content is None: path.unlink(missing_ok=True)
            else: path.write_bytes(content)
print('Unity qualification passed. Evidence: ' + str(evidence))
