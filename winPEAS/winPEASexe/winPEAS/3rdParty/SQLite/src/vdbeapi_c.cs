using System;
using System.Diagnostics;
using System.Text;
using i64 = System.Int64;
using u8 = System.Byte;
using u16 = System.UInt16;
using u64 = System.UInt64;

namespace winPEAS._3rdParty.SQLite.src
{
  using Op = CSSQLite.VdbeOp;
  using sqlite3_value = CSSQLite.Mem;
  using sqlite3_stmt = CSSQLite.Vdbe;
  using sqlite_int64 = System.Int64;

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
    ** This file contains code use to implement APIs that are part of the
    ** VDBE.
    **
    ** $Id: vdbeapi.c,v 1.167 2009/06/25 01:47:12 drh Exp $
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

#if !SQLITE_OMIT_DEPRECATED
    /*
** Return TRUE (non-zero) of the statement supplied as an argument needs
** to be recompiled.  A statement needs to be recompiled whenever the
** execution environment changes in a way that would alter the program
** that sqlite3_prepare() generates.  For example, if new functions or
** collating sequences are registered or if an authorizer function is
** added or changed.
*/
    static int sqlite3_expired( sqlite3_stmt pStmt )
    {
      Vdbe p = (Vdbe)pStmt;
      return ( p == null || p.expired ) ? 1 : 0;
    }
#endif
    /*
** The following routine destroys a virtual machine that is created by
** the sqlite3_compile() routine. The integer returned is an SQLITE_
** success/failure code that describes the result of executing the virtual
** machine.
**
** This routine sets the error code and string returned by
** sqlite3_errcode(), sqlite3_errmsg() and sqlite3_errmsg16().
*/
    public static int sqlite3_finalize( ref sqlite3_stmt pStmt )
    {
      int rc;
      if ( pStmt == null )
      {
        rc = SQLITE_OK;
      }
      else
      {
        Vdbe v = pStmt;
        sqlite3 db = v.db;
#if  SQLITE_THREADSAFE
sqlite3_mutex mutex = v.db.mutex;
#endif
        sqlite3_mutex_enter( mutex );
        rc = sqlite3VdbeFinalize( v );
        rc = sqlite3ApiExit( db, rc );
        sqlite3_mutex_leave( mutex );
      }
      return rc;
    }

    /*
    ** Terminate the current execution of an SQL statement and reset it
    ** back to its starting state so that it can be reused. A success code from
    ** the prior execution is returned.
    **
    ** This routine sets the error code and string returned by
    ** sqlite3_errcode(), sqlite3_errmsg() and sqlite3_errmsg16().
    */
    public static int sqlite3_reset( sqlite3_stmt pStmt )
    {
      int rc;
      if ( pStmt == null )
      {
        rc = SQLITE_OK;
      }
      else
      {
        Vdbe v = (Vdbe)pStmt;
        sqlite3_mutex_enter( v.db.mutex );
        rc = sqlite3VdbeReset( v );
        sqlite3VdbeMakeReady( v, -1, 0, 0, 0 );
        Debug.Assert( ( rc & ( v.db.errMask ) ) == rc );
        rc = sqlite3ApiExit( v.db, rc );
        sqlite3_mutex_leave( v.db.mutex );
      }
      return rc;
    }

    /*
    ** Set all the parameters in the compiled SQL statement to NULL.
    */
    static int sqlite3_clear_bindings( sqlite3_stmt pStmt )
    {
      int i;
      int rc = SQLITE_OK;
      Vdbe p = (Vdbe)pStmt;
#if  SQLITE_THREADSAFE
sqlite3_mutex mutex = ( (Vdbe)pStmt ).db.mutex;
#endif
      sqlite3_mutex_enter( mutex );
      for ( i = 0 ; i < p.nVar ; i++ )
      {
        sqlite3VdbeMemRelease( p.aVar[i] );
        p.aVar[i].flags = MEM_Null;
      }
      sqlite3_mutex_leave( mutex );
      return rc;
    }


    /**************************** sqlite3_value_  *******************************
    ** The following routines extract information from a Mem or sqlite3_value
    ** structure.
    */
    public static byte[] sqlite3_value_blob( sqlite3_value pVal )
    {
      Mem p = pVal;
      if ( ( p.flags & ( MEM_Blob | MEM_Str ) ) != 0 )
      {
        sqlite3VdbeMemExpandBlob( p );
        if ( p.zBLOB == null && p.z != null )
        {
          if ( p.z.Length == 0 ) p.zBLOB = new byte[1];
          else
          {
            p.zBLOB = new byte[p.z.Length];
            for ( int i = 0 ; i < p.zBLOB.Length ; i++ ) p.zBLOB[i] = (u8)p.z[i];
          } p.z = null;
        }
        p.flags = (u16)( p.flags & ~MEM_Str );
        p.flags |= MEM_Blob;
        return p.zBLOB;
      }
      else
      {
        return sqlite3_value_text( pVal ) == null ? null : Encoding.UTF8.GetBytes( sqlite3_value_text( pVal ) );
      }
    }
    public static int sqlite3_value_bytes( sqlite3_value pVal )
    {
      return sqlite3ValueBytes( pVal, SQLITE_UTF8 );
    }
    public static int sqlite3_value_bytes16( sqlite3_value pVal )
    {
      return sqlite3ValueBytes( pVal, SQLITE_UTF16NATIVE );
    }
    public static double sqlite3_value_double( sqlite3_value pVal )
    {
      return sqlite3VdbeRealValue( pVal );
    }
    public static int sqlite3_value_int( sqlite3_value pVal )
    {
      return (int)sqlite3VdbeIntValue( pVal );
    }
    public static sqlite_int64 sqlite3_value_int64( sqlite3_value pVal )
    {
      return sqlite3VdbeIntValue( pVal );
    }
    public static string sqlite3_value_text( sqlite3_value pVal )
    {
      return sqlite3ValueText( pVal, SQLITE_UTF8 );
    }
#if  !SQLITE_OMIT_UTF16
static string sqlite3_value_text16(sqlite3_value pVal){
return sqlite3ValueText(pVal, SQLITE_UTF16NATIVE);
}
static string  sqlite3_value_text16be(sqlite3_value pVal){
return sqlite3ValueText(pVal, SQLITE_UTF16BE);
}
static string sqlite3_value_text16le(sqlite3_value pVal){
return sqlite3ValueText(pVal, SQLITE_UTF16LE);
}
#endif // * SQLITE_OMIT_UTF16 */
    public static int sqlite3_value_type( sqlite3_value pval )
    {
      return pval.type;
    }

