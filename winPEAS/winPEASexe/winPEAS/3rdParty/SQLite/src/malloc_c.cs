using System.Diagnostics;
using System.Text;

namespace CS_SQLite3
{
  using sqlite3_int64 = System.Int64;
  using sqlite3_u3264 = System.UInt64;

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
    **
    ** Memory allocation functions used throughout sqlite.
    **
    ** $Id: malloc.c,v 1.66 2009/07/17 11:44:07 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
    //#include <stdarg.h>

#if FALSE
    /*
    ** This routine runs when the memory allocator sees that the
    ** total memory allocation is about to exceed the soft heap
    ** limit.
    */
    static void softHeapLimitEnforcer(
    object NotUsed,
    sqlite3_int64 NotUsed2,
    int allocSize
    )
    {
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      sqlite3_release_memory( allocSize );
    }

    /*
    ** Set the soft heap-size limit for the library. Passing a zero or
    ** negative value indicates no limit.
    */
    static void sqlite3_soft_heap_limit( int n )
    {
      long iLimit;
      int overage;
      if ( n < 0 )
      {
        iLimit = 0;
      }
      else
      {
        iLimit = n;
      }
      sqlite3_initialize();
      if ( iLimit > 0 )
      {
        sqlite3MemoryAlarm( (dxalarmCallback)softHeapLimitEnforcer, 0, iLimit );
      }
      else
      {
        sqlite3MemoryAlarm( null, null, 0 );
      }
      overage = (int)( sqlite3_memory_used() - n );
      if ( overage > 0 )
      {
        sqlite3_release_memory( overage );
      }
    }

    /*
    ** Attempt to release up to n bytes of non-essential memory currently
    ** held by SQLite. An example of non-essential memory is memory used to
    ** cache database pages that are not currently in use.
    */
    static int sqlite3_release_memory( int n )
    {
#if  SQLITE_ENABLE_MEMORY_MANAGEMENT
int nRet = 0;
#if FALSE
nRet += sqlite3VdbeReleaseMemory(n);
#endif
nRet += sqlite3PcacheReleaseMemory(n-nRet);
return nRet;
#else
      UNUSED_PARAMETER( n );
      return SQLITE_OK;
#endif
    }

    /*
    ** State information local to the memory allocation subsystem.
    */
    public class Mem0Global
    {
      /* Number of free pages for scratch and page-cache memory */
      public int nScratchFree;
      public int nPageFree;

      public sqlite3_mutex mutex;         /* Mutex to serialize access */

      /*
      ** The alarm callback and its arguments.  The mem0.mutex lock will
      ** be held while the callback is running.  Recursive calls into
      ** the memory subsystem are allowed, but no new callbacks will be
      ** issued.
      */
      public sqlite3_int64 alarmThreshold;
      public dxalarmCallback alarmCallback; // (*alarmCallback)(void*, sqlite3_int64,int);
      public object alarmArg;

      /*
      ** Pointers to the end of  sqlite3GlobalConfig.pScratch and
      **  sqlite3GlobalConfig.pPage to a block of memory that records
      ** which pages are available.
      */
      public int[] aScratchFree;
      public int[] aPageFree;

      public Mem0Global() { }

      public Mem0Global( int nScratchFree, int nPageFree, sqlite3_mutex mutex, sqlite3_int64 alarmThreshold, dxalarmCallback alarmCallback, object alarmArg, int alarmBusy, int[] aScratchFree, int[] aPageFree )
      {
        this.nScratchFree = nScratchFree;
        this.nPageFree = nPageFree;
        this.mutex = mutex;
        this.alarmThreshold = alarmThreshold;
        this.alarmCallback = alarmCallback;
        this.alarmArg = alarmArg;
        this.alarmBusy = alarmBusy;
        this.aScratchFree = aScratchFree;
        this.aPageFree = aPageFree;
      }
    }
    static Mem0Global mem0 = new Mem0Global( 0, null, 0, null, null, 0, null, null );

    //#define mem0 GLOBAL(struct Mem0Global, mem0)


