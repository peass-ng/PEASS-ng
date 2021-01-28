using System;
using System.Diagnostics;
using HANDLE = System.IntPtr;

namespace CS_SQLite3
{
  public partial class CSSQLite
  {
    /*
    ** 2006 June 7
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains code used to dynamically load extensions into
    ** the SQLite library.
    **
    ** $Id: loadext.c,v 1.60 2009/06/03 01:24:54 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */

#if !SQLITE_CORE
    //#define SQLITE_CORE 1  /* Disable the API redefinition in sqlite3ext.h */
    const int SQLITE_CORE = 1;
#endif
    //#include "sqlite3ext.h"
    //#include "sqliteInt.h"
    //#include <string.h>

#if !SQLITE_OMIT_LOAD_EXTENSION

    /*
** Some API routines are omitted when various features are
** excluded from a build of SQLite.  Substitute a NULL pointer
** for any missing APIs.
*/
#if !SQLITE_ENABLE_COLUMN_METADATA
    //# define sqlite3_column_database_name   0
    //# define sqlite3_column_database_name16 0
    //# define sqlite3_column_table_name      0
    //# define sqlite3_column_table_name16    0
    //# define sqlite3_column_origin_name     0
    //# define sqlite3_column_origin_name16   0
    //# define sqlite3_table_column_metadata  0
#endif

#if SQLITE_OMIT_AUTHORIZATION
    //# define sqlite3_set_authorizer         0
#endif

#if SQLITE_OMIT_UTF16
    //# define sqlite3_bind_text16            0
    //# define sqlite3_collation_needed16     0
    //# define sqlite3_column_decltype16      0
    //# define sqlite3_column_name16          0
    //# define sqlite3_column_text16          0
    //# define sqlite3_complete16             0
    //# define sqlite3_create_collation16     0
    //# define sqlite3_create_function16      0
    //# define sqlite3_errmsg16               0
    static string sqlite3_errmsg16( sqlite3 db ) { return ""; }
    //# define sqlite3_open16                 0
    //# define sqlite3_prepare16              0
    //# define sqlite3_prepare16_v2           0
    //# define sqlite3_result_error16         0
    //# define sqlite3_result_text16          0
    static void sqlite3_result_text16( sqlite3_context pCtx, string z, int n, dxDel xDel ) { }
    //# define sqlite3_result_text16be        0
    //# define sqlite3_result_text16le        0
    //# define sqlite3_value_text16           0
    //# define sqlite3_value_text16be         0
    //# define sqlite3_value_text16le         0
    //# define sqlite3_column_database_name16 0
    //# define sqlite3_column_table_name16    0
    //# define sqlite3_column_origin_name16   0
#endif

#if SQLITE_OMIT_COMPLETE
//# define sqlite3_complete 0
//# define sqlite3_complete16 0
#endif

#if SQLITE_OMIT_PROGRESS_CALLBACK
//# define sqlite3_progress_handler 0
static void sqlite3_progress_handler (sqlite3 db,       int nOps, dxProgress xProgress, object pArg){}
#endif

#if SQLITE_OMIT_VIRTUALTABLE
    //# define sqlite3_create_module 0
    //# define sqlite3_create_module_v2 0
    //# define sqlite3_declare_vtab 0
#endif

#if SQLITE_OMIT_SHARED_CACHE
    //# define sqlite3_enable_shared_cache 0
#endif

#if SQLITE_OMIT_TRACE
//# define sqlite3_profile       0
//# define sqlite3_trace         0
#endif

#if SQLITE_OMIT_GET_TABLE
    //# define //sqlite3_free_table    0
    //# define sqlite3_get_table     0
    public static int sqlite3_get_table(
    sqlite3 db,             /* An open database */
    string zSql,            /* SQL to be evaluated */
    ref string[] pazResult, /* Results of the query */
    ref int pnRow,          /* Number of result rows written here */
    ref int pnColumn,       /* Number of result columns written here */
    ref string pzErrmsg     /* Error msg written here */
    ) { return 0; }
#endif

#if SQLITE_OMIT_INCRBLOB
    //#define sqlite3_bind_zeroblob  0
    //#define sqlite3_blob_bytes     0
    //#define sqlite3_blob_close     0
    //#define sqlite3_blob_open      0
    //#define sqlite3_blob_read      0
    //#define sqlite3_blob_write     0
#endif