    /**************************** sqlite3_result_  *******************************
    ** The following routines are used by user-defined functions to specify
    ** the function result.
    **
    ** The setStrOrError() funtion calls sqlite3VdbeMemSetStr() to store the
    ** result as a string or blob but if the string or blob is too large, it
    ** then sets the error code to SQLITE_TOOBIG
    */
    static void setResultStrOrError(
    sqlite3_context pCtx,   /* Function context */
    string z,               /* String pointer */
    int n,                  /* Bytes in string, or negative */
    u8 enc,                 /* Encoding of z.  0 for BLOBs */
    dxDel xDel //void (*xDel)(void*)     /* Destructor function */
    )
    {
      if ( sqlite3VdbeMemSetStr( pCtx.s, z, n, enc, xDel ) == SQLITE_TOOBIG )
      {
        sqlite3_result_error_toobig( pCtx );
      }
    }
    public static void sqlite3_result_blob(
    sqlite3_context pCtx,
    string z,
    int n,
    dxDel xDel
    )
    {
      Debug.Assert( n >= 0 );
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      setResultStrOrError( pCtx, z, n, 0, xDel );
    }
    public static void sqlite3_result_double( sqlite3_context pCtx, double rVal )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      sqlite3VdbeMemSetDouble( pCtx.s, rVal );
    }
    public static void sqlite3_result_error( sqlite3_context pCtx, string z, int n )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      setResultStrOrError( pCtx, z, n, SQLITE_UTF8, SQLITE_TRANSIENT );
      pCtx.isError = SQLITE_ERROR;
    }
#if  !SQLITE_OMIT_UTF16
//void sqlite3_result_error16(sqlite3_context pCtx, const void *z, int n){
//  Debug.Assert( sqlite3_mutex_held(pCtx.s.db.mutex) );
//  pCtx.isError = SQLITE_ERROR;
//  sqlite3VdbeMemSetStr(pCtx.s, z, n, SQLITE_UTF16NATIVE, SQLITE_TRANSIENT);
//}
#endif
    static void sqlite3_result_int( sqlite3_context pCtx, int iVal )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      sqlite3VdbeMemSetInt64( pCtx.s, (i64)iVal );
    }
    static void sqlite3_result_int64( sqlite3_context pCtx, i64 iVal )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      sqlite3VdbeMemSetInt64( pCtx.s, iVal );
    }
    static void sqlite3_result_null( sqlite3_context pCtx )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      sqlite3VdbeMemSetNull( pCtx.s );
    }

    public static void sqlite3_result_text(
    sqlite3_context pCtx,
    string z,
    int n,
    dxDel xDel
    )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      setResultStrOrError( pCtx, z, n, SQLITE_UTF8, xDel );
    }
