#define SQLITE_OS_WIN

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HANDLE = System.IntPtr;
using DWORD = System.UInt64;
using i64 = System.Int64;
using u8 = System.Byte;
using u32 = System.UInt32;

using sqlite3_int64 = System.Int64;

namespace winPEAS._3rdParty.SQLite.src
{
  internal static class HelperMethods
  {
    public static bool IsRunningMediumTrust()
    {
      // placeholder method
      // this is where it needs to check if it's running in an ASP.Net MediumTrust or lower environment
      // in order to pick the appropriate locking strategy
      return false;
    }
  }

  public partial class CSSQLite
  {
    /// <summary>
    /// Basic locking strategy for Console/Winform applications
    /// </summary>
    private class LockingStrategy
    {
      [DllImport( "kernel32.dll" )]
      static extern bool LockFileEx( IntPtr hFile, uint dwFlags, uint dwReserved,
      uint nNumberOfBytesToLockLow, uint nNumberOfBytesToLockHigh,
      [In] ref System.Threading.NativeOverlapped lpOverlapped );

      const int LOCKFILE_FAIL_IMMEDIATELY = 1;

      public virtual void LockFile( sqlite3_file pFile, long offset, long length )
      {
        pFile.fs.Lock( offset, length );
      }

      public virtual int SharedLockFile( sqlite3_file pFile, long offset, long length )
      {
        Debug.Assert( length == SHARED_SIZE );
        Debug.Assert( offset == SHARED_FIRST );
                NativeOverlapped ovlp = new System.Threading.NativeOverlapped
                {
                    OffsetLow = (int)offset,
                    OffsetHigh = 0,
                    EventHandle = IntPtr.Zero
                };

                return LockFileEx( pFile.fs.Handle, LOCKFILE_FAIL_IMMEDIATELY, 0, (uint)length, 0, ref ovlp ) ? 1 : 0;
      }

      public virtual void UnlockFile( sqlite3_file pFile, long offset, long length )
      {
        pFile.fs.Unlock( offset, length );
      }
    }

    /// <summary>
    /// Locking strategy for Medium Trust. It uses the same trick used in the native code for WIN_CE
    /// which doesn't support LockFileEx as well.
    /// </summary>
    private class MediumTrustLockingStrategy : LockingStrategy
    {
      public override int SharedLockFile( sqlite3_file pFile, long offset, long length )
      {
        Debug.Assert( length == SHARED_SIZE );
        Debug.Assert( offset == SHARED_FIRST );
        try
        {
          pFile.fs.Lock( offset + pFile.sharedLockByte, 1 );
        }
        catch ( IOException )
        {
          return 0;
        }
        return 1;
      }
    }



    /*
    ** 2004 May 22
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    ******************************************************************************
    **
    ** This file contains code that is specific to windows.
    **
    ** $Id: os_win.c,v 1.157 2009/08/05 04:08:30 shane Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
#if SQLITE_OS_WIN               // * This file is used for windows only */


    /*
** A Note About Memory Allocation:
**
** This driver uses malloc()/free() directly rather than going through
** the SQLite-wrappers sqlite3Malloc()///sqlite3DbFree(db,ref  ).  Those wrappers
** are designed for use on embedded systems where memory is scarce and
** malloc failures happen frequently.  Win32 does not typically run on
** embedded systems, and when it does the developers normally have bigger
** problems to worry about than running out of memory.  So there is not
** a compelling need to use the wrappers.
**
** But there is a good reason to not use the wrappers.  If we use the
** wrappers then we will get simulated malloc() failures within this
** driver.  And that causes all kinds of problems for our tests.  We
** could enhance SQLite to deal with simulated malloc failures within
** the OS driver, but the code to deal with those failure would not
** be exercised on Linux (which does not need to malloc() in the driver)
** and so we would have difficulty writing coverage tests for that
** code.  Better to leave the code out, we think.
**
** The point of this discussion is as follows:  When creating a new
** OS layer for an embedded system, if you use this file as an example,
** avoid the use of malloc()/free().  Those routines work ok on windows
** desktops but not so well in embedded systems.
*/

    //#include <winbase.h>

#if __CYGWIN__
//# include <sys/cygwin.h>
#endif

    /*
** Macros used to determine whether or not to use threads.
*/
#if THREADSAFE
//# define SQLITE_W32_THREADS 1
#endif

    /*
** Include code that is common to all os_*.c files
*/
    //#include "os_common.h"

    /*
    ** Some microsoft compilers lack this definition.
    */
#if !INVALID_FILE_ATTRIBUTES
    //# define INVALID_FILE_ATTRIBUTES ((DWORD)-1)
    const int INVALID_FILE_ATTRIBUTES = -1;
#endif

    /*
** Determine if we are dealing with WindowsCE - which has a much
** reduced API.
*/
#if SQLITE_OS_WINCE
//# define AreFileApisANSI() 1
//# define GetDiskFreeSpaceW() 0
#endif

    /*
** WinCE lacks native support for file locking so we have to fake it
** with some code of our own.
*/
#if SQLITE_OS_WINCE
typedef struct winceLock {
int nReaders;       /* Number of reader locks obtained */
BOOL bPending;      /* Indicates a pending lock has been obtained */
BOOL bReserved;     /* Indicates a reserved lock has been obtained */
BOOL bExclusive;    /* Indicates an exclusive lock has been obtained */
} winceLock;
#endif

    private static LockingStrategy lockingStrategy = HelperMethods.IsRunningMediumTrust() ? new MediumTrustLockingStrategy() : new LockingStrategy();

    /*
    ** The winFile structure is a subclass of sqlite3_file* specific to the win32
    ** portability layer.
    */
    //typedef struct sqlite3_file sqlite3_file;
    public partial class sqlite3_file
    {
      public FileStream fs;          /* Filestream access to this file*/
      // public HANDLE h;            /* Handle for accessing the file */
      public int locktype;           /* Type of lock currently held on this file */
      public int sharedLockByte;     /* Randomly chosen byte used as a shared lock */
      public DWORD lastErrno;        /* The Windows errno from the last I/O error */
      public DWORD sectorSize;       /* Sector size of the device file is on */
#if SQLITE_OS_WINCE
WCHAR *zDeleteOnClose;  /* Name of file to delete when closing */
HANDLE hMutex;          /* Mutex used to control access to shared lock */
HANDLE hShared;         /* Shared memory segment used for locking */
winceLock local;        /* Locks obtained by this instance of sqlite3_file */
winceLock *shared;      /* Global shared lock memory for the file  */
#endif

      public void Clear()
      {
        pMethods = null;
        fs = null;
        locktype = 0;
        sharedLockByte = 0;
        lastErrno = 0;
        sectorSize = 0;
      }
    };

    /*
    ** Forward prototypes.
    */
    //static int getSectorSize(
    //    sqlite3_vfs *pVfs,
    //    const char *zRelative     /* UTF-8 file name */
    //);

    /*
    ** The following variable is (normally) set once and never changes
    ** thereafter.  It records whether the operating system is Win95
    ** or WinNT.
    **
    ** 0:   Operating system unknown.
    ** 1:   Operating system is Win95.
    ** 2:   Operating system is WinNT.
    **
    ** In order to facilitate testing on a WinNT system, the test fixture
    ** can manually set this value to 1 to emulate Win98 behavior.
    */
#if SQLITE_TEST
    int sqlite3_os_type = 0;
#else
static int sqlite3_os_type = 0;
#endif

    /*
** Return true (non-zero) if we are running under WinNT, Win2K, WinXP,
** or WinCE.  Return false (zero) for Win95, Win98, or WinME.
**
** Here is an interesting observation:  Win95, Win98, and WinME lack
** the LockFileEx() API.  But we can still statically link against that
** API as long as we don't call it when running Win95/98/ME.  A call to
** this routine is used to determine if the host is Win95/98/ME or
** WinNT/2K/XP so that we will know whether or not we can safely call
** the LockFileEx() API.
*/
#if SQLITE_OS_WINCE
//# define isNT()  (1)
#else
    static bool isNT()
    {
      //if (sqlite3_os_type == 0)
      //{
      //  OSVERSIONINFO sInfo;
      //  sInfo.dwOSVersionInfoSize = sInfo.Length;
      //  GetVersionEx(&sInfo);
      //  sqlite3_os_type = sInfo.dwPlatformId == VER_PLATFORM_WIN32_NT ? 2 : 1;
      //}
      //return sqlite3_os_type == 2;
      return Environment.OSVersion.Platform >= PlatformID.Win32NT;
    }
#endif // * SQLITE_OS_WINCE */

    /*
** Convert a UTF-8 string to microsoft unicode (UTF-16?).
**
** Space to hold the returned string is obtained from malloc.
*/
    //static WCHAR *utf8ToUnicode(string zFilename){
    //  int nChar;
    //  WCHAR *zWideFilename;

    //  nChar = MultiByteToWideChar(CP_UTF8, 0, zFilename, -1, NULL, 0);
    //  zWideFilename = malloc( nChar*sizeof(zWideFilename[0]) );
    //  if( zWideFilename==0 ){
    //    return 0;
    //  }
    //  nChar = MultiByteToWideChar(CP_UTF8, 0, zFilename, -1, zWideFilename, nChar);
    //  if( nChar==0 ){
    //    free(zWideFilename);
    //    zWideFileName = "";
    //  }
    //  return zWideFilename;
    //}

    /*
    ** Convert microsoft unicode to UTF-8.  Space to hold the returned string is
    ** obtained from malloc().
    */
    //static char *unicodeToUtf8(const WCHAR *zWideFilename){
    //  int nByte;
    //  char *zFilename;

    //  nByte = WideCharToMultiByte(CP_UTF8, 0, zWideFilename, -1, 0, 0, 0, 0);
    //  zFilename = malloc( nByte );
    //  if( zFilename==0 ){
    //    return 0;
    //  }
    //  nByte = WideCharToMultiByte(CP_UTF8, 0, zWideFilename, -1, zFilename, nByte,
    //                              0, 0);
    //  if( nByte == 0 ){
    //    free(zFilename);
    //    zFileName = "";
    //  }
    //  return zFilename;
    //}

    /*
    ** Convert an ansi string to microsoft unicode, based on the
    ** current codepage settings for file apis.
    **
    ** Space to hold the returned string is obtained
    ** from malloc.
    */
    //static WCHAR *mbcsToUnicode(string zFilename){
    //  int nByte;
    //  WCHAR *zMbcsFilename;
    //  int codepage = AreFileApisANSI() ? CP_ACP : CP_OEMCP;

