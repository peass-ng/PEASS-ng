using System.Diagnostics;
using u32 = System.UInt32;

namespace winPEAS._3rdParty.SQLite.src
{
    using sqlite3_pcache = CSSQLite.PCache1;
  public partial class CSSQLite
  {
    /*
    ** 2008 November 05
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
    ** This file implements the default page cache implementation (the
    ** sqlite3_pcache interface). It also contains part of the implementation
    ** of the SQLITE_CONFIG_PAGECACHE and sqlite3_release_memory() features.
    ** If the default page cache implementation is overriden, then neither of
    ** these two features are available.
    **
    ** @(#) $Id: pcache1.c,v 1.19 2009/07/17 11:44:07 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */

    //#include "sqliteInt.h"

    //typedef struct PCache1 PCache1;
    //typedef struct PgHdr1 PgHdr1;
    //typedef struct PgFreeslot PgFreeslot;

    /* Pointers to structures of this type are cast and returned as
    ** opaque sqlite3_pcache* handles
    */

    public class PCache1
    {
      /* Cache configuration parameters. Page size (szPage) and the purgeable
      ** flag (bPurgeable) are set when the cache is created. nMax may be
      ** modified at any time by a call to the pcache1CacheSize() method.
      ** The global mutex must be held when accessing nMax.
      */
      public int szPage;                /* Size of every page in this cache */
      public bool bPurgeable;           /* True if pages are on backing store */
      public u32 nMin;                  /* Minimum number of pages reserved */
      public u32 nMax;                  /* Configured "cache_size" value */

      /* Hash table of all pages. The following variables may only be accessed
      ** when the accessor is holding the global mutex (see pcache1EnterMutex()
      ** and pcache1LeaveMutex()).
      */
      public u32 nRecyclable;           /* Number of pages in the LRU list */
      public u32 nPage;                 /* Total number of pages in apHash */
      public u32 nHash;                 /* Number of slots in apHash[] */
      public PgHdr1[] apHash;           /* Hash table for fast lookup by pgno */
      public u32 iMaxKey;               /* Largest key seen since xTruncate() */


      public void Clear()
      {
        nRecyclable = 0;
        nPage = 0;
        nHash = 0;
        apHash = null;
        iMaxKey = 0;
      }
    };

    /*
    ** Each cache entry is represented by an instance of the following
    ** structure. A buffer of PgHdr1.pCache.szPage bytes is allocated
    ** directly before this structure in memory (see the PGHDR1_TO_PAGE()
    ** macro below).
    */
    public class PgHdr1
    {
      public u32 iKey;                     /* Key value (page number) */
      public PgHdr1 pNext;                 /* Next in hash table chain */
      public PCache1 pCache;               /* Cache that currently owns this page */
      public PgHdr1 pLruNext;              /* Next in LRU list of unpinned pages */
      public PgHdr1 pLruPrev;              /* Previous in LRU list of unpinned pages */
      public PgHdr pPgHdr = new PgHdr();   /* Pointer to Actual Page Header */

      public void Clear()
      {
        this.iKey = 0;
        this.pNext = null;
        this.pCache = null;
        this.pPgHdr.Clear();
      }
    };

    /*
    ** Free slots in the allocator used to divide up the buffer provided using
    ** the SQLITE_CONFIG_PAGECACHE mechanism.
    */
    //typedef struct PgFreeslot PgFreeslot;
    public class PgFreeslot
    {
      public PgFreeslot pNext;  /* Next free slot */
      public PgHdr _PgHdr;      /* Next Free Header */
    };

    /*
    ** Global data for the page cache.
    */
    public class PCacheGlobal
    {
      public sqlite3_mutex mutex;               /* static mutex MUTEX_STATIC_LRU */

      public int nMaxPage;                     /* Sum of nMaxPage for purgeable caches */
      public int nMinPage;                     /* Sum of nMinPage for purgeable caches */
      public int nCurrentPage;                  /* Number of purgeable pages allocated */
      public PgHdr1 pLruHead, pLruTail;          /* LRU list of unused clean pgs */

