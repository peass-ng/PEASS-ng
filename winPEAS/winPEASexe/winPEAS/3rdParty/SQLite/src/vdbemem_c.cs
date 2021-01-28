using System;
using System.Diagnostics;
using System.Text;

using i64 = System.Int64;
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;

namespace CS_SQLite3
{
  using sqlite3_value = CSSQLite.Mem;

  public partial class CSSQLite
  {
    /*
    ** 2004 May 26
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
    ** This file contains code use to manipulate "Mem" structure.  A "Mem"
    ** stores a single value in the VDBE.  Mem is an opaque structure visible
    ** only within the VDBE.  Interface routines refer to a Mem using the
    ** name sqlite_value
    **
    ** $Id: vdbemem.c,v 1.152 2009/07/22 18:07:41 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
    //#include "vdbeInt.h"

    /*
    ** Call sqlite3VdbeMemExpandBlob() on the supplied value (type Mem*)
    ** P if required.
    */
    //#define expandBlob(P) (((P)->flags&MEM_Zero)?sqlite3VdbeMemExpandBlob(P):0)
    static void expandBlob( Mem P )
    { if ( ( P.flags & MEM_Zero ) != 0 ) sqlite3VdbeMemExpandBlob( P ); } // TODO -- Convert to inline for speed

    /*
    ** If pMem is an object with a valid string representation, this routine
    ** ensures the internal encoding for the string representation is
    ** 'desiredEnc', one of SQLITE_UTF8, SQLITE_UTF16LE or SQLITE_UTF16BE.
    **
    ** If pMem is not a string object, or the encoding of the string
    ** representation is already stored using the requested encoding, then this
    ** routine is a no-op.
    **
    ** SQLITE_OK is returned if the conversion is successful (or not required).
    ** SQLITE_NOMEM may be returned if a malloc() fails during conversion
    ** between formats.
    */
    static int sqlite3VdbeChangeEncoding( Mem pMem, int desiredEnc )
    {
      int rc;
      Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );
      Debug.Assert( desiredEnc == SQLITE_UTF8 || desiredEnc == SQLITE_UTF16LE
      || desiredEnc == SQLITE_UTF16BE );
      if ( ( pMem.flags & MEM_Str ) == 0 || pMem.enc == desiredEnc )
      {
        if ( pMem.z == null && pMem.zBLOB != null ) pMem.z = Encoding.UTF8.GetString( pMem.zBLOB );
        return SQLITE_OK;
      }
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
#if  SQLITE_OMIT_UTF16
      return SQLITE_ERROR;
#else

/* MemTranslate() may return SQLITE_OK or SQLITE_NOMEM. If NOMEM is returned,
** then the encoding of the value may not have changed.
*/
rc = sqlite3VdbeMemTranslate(pMem, (u8)desiredEnc);
Debug.Assert(rc==SQLITE_OK    || rc==SQLITE_NOMEM);
Debug.Assert(rc==SQLITE_OK    || pMem.enc!=desiredEnc);
Debug.Assert(rc==SQLITE_NOMEM || pMem.enc==desiredEnc);
return rc;
#endif
    }

    /*
    ** Make sure pMem.z points to a writable allocation of at least
    ** n bytes.
    **
    ** If the memory cell currently contains string or blob data
    ** and the third argument passed to this function is true, the
    ** current content of the cell is preserved. Otherwise, it may
    ** be discarded.
    **
    ** This function sets the MEM_Dyn flag and clears any xDel callback.
    ** It also clears MEM_Ephem and MEM_Static. If the preserve flag is
    ** not set, Mem.n is zeroed.
    */
    static int sqlite3VdbeMemGrow( Mem pMem, int n, int preserve )
    {
      // TODO -- What do we want to do about this routine?
      //Debug.Assert( 1 >=
      //  ((pMem.zMalloc !=null )? 1 : 0) + //&& pMem.zMalloc==pMem.z) ? 1 : 0) +
      //  (((pMem.flags & MEM_Dyn)!=0 && pMem.xDel!=null) ? 1 : 0) +
      //  ((pMem.flags & MEM_Ephem)!=0 ? 1 : 0) +
      //  ((pMem.flags & MEM_Static)!=0 ? 1 : 0)
      //);
      //assert( (pMem->flags&MEM_RowSet)==0 );

      //if( n<32 ) n = 32;
      //if( sqlite3DbMallocSize(pMem->db, pMem.zMalloc)<n ){
      if ( preserve != 0 )
      {//& pMem.z==pMem.zMalloc ){
        if ( pMem.z == null ) pMem.z = "";//      sqlite3DbReallocOrFree( pMem.db, pMem.z, n );
        else pMem.z = pMem.z.Substring( 0, n );
        preserve = 0;
      }
      else
      {
        //  //sqlite3DbFree(pMem->db,ref pMem.zMalloc);
        pMem.z = "";//   sqlite3DbMallocRaw( pMem.db, n );
      }
      //}

      //  if( pMem->z && preserve && pMem->zMalloc && pMem->z!=pMem->zMalloc ){
      // memcpy(pMem.zMalloc, pMem.z, pMem.n);
      //}
      if ( ( pMem.flags & MEM_Dyn ) != 0 && pMem.xDel != null )
      {
        pMem.xDel( ref pMem.z );
      }

      // TODO --pMem.z = pMem.zMalloc;
      if ( pMem.z == null )
      {
        pMem.flags = MEM_Null;
      }
      else
      {
        pMem.flags = (u16)( pMem.flags & ~( MEM_Ephem | MEM_Static ) );
      }
      pMem.xDel = null;
      return pMem.z != null ? SQLITE_OK : SQLITE_NOMEM;
    }

    /*
    ** Make the given Mem object MEM_Dyn.  In other words, make it so
    ** that any TEXT or BLOB content is stored in memory obtained from
    ** malloc().  In this way, we know that the memory is safe to be
    ** overwritten or altered.
    **
    ** Return SQLITE_OK on success or SQLITE_NOMEM if malloc fails.
    */
    static int sqlite3VdbeMemMakeWriteable( Mem pMem )
    {
      int f;
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );
      expandBlob( pMem );
      f = pMem.flags;
      if ( ( f & ( MEM_Str | MEM_Blob ) ) != 0 ) // TODO -- && pMem.z != pMem.zMalloc )
      {
        //if ( sqlite3VdbeMemGrow( pMem, pMem.n + 2, 1 ) != 0 )
        //{
        //  return SQLITE_NOMEM;
        //}
        //pMem.z[pMem->n] = 0;
        //pMem.z[pMem->n + 1] = 0;
        pMem.flags |= MEM_Term;
      }