    //  nByte = MultiByteToWideChar(codepage, 0, zFilename, -1, NULL,0)*WCHAR.Length;
    //  zMbcsFilename = malloc( nByte*sizeof(zMbcsFilename[0]) );
    //  if( zMbcsFilename==0 ){
    //    return 0;
    //  }
    //  nByte = MultiByteToWideChar(codepage, 0, zFilename, -1, zMbcsFilename, nByte);
    //  if( nByte==0 ){
    //    free(zMbcsFilename);
    //    zMbcsFileName = "";
    //  }
    //  return zMbcsFilename;
    //}

    /*
    ** Convert microsoft unicode to multibyte character string, based on the
    ** user's Ansi codepage.
    **
    ** Space to hold the returned string is obtained from
    ** malloc().
    */
    //static char *unicodeToMbcs(const WCHAR *zWideFilename){
    //  int nByte;
    //  char *zFilename;
    //  int codepage = AreFileApisANSI() ? CP_ACP : CP_OEMCP;

    //  nByte = WideCharToMultiByte(codepage, 0, zWideFilename, -1, 0, 0, 0, 0);
    //  zFilename = malloc( nByte );
    //  if( zFilename==0 ){
    //    return 0;
    //  }
    //  nByte = WideCharToMultiByte(codepage, 0, zWideFilename, -1, zFilename, nByte,
    //                              0, 0);
    //  if( nByte == 0 ){
    //    free(zFilename);
    //    zFileName = "";
    //  }
    //  return zFilename;
    //}

    /*
    ** Convert multibyte character string to UTF-8.  Space to hold the
    ** returned string is obtained from malloc().
    */
    //static char *sqlite3_win32_mbcs_to_utf8(string zFilename){
    //  char *zFilenameUtf8;
    //  WCHAR *zTmpWide;

    //  zTmpWide = mbcsToUnicode(zFilename);
    //  if( zTmpWide==0 ){
    //    return 0;
    //  }
    //  zFilenameUtf8 = unicodeToUtf8(zTmpWide);
    //  free(zTmpWide);
    //  return zFilenameUtf8;
    //}

    /*
    ** Convert UTF-8 to multibyte character string.  Space to hold the
    ** returned string is obtained from malloc().
    */
    //static char *utf8ToMbcs(string zFilename){
    //  char *zFilenameMbcs;
    //  WCHAR *zTmpWide;

    //  zTmpWide = utf8ToUnicode(zFilename);
    //  if( zTmpWide==0 ){
    //    return 0;
    //  }
    //  zFilenameMbcs = unicodeToMbcs(zTmpWide);
    //  free(zTmpWide);
    //  return zFilenameMbcs;
    //}

#if SQLITE_OS_WINCE
/*************************************************************************
** This section contains code for WinCE only.
*/
/*
** WindowsCE does not have a localtime() function.  So create a
** substitute.
*/
//#include <time.h>
struct tm *__cdecl localtime(const time_t *t)
{
static struct tm y;
FILETIME uTm, lTm;
SYSTEMTIME pTm;
sqlite3_int64 t64;
t64 = *t;
t64 = (t64 + 11644473600)*10000000;
uTm.dwLowDateTime = t64 & 0xFFFFFFFF;
uTm.dwHighDateTime= t64 >> 32;
FileTimeToLocalFileTime(&uTm,&lTm);
FileTimeToSystemTime(&lTm,&pTm);
y.tm_year = pTm.wYear - 1900;
y.tm_mon = pTm.wMonth - 1;
y.tm_wday = pTm.wDayOfWeek;
y.tm_mday = pTm.wDay;
y.tm_hour = pTm.wHour;
y.tm_min = pTm.wMinute;
y.tm_sec = pTm.wSecond;
return &y;
}

/* This will never be called, but defined to make the code compile */
//#define GetTempPathA(a,b)

//#define LockFile(a,b,c,d,e)       winceLockFile(&a, b, c, d, e)
//#define UnlockFile(a,b,c,d,e)     winceUnlockFile(&a, b, c, d, e)
//#define LockFileEx(a,b,c,d,e,f)   winceLockFileEx(&a, b, c, d, e, f)

//#define HANDLE_TO_WINFILE(a) (sqlite3_file*)&((char*)a)[-offsetof(sqlite3_file,h)]

/*
** Acquire a lock on the handle h
*/
static void winceMutexAcquire(HANDLE h){
DWORD dwErr;
do {
dwErr = WaitForSingleObject(h, INFINITE);
} while (dwErr != WAIT_OBJECT_0 && dwErr != WAIT_ABANDONED);
}
/*
** Release a lock acquired by winceMutexAcquire()
*/
//#define winceMutexRelease(h) ReleaseMutex(h)

/*
** Create the mutex and shared memory used for locking in the file
** descriptor pFile
*/
static BOOL winceCreateLock(string zFilename, sqlite3_file pFile){
WCHAR *zTok;
WCHAR *zName = utf8ToUnicode(zFilename);
BOOL bInit = TRUE;

/* Initialize the local lockdata */
ZeroMemory(pFile.local, pFile.local).Length;

/* Replace the backslashes from the filename and lowercase it
** to derive a mutex name. */
zTok = CharLowerW(zName);
for (;*zTok;zTok++){
if (*zTok == '\\') *zTok = '_';
}

/* Create/open the named mutex */
pFile.hMutex = CreateMutexW(NULL, FALSE, zName);
if (!pFile.hMutex){
pFile->lastErrno = (u32)GetLastError();
free(zName);
return FALSE;
}

/* Acquire the mutex before continuing */
winceMutexAcquire(pFile.hMutex);

/* Since the names of named mutexes, semaphores, file mappings etc are
** case-sensitive, take advantage of that by uppercasing the mutex name
** and using that as the shared filemapping name.
*/
CharUpperW(zName);
pFile.hShared = CreateFileMappingW(INVALID_HANDLE_VALUE, NULL,
PAGE_READWRITE, 0, winceLock.Length,
zName);

/* Set a flag that indicates we're the first to create the memory so it
** must be zero-initialized */
if (GetLastError() == ERROR_ALREADY_EXISTS){
bInit = FALSE;
}

free(zName);

/* If we succeeded in making the shared memory handle, map it. */
if (pFile.hShared){
pFile.shared = (winceLock*)MapViewOfFile(pFile.hShared,
FILE_MAP_READ|FILE_MAP_WRITE, 0, 0, winceLock).Length;
/* If mapping failed, close the shared memory handle and erase it */
if (!pFile.shared){
pFile->lastErrno = (u32)GetLastError();
CloseHandle(pFile.hShared);
pFile.hShared = NULL;
}
}

/* If shared memory could not be created, then close the mutex and fail */
if (pFile.hShared == NULL){
winceMutexRelease(pFile.hMutex);
CloseHandle(pFile.hMutex);
pFile.hMutex = NULL;
return FALSE;
}

/* Initialize the shared memory if we're supposed to */
if (bInit) {
ZeroMemory(pFile.shared, winceLock).Length;
}

winceMutexRelease(pFile.hMutex);
return TRUE;
}

/*
** Destroy the part of sqlite3_file that deals with wince locks
*/
static void winceDestroyLock(sqlite3_file pFile){
if (pFile.hMutex){
/* Acquire the mutex */
winceMutexAcquire(pFile.hMutex);

/* The following blocks should probably Debug.Assert in debug mode, but they
are to cleanup in case any locks remained open */
if (pFile.local.nReaders){
pFile.shared.nReaders --;
}
if (pFile.local.bReserved){
pFile.shared.bReserved = FALSE;
}
if (pFile.local.bPending){
pFile.shared.bPending = FALSE;
}
if (pFile.local.bExclusive){
pFile.shared.bExclusive = FALSE;
}

/* De-reference and close our copy of the shared memory handle */
UnmapViewOfFile(pFile.shared);
CloseHandle(pFile.hShared);

/* Done with the mutex */
winceMutexRelease(pFile.hMutex);
CloseHandle(pFile.hMutex);
pFile.hMutex = NULL;
}
}

/*
** An implementation of the LockFile() API of windows for wince
*/
static BOOL winceLockFile(
HANDLE phFile,
DWORD dwFileOffsetLow,
DWORD dwFileOffsetHigh,
DWORD nNumberOfBytesToLockLow,
DWORD nNumberOfBytesToLockHigh
){
sqlite3_file pFile = HANDLE_TO_WINFILE(phFile);
BOOL bReturn = FALSE;

if (!pFile.hMutex) return TRUE;
winceMutexAcquire(pFile.hMutex);

/* Wanting an exclusive lock? */
if (dwFileOffsetLow == SHARED_FIRST
&& nNumberOfBytesToLockLow == SHARED_SIZE){
if (pFile.shared.nReaders == 0 && pFile.shared.bExclusive == 0){
pFile.shared.bExclusive = TRUE;
pFile.local.bExclusive = TRUE;
bReturn = TRUE;
}
}

/* Want a read-only lock? */
else if (dwFileOffsetLow == SHARED_FIRST &&
nNumberOfBytesToLockLow == 1){
if (pFile.shared.bExclusive == 0){
pFile.local.nReaders ++;
if (pFile.local.nReaders == 1){
pFile.shared.nReaders ++;
}
bReturn = TRUE;
}
}

/* Want a pending lock? */
else if (dwFileOffsetLow == PENDING_BYTE && nNumberOfBytesToLockLow == 1){
/* If no pending lock has been acquired, then acquire it */
if (pFile.shared.bPending == 0) {
pFile.shared.bPending = TRUE;
pFile.local.bPending = TRUE;
bReturn = TRUE;
}
}
/* Want a reserved lock? */
else if (dwFileOffsetLow == RESERVED_BYTE && nNumberOfBytesToLockLow == 1){
if (pFile.shared.bReserved == 0) {
pFile.shared.bReserved = TRUE;
pFile.local.bReserved = TRUE;
bReturn = TRUE;
}
}

winceMutexRelease(pFile.hMutex);
return bReturn;
}

/*
** An implementation of the UnlockFile API of windows for wince
*/
static BOOL winceUnlockFile(
HANDLE phFile,
DWORD dwFileOffsetLow,
DWORD dwFileOffsetHigh,
DWORD nNumberOfBytesToUnlockLow,
DWORD nNumberOfBytesToUnlockHigh
){
sqlite3_file pFile = HANDLE_TO_WINFILE(phFile);
BOOL bReturn = FALSE;

if (!pFile.hMutex) return TRUE;
winceMutexAcquire(pFile.hMutex);

/* Releasing a reader lock or an exclusive lock */
if (dwFileOffsetLow >= SHARED_FIRST &&
dwFileOffsetLow < SHARED_FIRST + SHARED_SIZE){
/* Did we have an exclusive lock? */
if (pFile.local.bExclusive){
pFile.local.bExclusive = FALSE;
pFile.shared.bExclusive = FALSE;
bReturn = TRUE;
}

/* Did we just have a reader lock? */
else if (pFile.local.nReaders){
pFile.local.nReaders --;
if (pFile.local.nReaders == 0)
{
pFile.shared.nReaders --;
}
bReturn = TRUE;
}
}

/* Releasing a pending lock */
else if (dwFileOffsetLow == PENDING_BYTE && nNumberOfBytesToUnlockLow == 1){
if (pFile.local.bPending){
pFile.local.bPending = FALSE;
pFile.shared.bPending = FALSE;
bReturn = TRUE;
}
}
/* Releasing a reserved lock */
else if (dwFileOffsetLow == RESERVED_BYTE && nNumberOfBytesToUnlockLow == 1){
if (pFile.local.bReserved) {
pFile.local.bReserved = FALSE;
pFile.shared.bReserved = FALSE;
bReturn = TRUE;
}
}

winceMutexRelease(pFile.hMutex);
return bReturn;
}

/*
** An implementation of the LockFileEx() API of windows for wince
*/
static BOOL winceLockFileEx(
HANDLE phFile,
DWORD dwFlags,
DWORD dwReserved,
DWORD nNumberOfBytesToLockLow,
DWORD nNumberOfBytesToLockHigh,
LPOVERLAPPED lpOverlapped
){
/* If the caller wants a shared read lock, forward this call
** to winceLockFile */
if (lpOverlapped.Offset == SHARED_FIRST &&
dwFlags == 1 &&
nNumberOfBytesToLockLow == SHARED_SIZE){
return winceLockFile(phFile, SHARED_FIRST, 0, 1, 0);
}
return FALSE;
}
/*
** End of the special code for wince
*****************************************************************************/
#endif // * SQLITE_OS_WINCE */