      /* Variables related to SQLITE_CONFIG_PAGECACHE settings. */
      public int szSlot;                         /* Size of each free slot */
      public object pStart, pEnd;                /* Bounds of pagecache malloc range */
      public PgFreeslot pFree;                   /* Free page blocks */
      public int isInit;                         /* True if initialized */
    }
    static PCacheGlobal pcache = new PCacheGlobal();

    /*
    ** All code in this file should access the global structure above via the
    ** alias "pcache1". This ensures that the WSD emulation is used when
    ** compiling for systems that do not support real WSD.
    */

    //#define pcache1 (GLOBAL(struct PCacheGlobal, pcache1_g))
    static PCacheGlobal pcache1 = pcache;

    /*
    ** When a PgHdr1 structure is allocated, the associated PCache1.szPage
    ** bytes of data are located directly before it in memory (i.e. the total
    ** size of the allocation is sizeof(PgHdr1)+PCache1.szPage byte). The
    ** PGHDR1_TO_PAGE() macro takes a pointer to a PgHdr1 structure as
    ** an argument and returns a pointer to the associated block of szPage
    ** bytes. The PAGE_TO_PGHDR1() macro does the opposite: its argument is
    ** a pointer to a block of szPage bytes of data and the return value is
    ** a pointer to the associated PgHdr1 structure.
    **
    **   assert( PGHDR1_TO_PAGE(PAGE_TO_PGHDR1(pCache, X))==X );
    */
    //#define PGHDR1_TO_PAGE(p)    (void*)(((char*)p) - p->pCache->szPage)
    static PgHdr PGHDR1_TO_PAGE( PgHdr1 p ) { return p.pPgHdr; }

    //#define PAGE_TO_PGHDR1(c, p) (PgHdr1*)(((char*)p) + c->szPage)
    static PgHdr1 PAGE_TO_PGHDR1( PCache1 c, PgHdr p ) { return p.pPgHdr1; }
    /*
    ** Macros to enter and leave the global LRU mutex.
    */
    //#define pcache1EnterMutex() sqlite3_mutex_enter(pcache1.mutex)
    //#define pcache1LeaveMutex() sqlite3_mutex_leave(pcache1.mutex)
    static void pcache1EnterMutex() { sqlite3_mutex_enter( pcache1.mutex ); }
    static void pcache1LeaveMutex() { sqlite3_mutex_leave( pcache1.mutex ); }

    /******************************************************************************/
    /******** Page Allocation/SQLITE_CONFIG_PCACHE Related Functions **************/

    /*
    ** This function is called during initialization if a static buffer is
    ** supplied to use for the page-cache by passing the SQLITE_CONFIG_PAGECACHE
    ** verb to sqlite3_config(). Parameter pBuf points to an allocation large
    ** enough to contain 'n' buffers of 'sz' bytes each.
    */
    static void sqlite3PCacheBufferSetup( object pBuf, int sz, int n )
    {
      if ( pcache1.isInit != 0 )
      {
        PgFreeslot p;
        sz = ROUNDDOWN8( sz );
        pcache1.szSlot = sz;
        pcache1.pStart = pBuf;
        pcache1.pFree = null;
        while ( n-- != 0 )
        {
          p = new PgFreeslot();// (PgFreeslot)pBuf;
          p._PgHdr = new PgHdr();
          p.pNext = pcache1.pFree;
          pcache1.pFree = p;
          //pBuf = (void*)&((char*)pBuf)[sz];
        }
        pcache1.pEnd = pBuf;
      }
    }

