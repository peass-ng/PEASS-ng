using System;
using System.Diagnostics;
using System.Text;

using sqlite_int64 = System.Int64;
using unsigned = System.Int32;

using i16 = System.Int16;
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;

using Pgno = System.UInt32;

namespace CS_SQLite3
{
  using sqlite3_value = CSSQLite.Mem;

  public partial class CSSQLite
  {
    /*
    ** 2001 September 15
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** Main file for the SQLite library.  The routines in this file
    ** implement the programmer interface to the library.  Routines in
    ** other files are for internal use by SQLite and should not be
    ** accessed by users of the library.
    **
    ** $Id: main.c,v 1.562 2009/07/20 11:32:03 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
#if  SQLITE_ENABLE_FTS3
//# include "fts3.h"
#endif
#if SQLITE_ENABLE_RTREE
//# include "rtree.h"
#endif
#if SQLITE_ENABLE_ICU
//# include "sqliteicu.h"
#endif

    /*
** The version of the library
*/
#if !SQLITE_AMALGAMATION
    public static string sqlite3_version = SQLITE_VERSION;
#endif
    public static string sqlite3_libversion() { return sqlite3_version; }
    public static int sqlite3_libversion_number() { return SQLITE_VERSION_NUMBER; }
    public static int sqlite3_threadsafe() { return SQLITE_THREADSAFE; }

#if !SQLITE_OMIT_TRACE && SQLITE_ENABLE_IOTRACE
/*
** If the following function pointer is not NULL and if
** SQLITE_ENABLE_IOTRACE is enabled, then messages describing
** I/O active are written using this function.  These messages
** are intended for debugging activity only.
*/
//void (*sqlite3IoTrace)(const char*, ...) = 0;
static void sqlite3IoTrace( string X, params object[] ap ) {  }
#endif

    /*
** If the following global variable points to a string which is the
** name of a directory, then that directory will be used to store
** temporary files.
**
** See also the "PRAGMA temp_store_directory" SQL command.
*/
    static string sqlite3_temp_directory = "";//char *sqlite3_temp_directory = 0;

    /*
    ** Initialize SQLite.
    **
    ** This routine must be called to initialize the memory allocation,
    ** VFS, and mutex subsystems prior to doing any serious work with
    ** SQLite.  But as long as you do not compile with SQLITE_OMIT_AUTOINIT
    ** this routine will be called automatically by key routines such as
    ** sqlite3_open().
    **
    ** This routine is a no-op except on its very first call for the process,
    ** or for the first call after a call to sqlite3_shutdown.
    **
    ** The first thread to call this routine runs the initialization to
    ** completion.  If subsequent threads call this routine before the first
    ** thread has finished the initialization process, then the subsequent
    ** threads must block until the first thread finishes with the initialization.
    **
    ** The first thread might call this routine recursively.  Recursive
    ** calls to this routine should not block, of course.  Otherwise the
    ** initialization process would never complete.
    **
    ** Let X be the first thread to enter this routine.  Let Y be some other
    ** thread.  Then while the initial invocation of this routine by X is
    ** incomplete, it is required that:
    **
    **    *  Calls to this routine from Y must block until the outer-most
    **       call by X completes.
    **
    **    *  Recursive calls to this routine from thread X return immediately
    **       without blocking.
    */
    static int sqlite3_initialize()
    {
      //--------------------------------------------------------------------
      // Under C#, Need to initialize some global structures
      //
      if ( opcodeProperty == null ) opcodeProperty = OPFLG_INITIALIZER;
      if ( sqlite3GlobalConfig == null ) sqlite3GlobalConfig = sqlite3Config;
      if ( UpperToLower == null ) UpperToLower = sqlite3UpperToLower;
      //--------------------------------------------------------------------


      sqlite3_mutex pMaster;            /* The main static mutex */
      int rc;                           /* Result code */

#if SQLITE_OMIT_WSD
rc = sqlite3_wsd_init(4096, 24);
if( rc!=SQLITE_OK ){
return rc;
}
#endif
      /* If SQLite is already completely initialized, then this call
** to sqlite3_initialize() should be a no-op.  But the initialization
** must be complete.  So isInit must not be set until the very end
** of this routine.
*/
      if ( sqlite3GlobalConfig.isInit != 0 ) return SQLITE_OK;

      /* Make sure the mutex subsystem is initialized.  If unable to
      ** initialize the mutex subsystem, return early with the error.
      ** If the system is so sick that we are unable to allocate a mutex,
      ** there is not much SQLite is going to be able to do.
      **
      ** The mutex subsystem must take care of serializing its own
      ** initialization.
      */
      rc = sqlite3MutexInit();
      if ( rc != 0 ) return rc;

      /* Initialize the malloc() system and the recursive pInitMutex mutex.
      ** This operation is protected by the STATIC_MASTER mutex.  Note that
      ** MutexAlloc() is called for a static mutex prior to initializing the
      ** malloc subsystem - this implies that the allocation of a static
      ** mutex must not require support from the malloc subsystem.
      */
      pMaster = sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER );
      sqlite3_mutex_enter( pMaster );
      if ( sqlite3GlobalConfig.isMallocInit == 0 )
      {
        //rc = sqlite3MallocInit();
      }
      if ( rc == SQLITE_OK )
      {
        sqlite3GlobalConfig.isMallocInit = 1;
        if ( sqlite3GlobalConfig.pInitMutex == null )
        {
          sqlite3GlobalConfig.pInitMutex = sqlite3MutexAlloc( SQLITE_MUTEX_RECURSIVE );
          if ( sqlite3GlobalConfig.bCoreMutex && sqlite3GlobalConfig.pInitMutex == null )
          {
            rc = SQLITE_NOMEM;
          }
        }
      }
      if ( rc == SQLITE_OK )
      {
        sqlite3GlobalConfig.nRefInitMutex++;
      }
      sqlite3_mutex_leave( pMaster );
      /* If unable to initialize the malloc subsystem, then return early.
      ** There is little hope of getting SQLite to run if the malloc
      ** subsystem cannot be initialized.
      */
      if ( rc != SQLITE_OK )
      {
        return rc;
      }

      /* Do the rest of the initialization under the recursive mutex so
      ** that we will be able to handle recursive calls into
      ** sqlite3_initialize().  The recursive calls normally come through
      ** sqlite3_os_init() when it invokes sqlite3_vfs_register(), but other
      ** recursive calls might also be possible.
      */
      sqlite3_mutex_enter( sqlite3GlobalConfig.pInitMutex );
      if ( sqlite3GlobalConfig.isInit == 0 && sqlite3GlobalConfig.inProgress == 0 )
      {
        sqlite3GlobalConfig.inProgress = 1;
#if SQLITE_OMIT_WSD
FuncDefHash *pHash = &GLOBAL(FuncDefHash, sqlite3GlobalFunctions);
memset( pHash, 0, sizeof( sqlite3GlobalFunctions ) );
#else
        sqlite3GlobalFunctions = new FuncDefHash();
        FuncDefHash pHash = sqlite3GlobalFunctions;
#endif
        sqlite3RegisterGlobalFunctions();
        rc = sqlite3PcacheInitialize();
        if ( rc == SQLITE_OK )
        {
          rc = sqlite3_os_init();
        }
        if ( rc == SQLITE_OK )
        {
          sqlite3PCacheBufferSetup( sqlite3GlobalConfig.pPage,
          sqlite3GlobalConfig.szPage, sqlite3GlobalConfig.nPage );
          sqlite3GlobalConfig.isInit = 1;
        }
        sqlite3GlobalConfig.inProgress = 0;
      }
      sqlite3_mutex_leave( sqlite3GlobalConfig.pInitMutex );
      /* Go back under the static mutex and clean up the recursive
      ** mutex to prevent a resource leak.
      */
      sqlite3_mutex_enter( pMaster );
      sqlite3GlobalConfig.nRefInitMutex--;
      if ( sqlite3GlobalConfig.nRefInitMutex <= 0 )
      {
        Debug.Assert( sqlite3GlobalConfig.nRefInitMutex == 0 );
        sqlite3_mutex_free( ref  sqlite3GlobalConfig.pInitMutex );
        sqlite3GlobalConfig.pInitMutex = null;
      }
      sqlite3_mutex_leave( pMaster );

      /* The following is just a sanity check to make sure SQLite has
      ** been compiled correctly.  It is important to run this code, but
      ** we don't want to run it too often and soak up CPU cycles for no
      ** reason.  So we run it once during initialization.
      */
#if !NDEBUG
#if !SQLITE_OMIT_FLOATING_POINT
      /* This section of code's only "output" is via Debug.Assert() statements. */
      if ( rc == SQLITE_OK )
      {
        //u64 x = ( ( (u64)1 ) << 63 ) - 1;
        //double y;
        //Debug.Assert( sizeof( u64 ) == 8 );
        //Debug.Assert( sizeof( u64 ) == sizeof( double ) );
        //memcpy( &y, x, 8 );
        //Debug.Assert( sqlite3IsNaN( y ) );
      }
#endif
#endif

      return rc;
    }

    /*
    ** Undo the effects of sqlite3_initialize().  Must not be called while
    ** there are outstanding database connections or memory allocations or
    ** while any part of SQLite is otherwise in use in any thread.  This
    ** routine is not threadsafe.  But it is safe to invoke this routine
    ** on when SQLite is already shut down.  If SQLite is already shut down
    ** when this routine is invoked, then this routine is a harmless no-op.
    */
    static int sqlite3_shutdown()
    {
      if ( sqlite3GlobalConfig.isInit != 0 )
      {
        sqlite3GlobalConfig.isMallocInit = 0;
        sqlite3PcacheShutdown();
        sqlite3_os_end();
        sqlite3_reset_auto_extension();
        //sqlite3MallocEnd();
        sqlite3MutexEnd();
        sqlite3GlobalConfig.isInit = 0;
      }
      return SQLITE_OK;
    }

    /*
    ** This API allows applications to modify the global configuration of
    ** the SQLite library at run-time.
    **
    ** This routine should only be called when there are no outstanding
    ** database connections or memory allocations.  This routine is not
    ** threadsafe.  Failure to heed these warnings can lead to unpredictable
    ** behavior.
    */
    // Overloads for ap assignments
    static int sqlite3_config( int op, sqlite3_pcache_methods ap )
    {      //  va_list ap;
      int rc = SQLITE_OK;
      switch ( op )
      {
        case SQLITE_CONFIG_PCACHE:
          {
            /* Specify an alternative malloc implementation */
            sqlite3GlobalConfig.pcache = ap; //sqlite3GlobalConfig.pcache = (sqlite3_pcache_methods)va_arg(ap, "sqlite3_pcache_methods");
            break;
          }
      }
      return rc;
    }

    static int sqlite3_config( int op, ref sqlite3_pcache_methods ap )
    {      //  va_list ap;
      int rc = SQLITE_OK;
      switch ( op )
      {
        case SQLITE_CONFIG_GETPCACHE:
          {
            if ( sqlite3GlobalConfig.pcache.xInit == null )
            {
              sqlite3PCacheSetDefault();
            }
            ap = sqlite3GlobalConfig.pcache;//va_arg(ap, sqlite3_pcache_methods*) = sqlite3GlobalConfig.pcache;
            break;
          }
      }
      return rc;
    }