    /*****************************************************************************
** The next group of routines implement the I/O methods specified
** by the sqlite3_io_methods object.
******************************************************************************/

    /*
    ** Close a file.
    **
    ** It is reported that an attempt to close a handle might sometimes
    ** fail.  This is a very unreasonable result, but windows is notorious
    ** for being unreasonable so I do not doubt that it might happen.  If
    ** the close fails, we pause for 100 milliseconds and try again.  As
    ** many as MX_CLOSE_ATTEMPT attempts to close the handle are made before
    ** giving up and returning an error.
    */
    public static int MX_CLOSE_ATTEMPT = 3;
    static int winClose( sqlite3_file id )
    {
      bool rc;
      int cnt = 0;
      sqlite3_file pFile = (sqlite3_file)id;

      Debug.Assert( id != null );
#if SQLITE_DEBUG
      OSTRACE3( "CLOSE %d (%s)\n", pFile.fs.GetHashCode(), pFile.fs.Name );
#endif
      do
      {
        pFile.fs.Close();
        rc = true;
        //  rc = CloseHandle(pFile.h);
        //  if (!rc && ++cnt < MX_CLOSE_ATTEMPT) Thread.Sleep(100); //, 1) );
      } while ( !rc && ++cnt < MX_CLOSE_ATTEMPT ); //, 1) );
#if SQLITE_OS_WINCE
//#define WINCE_DELETION_ATTEMPTS 3
winceDestroyLock(pFile);
if( pFile.zDeleteOnClose ){
int cnt = 0;
while(
DeleteFileW(pFile.zDeleteOnClose)==0
&& GetFileAttributesW(pFile.zDeleteOnClose)!=0xffffffff
&& cnt++ < WINCE_DELETION_ATTEMPTS
){
Sleep(100);  /* Wait a little before trying again */
}
free(pFile.zDeleteOnClose);
}
#endif
#if SQLITE_TEST
      OpenCounter( -1 );
#endif
      return rc ? SQLITE_OK : SQLITE_IOERR;
    }

    /*
    ** Some microsoft compilers lack this definition.
    */
#if !INVALID_SET_FILE_POINTER
    const int INVALID_SET_FILE_POINTER = -1;
#endif

    /*
** Read data from a file into a buffer.  Return SQLITE_OK if all
** bytes were read successfully and SQLITE_IOERR if anything goes
** wrong.
*/
    static int winRead(
    sqlite3_file id,           /* File to read from */
    byte[] pBuf,           /* Write content into this buffer */
    int amt,                   /* Number of bytes to read */
    sqlite3_int64 offset       /* Begin reading at this offset */
    )
    {

      //LONG upperBits = (LONG)( ( offset >> 32 ) & 0x7fffffff );
      //LONG lowerBits = (LONG)( offset & 0xffffffff );
      long rc;
      sqlite3_file pFile = id;
      //DWORD error;
      long got;

      Debug.Assert( id != null );
#if SQLITE_TEST
      //SimulateIOError(return SQLITE_IOERR_READ);  TODO --  How to implement this?
#endif
#if SQLITE_DEBUG
      OSTRACE3( "READ %d lock=%d\n", pFile.fs.GetHashCode(), pFile.locktype );
#endif
      if ( !id.fs.CanRead ) return SQLITE_IOERR_READ;
      try
      {
        rc = id.fs.Seek( offset, SeekOrigin.Begin ); // SetFilePointer(pFile.fs.Name, lowerBits, upperBits, FILE_BEGIN);
      }
      catch ( Exception e )      //            if( rc==INVALID_SET_FILE_POINTER && (error=GetLastError())!=NO_ERROR )
      {
        pFile.lastErrno = (u32)Marshal.GetLastWin32Error();
        return SQLITE_FULL;
      }

      try
      {
        got = id.fs.Read( pBuf, 0, amt ); // if (!ReadFile(pFile.fs.Name, pBuf, amt, got, 0))
      }
      catch ( Exception e )
      {
        pFile.lastErrno = (u32)Marshal.GetLastWin32Error();
        return SQLITE_IOERR_READ;
      }
      if ( got == amt )
      {
        return SQLITE_OK;
      }
      else
      {
        /* Unread parts of the buffer must be zero-filled */
        Array.Clear( pBuf, (int)got, (int)( amt - got ) ); // memset(&((char*)pBuf)[got], 0, amt - got);
        return SQLITE_IOERR_SHORT_READ;
      }
    }

    /*
    ** Write data from a buffer into a file.  Return SQLITE_OK on success
    ** or some other error code on failure.
    */
    static int winWrite(
    sqlite3_file id,          /* File to write into */
    byte[] pBuf,              /* The bytes to be written */
    int amt,                  /* Number of bytes to write */
    sqlite3_int64 offset      /* Offset into the file to begin writing at */
    )
    {
      //LONG upperBits = (LONG)( ( offset >> 32 ) & 0x7fffffff );
      //LONG lowerBits = (LONG)( offset & 0xffffffff );
      int rc;
      //  sqlite3_file pFile = (sqlite3_file*)id;
      //  DWORD error;
      long wrote = 0;

      Debug.Assert( id != null );
#if SQLITE_TEST
      if ( SimulateIOError() ) return SQLITE_IOERR_WRITE;
      if ( SimulateDiskfullError() ) return SQLITE_FULL;
#endif
#if SQLITE_DEBUG
      OSTRACE3( "WRITE %d lock=%d\n", id.fs.GetHashCode(), id.locktype );
#endif
      //  rc = SetFilePointer(pFile.fs.Name, lowerBits, upperBits, FILE_BEGIN);
      id.fs.Seek( offset, SeekOrigin.Begin );
      //  if( rc==INVALID_SET_FILE_POINTER && GetLastError()!=NO_ERROR ){
      //  pFile.lastErrno = (u32)GetLastError();
      //    return SQLITE_FULL;
      //  }
      Debug.Assert( amt > 0 );
      wrote = id.fs.Position;
      try
      {
        Debug.Assert( pBuf.Length >= amt );
        id.fs.Write( pBuf, 0, amt );
        rc = 1;// Success
        wrote = id.fs.Position - wrote;
      }
      catch ( IOException e )
      {
        return SQLITE_READONLY;
      }
      //  while(
      //     amt>0
      //     && (rc = WriteFile(pFile.fs.Name, pBuf, amt, wrote, 0))!=0
      //     && wrote>0
      //  ){
      //    amt -= wrote;
      //    pBuf = &((char*)pBuf)[wrote];
      //  }
      if ( rc == 0 || amt > (int)wrote )
      {
        id.lastErrno = (u32)Marshal.GetLastWin32Error();
        return SQLITE_FULL;
      }
      return SQLITE_OK;
    }

    /*
    ** Truncate an open file to a specified size
    */
    static int winTruncate( sqlite3_file id, sqlite3_int64 nByte )
    {
      //LONG upperBits = (LONG)( ( nByte >> 32 ) & 0x7fffffff );
      //LONG lowerBits = (LONG)( nByte & 0xffffffff );
      //DWORD rc;
      //winFile* pFile = (winFile*)id;
      //DWORD error;

      Debug.Assert( id != null );
#if SQLITE_DEBUG
      OSTRACE3( "TRUNCATE %d %lld\n", id.fs.Name, nByte );
#endif
#if SQLITE_TEST
      //SimulateIOError(return SQLITE_IOERR_TRUNCATE);  TODO --  How to implement this?
#endif
      //rc = SetFilePointer( pFile->h, lowerBits, &upperBits, FILE_BEGIN );
      //if ( rc == INVALID_SET_FILE_POINTER && ( error = GetLastError() ) != NO_ERROR )
      //{
      //  pFile->lastErrno = error;
      //  return SQLITE_IOERR_TRUNCATE;
      //}
      ///* SetEndOfFile will fail if nByte is negative */
      //if ( !SetEndOfFile( pFile->h ) )
      //{
      //  pFile->lastErrno = GetLastError();
      //  return SQLITE_IOERR_TRUNCATE;
      //}
      try
      {
        id.fs.SetLength( nByte );
      }
      catch ( IOException e )
      {
        id.lastErrno = (u32)Marshal.GetLastWin32Error();
        return SQLITE_IOERR_TRUNCATE;
      }
      return SQLITE_OK;
    }

#if SQLITE_TEST
    /*
** Count the number of fullsyncs and normal syncs.  This is used to test
** that syncs and fullsyncs are occuring at the right times.
*/
    static int sqlite3_sync_count = 0;
    static int sqlite3_fullsync_count = 0;
#endif