    /*
    ** Malloc function used within this file to allocate space from the buffer
    ** configured using sqlite3_config(SQLITE_CONFIG_PAGECACHE) option. If no
    ** such buffer exists or there is no space left in it, this function falls
    ** back to sqlite3Malloc().
    */
    static PgHdr pcache1Alloc( int nByte )
    {
      PgHdr p;
      Debug.Assert( sqlite3_mutex_held( pcache1.mutex ) );
      if ( nByte <= pcache1.szSlot && pcache1.pFree != null )
      {
        Debug.Assert( pcache1.isInit != 0 );
        p = pcache1.pFree._PgHdr;
        p.CacheAllocated = true;
        pcache1.pFree = pcache1.pFree.pNext;
        sqlite3StatusSet( SQLITE_STATUS_PAGECACHE_SIZE, nByte );
        sqlite3StatusAdd( SQLITE_STATUS_PAGECACHE_USED, 1 );
      }
      else
      {

        /* Allocate a new buffer using sqlite3Malloc. Before doing so, exit the
        ** global pcache mutex and unlock the pager-cache object pCache. This is
        ** so that if the attempt to allocate a new buffer causes the the
        ** configured soft-heap-limit to be breached, it will be possible to
        ** reclaim memory from this pager-cache.
        */
        pcache1LeaveMutex();
        p = new PgHdr();//  p = sqlite3Malloc(nByte);
        p.CacheAllocated = false;
        pcache1EnterMutex();
        //  if( p !=null){
        int sz = nByte;//int sz = sqlite3MallocSize(p);
        sqlite3StatusAdd( SQLITE_STATUS_PAGECACHE_OVERFLOW, sz );
      }
      return p;
    }

    /*
    ** Free an allocated buffer obtained from pcache1Alloc().
    */
    static void pcache1Free( ref PgHdr p )
    {
      Debug.Assert( sqlite3_mutex_held( pcache1.mutex ) );
      if ( p == null ) return;
      if (p.CacheAllocated) //if ( p >= pcache1.pStart && p < pcache1.pEnd )
      {
        PgFreeslot pSlot = new PgFreeslot();
        sqlite3StatusAdd( SQLITE_STATUS_PAGECACHE_USED, -1 );
        pSlot._PgHdr = p;// (PgFreeslot)p;
        pSlot.pNext = pcache1.pFree;
        pcache1.pFree = pSlot;
      }
      else
      {
        int iSize = p.pData.Length;//sqlite3MallocSize( p );
        sqlite3StatusAdd( SQLITE_STATUS_PAGECACHE_OVERFLOW, -iSize );
        p = null;//sqlite3_free( ref p );
      }
    }

    /*
    ** Allocate a new page object initially associated with cache pCache.
    */
    static PgHdr1 pcache1AllocPage( PCache1 pCache )
    {
      //int nByte = sizeof(PgHdr1) + pCache.szPage;
      PgHdr pPg = pcache1Alloc( pCache.szPage );
      PgHdr1 p;
      //if ( pPg != null )
      {
        // PAGE_TO_PGHDR1( pCache, pPg );
        p = new PgHdr1();
        p.pCache = pCache;
        p.pPgHdr = pPg;
        if ( pCache.bPurgeable )
        {
          pcache1.nCurrentPage++;
        }
      }
      //else
      //{
      //  p = null;
      //}
      return p;
    }

    /*
    ** Free a page object allocated by pcache1AllocPage().
    **
    ** The pointer is allowed to be NULL, which is prudent.  But it turns out
    ** that the current implementation happens to never call this routine
    ** with a NULL pointer, so we mark the NULL test with ALWAYS().
    */
    static void pcache1FreePage( ref PgHdr1 p )
    {
      if ( ALWAYS( p != null ) )
      {
        if ( p.pCache.bPurgeable )
        {
          pcache1.nCurrentPage--;
        }
        pcache1Free( ref p.pPgHdr );//PGHDR1_TO_PAGE( p );
      }
    }

    /*
    ** Malloc function used by SQLite to obtain space from the buffer configured
    ** using sqlite3_config(SQLITE_CONFIG_PAGECACHE) option. If no such buffer
    ** exists, this function falls back to sqlite3Malloc().
    */
    static PgHdr sqlite3PageMalloc( int sz )
    {
      PgHdr p;
      pcache1EnterMutex();
      p = pcache1Alloc( sz );
      pcache1LeaveMutex();
      return p;
    }

    /*
    ** Free an allocated buffer obtained from sqlite3PageMalloc().
    */
    static void sqlite3PageFree( ref PgHdr p)
    {
      pcache1EnterMutex();
      pcache1Free( ref p );
      pcache1LeaveMutex();
    }

    /******************************************************************************/
    /******** General Implementation Functions ************************************/

