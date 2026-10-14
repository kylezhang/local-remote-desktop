"""Portable integrity/provenance rejection checks; no API calls or publishing."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('release', root/'scripts/prepare-cloud-release.py')
release = importlib.util.module_from_spec(spec); spec.loader.exec_module(release)


class Checks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name)
        self.commit = 'a'*40
        self.meta = {'version':'0.3.0-alpha.3','commit':self.commit,'platform':'windows-x64'}
        self.asset = self.path/'package.zip'; self.asset.write_bytes(b'exact fixture bytes')
        self.write()

    def write(self):
        (self.path/'ci.json').write_text(json.dumps(self.meta))
        (self.path/'SHA256SUMS.txt').write_text(release.digest(self.asset)+'  package.zip\n')

    def test_exact_asset(self):
        self.assertEqual(release.verify(self.path,['package.zip'],'0.3.0-alpha.3',self.commit,'windows-x64'), self.meta)

    def test_corruption(self):
        self.asset.write_bytes(b'changed bytes')
        with self.assertRaises(ValueError): release.verify(self.path,['package.zip'],'0.3.0-alpha.3',self.commit,'windows-x64')

    def test_wrong_provenance(self):
        for key,value in [('version','0.3.0'),('commit','b'*40),('platform','macos-arm64')]:
            old=self.meta[key]; self.meta[key]=value; self.write()
            with self.assertRaises(ValueError): release.verify(self.path,['package.zip'],'0.3.0-alpha.3',self.commit,'windows-x64')
            self.meta[key]=old

    def test_inventory_traversal_duplicate(self):
        for line in [release.digest(self.asset)+'  ../package.zip\n', (release.digest(self.asset)+'  package.zip\n')*2, '']:
            (self.path/'SHA256SUMS.txt').write_text(line)
            with self.assertRaises(ValueError): release.verify(self.path,['package.zip'],'0.3.0-alpha.3',self.commit,'windows-x64')

    def test_stable_not_authorized(self):
        with self.assertRaises(ValueError): release.prepare(self.path,self.path,self.path/'out','0.3.0',self.commit,'123')
        self.assertFalse((self.path/'out').exists())


if __name__ == '__main__': unittest.main()
