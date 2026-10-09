import os
import re
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/9_interesting_files/17_Web_files.sh"
CHECK = re.search(
    r"<<'DJANGO_FILE_CACHE_CHECK'\n(.*?)\nDJANGO_FILE_CACHE_CHECK",
    MODULE.read_text(encoding="utf-8"),
    re.S,
).group(1)


class DjangoFileCacheTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name).resolve()
        self.project = self.root / "site with spaces"
        self.package = self.project / "webapp"
        self.package.mkdir(parents=True)
        self.cache = self.root / "cache with spaces"
        self.cache.mkdir(mode=0o777)
        self.cache.chmod(0o777)
        self.settings = self.package / "settings.py"
        self.proc = self.root / "fake-proc"
        process = self.proc / "1234"
        process.mkdir(parents=True)
        (process / "cwd").symlink_to(self.project)
        (process / "cmdline").write_bytes(b"gunicorn\0webapp.wsgi:application\0")
        (process / "status").write_text("Uid:\t1000\t1000\t1000\t1000\n", encoding="ascii")
        self.payload = self.cache / "entry.djcache"
        self.payload.write_bytes(b"opaque cache payload")
        self.payload.chmod(0o600)

    def config(self, cache=None, location=None, tail=""):
        cache = cache or "django.core.cache.backends.filebased.FileBasedCache"
        location = repr(str(location if location is not None else self.cache))
        self.settings.write_text(
            "CACHES = {'default': {'BACKEND': %r, 'LOCATION': %s}}\n%s" % (cache, location, tail),
            encoding="utf-8",
        )

    def run_check(self, source=None):
        source = source or CHECK
        source = source.replace('os.scandir("/proc")', 'os.scandir(%r)' % str(self.proc))
        source = source.replace("os.geteuid()", "2000")
        result = subprocess.run(
            ["python3", "-I", "-S", "-c", source, str(self.root)],
            capture_output=True, text=True, timeout=10,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_literal_same_alias_with_process_evidence(self):
        self.config()
        before = self.payload.read_bytes()
        output = self.run_check()
        self.assertEqual(output.count("potential cross-user"), 1)
        self.assertIn(repr(str(self.settings)), output)
        self.assertIn(repr(str(self.cache)), output)
        self.assertIn("gunicorn pid=1234 uid=1000", output)
        self.assertEqual(self.payload.read_bytes(), before)

    def test_mismatched_aliases_and_other_backends(self):
        self.settings.write_text(
            "CACHES = {'default': {'BACKEND': 'django.core.cache.backends.filebased.FileBasedCache'}, "
            "'other': {'BACKEND': 'django.core.cache.backends.locmem.LocMemCache', 'LOCATION': %r}}\n" % str(self.cache),
            encoding="utf-8",
        )
        output = self.run_check()
        self.assertNotIn("potential cross-user", output)
        self.assertNotIn(repr(str(self.cache)), output)
        self.assertIn("LOCATION is dynamic", output)
        self.config(cache="django.core.cache.backends.locmem.LocMemCache")
        self.assertEqual(self.run_check(), "")

    def test_dynamic_and_mutated_settings_are_review_only(self):
        self.settings.write_text(
            "import os\nCACHES = {'default': {'BACKEND': 'django.core.cache.backends.filebased.FileBasedCache', "
            "'LOCATION': os.environ['CACHE_DIR']}}\n",
            encoding="utf-8",
        )
        self.assertIn("LOCATION is dynamic", self.run_check())
        self.config(tail="CACHES['default']['LOCATION'] = '/tmp/changed'\n")
        output = self.run_check()
        self.assertIn("dynamic or assigned more than once", output)
        self.assertNotIn("potential cross-user", output)

    def test_sticky_symlink_missing_identity_and_no_access(self):
        self.config()
        self.cache.chmod(0o1777)
        self.assertIn("review Django file cache candidate", self.run_check())
        self.assertNotIn("potential cross-user", self.run_check())
        self.cache.chmod(0o777)
        link = self.root / "cache-link"
        link.symlink_to(self.cache, target_is_directory=True)
        self.config(location=link)
        self.assertIn("symlink=True", self.run_check())
        self.assertNotIn("potential cross-user", self.run_check())
        self.config()
        (self.proc / "1234" / "cmdline").write_bytes(b"other-service\0")
        self.assertIn("consumer identity unverified", self.run_check())
        self.assertNotIn("potential cross-user", self.run_check())
        (self.proc / "1234" / "cmdline").write_bytes(b"gunicorn\0webapp.wsgi:application\0")
        (self.proc / "1234" / "status").write_text("Uid:\t2000\t2000\t2000\t2000\n", encoding="ascii")
        self.assertNotIn("potential cross-user", self.run_check())
        (self.proc / "1234" / "status").write_text("Uid:\t1000\t1000\t1000\t1000\n", encoding="ascii")
        self.cache.chmod(0o755)
        denied = CHECK.replace(
            "os.access(location, os.W_OK | os.X_OK, effective_ids=True)", "False"
        )
        self.assertIn("access=no write+search", self.run_check(denied))
        self.assertNotIn("potential cross-user", self.run_check(denied))

    def test_caps_size_depth_and_candidate_output(self):
        self.config()
        self.settings.write_bytes(self.settings.read_bytes() + b" " * 262145)
        self.assertEqual(self.run_check(), "")
        self.settings.unlink()
        for index in range(35):
            package = self.root / ("project%02d" % index) / "webapp"
            package.mkdir(parents=True)
            (package / "settings.py").write_text(
                "CACHES = {'default': {'BACKEND': 'django.core.cache.backends.filebased.FileBasedCache', "
                "'LOCATION': %r}}\n" % str(self.cache), encoding="utf-8"
            )
        output = self.run_check()
        self.assertLessEqual(len(output.splitlines()), 10)
        candidate_capped = self.run_check(CHECK.replace("MAX_OUTPUT = 10", "MAX_OUTPUT = 100"))
        self.assertEqual(len(candidate_capped.splitlines()), 30)
        deep = self.root.joinpath(*(["deep"] * 6))
        deep.mkdir(parents=True)
        (deep / "settings.py").write_text("CACHES = {}\n", encoding="utf-8")

    def test_signed_cookie_pickle_pair_without_secret_disclosure(self):
        secret = "fixture-secret-must-remain-hidden"
        self.settings.write_text(
            "SESSION_ENGINE = 'django.contrib.sessions.backends.signed_cookies'\n"
            "SESSION_SERIALIZER = 'django.contrib.sessions.serializers.PickleSerializer'\n"
            "SECRET_KEY = %r\n" % secret,
            encoding="utf-8",
        )
        output = self.run_check()
        self.assertEqual(output.count("review signed-cookie pickle session"), 1)
        self.assertIn(repr(str(self.settings)), output)
        self.assertNotIn(secret, output)
        self.assertNotIn("review Django cache settings", output)

    def test_signed_cookie_pickle_requires_unambiguous_literal_pair(self):
        engine = "SESSION_ENGINE = 'django.contrib.sessions.backends.signed_cookies'\n"
        serializer = "SESSION_SERIALIZER = 'django.contrib.sessions.serializers.PickleSerializer'\n"
        negatives = (
            engine,
            serializer,
            engine + "SESSION_SERIALIZER = 'django.contrib.sessions.serializers.JSONSerializer'\n",
            "SESSION_ENGINE = 'django.contrib.sessions.backends.db'\n" + serializer,
            "# " + engine + serializer,
            "description = %r\n" % (engine + serializer),
            engine + "SESSION_SERIALIZER = os.environ['SESSION_SERIALIZER']\n# PickleSerializer\n",
            engine + serializer + "SESSION_SERIALIZER = 'django.contrib.sessions.serializers.JSONSerializer'\n",
            engine + serializer + "SESSION_ENGINE = 'django.contrib.sessions.backends.db'\n",
            engine + serializer + "from local_settings import *\n",
            engine + serializer + "SESSION_ENGINE += '.other'\n",
        )
        for source in negatives:
            with self.subTest(source=source):
                self.settings.write_text(source, encoding="utf-8")
                self.assertNotIn("review signed-cookie pickle session", self.run_check())

    def test_session_check_does_not_execute_settings_and_preserves_cache(self):
        marker = self.root / "must-not-exist"
        self.config(tail=(
            "SESSION_ENGINE = 'django.contrib.sessions.backends.signed_cookies'\n"
            "SESSION_SERIALIZER = 'django.contrib.sessions.serializers.PickleSerializer'\n"
            "open(%r, 'w')\n" % str(marker)
        ))
        output = self.run_check()
        self.assertIn("potential cross-user Django file cache replacement", output)
        self.assertIn("review signed-cookie pickle session", output)
        self.assertFalse(marker.exists())

    def test_exact_default_path_runs_after_existing_roots_and_respects_candidate_cap(self):
        self.settings.write_text(
            "SESSION_ENGINE = 'django.contrib.sessions.backends.signed_cookies'\n"
            "SESSION_SERIALIZER = 'django.contrib.sessions.serializers.PickleSerializer'\n",
            encoding="utf-8",
        )
        absent = self.root / "absent"
        args = [str(absent)] * 3 + [str(self.settings)]
        result = subprocess.run(
            ["python3", "-I", "-S", "-c", CHECK] + args,
            capture_output=True, text=True, timeout=10,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("review signed-cookie pickle session", result.stdout)
        link = self.root / "linked-settings.py"
        link.symlink_to(self.settings)
        result = subprocess.run(
            ["python3", "-I", "-S", "-c", CHECK] + args[:3] + [str(link)],
            capture_output=True, text=True, timeout=10,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "")
        result = subprocess.run(
            ["python3", "-I", "-S", "-c", CHECK.replace("MAX_CANDIDATES = 30", "MAX_CANDIDATES = 0")] + args,
            capture_output=True, text=True, timeout=10,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "")

    def test_shell_gates_fast_and_missing_tools(self):
        self.config(tail=(
            "SESSION_ENGINE = 'django.contrib.sessions.backends.signed_cookies'\n"
            "SESSION_SERIALIZER = 'django.contrib.sessions.serializers.PickleSerializer'\n"
        ))
        shell = "print_2title() { :; }; . \"$1\""
        sh = shutil.which("sh") or "/bin/sh"
        env = dict(os.environ, SEARCH_IN_FOLDER=str(self.root), FAST="1", SUPERFAST="", TIMEOUT=shutil.which("timeout") or "")
        result = subprocess.run([sh, "-c", shell, "sh", str(MODULE)], env=env, capture_output=True, text=True, timeout=10)
        self.assertEqual(result.stdout, "")
        env.update(FAST="", SUPERFAST="1")
        result = subprocess.run([sh, "-c", shell, "sh", str(MODULE)], env=env, capture_output=True, text=True, timeout=10)
        self.assertEqual(result.stdout, "")
        env.update(SUPERFAST="", TIMEOUT="")
        result = subprocess.run([sh, "-c", shell, "sh", str(MODULE)], env=env, capture_output=True, text=True, timeout=10)
        self.assertEqual(result.stdout, "")
        if shutil.which("timeout"):
            env["TIMEOUT"] = shutil.which("timeout")
            env["PATH"] = str(self.root / "empty-path")
            result = subprocess.run([sh, "-c", shell, "sh", str(MODULE)], env=env, capture_output=True, text=True, timeout=10)
            self.assertEqual(result.stdout, "")
            env["PATH"] = os.environ["PATH"]
            result = subprocess.run([sh, "-c", shell, "sh", str(MODULE)], env=env, capture_output=True, text=True, timeout=10)
            self.assertIn("review Django file cache candidate", result.stdout)
            self.assertIn("review signed-cookie pickle session", result.stdout)


if __name__ == "__main__":
    unittest.main()