    /*
** The following structure contains pointers to all SQLite API routines.
** A pointer to this structure is passed into extensions when they are
** loaded so that the extension can make calls back into the SQLite
** library.
**
** When adding new APIs, add them to the bottom of this structure
** in order to preserve backwards compatibility.
**
** Extensions that use newer APIs should first call the
** sqlite3_libversion_number() to make sure that the API they
** intend to use is supported by the library.  Extensions should
** also check to make sure that the pointer to the function is
** not NULL before calling it.
*/
    static sqlite3_api_routines sqlite3Apis = new sqlite3_api_routines();
    //{
    //  sqlite3_aggregate_context,
#if !SQLITE_OMIT_DEPRECATED
    /  sqlite3_aggregate_count,
#else
//  0,
#endif
    //  sqlite3_bind_blob,
    //  sqlite3_bind_double,
    //  sqlite3_bind_int,
    //  sqlite3_bind_int64,
    //  sqlite3_bind_null,
    //  sqlite3_bind_parameter_count,
    //  sqlite3_bind_parameter_index,
    //  sqlite3_bind_parameter_name,
    //  sqlite3_bind_text,
    //  sqlite3_bind_text16,
    //  sqlite3_bind_value,
    //  sqlite3_busy_handler,
    //  sqlite3_busy_timeout,
    //  sqlite3_changes,
    //  sqlite3_close,
    //  sqlite3_collation_needed,
    //  sqlite3_collation_needed16,
    //  sqlite3_column_blob,
    //  sqlite3_column_bytes,
    //  sqlite3_column_bytes16,
    //  sqlite3_column_count,
    //  sqlite3_column_database_name,
    //  sqlite3_column_database_name16,
    //  sqlite3_column_decltype,
    //  sqlite3_column_decltype16,
    //  sqlite3_column_double,
    //  sqlite3_column_int,
    //  sqlite3_column_int64,
    //  sqlite3_column_name,
    //  sqlite3_column_name16,
    //  sqlite3_column_origin_name,
    //  sqlite3_column_origin_name16,
    //  sqlite3_column_table_name,
    //  sqlite3_column_table_name16,
    //  sqlite3_column_text,
    //  sqlite3_column_text16,
    //  sqlite3_column_type,
    //  sqlite3_column_value,
    //  sqlite3_commit_hook,
    //  sqlite3_complete,
    //  sqlite3_complete16,
    //  sqlite3_create_collation,
    //  sqlite3_create_collation16,
    //  sqlite3_create_function,
    //  sqlite3_create_function16,
    //  sqlite3_create_module,
    //  sqlite3_data_count,
    //  sqlite3_db_handle,
    //  sqlite3_declare_vtab,
    //  sqlite3_enable_shared_cache,
    //  sqlite3_errcode,
    //  sqlite3_errmsg,
    //  sqlite3_errmsg16,
    //  sqlite3_exec,
#if !SQLITE_OMIT_DEPRECATED
    //sqlite3_expired,
#else
//0,
#endif
    //  sqlite3_finalize,
    //  //sqlite3_free,
    //  //sqlite3_free_table,
    //  sqlite3_get_autocommit,
    //  sqlite3_get_auxdata,
    //  sqlite3_get_table,
    //  0,     /* Was sqlite3_global_recover(), but that function is deprecated */
    //  sqlite3_interrupt,
    //  sqlite3_last_insert_rowid,
    //  sqlite3_libversion,
    //  sqlite3_libversion_number,
    //  sqlite3_malloc,
    //  sqlite3_mprintf,
    //  sqlite3_open,
    //  sqlite3_open16,
    //  sqlite3_prepare,
    //  sqlite3_prepare16,
    //  sqlite3_profile,
    //  sqlite3_progress_handler,
    //  sqlite3_realloc,
    //  sqlite3_reset,
    //  sqlite3_result_blob,
    //  sqlite3_result_double,
    //  sqlite3_result_error,
    //  sqlite3_result_error16,
    //  sqlite3_result_int,
    //  sqlite3_result_int64,
    //  sqlite3_result_null,
    //  sqlite3_result_text,
    //  sqlite3_result_text16,
    //  sqlite3_result_text16be,
    //  sqlite3_result_text16le,
    //  sqlite3_result_value,
    //  sqlite3_rollback_hook,
    //  sqlite3_set_authorizer,
    //  sqlite3_set_auxdata,
    //  sqlite3_snprintf,
    //  sqlite3_step,
    //  sqlite3_table_column_metadata,
#if !SQLITE_OMIT_DEPRECATED
    //sqlite3_thread_cleanup,
#else
//  0,
#endif
    //  sqlite3_total_changes,
    //  sqlite3_trace,
#if !SQLITE_OMIT_DEPRECATED
    //sqlite3_transfer_bindings,
#else
//  0,
#endif
    //  sqlite3_update_hook,
    //  sqlite3_user_data,
    //  sqlite3_value_blob,
    //  sqlite3_value_bytes,
    //  sqlite3_value_bytes16,
    //  sqlite3_value_double,
    //  sqlite3_value_int,
    //  sqlite3_value_int64,
    //  sqlite3_value_numeric_type,
    //  sqlite3_value_text,
    //  sqlite3_value_text16,
    //  sqlite3_value_text16be,
    //  sqlite3_value_text16le,
    //  sqlite3_value_type,
    //  sqlite3_vmprintf,
    //  /*
    //  ** The original API set ends here.  All extensions can call any
    //  ** of the APIs above provided that the pointer is not NULL.  But
    //  ** before calling APIs that follow, extension should check the
    //  ** sqlite3_libversion_number() to make sure they are dealing with
    //  ** a library that is new enough to support that API.
    //  *************************************************************************
    //  */
    //  sqlite3_overload_function,

