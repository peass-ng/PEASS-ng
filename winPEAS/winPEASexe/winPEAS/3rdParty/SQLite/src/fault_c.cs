namespace winPEAS._3rdParty.SQLite.src
{
  public partial class CSSQLite
  {
    /*
    ** 2008 Jan 22
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
    ** $Id: fault.c,v 1.11 2008/09/02 00:52:52 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */

    /*
    ** This file contains code to support the concept of "benign"
    ** malloc failures (when the xMalloc() or xRealloc() method of the
    ** sqlite3_mem_methods structure fails to allocate a block of memory
    ** and returns 0).
    **
    ** Most malloc failures are non-benign. After they occur, SQLite
    ** abandons the current operation and returns an error code (usually
    ** SQLITE_NOMEM) to the user. However, sometimes a fault is not necessarily
    ** fatal. For example, if a malloc fails while resizing a hash table, this
    ** is completely recoverable simply by not carrying out the resize. The
    ** hash table will continue to function normally.  So a malloc failure
    ** during a hash table resize is a benign fault.
    */

    //#include "sqliteInt.h"

#if !SQLITE_OMIT_BUILTIN_TEST
    /*
** Global variables.
*/
    //typedef struct BenignMallocHooks BenignMallocHooks;
    public struct BenignMallocHooks//
    {
      public void_function xBenignBegin;//void (*xBenignBegin)(void);
      public void_function xBenignEnd;    //void (*xBenignEnd)(void);
      public BenignMallocHooks( void_function xBenignBegin, void_function xBenignEnd )
      {
        this.xBenignBegin = xBenignBegin;
        this.xBenignEnd = xBenignEnd;
      }
    }
    static BenignMallocHooks sqlite3Hooks = new BenignMallocHooks( null, null );

    /* The "wsdHooks" macro will resolve to the appropriate BenignMallocHooks
    ** structure.  If writable static data is unsupported on the target,
    ** we have to locate the state vector at run-time.  In the more common
    ** case where writable static data is supported, wsdHooks can refer directly
    ** to the "sqlite3Hooks" state vector declared above.
    */
#if SQLITE_OMIT_WSD
//# define wsdHooksInit \
BenignMallocHooks *x = &GLOBAL(BenignMallocHooks,sqlite3Hooks)
//# define wsdHooks x[0]
#else
    //# define wsdHooksInit
    static void wsdHooksInit() { }
    //# define wsdHooks sqlite3Hooks
    static BenignMallocHooks wsdHooks = sqlite3Hooks;
#endif



    /*
** Register hooks to call when sqlite3BeginBenignMalloc() and
** sqlite3EndBenignMalloc() are called, respectively.
*/
    static void sqlite3BenignMallocHooks(
    void_function xBenignBegin, //void (*xBenignBegin)(void),
    void_function xBenignEnd //void (*xBenignEnd)(void)
    )
    {
      wsdHooksInit();
      wsdHooks.xBenignBegin = xBenignBegin;
      wsdHooks.xBenignEnd = xBenignEnd;
    }

    /*
    ** This (sqlite3EndBenignMalloc()) is called by SQLite code to indicate that
    ** subsequent malloc failures are benign. A call to sqlite3EndBenignMalloc()
    ** indicates that subsequent malloc failures are non-benign.
    */
    static void sqlite3BeginBenignMalloc()
    {
      wsdHooksInit();
      if ( wsdHooks.xBenignBegin != null )
      {
        wsdHooks.xBenignBegin();
      }
    }
    static void sqlite3EndBenignMalloc()
    {
      wsdHooksInit();
      if ( wsdHooks.xBenignEnd != null )
      {
        wsdHooks.xBenignEnd();
      }
    }
#endif //* SQLITE_OMIT_BUILTIN_TEST */
  }
}