    static int sqlite3_config( int op, sqlite3_mem_methods ap )
    {      //  va_list ap;
      int rc = SQLITE_OK;
      switch ( op )
      {
        case SQLITE_CONFIG_MALLOC:
          {
            /* Specify an alternative malloc implementation */
            sqlite3GlobalConfig.m = ap;// (sqlite3_mem_methods)va_arg( ap, "sqlite3_mem_methods" );
            break;
          }
      }
      return rc;
    }

    static int sqlite3_config( int op, ref sqlite3_mem_methods ap )
    {      //  va_list ap;
      int rc = SQLITE_OK;
      switch ( op )
      {
        case SQLITE_CONFIG_GETMALLOC:
          {
            /* Retrieve the current malloc() implementation */
            //if ( sqlite3GlobalConfig.m.xMalloc == null ) sqlite3MemSetDefault();
            ap = sqlite3GlobalConfig.m;//va_arg(ap, sqlite3_mem_methods*) =  sqlite3GlobalConfig.m;
            break;
          }
      }
      return rc;
    }

#if SQLITE_THREADSAFE
static int sqlite3_config( int op,  sqlite3_mutex_methods ap )
{
//  va_list ap;
int rc = SQLITE_OK;
switch ( op )
{
case SQLITE_CONFIG_MUTEX:
{
/* Specify an alternative mutex implementation */
sqlite3GlobalConfig.mutex = ap;// (sqlite3_mutex_methods)va_arg( ap, "sqlite3_mutex_methods" );
break;
}
}
return rc;
}

static int sqlite3_config( int op, ref sqlite3_mutex_methods ap )
{
//  va_list ap;
int rc = SQLITE_OK;
switch ( op )
{
case SQLITE_CONFIG_GETMUTEX:
{
/* Retrieve the current mutex implementation */
ap =  sqlite3GlobalConfig.mutex;// *va_arg(ap, sqlite3_mutex_methods*) =  sqlite3GlobalConfig.mutex;
break;
}
}
return rc;
}
#endif

    static int sqlite3_config( int op, params object[] ap )
    {
      //  va_list ap;
      int rc = SQLITE_OK;

      /* sqlite3_config() shall return SQLITE_MISUSE if it is invoked while
      ** the SQLite library is in use. */
      if ( sqlite3GlobalConfig.isInit != 0 ) return SQLITE_MISUSE;

      va_start( ap, null );
      switch ( op )
      {

        /* Mutex configuration options are only available in a threadsafe
        ** compile.
        */
#if SQLITE_THREADSAFE
case SQLITE_CONFIG_SINGLETHREAD:
{
/* Disable all mutexing */
sqlite3GlobalConfig.bCoreMutex = false;
sqlite3GlobalConfig.bFullMutex = false;
break;
}
case SQLITE_CONFIG_MULTITHREAD:
{
/* Disable mutexing of database connections */
/* Enable mutexing of core data structures */
sqlite3GlobalConfig.bCoreMutex = true;
sqlite3GlobalConfig.bFullMutex = false;
break;
}
case SQLITE_CONFIG_SERIALIZED:
{
/* Enable all mutexing */
sqlite3GlobalConfig.bCoreMutex = true;
sqlite3GlobalConfig.bFullMutex = true;
break;
}
case SQLITE_CONFIG_MUTEX: {
/* Specify an alternative mutex implementation */
sqlite3GlobalConfig.mutex = *va_arg(ap, sqlite3_mutex_methods*);
break;
}
case SQLITE_CONFIG_GETMUTEX: {
/* Retrieve the current mutex implementation */
*va_arg(ap, sqlite3_mutex_methods*) = sqlite3GlobalConfig.mutex;
break;
}
#endif
        case SQLITE_CONFIG_MALLOC:
          {
            Debugger.Break(); // TODO --
            /* Specify an alternative malloc implementation */
            sqlite3GlobalConfig.m = (sqlite3_mem_methods)va_arg( ap, "sqlite3_mem_methods" );
            break;
          }
        case SQLITE_CONFIG_GETMALLOC:
          {
            /* Retrieve the current malloc() implementation */
            //if ( sqlite3GlobalConfig.m.xMalloc == null ) sqlite3MemSetDefault();
            //Debugger.Break(); // TODO --//va_arg(ap, sqlite3_mem_methods*) =  sqlite3GlobalConfig.m;
            break;
          }
        case SQLITE_CONFIG_MEMSTATUS:
          {
            /* Enable or disable the malloc status collection */
            sqlite3GlobalConfig.bMemstat = (int)va_arg( ap, "int" ) != 0;
            break;
          }
        case SQLITE_CONFIG_SCRATCH:
          {
            /* Designate a buffer for scratch memory space */
            sqlite3GlobalConfig.pScratch = (byte[])va_arg( ap, "byte[]" );
            sqlite3GlobalConfig.szScratch = (int)va_arg( ap, "int" );
            sqlite3GlobalConfig.nScratch = (int)va_arg( ap, "int" );
            break;
          }

        case SQLITE_CONFIG_PAGECACHE:
          {
            /* Designate a buffer for page cache memory space */
            sqlite3GlobalConfig.pPage = (MemPage)va_arg( ap, "MemPage" );
            sqlite3GlobalConfig.szPage = (int)va_arg( ap, "int" );
            sqlite3GlobalConfig.nPage = (int)va_arg( ap, "int" );
            break;
          }

        case SQLITE_CONFIG_PCACHE:
          {
            /* Specify an alternative page cache implementation */
            Debugger.Break(); // TODO --sqlite3GlobalConfig.pcache = (sqlite3_pcache_methods)va_arg(ap, "sqlite3_pcache_methods");
            break;
          }

        case SQLITE_CONFIG_GETPCACHE:
          {
            if ( sqlite3GlobalConfig.pcache.xInit == null )
            {
              sqlite3PCacheSetDefault();
            }
            Debugger.Break(); // TODO -- *va_arg(ap, sqlite3_pcache_methods*) = sqlite3GlobalConfig.pcache;
            break;
          }

#if SQLITE_ENABLE_MEMSYS3 || SQLITE_ENABLE_MEMSYS5
case SQLITE_CONFIG_HEAP: {
/* Designate a buffer for heap memory space */
sqlite3GlobalConfig.pHeap = va_arg(ap, void*);
sqlite3GlobalConfig.nHeap = va_arg(ap, int);
sqlite3GlobalConfig.mnReq = va_arg(ap, int);

if(  sqlite3GlobalConfig.pHeap==0 ){
/* If the heap pointer is NULL, then restore the malloc implementation
** back to NULL pointers too.  This will cause the malloc to go
** back to its default implementation when sqlite3_initialize() is
** run.
*/
memset(& sqlite3GlobalConfig.m, 0, sizeof( sqlite3GlobalConfig.m));
}else{
/* The heap pointer is not NULL, then install one of the
** mem5.c/mem3.c methods. If neither ENABLE_MEMSYS3 nor
** ENABLE_MEMSYS5 is defined, return an error.
*/
#if SQLITE_ENABLE_MEMSYS3
sqlite3GlobalConfig.m = *sqlite3MemGetMemsys3();
#endif
#if SQLITE_ENABLE_MEMSYS5
sqlite3GlobalConfig.m = *sqlite3MemGetMemsys5();
#endif
}
break;
}
#endif

        case SQLITE_CONFIG_LOOKASIDE:
          {
            sqlite3GlobalConfig.szLookaside = (int)va_arg( ap, "int" );
            sqlite3GlobalConfig.nLookaside = (int)va_arg( ap, "int" );
            break;
          }

        default:
          {
            rc = SQLITE_ERROR;
            break;
          }
      }
      va_end( ap );
      return rc;
    }

    /*
    ** Set up the lookaside buffers for a database connection.
    ** Return SQLITE_OK on success.
    ** If lookaside is already active, return SQLITE_BUSY.
    **
    ** The sz parameter is the number of bytes in each lookaside slot.
    ** The cnt parameter is the number of slots.  If pStart is NULL the
    ** space for the lookaside memory is obtained from sqlite3_malloc().
    ** If pStart is not NULL then it is sz*cnt bytes of memory to use for
    ** the lookaside memory.
    */
    static int setupLookaside( sqlite3 db, byte[] pBuf, int sz, int cnt )
    {
      //void* pStart;
      //if ( db.lookaside.nOut )
      //{
      //  return SQLITE_BUSY;
      //}
      ///* Free any existing lookaside buffer for this handle before
      //** allocating a new one so we don't have to have space for
      //** both at the same time.
      //*/
      //if ( db.lookaside.bMalloced )
      //{
      //  //sqlite3_free( db.lookaside.pStart );
      //}
      ///* The size of a lookaside slot needs to be larger than a pointer
      //** to be useful.
      //*/
      //if ( sz <= (int)sizeof( LookasideSlot* ) ) sz = 0;
      //if ( cnt < 0 ) cnt = 0;
      //if ( sz == 0 || cnt == 0 )
      //{
      //  sz = 0;
      //  pStart = 0;
      //}
      //else if ( pBuf == 0 )
      //{
      //   sz = ROUND8(sz);
      //  sqlite3BeginBenignMalloc();
      //  pStart = sqlite3Malloc( sz * cnt );
      //  sqlite3EndBenignMalloc();
      //}
      //else
      //{
      //  ROUNDDOWN8(sz);
      //  pStart = pBuf;
      //}
      //db.lookaside.pStart = pStart;
      //db.lookaside.pFree = 0;
      //db.lookaside.sz = (u16)sz;
      //if ( pStart )
      //{
      //  int i;
      //  LookasideSlot* p;
      //  Debug.Assert( sz > sizeof( LookasideSlot* ) );
      //  p = (LookasideSlot*)pStart;
      //  for ( i = cnt - 1 ; i >= 0 ; i-- )
      //  {
      //    p.pNext = db.lookaside.pFree;
      //    db.lookaside.pFree = p;
      //    p = (LookasideSlot*)&( (u8*)p )[sz];
      //  }
      //  db.lookaside.pEnd = p;
      //  db.lookaside.bEnabled = 1;
      //  db.lookaside.bMalloced = pBuf == 0 ? 1 : 0;
      //}
      //else
      //{
      //  db.lookaside.pEnd = 0;
      //  db.lookaside.bEnabled = 0;
      //  db.lookaside.bMalloced = 0;
      //}
      return SQLITE_OK;
    }

    /*
    ** Return the mutex associated with a database connection.
    */
    sqlite3_mutex sqlite3_db_mutex( sqlite3 db )
    {
      return db.mutex;
    }

    /*
    ** Configuration settings for an individual database connection
    */
    static int sqlite3_db_config( sqlite3 db, int op, params object[] ap )
    {
      //va_list ap;
      int rc;
      va_start( ap, "" );
      switch ( op )
      {
        case SQLITE_DBCONFIG_LOOKASIDE:
          {
            byte[] pBuf = (byte[])va_arg( ap, "byte[]" );
            int sz = (int)va_arg( ap, "int" );
            int cnt = (int)va_arg( ap, "int" );
            rc = setupLookaside( db, pBuf, sz, cnt );
            break;
          }
        default:
          {
            rc = SQLITE_ERROR;
            break;
          }
      }
      va_end( ap );
      return rc;
    }


    /*
    ** Return true if the buffer z[0..n-1] contains all spaces.
    */
    static bool allSpaces( string z, int iStart, int n )
    {
      while ( n > 0 && z[iStart + n - 1] == ' ' ) { n--; }
      return n == 0;
    }

