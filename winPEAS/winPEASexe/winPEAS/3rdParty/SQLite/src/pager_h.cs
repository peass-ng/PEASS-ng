using Pgno = System.UInt32;

namespace CS_SQLite3
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
    ** This header file defines the interface that the sqlite page cache
    ** subsystem.  The page cache subsystem reads and writes a file a page
    ** at a time and provides a journal for rollback.
    **
    ** @(#) $Id: pager.h,v 1.104 2009/07/24 19:01:19 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */

    //#if !_PAGER_H_
    //#define _PAGER_H_

    /*
    ** Default maximum size for persistent journal files. A negative
    ** value means no limit. This value may be overridden using the
    ** sqlite3PagerJournalSizeLimit() API. See also "PRAGMA journal_size_limit".
    */
#if !SQLITE_DEFAULT_JOURNAL_SIZE_LIMIT
    const int SQLITE_DEFAULT_JOURNAL_SIZE_LIMIT = -1;//#define SQLITE_DEFAULT_JOURNAL_SIZE_LIMIT -1
#endif

    /*
** The type used to represent a page number.  The first page in a file
** is called page 1.  0 is used to represent "not a page".
*/
    //typedef u32 Pgno;

    /*
    ** Each open file is managed by a separate instance of the "Pager" structure.
    */
    //typedef struct Pager Pager;

    /*
    ** Handle type for pages.
    */
    //typedef struct PgHdr DbPage;

    /*
    ** Page number PAGER_MJ_PGNO is never used in an SQLite database (it is
    ** reserved for working around a windows/posix incompatibility). It is
    ** used in the journal to signify that the remainder of the journal file
    ** is devoted to storing a master journal name - there are no more pages to
    ** roll back. See comments for function writeMasterJournal() in pager.c
    ** for details.
    */
    //#define PAGER_MJ_PGNO(x) ((Pgno)((PENDING_BYTE/((x)->pageSize))+1))
    static Pgno PAGER_MJ_PGNO( Pager x ) { return ( (Pgno)( ( PENDING_BYTE / ( ( x ).pageSize ) ) + 1 ) ); }
    /*
    ** Allowed values for the flags parameter to sqlite3PagerOpen().
    **
    ** NOTE: These values must match the corresponding BTREE_ values in btree.h.
    */
    //#define PAGER_OMIT_JOURNAL  0x0001    /* Do not use a rollback journal */
    //#define PAGER_NO_READLOCK   0x0002    /* Omit readlocks on readonly files */
    const int PAGER_OMIT_JOURNAL = 0x0001;   /* Do not use a rollback journal */
    const int PAGER_NO_READLOCK = 0x0002;  /* Omit readlocks on readonly files */

    /*
    ** Valid values for the second argument to sqlite3PagerLockingMode().
    */
    //#define PAGER_LOCKINGMODE_QUERY      -1
    //#define PAGER_LOCKINGMODE_NORMAL      0
    //#define PAGER_LOCKINGMODE_EXCLUSIVE   1
    static int PAGER_LOCKINGMODE_QUERY = -1;
    static int PAGER_LOCKINGMODE_NORMAL = 0;
    static int PAGER_LOCKINGMODE_EXCLUSIVE = 1;

    /*
    ** Valid values for the second argument to sqlite3PagerJournalMode().
    */
    //#define PAGER_JOURNALMODE_QUERY      -1
    //#define PAGER_JOURNALMODE_DELETE      0   /* Commit by deleting journal file */
    //#define PAGER_JOURNALMODE_PERSIST     1   /* Commit by zeroing journal header */
    //#define PAGER_JOURNALMODE_OFF         2   /* Journal omitted.  */
    //#define PAGER_JOURNALMODE_TRUNCATE    3   /* Commit by truncating journal */
    //#define PAGER_JOURNALMODE_MEMORY      4   /* In-memory journal file */
    const int PAGER_JOURNALMODE_QUERY = -1;
    const int PAGER_JOURNALMODE_DELETE = 0;  /* Commit by deleting journal file */
    const int PAGER_JOURNALMODE_PERSIST = 1; /* Commit by zeroing journal header */
    const int PAGER_JOURNALMODE_OFF = 2;     /* Journal omitted.  */
    const int PAGER_JOURNALMODE_TRUNCATE = 3;/* Commit by truncating journal */
    const int PAGER_JOURNALMODE_MEMORY = 4;/* In-memory journal file */

    /*
    ** The remainder of this file contains the declarations of the functions
    ** that make up the Pager sub-system API. See source code comments for
    ** a detailed description of each routine.
    */
    /* Open and close a Pager connection. */
    //int sqlite3PagerOpen(
    //  sqlite3_vfs*,
    //  Pager **ppPager,
    //  const char*,
    //  int,
    //  int,
    //  int,
    ////  void(*)(DbPage*)
    //);
    //int sqlite3PagerClose(Pager *pPager);
    //int sqlite3PagerReadFileheader(Pager*, int, unsigned char*);

    /* Functions used to configure a Pager object. */
    //void sqlite3PagerSetBusyhandler(Pager*, int(*)(void *), void *);
    //int sqlite3PagerSetPagesize(Pager*, u16*, int);
    //int sqlite3PagerMaxPageCount(Pager*, int);
    //void sqlite3PagerSetCachesize(Pager*, int);
    //void sqlite3PagerSetSafetyLevel(Pager*,int,int);
    //int sqlite3PagerLockingMode(Pager *, int);
    //int sqlite3PagerJournalMode(Pager *, int);
    //i64 sqlite3PagerJournalSizeLimit(Pager *, i64);
    //sqlite3_backup **sqlite3PagerBackupPtr(Pager*);

    /* Functions used to obtain and release page references. */
    //int sqlite3PagerAcquire(Pager *pPager, Pgno pgno, DbPage **ppPage, int clrFlag);
    //#define sqlite3PagerGet(A,B,C) sqlite3PagerAcquire(A,B,C,0)
    //DbPage *sqlite3PagerLookup(Pager *pPager, Pgno pgno);
    //void sqlite3PagerRef(DbPage*);
    //void sqlite3PagerUnref(DbPage*);

    /* Operations on page references. */
    //int sqlite3PagerWrite(DbPage*);
    //void sqlite3PagerDontWrite(DbPage*);
    //int sqlite3PagerMovepage(Pager*,DbPage*,Pgno,int);
    //int sqlite3PagerPageRefcount(DbPage*);
    //void *sqlite3PagerGetData(DbPage *);
    //void *sqlite3PagerGetExtra(DbPage *);

    /* Functions used to manage pager transactions and savepoints. */
    //int sqlite3PagerPagecount(Pager*, int*);
    //int sqlite3PagerBegin(Pager*, int exFlag, int);
    //int sqlite3PagerCommitPhaseOne(Pager*,const char *zMaster, int);
    //int sqlite3PagerSync(Pager *pPager);
    //int sqlite3PagerCommitPhaseTwo(Pager*);
    //int sqlite3PagerRollback(Pager*);
    //int sqlite3PagerOpenSavepoint(Pager *pPager, int n);
    //int sqlite3PagerSavepoint(Pager *pPager, int op, int iSavepoint);
    //int sqlite3PagerSharedLock(Pager *pPager);

    /* Functions used to query pager state and configuration. */
    //u8 sqlite3PagerIsreadonly(Pager*);
    //int sqlite3PagerRefcount(Pager*);
    //const char *sqlite3PagerFilename(Pager*);
    //const sqlite3_vfs *sqlite3PagerVfs(Pager*);
    //sqlite3_file *sqlite3PagerFile(Pager*);
    //const char *sqlite3PagerJournalname(Pager*);
    //int sqlite3PagerNosync(Pager*);
    //void *sqlite3PagerTempSpace(Pager*);
    //int sqlite3PagerIsMemdb(Pager*);

    /* Functions used to truncate the database file. */
    //void sqlite3PagerTruncateImage(Pager*,Pgno);

    /* Functions to support testing and debugging. */
    //#if !NDEBUG || SQLITE_TEST
    //  Pgno sqlite3PagerPagenumber(DbPage*);
    //  int sqlite3PagerIswriteable(DbPage*);
    //#endif
    //#if SQLITE_TEST
    //  int *sqlite3PagerStats(Pager*);
    //  void sqlite3PagerRefdump(Pager*);
    //  void disable_simulated_io_errors(void);
    //  void enable_simulated_io_errors(void);
    //#else
    //# define disable_simulated_io_errors()
    //# define enable_simulated_io_errors()
    //#endif
  }
}