    /*
** Make sure all writes to a particular file are committed to disk.
*/
    static int winSync( sqlite3_file id, int flags )
    {
#if !SQLITE_NO_SYNC
      sqlite3_file pFile = (sqlite3_file)id;
      Debug.Assert( id != null );
#if SQLITE_DEBUG
      OSTRACE3( "SYNC %d lock=%d\n", pFile.fs.GetHashCode(), pFile.locktype );
#endif
#else
UNUSED_PARAMETER(id);
#endif
#if !SQLITE_TEST
UNUSED_PARAMETER(flags);
#else
      if ( ( flags & SQLITE_SYNC_FULL ) != 0 )
      {
        sqlite3_fullsync_count++;
      }
      sqlite3_sync_count++;
#endif
      /* If we compiled with the SQLITE_NO_SYNC flag, then syncing is a
** no-op
*/
#if SQLITE_NO_SYNC
return SQLITE_OK;
#else
      pFile.fs.Flush();
      return SQLITE_OK;
      //if (FlushFileBuffers(pFile.h) != 0)
      //{
      //  return SQLITE_OK;
      //}
      //else
      //{
      //  pFile->lastErrno = (u32)GetLastError();
      //  return SQLITE_IOERR;
      //}
#endif
    }

    /*
    ** Determine the current size of a file in bytes
    */
    static int sqlite3_fileSize( sqlite3_file id, ref int pSize )
    {
      //DWORD upperBits;
      //DWORD lowerBits;
      //  sqlite3_file pFile = (sqlite3_file)id;
      //  DWORD error;
      Debug.Assert( id != null );
#if SQLITE_TEST
      //SimulateIOError(return SQLITE_IOERR_FSTAT);  TODO --  How to implement this?
#endif
      //lowerBits = GetFileSize(pFile.fs.Name, upperBits);
      //if ( ( lowerBits == INVALID_FILE_SIZE )
      //   && ( ( error = GetLastError() ) != NO_ERROR ) )
      //{
      //  pFile->lastErrno = error;
      //  return SQLITE_IOERR_FSTAT;
      //}
      //pSize = (((sqlite3_int64)upperBits)<<32) + lowerBits;
      pSize = id.fs.CanRead ? (int)id.fs.Length : 0;
      return SQLITE_OK;
    }


    /*
    ** Acquire a reader lock.
    ** Different API routines are called depending on whether or not this
    ** is Win95 or WinNT.
    */
    static int getReadLock( sqlite3_file pFile )
    {
      int res = 0;
      if ( isNT() )
      {
        res = lockingStrategy.SharedLockFile( pFile, SHARED_FIRST, SHARED_SIZE );
      }
      /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
      */
#if !SQLITE_OS_WINCE
      //else
      //{
      //  int lk;
      //  sqlite3_randomness(lk.Length, lk);
      //  pFile->sharedLockByte = (u16)((lk & 0x7fffffff)%(SHARED_SIZE - 1));
      //  res = pFile.fs.Lock( SHARED_FIRST + pFile.sharedLockByte, 0, 1, 0);
#endif
      //}
      if ( res == 0 )
      {
        pFile.lastErrno = (u32)Marshal.GetLastWin32Error();
      }
      return res;
    }

    /*
    ** Undo a readlock
    */
    static int unlockReadLock( sqlite3_file pFile )
    {
      int res = 1;
      if ( isNT() )
      {
        try
        {
          lockingStrategy.UnlockFile( pFile, SHARED_FIRST, SHARED_SIZE ); //     res = UnlockFile(pFilE.h, SHARED_FIRST, 0, SHARED_SIZE, 0);
        }
        catch ( Exception e )
        {
          res = 0;
        }
      }
      /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
      */
#if !SQLITE_OS_WINCE
      else
      {
        Debugger.Break(); //    res = UnlockFile(pFilE.h, SHARED_FIRST + pFilE.sharedLockByte, 0, 1, 0);
      }
#endif
      if ( res == 0 )
      {
        pFile.lastErrno = (u32)Marshal.GetLastWin32Error();
      }
      return res;
    }

    /*
    ** Lock the file with the lock specified by parameter locktype - one
    ** of the following:
    **
    **     (1) SHARED_LOCK
    **     (2) RESERVED_LOCK
    **     (3) PENDING_LOCK
    **     (4) EXCLUSIVE_LOCK
    **
    ** Sometimes when requesting one lock state, additional lock states
    ** are inserted in between.  The locking might fail on one of the later
    ** transitions leaving the lock state different from what it started but
    ** still short of its goal.  The following chart shows the allowed
    ** transitions and the inserted intermediate states:
    **
    **    UNLOCKED -> SHARED
    **    SHARED -> RESERVED
    **    SHARED -> (PENDING) -> EXCLUSIVE
    **    RESERVED -> (PENDING) -> EXCLUSIVE
    **    PENDING -> EXCLUSIVE
    **
    ** This routine will only increase a lock.  The winUnlock() routine
    ** erases all locks at once and returns us immediately to locking level 0.
    ** It is not possible to lower the locking level one step at a time.  You
    ** must go straight to locking level 0.
    */
    static int winLock( sqlite3_file id, int locktype )
    {
      int rc = SQLITE_OK;         /* Return code from subroutines */
      int res = 1;                /* Result of a windows lock call */
      int newLocktype;            /* Set pFile.locktype to this value before exiting */
      bool gotPendingLock = false;/* True if we acquired a PENDING lock this time */
      sqlite3_file pFile = (sqlite3_file)id;
      DWORD error = NO_ERROR;

      Debug.Assert( id != null );
#if SQLITE_DEBUG
      OSTRACE5( "LOCK %d %d was %d(%d)\n",
      pFile.fs.GetHashCode(), locktype, pFile.locktype, pFile.sharedLockByte );
#endif
      /* If there is already a lock of this type or more restrictive on the
** OsFile, do nothing. Don't use the end_lock: exit path, as
** sqlite3OsEnterMutex() hasn't been called yet.
*/
      if ( pFile.locktype >= locktype )
      {
        return SQLITE_OK;
      }

      /* Make sure the locking sequence is correct
      */
      Debug.Assert( pFile.locktype != NO_LOCK || locktype == SHARED_LOCK );
      Debug.Assert( locktype != PENDING_LOCK );
      Debug.Assert( locktype != RESERVED_LOCK || pFile.locktype == SHARED_LOCK );

      /* Lock the PENDING_LOCK byte if we need to acquire a PENDING lock or
      ** a SHARED lock.  If we are acquiring a SHARED lock, the acquisition of
      ** the PENDING_LOCK byte is temporary.
      */
      newLocktype = pFile.locktype;
      if ( pFile.locktype == NO_LOCK
      || ( ( locktype == EXCLUSIVE_LOCK )
      && ( pFile.locktype == RESERVED_LOCK ) )
      )
      {
        int cnt = 3;
        res = 0;
        while ( cnt-- > 0 && res == 0 )//(res = LockFile(pFile.fs.SafeFileHandle.DangerousGetHandle().ToInt32(), PENDING_BYTE, 0, 1, 0)) == 0)
        {
          try
          {
            lockingStrategy.LockFile( pFile, PENDING_BYTE, 1 );
            res = 1;
          }
          catch ( Exception e )
          {
            /* Try 3 times to get the pending lock.  The pending lock might be
            ** held by another reader process who will release it momentarily.
            */
#if SQLITE_DEBUG
            OSTRACE2( "could not get a PENDING lock. cnt=%d\n", cnt );
#endif
            Thread.Sleep( 1 );
          }
        }
        gotPendingLock = ( res != 0 );
        if ( 0 == res )
        {
          error = (u32)Marshal.GetLastWin32Error();
        }
      }

      /* Acquire a shared lock
      */
      if ( locktype == SHARED_LOCK && res != 0 )
      {
        Debug.Assert( pFile.locktype == NO_LOCK );
        res = getReadLock( pFile );
        if ( res != 0 )
        {
          newLocktype = SHARED_LOCK;
        }
        else
        {
          error = (u32)Marshal.GetLastWin32Error();
        }
      }

      /* Acquire a RESERVED lock
      */
      if ( ( locktype == RESERVED_LOCK ) && res != 0 )
      {
        Debug.Assert( pFile.locktype == SHARED_LOCK );
        try
        {
          lockingStrategy.LockFile( pFile, RESERVED_BYTE, 1 );//res = LockFile(pFile.fs.SafeFileHandle.DangerousGetHandle().ToInt32(), RESERVED_BYTE, 0, 1, 0);
          newLocktype = RESERVED_LOCK;
          res = 1;
        }
        catch ( Exception e )
        {
          res = 0;
          error = (u32)Marshal.GetLastWin32Error();
        }
        if ( res != 0 )
        {
          newLocktype = RESERVED_LOCK;
        }
        else
        {
          error = (u32)Marshal.GetLastWin32Error();
        }
      }

      /* Acquire a PENDING lock
      */
      if ( locktype == EXCLUSIVE_LOCK && res != 0 )
      {
        newLocktype = PENDING_LOCK;
        gotPendingLock = false;
      }

      /* Acquire an EXCLUSIVE lock
      */
      if ( locktype == EXCLUSIVE_LOCK && res != 0 )
      {
        Debug.Assert( pFile.locktype >= SHARED_LOCK );
        res = unlockReadLock( pFile );
#if SQLITE_DEBUG
        OSTRACE2( "unreadlock = %d\n", res );
#endif
        //res = LockFile(pFile.fs.SafeFileHandle.DangerousGetHandle().ToInt32(), SHARED_FIRST, 0, SHARED_SIZE, 0);
        try
        {
          lockingStrategy.LockFile( pFile, SHARED_FIRST, SHARED_SIZE );
          newLocktype = EXCLUSIVE_LOCK;
          res = 1;
        }
        catch ( Exception e )
        {
          res = 0;
        }
        if ( res != 0 )
        {
          newLocktype = EXCLUSIVE_LOCK;
        }
        else
        {
          error = (u32)Marshal.GetLastWin32Error();
#if SQLITE_DEBUG
          OSTRACE2( "error-code = %d\n", error );
#endif
          getReadLock( pFile );
        }
      }

      /* If we are holding a PENDING lock that ought to be released, then
      ** release it now.
      */
      if ( gotPendingLock && locktype == SHARED_LOCK )
      {
        lockingStrategy.UnlockFile( pFile, PENDING_BYTE, 1 );
      }

      /* Update the state of the lock has held in the file descriptor then
      ** return the appropriate result code.
      */
      if ( res != 0 )
      {
        rc = SQLITE_OK;
      }
      else
      {
#if SQLITE_DEBUG
        OSTRACE4( "LOCK FAILED %d trying for %d but got %d\n", pFile.fs.GetHashCode(),
        locktype, newLocktype );
#endif
        pFile.lastErrno = error;
        rc = SQLITE_BUSY;
      }
      pFile.locktype = (u8)newLocktype;
      return rc;
    }