#if  !SQLITE_OMIT_UTF16
void sqlite3_result_text16(
sqlite3_context pCtx,
string z,
int n,
dxDel xDel
){
Debug.Assert( sqlite3_mutex_held(pCtx.s.db.mutex) );
sqlite3VdbeMemSetStr(pCtx.s, z, n, SQLITE_UTF16NATIVE, xDel);
}
void sqlite3_result_text16be(
sqlite3_context pCtx,
string z,
int n,
dxDel xDel
){
Debug.Assert( sqlite3_mutex_held(pCtx.s.db.mutex) );
sqlite3VdbeMemSetStr(pCtx.s, z, n, SQLITE_UTF16BE, xDel);
}
void sqlite3_result_text16le(
sqlite3_context pCtx,
string z,
int n,
dxDel xDel
){
Debug.Assert( sqlite3_mutex_held(pCtx.s.db.mutex) );
sqlite3VdbeMemSetStr(pCtx.s, z, n, SQLITE_UTF16LE, xDel);
}
#endif // * SQLITE_OMIT_UTF16 */
    static void sqlite3_result_value( sqlite3_context pCtx, sqlite3_value pValue )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      sqlite3VdbeMemCopy( pCtx.s, pValue );
    }
    static void sqlite3_result_zeroblob( sqlite3_context pCtx, int n )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      sqlite3VdbeMemSetZeroBlob( pCtx.s, n );
    }
    static void sqlite3_result_error_code( sqlite3_context pCtx, int errCode )
    {
      pCtx.isError = errCode;
      if ( ( pCtx.s.flags & MEM_Null ) != 0 )
      {
        setResultStrOrError( pCtx, sqlite3ErrStr( errCode ), -1,
           SQLITE_UTF8, SQLITE_STATIC );
      }
    }

    /* Force an SQLITE_TOOBIG error. */
    static void sqlite3_result_error_toobig( sqlite3_context pCtx )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      pCtx.isError = SQLITE_ERROR;
      setResultStrOrError( pCtx, "string or blob too big", -1,
      SQLITE_UTF8, SQLITE_STATIC );
    }

    /* An SQLITE_NOMEM error. */
    static void sqlite3_result_error_nomem( sqlite3_context pCtx )
    {
      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      sqlite3VdbeMemSetNull( pCtx.s );
      pCtx.isError = SQLITE_NOMEM;
      //pCtx.s.db.mallocFailed = 1;
    }

    /*
    ** Execute the statement pStmt, either until a row of data is ready, the
    ** statement is completely executed or an error occurs.
    **
    ** This routine implements the bulk of the logic behind the sqlite_step()
    ** API.  The only thing omitted is the automatic recompile if a
    ** schema change has occurred.  That detail is handled by the
    ** outer sqlite3_step() wrapper procedure.
    */
    static int sqlite3Step( Vdbe p )
    {
      sqlite3 db;
      int rc;

      Debug.Assert( p != null );
      if ( p.magic != VDBE_MAGIC_RUN )
      {
        return SQLITE_MISUSE;
      }

      /* Assert that malloc() has not failed */
      db = p.db;
      //if ( db.mallocFailed != 0 )
      //{
      //  return SQLITE_NOMEM;
      //}

      if ( p.pc <= 0 && p.expired )
      {
        if ( ALWAYS( p.rc == SQLITE_OK ) )
        {
          p.rc = SQLITE_SCHEMA;
        }
        rc = SQLITE_ERROR;
        goto end_of_step;
      }
      if ( sqlite3SafetyOn( db ) )
      {
        p.rc = SQLITE_MISUSE;
        return SQLITE_MISUSE;
      }
      if ( p.pc < 0 )
      {
        /* If there are no other statements currently running, then
        ** reset the interrupt flag.  This prevents a call to sqlite3_interrupt
        ** from interrupting a statement that has not yet started.
        */
        if ( db.activeVdbeCnt == 0 )
        {
          db.u1.isInterrupted = false;
        }

#if  !SQLITE_OMIT_TRACE
        if ( db.xProfile != null && 0 == db.init.busy )
        {
          double rNow = 0;
          sqlite3OsCurrentTime( db.pVfs, ref rNow );
          p.startTime = (u64)( ( rNow - (int)rNow ) * 3600.0 * 24.0 * 1000000000.0 );
        }
#endif

        db.activeVdbeCnt++;
        if ( p.readOnly == false ) db.writeVdbeCnt++;
        p.pc = 0;
      }
#if  !SQLITE_OMIT_EXPLAIN
      if ( p.explain != 0 )
      {
        rc = sqlite3VdbeList( p );
      }
      else
#endif // * SQLITE_OMIT_EXPLAIN */
      {

        rc = sqlite3VdbeExec( p );
      }

      if ( sqlite3SafetyOff( db ) )
      {
        rc = SQLITE_MISUSE;
      }

#if  !SQLITE_OMIT_TRACE
      /* Invoke the profile callback if there is one
*/
      if ( rc != SQLITE_ROW && db.xProfile != null && 0 == db.init.busy && p.zSql != null )
      {
        double rNow = 0;
        u64 elapseTime;

        sqlite3OsCurrentTime( db.pVfs, ref rNow );
        elapseTime = (u64)( ( rNow - (int)rNow ) * 3600.0 * 24.0 * 1000000000.0 );
        elapseTime -= p.startTime;
        db.xProfile( db.pProfileArg, p.zSql, elapseTime );
      }
#endif

      db.errCode = rc;
      if ( SQLITE_NOMEM == sqlite3ApiExit( p.db, p.rc ) )
      {
        p.rc = SQLITE_NOMEM;
      }
end_of_step:
      /* At this point local variable rc holds the value that should be
      ** returned if this statement was compiled using the legacy
      ** sqlite3_prepare() interface. According to the docs, this can only
      ** be one of the values in the first Debug.Assert() below. Variable p.rc
      ** contains the value that would be returned if sqlite3_finalize()
      ** were called on statement p.
      */
      Debug.Assert( rc == SQLITE_ROW || rc == SQLITE_DONE || rc == SQLITE_ERROR
      || rc == SQLITE_BUSY || rc == SQLITE_MISUSE
      );
      Debug.Assert( p.rc != SQLITE_ROW && p.rc != SQLITE_DONE );
      if ( p.isPrepareV2 && rc != SQLITE_ROW && rc != SQLITE_DONE )
      {
        /* If this statement was prepared using sqlite3_prepare_v2(), and an
        ** error has occured, then return the error code in p.rc to the
        ** caller. Set the error code in the database handle to the same value.
        */
        rc = db.errCode = p.rc;
      }
      return ( rc & db.errMask );
    }

    /*
    ** This is the top-level implementation of sqlite3_step().  Call
    ** sqlite3Step() to do most of the work.  If a schema error occurs,
    ** call sqlite3Reprepare() and try again.
    */
    public static int sqlite3_step( sqlite3_stmt pStmt )
    {
      int rc = SQLITE_MISUSE;
      if ( pStmt != null )
      {
        int cnt = 0;
        Vdbe v = (Vdbe)pStmt;
        sqlite3 db = v.db;
        sqlite3_mutex_enter( db.mutex );
        while ( ( rc = sqlite3Step( v ) ) == SQLITE_SCHEMA
        && cnt++ < 5
        && ( rc = sqlite3Reprepare( v ) ) == SQLITE_OK )
        {
          sqlite3_reset( pStmt );
          v.expired = false;
        }
        if ( rc == SQLITE_SCHEMA && ALWAYS( v.isPrepareV2 ) && ALWAYS( db.pErr != null ) )
        {
          /* This case occurs after failing to recompile an sql statement.
          ** The error message from the SQL compiler has already been loaded
          ** into the database handle. This block copies the error message
          ** from the database handle into the statement and sets the statement
          ** program counter to 0 to ensure that when the statement is
          ** finalized or reset the parser error message is available via
          ** sqlite3_errmsg() and sqlite3_errcode().
          */
          string zErr = sqlite3_value_text( db.pErr );
          //sqlite3DbFree( db, ref v.zErrMsg );
          //if ( 0 == db.mallocFailed )
          {
            v.zErrMsg = zErr;// sqlite3DbStrDup(db, zErr);
          }
          //else
          //{
          //  v.zErrMsg = "";
          //  v.rc = SQLITE_NOMEM;
          //}
        }
        rc = sqlite3ApiExit( db, rc );
        sqlite3_mutex_leave( db.mutex );
      }
      return rc;
    }

    /*
    ** Extract the user data from a sqlite3_context structure and return a
    ** pointer to it.
    */
    static object sqlite3_user_data( sqlite3_context p )
    {
      Debug.Assert( p != null && p.pFunc != null );
      return p.pFunc.pUserData;
    }

    /*
    ** Extract the user data from a sqlite3_context structure and return a
    ** pointer to it.
    */
    static sqlite3 sqlite3_context_db_handle( sqlite3_context p )
    {
      Debug.Assert( p != null && p.pFunc != null );
      return p.s.db;
    }

    /*
    ** The following is the implementation of an SQL function that always
    ** fails with an error message stating that the function is used in the
    ** wrong context.  The sqlite3_overload_function() API might construct
    ** SQL function that use this routine so that the functions will exist
    ** for name resolution but are actually overloaded by the xFindFunction
    ** method of virtual tables.
    */
    static void sqlite3InvalidFunction(
    sqlite3_context context, /* The function calling context */
    int NotUsed,                /* Number of arguments to the function */
    sqlite3_value[] NotUsed2       /* Value of each argument */
    )
    {
      string zName = context.pFunc.zName;
      string zErr;
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      zErr = sqlite3_mprintf(
      "unable to use function %s in the requested context", zName );
      sqlite3_result_error( context, zErr, -1 );
      //sqlite3_free( ref zErr );
    }

    /*
    ** Allocate or return the aggregate context for a user function.  A new
    ** context is allocated on the first call.  Subsequent calls return the
    ** same context that was returned on prior calls.
    */
    public static Mem sqlite3_aggregate_context( sqlite3_context p, int nByte )
    {
      Mem pMem;
      Debug.Assert( p != null && p.pFunc != null && p.pFunc.xStep != null );
      Debug.Assert( sqlite3_mutex_held( p.s.db.mutex ) );
      pMem = p.pMem;
      if ( ( pMem.flags & MEM_Agg ) == 0 )
      {
        if ( nByte == 0 )
        {
          sqlite3VdbeMemReleaseExternal( pMem );
          pMem.flags = MEM_Null;
          pMem.z = null;
        }
        else
        {
          sqlite3VdbeMemGrow( pMem, nByte, 0 );
          pMem.flags = MEM_Agg;
          pMem.u.pDef = p.pFunc;
          if ( pMem.z != null )
          {
            pMem.z = null;
          }
          pMem._Mem = new Mem();
          pMem._Mem.flags = 0;
          pMem._SumCtx = new SumCtx();
        }
      }
      return pMem._Mem;
    }

    /*
    ** Return the auxillary data pointer, if any, for the iArg'th argument to
    ** the user-function defined by pCtx.
    */
    static string sqlite3_get_auxdata( sqlite3_context pCtx, int iArg )
    {
      VdbeFunc pVdbeFunc;

      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      pVdbeFunc = pCtx.pVdbeFunc;
      if ( null == pVdbeFunc || iArg >= pVdbeFunc.nAux || iArg < 0 )
      {
        return null;
      }
      return pVdbeFunc.apAux[iArg].pAux;
    }

    /*
    ** Set the auxillary data pointer and delete function, for the iArg'th
    ** argument to the user-function defined by pCtx. Any previous value is
    ** deleted by calling the delete function specified when it was set.
    */
    static void sqlite3_set_auxdata(
    sqlite3_context pCtx,
    int iArg,
    string pAux,
    dxDel xDelete//void (*xDelete)(void*)
    )
    {
      AuxData pAuxData;
      VdbeFunc pVdbeFunc;
      if ( iArg < 0 ) goto failed;

      Debug.Assert( sqlite3_mutex_held( pCtx.s.db.mutex ) );
      pVdbeFunc = pCtx.pVdbeFunc;
      if ( null == pVdbeFunc || pVdbeFunc.nAux <= iArg )
      {
        int nAux = ( pVdbeFunc != null ? pVdbeFunc.nAux : 0 );
        int nMalloc = iArg; ;//VdbeFunc+ sizeof(struct AuxData)*iArg;
        if ( pVdbeFunc == null )
        {
          //pVdbeFunc = (VdbeFunc)sqlite3DbRealloc( pCtx.s.db, pVdbeFunc, nMalloc );
          pVdbeFunc = new VdbeFunc();
          if ( null == pVdbeFunc )
          {
            goto failed;
          }
          pCtx.pVdbeFunc = pVdbeFunc;
        }
        pVdbeFunc.apAux[nAux] = new AuxData();//memset(pVdbeFunc.apAux[nAux], 0, sizeof(struct AuxData)*(iArg+1-nAux));
        pVdbeFunc.nAux = iArg + 1;
        pVdbeFunc.pFunc = pCtx.pFunc;
      }

      pAuxData = pVdbeFunc.apAux[iArg];
      if ( pAuxData.pAux != null && pAuxData.xDelete != null )
      {
        pAuxData.xDelete( ref pAuxData.pAux );
      }
      pAuxData.pAux = pAux;
      pAuxData.xDelete = xDelete;
      return;

failed:
      if ( xDelete != null )
      {
        xDelete( ref pAux );
      }
    }

