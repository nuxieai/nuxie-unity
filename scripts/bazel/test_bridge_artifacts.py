"""Compiler product packaging checked against archive and Gradle publication oracles."""
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile
from bridge_artifacts import aar, manifest, publish_maven

class BridgeArtifactsTests(unittest.TestCase):
    def test_aar_preserves_compiled_entries_and_owning_consumer_rules(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, rules, output = root / 'compiled.aar', root / 'consumer.pro', root / 'bridge.aar'
            with zipfile.ZipFile(source, 'w') as archive:
                for name, content in {'classes.jar': b'compiled JVM bytecode', 'AndroidManifest.xml': b'compiled manifest', 'res/values/strings.xml': b'owned resources'}.items():
                    archive.writestr(name, content)
            rules.write_bytes(b'-keep class ai.nuxie.** { *; }')
            aar(source, output, rules)
            first = output.read_bytes()
            with zipfile.ZipFile(output) as archive:
                self.assertEqual(archive.read('classes.jar'), b'compiled JVM bytecode')
                self.assertEqual(archive.read('res/values/strings.xml'), b'owned resources')
                self.assertEqual(archive.read('proguard.txt'), rules.read_bytes())
                self.assertTrue(all(item.date_time == (1980, 1, 1, 0, 0, 0) for item in archive.infolist()))
            aar(source, output, rules)
            self.assertEqual(output.read_bytes(), first)

    def test_manifest_keeps_permissions_and_requires_resolved_engine_metadata(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, output = root / 'source.xml', root / 'manifest.xml'
            source.write_text('<manifest xmlns:android="http://schemas.android.com/apk/res/android"><uses-permission android:name="android.permission.INTERNET"/><application><meta-data android:name="plugin" android:value="${plugin}"/></application></manifest>')
            with self.assertRaisesRegex(ValueError, 'Unresolved'):
                manifest(source, output, 'ai.nuxie.bridge', [])
            manifest(source, output, 'ai.nuxie.bridge', ['plugin=engine'])
            contents = ET.parse(output).getroot()
            android = '{http://schemas.android.com/apk/res/android}'
            self.assertEqual(contents.find('uses-permission').get(android + 'name'), 'android.permission.INTERNET')
            self.assertEqual(contents.find('application/meta-data').get(android + 'value'), 'engine')
            self.assertEqual(contents.find('uses-sdk').get(android + 'minSdkVersion'), '23')

    def test_unity_maven_scopes_match_the_owning_gradle_release_publication(self):
        ns = {'m': 'http://maven.apache.org/POM/4.0.0'}
        oracle = ET.parse(Path(__file__).with_name('bridge-pom-oracle.xml')).getroot()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / 'bridge.aar'
            source.write_bytes(b'compiled bridge')
            destination = root / 'maven'
            pin = '0cbe8086068eb1a0d7e1c53c4440de4c9e3bd5ae'
            publish_maven(source, destination, '0.2.0', pin)
            actual = ET.parse(destination / 'nuxie-unity-bridge-0.2.0.pom').getroot()
            def dependencies(tree):
                return {tuple(dep.find('m:' + field, ns).text for field in ('groupId','artifactId','version','scope')) for dep in tree.findall('m:dependencies/m:dependency', ns)}
            self.assertEqual(dependencies(actual), dependencies(oracle))
            self.assertEqual((destination / 'nuxie-unity-bridge-0.2.0.aar').read_bytes(), source.read_bytes())

if __name__ == '__main__':
    unittest.main()