    /*
    ** This routine checks if there is a RESERVED lock held on the specified
    ** file by this or any other process. If such a lock is held, return
    ** non-zero, otherwise zero.
    */
    static int winCheckReservedLock( sqlite3_file id, ref int pResOut )
    {
      int rc;
      sqlite3_file pFile = (sqlite3_file)id;
      Debug.Assert( id != null );
      if ( pFile.locktype >= RESERVED_LOCK )
      {
        rc = 1;
#if SQLITE_DEBUG
        OSTRACE3( "TEST WR-LOCK %d %d (local)\n", pFile.fs.Name, rc );
#endif
      }
      else
      {
        try
        {
          lockingStrategy.LockFile( pFile, RESERVED_BYTE, 1 );
          lockingStrategy.UnlockFile( pFile, RESERVED_BYTE, 1 );
          rc = 1;
        }
        catch ( IOException e )
        { rc = 0; }
        rc = 1 - rc; // !rc
#if SQLITE_DEBUG
        OSTRACE3( "TEST WR-LOCK %d %d (remote)\n", pFile.fs.GetHashCode(), rc );
#endif
      }
      pResOut = rc;
      return SQLITE_OK;
    }

    /*
    ** Lower the locking level on file descriptor id to locktype.  locktype
    ** must be either NO_LOCK or SHARED_LOCK.
    **
    ** If the locking level of the file descriptor is already at or below
    ** the requested locking level, this routine is a no-op.
    **
    ** It is not possible for this routine to fail if the second argument
    ** is NO_LOCK.  If the second argument is SHARED_LOCK then this routine
    ** might return SQLITE_IOERR;
    */
    static int winUnlock( sqlite3_file id, int locktype )
    {
      int type;
      sqlite3_file pFile = (sqlite3_file)id;
      int rc = SQLITE_OK;
      Debug.Assert( pFile != null );
      Debug.Assert( locktype <= SHARED_LOCK );

#if SQLITE_DEBUG
      OSTRACE5( "UNLOCK %d to %d was %d(%d)\n", pFile.fs.GetHashCode(), locktype,
      pFile.locktype, pFile.sharedLockByte );
#endif
      type = pFile.locktype;
      if ( type >= EXCLUSIVE_LOCK )
      {
        lockingStrategy.UnlockFile( pFile, SHARED_FIRST, SHARED_SIZE ); // UnlockFile(pFilE.h, SHARED_FIRST, 0, SHARED_SIZE, 0);
        if ( locktype == SHARED_LOCK && getReadLock( pFile ) == 0 )
        {
          /* This should never happen.  We should always be able to
          ** reacquire the read lock */
          rc = SQLITE_IOERR_UNLOCK;
        }
      }
      if ( type >= RESERVED_LOCK )
      {
        try
        {
          lockingStrategy.UnlockFile( pFile, RESERVED_BYTE, 1 );// UnlockFile(pFilE.h, RESERVED_BYTE, 0, 1, 0);
        }
        catch ( Exception e ) { }
      }
      if ( locktype == NO_LOCK && type >= SHARED_LOCK )
      {
        unlockReadLock( pFile );
      }
      if ( type >= PENDING_LOCK )
      {
        try
        {
          lockingStrategy.UnlockFile( pFile, PENDING_BYTE, 1 );//    UnlockFile(pFilE.h, PENDING_BYTE, 0, 1, 0);
        }
        catch ( Exception e )
        { }
      }
      pFile.locktype = (u8)locktype;
      return rc;
    }

    /*
    ** Control and query of the open file handle.
    */
    static int winFileControl( sqlite3_file id, int op, ref int pArg )
    {
      switch ( op )
      {
        case SQLITE_FCNTL_LOCKSTATE:
          {
            pArg = ( (sqlite3_file)id ).locktype;
            return SQLITE_OK;
          }
        case SQLITE_LAST_ERRNO:
          {
            pArg = (int)( (sqlite3_file)id ).lastErrno;
            return SQLITE_OK;
          }
      }
      return SQLITE_ERROR;
    }

    /*
    ** Return the sector size in bytes of the underlying block device for
    ** the specified file. This is almost always 512 bytes, but may be
    ** larger for some devices.
    **
    ** SQLite code assumes this function cannot fail. It also assumes that
    ** if two files are created in the same file-system directory (i.e.
    ** a database and its journal file) that the sector size will be the
    ** same for both.
    */
    static int winSectorSize( sqlite3_file id )
    {
      Debug.Assert( id != null );
      return (int)( id.sectorSize );
    }

    /*
    ** Return a vector of device characteristics.
    */
    static int winDeviceCharacteristics( sqlite3_file id )
    {
      UNUSED_PARAMETER( id );
      return 0;
    }

    /*
    ** This vector defines all the methods that can operate on an
    ** sqlite3_file for win32.
    */
    static sqlite3_io_methods winIoMethod = new sqlite3_io_methods(
    1,                        /* iVersion */
    (dxClose)winClose,
    (dxRead)winRead,
    (dxWrite)winWrite,
    (dxTruncate)winTruncate,
    (dxSync)winSync,
    (dxFileSize)sqlite3_fileSize,
    (dxLock)winLock,
    (dxUnlock)winUnlock,
    (dxCheckReservedLock)winCheckReservedLock,
    (dxFileControl)winFileControl,
    (dxSectorSize)winSectorSize,
    (dxDeviceCharacteristics)winDeviceCharacteristics
    );

    /***************************************************************************
    ** Here ends the I/O methods that form the sqlite3_io_methods object.
    **
    ** The next block of code implements the VFS methods.
    ****************************************************************************/

    /*
    ** Convert a UTF-8 filename into whatever form the underlying
    ** operating system wants filenames in.  Space to hold the result
    ** is obtained from malloc and must be freed by the calling
    ** function.
    */
    static string convertUtf8Filename( string zFilename )
    {
      return zFilename;
      // string zConverted = "";
      //if (isNT())
      //{
      //  zConverted = utf8ToUnicode(zFilename);
      /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
      */
#if !SQLITE_OS_WINCE
      //}
      //else
      //{
      //  zConverted = utf8ToMbcs(zFilename);
#endif
      //}
      /* caller will handle out of memory */
      //return zConverted;
    }

    /*
    ** Create a temporary file name in zBuf.  zBuf must be big enough to
    ** hold at pVfs.mxPathname characters.
    */
    static int getTempname( int nBuf, StringBuilder zBuf )
    {
      const string zChars = "abcdefghijklmnopqrstuvwxyz0123456789";
      //static char zChars[] =
      //  "abcdefghijklmnopqrstuvwxyz"
      //  "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
      //  "0123456789";
      //size_t i, j;
      //char zTempPath[MAX_PATH+1];
      //if( sqlite3_temp_directory ){
      //  sqlite3_snprintf(MAX_PATH-30, zTempPath, "%s", sqlite3_temp_directory);
      //}else if( isNT() ){
      //  char *zMulti;
      //  WCHAR zWidePath[MAX_PATH];
      //  GetTempPathW(MAX_PATH-30, zWidePath);
      //  zMulti = unicodeToUtf8(zWidePath);
      //  if( zMulti ){
      //    sqlite3_snprintf(MAX_PATH-30, zTempPath, "%s", zMulti);
      //    free(zMulti);
      //  }else{
      //    return SQLITE_NOMEM;
      //  }
      /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
      ** Since the ASCII version of these Windows API do not exist for WINCE,
      ** it's important to not reference them for WINCE builds.
      */
#if !SQLITE_OS_WINCE
      //}else{
      //  char *zUtf8;
      //  char zMbcsPath[MAX_PATH];
      //  GetTempPathA(MAX_PATH-30, zMbcsPath);
      //  zUtf8 = sqlite3_win32_mbcs_to_utf8(zMbcsPath);
      //  if( zUtf8 ){
      //    sqlite3_snprintf(MAX_PATH-30, zTempPath, "%s", zUtf8);
      //    free(zUtf8);
      //  }else{
      //    return SQLITE_NOMEM;
      //  }
#endif
      //}

      StringBuilder zRandom = new StringBuilder( 20 );
      i64 iRandom = 0;
      for ( int i = 0 ; i < 20 ; i++ )
      {
        sqlite3_randomness( 1, ref iRandom );
        zRandom.Append( (char)zChars[(int)( iRandom % ( zChars.Length - 1 ) )] );
      }
      //  zBuf[j] = 0;
      zBuf.Append( Path.GetTempPath() + SQLITE_TEMP_FILE_PREFIX + zRandom.ToString() );
      //for(i=sqlite3Strlen30(zTempPath); i>0 && zTempPath[i-1]=='\\'; i--){}
      //zTempPath[i] = 0;
      //sqlite3_snprintf(nBuf-30, zBuf,
      //                 "%s\\"SQLITE_TEMP_FILE_PREFIX, zTempPath);
      //j = sqlite3Strlen30(zBuf);
      //sqlite3_randomness(20, zBuf[j]);
      //for(i=0; i<20; i++, j++){
      //  zBuf[j] = (char)zChars[ ((unsigned char)zBuf[j])%(sizeof(zChars)-1) ];
      //}
      //zBuf[j] = 0;

#if SQLITE_DEBUG
      OSTRACE2( "TEMP FILENAME: %s\n", zBuf.ToString() );
#endif
      return SQLITE_OK;
    }

