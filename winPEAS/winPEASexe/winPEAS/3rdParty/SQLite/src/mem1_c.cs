using System;

namespace winPEAS._3rdParty.SQLite.src
{
  public partial class CSSQLite
  {
    /*
    ** 2007 August 14
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
    ** This file contains low-level memory allocation drivers for when
    ** SQLite will use the standard C-library malloc/realloc/free interface
    ** to obtain the memory it needs.
    **
    ** This file contains implementations of the low-level memory allocation
    ** routines specified in the sqlite3_mem_methods object.
    **
    ** $Id: mem1.c,v 1.30 2009/03/23 04:33:33 danielk1977 Exp $
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
    ** This version of the memory allocator is the default.  It is
    ** used when no other memory allocator is specified using compile-time
    ** macros.
    */
#if SQLITE_SYSTEM_MALLOC

    /*
** Like malloc(), but remember the size of the allocation
** so that we can find it later using sqlite3MemSize().
**
** For this low-level routine, we are guaranteed that nByte>0 because
** cases of nByte<=0 will be intercepted and dealt with by higher level
** routines.
*/
    static byte[] sqlite3MemMalloc( int nByte )
    {
      //sqlite3_int64 p;
      //Debug.Assert(nByte > 0 );
      //nByte = ROUND8(nByte);
      //p = malloc( nByte + 8 );
      //if ( p )
      //{
      //  p[0] = nByte;
      //  p++;
      //}
      //return (void*)p;
      return new byte[nByte];
    }
    /*
    ** Free memory.
    */
    // -- overloads ---------------------------------------
    static void sqlite3MemFree<T>( ref T x ) where T : class
    { x = null; }
    static void sqlite3MemFree( ref  string x ) { x = null; }
    //

    /*
    ** Like free() but works for allocations obtained from sqlite3MemMalloc()
    ** or sqlite3MemRealloc().
    **
    ** For this low-level routine, we already know that pPrior!=0 since
    ** cases where pPrior==0 will have been intecepted and dealt with
    ** by higher-level routines.
    */
    //static void sqlite3MemFree(void pPrior){
    //  sqlite3_int64 p = (sqlite3_int64*)pPrior;
    //  Debug.Assert(pPrior!=0 );
    //  p--;
    //  free(p);
    //}

    /*
    ** Like realloc().  Resize an allocation previously obtained from
    ** sqlite3MemMalloc().
    **
    ** For this low-level interface, we know that pPrior!=0.  Cases where
    ** pPrior==0 while have been intercepted by higher-level routine and
    ** redirected to xMalloc.  Similarly, we know that nByte>0 becauses
    ** cases where nByte<=0 will have been intercepted by higher-level
    ** routines and redirected to xFree.
    */
    static byte[] sqlite3MemRealloc( ref byte[] pPrior, int nByte )
    {
      //  sqlite3_int64 p = (sqlite3_int64*)pPrior;
      //  Debug.Assert(pPrior!=0 && nByte>0 );
      //  nByte = ROUND8( nByte );
      //  p = (sqlite3_int64*)pPrior;
      //  p--;
      //  p = realloc(p, nByte+8 );
      //  if( p ){
      //    p[0] = nByte;
      //    p++;
      //  }
      //  return (void*)p;
      Array.Resize( ref pPrior, nByte );
      return pPrior;
    }

    /*
    ** Report the allocated size of a prior return from xMalloc()
    ** or xRealloc().
    */
    static int sqlite3MemSize( byte[] pPrior )
    {
      //  sqlite3_int64 p;
      //  if( pPrior==0 ) return 0;
      //  p = (sqlite3_int64*)pPrior;
      //  p--;
      //  return p[0];
      return (int)pPrior.Length;
    }

    /*
    ** Round up a request size to the next valid allocation size.
    */
    static int sqlite3MemRoundup( int n )
    {
      return ROUND8( n );
    }

    /*
    ** Initialize this module.
    */
    static int sqlite3MemInit( object NotUsed )
    {
      UNUSED_PARAMETER( NotUsed );
      return SQLITE_OK;
    }

    /*
    ** Deinitialize this module.
    */
    static void sqlite3MemShutdown( object NotUsed )
    {
      UNUSED_PARAMETER( NotUsed );
      return;
    }

    /*
    ** This routine is the only routine in this file with external linkage.
    **
    ** Populate the low-level memory allocation function pointers in
    ** sqlite3GlobalConfig.m with pointers to the routines in this file.
    */
    static void sqlite3MemSetDefault()
    {
      sqlite3_mem_methods defaultMethods = new sqlite3_mem_methods(
      sqlite3MemMalloc,
      sqlite3MemFree,
      sqlite3MemRealloc,
      sqlite3MemSize,
      sqlite3MemRoundup,
      (dxMemInit)sqlite3MemInit,
      (dxMemShutdown)sqlite3MemShutdown,
      0
      );
      sqlite3_config( SQLITE_CONFIG_MALLOC, defaultMethods );
    }
#endif //* SQLITE_SYSTEM_MALLOC */
  }
}
