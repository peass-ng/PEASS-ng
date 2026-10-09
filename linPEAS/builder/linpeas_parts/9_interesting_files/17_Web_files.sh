# Title: Interesting Files - Web files
# ID: IF_Web_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Web files
# License: GNU GPL
# Version: 1.0
# Mitre: T1005
# Functions Used:  print_2title
# Global Variables: $SEARCH_IN_FOLDER, $FAST, $SUPERFAST, $TIMEOUT
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Web files?(output limit)" "T1005"
  ls -alhR /var/www/ 2>/dev/null | head
  ls -alhR /srv/www/htdocs/ 2>/dev/null | head
  ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head
  ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head
  echo ""
fi

if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ] && command -v python3 >/dev/null 2>&1; then
  print_2title "Django settings and FileBasedCache permissions (bounded passive check)" "T1005"
  (
    if [ "$SEARCH_IN_FOLDER" ]; then
      set -- "$SEARCH_IN_FOLDER"
    else
      set -- /var/www /srv/www /opt /usr/src/app/app/settings.py
    fi
    "$TIMEOUT" 8 python3 -I -S - "$@" <<'DJANGO_FILE_CACHE_CHECK'
import ast
import os
import stat
import sys
import time

if sys.version_info < (3, 6):
    raise SystemExit(0)  # Scoped scandir context managers are unavailable.

BACKEND = "django.core.cache.backends.filebased.FileBasedCache"
COOKIE_BACKEND = "django.contrib.sessions.backends.signed_cookies"
PICKLE_SERIALIZER = "django.contrib.sessions.serializers.PickleSerializer"
MAX_CANDIDATES = 30
MAX_BYTES = 262144
MAX_DIRS = 240
MAX_ENTRIES = 5000
MAX_OUTPUT = 10
DEADLINE = time.monotonic() + 6


def literal(node):
    if hasattr(ast, "Constant") and isinstance(node, ast.Constant):
        return node.value if isinstance(node.value, str) else None
    legacy_string = getattr(ast, "Str", ())
    return node.s if isinstance(node, legacy_string) else None


def signed_pickle_session(source):
    if b"signed_cookies" not in source or b"PickleSerializer" not in source:
        return False
    try:
        tree = ast.parse(source)
    except (SyntaxError, ValueError):
        return False
    names = ("SESSION_ENGINE", "SESSION_SERIALIZER")
    writes = {name: 0 for name in names}
    for node in ast.walk(tree):
        if isinstance(node, ast.ImportFrom) and any(alias.name == "*" for alias in node.names):
            return False
        if isinstance(node, ast.Call) and isinstance(node.func, ast.Name) and node.func.id in ("exec", "eval", "globals", "locals"):
            return False
        if isinstance(node, ast.Name) and node.id in writes and isinstance(node.ctx, (ast.Store, ast.Del)):
            writes[node.id] += 1
    if any(writes[name] != 1 for name in names):
        return False
    settings = {}
    for node in tree.body:
        if isinstance(node, ast.Assign) and len(node.targets) == 1 and isinstance(node.targets[0], ast.Name):
            name = node.targets[0].id
            if name in writes:
                settings[name] = literal(node.value)
    return settings.get("SESSION_ENGINE") == COOKIE_BACKEND and settings.get("SESSION_SERIALIZER") == PICKLE_SERIALIZER


def cache_entries(source):
    try:
        tree = ast.parse(source)
    except (SyntaxError, ValueError):
        return [], "settings syntax could not be parsed"
    for node in ast.walk(tree):
        if isinstance(node, ast.ImportFrom) and any(alias.name == "*" for alias in node.names):
            return [], "settings uses a wildcard import"
        if isinstance(node, ast.Call) and isinstance(node.func, ast.Name) and node.func.id in ("exec", "eval", "globals", "locals"):
            return [], "settings uses dynamic namespace operations"
    assignments = [node for node in tree.body if isinstance(node, ast.Assign)
                   and len(node.targets) == 1 and isinstance(node.targets[0], ast.Name)
                   and node.targets[0].id == "CACHES"]
    references = [node for node in ast.walk(tree) if isinstance(node, ast.Name) and node.id == "CACHES"]
    if len(assignments) != 1 or len(references) != 1:
        return [], "CACHES is dynamic or assigned more than once"
    caches = assignments[0].value
    if not isinstance(caches, ast.Dict):
        return [], "CACHES is not a literal dictionary"
    aliases = []
    seen = set()
    for key, value in zip(caches.keys, caches.values):
        alias = literal(key)
        if alias is None or alias in seen or not isinstance(value, ast.Dict):
            return [], "CACHES has a dynamic or duplicate alias"
        seen.add(alias)
        fields = {}
        for field_key, field_value in zip(value.keys, value.values):
            field = literal(field_key)
            if field is None or field in fields:
                return [], "cache entry has a dynamic or duplicate field"
            fields[field] = field_value
        if literal(fields.get("BACKEND")) == BACKEND:
            location = literal(fields.get("LOCATION"))
            if location is None or not os.path.isabs(location) or os.path.normpath(location) != location:
                aliases.append((alias, None))
            else:
                aliases.append((alias, location))
    return aliases, None


def candidate_files(roots):
    stack = [(root, 0) for root in reversed(roots[:3]) if os.path.isdir(root)]
    dirs = entries = candidates = 0
    while stack and dirs < MAX_DIRS and entries < MAX_ENTRIES and candidates < MAX_CANDIDATES and time.monotonic() < DEADLINE:
        directory, depth = stack.pop()
        dirs += 1
        try:
            with os.scandir(directory) as contents:
                for entry in contents:
                    entries += 1
                    if entries >= MAX_ENTRIES or time.monotonic() >= DEADLINE:
                        break
                    try:
                        if entry.name == "settings.py" and entry.is_file(follow_symlinks=False):
                            candidates += 1
                            yield entry.path
                            if candidates >= MAX_CANDIDATES:
                                break
                        elif depth < 5 and entry.is_dir(follow_symlinks=False):
                            stack.append((entry.path, depth + 1))
                    except OSError:
                        continue
        except OSError:
            continue
    if candidates < MAX_CANDIDATES and time.monotonic() < DEADLINE:
        for path in roots[3:4]:
            if os.path.isfile(path) and not os.path.islink(path):
                candidates += 1
                yield path


def process_consumer(settings):
    package = os.path.basename(os.path.dirname(settings))
    project = os.path.realpath(os.path.dirname(os.path.dirname(settings)))
    if not package or project == os.path.sep:
        return None
    try:
        with os.scandir("/proc") as processes:
            checked = 0
            for proc in processes:
                if checked >= 400 or time.monotonic() >= DEADLINE:
                    break
                if not proc.name.isdigit():
                    continue
                checked += 1
                try:
                    if os.path.realpath(proc.path + "/cwd") != project:
                        continue
                    with open(proc.path + "/cmdline", "rb") as command_file:
                        command = command_file.read(1024)
                    if b"gunicorn" not in command.lower() or (package + ".wsgi").encode() not in command:
                        continue
                    with open(proc.path + "/status", encoding="ascii", errors="replace") as status_file:
                        for line in status_file:
                            if line.startswith("Uid:"):
                                return int(line.split()[2]), proc.name
                except (OSError, ValueError, IndexError):
                    continue
    except OSError:
        pass
    return None


def cache_metadata(location):
    samples = []
    try:
        with os.scandir(location) as contents:
            for index, entry in enumerate(contents):
                if index >= 100 or len(samples) >= 3:
                    break
                if entry.name.endswith(".djcache"):
                    try:
                        info = entry.stat(follow_symlinks=False)
                        if stat.S_ISREG(info.st_mode):
                            samples.append("uid=%s mode=%04o" % (info.st_uid, stat.S_IMODE(info.st_mode)))
                    except OSError:
                        pass
    except OSError:
        pass
    return ", ".join(samples) if samples else "none sampled"


def inspect(settings):
    try:
        info = os.stat(settings, follow_symlinks=False)
        if not stat.S_ISREG(info.st_mode) or info.st_size > MAX_BYTES or info.st_size < 1:
            return []
        with open(settings, "rb") as settings_file:
            source = settings_file.read(MAX_BYTES + 1)
        if len(source) > MAX_BYTES:
            return []
        aliases, reason = cache_entries(source)
        session_candidate = signed_pickle_session(source)
    except (OSError, UnicodeError):
        return []
    if reason:
        findings = ["review Django cache settings %r: %s" % (settings, reason)] if b"FileBasedCache" in source else []
        if session_candidate:
            findings.append("review signed-cookie pickle session: settings=%r; SECRET_KEY access and active backend require confirmation" % settings)
        return findings
    findings = []
    for alias, location in aliases:
        if location is None:
            findings.append("review Django cache settings %r alias %r: LOCATION is dynamic or not a literal absolute path" % (settings, alias))
            continue
        try:
            directory = os.stat(location)
            if not stat.S_ISDIR(directory.st_mode):
                raise OSError("not a directory")
        except OSError:
            findings.append("review Django cache settings %r alias %r location %r: directory unavailable" % (settings, alias, location))
            continue
        try:
            writable = os.access(location, os.W_OK | os.X_OK, effective_ids=True)
        except (TypeError, NotImplementedError):
            writable = os.getuid() == os.geteuid() and os.access(location, os.W_OK | os.X_OK)
        sticky = bool(directory.st_mode & stat.S_ISVTX)
        symlinked = os.path.realpath(location) != os.path.abspath(location)
        consumer = process_consumer(settings) if writable else None
        consumer_text = "gunicorn pid=%s uid=%s" % (consumer[1], consumer[0]) if consumer else "consumer identity unverified"
        evidence = "settings=%r alias=%r location=%r dir_uid=%s mode=%04o access=%s sticky=%s symlink=%s %s cache_files=%s" % (
            settings, alias, location, directory.st_uid, stat.S_IMODE(directory.st_mode),
            "write+search" if writable else "no write+search", sticky, symlinked,
            consumer_text, cache_metadata(location))
        if writable and not sticky and not symlinked and consumer and consumer[0] != os.geteuid() and os.geteuid() != 0:
            findings.append("potential cross-user Django file cache replacement: " + evidence)
        else:
            findings.append("review Django file cache candidate: " + evidence)
    if session_candidate:
        findings.append("review signed-cookie pickle session: settings=%r; SECRET_KEY access and active backend require confirmation" % settings)
    return findings


printed = 0
for path in candidate_files(sys.argv[1:]):
    for finding in inspect(path):
        if printed >= MAX_OUTPUT or time.monotonic() >= DEADLINE:
            break
        print(finding, flush=True)
        printed += 1
    if printed >= MAX_OUTPUT:
        break
DJANGO_FILE_CACHE_CHECK
  )
  echo ""
fi