      return SQLITE_OK;
    }
    /*
    ** If the given Mem* has a zero-filled tail, turn it into an ordinary
    ** blob stored in dynamically allocated space.
    */
#if !SQLITE_OMIT_INCRBLOB
static int sqlite3VdbeMemExpandBlob( Mem pMem )
{
if ( ( pMem.flags & MEM_Zero ) != 0 )
{
u32 nByte;
Debug.Assert( ( pMem.flags & MEM_Blob ) != 0 );
Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );
Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
/* Set nByte to the number of bytes required to store the expanded blob. */
nByte = (u32)( pMem.n + pMem.u.nZero );
if ( nByte <= 0 )
{
nByte = 1;
}
if ( sqlite3VdbeMemGrow( pMem, (int)nByte, 1 ) != 0 )
{
return SQLITE_NOMEM;
} /* Set nByte to the number of bytes required to store the expanded blob. */
nByte = (u32)( pMem.n + pMem.u.nZero );
if ( nByte <= 0 )
{
nByte = 1;
}
if ( sqlite3VdbeMemGrow( pMem, (int)nByte, 1 ) != 0 )
{
return SQLITE_NOMEM;
}
//memset(&pMem->z[pMem->n], 0, pMem->u.nZero);
pMem.zBLOB = Encoding.UTF8.GetBytes( pMem.z );
pMem.z = null;
pMem.n += (int)pMem.u.nZero;
pMem.u.i = 0;
pMem.flags = (u16)( pMem.flags & ~( MEM_Zero | MEM_Static | MEM_Ephem | MEM_Term ) );
pMem.flags |= MEM_Dyn;
}
return SQLITE_OK;
}
#endif


    /*
** Make sure the given Mem is \u0000 terminated.
*/
    static int sqlite3VdbeMemNulTerminate( Mem pMem )
    {
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      if ( ( pMem.flags & MEM_Term ) != 0 || ( pMem.flags & MEM_Str ) == 0 )
      {
        return SQLITE_OK;   /* Nothing to do */
      }
      //if ( pMem.n != 0 && sqlite3VdbeMemGrow( pMem, pMem.n + 2, 1 ) != 0 )
      //{
      //  return SQLITE_NOMEM;
      //}
      //  pMem.z[pMem->n] = 0;
      //  pMem.z[pMem->n+1] = 0;
      if ( pMem.z != null && pMem.n < pMem.z.Length ) pMem.z = pMem.z.Substring( 0, pMem.n );
      pMem.flags |= MEM_Term;
      return SQLITE_OK;
    }

    /*
    ** Add MEM_Str to the set of representations for the given Mem.  Numbers
    ** are converted using sqlite3_snprintf().  Converting a BLOB to a string
    ** is a no-op.
    **
    ** Existing representations MEM_Int and MEM_Real are *not* invalidated.
    **
    ** A MEM_Null value will never be passed to this function. This function is
    ** used for converting values to text for returning to the user (i.e. via
    ** sqlite3_value_text()), or for ensuring that values to be used as btree
    ** keys are strings. In the former case a NULL pointer is returned the
    ** user and the later is an internal programming error.
    */
    static int sqlite3VdbeMemStringify( Mem pMem, int enc )
    {
      int rc = SQLITE_OK;
      int fg = pMem.flags;
      const int nByte = 32;

      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      Debug.Assert( ( fg & MEM_Zero ) == 0 );
      Debug.Assert( ( fg & ( MEM_Str | MEM_Blob ) ) == 0 );
      Debug.Assert( ( fg & ( MEM_Int | MEM_Real ) ) != 0 );
      Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );
      //assert( EIGHT_BYTE_ALIGNMENT(pMem) );

      if ( sqlite3VdbeMemGrow( pMem, nByte, 0 ) != 0 )
      {
        return SQLITE_NOMEM;
      }

      /* For a Real or Integer, use sqlite3_snprintf() to produce the UTF-8
      ** string representation of the value. Then, if the required encoding
      ** is UTF-16le or UTF-16be do a translation.
      **
      ** FIX ME: It would be better if sqlite3_snprintf() could do UTF-16.
      */
      if ( ( fg & MEM_Int ) != 0 )
      {
        pMem.z = pMem.u.i.ToString(); //sqlite3_snprintf(nByte, pMem.z, "%lld", pMem->u.i);
      }
      else
      {
        Debug.Assert( ( fg & MEM_Real ) != 0 );
        if ( Double.IsNegativeInfinity( pMem.r ) ) pMem.z = "-Inf";
        else if ( Double.IsInfinity( pMem.r ) ) pMem.z = "Inf";
        else if ( Double.IsPositiveInfinity( pMem.r ) ) pMem.z = "+Inf";
        else if ( pMem.r.ToString().Contains( "." ) ) pMem.z = pMem.r.ToString().ToLower();//sqlite3_snprintf(nByte, pMem.z, "%!.15g", pMem->r);
        else pMem.z = pMem.r.ToString() + ".0";
      }
      pMem.n = sqlite3Strlen30( pMem.z );
      pMem.enc = SQLITE_UTF8;
      pMem.flags |= MEM_Str | MEM_Term;
      sqlite3VdbeChangeEncoding( pMem, enc );
      return rc;
    }

    /*
    ** Memory cell pMem contains the context of an aggregate function.
    ** This routine calls the finalize method for that function.  The
    ** result of the aggregate is stored back into pMem.
    **
    ** Return SQLITE_ERROR if the finalizer reports an error.  SQLITE_OK
    ** otherwise.
    */
    static int sqlite3VdbeMemFinalize( Mem pMem, FuncDef pFunc )
    {
      int rc = SQLITE_OK;
      if ( ALWAYS( pFunc != null && pFunc.xFinalize != null ) )
      {
        sqlite3_context ctx = new sqlite3_context();
        Debug.Assert( ( pMem.flags & MEM_Null ) != 0 || pFunc == pMem.u.pDef );
        Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
        //memset(&ctx, 0, sizeof(ctx));
        ctx.s.flags = MEM_Null;
        ctx.s.db = pMem.db;
        ctx.pMem = pMem;
        ctx.pFunc = pFunc;
        pFunc.xFinalize( ctx );
        Debug.Assert( 0 == ( pMem.flags & MEM_Dyn ) && pMem.xDel == null );
        //sqlite3DbFree(pMem.db,ref pMem.zMalloc);
        ctx.s.CopyTo( pMem );//memcpy(pMem, &ctx.s, sizeof(ctx.s));
        rc = ctx.isError;
      }
      return rc;
    }

    /*
    ** If the memory cell contains a string value that must be freed by
    ** invoking an external callback, free it now. Calling this function
    ** does not free any Mem.zMalloc buffer.
    */
    static void sqlite3VdbeMemReleaseExternal( Mem p )
    {
      Debug.Assert( p.db == null || sqlite3_mutex_held( p.db.mutex ) );
      if ( ( p.flags & ( MEM_Agg | MEM_Dyn | MEM_RowSet ) ) != 0 )
      {
        if ( ( p.flags & MEM_Agg ) != 0 )
        {
          sqlite3VdbeMemFinalize( p, p.u.pDef );
          Debug.Assert( ( p.flags & MEM_Agg ) == 0 );
          sqlite3VdbeMemRelease( p );
        }
        else if ( ( p.flags & MEM_Dyn ) != 0 && p.xDel != null )
        {
          Debug.Assert( ( p.flags & MEM_RowSet ) == 0 );
          p.xDel( ref p.z );
          p.xDel = null;
        }
        else if ( ( p.flags & MEM_RowSet ) != 0 )
        {
          sqlite3RowSetClear( p.u.pRowSet );
        }
      }
      p.n = 0;
      p.z = null;
      p.zBLOB = null;
      //
      // Release additional C# pointers for backlinks
      p._Mem = null;
      p._SumCtx = null;
      p._MD5Context = null;
      p._MD5Context = null;
    }

    /*
    ** Release any memory held by the Mem. This may leave the Mem in an
    ** inconsistent state, for example with (Mem.z==0) and
    ** (Mem.type==SQLITE_TEXT).
    */
    static void sqlite3VdbeMemRelease( Mem p )
    {
      sqlite3VdbeMemReleaseExternal( p );
      //sqlite3DbFree(p.db,ref p.zMalloc);
      p.zBLOB = null;
      p.z = null;
      //p.zMalloc = null;
      p.xDel = null;
    }

    /*
    ** Convert a 64-bit IEEE double into a 64-bit signed integer.
    ** If the double is too large, return 0x8000000000000000.
    **
    ** Most systems appear to do this simply by assigning
    ** variables and without the extra range tests.  But
    ** there are reports that windows throws an expection
    ** if the floating point value is out of range. (See ticket #2880.)
    ** Because we do not completely understand the problem, we will
    ** take the conservative approach and always do range tests
    ** before attempting the conversion.
    */
    static i64 doubleToInt64( double r )
    {
      /*
      ** Many compilers we encounter do not define constants for the
      ** minimum and maximum 64-bit integers, or they define them
      ** inconsistently.  And many do not understand the "LL" notation.
      ** So we define our own static constants here using nothing
      ** larger than a 32-bit integer constant.
      */
      const i64 maxInt = LARGEST_INT64;
      const i64 minInt = SMALLEST_INT64;

      if ( r < (double)minInt )
      {
        return minInt;
      }
      else if ( r > (double)maxInt )
      {
        /* minInt is correct here - not maxInt.  It turns out that assigning
        ** a very large positive number to an integer results in a very large
        ** negative integer.  This makes no sense, but it is what x86 hardware
        ** does so for compatibility we will do the same in software. */
        return minInt;
      }
      else
      {
        return (i64)r;
      }
    }

    /*
    ** Return some kind of integer value which is the best we can do
    ** at representing the value that *pMem describes as an integer.
    ** If pMem is an integer, then the value is exact.  If pMem is
    ** a floating-point then the value returned is the integer part.
    ** If pMem is a string or blob, then we make an attempt to convert
    ** it into a integer and return that.  If pMem represents an
    ** an SQL-NULL value, return 0.
    **
    ** If pMem represents a string value, its encoding might be changed.
    */
    static i64 sqlite3VdbeIntValue( Mem pMem )
    {
      int flags;
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      // assert( EIGHT_BYTE_ALIGNMENT(pMem) );
      flags = pMem.flags;
      if ( ( flags & MEM_Int ) != 0 )
      {
        return pMem.u.i;
      }
      else if ( ( flags & MEM_Real ) != 0 )
      {
        return doubleToInt64( pMem.r );
      }
      else if ( ( flags & ( MEM_Str | MEM_Blob ) ) != 0 )
      {
        i64 value = 0;
        pMem.flags |= MEM_Str;
        if ( sqlite3VdbeChangeEncoding( pMem, SQLITE_UTF8 ) != 0
        || ( sqlite3VdbeMemNulTerminate( pMem ) != 0 ) )
        {
          return 0;
        }
        if ( pMem.z == null ) return 0;
        Debug.Assert( pMem.z != null );
        sqlite3Atoi64( pMem.z, ref value );
        return value;
      }
      else
      {
        return 0;
      }
    }

    /*
    ** Return the best representation of pMem that we can get into a
    ** double.  If pMem is already a double or an integer, return its
    ** value.  If it is a string or blob, try to convert it to a double.
    ** If it is a NULL, return 0.0.
    */
    static double sqlite3VdbeRealValue( Mem pMem )
    {
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      //assert( EIGHT_BYTE_ALIGNMENT(pMem) );
      if ( ( pMem.flags & MEM_Real ) != 0 )
      {
        return pMem.r;
      }
      else if ( ( pMem.flags & MEM_Int ) != 0 )
      {
        return (double)pMem.u.i;
      }
      else if ( ( pMem.flags & ( MEM_Str | MEM_Blob ) ) != 0 )
      {
        /* (double)0 In case of SQLITE_OMIT_FLOATING_POINT... */
        double val = (double)0;
        pMem.flags |= MEM_Str;
        if ( sqlite3VdbeChangeEncoding( pMem, SQLITE_UTF8 ) != 0
        || sqlite3VdbeMemNulTerminate( pMem ) != 0 )
        {
          /* (double)0 In case of SQLITE_OMIT_FLOATING_POINT... */
          return (double)0;
        }
        if ( pMem.zBLOB != null ) sqlite3AtoF( Encoding.UTF8.GetString( pMem.zBLOB ), ref val );
        else if ( pMem.z != null ) sqlite3AtoF( pMem.z, ref val );
        else val = 0.0;
        return val;
      }
      else
      {
        /* (double)0 In case of SQLITE_OMIT_FLOATING_POINT... */
        return (double)0;
      }
    }

    /*
    ** The MEM structure is already a MEM_Real.  Try to also make it a
    ** MEM_Int if we can.
    */
    static void sqlite3VdbeIntegerAffinity( Mem pMem )
    {
      Debug.Assert( ( pMem.flags & MEM_Real ) != 0 );
      Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      //assert( EIGHT_BYTE_ALIGNMENT(pMem) );

      pMem.u.i = doubleToInt64( pMem.r );

      /* Only mark the value as an integer if
      **
      **    (1) the round-trip conversion real->int->real is a no-op, and
      **    (2) The integer is neither the largest nor the smallest
      **        possible integer (ticket #3922)
      **
      ** The second term in the following conditional enforces the second
      ** condition under the assumption that additional overflow causes
      ** values to wrap around.
      */
      if ( pMem.r == (double)pMem.u.i && ( pMem.u.i - 1 ) < ( pMem.u.i + 1 ) )
      {
        pMem.flags |= MEM_Int;
      }
    }

    /*
    ** Convert pMem to type integer.  Invalidate any prior representations.
    */
    static int sqlite3VdbeMemIntegerify( Mem pMem )
    {
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );
      //assert( EIGHT_BYTE_ALIGNMENT(pMem) );

      pMem.u.i = sqlite3VdbeIntValue( pMem );
      MemSetTypeFlag( pMem, MEM_Int );
      return SQLITE_OK;
    }

    /*
    ** Convert pMem so that it is of type MEM_Real.
    ** Invalidate any prior representations.
    */
    static int sqlite3VdbeMemRealify( Mem pMem )
    {
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      //assert( EIGHT_BYTE_ALIGNMENT(pMem) );

      pMem.r = sqlite3VdbeRealValue( pMem );
      MemSetTypeFlag( pMem, MEM_Real );
      return SQLITE_OK;
    }

    /*
    ** Convert pMem so that it has types MEM_Real or MEM_Int or both.
    ** Invalidate any prior representations.
    */
    static int sqlite3VdbeMemNumerify( Mem pMem )
    {
      double r1, r2;
      i64 i;
      Debug.Assert( ( pMem.flags & ( MEM_Int | MEM_Real | MEM_Null ) ) == 0 );
      Debug.Assert( ( pMem.flags & ( MEM_Blob | MEM_Str ) ) != 0 );
      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      r1 = sqlite3VdbeRealValue( pMem );
      i = doubleToInt64( r1 );
      r2 = (double)i;
      if ( r1 == r2 )
      {
        sqlite3VdbeMemIntegerify( pMem );
      }
      else
      {
        pMem.r = r1;
        MemSetTypeFlag( pMem, MEM_Real );
      }
      return SQLITE_OK;
    }

    /*
    ** Delete any previous value and set the value stored in pMem to NULL.
    */
    static void sqlite3VdbeMemSetNull( Mem pMem )
    {
      if ( ( pMem.flags & MEM_RowSet ) != 0 )
      {
        sqlite3RowSetClear( pMem.u.pRowSet );
      }
      MemSetTypeFlag( pMem, MEM_Null );
      pMem.zBLOB = null;
      pMem.z = null;
      pMem.type = SQLITE_NULL;
    }

    /*
    ** Delete any previous value and set the value to be a BLOB of length
    ** n containing all zeros.
    */
    static void sqlite3VdbeMemSetZeroBlob( Mem pMem, int n )
    {
      sqlite3VdbeMemRelease( pMem );
      pMem.flags = MEM_Blob | MEM_Zero;
      pMem.type = SQLITE_BLOB;
      pMem.n = 0;
      if ( n < 0 ) n = 0;
      pMem.u.nZero = n;
      pMem.enc = SQLITE_UTF8;
#if SQLITE_OMIT_INCRBLOB
  sqlite3VdbeMemGrow(pMem, n, 0);
  //if( pMem.z!= null ){
   pMem.n = n;
   pMem.z = null;//memset(pMem.z, 0, n);
   pMem.zBLOB = new byte[n];
   //}
#endif
    }

    /*
    ** Delete any previous value and set the value stored in pMem to val,
    ** manifest type INTEGER.
    */
    static void sqlite3VdbeMemSetInt64( Mem pMem, i64 val )
    {
      sqlite3VdbeMemRelease( pMem );
      pMem.u.i = val;
      pMem.flags = MEM_Int;
      pMem.type = SQLITE_INTEGER;
    }

    /*
    ** Delete any previous value and set the value stored in pMem to val,
    ** manifest type REAL.
    */
    static void sqlite3VdbeMemSetDouble( Mem pMem, double val )
    {
      if ( sqlite3IsNaN( val ) )
      {
        sqlite3VdbeMemSetNull( pMem );
      }
      else
      {
        sqlite3VdbeMemRelease( pMem );
        pMem.r = val;
        pMem.flags = MEM_Real;
        pMem.type = SQLITE_FLOAT;
      }
    }

    /*
    ** Delete any previous value and set the value of pMem to be an
    ** empty boolean index.
    */
    static void sqlite3VdbeMemSetRowSet( Mem pMem )
    {
      sqlite3 db = pMem.db;
      Debug.Assert( db != null );
      Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );
      sqlite3VdbeMemRelease( pMem );
      //pMem.zMalloc = sqlite3DbMallocRaw( db, 64 );
      //if ( db.mallocFailed != 0 )
      //{
      //  pMem.flags = MEM_Null;
      //}
      //else
      {
        //Debug.Assert( pMem.zMalloc );
        pMem.u.pRowSet = new RowSet( db, 5 );// sqlite3RowSetInit( db, pMem.zMalloc,
        //     sqlite3DbMallocSize( db, pMem.zMalloc ) );
        Debug.Assert( pMem.u.pRowSet != null );
        pMem.flags = MEM_RowSet;
      }
    }

    /*
    ** Return true if the Mem object contains a TEXT or BLOB that is
    ** too large - whose size exceeds p.db.aLimit[SQLITE_LIMIT_LENGTH].
    */
    static bool sqlite3VdbeMemTooBig( Mem p )
    {
      Debug.Assert( p.db != null );
      if ( ( p.flags & ( MEM_Str | MEM_Blob ) ) != 0 )
      {
        int n = p.n;
        if ( ( p.flags & MEM_Zero ) != 0 )
        {
          n += p.u.nZero;
        }
        return n > p.db.aLimit[SQLITE_LIMIT_LENGTH];
      }
      return false;
    }

    /*
    ** Size of struct Mem not including the Mem.zMalloc member.
    */
    //#define MEMCELLSIZE (size_t)(&(((Mem *)0).zMalloc))

    /*
    ** Make an shallow copy of pFrom into pTo.  Prior contents of
    ** pTo are freed.  The pFrom.z field is not duplicated.  If
    ** pFrom.z is used, then pTo.z points to the same thing as pFrom.z
    ** and flags gets srcType (either MEM_Ephem or MEM_Static).
    */
    static void sqlite3VdbeMemShallowCopy( Mem pTo, Mem pFrom, int srcType )
    {
      Debug.Assert( ( pFrom.flags & MEM_RowSet ) == 0 );
      sqlite3VdbeMemReleaseExternal( pTo );
      pFrom.CopyTo( pTo );//  memcpy(pTo, pFrom, MEMCELLSIZE);
      pTo.xDel = null;
      if ( ( pFrom.flags & MEM_Dyn ) != 0 )
      {//|| pFrom.z==pFrom.zMalloc ){
        pTo.flags = (u16)( pFrom.flags & ~( MEM_Dyn | MEM_Static | MEM_Ephem ) );
        Debug.Assert( srcType == MEM_Ephem || srcType == MEM_Static );
        pTo.flags |= (u16)srcType;
      }
    }

    /*
    ** Make a full copy of pFrom into pTo.  Prior contents of pTo are
    ** freed before the copy is made.
    */
    static int sqlite3VdbeMemCopy( Mem pTo, Mem pFrom )
    {
      int rc = SQLITE_OK;

      Debug.Assert( ( pFrom.flags & MEM_RowSet ) == 0 );
      sqlite3VdbeMemReleaseExternal( pTo );
      pFrom.CopyTo( pTo );// memcpy(pTo, pFrom, MEMCELLSIZE);
      pTo.flags = (u16)( pTo.flags & ~MEM_Dyn );

      if ( ( pTo.flags & ( MEM_Str | MEM_Blob ) ) != 0 )
      {
        if ( 0 == ( pFrom.flags & MEM_Static ) )
        {
          pTo.flags |= MEM_Ephem;
          rc = sqlite3VdbeMemMakeWriteable( pTo );
        }
      }

      return rc;
    }




    /*
    ** Transfer the contents of pFrom to pTo. Any existing value in pTo is
    ** freed. If pFrom contains ephemeral data, a copy is made.
    **
    ** pFrom contains an SQL NULL when this routine returns.
    */
    static void sqlite3VdbeMemMove( Mem pTo, Mem pFrom )
    {
      Debug.Assert( pFrom.db == null || sqlite3_mutex_held( pFrom.db.mutex ) );
      Debug.Assert( pTo.db == null || sqlite3_mutex_held( pTo.db.mutex ) );
      Debug.Assert( pFrom.db == null || pTo.db == null || pFrom.db == pTo.db );
      sqlite3VdbeMemRelease( pTo );
      pFrom.CopyTo( pTo );// memcpy(pTo, pFrom, Mem).Length;
      pFrom.flags = MEM_Null;
      pFrom.xDel = null;
      pFrom.z = null;
      pFrom.zBLOB = null;
      //pFrom.zMalloc=null;
    }

    /*
    ** Change the value of a Mem to be a string or a BLOB.
    **
    ** The memory management strategy depends on the value of the xDel
    ** parameter. If the value passed is SQLITE_TRANSIENT, then the
    ** string is copied into a (possibly existing) buffer managed by the
    ** Mem structure. Otherwise, any existing buffer is freed and the
    ** pointer copied.
    **
    ** If the string is too large (if it exceeds the SQLITE_LIMIT_LENGTH
    ** size limit) then no memory allocation occurs.  If the string can be
    ** stored without allocating memory, then it is.  If a memory allocation
    ** is required to store the string, then value of pMem is unchanged.  In
    ** either case, SQLITE_TOOBIG is returned.
    */
    static int sqlite3VdbeMemSetStr(
    Mem pMem,           /* Memory cell to set to string value */
    string z,           /* String pointer */
    int n,              /* Bytes in string, or negative */
    u8 enc,             /* Encoding of z.  0 for BLOBs */
    dxDel xDel//)(void*)/* Destructor function */
    )
    {
      int nByte = n;      /* New value for pMem->n */
      int iLimit;         /* Maximum allowed string or blob size */
      u16 flags = 0;      /* New value for pMem->flags */

      Debug.Assert( pMem.db == null || sqlite3_mutex_held( pMem.db.mutex ) );
      Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );

      /* If z is a NULL pointer, set pMem to contain an SQL NULL. */
      if ( z == null )
      {
        sqlite3VdbeMemSetNull( pMem );
        return SQLITE_OK;
      }

      if ( pMem.db != null )
      {
        iLimit = pMem.db.aLimit[SQLITE_LIMIT_LENGTH];
      }
      else
      {
        iLimit = SQLITE_MAX_LENGTH;
      }
      flags = (u16)( enc == 0 ? MEM_Blob : MEM_Str );
      if ( nByte < 0 )
      {
        Debug.Assert( enc != 0 );
        if ( enc == SQLITE_UTF8 )
        {
          for ( nByte = 0 ; nByte <= iLimit && nByte < z.Length && z[nByte] != 0 ; nByte++ ) { }
        }
        else
        {
          for ( nByte = 0 ; nByte <= iLimit && z[nByte] != 0 || z[nByte + 1] != 0 ; nByte += 2 ) { }
        }
        flags |= MEM_Term;
      }

      /* The following block sets the new values of Mem.z and Mem.xDel. It
      ** also sets a flag in local variable "flags" to indicate the memory
      ** management (one of MEM_Dyn or MEM_Static).
      */
      if ( xDel == SQLITE_TRANSIENT )
      {
        u32 nAlloc = (u32)nByte;
        if ( ( flags & MEM_Term ) != 0 )
        {
          nAlloc += (u32)( enc == SQLITE_UTF8 ? 1 : 2 );
        }
        if ( nByte > iLimit )
        {
          return SQLITE_TOOBIG;
        }
        if ( sqlite3VdbeMemGrow( pMem, (int)nAlloc, 0 ) != 0 )
        {
          return SQLITE_NOMEM;
        }
        //if ( nAlloc < z.Length )
        //{ pMem.z = new byte[nAlloc]; Buffer.BlockCopy( z, 0, pMem.z, 0, (int)nAlloc ); }
        //else
        if ( enc == 0 )
        {
          pMem.z = null;
          pMem.zBLOB = new byte[n];
          for ( int i = 0 ; i < n && i < z.Length ; i++ ) pMem.zBLOB[i] = (byte)z[i];
        }
        else
        {
          pMem.z = z;//memcpy(pMem.z, z, nAlloc);
          pMem.zBLOB = null;
        }
      }
      else if ( xDel == SQLITE_DYNAMIC )
      {
        sqlite3VdbeMemRelease( pMem );
        //pMem.zMalloc = pMem.z = (char*)z;
        if ( enc == 0 )
        {
          pMem.z = null;
          pMem.zBLOB = Encoding.UTF8.GetBytes( z );
        }
        else
        {
          pMem.z = z;//memcpy(pMem.z, z, nAlloc);
          pMem.zBLOB = null;
        }
        pMem.xDel = null;
      }
      else
      {
        sqlite3VdbeMemRelease( pMem );
        if ( enc == 0 )
        {
          pMem.z = null;
          pMem.zBLOB = Encoding.UTF8.GetBytes( z );
        }
        else
        {
          pMem.z = z;//memcpy(pMem.z, z, nAlloc);
          pMem.zBLOB = null;
        }
        pMem.xDel = xDel;
        flags |= (u16)( ( xDel == SQLITE_STATIC ) ? MEM_Static : MEM_Dyn );
      }
      pMem.n = nByte;
      pMem.flags = flags;
      pMem.enc = ( enc == 0 ? SQLITE_UTF8 : enc );
      pMem.type = ( enc == 0 ? SQLITE_BLOB : SQLITE_TEXT );

