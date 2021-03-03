using System.Diagnostics;

namespace winPEAS._3rdParty.SQLite.src
{
    public partial class CSSQLite
  {
    /*
    ** 2008 June 18
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
    ** This module implements the sqlite3_status() interface and related
    ** functionality.
    **
    ** $Id: status.c,v 1.9 2008/09/02 00:52:52 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"

    /*
    ** Variables in which to record status information.
    */
    //typedef struct sqlite3StatType sqlite3StatType;
    public class sqlite3StatType
    {
      public int[] nowValue = new int[9];        /* Current value */
      public int[] mxValue = new int[9];           /* Maximum value */
    }
    public static sqlite3StatType sqlite3Stat = new sqlite3StatType();

    /* The "wsdStat" macro will resolve to the status information
    ** state vector.  If writable static data is unsupported on the target,
    ** we have to locate the state vector at run-time.  In the more common
    ** case where writable static data is supported, wsdStat can refer directly
    ** to the "sqlite3Stat" state vector declared above.
    */
#if SQLITE_OMIT_WSD
//# define wsdStatInit  sqlite3StatType *x = &GLOBAL(sqlite3StatType,sqlite3Stat)
//# define wsdStat x[0]
#else
    //# define wsdStatInit
    static void wsdStatInit() { }
    //# define wsdStat sqlite3Stat
    static sqlite3StatType wsdStat = sqlite3Stat;
#endif

    /*
** Return the current value of a status parameter.
*/
    static int sqlite3StatusValue( int op )
    {
      wsdStatInit();
      Debug.Assert( op >= 0 && op < ArraySize( wsdStat.nowValue ) );
      return wsdStat.nowValue[op];
    }

    /*
    ** Add N to the value of a status record.  It is assumed that the
    ** caller holds appropriate locks.
    */
    static void sqlite3StatusAdd( int op, int N )
    {
      wsdStatInit();
      Debug.Assert( op >= 0 && op < ArraySize( wsdStat.nowValue ) );
      wsdStat.nowValue[op] += N;
      if ( wsdStat.nowValue[op] > wsdStat.mxValue[op] )
      {
        wsdStat.mxValue[op] = wsdStat.nowValue[op];
      }
    }

    /*
    ** Set the value of a status to X.
    */
    static void sqlite3StatusSet( int op, int X )
    {
      wsdStatInit();
      Debug.Assert( op >= 0 && op < ArraySize( wsdStat.nowValue ) );
      wsdStat.nowValue[op] = X;
      if ( wsdStat.nowValue[op] > wsdStat.mxValue[op] )
      {
        wsdStat.mxValue[op] = wsdStat.nowValue[op];
      }
    }

    /*
    ** Query status information.
    **
    ** This implementation assumes that reading or writing an aligned
    ** 32-bit integer is an atomic operation.  If that assumption is not true,
    ** then this routine is not threadsafe.
    */
    static int sqlite3_status( int op, ref int pCurrent, ref int pHighwater, int resetFlag )
    {
      wsdStatInit();
      if ( op < 0 || op >= ArraySize( wsdStat.nowValue ) )
      {
        return SQLITE_MISUSE;
      }
      pCurrent = wsdStat.nowValue[op];
      pHighwater = wsdStat.mxValue[op];
      if ( resetFlag != 0 )
      {
        wsdStat.mxValue[op] = wsdStat.nowValue[op];
      }
      return SQLITE_OK;
    }
    /*
    ** Query status information for a single database connection
    */
    int sqlite3_db_status(
    sqlite3 db,          /* The database connection whose status is desired */
    int op,              /* Status verb */
    ref int pCurrent,    /* Write current value here */
    ref int pHighwater,  /* Write high-water mark here */
    int resetFlag        /* Reset high-water mark if true */
    )
    {
      switch ( op )
      {
        case SQLITE_DBSTATUS_LOOKASIDE_USED:
          {
            pCurrent = db.lookaside.nOut;
            pHighwater = db.lookaside.mxOut;
            if ( resetFlag != 0 )
            {
              db.lookaside.mxOut = db.lookaside.nOut;
            }
            break;
          }
        default:
          {
            return SQLITE_ERROR;
          }
      }
      return SQLITE_OK;
    }
  }
}