    //  /*
    //  ** Added after 3.3.13
    //  */
    //  sqlite3_prepare_v2,
    //  sqlite3_prepare16_v2,
    //  sqlite3_clear_bindings,

    //  /*
    //  ** Added for 3.4.1
    //  */
    //  sqlite3_create_module_v2,

    //  /*
    //  ** Added for 3.5.0
    //  */
    //  sqlite3_bind_zeroblob,
    //  sqlite3_blob_bytes,
    //  sqlite3_blob_close,
    //  sqlite3_blob_open,
    //  sqlite3_blob_read,
    //  sqlite3_blob_write,
    //  sqlite3_create_collation_v2,
    //  sqlite3_file_control,
    //  sqlite3_memory_highwater,
    //  sqlite3_memory_used,
#if SQLITE_MUTEX_OMIT
    //  0,
    //  0,
    //  0,
    //  0,
    //  0,
#else
//  sqlite3MutexAlloc,
//  sqlite3_mutex_enter,
//  sqlite3_mutex_free,
//  sqlite3_mutex_leave,
//  sqlite3_mutex_try,
#endif
    //  sqlite3_open_v2,
    //  sqlite3_release_memory,
    //  sqlite3_result_error_nomem,
    //  sqlite3_result_error_toobig,
    //  sqlite3_sleep,
    //  sqlite3_soft_heap_limit,
    //  sqlite3_vfs_find,
    //  sqlite3_vfs_register,
    //  sqlite3_vfs_unregister,

    //  /*
    //  ** Added for 3.5.8
    //  */
    //  sqlite3_threadsafe,
    //  sqlite3_result_zeroblob,
    //  sqlite3_result_error_code,
    //  sqlite3_test_control,
    //  sqlite3_randomness,
    //  sqlite3_context_db_handle,

    //  /*
    //  ** Added for 3.6.0
    //  */
    //  sqlite3_extended_result_codes,
    //  sqlite3_limit,
    //  sqlite3_next_stmt,
    //  sqlite3_sql,
    //  sqlite3_status,
    //};