    /*
    ** This is the default collating function named "BINARY" which is always
    ** available.
    **
    ** If the padFlag argument is not NULL then space padding at the end
    ** of strings is ignored.  This implements the RTRIM collation.
    */
    static int binCollFunc(
    object padFlag,
    int nKey1, string pKey1,
    int nKey2, string pKey2
    )
    {
      int rc, n;
      n = nKey1 < nKey2 ? nKey1 : nKey2;
      rc = memcmp( pKey1, pKey2, n );
      if ( rc == 0 )
      {
        if ( (int)padFlag != 0 && allSpaces( pKey1, n, nKey1 - n ) && allSpaces( pKey2, n, nKey2 - n ) )
        {
          /* Leave rc unchanged at 0 */
        }
        else
        {
          rc = nKey1 - nKey2;
        }
      }
      return rc;
    }

    /*
    ** Another built-in collating sequence: NOCASE.
    **
    ** This collating sequence is intended to be used for "case independant
    ** comparison". SQLite's knowledge of upper and lower case equivalents
    ** extends only to the 26 characters used in the English language.
    **
    ** At the moment there is only a UTF-8 implementation.
    */
    static int nocaseCollatingFunc(
    object NotUsed,
    int nKey1, string pKey1,
    int nKey2, string pKey2
    )
    {
      int n = ( nKey1 < nKey2 ) ? nKey1 : nKey2;
      int r = sqlite3StrNICmp( pKey1, pKey2, ( nKey1 < nKey2 ) ? nKey1 : nKey2 );
      UNUSED_PARAMETER( NotUsed );
      if ( 0 == r )
      {
        r = nKey1 - nKey2;
      }
      return r;
    }

    /*
    ** Return the ROWID of the most recent insert
    */
    public static sqlite_int64 sqlite3_last_insert_rowid( sqlite3 db )
    {
      return db.lastRowid;
    }

    /*
    ** Return the number of changes in the most recent call to sqlite3_exec().
    */
    public static int sqlite3_changes( sqlite3 db )
    {
      return db.nChange;
    }

    /*
    ** Return the number of changes since the database handle was opened.
    */
    public static int sqlite3_total_changes( sqlite3 db )
    {
      return db.nTotalChange;
    }

    /*
    ** Close all open savepoints. This function only manipulates fields of the
    ** database handle object, it does not close any savepoints that may be open
    ** at the b-tree/pager level.
    */
    static void sqlite3CloseSavepoints( sqlite3 db )
    {
      while ( db.pSavepoint != null )
      {
        Savepoint pTmp = db.pSavepoint;
        db.pSavepoint = pTmp.pNext;
        //sqlite3DbFree( db, ref pTmp );
      }
      db.nSavepoint = 0;
      db.nStatement = 0;
      db.isTransactionSavepoint = 0;
    }

    /*
    ** Close an existing SQLite database
    */
    public static int sqlite3_close( sqlite3 db )
    {
      HashElem i;
      int j;

      if ( db == null )
      {
        return SQLITE_OK;
      }
      if ( !sqlite3SafetyCheckSickOrOk( db ) )
      {
        return SQLITE_MISUSE;
      }
      sqlite3_mutex_enter( db.mutex );

      sqlite3ResetInternalSchema( db, 0 );

      /* Tell the code in notify.c that the connection no longer holds any
      ** locks and does not require any further unlock-notify callbacks.
      */
      sqlite3ConnectionClosed( db );

      /* If a transaction is open, the ResetInternalSchema() call above
      ** will not have called the xDisconnect() method on any virtual
      ** tables in the db.aVTrans[] array. The following sqlite3VtabRollback()
      ** call will do so. We need to do this before the check for active
      ** SQL statements below, as the v-table implementation may be storing
      ** some prepared statements internally.
      */

      sqlite3VtabRollback( db );

      /* If there are any outstanding VMs, return SQLITE_BUSY. */
      if ( db.pVdbe != null )
      {
        sqlite3Error( db, SQLITE_BUSY,
        "unable to close due to unfinalised statements" );
        sqlite3_mutex_leave( db.mutex );
        return SQLITE_BUSY;
      }
      Debug.Assert( sqlite3SafetyCheckSickOrOk( db ) );

      for ( j = 0 ; j < db.nDb ; j++ )
      {
        Btree pBt = db.aDb[j].pBt;
        if ( pBt != null && sqlite3BtreeIsInBackup( pBt ) )
        {
          sqlite3Error( db, SQLITE_BUSY,
          "unable to close due to unfinished backup operation" );
          sqlite3_mutex_leave( db.mutex );
          return SQLITE_BUSY;
        }
      }

      /* Free any outstanding Savepoint structures. */
      sqlite3CloseSavepoints( db );

      for ( j = 0 ; j < db.nDb ; j++ )
      {
        Db pDb = db.aDb[j];
        if ( pDb.pBt != null )
        {
          sqlite3BtreeClose( ref pDb.pBt );
          pDb.pBt = null;
          if ( j != 1 )
          {
            pDb.pSchema = null;
          }
        }
      }
      sqlite3ResetInternalSchema( db, 0 );
      Debug.Assert( db.nDb <= 2 );
      Debug.Assert( db.aDb[0].Equals( db.aDbStatic[0] ) );
      for ( j = 0 ; j < ArraySize( db.aFunc.a ) ; j++ )
      {
        FuncDef pNext, pHash, p;
        for ( p = db.aFunc.a[j] ; p != null ; p = pHash )
        {
          pHash = p.pHash;
          while ( p != null )
          {
            pNext = p.pNext;
            //sqlite3DbFree( db, p );
            p = pNext;
          }

        }
      }

      for ( i = db.aCollSeq.first ; i != null ; i = i.next )
      {//sqliteHashFirst(db.aCollSeq); i!=null; i=sqliteHashNext(i)){
        CollSeq[] pColl = (CollSeq[])i.data;// sqliteHashData(i);
        /* Invoke any destructors registered for collation sequence user data. */
        for ( j = 0 ; j < 3 ; j++ )
        {
          if ( pColl[j].xDel != null )
          {
            pColl[j].xDel( ref  pColl[j].pUser );
          }
        }
        //sqlite3DbFree( db, ref pColl );
      }
      sqlite3HashClear( db.aCollSeq );
#if !SQLITE_OMIT_VIRTUALTABLE
for(i=sqliteHashFirst(&db.aModule); i; i=sqliteHashNext(i)){
Module pMod = (Module *)sqliteHashData(i);
if( pMod.xDestroy ){
pMod.xDestroy(pMod.pAux);
}
//sqlite3DbFree(db,ref pMod);
}
sqlite3HashClear(&db.aModule);
#endif

      sqlite3Error( db, SQLITE_OK, 0 ); /* Deallocates any cached error strings. */
      if ( db.pErr != null )
      {
        sqlite3ValueFree( ref db.pErr );
      }
#if !SQLITE_OMIT_LOAD_EXTENSION
      sqlite3CloseExtensions( db );
#endif

      db.magic = SQLITE_MAGIC_ERROR;

      /* The temp.database schema is allocated differently from the other schema
      ** objects (using sqliteMalloc() directly, instead of sqlite3BtreeSchema()).
      ** So it needs to be freed here. Todo: Why not roll the temp schema into
      ** the same sqliteMalloc() as the one that allocates the database
      ** structure?
      */
      //sqlite3DbFree( db, ref db.aDb[1].pSchema );
      sqlite3_mutex_leave( db.mutex );
      db.magic = SQLITE_MAGIC_CLOSED;
      sqlite3_mutex_free( ref db.mutex );
      Debug.Assert( db.lookaside.nOut == 0 );  /* Fails on a lookaside memory leak */
      if ( db.lookaside.bMalloced )
      {
        ////sqlite3_free( ref db.lookaside.pStart );
      }
      //sqlite3_free( ref db );
      return SQLITE_OK;
    }

    /*
    ** Rollback all database files.
    */
    static void sqlite3RollbackAll( sqlite3 db )
    {
      int i;
      int inTrans = 0;
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      sqlite3BeginBenignMalloc();
      for ( i = 0 ; i < db.nDb ; i++ )
      {
        if ( db.aDb[i].pBt != null )
        {
          if ( sqlite3BtreeIsInTrans( db.aDb[i].pBt ) )
          {
            inTrans = 1;
          }
          sqlite3BtreeRollback( db.aDb[i].pBt );
          db.aDb[i].inTrans = 0;
        }
      }

      sqlite3VtabRollback( db );
      sqlite3EndBenignMalloc();
      if ( ( db.flags & SQLITE_InternChanges ) != 0 )
      {
        sqlite3ExpirePreparedStatements( db );
        sqlite3ResetInternalSchema( db, 0 );
      }

      /* If one has been configured, invoke the rollback-hook callback */
      if ( db.xRollbackCallback != null && ( inTrans != 0 || 0 == db.autoCommit ) )
      {
        db.xRollbackCallback( db.pRollbackArg );
      }
    }

    /*
    ** Return a static string that describes the kind of error specified in the
    ** argument.
    */
    static string sqlite3ErrStr( int rc )
    {
      string[] aMsg = new string[]{
/* SQLITE_OK          */ "not an error",
/* SQLITE_ERROR       */ "SQL logic error or missing database",
/* SQLITE_INTERNAL    */ "",
/* SQLITE_PERM        */ "access permission denied",
/* SQLITE_ABORT       */ "callback requested query abort",
/* SQLITE_BUSY        */ "database is locked",
/* SQLITE_LOCKED      */ "database table is locked",
/* SQLITE_NOMEM       */ "out of memory",
/* SQLITE_READONLY    */ "attempt to write a readonly database",
/* SQLITE_INTERRUPT   */ "interrupted",
/* SQLITE_IOERR       */ "disk I/O error",
/* SQLITE_CORRUPT     */ "database disk image is malformed",
/* SQLITE_NOTFOUND    */ "",
/* SQLITE_FULL        */ "database or disk is full",
/* SQLITE_CANTOPEN    */ "unable to open database file",
/* SQLITE_PROTOCOL    */ "",
/* SQLITE_EMPTY       */ "table contains no data",
/* SQLITE_SCHEMA      */ "database schema has changed",
/* SQLITE_TOOBIG      */ "string or blob too big",
/* SQLITE_CONSTRAINT  */ "constraint failed",
/* SQLITE_MISMATCH    */ "datatype mismatch",
/* SQLITE_MISUSE      */ "library routine called out of sequence",
/* SQLITE_NOLFS       */ "large file support is disabled",
/* SQLITE_AUTH        */ "authorization denied",
/* SQLITE_FORMAT      */ "auxiliary database format error",
/* SQLITE_RANGE       */ "bind or column index out of range",
/* SQLITE_NOTADB      */ "file is encrypted or is not a database",
};
      rc &= 0xff;
      if ( ALWAYS( rc >= 0 ) && rc < aMsg.Length && aMsg[rc] != "" )//(int)(sizeof(aMsg)/sizeof(aMsg[0]))
      {
        return aMsg[rc];
      }
      else
      {
        return "unknown error";
      }
    }

