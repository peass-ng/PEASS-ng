    /*
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  Repository path : $HeadURL: https://sqlitecs.googlecode.com/svn/trunk/C%23SQLite/src/Delagates.cs $
    **  Revision        : $Revision$
    **  Last Change Date: $LastChangedDate: 2009-08-04 13:34:52 -0700 (Tue, 04 Aug 2009) $
    **  Last Changed By : $LastChangedBy: noah.hart $
    *************************************************************************
    */

using System.Text;

using HANDLE = System.IntPtr;

using i32 = System.Int32;
using u32 = System.UInt32;
using u64 = System.UInt64;

using sqlite3_int64 = System.Int64;

using Pgno = System.UInt32;

namespace CS_SQLite3
{
  using DbPage = CSSQLite.PgHdr;
  using sqlite3_stmt = CSSQLite.Vdbe;
  using sqlite3_value = CSSQLite.Mem;
  using sqlite3_pcache = CSSQLite.PCache1;

  public partial class CSSQLite
  {
    public delegate void dxAuth( object pAuthArg, int b, string c, string d, string e, string f );
    public delegate int dxBusy( object pBtShared, int iValue );
    public delegate void dxFreeAux( object pAuxArg );
    public delegate int dxCallback( object pCallbackArg, sqlite3_int64 argc, object p2, object p3 );
    public delegate void dxCollNeeded( object pCollNeededArg, sqlite3 db, int eTextRep, string collationName );
    public delegate int dxCommitCallback( object pCommitArg );
    public delegate int dxCompare( object pCompareArg, int size1, string Key1, int size2, string Key2 );
    public delegate bool dxCompare4( string Key1, int size1, string Key2, int size2 );
    public delegate void dxDel ( ref string pDelArg ); // needs ref
    public delegate void dxDelCollSeq( ref object pDelArg ); // needs ref
    public delegate void dxProfile( object pProfileArg, string msg, u64 time );
    public delegate int dxProgress( object pProgressArg );
    public delegate void dxRollbackCallback( object pRollbackArg );
    public delegate void dxTrace( object pTraceArg, string msg );
    public delegate void dxUpdateCallback( object pUpdateArg, int b, string c, string d, sqlite3_int64 e );

    /*
     * FUNCTIONS
     *
     */
    public delegate void dxFunc( sqlite3_context ctx, int intValue, sqlite3_value[] value );
    public delegate void dxStep( sqlite3_context ctx, int intValue, sqlite3_value[] value );
    public delegate void dxFinal( sqlite3_context ctx );
    //
    public delegate string dxColname( sqlite3_value pVal );
    public delegate int dxFuncBtree( Btree p );
    public delegate int dxExprTreeFunction( ref int pArg, Expr pExpr );
    public delegate int dxExprTreeFunction_NC( NameContext pArg, ref Expr pExpr );
    public delegate int dxExprTreeFunction_OBJ( object pArg, Expr pExpr );
    /*
       VFS Delegates
    */
    public delegate int dxClose( sqlite3_file File_ID );
    public delegate int dxCheckReservedLock( sqlite3_file File_ID, ref int pRes);
    public delegate int dxDeviceCharacteristics( sqlite3_file File_ID );
    public delegate int dxFileControl( sqlite3_file File_ID, int op, ref int pArgs );
    public delegate int dxFileSize( sqlite3_file File_ID, ref int size );
    public delegate int dxLock( sqlite3_file File_ID, int locktype );
    public delegate int dxRead( sqlite3_file File_ID, byte[] buffer, int amount, sqlite3_int64 offset );
    public delegate int dxSectorSize( sqlite3_file File_ID );
    public delegate int dxSync( sqlite3_file File_ID, int flags );
    public delegate int dxTruncate( sqlite3_file File_ID, sqlite3_int64 size );
    public delegate int dxUnlock( sqlite3_file File_ID, int locktype );
    public delegate int dxWrite( sqlite3_file File_ID, byte[] buffer, int amount, sqlite3_int64 offset );

