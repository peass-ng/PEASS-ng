#define SQLITE_OS_WIN

namespace CS_SQLite3
{
  public partial class CSSQLite
  {
    /*
    ** 2007 August 28
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    **
    ** This file contains the common header for all mutex implementations.
    ** The sqliteInt.h header #includes this file so that it is available
    ** to all source files.  We break it out in an effort to keep the code
    ** better organized.
    **
    ** NOTE:  source files should *not* #include this header file directly.
    ** Source files should #include the sqliteInt.h file and let that file
    ** include this one indirectly.
    **
    ** $Id: mutex.h,v 1.9 2008/10/07 15:25:48 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */


    /*
    ** Figure out what version of the code to use.  The choices are
    **
    **   SQLITE_MUTEX_OMIT         No mutex logic.  Not even stubs.  The
    **                             mutexes implemention cannot be overridden
    **                             at start-time.
    **
    **   SQLITE_MUTEX_NOOP         For single-threaded applications.  No
    **                             mutual exclusion is provided.  But this
    **                             implementation can be overridden at
    **                             start-time.
    **
    **   SQLITE_MUTEX_PTHREADS     For multi-threaded applications on Unix.
    **
    **   SQLITE_MUTEX_W32          For multi-threaded applications on Win32.
    **
    **   SQLITE_MUTEX_OS2          For multi-threaded applications on OS/2.
    */

    //#if !SQLITE_THREADSAFE
    //# define SQLITE_MUTEX_OMIT
    //#endif
    //#if SQLITE_THREADSAFE && !defined(SQLITE_MUTEX_NOOP)
    //#  if SQLITE_OS_UNIX
    //#    define SQLITE_MUTEX_PTHREADS
    //#  elif SQLITE_OS_WIN
    //#    define SQLITE_MUTEX_W32
    //#  elif SQLITE_OS_OS2
    //#    define SQLITE_MUTEX_OS2
    //#  else
    //#    define SQLITE_MUTEX_NOOP
    //#  endif
    //#endif


#if SQLITE_MUTEX_OMIT
    /*
** If this is a no-op implementation, implement everything as macros.
*/
    public class sqlite3_mutex { }
    static sqlite3_mutex mutex = null;  //sqlite3_mutex sqlite3_mutex;
    static sqlite3_mutex sqlite3MutexAlloc( int iType ) { return new sqlite3_mutex(); }//#define sqlite3MutexAlloc(X)      ((sqlite3_mutex*)8)
    static sqlite3_mutex sqlite3_mutex_alloc( int iType ) { return new sqlite3_mutex(); }//#define sqlite3_mutex_alloc(X)    ((sqlite3_mutex*)8)
    static void sqlite3_mutex_free( ref sqlite3_mutex m ) { }          //#define sqlite3_mutex_free(X)
    static void sqlite3_mutex_enter( sqlite3_mutex m ) { }            //#define sqlite3_mutex_enter(X)
    static int sqlite3_mutex_try( int iType ) { return SQLITE_OK; }   //#define sqlite3_mutex_try(X)      SQLITE_OK
    static void sqlite3_mutex_leave( sqlite3_mutex m ) { }            //#define sqlite3_mutex_leave(X)
    static bool sqlite3_mutex_held( sqlite3_mutex m ) { return true; }//#define sqlite3_mutex_held(X)     1
    static bool sqlite3_mutex_notheld( sqlite3_mutex m ) { return true; }   //#define sqlite3_mutex_notheld(X)  1
    static int sqlite3MutexInit() { return SQLITE_OK; }              //#define sqlite3MutexInit()        SQLITE_OK
    static void sqlite3MutexEnd() { }                                //#define sqlite3MutexEnd()
#endif //* defined(SQLITE_OMIT_MUTEX) */
  }
}