    /*
    ** This routine implements a busy callback that sleeps and tries
    ** again until a timeout value is reached.  The timeout value is
    ** an integer number of milliseconds passed in as the first
    ** argument.
    */
    static int sqliteDefaultBusyCallback(
    object ptr,               /* Database connection */
    int count                /* Number of times table has been busy */
    )
    {
#if SQLITE_OS_WIN || HAVE_USLEEP
      u8[] delays = new u8[] { 1, 2, 5, 10, 15, 20, 25, 25, 25, 50, 50, 100 };
      u8[] totals = new u8[] { 0, 1, 3, 8, 18, 33, 53, 78, 103, 128, 178, 228 };
      //# define NDELAY (delays.Length/sizeof(delays[0]))
      int NDELAY = delays.Length;
      sqlite3 db = (sqlite3)ptr;
      int timeout = db.busyTimeout;
      int delay, prior;

      Debug.Assert( count >= 0 );
      if ( count < NDELAY )
      {
        delay = delays[count];
        prior = totals[count];
      }
      else
      {
        delay = delays[NDELAY - 1];
        prior = totals[NDELAY - 1] + delay * ( count - ( NDELAY - 1 ) );
      }
      if ( prior + delay > timeout )
      {
        delay = timeout - prior;
        if ( delay <= 0 ) return 0;
      }
      sqlite3OsSleep( db.pVfs, delay * 1000 );
      return 1;
#else
sqlite3 db = (sqlite3)ptr;
int timeout = ( (sqlite3)ptr ).busyTimeout;
if ( ( count + 1 ) * 1000 > timeout )
{
return 0;
}
sqlite3OsSleep( db.pVfs, 1000000 );
return 1;
#endif
    }

    /*
    ** Invoke the given busy handler.
    **
    ** This routine is called when an operation failed with a lock.
    ** If this routine returns non-zero, the lock is retried.  If it
    ** returns 0, the operation aborts with an SQLITE_BUSY error.
    */
    static int sqlite3InvokeBusyHandler( BusyHandler p )
    {
      int rc;
      if ( NEVER( p == null ) || p.xFunc == null || p.nBusy < 0 ) return 0;
      rc = p.xFunc( p.pArg, p.nBusy );
      if ( rc == 0 )
      {
        p.nBusy = -1;
      }
      else
      {
        p.nBusy++;
      }
      return rc;
    }

    /*
    ** This routine sets the busy callback for an Sqlite database to the
    ** given callback function with the given argument.
    */
    static int sqlite3_busy_handler(
    sqlite3 db,
    dxBusy xBusy,
    object pArg
    )
    {
      sqlite3_mutex_enter( db.mutex );
      db.busyHandler.xFunc = xBusy;
      db.busyHandler.pArg = pArg;
      db.busyHandler.nBusy = 0;
      sqlite3_mutex_leave( db.mutex );
      return SQLITE_OK;
    }

#if !SQLITE_OMIT_PROGRESS_CALLBACK
    /*
** This routine sets the progress callback for an Sqlite database to the
** given callback function with the given argument. The progress callback will
** be invoked every nOps opcodes.
*/
    static void sqlite3_progress_handler(
    sqlite3 db,
    int nOps,
    dxProgress xProgress, //int (xProgress)(void*),
    object pArg
    )
    {
      sqlite3_mutex_enter( db.mutex );
      if ( nOps > 0 )
      {
        db.xProgress = xProgress;
        db.nProgressOps = nOps;
        db.pProgressArg = pArg;
      }
      else
      {
        db.xProgress = null;
        db.nProgressOps = 0;
        db.pProgressArg = null;
      }
      sqlite3_mutex_leave( db.mutex );
    }
#endif


    /*
** This routine installs a default busy handler that waits for the
** specified number of milliseconds before returning 0.
*/
    public static int sqlite3_busy_timeout( sqlite3 db, int ms )
    {
      if ( ms > 0 )
      {
        db.busyTimeout = ms;
        sqlite3_busy_handler( db, sqliteDefaultBusyCallback, db );
      }
      else
      {
        sqlite3_busy_handler( db, null, null );
      }
      return SQLITE_OK;
    }

    /*
    ** Cause any pending operation to stop at its earliest opportunity.
    */
    static void sqlite3_interrupt( sqlite3 db )
    {
      db.u1.isInterrupted = true;
    }


    /*
    ** This function is exactly the same as sqlite3_create_function(), except
    ** that it is designed to be called by internal code. The difference is
    ** that if a malloc() fails in sqlite3_create_function(), an error code
    ** is returned and the mallocFailed flag cleared.
    */
    static int sqlite3CreateFunc(
    sqlite3 db,
    string zFunctionName,
    int nArg,
    u8 enc,
    object pUserData,
    dxFunc xFunc, //)(sqlite3_context*,int,sqlite3_value **),
    dxStep xStep,//)(sqlite3_context*,int,sqlite3_value **),
    dxFinal xFinal//)(sqlite3_context*)
    )
    {
      FuncDef p;
      int nName;

      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      if ( zFunctionName == null ||
      ( xFunc != null && ( xFinal != null || xStep != null ) ) ||
      ( xFunc == null && ( xFinal != null && xStep == null ) ) ||
      ( xFunc == null && ( xFinal == null && xStep != null ) ) ||
      ( nArg < -1 || nArg > SQLITE_MAX_FUNCTION_ARG ) ||
      ( 255 < ( nName = sqlite3Strlen30( zFunctionName ) ) ) )
      {
        return SQLITE_MISUSE;
      }

#if !SQLITE_OMIT_UTF16
/* If SQLITE_UTF16 is specified as the encoding type, transform this
** to one of SQLITE_UTF16LE or SQLITE_UTF16BE using the
** SQLITE_UTF16NATIVE macro. SQLITE_UTF16 is not used internally.
**
** If SQLITE_ANY is specified, add three versions of the function
** to the hash table.
*/
if( enc==SQLITE_UTF16 ){
enc = SQLITE_UTF16NATIVE;
}else if( enc==SQLITE_ANY ){
int rc;
rc = sqlite3CreateFunc(db, zFunctionName, nArg, SQLITE_UTF8,
pUserData, xFunc, xStep, xFinal);
if( rc==SQLITE_OK ){
rc = sqlite3CreateFunc(db, zFunctionName, nArg, SQLITE_UTF16LE,
pUserData, xFunc, xStep, xFinal);
}
if( rc!=SQLITE_OK ){
return rc;
}
enc = SQLITE_UTF16BE;
}
#else
      enc = SQLITE_UTF8;
#endif

      /* Check if an existing function is being overridden or deleted. If so,
** and there are active VMs, then return SQLITE_BUSY. If a function
** is being overridden/deleted but there are no active VMs, allow the
** operation to continue but invalidate all precompiled statements.
*/
      p = sqlite3FindFunction( db, zFunctionName, nName, nArg, enc, 0 );
      if ( p != null && p.iPrefEnc == enc && p.nArg == nArg )
      {
        if ( db.activeVdbeCnt != 0 )
        {
          sqlite3Error( db, SQLITE_BUSY,
          "unable to delete/modify user-function due to active statements" );
          //Debug.Assert( 0 == db.mallocFailed );
          return SQLITE_BUSY;
        }
        else
        {
          sqlite3ExpirePreparedStatements( db );
        }
      }

      p = sqlite3FindFunction( db, zFunctionName, nName, nArg, enc, 1 );
      Debug.Assert( p != null /*|| db.mallocFailed != 0 */ );
      if ( p == null )
      {
        return SQLITE_NOMEM;
      }
      p.flags = 0;
      p.xFunc = xFunc;
      p.xStep = xStep;
      p.xFinalize = xFinal;
      p.pUserData = pUserData;
      p.nArg = (i16)nArg;
      return SQLITE_OK;
    }

    /*
    ** Create new user functions.
    */
    public static int sqlite3_create_function(
    sqlite3 db,
    string zFunctionName,
    int nArg,
    u8 enc,
    object p,
    dxFunc xFunc, //)(sqlite3_context*,int,sqlite3_value **),
    dxStep xStep,//)(sqlite3_context*,int,sqlite3_value **),
    dxFinal xFinal//)(sqlite3_context*)
    )
    {
      int rc;
      sqlite3_mutex_enter( db.mutex );
      rc = sqlite3CreateFunc( db, zFunctionName, nArg, enc, p, xFunc, xStep, xFinal );
      rc = sqlite3ApiExit( db, rc );
      sqlite3_mutex_leave( db.mutex );
      return rc;
    }

#if !SQLITE_OMIT_UTF16
static int sqlite3_create_function16(
sqlite3 db,
string zFunctionName,
int nArg,
int eTextRep,
object p,
dxFunc xFunc,   //)(sqlite3_context*,int,sqlite3_value**),
dxStep xStep,   //)(sqlite3_context*,int,sqlite3_value**),
dxFinal xFinal  //)(sqlite3_context*)
){
int rc;
string zFunc8;
sqlite3_mutex_enter(db.mutex);
Debug.Assert( 0==db.mallocFailed );
zFunc8 = sqlite3Utf16to8(db, zFunctionName, -1);
rc = sqlite3CreateFunc(db, zFunc8, nArg, eTextRep, p, xFunc, xStep, xFinal);
//sqlite3DbFree(db,ref zFunc8);
rc = sqlite3ApiExit(db, rc);
sqlite3_mutex_leave(db.mutex);
return rc;
}
#endif


    /*
** Declare that a function has been overloaded by a virtual table.
**
** If the function already exists as a regular global function, then
** this routine is a no-op.  If the function does not exist, then create
** a new one that always throws a run-time error.
**
** When virtual tables intend to provide an overloaded function, they
** should call this routine to make sure the global function exists.
** A global function must exist in order for name resolution to work
** properly.
*/
    static int sqlite3_overload_function(
    sqlite3 db,
    string zName,
    int nArg
    )
    {
      int nName = sqlite3Strlen30( zName );
      int rc;
      sqlite3_mutex_enter( db.mutex );
      if ( sqlite3FindFunction( db, zName, nName, nArg, SQLITE_UTF8, 0 ) == null )
      {
        sqlite3CreateFunc( db, zName, nArg, SQLITE_UTF8,
        0, (dxFunc)sqlite3InvalidFunction, null, null );
      }
      rc = sqlite3ApiExit( db, SQLITE_OK );
      sqlite3_mutex_leave( db.mutex );
      return rc;
    }

#if !SQLITE_OMIT_TRACE
    /*
** Register a trace function.  The pArg from the previously registered trace
** is returned.
**
** A NULL trace function means that no tracing is executes.  A non-NULL
** trace is a pointer to a function that is invoked at the start of each
** SQL statement.
*/
    static object sqlite3_trace( sqlite3 db, dxTrace xTrace, object pArg )
    {// (*xTrace)(void*,const char*), object pArg){
      object pOld;
      sqlite3_mutex_enter( db.mutex );
      pOld = db.pTraceArg;
      db.xTrace = xTrace;
      db.pTraceArg = pArg;
      sqlite3_mutex_leave( db.mutex );
      return pOld;
    }
    /*
    ** Register a profile function.  The pArg from the previously registered
    ** profile function is returned.
    **
    ** A NULL profile function means that no profiling is executes.  A non-NULL
    ** profile is a pointer to a function that is invoked at the conclusion of
    ** each SQL statement that is run.
    */
    static object sqlite3_profile(
    sqlite3 db,
    dxProfile xProfile,//void (*xProfile)(void*,const char*,sqlite_u3264),
    object pArg
    )
    {
      object pOld;
      sqlite3_mutex_enter( db.mutex );
      pOld = db.pProfileArg;
      db.xProfile = xProfile;
      db.pProfileArg = pArg;
      sqlite3_mutex_leave( db.mutex );
      return pOld;
    }
#endif // * SQLITE_OMIT_TRACE */