    /*
    ** The return value of getLastErrorMsg
    ** is zero if the error message fits in the buffer, or non-zero
    ** otherwise (if the message was truncated).
    */
    static int getLastErrorMsg( int nBuf, ref string zBuf )
    {
      //int error = GetLastError ();

#if SQLITE_OS_WINCE
sqlite3_snprintf(nBuf, zBuf, "OsError 0x%x (%u)", error, error);
#else
      /* FormatMessage returns 0 on failure.  Otherwise it
** returns the number of TCHARs written to the output
** buffer, excluding the terminating null char.
*/
      //int iDummy = 0;
      //object oDummy = null;
      //if ( 00 == FormatMessageA( FORMAT_MESSAGE_FROM_SYSTEM,
      //ref oDummy,
      //error,
      //0,
      //zBuf,
      //nBuf - 1,
      //ref iDummy ) )
      //{
      //  sqlite3_snprintf( nBuf, ref zBuf, "OsError 0x%x (%u)", error, error );
      //}
#endif
      zBuf = new Win32Exception( Marshal.GetLastWin32Error() ).Message;

      return 0;
    }

    /*
    ** Open a file.
    */
    static int winOpen(
    sqlite3_vfs pVfs,       /* Not used */
    string zName,           /* Name of the file (UTF-8) */
    sqlite3_file pFile, /* Write the SQLite file handle here */
    int flags,              /* Open mode flags */
    ref int pOutFlags       /* Status return flags */
    )
    {
      //HANDLE h;
      FileStream fs = null;
      FileAccess dwDesiredAccess;
      FileShare dwShareMode;
      FileMode dwCreationDisposition;
      FileOptions dwFlagsAndAttributes;
#if SQLITE_OS_WINCE
int isTemp = 0;
#endif
      //winFile* pFile = (winFile*)id;
      string zConverted;                 /* Filename in OS encoding */
      string zUtf8Name = zName;    /* Filename in UTF-8 encoding */
      StringBuilder zTmpname = new StringBuilder( MAX_PATH + 1 );        /* Buffer used to create temp filename */

      Debug.Assert( pFile != null );
      UNUSED_PARAMETER( pVfs );

      /* If the second argument to this function is NULL, generate a
      ** temporary file name to use
      */
      if ( String.IsNullOrEmpty( zUtf8Name ) )
      {
        int rc = getTempname( MAX_PATH + 1, zTmpname );
        if ( rc != SQLITE_OK )
        {
          return rc;
        }
        zUtf8Name = zTmpname.ToString();
      }

      // /* Convert the filename to the system encoding. */
      zConverted = zUtf8Name;// convertUtf8Filename( zUtf8Name );
      if ( String.IsNullOrEmpty( zConverted ) )
      {
        return SQLITE_NOMEM;
      }

      if ( ( flags & SQLITE_OPEN_READWRITE ) != 0 )
      {
        dwDesiredAccess = FileAccess.Read | FileAccess.Write; // GENERIC_READ | GENERIC_WRITE;
      }
      else
      {
        dwDesiredAccess = FileAccess.Read; // GENERIC_READ;
      }
      /* SQLITE_OPEN_EXCLUSIVE is used to make sure that a new file is
      ** created. SQLite doesn't use it to indicate "exclusive access"
      ** as it is usually understood.
      */
      Debug.Assert( 0 == ( flags & SQLITE_OPEN_EXCLUSIVE ) || ( flags & SQLITE_OPEN_CREATE ) != 0 );
      if ( ( flags & SQLITE_OPEN_EXCLUSIVE ) != 0 )
      {
        /* Creates a new file, only if it does not already exist. */
        /* If the file exists, it fails. */
        dwCreationDisposition = FileMode.CreateNew;// CREATE_NEW;
      }
      else if ( ( flags & SQLITE_OPEN_CREATE ) != 0 )
      {
        /* Open existing file, or create if it doesn't exist */
        dwCreationDisposition = FileMode.OpenOrCreate;// OPEN_ALWAYS;
      }
      else
      {
        /* Opens a file, only if it exists. */
        dwCreationDisposition = FileMode.Open;//OPEN_EXISTING;
      }
      dwShareMode = FileShare.Read | FileShare.Write;// FILE_SHARE_READ | FILE_SHARE_WRITE;
      if ( ( flags & SQLITE_OPEN_DELETEONCLOSE ) != 0 )
      {
#if SQLITE_OS_WINCE
dwFlagsAndAttributes = FILE_ATTRIBUTE_HIDDEN;
isTemp = 1;
#else
        dwFlagsAndAttributes = FileOptions.DeleteOnClose; // FILE_ATTRIBUTE_TEMPORARY
        //| FILE_ATTRIBUTE_HIDDEN
        //| FILE_FLAG_DELETE_ON_CLOSE;
#endif
      }
      else
      {
        dwFlagsAndAttributes = FileOptions.None; // FILE_ATTRIBUTE_NORMAL;
      }
      /* Reports from the internet are that performance is always
      ** better if FILE_FLAG_RANDOM_ACCESS is used.  Ticket #2699. */
#if SQLITE_OS_WINCE
dwFlagsAndAttributes |= FileOptions.RandomAccess; // FILE_FLAG_RANDOM_ACCESS;
#endif
      if ( isNT() )
      {
        //h = CreateFileW((WCHAR*)zConverted,
        //   dwDesiredAccess,
        //   dwShareMode,
        //   NULL,
        //   dwCreationDisposition,
        //   dwFlagsAndAttributes,
        //   NULL
        //);

        //
        // retry opening the file a few times; this is because of a racing condition between a delete and open call to the FS
        //
        int retries = 3;
        while ( ( fs == null ) && ( retries > 0 ) )
          try
          {
            retries--;
            fs = new FileStream( zConverted, dwCreationDisposition, dwDesiredAccess, dwShareMode, 1024, dwFlagsAndAttributes );
#if SQLITE_DEBUG
            OSTRACE3( "OPEN %d (%s)\n", fs.GetHashCode(), fs.Name );
#endif
          }
          catch ( Exception e )
          {
            Thread.Sleep( 100 );
          }

        /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
        ** Since the ASCII version of these Windows API do not exist for WINCE,
        ** it's important to not reference them for WINCE builds.
        */
#if !SQLITE_OS_WINCE
      }
      else
      {
        Debugger.Break(); // Not NT
        //h = CreateFileA((char*)zConverted,
        //   dwDesiredAccess,
        //   dwShareMode,
        //   NULL,
        //   dwCreationDisposition,
        //   dwFlagsAndAttributes,
        //   NULL
        //);
#endif
      }
      if ( fs == null || fs.SafeFileHandle.IsInvalid ) //(h == INVALID_HANDLE_VALUE)
      {
        //        free(zConverted);
        if ( ( flags & SQLITE_OPEN_READWRITE ) != 0 )
        {
          return winOpen( pVfs, zName, pFile,
          ( ( flags | SQLITE_OPEN_READONLY ) & ~SQLITE_OPEN_READWRITE ), ref pOutFlags );
        }
        else
        {
          return SQLITE_CANTOPEN;
        }
      }
      //if ( pOutFlags )
      //{
      if ( ( flags & SQLITE_OPEN_READWRITE ) != 0 )
      {
        pOutFlags = SQLITE_OPEN_READWRITE;
      }
      else
      {
        pOutFlags = SQLITE_OPEN_READONLY;
      }
      //}
      pFile.Clear(); // memset(pFile, 0, sizeof(*pFile));
      pFile.pMethods = winIoMethod;
      pFile.fs = fs;
      pFile.lastErrno = NO_ERROR;
      pFile.sectorSize = (ulong)getSectorSize( pVfs, zUtf8Name );
#if SQLITE_OS_WINCE
if( (flags & (SQLITE_OPEN_READWRITE|SQLITE_OPEN_MAIN_DB)) ==
(SQLITE_OPEN_READWRITE|SQLITE_OPEN_MAIN_DB)
&& !winceCreateLock(zName, pFile)
){
CloseHandle(h);
free(zConverted);
return SQLITE_CANTOPEN;
}
if( isTemp ){
pFile.zDeleteOnClose = zConverted;
}else
#endif
      {
        // free(zConverted);
      }
#if SQLITE_TEST
      OpenCounter( +1 );
#endif
      return SQLITE_OK;
    }

    /*
    ** Delete the named file.
    **
    ** Note that windows does not allow a file to be deleted if some other
    ** process has it open.  Sometimes a virus scanner or indexing program
    ** will open a journal file shortly after it is created in order to do
    ** whatever it does.  While this other process is holding the
    ** file open, we will be unable to delete it.  To work around this
    ** problem, we delay 100 milliseconds and try to delete again.  Up
    ** to MX_DELETION_ATTEMPTs deletion attempts are run before giving
    ** up and returning an error.
    */
    static int MX_DELETION_ATTEMPTS = 5;
    static int winDelete(
    sqlite3_vfs pVfs,         /* Not used on win32 */
    string zFilename,         /* Name of file to delete */
    int syncDir               /* Not used on win32 */
    )
    {
      int cnt = 0;
      int rc;
      int error;
      UNUSED_PARAMETER( pVfs );
      UNUSED_PARAMETER( syncDir );
      string zConverted = convertUtf8Filename( zFilename );
      if ( zConverted == null || zConverted == "" )
      {
        return SQLITE_NOMEM;
      }
#if SQLITE_TEST
      //SimulateIOError(return SQLITE_IOERR_DELETE);  TODO --  How to implement this?
#endif
      if ( isNT() )
      {
        do
        //  DeleteFileW(zConverted);
        //}while(   (   ((rc = GetFileAttributesW(zConverted)) != INVALID_FILE_ATTRIBUTES)
        //           || ((error = GetLastError()) == ERROR_ACCESS_DENIED))
        //       && (++cnt < MX_DELETION_ATTEMPTS)
        //       && (Sleep(100), 1) );
        {
          if ( !File.Exists( zFilename ) )
          {
            rc = SQLITE_IOERR;
            break;
          }
          try
          {
            File.Delete( zConverted );
            rc = SQLITE_OK;
          }
          catch ( IOException e )
          {
            rc = SQLITE_IOERR;
            Thread.Sleep( 100 );
          }
        } while ( rc != SQLITE_OK && ++cnt < MX_DELETION_ATTEMPTS );
        /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
        ** Since the ASCII version of these Windows API do not exist for WINCE,
        ** it's important to not reference them for WINCE builds.
        */
#if !SQLITE_OS_WINCE
      }
      else
      {
        do
        {
          //DeleteFileA( zConverted );
          //}while(   (   ((rc = GetFileAttributesA(zConverted)) != INVALID_FILE_ATTRIBUTES)
          //           || ((error = GetLastError()) == ERROR_ACCESS_DENIED))
          //       && (cnt++ < MX_DELETION_ATTEMPTS)
          //       && (Sleep(100), 1) );
          if ( !File.Exists( zFilename ) )
          {
            rc = SQLITE_IOERR;
            break;
          }
          try
          {
            File.Delete( zConverted );
            rc = SQLITE_OK;
          }
          catch ( IOException e )
          {
            rc = SQLITE_IOERR;
            Thread.Sleep( 100 );
          }
        } while ( rc != SQLITE_OK && cnt++ < MX_DELETION_ATTEMPTS );
#endif
      }
      //free(zConverted);
#if SQLITE_DEBUG
      OSTRACE2( "DELETE \"%s\"\n", zFilename );
#endif
      //return ( ( rc == INVALID_FILE_ATTRIBUTES )
      //&& ( error == ERROR_FILE_NOT_FOUND ) ) ? SQLITE_OK : SQLITE_IOERR_DELETE;
      return rc;
    }