    /*
         sqlite_vfs Delegates
     */
    public delegate int dxOpen( sqlite3_vfs vfs, string zName, sqlite3_file db, int flags, ref int pOutFlags );
    public delegate int dxDelete( sqlite3_vfs vfs, string zName, int syncDir );
    public delegate int dxAccess( sqlite3_vfs vfs, string zName, int flags, ref int pResOut );
    public delegate int dxFullPathname( sqlite3_vfs vfs, string zName, int nOut, StringBuilder zOut );
    public delegate HANDLE dxDlOpen( sqlite3_vfs vfs, string zFilename );
    public delegate int dxDlError( sqlite3_vfs vfs, int nByte, ref string zErrMsg );
    public delegate HANDLE dxDlSym( sqlite3_vfs vfs, HANDLE data, string zSymbol );
    public delegate int dxDlClose( sqlite3_vfs vfs, HANDLE data );
    public delegate int dxRandomness( sqlite3_vfs vfs, int nByte, ref byte[] buffer );
    public delegate int dxSleep( sqlite3_vfs vfs, int microseconds );
    public delegate int dxCurrentTime( sqlite3_vfs vfs, ref double currenttime );
    public delegate int dxGetLastError( sqlite3_vfs pVfs, int nBuf, ref string zBuf );

    /*
     * Pager Delegates
     */

    public delegate void dxDestructor( DbPage dbPage); /* Call this routine when freeing pages */
    public delegate int dxBusyHandler( object pBusyHandlerArg );
    public delegate void dxReiniter( DbPage dbPage );   /* Call this routine when reloading pages */

    public delegate void dxFreeSchema( Schema schema );

    //Module
    public delegate void dxDestroy( ref PgHdr pDestroyArg );
    public delegate int dxStress (object obj,PgHdr pPhHdr);

    //sqlite3_module
    public delegate int smdxCreate( sqlite3 db, object pAux, int argc, string constargv, ref sqlite3_vtab ppVTab, ref string pError );
    public delegate int smdxConnect( sqlite3 db, object pAux, int argc, string constargv, ref sqlite3_vtab ppVTab, ref string pError );
    public delegate int smdxBestIndex( sqlite3_vtab pVTab, ref sqlite3_index_info pIndex );
    public delegate int smdxDisconnect( sqlite3_vtab pVTab );
    public delegate int smdxDestroy( sqlite3_vtab pVTab );
    public delegate int smdxOpen( sqlite3_vtab pVTab, ref sqlite3_vtab_cursor ppCursor );
    public delegate int smdxClose( sqlite3_vtab_cursor pCursor );
    public delegate int smdxFilter( sqlite3_vtab_cursor pCursor, int idxNum, string idxStr, int argc, sqlite3_value[] argv );
    public delegate int smdxNext( sqlite3_vtab_cursor pCursor );
    public delegate int smdxEof( sqlite3_vtab_cursor pCursor );
    public delegate int smdxColumn( sqlite3_vtab_cursor pCursor, sqlite3_context p2, int p3 );
    public delegate int smdxRowid( sqlite3_vtab_cursor pCursor, sqlite3_int64 pRowid );
    public delegate int smdxUpdate( sqlite3_vtab pVTab, int p1, sqlite3_value[] p2, sqlite3_int64 p3 );
    public delegate int smdxBegin( sqlite3_vtab pVTab );
    public delegate int smdxSync( sqlite3_vtab pVTab );
    public delegate int smdxCommit( sqlite3_vtab pVTab );
    public delegate int smdxRollback( sqlite3_vtab pVTab );
    public delegate int smdxFindFunction( sqlite3_vtab pVtab, int nArg, string zName, object pxFunc, ref sqlite3_value[] ppArg );
    public delegate int smdxRename( sqlite3_vtab pVtab, string zNew );