    /*
    ** Attempt to load an SQLite extension library contained in the file
    ** zFile.  The entry point is zProc.  zProc may be 0 in which case a
    ** default entry point name (sqlite3_extension_init) is used.  Use
    ** of the default name is recommended.
    **
    ** Return SQLITE_OK on success and SQLITE_ERROR if something goes wrong.
    **
    ** If an error occurs and pzErrMsg is not 0, then fill pzErrMsg with
    ** error message text.  The calling function should free this memory
    ** by calling //sqlite3DbFree(db, ).
    */
    static int sqlite3LoadExtension(
    sqlite3 db,           /* Load the extension into this database connection */
    string zFile,         /* Name of the shared library containing extension */
    string zProc,         /* Entry point.  Use "sqlite3_extension_init" if 0 */
    ref string pzErrMsg   /* Put error message here if not 0 */
    )
    {
      sqlite3_vfs pVfs = db.pVfs;
      HANDLE handle;
      dxInit xInit; //int (*xInit)(sqlite3*,char**,const sqlite3_api_routines*);
      string zErrmsg = "";
      //object aHandle;
      const int nMsg = 300;
      if ( pzErrMsg != null ) pzErrMsg = null;


      /* Ticket #1863.  To avoid a creating security problems for older
      ** applications that relink against newer versions of SQLite, the
      ** ability to run load_extension is turned off by default.  One
      ** must call sqlite3_enable_load_extension() to turn on extension
      ** loading.  Otherwise you get the following error.
      */
      if ( ( db.flags & SQLITE_LoadExtension ) == 0 )
      {
        //if( pzErrMsg != null){
        pzErrMsg = sqlite3_mprintf( "not authorized" );
        //}
        return SQLITE_ERROR;
      }

      if ( zProc == null || zProc == "" )
      {
        zProc = "sqlite3_extension_init";
      }

      handle = sqlite3OsDlOpen( pVfs, zFile );
      if ( handle == IntPtr.Zero )
      {
        //    if( pzErrMsg ){
        zErrmsg = "";//zErrmsg = sqlite3StackAllocZero(db, nMsg);
        //if( zErrmsg !=null){
        sqlite3_snprintf( nMsg, ref zErrmsg,
        "unable to open shared library [%s]", zFile );
        sqlite3OsDlError( pVfs, nMsg - 1, ref zErrmsg );
        pzErrMsg = zErrmsg;// sqlite3DbStrDup( 0, zErrmsg );
        //sqlite3StackFree( db, zErrmsg );
        //}
        return SQLITE_ERROR;
      }
      //xInit = (int(*)(sqlite3*,char**,const sqlite3_api_routines*))
      //                 sqlite3OsDlSym(pVfs, handle, zProc);
      xInit = (dxInit)sqlite3OsDlSym( pVfs, handle, ref  zProc );
      Debugger.Break(); // TODO --
      //if( xInit==0 ){
      //  if( pzErrMsg ){
      //    zErrmsg = sqlite3StackAllocZero(db, nMsg);
      //    if( zErrmsg ){
      //      sqlite3_snprintf(nMsg, zErrmsg,
      //          "no entry point [%s] in shared library [%s]", zProc,zFile);
      //      sqlite3OsDlError(pVfs, nMsg-1, zErrmsg);
      //      *pzErrMsg = sqlite3DbStrDup(0, zErrmsg);
      //      //sqlite3StackFree(db, zErrmsg);
      //    }
      //    sqlite3OsDlClose(pVfs, handle);
      //  }
      //  return SQLITE_ERROR;
      //  }else if( xInit(db, ref zErrmsg, sqlite3Apis) ){
      ////    if( pzErrMsg !=null){
      //      pzErrMsg = sqlite3_mprintf("error during initialization: %s", zErrmsg);
      //    //}
      //    //sqlite3DbFree(db,ref zErrmsg);
      //    sqlite3OsDlClose(pVfs, ref handle);
      //    return SQLITE_ERROR;
      //  }

      //  /* Append the new shared library handle to the db.aExtension array. */
      //  aHandle = sqlite3DbMallocZero(db, sizeof(handle)*db.nExtension+1);
      //  if( aHandle==null ){
      //    return SQLITE_NOMEM;
      //  }
      //  if( db.nExtension>0 ){
      //    memcpy(aHandle, db.aExtension, sizeof(handle)*(db.nExtension));
      //  }
      //  //sqlite3DbFree(db,ref db.aExtension);
      //  db.aExtension = aHandle;

      //  db.aExtension[db.nExtension++] = handle;
      return SQLITE_OK;
    }