    /*
    ** Initialize the memory allocation subsystem.
    */
    static int sqlite3MallocInit()
    {
      if ( sqlite3GlobalConfig.m.xMalloc == null )
      {
        sqlite3MemSetDefault();
      }
      mem0 = new Mem0Global(); //memset(&mem0, 0, sizeof(mem0));
      if ( sqlite3GlobalConfig.bCoreMutex )
      {
        mem0.mutex = sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MEM );
      }
      if ( sqlite3GlobalConfig.pScratch != null && sqlite3GlobalConfig.szScratch >= 100
      && sqlite3GlobalConfig.nScratch >= 0 )
      {
        Debugger.Break(); // TODO --

        //  int i;
        //  sqlite3GlobalConfig.szScratch = ROUNDDOWN8(sqlite3GlobalConfig.szScratch-4);
        //  mem0.aScratchFree = (u32*)&((char*) sqlite3GlobalConfig.pScratch)
        //                [ sqlite3GlobalConfig.szScratch* sqlite3GlobalConfig.nScratch];
        //  for(i=0; i< sqlite3GlobalConfig.nScratch; i++){ mem0.aScratchFree[i] = i; }
        //  mem0.nScratchFree =  sqlite3GlobalConfig.nScratch;
      }
      else
      {
        sqlite3GlobalConfig.pScratch = null;
        sqlite3GlobalConfig.szScratch = 0;
      }
      if ( sqlite3GlobalConfig.pPage != null && sqlite3GlobalConfig.szPage >= 512
      && sqlite3GlobalConfig.nPage >= 1 )
      {
        int i;
        int overhead;
        int sz = ROUNDDOWN8( sqlite3GlobalConfig.szPage );
        int n = sqlite3GlobalConfig.nPage;
        overhead = ( 4 * n + sz - 1 ) / sz;
        sqlite3GlobalConfig.nPage -= overhead;
        mem0.aPageFree = new int[sqlite3GlobalConfig.szPage * sqlite3GlobalConfig.nPage];
        //  mem0.aPageFree = (u32*)&((char*) sqlite3GlobalConfig.pPage)
        //                [ sqlite3GlobalConfig.szPage* sqlite3GlobalConfig.nPage];
        for ( i = 0 ; i < sqlite3GlobalConfig.nPage ; i++ ) { mem0.aPageFree[i] = i; }
        mem0.nPageFree = sqlite3GlobalConfig.nPage;
      }
      else
      {
        sqlite3GlobalConfig.pPage = null;
        sqlite3GlobalConfig.szPage = 0;
      }
      return sqlite3GlobalConfig.m.xInit( sqlite3GlobalConfig.m.pAppData );
    }

    /*
    ** Deinitialize the memory allocation subsystem.
    */
    static void sqlite3MallocEnd()
    {
      if ( sqlite3GlobalConfig.m.xShutdown != null )
      {
        sqlite3GlobalConfig.m.xShutdown( sqlite3GlobalConfig.m.pAppData );
        mem0 = new Mem0Global();//memset(&mem0, 0, sizeof(mem0));
      }
    }
    /*
    ** Return the amount of memory currently checked out.
    */
    static sqlite3_int64 sqlite3_memory_used()
    {
      int n = 0, mx = 0;
      sqlite3_int64 res;
      sqlite3_status( SQLITE_STATUS_MEMORY_USED, ref n, ref mx, 0 );
      res = (sqlite3_int64)n;  /* Work around bug in Borland C. Ticket #3216 */
      return res;
    }

    /*
    ** Return the maximum amount of memory that has ever been
    ** checked out since either the beginning of this process
    ** or since the most recent reset.
    */
    static sqlite3_int64 sqlite3_memory_highwater( int resetFlag )
    {
      int n = 0, mx = 0;
      sqlite3_int64 res;
      sqlite3_status( SQLITE_STATUS_MEMORY_USED, ref n, ref mx, 0 );
      res = (sqlite3_int64)mx;  /* Work around bug in Borland C. Ticket #3216 */
      return res;
    }

    /*
    ** Change the alarm callback
    */
    static int sqlite3MemoryAlarm(
    dxalarmCallback xCallback, //void(*xCallback)(void pArg, sqlite3_int64 used,int N),
    object pArg,
    sqlite3_int64 iThreshold
    )
    {
      sqlite3_mutex_enter( mem0.mutex );
      mem0.alarmCallback = xCallback;
      mem0.alarmArg = pArg;
      mem0.alarmThreshold = iThreshold;
      sqlite3_mutex_leave( mem0.mutex );
      return SQLITE_OK;
    }

