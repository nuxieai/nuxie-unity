#!/usr/bin/env python3
"""Compile the shipped Swift bridge against the exact native package, without a Unity stub."""
from pathlib import Path
import json, subprocess
root = Path(__file__).resolve().parent.parent
pin = json.loads((root / 'NATIVE-PINS.json').read_text())['ios']
project = root / '.native/ios-check'
project.mkdir(parents=True, exist_ok=True)
(project / 'Host.swift').write_text('import UIKit\n@main class AppDelegate: UIResponder, UIApplicationDelegate { var window: UIWindow? }\n')
config = {
 'name': 'NuxieUnityCheck', 'options': {'deploymentTarget': {'iOS': '15.0'}},
 'packages': {'Nuxie': {'url': pin['repository'], 'revision': pin['revision']}},
 'targets': {'NuxieUnityCheck': {'type': 'application', 'platform': 'iOS',
 'sources': [{'path': str(root / 'Runtime/Plugins/iOS')}, {'path': 'Host.swift'}],
 'dependencies': [{'package': 'Nuxie'}],
 'settings': {'base': {'PRODUCT_BUNDLE_IDENTIFIER': 'ai.nuxie.unity.nativecheck', 'GENERATE_INFOPLIST_FILE': 'YES', 'SWIFT_VERSION': '5.0', 'SWIFT_STRICT_CONCURRENCY': 'complete', 'CODE_SIGNING_ALLOWED': 'NO'}}}}
}
(project / 'project.json').write_text(json.dumps(config,indent=2))
subprocess.run(['xcodegen','generate','--spec','project.json'],cwd=project,check=True)
subprocess.run(['xcodebuild','-project','NuxieUnityCheck.xcodeproj','-scheme','NuxieUnityCheck','-sdk','iphonesimulator','-destination','generic/platform=iOS Simulator','-derivedDataPath','build','ARCHS=arm64','build'],cwd=project,check=True)