    /*
    ** This function is used to resize the hash table used by the cache passed
    ** as the first argument.
    **
    ** The global mutex must be held when this function is called.
    */
    static int pcache1ResizeHash( PCache1 p )
    {
      PgHdr1[] apNew;
      u32 nNew;
      u32 i;

      Debug.Assert( sqlite3_mutex_held( pcache1.mutex ) );

      nNew = p.nHash * 2;
      if ( nNew < 256 )
      {
        nNew = 256;
      }

      pcache1LeaveMutex();
      if ( p.nHash != 0 ) { sqlite3BeginBenignMalloc(); }
      apNew = new PgHdr1[nNew];// (PgHdr1**)sqlite3_malloc( sizeof( PgHdr1* ) * nNew );
      if ( p.nHash != 0 ) { sqlite3EndBenignMalloc(); }
      pcache1EnterMutex();
      if ( apNew != null )
      {
        //memset(apNew, 0, sizeof(PgHdr1 *)*nNew);
        for ( i = 0 ; i < p.nHash ; i++ )
        {
          PgHdr1 pPage;
          PgHdr1 pNext = p.apHash[i];
          while ( ( pPage = pNext ) != null )
          {
            u32 h = (u32)( pPage.iKey % nNew );
            pNext = pPage.pNext;
            pPage.pNext = apNew[h];
            apNew[h] = pPage;
          }
        }
        //sqlite3_free( ref p.apHash );
        p.apHash = apNew;
        p.nHash = nNew;
      }

      return ( p.apHash != null ? SQLITE_OK : SQLITE_NOMEM );
    }

    /*
    ** This function is used internally to remove the page pPage from the
    ** global LRU list, if is part of it. If pPage is not part of the global
    ** LRU list, then this function is a no-op.
    **
    ** The global mutex must be held when this function is called.
    */
    static void pcache1PinPage( PgHdr1 pPage )
    {
      Debug.Assert( sqlite3_mutex_held( pcache1.mutex ) );
      if ( pPage != null && ( pPage.pLruNext != null || pPage == pcache1.pLruTail ) )
      {
        if ( pPage.pLruPrev != null )
        {
          pPage.pLruPrev.pLruNext = pPage.pLruNext;
        }
        if ( pPage.pLruNext != null )
        {
          pPage.pLruNext.pLruPrev = pPage.pLruPrev;
        }
        if ( pcache1.pLruHead == pPage )
        {
          pcache1.pLruHead = pPage.pLruNext;
        }
        if ( pcache1.pLruTail == pPage )
        {
          pcache1.pLruTail = pPage.pLruPrev;
        }
        pPage.pLruNext = null;
        pPage.pLruPrev = null;
        pPage.pCache.nRecyclable--;
      }
    }


    /*
    ** Remove the page supplied as an argument from the hash table
    ** (PCache1.apHash structure) that it is currently stored in.
    **
    ** The global mutex must be held when this function is called.
    */
    static void pcache1RemoveFromHash( PgHdr1 pPage )
    {
      u32 h;
      PCache1 pCache = pPage.pCache;
      PgHdr1 pp, pPrev;

      h = pPage.iKey % pCache.nHash;
      pPrev = null;
      for ( pp = pCache.apHash[h] ; pp != pPage ; pPrev = pp, pp = pp.pNext ) ;
      if ( pPrev == null ) pCache.apHash[h] = pp.pNext; else pPrev.pNext = pp.pNext; // pCache.apHash[h] = pp.pNext;

      pCache.nPage--;
    }

    /*
    ** If there are currently more than pcache.nMaxPage pages allocated, try
    ** to recycle pages to reduce the number allocated to pcache.nMaxPage.
    */
    static void pcache1EnforceMaxPage()
    {
      Debug.Assert( sqlite3_mutex_held( pcache1.mutex ) );
      while ( pcache1.nCurrentPage > pcache1.nMaxPage && pcache1.pLruTail != null )
      {
        PgHdr1 p = pcache1.pLruTail;
        pcache1PinPage( p );
        pcache1RemoveFromHash( p );
        pcache1FreePage( ref p );
      }
    }

