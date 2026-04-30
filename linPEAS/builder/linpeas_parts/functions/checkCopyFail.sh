# Title: Function - checkCopyFail
# ID: checkCopyFail
# Author: GitHub Copilot
# Last Update: 30-04-2026
# Description: Check whether the current Linux kernel looks vulnerable to Copy Fail (CVE-2026-31431).
# Description: Prefer a non-destructive Python runtime probe when Python is available; otherwise fall back to shell heuristics based on upstream fixed releases and AEAD user API exposure.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $E, $SED_GREEN, $SED_RED, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $CF31_PY_TIMEOUT, $CF31_TMP_PY, $CF31_PY, $CF31_RC, $CF31_KERNEL_OS, $CF31_KERNEL_RELEASE, $CF31_KERNEL_VERSION, $CF31_CFG, $CF31_CFG_FILE, $CF31_CFG_LINE, $CF31_API, $CF31_BLOCKED, $CF31_FIXED_PKG, $CF31_PKG, $CF31_MAJ, $CF31_MIN, $CF31_PAT, $CF31_CANDIDATE, $CF31_KV
# Fat linpeas: 0
# Small linpeas: 1


cf31_num() {
    printf '%s\n' "$1" | sed 's/[^0-9].*$//; s/^0*//; s/^$/0/'
}

cf31_is_fixed_upstream_release() {
    if [ "$CF31_MAJ" -ge 7 ]; then
        return 0
    fi

    if [ "$CF31_MAJ" -eq 6 ]; then
        case "$CF31_MIN" in
            19) [ "$CF31_PAT" -ge 12 ] && return 0 ;;
            18) [ "$CF31_PAT" -ge 22 ] && return 0 ;;
            12) [ "$CF31_PAT" -ge 85 ] && return 0 ;;
            6) [ "$CF31_PAT" -ge 137 ] && return 0 ;;
            1) [ "$CF31_PAT" -ge 170 ] && return 0 ;;
        esac
    elif [ "$CF31_MAJ" -eq 5 ]; then
        case "$CF31_MIN" in
            15) [ "$CF31_PAT" -ge 204 ] && return 0 ;;
            10) [ "$CF31_PAT" -ge 254 ] && return 0 ;;
        esac
    fi

    return 1
}

cf31_py_can_run_probe() {
    CF31_PY="$1"

    if command -v timeout >/dev/null 2>&1; then
        timeout "$CF31_PY_TIMEOUT" "$CF31_PY" -c 'import ctypes, os, sys
try:
    if hasattr(os, "splice"):
        sys.exit(0)
    libc = ctypes.CDLL(None, use_errno=True)
    sys.exit(0 if hasattr(libc, "splice") else 1)
except Exception:
    sys.exit(1)
' >/dev/null 2>&1
    else
        "$CF31_PY" -c 'import ctypes, os, sys
try:
    if hasattr(os, "splice"):
        sys.exit(0)
    libc = ctypes.CDLL(None, use_errno=True)
    sys.exit(0 if hasattr(libc, "splice") else 1)
except Exception:
    sys.exit(1)
' >/dev/null 2>&1
    fi
}