#if !SQLITE_OMIT_DEPRECATED
    /*
** Return the number of times the Step function of a aggregate has been
** called.
**
** This function is deprecated.  Do not use it for new code.  It is
** provide only to avoid breaking legacy code.  New aggregate function
** implementations should keep their own counts within their aggregate
** context.
*/
    static int sqlite3_aggregate_count( sqlite3_context p )
    {
      Debug.Assert( p != null && p.pMem != null && p.pFunc != null && p.pFunc.xStep != null );
      return p.pMem.n;
    }
#endif

    /*
** Return the number of columns in the result set for the statement pStmt.
*/
    public static int sqlite3_column_count( sqlite3_stmt pStmt )
    {
      Vdbe pVm = pStmt;
      return pVm != null ? (int)pVm.nResColumn : 0;
    }

    /*
    ** Return the number of values available from the current row of the
    ** currently executing statement pStmt.
    */
    public static int sqlite3_data_count( sqlite3_stmt pStmt )
    {
      Vdbe pVm = pStmt;
      if ( pVm == null || pVm.pResultSet == null ) return 0;
      return pVm.nResColumn;
    }


    /*
    ** Check to see if column iCol of the given statement is valid.  If
    ** it is, return a pointer to the Mem for the value of that column.
    ** If iCol is not valid, return a pointer to a Mem which has a value
    ** of NULL.
    */
    static Mem columnMem( sqlite3_stmt pStmt, int i )
    {
      Vdbe pVm;
      int vals;
      Mem pOut;

      pVm = (Vdbe)pStmt;
      if ( pVm != null && pVm.pResultSet != null && i < pVm.nResColumn && i >= 0 )
      {
        sqlite3_mutex_enter( pVm.db.mutex );
        vals = sqlite3_data_count( pStmt );
        pOut = pVm.pResultSet[i];
      }
      else
      {
        /* If the value passed as the second argument is out of range, return
        ** a pointer to the following static Mem object which contains the
        ** value SQL NULL. Even though the Mem structure contains an element
        ** of type i64, on certain architecture (x86) with certain compiler
        ** switches (-Os), gcc may align this Mem object on a 4-byte boundary
        ** instead of an 8-byte one. This all works fine, except that when
        ** running with SQLITE_DEBUG defined the SQLite code sometimes assert()s
        ** that a Mem structure is located on an 8-byte boundary. To prevent
        ** this assert() from failing, when building with SQLITE_DEBUG defined
        ** using gcc, force nullMem to be 8-byte aligned using the magical
        ** __attribute__((aligned(8))) macro.  */
        //    Mem nullMem
#if (SQLITE_DEBUG) && (__GNUC__)
__attribute__((aligned(8)))
#endif
        //
        Mem nullMem = new Mem();//    static const Mem nullMem = {{0}, (double)0, 0, "", 0, MEM_Null, SQLITE_NULL, 0, 0, 0 };

        if ( pVm != null && ALWAYS( pVm.db != null ) )
        {
          sqlite3_mutex_enter( pVm.db.mutex );
          sqlite3Error( pVm.db, SQLITE_RANGE, 0 );
        }
        pOut = (Mem)nullMem;
      }
      return pOut;
    }

    /*
    ** This function is called after invoking an sqlite3_value_XXX function on a
    ** column value (i.e. a value returned by evaluating an SQL expression in the
    ** select list of a SELECT statement) that may cause a malloc() failure. If
    ** malloc() has failed, the threads mallocFailed flag is cleared and the result
    ** code of statement pStmt set to SQLITE_NOMEM.
    **
    ** Specifically, this is called from within:
    **
    **     sqlite3_column_int()
    **     sqlite3_column_int64()
    **     sqlite3_column_text()
    **     sqlite3_column_text16()
    **     sqlite3_column_real()
    **     sqlite3_column_bytes()
    **     sqlite3_column_bytes16()
    **
    ** But not for sqlite3_column_blob(), which never calls malloc().
    */
    static void columnMallocFailure( sqlite3_stmt pStmt )
    {
      /* If malloc() failed during an encoding conversion within an
      ** sqlite3_column_XXX API, then set the return code of the statement to
      ** SQLITE_NOMEM. The next call to _step() (if any) will return SQLITE_ERROR
      ** and _finalize() will return NOMEM.
      */
      Vdbe p = pStmt;
      if ( p != null )
      {
        p.rc = sqlite3ApiExit( p.db, p.rc );
        sqlite3_mutex_leave( p.db.mutex );
      }
    }

    /**************************** sqlite3_column_  *******************************
    ** The following routines are used to access elements of the current row
    ** in the result set.
    */
    public static byte[] sqlite3_column_blob( sqlite3_stmt pStmt, int i )
    {
      byte[] val;
      val = sqlite3_value_blob( columnMem( pStmt, i ) );
      /* Even though there is no encoding conversion, value_blob() might
      ** need to call malloc() to expand the result of a zeroblob()
      ** expression.
      */
      columnMallocFailure( pStmt );
      return val;
    }
    static int sqlite3_column_bytes( sqlite3_stmt pStmt, int i )
    {
      int val = sqlite3_value_bytes( columnMem( pStmt, i ) );
      columnMallocFailure( pStmt );
      return val;
    }
    static int sqlite3_column_bytes16( sqlite3_stmt pStmt, int i )
    {
      int val = sqlite3_value_bytes16( columnMem( pStmt, i ) );
      columnMallocFailure( pStmt );
      return val;
    }
    public static double sqlite3_column_double(sqlite3_stmt pStmt, int i)
    {
      double val = sqlite3_value_double( columnMem( pStmt, i ) );
      columnMallocFailure( pStmt );
      return val;
    }
    public static int sqlite3_column_int( sqlite3_stmt pStmt, int i )
    {
      int val = sqlite3_value_int( columnMem( pStmt, i ) );
      columnMallocFailure( pStmt );
      return val;
    }
    public static sqlite_int64 sqlite3_column_int64( sqlite3_stmt pStmt, int i )
    {
      sqlite_int64 val = sqlite3_value_int64( columnMem( pStmt, i ) );
      columnMallocFailure( pStmt );
      return val;
    }
    public static string sqlite3_column_text( sqlite3_stmt pStmt, int i )
    {
      string val = sqlite3_value_text( columnMem( pStmt, i ) );
      columnMallocFailure( pStmt );
      if ( String.IsNullOrEmpty( val ) ) return null; return val;
    }
    static sqlite3_value sqlite3_column_value( sqlite3_stmt pStmt, int i )
    {
      Mem pOut = columnMem( pStmt, i );
      if ( ( pOut.flags & MEM_Static ) != 0 )
      {
        pOut.flags = (u16)( pOut.flags & ~MEM_Static );
        pOut.flags |= MEM_Ephem;
      }
      columnMallocFailure( pStmt );
      return (sqlite3_value)pOut;
    }