    /*
    ** Discard all pages from cache pCache with a page number (key value)
    ** greater than or equal to iLimit. Any pinned pages that meet this
    ** criteria are unpinned before they are discarded.
    **
    ** The global mutex must be held when this function is called.
    */
    static void pcache1TruncateUnsafe(
    PCache1 pCache,
    u32 iLimit
    )
    {
      //TESTONLY( unsigned int nPage = 0; )      /* Used to assert pCache->nPage is correct */
#if !NDEBUG || SQLITE_COVERAGE_TEST
      u32 nPage = 0;
#endif
      u32 h;
      Debug.Assert( sqlite3_mutex_held( pcache1.mutex ) );
      for ( h = 0 ; h < pCache.nHash ; h++ )
      {
        PgHdr1 pp = pCache.apHash[h];
        PgHdr1 pPage;
        while ( ( pPage = pp ) != null )
        {
          if ( pPage.iKey >= iLimit )
          {
            pCache.nPage--;
            pp = pPage.pNext;
            pcache1PinPage( pPage );
            if ( pCache.apHash[h] == pPage )
              pCache.apHash[h] = pPage.pNext;
            else Debugger.Break();
            pcache1FreePage( ref  pPage );
          }
          else
          {
            pp = pPage.pNext;
            //TESTONLY( nPage++; )
#if !NDEBUG || SQLITE_COVERAGE_TEST
            nPage++;
#endif
          }
        }
      }
#if !NDEBUG || SQLITE_COVERAGE_TEST
      Debug.Assert( pCache.nPage == nPage );
#endif
    }

    /******************************************************************************/
    /******** sqlite3_pcache Methods **********************************************/

    /*
    ** Implementation of the sqlite3_pcache.xInit method.
    */
    static int pcache1Init( object NotUsed )
    {
      UNUSED_PARAMETER( NotUsed );
      Debug.Assert( pcache1.isInit == 0 );
      pcache1 = new PCacheGlobal();// memset( &pcache1, 0, sizeof( pcache1 ) );
      if ( sqlite3GlobalConfig.bCoreMutex )
      {
        pcache1.mutex = sqlite3_mutex_alloc( SQLITE_MUTEX_STATIC_LRU );
      }
      pcache1.isInit = 1;
      return SQLITE_OK;
    }

    /*
    ** Implementation of the sqlite3_pcache.xShutdown method.
    */
    static void pcache1Shutdown( object NotUsed )
    {
      UNUSED_PARAMETER( NotUsed );
      Debug.Assert( pcache1.isInit != 0 );
      pcache1 = new PCacheGlobal(); //memset( &pcache1, 0, sizeof( pcache1 ) );
    }

    /*
    ** Implementation of the sqlite3_pcache.xCreate method.
    **
    ** Allocate a new cache.
    */
    static sqlite3_pcache pcache1Create( int szPage, int bPurgeable )
    {
      PCache1 pCache;

      pCache = new PCache1();// (PCache1*)sqlite3_malloc( sizeof( PCache1 ) );
      if ( pCache != null )
      {
        //memset(pCache, 0, sizeof(PCache1));
        pCache.szPage = szPage;
        pCache.bPurgeable = ( bPurgeable != 0 );
        if ( bPurgeable != 0 )
        {
          pCache.nMin = 10;
          pcache1EnterMutex();
          pcache1.nMinPage += (int)pCache.nMin;
          pcache1LeaveMutex();
        }
      }
      return pCache;
    }

    /*
    ** Implementation of the sqlite3_pcache.xCachesize method.
    **
    ** Configure the cache_size limit for a cache.
    */
    static void pcache1Cachesize( sqlite3_pcache p, int nMax )
    {
      PCache1 pCache = (PCache1)p;
      if ( pCache.bPurgeable )
      {
        pcache1EnterMutex();
        pcache1.nMaxPage += (int)( nMax - pCache.nMax );
        pCache.nMax = (u32)nMax;
        pcache1EnforceMaxPage();
        pcache1LeaveMutex();
      }
    }

    /*
    ** Implementation of the sqlite3_pcache.xPagecount method.
    */
    static int pcache1Pagecount( sqlite3_pcache p )
    {
      int n;
      pcache1EnterMutex();
      n = (int)( (PCache1)p ).nPage;
      pcache1LeaveMutex();
      return n;
    }