    /*
    ** Check the existence and status of a file.
    */
    static int winAccess(
    sqlite3_vfs pVfs,       /* Not used on win32 */
    string zFilename,       /* Name of file to check */
    int flags,              /* Type of test to make on this file */
    ref int pResOut         /* OUT: Result */
    )
    {
      FileAttributes attr = 0; // DWORD attr;
      int rc = 0;
      //  void *zConverted = convertUtf8Filename(zFilename);
      UNUSED_PARAMETER( pVfs );
      //  if( zConverted==0 ){
      //    return SQLITE_NOMEM;
      //  }
      //if ( isNT() )
      //{
      //
      // Do a quick test to prevent the try/catch block
      if ( flags == SQLITE_ACCESS_EXISTS )
      {
        pResOut = File.Exists( zFilename ) ? 1 : 0;
        return SQLITE_OK;
      }
      //
      try
      {
        attr = File.GetAttributes( zFilename );// GetFileAttributesW( (WCHAR*)zConverted );
        if ( attr == FileAttributes.Directory )
        {
          StringBuilder zTmpname = new StringBuilder( 255 );        /* Buffer used to create temp filename */
          getTempname( 256, zTmpname );

          string zTempFilename;
          zTempFilename = zTmpname.ToString();//( SQLITE_TEMP_FILE_PREFIX.Length + 1 );
          try
          {
            FileStream fs = File.Create( zTempFilename, 1, FileOptions.DeleteOnClose );
            fs.Close();
            attr = FileAttributes.Normal;
          }
          catch ( IOException e ) { attr = FileAttributes.ReadOnly; }
        }
      }
      /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
      ** Since the ASCII version of these Windows API do not exist for WINCE,
      ** it's important to not reference them for WINCE builds.
      */
#if !SQLITE_OS_WINCE
      //}
      //else
      //{
      //  attr = GetFileAttributesA( (char*)zConverted );
#endif
      //}
      catch ( IOException e )
      { }
      //  free(zConverted);
      switch ( flags )
      {
        case SQLITE_ACCESS_READ:
        case SQLITE_ACCESS_EXISTS:
          rc = attr != 0 ? 1 : 0;// != INVALID_FILE_ATTRIBUTES;
          break;
        case SQLITE_ACCESS_READWRITE:
          rc = attr == 0 ? 0 : (int)( attr & FileAttributes.ReadOnly ) != 0 ? 0 : 1; //FILE_ATTRIBUTE_READONLY ) == 0;
          break;
        default:
          Debug.Assert( "" == "Invalid flags argument" );
          rc = 0;
          break;
      }
      pResOut = rc;
      return SQLITE_OK;
    }

    /*
    ** Turn a relative pathname into a full pathname.  Write the full
    ** pathname into zOut[].  zOut[] will be at least pVfs.mxPathname
    ** bytes in size.
    */
    static int winFullPathname(
    sqlite3_vfs pVfs,             /* Pointer to vfs object */
    string zRelative,             /* Possibly relative input path */
    int nFull,                    /* Size of output buffer in bytes */
    StringBuilder zFull           /* Output buffer */
    )
    {

#if __CYGWIN__
UNUSED_PARAMETER(nFull);
cygwin_conv_to_full_win32_path(zRelative, zFull);
return SQLITE_OK;
#endif

#if SQLITE_OS_WINCE
UNUSED_PARAMETER(nFull);
/* WinCE has no concept of a relative pathname, or so I am told. */
sqlite3_snprintf(pVfs.mxPathname, zFull, "%s", zRelative);
return SQLITE_OK;
#endif

#if !SQLITE_OS_WINCE && !__CYGWIN__
      int nByte;
      //string  zConverted;
      string zOut = null;
      UNUSED_PARAMETER( nFull );
      //convertUtf8Filename(zRelative));
      if ( isNT() )
      {
        //string zTemp;
        //nByte = GetFullPathNameW( zConverted, 0, 0, 0) + 3;
        //zTemp = malloc( nByte*sizeof(zTemp[0]) );
        //if( zTemp==0 ){
        //  free(zConverted);
        //  return SQLITE_NOMEM;
        //}
        //zTemp = GetFullPathNameW(zConverted, nByte, zTemp, 0);
        // will happen on exit; was   free(zConverted);
        try
        {
          zOut = Path.GetFullPath( zRelative ); // was unicodeToUtf8(zTemp);
        }
        catch ( IOException e )
        { zOut = zRelative; }
        // will happen on exit; was   free(zTemp);
        /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
        ** Since the ASCII version of these Windows API do not exist for WINCE,
        ** it's important to not reference them for WINCE builds.
        */
#if !SQLITE_OS_WINCE
      }
      else
      {
        Debugger.Break(); // -- Not Running under NT
        //string zTemp;
        //nByte = GetFullPathNameA(zConverted, 0, 0, 0) + 3;
        //zTemp = malloc( nByte*sizeof(zTemp[0]) );
        //if( zTemp==0 ){
        //  free(zConverted);
        //  return SQLITE_NOMEM;
        //}
        //GetFullPathNameA( zConverted, nByte, zTemp, 0);
        // free(zConverted);
        //zOut = sqlite3_win32_mbcs_to_utf8(zTemp);
        // free(zTemp);
#endif
      }
      if ( zOut != null )
      {
        // sqlite3_snprintf(pVfs.mxPathname, zFull, "%s", zOut);
        if ( zFull.Length > pVfs.mxPathname ) zFull.Length = pVfs.mxPathname;
        zFull.Append( zOut );

        // will happen on exit; was   free(zOut);
        return SQLITE_OK;
      }
      else
      {
        return SQLITE_NOMEM;
      }
#endif
    }


    /*
    ** Get the sector size of the device used to store
    ** file.
    */
    static int getSectorSize(
    sqlite3_vfs pVfs,
    string zRelative     /* UTF-8 file name */
    )
    {
      int bytesPerSector = SQLITE_DEFAULT_SECTOR_SIZE;
      StringBuilder zFullpath = new StringBuilder( MAX_PATH + 1 );
      int rc;
//      bool dwRet = false;
//      int dwDummy = 0;

      /*
      ** We need to get the full path name of the file
      ** to get the drive letter to look up the sector
      ** size.
      */
      rc = winFullPathname( pVfs, zRelative, MAX_PATH, zFullpath );
      if ( rc == SQLITE_OK )
      {
        StringBuilder zConverted = new StringBuilder( convertUtf8Filename( zFullpath.ToString() ) );
        if ( zConverted.Length != 0 )
        {
          if ( isNT() )
          {
            /* trim path to just drive reference */
            //for ( ; *p ; p++ )
            //{
            //  if ( *p == '\\' )
            //  {
            //    *p = '\0';
            //    break;
            //  }
            //}
            int i;
            for ( i = 0 ; i < zConverted.Length && i < MAX_PATH ; i++ )
            {
              if ( zConverted[i] == '\\' )
              {
                i++;
                break;
              }
            }
            zConverted.Length = i;
            //dwRet = GetDiskFreeSpace( zConverted,
            //     ref dwDummy,
            //     ref bytesPerSector,
            //     ref dwDummy,
            //     ref dwDummy );
            //#if !SQLITE_OS_WINCE
            //}else{
            //  /* trim path to just drive reference */
            //  CHAR* p = (CHAR*)zConverted;
            //  for ( ; *p ; p++ )
            //  {
            //    if ( *p == '\\' )
            //    {
            //      *p = '\0';
            //      break;
            //    }
            //  }
            //        dwRet = GetDiskFreeSpaceA((CHAR*)zConverted,
            //                                  dwDummy,
            //                                  ref bytesPerSector,
            //                                  dwDummy,
            //                                  dwDummy );
            //#endif
          }
          //free(zConverted);
        }
        //  if ( !dwRet )
        //  {
        //    bytesPerSector = SQLITE_DEFAULT_SECTOR_SIZE;
        //  }
        //}
        bytesPerSector = GetbytesPerSector( zConverted );
      }
      return bytesPerSector == 0 ? SQLITE_DEFAULT_SECTOR_SIZE : bytesPerSector;
    }

#if !SQLITE_OMIT_LOAD_EXTENSION
    /*
** Interfaces for opening a shared library, finding entry points
** within the shared library, and closing the shared library.
*/
    /*
    ** Interfaces for opening a shared library, finding entry points
    ** within the shared library, and closing the shared library.
    */
    //static void *winDlOpen(sqlite3_vfs pVfs, string zFilename){
    //  HANDLE h;
    //  void *zConverted = convertUtf8Filename(zFilename);
    //  UNUSED_PARAMETER(pVfs);
    //  if( zConverted==0 ){
    //    return 0;
    //  }
    //  if( isNT() ){
    //    h = LoadLibraryW((WCHAR*)zConverted);
    /* isNT() is 1 if SQLITE_OS_WINCE==1, so this else is never executed.
    ** Since the ASCII version of these Windows API do not exist for WINCE,
    ** it's important to not reference them for WINCE builds.
    */
#if !SQLITE_OS_WINCE
    //  }else{
    //    h = LoadLibraryA((char*)zConverted);
#endif
    //  }
    //  free(zConverted);
    //  return (void*)h;
    //}
    //static void winDlError(sqlite3_vfs pVfs, int nBuf, char *zBufOut){
    //  UNUSED_PARAMETER(pVfs);
    //  getLastErrorMsg(nBuf, zBufOut);
    //}
    //    static object winDlSym(sqlite3_vfs pVfs, HANDLE pHandle, String zSymbol){
    //  UNUSED_PARAMETER(pVfs);
    //#if SQLITE_OS_WINCE
    //      /* The GetProcAddressA() routine is only available on wince. */
    //      return GetProcAddressA((HANDLE)pHandle, zSymbol);
    //#else
    //     /* All other windows platforms expect GetProcAddress() to take
    //      ** an Ansi string regardless of the _UNICODE setting */
    //      return GetProcAddress((HANDLE)pHandle, zSymbol);
    //#endif
    //   }
    //    static void winDlClose( sqlite3_vfs pVfs, HANDLE pHandle )
    //   {
    //  UNUSED_PARAMETER(pVfs);
    //     FreeLibrary((HANDLE)pHandle);
    //   }
    //TODO -- Fix This
    static HANDLE winDlOpen( sqlite3_vfs vfs, string zFilename ) { return new HANDLE(); }
    static int winDlError( sqlite3_vfs vfs, int nByte, ref string zErrMsg ) { return 0; }
    static HANDLE winDlSym( sqlite3_vfs vfs, HANDLE data, string zSymbol ) { return new HANDLE(); }
    static int winDlClose( sqlite3_vfs vfs, HANDLE data ) { return 0; }
#else // * if SQLITE_OMIT_LOAD_EXTENSION is defined: */
static object winDlOpen(ref sqlite3_vfs vfs, string zFilename) { return null; }
static int winDlError(ref sqlite3_vfs vfs, int nByte, ref string zErrMsg) { return 0; }
static object winDlSym(ref sqlite3_vfs vfs, object data, string zSymbol) { return null; }
static int winDlClose(ref sqlite3_vfs vfs, object data) { return 0; }
#endif