    /*** EXPERIMENTAL ***
**
** Register a function to be invoked when a transaction comments.
** If the invoked function returns non-zero, then the commit becomes a
** rollback.
*/
    static object sqlite3_commit_hook(
    sqlite3 db,             /* Attach the hook to this database */
    dxCommitCallback xCallback,   //int (*xCallback)(void*),  /* Function to invoke on each commit */
    object pArg             /* Argument to the function */
    )
    {
      object pOld;
      sqlite3_mutex_enter( db.mutex );
      pOld = db.pCommitArg;
      db.xCommitCallback = xCallback;
      db.pCommitArg = pArg;
      sqlite3_mutex_leave( db.mutex );
      return pOld;
    }

    /*
    ** Register a callback to be invoked each time a row is updated,
    ** inserted or deleted using this database connection.
    */
    static object sqlite3_update_hook(
    sqlite3 db,             /* Attach the hook to this database */
    dxUpdateCallback xCallback,   //void (*xCallback)(void*,int,char const *,char const *,sqlite_int64),
    object pArg             /* Argument to the function */
    )
    {
      object pRet;
      sqlite3_mutex_enter( db.mutex );
      pRet = db.pUpdateArg;
      db.xUpdateCallback = xCallback;
      db.pUpdateArg = pArg;
      sqlite3_mutex_leave( db.mutex );
      return pRet;
    }

    /*
    ** Register a callback to be invoked each time a transaction is rolled
    ** back by this database connection.
    */
    static object sqlite3_rollback_hook(
    sqlite3 db,             /* Attach the hook to this database */
    dxRollbackCallback xCallback,   //void (*xCallback)(void*), /* Callback function */
    object pArg             /* Argument to the function */
    )
    {
      object pRet;
      sqlite3_mutex_enter( db.mutex );
      pRet = db.pRollbackArg;
      db.xRollbackCallback = xCallback;
      db.pRollbackArg = pArg;
      sqlite3_mutex_leave( db.mutex );
      return pRet;
    }

    /*
    ** This function returns true if main-memory should be used instead of
    ** a temporary file for transient pager files and statement journals.
    ** The value returned depends on the value of db->temp_store (runtime
    ** parameter) and the compile time value of SQLITE_TEMP_STORE. The
    ** following table describes the relationship between these two values
    ** and this functions return value.
    **
    **   SQLITE_TEMP_STORE     db->temp_store     Location of temporary database
    **   -----------------     --------------     ------------------------------
    **   0                     any                file      (return 0)
    **   1                     1                  file      (return 0)
    **   1                     2                  memory    (return 1)
    **   1                     0                  file      (return 0)
    **   2                     1                  file      (return 0)
    **   2                     2                  memory    (return 1)
    **   2                     0                  memory    (return 1)
    **   3                     any                memory    (return 1)
    */
    static bool sqlite3TempInMemory( sqlite3 db )
    {
      //#if SQLITE_TEMP_STORE==1
      if ( SQLITE_TEMP_STORE == 1 )
        return ( db.temp_store == 2 );
      //#endif
      //#if SQLITE_TEMP_STORE==2
      if ( SQLITE_TEMP_STORE == 2 )
        return ( db.temp_store != 1 );
      //#endif
      //#if SQLITE_TEMP_STORE==3
      if ( SQLITE_TEMP_STORE == 3 )
        return true;
      //#endif
      //#if SQLITE_TEMP_STORE<1 || SQLITE_TEMP_STORE>3
      if ( SQLITE_TEMP_STORE < 1 || SQLITE_TEMP_STORE > 3 )
        return false;
      //#endif
      return false;
    }

    /*
    ** This routine is called to create a connection to a database BTree
    ** driver.  If zFilename is the name of a file, then that file is
    ** opened and used.  If zFilename is the magic name ":memory:" then
    ** the database is stored in memory (and is thus forgotten as soon as
    ** the connection is closed.)  If zFilename is NULL then the database
    ** is a "virtual" database for transient use only and is deleted as
    ** soon as the connection is closed.
    **
    ** A virtual database can be either a disk file (that is automatically
    ** deleted when the file is closed) or it an be held entirely in memory.
    ** The sqlite3TempInMemory() function is used to determine which.
    */
    static int sqlite3BtreeFactory(
    sqlite3 db,           /* Main database when opening aux otherwise 0 */
    string zFilename,     /* Name of the file containing the BTree database */
    bool omitJournal,     /* if TRUE then do not journal this file */
    int nCache,           /* How many pages in the page cache */
    int vfsFlags,         /* Flags passed through to vfsOpen */
    ref Btree ppBtree     /* Pointer to new Btree object written here */
    )
    {
      int btFlags = 0;
      int rc;

      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      //Debug.Assert( ppBtree != null);
      if ( omitJournal )
      {
        btFlags |= BTREE_OMIT_JOURNAL;
      }
      if ( ( db.flags & SQLITE_NoReadlock ) != 0 )
      {
        btFlags |= BTREE_NO_READLOCK;
      }
#if !SQLITE_OMIT_MEMORYDB
      if ( String.IsNullOrEmpty( zFilename ) && sqlite3TempInMemory( db ) )
      {

        zFilename = ":memory:";
      }
#endif // * SQLITE_OMIT_MEMORYDB */

      if ( ( vfsFlags & SQLITE_OPEN_MAIN_DB ) != 0 && ( zFilename == null ) )
      {// || *zFilename==0) ){
        vfsFlags = ( vfsFlags & ~SQLITE_OPEN_MAIN_DB ) | SQLITE_OPEN_TEMP_DB;
      }
      rc = sqlite3BtreeOpen( zFilename, db, ref ppBtree, btFlags, vfsFlags );
      /* If the B-Tree was successfully opened, set the pager-cache size to the
      ** default value. Except, if the call to BtreeOpen() returned a handle
      ** open on an existing shared pager-cache, do not change the pager-cache
      ** size.
      */
      if ( rc == SQLITE_OK && null == sqlite3BtreeSchema( ppBtree, 0, null ) )
      {
        sqlite3BtreeSetCacheSize( ppBtree, nCache );
      }
      return rc;
    }

    /*
    ** Return UTF-8 encoded English language explanation of the most recent
    ** error.
    */
    public static string sqlite3_errmsg( sqlite3 db )
    {
      string z;
      if ( db == null )
      {
        return sqlite3ErrStr( SQLITE_NOMEM );
      }
      if ( !sqlite3SafetyCheckSickOrOk( db ) )
      {
        return sqlite3ErrStr( SQLITE_MISUSE );
      }
      sqlite3_mutex_enter( db.mutex );
      //if ( db.mallocFailed != 0 )
      //{
      //  z = sqlite3ErrStr( SQLITE_NOMEM );
      //}
      //else
      {
        z = sqlite3_value_text( db.pErr );
        //Debug.Assert( 0 == db.mallocFailed );
        if ( String.IsNullOrEmpty( z ))
        {
          z = sqlite3ErrStr( db.errCode );
        }
      }
      sqlite3_mutex_leave( db.mutex );
      return z;
    }

#if !SQLITE_OMIT_UTF16
/*
** Return UTF-16 encoded English language explanation of the most recent
** error.
*/
const void *sqlite3_errmsg16(sqlite3 *db){
static const u16 outOfMem[] = {
'o', 'u', 't', ' ', 'o', 'f', ' ', 'm', 'e', 'm', 'o', 'r', 'y', 0
};
static const u16 misuse[] = {
'l', 'i', 'b', 'r', 'a', 'r', 'y', ' ',
'r', 'o', 'u', 't', 'i', 'n', 'e', ' ',
'c', 'a', 'l', 'l', 'e', 'd', ' ',
'o', 'u', 't', ' ',
'o', 'f', ' ',
's', 'e', 'q', 'u', 'e', 'n', 'c', 'e', 0
};

const void *z;
if( !db ){
return (void *)outOfMem;
}
if( !sqlite3SafetyCheckSickOrOk(db) ){
return (void *)misuse;
}
sqlite3_mutex_enter(db->mutex);
if( db->mallocFailed ){
z = (void *)outOfMem;
}else{
z = sqlite3_value_text16(db->pErr);
if( z==0 ){
sqlite3ValueSetStr(db->pErr, -1, sqlite3ErrStr(db->errCode),
SQLITE_UTF8, SQLITE_STATIC);
z = sqlite3_value_text16(db->pErr);
}
/* A malloc() may have failed within the call to sqlite3_value_text16()
** above. If this is the case, then the db->mallocFailed flag needs to
** be cleared before returning. Do this directly, instead of via
** sqlite3ApiExit(), to avoid setting the database handle error message.
*/
db->mallocFailed = 0;
}
sqlite3_mutex_leave(db->mutex);
return z;
}
#endif // * SQLITE_OMIT_UTF16 */

    /*
** Return the most recent error code generated by an SQLite routine. If NULL is
** passed to this function, we assume a malloc() failed during sqlite3_open().
*/
    public static int sqlite3_errcode( sqlite3 db )
    {
      if ( db != null && !sqlite3SafetyCheckSickOrOk( db ) )
      {
        return SQLITE_MISUSE;
      }
      if ( null == db /*|| db.mallocFailed != 0 */ )
      {
        return SQLITE_NOMEM;
      }
      return db.errCode & db.errMask;
    }
    static int sqlite3_extended_errcode( sqlite3 db )
    {
      if ( db != null && !sqlite3SafetyCheckSickOrOk( db ) )
      {
        return SQLITE_MISUSE;
      }
      if ( null == db /*|| db.mallocFailed != 0 */ )
      {
        return SQLITE_NOMEM;
      }
      return db.errCode;
    }
    /*
    ** Create a new collating function for database "db".  The name is zName
    ** and the encoding is enc.
    */
    static int createCollation(
    sqlite3 db,
    string zName,
    int enc,
    object pCtx,
    dxCompare xCompare,//)(void*,int,const void*,int,const void*),
    dxDelCollSeq xDel//)(void*)
    )
    {
      CollSeq pColl;
      int enc2;
      int nName = sqlite3Strlen30( zName );

      Debug.Assert( sqlite3_mutex_held( db.mutex ) );

      /* If SQLITE_UTF16 is specified as the encoding type, transform this
      ** to one of SQLITE_UTF16LE or SQLITE_UTF16BE using the
      ** SQLITE_UTF16NATIVE macro. SQLITE_UTF16 is not used internally.
      */
      enc2 = enc;
      testcase( enc2 == SQLITE_UTF16 );
      testcase( enc2 == SQLITE_UTF16_ALIGNED );
      if ( enc2 == SQLITE_UTF16 || enc2 == SQLITE_UTF16_ALIGNED )
      {
        enc2 = SQLITE_UTF16NATIVE;
      }
      if ( enc2 < SQLITE_UTF8 || enc2 > SQLITE_UTF16BE )
      {
        return SQLITE_MISUSE;
      }

      /* Check if this call is removing or replacing an existing collation
      ** sequence. If so, and there are active VMs, return busy. If there
      ** are no active VMs, invalidate any pre-compiled statements.
      */
      pColl = sqlite3FindCollSeq( db, (u8)enc2, zName, 0 );
      if ( pColl != null && pColl.xCmp != null )
      {
        if ( db.activeVdbeCnt != 0 )
        {
          sqlite3Error( db, SQLITE_BUSY,
          "unable to delete/modify collation sequence due to active statements" );
          return SQLITE_BUSY;
        }
        sqlite3ExpirePreparedStatements( db );

        /* If collation sequence pColl was created directly by a call to
        ** sqlite3_create_collation, and not generated by synthCollSeq(),
        ** then any copies made by synthCollSeq() need to be invalidated.
        ** Also, collation destructor - CollSeq.xDel() - function may need
        ** to be called.
        */
        if ( ( pColl.enc & ~SQLITE_UTF16_ALIGNED ) == enc2 )
        {
          CollSeq[] aColl = (CollSeq[])sqlite3HashFind( db.aCollSeq, zName, nName );
          int j;
          for ( j = 0 ; j < 3 ; j++ )
          {
            CollSeq p = aColl[j];
            if ( p.enc == pColl.enc )
            {
              if ( p.xDel != null )
              {
                p.xDel( ref p.pUser );
              }
              p.xCmp = null;
            }
          }
        }
      }

      pColl = sqlite3FindCollSeq( db, (u8)enc2, zName, 1 );
      if ( pColl != null )
      {
        pColl.xCmp = xCompare;
        pColl.pUser = pCtx;
        pColl.xDel = xDel;
        pColl.enc = (u8)( enc2 | ( enc & SQLITE_UTF16_ALIGNED ) );
      }
      sqlite3Error( db, SQLITE_OK, 0 );
      return SQLITE_OK;
    }