    /*
    ** Implementation of the sqlite3_pcache.xFetch method. 
    **
    ** Fetch a page by key value.
    **
    ** Whether or not a new page may be allocated by this function depends on
    ** the value of the createFlag argument.  0 means do not allocate a new
    ** page.  1 means allocate a new page if space is easily available.  2 
    ** means to try really hard to allocate a new page.
    **
    ** For a non-purgeable cache (a cache used as the storage for an in-memory
    ** database) there is really no difference between createFlag 1 and 2.  So
    ** the calling function (pcache.c) will never have a createFlag of 1 on
    ** a non-purgable cache.
    **
    ** There are three different approaches to obtaining space for a page,
    ** depending on the value of parameter createFlag (which may be 0, 1 or 2).
    **
    **   1. Regardless of the value of createFlag, the cache is searched for a 
    **      copy of the requested page. If one is found, it is returned.
    **
    **   2. If createFlag==0 and the page is not already in the cache, NULL is
    **      returned.
    **
    **   3. If createFlag is 1, and the page is not already in the cache,
    **      and if either of the following are true, return NULL:
    **
    **       (a) the number of pages pinned by the cache is greater than
    **           PCache1.nMax, or
    **       (b) the number of pages pinned by the cache is greater than
    **           the sum of nMax for all purgeable caches, less the sum of 
    **           nMin for all other purgeable caches. 
    **
    **   4. If none of the first three conditions apply and the cache is marked
    **      as purgeable, and if one of the following is true:
    **
    **       (a) The number of pages allocated for the cache is already 
    **           PCache1.nMax, or
    **
    **       (b) The number of pages allocated for all purgeable caches is
    **           already equal to or greater than the sum of nMax for all
    **           purgeable caches,
    **
    **      then attempt to recycle a page from the LRU list. If it is the right
    **      size, return the recycled buffer. Otherwise, free the buffer and
    **      proceed to step 5. 
    **
    **   5. Otherwise, allocate and return a new page buffer.
    */
    static PgHdr pcache1Fetch( sqlite3_pcache p, u32 iKey, int createFlag )
    {
      u32 nPinned;
      PCache1 pCache = p;
      PgHdr1 pPage = null;

      Debug.Assert( pCache.bPurgeable || createFlag != 1 );
      pcache1EnterMutex();
      if ( createFlag == 1 ) sqlite3BeginBenignMalloc();

      /* Search the hash table for an existing entry. */
      if ( pCache.nHash > 0 )
      {
        u32 h = iKey % pCache.nHash;
        for ( pPage = pCache.apHash[h] ; pPage != null && pPage.iKey != iKey ; pPage = pPage.pNext ) ;
      }

      if ( pPage != null || createFlag == 0 )
      {
        pcache1PinPage( pPage );
        goto fetch_out;
      }

      /* Step 3 of header comment. */
      nPinned = pCache.nPage - pCache.nRecyclable;
      if ( createFlag == 1 && (
      nPinned >= ( pcache1.nMaxPage + pCache.nMin - pcache1.nMinPage )
      || nPinned >= ( pCache.nMax * 9 / 10 )
      ) )
      {
        goto fetch_out;
      }

      if ( pCache.nPage >= pCache.nHash && pcache1ResizeHash( pCache ) != 0 )
      {
        goto fetch_out;
      }

      /* Step 4. Try to recycle a page buffer if appropriate. */
      if ( pCache.bPurgeable && pcache1.pLruTail != null && (
      pCache.nPage + 1 >= pCache.nMax || pcache1.nCurrentPage >= pcache1.nMaxPage
      ) )
      {
        pPage = pcache1.pLruTail;
        pcache1RemoveFromHash( pPage );
        pcache1PinPage( pPage );
        if ( pPage.pCache.szPage != pCache.szPage )
        {
          pcache1FreePage( ref pPage );
          pPage = null;
        }
        else
        {
          pcache1.nCurrentPage -= ( ( pPage.pCache.bPurgeable ? 1 : 0 ) - ( pCache.bPurgeable ? 1 : 0 ) );
        }
      }

      /* Step 5. If a usable page buffer has still not been found,
      ** attempt to allocate a new one.
      */
      if ( null == pPage )
      {
        pPage = pcache1AllocPage( pCache );
      }

      if ( pPage != null )
      {
        u32 h = iKey % pCache.nHash;
        pCache.nPage++;
        pPage.iKey = iKey;
        pPage.pNext = pCache.apHash[h];
        pPage.pCache = pCache;
        pPage.pLruPrev = null;
        pPage.pLruNext = null;
        PGHDR1_TO_PAGE( pPage ).Clear();// *(void **)(PGHDR1_TO_PAGE(pPage)) = 0;
        pPage.pPgHdr.pPgHdr1 = pPage;
        pCache.apHash[h] = pPage;
      }

fetch_out:
      if ( pPage != null && iKey > pCache.iMaxKey )
      {
        pCache.iMaxKey = iKey;
      }
      if ( createFlag == 1 ) sqlite3EndBenignMalloc();
      pcache1LeaveMutex();
      return ( pPage != null ? PGHDR1_TO_PAGE( pPage ) : null );
    }