#if !SQLITE_OMIT_DEPRECATED
    /*
** Deprecated external interface.  Internal/core SQLite code
** should call sqlite3MemoryAlarm.
*/
    static int sqlite3_memory_alarm(
    dxalarmCallback xCallback, //void(*xCallback)(void *pArg, sqlite3_int64 used,int N),
    object pArg,
    sqlite3_int64 iThreshold
    )
    {
      return sqlite3MemoryAlarm( xCallback, pArg, iThreshold );
    }
#endif


    /*
** Trigger the alarm
*/
    static void sqlite3MallocAlarm( int nByte )
    {
      Debugger.Break(); // TODO --
      //dxCallback xCallback; //void (*xCallback)(void*,sqlite3_int64,int);
      //sqlite3_int64 nowUsed;
      //object pArg;
      //if( mem0.alarmCallback==0 ) return;
      //xCallback = mem0.alarmCallback;
      //nowUsed = sqlite3StatusValue(SQLITE_STATUS_MEMORY_USED);
      //pArg = mem0.alarmArg;
      //mem0.alarmCallback = null;
      //sqlite3_mutex_leave(mem0.mutex);
      //xCallback(pArg, nowUsed, nByte);
      //sqlite3_mutex_enter(mem0.mutex);
      //mem0.alarmCallback = xCallback;
      //mem0.alarmArg = pArg;
      }

    /*
    ** Do a memory allocation with statistics and alarms.  Assume the
    ** lock is already held.
    */
    static int mallocWithAlarm( int n, ref byte[] pp )
    {
      int nFull;
      byte[] p;
      Debug.Assert( sqlite3_mutex_held( mem0.mutex ) );
      nFull = sqlite3GlobalConfig.m.xRoundup( n );
      sqlite3StatusSet( SQLITE_STATUS_MALLOC_SIZE, n );
      if ( mem0.alarmCallback != null )
      {
        int nUsed = sqlite3StatusValue( SQLITE_STATUS_MEMORY_USED );
        if ( nUsed + nFull >= mem0.alarmThreshold )
        {
          sqlite3MallocAlarm( nFull );
        }
      }
      p = sqlite3GlobalConfig.m.xMalloc( nFull );
      if ( p == null && mem0.alarmCallback != null )
      {
        sqlite3MallocAlarm( nFull );
        p = sqlite3GlobalConfig.m.xMalloc( nFull );
      }
      if ( p != null )
      {
        nFull = sqlite3MallocSize( p );
        sqlite3StatusAdd( SQLITE_STATUS_MEMORY_USED, nFull );
      }
      pp = p;
      return nFull;
    }

    /*
    ** Allocate memory.  This routine is like sqlite3_malloc() except that it
    ** assumes the memory subsystem has already been initialized.
    */
    static byte[] sqlite3Malloc( int n )
    {
      byte[] p = null;
      if ( n <= 0 || n >= 0x7fffff00 )
      {
        /* A memory allocation of a number of bytes which is near the maximum
        ** signed integer value might cause an integer overflow inside of the
        ** xMalloc().  Hence we limit the maximum size to 0x7fffff00, giving
        ** 255 bytes of overhead.  SQLite itself will never use anything near
        ** this amount.  The only way to reach the limit is with sqlite3_malloc() */
        p = null;
      }
      else if ( sqlite3GlobalConfig.bMemstat )
      {
        sqlite3_mutex_enter( mem0.mutex );
        mallocWithAlarm( n, ref p );
        sqlite3_mutex_leave( mem0.mutex );
      }
      else
      {
        p = sqlite3GlobalConfig.m.xMalloc( n );
      }
      return p;
    }

    /*
    ** This version of the memory allocation is for use by the application.
    ** First make sure the memory subsystem is initialized, then do the
    ** allocation.
    */
    static byte[] sqlite3_malloc( int n )
    {
#if !SQLITE_OMIT_AUTOINIT
      if ( sqlite3_initialize() != 0 ) return null;
#endif
      return sqlite3Malloc( n );
    }

    /*
    ** Each thread may only have a single outstanding allocation from
    ** xScratchMalloc().  We verify this constraint in the single-threaded
    ** case by setting scratchAllocOut to 1 when an allocation
    ** is outstanding clearing it when the allocation is freed.
    */
#if !SQLITE_THREADSAFE && !NDEBUG
    static int scratchAllocOut = 0;
