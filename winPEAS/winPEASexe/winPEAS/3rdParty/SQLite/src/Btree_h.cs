namespace winPEAS._3rdParty.SQLite.src
{
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
    ** This header file defines the interface that the sqlite B-Tree file
    ** subsystem.  See comments in the source code for a detailed description
    ** of what each interface routine does.
    **
    ** @(#) $Id: btree.h,v 1.120 2009/07/22 00:35:24 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#if !_BTREE_H_
    //#define _BTREE_H_

    /* TODO: This definition is just included so other modules compile. It
    ** needs to be revisited.
    */
    const int SQLITE_N_BTREE_META = 10;

    /*
    ** If defined as non-zero, auto-vacuum is enabled by default. Otherwise
    ** it must be turned on for each database using "PRAGMA auto_vacuum = 1".
    */
#if !SQLITE_DEFAULT_AUTOVACUUM
    const int SQLITE_DEFAULT_AUTOVACUUM = 0;
#endif

    const int BTREE_AUTOVACUUM_NONE = 0;        /* Do not do auto-vacuum */
    const int BTREE_AUTOVACUUM_FULL = 1;        /* Do full auto-vacuum */
    const int BTREE_AUTOVACUUM_INCR = 2;        /* Incremental vacuum */

    /*
    ** Forward declarations of structure
    */
    //typedef struct Btree Btree;
    //typedef struct BtCursor BtCursor;
    //typedef struct BtShared BtShared;
    //typedef struct BtreeMutexArray BtreeMutexArray;

    /*
    ** This structure records all of the Btrees that need to hold
    ** a mutex before we enter sqlite3VdbeExec().  The Btrees are
    ** are placed in aBtree[] in order of aBtree[].pBt.  That way,
    ** we can always lock and unlock them all quickly.
    */
    public class BtreeMutexArray
    {
      public int nMutex;
      public Btree[] aBtree = new Btree[SQLITE_MAX_ATTACHED + 1];
    };


    //int sqlite3BtreeOpen(
    //  string zFilename,       /* Name of database file to open */
    //  sqlite3 db,             /* Associated database connection */
    //  Btree **ppBtree,        /* Return open Btree* here */
    //  int flags,              /* Flags */
    //  int vfsFlags            /* Flags passed through to VFS open */
    //);

    /* The flags parameter to sqlite3BtreeOpen can be the bitwise or of the
    ** following values.
    **
    ** NOTE:  These values must match the corresponding PAGER_ values in
    ** pager.h.
    */
    const int BTREE_OMIT_JOURNAL = 1; /* Do not use journal.  No argument */
    const int BTREE_NO_READLOCK = 2; /* Omit readlocks on readonly files */
    const int BTREE_MEMORY = 4; /* In-memory DB.  No argument */
    const int BTREE_READONLY = 8; /* Open the database in read-only mode */
    const int BTREE_READWRITE = 16; /* Open for both reading and writing */
    const int BTREE_CREATE = 32; /* Create the database if it does not exist */

    //int sqlite3BtreeClose(Btree*);
    //int sqlite3BtreeSetCacheSize(Btree*,int);
    //int sqlite3BtreeSetSafetyLevel(Btree*,int,int);
    //int sqlite3BtreeSyncDisabled(Btree*);
    //int sqlite3BtreeSetPageSize(Btree *p, int nPagesize, int nReserve, int eFix);
    //int sqlite3BtreeGetPageSize(Btree*);
    //int sqlite3BtreeMaxPageCount(Btree*,int);
    //int sqlite3BtreeGetReserve(Btree*);
    //int sqlite3BtreeSetAutoVacuum(Btree , int);
    //int sqlite3BtreeGetAutoVacuum(Btree );
    //int sqlite3BtreeBeginTrans(Btree*,int);
    //int sqlite3BtreeCommitPhaseOne(Btree*, string zMaster);
    //int sqlite3BtreeCommitPhaseTwo(Btree*);
    //int sqlite3BtreeCommit(Btree*);
    //int sqlite3BtreeRollback(Btree*);
    //int sqlite3BtreeBeginStmt(Btree*);
    //int sqlite3BtreeCreateTable(Btree*, int*, int flags);
    //int sqlite3BtreeIsInTrans(Btree*);
    //int sqlite3BtreeIsInReadTrans(Btree*);
    //int sqlite3BtreeIsInBackup(Btree*);
    //void *sqlite3BtreeSchema(Btree , int, void(*)(void *));
    //int sqlite3BtreeSchemaLocked( Btree* pBtree );
    //int sqlite3BtreeLockTable( Btree* pBtree, int iTab, u8 isWriteLock );
    //int sqlite3BtreeSavepoint(Btree *, int, int);

    //const char *sqlite3BtreeGetFilename(Btree );
    //const char *sqlite3BtreeGetJournalname(Btree );
    //int sqlite3BtreeCopyFile(Btree *, Btree *);

    //int sqlite3BtreeIncrVacuum(Btree );

    /* The flags parameter to sqlite3BtreeCreateTable can be the bitwise OR
    ** of the following flags:
    */
    const int BTREE_INTKEY = 1;   /* Table has only 64-bit signed integer keys */
    const int BTREE_ZERODATA = 2;   /* Table has keys only - no data */
    const int BTREE_LEAFDATA = 4; /* Data stored in leaves only.  Implies INTKEY */

    //int sqlite3BtreeDropTable(Btree*, int, int*);
    //int sqlite3BtreeClearTable(Btree*, int, int*);
    //void sqlite3BtreeTripAllCursors(Btree*, int);

    //void sqlite3BtreeGetMeta(Btree *pBtree, int idx, u32 *pValue);
    //int sqlite3BtreeUpdateMeta(Btree*, int idx, u32 value);


    /*
    ** The second parameter to sqlite3BtreeGetMeta or sqlite3BtreeUpdateMeta
    ** should be one of the following values. The integer values are assigned
    ** to constants so that the offset of the corresponding field in an
    ** SQLite database header may be found using the following formula:
    **
    **   offset = 36 + (idx * 4)
    **
    ** For example, the free-page-count field is located at byte offset 36 of
    ** the database file header. The incr-vacuum-flag field is located at
    ** byte offset 64 (== 36+4*7).
    */
    //#define BTREE_FREE_PAGE_COUNT     0
    //#define BTREE_SCHEMA_VERSION      1
    //#define BTREE_FILE_FORMAT         2
    //#define BTREE_DEFAULT_CACHE_SIZE  3
    //#define BTREE_LARGEST_ROOT_PAGE   4
    //#define BTREE_TEXT_ENCODING       5
    //#define BTREE_USER_VERSION        6
    //#define BTREE_INCR_VACUUM         7
    const int BTREE_FREE_PAGE_COUNT = 0;
    const int BTREE_SCHEMA_VERSION = 1;
    const int BTREE_FILE_FORMAT = 2;
    const int BTREE_DEFAULT_CACHE_SIZE = 3;
    const int BTREE_LARGEST_ROOT_PAGE = 4;
    const int BTREE_TEXT_ENCODING = 5;
    const int BTREE_USER_VERSION = 6;
    const int BTREE_INCR_VACUUM = 7;

    //int sqlite3BtreeCursor(
    //  Btree*,                              /* BTree containing table to open */
    //  int iTable,                          /* Index of root page */
    //  int wrFlag,                          /* 1 for writing.  0 for read-only */
    //  struct KeyInfo*,                     /* First argument to compare function */
    //  BtCursor pCursor                    /* Space to write cursor structure */
    //);
    //int sqlite3BtreeCursorSize(void);

    //int sqlite3BtreeCloseCursor(BtCursor*);
    //int sqlite3BtreeMovetoUnpacked(
    //  BtCursor*,
    //  UnpackedRecord pUnKey,
    //  i64 intKey,
    //  int bias,
    //  int pRes
    //);
    //int sqlite3BtreeCursorHasMoved(BtCursor*, int*);
    //int sqlite3BtreeDelete(BtCursor*);
    //int sqlite3BtreeInsert(BtCursor*, const void pKey, i64 nKey,
    //                                  const void pData, int nData,
    //                                  int nZero, int bias, int seekResult);
    //int sqlite3BtreeFirst(BtCursor*, int pRes);
    //int sqlite3BtreeLast(BtCursor*, int pRes);
    //int sqlite3BtreeNext(BtCursor*, int pRes);
    //int sqlite3BtreeEof(BtCursor*);
    //int sqlite3BtreePrevious(BtCursor*, int pRes);
    //int sqlite3BtreeKeySize(BtCursor*, i64 pSize);
    //int sqlite3BtreeKey(BtCursor*, u32 offset, u32 amt, void*);
    //const void *sqlite3BtreeKeyFetch(BtCursor*, int pAmt);
    //const void *sqlite3BtreeDataFetch(BtCursor*, int pAmt);
    //int sqlite3BtreeDataSize(BtCursor*, u32 pSize);
    //int sqlite3BtreeData(BtCursor*, u32 offset, u32 amt, void*);
    //void sqlite3BtreeSetCachedRowid(BtCursor*, sqlite3_int64);
    //sqlite3_int64 sqlite3BtreeGetCachedRowid(BtCursor*);

    //char *sqlite3BtreeIntegrityCheck(Btree*, int *aRoot, int nRoot, int, int*);
    //struct Pager *sqlite3BtreePager(Btree*);

    //int sqlite3BtreePutData(BtCursor*, u32 offset, u32 amt, void*);
    //void sqlite3BtreeCacheOverflow(BtCursor );
    //void sqlite3BtreeClearCursor(BtCursor *);

    //#ifndef NDEBUG
    //int sqlite3BtreeCursorIsValid(BtCursor*);
    //#endif

    //#if !SQLITE_OMIT_BTREECOUNT
    //int sqlite3BtreeCount(BtCursor *, i64 *);
    //#endif

    //#if SQLITE_TEST
    //int sqlite3BtreeCursorInfo(BtCursor*, int*, int);
    //void sqlite3BtreeCursorList(Btree*);
    //#endif

#if !SQLITE_OMIT_SHARED_CACHE
//void sqlite3BtreeEnter(Btree*);
//void sqlite3BtreeEnterAll(sqlite3*);
#else
    //# define sqlite3BtreeEnter(X)
    static void sqlite3BtreeEnter( Btree bt ) { }
    //# define sqlite3BtreeEnterAll(X)
    static void sqlite3BtreeEnterAll( sqlite3 p ) { }
#endif

#if !(SQLITE_OMIT_SHARED_CACHE) && SQLITE_THREADSAFE
//void sqlite3BtreeLeave(Btree*);
//void sqlite3BtreeEnterCursor(BtCursor*);
//void sqlite3BtreeLeaveCursor(BtCursor*);
//void sqlite3BtreeLeaveAll(sqlite3*);
//void sqlite3BtreeMutexArrayEnter(BtreeMutexArray*);
//void sqlite3BtreeMutexArrayLeave(BtreeMutexArray*);
//void sqlite3BtreeMutexArrayInsert(BtreeMutexArray*, Btree*);
#if !NDEBUG
/* These routines are used inside assert() statements only. */
int sqlite3BtreeHoldsMutex(Btree*);
int sqlite3BtreeHoldsAllMutexes(sqlite3*);
#endif
#else

    //# define sqlite3BtreeLeave(X)
    static void sqlite3BtreeLeave( Btree X ) { }

    //# define sqlite3BtreeEnterCursor(X)
    static void sqlite3BtreeEnterCursor( BtCursor X ) { }

    //# define sqlite3BtreeLeaveCursor(X)
    static void sqlite3BtreeLeaveCursor( BtCursor X ) { }

    //# define sqlite3BtreeLeaveAll(X)
    static void sqlite3BtreeLeaveAll( sqlite3 X ) { }

    //# define sqlite3BtreeMutexArrayEnter(X)
    static void sqlite3BtreeMutexArrayEnter( BtreeMutexArray X ) { }

    //# define sqlite3BtreeMutexArrayLeave(X)
    static void sqlite3BtreeMutexArrayLeave( BtreeMutexArray X ) { }

    //# define sqlite3BtreeMutexArrayInsert(X,Y)
    static void sqlite3BtreeMutexArrayInsert( BtreeMutexArray X, Btree Y ) { }

    //# define sqlite3BtreeHoldsMutex(X) 1
    static bool sqlite3BtreeHoldsMutex( Btree X ) { return true; }

    //# define sqlite3BtreeHoldsAllMutexes(X) 1
    static bool sqlite3BtreeHoldsAllMutexes( sqlite3 X ) { return true; }
#endif


    //#endif // * _BTREE_H_ */

  }
}