    /*
    ** Implementation of the sqlite3_pcache.xUnpin method.
    **
    ** Mark a page as unpinned (eligible for asynchronous recycling).
    */
    static void pcache1Unpin( sqlite3_pcache p, PgHdr pPg, int reuseUnlikely )
    {
      PCache1 pCache = (PCache1)p;
      PgHdr1 pPage = PAGE_TO_PGHDR1( pCache, pPg );

      Debug.Assert( pPage.pCache == pCache );
      pcache1EnterMutex();

      /* It is an error to call this function if the page is already
      ** part of the global LRU list.
      */
      Debug.Assert( pPage.pLruPrev == null && pPage.pLruNext == null );
      Debug.Assert( pcache1.pLruHead != pPage && pcache1.pLruTail != pPage );

      if ( reuseUnlikely != 0 || pcache1.nCurrentPage > pcache1.nMaxPage )
      {
        pcache1RemoveFromHash( pPage );
        pcache1FreePage( ref pPage );
      }
      else
      {
        /* Add the page to the global LRU list. Normally, the page is added to
        ** the head of the list (last page to be recycled). However, if the
        ** reuseUnlikely flag passed to this function is true, the page is added
        ** to the tail of the list (first page to be recycled).
        */
        if ( pcache1.pLruHead != null )
        {
          pcache1.pLruHead.pLruPrev = pPage;
          pPage.pLruNext = pcache1.pLruHead;
          pcache1.pLruHead = pPage;
        }
        else
        {
          pcache1.pLruTail = pPage;
          pcache1.pLruHead = pPage;
        }
        pCache.nRecyclable++;
      }

      pcache1LeaveMutex();
    }

    /*
    ** Implementation of the sqlite3_pcache.xRekey method.
    */
    static void pcache1Rekey(
    sqlite3_pcache p,
    PgHdr pPg,
    u32 iOld,
    u32 iNew
    )
    {
      PCache1 pCache = p;
      PgHdr1 pPage = PAGE_TO_PGHDR1( pCache, pPg );
      PgHdr1 pp;
      u32 h;
      Debug.Assert( pPage.iKey == iOld );
      Debug.Assert( pPage.pCache == pCache );

      pcache1EnterMutex();

      h = iOld % pCache.nHash;
      pp = pCache.apHash[h];
      while ( pp != pPage )
      {
        pp = pp.pNext;
      }
      if ( pp == pCache.apHash[h] ) pCache.apHash[h] = pp.pNext;
      else pp.pNext = pPage.pNext;

      h = iNew % pCache.nHash;
      pPage.iKey = iNew;
      pPage.pNext = pCache.apHash[h];
      pCache.apHash[h] = pPage;

      /* The xRekey() interface is only used to move pages earlier in the
      ** database file (in order to move all free pages to the end of the
      ** file where they can be truncated off.)  Hence, it is not possible
      ** for the new page number to be greater than the largest previously
      ** fetched page.  But we retain the following test in case xRekey()
      ** begins to be used in different ways in the future.
      */
      if ( NEVER( iNew > pCache.iMaxKey ) )
      {
        pCache.iMaxKey = iNew;
      }

      pcache1LeaveMutex();
    }