    /*
    ** This array defines hard upper bounds on limit values.  The
    ** initializer must be kept in sync with the SQLITE_LIMIT_*
    ** #defines in sqlite3.h.
    */
    static int[] aHardLimit = new int[]  {
SQLITE_MAX_LENGTH,
SQLITE_MAX_SQL_LENGTH,
SQLITE_MAX_COLUMN,
SQLITE_MAX_EXPR_DEPTH,
SQLITE_MAX_COMPOUND_SELECT,
SQLITE_MAX_VDBE_OP,
SQLITE_MAX_FUNCTION_ARG,
SQLITE_MAX_ATTACHED,
SQLITE_MAX_LIKE_PATTERN_LENGTH,
SQLITE_MAX_VARIABLE_NUMBER,
};

    /*
    ** Make sure the hard limits are set to reasonable values
    */
    //#if SQLITE_MAX_LENGTH<100
    //# error SQLITE_MAX_LENGTH must be at least 100
    //#endif
    //#if SQLITE_MAX_SQL_LENGTH<100
    //# error SQLITE_MAX_SQL_LENGTH must be at least 100
    //#endif
    //#if SQLITE_MAX_SQL_LENGTH>SQLITE_MAX_LENGTH
    //# error SQLITE_MAX_SQL_LENGTH must not be greater than SQLITE_MAX_LENGTH
    //#endif
    //#if SQLITE_MAX_COMPOUND_SELECT<2
    //# error SQLITE_MAX_COMPOUND_SELECT must be at least 2
    //#endif
    //#if SQLITE_MAX_VDBE_OP<40
    //# error SQLITE_MAX_VDBE_OP must be at least 40
    //#endif
    //#if SQLITE_MAX_FUNCTION_ARG<0 || SQLITE_MAX_FUNCTION_ARG>1000
    //# error SQLITE_MAX_FUNCTION_ARG must be between 0 and 1000
    //#endif
    //#if SQLITE_MAX_ATTACHED<0 || SQLITE_MAX_ATTACHED>30
    //# error SQLITE_MAX_ATTACHED must be between 0 and 30
    //#endif
    //#if SQLITE_MAX_LIKE_PATTERN_LENGTH<1
    //# error SQLITE_MAX_LIKE_PATTERN_LENGTH must be at least 1
    //#endif
    //#if SQLITE_MAX_VARIABLE_NUMBER<1
    //# error SQLITE_MAX_VARIABLE_NUMBER must be at least 1
    //#endif
    //#if SQLITE_MAX_COLUMN>32767
    //# error SQLITE_MAX_COLUMN must not exceed 32767
    //#endif

    /*
    ** Change the value of a limit.  Report the old value.
    ** If an invalid limit index is supplied, report -1.
    ** Make no changes but still report the old value if the
    ** new limit is negative.
    **
    ** A new lower limit does not shrink existing constructs.
    ** It merely prevents new constructs that exceed the limit
    ** from forming.
    */
    static int sqlite3_limit( sqlite3 db, int limitId, int newLimit )
    {
      int oldLimit;
      if ( limitId < 0 || limitId >= SQLITE_N_LIMIT )
      {
        return -1;
      }
      oldLimit = db.aLimit[limitId];
      if ( newLimit >= 0 )
      {
        if ( newLimit > aHardLimit[limitId] )
        {
          newLimit = aHardLimit[limitId];
        }
        db.aLimit[limitId] = newLimit;
      }
      return oldLimit;
    }
    /*
    ** This routine does the work of opening a database on behalf of
    ** sqlite3_open() and sqlite3_open16(). The database filename "zFilename"
    ** is UTF-8 encoded.
    */
    static int openDatabase(
    string zFilename, /* Database filename UTF-8 encoded */
    ref sqlite3 ppDb,        /* OUT: Returned database handle */
    unsigned flags,        /* Operational flags */
    string zVfs       /* Name of the VFS to use */
    )
    {
      sqlite3 db;
      int rc;
      CollSeq pColl;
      int isThreadsafe;

      ppDb = null;
#if !SQLITE_OMIT_AUTOINIT
      rc = sqlite3_initialize();
      if ( rc != 0 ) return rc;
#endif

      if ( sqlite3GlobalConfig.bCoreMutex == false )
      {
        isThreadsafe = 0;
      }
      else if ( ( flags & SQLITE_OPEN_NOMUTEX ) != 0 )
      {
        isThreadsafe = 0;
      }
      else if ( ( flags & SQLITE_OPEN_FULLMUTEX ) != 0 )
      {
        isThreadsafe = 1;
      }
      else
      {
        isThreadsafe = sqlite3GlobalConfig.bFullMutex ? 1 : 0;
      }

      /* Remove harmful bits from the flags parameter
      **
      ** The SQLITE_OPEN_NOMUTEX and SQLITE_OPEN_FULLMUTEX flags were
      ** dealt with in the previous code block.  Besides these, the only
      ** valid input flags for sqlite3_open_v2() are SQLITE_OPEN_READONLY,
      ** SQLITE_OPEN_READWRITE, and SQLITE_OPEN_CREATE.  Silently mask
      ** off all other flags.
      */
      flags &= ~( SQLITE_OPEN_DELETEONCLOSE |
      SQLITE_OPEN_EXCLUSIVE |
      SQLITE_OPEN_MAIN_DB |
      SQLITE_OPEN_TEMP_DB |
      SQLITE_OPEN_TRANSIENT_DB |
      SQLITE_OPEN_MAIN_JOURNAL |
      SQLITE_OPEN_TEMP_JOURNAL |
      SQLITE_OPEN_SUBJOURNAL |
      SQLITE_OPEN_MASTER_JOURNAL |
      SQLITE_OPEN_NOMUTEX |
      SQLITE_OPEN_FULLMUTEX
      );


      /* Allocate the sqlite data structure */
      db = new sqlite3();//sqlite3MallocZero( sqlite3.Length );
      if ( db == null ) goto opendb_out;
      if ( sqlite3GlobalConfig.bFullMutex && isThreadsafe != 0 )
      {
        db.mutex = sqlite3MutexAlloc( SQLITE_MUTEX_RECURSIVE );
        if ( db.mutex == null )
        {
          //sqlite3_free( ref db );
          goto opendb_out;
        }
      }
      sqlite3_mutex_enter( db.mutex );
      db.errMask = 0xff;
      db.nDb = 2;
      db.magic = SQLITE_MAGIC_BUSY;
      Array.Copy( db.aDbStatic, db.aDb, db.aDbStatic.Length );// db.aDb = db.aDbStatic;
      Debug.Assert( db.aLimit.Length == aHardLimit.Length );
      Buffer.BlockCopy( aHardLimit, 0, db.aLimit, 0, aHardLimit.Length * sizeof( int ) );//memcpy(db.aLimit, aHardLimit, sizeof(db.aLimit));
      db.autoCommit = 1;
      db.nextAutovac = -1;
      db.nextPagesize = 0;
      db.flags |= SQLITE_ShortColNames;
      if ( SQLITE_DEFAULT_FILE_FORMAT < 4 )
        db.flags |= SQLITE_LegacyFileFmt
#if  SQLITE_ENABLE_LOAD_EXTENSION
| SQLITE_LoadExtension
#endif
;
      sqlite3HashInit( db.aCollSeq );
#if !SQLITE_OMIT_VIRTUALTABLE
sqlite3HashInit( ref db.aModule );
#endif
      db.pVfs = sqlite3_vfs_find( zVfs );
      if ( db.pVfs == null )
      {
        rc = SQLITE_ERROR;
        sqlite3Error( db, rc, "no such vfs: %s", zVfs );
        goto opendb_out;
      }

      /* Add the default collation sequence BINARY. BINARY works for both UTF-8
      ** and UTF-16, so add a version for each to avoid any unnecessary
      ** conversions. The only error that can occur here is a malloc() failure.
      */
      createCollation( db, "BINARY", SQLITE_UTF8, 0, (dxCompare)binCollFunc, null );
      createCollation( db, "BINARY", SQLITE_UTF16BE, 0, (dxCompare)binCollFunc, null );
      createCollation( db, "BINARY", SQLITE_UTF16LE, 0, (dxCompare)binCollFunc, null );
      createCollation( db, "RTRIM", SQLITE_UTF8, 1, (dxCompare)binCollFunc, null );
      //if ( db.mallocFailed != 0 )
      //{
      //  goto opendb_out;
      //}
      db.pDfltColl = sqlite3FindCollSeq( db, SQLITE_UTF8, "BINARY", 0 );
      Debug.Assert( db.pDfltColl != null );

      /* Also add a UTF-8 case-insensitive collation sequence. */
      createCollation( db, "NOCASE", SQLITE_UTF8, 0, nocaseCollatingFunc, null );

      /* Set flags on the built-in collating sequences */
      db.pDfltColl.type = SQLITE_COLL_BINARY;
      pColl = sqlite3FindCollSeq( db, SQLITE_UTF8, "NOCASE", 0 );
      if ( pColl != null )
      {
        pColl.type = SQLITE_COLL_NOCASE;
      }

      /* Open the backend database driver */
      db.openFlags = flags;
      rc = sqlite3BtreeFactory( db, zFilename, false, SQLITE_DEFAULT_CACHE_SIZE,
      flags | SQLITE_OPEN_MAIN_DB,
      ref db.aDb[0].pBt );
      if ( rc != SQLITE_OK )
      {
        if ( rc == SQLITE_IOERR_NOMEM )
        {
          rc = SQLITE_NOMEM;
        }
        sqlite3Error( db, rc, 0 );
        goto opendb_out;
      }
      db.aDb[0].pSchema = sqlite3SchemaGet( db, db.aDb[0].pBt );
      db.aDb[1].pSchema = sqlite3SchemaGet( db, null );


      /* The default safety_level for the main database is 'full'; for the temp
      ** database it is 'NONE'. This matches the pager layer defaults.
      */
      db.aDb[0].zName = "main";
      db.aDb[0].safety_level = 3;
      db.aDb[1].zName = "temp";
      db.aDb[1].safety_level = 1;

      db.magic = SQLITE_MAGIC_OPEN;
      //if ( db.mallocFailed != 0 )
      //{
      //  goto opendb_out;
      //}

      /* Register all built-in functions, but do not attempt to read the
      ** database schema yet. This is delayed until the first time the database
      ** is accessed.
      */
      sqlite3Error( db, SQLITE_OK, 0 );
      sqlite3RegisterBuiltinFunctions( db );

      /* Load automatic extensions - extensions that have been registered
      ** using the sqlite3_automatic_extension() API.
      */
      sqlite3AutoLoadExtensions( db );
      rc = sqlite3_errcode( db );
      if ( rc != SQLITE_OK )
      {
        goto opendb_out;
      }


#if  SQLITE_ENABLE_FTS1
if( 0==db.mallocFailed ){
extern int sqlite3Fts1Init(sqlite3*);
rc = sqlite3Fts1Init(db);
}
#endif

#if  SQLITE_ENABLE_FTS2
if( 0==db.mallocFailed && rc==SQLITE_OK ){
extern int sqlite3Fts2Init(sqlite3*);
rc = sqlite3Fts2Init(db);
}
#endif

#if  SQLITE_ENABLE_FTS3
if( 0==db.mallocFailed && rc==SQLITE_OK ){
rc = sqlite3Fts3Init(db);
}
#endif

#if  SQLITE_ENABLE_ICU
if( 0==db.mallocFailed && rc==SQLITE_OK ){
extern int sqlite3IcuInit(sqlite3*);
rc = sqlite3IcuInit(db);
}
#endif

#if SQLITE_ENABLE_RTREE
if( 0==db.mallocFailed && rc==SQLITE_OK){
rc = sqlite3RtreeInit(db);
}
#endif

      sqlite3Error( db, rc, 0 );

      /* -DSQLITE_DEFAULT_LOCKING_MODE=1 makes EXCLUSIVE the default locking
      ** mode.  -DSQLITE_DEFAULT_LOCKING_MODE=0 make NORMAL the default locking
      ** mode.  Doing nothing at all also makes NORMAL the default.
      */
#if  SQLITE_DEFAULT_LOCKING_MODE
db.dfltLockMode = SQLITE_DEFAULT_LOCKING_MODE;
sqlite3PagerLockingMode(sqlite3BtreePager(db.aDb[0].pBt),
SQLITE_DEFAULT_LOCKING_MODE);
#endif

      /* Enable the lookaside-malloc subsystem */
      setupLookaside( db, null, sqlite3GlobalConfig.szLookaside,
      sqlite3GlobalConfig.nLookaside );

opendb_out:
      if ( db != null )
      {
        Debug.Assert( db.mutex != null || isThreadsafe == 0 || !sqlite3GlobalConfig.bFullMutex );
        sqlite3_mutex_leave( db.mutex );
      }
      rc = sqlite3_errcode( db );
      if ( rc == SQLITE_NOMEM )
      {
        sqlite3_close( db );
        db = null;
      }
      else if ( rc != SQLITE_OK )
      {
        db.magic = SQLITE_MAGIC_SICK;
      }
      ppDb = db;
      return sqlite3ApiExit( 0, rc );
    }

