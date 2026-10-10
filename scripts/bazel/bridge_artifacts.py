#!/usr/bin/env python3
"""Package compiler-owned bridge products without changing their language code."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ET
import zipfile


ANDROID = "http://schemas.android.com/apk/res/android"


def manifest(source, output, package, placeholders):
    ET.register_namespace("android", ANDROID)
    contents = source.read_text()
    for value in placeholders:
        key, replacement = value.split("=", 1)
        contents = contents.replace("${" + key + "}", replacement)
    if "${" in contents:
        raise ValueError("Unresolved Android bridge manifest placeholder")
    tree = ET.fromstring(contents)
    tree.set("package", package)
    minimum = tree.find("uses-sdk")
    if minimum is None:
        minimum = ET.SubElement(tree, "uses-sdk")
    minimum.set("{" + ANDROID + "}minSdkVersion", "23")
    minimum.set("{" + ANDROID + "}targetSdkVersion", "36")
    output.write_bytes(ET.tostring(tree, encoding="utf-8", xml_declaration=True))


def aar(source, output, consumer_rules=None):
    with zipfile.ZipFile(source) as archive:
        entries = {name: archive.read(name) for name in archive.namelist() if not name.endswith("/")}
    if not entries.get("classes.jar") or not entries.get("AndroidManifest.xml"):
        raise ValueError("The Kotlin compiler/resource target must provide classes.jar and AndroidManifest.xml")
    if consumer_rules:
        entries["proguard.txt"] = consumer_rules.read_bytes()
    entries["META-INF/com/android/build/gradle/aar-metadata.properties"] = (
        b"aarFormatVersion=1.0\naarMetadataVersion=1.0\nminCompileSdk=1\nminCompileSdkExtension=0\n"
        b"minAndroidGradlePluginVersion=1.0.0\ncoreLibraryDesugaringEnabled=false\n")
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(entries.items()):
            info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    for command in ("manifest", "aar"):
        sub = commands.add_parser(command)
        sub.add_argument("--input", type=Path, required=True)
        sub.add_argument("--output", type=Path, required=True)
        if command == "manifest":
            sub.add_argument("--package", required=True)
            sub.add_argument("--placeholder", action="append", default=[])
        else:
            sub.add_argument("--consumer-rules", type=Path)
    args = parser.parse_args()
    if args.command == "manifest":
        manifest(args.input, args.output, args.package, args.placeholder)
    else:
        aar(args.input, args.output, args.consumer_rules)


if __name__ == "__main__":
    main()


def publish_maven(artifact, destination, version, native_revision):
    """Preserve the owning Gradle release component's runtime dependency scopes."""
    import hashlib
    import json
    import shutil
    destination.mkdir(parents=True, exist_ok=True)
    stem = 'nuxie-unity-bridge-' + version
    shutil.copyfile(artifact, destination / (stem + '.aar'))
    dependencies = [('ai.nuxie', 'nuxie-android', '0.2.0-' + native_revision),
                    ('org.jetbrains.kotlinx', 'kotlinx-coroutines-android', '1.9.0'),
                    ('org.jetbrains.kotlin', 'kotlin-stdlib', '2.0.21')]
    project = ET.Element('project', xmlns='http://maven.apache.org/POM/4.0.0')
    project.append(ET.Comment(' do_not_remove: published-with-gradle-metadata '))
    for key, value in [('modelVersion', '4.0.0'), ('groupId', 'ai.nuxie'),
                       ('artifactId', 'nuxie-unity-bridge'), ('version', version), ('packaging', 'aar')]:
        ET.SubElement(project, key).text = value
    deps = ET.SubElement(project, 'dependencies')
    for group, name, selected in dependencies:
        dependency = ET.SubElement(deps, 'dependency')
        for key, value in [('groupId', group), ('artifactId', name), ('version', selected), ('scope', 'compile' if name == 'kotlin-stdlib' else 'runtime')]:
            ET.SubElement(dependency, key).text = value
    (destination / (stem + '.pom')).write_bytes(ET.tostring(project, encoding='utf-8', xml_declaration=True))
    aar_bytes = artifact.read_bytes()
    metadata = {'formatVersion': '1.1', 'component': {'group': 'ai.nuxie', 'module': 'nuxie-unity-bridge', 'version': version}, 'variants': []}
    for usage in ('java-api', 'java-runtime'):
        variant = {'name': 'releaseApiElements-published' if usage == 'java-api' else 'releaseRuntimeElements-published',
                   'attributes': {'org.gradle.category': 'library', 'org.gradle.dependency.bundling': 'external',
                                  'org.gradle.libraryelements': 'aar', 'org.gradle.usage': usage,
                                  'org.jetbrains.kotlin.platform.type': 'androidJvm'},
                   'files': [{'name': stem + '.aar', 'url': stem + '.aar', 'size': len(aar_bytes),
                              **{algorithm: hashlib.new(algorithm, aar_bytes).hexdigest() for algorithm in ('md5','sha1','sha256','sha512')}}]}
        selected_dependencies = dependencies if usage == 'java-runtime' else dependencies[-1:]
        variant['dependencies'] = [{'group': group, 'module': name, 'version': {'requires': selected}} for group, name, selected in selected_dependencies]
        metadata['variants'].append(variant)
    (destination / (stem + '.module')).write_text(json.dumps(metadata, indent=2) + '\n')
    for path in list(destination.iterdir()):
        if path.is_file():
            for algorithm in ('md5','sha1','sha256','sha512'):
                (destination / (path.name + '.' + algorithm)).write_text(hashlib.new(algorithm, path.read_bytes()).hexdigest())