    /*
    ** Implementation of the sqlite3_pcache.xTruncate method.
    **
    ** Discard all unpinned pages in the cache with a page number equal to
    ** or greater than parameter iLimit. Any pinned pages with a page number
    ** equal to or greater than iLimit are implicitly unpinned.
    */
    static void pcache1Truncate( sqlite3_pcache p, u32 iLimit )
    {
      PCache1 pCache = (PCache1)p;
      pcache1EnterMutex();
      if ( iLimit <= pCache.iMaxKey )
      {
        pcache1TruncateUnsafe( pCache, iLimit );
        pCache.iMaxKey = iLimit - 1;
      }
      pcache1LeaveMutex();
    }

    /*
    ** Implementation of the sqlite3_pcache.xDestroy method.
    **
    ** Destroy a cache allocated using pcache1Create().
    */
    static void pcache1Destroy( ref sqlite3_pcache p )
    {
      PCache1 pCache = p;
      pcache1EnterMutex();
      pcache1TruncateUnsafe( pCache, 0 );
      pcache1.nMaxPage -= (int)pCache.nMax;
      pcache1.nMinPage -= (int)pCache.nMin;
      pcache1EnforceMaxPage();
      pcache1LeaveMutex();
      //sqlite3_free( ref pCache.apHash );
      //sqlite3_free( ref pCache );
    }

    /*
    ** This function is called during initialization (sqlite3_initialize()) to
    ** install the default pluggable cache module, assuming the user has not
    ** already provided an alternative.
    */
    static void sqlite3PCacheSetDefault()
    {
      sqlite3_pcache_methods defaultMethods = new sqlite3_pcache_methods(
      0,                                /* pArg */
      (dxPC_Init)pcache1Init,             /* xInit */
      (dxPC_Shutdown)pcache1Shutdown,  /* xShutdown */
      (dxPC_Create)pcache1Create,      /* xCreate */
      (dxPC_Cachesize)pcache1Cachesize,/* xCachesize */
      (dxPC_Pagecount)pcache1Pagecount,/* xPagecount */
      (dxPC_Fetch)pcache1Fetch,         /* xFetch */
      (dxPC_Unpin)pcache1Unpin,         /* xUnpin */
      (dxPC_Rekey)pcache1Rekey,         /* xRekey */
      (dxPC_Truncate)pcache1Truncate,   /* xTruncate */
      (dxPC_Destroy)pcache1Destroy      /* xDestroy */
      );
      sqlite3_config( SQLITE_CONFIG_PCACHE, defaultMethods );
    }

#if  SQLITE_ENABLE_MEMORY_MANAGEMENT
/*
** This function is called to free superfluous dynamically allocated memory
** held by the pager system. Memory in use by any SQLite pager allocated
** by the current thread may be //sqlite3_free()ed.
**
** nReq is the number of bytes of memory required. Once this much has
** been released, the function returns. The return value is the total number
** of bytes of memory released.
*/
int sqlite3PcacheReleaseMemory(int nReq){
int nFree = 0;
if( pcache1.pStart==0 ){
PgHdr1 p;
pcache1EnterMutex();
while( (nReq<0 || nFree<nReq) && (p=pcache1.pLruTail) ){
nFree += sqlite3MallocSize(PGHDR1_TO_PAGE(p));
pcache1PinPage(p);
pcache1RemoveFromHash(p);
pcache1FreePage(p);
}
pcache1LeaveMutex();
}
return nFree;
}
#endif //* SQLITE_ENABLE_MEMORY_MANAGEMENT */

#if  SQLITE_TEST
    /*
** This function is used by test procedures to inspect the internal state
** of the global cache.
*/
    static void sqlite3PcacheStats(
    ref int pnCurrent,      /* OUT: Total number of pages cached */
    ref int pnMax,          /* OUT: Global maximum cache size */
    ref int pnMin,          /* OUT: Sum of PCache1.nMin for purgeable caches */
    ref int pnRecyclable    /* OUT: Total number of pages available for recycling */
    )
    {
      PgHdr1 p;
      int nRecyclable = 0;
      for ( p = pcache1.pLruHead ; p != null ; p = p.pLruNext )
      {
        nRecyclable++;
      }
      pnCurrent = pcache1.nCurrentPage;
      pnMax = pcache1.nMaxPage;
      pnMin = pcache1.nMinPage;
      pnRecyclable = nRecyclable;
    }
#endif

  }
}