#if  !SQLITE_OMIT_UTF16
//const void *sqlite3_column_text16(sqlite3_stmt pStmt, int i){
//  const void *val = sqlite3_value_text16( columnMem(pStmt,i) );
//  columnMallocFailure(pStmt);
//  return val;
//}
#endif // * SQLITE_OMIT_UTF16 */
    public static int sqlite3_column_type( sqlite3_stmt pStmt, int i )
    {
      int iType = sqlite3_value_type( columnMem( pStmt, i ) );
      columnMallocFailure( pStmt );
      return iType;
    }

    /* The following function is experimental and subject to change or
    ** removal */
    /*int sqlite3_column_numeric_type(sqlite3_stmt pStmt, int i){
    **  return sqlite3_value_numeric_type( columnMem(pStmt,i) );
    **}
    */

    /*
    ** Convert the N-th element of pStmt.pColName[] into a string using
    ** xFunc() then return that string.  If N is out of range, return 0.
    **
    ** There are up to 5 names for each column.  useType determines which
    ** name is returned.  Here are the names:
    **
    **    0      The column name as it should be displayed for output
    **    1      The datatype name for the column
    **    2      The name of the database that the column derives from
    **    3      The name of the table that the column derives from
    **    4      The name of the table column that the result column derives from
    **
    ** If the result is not a simple column reference (if it is an expression
    ** or a constant) then useTypes 2, 3, and 4 return NULL.
    */
    static string columnName(
    sqlite3_stmt pStmt,
    int N,
    dxColname xFunc,
    int useType
    )
    {
      string ret = null;
      Vdbe p = pStmt;
      int n;
      sqlite3 db = p.db;

      Debug.Assert( db != null );

      n = sqlite3_column_count( pStmt );
      if ( N < n && N >= 0 )
      {
        N += useType * n;
        sqlite3_mutex_enter( db.mutex );
        //Debug.Assert( db.mallocFailed == 0 );
        ret = xFunc( p.aColName[N] );

        /* A malloc may have failed inside of the xFunc() call. If this
        ** is the case, clear the mallocFailed flag and return NULL.
        */
        //if ( db.mallocFailed != 0 )
        //{
        //  //db.mallocFailed = 0;
        //  ret = null;
        //}
        sqlite3_mutex_leave( db.mutex );
      }
      return ret;
    }

    /*
    ** Return the name of the Nth column of the result set returned by SQL
    ** statement pStmt.
    */
    public static string sqlite3_column_name( sqlite3_stmt pStmt, int N )
    {
      return columnName(
      pStmt, N, sqlite3_value_text, COLNAME_NAME );
    }
