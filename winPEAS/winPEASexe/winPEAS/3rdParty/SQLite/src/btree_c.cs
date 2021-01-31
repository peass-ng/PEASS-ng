using System;
using System.Diagnostics;
using System.Text;
using i64 = System.Int64;
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;
using sqlite3_int64 = System.Int64;
using Pgno = System.UInt32;
namespace winPEAS._3rdParty.SQLite.src
{
  using DbPage = CSSQLite.PgHdr;

  public partial class CSSQLite
  {
    /*
    ** 2004 April 6
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** $Id: btree.c,v 1.705 2009/08/10 03:57:58 shane Exp $
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    **
    ** This file implements a external (disk-based) database using BTrees.
    ** See the header comment on "btreeInt.h" for additional information.
    ** Including a description of file format and an overview of operation.
    */
    //#include "btreeInt.h"

    /*
    ** The header string that appears at the beginning of every
    ** SQLite database.
    */
    static string zMagicHeader = SQLITE_FILE_HEADER;

    /*
    ** Set this global variable to 1 to enable tracing using the TRACE
    ** macro.
    */
#if TRACE 
static bool sqlite3BtreeTrace=false;  /* True to enable tracing */
//# define TRACE(X)  if(sqlite3BtreeTrace){printf X;fflush(stdout);}
static void TRACE(string X, params object[] ap) { if (sqlite3BtreeTrace)  printf(X, ap); }
#else
    //# define TRACE(X)
    static void TRACE(string X, params object[] ap) { }
#endif



#if !SQLITE_OMIT_SHARED_CACHE
/*
** A list of BtShared objects that are eligible for participation
** in shared cache.  This variable has file scope during normal builds,
** but the test harness needs to access it so we make it global for
** test builds.
**
** Access to this variable is protected by SQLITE_MUTEX_STATIC_MASTER.
*/
#if SQLITE_TEST
BtShared *SQLITE_WSD sqlite3SharedCacheList = 0;
#else
static BtShared *SQLITE_WSD sqlite3SharedCacheList = 0;
#endif
#endif //* SQLITE_OMIT_SHARED_CACHE */

#if !SQLITE_OMIT_SHARED_CACHE
/*
** Enable or disable the shared pager and schema features.
**
** This routine has no effect on existing database connections.
** The shared cache setting effects only future calls to
** sqlite3_open(), sqlite3_open16(), or sqlite3_open_v2().
*/
int sqlite3_enable_shared_cache(int enable){
sqlite3GlobalConfig.sharedCacheEnabled = enable;
return SQLITE_OK;
}
#endif



#if SQLITE_OMIT_SHARED_CACHE
    /*
** The functions querySharedCacheTableLock(), setSharedCacheTableLock(),
** and clearAllSharedCacheTableLocks()
** manipulate entries in the BtShared.pLock linked list used to store
** shared-cache table level locks. If the library is compiled with the
** shared-cache feature disabled, then there is only ever one user
** of each BtShared structure and so this locking is not necessary.
** So define the lock related functions as no-ops.
*/
    //#define querySharedCacheTableLock(a,b,c) SQLITE_OK
    static int querySharedCacheTableLock(Btree p, Pgno iTab, u8 eLock) { return SQLITE_OK; }

    //#define setSharedCacheTableLock(a,b,c) SQLITE_OK
    //#define clearAllSharedCacheTableLocks(a)
    static void clearAllSharedCacheTableLocks(Btree a) { }
    //#define downgradeAllSharedCacheTableLocks(a)
    static void downgradeAllSharedCacheTableLocks(Btree a) { }
    //#define hasSharedCacheTableLock(a,b,c,d) 1
    static bool hasSharedCacheTableLock(Btree a, Pgno b, int c, int d) { return true; }
    //#define hasReadConflicts(a, b) 0
    static bool hasReadConflicts(Btree a, Pgno b) { return false; }
#endif

#if !SQLITE_OMIT_SHARED_CACHE

#if SQLITE_DEBUG
/*
** This function is only used as part of an Debug.Assert() statement. It checks
** that connection p holds the required locks to read or write to the
** b-tree with root page iRoot. If so, true is returned. Otherwise, false.
** For example, when writing to a table b-tree with root-page iRoot via
** Btree connection pBtree:
**
**    Debug.Assert( hasSharedCacheTableLock(pBtree, iRoot, 0, WRITE_LOCK) );
**
** When writing to an index b-tree that resides in a sharable database, the
** caller should have first obtained a lock specifying the root page of
** the corresponding table b-tree. This makes things a bit more complicated,
** as this module treats each b-tree as a separate structure. To determine
** the table b-tree corresponding to the index b-tree being written, this
** function has to search through the database schema.
**
** Instead of a lock on the b-tree rooted at page iRoot, the caller may
** hold a write-lock on the schema table (root page 1). This is also
** acceptable.
*/
static int hasSharedCacheTableLock(
Btree pBtree,         /* Handle that must hold lock */
Pgno iRoot,            /* Root page of b-tree */
int isIndex,           /* True if iRoot is the root of an index b-tree */
int eLockType          /* Required lock type (READ_LOCK or WRITE_LOCK) */
){
Schema pSchema = (Schema *)pBtree.pBt.pSchema;
Pgno iTab = 0;
BtLock pLock;

/* If this b-tree database is not shareable, or if the client is reading
** and has the read-uncommitted flag set, then no lock is required.
** In these cases return true immediately.  If the client is reading
** or writing an index b-tree, but the schema is not loaded, then return
** true also. In this case the lock is required, but it is too difficult
** to check if the client actually holds it. This doesn't happen very
** often.  */
if( (pBtree.sharable==null)
|| (eLockType==READ_LOCK && (pBtree.db.flags & SQLITE_ReadUncommitted))
|| (isIndex && (!pSchema || (pSchema.flags&DB_SchemaLoaded)==null ))
){
return 1;
}

/* Figure out the root-page that the lock should be held on. For table
** b-trees, this is just the root page of the b-tree being read or
** written. For index b-trees, it is the root page of the associated
** table.  */
if( isIndex ){
HashElem p;
for(p=sqliteHashFirst(pSchema.idxHash); p!=null; p=sqliteHashNext(p)){
Index pIdx = (Index *)sqliteHashData(p);
if( pIdx.tnum==(int)iRoot ){
iTab = pIdx.pTable.tnum;
}
}
}else{
iTab = iRoot;
}

/* Search for the required lock. Either a write-lock on root-page iTab, a
** write-lock on the schema table, or (if the client is reading) a
** read-lock on iTab will suffice. Return 1 if any of these are found.  */
for(pLock=pBtree.pBt.pLock; pLock; pLock=pLock.pNext){
if( pLock.pBtree==pBtree
&& (pLock.iTable==iTab || (pLock.eLock==WRITE_LOCK && pLock.iTable==1))
&& pLock.eLock>=eLockType
){
return 1;
}
}

/* Failed to find the required lock. */
return 0;
}

/*
** This function is also used as part of Debug.Assert() statements only. It
** returns true if there exist one or more cursors open on the table
** with root page iRoot that do not belong to either connection pBtree
** or some other connection that has the read-uncommitted flag set.
**
** For example, before writing to page iRoot:
**
**    Debug.Assert( !hasReadConflicts(pBtree, iRoot) );
*/
static int hasReadConflicts(Btree pBtree, Pgno iRoot){
BtCursor p;
for(p=pBtree.pBt.pCursor; p!=null; p=p.pNext){
if( p.pgnoRoot==iRoot
&& p.pBtree!=pBtree
&& 0==(p.pBtree.db.flags & SQLITE_ReadUncommitted)
){
return 1;
}
}
return 0;
}
#endif    //* #if SQLITE_DEBUG */

/*
** Query to see if btree handle p may obtain a lock of type eLock
** (READ_LOCK or WRITE_LOCK) on the table with root-page iTab. Return
** SQLITE_OK if the lock may be obtained (by calling
** setSharedCacheTableLock()), or SQLITE_LOCKED if not.
*/
static int querySharedCacheTableLock(Btree p, Pgno iTab, u8 eLock){
BtShared pBt = p.pBt;
BtLock pIter;

Debug.Assert( sqlite3BtreeHoldsMutex(p) );
Debug.Assert( eLock==READ_LOCK || eLock==WRITE_LOCK );
Debug.Assert( p.db!=null );
Debug.Assert( !(p.db.flags&SQLITE_ReadUncommitted)||eLock==WRITE_LOCK||iTab==1 );

/* If requesting a write-lock, then the Btree must have an open write
** transaction on this file. And, obviously, for this to be so there
** must be an open write transaction on the file itself.
*/
Debug.Assert( eLock==READ_LOCK || (p==pBt.pWriter && p.inTrans==TRANS_WRITE) );
Debug.Assert( eLock==READ_LOCK || pBt.inTransaction==TRANS_WRITE );

/* This is a no-op if the shared-cache is not enabled */
if( !p.sharable ){
return SQLITE_OK;
}

/* If some other connection is holding an exclusive lock, the
** requested lock may not be obtained.
*/
if( pBt.pWriter!=p && pBt.isExclusive ){
sqlite3ConnectionBlocked(p.db, pBt.pWriter.db);
return SQLITE_LOCKED_SHAREDCACHE;
}

for(pIter=pBt.pLock; pIter; pIter=pIter.pNext){
/* The condition (pIter.eLock!=eLock) in the following if(...)
** statement is a simplification of:
**
**   (eLock==WRITE_LOCK || pIter.eLock==WRITE_LOCK)
**
** since we know that if eLock==WRITE_LOCK, then no other connection
** may hold a WRITE_LOCK on any table in this file (since there can
** only be a single writer).
*/
Debug.Assert( pIter.eLock==READ_LOCK || pIter.eLock==WRITE_LOCK );
Debug.Assert( eLock==READ_LOCK || pIter.pBtree==p || pIter.eLock==READ_LOCK);
if( pIter.pBtree!=p && pIter.iTable==iTab && pIter.eLock!=eLock ){
sqlite3ConnectionBlocked(p.db, pIter.pBtree.db);
if( eLock==WRITE_LOCK ){
Debug.Assert( p==pBt.pWriter );
pBt.isPending = 1;
}
return SQLITE_LOCKED_SHAREDCACHE;
}
}
return SQLITE_OK;
}
#endif //* !SQLITE_OMIT_SHARED_CACHE */

#if !SQLITE_OMIT_SHARED_CACHE
/*
** Add a lock on the table with root-page iTable to the shared-btree used
** by Btree handle p. Parameter eLock must be either READ_LOCK or
** WRITE_LOCK.
**
** This function assumes the following:
**
**   (a) The specified b-tree connection handle is connected to a sharable
**       b-tree database (one with the BtShared.sharable) flag set, and
**
**   (b) No other b-tree connection handle holds a lock that conflicts
**       with the requested lock (i.e. querySharedCacheTableLock() has
**       already been called and returned SQLITE_OK).
**
** SQLITE_OK is returned if the lock is added successfully. SQLITE_NOMEM
** is returned if a malloc attempt fails.
*/
static int setSharedCacheTableLock(Btree p, Pgno iTable, u8 eLock){
BtShared pBt = p.pBt;
BtLock pLock = 0;
BtLock pIter;

Debug.Assert( sqlite3BtreeHoldsMutex(p) );
Debug.Assert( eLock==READ_LOCK || eLock==WRITE_LOCK );
Debug.Assert( p.db!=null );

/* A connection with the read-uncommitted flag set will never try to
** obtain a read-lock using this function. The only read-lock obtained
** by a connection in read-uncommitted mode is on the sqlite_master
** table, and that lock is obtained in BtreeBeginTrans().  */
Debug.Assert( 0==(p.db.flags&SQLITE_ReadUncommitted) || eLock==WRITE_LOCK );

/* This function should only be called on a sharable b-tree after it
** has been determined that no other b-tree holds a conflicting lock.  */
Debug.Assert( p.sharable );
Debug.Assert( SQLITE_OK==querySharedCacheTableLock(p, iTable, eLock) );

/* First search the list for an existing lock on this table. */
for(pIter=pBt.pLock; pIter; pIter=pIter.pNext){
if( pIter.iTable==iTable && pIter.pBtree==p ){
pLock = pIter;
break;
}
}

/* If the above search did not find a BtLock struct associating Btree p
** with table iTable, allocate one and link it into the list.
*/
if( !pLock ){
pLock = (BtLock *)sqlite3MallocZero(sizeof(BtLock));
if( !pLock ){
return SQLITE_NOMEM;
}
pLock.iTable = iTable;
pLock.pBtree = p;
pLock.pNext = pBt.pLock;
pBt.pLock = pLock;
}

/* Set the BtLock.eLock variable to the maximum of the current lock
** and the requested lock. This means if a write-lock was already held
** and a read-lock requested, we don't incorrectly downgrade the lock.
*/
Debug.Assert( WRITE_LOCK>READ_LOCK );
if( eLock>pLock.eLock ){
pLock.eLock = eLock;
}

return SQLITE_OK;
}
#endif //* !SQLITE_OMIT_SHARED_CACHE */

#if !SQLITE_OMIT_SHARED_CACHE
/*
** Release all the table locks (locks obtained via calls to
** the setSharedCacheTableLock() procedure) held by Btree handle p.
**
** This function assumes that handle p has an open read or write
** transaction. If it does not, then the BtShared.isPending variable
** may be incorrectly cleared.
*/
static void clearAllSharedCacheTableLocks(Btree p){
BtShared pBt = p.pBt;
BtLock **ppIter = &pBt.pLock;

Debug.Assert( sqlite3BtreeHoldsMutex(p) );
Debug.Assert( p.sharable || 0==*ppIter );
Debug.Assert( p.inTrans>0 );

while( ppIter ){
BtLock pLock = ppIter;
Debug.Assert( pBt.isExclusive==null || pBt.pWriter==pLock.pBtree );
Debug.Assert( pLock.pBtree.inTrans>=pLock.eLock );
if( pLock.pBtree==p ){
ppIter = pLock.pNext;
Debug.Assert( pLock.iTable!=1 || pLock==&p.lock );
if( pLock.iTable!=1 ){
pLock=null;//sqlite3_free(ref pLock);
}
}else{
ppIter = &pLock.pNext;
}
}

Debug.Assert( pBt.isPending==null || pBt.pWriter );
if( pBt.pWriter==p ){
pBt.pWriter = 0;
pBt.isExclusive = 0;
pBt.isPending = 0;
}else if( pBt.nTransaction==2 ){
/* This function is called when connection p is concluding its
** transaction. If there currently exists a writer, and p is not
** that writer, then the number of locks held by connections other
** than the writer must be about to drop to zero. In this case
** set the isPending flag to 0.
**
** If there is not currently a writer, then BtShared.isPending must
** be zero already. So this next line is harmless in that case.
*/
pBt.isPending = 0;
}
}

/*
** This function changes all write-locks held by connection p to read-locks.
*/
static void downgradeAllSharedCacheTableLocks(Btree p){
BtShared pBt = p.pBt;
if( pBt.pWriter==p ){
BtLock pLock;
pBt.pWriter = 0;
pBt.isExclusive = 0;
pBt.isPending = 0;
for(pLock=pBt.pLock; pLock; pLock=pLock.pNext){
Debug.Assert( pLock.eLock==READ_LOCK || pLock.pBtree==p );
pLock.eLock = READ_LOCK;
}
}
}

#endif //* SQLITE_OMIT_SHARED_CACHE */

    //static void releasePage(MemPage pPage);  /* Forward reference */

    /*
    ** Verify that the cursor holds a mutex on the BtShared
    */
#if !NDEBUG
    static bool cursorHoldsMutex(BtCursor p)
    {
      return sqlite3_mutex_held(p.pBt.mutex);
    }
#else
static bool cursorHoldsMutex(BtCursor p) { return true; }
#endif


#if !SQLITE_OMIT_INCRBLOB
/*
** Invalidate the overflow page-list cache for cursor pCur, if any.
*/
static void invalidateOverflowCache(BtCursor pCur){
Debug.Assert( cursorHoldsMutex(pCur) );
//sqlite3_free(ref pCur.aOverflow);
pCur.aOverflow = null;
}

/*
** Invalidate the overflow page-list cache for all cursors opened
** on the shared btree structure pBt.
*/
static void invalidateAllOverflowCache(BtShared pBt){
BtCursor p;
Debug.Assert( sqlite3_mutex_held(pBt.mutex) );
for(p=pBt.pCursor; p!=null; p=p.pNext){
invalidateOverflowCache(p);
}
}

/*
** This function is called before modifying the contents of a table
** b-tree to invalidate any incrblob cursors that are open on the
** row or one of the rows being modified.
**
** If argument isClearTable is true, then the entire contents of the
** table is about to be deleted. In this case invalidate all incrblob
** cursors open on any row within the table with root-page pgnoRoot.
**
** Otherwise, if argument isClearTable is false, then the row with
** rowid iRow is being replaced or deleted. In this case invalidate
** only those incrblob cursors open on this specific row.
*/
static void invalidateIncrblobCursors(
Btree pBtree,          /* The database file to check */
i64 iRow,               /* The rowid that might be changing */
int isClearTable        /* True if all rows are being deleted */
){
BtCursor p;
BtShared pBt = pBtree.pBt;
Debug.Assert( sqlite3BtreeHoldsMutex(pBtree) );
for(p=pBt.pCursor; p!=null; p=p.pNext){
if( p.isIncrblobHandle && (isClearTable || p.info.nKey==iRow) ){
p.eState = CURSOR_INVALID;
}
}
}

#else
    //#define invalidateOverflowCache(x)
    static void invalidateOverflowCache(BtCursor pCur) { }
    //#define invalidateAllOverflowCache(x)
    static void invalidateAllOverflowCache(BtShared pBt) { }
    //#define invalidateIncrblobCursors(x,y,z)
    static void invalidateIncrblobCursors(Btree x, i64 y, int z) { }

#endif

    /*
** Set bit pgno of the BtShared.pHasContent bitvec. This is called
** when a page that previously contained data becomes a free-list leaf
** page.
**
** The BtShared.pHasContent bitvec exists to work around an obscure
** bug caused by the interaction of two useful IO optimizations surrounding
** free-list leaf pages:
**
**   1) When all data is deleted from a page and the page becomes
**      a free-list leaf page, the page is not written to the database
**      (as free-list leaf pages contain no meaningful data). Sometimes
**      such a page is not even journalled (as it will not be modified,
**      why bother journalling it?).
**
**   2) When a free-list leaf page is reused, its content is not read
**      from the database or written to the journal file (why should it
**      be, if it is not at all meaningful?).
**
** By themselves, these optimizations work fine and provide a handy
** performance boost to bulk delete or insert operations. However, if
** a page is moved to the free-list and then reused within the same
** transaction, a problem comes up. If the page is not journalled when
** it is moved to the free-list and it is also not journalled when it
** is extracted from the free-list and reused, then the original data
** may be lost. In the event of a rollback, it may not be possible
** to restore the database to its original configuration.
**
** The solution is the BtShared.pHasContent bitvec. Whenever a page is
** moved to become a free-list leaf page, the corresponding bit is
** set in the bitvec. Whenever a leaf page is extracted from the free-list,
** optimization 2 above is ommitted if the corresponding bit is already
** set in BtShared.pHasContent. The contents of the bitvec are cleared
** at the end of every transaction.
*/
    static int btreeSetHasContent(BtShared pBt, Pgno pgno)
    {
      int rc = SQLITE_OK;
      if (null == pBt.pHasContent)
      {
        int nPage = 100;
        sqlite3PagerPagecount(pBt.pPager, ref nPage);
        /* If sqlite3PagerPagecount() fails there is no harm because the
        ** nPage variable is unchanged from its default value of 100 */
        pBt.pHasContent = sqlite3BitvecCreate((u32)nPage);
        if (null == pBt.pHasContent)
        {
          rc = SQLITE_NOMEM;
        }
      }
      if (rc == SQLITE_OK && pgno <= sqlite3BitvecSize(pBt.pHasContent))
      {
        rc = sqlite3BitvecSet(pBt.pHasContent, pgno);
      }
      return rc;
    }

    /*
    ** Query the BtShared.pHasContent vector.
    **
    ** This function is called when a free-list leaf page is removed from the
    ** free-list for reuse. It returns false if it is safe to retrieve the
    ** page from the pager layer with the 'no-content' flag set. True otherwise.
    */
    static bool btreeGetHasContent(BtShared pBt, Pgno pgno)
    {
      Bitvec p = pBt.pHasContent;
      return (p != null && (pgno > sqlite3BitvecSize(p) || sqlite3BitvecTest(p, pgno) != 0));
    }

    /*
    ** Clear (destroy) the BtShared.pHasContent bitvec. This should be
    ** invoked at the conclusion of each write-transaction.
    */
    static void btreeClearHasContent(BtShared pBt)
    {
      sqlite3BitvecDestroy(ref pBt.pHasContent);
      pBt.pHasContent = null;
    }

    /*
    ** Save the current cursor position in the variables BtCursor.nKey
    ** and BtCursor.pKey. The cursor's state is set to CURSOR_REQUIRESEEK.
    **
    ** The caller must ensure that the cursor is valid (has eState==CURSOR_VALID)
    ** prior to calling this routine.
    */
    static int saveCursorPosition(BtCursor pCur)
    {
      int rc;

      Debug.Assert(CURSOR_VALID == pCur.eState);
      Debug.Assert(null == pCur.pKey);
      Debug.Assert(cursorHoldsMutex(pCur));

      rc = sqlite3BtreeKeySize(pCur, ref pCur.nKey);
      Debug.Assert(rc == SQLITE_OK);  /* KeySize() cannot fail */

      /* If this is an intKey table, then the above call to BtreeKeySize()
      ** stores the integer key in pCur.nKey. In this case this value is
      ** all that is required. Otherwise, if pCur is not open on an intKey
      ** table, then malloc space for and store the pCur.nKey bytes of key
      ** data.
      */
      if (0 == pCur.apPage[0].intKey)
      {
        byte[] pKey = new byte[pCur.nKey];//void pKey = sqlite3Malloc( (int)pCur.nKey );
        //if( pKey !=null){
        rc = sqlite3BtreeKey(pCur, 0, (u32)pCur.nKey, pKey);
        if (rc == SQLITE_OK)
        {
          pCur.pKey = pKey;
        }
        //else{
        //  sqlite3_free(ref pKey);
        //}
        //}else{
        //  rc = SQLITE_NOMEM;
        //}
      }
      Debug.Assert(0 == pCur.apPage[0].intKey || null == pCur.pKey);

      if (rc == SQLITE_OK)
      {
        int i;
        for (i = 0; i <= pCur.iPage; i++)
        {
          releasePage(pCur.apPage[i]);
          pCur.apPage[i] = null;
        }
        pCur.iPage = -1;
        pCur.eState = CURSOR_REQUIRESEEK;
      }

      invalidateOverflowCache(pCur);
      return rc;
    }

    /*
    ** Save the positions of all cursors except pExcept open on the table
    ** with root-page iRoot. Usually, this is called just before cursor
    ** pExcept is used to modify the table (BtreeDelete() or BtreeInsert()).
    */
    static int saveAllCursors(BtShared pBt, Pgno iRoot, BtCursor pExcept)
    {
      BtCursor p;
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      Debug.Assert(pExcept == null || pExcept.pBt == pBt);
      for (p = pBt.pCursor; p != null; p = p.pNext)
      {
        if (p != pExcept && (0 == iRoot || p.pgnoRoot == iRoot) &&
        p.eState == CURSOR_VALID)
        {
          int rc = saveCursorPosition(p);
          if (SQLITE_OK != rc)
          {
            return rc;
          }
        }
      }
      return SQLITE_OK;
    }

    /*
    ** Clear the current cursor position.
    */
    static void sqlite3BtreeClearCursor(BtCursor pCur)
    {
      Debug.Assert(cursorHoldsMutex(pCur));
      //sqlite3_free(ref pCur.pKey);
      pCur.pKey = null;
      pCur.eState = CURSOR_INVALID;
    }

    /*
    ** In this version of BtreeMoveto, pKey is a packed index record
    ** such as is generated by the OP_MakeRecord opcode.  Unpack the
    ** record and then call BtreeMovetoUnpacked() to do the work.
    */
    static int btreeMoveto(
    BtCursor pCur,     /* Cursor open on the btree to be searched */
    byte[] pKey,       /* Packed key if the btree is an index */
    i64 nKey,          /* Integer key for tables.  Size of pKey for indices */
    int bias,          /* Bias search to the high end */
    ref int pRes       /* Write search results here */
    )
    {
      int rc;                    /* Status code */
      UnpackedRecord pIdxKey;   /* Unpacked index key */
      UnpackedRecord aSpace = new UnpackedRecord();//char aSpace[150]; /* Temp space for pIdxKey - to avoid a malloc */

      if (pKey != null)
      {
        Debug.Assert(nKey == (i64)(int)nKey);
        pIdxKey = sqlite3VdbeRecordUnpack(pCur.pKeyInfo, (int)nKey, pKey,
        aSpace, 16);//sizeof( aSpace ) );
        if (pIdxKey == null) return SQLITE_NOMEM;
      }
      else
      {
        pIdxKey = null;
      }
      rc = sqlite3BtreeMovetoUnpacked(pCur, pIdxKey, nKey, bias != 0 ? 1 : 0, ref pRes);

      if (pKey != null)
      {
        sqlite3VdbeDeleteUnpackedRecord(pIdxKey);
      }
      return rc;
    }

    /*
    ** Restore the cursor to the position it was in (or as close to as possible)
    ** when saveCursorPosition() was called. Note that this call deletes the
    ** saved position info stored by saveCursorPosition(), so there can be
    ** at most one effective restoreCursorPosition() call after each
    ** saveCursorPosition().
    */
    static int btreeRestoreCursorPosition(BtCursor pCur)
    {
      int rc;
      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.eState >= CURSOR_REQUIRESEEK);
      if (pCur.eState == CURSOR_FAULT)
      {
        return pCur.skipNext;
      }
      pCur.eState = CURSOR_INVALID;
      rc = btreeMoveto(pCur, pCur.pKey, pCur.nKey, 0, ref pCur.skipNext);
      if (rc == SQLITE_OK)
      {
        //sqlite3_free(ref pCur.pKey);
        pCur.pKey = null;
        Debug.Assert(pCur.eState == CURSOR_VALID || pCur.eState == CURSOR_INVALID);
      }
      return rc;
    }

    //#define restoreCursorPosition(p) \
    //  (p.eState>=CURSOR_REQUIRESEEK ? \
    //         btreeRestoreCursorPosition(p) : \
    //         SQLITE_OK)
    static int restoreCursorPosition(BtCursor pCur)
    {
      if ( pCur.eState >= CURSOR_REQUIRESEEK )
        return btreeRestoreCursorPosition( pCur );
      else
        return SQLITE_OK;
    }

    /*
    ** Determine whether or not a cursor has moved from the position it
    ** was last placed at.  Cursors can move when the row they are pointing
    ** at is deleted out from under them.
    **
    ** This routine returns an error code if something goes wrong.  The
    ** integer pHasMoved is set to one if the cursor has moved and 0 if not.
    */
    static int sqlite3BtreeCursorHasMoved(BtCursor pCur, ref int pHasMoved)
    {
      int rc;

      rc = restoreCursorPosition(pCur);
      if (rc != 0)
      {
        pHasMoved = 1;
        return rc;
      }
      if (pCur.eState != CURSOR_VALID || pCur.skipNext != 0)
      {
        pHasMoved = 1;
      }
      else
      {
        pHasMoved = 0;
      }
      return SQLITE_OK;
    }

#if !SQLITE_OMIT_AUTOVACUUM
    /*
** Given a page number of a regular database page, return the page
** number for the pointer-map page that contains the entry for the
** input page number.
*/
    static Pgno ptrmapPageno(BtShared pBt, Pgno pgno)
    {
      int nPagesPerMapPage;
      Pgno iPtrMap, ret;
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      nPagesPerMapPage = (pBt.usableSize / 5) + 1;
      iPtrMap = (Pgno)((pgno - 2) / nPagesPerMapPage);
      ret = (Pgno)( iPtrMap * nPagesPerMapPage ) + 2;
      if (ret == PENDING_BYTE_PAGE(pBt))
      {
        ret++;
      }
      return ret;
    }

    /*
    ** Write an entry into the pointer map.
    **
    ** This routine updates the pointer map entry for page number 'key'
    ** so that it maps to type 'eType' and parent page number 'pgno'.
    **
    ** If pRC is initially non-zero (non-SQLITE_OK) then this routine is
    ** a no-op.  If an error occurs, the appropriate error code is written
    ** into pRC.
    */
    static void ptrmapPut(BtShared pBt, Pgno key, u8 eType, Pgno parent, ref int pRC)
    {
      DbPage pDbPage = new PgHdr(); /* The pointer map page */
      u8[] pPtrmap;                 /* The pointer map data */
      Pgno iPtrmap;                 /* The pointer map page number */
      int offset;                   /* Offset in pointer map page */
      int rc;                       /* Return code from subfunctions */

      if (pRC != 0) return;

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      /* The master-journal page number must never be used as a pointer map page */
      Debug.Assert(false == PTRMAP_ISPAGE(pBt, PENDING_BYTE_PAGE(pBt)));

      Debug.Assert(pBt.autoVacuum);
      if (key == 0)
      {
#if SQLITE_DEBUG || DEBUG
        pRC = SQLITE_CORRUPT_BKPT();
#else
pRC = SQLITE_CORRUPT_BKPT;
#endif
        return;
      }
      iPtrmap = PTRMAP_PAGENO(pBt, key);
      rc = sqlite3PagerGet(pBt.pPager, iPtrmap, ref pDbPage);
      if (rc != SQLITE_OK)
      {
        pRC = rc;
        return;
      }
      offset = (int)PTRMAP_PTROFFSET(iPtrmap, key);
      if (offset < 0)
      {
#if SQLITE_DEBUG || DEBUG
        pRC = SQLITE_CORRUPT_BKPT();
#else
pRC = SQLITE_CORRUPT_BKPT;
#endif
        goto ptrmap_exit;
      }
      pPtrmap = sqlite3PagerGetData(pDbPage);

      if (eType != pPtrmap[offset] || sqlite3Get4byte(pPtrmap, offset + 1) != parent)
      {
        TRACE("PTRMAP_UPDATE: %d->(%d,%d)\n", key, eType, parent);
        pRC = rc = sqlite3PagerWrite(pDbPage);
        if (rc == SQLITE_OK)
        {
          pPtrmap[offset] = eType;
          sqlite3Put4byte(pPtrmap, offset + 1, parent);
        }
      }

    ptrmap_exit:
      sqlite3PagerUnref(pDbPage);
    }

    /*
    ** Read an entry from the pointer map.
    **
    ** This routine retrieves the pointer map entry for page 'key', writing
    ** the type and parent page number to pEType and pPgno respectively.
    ** An error code is returned if something goes wrong, otherwise SQLITE_OK.
    */
    static int ptrmapGet(BtShared pBt, Pgno key, ref u8 pEType, ref Pgno pPgno)
    {
      DbPage pDbPage = new PgHdr();/* The pointer map page */
      int iPtrmap;                 /* Pointer map page index */
      u8[] pPtrmap;                /* Pointer map page data */
      int offset;                  /* Offset of entry in pointer map */
      int rc;

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));

      iPtrmap = (int)PTRMAP_PAGENO(pBt, key);
      rc = sqlite3PagerGet(pBt.pPager, (u32)iPtrmap, ref pDbPage);
      if (rc != 0)
      {
        return rc;
      }
      pPtrmap = sqlite3PagerGetData(pDbPage);

      offset = (int)PTRMAP_PTROFFSET((u32)iPtrmap, key);
      // Under C# pEType will always exist. No need to test; //
      //Debug.Assert( pEType != 0 );
      pEType = pPtrmap[offset];
      // Under C# pPgno will always exist. No need to test; //
      //if ( pPgno != 0 )
      pPgno = sqlite3Get4byte(pPtrmap, offset + 1);

      sqlite3PagerUnref(pDbPage);
      if (pEType < 1 || pEType > 5)
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      return SQLITE_OK;
    }

#else //* if defined SQLITE_OMIT_AUTOVACUUM */
//#define ptrmapPut(w,x,y,z,rc)
//#define ptrmapGet(w,x,y,z) SQLITE_OK
//#define ptrmapPutOvflPtr(x, y, rc)
#endif

    /*
** Given a btree page and a cell index (0 means the first cell on
** the page, 1 means the second cell, and so forth) return a pointer
** to the cell content.
**
** This routine works only for pages that do not contain overflow cells.
*/
    //#define findCell(P,I) \
    //  ((P).aData + ((P).maskPage & get2byte((P).aData[(P).cellOffset+2*(I)])))
    static int findCell(MemPage pPage, int iCell)
    {
      return get2byte(pPage.aData, (pPage).cellOffset + 2 * (iCell));
    }
    /*
    ** This a more complex version of findCell() that works for
    ** pages that do contain overflow cells.
    */
    static int findOverflowCell(MemPage pPage, int iCell)
    {
      int i;
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      for (i = pPage.nOverflow - 1; i >= 0; i--)
      {
        int k;
        _OvflCell pOvfl;
        pOvfl = pPage.aOvfl[i];
        k = pOvfl.idx;
        if (k <= iCell)
        {
          if (k == iCell)
          {
            //return pOvfl.pCell;
            return -i - 1; // Negative Offset means overflow cells
          }
          iCell--;
        }
      }
      return findCell(pPage, iCell);
    }

    /*
    ** Parse a cell content block and fill in the CellInfo structure.  There
    ** are two versions of this function.  btreeParseCell() takes a
    ** cell index as the second argument and btreeParseCellPtr()
    ** takes a pointer to the body of the cell as its second argument.
    **
    ** Within this file, the parseCell() macro can be called instead of
    ** btreeParseCellPtr(). Using some compilers, this will be faster.
    */
    //OVERLOADS
    static void btreeParseCellPtr(
    MemPage pPage,        /* Page containing the cell */
    int iCell,            /* Pointer to the cell text. */
    ref CellInfo pInfo        /* Fill in this structure */
    )
    { btreeParseCellPtr(pPage, pPage.aData, iCell, ref pInfo); }
    static void btreeParseCellPtr(
    MemPage pPage,        /* Page containing the cell */
    byte[] pCell,         /* The actual data */
    ref CellInfo pInfo        /* Fill in this structure */
    )
    { btreeParseCellPtr( pPage, pCell, 0, ref pInfo ); }
    static void btreeParseCellPtr(
    MemPage pPage,         /* Page containing the cell */
    u8[] pCell,            /* Pointer to the cell text. */
    int iCell,             /* Pointer to the cell text. */
    ref CellInfo pInfo         /* Fill in this structure */
    )
    {
      u16 n;                  /* Number bytes in cell content header */
      u32 nPayload = 0;           /* Number of bytes of cell payload */

      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));

      pInfo.pCell = pCell;
      pInfo.iCell = iCell;
      Debug.Assert(pPage.leaf == 0 || pPage.leaf == 1);
      n = pPage.childPtrSize;
      Debug.Assert(n == 4 - 4 * pPage.leaf);
      if (pPage.intKey != 0)
      {
        if (pPage.hasData != 0)
        {
          n += (u16)getVarint32(pCell, iCell + n, ref nPayload);
        }
        else
        {
          nPayload = 0;
        }
        n += (u16)getVarint(pCell, iCell + n, ref pInfo.nKey);
        pInfo.nData = nPayload;
      }
      else
      {
        pInfo.nData = 0;
        n += (u16)getVarint32(pCell, iCell + n, ref nPayload);
        pInfo.nKey = nPayload;
      }
      pInfo.nPayload = nPayload;
      pInfo.nHeader = n;
      testcase(nPayload == pPage.maxLocal);
      testcase(nPayload == pPage.maxLocal + 1);
      if (likely(nPayload <= pPage.maxLocal))
      {
        /* This is the (easy) common case where the entire payload fits
        ** on the local page.  No overflow is required.
        */
        int nSize;          /* Total size of cell content in bytes */
        nSize = (int)nPayload + n;
        pInfo.nLocal = (u16)nPayload;
        pInfo.iOverflow = 0;
        if ((nSize & ~3) == 0)
        {
          nSize = 4;        /* Minimum cell size is 4 */
        }
        pInfo.nSize = (u16)nSize;
      }
      else
      {
        /* If the payload will not fit completely on the local page, we have
        ** to decide how much to store locally and how much to spill onto
        ** overflow pages.  The strategy is to minimize the amount of unused
        ** space on overflow pages while keeping the amount of local storage
        ** in between minLocal and maxLocal.
        **
        ** Warning:  changing the way overflow payload is distributed in any
        ** way will result in an incompatible file format.
        */
        int minLocal;  /* Minimum amount of payload held locally */
        int maxLocal;  /* Maximum amount of payload held locally */
        int surplus;   /* Overflow payload available for local storage */

        minLocal = pPage.minLocal;
        maxLocal = pPage.maxLocal;
        surplus = (int)(minLocal + (nPayload - minLocal) % (pPage.pBt.usableSize - 4));
        testcase(surplus == maxLocal);
        testcase(surplus == maxLocal + 1);
        if (surplus <= maxLocal)
        {
          pInfo.nLocal = (u16)surplus;
        }
        else
        {
          pInfo.nLocal = (u16)minLocal;
        }
        pInfo.iOverflow = (u16)(pInfo.nLocal + n);
        pInfo.nSize = (u16)(pInfo.iOverflow + 4);
      }
    }
    //#define parseCell(pPage, iCell, pInfo) \
    //  btreeParseCellPtr((pPage), findCell((pPage), (iCell)), (pInfo))
    static void parseCell( MemPage pPage, int iCell, ref CellInfo pInfo )
    {
      btreeParseCellPtr( ( pPage ), findCell( ( pPage ), ( iCell ) ), ref ( pInfo ) );
    }

    static void btreeParseCell(
    MemPage pPage,         /* Page containing the cell */
    int iCell,              /* The cell index.  First cell is 0 */
    ref CellInfo pInfo         /* Fill in this structure */
    )
    {
      parseCell( pPage, iCell, ref pInfo );
    }

    /*
    ** Compute the total number of bytes that a Cell needs in the cell
    ** data area of the btree-page.  The return number includes the cell
    ** data header and the local payload, but not any overflow page or
    ** the space used by the cell pointer.
    */
    // Alternative form for C#
    static u16 cellSizePtr(MemPage pPage, int iCell)
    {
      CellInfo info = new CellInfo();
      byte[] pCell = new byte[13];// Minimum Size = (2 bytes of Header  or (4) Child Pointer) + (maximum of) 9 bytes data
      if (iCell < 0)// Overflow Cell
        Buffer.BlockCopy(pPage.aOvfl[-(iCell + 1)].pCell, 0, pCell, 0, pCell.Length < pPage.aOvfl[-(iCell + 1)].pCell.Length ? pCell.Length : pPage.aOvfl[-(iCell + 1)].pCell.Length);
      else if (iCell >= pPage.aData.Length + 1 - pCell.Length)
        Buffer.BlockCopy(pPage.aData, iCell, pCell, 0, pPage.aData.Length - iCell);
      else
        Buffer.BlockCopy(pPage.aData, iCell, pCell, 0, pCell.Length);
      btreeParseCellPtr( pPage, pCell, ref info );
      return info.nSize;
    }

    // Alternative form for C#
    static u16 cellSizePtr(MemPage pPage, byte[] pCell, int offset)
    {
      CellInfo info = new CellInfo();
      byte[] pTemp = new byte[pCell.Length];
      Buffer.BlockCopy(pCell, offset, pTemp, 0, pCell.Length - offset);
      btreeParseCellPtr( pPage, pTemp, ref info );
      return info.nSize;
    }

    static u16 cellSizePtr(MemPage pPage, u8[] pCell)
    {
      int _pIter = pPage.childPtrSize; //u8 pIter = &pCell[pPage.childPtrSize];
      u32 nSize = 0;

#if SQLITE_DEBUG || DEBUG
      /* The value returned by this function should always be the same as
** the (CellInfo.nSize) value found by doing a full parse of the
** cell. If SQLITE_DEBUG is defined, an Debug.Assert() at the bottom of
** this function verifies that this invariant is not violated. */
      CellInfo debuginfo = new CellInfo();
      btreeParseCellPtr(pPage, pCell, ref debuginfo);
#else
      CellInfo debuginfo = new CellInfo();
#endif

      if (pPage.intKey != 0)
      {
        int pEnd;
        if (pPage.hasData != 0)
        {
          _pIter += getVarint32(pCell, ref nSize);// pIter += getVarint32( pIter, ref nSize );
        }
        else
        {
          nSize = 0;
        }

        /* pIter now points at the 64-bit integer key value, a variable length
        ** integer. The following block moves pIter to point at the first byte
        ** past the end of the key value. */
        pEnd = _pIter + 9;//pEnd = &pIter[9];
        while (((pCell[_pIter++]) & 0x80) != 0 && _pIter < pEnd) ;//while( (pIter++)&0x80 && pIter<pEnd );
      }
      else
      {
        _pIter += getVarint32(pCell, _pIter, ref nSize); //pIter += getVarint32( pIter, ref nSize );
      }

      testcase(nSize == pPage.maxLocal);
      testcase(nSize == pPage.maxLocal + 1);
      if (nSize > pPage.maxLocal)
      {
        int minLocal = pPage.minLocal;
        nSize = (u32)(minLocal + (nSize - minLocal) % (pPage.pBt.usableSize - 4));
        testcase(nSize == pPage.maxLocal);
        testcase(nSize == pPage.maxLocal + 1);
        if (nSize > pPage.maxLocal)
        {
          nSize = (u32)minLocal;
        }
        nSize += 4;
      }
      nSize += (uint)_pIter;//nSize += (u32)(pIter - pCell);

      /* The minimum size of any cell is 4 bytes. */
      if (nSize < 4)
      {
        nSize = 4;
      }

      Debug.Assert(nSize == debuginfo.nSize);
      return (u16)nSize;
    }
#if !NDEBUG || DEBUG
    static u16 cellSize(MemPage pPage, int iCell)
    {
      return cellSizePtr(pPage, findCell(pPage, iCell));
    }
#else
static int cellSize(MemPage pPage, int iCell) { return -1; }
#endif

#if !SQLITE_OMIT_AUTOVACUUM
    /*
** If the cell pCell, part of page pPage contains a pointer
** to an overflow page, insert an entry into the pointer-map
** for the overflow page.
*/
    static void ptrmapPutOvflPtr(MemPage pPage, int pCell, ref int pRC)
    {
      if (pRC != 0) return;
      CellInfo info = new CellInfo();
      Debug.Assert(pCell != 0);
      btreeParseCellPtr( pPage, pCell, ref info );
      Debug.Assert((info.nData + (pPage.intKey != 0 ? 0 : info.nKey)) == info.nPayload);
      if (info.iOverflow != 0)
      {
        Pgno ovfl = sqlite3Get4byte(pPage.aData, pCell, info.iOverflow);
        ptrmapPut(pPage.pBt, ovfl, PTRMAP_OVERFLOW1, pPage.pgno, ref pRC);
      }
    }

    static void ptrmapPutOvflPtr(MemPage pPage, u8[] pCell, ref int pRC)
    {
      if (pRC != 0) return;
      CellInfo info = new CellInfo();
      Debug.Assert(pCell != null);
      btreeParseCellPtr( pPage, pCell, ref info );
      Debug.Assert((info.nData + (pPage.intKey != 0 ? 0 : info.nKey)) == info.nPayload);
      if (info.iOverflow != 0)
      {
        Pgno ovfl = sqlite3Get4byte(pCell, info.iOverflow);
        ptrmapPut(pPage.pBt, ovfl, PTRMAP_OVERFLOW1, pPage.pgno, ref pRC);
      }
    }
#endif


    /*
** Defragment the page given.  All Cells are moved to the
** end of the page and all free space is collected into one
** big FreeBlk that occurs in between the header and cell
** pointer array and the cell content area.
*/
    static int defragmentPage(MemPage pPage)
    {
      int i;                     /* Loop counter */
      int pc;                    /* Address of a i-th cell */
      int addr;                  /* Offset of first byte after cell pointer array */
      int hdr;                   /* Offset to the page header */
      int size;                  /* Size of a cell */
      int usableSize;            /* Number of usable bytes on a page */
      int cellOffset;            /* Offset to the cell pointer array */
      int cbrk;                  /* Offset to the cell content area */
      int nCell;                 /* Number of cells on the page */
      byte[] data;               /* The page data */
      byte[] temp;               /* Temp area for cell content */
      int iCellFirst;            /* First allowable cell index */
      int iCellLast;             /* Last possible cell index */


      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
      Debug.Assert(pPage.pBt != null);
      Debug.Assert(pPage.pBt.usableSize <= SQLITE_MAX_PAGE_SIZE);
      Debug.Assert(pPage.nOverflow == 0);
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      temp = sqlite3PagerTempSpace(pPage.pBt.pPager);
      data = pPage.aData;
      hdr = pPage.hdrOffset;
      cellOffset = pPage.cellOffset;
      nCell = pPage.nCell;
      Debug.Assert(nCell == get2byte(data, hdr + 3));
      usableSize = pPage.pBt.usableSize;
      cbrk = get2byte(data, hdr + 5);
      Buffer.BlockCopy(data, cbrk, temp, cbrk, usableSize - cbrk);//memcpy( temp[cbrk], ref data[cbrk], usableSize - cbrk );
      cbrk = usableSize;
      iCellFirst = cellOffset + 2 * nCell;
      iCellLast = usableSize - 4;
      for (i = 0; i < nCell; i++)
      {
        int pAddr;     /* The i-th cell pointer */
        pAddr = cellOffset + i * 2; // &data[cellOffset + i * 2];
        pc = get2byte(data, pAddr);
        testcase(pc == iCellFirst);
        testcase(pc == iCellLast);
#if !(SQLITE_ENABLE_OVERSIZE_CELL_CHECK)
/* These conditions have already been verified in btreeInitPage()
** if SQLITE_ENABLE_OVERSIZE_CELL_CHECK is defined
*/
if( pc<iCellFirst || pc>iCellLast ){
#if SQLITE_DEBUG || DEBUG
return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
}
#endif
        Debug.Assert(pc >= iCellFirst && pc <= iCellLast);
        size = cellSizePtr(pPage, temp, pc);
        cbrk -= size;
#if (SQLITE_ENABLE_OVERSIZE_CELL_CHECK)
        if (cbrk < iCellFirst)
        {
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
#else
if( cbrk<iCellFirst || pc+size>usableSize ){
#if SQLITE_DEBUG || DEBUG
return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
}
#endif
        Debug.Assert(cbrk + size <= usableSize && cbrk >= iCellFirst);
        testcase(cbrk + size == usableSize);
        testcase(pc + size == usableSize);
        Buffer.BlockCopy(temp, pc, data, cbrk, size);//memcpy(data[cbrk], ref temp[pc], size);
        put2byte(data, pAddr, cbrk);
      }
      Debug.Assert(cbrk >= iCellFirst);
      put2byte(data, hdr + 5, cbrk);
      data[hdr + 1] = 0;
      data[hdr + 2] = 0;
      data[hdr + 7] = 0;
      addr = cellOffset + 2 * nCell;
      Array.Clear(data, addr, cbrk - addr);  //memset(data[iCellFirst], 0, cbrk-iCellFirst);
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
      if (cbrk - iCellFirst != pPage.nFree)
      {
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }
      return SQLITE_OK;
    }

    /*
    ** Allocate nByte bytes of space from within the B-Tree page passed
    ** as the first argument. Write into pIdx the index into pPage.aData[]
    ** of the first byte of allocated space. Return either SQLITE_OK or
    ** an error code (usually SQLITE_CORRUPT).
    **
    ** The caller guarantees that there is sufficient space to make the
    ** allocation.  This routine might need to defragment in order to bring
    ** all the space together, however.  This routine will avoid using
    ** the first two bytes past the cell pointer area since presumably this
    ** allocation is being made in order to insert a new cell, so we will
    ** also end up needing a new cell pointer.
    */
    static int allocateSpace(MemPage pPage, int nByte, ref int pIdx)
    {
      int hdr = pPage.hdrOffset;  /* Local cache of pPage.hdrOffset */
      u8[] data = pPage.aData;    /* Local cache of pPage.aData */
      int nFrag;                  /* Number of fragmented bytes on pPage */
      int top;                    /* First byte of cell content area */
      int gap;                    /* First byte of gap between cell pointers and cell content */
      int rc;                     /* Integer return code */

      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
      Debug.Assert(pPage.pBt != null);
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      Debug.Assert(nByte >= 0);  /* Minimum cell size is 4 */
      Debug.Assert(pPage.nFree >= nByte);
      Debug.Assert(pPage.nOverflow == 0);
      Debug.Assert(nByte < pPage.pBt.usableSize - 8);

      nFrag = data[hdr + 7];
      Debug.Assert(pPage.cellOffset == hdr + 12 - 4 * pPage.leaf);
      gap = pPage.cellOffset + 2 * pPage.nCell;
      top = get2byte(data, hdr + 5);
      if (gap > top)
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      testcase(gap + 2 == top);
      testcase(gap + 1 == top);
      testcase(gap == top);

      if (nFrag >= 60)
      {
        /* Always defragment highly fragmented pages */
        rc = defragmentPage(pPage);
        if (rc != 0) return rc;
        top = get2byte(data, hdr + 5);
      }
      else if (gap + 2 <= top)
      {
        /* Search the freelist looking for a free slot big enough to satisfy
        ** the request. The allocation is made from the first free slot in
        ** the list that is large enough to accomadate it.
        */
        int pc, addr;
        for (addr = hdr + 1; (pc = get2byte(data, addr)) > 0; addr = pc)
        {
          int size = get2byte(data, pc + 2);     /* Size of free slot */
          if (size >= nByte)
          {
            int x = size - nByte;
            testcase(x == 4);
            testcase(x == 3);
            if (x < 4)
            {
              /* Remove the slot from the free-list. Update the number of
              ** fragmented bytes within the page. */
              data[addr + 0] = data[pc + 0]; data[addr + 1] = data[pc + 1]; //memcpy( data[addr], ref data[pc], 2 );
              data[hdr + 7] = (u8)(nFrag + x);
            }
            else
            {
              /* The slot remains on the free-list. Reduce its size to account
              ** for the portion used by the new allocation. */
              put2byte(data, pc + 2, x);
            }
            pIdx = pc + x;
            return SQLITE_OK;
          }
        }
      }

      /* Check to make sure there is enough space in the gap to satisfy
      ** the allocation.  If not, defragment.
      */
      testcase(gap + 2 + nByte == top);
      if (gap + 2 + nByte > top)
      {
        rc = defragmentPage(pPage);
        if (rc != 0) return rc;
        top = get2byte(data, hdr + 5);
        Debug.Assert(gap + nByte <= top);
      }


      /* Allocate memory from the gap in between the cell pointer array
      ** and the cell content area.  The btreeInitPage() call has already
      ** validated the freelist.  Given that the freelist is valid, there
      ** is no way that the allocation can extend off the end of the page.
      ** The Debug.Assert() below verifies the previous sentence.
      */
      top -= nByte;
      put2byte(data, hdr + 5, top);
      Debug.Assert(top + nByte <= pPage.pBt.usableSize);
      pIdx = top;
      return SQLITE_OK;
    }

    /*
    ** Return a section of the pPage.aData to the freelist.
    ** The first byte of the new free block is pPage.aDisk[start]
    ** and the size of the block is "size" bytes.
    **
    ** Most of the effort here is involved in coalesing adjacent
    ** free blocks into a single big free block.
    */
    static int freeSpace(MemPage pPage, int start, int size)
    {
      int addr, pbegin, hdr;
      int iLast;                        /* Largest possible freeblock offset */
      byte[] data = pPage.aData;

      Debug.Assert(pPage.pBt != null);
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
      Debug.Assert(start >= pPage.hdrOffset + 6 + pPage.childPtrSize);
      Debug.Assert((start + size) <= pPage.pBt.usableSize);
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      Debug.Assert(size >= 0);   /* Minimum cell size is 4 */

#if SQLITE_SECURE_DELETE
/* Overwrite deleted information with zeros when the SECURE_DELETE
** option is enabled at compile-time */
memset(data[start], 0, size);
#endif

      /* Add the space back into the linked list of freeblocks.  Note that
** even though the freeblock list was checked by btreeInitPage(),
** btreeInitPage() did not detect overlapping cells or
** freeblocks that overlapped cells.   Nor does it detect when the
** cell content area exceeds the value in the page header.  If these
** situations arise, then subsequent insert operations might corrupt
** the freelist.  So we do need to check for corruption while scanning
** the freelist.
*/
      hdr = pPage.hdrOffset;
      addr = hdr + 1;
      iLast = pPage.pBt.usableSize - 4;
      Debug.Assert(start <= iLast);
      while ((pbegin = get2byte(data, addr)) < start && pbegin > 0)
      {
        if (pbegin < addr + 4)
        {
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
        addr = pbegin;
      }
      if (pbegin > iLast)
      {
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }
      Debug.Assert(pbegin > addr || pbegin == 0);
      put2byte(data, addr, start);
      put2byte(data, start, pbegin);
      put2byte(data, start + 2, size);
      pPage.nFree = (u16)(pPage.nFree + size);

      /* Coalesce adjacent free blocks */
      addr = hdr + 1;
      while ((pbegin = get2byte(data, addr)) > 0)
      {
        int pnext, psize, x;
        Debug.Assert(pbegin > addr);
        Debug.Assert(pbegin <= pPage.pBt.usableSize - 4);
        pnext = get2byte(data, pbegin);
        psize = get2byte(data, pbegin + 2);
        if (pbegin + psize + 3 >= pnext && pnext > 0)
        {
          int frag = pnext - (pbegin + psize);
          if ((frag < 0) || (frag > (int)data[hdr + 7]))
          {
#if SQLITE_DEBUG || DEBUG
            return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
          }
          data[hdr + 7] -= (u8)frag;
          x = get2byte(data, pnext);
          put2byte(data, pbegin, x);
          x = pnext + get2byte(data, pnext + 2) - pbegin;
          put2byte(data, pbegin + 2, x);
        }
        else
        {
          addr = pbegin;
        }
      }

      /* If the cell content area begins with a freeblock, remove it. */
      if (data[hdr + 1] == data[hdr + 5] && data[hdr + 2] == data[hdr + 6])
      {
        int top;
        pbegin = get2byte(data, hdr + 1);
        put2byte(data, hdr + 1, get2byte(data, pbegin)); //memcpy( data[hdr + 1], ref data[pbegin], 2 );
        top = get2byte(data, hdr + 5) + get2byte(data, pbegin + 2);
        put2byte(data, hdr + 5, top);
      }
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
      return SQLITE_OK;
    }

    /*
    ** Decode the flags byte (the first byte of the header) for a page
    ** and initialize fields of the MemPage structure accordingly.
    **
    ** Only the following combinations are supported.  Anything different
    ** indicates a corrupt database files:
    **
    **         PTF_ZERODATA
    **         PTF_ZERODATA | PTF_LEAF
    **         PTF_LEAFDATA | PTF_INTKEY
    **         PTF_LEAFDATA | PTF_INTKEY | PTF_LEAF
    */
    static int decodeFlags(MemPage pPage, int flagByte)
    {
      BtShared pBt;     /* A copy of pPage.pBt */

      Debug.Assert(pPage.hdrOffset == (pPage.pgno == 1 ? 100 : 0));
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      pPage.leaf = (u8)(flagByte >> 3); Debug.Assert(PTF_LEAF == 1 << 3);
      flagByte &= ~PTF_LEAF;
      pPage.childPtrSize = (u8)(4 - 4 * pPage.leaf);
      pBt = pPage.pBt;
      if (flagByte == (PTF_LEAFDATA | PTF_INTKEY))
      {
        pPage.intKey = 1;
        pPage.hasData = pPage.leaf;
        pPage.maxLocal = pBt.maxLeaf;
        pPage.minLocal = pBt.minLeaf;
      }
      else if (flagByte == PTF_ZERODATA)
      {
        pPage.intKey = 0;
        pPage.hasData = 0;
        pPage.maxLocal = pBt.maxLocal;
        pPage.minLocal = pBt.minLocal;
      }
      else
      {
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }
      return SQLITE_OK;
    }

    /*
    ** Initialize the auxiliary information for a disk block.
    **
    ** Return SQLITE_OK on success.  If we see that the page does
    ** not contain a well-formed database page, then return
    ** SQLITE_CORRUPT.  Note that a return of SQLITE_OK does not
    ** guarantee that the page is well-formed.  It only shows that
    ** we failed to detect any corruption.
    */
    static int btreeInitPage(MemPage pPage)
    {

      Debug.Assert(pPage.pBt != null);
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      Debug.Assert(pPage.pgno == sqlite3PagerPagenumber(pPage.pDbPage));
      Debug.Assert(pPage == sqlite3PagerGetExtra(pPage.pDbPage));
      Debug.Assert(pPage.aData == sqlite3PagerGetData(pPage.pDbPage));

      if (0 == pPage.isInit)
      {
        u16 pc;            /* Address of a freeblock within pPage.aData[] */
        u8 hdr;            /* Offset to beginning of page header */
        u8[] data;         /* Equal to pPage.aData */
        BtShared pBt;      /* The main btree structure */
        u16 usableSize;    /* Amount of usable space on each page */
        u16 cellOffset;    /* Offset from start of page to first cell pointer */
        u16 nFree;         /* Number of unused bytes on the page */
        u16 top;           /* First byte of the cell content area */
        int iCellFirst;    /* First allowable cell or freeblock offset */
        int iCellLast;     /* Last possible cell or freeblock offset */

        pBt = pPage.pBt;

        hdr = pPage.hdrOffset;
        data = pPage.aData;
        if (decodeFlags(pPage, data[hdr]) != 0)
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        Debug.Assert(pBt.pageSize >= 512 && pBt.pageSize <= 32768);
        pPage.maskPage = (u16)(pBt.pageSize - 1);
        pPage.nOverflow = 0;
        usableSize = pBt.usableSize;
        pPage.cellOffset = (cellOffset = (u16)(hdr + 12 - 4 * pPage.leaf));
        top = (u16)get2byte(data, hdr + 5);
        pPage.nCell = (u16)(get2byte(data, hdr + 3));
        if (pPage.nCell > MX_CELL(pBt))
        {
          /* To many cells for a single page.  The page must be corrupt */
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
        testcase(pPage.nCell == MX_CELL(pBt));

        /* A malformed database page might cause us to read past the end
        ** of page when parsing a cell.
        **
        ** The following block of code checks early to see if a cell extends
        ** past the end of a page boundary and causes SQLITE_CORRUPT to be
        ** returned if it does.
        */
        iCellFirst = cellOffset + 2 * pPage.nCell;
        iCellLast = usableSize - 4;
#if (SQLITE_ENABLE_OVERSIZE_CELL_CHECK)
        {
          int i;            /* Index into the cell pointer array */
          int sz;           /* Size of a cell */

          if (0 == pPage.leaf) iCellLast--;
          for (i = 0; i < pPage.nCell; i++)
          {
            pc = (u16)get2byte(data, cellOffset + i * 2);
            testcase(pc == iCellFirst);
            testcase(pc == iCellLast);
            if (pc < iCellFirst || pc > iCellLast)
            {
#if SQLITE_DEBUG || DEBUG
              return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
            }
            sz = cellSizePtr(pPage, data, pc);
            testcase(pc + sz == usableSize);
            if (pc + sz > usableSize)
            {
#if SQLITE_DEBUG || DEBUG
              return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
            }
          }
          if (0 == pPage.leaf) iCellLast++;
        }
#endif

        /* Compute the total free space on the page */
        pc = (u16)get2byte(data, hdr + 1);
        nFree = (u16)(data[hdr + 7] + top);
        while (pc > 0)
        {
          u16 next, size;
          if (pc < iCellFirst || pc > iCellLast)
          {
            /* Free block is off the page */
#if SQLITE_DEBUG || DEBUG
            return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
          }
          next = (u16)get2byte(data, pc);
          size = (u16)get2byte(data, pc + 2);
          if (next > 0 && next <= pc + size + 3)
          {
            /* Free blocks must be in ascending order */
#if SQLITE_DEBUG || DEBUG
            return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
          }
          nFree = (u16)(nFree + size);
          pc = next;
        }

        /* At this point, nFree contains the sum of the offset to the start
        ** of the cell-content area plus the number of free bytes within
        ** the cell-content area. If this is greater than the usable-size
        ** of the page, then the page must be corrupted. This check also
        ** serves to verify that the offset to the start of the cell-content
        ** area, according to the page header, lies within the page.
        */
        if (nFree > usableSize)
        {
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
        pPage.nFree = (u16)(nFree - iCellFirst);
        pPage.isInit = 1;
      }
      return SQLITE_OK;
    }

    /*
    ** Set up a raw page so that it looks like a database page holding
    ** no entries.
    */
    static void zeroPage(MemPage pPage, int flags)
    {
      byte[] data = pPage.aData;
      BtShared pBt = pPage.pBt;
      u8 hdr = pPage.hdrOffset;
      u16 first;

      Debug.Assert(sqlite3PagerPagenumber(pPage.pDbPage) == pPage.pgno);
      Debug.Assert(sqlite3PagerGetExtra(pPage.pDbPage) == pPage);
      Debug.Assert(sqlite3PagerGetData(pPage.pDbPage) == data);
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      /*memset(data[hdr], 0, pBt.usableSize - hdr);*/
      data[hdr] = (u8)flags;
      first = (u16)(hdr + 8 + 4 * ((flags & PTF_LEAF) == 0 ? 1 : 0));
      Array.Clear(data, hdr + 1, 4);//memset(data[hdr+1], 0, 4);
      data[hdr + 7] = 0;
      put2byte(data, hdr + 5, pBt.usableSize);
      pPage.nFree = (u16)(pBt.usableSize - first);
      decodeFlags(pPage, flags);
      pPage.hdrOffset = hdr;
      pPage.cellOffset = first;
      pPage.nOverflow = 0;
      Debug.Assert(pBt.pageSize >= 512 && pBt.pageSize <= 32768);
      pPage.maskPage = (u16)(pBt.pageSize - 1);
      pPage.nCell = 0;
      pPage.isInit = 1;
    }


    /*
    ** Convert a DbPage obtained from the pager into a MemPage used by
    ** the btree layer.
    */
    static MemPage btreePageFromDbPage(DbPage pDbPage, Pgno pgno, BtShared pBt)
    {
      MemPage pPage = (MemPage)sqlite3PagerGetExtra(pDbPage);
      pPage.aData = sqlite3PagerGetData(pDbPage);
      pPage.pDbPage = pDbPage;
      pPage.pBt = pBt;
      pPage.pgno = pgno;
      pPage.hdrOffset = (u8)(pPage.pgno == 1 ? 100 : 0);
      return pPage;
    }

    /*
    ** Get a page from the pager.  Initialize the MemPage.pBt and
    ** MemPage.aData elements if needed.
    **
    ** If the noContent flag is set, it means that we do not care about
    ** the content of the page at this time.  So do not go to the disk
    ** to fetch the content.  Just fill in the content with zeros for now.
    ** If in the future we call sqlite3PagerWrite() on this page, that
    ** means we have started to be concerned about content and the disk
    ** read should occur at that point.
    */
    static int btreeGetPage(
    BtShared pBt,        /* The btree */
    Pgno pgno,           /* Number of the page to fetch */
    ref MemPage ppPage,  /* Return the page in this parameter */
    int noContent        /* Do not load page content if true */
    )
    {
      int rc;
      DbPage pDbPage = new PgHdr();

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      rc = sqlite3PagerAcquire(pBt.pPager, pgno, ref pDbPage, (u8)noContent);
      if (rc != 0) return rc;
      ppPage = btreePageFromDbPage(pDbPage, pgno, pBt);
      return SQLITE_OK;
    }

    /*
    ** Retrieve a page from the pager cache. If the requested page is not
    ** already in the pager cache return NULL. Initialize the MemPage.pBt and
    ** MemPage.aData elements if needed.
    */
    static MemPage btreePageLookup(BtShared pBt, Pgno pgno)
    {
      DbPage pDbPage;
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      pDbPage = sqlite3PagerLookup(pBt.pPager, pgno);
      if (pDbPage)
      {
        return btreePageFromDbPage(pDbPage, pgno, pBt);
      }
      return null;
    }

    /*
    ** Return the size of the database file in pages. If there is any kind of
    ** error, return ((unsigned int)-1).
    */
    static Pgno pagerPagecount(BtShared pBt)
    {
      int nPage = -1;
      int rc;
      Debug.Assert(pBt.pPage1 != null);
      rc = sqlite3PagerPagecount(pBt.pPager, ref nPage);
      Debug.Assert(rc == SQLITE_OK || nPage == -1);
      return (Pgno)nPage;
    }

    /*
    ** Get a page from the pager and initialize it.  This routine is just a
    ** convenience wrapper around separate calls to btreeGetPage() and
    ** btreeInitPage().
    **
    ** If an error occurs, then the value ppPage is set to is undefined. It
    ** may remain unchanged, or it may be set to an invalid value.
    */
    static int getAndInitPage(
    BtShared pBt,          /* The database file */
    Pgno pgno,             /* Number of the page to get */
    ref MemPage ppPage     /* Write the page pointer here */
    )
    {
      int rc;
#if !NDEBUG || SQLITE_COVERAGE_TEST
      Pgno iLastPg = pagerPagecount(pBt);//  TESTONLY( Pgno iLastPg = pagerPagecount(pBt); )
#else
const Pgno iLastPg = Pgno.MaxValue;
#endif
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));

      rc = btreeGetPage(pBt, pgno, ref ppPage, 0);
      if (rc == SQLITE_OK)
      {
        rc = btreeInitPage(ppPage);
        if (rc != SQLITE_OK)
        {
          releasePage(ppPage);
        }
      }

      /* If the requested page number was either 0 or greater than the page
      ** number of the last page in the database, this function should return
      ** SQLITE_CORRUPT or some other error (i.e. SQLITE_FULL). Check that this
      ** is the case.  */
      Debug.Assert((pgno > 0 && pgno <= iLastPg) || rc != SQLITE_OK);
      testcase(pgno == 0);
      testcase(pgno == iLastPg);

      return rc;
    }

    /*
    ** Release a MemPage.  This should be called once for each prior
    ** call to btreeGetPage.
    */
    static void releasePage(MemPage pPage)
    {
      if (pPage != null)
      {
        Debug.Assert(pPage.nOverflow == 0 || sqlite3PagerPageRefcount(pPage.pDbPage) > 1);
        Debug.Assert(pPage.aData != null);
        Debug.Assert(pPage.pBt != null);
        Debug.Assert(sqlite3PagerGetExtra(pPage.pDbPage) == pPage);
        Debug.Assert(sqlite3PagerGetData(pPage.pDbPage) == pPage.aData);
        Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
        sqlite3PagerUnref(pPage.pDbPage);
      }
    }

    /*
    ** During a rollback, when the pager reloads information into the cache
    ** so that the cache is restored to its original state at the start of
    ** the transaction, for each page restored this routine is called.
    **
    ** This routine needs to reset the extra data section at the end of the
    ** page to agree with the restored data.
    */
    static void pageReinit(DbPage pData)
    {
      MemPage pPage;
      pPage = sqlite3PagerGetExtra(pData);
      Debug.Assert(sqlite3PagerPageRefcount(pData) > 0);
      if (pPage.isInit != 0)
      {
        Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
        pPage.isInit = 0;
        if (sqlite3PagerPageRefcount(pData) > 1)
        {
          /* pPage might not be a btree page;  it might be an overflow page
          ** or ptrmap page or a free page.  In those cases, the following
          ** call to btreeInitPage() will likely return SQLITE_CORRUPT.
          ** But no harm is done by this.  And it is very important that
          ** btreeInitPage() be called on every btree page so we make
          ** the call for every page that comes in for re-initing. */
          btreeInitPage(pPage);
        }
      }
    }

    /*
    ** Invoke the busy handler for a btree.
    */
    static int btreeInvokeBusyHandler(object pArg)
    {
      BtShared pBt = (BtShared)pArg;
      Debug.Assert(pBt.db != null);
      Debug.Assert(sqlite3_mutex_held(pBt.db.mutex));
      return sqlite3InvokeBusyHandler(pBt.db.busyHandler);
    }

    /*
    ** Open a database file.
    **
    ** zFilename is the name of the database file.  If zFilename is NULL
    ** a new database with a random name is created.  This randomly named
    ** database file will be deleted when sqlite3BtreeClose() is called.
    ** If zFilename is ":memory:" then an in-memory database is created
    ** that is automatically destroyed when it is closed.
    **
    ** If the database is already opened in the same database connection
    ** and we are in shared cache mode, then the open will fail with an
    ** SQLITE_CONSTRAINT error.  We cannot allow two or more BtShared
    ** objects in the same database connection since doing so will lead
    ** to problems with locking.
    */
    static int sqlite3BtreeOpen(
    string zFilename,       /* Name of the file containing the BTree database */
    sqlite3 db,             /* Associated database handle */
    ref Btree ppBtree,      /* Pointer to new Btree object written here */
    int flags,              /* Options */
    int vfsFlags            /* Flags passed through to sqlite3_vfs.xOpen() */
    )
    {
      sqlite3_vfs pVfs;             /* The VFS to use for this btree */
      BtShared pBt = null;          /* Shared part of btree structure */
      Btree p;                      /* Handle to return */
      sqlite3_mutex mutexOpen = null;  /* Prevents a race condition. Ticket #3537 */
      int rc = SQLITE_OK;            /* Result code from this function */
      u8 nReserve;                   /* Byte of unused space on each page */
      byte[] zDbHeader = new byte[100]; /* Database header content */

      /* Set the variable isMemdb to true for an in-memory database, or
      ** false for a file-based database. This symbol is only required if
      ** either of the shared-data or autovacuum features are compiled
      ** into the library.
      */
#if !(SQLITE_OMIT_SHARED_CACHE) || !(SQLITE_OMIT_AUTOVACUUM)
#if SQLITE_OMIT_MEMORYDB
bool isMemdb = false;
#else
      bool isMemdb = zFilename == ":memory:";
#endif
#endif

      Debug.Assert(db != null);
      Debug.Assert(sqlite3_mutex_held(db.mutex));

      pVfs = db.pVfs;
      p = new Btree();//sqlite3MallocZero(sizeof(Btree));
      //if( !p ){
      //  return SQLITE_NOMEM;
      //}
      p.inTrans = TRANS_NONE;
      p.db = db;
#if !SQLITE_OMIT_SHARED_CACHE
p.lock.pBtree = p;
p.lock.iTable = 1;
#endif

#if !(SQLITE_OMIT_SHARED_CACHE) && !(SQLITE_OMIT_DISKIO)
/*
** If this Btree is a candidate for shared cache, try to find an
** existing BtShared object that we can share with
*/
if( isMemdb==null && zFilename && zFilename[0] ){
if( sqlite3GlobalConfig.sharedCacheEnabled ){
int nFullPathname = pVfs.mxPathname+1;
string zFullPathname = sqlite3Malloc(nFullPathname);
sqlite3_mutex *mutexShared;
p.sharable = 1;
if( !zFullPathname ){
p = null;//sqlite3_free(ref p);
return SQLITE_NOMEM;
}
sqlite3OsFullPathname(pVfs, zFilename, nFullPathname, zFullPathname);
mutexOpen = sqlite3MutexAlloc(SQLITE_MUTEX_STATIC_OPEN);
sqlite3_mutex_enter(mutexOpen);
mutexShared = sqlite3MutexAlloc(SQLITE_MUTEX_STATIC_MASTER);
sqlite3_mutex_enter(mutexShared);
for(pBt=GLOBAL(BtShared*,sqlite3SharedCacheList); pBt; pBt=pBt.pNext){
Debug.Assert( pBt.nRef>0 );
if( 0==strcmp(zFullPathname, sqlite3PagerFilename(pBt.pPager))
&& sqlite3PagerVfs(pBt.pPager)==pVfs ){
int iDb;
for(iDb=db.nDb-1; iDb>=0; iDb--){
Btree pExisting = db.aDb[iDb].pBt;
if( pExisting && pExisting.pBt==pBt ){
sqlite3_mutex_leave(mutexShared);
sqlite3_mutex_leave(mutexOpen);
zFullPathname = null;//sqlite3_free(ref zFullPathname);
p=null;//sqlite3_free(ref p);
return SQLITE_CONSTRAINT;
}
}
p.pBt = pBt;
pBt.nRef++;
break;
}
}
sqlite3_mutex_leave(mutexShared);
zFullPathname=null;//sqlite3_free(ref zFullPathname);
}
#if SQLITE_DEBUG
else{
/* In debug mode, we mark all persistent databases as sharable
** even when they are not.  This exercises the locking code and
** gives more opportunity for asserts(sqlite3_mutex_held())
** statements to find locking problems.
*/
p.sharable = 1;
}
#endif
}
#endif
      if (pBt == null)
      {
        /*
        ** The following asserts make sure that structures used by the btree are
        ** the right size.  This is to guard against size changes that result
        ** when compiling on a different architecture.
        */
        Debug.Assert(sizeof(i64) == 8 || sizeof(i64) == 4);
        Debug.Assert(sizeof(u64) == 8 || sizeof(u64) == 4);
        Debug.Assert(sizeof(u32) == 4);
        Debug.Assert(sizeof(u16) == 2);
        Debug.Assert(sizeof(Pgno) == 4);

        pBt = new BtShared();//sqlite3MallocZero( sizeof(pBt) );
        //if( pBt==null ){
        //  rc = SQLITE_NOMEM;
        //  goto btree_open_out;
        //}
        rc = sqlite3PagerOpen(pVfs, ref pBt.pPager, zFilename,
        EXTRA_SIZE, flags, vfsFlags, pageReinit);
        if (rc == SQLITE_OK)
        {
          rc = sqlite3PagerReadFileheader(pBt.pPager, zDbHeader.Length, zDbHeader);
        }
        if (rc != SQLITE_OK)
        {
          goto btree_open_out;
        }
        pBt.db = db;
        sqlite3PagerSetBusyhandler(pBt.pPager, btreeInvokeBusyHandler, pBt);
        p.pBt = pBt;

        pBt.pCursor = null;
        pBt.pPage1 = null;
        pBt.readOnly = sqlite3PagerIsreadonly(pBt.pPager);
        pBt.pageSize = (u16)get2byte(zDbHeader, 16);
        if (pBt.pageSize < 512 || pBt.pageSize > SQLITE_MAX_PAGE_SIZE
        || ((pBt.pageSize - 1) & pBt.pageSize) != 0)
        {
          pBt.pageSize = 0;
#if !SQLITE_OMIT_AUTOVACUUM
          /* If the magic name ":memory:" will create an in-memory database, then
** leave the autoVacuum mode at 0 (do not auto-vacuum), even if
** SQLITE_DEFAULT_AUTOVACUUM is true. On the other hand, if
** SQLITE_OMIT_MEMORYDB has been defined, then ":memory:" is just a
** regular file-name. In this case the auto-vacuum applies as per normal.
*/
          if (zFilename != "" && !isMemdb)
          {
            pBt.autoVacuum = (SQLITE_DEFAULT_AUTOVACUUM != 0);
            pBt.incrVacuum = (SQLITE_DEFAULT_AUTOVACUUM == 2);
          }
#endif
          nReserve = 0;
        }
        else
        {
          nReserve = zDbHeader[20];
          pBt.pageSizeFixed = true;
#if !SQLITE_OMIT_AUTOVACUUM
          pBt.autoVacuum = sqlite3Get4byte(zDbHeader, 36 + 4 * 4) != 0;
          pBt.incrVacuum = sqlite3Get4byte(zDbHeader, 36 + 7 * 4) != 0;
#endif
        }
        rc = sqlite3PagerSetPagesize(pBt.pPager, ref pBt.pageSize, nReserve);
        if (rc != 0) goto btree_open_out;
        pBt.usableSize = (u16)(pBt.pageSize - nReserve);
        Debug.Assert((pBt.pageSize & 7) == 0);  /* 8-byte alignment of pageSize */

#if !(SQLITE_OMIT_SHARED_CACHE) && !(SQLITE_OMIT_DISKIO)
/* Add the new BtShared object to the linked list sharable BtShareds.
*/
if( p.sharable ){
sqlite3_mutex *mutexShared;
pBt.nRef = 1;
mutexShared = sqlite3MutexAlloc(SQLITE_MUTEX_STATIC_MASTER);
if( SQLITE_THREADSAFE && sqlite3GlobalConfig.bCoreMutex ){
pBt.mutex = sqlite3MutexAlloc(SQLITE_MUTEX_FAST);
if( pBt.mutex==null ){
rc = SQLITE_NOMEM;
db.mallocFailed = 0;
goto btree_open_out;
}
}
sqlite3_mutex_enter(mutexShared);
pBt.pNext = GLOBAL(BtShared*,sqlite3SharedCacheList);
GLOBAL(BtShared*,sqlite3SharedCacheList) = pBt;
sqlite3_mutex_leave(mutexShared);
}
#endif
      }

#if !(SQLITE_OMIT_SHARED_CACHE) && !(SQLITE_OMIT_DISKIO)
/* If the new Btree uses a sharable pBtShared, then link the new
** Btree into the list of all sharable Btrees for the same connection.
** The list is kept in ascending order by pBt address.
*/
if( p.sharable ){
int i;
Btree pSib;
for(i=0; i<db.nDb; i++){
if( (pSib = db.aDb[i].pBt)!=null && pSib.sharable ){
while( pSib.pPrev ){ pSib = pSib.pPrev; }
if( p.pBt<pSib.pBt ){
p.pNext = pSib;
p.pPrev = 0;
pSib.pPrev = p;
}else{
while( pSib.pNext && pSib.pNext.pBt<p.pBt ){
pSib = pSib.pNext;
}
p.pNext = pSib.pNext;
p.pPrev = pSib;
if( p.pNext ){
p.pNext.pPrev = p;
}
pSib.pNext = p;
}
break;
}
}
}
#endif
      ppBtree = p;

    btree_open_out:
      if (rc != SQLITE_OK)
      {
        if (pBt != null && pBt.pPager != null)
        {
          sqlite3PagerClose(pBt.pPager);
        }
        pBt = null; //    sqlite3_free(ref pBt);
        p = null; //    sqlite3_free(ref p);
        ppBtree = null;
      }
      if (mutexOpen != null)
      {
        Debug.Assert(sqlite3_mutex_held(mutexOpen));
        sqlite3_mutex_leave(mutexOpen);
      }
      return rc;
    }

    /*
    ** Decrement the BtShared.nRef counter.  When it reaches zero,
    ** remove the BtShared structure from the sharing list.  Return
    ** true if the BtShared.nRef counter reaches zero and return
    ** false if it is still positive.
    */
    static bool removeFromSharingList(BtShared pBt)
    {
#if !SQLITE_OMIT_SHARED_CACHE
sqlite3_mutex pMaster;
BtShared pList;
bool removed = false;

Debug.Assert( sqlite3_mutex_notheld(pBt.mutex) );
pMaster = sqlite3MutexAlloc(SQLITE_MUTEX_STATIC_MASTER);
sqlite3_mutex_enter(pMaster);
pBt.nRef--;
if( pBt.nRef<=0 ){
if( GLOBAL(BtShared*,sqlite3SharedCacheList)==pBt ){
GLOBAL(BtShared*,sqlite3SharedCacheList) = pBt.pNext;
}else{
pList = GLOBAL(BtShared*,sqlite3SharedCacheList);
while( ALWAYS(pList) && pList.pNext!=pBt ){
pList=pList.pNext;
}
if( ALWAYS(pList) ){
pList.pNext = pBt.pNext;
}
}
if( SQLITE_THREADSAFE ){
sqlite3_mutex_free(pBt.mutex);
}
removed = true;
}
sqlite3_mutex_leave(pMaster);
return removed;
#else
      return true;
#endif
    }

    /*
    ** Make sure pBt.pTmpSpace points to an allocation of
    ** MX_CELL_SIZE(pBt) bytes.
    */
    static void allocateTempSpace(BtShared pBt)
    {
      if (null == pBt.pTmpSpace)
      {
        pBt.pTmpSpace = new byte[pBt.pageSize]; //sqlite3PageMalloc( pBt.pageSize );
      }
    }

    /*
    ** Free the pBt.pTmpSpace allocation
    */
    static void freeTempSpace(BtShared pBt)
    {
      //sqlite3PageFree(ref pBt.pTmpSpace);
      pBt.pTmpSpace = null;
    }

    /*
** Close an open database and invalidate all cursors.
*/
    static int sqlite3BtreeClose(ref Btree p)
    {
      BtShared pBt = p.pBt;
      BtCursor pCur;

      /* Close all cursors opened via this handle.  */
      Debug.Assert(sqlite3_mutex_held(p.db.mutex));
      sqlite3BtreeEnter(p);
      pCur = pBt.pCursor;
      while (pCur != null)
      {
        BtCursor pTmp = pCur;
        pCur = pCur.pNext;
        if (pTmp.pBtree == p)
        {
          sqlite3BtreeCloseCursor(pTmp);
        }
      }

      /* Rollback any active transaction and free the handle structure.
      ** The call to sqlite3BtreeRollback() drops any table-locks held by
      ** this handle.
      */
      sqlite3BtreeRollback(p);
      sqlite3BtreeLeave(p);

      /* If there are still other outstanding references to the shared-btree
      ** structure, return now. The remainder of this procedure cleans
      ** up the shared-btree.
      */
      Debug.Assert(p.wantToLock == 0 && !p.locked);
      if (!p.sharable || removeFromSharingList(pBt))
      {
        /* The pBt is no longer on the sharing list, so we can access
        ** it without having to hold the mutex.
        **
        ** Clean out and delete the BtShared object.
        */
        Debug.Assert(null == pBt.pCursor);
        sqlite3PagerClose(pBt.pPager);
        if (pBt.xFreeSchema != null && pBt.pSchema != null)
        {
          pBt.xFreeSchema(pBt.pSchema);
        }
        pBt.pSchema = null;// sqlite3_free( ref pBt.pSchema );
        //freeTempSpace(pBt);
        pBt = null; //sqlite3_free(ref pBt);
      }

#if !SQLITE_OMIT_SHARED_CACHE
Debug.Assert( p.wantToLock==null );
Debug.Assert( p.locked==null );
if( p.pPrev ) p.pPrev.pNext = p.pNext;
if( p.pNext ) p.pNext.pPrev = p.pPrev;
#endif

      //sqlite3_free(ref p);
      return SQLITE_OK;
    }

    /*
    ** Change the limit on the number of pages allowed in the cache.
    **
    ** The maximum number of cache pages is set to the absolute
    ** value of mxPage.  If mxPage is negative, the pager will
    ** operate asynchronously - it will not stop to do fsync()s
    ** to insure data is written to the disk surface before
    ** continuing.  Transactions still work if synchronous is off,
    ** and the database cannot be corrupted if this program
    ** crashes.  But if the operating system crashes or there is
    ** an abrupt power failure when synchronous is off, the database
    ** could be left in an inconsistent and unrecoverable state.
    ** Synchronous is on by default so database corruption is not
    ** normally a worry.
    */
    static int sqlite3BtreeSetCacheSize(Btree p, int mxPage)
    {
      BtShared pBt = p.pBt;
      Debug.Assert(sqlite3_mutex_held(p.db.mutex));
      sqlite3BtreeEnter(p);
      sqlite3PagerSetCachesize(pBt.pPager, mxPage);
      sqlite3BtreeLeave(p);
      return SQLITE_OK;
    }

    /*
    ** Change the way data is synced to disk in order to increase or decrease
    ** how well the database resists damage due to OS crashes and power
    ** failures.  Level 1 is the same as asynchronous (no syncs() occur and
    ** there is a high probability of damage)  Level 2 is the default.  There
    ** is a very low but non-zero probability of damage.  Level 3 reduces the
    ** probability of damage to near zero but with a write performance reduction.
    */
#if !SQLITE_OMIT_PAGER_PRAGMAS
    static int sqlite3BtreeSetSafetyLevel(Btree p, int level, int fullSync)
    {
      BtShared pBt = p.pBt;
      Debug.Assert(sqlite3_mutex_held(p.db.mutex));
      sqlite3BtreeEnter(p);
      sqlite3PagerSetSafetyLevel(pBt.pPager, level, fullSync != 0);
      sqlite3BtreeLeave(p);
      return SQLITE_OK;
    }
#endif

    /*
** Return TRUE if the given btree is set to safety level 1.  In other
** words, return TRUE if no sync() occurs on the disk files.
*/
    static int sqlite3BtreeSyncDisabled(Btree p)
    {
      BtShared pBt = p.pBt;
      int rc;
      Debug.Assert(sqlite3_mutex_held(p.db.mutex));
      sqlite3BtreeEnter(p);
      Debug.Assert(pBt != null && pBt.pPager != null);
      rc = sqlite3PagerNosync(pBt.pPager) ? 1 : 0;
      sqlite3BtreeLeave(p);
      return rc;
    }

#if !(SQLITE_OMIT_PAGER_PRAGMAS) || !(SQLITE_OMIT_VACUUM)
    /*
** Change the default pages size and the number of reserved bytes per page.
** Or, if the page size has already been fixed, return SQLITE_READONLY
** without changing anything.
**
** The page size must be a power of 2 between 512 and 65536.  If the page
** size supplied does not meet this constraint then the page size is not
** changed.
**
** Page sizes are constrained to be a power of two so that the region
** of the database file used for locking (beginning at PENDING_BYTE,
** the first byte past the 1GB boundary, 0x40000000) needs to occur
** at the beginning of a page.
**
** If parameter nReserve is less than zero, then the number of reserved
** bytes per page is left unchanged.
**
** If the iFix!=null then the pageSizeFixed flag is set so that the page size
** and autovacuum mode can no longer be changed.
*/
    static int sqlite3BtreeSetPageSize(Btree p, int pageSize, int nReserve, int iFix)
    {
      int rc = SQLITE_OK;
      BtShared pBt = p.pBt;
      Debug.Assert(nReserve >= -1 && nReserve <= 255);
      sqlite3BtreeEnter(p);
      if (pBt.pageSizeFixed)
      {
        sqlite3BtreeLeave(p);
        return SQLITE_READONLY;
      }
      if (nReserve < 0)
      {
        nReserve = pBt.pageSize - pBt.usableSize;
      }
      Debug.Assert(nReserve >= 0 && nReserve <= 255);
      if (pageSize >= 512 && pageSize <= SQLITE_MAX_PAGE_SIZE &&
      ((pageSize - 1) & pageSize) == 0)
      {
        Debug.Assert((pageSize & 7) == 0);
        Debug.Assert(null == pBt.pPage1 && null == pBt.pCursor);
        pBt.pageSize = (u16)pageSize;
        //        freeTempSpace(pBt);
      }
      rc = sqlite3PagerSetPagesize(pBt.pPager, ref pBt.pageSize, nReserve);
      pBt.usableSize = (u16)(pBt.pageSize - nReserve);
      if (iFix != 0) pBt.pageSizeFixed = true;
      sqlite3BtreeLeave(p);
      return rc;
    }

    /*
    ** Return the currently defined page size
    */
    static int sqlite3BtreeGetPageSize(Btree p)
    {
      return p.pBt.pageSize;
    }

    /*
    ** Return the number of bytes of space at the end of every page that
    ** are intentually left unused.  This is the "reserved" space that is
    ** sometimes used by extensions.
    */
    static int sqlite3BtreeGetReserve(Btree p)
    {
      int n;
      sqlite3BtreeEnter(p);
      n = p.pBt.pageSize - p.pBt.usableSize;
      sqlite3BtreeLeave(p);
      return n;
    }

    /*
    ** Set the maximum page count for a database if mxPage is positive.
    ** No changes are made if mxPage is 0 or negative.
    ** Regardless of the value of mxPage, return the maximum page count.
    */
    static int sqlite3BtreeMaxPageCount(Btree p, int mxPage)
    {
      int n;
      sqlite3BtreeEnter(p);
      n = (int)sqlite3PagerMaxPageCount(p.pBt.pPager, mxPage);
      sqlite3BtreeLeave(p);
      return n;
    }
#endif //* !(SQLITE_OMIT_PAGER_PRAGMAS) || !(SQLITE_OMIT_VACUUM) */

    /*
** Change the 'auto-vacuum' property of the database. If the 'autoVacuum'
** parameter is non-zero, then auto-vacuum mode is enabled. If zero, it
** is disabled. The default value for the auto-vacuum property is
** determined by the SQLITE_DEFAULT_AUTOVACUUM macro.
*/
    static int sqlite3BtreeSetAutoVacuum(Btree p, int autoVacuum)
    {
#if SQLITE_OMIT_AUTOVACUUM
return SQLITE_READONLY;
#else
      BtShared pBt = p.pBt;
      int rc = SQLITE_OK;
      u8 av = (u8)autoVacuum;

      sqlite3BtreeEnter(p);
      if (pBt.pageSizeFixed && (av != 0) != pBt.autoVacuum)
      {
        rc = SQLITE_READONLY;
      }
      else
      {
        pBt.autoVacuum = av != 0;
        pBt.incrVacuum = av == 2;
      }
      sqlite3BtreeLeave(p);
      return rc;
#endif
    }

    /*
    ** Return the value of the 'auto-vacuum' property. If auto-vacuum is
    ** enabled 1 is returned. Otherwise 0.
    */
    static int sqlite3BtreeGetAutoVacuum(Btree p)
    {
#if SQLITE_OMIT_AUTOVACUUM
return BTREE_AUTOVACUUM_NONE;
#else
      int rc;
      sqlite3BtreeEnter(p);
      rc = (
      (!p.pBt.autoVacuum) ? BTREE_AUTOVACUUM_NONE :
      (!p.pBt.incrVacuum) ? BTREE_AUTOVACUUM_FULL :
      BTREE_AUTOVACUUM_INCR
      );
      sqlite3BtreeLeave(p);
      return rc;
#endif
    }


    /*
    ** Get a reference to pPage1 of the database file.  This will
    ** also acquire a readlock on that file.
    **
    ** SQLITE_OK is returned on success.  If the file is not a
    ** well-formed database file, then SQLITE_CORRUPT is returned.
    ** SQLITE_BUSY is returned if the database is locked.  SQLITE_NOMEM
    ** is returned if we run out of memory.
    */
    static int lockBtree(BtShared pBt)
    {
      int rc;
      MemPage pPage1 = new MemPage();
      int nPage = 0;

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      Debug.Assert(pBt.pPage1 == null);
      rc = sqlite3PagerSharedLock(pBt.pPager);
      if (rc != SQLITE_OK) return rc;
      rc = btreeGetPage(pBt, 1, ref pPage1, 0);
      if (rc != SQLITE_OK) return rc;

      /* Do some checking to help insure the file we opened really is
      ** a valid database file.
      */
      rc = sqlite3PagerPagecount(pBt.pPager, ref nPage);
      if (rc != SQLITE_OK)
      {
        goto page1_init_failed;
      }
      else if (nPage > 0)
      {
        int pageSize;
        int usableSize;
        u8[] page1 = pPage1.aData;
        rc = SQLITE_NOTADB;
        if (memcmp(page1, zMagicHeader, 16) != 0)
        {
          goto page1_init_failed;
        }
        if (page1[18] > 1)
        {
          pBt.readOnly = true;
        }
        if (page1[19] > 1)
        {
          goto page1_init_failed;
        }

        /* The maximum embedded fraction must be exactly 25%.  And the minimum
        ** embedded fraction must be 12.5% for both leaf-data and non-leaf-data.
        ** The original design allowed these amounts to vary, but as of
        ** version 3.6.0, we require them to be fixed.
        */
        if (memcmp(page1, 21, "\x0040\x0020\x0020", 3) != 0)//   "\100\040\040"
        {
          goto page1_init_failed;
        }
        pageSize = get2byte(page1, 16);
        if (((pageSize - 1) & pageSize) != 0 || pageSize < 512 ||
        (SQLITE_MAX_PAGE_SIZE < 32768 && pageSize > SQLITE_MAX_PAGE_SIZE)
        )
        {
          goto page1_init_failed;
        }
        Debug.Assert((pageSize & 7) == 0);
        usableSize = pageSize - page1[20];
        if (pageSize != pBt.pageSize)
        {
          /* After reading the first page of the database assuming a page size
          ** of BtShared.pageSize, we have discovered that the page-size is
          ** actually pageSize. Unlock the database, leave pBt.pPage1 at
          ** zero and return SQLITE_OK. The caller will call this function
          ** again with the correct page-size.
          */
          releasePage(pPage1);
          pBt.usableSize = (u16)usableSize;
          pBt.pageSize = (u16)pageSize;
          //          freeTempSpace(pBt);
          rc = sqlite3PagerSetPagesize(pBt.pPager, ref pBt.pageSize,
          pageSize - usableSize);
          return rc;
        }
        if (usableSize < 480)
        {
          goto page1_init_failed;
        }
        pBt.pageSize = (u16)pageSize;
        pBt.usableSize = (u16)usableSize;
#if !SQLITE_OMIT_AUTOVACUUM
        pBt.autoVacuum = (sqlite3Get4byte(page1, 36 + 4 * 4) != 0);
        pBt.incrVacuum = (sqlite3Get4byte(page1, 36 + 7 * 4) != 0);
#endif
      }

      /* maxLocal is the maximum amount of payload to store locally for
      ** a cell.  Make sure it is small enough so that at least minFanout
      ** cells can will fit on one page.  We assume a 10-byte page header.
      ** Besides the payload, the cell must store:
      **     2-byte pointer to the cell
      **     4-byte child pointer
      **     9-byte nKey value
      **     4-byte nData value
      **     4-byte overflow page pointer
      ** So a cell consists of a 2-byte poiner, a header which is as much as
      ** 17 bytes long, 0 to N bytes of payload, and an optional 4 byte overflow
      ** page pointer.
      */
      pBt.maxLocal = (u16)((pBt.usableSize - 12) * 64 / 255 - 23);
      pBt.minLocal = (u16)((pBt.usableSize - 12) * 32 / 255 - 23);
      pBt.maxLeaf = (u16)(pBt.usableSize - 35);
      pBt.minLeaf = (u16)((pBt.usableSize - 12) * 32 / 255 - 23);
      Debug.Assert(pBt.maxLeaf + 23 <= MX_CELL_SIZE(pBt));
      pBt.pPage1 = pPage1;
      return SQLITE_OK;

    page1_init_failed:
      releasePage(pPage1);
      pBt.pPage1 = null;
      return rc;
    }

    /*
    ** If there are no outstanding cursors and we are not in the middle
    ** of a transaction but there is a read lock on the database, then
    ** this routine unrefs the first page of the database file which
    ** has the effect of releasing the read lock.
    **
    ** If there is a transaction in progress, this routine is a no-op.
    */
    static void unlockBtreeIfUnused(BtShared pBt)
    {
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      Debug.Assert(pBt.pCursor == null || pBt.inTransaction > TRANS_NONE);
      if (pBt.inTransaction == TRANS_NONE && pBt.pPage1 != null)
      {
        Debug.Assert(pBt.pPage1.aData != null);
        Debug.Assert(sqlite3PagerRefcount(pBt.pPager) == 1);
        Debug.Assert(pBt.pPage1.aData != null);
        releasePage(pBt.pPage1);
        pBt.pPage1 = null;
      }
    }

    /*
    ** If pBt points to an empty file then convert that empty file
    ** into a new empty database by initializing the first page of
    ** the database.
    */
    static int newDatabase(BtShared pBt)
    {
      MemPage pP1;
      byte[] data;
      int rc;
      int nPage = 0;

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      /* The database size has already been measured and cached, so failure
      ** is impossible here.  If the original size measurement failed, then
      ** processing aborts before entering this routine. */
      rc = sqlite3PagerPagecount(pBt.pPager, ref nPage);
      if (NEVER(rc != SQLITE_OK) || nPage > 0)
      {
        return rc;
      }
      pP1 = pBt.pPage1;
      Debug.Assert(pP1 != null);
      data = pP1.aData;
      rc = sqlite3PagerWrite(pP1.pDbPage);
      if (rc != 0) return rc;
      Buffer.BlockCopy(Encoding.UTF8.GetBytes(zMagicHeader), 0, data, 0, 16);// memcpy(data, zMagicHeader, sizeof(zMagicHeader));
      Debug.Assert(zMagicHeader.Length == 16);
      put2byte(data, 16, pBt.pageSize);
      data[18] = 1;
      data[19] = 1;
      Debug.Assert(pBt.usableSize <= pBt.pageSize && pBt.usableSize + 255 >= pBt.pageSize);
      data[20] = (u8)(pBt.pageSize - pBt.usableSize);
      data[21] = 64;
      data[22] = 32;
      data[23] = 32;
      //memset(&data[24], 0, 100-24);
      zeroPage(pP1, PTF_INTKEY | PTF_LEAF | PTF_LEAFDATA);
      pBt.pageSizeFixed = true;
#if !SQLITE_OMIT_AUTOVACUUM
      Debug.Assert(pBt.autoVacuum == true || pBt.autoVacuum == false);
      Debug.Assert(pBt.incrVacuum == true || pBt.incrVacuum == false);
      sqlite3Put4byte(data, 36 + 4 * 4, pBt.autoVacuum ? 1 : 0);
      sqlite3Put4byte(data, 36 + 7 * 4, pBt.incrVacuum ? 1 : 0);
#endif
      return SQLITE_OK;
    }

    /*
    ** Attempt to start a new transaction. A write-transaction
    ** is started if the second argument is nonzero, otherwise a read-
    ** transaction.  If the second argument is 2 or more and exclusive
    ** transaction is started, meaning that no other process is allowed
    ** to access the database.  A preexisting transaction may not be
    ** upgraded to exclusive by calling this routine a second time - the
    ** exclusivity flag only works for a new transaction.
    **
    ** A write-transaction must be started before attempting any
    ** changes to the database.  None of the following routines
    ** will work unless a transaction is started first:
    **
    **      sqlite3BtreeCreateTable()
    **      sqlite3BtreeCreateIndex()
    **      sqlite3BtreeClearTable()
    **      sqlite3BtreeDropTable()
    **      sqlite3BtreeInsert()
    **      sqlite3BtreeDelete()
    **      sqlite3BtreeUpdateMeta()
    **
    ** If an initial attempt to acquire the lock fails because of lock contention
    ** and the database was previously unlocked, then invoke the busy handler
    ** if there is one.  But if there was previously a read-lock, do not
    ** invoke the busy handler - just return SQLITE_BUSY.  SQLITE_BUSY is
    ** returned when there is already a read-lock in order to avoid a deadlock.
    **
    ** Suppose there are two processes A and B.  A has a read lock and B has
    ** a reserved lock.  B tries to promote to exclusive but is blocked because
    ** of A's read lock.  A tries to promote to reserved but is blocked by B.
    ** One or the other of the two processes must give way or there can be
    ** no progress.  By returning SQLITE_BUSY and not invoking the busy callback
    ** when A already has a read lock, we encourage A to give up and let B
    ** proceed.
    */
    static int sqlite3BtreeBeginTrans(Btree p, int wrflag)
    {
      BtShared pBt = p.pBt;
      int rc = SQLITE_OK;

      sqlite3BtreeEnter(p);
      btreeIntegrity(p);

      /* If the btree is already in a write-transaction, or it
      ** is already in a read-transaction and a read-transaction
      ** is requested, this is a no-op.
      */
      if (p.inTrans == TRANS_WRITE || (p.inTrans == TRANS_READ && 0 == wrflag))
      {
        goto trans_begun;
      }

      /* Write transactions are not possible on a read-only database */
      if (pBt.readOnly && wrflag != 0)
      {
        rc = SQLITE_READONLY;
        goto trans_begun;
      }

#if !SQLITE_OMIT_SHARED_CACHE
/* If another database handle has already opened a write transaction
** on this shared-btree structure and a second write transaction is
** requested, return SQLITE_LOCKED.
*/
if( (wrflag && pBt.inTransaction==TRANS_WRITE) || pBt.isPending ){
sqlite3 pBlock = pBt.pWriter.db;
}else if( wrflag>1 ){
BtLock pIter;
for(pIter=pBt.pLock; pIter; pIter=pIter.pNext){
if( pIter.pBtree!=p ){
pBlock = pIter.pBtree.db;
break;
}
}
}
if( pBlock ){
sqlite3ConnectionBlocked(p.db, pBlock);
rc = SQLITE_LOCKED_SHAREDCACHE;
goto trans_begun;
}
#endif

      /* Any read-only or read-write transaction implies a read-lock on
** page 1. So if some other shared-cache client already has a write-lock
** on page 1, the transaction cannot be opened. */
      rc = querySharedCacheTableLock(p, MASTER_ROOT, READ_LOCK);
      if (SQLITE_OK != rc) goto trans_begun;

      do
      {
        /* Call lockBtree() until either pBt.pPage1 is populated or
        ** lockBtree() returns something other than SQLITE_OK. lockBtree()
        ** may return SQLITE_OK but leave pBt.pPage1 set to 0 if after
        ** reading page 1 it discovers that the page-size of the database
        ** file is not pBt.pageSize. In this case lockBtree() will update
        ** pBt.pageSize to the page-size of the file on disk.
        */
        while (pBt.pPage1 == null && SQLITE_OK == (rc = lockBtree(pBt))) ;

        if (rc == SQLITE_OK && wrflag != 0)
        {
          if (pBt.readOnly)
          {
            rc = SQLITE_READONLY;
          }
          else
          {
            rc = sqlite3PagerBegin(pBt.pPager, wrflag > 1, sqlite3TempInMemory(p.db) ? 1 : 0);
            if (rc == SQLITE_OK)
            {
              rc = newDatabase(pBt);
            }
          }
        }

        if (rc != SQLITE_OK)
        {
          unlockBtreeIfUnused(pBt);
        }
      } while (rc == SQLITE_BUSY && pBt.inTransaction == TRANS_NONE &&
      btreeInvokeBusyHandler(pBt) != 0);

      if (rc == SQLITE_OK)
      {
        if (p.inTrans == TRANS_NONE)
        {
          pBt.nTransaction++;
#if !SQLITE_OMIT_SHARED_CACHE
if( p.sharable ){
Debug.Assert( p.lock.pBtree==p && p.lock.iTable==1 );
p.lock.eLock = READ_LOCK;
p.lock.pNext = pBt.pLock;
pBt.pLock = &p.lock;
}
#endif
        }
        p.inTrans = (wrflag != 0 ? TRANS_WRITE : TRANS_READ);
        if (p.inTrans > pBt.inTransaction)
        {
          pBt.inTransaction = p.inTrans;
        }
#if !SQLITE_OMIT_SHARED_CACHE
if( wrflag ){
Debug.Assert( !pBt.pWriter );
pBt.pWriter = p;
pBt.isExclusive = (u8)(wrflag>1);
}
#endif
      }


    trans_begun:
      if (rc == SQLITE_OK && wrflag != 0)
      {
        /* This call makes sure that the pager has the correct number of
        ** open savepoints. If the second parameter is greater than 0 and
        ** the sub-journal is not already open, then it will be opened here.
        */
        rc = sqlite3PagerOpenSavepoint(pBt.pPager, p.db.nSavepoint);
      }

      btreeIntegrity(p);
      sqlite3BtreeLeave(p);
      return rc;
    }

#if !SQLITE_OMIT_AUTOVACUUM

    /*
** Set the pointer-map entries for all children of page pPage. Also, if
** pPage contains cells that point to overflow pages, set the pointer
** map entries for the overflow pages as well.
*/
    static int setChildPtrmaps(MemPage pPage)
    {
      int i;                             /* Counter variable */
      int nCell;                         /* Number of cells in page pPage */
      int rc;                            /* Return code */
      BtShared pBt = pPage.pBt;
      u8 isInitOrig = pPage.isInit;
      Pgno pgno = pPage.pgno;

      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      rc = btreeInitPage(pPage);
      if (rc != SQLITE_OK)
      {
        goto set_child_ptrmaps_out;
      }
      nCell = pPage.nCell;

      for (i = 0; i < nCell; i++)
      {
        int pCell = findCell(pPage, i);

        ptrmapPutOvflPtr(pPage, pCell, ref rc);

        if (0 == pPage.leaf)
        {
          Pgno childPgno = sqlite3Get4byte(pPage.aData, pCell);
          ptrmapPut(pBt, childPgno, PTRMAP_BTREE, pgno, ref rc);
        }
      }

      if (0 == pPage.leaf)
      {
        Pgno childPgno = sqlite3Get4byte(pPage.aData, pPage.hdrOffset + 8);
        ptrmapPut(pBt, childPgno, PTRMAP_BTREE, pgno, ref rc);
      }

    set_child_ptrmaps_out:
      pPage.isInit = isInitOrig;
      return rc;
    }

    /*
    ** Somewhere on pPage is a pointer to page iFrom.  Modify this pointer so
    ** that it points to iTo. Parameter eType describes the type of pointer to
    ** be modified, as  follows:
    **
    ** PTRMAP_BTREE:     pPage is a btree-page. The pointer points at a child
    **                   page of pPage.
    **
    ** PTRMAP_OVERFLOW1: pPage is a btree-page. The pointer points at an overflow
    **                   page pointed to by one of the cells on pPage.
    **
    ** PTRMAP_OVERFLOW2: pPage is an overflow-page. The pointer points at the next
    **                   overflow page in the list.
    */
    static int modifyPagePointer(MemPage pPage, Pgno iFrom, Pgno iTo, u8 eType)
    {
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
      if (eType == PTRMAP_OVERFLOW2)
      {
        /* The pointer is always the first 4 bytes of the page in this case.  */
        if (sqlite3Get4byte(pPage.aData) != iFrom)
        {
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
        sqlite3Put4byte(pPage.aData, iTo);
      }
      else
      {
        u8 isInitOrig = pPage.isInit;
        int i;
        int nCell;

        btreeInitPage(pPage);
        nCell = pPage.nCell;

        for (i = 0; i < nCell; i++)
        {
          int pCell = findCell(pPage, i);
          if (eType == PTRMAP_OVERFLOW1)
          {
            CellInfo info = new CellInfo();
            btreeParseCellPtr( pPage, pCell, ref info );
            if (info.iOverflow != 0)
            {
              if (iFrom == sqlite3Get4byte(pPage.aData, pCell, info.iOverflow))
              {
                sqlite3Put4byte(pPage.aData, pCell + info.iOverflow, (int)iTo);
                break;
              }
            }
          }
          else
          {
            if (sqlite3Get4byte(pPage.aData, pCell) == iFrom)
            {
              sqlite3Put4byte(pPage.aData, pCell, (int)iTo);
              break;
            }
          }
        }

        if (i == nCell)
        {
          if (eType != PTRMAP_BTREE ||
          sqlite3Get4byte(pPage.aData, pPage.hdrOffset + 8) != iFrom)
          {
#if SQLITE_DEBUG || DEBUG
            return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
          }
          sqlite3Put4byte(pPage.aData, pPage.hdrOffset + 8, iTo);
        }

        pPage.isInit = isInitOrig;
      }
      return SQLITE_OK;
    }


    /*
    ** Move the open database page pDbPage to location iFreePage in the
    ** database. The pDbPage reference remains valid.
    **
    ** The isCommit flag indicates that there is no need to remember that
    ** the journal needs to be sync()ed before database page pDbPage.pgno
    ** can be written to. The caller has already promised not to write to that
    ** page.
    */
    static int relocatePage(
    BtShared pBt,           /* Btree */
    MemPage pDbPage,        /* Open page to move */
    u8 eType,                /* Pointer map 'type' entry for pDbPage */
    Pgno iPtrPage,           /* Pointer map 'page-no' entry for pDbPage */
    Pgno iFreePage,          /* The location to move pDbPage to */
    int isCommit             /* isCommit flag passed to sqlite3PagerMovepage */
    )
    {
      MemPage pPtrPage = new MemPage();   /* The page that contains a pointer to pDbPage */
      Pgno iDbPage = pDbPage.pgno;
      Pager pPager = pBt.pPager;
      int rc;

      Debug.Assert(eType == PTRMAP_OVERFLOW2 || eType == PTRMAP_OVERFLOW1 ||
      eType == PTRMAP_BTREE || eType == PTRMAP_ROOTPAGE);
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      Debug.Assert(pDbPage.pBt == pBt);

      /* Move page iDbPage from its current location to page number iFreePage */
      TRACE("AUTOVACUUM: Moving %d to free page %d (ptr page %d type %d)\n",
      iDbPage, iFreePage, iPtrPage, eType);
      rc = sqlite3PagerMovepage(pPager, pDbPage.pDbPage, iFreePage, isCommit);
      if (rc != SQLITE_OK)
      {
        return rc;
      }
      pDbPage.pgno = iFreePage;

      /* If pDbPage was a btree-page, then it may have child pages and/or cells
      ** that point to overflow pages. The pointer map entries for all these
      ** pages need to be changed.
      **
      ** If pDbPage is an overflow page, then the first 4 bytes may store a
      ** pointer to a subsequent overflow page. If this is the case, then
      ** the pointer map needs to be updated for the subsequent overflow page.
      */
      if (eType == PTRMAP_BTREE || eType == PTRMAP_ROOTPAGE)
      {
        rc = setChildPtrmaps(pDbPage);
        if (rc != SQLITE_OK)
        {
          return rc;
        }
      }
      else
      {
        Pgno nextOvfl = sqlite3Get4byte(pDbPage.aData);
        if (nextOvfl != 0)
        {
          ptrmapPut(pBt, nextOvfl, PTRMAP_OVERFLOW2, iFreePage, ref rc);
          if (rc != SQLITE_OK)
          {
            return rc;
          }
        }
      }

      /* Fix the database pointer on page iPtrPage that pointed at iDbPage so
      ** that it points at iFreePage. Also fix the pointer map entry for
      ** iPtrPage.
      */
      if (eType != PTRMAP_ROOTPAGE)
      {
        rc = btreeGetPage(pBt, iPtrPage, ref pPtrPage, 0);
        if (rc != SQLITE_OK)
        {
          return rc;
        }
        rc = sqlite3PagerWrite(pPtrPage.pDbPage);
        if (rc != SQLITE_OK)
        {
          releasePage(pPtrPage);
          return rc;
        }
        rc = modifyPagePointer(pPtrPage, iDbPage, iFreePage, eType);
        releasePage(pPtrPage);
        if (rc == SQLITE_OK)
        {
          ptrmapPut(pBt, iFreePage, eType, iPtrPage, ref rc);
        }
      }
      return rc;
    }

    /* Forward declaration required by incrVacuumStep(). */
    //static int allocateBtreePage(BtShared *, MemPage **, Pgno *, Pgno, u8);

    /*
    ** Perform a single step of an incremental-vacuum. If successful,
    ** return SQLITE_OK. If there is no work to do (and therefore no
    ** point in calling this function again), return SQLITE_DONE.
    **
    ** More specificly, this function attempts to re-organize the
    ** database so that the last page of the file currently in use
    ** is no longer in use.
    **
    ** If the nFin parameter is non-zero, this function assumes
    ** that the caller will keep calling incrVacuumStep() until
    ** it returns SQLITE_DONE or an error, and that nFin is the
    ** number of pages the database file will contain after this
    ** process is complete.  If nFin is zero, it is assumed that
    ** incrVacuumStep() will be called a finite amount of times
    ** which may or may not empty the freelist.  A full autovacuum
    ** has nFin>0.  A "PRAGMA incremental_vacuum" has nFin==null.
    */
    static int incrVacuumStep(BtShared pBt, Pgno nFin, Pgno iLastPg)
    {
      Pgno nFreeList;           /* Number of pages still on the free-list */

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      Debug.Assert(iLastPg > nFin);

      if (!PTRMAP_ISPAGE(pBt, iLastPg) && iLastPg != PENDING_BYTE_PAGE(pBt))
      {
        int rc;
        u8 eType = 0;
        Pgno iPtrPage = 0;

        nFreeList = sqlite3Get4byte(pBt.pPage1.aData, 36);
        if (nFreeList == 0)
        {
          return SQLITE_DONE;
        }

        rc = ptrmapGet(pBt, iLastPg, ref eType, ref iPtrPage);
        if (rc != SQLITE_OK)
        {
          return rc;
        }
        if (eType == PTRMAP_ROOTPAGE)
        {
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }

        if (eType == PTRMAP_FREEPAGE)
        {
          if (nFin == 0)
          {
            /* Remove the page from the files free-list. This is not required
            ** if nFin is non-zero. In that case, the free-list will be
            ** truncated to zero after this function returns, so it doesn't
            ** matter if it still contains some garbage entries.
            */
            Pgno iFreePg = 0;
            MemPage pFreePg = new MemPage();
            rc = allocateBtreePage(pBt, ref pFreePg, ref iFreePg, iLastPg, 1);
            if (rc != SQLITE_OK)
            {
              return rc;
            }
            Debug.Assert(iFreePg == iLastPg);
            releasePage(pFreePg);
          }
        }
        else
        {
          Pgno iFreePg = 0;             /* Index of free page to move pLastPg to */
          MemPage pLastPg = new MemPage();

          rc = btreeGetPage(pBt, iLastPg, ref pLastPg, 0);
          if (rc != SQLITE_OK)
          {
            return rc;
          }

          /* If nFin is zero, this loop runs exactly once and page pLastPg
          ** is swapped with the first free page pulled off the free list.
          **
          ** On the other hand, if nFin is greater than zero, then keep
          ** looping until a free-page located within the first nFin pages
          ** of the file is found.
          */
          do
          {
            MemPage pFreePg = new MemPage();
            rc = allocateBtreePage(pBt, ref pFreePg, ref iFreePg, 0, 0);
            if (rc != SQLITE_OK)
            {
              releasePage(pLastPg);
              return rc;
            }
            releasePage(pFreePg);
          } while (nFin != 0 && iFreePg > nFin);
          Debug.Assert(iFreePg < iLastPg);

          rc = sqlite3PagerWrite(pLastPg.pDbPage);
          if (rc == SQLITE_OK)
          {
            rc = relocatePage(pBt, pLastPg, eType, iPtrPage, iFreePg, (nFin != 0) ? 1 : 0);
          }
          releasePage(pLastPg);
          if (rc != SQLITE_OK)
          {
            return rc;
          }
        }
      }

      if (nFin == 0)
      {
        iLastPg--;
        while (iLastPg == PENDING_BYTE_PAGE(pBt) || PTRMAP_ISPAGE(pBt, iLastPg))
        {
          if (PTRMAP_ISPAGE(pBt, iLastPg))
          {
            MemPage pPg = new MemPage();
            int rc = btreeGetPage(pBt, iLastPg, ref pPg, 0);
            if (rc != SQLITE_OK)
            {
              return rc;
            }
            rc = sqlite3PagerWrite(pPg.pDbPage);
            releasePage(pPg);
            if (rc != SQLITE_OK)
            {
              return rc;
            }
          }
          iLastPg--;
        }
        sqlite3PagerTruncateImage(pBt.pPager, iLastPg);
      }
      return SQLITE_OK;
    }

    /*
    ** A write-transaction must be opened before calling this function.
    ** It performs a single unit of work towards an incremental vacuum.
    **
    ** If the incremental vacuum is finished after this function has run,
    ** SQLITE_DONE is returned. If it is not finished, but no error occurred,
    ** SQLITE_OK is returned. Otherwise an SQLite error code.
    */
    static int sqlite3BtreeIncrVacuum(Btree p)
    {
      int rc;
      BtShared pBt = p.pBt;

      sqlite3BtreeEnter(p);
      Debug.Assert(pBt.inTransaction == TRANS_WRITE && p.inTrans == TRANS_WRITE);
      if (!pBt.autoVacuum)
      {
        rc = SQLITE_DONE;
      }
      else
      {
        invalidateAllOverflowCache(pBt);
        rc = incrVacuumStep(pBt, 0, pagerPagecount(pBt));
      }
      sqlite3BtreeLeave(p);
      return rc;
    }

    /*
    ** This routine is called prior to sqlite3PagerCommit when a transaction
    ** is commited for an auto-vacuum database.
    **
    ** If SQLITE_OK is returned, then pnTrunc is set to the number of pages
    ** the database file should be truncated to during the commit process.
    ** i.e. the database has been reorganized so that only the first pnTrunc
    ** pages are in use.
    */
    static int autoVacuumCommit(BtShared pBt)
    {
      int rc = SQLITE_OK;
      Pager pPager = pBt.pPager;
      // VVA_ONLY( int nRef = sqlite3PagerRefcount(pPager) );
#if !NDEBUG || DEBUG
      int nRef = sqlite3PagerRefcount(pPager);
#else
int nRef=0;
#endif


      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      invalidateAllOverflowCache(pBt);
      Debug.Assert(pBt.autoVacuum);
      if (!pBt.incrVacuum)
      {
        Pgno nFin;         /* Number of pages in database after autovacuuming */
        Pgno nFree;        /* Number of pages on the freelist initially */
        Pgno nPtrmap;      /* Number of PtrMap pages to be freed */
        Pgno iFree;        /* The next page to be freed */
        int nEntry;        /* Number of entries on one ptrmap page */
        Pgno nOrig;        /* Database size before freeing */

        nOrig = pagerPagecount(pBt);
        if (PTRMAP_ISPAGE(pBt, nOrig) || nOrig == PENDING_BYTE_PAGE(pBt))
        {
          /* It is not possible to create a database for which the final page
          ** is either a pointer-map page or the pending-byte page. If one
          ** is encountered, this indicates corruption.
          */
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }

        nFree = sqlite3Get4byte(pBt.pPage1.aData, 36);
        nEntry = pBt.usableSize / 5;
        nPtrmap = (Pgno)(( nFree - nOrig + PTRMAP_PAGENO( pBt, nOrig ) + (Pgno)nEntry ) / nEntry);
        nFin = nOrig - nFree - nPtrmap;
        if (nOrig > PENDING_BYTE_PAGE(pBt) && nFin < PENDING_BYTE_PAGE(pBt))
        {
          nFin--;
        }
        while (PTRMAP_ISPAGE(pBt, nFin) || nFin == PENDING_BYTE_PAGE(pBt))
        {
          nFin--;
        }
        if (nFin > nOrig)
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif

        for (iFree = nOrig; iFree > nFin && rc == SQLITE_OK; iFree--)
        {
          rc = incrVacuumStep(pBt, nFin, iFree);
        }
        if ((rc == SQLITE_DONE || rc == SQLITE_OK) && nFree > 0)
        {
          rc = SQLITE_OK;
          rc = sqlite3PagerWrite(pBt.pPage1.pDbPage);
          sqlite3Put4byte(pBt.pPage1.aData, 32, 0);
          sqlite3Put4byte(pBt.pPage1.aData, 36, 0);
          sqlite3PagerTruncateImage(pBt.pPager, nFin);
        }
        if (rc != SQLITE_OK)
        {
          sqlite3PagerRollback(pPager);
        }
      }

      Debug.Assert(nRef == sqlite3PagerRefcount(pPager));
      return rc;
    }

#else //* ifndef SQLITE_OMIT_AUTOVACUUM */
//# define setChildPtrmaps(x) SQLITE_OK
#endif

    /*
** This routine does the first phase of a two-phase commit.  This routine
** causes a rollback journal to be created (if it does not already exist)
** and populated with enough information so that if a power loss occurs
** the database can be restored to its original state by playing back
** the journal.  Then the contents of the journal are flushed out to
** the disk.  After the journal is safely on oxide, the changes to the
** database are written into the database file and flushed to oxide.
** At the end of this call, the rollback journal still exists on the
** disk and we are still holding all locks, so the transaction has not
** committed.  See sqlite3BtreeCommitPhaseTwo() for the second phase of the
** commit process.
**
** This call is a no-op if no write-transaction is currently active on pBt.
**
** Otherwise, sync the database file for the btree pBt. zMaster points to
** the name of a master journal file that should be written into the
** individual journal file, or is NULL, indicating no master journal file
** (single database transaction).
**
** When this is called, the master journal should already have been
** created, populated with this journal pointer and synced to disk.
**
** Once this is routine has returned, the only thing required to commit
** the write-transaction for this database file is to delete the journal.
*/
    static int sqlite3BtreeCommitPhaseOne(Btree p, string zMaster)
    {
      int rc = SQLITE_OK;
      if (p.inTrans == TRANS_WRITE)
      {
        BtShared pBt = p.pBt;
        sqlite3BtreeEnter(p);
#if !SQLITE_OMIT_AUTOVACUUM
        if (pBt.autoVacuum)
        {
          rc = autoVacuumCommit(pBt);
          if (rc != SQLITE_OK)
          {
            sqlite3BtreeLeave(p);
            return rc;
          }
        }
#endif
        rc = sqlite3PagerCommitPhaseOne(pBt.pPager, zMaster, false);
        sqlite3BtreeLeave(p);
      }
      return rc;
    }

    /*
    ** This function is called from both BtreeCommitPhaseTwo() and BtreeRollback()
    ** at the conclusion of a transaction.
    */
    static void btreeEndTransaction(Btree p)
    {
      BtShared pBt = p.pBt;
      BtCursor pCsr;
      Debug.Assert(sqlite3BtreeHoldsMutex(p));

      /* Search for a cursor held open by this b-tree connection. If one exists,
      ** then the transaction will be downgraded to a read-only transaction
      ** instead of actually concluded. A subsequent call to CommitPhaseTwo()
      ** or Rollback() will finish the transaction and unlock the database.  */
      for (pCsr = pBt.pCursor; pCsr != null && pCsr.pBtree != p; pCsr = pCsr.pNext) ;
      Debug.Assert(pCsr == null || p.inTrans > TRANS_NONE);

      btreeClearHasContent(pBt);
      if (pCsr != null)
      {
        downgradeAllSharedCacheTableLocks(p);
        p.inTrans = TRANS_READ;
      }
      else
      {
        /* If the handle had any kind of transaction open, decrement the
        ** transaction count of the shared btree. If the transaction count
        ** reaches 0, set the shared state to TRANS_NONE. The unlockBtreeIfUnused()
        ** call below will unlock the pager.  */
        if (p.inTrans != TRANS_NONE)
        {
          clearAllSharedCacheTableLocks(p);
          pBt.nTransaction--;
          if (0 == pBt.nTransaction)
          {
            pBt.inTransaction = TRANS_NONE;
          }
        }

        /* Set the current transaction state to TRANS_NONE and unlock the
        ** pager if this call closed the only read or write transaction.  */
        p.inTrans = TRANS_NONE;
        unlockBtreeIfUnused(pBt);
      }

      btreeIntegrity(p);
    }

    /*
    ** Commit the transaction currently in progress.
    **
    ** This routine implements the second phase of a 2-phase commit.  The
    ** sqlite3BtreeCommitPhaseOne() routine does the first phase and should
    ** be invoked prior to calling this routine.  The sqlite3BtreeCommitPhaseOne()
    ** routine did all the work of writing information out to disk and flushing the
    ** contents so that they are written onto the disk platter.  All this
    ** routine has to do is delete or truncate or zero the header in the
    ** the rollback journal (which causes the transaction to commit) and
    ** drop locks.
    **
    ** This will release the write lock on the database file.  If there
    ** are no active cursors, it also releases the read lock.
    */
    static int sqlite3BtreeCommitPhaseTwo(Btree p)
    {
      BtShared pBt = p.pBt;

      sqlite3BtreeEnter(p);
      btreeIntegrity(p);

      /* If the handle has a write-transaction open, commit the shared-btrees
      ** transaction and set the shared state to TRANS_READ.
      */
      if (p.inTrans == TRANS_WRITE)
      {
        int rc;
        Debug.Assert(pBt.inTransaction == TRANS_WRITE);
        Debug.Assert(pBt.nTransaction > 0);
        rc = sqlite3PagerCommitPhaseTwo(pBt.pPager);
        if (rc != SQLITE_OK)
        {
          sqlite3BtreeLeave(p);
          return rc;
        }
        pBt.inTransaction = TRANS_READ;
      }

      btreeEndTransaction(p);
      sqlite3BtreeLeave(p);
      return SQLITE_OK;
    }

    /*
    ** Do both phases of a commit.
    */
    static int sqlite3BtreeCommit(Btree p)
    {
      int rc;
      sqlite3BtreeEnter(p);
      rc = sqlite3BtreeCommitPhaseOne(p, null);
      if (rc == SQLITE_OK)
      {
        rc = sqlite3BtreeCommitPhaseTwo(p);
      }
      sqlite3BtreeLeave(p);
      return rc;
    }

#if !NDEBUG || DEBUG
    /*
** Return the number of write-cursors open on this handle. This is for use
** in Debug.Assert() expressions, so it is only compiled if NDEBUG is not
** defined.
**
** For the purposes of this routine, a write-cursor is any cursor that
** is capable of writing to the databse.  That means the cursor was
** originally opened for writing and the cursor has not be disabled
** by having its state changed to CURSOR_FAULT.
*/
    static int countWriteCursors(BtShared pBt)
    {
      BtCursor pCur;
      int r = 0;
      for (pCur = pBt.pCursor; pCur != null; pCur = pCur.pNext)
      {
        if (pCur.wrFlag != 0 && pCur.eState != CURSOR_FAULT) r++;
      }
      return r;
    }
#else
static int countWriteCursors(BtShared pBt) { return -1; }
#endif

    /*
** This routine sets the state to CURSOR_FAULT and the error
** code to errCode for every cursor on BtShared that pBtree
** references.
**
** Every cursor is tripped, including cursors that belong
** to other database connections that happen to be sharing
** the cache with pBtree.
**
** This routine gets called when a rollback occurs.
** All cursors using the same cache must be tripped
** to prevent them from trying to use the btree after
** the rollback.  The rollback may have deleted tables
** or moved root pages, so it is not sufficient to
** save the state of the cursor.  The cursor must be
** invalidated.
*/
    static void sqlite3BtreeTripAllCursors(Btree pBtree, int errCode)
    {
      BtCursor p;
      sqlite3BtreeEnter(pBtree);
      for (p = pBtree.pBt.pCursor; p != null; p = p.pNext)
      {
        int i;
        sqlite3BtreeClearCursor(p);
        p.eState = CURSOR_FAULT;
        p.skipNext = errCode;
        for (i = 0; i <= p.iPage; i++)
        {
          releasePage(p.apPage[i]);
          p.apPage[i] = null;
        }
      }
      sqlite3BtreeLeave(pBtree);
    }

    /*
    ** Rollback the transaction in progress.  All cursors will be
    ** invalided by this operation.  Any attempt to use a cursor
    ** that was open at the beginning of this operation will result
    ** in an error.
    **
    ** This will release the write lock on the database file.  If there
    ** are no active cursors, it also releases the read lock.
    */
    static int sqlite3BtreeRollback(Btree p)
    {
      int rc;
      BtShared pBt = p.pBt;
      MemPage pPage1 = new MemPage();

      sqlite3BtreeEnter(p);
      rc = saveAllCursors(pBt, 0, null);
#if !SQLITE_OMIT_SHARED_CACHE
if( rc!=SQLITE_OK ){
/* This is a horrible situation. An IO or malloc() error occurred whilst
** trying to save cursor positions. If this is an automatic rollback (as
** the result of a constraint, malloc() failure or IO error) then
** the cache may be internally inconsistent (not contain valid trees) so
** we cannot simply return the error to the caller. Instead, abort
** all queries that may be using any of the cursors that failed to save.
*/
sqlite3BtreeTripAllCursors(p, rc);
}
#endif
      btreeIntegrity(p);

      if (p.inTrans == TRANS_WRITE)
      {
        int rc2;

        Debug.Assert(TRANS_WRITE == pBt.inTransaction);
        rc2 = sqlite3PagerRollback(pBt.pPager);
        if (rc2 != SQLITE_OK)
        {
          rc = rc2;
        }

        /* The rollback may have destroyed the pPage1.aData value.  So
        ** call btreeGetPage() on page 1 again to make
        ** sure pPage1.aData is set correctly. */
        if (btreeGetPage(pBt, 1, ref pPage1, 0) == SQLITE_OK)
        {
          releasePage(pPage1);
        }
        Debug.Assert(countWriteCursors(pBt) == 0);
        pBt.inTransaction = TRANS_READ;
      }

      btreeEndTransaction(p);
      sqlite3BtreeLeave(p);
      return rc;
    }

    /*
    ** Start a statement subtransaction. The subtransaction can can be rolled
    ** back independently of the main transaction. You must start a transaction
    ** before starting a subtransaction. The subtransaction is ended automatically
    ** if the main transaction commits or rolls back.
    **
    ** Statement subtransactions are used around individual SQL statements
    ** that are contained within a BEGIN...COMMIT block.  If a constraint
    ** error occurs within the statement, the effect of that one statement
    ** can be rolled back without having to rollback the entire transaction.
    **
    ** A statement sub-transaction is implemented as an anonymous savepoint. The
    ** value passed as the second parameter is the total number of savepoints,
    ** including the new anonymous savepoint, open on the B-Tree. i.e. if there
    ** are no active savepoints and no other statement-transactions open,
    ** iStatement is 1. This anonymous savepoint can be released or rolled back
    ** using the sqlite3BtreeSavepoint() function.
    */
    static int sqlite3BtreeBeginStmt(Btree p, int iStatement)
    {
      int rc;
      BtShared pBt = p.pBt;
      sqlite3BtreeEnter(p);
      Debug.Assert(p.inTrans == TRANS_WRITE);
      Debug.Assert(!pBt.readOnly);
      Debug.Assert(iStatement > 0);
      Debug.Assert(iStatement > p.db.nSavepoint);
      if (NEVER(p.inTrans != TRANS_WRITE || pBt.readOnly))
      {
        rc = SQLITE_INTERNAL;
      }
      else
      {
        Debug.Assert(pBt.inTransaction == TRANS_WRITE);
        /* At the pager level, a statement transaction is a savepoint with
        ** an index greater than all savepoints created explicitly using
        ** SQL statements. It is illegal to open, release or rollback any
        ** such savepoints while the statement transaction savepoint is active.
        */
        rc = sqlite3PagerOpenSavepoint(pBt.pPager, iStatement);
      }
      sqlite3BtreeLeave(p);
      return rc;
    }

    /*
    ** The second argument to this function, op, is always SAVEPOINT_ROLLBACK
    ** or SAVEPOINT_RELEASE. This function either releases or rolls back the
    ** savepoint identified by parameter iSavepoint, depending on the value
    ** of op.
    **
    ** Normally, iSavepoint is greater than or equal to zero. However, if op is
    ** SAVEPOINT_ROLLBACK, then iSavepoint may also be -1. In this case the
    ** contents of the entire transaction are rolled back. This is different
    ** from a normal transaction rollback, as no locks are released and the
    ** transaction remains open.
    */
    static int sqlite3BtreeSavepoint(Btree p, int op, int iSavepoint)
    {
      int rc = SQLITE_OK;
      if (p != null && p.inTrans == TRANS_WRITE)
      {
        BtShared pBt = p.pBt;
        Debug.Assert(op == SAVEPOINT_RELEASE || op == SAVEPOINT_ROLLBACK);
        Debug.Assert(iSavepoint >= 0 || (iSavepoint == -1 && op == SAVEPOINT_ROLLBACK));
        sqlite3BtreeEnter(p);
        rc = sqlite3PagerSavepoint(pBt.pPager, op, iSavepoint);
        if (rc == SQLITE_OK)
        {
          rc = newDatabase(pBt);
        }
        sqlite3BtreeLeave(p);
      }
      return rc;
    }

    /*
    ** Create a new cursor for the BTree whose root is on the page
    ** iTable. If a read-only cursor is requested, it is assumed that
    ** the caller already has at least a read-only transaction open
    ** on the database already. If a write-cursor is requested, then
    ** the caller is assumed to have an open write transaction.
    **
    ** If wrFlag==null, then the cursor can only be used for reading.
    ** If wrFlag==1, then the cursor can be used for reading or for
    ** writing if other conditions for writing are also met.  These
    ** are the conditions that must be met in order for writing to
    ** be allowed:
    **
    ** 1:  The cursor must have been opened with wrFlag==1
    **
    ** 2:  Other database connections that share the same pager cache
    **     but which are not in the READ_UNCOMMITTED state may not have
    **     cursors open with wrFlag==null on the same table.  Otherwise
    **     the changes made by this write cursor would be visible to
    **     the read cursors in the other database connection.
    **
    ** 3:  The database must be writable (not on read-only media)
    **
    ** 4:  There must be an active transaction.
    **
    ** No checking is done to make sure that page iTable really is the
    ** root page of a b-tree.  If it is not, then the cursor acquired
    ** will not work correctly.
    **
    ** It is assumed that the sqlite3BtreeCursorSize() bytes of memory
    ** pointed to by pCur have been zeroed by the caller.
    */
    static int btreeCursor(
    Btree p,                              /* The btree */
    int iTable,                           /* Root page of table to open */
    int wrFlag,                           /* 1 to write. 0 read-only */
    KeyInfo pKeyInfo,                     /* First arg to comparison function */
    BtCursor pCur                         /* Space for new cursor */
    )
    {
      BtShared pBt = p.pBt;                 /* Shared b-tree handle */

      Debug.Assert(sqlite3BtreeHoldsMutex(p));
      Debug.Assert(wrFlag == 0 || wrFlag == 1);

      /* The following Debug.Assert statements verify that if this is a sharable
      ** b-tree database, the connection is holding the required table locks,
      ** and that no other connection has any open cursor that conflicts with
      ** this lock.  */
      Debug.Assert(hasSharedCacheTableLock(p, (u32)iTable, pKeyInfo != null ? 1 : 0, wrFlag + 1));
      Debug.Assert(wrFlag == 0 || !hasReadConflicts(p, (u32)iTable));

      /* Assert that the caller has opened the required transaction. */
      Debug.Assert(p.inTrans > TRANS_NONE);
      Debug.Assert(wrFlag == 0 || p.inTrans == TRANS_WRITE);
      Debug.Assert(pBt.pPage1 != null && pBt.pPage1.aData != null);

      if (NEVER(wrFlag != 0 && pBt.readOnly))
      {
        return SQLITE_READONLY;
      }
      if (iTable == 1 && pagerPagecount(pBt) == 0)
      {
        return SQLITE_EMPTY;
      }

      /* Now that no other errors can occur, finish filling in the BtCursor
      ** variables and link the cursor into the BtShared list.  */
      pCur.pgnoRoot = (Pgno)iTable;
      pCur.iPage = -1;
      pCur.pKeyInfo = pKeyInfo;
      pCur.pBtree = p;
      pCur.pBt = pBt;
      pCur.wrFlag = (u8)wrFlag;
      pCur.pNext = pBt.pCursor;
      if (pCur.pNext != null)
      {
        pCur.pNext.pPrev = pCur;
      }
      pBt.pCursor = pCur;
      pCur.eState = CURSOR_INVALID;
      pCur.cachedRowid = 0;
      return SQLITE_OK;
    }
    static int sqlite3BtreeCursor(
    Btree p,                                   /* The btree */
    int iTable,                                /* Root page of table to open */
    int wrFlag,                                /* 1 to write. 0 read-only */
    KeyInfo pKeyInfo,                          /* First arg to xCompare() */
    BtCursor pCur                              /* Write new cursor here */
    )
    {
      int rc;
      sqlite3BtreeEnter(p);
      rc = btreeCursor(p, iTable, wrFlag, pKeyInfo, pCur);
      sqlite3BtreeLeave(p);
      return rc;
    }

    /*
    ** Return the size of a BtCursor object in bytes.
    **
    ** This interfaces is needed so that users of cursors can preallocate
    ** sufficient storage to hold a cursor.  The BtCursor object is opaque
    ** to users so they cannot do the sizeof() themselves - they must call
    ** this routine.
    */
    static int sqlite3BtreeCursorSize()
    {
      return -1; // Not Used --  sizeof( BtCursor );
    }
    /*
    ** Set the cached rowid value of every cursor in the same database file
    ** as pCur and having the same root page number as pCur.  The value is
    ** set to iRowid.
    **
    ** Only positive rowid values are considered valid for this cache.
    ** The cache is initialized to zero, indicating an invalid cache.
    ** A btree will work fine with zero or negative rowids.  We just cannot
    ** cache zero or negative rowids, which means tables that use zero or
    ** negative rowids might run a little slower.  But in practice, zero
    ** or negative rowids are very uncommon so this should not be a problem.
    */
    static void sqlite3BtreeSetCachedRowid(BtCursor pCur, sqlite3_int64 iRowid)
    {
      BtCursor p;
      for (p = pCur.pBt.pCursor; p != null; p = p.pNext)
      {
        if (p.pgnoRoot == pCur.pgnoRoot) p.cachedRowid = iRowid;
      }
      Debug.Assert(pCur.cachedRowid == iRowid);
    }

    /*
    ** Return the cached rowid for the given cursor.  A negative or zero
    ** return value indicates that the rowid cache is invalid and should be
    ** ignored.  If the rowid cache has never before been set, then a
    ** zero is returned.
    */
    static sqlite3_int64 sqlite3BtreeGetCachedRowid(BtCursor pCur)
    {
      return pCur.cachedRowid;
    }

    /*
    ** Close a cursor.  The read lock on the database file is released
    ** when the last cursor is closed.
    */
    static int sqlite3BtreeCloseCursor(BtCursor pCur)
    {
      Btree pBtree = pCur.pBtree;
      if (pBtree != null)
      {
        int i;
        BtShared pBt = pCur.pBt;
        sqlite3BtreeEnter(pBtree);
        sqlite3BtreeClearCursor(pCur);
        if (pCur.pPrev != null)
        {
          pCur.pPrev.pNext = pCur.pNext;
        }
        else
        {
          pBt.pCursor = pCur.pNext;
        }
        if (pCur.pNext != null)
        {
          pCur.pNext.pPrev = pCur.pPrev;
        }
        for (i = 0; i <= pCur.iPage; i++)
        {
          releasePage(pCur.apPage[i]);
        }
        unlockBtreeIfUnused(pBt);
        invalidateOverflowCache(pCur);
        /* sqlite3_free(ref pCur); */
        sqlite3BtreeLeave(pBtree);
      }
      return SQLITE_OK;
    }

    /*
    ** Make sure the BtCursor* given in the argument has a valid
    ** BtCursor.info structure.  If it is not already valid, call
    ** btreeParseCell() to fill it in.
    **
    ** BtCursor.info is a cache of the information in the current cell.
    ** Using this cache reduces the number of calls to btreeParseCell().
    **
    ** 2007-06-25:  There is a bug in some versions of MSVC that cause the
    ** compiler to crash when getCellInfo() is implemented as a macro.
    ** But there is a measureable speed advantage to using the macro on gcc
    ** (when less compiler optimizations like -Os or -O0 are used and the
    ** compiler is not doing agressive inlining.)  So we use a real function
    ** for MSVC and a macro for everything else.  Ticket #2457.
    */
#if !NDEBUG
    static void assertCellInfo(BtCursor pCur)
    {
      CellInfo info;
      int iPage = pCur.iPage;
      info = new CellInfo();//memset(info, 0, sizeof(info));
      btreeParseCell(pCur.apPage[iPage], pCur.aiIdx[iPage], ref info);
      Debug.Assert(info.Equals(pCur.info));//memcmp(info, pCur.info, sizeof(info))==0 );
    }
#else
//  #define assertCellInfo(x)
static void assertCellInfo(BtCursor pCur) { }
#endif
#if _MSC_VER
    /* Use a real function in MSVC to work around bugs in that compiler. */
    static void getCellInfo(BtCursor pCur)
    {
      if (pCur.info.nSize == 0)
      {
        int iPage = pCur.iPage;
        btreeParseCell( pCur.apPage[iPage], pCur.aiIdx[iPage], ref pCur.info );
        pCur.validNKey = true;
      }
      else
      {
        assertCellInfo(pCur);
      }
    }
#else //* if not _MSC_VER */
/* Use a macro in all other compilers so that the function is inlined */
//#define getCellInfo(pCur)                                                      \
//  if( pCur.info.nSize==null ){                                                   \
//    int iPage = pCur.iPage;                                                   \
//    btreeParseCell(pCur.apPage[iPage],pCur.aiIdx[iPage],&pCur.info); \
//    pCur.validNKey = true;                                                       \
//  }else{                                                                       \
//    assertCellInfo(pCur);                                                      \
//  }
#endif //* _MSC_VER */

#if !NDEBUG  //* The next routine used only within Debug.Assert() statements */
    /*
** Return true if the given BtCursor is valid.  A valid cursor is one
** that is currently pointing to a row in a (non-empty) table.
** This is a verification routine is used only within Debug.Assert() statements.
*/
    static bool sqlite3BtreeCursorIsValid(BtCursor pCur)
    {
      return pCur != null && pCur.eState == CURSOR_VALID;
    }
#else
static bool sqlite3BtreeCursorIsValid(BtCursor pCur) { return true; }
#endif //* NDEBUG */

    /*
** Set pSize to the size of the buffer needed to hold the value of
** the key for the current entry.  If the cursor is not pointing
** to a valid entry, pSize is set to 0.
**
** For a table with the INTKEY flag set, this routine returns the key
** itself, not the number of bytes in the key.
**
** The caller must position the cursor prior to invoking this routine.
**
** This routine cannot fail.  It always returns SQLITE_OK.
*/
    static int sqlite3BtreeKeySize(BtCursor pCur, ref i64 pSize)
    {
      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.eState == CURSOR_INVALID || pCur.eState == CURSOR_VALID);
      if (pCur.eState != CURSOR_VALID)
      {
        pSize = 0;
      }
      else
      {
        getCellInfo(pCur);
        pSize = pCur.info.nKey;
      }
      return SQLITE_OK;
    }

    /*
    ** Set pSize to the number of bytes of data in the entry the
    ** cursor currently points to.
    **
    ** The caller must guarantee that the cursor is pointing to a non-NULL
    ** valid entry.  In other words, the calling procedure must guarantee
    ** that the cursor has Cursor.eState==CURSOR_VALID.
    **
    ** Failure is not possible.  This function always returns SQLITE_OK.
    ** It might just as well be a procedure (returning void) but we continue
    ** to return an integer result code for historical reasons.
    */
    static int sqlite3BtreeDataSize(BtCursor pCur, ref u32 pSize)
    {
      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.eState == CURSOR_VALID);
      getCellInfo(pCur);
      pSize = pCur.info.nData;
      return SQLITE_OK;
    }

    /*
    ** Given the page number of an overflow page in the database (parameter
    ** ovfl), this function finds the page number of the next page in the
    ** linked list of overflow pages. If possible, it uses the auto-vacuum
    ** pointer-map data instead of reading the content of page ovfl to do so.
    **
    ** If an error occurs an SQLite error code is returned. Otherwise:
    **
    ** The page number of the next overflow page in the linked list is
    ** written to pPgnoNext. If page ovfl is the last page in its linked
    ** list, pPgnoNext is set to zero.
    **
    ** If ppPage is not NULL, and a reference to the MemPage object corresponding
    ** to page number pOvfl was obtained, then ppPage is set to point to that
    ** reference. It is the responsibility of the caller to call releasePage()
    ** on ppPage to free the reference. In no reference was obtained (because
    ** the pointer-map was used to obtain the value for pPgnoNext), then
    ** ppPage is set to zero.
    */
    static int getOverflowPage(
    BtShared pBt,               /* The database file */
    Pgno ovfl,                  /* Current overflow page number */
    ref MemPage ppPage,         /* OUT: MemPage handle (may be NULL) */
    ref Pgno pPgnoNext          /* OUT: Next overflow page number */
    )
    {
      Pgno next = 0;
      MemPage pPage = null;
      int rc = SQLITE_OK;

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      // Debug.Assert( pPgnoNext);

#if !SQLITE_OMIT_AUTOVACUUM
      /* Try to find the next page in the overflow list using the
** autovacuum pointer-map pages. Guess that the next page in
** the overflow list is page number (ovfl+1). If that guess turns
** out to be wrong, fall back to loading the data of page
** number ovfl to determine the next page number.
*/
      if (pBt.autoVacuum)
      {
        Pgno pgno = 0;
        Pgno iGuess = ovfl + 1;
        u8 eType = 0;

        while (PTRMAP_ISPAGE(pBt, iGuess) || iGuess == PENDING_BYTE_PAGE(pBt))
        {
          iGuess++;
        }

        if (iGuess <= pagerPagecount(pBt))
        {
          rc = ptrmapGet(pBt, iGuess, ref eType, ref pgno);
          if (rc == SQLITE_OK && eType == PTRMAP_OVERFLOW2 && pgno == ovfl)
          {
            next = iGuess;
            rc = SQLITE_DONE;
          }
        }
      }
#endif

      Debug.Assert(next == 0 || rc == SQLITE_DONE);
      if (rc == SQLITE_OK)
      {
        rc = btreeGetPage(pBt, ovfl, ref pPage, 0);
        Debug.Assert(rc == SQLITE_OK || pPage == null);
        if (rc == SQLITE_OK)
        {
          next = sqlite3Get4byte(pPage.aData);
        }
      }

      pPgnoNext = next;
      if (ppPage != null)
      {
        ppPage = pPage;
      }
      else
      {
        releasePage(pPage);
      }
      return (rc == SQLITE_DONE ? SQLITE_OK : rc);
    }

    /*
    ** Copy data from a buffer to a page, or from a page to a buffer.
    **
    ** pPayload is a pointer to data stored on database page pDbPage.
    ** If argument eOp is false, then nByte bytes of data are copied
    ** from pPayload to the buffer pointed at by pBuf. If eOp is true,
    ** then sqlite3PagerWrite() is called on pDbPage and nByte bytes
    ** of data are copied from the buffer pBuf to pPayload.
    **
    ** SQLITE_OK is returned on success, otherwise an error code.
    */
    static int copyPayload(
    byte[] pPayload,           /* Pointer to page data */
    u32 payloadOffset,         /* Offset into page data */
    byte[] pBuf,               /* Pointer to buffer */
    u32 pBufOffset,            /* Offset into buffer */
    u32 nByte,                 /* Number of bytes to copy */
    int eOp,                   /* 0 . copy from page, 1 . copy to page */
    DbPage pDbPage             /* Page containing pPayload */
    )
    {
      if (eOp != 0)
      {
        /* Copy data from buffer to page (a write operation) */
        int rc = sqlite3PagerWrite(pDbPage);
        if (rc != SQLITE_OK)
        {
          return rc;
        }
        Buffer.BlockCopy(pBuf, (int)pBufOffset, pPayload, (int)payloadOffset, (int)nByte);// memcpy( pPayload, pBuf, nByte );
      }
      else
      {
        /* Copy data from page to buffer (a read operation) */
        Buffer.BlockCopy(pPayload, (int)payloadOffset, pBuf, (int)pBufOffset, (int)nByte);//memcpy(pBuf, pPayload, nByte);
      }
      return SQLITE_OK;
    }
    //static int copyPayload(
    //  byte[] pPayload,           /* Pointer to page data */
    //  byte[] pBuf,               /* Pointer to buffer */
    //  int nByte,                 /* Number of bytes to copy */
    //  int eOp,                   /* 0 -> copy from page, 1 -> copy to page */
    //  DbPage pDbPage             /* Page containing pPayload */
    //){
    //  if( eOp!=0 ){
    //    /* Copy data from buffer to page (a write operation) */
    //    int rc = sqlite3PagerWrite(pDbPage);
    //    if( rc!=SQLITE_OK ){
    //      return rc;
    //    }
    //    memcpy(pPayload, pBuf, nByte);
    //  }else{
    //    /* Copy data from page to buffer (a read operation) */
    //    memcpy(pBuf, pPayload, nByte);
    //  }
    //  return SQLITE_OK;
    //}

    /*
    ** This function is used to read or overwrite payload information
    ** for the entry that the pCur cursor is pointing to. If the eOp
    ** parameter is 0, this is a read operation (data copied into
    ** buffer pBuf). If it is non-zero, a write (data copied from
    ** buffer pBuf).
    **
    ** A total of "amt" bytes are read or written beginning at "offset".
    ** Data is read to or from the buffer pBuf.
    **
    ** The content being read or written might appear on the main page
    ** or be scattered out on multiple overflow pages.
    **
    ** If the BtCursor.isIncrblobHandle flag is set, and the current
    ** cursor entry uses one or more overflow pages, this function
    ** allocates space for and lazily popluates the overflow page-list
    ** cache array (BtCursor.aOverflow). Subsequent calls use this
    ** cache to make seeking to the supplied offset more efficient.
    **
    ** Once an overflow page-list cache has been allocated, it may be
    ** invalidated if some other cursor writes to the same table, or if
    ** the cursor is moved to a different row. Additionally, in auto-vacuum
    ** mode, the following events may invalidate an overflow page-list cache.
    **
    **   * An incremental vacuum,
    **   * A commit in auto_vacuum="full" mode,
    **   * Creating a table (may require moving an overflow page).
    */
    static int accessPayload(
    BtCursor pCur,      /* Cursor pointing to entry to read from */
    u32 offset,         /* Begin reading this far into payload */
    u32 amt,            /* Read this many bytes */
    byte[] pBuf,        /* Write the bytes into this buffer */
    int eOp             /* zero to read. non-zero to write. */
    )
    {
      u32 pBufOffset = 0;
      byte[] aPayload;
      int rc = SQLITE_OK;
      u32 nKey;
      int iIdx = 0;
      MemPage pPage = pCur.apPage[pCur.iPage]; /* Btree page of current entry */
      BtShared pBt = pCur.pBt;                  /* Btree this cursor belongs to */

      Debug.Assert(pPage != null);
      Debug.Assert(pCur.eState == CURSOR_VALID);
      Debug.Assert(pCur.aiIdx[pCur.iPage] < pPage.nCell);
      Debug.Assert(cursorHoldsMutex(pCur));

      getCellInfo(pCur);
      aPayload = pCur.info.pCell; //pCur.info.pCell + pCur.info.nHeader;
      nKey = (u32)(pPage.intKey != 0 ? 0 : (int)pCur.info.nKey);

      if (NEVER(offset + amt > nKey + pCur.info.nData)
      || pCur.info.nLocal > pBt.usableSize//&aPayload[pCur.info.nLocal] > &pPage.aData[pBt.usableSize]
      )
      {
        /* Trying to read or write past the end of the data is an error */
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }

      /* Check if data must be read/written to/from the btree page itself. */
      if (offset < pCur.info.nLocal)
      {
        int a = (int)amt;
        if (a + offset > pCur.info.nLocal)
        {
          a = (int)(pCur.info.nLocal - offset);
        }
        rc = copyPayload(aPayload, (u32)(offset + pCur.info.iCell + pCur.info.nHeader), pBuf, pBufOffset, (u32)a, eOp, pPage.pDbPage);
        offset = 0;
        pBufOffset += (u32)a; //pBuf += a;
        amt -= (u32)a;
      }
      else
      {
        offset -= pCur.info.nLocal;
      }

      if (rc == SQLITE_OK && amt > 0)
      {
        u32 ovflSize = (u32)(pBt.usableSize - 4);  /* Bytes content per ovfl page */
        Pgno nextPage;

        nextPage = sqlite3Get4byte(aPayload, pCur.info.nLocal + pCur.info.iCell + pCur.info.nHeader);

#if !SQLITE_OMIT_INCRBLOB
/* If the isIncrblobHandle flag is set and the BtCursor.aOverflow[]
** has not been allocated, allocate it now. The array is sized at
** one entry for each overflow page in the overflow chain. The
** page number of the first overflow page is stored in aOverflow[0],
** etc. A value of 0 in the aOverflow[] array means "not yet known"
** (the cache is lazily populated).
*/
if( pCur.isIncrblobHandle && !pCur.aOverflow ){
int nOvfl = (pCur.info.nPayload-pCur.info.nLocal+ovflSize-1)/ovflSize;
pCur.aOverflow = (Pgno *)sqlite3MallocZero(sizeof(Pgno)*nOvfl);
/* nOvfl is always positive.  If it were zero, fetchPayload would have
** been used instead of this routine. */
if( ALWAYS(nOvfl) && !pCur.aOverflow ){
rc = SQLITE_NOMEM;
}
}

/* If the overflow page-list cache has been allocated and the
** entry for the first required overflow page is valid, skip
** directly to it.
*/
if( pCur.aOverflow && pCur.aOverflow[offset/ovflSize] ){
iIdx = (offset/ovflSize);
nextPage = pCur.aOverflow[iIdx];
offset = (offset%ovflSize);
}
#endif

        for (; rc == SQLITE_OK && amt > 0 && nextPage != 0; iIdx++)
        {

#if !SQLITE_OMIT_INCRBLOB
/* If required, populate the overflow page-list cache. */
if( pCur.aOverflow ){
Debug.Assert(!pCur.aOverflow[iIdx] || pCur.aOverflow[iIdx]==nextPage);
pCur.aOverflow[iIdx] = nextPage;
}
#endif

          MemPage MemPageDummy = null;
          if (offset >= ovflSize)
          {
            /* The only reason to read this page is to obtain the page
            ** number for the next page in the overflow chain. The page
            ** data is not required. So first try to lookup the overflow
            ** page-list cache, if any, then fall back to the getOverflowPage()
            ** function.
            */
#if !SQLITE_OMIT_INCRBLOB
if( pCur.aOverflow && pCur.aOverflow[iIdx+1] ){
nextPage = pCur.aOverflow[iIdx+1];
} else
#endif
            rc = getOverflowPage(pBt, nextPage, ref  MemPageDummy, ref nextPage);
            offset -= ovflSize;
          }
          else
          {
            /* Need to read this page properly. It contains some of the
            ** range of data that is being read (eOp==null) or written (eOp!=null).
            */
            DbPage pDbPage = new PgHdr();
            int a = (int)amt;
            rc = sqlite3PagerGet(pBt.pPager, nextPage, ref pDbPage);
            if (rc == SQLITE_OK)
            {
              aPayload = sqlite3PagerGetData(pDbPage);
              nextPage = sqlite3Get4byte(aPayload);
              if (a + offset > ovflSize)
              {
                a = (int)(ovflSize - offset);
              }
              rc = copyPayload(aPayload, offset + 4, pBuf, pBufOffset, (u32)a, eOp, pDbPage);
              sqlite3PagerUnref(pDbPage);
              offset = 0;
              amt -= (u32)a;
              pBufOffset += (u32)a;//pBuf += a;
            }
          }
        }
      }

      if (rc == SQLITE_OK && amt > 0)
      {
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }
      return rc;
    }

    /*
    ** Read part of the key associated with cursor pCur.  Exactly
    ** "amt" bytes will be transfered into pBuf[].  The transfer
    ** begins at "offset".
    **
    ** The caller must ensure that pCur is pointing to a valid row
    ** in the table.
    **
    ** Return SQLITE_OK on success or an error code if anything goes
    ** wrong.  An error is returned if "offset+amt" is larger than
    ** the available payload.
    */
    static int sqlite3BtreeKey(BtCursor pCur, u32 offset, u32 amt, byte[] pBuf)
    {
      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.eState == CURSOR_VALID);
      Debug.Assert(pCur.iPage >= 0 && pCur.apPage[pCur.iPage] != null);
      Debug.Assert(pCur.aiIdx[pCur.iPage] < pCur.apPage[pCur.iPage].nCell);
      return accessPayload(pCur, offset, amt, pBuf, 0);
    }

    /*
    ** Read part of the data associated with cursor pCur.  Exactly
    ** "amt" bytes will be transfered into pBuf[].  The transfer
    ** begins at "offset".
    **
    ** Return SQLITE_OK on success or an error code if anything goes
    ** wrong.  An error is returned if "offset+amt" is larger than
    ** the available payload.
    */
    static int sqlite3BtreeData(BtCursor pCur, u32 offset, u32 amt, byte[] pBuf)
    {
      int rc;

#if !SQLITE_OMIT_INCRBLOB
if ( pCur.eState==CURSOR_INVALID ){
return SQLITE_ABORT;
}
#endif

      Debug.Assert(cursorHoldsMutex(pCur));
      rc = restoreCursorPosition(pCur);
      if (rc == SQLITE_OK)
      {
        Debug.Assert(pCur.eState == CURSOR_VALID);
        Debug.Assert(pCur.iPage >= 0 && pCur.apPage[pCur.iPage] != null);
        Debug.Assert(pCur.aiIdx[pCur.iPage] < pCur.apPage[pCur.iPage].nCell);
        rc = accessPayload(pCur, offset, amt, pBuf, 0);
      }
      return rc;
    }

    /*
    ** Return a pointer to payload information from the entry that the
    ** pCur cursor is pointing to.  The pointer is to the beginning of
    ** the key if skipKey==null and it points to the beginning of data if
    ** skipKey==1.  The number of bytes of available key/data is written
    ** into pAmt.  If pAmt==null, then the value returned will not be
    ** a valid pointer.
    **
    ** This routine is an optimization.  It is common for the entire key
    ** and data to fit on the local page and for there to be no overflow
    ** pages.  When that is so, this routine can be used to access the
    ** key and data without making a copy.  If the key and/or data spills
    ** onto overflow pages, then accessPayload() must be used to reassemble
    ** the key/data and copy it into a preallocated buffer.
    **
    ** The pointer returned by this routine looks directly into the cached
    ** page of the database.  The data might change or move the next time
    ** any btree routine is called.
    */
    static byte[] fetchPayload(
    BtCursor pCur,   /* Cursor pointing to entry to read from */
    ref int pAmt,    /* Write the number of available bytes here */
    ref int outOffset, /* Offset into Buffer */
    bool skipKey    /* read beginning at data if this is true */
    )
    {
      byte[] aPayload;
      MemPage pPage;
      u32 nKey;
      u32 nLocal;

      Debug.Assert(pCur != null && pCur.iPage >= 0 && pCur.apPage[pCur.iPage] != null);
      Debug.Assert(pCur.eState == CURSOR_VALID);
      Debug.Assert(cursorHoldsMutex(pCur));
      outOffset = -1;
      pPage = pCur.apPage[pCur.iPage];
      Debug.Assert(pCur.aiIdx[pCur.iPage] < pPage.nCell);
      if (NEVER(pCur.info.nSize == 0))
      {
        btreeParseCell(pCur.apPage[pCur.iPage], pCur.aiIdx[pCur.iPage],
        ref pCur.info );
      }
      //aPayload = pCur.info.pCell;
      //aPayload += pCur.info.nHeader;
      aPayload = new byte[pCur.info.nSize - pCur.info.nHeader];
      if (pPage.intKey != 0)
      {
        nKey = 0;
      }
      else
      {
        nKey = (u32)pCur.info.nKey;
      }
      if ( skipKey )
      {
        //aPayload += nKey;
        outOffset = (int)( pCur.info.iCell + pCur.info.nHeader + nKey );
        Buffer.BlockCopy( pCur.info.pCell, outOffset, aPayload, 0, (int)( pCur.info.nSize - pCur.info.nHeader - nKey ) );
        nLocal = pCur.info.nLocal - nKey;
      }
      else
      {
        outOffset = (int)( pCur.info.iCell + pCur.info.nHeader );
        Buffer.BlockCopy( pCur.info.pCell, outOffset, aPayload, 0, pCur.info.nSize - pCur.info.nHeader );
        nLocal = pCur.info.nLocal;
        Debug.Assert( nLocal <= nKey );
      }
      pAmt = (int)nLocal;
      return aPayload;
    }

    /*
    ** For the entry that cursor pCur is point to, return as
    ** many bytes of the key or data as are available on the local
    ** b-tree page.  Write the number of available bytes into pAmt.
    **
    ** The pointer returned is ephemeral.  The key/data may move
    ** or be destroyed on the next call to any Btree routine,
    ** including calls from other threads against the same cache.
    ** Hence, a mutex on the BtShared should be held prior to calling
    ** this routine.
    **
    ** These routines is used to get quick access to key and data
    ** in the common case where no overflow pages are used.
    */
    static byte[] sqlite3BtreeKeyFetch( BtCursor pCur, ref int pAmt, ref int outOffset )
    {
      byte[] p = null;
      Debug.Assert( sqlite3_mutex_held( pCur.pBtree.db.mutex ) );
      Debug.Assert( cursorHoldsMutex( pCur ) );
      if ( ALWAYS( pCur.eState == CURSOR_VALID ) )
      {
        p = fetchPayload( pCur, ref pAmt, ref outOffset, false );
      }
      return p;
    }
    static byte[] sqlite3BtreeDataFetch( BtCursor pCur, ref int pAmt, ref int outOffset )
    {
      byte[] p = null;
      Debug.Assert( sqlite3_mutex_held( pCur.pBtree.db.mutex ) );
      Debug.Assert( cursorHoldsMutex( pCur ) );
      if ( ALWAYS( pCur.eState == CURSOR_VALID ) )
      {
        p = fetchPayload( pCur, ref pAmt, ref outOffset, true );
      }
      return p;
    }

    /*
    ** Move the cursor down to a new child page.  The newPgno argument is the
    ** page number of the child page to move to.
    **
    ** This function returns SQLITE_CORRUPT if the page-header flags field of
    ** the new child page does not match the flags field of the parent (i.e.
    ** if an intkey page appears to be the parent of a non-intkey page, or
    ** vice-versa).
    */
    static int moveToChild(BtCursor pCur, u32 newPgno)
    {
      int rc;
      int i = pCur.iPage;
      MemPage pNewPage = new MemPage();
      BtShared pBt = pCur.pBt;

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.eState == CURSOR_VALID);
      Debug.Assert(pCur.iPage < BTCURSOR_MAX_DEPTH);
      if (pCur.iPage >= (BTCURSOR_MAX_DEPTH - 1))
      {
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }
      rc = getAndInitPage(pBt, newPgno, ref pNewPage);
      if (rc != 0) return rc;
      pCur.apPage[i + 1] = pNewPage;
      pCur.aiIdx[i + 1] = 0;
      pCur.iPage++;

      pCur.info.nSize = 0;
      pCur.validNKey = false;
      if (pNewPage.nCell < 1 || pNewPage.intKey != pCur.apPage[i].intKey)
      {
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }
      return SQLITE_OK;
    }

#if !NDEBUG
    /*
** Page pParent is an internal (non-leaf) tree page. This function
** asserts that page number iChild is the left-child if the iIdx'th
** cell in page pParent. Or, if iIdx is equal to the total number of
** cells in pParent, that page number iChild is the right-child of
** the page.
*/
    static void assertParentIndex(MemPage pParent, int iIdx, Pgno iChild)
    {
      Debug.Assert(iIdx <= pParent.nCell);
      if (iIdx == pParent.nCell)
      {
        Debug.Assert(sqlite3Get4byte(pParent.aData, pParent.hdrOffset + 8) == iChild);
      }
      else
      {
        Debug.Assert(sqlite3Get4byte(pParent.aData, findCell(pParent, iIdx)) == iChild);
      }
    }
#else
//#  define assertParentIndex(x,y,z)
static void assertParentIndex(MemPage pParent, int iIdx, Pgno iChild) { }
#endif

    /*
** Move the cursor up to the parent page.
**
** pCur.idx is set to the cell index that contains the pointer
** to the page we are coming from.  If we are coming from the
** right-most child page then pCur.idx is set to one more than
** the largest cell index.
*/
    static void moveToParent(BtCursor pCur)
    {
      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.eState == CURSOR_VALID);
      Debug.Assert(pCur.iPage > 0);
      Debug.Assert(pCur.apPage[pCur.iPage] != null);
      assertParentIndex(
      pCur.apPage[pCur.iPage - 1],
      pCur.aiIdx[pCur.iPage - 1],
      pCur.apPage[pCur.iPage].pgno
      );
      releasePage(pCur.apPage[pCur.iPage]);
      pCur.iPage--;
      pCur.info.nSize = 0;
      pCur.validNKey = false;
    }

    /*
    ** Move the cursor to point to the root page of its b-tree structure.
    **
    ** If the table has a virtual root page, then the cursor is moved to point
    ** to the virtual root page instead of the actual root page. A table has a
    ** virtual root page when the actual root page contains no cells and a
    ** single child page. This can only happen with the table rooted at page 1.
    **
    ** If the b-tree structure is empty, the cursor state is set to
    ** CURSOR_INVALID. Otherwise, the cursor is set to point to the first
    ** cell located on the root (or virtual root) page and the cursor state
    ** is set to CURSOR_VALID.
    **
    ** If this function returns successfully, it may be assumed that the
    ** page-header flags indicate that the [virtual] root-page is the expected
    ** kind of b-tree page (i.e. if when opening the cursor the caller did not
    ** specify a KeyInfo structure the flags byte is set to 0x05 or 0x0D,
    ** indicating a table b-tree, or if the caller did specify a KeyInfo
    ** structure the flags byte is set to 0x02 or 0x0A, indicating an index
    ** b-tree).
    */
    static int moveToRoot(BtCursor pCur)
    {
      MemPage pRoot;
      int rc = SQLITE_OK;
      Btree p = pCur.pBtree;
      BtShared pBt = p.pBt;

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(CURSOR_INVALID < CURSOR_REQUIRESEEK);
      Debug.Assert(CURSOR_VALID < CURSOR_REQUIRESEEK);
      Debug.Assert(CURSOR_FAULT > CURSOR_REQUIRESEEK);
      if (pCur.eState >= CURSOR_REQUIRESEEK)
      {
        if (pCur.eState == CURSOR_FAULT)
        {
          Debug.Assert(pCur.skipNext != SQLITE_OK);
          return pCur.skipNext;
        }
        sqlite3BtreeClearCursor(pCur);
      }

      if (pCur.iPage >= 0)
      {
        int i;
        for (i = 1; i <= pCur.iPage; i++)
        {
          releasePage(pCur.apPage[i]);
        }
        pCur.iPage = 0;
      }
      else
      {
        rc = getAndInitPage(pBt, pCur.pgnoRoot, ref pCur.apPage[0]);
        if (rc != SQLITE_OK)
        {
          pCur.eState = CURSOR_INVALID;
          return rc;
        }
        pCur.iPage = 0;

        /* If pCur.pKeyInfo is not NULL, then the caller that opened this cursor
        ** expected to open it on an index b-tree. Otherwise, if pKeyInfo is
        ** NULL, the caller expects a table b-tree. If this is not the case,
        ** return an SQLITE_CORRUPT error.  */
        Debug.Assert(pCur.apPage[0].intKey == 1 || pCur.apPage[0].intKey == 0);
        if ((pCur.pKeyInfo == null) != (pCur.apPage[0].intKey != 0))
        {
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
      }

      /* Assert that the root page is of the correct type. This must be the
      ** case as the call to this function that loaded the root-page (either
      ** this call or a previous invocation) would have detected corruption
      ** if the assumption were not true, and it is not possible for the flags
      ** byte to have been modified while this cursor is holding a reference
      ** to the page.  */
      pRoot = pCur.apPage[0];
      Debug.Assert(pRoot.pgno == pCur.pgnoRoot);
      Debug.Assert(pRoot.isInit != 0 && (pCur.pKeyInfo == null) == (pRoot.intKey != 0));

      pCur.aiIdx[0] = 0;
      pCur.info.nSize = 0;
      pCur.atLast = 0;
      pCur.validNKey = false;

      if (pRoot.nCell == 0 && 0 == pRoot.leaf)
      {
        Pgno subpage;
        if (pRoot.pgno != 1)
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        subpage = sqlite3Get4byte(pRoot.aData, pRoot.hdrOffset + 8);
        pCur.eState = CURSOR_VALID;
        rc = moveToChild(pCur, subpage);
      }
      else
      {
        pCur.eState = ((pRoot.nCell > 0) ? CURSOR_VALID : CURSOR_INVALID);
      }
      return rc;
    }

    /*
    ** Move the cursor down to the left-most leaf entry beneath the
    ** entry to which it is currently pointing.
    **
    ** The left-most leaf is the one with the smallest key - the first
    ** in ascending order.
    */
    static int moveToLeftmost(BtCursor pCur)
    {
      Pgno pgno;
      int rc = SQLITE_OK;
      MemPage pPage;

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.eState == CURSOR_VALID);
      while (rc == SQLITE_OK && 0 == (pPage = pCur.apPage[pCur.iPage]).leaf)
      {
        Debug.Assert(pCur.aiIdx[pCur.iPage] < pPage.nCell);
        pgno = sqlite3Get4byte(pPage.aData, findCell(pPage, pCur.aiIdx[pCur.iPage]));
        rc = moveToChild(pCur, pgno);
      }
      return rc;
    }

    /*
    ** Move the cursor down to the right-most leaf entry beneath the
    ** page to which it is currently pointing.  Notice the difference
    ** between moveToLeftmost() and moveToRightmost().  moveToLeftmost()
    ** finds the left-most entry beneath the *entry* whereas moveToRightmost()
    ** finds the right-most entry beneath the page*.
    **
    ** The right-most entry is the one with the largest key - the last
    ** key in ascending order.
    */
    static int moveToRightmost(BtCursor pCur)
    {
      Pgno pgno;
      int rc = SQLITE_OK;
      MemPage pPage = null;

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.eState == CURSOR_VALID);
      while (rc == SQLITE_OK && 0 == (pPage = pCur.apPage[pCur.iPage]).leaf)
      {
        pgno = sqlite3Get4byte(pPage.aData, pPage.hdrOffset + 8);
        pCur.aiIdx[pCur.iPage] = pPage.nCell;
        rc = moveToChild(pCur, pgno);
      }
      if (rc == SQLITE_OK)
      {
        pCur.aiIdx[pCur.iPage] = (u16)(pPage.nCell - 1);
        pCur.info.nSize = 0;
        pCur.validNKey = false;
      }
      return rc;
    }

    /* Move the cursor to the first entry in the table.  Return SQLITE_OK
    ** on success.  Set pRes to 0 if the cursor actually points to something
    ** or set pRes to 1 if the table is empty.
    */
    static int sqlite3BtreeFirst(BtCursor pCur, ref int pRes)
    {
      int rc;

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(sqlite3_mutex_held(pCur.pBtree.db.mutex));
      rc = moveToRoot(pCur);
      if (rc == SQLITE_OK)
      {
        if (pCur.eState == CURSOR_INVALID)
        {
          Debug.Assert(pCur.apPage[pCur.iPage].nCell == 0);
          pRes = 1;
          rc = SQLITE_OK;
        }
        else
        {
          Debug.Assert(pCur.apPage[pCur.iPage].nCell > 0);
          pRes = 0;
          rc = moveToLeftmost(pCur);
        }
      }
      return rc;
    }

    /* Move the cursor to the last entry in the table.  Return SQLITE_OK
    ** on success.  Set pRes to 0 if the cursor actually points to something
    ** or set pRes to 1 if the table is empty.
    */
    static int sqlite3BtreeLast(BtCursor pCur, ref int pRes)
    {
      int rc;

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(sqlite3_mutex_held(pCur.pBtree.db.mutex));

      /* If the cursor already points to the last entry, this is a no-op. */
      if (CURSOR_VALID == pCur.eState && pCur.atLast != 0)
      {
#if SQLITE_DEBUG
        /* This block serves to Debug.Assert() that the cursor really does point
** to the last entry in the b-tree. */
        int ii;
        for (ii = 0; ii < pCur.iPage; ii++)
        {
          Debug.Assert(pCur.aiIdx[ii] == pCur.apPage[ii].nCell);
        }
        Debug.Assert(pCur.aiIdx[pCur.iPage] == pCur.apPage[pCur.iPage].nCell - 1);
        Debug.Assert(pCur.apPage[pCur.iPage].leaf != 0);
#endif
        return SQLITE_OK;
      }

      rc = moveToRoot(pCur);
      if (rc == SQLITE_OK)
      {
        if (CURSOR_INVALID == pCur.eState)
        {
          Debug.Assert(pCur.apPage[pCur.iPage].nCell == 0);
          pRes = 1;
        }
        else
        {
          Debug.Assert(pCur.eState == CURSOR_VALID);
          pRes = 0;
          rc = moveToRightmost(pCur);
          pCur.atLast = (u8)(rc == SQLITE_OK ? 1 : 0);
        }
      }
      return rc;
    }

    /* Move the cursor so that it points to an entry near the key
    ** specified by pIdxKey or intKey.   Return a success code.
    **
    ** For INTKEY tables, the intKey parameter is used.  pIdxKey
    ** must be NULL.  For index tables, pIdxKey is used and intKey
    ** is ignored.
    **
    ** If an exact match is not found, then the cursor is always
    ** left pointing at a leaf page which would hold the entry if it
    ** were present.  The cursor might point to an entry that comes
    ** before or after the key.
    **
    ** An integer is written into pRes which is the result of
    ** comparing the key with the entry to which the cursor is
    ** pointing.  The meaning of the integer written into
    ** pRes is as follows:
    **
    **     pRes<0      The cursor is left pointing at an entry that
    **                  is smaller than intKey/pIdxKey or if the table is empty
    **                  and the cursor is therefore left point to nothing.
    **
    **     pRes==null     The cursor is left pointing at an entry that
    **                  exactly matches intKey/pIdxKey.
    **
    **     pRes>0      The cursor is left pointing at an entry that
    **                  is larger than intKey/pIdxKey.
    **
    */
    static int sqlite3BtreeMovetoUnpacked(
    BtCursor pCur,           /* The cursor to be moved */
    UnpackedRecord pIdxKey,  /* Unpacked index key */
    i64 intKey,              /* The table key */
    int biasRight,           /* If true, bias the search to the high end */
    ref int pRes             /* Write search results here */
    )
    {
      int rc;

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(sqlite3_mutex_held(pCur.pBtree.db.mutex));
      // Not needed in C# // Debug.Assert( pRes != 0 );
      Debug.Assert((pIdxKey == null) == (pCur.pKeyInfo == null));

      /* If the cursor is already positioned at the point we are trying
      ** to move to, then just return without doing any work */
      if (pCur.eState == CURSOR_VALID && pCur.validNKey
      && pCur.apPage[0].intKey != 0
      )
      {
        if (pCur.info.nKey == intKey)
        {
          pRes = 0;
          return SQLITE_OK;
        }
        if (pCur.atLast != 0 && pCur.info.nKey < intKey)
        {
          pRes = -1;
          return SQLITE_OK;
        }
      }

      rc = moveToRoot(pCur);
      if (rc != 0)
      {
        return rc;
      }
      Debug.Assert(pCur.apPage[pCur.iPage] != null);
      Debug.Assert(pCur.apPage[pCur.iPage].isInit != 0);
      Debug.Assert(pCur.apPage[pCur.iPage].nCell > 0 || pCur.eState == CURSOR_INVALID);
      if (pCur.eState == CURSOR_INVALID)
      {
        pRes = -1;
        Debug.Assert(pCur.apPage[pCur.iPage].nCell == 0);
        return SQLITE_OK;
      }
      Debug.Assert(pCur.apPage[0].intKey != 0 || pIdxKey != null);
      for (; ; )
      {
        int lwr, upr;
        Pgno chldPg;
        MemPage pPage = pCur.apPage[pCur.iPage];
        int c;

        /* pPage.nCell must be greater than zero. If this is the root-page
        ** the cursor would have been INVALID above and this for(;;) loop
        ** not run. If this is not the root-page, then the moveToChild() routine
        ** would have already detected db corruption. Similarly, pPage must
        ** be the right kind (index or table) of b-tree page. Otherwise
        ** a moveToChild() or moveToRoot() call would have detected corruption.  */
        Debug.Assert(pPage.nCell > 0);
        Debug.Assert(pPage.intKey == ((pIdxKey == null) ? 1 : 0));
        lwr = 0;
        upr = pPage.nCell - 1;
        if (biasRight != 0)
        {
          pCur.aiIdx[pCur.iPage] = (u16)upr;
        }
        else
        {
          pCur.aiIdx[pCur.iPage] = (u16)((upr + lwr) / 2);
        }
        for (; ; )
        {
          int idx = pCur.aiIdx[pCur.iPage]; /* Index of current cell in pPage */
          int pCell;                        /* Pointer to current cell in pPage */

          pCur.info.nSize = 0;
          pCell = findCell(pPage, idx) + pPage.childPtrSize;
          if (pPage.intKey != 0)
          {
            i64 nCellKey = 0;
            if (pPage.hasData != 0)
            {
              u32 Dummy0 = 0;
              pCell += getVarint32(pPage.aData, pCell, ref Dummy0);
            }
            getVarint(pPage.aData, pCell, ref nCellKey);
            if (nCellKey == intKey)
            {
              c = 0;
            }
            else if (nCellKey < intKey)
            {
              c = -1;
            }
            else
            {
              Debug.Assert(nCellKey > intKey);
              c = +1;
            }
            pCur.validNKey = true;
            pCur.info.nKey = nCellKey;
          }
          else
          {
            /* The maximum supported page-size is 32768 bytes. This means that
            ** the maximum number of record bytes stored on an index B-Tree
            ** page is at most 8198 bytes, which may be stored as a 2-byte
            ** varint. This information is used to attempt to avoid parsing
            ** the entire cell by checking for the cases where the record is
            ** stored entirely within the b-tree page by inspecting the first
            ** 2 bytes of the cell.
            */
            int nCell = pPage.aData[pCell + 0]; //pCell[0];
            if (0 == (nCell & 0x80) && nCell <= pPage.maxLocal)
            {
              /* This branch runs if the record-size field of the cell is a
              ** single byte varint and the record fits entirely on the main
              ** b-tree page.  */
              c = sqlite3VdbeRecordCompare(nCell, pPage.aData, pCell + 1, pIdxKey); //c = sqlite3VdbeRecordCompare( nCell, (void*)&pCell[1], pIdxKey );
            }
            else if (0 == (pPage.aData[pCell + 1] & 0x80)//!(pCell[1] & 0x80)
            && (nCell = ((nCell & 0x7f) << 7) + pPage.aData[pCell + 1]) <= pPage.maxLocal//pCell[1])<=pPage.maxLocal
            )
            {
              /* The record-size field is a 2 byte varint and the record
              ** fits entirely on the main b-tree page.  */
              c = sqlite3VdbeRecordCompare(nCell, pPage.aData, pCell + 2, pIdxKey); //c = sqlite3VdbeRecordCompare( nCell, (void*)&pCell[2], pIdxKey );
            }
            else
            {
              /* The record flows over onto one or more overflow pages. In
              ** this case the whole cell needs to be parsed, a buffer allocated
              ** and accessPayload() used to retrieve the record into the
              ** buffer before VdbeRecordCompare() can be called. */
              u8[] pCellKey;
              u8[] pCellBody = new u8[pPage.aData.Length - pCell + pPage.childPtrSize];
              Buffer.BlockCopy(pPage.aData, pCell - pPage.childPtrSize, pCellBody, 0, pCellBody.Length);//          u8 * const pCellBody = pCell - pPage->childPtrSize;
              btreeParseCellPtr( pPage, pCellBody, ref pCur.info );
              nCell = (int)pCur.info.nKey;
              pCellKey = new byte[nCell]; //sqlite3Malloc( nCell );
              //if ( pCellKey == null )
              //{
              //  rc = SQLITE_NOMEM;
              //  goto moveto_finish;
              //}
              rc = accessPayload(pCur, 0, (u32)nCell, pCellKey, 0);
              c = sqlite3VdbeRecordCompare(nCell, pCellKey, pIdxKey);
              pCellKey = null;// sqlite3_free( ref pCellKey );
              if (rc != 0) goto moveto_finish;
            }
          }
          if (c == 0)
          {
            if (pPage.intKey != 0 && 0 == pPage.leaf)
            {
              lwr = idx;
              upr = lwr - 1;
              break;
            }
            else
            {
              pRes = 0;
              rc = SQLITE_OK;
              goto moveto_finish;
            }
          }
          if (c < 0)
          {
            lwr = idx + 1;
          }
          else
          {
            upr = idx - 1;
          }
          if (lwr > upr)
          {
            break;
          }
          pCur.aiIdx[pCur.iPage] = (u16)((lwr + upr) / 2);
        }
        Debug.Assert(lwr == upr + 1);
        Debug.Assert(pPage.isInit != 0);
        if (pPage.leaf != 0)
        {
          chldPg = 0;
        }
        else if (lwr >= pPage.nCell)
        {
          chldPg = sqlite3Get4byte(pPage.aData, pPage.hdrOffset + 8);
        }
        else
        {
          chldPg = sqlite3Get4byte(pPage.aData, findCell(pPage, lwr));
        }
        if (chldPg == 0)
        {
          Debug.Assert(pCur.aiIdx[pCur.iPage] < pCur.apPage[pCur.iPage].nCell);
          pRes = c;
          rc = SQLITE_OK;
          goto moveto_finish;
        }
        pCur.aiIdx[pCur.iPage] = (u16)lwr;
        pCur.info.nSize = 0;
        pCur.validNKey = false;
        rc = moveToChild(pCur, chldPg);
        if (rc != 0) goto moveto_finish;
      }
    moveto_finish:
      return rc;
    }


    /*
    ** Return TRUE if the cursor is not pointing at an entry of the table.
    **
    ** TRUE will be returned after a call to sqlite3BtreeNext() moves
    ** past the last entry in the table or sqlite3BtreePrev() moves past
    ** the first entry.  TRUE is also returned if the table is empty.
    */
    static bool sqlite3BtreeEof(BtCursor pCur)
    {
      /* TODO: What if the cursor is in CURSOR_REQUIRESEEK but all table entries
      ** have been deleted? This API will need to change to return an error code
      ** as well as the boolean result value.
      */
      return (CURSOR_VALID != pCur.eState);
    }

    /*
    ** Advance the cursor to the next entry in the database.  If
    ** successful then set pRes=0.  If the cursor
    ** was already pointing to the last entry in the database before
    ** this routine was called, then set pRes=1.
    */
    static int sqlite3BtreeNext(BtCursor pCur, ref int pRes)
    {
      int rc;
      int idx;
      MemPage pPage;

      Debug.Assert(cursorHoldsMutex(pCur));
      rc = restoreCursorPosition(pCur);
      if (rc != SQLITE_OK)
      {
        return rc;
      }
      // Not needed in C# // Debug.Assert( pRes != 0 );
      if (CURSOR_INVALID == pCur.eState)
      {
        pRes = 1;
        return SQLITE_OK;
      }
      if (pCur.skipNext > 0)
      {
        pCur.skipNext = 0;
        pRes = 0;
        return SQLITE_OK;
      }
      pCur.skipNext = 0;

      pPage = pCur.apPage[pCur.iPage];
      idx = ++pCur.aiIdx[pCur.iPage];
      Debug.Assert(pPage.isInit != 0);
      Debug.Assert(idx <= pPage.nCell);

      pCur.info.nSize = 0;
      pCur.validNKey = false;
      if (idx >= pPage.nCell)
      {
        if (0 == pPage.leaf)
        {
          rc = moveToChild(pCur, sqlite3Get4byte(pPage.aData, pPage.hdrOffset + 8));
          if (rc != 0) return rc;
          rc = moveToLeftmost(pCur);
          pRes = 0;
          return rc;
        }
        do
        {
          if (pCur.iPage == 0)
          {
            pRes = 1;
            pCur.eState = CURSOR_INVALID;
            return SQLITE_OK;
          }
          moveToParent(pCur);
          pPage = pCur.apPage[pCur.iPage];
        } while (pCur.aiIdx[pCur.iPage] >= pPage.nCell);
        pRes = 0;
        if (pPage.intKey != 0)
        {
          rc = sqlite3BtreeNext(pCur, ref pRes);
        }
        else
        {
          rc = SQLITE_OK;
        }
        return rc;
      }
      pRes = 0;
      if (pPage.leaf != 0)
      {
        return SQLITE_OK;
      }
      rc = moveToLeftmost(pCur);
      return rc;
    }


    /*
    ** Step the cursor to the back to the previous entry in the database.  If
    ** successful then set pRes=0.  If the cursor
    ** was already pointing to the first entry in the database before
    ** this routine was called, then set pRes=1.
    */
    static int sqlite3BtreePrevious(BtCursor pCur, ref int pRes)
    {
      int rc;
      MemPage pPage;

      Debug.Assert(cursorHoldsMutex(pCur));
      rc = restoreCursorPosition(pCur);
      if (rc != SQLITE_OK)
      {
        return rc;
      }
      pCur.atLast = 0;
      if (CURSOR_INVALID == pCur.eState)
      {
        pRes = 1;
        return SQLITE_OK;
      }
      if (pCur.skipNext < 0)
      {
        pCur.skipNext = 0;
        pRes = 0;
        return SQLITE_OK;
      }
      pCur.skipNext = 0;

      pPage = pCur.apPage[pCur.iPage];
      Debug.Assert(pPage.isInit != 0);
      if (0 == pPage.leaf)
      {
        int idx = pCur.aiIdx[pCur.iPage];
        rc = moveToChild(pCur, sqlite3Get4byte(pPage.aData, findCell(pPage, idx)));
        if (rc != 0)
        {
          return rc;
        }
        rc = moveToRightmost(pCur);
      }
      else
      {
        while (pCur.aiIdx[pCur.iPage] == 0)
        {
          if (pCur.iPage == 0)
          {
            pCur.eState = CURSOR_INVALID;
            pRes = 1;
            return SQLITE_OK;
          }
          moveToParent(pCur);
        }
        pCur.info.nSize = 0;
        pCur.validNKey = false;

        pCur.aiIdx[pCur.iPage]--;
        pPage = pCur.apPage[pCur.iPage];
        if (pPage.intKey != 0 && 0 == pPage.leaf)
        {
          rc = sqlite3BtreePrevious(pCur, ref pRes);
        }
        else
        {
          rc = SQLITE_OK;
        }
      }
      pRes = 0;
      return rc;
    }

    /*
    ** Allocate a new page from the database file.
    **
    ** The new page is marked as dirty.  (In other words, sqlite3PagerWrite()
    ** has already been called on the new page.)  The new page has also
    ** been referenced and the calling routine is responsible for calling
    ** sqlite3PagerUnref() on the new page when it is done.
    **
    ** SQLITE_OK is returned on success.  Any other return value indicates
    ** an error.  ppPage and pPgno are undefined in the event of an error.
    ** Do not invoke sqlite3PagerUnref() on ppPage if an error is returned.
    **
    ** If the "nearby" parameter is not 0, then a (feeble) effort is made to
    ** locate a page close to the page number "nearby".  This can be used in an
    ** attempt to keep related pages close to each other in the database file,
    ** which in turn can make database access faster.
    **
    ** If the "exact" parameter is not 0, and the page-number nearby exists
    ** anywhere on the free-list, then it is guarenteed to be returned. This
    ** is only used by auto-vacuum databases when allocating a new table.
    */
    static int allocateBtreePage(
    BtShared pBt,
    ref MemPage ppPage,
    ref Pgno pPgno,
    Pgno nearby,
    u8 exact
    )
    {
      MemPage pPage1;
      int rc;
      u32 n;     /* Number of pages on the freelist */
      u32 k;     /* Number of leaves on the trunk of the freelist */
      MemPage pTrunk = null;
      MemPage pPrevTrunk = null;
      Pgno mxPage;     /* Total size of the database file */

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      pPage1 = pBt.pPage1;
      mxPage = pagerPagecount(pBt);
      n = sqlite3Get4byte(pPage1.aData, 36);
      testcase(n == mxPage - 1);
      if (n >= mxPage)
      {
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }
      if (n > 0)
      {
        /* There are pages on the freelist.  Reuse one of those pages. */
        Pgno iTrunk;
        u8 searchList = 0; /* If the free-list must be searched for 'nearby' */

        /* If the 'exact' parameter was true and a query of the pointer-map
        ** shows that the page 'nearby' is somewhere on the free-list, then
        ** the entire-list will be searched for that page.
        */
#if !SQLITE_OMIT_AUTOVACUUM
        if (exact != 0 && nearby <= mxPage)
        {
          u8 eType = 0;
          Debug.Assert(nearby > 0);
          Debug.Assert(pBt.autoVacuum);
          u32 Dummy0 = 0; rc = ptrmapGet(pBt, nearby, ref eType, ref Dummy0);
          if (rc != 0) return rc;
          if (eType == PTRMAP_FREEPAGE)
          {
            searchList = 1;
          }
          pPgno = nearby;
        }
#endif

        /* Decrement the free-list count by 1. Set iTrunk to the index of the
** first free-list trunk page. iPrevTrunk is initially 1.
*/
        rc = sqlite3PagerWrite(pPage1.pDbPage);
        if (rc != 0) return rc;
        sqlite3Put4byte(pPage1.aData, (u32)36, n - 1);

        /* The code within this loop is run only once if the 'searchList' variable
        ** is not true. Otherwise, it runs once for each trunk-page on the
        ** free-list until the page 'nearby' is located.
        */
        do
        {
          pPrevTrunk = pTrunk;
          if (pPrevTrunk != null)
          {
            iTrunk = sqlite3Get4byte(pPrevTrunk.aData, 0);
          }
          else
          {
            iTrunk = sqlite3Get4byte(pPage1.aData, 32);
          }
          testcase(iTrunk == mxPage);
          if (iTrunk > mxPage)
          {
#if SQLITE_DEBUG || DEBUG
            rc = SQLITE_CORRUPT_BKPT();
#else
rc = SQLITE_CORRUPT_BKPT;
#endif
          }
          else
          {
            rc = btreeGetPage(pBt, iTrunk, ref pTrunk, 0);
          }
          if (rc != 0)
          {
            pTrunk = null;
            goto end_allocate_page;
          }

          k = sqlite3Get4byte(pTrunk.aData, 4);
          if (k == 0 && 0 == searchList)
          {
            /* The trunk has no leaves and the list is not being searched.
            ** So extract the trunk page itself and use it as the newly
            ** allocated page */
            Debug.Assert(pPrevTrunk == null);
            rc = sqlite3PagerWrite(pTrunk.pDbPage);
            if (rc != 0)
            {
              goto end_allocate_page;
            }
            pPgno = iTrunk;
            Buffer.BlockCopy(pTrunk.aData, 0, pPage1.aData, 32, 4);//memcpy( pPage1.aData[32], ref pTrunk.aData[0], 4 );
            ppPage = pTrunk;
            pTrunk = null;
            TRACE("ALLOCATE: %d trunk - %d free pages left\n", pPgno, n - 1);
          }
          else if (k > (u32)(pBt.usableSize / 4 - 2))
          {
            /* Value of k is out of range.  Database corruption */
#if SQLITE_DEBUG || DEBUG
            rc = SQLITE_CORRUPT_BKPT();
#else
rc =  SQLITE_CORRUPT_BKPT;
#endif
            goto end_allocate_page;
#if !SQLITE_OMIT_AUTOVACUUM
          }
          else if (searchList != 0 && nearby == iTrunk)
          {
            /* The list is being searched and this trunk page is the page
            ** to allocate, regardless of whether it has leaves.
            */
            Debug.Assert(pPgno == iTrunk);
            ppPage = pTrunk;
            searchList = 0;
            rc = sqlite3PagerWrite(pTrunk.pDbPage);
            if (rc != 0)
            {
              goto end_allocate_page;
            }
            if (k == 0)
            {
              if (null == pPrevTrunk)
              {
                //memcpy(pPage1.aData[32], pTrunk.aData[0], 4);
                pPage1.aData[32 + 0] = pTrunk.aData[0 + 0];
                pPage1.aData[32 + 1] = pTrunk.aData[0 + 1];
                pPage1.aData[32 + 2] = pTrunk.aData[0 + 2];
                pPage1.aData[32 + 3] = pTrunk.aData[0 + 3];
              }
              else
              {
                //memcpy(pPrevTrunk.aData[0], pTrunk.aData[0], 4);
                pPrevTrunk.aData[0 + 0] = pTrunk.aData[0 + 0];
                pPrevTrunk.aData[0 + 1] = pTrunk.aData[0 + 1];
                pPrevTrunk.aData[0 + 2] = pTrunk.aData[0 + 2];
                pPrevTrunk.aData[0 + 3] = pTrunk.aData[0 + 3];
              }
            }
            else
            {
              /* The trunk page is required by the caller but it contains
              ** pointers to free-list leaves. The first leaf becomes a trunk
              ** page in this case.
              */
              MemPage pNewTrunk = new MemPage();
              Pgno iNewTrunk = sqlite3Get4byte(pTrunk.aData, 8);
              if (iNewTrunk > mxPage)
              {
#if SQLITE_DEBUG || DEBUG
                rc = SQLITE_CORRUPT_BKPT();
#else
rc = SQLITE_CORRUPT_BKPT;
#endif
                goto end_allocate_page;
              }
              testcase(iNewTrunk == mxPage);
              rc = btreeGetPage(pBt, iNewTrunk, ref pNewTrunk, 0);
              if (rc != SQLITE_OK)
              {
                goto end_allocate_page;
              }
              rc = sqlite3PagerWrite(pNewTrunk.pDbPage);
              if (rc != SQLITE_OK)
              {
                releasePage(pNewTrunk);
                goto end_allocate_page;
              }
              //memcpy(pNewTrunk.aData[0], pTrunk.aData[0], 4);
              pNewTrunk.aData[0 + 0] = pTrunk.aData[0 + 0];
              pNewTrunk.aData[0 + 1] = pTrunk.aData[0 + 1];
              pNewTrunk.aData[0 + 2] = pTrunk.aData[0 + 2];
              pNewTrunk.aData[0 + 3] = pTrunk.aData[0 + 3];
              sqlite3Put4byte(pNewTrunk.aData, (u32)4, (u32)(k - 1));
              Buffer.BlockCopy(pTrunk.aData, 12, pNewTrunk.aData, 8, (int)(k - 1) * 4);//memcpy( pNewTrunk.aData[8], ref pTrunk.aData[12], ( k - 1 ) * 4 );
              releasePage(pNewTrunk);
              if (null == pPrevTrunk)
              {
                Debug.Assert(sqlite3PagerIswriteable(pPage1.pDbPage));
                sqlite3Put4byte(pPage1.aData, (u32)32, iNewTrunk);
              }
              else
              {
                rc = sqlite3PagerWrite(pPrevTrunk.pDbPage);
                if (rc != 0)
                {
                  goto end_allocate_page;
                }
                sqlite3Put4byte(pPrevTrunk.aData, (u32)0, iNewTrunk);
              }
            }
            pTrunk = null;
            TRACE("ALLOCATE: %d trunk - %d free pages left\n", pPgno, n - 1);
#endif
          }
          else if (k > 0)
          {
            /* Extract a leaf from the trunk */
            u32 closest;
            Pgno iPage;
            byte[] aData = pTrunk.aData;
            rc = sqlite3PagerWrite(pTrunk.pDbPage);
            if (rc != 0)
            {
              goto end_allocate_page;
            }
            if (nearby > 0)
            {
              u32 i;
              int dist;
              closest = 0;
              dist = (int)(sqlite3Get4byte(aData, 8) - nearby);
              if (dist < 0) dist = -dist;
              for (i = 1; i < k; i++)
              {
                int d2 = (int)(sqlite3Get4byte(aData, 8 + i * 4) - nearby);
                if (d2 < 0) d2 = -d2;
                if (d2 < dist)
                {
                  closest = i;
                  dist = d2;
                }
              }
            }
            else
            {
              closest = 0;
            }

            iPage = sqlite3Get4byte(aData, 8 + closest * 4);
            testcase(iPage == mxPage);
            if (iPage > mxPage)
            {
#if SQLITE_DEBUG || DEBUG
              rc = SQLITE_CORRUPT_BKPT();
#else
rc = SQLITE_CORRUPT_BKPT;
#endif
              goto end_allocate_page;
            }
            testcase(iPage == mxPage);
            if (0 == searchList || iPage == nearby)
            {
              int noContent;
              pPgno = iPage;
              TRACE("ALLOCATE: %d was leaf %d of %d on trunk %d" +
              ": %d more free pages\n",
              pPgno, closest + 1, k, pTrunk.pgno, n - 1);
              if (closest < k - 1)
              {
                Buffer.BlockCopy(aData, (int)(4 + k * 4), aData, 8 + (int)closest * 4, 4);//memcpy( aData[8 + closest * 4], ref aData[4 + k * 4], 4 );
              }
              sqlite3Put4byte(aData, (u32)4, (k - 1));// sqlite3Put4byte( aData, 4, k - 1 );
              Debug.Assert(sqlite3PagerIswriteable(pTrunk.pDbPage));
              noContent = !btreeGetHasContent(pBt, pPgno) ? 1 : 0;
              rc = btreeGetPage(pBt, pPgno, ref ppPage, noContent);
              if (rc == SQLITE_OK)
              {
                rc = sqlite3PagerWrite((ppPage).pDbPage);
                if (rc != SQLITE_OK)
                {
                  releasePage(ppPage);
                }
              }
              searchList = 0;
            }
          }
          releasePage(pPrevTrunk);
          pPrevTrunk = null;
        } while (searchList != 0);
      }
      else
      {
        /* There are no pages on the freelist, so create a new page at the
        ** end of the file */
        int nPage = (int)pagerPagecount(pBt);
        pPgno = (u32)nPage + 1;

        if (pPgno == PENDING_BYTE_PAGE(pBt))
        {
          (pPgno)++;
        }

#if !SQLITE_OMIT_AUTOVACUUM
        if (pBt.autoVacuum && PTRMAP_ISPAGE(pBt, pPgno))
        {
          /* If pPgno refers to a pointer-map page, allocate two new pages
          ** at the end of the file instead of one. The first allocated page
          ** becomes a new pointer-map page, the second is used by the caller.
          */
          MemPage pPg = null;
          TRACE("ALLOCATE: %d from end of file (pointer-map page)\n", pPgno);
          Debug.Assert(pPgno != PENDING_BYTE_PAGE(pBt));
          rc = btreeGetPage(pBt, pPgno, ref pPg, 0);
          if (rc == SQLITE_OK)
          {
            rc = sqlite3PagerWrite(pPg.pDbPage);
            releasePage(pPg);
          }
          if (rc != 0) return rc;
          (pPgno)++;
          if (pPgno == PENDING_BYTE_PAGE(pBt)) { (pPgno)++; }
        }
#endif

        Debug.Assert(pPgno != PENDING_BYTE_PAGE(pBt));
        rc = btreeGetPage(pBt, pPgno, ref ppPage, 0);
        if (rc != 0) return rc;
        rc = sqlite3PagerWrite((ppPage).pDbPage);
        if (rc != SQLITE_OK)
        {
          releasePage(ppPage);
        }
        TRACE("ALLOCATE: %d from end of file\n", pPgno);
      }

      Debug.Assert(pPgno != PENDING_BYTE_PAGE(pBt));

    end_allocate_page:
      releasePage(pTrunk);
      releasePage(pPrevTrunk);
      if (rc == SQLITE_OK)
      {
        if (sqlite3PagerPageRefcount((ppPage).pDbPage) > 1)
        {
          releasePage(ppPage);
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
        (ppPage).isInit = 0;
      }
      else
      {
        ppPage = null;
      }
      return rc;
    }

    /*
    ** This function is used to add page iPage to the database file free-list.
    ** It is assumed that the page is not already a part of the free-list.
    **
    ** The value passed as the second argument to this function is optional.
    ** If the caller happens to have a pointer to the MemPage object
    ** corresponding to page iPage handy, it may pass it as the second value.
    ** Otherwise, it may pass NULL.
    **
    ** If a pointer to a MemPage object is passed as the second argument,
    ** its reference count is not altered by this function.
    */
    static int freePage2(BtShared pBt, MemPage pMemPage, Pgno iPage)
    {
      MemPage pTrunk = null;                /* Free-list trunk page */
      Pgno iTrunk = 0;                    /* Page number of free-list trunk page */
      MemPage pPage1 = pBt.pPage1;      /* Local reference to page 1 */
      MemPage pPage;                     /* Page being freed. May be NULL. */
      int rc;                             /* Return Code */
      int nFree;                          /* Initial number of pages on free-list */

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      Debug.Assert(iPage > 1);
      Debug.Assert(null == pMemPage || pMemPage.pgno == iPage);

      if (pMemPage != null)
      {
        pPage = pMemPage;
        sqlite3PagerRef(pPage.pDbPage);
      }
      else
      {
        pPage = btreePageLookup(pBt, iPage);
      }

      /* Increment the free page count on pPage1 */
      rc = sqlite3PagerWrite(pPage1.pDbPage);
      if (rc != 0) goto freepage_out;
      nFree = (int)sqlite3Get4byte(pPage1.aData, 36);
      sqlite3Put4byte(pPage1.aData, 36, nFree + 1);

#if SQLITE_SECURE_DELETE
/* If the SQLITE_SECURE_DELETE compile-time option is enabled, then
** always fully overwrite deleted information with zeros.
*/
if( (!pPage && (rc = btreeGetPage(pBt, iPage, ref pPage, 0)))
||            (rc = sqlite3PagerWrite(pPage.pDbPage))
){
goto freepage_out;
}
memset(pPage.aData, 0, pPage.pBt.pageSize);
#endif

      /* If the database supports auto-vacuum, write an entry in the pointer-map
** to indicate that the page is free.
*/
#if !SQLITE_OMIT_AUTOVACUUM //   if ( ISAUTOVACUUM )
      if (pBt.autoVacuum)
#else
if (false)
#endif
      {
        ptrmapPut(pBt, iPage, PTRMAP_FREEPAGE, 0, ref rc);
        if (rc != 0) goto freepage_out;
      }

      /* Now manipulate the actual database free-list structure. There are two
      ** possibilities. If the free-list is currently empty, or if the first
      ** trunk page in the free-list is full, then this page will become a
      ** new free-list trunk page. Otherwise, it will become a leaf of the
      ** first trunk page in the current free-list. This block tests if it
      ** is possible to add the page as a new free-list leaf.
      */
      if (nFree != 0)
      {
        u32 nLeaf;                /* Initial number of leaf cells on trunk page */

        iTrunk = sqlite3Get4byte(pPage1.aData, 32);
        rc = btreeGetPage(pBt, iTrunk, ref pTrunk, 0);
        if (rc != SQLITE_OK)
        {
          goto freepage_out;
        }

        nLeaf = sqlite3Get4byte(pTrunk.aData, 4);
        Debug.Assert(pBt.usableSize > 32);
        if (nLeaf > (u32)pBt.usableSize / 4 - 2)
        {
#if SQLITE_DEBUG || DEBUG
          rc = SQLITE_CORRUPT_BKPT();
#else
rc = SQLITE_CORRUPT_BKPT;
#endif
          goto freepage_out;
        }
        if (nLeaf < (u32)pBt.usableSize / 4 - 8)
        {
          /* In this case there is room on the trunk page to insert the page
          ** being freed as a new leaf.
          **
          ** Note that the trunk page is not really full until it contains
          ** usableSize/4 - 2 entries, not usableSize/4 - 8 entries as we have
          ** coded.  But due to a coding error in versions of SQLite prior to
          ** 3.6.0, databases with freelist trunk pages holding more than
          ** usableSize/4 - 8 entries will be reported as corrupt.  In order
          ** to maintain backwards compatibility with older versions of SQLite,
          ** we will continue to restrict the number of entries to usableSize/4 - 8
          ** for now.  At some point in the future (once everyone has upgraded
          ** to 3.6.0 or later) we should consider fixing the conditional above
          ** to read "usableSize/4-2" instead of "usableSize/4-8".
          */
          rc = sqlite3PagerWrite(pTrunk.pDbPage);
          if (rc == SQLITE_OK)
          {
            sqlite3Put4byte(pTrunk.aData, (u32)4, nLeaf + 1);
            sqlite3Put4byte(pTrunk.aData, (u32)8 + nLeaf * 4, iPage);
#if !SQLITE_SECURE_DELETE
            if (pPage != null)
            {
              sqlite3PagerDontWrite(pPage.pDbPage);
            }
#endif
            rc = btreeSetHasContent(pBt, iPage);
          }
          TRACE("FREE-PAGE: %d leaf on trunk page %d\n", iPage, pTrunk.pgno);
          goto freepage_out;
        }
      }

      /* If control flows to this point, then it was not possible to add the
      ** the page being freed as a leaf page of the first trunk in the free-list.
      ** Possibly because the free-list is empty, or possibly because the
      ** first trunk in the free-list is full. Either way, the page being freed
      ** will become the new first trunk page in the free-list.
      */
      if (pPage == null && SQLITE_OK != (rc = btreeGetPage(pBt, iPage, ref pPage, 0)))
      {
        goto freepage_out;
      }
      rc = sqlite3PagerWrite(pPage.pDbPage);
      if (rc != SQLITE_OK)
      {
        goto freepage_out;
      }
      sqlite3Put4byte(pPage.aData, iTrunk);
      sqlite3Put4byte(pPage.aData, 4, 0);
      sqlite3Put4byte(pPage1.aData, (u32)32, iPage);
      TRACE("FREE-PAGE: %d new trunk page replacing %d\n", pPage.pgno, iTrunk);

    freepage_out:
      if (pPage != null)
      {
        pPage.isInit = 0;
      }
      releasePage(pPage);
      releasePage(pTrunk);
      return rc;
    }
    static void freePage(MemPage pPage, ref int pRC)
    {
      if ((pRC) == SQLITE_OK)
      {
        pRC = freePage2(pPage.pBt, pPage, pPage.pgno);
      }
    }

    /*
    ** Free any overflow pages associated with the given Cell.
    */
    static int clearCell(MemPage pPage, int pCell)
    {
      BtShared pBt = pPage.pBt;
      CellInfo info = new CellInfo();
      Pgno ovflPgno;
      int rc;
      int nOvfl;
      u16 ovflPageSize;

      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      btreeParseCellPtr( pPage, pCell, ref info );
      if (info.iOverflow == 0)
      {
        return SQLITE_OK;  /* No overflow pages. Return without doing anything */
      }
      ovflPgno = sqlite3Get4byte(pPage.aData, pCell, info.iOverflow);
      Debug.Assert(pBt.usableSize > 4);
      ovflPageSize = (u16)(pBt.usableSize - 4);
      nOvfl = (int)((info.nPayload - info.nLocal + ovflPageSize - 1) / ovflPageSize);
      Debug.Assert(ovflPgno == 0 || nOvfl > 0);
      while (nOvfl-- != 0)
      {
        Pgno iNext = 0;
        MemPage pOvfl = null;
        if (ovflPgno < 2 || ovflPgno > pagerPagecount(pBt))
        {
          /* 0 is not a legal page number and page 1 cannot be an
          ** overflow page. Therefore if ovflPgno<2 or past the end of the
          ** file the database must be corrupt. */
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
        if (nOvfl != 0)
        {
          rc = getOverflowPage(pBt, ovflPgno, ref pOvfl, ref iNext);
          if (rc != 0) return rc;
        }
        rc = freePage2(pBt, pOvfl, ovflPgno);
        if (pOvfl != null)
        {
          sqlite3PagerUnref(pOvfl.pDbPage);
        }
        if (rc != 0) return rc;
        ovflPgno = iNext;
      }
      return SQLITE_OK;
    }

    /*
    ** Create the byte sequence used to represent a cell on page pPage
    ** and write that byte sequence into pCell[].  Overflow pages are
    ** allocated and filled in as necessary.  The calling procedure
    ** is responsible for making sure sufficient space has been allocated
    ** for pCell[].
    **
    ** Note that pCell does not necessary need to point to the pPage.aData
    ** area.  pCell might point to some temporary storage.  The cell will
    ** be constructed in this temporary area then copied into pPage.aData
    ** later.
    */
    static int fillInCell(
    MemPage pPage,            /* The page that contains the cell */
    byte[] pCell,             /* Complete text of the cell */
    byte[] pKey, i64 nKey,    /* The key */
    byte[] pData, int nData,  /* The data */
    int nZero,                /* Extra zero bytes to append to pData */
    ref int pnSize            /* Write cell size here */
    )
    {
      int nPayload;
      u8[] pSrc; int pSrcIndex = 0;
      int nSrc, n, rc;
      int spaceLeft;
      MemPage pOvfl = null;
      MemPage pToRelease = null;
      byte[] pPrior; int pPriorIndex = 0;
      byte[] pPayload; int pPayloadIndex = 0;
      BtShared pBt = pPage.pBt;
      Pgno pgnoOvfl = 0;
      int nHeader;
      CellInfo info = new CellInfo();

      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));

      /* pPage is not necessarily writeable since pCell might be auxiliary
      ** buffer space that is separate from the pPage buffer area */
      // TODO -- Determine if the following Assert is needed under c#
      //Debug.Assert( pCell < pPage.aData || pCell >= &pPage.aData[pBt.pageSize]
      //          || sqlite3PagerIswriteable(pPage.pDbPage) );

      /* Fill in the header. */
      nHeader = 0;
      if (0 == pPage.leaf)
      {
        nHeader += 4;
      }
      if (pPage.hasData != 0)
      {
        nHeader += (int)putVarint(pCell, nHeader, (int)(nData + nZero)); //putVarint( pCell[nHeader], nData + nZero );
      }
      else
      {
        nData = nZero = 0;
      }
      nHeader += putVarint(pCell, nHeader, (u64)nKey); //putVarint( pCell[nHeader], *(u64*)&nKey );
      btreeParseCellPtr( pPage, pCell, ref info );
      Debug.Assert(info.nHeader == nHeader);
      Debug.Assert(info.nKey == nKey);
      Debug.Assert(info.nData == (u32)(nData + nZero));

      /* Fill in the payload */
      nPayload = nData + nZero;
      if (pPage.intKey != 0)
      {
        pSrc = pData;
        nSrc = nData;
        nData = 0;
      }
      else
      {
        if (NEVER(nKey > 0x7fffffff || pKey == null))
        {
#if SQLITE_DEBUG || DEBUG
          return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
        }
        nPayload += (int)nKey;
        pSrc = pKey;
        nSrc = (int)nKey;
      }
      pnSize = info.nSize;
      spaceLeft = info.nLocal;
      //  pPayload = &pCell[nHeader];
      pPayload = pCell;
      pPayloadIndex = nHeader;
      //  pPrior = &pCell[info.iOverflow];
      pPrior = pCell;
      pPriorIndex = info.iOverflow;

      while (nPayload > 0)
      {
        if (spaceLeft == 0)
        {
#if !SQLITE_OMIT_AUTOVACUUM
          Pgno pgnoPtrmap = pgnoOvfl; /* Overflow page pointer-map entry page */
          if (pBt.autoVacuum)
          {
            do
            {
              pgnoOvfl++;
            } while (
            PTRMAP_ISPAGE(pBt, pgnoOvfl) || pgnoOvfl == PENDING_BYTE_PAGE(pBt)
            );
          }
#endif
          rc = allocateBtreePage(pBt, ref pOvfl, ref pgnoOvfl, pgnoOvfl, 0);
#if !SQLITE_OMIT_AUTOVACUUM
          /* If the database supports auto-vacuum, and the second or subsequent
** overflow page is being allocated, add an entry to the pointer-map
** for that page now.
**
** If this is the first overflow page, then write a partial entry
** to the pointer-map. If we write nothing to this pointer-map slot,
** then the optimistic overflow chain processing in clearCell()
** may misinterpret the uninitialised values and delete the
** wrong pages from the database.
*/
          if (pBt.autoVacuum && rc == SQLITE_OK)
          {
            u8 eType = (u8)(pgnoPtrmap != 0 ? PTRMAP_OVERFLOW2 : PTRMAP_OVERFLOW1);
            ptrmapPut(pBt, pgnoOvfl, eType, pgnoPtrmap, ref rc);
            if (rc != 0)
            {
              releasePage(pOvfl);
            }
          }
#endif
          if (rc != 0)
          {
            releasePage(pToRelease);
            return rc;
          }

          /* If pToRelease is not zero than pPrior points into the data area
          ** of pToRelease.  Make sure pToRelease is still writeable. */
          Debug.Assert(pToRelease == null || sqlite3PagerIswriteable(pToRelease.pDbPage));

          /* If pPrior is part of the data area of pPage, then make sure pPage
          ** is still writeable */
          // TODO -- Determine if the following Assert is needed under c#
          //Debug.Assert( pPrior < pPage.aData || pPrior >= &pPage.aData[pBt.pageSize]
          //      || sqlite3PagerIswriteable(pPage.pDbPage) );

          sqlite3Put4byte(pPrior, pPriorIndex, pgnoOvfl);
          releasePage(pToRelease);
          pToRelease = pOvfl;
          pPrior = pOvfl.aData; pPriorIndex = 0;
          sqlite3Put4byte(pPrior, 0);
          pPayload = pOvfl.aData; pPayloadIndex = 4; //&pOvfl.aData[4];
          spaceLeft = pBt.usableSize - 4;
        }
        n = nPayload;
        if (n > spaceLeft) n = spaceLeft;

        /* If pToRelease is not zero than pPayload points into the data area
        ** of pToRelease.  Make sure pToRelease is still writeable. */
        Debug.Assert(pToRelease == null || sqlite3PagerIswriteable(pToRelease.pDbPage));

        /* If pPayload is part of the data area of pPage, then make sure pPage
        ** is still writeable */
        // TODO -- Determine if the following Assert is needed under c#
        //Debug.Assert( pPayload < pPage.aData || pPayload >= &pPage.aData[pBt.pageSize]
        //        || sqlite3PagerIswriteable(pPage.pDbPage) );

        if (nSrc > 0)
        {
          if (n > nSrc) n = nSrc;
          Debug.Assert(pSrc != null);
          Buffer.BlockCopy(pSrc, pSrcIndex, pPayload, pPayloadIndex, n);//memcpy(pPayload, pSrc, n);
        }
        else
        {
          byte[] pZeroBlob = new byte[n]; // memset(pPayload, 0, n);
          Buffer.BlockCopy(pZeroBlob, 0, pPayload, pPayloadIndex, n);
        }
        nPayload -= n;
        pPayloadIndex += n;// pPayload += n;
        pSrcIndex += n;// pSrc += n;
        nSrc -= n;
        spaceLeft -= n;
        if (nSrc == 0)
        {
          nSrc = nData;
          pSrc = pData;
        }
      }
      releasePage(pToRelease);
      return SQLITE_OK;
    }

    /*
    ** Remove the i-th cell from pPage.  This routine effects pPage only.
    ** The cell content is not freed or deallocated.  It is assumed that
    ** the cell content has been copied someplace else.  This routine just
    ** removes the reference to the cell from pPage.
    **
    ** "sz" must be the number of bytes in the cell.
    */
    static void dropCell(MemPage pPage, int idx, int sz, ref int pRC)
    {
      int i;          /* Loop counter */
      int pc;         /* Offset to cell content of cell being deleted */
      u8[] data;      /* pPage.aData */
      int ptr;        /* Used to move bytes around within data[] */
      int rc;         /* The return code */
      int hdr;        /* Beginning of the header.  0 most pages.  100 page 1 */

      if (pRC != 0) return;

      Debug.Assert(idx >= 0 && idx < pPage.nCell);
      Debug.Assert(sz == cellSize(pPage, idx));
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      data = pPage.aData;
      ptr = pPage.cellOffset + 2 * idx; //ptr = &data[pPage.cellOffset + 2 * idx];
      pc = get2byte(data, ptr);
      hdr = pPage.hdrOffset;
      testcase(pc == get2byte(data, hdr + 5));
      testcase(pc + sz == pPage.pBt.usableSize);
      if (pc < get2byte(data, hdr + 5) || pc + sz > pPage.pBt.usableSize)
      {
#if SQLITE_DEBUG || DEBUG
        pRC = SQLITE_CORRUPT_BKPT();
#else
pRC = SQLITE_CORRUPT_BKPT;
#endif

        return;
      }
      rc = freeSpace(pPage, pc, sz);
      if (rc != 0)
      {
        pRC = rc;
        return;
      }
      //for ( i = idx + 1 ; i < pPage.nCell ; i++, ptr += 2 )
      //{
      //  ptr[0] = ptr[2];
      //  ptr[1] = ptr[3];
      //}
      Buffer.BlockCopy(data, ptr + 2, data, ptr, (pPage.nCell - 1 - idx) * 2);
      pPage.nCell--;
      data[pPage.hdrOffset + 3] = (byte)(pPage.nCell >> 8); data[pPage.hdrOffset + 4] = (byte)(pPage.nCell); //put2byte( data, hdr + 3, pPage.nCell );
      pPage.nFree += 2;
    }

    /*
    ** Insert a new cell on pPage at cell index "i".  pCell points to the
    ** content of the cell.
    **
    ** If the cell content will fit on the page, then put it there.  If it
    ** will not fit, then make a copy of the cell content into pTemp if
    ** pTemp is not null.  Regardless of pTemp, allocate a new entry
    ** in pPage.aOvfl[] and make it point to the cell content (either
    ** in pTemp or the original pCell) and also record its index.
    ** Allocating a new entry in pPage.aCell[] implies that
    ** pPage.nOverflow is incremented.
    **
    ** If nSkip is non-zero, then do not copy the first nSkip bytes of the
    ** cell. The caller will overwrite them after this function returns. If
    ** nSkip is non-zero, then pCell may not point to an invalid memory location
    ** (but pCell+nSkip is always valid).
    */
    static void insertCell(
    MemPage pPage,      /* Page into which we are copying */
    int i,              /* New cell becomes the i-th cell of the page */
    u8[] pCell,         /* Content of the new cell */
    int sz,             /* Bytes of content in pCell */
    u8[] pTemp,         /* Temp storage space for pCell, if needed */
    Pgno iChild,        /* If non-zero, replace first 4 bytes with this value */
    ref int pRC         /* Read and write return code from here */
    )
    {
      int idx = 0;      /* Where to write new cell content in data[] */
      int j;            /* Loop counter */
      int end;          /* First byte past the last cell pointer in data[] */
      int ins;          /* Index in data[] where new cell pointer is inserted */
      int cellOffset;   /* Address of first cell pointer in data[] */
      u8[] data;        /* The content of the whole page */
      u8 ptr;           /* Used for moving information around in data[] */

      int nSkip = (iChild != 0 ? 4 : 0);

      if (pRC != 0) return;

      Debug.Assert(i >= 0 && i <= pPage.nCell + pPage.nOverflow);
      Debug.Assert(pPage.nCell <= MX_CELL(pPage.pBt) && MX_CELL(pPage.pBt) <= 5460);
      Debug.Assert(pPage.nOverflow <= ArraySize(pPage.aOvfl));
      Debug.Assert(sz == cellSizePtr(pPage, pCell));
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      if (pPage.nOverflow != 0 || sz + 2 > pPage.nFree)
      {
        if (pTemp != null)
        {
          Buffer.BlockCopy(pCell, nSkip, pTemp, nSkip, sz - nSkip);//memcpy(pTemp+nSkip, pCell+nSkip, sz-nSkip);
          pCell = pTemp;
        }
        if (iChild != 0)
        {
          sqlite3Put4byte(pCell, iChild);
        }
        j = pPage.nOverflow++;
        Debug.Assert(j < pPage.aOvfl.Length);//(int)(sizeof(pPage.aOvfl)/sizeof(pPage.aOvfl[0])) );
        pPage.aOvfl[j].pCell = pCell;
        pPage.aOvfl[j].idx = (u16)i;
      }
      else
      {
        int rc = sqlite3PagerWrite(pPage.pDbPage);
        if (rc != SQLITE_OK)
        {
          pRC = rc;
          return;
        }
        Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));
        data = pPage.aData;
        cellOffset = pPage.cellOffset;
        end = cellOffset + 2 * pPage.nCell;
        ins = cellOffset + 2 * i;
        rc = allocateSpace(pPage, sz, ref idx);
        if (rc != 0) { pRC = rc; return; }
        /* The allocateSpace() routine guarantees the following two properties
        ** if it returns success */
        Debug.Assert(idx >= end + 2);
        Debug.Assert(idx + sz <= pPage.pBt.usableSize);
        pPage.nCell++;
        pPage.nFree -= (u16)(2 + sz);
        Buffer.BlockCopy(pCell, nSkip, data, idx + nSkip, sz - nSkip); //memcpy( data[idx + nSkip], pCell + nSkip, sz - nSkip );
        if (iChild != 0)
        {
          sqlite3Put4byte(data, idx, iChild);
        }
        //for(j=end, ptr=&data[j]; j>ins; j-=2, ptr-=2){
        //  ptr[0] = ptr[-2];
        //  ptr[1] = ptr[-1];
        //}
        for (j = end  ; j > ins; j -= 2)
        {
          data[j + 0] = data[j - 2];
          data[j + 1] = data[j - 1];
        }
        put2byte(data, ins, idx);
        put2byte(data, pPage.hdrOffset + 3, pPage.nCell);
#if !SQLITE_OMIT_AUTOVACUUM
        if (pPage.pBt.autoVacuum)
        {
          /* The cell may contain a pointer to an overflow page. If so, write
          ** the entry for the overflow page into the pointer map.
          */
          ptrmapPutOvflPtr(pPage, pCell, ref pRC);
        }
#endif
      }
    }

    /*
    ** Add a list of cells to a page.  The page should be initially empty.
    ** The cells are guaranteed to fit on the page.
    */
    static void assemblePage(
    MemPage pPage,    /* The page to be assemblied */
    int nCell,        /* The number of cells to add to this page */
    u8[] apCell,      /* Pointer to a single the cell bodies */
    int[] aSize       /* Sizes of the cells bodie*/
    )
    {
      int i;            /* Loop counter */
      int pCellptr;     /* Address of next cell pointer */
      int cellbody;     /* Address of next cell body */
      byte[] data = pPage.aData;          /* Pointer to data for pPage */
      int hdr = pPage.hdrOffset;          /* Offset of header on pPage */
      int nUsable = pPage.pBt.usableSize; /* Usable size of page */

      Debug.Assert(pPage.nOverflow == 0);
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      Debug.Assert(nCell >= 0 && nCell <= MX_CELL(pPage.pBt) && MX_CELL(pPage.pBt) <= 5460);
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));

      /* Check that the page has just been zeroed by zeroPage() */
      Debug.Assert(pPage.nCell == 0);
      Debug.Assert(get2byte(data, hdr + 5) == nUsable);

      pCellptr = pPage.cellOffset + nCell * 2; //data[pPage.cellOffset + nCell * 2];
      cellbody = nUsable;
      for (i = nCell - 1; i >= 0; i--)
      {
        pCellptr -= 2;
        cellbody -= aSize[i];
        put2byte(data, pCellptr, cellbody);
        Buffer.BlockCopy(apCell, 0, data, cellbody, aSize[i]);//          memcpy(data[cellbody], apCell[i], aSize[i]);
      }
      put2byte(data, hdr + 3, nCell);
      put2byte(data, hdr + 5, cellbody);
      pPage.nFree -= (u16)(nCell * 2 + nUsable - cellbody);
      pPage.nCell = (u16)nCell;
    }
    static void assemblePage(
    MemPage pPage,    /* The page to be assemblied */
    int nCell,        /* The number of cells to add to this page */
    u8[][] apCell,    /* Pointers to cell bodies */
    u16[] aSize,      /* Sizes of the cells */
    int offset        /* Offset into the cell bodies, for c#  */
    )
    {
      int i;            /* Loop counter */
      int pCellptr;      /* Address of next cell pointer */
      int cellbody;     /* Address of next cell body */
      byte[] data = pPage.aData;          /* Pointer to data for pPage */
      int hdr = pPage.hdrOffset;          /* Offset of header on pPage */
      int nUsable = pPage.pBt.usableSize; /* Usable size of page */

      Debug.Assert(pPage.nOverflow == 0);
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      Debug.Assert(nCell >= 0 && nCell <= MX_CELL(pPage.pBt) && MX_CELL(pPage.pBt) <= 5460);
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));

      /* Check that the page has just been zeroed by zeroPage() */
      Debug.Assert(pPage.nCell == 0);
      Debug.Assert(get2byte(data, hdr + 5) == nUsable);

      pCellptr = pPage.cellOffset + nCell * 2; //data[pPage.cellOffset + nCell * 2];
      cellbody = nUsable;
      for (i = nCell - 1; i >= 0; i--)
      {
        pCellptr -= 2;
        cellbody -= aSize[i + offset];
        put2byte(data, pCellptr, cellbody);
        Buffer.BlockCopy(apCell[offset + i], 0, data, cellbody, aSize[i + offset]);//          memcpy(&data[cellbody], apCell[i], aSize[i]);
      }
      put2byte(data, hdr + 3, nCell);
      put2byte(data, hdr + 5, cellbody);
      pPage.nFree -= (u16)(nCell * 2 + nUsable - cellbody);
      pPage.nCell = (u16)nCell;
    }

    static void assemblePage(
    MemPage pPage,    /* The page to be assemblied */
    int nCell,        /* The number of cells to add to this page */
    u8[] apCell,      /* Pointers to cell bodies */
    u16[] aSize       /* Sizes of the cells */
    )
    {
      int i;            /* Loop counter */
      int pCellptr;     /* Address of next cell pointer */
      int cellbody;     /* Address of next cell body */
      u8[] data = pPage.aData;             /* Pointer to data for pPage */
      int hdr = pPage.hdrOffset;           /* Offset of header on pPage */
      int nUsable = pPage.pBt.usableSize; /* Usable size of page */

      Debug.Assert(pPage.nOverflow == 0);
      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      Debug.Assert(nCell >= 0 && nCell <= MX_CELL(pPage.pBt) && MX_CELL(pPage.pBt) <= 5460);
      Debug.Assert(sqlite3PagerIswriteable(pPage.pDbPage));

      /* Check that the page has just been zeroed by zeroPage() */
      Debug.Assert(pPage.nCell == 0);
      Debug.Assert(get2byte(data, hdr + 5) == nUsable);

      pCellptr = pPage.cellOffset + nCell * 2; //&data[pPage.cellOffset + nCell * 2];
      cellbody = nUsable;
      for (i = nCell - 1; i >= 0; i--)
      {
        pCellptr -= 2;
        cellbody -= aSize[i];
        put2byte(data, pCellptr, cellbody);
        Buffer.BlockCopy(apCell, 0, data, cellbody, aSize[i]);//memcpy( data[cellbody], apCell[i], aSize[i] );
      }
      put2byte(data, hdr + 3, nCell);
      put2byte(data, hdr + 5, cellbody);
      pPage.nFree -= (u16)(nCell * 2 + nUsable - cellbody);
      pPage.nCell = (u16)nCell;
    }

    /*
    ** The following parameters determine how many adjacent pages get involved
    ** in a balancing operation.  NN is the number of neighbors on either side
    ** of the page that participate in the balancing operation.  NB is the
    ** total number of pages that participate, including the target page and
    ** NN neighbors on either side.
    **
    ** The minimum value of NN is 1 (of course).  Increasing NN above 1
    ** (to 2 or 3) gives a modest improvement in SELECT and DELETE performance
    ** in exchange for a larger degradation in INSERT and UPDATE performance.
    ** The value of NN appears to give the best results overall.
    */
    public const int NN = 1;              /* Number of neighbors on either side of pPage */
    public const int NB = (NN * 2 + 1);   /* Total pages involved in the balance */

#if !SQLITE_OMIT_QUICKBALANCE
    /*
** This version of balance() handles the common special case where
** a new entry is being inserted on the extreme right-end of the
** tree, in other words, when the new entry will become the largest
** entry in the tree.
**
** Instead of trying to balance the 3 right-most leaf pages, just add
** a new page to the right-hand side and put the one new entry in
** that page.  This leaves the right side of the tree somewhat
** unbalanced.  But odds are that we will be inserting new entries
** at the end soon afterwards so the nearly empty page will quickly
** fill up.  On average.
**
** pPage is the leaf page which is the right-most page in the tree.
** pParent is its parent.  pPage must have a single overflow entry
** which is also the right-most entry on the page.
**
** The pSpace buffer is used to store a temporary copy of the divider
** cell that will be inserted into pParent. Such a cell consists of a 4
** byte page number followed by a variable length integer. In other
** words, at most 13 bytes. Hence the pSpace buffer must be at
** least 13 bytes in size.
*/
    static int balance_quick(MemPage pParent, MemPage pPage, u8[] pSpace)
    {
      BtShared pBt = pPage.pBt;    /* B-Tree Database */
      MemPage pNew = new MemPage();/* Newly allocated page */
      int rc;                      /* Return Code */
      Pgno pgnoNew = 0;              /* Page number of pNew */

      Debug.Assert(sqlite3_mutex_held(pPage.pBt.mutex));
      Debug.Assert(sqlite3PagerIswriteable(pParent.pDbPage));
      Debug.Assert(pPage.nOverflow == 1);

      if (pPage.nCell <= 0)
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif

      /* Allocate a new page. This page will become the right-sibling of
** pPage. Make the parent page writable, so that the new divider cell
** may be inserted. If both these operations are successful, proceed.
*/
      rc = allocateBtreePage(pBt, ref pNew, ref pgnoNew, 0, 0);

      if (rc == SQLITE_OK)
      {

        int pOut = 4;//u8 pOut = &pSpace[4];
        u8[] pCell = pPage.aOvfl[0].pCell;
        int[] szCell = new int[1]; szCell[0] = cellSizePtr(pPage, pCell);
        int pStop;

        Debug.Assert(sqlite3PagerIswriteable(pNew.pDbPage));
        Debug.Assert(pPage.aData[0] == (PTF_INTKEY | PTF_LEAFDATA | PTF_LEAF));
        zeroPage(pNew, PTF_INTKEY | PTF_LEAFDATA | PTF_LEAF);
        assemblePage(pNew, 1, pCell, szCell);

        /* If this is an auto-vacuum database, update the pointer map
        ** with entries for the new page, and any pointer from the
        ** cell on the page to an overflow page. If either of these
        ** operations fails, the return code is set, but the contents
        ** of the parent page are still manipulated by thh code below.
        ** That is Ok, at this point the parent page is guaranteed to
        ** be marked as dirty. Returning an error code will cause a
        ** rollback, undoing any changes made to the parent page.
        */
#if !SQLITE_OMIT_AUTOVACUUM //   if ( ISAUTOVACUUM )
        if (pBt.autoVacuum)
#else
if (false)
#endif
        {
          ptrmapPut(pBt, pgnoNew, PTRMAP_BTREE, pParent.pgno, ref rc);
          if (szCell[0] > pNew.minLocal)
          {
            ptrmapPutOvflPtr(pNew, pCell, ref rc);
          }
        }

        /* Create a divider cell to insert into pParent. The divider cell
        ** consists of a 4-byte page number (the page number of pPage) and
        ** a variable length key value (which must be the same value as the
        ** largest key on pPage).
        **
        ** To find the largest key value on pPage, first find the right-most
        ** cell on pPage. The first two fields of this cell are the
        ** record-length (a variable length integer at most 32-bits in size)
        ** and the key value (a variable length integer, may have any value).
        ** The first of the while(...) loops below skips over the record-length
        ** field. The second while(...) loop copies the key value from the
        ** cell on pPage into the pSpace buffer.
        */
        int iCell = findCell(pPage, pPage.nCell - 1); //pCell = findCell( pPage, pPage.nCell - 1 );
        pCell = pPage.aData;
        int _pCell = iCell;
        pStop = _pCell + 9; //pStop = &pCell[9];
        while (((pCell[_pCell++]) & 0x80) != 0 && _pCell < pStop) ; //while ( ( *( pCell++ ) & 0x80 ) && pCell < pStop ) ;
        pStop = _pCell + 9;//pStop = &pCell[9];
        while (((pSpace[pOut++] = pCell[_pCell++]) & 0x80) != 0 && _pCell < pStop) ; //while ( ( ( *( pOut++ ) = *( pCell++ ) ) & 0x80 ) && pCell < pStop ) ;

        /* Insert the new divider cell into pParent. */
        insertCell(pParent, pParent.nCell, pSpace, pOut, //(int)(pOut-pSpace),
        null, pPage.pgno, ref rc);

        /* Set the right-child pointer of pParent to point to the new page. */
        sqlite3Put4byte(pParent.aData, pParent.hdrOffset + 8, pgnoNew);

        /* Release the reference to the new page. */
        releasePage(pNew);
      }

      return rc;
    }
#endif //* SQLITE_OMIT_QUICKBALANCE */

#if FALSE
/*
** This function does not contribute anything to the operation of SQLite.
** it is sometimes activated temporarily while debugging code responsible
** for setting pointer-map entries.
*/
static int ptrmapCheckPages(MemPage **apPage, int nPage){
int i, j;
for(i=0; i<nPage; i++){
Pgno n;
u8 e;
MemPage pPage = apPage[i];
BtShared pBt = pPage.pBt;
Debug.Assert( pPage.isInit!=0 );

for(j=0; j<pPage.nCell; j++){
CellInfo info;
u8 *z;

z = findCell(pPage, j);
btreeParseCellPtr(pPage, z,  info);
if( info.iOverflow ){
Pgno ovfl = sqlite3Get4byte(z[info.iOverflow]);
ptrmapGet(pBt, ovfl, ref e, ref n);
Debug.Assert( n==pPage.pgno && e==PTRMAP_OVERFLOW1 );
}
if( 0==pPage.leaf ){
Pgno child = sqlite3Get4byte(z);
ptrmapGet(pBt, child, ref e, ref n);
Debug.Assert( n==pPage.pgno && e==PTRMAP_BTREE );
}
}
if( 0==pPage.leaf ){
Pgno child = sqlite3Get4byte(pPage.aData,pPage.hdrOffset+8]);
ptrmapGet(pBt, child, ref e, ref n);
Debug.Assert( n==pPage.pgno && e==PTRMAP_BTREE );
}
}
return 1;
}
#endif

    /*
** This function is used to copy the contents of the b-tree node stored
** on page pFrom to page pTo. If page pFrom was not a leaf page, then
** the pointer-map entries for each child page are updated so that the
** parent page stored in the pointer map is page pTo. If pFrom contained
** any cells with overflow page pointers, then the corresponding pointer
** map entries are also updated so that the parent page is page pTo.
**
** If pFrom is currently carrying any overflow cells (entries in the
** MemPage.aOvfl[] array), they are not copied to pTo.
**
** Before returning, page pTo is reinitialized using btreeInitPage().
**
** The performance of this function is not critical. It is only used by
** the balance_shallower() and balance_deeper() procedures, neither of
** which are called often under normal circumstances.
*/
    static void copyNodeContent(MemPage pFrom, MemPage pTo, ref int pRC)
    {
      if ((pRC) == SQLITE_OK)
      {
        BtShared pBt = pFrom.pBt;
        u8[] aFrom = pFrom.aData;
        u8[] aTo = pTo.aData;
        int iFromHdr = pFrom.hdrOffset;
        int iToHdr = ((pTo.pgno == 1) ? 100 : 0);
#if !NDEBUG || SQLITE_COVERAGE_TEST || DEBUG
        int rc;//    TESTONLY(int rc;)
#else
int rc=0;
#endif
        int iData;


        Debug.Assert(pFrom.isInit != 0);
        Debug.Assert(pFrom.nFree >= iToHdr);
        Debug.Assert(get2byte(aFrom, iFromHdr + 5) <= pBt.usableSize);

        /* Copy the b-tree node content from page pFrom to page pTo. */
        iData = get2byte(aFrom, iFromHdr + 5);
        Buffer.BlockCopy(aFrom, iData, aTo, iData, pBt.usableSize - iData);//memcpy(aTo[iData], ref aFrom[iData], pBt.usableSize-iData);
        Buffer.BlockCopy(aFrom, iFromHdr, aTo, iToHdr, pFrom.cellOffset + 2 * pFrom.nCell);//memcpy(aTo[iToHdr], ref aFrom[iFromHdr], pFrom.cellOffset + 2*pFrom.nCell);

        /* Reinitialize page pTo so that the contents of the MemPage structure
        ** match the new data. The initialization of pTo "cannot" fail, as the
        ** data copied from pFrom is known to be valid.  */
        pTo.isInit = 0;
#if !NDEBUG || SQLITE_COVERAGE_TEST || DEBUG
        rc = btreeInitPage(pTo);//TESTONLY(rc = ) btreeInitPage(pTo);
#else
btreeInitPage(pTo);
#endif
        Debug.Assert(rc == SQLITE_OK);

        /* If this is an auto-vacuum database, update the pointer-map entries
        ** for any b-tree or overflow pages that pTo now contains the pointers to.
        */
#if !SQLITE_OMIT_AUTOVACUUM //   if ( ISAUTOVACUUM )
        if (pBt.autoVacuum)
#else
if (false)
#endif
        {
          pRC = setChildPtrmaps(pTo);
        }
      }
    }

    /*
    ** This routine redistributes cells on the iParentIdx'th child of pParent
    ** (hereafter "the page") and up to 2 siblings so that all pages have about the
    ** same amount of free space. Usually a single sibling on either side of the
    ** page are used in the balancing, though both siblings might come from one
    ** side if the page is the first or last child of its parent. If the page
    ** has fewer than 2 siblings (something which can only happen if the page
    ** is a root page or a child of a root page) then all available siblings
    ** participate in the balancing.
    **
    ** The number of siblings of the page might be increased or decreased by
    ** one or two in an effort to keep pages nearly full but not over full.
    **
    ** Note that when this routine is called, some of the cells on the page
    ** might not actually be stored in MemPage.aData[]. This can happen
    ** if the page is overfull. This routine ensures that all cells allocated
    ** to the page and its siblings fit into MemPage.aData[] before returning.
    **
    ** In the course of balancing the page and its siblings, cells may be
    ** inserted into or removed from the parent page (pParent). Doing so
    ** may cause the parent page to become overfull or underfull. If this
    ** happens, it is the responsibility of the caller to invoke the correct
    ** balancing routine to fix this problem (see the balance() routine).
    **
    ** If this routine fails for any reason, it might leave the database
    ** in a corrupted state. So if this routine fails, the database should
    ** be rolled back.
    **
    ** The third argument to this function, aOvflSpace, is a pointer to a
    ** buffer big enough to hold one page. If while inserting cells into the parent
    ** page (pParent) the parent page becomes overfull, this buffer is
    ** used to store the parent's overflow cells. Because this function inserts
    ** a maximum of four divider cells into the parent page, and the maximum
    ** size of a cell stored within an internal node is always less than 1/4
    ** of the page-size, the aOvflSpace[] buffer is guaranteed to be large
    ** enough for all overflow cells.
    **
    ** If aOvflSpace is set to a null pointer, this function returns
    ** SQLITE_NOMEM.
    */
    static int balance_nonroot(
    MemPage pParent,               /* Parent page of siblings being balanced */
    int iParentIdx,                /* Index of "the page" in pParent */
    u8[] aOvflSpace,               /* page-size bytes of space for parent ovfl */
    int isRoot                     /* True if pParent is a root-page */
    )
    {
      BtShared pBt;                /* The whole database */
      int nCell = 0;               /* Number of cells in apCell[] */
      int nMaxCells = 0;           /* Allocated size of apCell, szCell, aFrom. */
      int nNew = 0;                /* Number of pages in apNew[] */
      int nOld;                    /* Number of pages in apOld[] */
      int i, j, k;                 /* Loop counters */
      int nxDiv;                   /* Next divider slot in pParent.aCell[] */
      int rc = SQLITE_OK;          /* The return code */
      u16 leafCorrection;          /* 4 if pPage is a leaf.  0 if not */
      int leafData;                /* True if pPage is a leaf of a LEAFDATA tree */
      int usableSpace;             /* Bytes in pPage beyond the header */
      int pageFlags;               /* Value of pPage.aData[0] */
      int subtotal;                /* Subtotal of bytes in cells on one page */
      //int iSpace1 = 0;             /* First unused byte of aSpace1[] */
      int iOvflSpace = 0;          /* First unused byte of aOvflSpace[] */
      int szScratch;               /* Size of scratch memory requested */
      MemPage[] apOld = new MemPage[NB];    /* pPage and up to two siblings */
      MemPage[] apCopy = new MemPage[NB];   /* Private copies of apOld[] pages */
      MemPage[] apNew = new MemPage[NB + 2];/* pPage and up to NB siblings after balancing */
      int pRight;                  /* Location in parent of right-sibling pointer */
      int[] apDiv = new int[NB - 1];        /* Divider cells in pParent */
      int[] cntNew = new int[NB + 2];       /* Index in aCell[] of cell after i-th page */
      int[] szNew = new int[NB + 2];        /* Combined size of cells place on i-th page */
      u8[][] apCell = null;                 /* All cells begin balanced */
      u16[] szCell;                         /* Local size of all cells in apCell[] */
      //u8[] aSpace1;                         /* Space for copies of dividers cells */
      Pgno pgno;                   /* Temp var to store a page number in */

      pBt = pParent.pBt;
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      Debug.Assert(sqlite3PagerIswriteable(pParent.pDbPage));

#if FALSE
TRACE("BALANCE: begin page %d child of %d\n", pPage.pgno, pParent.pgno);
#endif

      /* At this point pParent may have at most one overflow cell. And if
** this overflow cell is present, it must be the cell with
** index iParentIdx. This scenario comes about when this function
** is called (indirectly) from sqlite3BtreeDelete().
*/
      Debug.Assert(pParent.nOverflow == 0 || pParent.nOverflow == 1);
      Debug.Assert(pParent.nOverflow == 0 || pParent.aOvfl[0].idx == iParentIdx);

      //if( !aOvflSpace ){
      //  return SQLITE_NOMEM;
      //}

      /* Find the sibling pages to balance. Also locate the cells in pParent
      ** that divide the siblings. An attempt is made to find NN siblings on
      ** either side of pPage. More siblings are taken from one side, however,
      ** if there are fewer than NN siblings on the other side. If pParent
      ** has NB or fewer children then all children of pParent are taken.
      **
      ** This loop also drops the divider cells from the parent page. This
      ** way, the remainder of the function does not have to deal with any
      ** overflow cells in the parent page, since if any existed they will
      ** have already been removed.
      */
      i = pParent.nOverflow + pParent.nCell;
      if (i < 2)
      {
        nxDiv = 0;
        nOld = i + 1;
      }
      else
      {
        nOld = 3;
        if (iParentIdx == 0)
        {
          nxDiv = 0;
        }
        else if (iParentIdx == i)
        {
          nxDiv = i - 2;
        }
        else
        {
          nxDiv = iParentIdx - 1;
        }
        i = 2;
      }
      if ((i + nxDiv - pParent.nOverflow) == pParent.nCell)
      {
        pRight = pParent.hdrOffset + 8; //&pParent.aData[pParent.hdrOffset + 8];
      }
      else
      {
        pRight = findCell(pParent, i + nxDiv - pParent.nOverflow);
      }
      pgno = sqlite3Get4byte(pParent.aData, pRight);
      while (true)
      {
        rc = getAndInitPage(pBt, pgno, ref apOld[i]);
        if (rc != 0)
        {
          apOld = new MemPage[i + 1];//memset(apOld, 0, (i+1)*sizeof(MemPage*));
          goto balance_cleanup;
        }
        nMaxCells += 1 + apOld[i].nCell + apOld[i].nOverflow;
        if ((i--) == 0) break;

        if (i + nxDiv == pParent.aOvfl[0].idx && pParent.nOverflow != 0)
        {
          apDiv[i] = 0;// = pParent.aOvfl[0].pCell;
          pgno = sqlite3Get4byte(pParent.aOvfl[0].pCell, apDiv[i]);
          szNew[i] = cellSizePtr(pParent, apDiv[i]);
          pParent.nOverflow = 0;
        }
        else
        {
          apDiv[i] = findCell(pParent, i + nxDiv - pParent.nOverflow);
          pgno = sqlite3Get4byte(pParent.aData, apDiv[i]);
          szNew[i] = cellSizePtr(pParent, apDiv[i]);

          /* Drop the cell from the parent page. apDiv[i] still points to
          ** the cell within the parent, even though it has been dropped.
          ** This is safe because dropping a cell only overwrites the first
          ** four bytes of it, and this function does not need the first
          ** four bytes of the divider cell. So the pointer is safe to use
          ** later on.
          **
          ** Unless SQLite is compiled in secure-delete mode. In this case,
          ** the dropCell() routine will overwrite the entire cell with zeroes.
          ** In this case, temporarily copy the cell into the aOvflSpace[]
          ** buffer. It will be copied out again as soon as the aSpace[] buffer
          ** is allocated.  */
#if SQLITE_SECURE_DELETE
memcpy(aOvflSpace[apDiv[i]-pParent.aData], apDiv[i], szNew[i]);
apDiv[i] = &aOvflSpace[apDiv[i]-pParent.aData];
#endif
          dropCell(pParent, i + nxDiv - pParent.nOverflow, szNew[i], ref rc);
        }
      }

      /* Make nMaxCells a multiple of 4 in order to preserve 8-byte
      ** alignment */
      nMaxCells = (nMaxCells + 3) & ~3;

      /*
      ** Allocate space for memory structures
      */
      //k = pBt.pageSize + ROUND8(sizeof(MemPage));
      //szScratch =
      //     nMaxCells*sizeof(u8*)                       /* apCell */
      //   + nMaxCells*sizeof(u16)                       /* szCell */
      //   + pBt.pageSize                               /* aSpace1 */
      //   + k*nOld;                                     /* Page copies (apCopy) */
      apCell = new byte[nMaxCells][];//apCell = sqlite3ScratchMalloc( szScratch );
      //if( apCell==null ){
      //  rc = SQLITE_NOMEM;
      //  goto balance_cleanup;
      //}
      szCell = new u16[nMaxCells];//(u16*)&apCell[nMaxCells];
      //aSpace1 = new byte[pBt.pageSize * (nMaxCells)];//  aSpace1 = (u8*)&szCell[nMaxCells];
      //Debug.Assert( EIGHT_BYTE_ALIGNMENT(aSpace1) );

      /*
      ** Load pointers to all cells on sibling pages and the divider cells
      ** into the local apCell[] array.  Make copies of the divider cells
      ** into space obtained from aSpace1[] and remove the the divider Cells
      ** from pParent.
      **
      ** If the siblings are on leaf pages, then the child pointers of the
      ** divider cells are stripped from the cells before they are copied
      ** into aSpace1[].  In this way, all cells in apCell[] are without
      ** child pointers.  If siblings are not leaves, then all cell in
      ** apCell[] include child pointers.  Either way, all cells in apCell[]
      ** are alike.
      **
      ** leafCorrection:  4 if pPage is a leaf.  0 if pPage is not a leaf.
      **       leafData:  1 if pPage holds key+data and pParent holds only keys.
      */
      leafCorrection = (u16)(apOld[0].leaf * 4);
      leafData = apOld[0].hasData;
      for (i = 0; i < nOld; i++)
      {
        int limit;

        /* Before doing anything else, take a copy of the i'th original sibling
        ** The rest of this function will use data from the copies rather
        ** that the original pages since the original pages will be in the
        ** process of being overwritten.  */
        //MemPage pOld = apCopy[i] = (MemPage*)&aSpace1[pBt.pageSize + k*i];
        //memcpy(pOld, apOld[i], sizeof(MemPage));
        //pOld.aData = (void*)&pOld[1];
        //memcpy(pOld.aData, apOld[i].aData, pBt.pageSize);
        MemPage pOld = apCopy[i] = apOld[i].Copy();

        limit = pOld.nCell + pOld.nOverflow;
        for (j = 0; j < limit; j++)
        {
          Debug.Assert(nCell < nMaxCells);
          //apCell[nCell] = findOverflowCell( pOld, j );
          //szCell[nCell] = cellSizePtr( pOld, apCell, nCell );
          int iFOFC = findOverflowCell(pOld, j);
          szCell[nCell] = cellSizePtr(pOld, iFOFC);
          // Copy the Data Locally
          apCell[nCell] = new u8[szCell[nCell]];
          if (iFOFC < 0)  // Overflow Cell
            Buffer.BlockCopy(pOld.aOvfl[-(iFOFC + 1)].pCell, 0, apCell[nCell], 0, szCell[nCell]);
          else
            Buffer.BlockCopy(pOld.aData, iFOFC, apCell[nCell], 0, szCell[nCell]);
          nCell++;
        }
        if (i < nOld - 1 && 0 == leafData)
        {
          u16 sz = (u16)szNew[i];
          byte[] pTemp = new byte[sz + leafCorrection];
          Debug.Assert(nCell < nMaxCells);
          szCell[nCell] = sz;
          //pTemp = &aSpace1[iSpace1];
          //iSpace1 += sz;
          Debug.Assert(sz <= pBt.pageSize / 4);
          //Debug.Assert(iSpace1 <= pBt.pageSize);
          Buffer.BlockCopy(pParent.aData, apDiv[i], pTemp, 0, sz);//memcpy( pTemp, apDiv[i], sz );
          apCell[nCell] = new byte[sz];
          Buffer.BlockCopy(pTemp, leafCorrection, apCell[nCell], 0, sz);//apCell[nCell] = pTemp + leafCorrection;
          Debug.Assert(leafCorrection == 0 || leafCorrection == 4);
          szCell[nCell] = (u16)(szCell[nCell] - leafCorrection);
          if (0 == pOld.leaf)
          {
            Debug.Assert(leafCorrection == 0);
            Debug.Assert(pOld.hdrOffset == 0);
            /* The right pointer of the child page pOld becomes the left
            ** pointer of the divider cell */
            Buffer.BlockCopy(pOld.aData, 8, apCell[nCell], 0, 4);//memcpy( apCell[nCell], ref pOld.aData[8], 4 );
          }
          else
          {
            Debug.Assert(leafCorrection == 4);
            if (szCell[nCell] < 4)
            {
              /* Do not allow any cells smaller than 4 bytes. */
              szCell[nCell] = 4;
            }
          }
          nCell++;
        }
      }

      /*
      ** Figure out the number of pages needed to hold all nCell cells.
      ** Store this number in "k".  Also compute szNew[] which is the total
      ** size of all cells on the i-th page and cntNew[] which is the index
      ** in apCell[] of the cell that divides page i from page i+1.
      ** cntNew[k] should equal nCell.
      **
      ** Values computed by this block:
      **
      **           k: The total number of sibling pages
      **    szNew[i]: Spaced used on the i-th sibling page.
      **   cntNew[i]: Index in apCell[] and szCell[] for the first cell to
      **              the right of the i-th sibling page.
      ** usableSpace: Number of bytes of space available on each sibling.
      **
      */
      usableSpace = pBt.usableSize - 12 + leafCorrection;
      for (subtotal = k = i = 0; i < nCell; i++)
      {
        Debug.Assert(i < nMaxCells);
        subtotal += szCell[i] + 2;
        if (subtotal > usableSpace)
        {
          szNew[k] = subtotal - szCell[i];
          cntNew[k] = i;
          if (leafData != 0) { i--; }
          subtotal = 0;
          k++;
          if (k > NB + 1) { rc = SQLITE_CORRUPT; goto balance_cleanup; }
        }
      }
      szNew[k] = subtotal;
      cntNew[k] = nCell;
      k++;

      /*
      ** The packing computed by the previous block is biased toward the siblings
      ** on the left side.  The left siblings are always nearly full, while the
      ** right-most sibling might be nearly empty.  This block of code attempts
      ** to adjust the packing of siblings to get a better balance.
      **
      ** This adjustment is more than an optimization.  The packing above might
      ** be so out of balance as to be illegal.  For example, the right-most
      ** sibling might be completely empty.  This adjustment is not optional.
      */
      for (i = k - 1; i > 0; i--)
      {
        int szRight = szNew[i];  /* Size of sibling on the right */
        int szLeft = szNew[i - 1]; /* Size of sibling on the left */
        int r;              /* Index of right-most cell in left sibling */
        int d;              /* Index of first cell to the left of right sibling */

        r = cntNew[i - 1] - 1;
        d = r + 1 - leafData;
        Debug.Assert(d < nMaxCells);
        Debug.Assert(r < nMaxCells);
        while (szRight == 0 || szRight + szCell[d] + 2 <= szLeft - (szCell[r] + 2))
        {
          szRight += szCell[d] + 2;
          szLeft -= szCell[r] + 2;
          cntNew[i - 1]--;
          r = cntNew[i - 1] - 1;
          d = r + 1 - leafData;
        }
        szNew[i] = szRight;
        szNew[i - 1] = szLeft;
      }

      /* Either we found one or more cells (cntnew[0])>0) or pPage is
      ** a virtual root page.  A virtual root page is when the real root
      ** page is page 1 and we are the only child of that page.
      */
      Debug.Assert(cntNew[0] > 0 || (pParent.pgno == 1 && pParent.nCell == 0));

      TRACE("BALANCE: old: %d %d %d  ",
      apOld[0].pgno,
      nOld >= 2 ? apOld[1].pgno : 0,
      nOld >= 3 ? apOld[2].pgno : 0
      );

      /*
      ** Allocate k new pages.  Reuse old pages where possible.
      */
      if (apOld[0].pgno <= 1)
      {
        rc = SQLITE_CORRUPT;
        goto balance_cleanup;
      }
      pageFlags = apOld[0].aData[0];
      for (i = 0; i < k; i++)
      {
        MemPage pNew = new MemPage();
        if (i < nOld)
        {
          pNew = apNew[i] = apOld[i];
          apOld[i] = null;
          rc = sqlite3PagerWrite(pNew.pDbPage);
          nNew++;
          if (rc != 0) goto balance_cleanup;
        }
        else
        {
          Debug.Assert(i > 0);
          rc = allocateBtreePage(pBt, ref pNew, ref pgno, pgno, 0);
          if (rc != 0) goto balance_cleanup;
          apNew[i] = pNew;
          nNew++;

          /* Set the pointer-map entry for the new sibling page. */
#if !SQLITE_OMIT_AUTOVACUUM //   if ( ISAUTOVACUUM )
          if (pBt.autoVacuum)
#else
if (false)
#endif
          {
            ptrmapPut(pBt, pNew.pgno, PTRMAP_BTREE, pParent.pgno, ref rc);
            if (rc != SQLITE_OK)
            {
              goto balance_cleanup;
            }
          }
        }
      }

      /* Free any old pages that were not reused as new pages.
      */
      while (i < nOld)
      {
        freePage(apOld[i], ref rc);
        if (rc != 0) goto balance_cleanup;
        releasePage(apOld[i]);
        apOld[i] = null;
        i++;
      }

      /*
      ** Put the new pages in accending order.  This helps to
      ** keep entries in the disk file in order so that a scan
      ** of the table is a linear scan through the file.  That
      ** in turn helps the operating system to deliver pages
      ** from the disk more rapidly.
      **
      ** An O(n^2) insertion sort algorithm is used, but since
      ** n is never more than NB (a small constant), that should
      ** not be a problem.
      **
      ** When NB==3, this one optimization makes the database
      ** about 25% faster for large insertions and deletions.
      */
      for (i = 0; i < k - 1; i++)
      {
        int minV = (int)apNew[i].pgno;
        int minI = i;
        for (j = i + 1; j < k; j++)
        {
          if (apNew[j].pgno < (u32)minV)
          {
            minI = j;
            minV = (int)apNew[j].pgno;
          }
        }
        if (minI > i)
        {
          int t;
          MemPage pT;
          t = (int)apNew[i].pgno;
          pT = apNew[i];
          apNew[i] = apNew[minI];
          apNew[minI] = pT;
        }
      }
      TRACE("new: %d(%d) %d(%d) %d(%d) %d(%d) %d(%d)\n",
      apNew[0].pgno, szNew[0],
      nNew >= 2 ? apNew[1].pgno : 0, nNew >= 2 ? szNew[1] : 0,
      nNew >= 3 ? apNew[2].pgno : 0, nNew >= 3 ? szNew[2] : 0,
      nNew >= 4 ? apNew[3].pgno : 0, nNew >= 4 ? szNew[3] : 0,
      nNew >= 5 ? apNew[4].pgno : 0, nNew >= 5 ? szNew[4] : 0);

      Debug.Assert(sqlite3PagerIswriteable(pParent.pDbPage));
      sqlite3Put4byte(pParent.aData, pRight, apNew[nNew - 1].pgno);

      /*
      ** Evenly distribute the data in apCell[] across the new pages.
      ** Insert divider cells into pParent as necessary.
      */
      j = 0;
      for (i = 0; i < nNew; i++)
      {
        /* Assemble the new sibling page. */
        MemPage pNew = apNew[i];
        Debug.Assert(j < nMaxCells);
        zeroPage(pNew, pageFlags);
        assemblePage(pNew, cntNew[i] - j, apCell, szCell, j);
        Debug.Assert(pNew.nCell > 0 || (nNew == 1 && cntNew[0] == 0));
        Debug.Assert(pNew.nOverflow == 0);

        j = cntNew[i];

        /* If the sibling page assembled above was not the right-most sibling,
        ** insert a divider cell into the parent page.
        */
        Debug.Assert(i < nNew - 1 || j == nCell);
        if (j < nCell)
        {
          u8[] pCell;
          u8[] pTemp;
          int sz;

          Debug.Assert(j < nMaxCells);
          pCell = apCell[j];
          sz = szCell[j] + leafCorrection;
          pTemp = new byte[sz];//&aOvflSpace[iOvflSpace];
          if (0 == pNew.leaf)
          {
            Buffer.BlockCopy(pCell, 0, pNew.aData, 8, 4);//memcpy( pNew.aData[8], pCell, 4 );
          }
          else if (leafData != 0)
          {
            /* If the tree is a leaf-data tree, and the siblings are leaves,
            ** then there is no divider cell in apCell[]. Instead, the divider
            ** cell consists of the integer key for the right-most cell of
            ** the sibling-page assembled above only.
            */
            CellInfo info = new CellInfo();
            j--;
            btreeParseCellPtr( pNew, apCell[j], ref info );
            pCell = pTemp;
            sz = 4 + putVarint( pCell, 4, (u64)info.nKey );
            pTemp = null;
          }
          else
          {
            //------------ pCell -= 4;
            byte[] _pCell_4 = new byte[pCell.Length + 4];
            Buffer.BlockCopy(pCell, 0, _pCell_4, 4, pCell.Length);
            pCell = _pCell_4;
            //
            /* Obscure case for non-leaf-data trees: If the cell at pCell was
            ** previously stored on a leaf node, and its reported size was 4
            ** bytes, then it may actually be smaller than this
            ** (see btreeParseCellPtr(), 4 bytes is the minimum size of
            ** any cell). But it is important to pass the correct size to
            ** insertCell(), so reparse the cell now.
            **
            ** Note that this can never happen in an SQLite data file, as all
            ** cells are at least 4 bytes. It only happens in b-trees used
            ** to evaluate "IN (SELECT ...)" and similar clauses.
            */
            if (szCell[j] == 4)
            {
              Debug.Assert(leafCorrection == 4);
              sz = cellSizePtr(pParent, pCell);
            }
          }
          iOvflSpace += sz;
          Debug.Assert(sz <= pBt.pageSize / 4);
          Debug.Assert(iOvflSpace <= pBt.pageSize);
          insertCell(pParent, nxDiv, pCell, sz, pTemp, pNew.pgno, ref rc);
          if (rc != SQLITE_OK) goto balance_cleanup;
          Debug.Assert(sqlite3PagerIswriteable(pParent.pDbPage));

          j++;
          nxDiv++;
        }
      }
      Debug.Assert(j == nCell);
      Debug.Assert(nOld > 0);
      Debug.Assert(nNew > 0);
      if ((pageFlags & PTF_LEAF) == 0)
      {
        Buffer.BlockCopy(apCopy[nOld - 1].aData, 8, apNew[nNew - 1].aData, 8, 4); //u8* zChild = &apCopy[nOld - 1].aData[8];
        //memcpy( apNew[nNew - 1].aData[8], zChild, 4 );
      }

      if (isRoot != 0 && pParent.nCell == 0 && pParent.hdrOffset <= apNew[0].nFree)
      {
        /* The root page of the b-tree now contains no cells. The only sibling
        ** page is the right-child of the parent. Copy the contents of the
        ** child page into the parent, decreasing the overall height of the
        ** b-tree structure by one. This is described as the "balance-shallower"
        ** sub-algorithm in some documentation.
        **
        ** If this is an auto-vacuum database, the call to copyNodeContent()
        ** sets all pointer-map entries corresponding to database image pages
        ** for which the pointer is stored within the content being copied.
        **
        ** The second Debug.Assert below verifies that the child page is defragmented
        ** (it must be, as it was just reconstructed using assemblePage()). This
        ** is important if the parent page happens to be page 1 of the database
        ** image.  */
        Debug.Assert(nNew == 1);
        Debug.Assert(apNew[0].nFree ==
        (get2byte(apNew[0].aData, 5) - apNew[0].cellOffset - apNew[0].nCell * 2)
        );
        copyNodeContent(apNew[0], pParent, ref rc);
        freePage(apNew[0], ref rc);
      }
      else
#if !SQLITE_OMIT_AUTOVACUUM //   if ( ISAUTOVACUUM )
        if (pBt.autoVacuum)
#else
if (false)
#endif
        {
          /* Fix the pointer-map entries for all the cells that were shifted around.
          ** There are several different types of pointer-map entries that need to
          ** be dealt with by this routine. Some of these have been set already, but
          ** many have not. The following is a summary:
          **
          **   1) The entries associated with new sibling pages that were not
          **      siblings when this function was called. These have already
          **      been set. We don't need to worry about old siblings that were
          **      moved to the free-list - the freePage() code has taken care
          **      of those.
          **
          **   2) The pointer-map entries associated with the first overflow
          **      page in any overflow chains used by new divider cells. These
          **      have also already been taken care of by the insertCell() code.
          **
          **   3) If the sibling pages are not leaves, then the child pages of
          **      cells stored on the sibling pages may need to be updated.
          **
          **   4) If the sibling pages are not internal intkey nodes, then any
          **      overflow pages used by these cells may need to be updated
          **      (internal intkey nodes never contain pointers to overflow pages).
          **
          **   5) If the sibling pages are not leaves, then the pointer-map
          **      entries for the right-child pages of each sibling may need
          **      to be updated.
          **
          ** Cases 1 and 2 are dealt with above by other code. The next
          ** block deals with cases 3 and 4 and the one after that, case 5. Since
          ** setting a pointer map entry is a relatively expensive operation, this
          ** code only sets pointer map entries for child or overflow pages that have
          ** actually moved between pages.  */
          MemPage pNew = apNew[0];
          MemPage pOld = apCopy[0];
          int nOverflow = pOld.nOverflow;
          int iNextOld = pOld.nCell + nOverflow;
          int iOverflow = (nOverflow != 0 ? pOld.aOvfl[0].idx : -1);
          j = 0;                             /* Current 'old' sibling page */
          k = 0;                             /* Current 'new' sibling page */
          for (i = 0; i < nCell; i++)
          {
            int isDivider = 0;
            while (i == iNextOld)
            {
              /* Cell i is the cell immediately following the last cell on old
              ** sibling page j. If the siblings are not leaf pages of an
              ** intkey b-tree, then cell i was a divider cell. */
              pOld = apCopy[++j];
              iNextOld = i + (0 == leafData ? 1 : 0) + pOld.nCell + pOld.nOverflow;
              if (pOld.nOverflow != 0)
              {
                nOverflow = pOld.nOverflow;
                iOverflow = i + (0 == leafData ? 1 : 0 )+ pOld.aOvfl[0].idx;
              }
              isDivider = 0 == leafData ? 1 : 0;
            }

            Debug.Assert(nOverflow > 0 || iOverflow < i);
            Debug.Assert(nOverflow < 2 || pOld.aOvfl[0].idx == pOld.aOvfl[1].idx - 1);
            Debug.Assert(nOverflow < 3 || pOld.aOvfl[1].idx == pOld.aOvfl[2].idx - 1);
            if (i == iOverflow)
            {
              isDivider = 1;
              if ((--nOverflow) > 0)
              {
                iOverflow++;
              }
            }

            if (i == cntNew[k])
            {
              /* Cell i is the cell immediately following the last cell on new
              ** sibling page k. If the siblings are not leaf pages of an
              ** intkey b-tree, then cell i is a divider cell.  */
              pNew = apNew[++k];
              if (0 == leafData) continue;
            }
            Debug.Assert(j < nOld);
            Debug.Assert(k < nNew);

            /* If the cell was originally divider cell (and is not now) or
            ** an overflow cell, or if the cell was located on a different sibling
            ** page before the balancing, then the pointer map entries associated
            ** with any child or overflow pages need to be updated.  */
            if (isDivider != 0 || pOld.pgno != pNew.pgno)
            {
              if (0 == leafCorrection)
              {
                ptrmapPut(pBt, sqlite3Get4byte(apCell[i]), PTRMAP_BTREE, pNew.pgno, ref rc);
              }
              if (szCell[i] > pNew.minLocal)
              {
                ptrmapPutOvflPtr(pNew, apCell[i], ref rc);
              }
            }
          }

          if (0 == leafCorrection)
          {
            for (i = 0; i < nNew; i++)
            {
              u32 key = sqlite3Get4byte(apNew[i].aData, 8);
              ptrmapPut(pBt, key, PTRMAP_BTREE, apNew[i].pgno, ref rc);
            }
          }

#if FALSE
/* The ptrmapCheckPages() contains Debug.Assert() statements that verify that
** all pointer map pages are set correctly. This is helpful while
** debugging. This is usually disabled because a corrupt database may
** cause an Debug.Assert() statement to fail.  */
ptrmapCheckPages(apNew, nNew);
ptrmapCheckPages(pParent, 1);
#endif
        }

      Debug.Assert(pParent.isInit != 0);
      TRACE("BALANCE: finished: old=%d new=%d cells=%d\n",
      nOld, nNew, nCell);

    /*
    ** Cleanup before returning.
    */
    balance_cleanup:
      //sqlite3ScratchFree( ref apCell );
      for (i = 0; i < nOld; i++)
      {
        releasePage(apOld[i]);
      }
      for (i = 0; i < nNew; i++)
      {
        releasePage(apNew[i]);
      }

      return rc;
    }


    /*
    ** This function is called when the root page of a b-tree structure is
    ** overfull (has one or more overflow pages).
    **
    ** A new child page is allocated and the contents of the current root
    ** page, including overflow cells, are copied into the child. The root
    ** page is then overwritten to make it an empty page with the right-child
    ** pointer pointing to the new page.
    **
    ** Before returning, all pointer-map entries corresponding to pages
    ** that the new child-page now contains pointers to are updated. The
    ** entry corresponding to the new right-child pointer of the root
    ** page is also updated.
    **
    ** If successful, ppChild is set to contain a reference to the child
    ** page and SQLITE_OK is returned. In this case the caller is required
    ** to call releasePage() on ppChild exactly once. If an error occurs,
    ** an error code is returned and ppChild is set to 0.
    */
    static int balance_deeper(MemPage pRoot, ref MemPage ppChild)
    {
      int rc;                        /* Return value from subprocedures */
      MemPage pChild = null;           /* Pointer to a new child page */
      Pgno pgnoChild = 0;            /* Page number of the new child page */
      BtShared pBt = pRoot.pBt;    /* The BTree */

      Debug.Assert(pRoot.nOverflow > 0);
      Debug.Assert(sqlite3_mutex_held(pBt.mutex));

      /* Make pRoot, the root page of the b-tree, writable. Allocate a new
      ** page that will become the new right-child of pPage. Copy the contents
      ** of the node stored on pRoot into the new child page.
      */
      rc = sqlite3PagerWrite(pRoot.pDbPage);
      if (rc == SQLITE_OK)
      {
        rc = allocateBtreePage(pBt, ref pChild, ref pgnoChild, pRoot.pgno, 0);
        copyNodeContent(pRoot, pChild, ref rc);
#if !SQLITE_OMIT_AUTOVACUUM //   if ( ISAUTOVACUUM )
        if (pBt.autoVacuum)
#else
if (false)
#endif
        {
          ptrmapPut(pBt, pgnoChild, PTRMAP_BTREE, pRoot.pgno, ref rc);
        }
      }
      if (rc != 0)
      {
        ppChild = null;
        releasePage(pChild);
        return rc;
      }
      Debug.Assert(sqlite3PagerIswriteable(pChild.pDbPage));
      Debug.Assert(sqlite3PagerIswriteable(pRoot.pDbPage));
      Debug.Assert(pChild.nCell == pRoot.nCell);

      TRACE("BALANCE: copy root %d into %d\n", pRoot.pgno, pChild.pgno);

      /* Copy the overflow cells from pRoot to pChild */
      Array.Copy(pRoot.aOvfl, pChild.aOvfl, pRoot.nOverflow);//memcpy(pChild.aOvfl, pRoot.aOvfl, pRoot.nOverflow*sizeof(pRoot.aOvfl[0]));
      pChild.nOverflow = pRoot.nOverflow;

      /* Zero the contents of pRoot. Then install pChild as the right-child. */
      zeroPage(pRoot, pChild.aData[0] & ~PTF_LEAF);
      sqlite3Put4byte(pRoot.aData, pRoot.hdrOffset + 8, pgnoChild);

      ppChild = pChild;
      return SQLITE_OK;
    }

    /*
    ** The page that pCur currently points to has just been modified in
    ** some way. This function figures out if this modification means the
    ** tree needs to be balanced, and if so calls the appropriate balancing
    ** routine. Balancing routines are:
    **
    **   balance_quick()
    **   balance_deeper()
    **   balance_nonroot()
    */
    static int balance(BtCursor pCur)
    {
      int rc = SQLITE_OK;
      int nMin = pCur.pBt.usableSize * 2 / 3;
      u8[] aBalanceQuickSpace = new u8[13];
      u8[] pFree = null;

#if !NDEBUG || SQLITE_COVERAGE_TEST || DEBUG
      int balance_quick_called = 0;//TESTONLY( int balance_quick_called = 0 );
      int balance_deeper_called = 0;//TESTONLY( int balance_deeper_called = 0 );
#else
int balance_quick_called = 0;
int balance_deeper_called = 0;
#endif

      do
      {
        int iPage = pCur.iPage;
        MemPage pPage = pCur.apPage[iPage];

        if (iPage == 0)
        {
          if (pPage.nOverflow != 0)
          {
            /* The root page of the b-tree is overfull. In this case call the
            ** balance_deeper() function to create a new child for the root-page
            ** and copy the current contents of the root-page to it. The
            ** next iteration of the do-loop will balance the child page.
            */
            Debug.Assert((balance_deeper_called++) == 0);
            rc = balance_deeper(pPage, ref pCur.apPage[1]);
            if (rc == SQLITE_OK)
            {
              pCur.iPage = 1;
              pCur.aiIdx[0] = 0;
              pCur.aiIdx[1] = 0;
              Debug.Assert(pCur.apPage[1].nOverflow != 0);
            }
          }
          else
          {
            break;
          }
        }
        else if (pPage.nOverflow == 0 && pPage.nFree <= nMin)
        {
          break;
        }
        else
        {
          MemPage pParent = pCur.apPage[iPage - 1];
          int iIdx = pCur.aiIdx[iPage - 1];

          rc = sqlite3PagerWrite(pParent.pDbPage);
          if (rc == SQLITE_OK)
          {
#if !SQLITE_OMIT_QUICKBALANCE
            if (pPage.hasData != 0
            && pPage.nOverflow == 1
            && pPage.aOvfl[0].idx == pPage.nCell
            && pParent.pgno != 1
            && pParent.nCell == iIdx
            )
            {
              /* Call balance_quick() to create a new sibling of pPage on which
              ** to store the overflow cell. balance_quick() inserts a new cell
              ** into pParent, which may cause pParent overflow. If this
              ** happens, the next interation of the do-loop will balance pParent
              ** use either balance_nonroot() or balance_deeper(). Until this
              ** happens, the overflow cell is stored in the aBalanceQuickSpace[]
              ** buffer.
              **
              ** The purpose of the following Debug.Assert() is to check that only a
              ** single call to balance_quick() is made for each call to this
              ** function. If this were not verified, a subtle bug involving reuse
              ** of the aBalanceQuickSpace[] might sneak in.
              */
              Debug.Assert((balance_quick_called++) == 0);
              rc = balance_quick(pParent, pPage, aBalanceQuickSpace);
            }
            else
#endif
            {
              /* In this case, call balance_nonroot() to redistribute cells
              ** between pPage and up to 2 of its sibling pages. This involves
              ** modifying the contents of pParent, which may cause pParent to
              ** become overfull or underfull. The next iteration of the do-loop
              ** will balance the parent page to correct this.
              **
              ** If the parent page becomes overfull, the overflow cell or cells
              ** are stored in the pSpace buffer allocated immediately below.
              ** A subsequent iteration of the do-loop will deal with this by
              ** calling balance_nonroot() (balance_deeper() may be called first,
              ** but it doesn't deal with overflow cells - just moves them to a
              ** different page). Once this subsequent call to balance_nonroot()
              ** has completed, it is safe to release the pSpace buffer used by
              ** the previous call, as the overflow cell data will have been
              ** copied either into the body of a database page or into the new
              ** pSpace buffer passed to the latter call to balance_nonroot().
              */
              u8[] pSpace = new u8[pCur.pBt.pageSize];// u8 pSpace = sqlite3PageMalloc( pCur.pBt.pageSize );
              rc = balance_nonroot(pParent, iIdx, pSpace, iPage == 1 ? 1 : 0);
              //if (pFree != null)
              //{
              //  /* If pFree is not NULL, it points to the pSpace buffer used
              //  ** by a previous call to balance_nonroot(). Its contents are
              //  ** now stored either on real database pages or within the
              //  ** new pSpace buffer, so it may be safely freed here. */
              //  sqlite3PageFree(ref pFree);
              //}

              /* The pSpace buffer will be freed after the next call to
              ** balance_nonroot(), or just before this function returns, whichever
              ** comes first. */
              pFree = pSpace;
            }
          }

          pPage.nOverflow = 0;

          /* The next iteration of the do-loop balances the parent page. */
          releasePage(pPage);
          pCur.iPage--;
        }
      } while (rc == SQLITE_OK);

      //if (pFree != null)
      //{
      //  sqlite3PageFree(ref pFree);
      //}
      return rc;
    }


    /*
    ** Insert a new record into the BTree.  The key is given by (pKey,nKey)
    ** and the data is given by (pData,nData).  The cursor is used only to
    ** define what table the record should be inserted into.  The cursor
    ** is left pointing at a random location.
    **
    ** For an INTKEY table, only the nKey value of the key is used.  pKey is
    ** ignored.  For a ZERODATA table, the pData and nData are both ignored.
    **
    ** If the seekResult parameter is non-zero, then a successful call to
    ** MovetoUnpacked() to seek cursor pCur to (pKey, nKey) has already
    ** been performed. seekResult is the search result returned (a negative
    ** number if pCur points at an entry that is smaller than (pKey, nKey), or
    ** a positive value if pCur points at an etry that is larger than
    ** (pKey, nKey)).
    **
    ** If the seekResult parameter is 0, then cursor pCur may point to any
    ** entry or to no entry at all. In this case this function has to seek
    ** the cursor before the new key can be inserted.
    */
    static int sqlite3BtreeInsert(
    BtCursor pCur,                /* Insert data into the table of this cursor */
    byte[] pKey, i64 nKey,        /* The key of the new record */
    byte[] pData, int nData,      /* The data of the new record */
    int nZero,                     /* Number of extra 0 bytes to append to data */
    int appendBias,                /* True if this is likely an append */
    int seekResult                 /* Result of prior MovetoUnpacked() call */
    )
    {
      int rc;
      int loc = seekResult;
      int szNew = 0;
      int idx;
      MemPage pPage;
      Btree p = pCur.pBtree;
      BtShared pBt = p.pBt;
      int oldCell;
      byte[] newCell = null;

      if (pCur.eState == CURSOR_FAULT)
      {
        Debug.Assert(pCur.skipNext != SQLITE_OK);
        return pCur.skipNext;
      }

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pCur.wrFlag != 0 && pBt.inTransaction == TRANS_WRITE && !pBt.readOnly);
      Debug.Assert(hasSharedCacheTableLock(p, pCur.pgnoRoot, pCur.pKeyInfo != null ? 1 : 0, 2));

      /* Assert that the caller has been consistent. If this cursor was opened
      ** expecting an index b-tree, then the caller should be inserting blob
      ** keys with no associated data. If the cursor was opened expecting an
      ** intkey table, the caller should be inserting integer keys with a
      ** blob of associated data.  */
      Debug.Assert((pKey == null) == (pCur.pKeyInfo == null));

      /* If this is an insert into a table b-tree, invalidate any incrblob
      ** cursors open on the row being replaced (assuming this is a replace
      ** operation - if it is not, the following is a no-op).  */
      if (pCur.pKeyInfo == null)
      {
        invalidateIncrblobCursors(p, nKey, 0);
      }

      /* Save the positions of any other cursors open on this table.
      **
      ** In some cases, the call to btreeMoveto() below is a no-op. For
      ** example, when inserting data into a table with auto-generated integer
      ** keys, the VDBE layer invokes sqlite3BtreeLast() to figure out the
      ** integer key to use. It then calls this function to actually insert the
      ** data into the intkey B-Tree. In this case btreeMoveto() recognizes
      ** that the cursor is already where it needs to be and returns without
      ** doing any work. To avoid thwarting these optimizations, it is important
      ** not to clear the cursor here.
      */
      rc = saveAllCursors(pBt, pCur.pgnoRoot, pCur);
      if (rc != 0) return rc;
      if (0 == loc)
      {
        rc = btreeMoveto(pCur, pKey, nKey, appendBias, ref loc);
        if (rc != 0) return rc;
      }
      Debug.Assert(pCur.eState == CURSOR_VALID || (pCur.eState == CURSOR_INVALID && loc != 0));

      pPage = pCur.apPage[pCur.iPage];
      Debug.Assert(pPage.intKey != 0 || nKey >= 0);
      Debug.Assert(pPage.leaf != 0 || 0 == pPage.intKey);

      TRACE("INSERT: table=%d nkey=%lld ndata=%d page=%d %s\n",
      pCur.pgnoRoot, nKey, nData, pPage.pgno,
      loc == 0 ? "overwrite" : "new entry");
      Debug.Assert(pPage.isInit != 0);
      allocateTempSpace(pBt);
      newCell = pBt.pTmpSpace;
      //if (newCell == null) return SQLITE_NOMEM;
      rc = fillInCell(pPage, newCell, pKey, nKey, pData, nData, nZero, ref szNew);
      if (rc != 0) goto end_insert;
      Debug.Assert(szNew == cellSizePtr(pPage, newCell));
      Debug.Assert(szNew <= MX_CELL_SIZE(pBt));
      idx = pCur.aiIdx[pCur.iPage];
      if (loc == 0)
      {
        u16 szOld;
        Debug.Assert(idx < pPage.nCell);
        rc = sqlite3PagerWrite(pPage.pDbPage);
        if (rc != 0)
        {
          goto end_insert;
        }
        oldCell = findCell(pPage, idx);
        if (0 == pPage.leaf)
        {
          //memcpy(newCell, oldCell, 4);
          newCell[0] = pPage.aData[oldCell + 0];
          newCell[1] = pPage.aData[oldCell + 1];
          newCell[2] = pPage.aData[oldCell + 2];
          newCell[3] = pPage.aData[oldCell + 3];
        }
        szOld = cellSizePtr(pPage, oldCell);
        rc = clearCell(pPage, oldCell);
        dropCell(pPage, idx, szOld, ref rc);
        if (rc != 0) goto end_insert;
      }
      else if (loc < 0 && pPage.nCell > 0)
      {
        Debug.Assert(pPage.leaf != 0);
        idx = ++pCur.aiIdx[pCur.iPage];
      }
      else
      {
        Debug.Assert(pPage.leaf != 0);
      }
      insertCell(pPage, idx, newCell, szNew, null, 0, ref rc);
      Debug.Assert(rc != SQLITE_OK || pPage.nCell > 0 || pPage.nOverflow > 0);

      /* If no error has occured and pPage has an overflow cell, call balance()
      ** to redistribute the cells within the tree. Since balance() may move
      ** the cursor, zero the BtCursor.info.nSize and BtCursor.validNKey
      ** variables.
      **
      ** Previous versions of SQLite called moveToRoot() to move the cursor
      ** back to the root page as balance() used to invalidate the contents
      ** of BtCursor.apPage[] and BtCursor.aiIdx[]. Instead of doing that,
      ** set the cursor state to "invalid". This makes common insert operations
      ** slightly faster.
      **
      ** There is a subtle but important optimization here too. When inserting
      ** multiple records into an intkey b-tree using a single cursor (as can
      ** happen while processing an "INSERT INTO ... SELECT" statement), it
      ** is advantageous to leave the cursor pointing to the last entry in
      ** the b-tree if possible. If the cursor is left pointing to the last
      ** entry in the table, and the next row inserted has an integer key
      ** larger than the largest existing key, it is possible to insert the
      ** row without seeking the cursor. This can be a big performance boost.
      */
      pCur.info.nSize = 0;
      pCur.validNKey = false;
      if (rc == SQLITE_OK && pPage.nOverflow != 0)
      {
        rc = balance(pCur);

        /* Must make sure nOverflow is reset to zero even if the balance()
        ** fails. Internal data structure corruption will result otherwise.
        ** Also, set the cursor state to invalid. This stops saveCursorPosition()
        ** from trying to save the current position of the cursor.  */
        pCur.apPage[pCur.iPage].nOverflow = 0;
        pCur.eState = CURSOR_INVALID;
      }
      Debug.Assert(pCur.apPage[pCur.iPage].nOverflow == 0);

    end_insert:
      return rc;
    }

    /*
    ** Delete the entry that the cursor is pointing to.  The cursor
    ** is left pointing at a arbitrary location.
    */
    static int sqlite3BtreeDelete(BtCursor pCur)
    {
      Btree p = pCur.pBtree;
      BtShared pBt = p.pBt;
      int rc;                             /* Return code */
      MemPage pPage;                      /* Page to delete cell from */
      int pCell;                          /* Pointer to cell to delete */
      int iCellIdx;                       /* Index of cell to delete */
      int iCellDepth;                     /* Depth of node containing pCell */

      Debug.Assert(cursorHoldsMutex(pCur));
      Debug.Assert(pBt.inTransaction == TRANS_WRITE);
      Debug.Assert(!pBt.readOnly);
      Debug.Assert(pCur.wrFlag != 0);
      Debug.Assert(hasSharedCacheTableLock(p, pCur.pgnoRoot, pCur.pKeyInfo != null ? 1 : 0, 2));
      Debug.Assert(!hasReadConflicts(p, pCur.pgnoRoot));

      if (NEVER(pCur.aiIdx[pCur.iPage] >= pCur.apPage[pCur.iPage].nCell)
      || NEVER(pCur.eState != CURSOR_VALID)
      )
      {
        return SQLITE_ERROR;  /* Something has gone awry. */
      }

      /* If this is a delete operation to remove a row from a table b-tree,
      ** invalidate any incrblob cursors open on the row being deleted.  */
      if (pCur.pKeyInfo == null)
      {
        invalidateIncrblobCursors(p, pCur.info.nKey, 0);
      }

      iCellDepth = pCur.iPage;
      iCellIdx = pCur.aiIdx[iCellDepth];
      pPage = pCur.apPage[iCellDepth];
      pCell = findCell(pPage, iCellIdx);

      /* If the page containing the entry to delete is not a leaf page, move
      ** the cursor to the largest entry in the tree that is smaller than
      ** the entry being deleted. This cell will replace the cell being deleted
      ** from the internal node. The 'previous' entry is used for this instead
      ** of the 'next' entry, as the previous entry is always a part of the
      ** sub-tree headed by the child page of the cell being deleted. This makes
      ** balancing the tree following the delete operation easier.  */
      if (0 == pPage.leaf)
      {
        int notUsed = 0;
        rc = sqlite3BtreePrevious(pCur, ref notUsed);
        if (rc != 0) return rc;
      }

      /* Save the positions of any other cursors open on this table before
      ** making any modifications. Make the page containing the entry to be
      ** deleted writable. Then free any overflow pages associated with the
      ** entry and finally remove the cell itself from within the page.
      */
      rc = saveAllCursors(pBt, pCur.pgnoRoot, pCur);
      if (rc != 0) return rc;
      rc = sqlite3PagerWrite(pPage.pDbPage);
      if (rc != 0) return rc;
      rc = clearCell(pPage, pCell);
      dropCell(pPage, iCellIdx, cellSizePtr(pPage, pCell), ref rc);
      if (rc != 0) return rc;

      /* If the cell deleted was not located on a leaf page, then the cursor
      ** is currently pointing to the largest entry in the sub-tree headed
      ** by the child-page of the cell that was just deleted from an internal
      ** node. The cell from the leaf node needs to be moved to the internal
      ** node to replace the deleted cell.  */
      if (0 == pPage.leaf)
      {
        MemPage pLeaf = pCur.apPage[pCur.iPage];
        int nCell;
        Pgno n = pCur.apPage[iCellDepth + 1].pgno;
        //byte[] pTmp;

        pCell = findCell(pLeaf, pLeaf.nCell - 1);
        nCell = cellSizePtr(pLeaf, pCell);
        Debug.Assert(MX_CELL_SIZE(pBt) >= nCell);

        //allocateTempSpace(pBt);
        //pTmp = pBt.pTmpSpace;

        rc = sqlite3PagerWrite(pLeaf.pDbPage);
        byte[] pNext_4 = new byte[nCell + 4];
        Buffer.BlockCopy(pLeaf.aData, pCell - 4, pNext_4, 0, nCell + 4);
        insertCell(pPage, iCellIdx, pNext_4, nCell + 4, null, n, ref rc); //insertCell( pPage, iCellIdx, pCell - 4, nCell + 4, pTmp, n, ref rc );
        dropCell(pLeaf, pLeaf.nCell - 1, nCell, ref rc);
        if (rc != 0) return rc;
      }

      /* Balance the tree. If the entry deleted was located on a leaf page,
      ** then the cursor still points to that page. In this case the first
      ** call to balance() repairs the tree, and the if(...) condition is
      ** never true.
      **
      ** Otherwise, if the entry deleted was on an internal node page, then
      ** pCur is pointing to the leaf page from which a cell was removed to
      ** replace the cell deleted from the internal node. This is slightly
      ** tricky as the leaf node may be underfull, and the internal node may
      ** be either under or overfull. In this case run the balancing algorithm
      ** on the leaf node first. If the balance proceeds far enough up the
      ** tree that we can be sure that any problem in the internal node has
      ** been corrected, so be it. Otherwise, after balancing the leaf node,
      ** walk the cursor up the tree to the internal node and balance it as
      ** well.  */
      rc = balance(pCur);
      if (rc == SQLITE_OK && pCur.iPage > iCellDepth)
      {
        while (pCur.iPage > iCellDepth)
        {
          releasePage(pCur.apPage[pCur.iPage--]);
        }
        rc = balance(pCur);
      }

      if (rc == SQLITE_OK)
      {
        moveToRoot(pCur);
      }
      return rc;
    }

    /*
    ** Create a new BTree table.  Write into piTable the page
    ** number for the root page of the new table.
    **
    ** The type of type is determined by the flags parameter.  Only the
    ** following values of flags are currently in use.  Other values for
    ** flags might not work:
    **
    **     BTREE_INTKEY|BTREE_LEAFDATA     Used for SQL tables with rowid keys
    **     BTREE_ZERODATA                  Used for SQL indices
    */
    static int btreeCreateTable(Btree p, ref int piTable, int flags)
    {
      BtShared pBt = p.pBt;
      MemPage pRoot = new MemPage();
      Pgno pgnoRoot = 0;
      int rc;

      Debug.Assert(sqlite3BtreeHoldsMutex(p));
      Debug.Assert(pBt.inTransaction == TRANS_WRITE);
      Debug.Assert(!pBt.readOnly);

#if SQLITE_OMIT_AUTOVACUUM
rc = allocateBtreePage(pBt, ref pRoot, ref pgnoRoot, 1, 0);
if( rc !=0){
return rc;
}
#else
      if (pBt.autoVacuum)
      {
        Pgno pgnoMove = 0;                    /* Move a page here to make room for the root-page */
        MemPage pPageMove = new MemPage();  /* The page to move to. */

        /* Creating a new table may probably require moving an existing database
        ** to make room for the new tables root page. In case this page turns
        ** out to be an overflow page, delete all overflow page-map caches
        ** held by open cursors.
        */
        invalidateAllOverflowCache(pBt);

        /* Read the value of meta[3] from the database to determine where the
        ** root page of the new table should go. meta[3] is the largest root-page
        ** created so far, so the new root-page is (meta[3]+1).
        */
        sqlite3BtreeGetMeta(p, BTREE_LARGEST_ROOT_PAGE, ref pgnoRoot);
        pgnoRoot++;

        /* The new root-page may not be allocated on a pointer-map page, or the
        ** PENDING_BYTE page.
        */
        while (pgnoRoot == PTRMAP_PAGENO(pBt, pgnoRoot) ||
        pgnoRoot == PENDING_BYTE_PAGE(pBt))
        {
          pgnoRoot++;
        }
        Debug.Assert(pgnoRoot >= 3);

        /* Allocate a page. The page that currently resides at pgnoRoot will
        ** be moved to the allocated page (unless the allocated page happens
        ** to reside at pgnoRoot).
        */
        rc = allocateBtreePage(pBt, ref pPageMove, ref pgnoMove, pgnoRoot, 1);
        if (rc != SQLITE_OK)
        {
          return rc;
        }

        if (pgnoMove != pgnoRoot)
        {
          /* pgnoRoot is the page that will be used for the root-page of
          ** the new table (assuming an error did not occur). But we were
          ** allocated pgnoMove. If required (i.e. if it was not allocated
          ** by extending the file), the current page at position pgnoMove
          ** is already journaled.
          */
          u8 eType = 0;
          Pgno iPtrPage = 0;

          releasePage(pPageMove);

          /* Move the page currently at pgnoRoot to pgnoMove. */
          rc = btreeGetPage(pBt, pgnoRoot, ref pRoot, 0);
          if (rc != SQLITE_OK)
          {
            return rc;
          }
          rc = ptrmapGet(pBt, pgnoRoot, ref eType, ref iPtrPage);
          if (eType == PTRMAP_ROOTPAGE || eType == PTRMAP_FREEPAGE)
          {
#if SQLITE_DEBUG || DEBUG
            rc = SQLITE_CORRUPT_BKPT();
#else
rc = SQLITE_CORRUPT_BKPT;
#endif
          }
          if (rc != SQLITE_OK)
          {
            releasePage(pRoot);
            return rc;
          }
          Debug.Assert(eType != PTRMAP_ROOTPAGE);
          Debug.Assert(eType != PTRMAP_FREEPAGE);
          rc = relocatePage(pBt, pRoot, eType, iPtrPage, pgnoMove, 0);
          releasePage(pRoot);

          /* Obtain the page at pgnoRoot */
          if (rc != SQLITE_OK)
          {
            return rc;
          }
          rc = btreeGetPage(pBt, pgnoRoot, ref pRoot, 0);
          if (rc != SQLITE_OK)
          {
            return rc;
          }
          rc = sqlite3PagerWrite(pRoot.pDbPage);
          if (rc != SQLITE_OK)
          {
            releasePage(pRoot);
            return rc;
          }
        }
        else
        {
          pRoot = pPageMove;
        }

        /* Update the pointer-map and meta-data with the new root-page number. */
        ptrmapPut(pBt, pgnoRoot, PTRMAP_ROOTPAGE, 0, ref rc);
        if (rc != 0)
        {
          releasePage(pRoot);
          return rc;
        }
        rc = sqlite3BtreeUpdateMeta(p, 4, pgnoRoot);
        if (rc != 0)
        {
          releasePage(pRoot);
          return rc;
        }

      }
      else
      {
        rc = allocateBtreePage(pBt, ref pRoot, ref pgnoRoot, 1, 0);
        if (rc != 0) return rc;
      }
#endif
      Debug.Assert(sqlite3PagerIswriteable(pRoot.pDbPage));
      zeroPage(pRoot, flags | PTF_LEAF);
      sqlite3PagerUnref(pRoot.pDbPage);
      piTable = (int)pgnoRoot;
      return SQLITE_OK;
    }
    static int sqlite3BtreeCreateTable(Btree p, ref int piTable, int flags)
    {
      int rc;
      sqlite3BtreeEnter(p);
      rc = btreeCreateTable(p, ref piTable, flags);
      sqlite3BtreeLeave(p);
      return rc;
    }

    /*
    ** Erase the given database page and all its children.  Return
    ** the page to the freelist.
    */
    static int clearDatabasePage(
    BtShared pBt,         /* The BTree that contains the table */
    Pgno pgno,            /* Page number to clear */
    int freePageFlag,     /* Deallocate page if true */
    ref int pnChange
    )
    {
      MemPage pPage = new MemPage();
      int rc;
      byte[] pCell;
      int i;

      Debug.Assert(sqlite3_mutex_held(pBt.mutex));
      if (pgno > pagerPagecount(pBt))
      {
#if SQLITE_DEBUG || DEBUG
        return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
      }

      rc = getAndInitPage(pBt, pgno, ref pPage);
      if (rc != 0) return rc;
      for (i = 0; i < pPage.nCell; i++)
      {
        int iCell = findCell(pPage, i); pCell = pPage.aData; //        pCell = findCell( pPage, i );
        if (0 == pPage.leaf)
        {
          rc = clearDatabasePage(pBt, sqlite3Get4byte(pCell, iCell), 1, ref pnChange);
          if (rc != 0) goto cleardatabasepage_out;
        }
        rc = clearCell(pPage, iCell);
        if (rc != 0) goto cleardatabasepage_out;
      }
      if (0 == pPage.leaf)
      {
        rc = clearDatabasePage(pBt, sqlite3Get4byte(pPage.aData, 8), 1, ref pnChange);
        if (rc != 0) goto cleardatabasepage_out;
      }
      else //if (pnChange != 0)
      {
        //Debug.Assert(pPage.intKey != 0);
        pnChange += pPage.nCell;
      }
      if (freePageFlag != 0)
      {
        freePage(pPage, ref rc);
      }
      else if ((rc = sqlite3PagerWrite(pPage.pDbPage)) == 0)
      {
        zeroPage(pPage, pPage.aData[0] | PTF_LEAF);
      }

    cleardatabasepage_out:
      releasePage(pPage);
      return rc;
    }

    /*
    ** Delete all information from a single table in the database.  iTable is
    ** the page number of the root of the table.  After this routine returns,
    ** the root page is empty, but still exists.
    **
    ** This routine will fail with SQLITE_LOCKED if there are any open
    ** read cursors on the table.  Open write cursors are moved to the
    ** root of the table.
    **
    ** If pnChange is not NULL, then table iTable must be an intkey table. The
    ** integer value pointed to by pnChange is incremented by the number of
    ** entries in the table.
    */
    static int sqlite3BtreeClearTable(Btree p, int iTable, ref int pnChange)
    {
      int rc;
      BtShared pBt = p.pBt;
      sqlite3BtreeEnter(p);
      Debug.Assert(p.inTrans == TRANS_WRITE);

      /* Invalidate all incrblob cursors open on table iTable (assuming iTable
      ** is the root of a table b-tree - if it is not, the following call is
      ** a no-op).  */
      invalidateIncrblobCursors(p, 0, 1);

      rc = saveAllCursors(pBt, (Pgno)iTable, null);
      if (SQLITE_OK == rc)
      {
        rc = clearDatabasePage(pBt, (Pgno)iTable, 0, ref pnChange);
      }
      sqlite3BtreeLeave(p);
      return rc;
    }

    /*
    ** Erase all information in a table and add the root of the table to
    ** the freelist.  Except, the root of the principle table (the one on
    ** page 1) is never added to the freelist.
    **
    ** This routine will fail with SQLITE_LOCKED if there are any open
    ** cursors on the table.
    **
    ** If AUTOVACUUM is enabled and the page at iTable is not the last
    ** root page in the database file, then the last root page
    ** in the database file is moved into the slot formerly occupied by
    ** iTable and that last slot formerly occupied by the last root page
    ** is added to the freelist instead of iTable.  In this say, all
    ** root pages are kept at the beginning of the database file, which
    ** is necessary for AUTOVACUUM to work right.  piMoved is set to the
    ** page number that used to be the last root page in the file before
    ** the move.  If no page gets moved, piMoved is set to 0.
    ** The last root page is recorded in meta[3] and the value of
    ** meta[3] is updated by this procedure.
    */
    static int btreeDropTable(Btree p, Pgno iTable, ref int piMoved)
    {
      int rc;
      MemPage pPage = null;
      BtShared pBt = p.pBt;

      Debug.Assert(sqlite3BtreeHoldsMutex(p));
      Debug.Assert(p.inTrans == TRANS_WRITE);

      /* It is illegal to drop a table if any cursors are open on the
      ** database. This is because in auto-vacuum mode the backend may
      ** need to move another root-page to fill a gap left by the deleted
      ** root page. If an open cursor was using this page a problem would
      ** occur.
      **
      ** This error is caught long before control reaches this point.
      */
      if (NEVER(pBt.pCursor))
      {
        sqlite3ConnectionBlocked(p.db, pBt.pCursor.pBtree.db);
        return SQLITE_LOCKED_SHAREDCACHE;
      }

      rc = btreeGetPage(pBt, (Pgno)iTable, ref pPage, 0);
      if (rc != 0) return rc;
      int Dummy0 = 0; rc = sqlite3BtreeClearTable(p, (int)iTable, ref Dummy0);
      if (rc != 0)
      {
        releasePage(pPage);
        return rc;
      }

      piMoved = 0;

      if (iTable > 1)
      {
#if SQLITE_OMIT_AUTOVACUUM
freePage(pPage, ref rc);
releasePage(pPage);
#else
        if (pBt.autoVacuum)
        {
          Pgno maxRootPgno = 0;
          sqlite3BtreeGetMeta(p, BTREE_LARGEST_ROOT_PAGE, ref maxRootPgno);

          if (iTable == maxRootPgno)
          {
            /* If the table being dropped is the table with the largest root-page
            ** number in the database, put the root page on the free list.
            */
            freePage(pPage, ref rc);
            releasePage(pPage);
            if (rc != SQLITE_OK)
            {
              return rc;
            }
          }
          else
          {
            /* The table being dropped does not have the largest root-page
            ** number in the database. So move the page that does into the
            ** gap left by the deleted root-page.
            */
            MemPage pMove = new MemPage();
            releasePage(pPage);
            rc = btreeGetPage(pBt, maxRootPgno, ref pMove, 0);
            if (rc != SQLITE_OK)
            {
              return rc;
            }
            rc = relocatePage(pBt, pMove, PTRMAP_ROOTPAGE, 0, iTable, 0);
            releasePage(pMove);
            if (rc != SQLITE_OK)
            {
              return rc;
            }
            pMove = null;
            rc = btreeGetPage(pBt, maxRootPgno, ref pMove, 0);
            freePage(pMove, ref rc);
            releasePage(pMove);
            if (rc != SQLITE_OK)
            {
              return rc;
            }
            piMoved = (int)maxRootPgno;
          }

          /* Set the new 'max-root-page' value in the database header. This
          ** is the old value less one, less one more if that happens to
          ** be a root-page number, less one again if that is the
          ** PENDING_BYTE_PAGE.
          */
          maxRootPgno--;
          while (maxRootPgno == PENDING_BYTE_PAGE(pBt)
          || PTRMAP_ISPAGE(pBt, maxRootPgno))
          {
            maxRootPgno--;
          }
          Debug.Assert(maxRootPgno != PENDING_BYTE_PAGE(pBt));

          rc = sqlite3BtreeUpdateMeta(p, 4, maxRootPgno);
        }
        else
        {
          freePage(pPage, ref rc);
          releasePage(pPage);
        }
#endif
      }
      else
      {
        /* If sqlite3BtreeDropTable was called on page 1.
        ** This really never should happen except in a corrupt
        ** database.
        */
        zeroPage(pPage, PTF_INTKEY | PTF_LEAF);
        releasePage(pPage);
      }
      return rc;
    }
    static int sqlite3BtreeDropTable(Btree p, int iTable, ref int piMoved)
    {
      int rc;
      sqlite3BtreeEnter(p);
      rc = btreeDropTable(p, (u32)iTable, ref piMoved);
      sqlite3BtreeLeave(p);
      return rc;
    }


    /*
    ** This function may only be called if the b-tree connection already
    ** has a read or write transaction open on the database.
    **
    ** Read the meta-information out of a database file.  Meta[0]
    ** is the number of free pages currently in the database.  Meta[1]
    ** through meta[15] are available for use by higher layers.  Meta[0]
    ** is read-only, the others are read/write.
    **
    ** The schema layer numbers meta values differently.  At the schema
    ** layer (and the SetCookie and ReadCookie opcodes) the number of
    ** free pages is not visible.  So Cookie[0] is the same as Meta[1].
    */
    static void sqlite3BtreeGetMeta(Btree p, int idx, ref u32 pMeta)
    {
      BtShared pBt = p.pBt;

      sqlite3BtreeEnter(p);
      Debug.Assert(p.inTrans > TRANS_NONE);
      Debug.Assert(SQLITE_OK == querySharedCacheTableLock(p, MASTER_ROOT, READ_LOCK));
      Debug.Assert(pBt.pPage1 != null);
      Debug.Assert(idx >= 0 && idx <= 15);

      pMeta = sqlite3Get4byte(pBt.pPage1.aData, 36 + idx * 4);

      /* If auto-vacuum is disabled in this build and this is an auto-vacuum
      ** database, mark the database as read-only.  */
#if SQLITE_OMIT_AUTOVACUUM
if( idx==BTREE_LARGEST_ROOT_PAGE && pMeta>0 ) pBt.readOnly = 1;
#endif

      sqlite3BtreeLeave(p);
    }

    /*
    ** Write meta-information back into the database.  Meta[0] is
    ** read-only and may not be written.
    */
    static int sqlite3BtreeUpdateMeta(Btree p, int idx, u32 iMeta)
    {
      BtShared pBt = p.pBt;
      byte[] pP1;
      int rc;
      Debug.Assert(idx >= 1 && idx <= 15);
      sqlite3BtreeEnter(p);
      Debug.Assert(p.inTrans == TRANS_WRITE);
      Debug.Assert(pBt.pPage1 != null);
      pP1 = pBt.pPage1.aData;
      rc = sqlite3PagerWrite(pBt.pPage1.pDbPage);
      if (rc == SQLITE_OK)
      {
        sqlite3Put4byte(pP1, 36 + idx * 4, iMeta);
#if !SQLITE_OMIT_AUTOVACUUM
        if (idx == BTREE_INCR_VACUUM)
        {
          Debug.Assert(pBt.autoVacuum || iMeta == 0);
          Debug.Assert(iMeta == 0 || iMeta == 1);
          pBt.incrVacuum = iMeta != 0;
        }
#endif
      }
      sqlite3BtreeLeave(p);
      return rc;
    }

#if !SQLITE_OMIT_BTREECOUNT
    /*
** The first argument, pCur, is a cursor opened on some b-tree. Count the
** number of entries in the b-tree and write the result to pnEntry.
**
** SQLITE_OK is returned if the operation is successfully executed.
** Otherwise, if an error is encountered (i.e. an IO error or database
** corruption) an SQLite error code is returned.
*/
    static int sqlite3BtreeCount(BtCursor pCur, ref i64 pnEntry)
    {
      i64 nEntry = 0;                      /* Value to return in pnEntry */
      int rc;                              /* Return code */
      rc = moveToRoot(pCur);

      /* Unless an error occurs, the following loop runs one iteration for each
      ** page in the B-Tree structure (not including overflow pages).
      */
      while (rc == SQLITE_OK)
      {
        int iIdx;                          /* Index of child node in parent */
        MemPage pPage;                    /* Current page of the b-tree */

        /* If this is a leaf page or the tree is not an int-key tree, then
        ** this page contains countable entries. Increment the entry counter
        ** accordingly.
        */
        pPage = pCur.apPage[pCur.iPage];
        if (pPage.leaf != 0 || 0 == pPage.intKey)
        {
          nEntry += pPage.nCell;
        }

        /* pPage is a leaf node. This loop navigates the cursor so that it
        ** points to the first interior cell that it points to the parent of
        ** the next page in the tree that has not yet been visited. The
        ** pCur.aiIdx[pCur.iPage] value is set to the index of the parent cell
        ** of the page, or to the number of cells in the page if the next page
        ** to visit is the right-child of its parent.
        **
        ** If all pages in the tree have been visited, return SQLITE_OK to the
        ** caller.
        */
        if (pPage.leaf != 0)
        {
          do
          {
            if (pCur.iPage == 0)
            {
              /* All pages of the b-tree have been visited. Return successfully. */
              pnEntry = nEntry;
              return SQLITE_OK;
            }
            moveToParent(pCur);
          } while (pCur.aiIdx[pCur.iPage] >= pCur.apPage[pCur.iPage].nCell);

          pCur.aiIdx[pCur.iPage]++;
          pPage = pCur.apPage[pCur.iPage];
        }

        /* Descend to the child node of the cell that the cursor currently
        ** points at. This is the right-child if (iIdx==pPage.nCell).
        */
        iIdx = pCur.aiIdx[pCur.iPage];
        if (iIdx == pPage.nCell)
        {
          rc = moveToChild(pCur, sqlite3Get4byte(pPage.aData, pPage.hdrOffset + 8));
        }
        else
        {
          rc = moveToChild(pCur, sqlite3Get4byte(pPage.aData, findCell(pPage, iIdx)));
        }
      }

      /* An error has occurred. Return an error code. */
      return rc;
    }
#endif

    /*
** Return the pager associated with a BTree.  This routine is used for
** testing and debugging only.
*/
    static Pager sqlite3BtreePager(Btree p)
    {
      return p.pBt.pPager;
    }

#if !SQLITE_OMIT_INTEGRITY_CHECK
    /*
** Append a message to the error message string.
*/
    static void checkAppendMsg(
    IntegrityCk pCheck,
    string zMsg1,
    string zFormat,
    params object[] ap
    )
    {
      //va_list ap;
      if (0 == pCheck.mxErr) return;
      pCheck.mxErr--;
      pCheck.nErr++;
      va_start(ap, zFormat);
      if (pCheck.errMsg.nChar != 0)
      {
        sqlite3StrAccumAppend(pCheck.errMsg, "\n", 1);
      }
      if (!String.IsNullOrEmpty(zMsg1))
      {
        sqlite3StrAccumAppend(pCheck.errMsg, zMsg1, -1);
      }
      sqlite3VXPrintf(pCheck.errMsg, 1, zFormat, ap);
      va_end(ap);
      //if( pCheck.errMsg.mallocFailed ){
      //  pCheck.mallocFailed = 1;
      //}
    }
#endif //* SQLITE_OMIT_INTEGRITY_CHECK */

#if !SQLITE_OMIT_INTEGRITY_CHECK
    /*
** Add 1 to the reference count for page iPage.  If this is the second
** reference to the page, add an error message to pCheck.zErrMsg.
** Return 1 if there are 2 ore more references to the page and 0 if
** if this is the first reference to the page.
**
** Also check that the page number is in bounds.
*/
    static int checkRef(IntegrityCk pCheck, Pgno iPage, string zContext)
    {
      if (iPage == 0) return 1;
      if (iPage > pCheck.nPage)
      {
        checkAppendMsg(pCheck, zContext, "invalid page number %d", iPage);
        return 1;
      }
      if (pCheck.anRef[iPage] == 1)
      {
        checkAppendMsg(pCheck, zContext, "2nd reference to page %d", iPage);
        return 1;
      }
      return ((pCheck.anRef[iPage]++) > 1) ? 1 : 0;
    }

#if !SQLITE_OMIT_AUTOVACUUM
    /*
** Check that the entry in the pointer-map for page iChild maps to
** page iParent, pointer type ptrType. If not, append an error message
** to pCheck.
*/
    static void checkPtrmap(
    IntegrityCk pCheck,    /* Integrity check context */
    Pgno iChild,           /* Child page number */
    u8 eType,              /* Expected pointer map type */
    Pgno iParent,          /* Expected pointer map parent page number */
    string zContext        /* Context description (used for error msg) */
    )
    {
      int rc;
      u8 ePtrmapType = 0;
      Pgno iPtrmapParent = 0;

      rc = ptrmapGet(pCheck.pBt, iChild, ref ePtrmapType, ref iPtrmapParent);
      if (rc != SQLITE_OK)
      {
        //if( rc==SQLITE_NOMEM || rc==SQLITE_IOERR_NOMEM ) pCheck.mallocFailed = 1;
        checkAppendMsg(pCheck, zContext, "Failed to read ptrmap key=%d", iChild);
        return;
      }

      if (ePtrmapType != eType || iPtrmapParent != iParent)
      {
        checkAppendMsg(pCheck, zContext,
        "Bad ptr map entry key=%d expected=(%d,%d) got=(%d,%d)",
        iChild, eType, iParent, ePtrmapType, iPtrmapParent);
      }
    }
#endif

    /*
** Check the integrity of the freelist or of an overflow page list.
** Verify that the number of pages on the list is N.
*/
    static void checkList(
    IntegrityCk pCheck,  /* Integrity checking context */
    int isFreeList,       /* True for a freelist.  False for overflow page list */
    int iPage,            /* Page number for first page in the list */
    int N,                /* Expected number of pages in the list */
    string zContext        /* Context for error messages */
    )
    {
      int i;
      int expected = N;
      int iFirst = iPage;
      while (N-- > 0 && pCheck.mxErr != 0)
      {
        DbPage pOvflPage = new PgHdr();
        byte[] pOvflData;
        if (iPage < 1)
        {
          checkAppendMsg(pCheck, zContext,
          "%d of %d pages missing from overflow list starting at %d",
          N + 1, expected, iFirst);
          break;
        }
        if (checkRef(pCheck, (u32)iPage, zContext) != 0) break;
        if (sqlite3PagerGet(pCheck.pPager, (Pgno)iPage, ref pOvflPage) != 0)
        {
          checkAppendMsg(pCheck, zContext, "failed to get page %d", iPage);
          break;
        }
        pOvflData = sqlite3PagerGetData(pOvflPage);
        if (isFreeList != 0)
        {
          int n = (int)sqlite3Get4byte(pOvflData, 4);
#if !SQLITE_OMIT_AUTOVACUUM
          if (pCheck.pBt.autoVacuum)
          {
            checkPtrmap(pCheck, (u32)iPage, PTRMAP_FREEPAGE, 0, zContext);
          }
#endif
          if (n > pCheck.pBt.usableSize / 4 - 2)
          {
            checkAppendMsg(pCheck, zContext,
            "freelist leaf count too big on page %d", iPage);
            N--;
          }
          else
          {
            for (i = 0; i < n; i++)
            {
              Pgno iFreePage = sqlite3Get4byte(pOvflData, 8 + i * 4);
#if !SQLITE_OMIT_AUTOVACUUM
              if (pCheck.pBt.autoVacuum)
              {
                checkPtrmap(pCheck, iFreePage, PTRMAP_FREEPAGE, 0, zContext);
              }
#endif
              checkRef(pCheck, iFreePage, zContext);
            }
            N -= n;
          }
        }
#if !SQLITE_OMIT_AUTOVACUUM
        else
        {
          /* If this database supports auto-vacuum and iPage is not the last
          ** page in this overflow list, check that the pointer-map entry for
          ** the following page matches iPage.
          */
          if (pCheck.pBt.autoVacuum && N > 0)
          {
            i = (int)sqlite3Get4byte(pOvflData);
            checkPtrmap(pCheck, (u32)i, PTRMAP_OVERFLOW2, (u32)iPage, zContext);
          }
        }
#endif
        iPage = (int)sqlite3Get4byte(pOvflData);
        sqlite3PagerUnref(pOvflPage);
      }
    }
#endif //* SQLITE_OMIT_INTEGRITY_CHECK */

#if !SQLITE_OMIT_INTEGRITY_CHECK
    /*
** Do various sanity checks on a single page of a tree.  Return
** the tree depth.  Root pages return 0.  Parents of root pages
** return 1, and so forth.
**
** These checks are done:
**
**      1.  Make sure that cells and freeblocks do not overlap
**          but combine to completely cover the page.
**  NO  2.  Make sure cell keys are in order.
**  NO  3.  Make sure no key is less than or equal to zLowerBound.
**  NO  4.  Make sure no key is greater than or equal to zUpperBound.
**      5.  Check the integrity of overflow pages.
**      6.  Recursively call checkTreePage on all children.
**      7.  Verify that the depth of all children is the same.
**      8.  Make sure this page is at least 33% full or else it is
**          the root of the tree.
*/
    static int checkTreePage(
    IntegrityCk pCheck,  /* Context for the sanity check */
    int iPage,            /* Page number of the page to check */
    string zParentContext  /* Parent context */
    )
    {
      MemPage pPage = new MemPage();
      int i, rc, depth, d2, pgno, cnt;
      int hdr, cellStart;
      int nCell;
      u8[] data;
      BtShared pBt;
      int usableSize;
      string zContext = "";//[100];
      byte[] hit = null;


      sqlite3_snprintf(200, ref zContext, "Page %d: ", iPage);

      /* Check that the page exists
      */
      pBt = pCheck.pBt;
      usableSize = pBt.usableSize;
      if (iPage == 0) return 0;
      if (checkRef(pCheck, (u32)iPage, zParentContext) != 0) return 0;
      if ((rc = btreeGetPage(pBt, (Pgno)iPage, ref pPage, 0)) != 0)
      {
        checkAppendMsg(pCheck, zContext,
        "unable to get the page. error code=%d", rc);
        return 0;
      }

      /* Clear MemPage.isInit to make sure the corruption detection code in
      ** btreeInitPage() is executed.  */
      pPage.isInit = 0;
      if ((rc = btreeInitPage(pPage)) != 0)
      {
        Debug.Assert(rc == SQLITE_CORRUPT);  /* The only possible error from InitPage */
        checkAppendMsg(pCheck, zContext,
        "btreeInitPage() returns error code %d", rc);
        releasePage(pPage);
        return 0;
      }

      /* Check out all the cells.
      */
      depth = 0;
      for (i = 0; i < pPage.nCell && pCheck.mxErr != 0; i++)
      {
        u8[] pCell;
        u32 sz;
        CellInfo info = new CellInfo();

        /* Check payload overflow pages
        */
        sqlite3_snprintf(200, ref zContext,
        "On tree page %d cell %d: ", iPage, i);
        int iCell = findCell(pPage, i); //pCell = findCell( pPage, i );
        pCell = pPage.aData;
        btreeParseCellPtr( pPage, iCell, ref info ); //btreeParseCellPtr( pPage, pCell, info );
        sz = info.nData;
        if (0 == pPage.intKey) sz += (u32)info.nKey;
        Debug.Assert(sz == info.nPayload);
        if ((sz > info.nLocal)
          //&& (pCell[info.iOverflow]<=&pPage.aData[pBt.usableSize])
        )
        {
          int nPage = (int)(sz - info.nLocal + usableSize - 5) / (usableSize - 4);
          Pgno pgnoOvfl = sqlite3Get4byte(pCell, iCell, info.iOverflow);
#if !SQLITE_OMIT_AUTOVACUUM
          if (pBt.autoVacuum)
          {
            checkPtrmap(pCheck, pgnoOvfl, PTRMAP_OVERFLOW1, (u32)iPage, zContext);
          }
#endif
          checkList(pCheck, 0, (int)pgnoOvfl, nPage, zContext);
        }

        /* Check sanity of left child page.
        */
        if (0 == pPage.leaf)
        {
          pgno = (int)sqlite3Get4byte(pCell, iCell); //sqlite3Get4byte( pCell );
#if !SQLITE_OMIT_AUTOVACUUM
          if (pBt.autoVacuum)
          {
            checkPtrmap(pCheck, (u32)pgno, PTRMAP_BTREE, (u32)iPage, zContext);
          }
#endif
          d2 = checkTreePage(pCheck, pgno, zContext);
          if (i > 0 && d2 != depth)
          {
            checkAppendMsg(pCheck, zContext, "Child page depth differs");
          }
          depth = d2;
        }
      }
      if (0 == pPage.leaf)
      {
        pgno = (int)sqlite3Get4byte(pPage.aData, pPage.hdrOffset + 8);
        sqlite3_snprintf(200, ref zContext,
        "On page %d at right child: ", iPage);
#if !SQLITE_OMIT_AUTOVACUUM
        if (pBt.autoVacuum)
        {
          checkPtrmap(pCheck, (u32)pgno, PTRMAP_BTREE, (u32)iPage, "");
        }
#endif
        checkTreePage(pCheck, pgno, zContext);
      }

      /* Check for complete coverage of the page
      */
      data = pPage.aData;
      hdr = pPage.hdrOffset;
      hit = new byte[pBt.pageSize]; //sqlite3PageMalloc( pBt.pageSize );
      //if( hit==null ){
      //  pCheck.mallocFailed = 1;
      //}else
      {
        u16 contentOffset = (u16)get2byte(data, hdr + 5);
        Debug.Assert(contentOffset <= usableSize);  /* Enforced by btreeInitPage() */
        //memset(hit+contentOffset, 0, usableSize-contentOffset);
        //memset(hit, 1, contentOffset);
        for (int iLoop = contentOffset - 1; iLoop >= 0; iLoop--) hit[iLoop] = 1;
        nCell = get2byte(data, hdr + 3);
        cellStart = hdr + 12 - 4 * pPage.leaf;
        for (i = 0; i < nCell; i++)
        {
          int pc = get2byte(data, cellStart + i * 2);
          u16 size = 1024;
          int j;
          if (pc <= usableSize - 4)
          {
            size = cellSizePtr(pPage, data, pc);
          }
          if ((pc + size - 1) >= usableSize)
          {
            checkAppendMsg(pCheck, null,
            "Corruption detected in cell %d on page %d", i, iPage, 0);
          }
          else
          {
            for (j = pc + size - 1; j >= pc; j--) hit[j]++;
          }
        }
        i = get2byte(data, hdr + 1);
        while (i > 0)
        {
          int size, j;
          Debug.Assert(i <= usableSize - 4);     /* Enforced by btreeInitPage() */
          size = get2byte(data, i + 2);
          Debug.Assert(i + size <= usableSize);  /* Enforced by btreeInitPage() */
          for (j = i + size - 1; j >= i; j--) hit[j]++;
          j = get2byte(data, i);
          Debug.Assert(j == 0 || j > i + size);  /* Enforced by btreeInitPage() */
          Debug.Assert(j <= usableSize - 4);   /* Enforced by btreeInitPage() */
          i = j;
        }
        for (i = cnt = 0; i < usableSize; i++)
        {
          if (hit[i] == 0)
          {
            cnt++;
          }
          else if (hit[i] > 1)
          {
            checkAppendMsg(pCheck, "",
            "Multiple uses for byte %d of page %d", i, iPage);
            break;
          }
        }
        if (cnt != data[hdr + 7])
        {
          checkAppendMsg(pCheck, null,
          "Fragmentation of %d bytes reported as %d on page %d",
          cnt, data[hdr + 7], iPage);
        }
      }
      //      sqlite3PageFree(ref hit);
      releasePage(pPage);
      return depth + 1;
    }
#endif //* SQLITE_OMIT_INTEGRITY_CHECK */

#if !SQLITE_OMIT_INTEGRITY_CHECK
    /*
** This routine does a complete check of the given BTree file.  aRoot[] is
** an array of pages numbers were each page number is the root page of
** a table.  nRoot is the number of entries in aRoot.
**
** A read-only or read-write transaction must be opened before calling
** this function.
**
** Write the number of error seen in pnErr.  Except for some memory
** allocation errors,  an error message held in memory obtained from
** malloc is returned if pnErr is non-zero.  If pnErr==null then NULL is
** returned.  If a memory allocation error occurs, NULL is returned.
*/
    static string sqlite3BtreeIntegrityCheck(
    Btree p,       /* The btree to be checked */
    int[] aRoot,   /* An array of root pages numbers for individual trees */
    int nRoot,     /* Number of entries in aRoot[] */
    int mxErr,     /* Stop reporting errors after this many */
    ref int pnErr  /* Write number of errors seen to this variable */
    )
    {
      Pgno i;
      int nRef;
      IntegrityCk sCheck = new IntegrityCk();
      BtShared pBt = p.pBt;
      StringBuilder zErr = new StringBuilder(100);//char zErr[100];


      sqlite3BtreeEnter(p);
      Debug.Assert(p.inTrans > TRANS_NONE && pBt.inTransaction > TRANS_NONE);
      nRef = sqlite3PagerRefcount(pBt.pPager);
      sCheck.pBt = pBt;
      sCheck.pPager = pBt.pPager;
      sCheck.nPage = pagerPagecount(sCheck.pBt);
      sCheck.mxErr = mxErr;
      sCheck.nErr = 0;
      //sCheck.mallocFailed = 0;
      pnErr = 0;
      if (sCheck.nPage == 0)
      {
        sqlite3BtreeLeave(p);
        return "";
      }
      sCheck.anRef = new int[sCheck.nPage + 1];//sqlite3Malloc( (sCheck.nPage+1)*sizeof(sCheck.anRef[0]) );
      //if( !sCheck.anRef ){
      //  pnErr = 1;
      //  sqlite3BtreeLeave(p);
      //  return 0;
      //}
      // for (i = 0; i <= sCheck.nPage; i++) { sCheck.anRef[i] = 0; }
      i = PENDING_BYTE_PAGE(pBt);
      if (i <= sCheck.nPage)
      {
        sCheck.anRef[i] = 1;
      }
      sqlite3StrAccumInit(sCheck.errMsg, zErr, zErr.Capacity, 20000);

      /* Check the integrity of the freelist
      */
      checkList(sCheck, 1, (int)sqlite3Get4byte(pBt.pPage1.aData, 32),
      (int)sqlite3Get4byte(pBt.pPage1.aData, 36), "Main freelist: ");

      /* Check all the tables.
      */
      for (i = 0; (int)i < nRoot && sCheck.mxErr != 0; i++)
      {
        if (aRoot[i] == 0) continue;
#if !SQLITE_OMIT_AUTOVACUUM
        if (pBt.autoVacuum && aRoot[i] > 1)
        {
          checkPtrmap(sCheck, (u32)aRoot[i], PTRMAP_ROOTPAGE, 0, "");
        }
#endif
        checkTreePage(sCheck, aRoot[i], "List of tree roots: ");
      }

      /* Make sure every page in the file is referenced
      */
      for (i = 1; i <= sCheck.nPage && sCheck.mxErr != 0; i++)
      {
#if SQLITE_OMIT_AUTOVACUUM
if( sCheck.anRef[i]==null ){
checkAppendMsg(sCheck, 0, "Page %d is never used", i);
}
#else
        /* If the database supports auto-vacuum, make sure no tables contain
** references to pointer-map pages.
*/
        if (sCheck.anRef[i] == 0 &&
        (PTRMAP_PAGENO(pBt, i) != i || !pBt.autoVacuum))
        {
          checkAppendMsg(sCheck, null, "Page %d is never used", i);
        }
        if (sCheck.anRef[i] != 0 &&
        (PTRMAP_PAGENO(pBt, i) == i && pBt.autoVacuum))
        {
          checkAppendMsg(sCheck, null, "Pointer map page %d is referenced", i);
        }
#endif
      }

      /* Make sure this analysis did not leave any unref() pages.
      ** This is an internal consistency check; an integrity check
      ** of the integrity check.
      */
      if (NEVER(nRef != sqlite3PagerRefcount(pBt.pPager)))
      {
        checkAppendMsg(sCheck, null,
        "Outstanding page count goes from %d to %d during this analysis",
        nRef, sqlite3PagerRefcount(pBt.pPager)
        );
      }

      /* Clean  up and report errors.
      */
      sqlite3BtreeLeave(p);
      sCheck.anRef = null;// sqlite3_free( ref sCheck.anRef );
      //if( sCheck.mallocFailed ){
      //  sqlite3StrAccumReset(sCheck.errMsg);
      //  pnErr = sCheck.nErr+1;
      //  return 0;
      //}
      pnErr = sCheck.nErr;
      if (sCheck.nErr == 0) sqlite3StrAccumReset(sCheck.errMsg);
      return sqlite3StrAccumFinish(sCheck.errMsg);
    }
#endif //* SQLITE_OMIT_INTEGRITY_CHECK */

    /*
** Return the full pathname of the underlying database file.
**
** The pager filename is invariant as long as the pager is
** open so it is safe to access without the BtShared mutex.
*/
    static string sqlite3BtreeGetFilename(Btree p)
    {
      Debug.Assert(p.pBt.pPager != null);
      return sqlite3PagerFilename(p.pBt.pPager);
    }

    /*
    ** Return the pathname of the journal file for this database. The return
    ** value of this routine is the same regardless of whether the journal file
    ** has been created or not.
    **
    ** The pager journal filename is invariant as long as the pager is
    ** open so it is safe to access without the BtShared mutex.
    */
    static string sqlite3BtreeGetJournalname(Btree p)
    {
      Debug.Assert(p.pBt.pPager != null);
      return sqlite3PagerJournalname(p.pBt.pPager);
    }

    /*
    ** Return non-zero if a transaction is active.
    */
    static bool sqlite3BtreeIsInTrans(Btree p)
    {
      Debug.Assert(p == null || sqlite3_mutex_held(p.db.mutex));
      return (p != null && (p.inTrans == TRANS_WRITE));
    }

    /*
    ** Return non-zero if a read (or write) transaction is active.
    */
    static bool sqlite3BtreeIsInReadTrans(Btree p)
    {
      Debug.Assert(p != null);
      Debug.Assert(sqlite3_mutex_held(p.db.mutex));
      return p.inTrans != TRANS_NONE;
    }

    static bool sqlite3BtreeIsInBackup(Btree p)
    {
      Debug.Assert(p != null);
      Debug.Assert(sqlite3_mutex_held(p.db.mutex));
      return p.nBackup != 0;
    }

    /*
    ** This function returns a pointer to a blob of memory associated with
    ** a single shared-btree. The memory is used by client code for its own
    ** purposes (for example, to store a high-level schema associated with
    ** the shared-btree). The btree layer manages reference counting issues.
    **
    ** The first time this is called on a shared-btree, nBytes bytes of memory
    ** are allocated, zeroed, and returned to the caller. For each subsequent
    ** call the nBytes parameter is ignored and a pointer to the same blob
    ** of memory returned.
    **
    ** If the nBytes parameter is 0 and the blob of memory has not yet been
    ** allocated, a null pointer is returned. If the blob has already been
    ** allocated, it is returned as normal.
    **
    ** Just before the shared-btree is closed, the function passed as the
    ** xFree argument when the memory allocation was made is invoked on the
    ** blob of allocated memory. This function should not call sqlite3_free(ref )
    ** on the memory, the btree layer does that.
    */
    static Schema sqlite3BtreeSchema(Btree p, int nBytes, dxFreeSchema xFree)
    {
      BtShared pBt = p.pBt;
      sqlite3BtreeEnter(p);
      if (null == pBt.pSchema && nBytes != 0)
      {
        pBt.pSchema = new Schema();//sqlite3MallocZero(nBytes);
        pBt.xFreeSchema = xFree;
      }
      sqlite3BtreeLeave(p);
      return pBt.pSchema;
    }

    /*
    ** Return SQLITE_LOCKED_SHAREDCACHE if another user of the same shared
    ** btree as the argument handle holds an exclusive lock on the
    ** sqlite_master table. Otherwise SQLITE_OK.
    */
    static int sqlite3BtreeSchemaLocked(Btree p)
    {
      int rc;
      Debug.Assert(sqlite3_mutex_held(p.db.mutex));
      sqlite3BtreeEnter(p);
      rc = querySharedCacheTableLock(p, MASTER_ROOT, READ_LOCK);
      Debug.Assert(rc == SQLITE_OK || rc == SQLITE_LOCKED_SHAREDCACHE);
      sqlite3BtreeLeave(p);
      return rc;
    }


#if !SQLITE_OMIT_SHARED_CACHE
/*
** Obtain a lock on the table whose root page is iTab.  The
** lock is a write lock if isWritelock is true or a read lock
** if it is false.
*/
int sqlite3BtreeLockTable(Btree p, int iTab, u8 isWriteLock){
int rc = SQLITE_OK;
Debug.Assert( p.inTrans!=TRANS_NONE );
if( p.sharable ){
u8 lockType = READ_LOCK + isWriteLock;
Debug.Assert( READ_LOCK+1==WRITE_LOCK );
Debug.Assert( isWriteLock==null || isWriteLock==1 );

sqlite3BtreeEnter(p);
rc = querySharedCacheTableLock(p, iTab, lockType);
if( rc==SQLITE_OK ){
rc = setSharedCacheTableLock(p, iTab, lockType);
}
sqlite3BtreeLeave(p);
}
return rc;
}
#endif

#if !SQLITE_OMIT_INCRBLOB
/*
** Argument pCsr must be a cursor opened for writing on an
** INTKEY table currently pointing at a valid table entry.
** This function modifies the data stored as part of that entry.
**
** Only the data content may only be modified, it is not possible to
** change the length of the data stored. If this function is called with
** parameters that attempt to write past the end of the existing data,
** no modifications are made and SQLITE_CORRUPT is returned.
*/
int sqlite3BtreePutData(BtCursor pCsr, u32 offset, u32 amt, void *z){
int rc;
Debug.Assert( cursorHoldsMutex(pCsr) );
Debug.Assert( sqlite3_mutex_held(pCsr.pBtree.db.mutex) );
Debug.Assert( pCsr.isIncrblobHandle );

rc = restoreCursorPosition(pCsr);
if( rc!=SQLITE_OK ){
return rc;
}
Debug.Assert( pCsr.eState!=CURSOR_REQUIRESEEK );
if( pCsr.eState!=CURSOR_VALID ){
return SQLITE_ABORT;
}

/* Check some assumptions:
**   (a) the cursor is open for writing,
**   (b) there is a read/write transaction open,
**   (c) the connection holds a write-lock on the table (if required),
**   (d) there are no conflicting read-locks, and
**   (e) the cursor points at a valid row of an intKey table.
*/
if( !pCsr.wrFlag ){
return SQLITE_READONLY;
}
Debug.Assert( !pCsr.pBt.readOnly && pCsr.pBt.inTransaction==TRANS_WRITE );
Debug.Assert( hasSharedCacheTableLock(pCsr.pBtree, pCsr.pgnoRoot, 0, 2) );
Debug.Assert( !hasReadConflicts(pCsr.pBtree, pCsr.pgnoRoot) );
Debug.Assert( pCsr.apPage[pCsr.iPage].intKey );

return accessPayload(pCsr, offset, amt, (byte[] *)z, 1);
}

/*
** Set a flag on this cursor to cache the locations of pages from the
** overflow list for the current row. This is used by cursors opened
** for incremental blob IO only.
**
** This function sets a flag only. The actual page location cache
** (stored in BtCursor.aOverflow[]) is allocated and used by function
** accessPayload() (the worker function for sqlite3BtreeData() and
** sqlite3BtreePutData()).
*/
void sqlite3BtreeCacheOverflow(BtCursor pCur){
Debug.Assert( cursorHoldsMutex(pCur) );
Debug.Assert( sqlite3_mutex_held(pCur.pBtree.db.mutex) );
Debug.Assert(!pCur.isIncrblobHandle);
Debug.Assert(!pCur.aOverflow);
pCur.isIncrblobHandle = 1;
}
#endif
  }
}
