#!/usr/bin/env python3
"""Submit this SDK's existing frontend to the shared, capability-selected pool."""
import importlib.util
import json
import os
import plistlib
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
SDK = 'unity'
OUTPUTS = ['.native/artifacts', 'ExampleProject/Build', '.native/test-evidence', '.native/build', 'Artifacts~', '.native/xcframework', '.native/licenses', '.native/unity-check']


def selection(mode, arguments, command=None, selected_platform=None):
    selected = 'android' if mode == 'prepare-native' and SDK in ('react-native', 'unity') else 'all'
    for i, arg in enumerate(arguments):
        if arg == '--platform' and i + 1 < len(arguments): selected = arguments[i + 1]
        elif arg.startswith('--platform='): selected = arg.split('=', 1)[1]
    if selected_platform is not None: selected = selected_platform
    command = command or (arguments[0] if arguments else '')
    requirements = set()
    apple = False
    if SDK == 'android':
        requirements.add('android')
    elif mode == 'sdk':
        if command == 'build-native':
            apple = selected in ('ios', 'all')
            if selected in ('android', 'all'): requirements.add('android')
        elif command.endswith('-ios'):
            apple = True
        elif command.endswith('-android'):
            requirements.add('android')
    elif mode in ('check-ios', 'prepare-ios'):
        apple = True
    else:
        apple = selected in ('ios', 'all')
        if mode in ('prepare-native', 'prepare-android') and SDK == 'unity': apple = False
        if mode == 'prepare-android': apple = False
        if selected in ('android', 'all'): requirements.add('android')
        if mode in ('check', 'check-unity'):
            requirements.update({'react-native': ['node'], 'flutter': [],
                'godot': ['godot@4.7.2'], 'unity': ['unity'], 'ue': ['unreal@5.8']}.get(SDK, []))
    if 'unity' in requirements:
        version = (ROOT / 'ExampleProject/ProjectSettings/ProjectVersion.txt').read_text().split(':', 1)[1].strip().splitlines()[0]
        requirements.remove('unity'); requirements.add('unity@' + version)
    if (mode == 'gradle' or (mode == 'check' and SDK not in ('unity', 'ue') and 'android' in requirements)
            or (SDK == 'android' and mode == 'sdk' and command in ('check', 'lint', 'api-check', 'providers'))):
        requirements.add('java17')
    if mode == 'check-unity' and '--tests-only' in arguments:
        apple = False; requirements.discard('android')
    if apple: requirements.add('xcode')
    return ('macos' if apple else 'portable'), sorted(requirements)


def dispatch(mode, arguments, *, output=None, command=None, input_files=None, environment=None, selected_platform=None):
    if (os.environ.get('_NUXIE_POOL_NESTED') == '1' or os.environ.get('NUXIE_HOMELAB') == '0'
            or os.environ.get('NUXIE_EXECUTION') == 'local' or '--help' in arguments or '-h' in arguments): return None
    helper = Path.home() / '.local/share/nuxie-homelab/sdk-task.py'
    configured = (Path.home() / '.config/nuxie/homelab-cluster.json').is_file()
    if not helper.is_file():
        if configured and os.environ.get('NUXIE_HOMELAB') != '0' and os.environ.get('NUXIE_EXECUTION') != 'local':
            raise RuntimeError('Configured pool needs its SDK task client installed')
        return None
    spec = importlib.util.spec_from_file_location('pool_sdk_task', helper)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    platform, requirements = selection(mode, arguments, command, selected_platform)
    names = {name: ROOT / name for name in OUTPUTS}
    package = json.loads((ROOT / 'package.json').read_text())
    name = package['name'] + '-' + package['version'] + '.tgz'
    names[name] = ROOT / name
    if output is not None:
        names['build/pool-prepared'] = Path(output).resolve()
        names.pop('build/bazel-sdk', None)
    frontend = 'scripts/bazel/sdk.py' if mode == 'sdk' else 'scripts/pool.py'
    args = list(arguments) if mode == 'sdk' else [mode, *arguments]
    if output is not None:
        for index, argument in enumerate(args):
            if argument == '--output' and index + 1 < len(args): args[index + 1] = str(Path(output).resolve())
            elif argument.startswith('--output='): args[index] = '--output=' + str(Path(output).resolve())
    if output is not None and not any(a == '--output' or a.startswith('--output=') for a in args):
        args += ['--output', str(output)]
    if mode == 'check-unity':
        editor = next((args[i + 1] for i, value in enumerate(args[:-1]) if value == '--editor'), None) or next((value.split('=',1)[1] for value in args if value.startswith('--editor=')), None)
        if editor:
            selected_editor = Path(editor).expanduser().resolve(strict=True)
            bundle = next((parent for parent in [selected_editor, *selected_editor.parents] if parent.suffix == '.app'), None)
            if bundle is None: raise ValueError('The selected editor must be a Unity application bundle')
            version = plistlib.loads((bundle / 'Contents/Info.plist').read_bytes()).get('CFBundleShortVersionString')
            if 'unity@' + str(version) not in requirements: raise ValueError('The selected editor must match the SDK pinned Unity version')
            retained = []; skip = False
            for value in args:
                if skip: skip = False; continue
                if value == '--editor': skip = True; continue
                if value.startswith('--editor='): continue
                retained.append(value)
            args = retained
        selected = ROOT / 'Artifacts~'
        if not selected.is_dir(): raise ValueError('Prepare native SDK products before Unity checks')
        input_files = {'Artifacts~': selected, **(input_files or {})}
    return module.route(ROOT, args, frontend=frontend, platform=platform,
                        requirements=requirements, outputs=names, input_files=input_files, environment=environment,
                        merge_outputs=[name for name in names if not (SDK == 'react-native' and name == 'dist')])


def main():
    mode, *arguments = sys.argv[1:]
    status = dispatch(mode, arguments)
    if status is not None: return status
    if mode == 'gradle':
        command = [str(ROOT / 'gradlew'), *arguments, '--no-daemon', '--max-workers=' + os.environ.get('NUXIE_BAZEL_CPU', '2')]
    elif SDK == 'react-native' and mode == 'check':
        # Dependencies belong to the selected host's disposable snapshot.
        subprocess.run(['pnpm', '--ignore-workspace', 'install', '--frozen-lockfile'], cwd=ROOT, check=True)
        command = ['node', 'scripts/check.mjs', *arguments]
    else:
        command = [sys.executable, 'scripts/' + mode + '.py', *arguments]
    return subprocess.run(command, cwd=ROOT, env=dict(os.environ, _NUXIE_POOL_NESTED='1')).returncode


if __name__ == '__main__':
    raise SystemExit(main())