#if  !SQLITE_OMIT_UTF16
public static string sqlite3_column_name16(sqlite3_stmt pStmt, int N){
return columnName(
pStmt, N,  sqlite3_value_text16, COLNAME_NAME);
}
#endif

    /*
** Constraint:  If you have ENABLE_COLUMN_METADATA then you must
** not define OMIT_DECLTYPE.
*/
#if SQLITE_OMIT_DECLTYPE && SQLITE_ENABLE_COLUMN_METADATA
# error "Must not define both SQLITE_OMIT_DECLTYPE and SQLITE_ENABLE_COLUMN_METADATA"
#endif

#if !SQLITE_OMIT_DECLTYPE
    /*
** Return the column declaration type (if applicable) of the 'i'th column
** of the result set of SQL statement pStmt.
*/
    public static string sqlite3_column_decltype( sqlite3_stmt pStmt, int N )
    {
      return columnName(
      pStmt, N, sqlite3_value_text, COLNAME_DECLTYPE );
    }
#if  !SQLITE_OMIT_UTF16
//const void *sqlite3_column_decltype16(sqlite3_stmt pStmt, int N){
//  return columnName(
//      pStmt, N, (const void*(*)(Mem*))sqlite3_value_text16, COLNAME_DECLTYPE);
//}
#endif // * SQLITE_OMIT_UTF16 */
#endif // * SQLITE_OMIT_DECLTYPE */

#if  SQLITE_ENABLE_COLUMN_METADATA

/*
** Return the name of the database from which a result column derives.
** NULL is returned if the result column is an expression or constant or
** anything else which is not an unabiguous reference to a database column.
*/
static byte[] sqlite3_column_database_name(sqlite3_stmt pStmt, int N){
return columnName(
pStmt, N, sqlite3_value_text, COLNAME_DATABASE);
}
#if !SQLITE_OMIT_UTF16
const void *sqlite3_column_database_name16(sqlite3_stmt pStmt, int N){
return columnName(
pStmt, N, (const void*(*)(Mem*))sqlite3_value_text16, COLNAME_DATABASE);
}
#endif //* SQLITE_OMIT_UTF16 */

/*
** Return the name of the table from which a result column derives.
** NULL is returned if the result column is an expression or constant or
** anything else which is not an unabiguous reference to a database column.
*/
static byte[] qlite3_column_table_name(sqlite3_stmt pStmt, int N){
return columnName(
pStmt, N, sqlite3_value_text, COLNAME_TABLE);
}
#if !SQLITE_OMIT_UTF16
const void *sqlite3_column_table_name16(sqlite3_stmt pStmt, int N){
return columnName(
pStmt, N, (const void*(*)(Mem*))sqlite3_value_text16, COLNAME_TABLE);
}
#endif //* SQLITE_OMIT_UTF16 */