#endif


    /*
** Allocate memory that is to be used and released right away.
** This routine is similar to alloca() in that it is not intended
** for situations where the memory might be held long-term.  This
** routine is intended to get memory to old large transient data
** structures that would not normally fit on the stack of an
** embedded processor.
*/
    byte[] sqlite3ScratchMalloc( int n )
    {
      byte[] p = null;
      Debug.Assert( n > 0 );

#if !SQLITE_THREADSAFE && !NDEBUG
      /* Verify that no more than one scratch allocation per thread
** is outstanding at one time.  (This is only checked in the
** single-threaded case since checking in the multi-threaded case
** would be much more complicated.) */
      Debug.Assert( scratchAllocOut == 0 );
#endif

      if ( sqlite3GlobalConfig.szScratch < n )
      {
        goto scratch_overflow;
      }
      else
      {
        sqlite3_mutex_enter( mem0.mutex );
        if ( mem0.nScratchFree == 0 )
        {
          sqlite3_mutex_leave( mem0.mutex );
          goto scratch_overflow;
        }
        else
        {
          Debugger.Break(); // TODO --
          //int i;
          //i = mem0.aScratchFree[--mem0.nScratchFree];
          //i *=  sqlite3GlobalConfig.szScratch;
          //sqlite3StatusAdd(SQLITE_STATUS_SCRATCH_USED, 1);
          //sqlite3StatusSet(SQLITE_STATUS_SCRATCH_SIZE, n);
          //sqlite3_mutex_leave(mem0.mutex);
          //p = (void*)&((char*) sqlite3GlobalConfig.pScratch)[i];
          //assert(  (((u8*)p - (u8*)0) & 7)==0 );
        }
      }
#if !SQLITE_THREADSAFE && !NDEBUG
      scratchAllocOut = p != null ? 1 : 0;
#endif

      return p;

scratch_overflow:
      if ( sqlite3GlobalConfig.bMemstat )
      {
        sqlite3_mutex_enter( mem0.mutex );
        sqlite3StatusSet( SQLITE_STATUS_SCRATCH_SIZE, n );
        n = mallocWithAlarm( n, ref p );
        if ( p != null ) sqlite3StatusAdd( SQLITE_STATUS_SCRATCH_OVERFLOW, n );
        sqlite3_mutex_leave( mem0.mutex );
      }
      else
      {
        p = sqlite3GlobalConfig.m.xMalloc( n );
      }
#if !SQLITE_THREADSAFE && !NDEBUG
      scratchAllocOut = ( p != null ) ? 1 : 0;
#endif
      return p;
    }
    static void //sqlite3ScratchFree( ref byte[][] p ) { p = null; }
    static void //sqlite3ScratchFree( ref byte[] p )
    {
      if ( p != null )
      {

#if !SQLITE_THREADSAFE && !NDEBUG
        /* Verify that no more than one scratch allocation per thread
** is outstanding at one time.  (This is only checked in the
** single-threaded case since checking in the multi-threaded case
** would be much more complicated.) */
        Debug.Assert( scratchAllocOut == 1 );
        scratchAllocOut = 0;
#endif
        Debugger.Break(); // TODO --
        //if(  sqlite3GlobalConfig.pScratch==null
        //       || p< sqlite3GlobalConfig.pScratch
        //       || p>=(void*)mem0.aScratchFree ){
        //  if(  sqlite3GlobalConfig.bMemstat ){
        //    int iSize = sqlite3MallocSize(p);
        //    sqlite3_mutex_enter(mem0.mutex);
        //    sqlite3StatusAdd(SQLITE_STATUS_SCRATCH_OVERFLOW, -iSize);
        //    sqlite3StatusAdd(SQLITE_STATUS_MEMORY_USED, -iSize);
        //     sqlite3GlobalConfig.m.xFree(p);
        //    sqlite3_mutex_leave(mem0.mutex);
        //  }else{
        //     sqlite3GlobalConfig.m.xFree(p);
        //  }
        //}else{
        //  int i;
        //  i = (int)((u8*)p - (u8*)sqlite3GlobalConfig.pScratch);
        //  i /=  sqlite3GlobalConfig.szScratch;
        //  Debug.Assert(i>=0 && i< sqlite3GlobalConfig.nScratch );
        //  sqlite3_mutex_enter(mem0.mutex);
        //  Debug.Assert(mem0.nScratchFree< (u32)sqlite3GlobalConfig.nScratch );
        //  mem0.aScratchFree[mem0.nScratchFree++] = i;
        //  sqlite3StatusAdd(SQLITE_STATUS_SCRATCH_USED, -1);
        //  sqlite3_mutex_leave(mem0.mutex);
        //}
      }
    }

    /*
    ** TRUE if p is a lookaside memory allocation from db
    */
