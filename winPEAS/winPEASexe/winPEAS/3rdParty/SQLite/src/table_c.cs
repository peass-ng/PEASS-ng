using System;
using System.Diagnostics;

using i64 = System.Int64;

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
    ** This file contains the sqlite3_get_table() and //sqlite3_free_table()
    ** interface routines.  These are just wrappers around the main
    ** interface routine of sqlite3_exec().
    **
    ** These routines are in a separate files so that they will not be linked
    ** if they are not used.
    **
    ** $Id: table.c,v 1.39 2009/01/19 20:49:10 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
    //#include <stdlib.h>
    //#include <string.h>

#if !SQLITE_OMIT_GET_TABLE

/*
** This structure is used to pass data from sqlite3_get_table() through
** to the callback function is uses to build the result.
*/
class TabResult {
public string[] azResult;
public string zErrMsg;
public int nResult;
public int nAlloc;
public int nRow;
public int nColumn;
public int nData;
public int rc;
};

/*
** This routine is called once for each row in the result table.  Its job
** is to fill in the TabResult structure appropriately, allocating new
** memory as necessary.
*/
public static int sqlite3_get_table_cb( object pArg, i64 nCol, object Oargv, object Ocolv )
{
string[] argv = (string[])Oargv;
string[]colv = (string[])Ocolv;
TabResult p = (TabResult)pArg;
int need;
int i;
string z;

/* Make sure there is enough space in p.azResult to hold everything
** we need to remember from this invocation of the callback.
*/
if( p.nRow==0 && argv!=null ){
need = (int)nCol*2;
}else{
need = (int)nCol;
}
if( p.nData + need >= p.nAlloc ){
string[] azNew;
p.nAlloc = p.nAlloc*2 + need + 1;
azNew = new string[p.nAlloc];//sqlite3_realloc( p.azResult, sizeof(char*)*p.nAlloc );
if( azNew==null ) goto malloc_failed;
p.azResult = azNew;
}

/* If this is the first row, then generate an extra row containing
** the names of all columns.
*/
if( p.nRow==0 ){
p.nColumn = (int)nCol;
for(i=0; i<nCol; i++){
z = sqlite3_mprintf("%s", colv[i]);
if( z==null ) goto malloc_failed;
p.azResult[p.nData++ -1] = z;
}
}else if( p.nColumn!=nCol ){
//sqlite3_free(ref p.zErrMsg);
p.zErrMsg = sqlite3_mprintf(
"sqlite3_get_table() called with two or more incompatible queries"
);
p.rc = SQLITE_ERROR;
return 1;
}

/* Copy over the row data
*/
if( argv!=null ){
for(i=0; i<nCol; i++){
if( argv[i]==null ){
z = null;
}else{
int n = sqlite3Strlen30(argv[i])+1;
//z = sqlite3_malloc( n );
//if( z==0 ) goto malloc_failed;
z= argv[i];//memcpy(z, argv[i], n);
}
p.azResult[p.nData++ -1] = z;
}
p.nRow++;
}
return 0;

malloc_failed:
p.rc = SQLITE_NOMEM;
return 1;
}

/*
** Query the database.  But instead of invoking a callback for each row,
** malloc() for space to hold the result and return the entire results
** at the conclusion of the call.
**
** The result that is written to ***pazResult is held in memory obtained
** from malloc().  But the caller cannot free this memory directly.
** Instead, the entire table should be passed to //sqlite3_free_table() when
** the calling procedure is finished using it.
*/
public static int sqlite3_get_table(
sqlite3 db,               /* The database on which the SQL executes */
string zSql,              /* The SQL to be executed */
ref string[] pazResult,   /* Write the result table here */
ref int pnRow,            /* Write the number of rows in the result here */
ref int pnColumn,         /* Write the number of columns of result here */
ref string pzErrMsg       /* Write error messages here */
){
int rc;
TabResult res = new TabResult();

pazResult = null;
pnColumn = 0;
pnRow = 0;
pzErrMsg = "";
res.zErrMsg = "";
res.nResult = 0;
res.nRow = 0;
res.nColumn = 0;
res.nData = 1;
res.nAlloc = 20;
res.rc = SQLITE_OK;
res.azResult = new string[res.nAlloc];// sqlite3_malloc( sizeof( char* ) * res.nAlloc );
if( res.azResult==null ){
db.errCode = SQLITE_NOMEM;
return SQLITE_NOMEM;
}
res.azResult[0] = null;
rc = sqlite3_exec(db, zSql, (dxCallback) sqlite3_get_table_cb, res, ref pzErrMsg);
//Debug.Assert( sizeof(res.azResult[0])>= sizeof(res.nData) );
//res.azResult = SQLITE_INT_TO_PTR( res.nData );
if( (rc&0xff)==SQLITE_ABORT ){
//sqlite3_free_table(ref res.azResult[1] );
if( res.zErrMsg !=""){
if( pzErrMsg !=null ){
//sqlite3_free(ref pzErrMsg);
pzErrMsg = sqlite3_mprintf("%s",res.zErrMsg);
}
//sqlite3_free(ref res.zErrMsg);
}
db.errCode = res.rc;  /* Assume 32-bit assignment is atomic */
return res.rc;
}
//sqlite3_free(ref res.zErrMsg);
if( rc!=SQLITE_OK ){
//sqlite3_free_table(ref res.azResult[1]);
return rc;
}
if( res.nAlloc>res.nData ){
string[] azNew;
Array.Resize(ref res.azResult, res.nData-1);//sqlite3_realloc( res.azResult, sizeof(char*)*(res.nData+1) );
//if( azNew==null ){
//  //sqlite3_free_table(ref res.azResult[1]);
//  db.errCode = SQLITE_NOMEM;
//  return SQLITE_NOMEM;
//}
res.nAlloc = res.nData+1;
//res.azResult = azNew;
}
pazResult = res.azResult;
pnColumn = res.nColumn;
pnRow = res.nRow;
return rc;
}

/*
** This routine frees the space the sqlite3_get_table() malloced.
*/
static void //sqlite3_free_table(
ref string azResult            /* Result returned from from sqlite3_get_table() */
){
if( azResult !=null){
int i, n;
//azResult--;
//Debug.Assert( azResult!=0 );
//n = SQLITE_PTR_TO_INT(azResult[0]);
//for(i=1; i<n; i++){ if( azResult[i] ) //sqlite3_free(azResult[i]); }
//sqlite3_free(ref azResult);
}
}

#endif //* SQLITE_OMIT_GET_TABLE */
  }
}
