using System;
using System.Diagnostics;
using System.IO;

using i16 = System.Int16;
using u32 = System.UInt32;
using Pgno = System.UInt32;

namespace CS_SQLite3
{
  public partial class CSSQLite
  {
    /*
    ** 2008 August 05
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
    ** subsystem.
    **
    ** @(#) $Id: pcache.h,v 1.20 2009/07/25 11:46:49 danielk1977 Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */

#if !_PCACHE_H_

    //typedef struct PgHdr PgHdr;
    //typedef struct PCache PCache;

    /*
    ** Every page in the cache is controlled by an instance of the following
    ** structure.
    */
    public class PgHdr
    {
      public byte[] pData;          /* Content of this page */
      public MemPage pExtra;        /* Extra content */
      public PgHdr pDirty;          /* Transient list of dirty pages */
      public Pgno pgno;             /* The page number for this page */
      public Pager pPager;          /* The pager to which this page belongs */
#if SQLITE_CHECK_PAGES || (SQLITE_DEBUG)
      public int pageHash;          /* Hash of page content */
#endif
      public int flags;             /* PGHDR flags defined below */
      /**********************************************************************
      ** Elements above are public.  All that follows is private to pcache.c
      ** and should not be accessed by other modules.
      */
      public int nRef;              /* Number of users of this page */
      public PCache pCache;         /* Cache that owns this page */
      public bool CacheAllocated;   /* True, if allocated from cache */

      public PgHdr pDirtyNext;      /* Next element in list of dirty pages */
      public PgHdr pDirtyPrev;      /* Previous element in list of dirty pages */
      public PgHdr1 pPgHdr1;        /* Cache page header this this page */

      public static implicit operator bool( PgHdr b )
      {
        return ( b != null );
      }


      public void Clear()
      {
        this.pData = null;
        this.pExtra = null;
        this.pDirty = null;
        this.pgno = 0;
        this.pPager = null;
#if SQLITE_CHECK_PAGES
this.pageHash=0;
#endif
        this.flags = 0;
        this.nRef = 0;
        this.pCache = null;
        this.pDirtyNext = null;
        this.pDirtyPrev = null;
        this.pPgHdr1 = null;
      }
    };

    /* Bit values for PgHdr.flags */
    //#define PGHDR_DIRTY             0x002  /* Page has changed */
    //#define PGHDR_NEED_SYNC         0x004  /* Fsync the rollback journal before
    //                                       ** writing this page to the database */
    //#define PGHDR_NEED_READ         0x008  /* Content is unread */
    //#define PGHDR_REUSE_UNLIKELY    0x010  /* A hint that reuse is unlikely */
    //#define PGHDR_DONT_WRITE        0x020  /* Do not write content to disk */

    const int PGHDR_DIRTY = 0x002; /* Page has changed */
    const int PGHDR_NEED_SYNC = 0x004;/* Fsync the rollback journal before
** writing this page to the database */
    const int PGHDR_NEED_READ = 0x008;/* Content is unread */
    const int PGHDR_REUSE_UNLIKELY = 0x010;/* A hint that reuse is unlikely */
    const int PGHDR_DONT_WRITE = 0x020;/* Do not write content to disk */

    /* Initialize and shutdown the page cache subsystem */
    //int sqlite3PcacheInitialize(void);
    //void sqlite3PcacheShutdown(void);

    /* Page cache buffer management:
    ** These routines implement SQLITE_CONFIG_PAGECACHE.
    */
    //void sqlite3PCacheBufferSetup(void *, int sz, int n);

    /* Create a new pager cache.
    ** Under memory stress, invoke xStress to try to make pages clean.
    ** Only clean and unpinned pages can be reclaimed.
    */
    //void sqlite3PcacheOpen(
    //  int szPage,                    /* Size of every page */
    //  int szExtra,                   /* Extra space associated with each page */
    //  int bPurgeable,                /* True if pages are on backing store */
    //  int (*xStress)(void*, PgHdr*), /* Call to try to make pages clean */
    //  void pStress,                 /* Argument to xStress */
    //  PCache pToInit                /* Preallocated space for the PCache */
    //);

    /* Modify the page-size after the cache has been created. */
    //void sqlite3PcacheSetPageSize(PCache *, int);

    /* Return the size in bytes of a PCache object.  Used to preallocate
    ** storage space.
    */
    //int sqlite3PcacheSize(void);

    /* One release per successful fetch.  Page is pinned until released.
    ** Reference counted.
    */
    //int sqlite3PcacheFetch(PCache*, Pgno, int createFlag, PgHdr**);
    //void sqlite3PcacheRelease(PgHdr*);

    //void sqlite3PcacheDrop(PgHdr*);         /* Remove page from cache */
    //void sqlite3PcacheMakeDirty(PgHdr*);    /* Make sure page is marked dirty */
    //void sqlite3PcacheMakeClean(PgHdr*);    /* Mark a single page as clean */
    //void sqlite3PcacheCleanAll(PCache*);    /* Mark all dirty list pages as clean */

    /* Change a page number.  Used by incr-vacuum. */
    //void sqlite3PcacheMove(PgHdr*, Pgno);

    /* Remove all pages with pgno>x.  Reset the cache if x==0 */
    //void sqlite3PcacheTruncate(PCache*, Pgno x);

    /* Get a list of all dirty pages in the cache, sorted by page number */
    //PgHdr *sqlite3PcacheDirtyList(PCache*);

    /* Reset and close the cache object */
    //void sqlite3PcacheClose(PCache*);

    /* Clear flags from pages of the page cache */
    //void sqlite3PcacheClearSyncFlags(PCache *);

    /* Discard the contents of the cache */
    //void sqlite3PcacheClear(PCache*);

    /* Return the total number of outstanding page references */
    //int sqlite3PcacheRefCount(PCache*);

    /* Increment the reference count of an existing page */
    //void sqlite3PcacheRef(PgHdr*);

    //int sqlite3PcachePageRefcount(PgHdr*);


    /* Return the total number of pages stored in the cache */
    //int sqlite3PcachePagecount(PCache*);

#if SQLITE_CHECK_PAGES
/* Iterate through all dirty pages currently stored in the cache. This
** interface is only available if SQLITE_CHECK_PAGES is defined when the
** library is built.
*/

//void sqlite3PcacheIterateDirty(PCache pCache, void (*xIter)(PgHdr *));
#endif

    /* Set and get the suggested cache-size for the specified pager-cache.
**
** If no global maximum is configured, then the system attempts to limit
** the total number of pages cached by purgeable pager-caches to the sum
** of the suggested cache-sizes.
*/
    //void sqlite3PcacheSetCachesize(PCache *, int);
#if SQLITE_TEST
    //int sqlite3PcacheGetCachesize(PCache *);
#endif

#if SQLITE_ENABLE_MEMORY_MANAGEMENT
/* Try to return memory used by the pcache module to the main memory heap */
//int sqlite3PcacheReleaseMemory(int);
#endif

#if SQLITE_TEST
    //void sqlite3PcacheStats(int*,int*,int*,int*);
#endif

    //void sqlite3PCacheSetDefault(void);

#endif //* _PCACHE_H_ */
  }
}