    public static int sqlite3_load_extension(
    sqlite3 db,          /* Load the extension into this database connection */
    string zFile,        /* Name of the shared library containing extension */
    string zProc,        /* Entry point.  Use "sqlite3_extension_init" if 0 */
    ref string pzErrMsg  /* Put error message here if not 0 */
    )
    {
      int rc;
      sqlite3_mutex_enter( db.mutex );
      rc = sqlite3LoadExtension( db, zFile, zProc, ref pzErrMsg );
      rc = sqlite3ApiExit( db, rc );
      sqlite3_mutex_leave( db.mutex );
      return rc;
    }

    /*
    ** Call this routine when the database connection is closing in order
    ** to clean up loaded extensions
    */
    static void sqlite3CloseExtensions( sqlite3 db )
    {
      int i;
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      for ( i = 0 ; i < db.nExtension ; i++ )
      {
        sqlite3OsDlClose( db.pVfs, (HANDLE)db.aExtension[i] );
      }
      //sqlite3DbFree( db, ref db.aExtension );
    }

    /*
    ** Enable or disable extension loading.  Extension loading is disabled by
    ** default so as not to open security holes in older applications.
    */
    public static int sqlite3_enable_load_extension( sqlite3 db, int onoff )
    {
      sqlite3_mutex_enter( db.mutex );
      if ( onoff != 0 )
      {
        db.flags |= SQLITE_LoadExtension;
      }
      else
      {
        db.flags &= ~SQLITE_LoadExtension;
      }
      sqlite3_mutex_leave( db.mutex );
      return SQLITE_OK;
    }

#endif //* SQLITE_OMIT_LOAD_EXTENSION */

    /*
** The auto-extension code added regardless of whether or not extension
** loading is supported.  We need a dummy sqlite3Apis pointer for that
** code if regular extension loading is not available.  This is that
** dummy pointer.
*/
#if SQLITE_OMIT_LOAD_EXTENSION
const sqlite3_api_routines sqlite3Apis = null;
#endif


    /*
** The following object holds the list of automatically loaded
** extensions.
**
** This list is shared across threads.  The SQLITE_MUTEX_STATIC_MASTER
** mutex must be held while accessing this list.
*/
    //typedef struct sqlite3AutoExtList sqlite3AutoExtList;
    public class sqlite3AutoExtList
    {
      public int nExt = 0;            /* Number of entries in aExt[] */
      public dxInit[] aExt = null;    /* Pointers to the extension init functions */
      public sqlite3AutoExtList( int nExt, dxInit[] aExt ) { this.nExt = nExt; this.aExt = aExt; }
    }
    static sqlite3AutoExtList sqlite3Autoext = new sqlite3AutoExtList( 0, null );
    /* The "wsdAutoext" macro will resolve to the autoextension
    ** state vector.  If writable static data is unsupported on the target,
    ** we have to locate the state vector at run-time.  In the more common
    ** case where writable static data is supported, wsdStat can refer directly
    ** to the "sqlite3Autoext" state vector declared above.
    */
#if SQLITE_OMIT_WSD
//# define wsdAutoextInit \
sqlite3AutoExtList *x = &GLOBAL(sqlite3AutoExtList,sqlite3Autoext)
//# define wsdAutoext x[0]
#else
    //# define wsdAutoextInit
    static void wsdAutoextInit() { }
    //# define wsdAutoext sqlite3Autoext
    static sqlite3AutoExtList wsdAutoext = sqlite3Autoext;
#endif