    /*
** Write up to nBuf bytes of randomness into zBuf.
*/

    //[StructLayout( LayoutKind.Explicit, Size = 16, CharSet = CharSet.Ansi )]
    //public class _SYSTEMTIME
    //{
    //  [FieldOffset( 0 )]
    //  public u32 byte_0_3;
    //  [FieldOffset( 4 )]
    //  public u32 byte_4_7;
    //  [FieldOffset( 8 )]
    //  public u32 byte_8_11;
    //  [FieldOffset( 12 )]
    //  public u32 byte_12_15;
    //}
    //[DllImport( "Kernel32.dll" )]
    //private static extern bool QueryPerformanceCounter( out long lpPerformanceCount );

    static int winRandomness( sqlite3_vfs pVfs, int nBuf, ref byte[] zBuf )
    {
      int n = 0;
      UNUSED_PARAMETER( pVfs );
#if (SQLITE_TEST)
      n = nBuf;
      Array.Clear( zBuf, 0, n );// memset( zBuf, 0, nBuf );
#else
byte[] sBuf = BitConverter.GetBytes(System.DateTime.Now.Ticks);
zBuf[0] = sBuf[0];
zBuf[1] = sBuf[1];
zBuf[2] = sBuf[2];
zBuf[3] = sBuf[3];
;// memcpy(&zBuf[n], x, sizeof(x))
n += 16;// sizeof(x);
if ( sizeof( DWORD ) <= nBuf - n )
{
//DWORD pid = GetCurrentProcessId();
put32bits( zBuf, n, (u32)Process.GetCurrentProcess().Id );//(memcpy(&zBuf[n], pid, sizeof(pid));
n += 4;// sizeof(pid);
}
if ( sizeof( DWORD ) <= nBuf - n )
{
//DWORD cnt = GetTickCount();
System.DateTime dt = new System.DateTime();
put32bits( zBuf, n, (u32)dt.Ticks );// memcpy(&zBuf[n], cnt, sizeof(cnt));
n += 4;// cnt.Length;
}
if ( sizeof( long ) <= nBuf - n )
{
long i;
i = System.DateTime.UtcNow.Millisecond;// QueryPerformanceCounter(out i);
put32bits( zBuf, n, (u32)( i & 0xFFFFFFFF ) );//memcpy(&zBuf[n], i, sizeof(i));
put32bits( zBuf, n, (u32)( i >> 32 ) );
n += sizeof( long );
}
#endif
      return n;
    }


    /*
    ** Sleep for a little while.  Return the amount of time slept.
    */
    static int winSleep( sqlite3_vfs pVfs, int microsec )
    {
      Thread.Sleep( ( microsec + 999 ) / 1000 );
      UNUSED_PARAMETER( pVfs );
      return ( ( microsec + 999 ) / 1000 ) * 1000;
    }

    /*
    ** The following variable, if set to a non-zero value, becomes the result
    ** returned from sqlite3OsCurrentTime().  This is used for testing.
    */
#if SQLITE_TEST
    //    static int sqlite3_current_time = 0;
#endif

    /*
** Find the current time (in Universal Coordinated Time).  Write the
** current time and date as a Julian Day number into prNow and
** return 0.  Return 1 if the time and date cannot be found.
*/
    static int winCurrentTime( sqlite3_vfs pVfs, ref double prNow )
    {
      //FILETIME ft = new FILETIME();
      /* FILETIME structure is a 64-bit value representing the number of
      100-nanosecond intervals since January 1, 1601 (= JD 2305813.5).
      */
      sqlite3_int64 timeW;   /* Whole days */
      sqlite3_int64 timeF;   /* Fractional Days */

      /* Number of 100-nanosecond intervals in a single day */
      const sqlite3_int64 ntuPerDay =
      10000000 * (sqlite3_int64)86400;

      /* Number of 100-nanosecond intervals in half of a day */
      const sqlite3_int64 ntuPerHalfDay =
      10000000 * (sqlite3_int64)43200;

      ///* 2^32 - to avoid use of LL and warnings in gcc */
      //const sqlite3_int64 max32BitValue =
      //(sqlite3_int64)2000000000 + (sqlite3_int64)2000000000 + (sqlite3_int64)294967296;

      //#if SQLITE_OS_WINCE
      //SYSTEMTIME time;
      //GetSystemTime(&time);
      ///* if SystemTimeToFileTime() fails, it returns zero. */
      //if (!SystemTimeToFileTime(&time,&ft)){
      //return 1;
      //}
      //#else
      //      GetSystemTimeAsFileTime( ref ft );
      //      ft = System.DateTime.UtcNow.ToFileTime();
      //#endif
      //      UNUSED_PARAMETER( pVfs );
      //      timeW = ( ( (sqlite3_int64)ft.dwHighDateTime ) * max32BitValue ) + (sqlite3_int64)ft.dwLowDateTime;
      timeW = System.DateTime.UtcNow.ToFileTime();
      timeF = timeW % ntuPerDay;          /* fractional days (100-nanoseconds) */
      timeW = timeW / ntuPerDay;          /* whole days */
      timeW = timeW + 2305813;            /* add whole days (from 2305813.5) */
      timeF = timeF + ntuPerHalfDay;      /* add half a day (from 2305813.5) */
      timeW = timeW + ( timeF / ntuPerDay );  /* add whole day if half day made one */
      timeF = timeF % ntuPerDay;          /* compute new fractional days */
      prNow = (double)timeW + ( (double)timeF / (double)ntuPerDay );
#if SQLITE_TEST
      if ( ( sqlite3_current_time.iValue ) != 0 )
      {
        prNow = ( (double)sqlite3_current_time.iValue + (double)43200 ) / (double)86400 + (double)2440587;
      }
#endif
      return 0;
    }


    /*
    ** The idea is that this function works like a combination of
    ** GetLastError() and FormatMessage() on windows (or errno and
    ** strerror_r() on unix). After an error is returned by an OS
    ** function, SQLite calls this function with zBuf pointing to
    ** a buffer of nBuf bytes. The OS layer should populate the
    ** buffer with a nul-terminated UTF-8 encoded error message
    ** describing the last IO error to have occurred within the calling
    ** thread.
    **
    ** If the error message is too large for the supplied buffer,
    ** it should be truncated. The return value of xGetLastError
    ** is zero if the error message fits in the buffer, or non-zero
    ** otherwise (if the message was truncated). If non-zero is returned,
    ** then it is not necessary to include the nul-terminator character
    ** in the output buffer.
    **
    ** Not supplying an error message will have no adverse effect
    ** on SQLite. It is fine to have an implementation that never
    ** returns an error message:
    **
    **   int xGetLastError(sqlite3_vfs pVfs, int nBuf, char *zBuf){
    **     Debug.Assert(zBuf[0]=='\0');
    **     return 0;
    **   }
    **
    ** However if an error message is supplied, it will be incorporated
    ** by sqlite into the error message available to the user using
    ** sqlite3_errmsg(), possibly making IO errors easier to debug.
    */
    static int winGetLastError( sqlite3_vfs pVfs, int nBuf, ref string zBuf )
    {
      UNUSED_PARAMETER( pVfs );
      return getLastErrorMsg( nBuf, ref zBuf );
    }

    static sqlite3_vfs winVfs = new sqlite3_vfs(
    1,                              /* iVersion */
    -1, //sqlite3_file.Length,      /* szOsFile */
    MAX_PATH,                       /* mxPathname */
    null,                           /* pNext */
    "win32",                        /* zName */
    0,                              /* pAppData */

    (dxOpen)winOpen,                /* xOpen */
    (dxDelete)winDelete,            /* xDelete */
    (dxAccess)winAccess,            /* xAccess */
    (dxFullPathname)winFullPathname,/* xFullPathname */
    (dxDlOpen)winDlOpen,            /* xDlOpen */
    (dxDlError)winDlError,          /* xDlError */
    (dxDlSym)winDlSym,              /* xDlSym */
    (dxDlClose)winDlClose,          /* xDlClose */
    (dxRandomness)winRandomness,    /* xRandomness */
    (dxSleep)winSleep,              /* xSleep */
    (dxCurrentTime)winCurrentTime,  /* xCurrentTime */
    (dxGetLastError)winGetLastError /* xGetLastError */
    );

    /*
    ** Initialize and deinitialize the operating system interface.
    */
    static int sqlite3_os_init()
    {
      sqlite3_vfs_register( winVfs, 1 );
      return SQLITE_OK;
    }
    static int sqlite3_os_end()
    {
      return SQLITE_OK;
    }

#endif // * SQLITE_OS_WIN */
    //
    //          Windows DLL definitions
    //

    const int NO_ERROR = 0;
  }
}