/*
** Return the name of the table column from which a result column derives.
** NULL is returned if the result column is an expression or constant or
** anything else which is not an unabiguous reference to a database column.
*/
static byte[] sqlite3_column_origin_name(sqlite3_stmt pStmt, int N){
return columnName(
pStmt, N, sqlite3_value_text, COLNAME_COLUMN);
}
#if !SQLITE_OMIT_UTF16
const void *sqlite3_column_origin_name16(sqlite3_stmt pStmt, int N){
return columnName(
pStmt, N, (const void*(*)(Mem*))sqlite3_value_text16, COLNAME_COLUMN);
}
#endif ///* SQLITE_OMIT_UTF16 */
#endif // * SQLITE_ENABLE_COLUMN_METADATA */


    /******************************* sqlite3_bind_  ***************************
**
** Routines used to attach values to wildcards in a compiled SQL statement.
*/
    /*
    ** Unbind the value bound to variable i in virtual machine p. This is the
    ** the same as binding a NULL value to the column. If the "i" parameter is
    ** out of range, then SQLITE_RANGE is returned. Othewise SQLITE_OK.
    **
    ** A successful evaluation of this routine acquires the mutex on p.
    ** the mutex is released if any kind of error occurs.
    **
    ** The error code stored in database p.db is overwritten with the return
    ** value in any case.
    */
    static int vdbeUnbind( Vdbe p, int i )
    {
      Mem pVar;
      if ( p == null ) return SQLITE_MISUSE;
      sqlite3_mutex_enter( p.db.mutex );
      if ( p.magic != VDBE_MAGIC_RUN || p.pc >= 0 )
      {
        sqlite3Error( p.db, SQLITE_MISUSE, 0 );
        sqlite3_mutex_leave( p.db.mutex );
        return SQLITE_MISUSE;
      }
      if ( i < 1 || i > p.nVar )
      {
        sqlite3Error( p.db, SQLITE_RANGE, 0 );
        sqlite3_mutex_leave( p.db.mutex );
        return SQLITE_RANGE;
      }
      i--;
      pVar = p.aVar[i];
      sqlite3VdbeMemRelease( pVar );
      pVar.flags = MEM_Null;
      sqlite3Error( p.db, SQLITE_OK, 0 );
      return SQLITE_OK;
    }

    /*
    ** Bind a text or BLOB value.
    */
    static int bindText(
    sqlite3_stmt pStmt,   /* The statement to bind against */
    int i,                /* Index of the parameter to bind */
    string zData,         /* Pointer to the data to be bound */
    int nData,            /* Number of bytes of data to be bound */
    dxDel xDel,           /* Destructor for the data */
    u8 encoding          /* Encoding for the data */
    )
    {
      Vdbe p = pStmt;
      Mem pVar;
      int rc;

      rc = vdbeUnbind( p, i );
      if ( rc == SQLITE_OK )
      {
        if ( zData != null )
        {
          pVar = p.aVar[i - 1];
          rc = sqlite3VdbeMemSetStr( pVar, zData, nData, encoding, xDel );
          if ( rc == SQLITE_OK && encoding != 0 )
          {
            rc = sqlite3VdbeChangeEncoding( pVar, ENC( p.db ) );
          }
          sqlite3Error( p.db, rc, 0 );
          rc = sqlite3ApiExit( p.db, rc );
        }
        sqlite3_mutex_leave( p.db.mutex );
      }
      return rc;
    }


    /*
    ** Bind a blob value to an SQL statement variable.
    */
    public static int sqlite3_bind_blob(
    sqlite3_stmt pStmt,
    int i,
    string zData,
    int nData,
    dxDel xDel
    )
    {
      return bindText( pStmt, i, zData, nData, xDel, 0 );
    }

    public static int sqlite3_bind_double( sqlite3_stmt pStmt, int i, double rValue )
    {
      int rc;
      Vdbe p = pStmt;
      rc = vdbeUnbind( p, i );
      if ( rc == SQLITE_OK )
      {
        sqlite3VdbeMemSetDouble( p.aVar[i - 1], rValue );
        sqlite3_mutex_leave( p.db.mutex );
      }
      return rc;
    }

    public static int sqlite3_bind_int( sqlite3_stmt p, int i, int iValue )
    {
      return sqlite3_bind_int64( p, i, (i64)iValue );
    }

    public static int sqlite3_bind_int64( sqlite3_stmt pStmt, int i, sqlite_int64 iValue )
    {
      int rc;
      Vdbe p = pStmt;
      rc = vdbeUnbind( p, i );
      if ( rc == SQLITE_OK )
      {
        sqlite3VdbeMemSetInt64( p.aVar[i - 1], iValue );
        sqlite3_mutex_leave( p.db.mutex );
      }
      return rc;
    }
    public static int sqlite3_bind_null( sqlite3_stmt pStmt, int i )
    {
      int rc;
      Vdbe p = (Vdbe)pStmt;
      rc = vdbeUnbind( p, i );
      if ( rc == SQLITE_OK )
      {
        sqlite3_mutex_leave( p.db.mutex );
      } return rc;
    }

    public static int sqlite3_bind_text(
    sqlite3_stmt pStmt,
    int i,
    string zData,
    int nData,
    dxDel xDel
    )
    {
      return bindText( pStmt, i, zData, nData, xDel, SQLITE_UTF8 );
    }