    /*
    ** Open a new database handle.
    */
    public static int sqlite3_open(
    string zFilename,
    ref sqlite3 ppDb
    )
    {
      return openDatabase( zFilename, ref ppDb,
      SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE, null );
    }

    public static int sqlite3_open_v2(
    string filename,   /* Database filename (UTF-8) */
    ref sqlite3 ppDb,         /* OUT: SQLite db handle */
    int flags,              /* Flags */
    string zVfs        /* Name of VFS module to use */
    )
    {
      return openDatabase( filename, ref ppDb, flags, zVfs );
    }

#if !SQLITE_OMIT_UTF16

/*
** Open a new database handle.
*/
int sqlite3_open16(
const void *zFilename,
sqlite3 **ppDb
){
char const *zFilename8;   /* zFilename encoded in UTF-8 instead of UTF-16 */
sqlite3_value pVal;
int rc;

Debug.Assert(zFilename );
Debug.Assert(ppDb );
*ppDb = 0;
#if !SQLITE_OMIT_AUTOINIT
rc = sqlite3_initialize();
if( rc !=0) return rc;
#endif
pVal = sqlite3ValueNew(0);
sqlite3ValueSetStr(pVal, -1, zFilename, SQLITE_UTF16NATIVE, SQLITE_STATIC);
zFilename8 = sqlite3ValueText(pVal, SQLITE_UTF8);
if( zFilename8 ){
rc = openDatabase(zFilename8, ppDb,
SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE, 0);
Debug.Assert(*ppDb || rc==SQLITE_NOMEM );
if( rc==SQLITE_OK && !DbHasProperty(*ppDb, 0, DB_SchemaLoaded) ){
ENC(*ppDb) = SQLITE_UTF16NATIVE;
}
}else{
rc = SQLITE_NOMEM;
}
sqlite3ValueFree(pVal);

return sqlite3ApiExit(0, rc);
}
#endif // * SQLITE_OMIT_UTF16 */

    /*
** Register a new collation sequence with the database handle db.
*/
    static int sqlite3_create_collation(
    sqlite3 db,
    string zName,
    int enc,
    object pCtx,
    dxCompare xCompare
    )
    {
      int rc;
      sqlite3_mutex_enter( db.mutex );
      //Debug.Assert( 0 == db.mallocFailed );
      rc = createCollation( db, zName, enc, pCtx, xCompare, null );
      rc = sqlite3ApiExit( db, rc );
      sqlite3_mutex_leave( db.mutex );
      return rc;
    }

    /*
    ** Register a new collation sequence with the database handle db.
    */
    static int sqlite3_create_collation_v2(
    sqlite3 db,
    string zName,
    int enc,
    object pCtx,
    dxCompare xCompare, //int(*xCompare)(void*,int,const void*,int,const void*),
    dxDelCollSeq xDel  //void(*xDel)(void*)
    )
    {
      int rc;
      sqlite3_mutex_enter( db.mutex );
      //Debug.Assert( 0 == db.mallocFailed );
      rc = createCollation( db, zName, enc, pCtx, xCompare, xDel );
      rc = sqlite3ApiExit( db, rc );
      sqlite3_mutex_leave( db.mutex );
      return rc;
    }

#if !SQLITE_OMIT_UTF16
/*
** Register a new collation sequence with the database handle db.
*/
//int sqlite3_create_collation16(
//  sqlite3* db,
//  string zName,
//  int enc,
//  void* pCtx,
//  int(*xCompare)(void*,int,const void*,int,const void*)
//){
//  int rc = SQLITE_OK;
//  char *zName8;
//  sqlite3_mutex_enter(db.mutex);
//  Debug.Assert( 0==db.mallocFailed );
//  zName8 = sqlite3Utf16to8(db, zName, -1);
//  if( zName8 ){
//    rc = createCollation(db, zName8, enc, pCtx, xCompare, 0);
//    //sqlite3DbFree(db,ref zName8);
//  }
//  rc = sqlite3ApiExit(db, rc);
//  sqlite3_mutex_leave(db.mutex);
//  return rc;
//}
#endif // * SQLITE_OMIT_UTF16 */

    /*
** Register a collation sequence factory callback with the database handle
** db. Replace any previously installed collation sequence factory.
*/
    static int sqlite3_collation_needed(
    sqlite3 db,
    object pCollNeededArg,
    dxCollNeeded xCollNeeded
    )
    {
      sqlite3_mutex_enter( db.mutex );
      db.xCollNeeded = xCollNeeded;
      db.xCollNeeded16 = null;
      db.pCollNeededArg = pCollNeededArg;
      sqlite3_mutex_leave( db.mutex );
      return SQLITE_OK;
    }

#if !SQLITE_OMIT_UTF16
/*
** Register a collation sequence factory callback with the database handle
** db. Replace any previously installed collation sequence factory.
*/
//int sqlite3_collation_needed16(
//  sqlite3 db,
//  void pCollNeededArg,
//  void(*xCollNeeded16)(void*,sqlite3*,int eTextRep,const void*)
//){
//  sqlite3_mutex_enter(db.mutex);
//  db.xCollNeeded = 0;
//  db.xCollNeeded16 = xCollNeeded16;
//  db.pCollNeededArg = pCollNeededArg;
//  sqlite3_mutex_leave(db.mutex);
//  return SQLITE_OK;
//}
#endif // * SQLITE_OMIT_UTF16 */

#if !SQLITE_OMIT_GLOBALRECOVER
#if !SQLITE_OMIT_DEPRECATED
    /*
** This function is now an anachronism. It used to be used to recover from a
** malloc() failure, but SQLite now does this automatically.
*/
    static int sqlite3_global_recover()
    {
      return SQLITE_OK;
    }
#endif
#endif