    /*
** Register a statically linked extension that is automatically
** loaded by every new database connection.
*/
    static int sqlite3_auto_extension( dxInit xInit )
    {
      int rc = SQLITE_OK;
#if !SQLITE_OMIT_AUTOINIT
      rc = sqlite3_initialize();
      if ( rc != 0 )
      {
        return rc;
      }
      else
#endif
      {
        int i;
#if SQLITE_THREADSAFE
sqlite3_mutex mutex = sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER );
#else
        sqlite3_mutex mutex = sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER ); // Need this since mutex_enter & leave are not MACROS under C#
#endif
        wsdAutoextInit();
        sqlite3_mutex_enter( mutex );
        for ( i = 0 ; i < wsdAutoext.nExt ; i++ )
        {
          if ( wsdAutoext.aExt[i] == xInit ) break;
        }
        //if( i==wsdAutoext.nExt ){
        //  int nByte = (wsdAutoext.nExt+1)*sizeof(wsdAutoext.aExt[0]);
        //  void **aNew;
        //  aNew = sqlite3_realloc(wsdAutoext.aExt, nByte);
        //  if( aNew==0 ){
        //    rc = SQLITE_NOMEM;
        //  }else{
        Array.Resize( ref wsdAutoext.aExt, wsdAutoext.nExt + 1 );//        wsdAutoext.aExt = aNew;
        wsdAutoext.aExt[wsdAutoext.nExt] = xInit;
        wsdAutoext.nExt++;
        //}
        sqlite3_mutex_leave( mutex );
        Debug.Assert( ( rc & 0xff ) == rc );
        return rc;
      }
    }

    /*
    ** Reset the automatic extension loading mechanism.
    */
    static void sqlite3_reset_auto_extension()
    {
#if !SQLITE_OMIT_AUTOINIT
      if ( sqlite3_initialize() == SQLITE_OK )
#endif
      {
#if SQLITE_THREADSAFE
sqlite3_mutex mutex = sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER );
#else
        sqlite3_mutex mutex = sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER ); // Need this since mutex_enter & leave are not MACROS under C#
#endif
        wsdAutoextInit();
        sqlite3_mutex_enter( mutex );
#if SQLITE_OMIT_WSD
//sqlite3_free( ref wsdAutoext.aExt );
wsdAutoext.aExt = null;
wsdAutoext.nExt = 0;
#else
        //sqlite3_free( ref sqlite3Autoext.aExt );
        sqlite3Autoext.aExt = null;
        sqlite3Autoext.nExt = 0;
#endif
        sqlite3_mutex_leave( mutex );
      }
    }

    /*
    ** Load all automatic extensions.
    **
    ** If anything goes wrong, set an error in the database connection.
    */
    static void sqlite3AutoLoadExtensions( sqlite3 db )
    {
      int i;
      bool go = true;
      dxInit xInit;//)(sqlite3*,char**,const sqlite3_api_routines*);

      wsdAutoextInit();
#if SQLITE_OMIT_WSD
if ( wsdAutoext.nExt == 0 )
#else
      if ( sqlite3Autoext.nExt == 0 )
#endif
      {
        /* Common case: early out without every having to acquire a mutex */
        return;
      }
      for ( i = 0 ; go ; i++ )
      {
        string zErrmsg = "";
#if SQLITE_THREADSAFE
sqlite3_mutex mutex = sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER );
#else
        sqlite3_mutex mutex = sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER ); // Need this since mutex_enter & leave are not MACROS under C#
#endif
        sqlite3_mutex_enter( mutex );
        if ( i >= wsdAutoext.nExt )
        {
          xInit = null;
          go = false;
        }
        else
        {
          xInit = (dxInit)
          wsdAutoext.aExt[i];
        }
        sqlite3_mutex_leave( mutex );
        zErrmsg = "";
        if ( xInit != null && xInit( db, ref zErrmsg, (sqlite3_api_routines)sqlite3Apis ) != 0 )
        {
          sqlite3Error( db, SQLITE_ERROR,
          "automatic extension loading failed: %s", zErrmsg );
          go = false;
        }
        //sqlite3DbFree( db, ref zErrmsg );
      }
    }
  }
}