    //AutoExtention
    public delegate int dxInit( sqlite3 db, ref string zMessage, sqlite3_api_routines sar );
#if !SQLITE_OMIT_VIRTUALTABLE
    public delegate int dmxCreate(sqlite3 db, object pAux, int argc, string p4, object argv, sqlite3_vtab ppVTab, char p7);
    public delegate int dmxConnect(sqlite3 db, object pAux, int argc, string p4, object argv, sqlite3_vtab ppVTab, char p7);
    public delegate int dmxBestIndex(sqlite3_vtab pVTab, sqlite3_index_info pIndexInfo);
    public delegate int dmxDisconnect(sqlite3_vtab pVTab);
    public delegate int dmxDestroy(sqlite3_vtab pVTab);
    public delegate int dmxOpen(sqlite3_vtab pVTab, sqlite3_vtab_cursor ppCursor);
    public delegate int dmxClose(sqlite3_vtab_cursor pCursor);
    public delegate int dmxFilter(sqlite3_vtab_cursor pCursor, int idmxNum, string idmxStr, int argc, sqlite3_value argv);
    public delegate int dmxNext(sqlite3_vtab_cursor pCursor);
    public delegate int dmxEof(sqlite3_vtab_cursor pCursor);
    public delegate int dmxColumn(sqlite3_vtab_cursor pCursor, sqlite3_context ctx, int i3);
    public delegate int dmxRowid(sqlite3_vtab_cursor pCursor, sqlite3_int64 pRowid);
    public delegate int dmxUpdate(sqlite3_vtab pVTab, int i2, sqlite3_value sv3, sqlite3_int64 v4);
    public delegate int dmxBegin(sqlite3_vtab pVTab);
    public delegate int dmxSync(sqlite3_vtab pVTab);
    public delegate int dmxCommit(sqlite3_vtab pVTab);
    public delegate int dmxRollback(sqlite3_vtab pVTab);
    public delegate int dmxFindFunction(sqlite3_vtab pVtab, int nArg, string zName);
    public delegate int dmxRename(sqlite3_vtab pVtab, string zNew);
#endif
    //Faults
    public delegate void void_function();

//Alarms
    public delegate void dxalarmCallback (object pData, sqlite3_int64 p1, int p2);

    //Mem Methods
    public delegate int dxMemInit (object o);
    public delegate void dxMemShutdown( object o );
    public delegate byte[] dxMalloc (int nSize);
    public delegate void dxFree( ref byte[]  pOld);
    public delegate byte[] dxRealloc( ref byte[] pOld, int nSize );
    public delegate int  dxSize (byte[] pArray);
    public delegate int dxRoundup( int nSize );

    //Mutex Methods
  public delegate int dxMutexInit();
  public delegate int dxMutexEnd( );
  public delegate   sqlite3_mutex dxMutexAlloc(int iNumber);
  public delegate  void dxMutexFree(sqlite3_mutex sm);
  public delegate   void dxMutexEnter(sqlite3_mutex sm);
  public delegate   int dxMutexTry(sqlite3_mutex sm);
  public delegate   void dxMutexLeave(sqlite3_mutex sm);
  public delegate   int dxMutexHeld(sqlite3_mutex sm);
  public delegate   int dxMutexNotheld(sqlite3_mutex sm);

    public delegate object dxColumn( sqlite3_stmt pStmt, int i );
    public delegate int dxColumn_I( sqlite3_stmt pStmt, int i );

  // Walker Methods
    public delegate int dxExprCallback (Walker W, ref Expr E);     /* Callback for expressions */
    public delegate int dxSelectCallback (Walker W, Select S);  /* Callback for SELECTs */


  // pcache Methods
    public delegate int dxPC_Init( object NotUsed );
    public delegate void dxPC_Shutdown( object NotUsed );
    public delegate  sqlite3_pcache dxPC_Create (int szPage, int bPurgeable);
    public delegate  void dxPC_Cachesize (sqlite3_pcache pCache, int nCachesize);
    public delegate  int dxPC_Pagecount (sqlite3_pcache pCache);
    public delegate PgHdr dxPC_Fetch( sqlite3_pcache pCache, u32 key, int createFlag );
    public delegate void dxPC_Unpin( sqlite3_pcache pCache, PgHdr p2, int discard );
    public delegate void dxPC_Rekey( sqlite3_pcache pCache, PgHdr p2, u32 oldKey, u32 newKey );
    public delegate void dxPC_Truncate( sqlite3_pcache pCache, u32 iLimit );
    public delegate  void dxPC_Destroy(ref sqlite3_pcache pCache);

    public delegate void dxIter(PgHdr p);
  }
}