    /*
** Test to see whether or not the database connection is in autocommit
** mode.  Return TRUE if it is and FALSE if not.  Autocommit mode is on
** by default.  Autocommit is disabled by a BEGIN statement and reenabled
** by the next COMMIT or ROLLBACK.
**
******* THIS IS AN EXPERIMENTAL API AND IS SUBJECT TO CHANGE ******
*/
    static u8 sqlite3_get_autocommit( sqlite3 db )
    {
      return db.autoCommit;
    }

#if  SQLITE_DEBUG
    /*
** The following routine is subtituted for constant SQLITE_CORRUPT in
** debugging builds.  This provides a way to set a breakpoint for when
** corruption is first detected.
*/
    static int sqlite3Corrupt()
    {
      return SQLITE_CORRUPT;
    }
#endif

#if !SQLITE_OMIT_DEPRECATED
    /*
** This is a convenience routine that makes sure that all thread-specific
** data for this thread has been deallocated.
**
** SQLite no longer uses thread-specific data so this routine is now a
** no-op.  It is retained for historical compatibility.
*/
    void sqlite3_thread_cleanup()
    {
    }
#endif
    /*
** Return meta information about a specific column of a database table.
** See comment in sqlite3.h (sqlite.h.in) for details.
*/
#if SQLITE_ENABLE_COLUMN_METADATA

int sqlite3_table_column_metadata(
sqlite3 db,            /* Connection handle */
string zDbName,        /* Database name or NULL */
string zTableName,     /* Table name */
string zColumnName,    /* Column name */
ref byte[] pzDataType, /* OUTPUT: Declared data type */
ref byte[] pzCollSeq,  /* OUTPUT: Collation sequence name */
ref int pNotNull,      /* OUTPUT: True if NOT NULL constraint exists */
ref int pPrimaryKey,   /* OUTPUT: True if column part of PK */
ref int pAutoinc       /* OUTPUT: True if column is auto-increment */
){
int rc;
string zErrMsg = "";
Table pTab = null;
Column pCol = null;
int iCol;

char const *zDataType = 0;
char const *zCollSeq = 0;
int notnull = 0;
int primarykey = 0;
int autoinc = 0;

/* Ensure the database schema has been loaded */
sqlite3_mutex_enter(db.mutex);
(void)sqlite3SafetyOn(db);
sqlite3BtreeEnterAll(db);
rc = sqlite3Init(db, zErrMsg);
if( SQLITE_OK!=rc ){
goto error_out;
}

/* Locate the table in question */
pTab = sqlite3FindTable(db, zTableName, zDbName);
if( null==pTab || pTab.pSelect ){
pTab = 0;
goto error_out;
}

/* Find the column for which info is requested */
if( sqlite3IsRowid(zColumnName) ){
iCol = pTab.iPKey;
if( iCol>=0 ){
pCol = pTab.aCol[iCol];
}
}else{
for(iCol=0; iCol<pTab.nCol; iCol++){
pCol = pTab.aCol[iCol];
if( 0==sqlite3StrICmp(pCol.zName, zColumnName) ){
break;
}
}
if( iCol==pTab.nCol ){
pTab = 0;
goto error_out;
}
}

/* The following block stores the meta information that will be returned
** to the caller in local variables zDataType, zCollSeq, notnull, primarykey
** and autoinc. At this point there are two possibilities:
**
**     1. The specified column name was rowid", "oid" or "_rowid_"
**        and there is no explicitly declared IPK column.
**
**     2. The table is not a view and the column name identified an
**        explicitly declared column. Copy meta information from pCol.
*/
if( pCol ){
zDataType = pCol.zType;
zCollSeq = pCol.zColl;
notnull = pCol->notNull!=0;
primarykey  = pCol->isPrimKey!=0;
autoinc = pTab.iPKey==iCol && (pTab.tabFlags & TF_Autoincrement)!=0;
}else{
zDataType = "INTEGER";
primarykey = 1;
}
if( !zCollSeq ){
zCollSeq = "BINARY";
}

error_out:
sqlite3BtreeLeaveAll(db);
(void)sqlite3SafetyOff(db);

/* Whether the function call succeeded or failed, set the output parameters
** to whatever their local counterparts contain. If an error did occur,
** this has the effect of zeroing all output parameters.
*/
if( pzDataType ) pzDataType = zDataType;
if( pzCollSeq ) pzCollSeq = zCollSeq;
if( pNotNull ) pNotNull = notnull;
if( pPrimaryKey ) pPrimaryKey = primarykey;
if( pAutoinc ) pAutoinc = autoinc;

if( SQLITE_OK==rc && !pTab ){
//sqlite3DbFree(db, zErrMsg);
zErrMsg = sqlite3MPrintf(db, "no such table column: %s.%s", zTableName,
zColumnName);
rc = SQLITE_ERROR;
}
sqlite3Error(db, rc, (zErrMsg?"%s":0), zErrMsg);
//sqlite3DbFree(db, zErrMsg);
rc = sqlite3ApiExit(db, rc);
sqlite3_mutex_leave(db.mutex);
return rc;
}
#endif

    /*
** Sleep for a little while.  Return the amount of time slept.
*/
    public static int sqlite3_sleep( int ms )
    {
      sqlite3_vfs pVfs;
      int rc;
      pVfs = sqlite3_vfs_find( null );
      if ( pVfs == null ) return 0;

      /* This function works in milliseconds, but the underlying OsSleep()
      ** API uses microseconds. Hence the 1000's.
      */
      rc = ( sqlite3OsSleep( pVfs, 1000 * ms ) / 1000 );
      return rc;
    }

    /*
    ** Enable or disable the extended result codes.
    */
    static int sqlite3_extended_result_codes( sqlite3 db, bool onoff )
    {
      sqlite3_mutex_enter( db.mutex );
      db.errMask = (int)( onoff ? 0xffffffff : 0xff );
      sqlite3_mutex_leave( db.mutex );
      return SQLITE_OK;
    }

    /*
    ** Invoke the xFileControl method on a particular database.
    */
    static int sqlite3_file_control( sqlite3 db, string zDbName, int op, ref int pArg )
    {
      int rc = SQLITE_ERROR;
      int iDb;
      sqlite3_mutex_enter( db.mutex );
      if ( zDbName == null )
      {
        iDb = 0;
      }
      else
      {
        for ( iDb = 0 ; iDb < db.nDb ; iDb++ )
        {
          if ( db.aDb[iDb].zName == zDbName ) break;
        }
      }
      if ( iDb < db.nDb )
      {
        Btree pBtree = db.aDb[iDb].pBt;
        if ( pBtree != null )
        {
          Pager pPager;
          sqlite3_file fd;
          sqlite3BtreeEnter( pBtree );
          pPager = sqlite3BtreePager( pBtree );
          Debug.Assert( pPager != null );
          fd = sqlite3PagerFile( pPager );
          Debug.Assert( fd != null );
          if ( fd.pMethods != null )
          {
            rc = sqlite3OsFileControl( fd, (u32)op, ref pArg );
          }
          sqlite3BtreeLeave( pBtree );
        }
      }
      sqlite3_mutex_leave( db.mutex );
      return rc;
    }

    /*
    ** Interface to the testing logic.
    */
    static int sqlite3_test_control( int op, params object[] ap )
    {
      int rc = 0;
#if !SQLITE_OMIT_BUILTIN_TEST
      //  va_list ap;
      va_start( ap, "op" );
      switch ( op )
      {

        /*
        ** Save the current state of the PRNG.
        */
        case SQLITE_TESTCTRL_PRNG_SAVE:
          {
            sqlite3PrngSaveState();
            break;
          }

        /*
        ** Restore the state of the PRNG to the last state saved using
        ** PRNG_SAVE.  If PRNG_SAVE has never before been called, then
        ** this verb acts like PRNG_RESET.
        */
        case SQLITE_TESTCTRL_PRNG_RESTORE:
          {
            sqlite3PrngRestoreState();
            break;
          }

        /*
        ** Reset the PRNG back to its uninitialized state.  The next call
        ** to sqlite3_randomness() will reseed the PRNG using a single call
        ** to the xRandomness method of the default VFS.
        */
        case SQLITE_TESTCTRL_PRNG_RESET:
          {
            sqlite3PrngResetState();
            break;
          }

        /*
        **  sqlite3_test_control(BITVEC_TEST, size, program)
        **
        ** Run a test against a Bitvec object of size.  The program argument
        ** is an array of integers that defines the test.  Return -1 on a
        ** memory allocation error, 0 on success, or non-zero for an error.
        ** See the sqlite3BitvecBuiltinTest() for additional information.
        */
        case SQLITE_TESTCTRL_BITVEC_TEST:
          {
            int sz = (int)va_arg( ap, "int" );
            int[] aProg = (int[])va_arg( ap, "int[]" );
            rc = sqlite3BitvecBuiltinTest( (u32)sz, aProg );
            break;
          }

        /*
        **  sqlite3_test_control(BENIGN_MALLOC_HOOKS, xBegin, xEnd)
        **
        ** Register hooks to call to indicate which malloc() failures
        ** are benign.
        */
        case SQLITE_TESTCTRL_BENIGN_MALLOC_HOOKS:
          {
            //typedef void (*void_function)(void);
            void_function xBenignBegin;
            void_function xBenignEnd;
            xBenignBegin = (void_function)va_arg( ap, "void_function" );
            xBenignEnd = (void_function)va_arg( ap, "void_function" );
            sqlite3BenignMallocHooks( xBenignBegin, xBenignEnd );
            break;
          }
        /*
        **  sqlite3_test_control(SQLITE_TESTCTRL_PENDING_BYTE, unsigned int X)
        **
        ** Set the PENDING byte to the value in the argument, if X>0.
        ** Make no changes if X==0.  Return the value of the pending byte
        ** as it existing before this routine was called.
        **
        ** IMPORTANT:  Changing the PENDING byte from 0x40000000 results in
        ** an incompatible database file format.  Changing the PENDING byte
        ** while any database connection is open results in undefined and
        ** dileterious behavior.
        */
        case SQLITE_TESTCTRL_PENDING_BYTE:
          {
            u32 newVal = (u32)va_arg( ap, "u32" );
            rc = sqlite3PendingByte;
            if ( newVal != 0 )
            {
              if ( sqlite3PendingByte != newVal )
                sqlite3PendingByte = (int)newVal;
#if DEBUG && !NO_TCL
              TCLsqlite3PendingByte.iValue = sqlite3PendingByte;
#endif
              PENDING_BYTE = sqlite3PendingByte;
            }
            break;
          }

        /*
        **  sqlite3_test_control(SQLITE_TESTCTRL_ASSERT, int X)
        **
        ** This action provides a run-time test to see whether or not
        ** assert() was enabled at compile-time.  If X is true and assert()
        ** is enabled, then the return value is true.  If X is true and
        ** assert() is disabled, then the return value is zero.  If X is
        ** false and assert() is enabled, then the assertion fires and the
        ** process aborts.  If X is false and assert() is disabled, then the
        ** return value is zero.
        */
        case SQLITE_TESTCTRL_ASSERT:
          {
            int x = 0;
            Debug.Assert( ( x = (int)va_arg( ap, "int" ) ) != 0 );
            rc = x;
            break;
          }


        /*
        **  sqlite3_test_control(SQLITE_TESTCTRL_ALWAYS, int X)
        **
        ** This action provides a run-time test to see how the ALWAYS and
        ** NEVER macros were defined at compile-time.
        **
        ** The return value is ALWAYS(X).
        **
        ** The recommended test is X==2.  If the return value is 2, that means
        ** ALWAYS() and NEVER() are both no-op pass-through macros, which is the
        ** default setting.  If the return value is 1, then ALWAYS() is either
        ** hard-coded to true or else it asserts if its argument is false.
        ** The first behavior (hard-coded to true) is the case if
        ** SQLITE_TESTCTRL_ASSERT shows that assert() is disabled and the second
        ** behavior (assert if the argument to ALWAYS() is false) is the case if
        ** SQLITE_TESTCTRL_ASSERT shows that assert() is enabled.
        **
        ** The run-time test procedure might look something like this:
        **
        **    if( sqlite3_test_control(SQLITE_TESTCTRL_ALWAYS, 2)==2 ){
        **      // ALWAYS() and NEVER() are no-op pass-through macros
        **    }else if( sqlite3_test_control(SQLITE_TESTCTRL_ASSERT, 1) ){
        **      // ALWAYS(x) asserts that x is true. NEVER(x) asserts x is false.
        **    }else{
        **      // ALWAYS(x) is a constant 1.  NEVER(x) is a constant 0.
        **    }
        */
        case SQLITE_TESTCTRL_ALWAYS:
          {
            int x = (int)va_arg( ap, "int" );
            rc = ALWAYS( x );
            break;
          }

        /*   sqlite3_test_control(SQLITE_TESTCTRL_RESERVE, sqlite3 *db, int N)
        **
        ** Set the nReserve size to N for the main database on the database
        ** connection db.
        */
        case SQLITE_TESTCTRL_RESERVE: {
          sqlite3 db = (sqlite3)va_arg(ap, "sqlite3");
          int x = (int)va_arg(ap,"int");
          sqlite3_mutex_enter(db.mutex);
          sqlite3BtreeSetPageSize(db.aDb[0].pBt, 0, x, 0);
          sqlite3_mutex_leave(db.mutex);
          break;
        }
      }
      va_end( ap );
#endif //* SQLITE_OMIT_BUILTIN_TEST */
      return rc;
    }
  }
}