#if !SQLITE_OMIT_LOOKASIDE
static bool isLookaside( sqlite3 db, object p )
{
return db != null && p >= db.lookaside.pStart && p < db.lookaside.pEnd;
}
#else
    //#define isLookaside(A,B) 0
    static bool isLookaside( sqlite3 db, object p )
    {
      return false;
    }
#endif

    /*
** Return the size of a memory allocation previously obtained from
** sqlite3Malloc() or sqlite3_malloc().
*/
    static int sqlite3MallocSize( byte[] p )
    {
      return sqlite3GlobalConfig.m.xSize( p );
    }

    int sqlite3DbMallocSize( sqlite3 db, byte[] p )
    {
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
      if ( isLookaside( db, p ) )
      {
        return db.lookaside.sz;
      }
      else
      {
        return sqlite3GlobalConfig.m.xSize( p );
      }
    }

    /*
    ** Free memory previously obtained from sqlite3Malloc().
    */
    // -- overloads ---------------------------------------
    static void //sqlite3_free( ref string x )
    { x = null; }

    static void //sqlite3_free<T>( ref T x ) where T : class
    { x = null; }

    static void //sqlite3_free( ref byte[] p )
    {
      if ( p == null ) return;
      if ( sqlite3GlobalConfig.bMemstat )
      {
        sqlite3_mutex_enter( mem0.mutex );
        sqlite3StatusAdd( SQLITE_STATUS_MEMORY_USED, -sqlite3MallocSize( p ) );
        sqlite3GlobalConfig.m.xFree( ref  p );
        sqlite3_mutex_leave( mem0.mutex );
      }
      else
      {
        Debugger.Break(); // TODO --    sqlite3GlobalConfig.m.xFree(p);
      }
    }
    /*
    ** Free memory that might be associated with a particular database
    ** connection.
    */
    // -- overloads ---------------------------------------
    static void //sqlite3DbFree( sqlite3 db, ref string x )
    {
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
      x = null;
    }
    static void //sqlite3DbFree( sqlite3 db, ref byte[] x )
    {
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
      x = null;
    }
    static void //sqlite3DbFree( sqlite3 db, ref int[] x )
    {
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
      x = null;
    }
    static void //sqlite3DbFree( sqlite3 db, ref StringBuilder x )
    {
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
      x = null;
    }
    static void //sqlite3DbFree<T>( sqlite3 db, ref T p ) where T : class
    {
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
      p = null;
    }
    static void //sqlite3DbFree( sqlite3 db, object p )
    {
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
      if ( isLookaside( db, p ) )
      {
        LookasideSlot pBuf = (LookasideSlot)p;
        pBuf.pNext = db.lookaside.pFree;
        db.lookaside.pFree = pBuf;
        db.lookaside.nOut--;
      }
      else
      {
        //sqlite3_free( ref p );
      }
    }

    /*
    ** Change the size of an existing memory allocation
    */
    static byte[] sqlite3Realloc( byte[] pOld, int nBytes )
    {
      int nOld, nNew;
      byte[] pNew = null;
      if ( pOld == null )
      {
        return sqlite3Malloc( nBytes );
      }
      if ( nBytes <= 0 )
      {
        //sqlite3_free( ref  pOld );
        return null;
      }
      if ( nBytes >= 0x7fffff00 )
      {
        /* The 0x7ffff00 limit term is explained in comments on sqlite3Malloc() */
        return null;
      }
      nOld = sqlite3MallocSize( pOld );
      if ( sqlite3GlobalConfig.bMemstat )
      {
        sqlite3_mutex_enter( mem0.mutex );
        sqlite3StatusSet( SQLITE_STATUS_MALLOC_SIZE, nBytes );
        nNew = sqlite3GlobalConfig.m.xRoundup( nBytes );
        if ( nOld == nNew )
        {
          pNew = pOld;
        }
        else
        {
          if ( sqlite3StatusValue( SQLITE_STATUS_MEMORY_USED ) + nNew - nOld >=
          mem0.alarmThreshold )
          {
            sqlite3MallocAlarm( nNew - nOld );
          }
          Debugger.Break(); // TODO --
          //pNew =  sqlite3GlobalConfig.m.xRealloc(pOld, nNew);
          //if( pNew==0 && mem0.alarmCallback ){
          //  sqlite3MallocAlarm(nBytes);
          //  pNew =  sqlite3GlobalConfig.m.xRealloc(pOld, nNew);
          //}
          if ( pNew != null )
          {
            nNew = sqlite3MallocSize( pNew );
            sqlite3StatusAdd( SQLITE_STATUS_MEMORY_USED, nNew - nOld );
          }
        }
        sqlite3_mutex_leave( mem0.mutex );
      }
      else
      {
        Debugger.Break(); // TODO --pNew =  sqlite3GlobalConfig.m.xRealloc(ref pOld, nBytes);
      }
      return pNew;
    }

    /*
    ** The public interface to sqlite3Realloc.  Make sure that the memory
    ** subsystem is initialized prior to invoking sqliteRealloc.
    */
    static byte[] sqlite3_realloc( object pOld, int n )
    {
#if !SQLITE_OMIT_AUTOINIT
      if ( sqlite3_initialize() != 0 ) return null;
#endif
      return sqlite3Realloc( (byte[])pOld, n );
    }


    /*
    ** Allocate and zero memory.
    */
    static byte[] sqlite3MallocZero( int n )
    {
      byte[] p = sqlite3Malloc( n );
      if ( p != null )
      {
        //memset(p, 0, n);
      }
      return p;
    }

    /*
    ** Allocate and zero memory.  If the allocation fails, make
    ** the mallocFailed flag in the connection pointer.
    */
    static byte[] sqlite3DbMallocZero( sqlite3 db, int n )
    {
      byte[] p = sqlite3DbMallocRaw( db, n );
      if ( p != null )
      {
        //  memset(p, 0, n);
      }
      return p;
    }

    /*
    ** Allocate and zero memory.  If the allocation fails, make
    ** the mallocFailed flag in the connection pointer.
    **
    ** If db!=0 and db->mallocFailed is true (indicating a prior malloc
    ** failure on the same database connection) then always return 0.
    ** Hence for a particular database connection, once malloc starts
    ** failing, it fails consistently until mallocFailed is reset.
    ** This is an important assumption.  There are many places in the
    ** code that do things like this:
    **
    **         int *a = (int*)sqlite3DbMallocRaw(db, 100);
    **         int *b = (int*)sqlite3DbMallocRaw(db, 200);
    **         if( b ) a[10] = 9;
    **
    ** In other words, if a subsequent malloc (ex: "b") worked, it is assumed
    ** that all prior mallocs (ex: "a") worked too.
    */
    static byte[] sqlite3DbMallocRaw( sqlite3 db, int n )
    {
      byte[] p;
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
#if !SQLITE_OMIT_LOOKASIDE
if( db ){
LookasideSlot pBuf;
if( db.mallocFailed !=0{
return 0;
}
if( db.lookaside.bEnabled && n<=db.lookaside.sz
&& (pBuf = db.lookaside.pFree)!=0 ){
db.lookaside.pFree = pBuf.pNext;
db.lookaside.nOut++;
if( db.lookaside.nOut>db.lookaside.mxOut ){
db.lookaside.mxOut = db.lookaside.nOut;
}
return (void*)pBuf;
}
}
#else
      if ( db != null && db.mallocFailed != 0 )
      {
        return null;
      }
#endif
      p = sqlite3Malloc( n );
      if ( null == p && db != null )
      {
////        db.mallocFailed = 1;
      }
      return p;
    }

    /*
    ** Resize the block of memory pointed to by p to n bytes. If the
    ** resize fails, set the mallocFailed flag inthe connection object.
    */
    static object sqlite3DbRealloc( sqlite3 db, object p, int n )
    {
      return p;
      //  void pNew = 0;
      //assert( db!=0 );
      //assert( sqlite3_mutex_held(db->mutex) );
      //  if( db.mallocFailed==0 ){
      //    if( p==0 ){
      //      return sqlite3DbMallocRaw(db, n);
      //    }
      //    if( isLookaside(db, p) ){
      //      if( n<=db.lookaside.sz ){
      //        return p;
      //      }
      //      pNew = sqlite3DbMallocRaw(db, n);
      //      if( pNew ){
      //        memcpy(pNew, p, db.lookaside.sz);
      //        //sqlite3DbFree(db, p);
      //      }
      //    }else{
      //      pNew = sqlite3_realloc(p, n);
      //      if( null==pNew ){
      //////        db.mallocFailed = 1;
      //      }
      //    }
      //  }
      //  return pNew;
    }

    /*
    ** Attempt to reallocate p.  If the reallocation fails, then free p
    ** and set the mallocFailed flag in the database connection.
    */
    //static     void sqlite3DbReallocOrFree(sqlite3 db, object p, int n){
    //  object pNew;
    //  pNew = "";//sqlite3DbRealloc(db, p, n);
    //      if( pNew ==null){
    //        //sqlite3DbFree(db,ref  p);
    //      }
    //      return pNew;
    //    }

    /*
    ** Make a copy of a string in memory obtained from sqliteMalloc(). These
    ** functions call sqlite3MallocRaw() directly instead of sqliteMalloc(). This
    ** is because when memory debugging is turned on, these two functions are
    ** called via macros that record the current file and line number in the
    ** ThreadData structure.
    */
    //char *sqlite3DbStrDup(sqlite3 db, const char *z){
    //  char *zNew;
    //  size_t n;
    //  if( z==0 ){
    //    return 0;
    //  }
    //  n = sqlite3Strlen30(z) + 1;
    //  assert( (n&0x7fffffff)==n );
    //  zNew = sqlite3DbMallocRaw(db, (int)n);
    //  if( zNew ){
    //    memcpy(zNew, z, n);
    //  }
    //  return zNew;
    //}
    //char *sqlite3DbStrNDup(sqlite3 *db, const char *z, int n){
    //  char *zNew;
    //  if( z==0 ){
    //    return 0;
    //  }
    //  assert( (n&0x7fffffff)==n );
    //  zNew = sqlite3DbMallocRaw(db, n+1);
    //  if( zNew ){
    //    memcpy(zNew, z, n);
    //    zNew[n] = 0;
    //  }
    //  return zNew;
    //}

#endif
    /*
    ** Create a string from the zFromat argument and the va_list that follows.
    ** Store the string in memory obtained from sqliteMalloc() and make pz
    ** point to that string.
    */
    static void sqlite3SetString( ref byte[] pz, sqlite3 db, string zFormat, params string[] ap )
    {
      string sz = "";
      sqlite3SetString( ref sz, db, zFormat, ap );
      pz = Encoding.UTF8.GetBytes( sz );
    }
    static void sqlite3SetString( ref string pz, sqlite3 db, string zFormat, byte[] ap )
    { sqlite3SetString( ref pz, db, zFormat, Encoding.UTF8.GetString( ap ) ); }

    static void sqlite3SetString( ref string pz, sqlite3 db, string zFormat, params string[] ap )
    {
      //va_list ap;
      string z;

      va_start( ap, zFormat );
      z = sqlite3VMPrintf( db, zFormat, ap );
      va_end( ap );
      //sqlite3DbFree( db, ref pz );
      pz = z;
    }

    /*
    ** This function must be called before exiting any API function (i.e.
    ** returning control to the user) that has called sqlite3_malloc or
    ** sqlite3_realloc.
    **
    ** The returned value is normally a copy of the second argument to this
    ** function. However, if a malloc() failure has occurred since the previous
    ** invocation SQLITE_NOMEM is returned instead.
    **
    ** If the first argument, db, is not NULL and a malloc() error has occurred,
    ** then the connection error-code (the value returned by sqlite3_errcode())
    ** is set to SQLITE_NOMEM.
    */
    static int sqlite3ApiExit( int zero, int rc )
    {
      sqlite3 db = null;
      return sqlite3ApiExit( db, rc );
    }

    static int sqlite3ApiExit( sqlite3 db, int rc )
    {
      /* If the db handle is not NULL, then we must hold the connection handle
      ** mutex here. Otherwise the read (and possible write) of db.mallocFailed
      ** is unsafe, as is the call to sqlite3Error().
      */
      Debug.Assert( db == null || sqlite3_mutex_held( db.mutex ) );
      if ( /*db != null && db.mallocFailed != 0 || */ rc == SQLITE_IOERR_NOMEM )
      {
        sqlite3Error( db, SQLITE_NOMEM, "" );
        //db.mallocFailed = 0;
        rc = SQLITE_NOMEM;
      }
      return rc & ( db != null ? db.errMask : 0xff );
    }
  }
}