cf31_run_python_probe() {
    CF31_PY="$1"
    CF31_TMP_PY=/tmp/cf31-probe-$$.py

    cat > "$CF31_TMP_PY" <<'PY'
import errno, os, signal, socket, struct, sys, tempfile, shutil

try:
    signal.signal(signal.SIGALRM, lambda *_: (_ for _ in ()).throw(TimeoutError("probe timeout")))
    signal.alarm(10)
except Exception:
    pass

AF_ALG=38
SOCK_SEQPACKET=5
SOL_ALG=279
ALG_SET_KEY=1
ALG_SET_IV=2
ALG_SET_OP=3
ALG_SET_AEAD_ASSOCLEN=4
ALG_SET_AEAD_AUTHSIZE=5
ALG_OP_DECRYPT=0
ALG="authencesn(hmac(sha256),cbc(aes))"
PAGE=4096
TARGET_OFF=16
MARK=b"CF31"

def out(msg, code):
    print(msg, flush=True)
    raise SystemExit(code)

def build_splice():
    if hasattr(os, "splice"):
        def _splice(fd_in, fd_out, length, offset_src=None, offset_dst=None):
            return os.splice(fd_in, fd_out, length, offset_src=offset_src, offset_dst=offset_dst)
        return _splice

    try:
        import ctypes
        libc=ctypes.CDLL(None, use_errno=True)
        splice_fn=libc.splice
        off_t=ctypes.c_longlong
        splice_fn.argtypes=[ctypes.c_int, ctypes.POINTER(off_t), ctypes.c_int, ctypes.POINTER(off_t), ctypes.c_size_t, ctypes.c_uint]
        splice_fn.restype=ctypes.c_ssize_t
    except Exception as e:
        out("PYTHON_UNUSABLE: splice helper is not available (%s)" % e, 1)

    def _splice(fd_in, fd_out, length, offset_src=None, offset_dst=None):
        in_off=off_t(offset_src) if offset_src is not None else None
        out_off=off_t(offset_dst) if offset_dst is not None else None
        n=splice_fn(
            fd_in,
            ctypes.byref(in_off) if in_off is not None else None,
            fd_out,
            ctypes.byref(out_off) if out_off is not None else None,
            length,
            0,
        )
        if n < 0:
            err=ctypes.get_errno()
            raise OSError(err, os.strerror(err))
        return n

    return _splice

SPLICE=build_splice()
fd=rfd=wfd=None
op=master=None
td=None

try:
    try:
        master=socket.socket(AF_ALG, SOCK_SEQPACKET, 0)
        master.bind(("aead", ALG))
    except OSError as e:
        out("NOT VULNERABLE: AF_ALG/authencesn is not reachable from this context (%s)" % (e.strerror or e), 0)

    master.setsockopt(SOL_ALG, ALG_SET_KEY, bytes.fromhex("0800010000000010" + "00"*32))
    master.setsockopt(SOL_ALG, ALG_SET_AEAD_AUTHSIZE, None, 4)
    op,_=master.accept()

    try:
        op.settimeout(3.0)
    except Exception:
        pass

    td=tempfile.mkdtemp(prefix="cf31-check-")
    path=os.path.join(td, "sentinel")
    baseline=b"A"*PAGE

    with open(path, "wb") as f:
        f.write(baseline)

    fd=os.open(path, os.O_RDONLY)
    os.read(fd, PAGE)
    os.lseek(fd, 0, 0)

    cmsgs=[
        (SOL_ALG, ALG_SET_OP, struct.pack("I", ALG_OP_DECRYPT)),
        (SOL_ALG, ALG_SET_IV, struct.pack("I",16)+b"\x00"*16),
        (SOL_ALG, ALG_SET_AEAD_ASSOCLEN, struct.pack("I",8)),
    ]

    op.sendmsg([b"AAAA"+MARK], cmsgs, socket.MSG_MORE)

    rfd,wfd=os.pipe()
    splice_len=TARGET_OFF+len(MARK)

    n=SPLICE(fd, wfd, splice_len, offset_src=0)
    if n != splice_len:
        out("UNKNOWN: short splice file->pipe (%d/%d)" % (n, splice_len), 1)

    n2=SPLICE(rfd, op.fileno(), splice_len)
    if n2 != splice_len:
        out("UNKNOWN: short splice pipe->AF_ALG (%d/%d)" % (n2, splice_len), 1)

    try:
        op.recv(64)
    except OSError as e:
        if e.errno not in (errno.EBADMSG, errno.EINVAL):
            raise
    except TimeoutError:
        out("PYTHON_PROBE_UNKNOWN: recv timed out", 1)

    os.lseek(fd, 0, 0)
    after=os.read(fd, PAGE)

    if after[TARGET_OFF:TARGET_OFF+len(MARK)] == MARK:
        out("VULNERABLE: non-destructive AF_ALG/splice page-cache write triggered", 2)

    if after != baseline:
        out("VULNERABLE: temp-file page cache changed unexpectedly", 2)

    out("NOT VULNERABLE: Python runtime probe left temp-file page cache intact", 0)

except SystemExit:
    raise
except Exception as e:
    out("PYTHON_PROBE_UNKNOWN: %s: %s" % (type(e).__name__, e), 1)
finally:
    try:
        signal.alarm(0)
    except Exception:
        pass

    for x in (fd,rfd,wfd):
        try:
            if x is not None:
                os.close(x)
        except Exception:
            pass

    for s in (op,master):
        try:
            if s is not None:
                s.close()
        except Exception:
            pass

    try:
        if td:
            shutil.rmtree(td)
    except Exception:
        pass
PY

    [ -s "$CF31_TMP_PY" ] || return 1

    if command -v timeout >/dev/null 2>&1; then
        timeout "$CF31_PY_TIMEOUT" "$CF31_PY" "$CF31_TMP_PY"
    else
        "$CF31_PY" "$CF31_TMP_PY"
    fi
}