#if  !SQLITE_OMIT_UTF16
static int sqlite3_bind_text16(
sqlite3_stmt pStmt,
int i,
string zData,
int nData,
dxDel xDel
){
return bindText(pStmt, i, zData, nData, xDel, SQLITE_UTF16NATIVE);
}
#endif // * SQLITE_OMIT_UTF16 */
    static int sqlite3_bind_value( sqlite3_stmt pStmt, int i, sqlite3_value pValue )
    {
      int rc;
      switch ( pValue.type )
      {
        case SQLITE_INTEGER:
          {
            rc = sqlite3_bind_int64( pStmt, i, pValue.u.i );
            break;
          }
        case SQLITE_FLOAT:
          {
            rc = sqlite3_bind_double( pStmt, i, pValue.r );
            break;
          }
        case SQLITE_BLOB:
          {
            if ( ( pValue.flags & MEM_Zero ) != 0 )
            {
              rc = sqlite3_bind_zeroblob( pStmt, i, pValue.u.nZero );
            }
            else
            {
              rc = sqlite3_bind_blob( pStmt, i, pValue.z, pValue.n, SQLITE_TRANSIENT );
            }
            break;
          }
        case SQLITE_TEXT:
          {
            rc = bindText( pStmt, i, pValue.z, pValue.n, SQLITE_TRANSIENT,
                      pValue.enc );
            break;
          }
        default:
          {
            rc = sqlite3_bind_null( pStmt, i );
            break;
          }
      }
      return rc;
    }

    static int sqlite3_bind_zeroblob( sqlite3_stmt pStmt, int i, int n )
    {
      int rc;
      Vdbe p = pStmt;
      rc = vdbeUnbind( p, i );
      if ( rc == SQLITE_OK )
      {
        sqlite3VdbeMemSetZeroBlob( p.aVar[i - 1], n );
        sqlite3_mutex_leave( p.db.mutex );
      }
      return rc;
    }

    /*
    ** Return the number of wildcards that can be potentially bound to.
    ** This routine is added to support DBD::SQLite.
    */
    static int sqlite3_bind_parameter_count( sqlite3_stmt pStmt )
    {
      Vdbe p = (Vdbe)pStmt;
      return ( p != null ) ? (int)p.nVar : 0;
    }

    /*
    ** Create a mapping from variable numbers to variable names
    ** in the Vdbe.azVar[] array, if such a mapping does not already
    ** exist.
    */
    static void createVarMap( Vdbe p )
    {
      if ( 0 == p.okVar )
      {
        int j;
        Op pOp;
        sqlite3_mutex_enter( p.db.mutex );
        /* The race condition here is harmless.  If two threads call this
        ** routine on the same Vdbe at the same time, they both might end
        ** up initializing the Vdbe.azVar[] array.  That is a little extra
        ** work but it results in the same answer.
        */
        p.azVar = new string[p.nOp];
        for ( j = 0 ; j < p.nOp ; j++ )//, pOp++ )
        {
          pOp = p.aOp[j];
          if ( pOp.opcode == OP_Variable )
          {
            Debug.Assert( pOp.p1 > 0 && pOp.p1 <= p.nVar );
            p.azVar[pOp.p1 - 1] = pOp.p4.z != null ? pOp.p4.z : "";
          }
        }
        p.okVar = 1;
        sqlite3_mutex_leave( p.db.mutex );
      }
    }

    /*
    ** Return the name of a wildcard parameter.  Return NULL if the index
    ** is out of range or if the wildcard is unnamed.
    **
    ** The result is always UTF-8.
    */
    static string sqlite3_bind_parameter_name( sqlite3_stmt pStmt, int i )
    {
      Vdbe p = (Vdbe)pStmt;
      if ( p == null || i < 1 || i > p.nVar )
      {
        return "";
      }
      createVarMap( p );
      return p.azVar[i - 1];
    }

    /*
    ** Given a wildcard parameter name, return the index of the variable
    ** with that name.  If there is no variable with the given name,
    ** return 0.
    */
    public static int sqlite3_bind_parameter_index( sqlite3_stmt pStmt, string zName )
    {
      Vdbe p = (Vdbe)pStmt;
      int i;
      if ( p == null )
      {
        return 0;
      }
      createVarMap( p );
      if ( zName != null && zName != "" )
      {
        for ( i = 0 ; i < p.nVar ; i++ )
        {
          string z = p.azVar[i];
          if ( z != null && z == zName )//&& strcmp(z, zName) == 0)
          {
            return i + 1;
          }
        }
      }
      return 0;
    }

    /*
    ** Transfer all bindings from the first statement over to the second.
    */
    static int sqlite3TransferBindings( sqlite3_stmt pFromStmt, sqlite3_stmt pToStmt )
    {
      Vdbe pFrom = (Vdbe)pFromStmt;
      Vdbe pTo = (Vdbe)pToStmt;
      int i;
      Debug.Assert( pTo.db == pFrom.db );
      Debug.Assert( pTo.nVar == pFrom.nVar );
      sqlite3_mutex_enter( pTo.db.mutex );
      for ( i = 0 ; i < pFrom.nVar ; i++ )
      {
        sqlite3VdbeMemMove( pTo.aVar[i], pFrom.aVar[i] );
      }
      sqlite3_mutex_leave( pTo.db.mutex );
      return SQLITE_OK;
    }

#if !SQLITE_OMIT_DEPRECATED
    /*
** Deprecated external interface.  Internal/core SQLite code
** should call sqlite3TransferBindings.
**
** Is is misuse to call this routine with statements from different
** database connections.  But as this is a deprecated interface, we
** will not bother to check for that condition.
**
** If the two statements contain a different number of bindings, then
** an SQLITE_ERROR is returned.  Nothing else can go wrong, so otherwise
** SQLITE_OK is returned.
*/
    static int sqlite3_transfer_bindings( sqlite3_stmt pFromStmt, sqlite3_stmt pToStmt )
    {
      Vdbe pFrom = (Vdbe)pFromStmt;
      Vdbe pTo = (Vdbe)pToStmt;
      if ( pFrom.nVar != pTo.nVar )
      {
        return SQLITE_ERROR;
      }
      return sqlite3TransferBindings( pFromStmt, pToStmt );
    }
#endif

    /*
** Return the sqlite3* database handle to which the prepared statement given
** in the argument belongs.  This is the same database handle that was
** the first argument to the sqlite3_prepare() that was used to create
** the statement in the first place.
*/
    static sqlite3 sqlite3_db_handle( sqlite3_stmt pStmt )
    {
      return pStmt != null ? ( (Vdbe)pStmt ).db : null;
    }

    /*
    ** Return a pointer to the next prepared statement after pStmt associated
    ** with database connection pDb.  If pStmt is NULL, return the first
    ** prepared statement for the database connection.  Return NULL if there
    ** are no more.
    */
    static sqlite3_stmt sqlite3_next_stmt( sqlite3 pDb, sqlite3_stmt pStmt )
    {
      sqlite3_stmt pNext;
      sqlite3_mutex_enter( pDb.mutex );
      if ( pStmt == null )
      {
        pNext = (sqlite3_stmt)pDb.pVdbe;
      }
      else
      {
        pNext = (sqlite3_stmt)( (Vdbe)pStmt ).pNext;
      }
      sqlite3_mutex_leave( pDb.mutex );
      return pNext;
    }
    /*
    ** Return the value of a status counter for a prepared statement
    */
    static int sqlite3_stmt_status( sqlite3_stmt pStmt, int op, int resetFlag )
    {
      Vdbe pVdbe = (Vdbe)pStmt;
      int v = pVdbe.aCounter[op - 1];
      if ( resetFlag != 0 ) pVdbe.aCounter[op - 1] = 0;
      return v;
    }
  }
}