#if !SQLITE_OMIT_UTF16
if( pMem.enc!=SQLITE_UTF8 && sqlite3VdbeMemHandleBom(pMem)!=0 ){
return SQLITE_NOMEM;
}
#endif

      if ( nByte > iLimit )
      {
        return SQLITE_TOOBIG;
      }

      return SQLITE_OK;
    }

    /*
    ** Compare the values contained by the two memory cells, returning
    ** negative, zero or positive if pMem1 is less than, equal to, or greater
    ** than pMem2. Sorting order is NULL's first, followed by numbers (integers
    ** and reals) sorted numerically, followed by text ordered by the collating
    ** sequence pColl and finally blob's ordered by memcmp().
    **
    ** Two NULL values are considered equal by this function.
    */
    static int sqlite3MemCompare( Mem pMem1, Mem pMem2, CollSeq pColl )
    {
      int rc;
      int f1, f2;
      int combined_flags;

      /* Interchange pMem1 and pMem2 if the collating sequence specifies
      ** DESC order.
      */
      f1 = pMem1.flags;
      f2 = pMem2.flags;
      combined_flags = f1 | f2;
      Debug.Assert( ( combined_flags & MEM_RowSet ) == 0 );

      /* If one value is NULL, it is less than the other. If both values
      ** are NULL, return 0.
      */
      if ( ( combined_flags & MEM_Null ) != 0 )
      {
        return ( f2 & MEM_Null ) - ( f1 & MEM_Null );
      }

      /* If one value is a number and the other is not, the number is less.
      ** If both are numbers, compare as reals if one is a real, or as integers
      ** if both values are integers.
      */
      if ( ( combined_flags & ( MEM_Int | MEM_Real ) ) != 0 )
      {
        if ( ( f1 & ( MEM_Int | MEM_Real ) ) == 0 )
        {
          return 1;
        }
        if ( ( f2 & ( MEM_Int | MEM_Real ) ) == 0 )
        {
          return -1;
        }
        if ( ( f1 & f2 & MEM_Int ) == 0 )
        {
          double r1, r2;
          if ( ( f1 & MEM_Real ) == 0 )
          {
            r1 = (double)pMem1.u.i;
          }
          else
          {
            r1 = pMem1.r;
          }
          if ( ( f2 & MEM_Real ) == 0 )
          {
            r2 = (double)pMem2.u.i;
          }
          else
          {
            r2 = pMem2.r;
          }
          if ( r1 < r2 ) return -1;
          if ( r1 > r2 ) return 1;
          return 0;
        }
        else
        {
          Debug.Assert( ( f1 & MEM_Int ) != 0 );
          Debug.Assert( ( f2 & MEM_Int ) != 0 );
          if ( pMem1.u.i < pMem2.u.i ) return -1;
          if ( pMem1.u.i > pMem2.u.i ) return 1;
          return 0;
        }
      }

      /* If one value is a string and the other is a blob, the string is less.
      ** If both are strings, compare using the collating functions.
      */
      if ( ( combined_flags & MEM_Str ) != 0 )
      {
        if ( ( f1 & MEM_Str ) == 0 )
        {
          return 1;
        }
        if ( ( f2 & MEM_Str ) == 0 )
        {
          return -1;
        }

        Debug.Assert( pMem1.enc == pMem2.enc );
        Debug.Assert( pMem1.enc == SQLITE_UTF8 ||
        pMem1.enc == SQLITE_UTF16LE || pMem1.enc == SQLITE_UTF16BE );

        /* The collation sequence must be defined at this point, even if
        ** the user deletes the collation sequence after the vdbe program is
        ** compiled (this was not always the case).
        */
        Debug.Assert( pColl == null || pColl.xCmp != null );

        if ( pColl != null )
        {
          if ( pMem1.enc == pColl.enc )
          {
            /* The strings are already in the correct encoding.  Call the
            ** comparison function directly */
            return pColl.xCmp( pColl.pUser, pMem1.n, pMem1.z, pMem2.n, pMem2.z );
          }
          else
          {
            string v1, v2;
            int n1, n2;
            Mem c1;
            Mem c2;
            c1 = new Mem();// memset( &c1, 0, sizeof( c1 ) );
            c2 = new Mem();//memset( &c2, 0, sizeof( c2 ) );
            sqlite3VdbeMemShallowCopy( c1, pMem1, MEM_Ephem );
            sqlite3VdbeMemShallowCopy( c2, pMem2, MEM_Ephem );
            v1 = sqlite3ValueText( (sqlite3_value)c1, pColl.enc );
            n1 = v1 == null ? 0 : c1.n;
            v2 = sqlite3ValueText( (sqlite3_value)c2, pColl.enc );
            n2 = v2 == null ? 0 : c2.n;
            rc = pColl.xCmp( pColl.pUser, n1, v1, n2, v2 );
            sqlite3VdbeMemRelease( c1 );
            sqlite3VdbeMemRelease( c2 );
            return rc;
          }
        }
        /* If a NULL pointer was passed as the collate function, fall through
        ** to the blob case and use memcmp().  */
      }

      /* Both values must be blobs.  Compare using memcmp().  */
      if ( ( pMem1.flags & MEM_Blob ) != 0 )
        if ( pMem1.zBLOB != null ) rc = memcmp( pMem1.zBLOB, pMem2.zBLOB, ( pMem1.n > pMem2.n ) ? pMem2.n : pMem1.n );
        else rc = memcmp( pMem1.z, pMem2.zBLOB, ( pMem1.n > pMem2.n ) ? pMem2.n : pMem1.n );
      else
        rc = memcmp( pMem1.z, pMem2.z, ( pMem1.n > pMem2.n ) ? pMem2.n : pMem1.n );
      if ( rc == 0 )
      {
        rc = pMem1.n - pMem2.n;
      }
      return rc;
    }

    /*
    ** Move data out of a btree key or data field and into a Mem structure.
    ** The data or key is taken from the entry that pCur is currently pointing
    ** to.  offset and amt determine what portion of the data or key to retrieve.
    ** key is true to get the key or false to get data.  The result is written
    ** into the pMem element.
    **
    ** The pMem structure is assumed to be uninitialized.  Any prior content
    ** is overwritten without being freed.
    **
    ** If this routine fails for any reason (malloc returns NULL or unable
    ** to read from the disk) then the pMem is left in an inconsistent state.
    */
    static int sqlite3VdbeMemFromBtree(
    BtCursor pCur,    /* Cursor pointing at record to retrieve. */
    int offset,       /* Offset from the start of data to return bytes from. */
    int amt,          /* Number of bytes to return. */
    bool key,         /* If true, retrieve from the btree key, not data. */
    Mem pMem          /* OUT: Return data in this Mem structure. */
    )
    {
      byte[] zData;       /* Data from the btree layer */
      int available = 0; /* Number of bytes available on the local btree page */
      int rc = SQLITE_OK; /* Return code */

      Debug.Assert( sqlite3BtreeCursorIsValid(pCur) );

	/* Note: the calls to BtreeKeyFetch() and DataFetch() below assert()
      ** that both the BtShared and database handle mutexes are held. */
      Debug.Assert( ( pMem.flags & MEM_RowSet ) == 0 );
      int outOffset = -1;
      if ( key )
      {
        zData = sqlite3BtreeKeyFetch( pCur, ref available, ref outOffset );
      }
      else
      {
        zData = sqlite3BtreeDataFetch( pCur, ref available, ref outOffset );
      }
      Debug.Assert( zData != null );

      if ( offset + amt <= available && ( pMem.flags & MEM_Dyn ) == 0 )
      {
        sqlite3VdbeMemRelease( pMem );
        pMem.zBLOB = new byte[amt];
        Buffer.BlockCopy( zData, offset, pMem.zBLOB, 0, amt );//pMem.z = &zData[offset];
        pMem.flags = MEM_Blob | MEM_Ephem;
      }
      else if ( SQLITE_OK == ( rc = sqlite3VdbeMemGrow( pMem, amt + 2, 0 ) ) )
      {
        pMem.enc = 0;
        pMem.type = SQLITE_BLOB;
        pMem.z = null;
        pMem.zBLOB = new byte[amt];
        pMem.flags = MEM_Blob | MEM_Dyn | MEM_Term;
        if ( key )
        {
          rc = sqlite3BtreeKey( pCur, (u32)offset, (u32)amt,  pMem.zBLOB );
        }
        else
        {
          rc = sqlite3BtreeData( pCur, (u32)offset, (u32)amt, pMem.zBLOB );//pMem.z =  pMem_z ;
        }
        //pMem.z[amt] = 0;
        //pMem.z[amt+1] = 0;
        if ( rc != SQLITE_OK )
        {
          sqlite3VdbeMemRelease( pMem );
        }
      }
      pMem.n = amt;

      return rc;
    }

    /* This function is only available internally, it is not part of the
    ** external API. It works in a similar way to sqlite3_value_text(),
    ** except the data returned is in the encoding specified by the second
    ** parameter, which must be one of SQLITE_UTF16BE, SQLITE_UTF16LE or
    ** SQLITE_UTF8.
    **
    ** (2006-02-16:)  The enc value can be or-ed with SQLITE_UTF16_ALIGNED.
    ** If that is the case, then the result must be aligned on an even byte
    ** boundary.
    */
    static string sqlite3ValueText( sqlite3_value pVal, int enc )
    {
      if ( pVal == null ) return null;

      Debug.Assert( pVal.db == null || sqlite3_mutex_held( pVal.db.mutex ) );
      Debug.Assert( ( enc & 3 ) == ( enc & ~SQLITE_UTF16_ALIGNED ) );
      Debug.Assert( ( pVal.flags & MEM_RowSet ) == 0 );

      if ( ( pVal.flags & MEM_Null ) != 0 )
      {
        return null;
      }
      Debug.Assert( ( MEM_Blob >> 3 ) == MEM_Str );
      pVal.flags |= (u16)( ( pVal.flags & MEM_Blob ) >> 3 );
      if ( ( pVal.flags & MEM_Zero ) != 0 ) sqlite3VdbeMemExpandBlob( pVal ); // expandBlob(pVal);
      if ( ( pVal.flags & MEM_Str ) != 0 )
      {
        sqlite3VdbeChangeEncoding( pVal, enc & ~SQLITE_UTF16_ALIGNED );
        if ( ( enc & SQLITE_UTF16_ALIGNED ) != 0 && 1 == ( 1 & ( pVal.z[0] ) ) )  //1==(1&SQLITE_PTR_TO_INT(pVal.z))
        {
          Debug.Assert( ( pVal.flags & ( MEM_Ephem | MEM_Static ) ) != 0 );
          if ( sqlite3VdbeMemMakeWriteable( pVal ) != SQLITE_OK )
          {
            return null;
          }
        }
        sqlite3VdbeMemNulTerminate( pVal );
      }
      else
      {
        Debug.Assert( ( pVal.flags & MEM_Blob ) == 0 );
        sqlite3VdbeMemStringify( pVal, enc );
        //  assert( 0==(1&SQLITE_PTR_TO_INT(pVal->z)) );
      }
      Debug.Assert( pVal.enc == ( enc & ~SQLITE_UTF16_ALIGNED ) || pVal.db == null
      //|| pVal.db.mallocFailed != 0
      );
      if ( pVal.enc == ( enc & ~SQLITE_UTF16_ALIGNED ) )
      {
        return pVal.z;
      }
      else
      {
        return null;
      }
    }

    /*
    ** Create a new sqlite3_value object.
    */
    static sqlite3_value sqlite3ValueNew( sqlite3 db )
    {
      Mem p = new Mem();//sqlite3DbMallocZero(db, sizeof(*p));
      if ( p != null )
      {
        p.flags = MEM_Null;
        p.type = SQLITE_NULL;
        p.db = db;
      }
      return p;
    }

    /*
    ** Create a new sqlite3_value object, containing the value of pExpr.
    **
    ** This only works for very simple expressions that consist of one constant
    ** token (i.e. "5", "5.1", "'a string'"). If the expression can
    ** be converted directly into a value, then the value is allocated and
    ** a pointer written to ppVal. The caller is responsible for deallocating
    ** the value by passing it to sqlite3ValueFree() later on. If the expression
    ** cannot be converted to a value, then ppVal is set to NULL.
    */
    static int sqlite3ValueFromExpr(
    sqlite3 db,              /* The database connection */
    Expr pExpr,              /* The expression to evaluate */
    int enc,                   /* Encoding to use */
    char affinity,              /* Affinity to use */
    ref sqlite3_value ppVal     /* Write the new value here */
    )
    {
      int op;
      string zVal = "";
      sqlite3_value pVal = null;

      if ( pExpr == null )
      {
        ppVal = null;
        return SQLITE_OK;
      }
      op = pExpr.op;

      if ( op == TK_STRING || op == TK_FLOAT || op == TK_INTEGER )
      {
        pVal = sqlite3ValueNew( db );
        if ( pVal == null ) goto no_mem;
        if ( ExprHasProperty( pExpr, EP_IntValue ) )
        {
          sqlite3VdbeMemSetInt64( pVal, (i64)pExpr.u.iValue );
        }
        else
        {
          zVal = pExpr.u.zToken;// sqlite3DbStrDup( db, pExpr.u.zToken );
          if ( zVal == null ) goto no_mem;
          sqlite3ValueSetStr( pVal, -1, zVal, SQLITE_UTF8, SQLITE_DYNAMIC );
        }
        if ( ( op == TK_INTEGER || op == TK_FLOAT ) && affinity == SQLITE_AFF_NONE )
        {
          sqlite3ValueApplyAffinity( pVal, SQLITE_AFF_NUMERIC, SQLITE_UTF8 );
        }
        else
        {
          sqlite3ValueApplyAffinity( pVal, affinity, SQLITE_UTF8 );
        }
        if ( enc != SQLITE_UTF8 )
        {
          sqlite3VdbeChangeEncoding( pVal, enc );
        }
      }
      if ( enc != SQLITE_UTF8 )
      {
        sqlite3VdbeChangeEncoding( pVal, enc );
      }
      else if ( op == TK_UMINUS )
      {
        if ( SQLITE_OK == sqlite3ValueFromExpr( db, pExpr.pLeft, enc, affinity, ref pVal ) )
        {
          pVal.u.i = -1 * pVal.u.i;
          /* (double)-1 In case of SQLITE_OMIT_FLOATING_POINT... */
          pVal.r = (double)-1 * pVal.r;
        }
      }
#if !SQLITE_OMIT_BLOB_LITERAL
      else if ( op == TK_BLOB )
      {
        int nVal;
        Debug.Assert( pExpr.u.zToken[0] == 'x' || pExpr.u.zToken[0] == 'X' );
        Debug.Assert( pExpr.u.zToken[1] == '\'' );
        pVal = sqlite3ValueNew( db );
        if ( null == pVal ) goto no_mem;
        zVal = pExpr.u.zToken.Substring( 2 );
        nVal = sqlite3Strlen30( zVal ) - 1;
        Debug.Assert( zVal[nVal] == '\'' );
        sqlite3VdbeMemSetStr( pVal, Encoding.UTF8.GetString( sqlite3HexToBlob( db, zVal, nVal ) ), nVal / 2,
        0, SQLITE_DYNAMIC );
      }
#endif

      ppVal = pVal;
      return SQLITE_OK;

no_mem:
      //db.mallocFailed = 1;
      //sqlite3DbFree( db, ref zVal );
      pVal = null;// sqlite3ValueFree(pVal);
      ppVal = null;
      return SQLITE_NOMEM;
    }

    /*
    ** Change the string value of an sqlite3_value object
    */
    static void sqlite3ValueSetStr(
    sqlite3_value v,     /* Value to be set */
    int n,               /* Length of string z */
    string z,            /* Text of the new string */
    u8 enc,              /* Encoding to use */
    dxDel xDel//)(void*) /* Destructor for the string */
    )
    {
      if ( v != null ) sqlite3VdbeMemSetStr( v, z, n, enc, xDel );
    }

    /*
    ** Free an sqlite3_value object
    */
    static void sqlite3ValueFree( ref sqlite3_value v )
    {
      if ( v == null ) return;
      sqlite3VdbeMemRelease( v );
      //sqlite3DbFree( v.db, ref v );
    }

    /*
    ** Return the number of bytes in the sqlite3_value object assuming
    ** that it uses the encoding "enc"
    */
    static int sqlite3ValueBytes( sqlite3_value pVal, int enc )
    {
      Mem p = (Mem)pVal;
      if ( ( p.flags & MEM_Blob ) != 0 || sqlite3ValueText( pVal, enc ) != null )
      {
        if ( ( p.flags & MEM_Zero ) != 0 )
        {
          return p.n + p.u.nZero;
        }
        else
        {
          return p.z == null ? p.zBLOB.Length : p.n;
        }
      }
      return 0;
    }
  }
}