checkCopyFail() {
    (
        CF31_PY_TIMEOUT=12
        CF31_TMP_PY=""
        trap '[ -n "$CF31_TMP_PY" ] && rm -f "$CF31_TMP_PY"' EXIT HUP INT TERM

        CF31_KERNEL_OS=$(uname -s 2>/dev/null || echo unknown)
        if [ "$CF31_KERNEL_OS" != "Linux" ]; then
            echo "NOT APPLICABLE: Copy Fail (CVE-2026-31431) affects Linux kernels only." | sed -${E} "s,.*,${SED_GREEN},"
            exit 0
        fi

        for CF31_CANDIDATE in python3 python; do
            if command -v "$CF31_CANDIDATE" >/dev/null 2>&1 && cf31_py_can_run_probe "$CF31_CANDIDATE"; then
                cf31_run_python_probe "$CF31_CANDIDATE"
                CF31_RC=$?
                case "$CF31_RC" in
                    0|2) exit "$CF31_RC" ;;
                esac
                echo "Python probe inconclusive; falling back to POSIX sh triage."
                break
            fi
        done

        CF31_KERNEL_RELEASE=$(uname -r 2>/dev/null || echo unknown)
        CF31_KV=$(printf '%s\n' "$CF31_KERNEL_RELEASE" | sed 's/^[^0-9]*//; s/[^0-9.].*$//')
        CF31_KERNEL_VERSION="$CF31_KV"

        set -- $(printf '%s\n' "$CF31_KV" | tr '.' ' ')

        CF31_MAJ=$(cf31_num "${1:-0}")
        CF31_MIN=$(cf31_num "${2:-0}")
        CF31_PAT=$(cf31_num "${3:-0}")

        CF31_API=unknown
        CF31_CFG=''

        for CF31_CFG_FILE in /proc/config.gz /boot/config-"$CF31_KERNEL_RELEASE" /lib/modules/"$CF31_KERNEL_RELEASE"/config; do
            [ -r "$CF31_CFG_FILE" ] || continue

            case "$CF31_CFG_FILE" in
                *.gz)
                    if command -v gzip >/dev/null 2>&1; then
                        CF31_CFG_LINE=$(gzip -cd "$CF31_CFG_FILE" 2>/dev/null | grep -E '^(# )?CONFIG_CRYPTO_USER_API_AEAD(=| is not set)' | tail -n 1)
                    else
                        CF31_CFG_LINE=''
                    fi
                    ;;
                *)
                    CF31_CFG_LINE=$(grep -E '^(# )?CONFIG_CRYPTO_USER_API_AEAD(=| is not set)' "$CF31_CFG_FILE" 2>/dev/null | tail -n 1)
                    ;;
            esac

            [ -n "$CF31_CFG_LINE" ] && CF31_CFG=$CF31_CFG_LINE
        done

        case "$CF31_CFG" in
            *'is not set'*) CF31_API=off ;;
            *=y) CF31_API=builtin ;;
            *=m) CF31_API=module ;;
        esac

        if [ "$CF31_API" = unknown ]; then
            if [ -e /sys/module/algif_aead ]; then
                CF31_API=loaded
            elif command -v modinfo >/dev/null 2>&1 && modinfo algif_aead >/dev/null 2>&1; then
                CF31_API=module
            elif find /lib/modules/"$CF31_KERNEL_RELEASE" -name 'algif_aead.ko*' -print 2>/dev/null | grep -q .; then
                CF31_API=module
            elif [ -r /proc/crypto ] && grep -q 'authencesn(hmac(sha256),cbc(aes))' /proc/crypto 2>/dev/null; then
                CF31_API=reachable
            fi
        fi

        if [ "$CF31_API" = off ]; then
            echo "NOT VULNERABLE: CONFIG_CRYPTO_USER_API_AEAD is disabled." | sed -${E} "s,.*,${SED_GREEN},"
            exit 0
        fi

        CF31_BLOCKED=no
        for CF31_CFG_FILE in /etc/modprobe.d/*.conf /lib/modprobe.d/*.conf /usr/lib/modprobe.d/*.conf; do
            [ -f "$CF31_CFG_FILE" ] || continue
            if grep -Eq '^[[:space:]]*install[[:space:]]+algif_aead[[:space:]]+(/usr)?/bin/(false|true)([[:space:]]|$)' "$CF31_CFG_FILE" 2>/dev/null; then
                CF31_BLOCKED=yes
            fi
        done

        if [ "$CF31_API" = module ] && [ "$CF31_BLOCKED" = yes ] && [ ! -e /sys/module/algif_aead ]; then
            echo "NOT VULNERABLE: algif_aead autoload is blocked and the module is not loaded." | sed -${E} "s,.*,${SED_GREEN},"
            exit 0
        fi

        if [ -r /proc/cmdline ] && grep -q 'initcall_blacklist=algif_aead_init' /proc/cmdline 2>/dev/null; then
            echo "LIKELY NOT VULNERABLE: kernel booted with initcall_blacklist=algif_aead_init." | sed -${E} "s,.*,${SED_GREEN},"
            exit 0
        fi

        CF31_FIXED_PKG=no
        if command -v dpkg-query >/dev/null 2>&1; then
            CF31_PKG=$(dpkg-query -S "/boot/vmlinuz-$CF31_KERNEL_RELEASE" 2>/dev/null | sed 's/:.*//' | sed -n '1p')
            if [ -n "$CF31_PKG" ]; then
                for CF31_CFG_FILE in /usr/share/doc/"$CF31_PKG"/changelog*; do
                    [ -f "$CF31_CFG_FILE" ] || continue
                    case "$CF31_CFG_FILE" in
                        *.gz) command -v gzip >/dev/null 2>&1 && gzip -cd "$CF31_CFG_FILE" 2>/dev/null ;;
                        *) cat "$CF31_CFG_FILE" 2>/dev/null ;;
                    esac
                done | grep -Eiq 'CVE-2026-31431|a664bf3d603d|ce42ee423e58|fafe0fa2995a|algif_aead.*out-of-place|Revert to operating out-of-place' && CF31_FIXED_PKG=yes
            fi
        fi

        if [ "$CF31_FIXED_PKG" = no ] && command -v rpm >/dev/null 2>&1; then
            CF31_PKG=$(rpm -q --whatprovides "kernel-uname-r = $CF31_KERNEL_RELEASE" 2>/dev/null | sed -n '1p')
            case "$CF31_PKG" in
                ''|no\ package*) ;;
                *)
                    rpm -q --changelog "$CF31_PKG" 2>/dev/null |
                        grep -Eiq 'CVE-2026-31431|a664bf3d603d|ce42ee423e58|fafe0fa2995a|algif_aead.*out-of-place|Revert to operating out-of-place' && CF31_FIXED_PKG=yes
                    ;;
            esac
        fi

        if [ "$CF31_FIXED_PKG" = yes ]; then
            echo "LIKELY NOT VULNERABLE: running kernel package changelog mentions the CVE-2026-31431 fix." | sed -${E} "s,.*,${SED_GREEN},"
            exit 0
        fi

        if [ "$CF31_MAJ" -lt 4 ] || { [ "$CF31_MAJ" -eq 4 ] && [ "$CF31_MIN" -lt 14 ]; }; then
            echo "NOT VULNERABLE for upstream kernel version: $CF31_KERNEL_RELEASE predates the vulnerable upstream commit." | sed -${E} "s,.*,${SED_GREEN},"
            exit 0
        fi

        if cf31_is_fixed_upstream_release; then
            echo "LIKELY NOT VULNERABLE for upstream kernel version: $CF31_KERNEL_RELEASE is at/after a fixed upstream release." | sed -${E} "s,.*,${SED_GREEN},"
            exit 0
        fi

        if [ "$CF31_API" = unknown ]; then
            echo "UNKNOWN: $CF31_KERNEL_RELEASE is in the affected upstream range, but AEAD user API exposure could not be verified." | sed -${E} "s,.*,${SED_RED_YELLOW},"
            exit 1
        fi

        echo "LIKELY VULNERABLE: $CF31_KERNEL_RELEASE is in the affected upstream range and AEAD user API appears $CF31_API." | sed -${E} "s,.*,${SED_RED_YELLOW},"
        exit 2
    )
}