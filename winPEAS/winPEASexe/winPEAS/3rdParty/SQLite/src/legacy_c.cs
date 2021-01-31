using System.Diagnostics;

namespace winPEAS._3rdParty.SQLite.src
{
  using sqlite3_callback = CSSQLite.dxCallback;
  using sqlite3_stmt = CSSQLite.Vdbe;

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
    ** Main file for the SQLite library.  The routines in this file
    ** implement the programmer interface to the library.  Routines in
    ** other files are for internal use by SQLite and should not be
    ** accessed by users of the library.
    **
    ** $Id: legacy.c,v 1.35 2009/08/07 16:56:00 danielk1977 Exp $
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
    ** Execute SQL code.  Return one of the SQLITE_ success/failure
    ** codes.  Also write an error message into memory obtained from
    ** malloc() and make pzErrMsg point to that message.
    **
    ** If the SQL is a query, then for each row in the query result
    ** the xCallback() function is called.  pArg becomes the first
    ** argument to xCallback().  If xCallback=NULL then no callback
    ** is invoked, even for queries.
    */
    //OVERLOADS

    public static int sqlite3_exec(
    sqlite3 db,             /* The database on which the SQL executes */
    string zSql,            /* The SQL to be executed */
    int NoCallback, int NoArgs, int NoErrors
    )
    {
      string Errors = "";
      return sqlite3_exec( db, zSql, null, null, ref Errors );
    }

    public static int sqlite3_exec(
    sqlite3 db,             /* The database on which the SQL executes */
    string zSql,                /* The SQL to be executed */
    sqlite3_callback xCallback, /* Invoke this callback routine */
    object pArg,                /* First argument to xCallback() */
    int NoErrors
    )
    {
      string Errors = "";
      return sqlite3_exec( db, zSql, xCallback, pArg, ref Errors );
    }
    public static int sqlite3_exec(
    sqlite3 db,             /* The database on which the SQL executes */
    string zSql,                /* The SQL to be executed */
    sqlite3_callback xCallback, /* Invoke this callback routine */
    object pArg,                /* First argument to xCallback() */
    ref string pzErrMsg         /* Write error messages here */
    )
    {

      int rc = SQLITE_OK;         /* Return code */
      string zLeftover = "";      /* Tail of unprocessed SQL */
      sqlite3_stmt pStmt = null;  /* The current SQL statement */
      string[] azCols = null;     /* Names of result columns */
      int nRetry = 0;             /* Number of retry attempts */
      int callbackIsInit;         /* True if callback data is initialized */

      if ( zSql == null ) zSql = "";

      sqlite3_mutex_enter( db.mutex );
      sqlite3Error( db, SQLITE_OK, 0 );
      while ( ( rc == SQLITE_OK || ( rc == SQLITE_SCHEMA && ( ++nRetry ) < 2 ) ) && zSql != "" )
      {
        int nCol;
        string[] azVals = null;

        pStmt = null;
        rc = sqlite3_prepare( db, zSql, -1, ref pStmt, ref zLeftover );
        Debug.Assert( rc == SQLITE_OK || pStmt == null );
        if ( rc != SQLITE_OK )
        {
          continue;
        }
        if ( pStmt == null )
        {
          /* this happens for a comment or white-space */
          zSql = zLeftover;
          continue;
        }

        callbackIsInit = 0;
        nCol = sqlite3_column_count( pStmt );

        while ( true )
        {
          int i;
          rc = sqlite3_step( pStmt );

          /* Invoke the callback function if required */
          if ( xCallback != null && ( SQLITE_ROW == rc ||
          ( SQLITE_DONE == rc && callbackIsInit == 0
          && ( db.flags & SQLITE_NullCallback ) != 0 ) ) )
          {
            if ( 0 == callbackIsInit )
            {
              azCols = new string[nCol];//sqlite3DbMallocZero(db, 2*nCol*sizeof(const char*) + 1);
              if ( azCols == null )
              {
                goto exec_out;
              }
              for ( i = 0 ; i < nCol ; i++ )
              {
                azCols[i] = sqlite3_column_name( pStmt, i );
                /* sqlite3VdbeSetColName() installs column names as UTF8
                ** strings so there is no way for sqlite3_column_name() to fail. */
                Debug.Assert( azCols[i] != null );
              }
              callbackIsInit = 1;
            }
            if ( rc == SQLITE_ROW )
            {
              azVals = new string[nCol];// azCols[nCol];
              for ( i = 0 ; i < nCol ; i++ )
              {
                azVals[i] = sqlite3_column_text( pStmt, i );
                if ( azVals[i] == null && sqlite3_column_type( pStmt, i ) != SQLITE_NULL )
                {
          ////        db.mallocFailed = 1;
                  goto exec_out;
                }
              }
            }
            if ( xCallback( pArg, nCol, azVals, azCols ) != 0 )
            {
              rc = SQLITE_ABORT;
              sqlite3VdbeFinalize( pStmt );
              pStmt = null;
              sqlite3Error( db, SQLITE_ABORT, 0 );
              goto exec_out;
            }
          }

          if ( rc != SQLITE_ROW )
          {
            rc = sqlite3VdbeFinalize( pStmt );
            pStmt = null;
            if ( rc != SQLITE_SCHEMA )
            {
              nRetry = 0;
              if ( ( zSql = zLeftover ) != "" )
              {
                int zindex = 0;
                while ( zindex < zSql.Length && sqlite3Isspace( zSql[zindex] ) ) zindex++;
                if ( zindex != 0 ) zSql = zindex < zSql.Length ? zSql.Substring( zindex ) : "";
              }
            }
            break;
          }
        }

        //sqlite3DbFree( db, ref  azCols );
        azCols = null;
      }

exec_out:
      if ( pStmt != null ) sqlite3VdbeFinalize( pStmt );
      //sqlite3DbFree( db, ref  azCols );

      rc = sqlite3ApiExit( db, rc );
      if ( rc != SQLITE_OK && ALWAYS( rc == sqlite3_errcode( db ) ) && pzErrMsg != null )
      {
        //int nErrMsg = 1 + sqlite3Strlen30(sqlite3_errmsg(db));
        //pzErrMsg = sqlite3Malloc(nErrMsg);
        //if (pzErrMsg)
        //{
        //   memcpy(pzErrMsg, sqlite3_errmsg(db), nErrMsg);
        //}else{
        //rc = SQLITE_NOMEM;
        //sqlite3Error(db, SQLITE_NOMEM, 0);
        //}
        pzErrMsg = sqlite3_errmsg( db );
      }
      else if ( pzErrMsg != "" )
      {
        pzErrMsg = "";
      }

      Debug.Assert( ( rc & db.errMask ) == rc );
      sqlite3_mutex_leave( db.mutex );
      return rc;
    }
  }
}
