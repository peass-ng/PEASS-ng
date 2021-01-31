using System;
using System.Diagnostics;
using u8 = System.Byte;
using u32 = System.UInt32;
using sqlite3_int64 = System.Int64;

namespace winPEAS._3rdParty.SQLite.src
{
  using sqlite3_stmt = CSSQLite.Vdbe;

  public partial class CSSQLite
  {
    /*
    ** 2005 May 25
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains the implementation of the sqlite3_prepare()
    ** interface, and routines that contribute to loading the database schema
    ** from disk.
    **
    ** $Id: prepare.c,v 1.131 2009/08/06 17:43:31 drh Exp $
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
    ** Fill the InitData structure with an error message that indicates
    ** that the database is corrupt.
    */
    static void corruptSchema(
    InitData pData, /* Initialization context */
    string zObj,    /* Object being parsed at the point of error */
    string zExtra   /* Error information */
    )
    {
      sqlite3 db = pData.db;
      if ( /*  0 == db.mallocFailed && */  ( db.flags & SQLITE_RecoveryMode ) == 0 )
      {
        {
          if ( zObj == null ) zObj = "?";
          sqlite3SetString( ref  pData.pzErrMsg, db,
          "malformed database schema (%s)", zObj );
          if ( !String.IsNullOrEmpty( zExtra ) )
          {
            pData.pzErrMsg = sqlite3MAppendf( db, pData.pzErrMsg
              , "%s - %s", pData.pzErrMsg, zExtra );
          }
        }
        pData.rc = //db.mallocFailed != 0 ? SQLITE_NOMEM :
#if SQLITE_DEBUG
 SQLITE_CORRUPT_BKPT();
#else
SQLITE_CORRUPT;
#endif
      }
    }

    /*
    ** This is the callback routine for the code that initializes the
    ** database.  See sqlite3Init() below for additional information.
    ** This routine is also called from the OP_ParseSchema opcode of the VDBE.
    **
    ** Each callback contains the following information:
    **
    **     argv[0] = name of thing being created
    **     argv[1] = root page number for table or index. 0 for trigger or view.
    **     argv[2] = SQL text for the CREATE statement.
    **
    */
    static int sqlite3InitCallback( object pInit, sqlite3_int64 argc, object p2, object NotUsed )
    {
      string[] argv = (string[])p2;
      InitData pData = (InitData)pInit;
      sqlite3 db = pData.db;
      int iDb = pData.iDb;

      Debug.Assert( argc == 3 );
      UNUSED_PARAMETER2( NotUsed, argc );
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      DbClearProperty( db, iDb, DB_Empty );
      //if ( db.mallocFailed != 0 )
      //{
      //  corruptSchema( pData, argv[0], "" );
      //  return 1;
      //}

      Debug.Assert( iDb >= 0 && iDb < db.nDb );
      if ( argv == null ) return 0;   /* Might happen if EMPTY_RESULT_CALLBACKS are on */
      if ( argv[1] == null )
      {
        corruptSchema( pData, argv[0], "" );
      }
      else if ( argv[2] != null && argv[2].Length != 0 )
      {
        /* Call the parser to process a CREATE TABLE, INDEX or VIEW.
        ** But because db.init.busy is set to 1, no VDBE code is generated
        ** or executed.  All the parser does is build the internal data
        ** structures that describe the table, index, or view.
        */
        string zErr = "";
        int rc;
        Debug.Assert( db.init.busy != 0 );
        db.init.iDb = iDb;
        db.init.newTnum = atoi( argv[1] );
        db.init.orphanTrigger = 0;
        rc = sqlite3_exec( db, argv[2], null, null, ref zErr );
        db.init.iDb = 0;
        Debug.Assert( rc != SQLITE_OK || zErr == "" );
        if ( SQLITE_OK != rc )
        {
          if ( db.init.orphanTrigger!=0 )
          {
            Debug.Assert( iDb == 1 );
          }
          else
          {
            pData.rc = rc;
            if ( rc == SQLITE_NOMEM )
            {
              //        db.mallocFailed = 1;
            }
            else if ( rc != SQLITE_INTERRUPT && rc != SQLITE_LOCKED )
            {
              corruptSchema( pData, argv[0], zErr );
            }
          }          //sqlite3DbFree( db, ref zErr );
        }
      }
      else if ( argv[0] == null || argv[0] == "" )
      {
        corruptSchema( pData, null, null );
      }
      else
      {
        /* If the SQL column is blank it means this is an index that
        ** was created to be the PRIMARY KEY or to fulfill a UNIQUE
        ** constraint for a CREATE TABLE.  The index should have already
        ** been created when we processed the CREATE TABLE.  All we have
        ** to do here is record the root page number for that index.
        */
        Index pIndex;
        pIndex = sqlite3FindIndex( db, argv[0], db.aDb[iDb].zName );
        if ( pIndex == null )
        {
          /* This can occur if there exists an index on a TEMP table which
          ** has the same name as another index on a permanent index.  Since
          ** the permanent table is hidden by the TEMP table, we can also
          ** safely ignore the index on the permanent table.
          */
          /* Do Nothing */
          ;
        }
        else if ( sqlite3GetInt32( argv[1], ref pIndex.tnum ) == false )
        {
          corruptSchema( pData, argv[0], "invalid rootpage" );
        }
      }
      return 0;
    }

    /*
    ** Attempt to read the database schema and initialize internal
    ** data structures for a single database file.  The index of the
    ** database file is given by iDb.  iDb==0 is used for the main
    ** database.  iDb==1 should never be used.  iDb>=2 is used for
    ** auxiliary databases.  Return one of the SQLITE_ error codes to
    ** indicate success or failure.
    */
    static int sqlite3InitOne( sqlite3 db, int iDb, ref string pzErrMsg )
    {
      int rc;
      int i;
      int size;
      Table pTab;
      Db pDb;
      string[] azArg = new string[4];
      u32[] meta = new u32[5];
      InitData initData = new InitData();
      string zMasterSchema;
      string zMasterName = SCHEMA_TABLE( iDb );
      int openedTransaction = 0;

      /*
      ** The master database table has a structure like this
      */
      string master_schema =
      "CREATE TABLE sqlite_master(\n" +
      "  type text,\n" +
      "  name text,\n" +
      "  tbl_name text,\n" +
      "  rootpage integer,\n" +
      "  sql text\n" +
      ")"
      ;
#if !SQLITE_OMIT_TEMPDB
      string temp_master_schema =
      "CREATE TEMP TABLE sqlite_temp_master(\n" +
      "  type text,\n" +
      "  name text,\n" +
      "  tbl_name text,\n" +
      "  rootpage integer,\n" +
      "  sql text\n" +
      ")"
      ;
#else
//#define temp_master_schema 0
#endif

      Debug.Assert( iDb >= 0 && iDb < db.nDb );
      Debug.Assert( db.aDb[iDb].pSchema != null );
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      Debug.Assert( iDb == 1 || sqlite3BtreeHoldsMutex( db.aDb[iDb].pBt ) );

      /* zMasterSchema and zInitScript are set to point at the master schema
      ** and initialisation script appropriate for the database being
      ** initialised. zMasterName is the name of the master table.
      */
      if ( OMIT_TEMPDB == 0 && iDb == 1 )
      {
        zMasterSchema = temp_master_schema;
      }
      else
      {
        zMasterSchema = master_schema;
      }
      zMasterName = SCHEMA_TABLE( iDb );

      /* Construct the schema tables.  */
      azArg[0] = zMasterName;
      azArg[1] = "1";
      azArg[2] = zMasterSchema;
      azArg[3] = "";
      initData.db = db;
      initData.iDb = iDb;
      initData.rc = SQLITE_OK;
      initData.pzErrMsg = pzErrMsg;
      sqlite3SafetyOff( db );
      sqlite3InitCallback( initData, 3, azArg, null );
      sqlite3SafetyOn( db );
      if ( initData.rc != 0 )
      {
        rc = initData.rc;
        goto error_out;
      }
      pTab = sqlite3FindTable( db, zMasterName, db.aDb[iDb].zName );
      if ( ALWAYS( pTab ) )
      {
        pTab.tabFlags |= TF_Readonly;
      }

      /* Create a cursor to hold the database open
      */
      pDb = db.aDb[iDb];
      if ( pDb.pBt == null )
      {
        if ( OMIT_TEMPDB == 0 && ALWAYS( iDb == 1 ) )
        {
          DbSetProperty( db, 1, DB_SchemaLoaded );
        }
        return SQLITE_OK;
      }

      /* If there is not already a read-only (or read-write) transaction opened
      ** on the b-tree database, open one now. If a transaction is opened, it 
      ** will be closed before this function returns.  */
      sqlite3BtreeEnter( pDb.pBt );
      if ( !sqlite3BtreeIsInReadTrans( pDb.pBt ) )
      {
        rc = sqlite3BtreeBeginTrans( pDb.pBt, 0 );
        if ( rc != SQLITE_OK )
        {
          sqlite3SetString( ref pzErrMsg, db, "%s", sqlite3ErrStr( rc ) );
          goto initone_error_out;
        }
        openedTransaction = 1;
      }

      /* Get the database meta information.
      **
      ** Meta values are as follows:
      **    meta[0]   Schema cookie.  Changes with each schema change.
      **    meta[1]   File format of schema layer.
      **    meta[2]   Size of the page cache.
      **    meta[3]   Largest rootpage (auto/incr_vacuum mode)
      **    meta[4]   Db text encoding. 1:UTF-8 2:UTF-16LE 3:UTF-16BE
      **    meta[5]   User version
      **    meta[6]   Incremental vacuum mode
      **    meta[7]   unused
      **    meta[8]   unused
      **    meta[9]   unused
      **
      ** Note: The #defined SQLITE_UTF* symbols in sqliteInt.h correspond to
      ** the possible values of meta[BTREE_TEXT_ENCODING-1].
      */
      for ( i = 0 ; i < ArraySize( meta ) ; i++ )
      {
        sqlite3BtreeGetMeta( pDb.pBt, i + 1, ref meta[i] );
      }
      pDb.pSchema.schema_cookie = (int)meta[BTREE_SCHEMA_VERSION - 1];

      /* If opening a non-empty database, check the text encoding. For the
      ** main database, set sqlite3.enc to the encoding of the main database.
      ** For an attached db, it is an error if the encoding is not the same
      ** as sqlite3.enc.
      */
      if ( meta[BTREE_TEXT_ENCODING - 1] != 0 )
      {  /* text encoding */
        if ( iDb == 0 )
        {
          u8 encoding;
          /* If opening the main database, set ENC(db). */
          encoding = (u8)( meta[BTREE_TEXT_ENCODING - 1] & 3 );
          if ( encoding == 0 ) encoding = SQLITE_UTF8;
          db.aDb[0].pSchema.enc = encoding; //ENC( db ) = encoding;
          db.pDfltColl = sqlite3FindCollSeq( db, SQLITE_UTF8, "BINARY", 0 );
        }
        else
        {
          /* If opening an attached database, the encoding much match ENC(db) */
          if ( meta[BTREE_TEXT_ENCODING - 1] != ENC( db ) )
          {
            sqlite3SetString( ref pzErrMsg, db, "attached databases must use the same" +
            " text encoding as main database" );
            rc = SQLITE_ERROR;
            goto initone_error_out;
          }
        }
      }
      else
      {
        DbSetProperty( db, iDb, DB_Empty );
      }
      pDb.pSchema.enc = ENC( db );

      if ( pDb.pSchema.cache_size == 0 )
      {
        size = (int)meta[BTREE_DEFAULT_CACHE_SIZE - 1];
        if ( size == 0 ) { size = SQLITE_DEFAULT_CACHE_SIZE; }
        if ( size < 0 ) size = -size;
        pDb.pSchema.cache_size = size;
        sqlite3BtreeSetCacheSize( pDb.pBt, pDb.pSchema.cache_size );
      }

      /*
      ** file_format==1    Version 3.0.0.
      ** file_format==2    Version 3.1.3.  // ALTER TABLE ADD COLUMN
      ** file_format==3    Version 3.1.4.  // ditto but with non-NULL defaults
      ** file_format==4    Version 3.3.0.  // DESC indices.  Boolean constants
      */
      pDb.pSchema.file_format = (u8)meta[BTREE_FILE_FORMAT - 1];
      if ( pDb.pSchema.file_format == 0 )
      {
        pDb.pSchema.file_format = 1;
      }
      if ( pDb.pSchema.file_format > SQLITE_MAX_FILE_FORMAT )
      {
        sqlite3SetString( ref pzErrMsg, db, "unsupported file format" );
        rc = SQLITE_ERROR;
        goto initone_error_out;
      }

      /* Ticket #2804:  When we open a database in the newer file format,
      ** clear the legacy_file_format pragma flag so that a VACUUM will
      ** not downgrade the database and thus invalidate any descending
      ** indices that the user might have created.
      */
      if ( iDb == 0 && meta[BTREE_FILE_FORMAT - 1] >= 4 )
      {
        db.flags &= ~SQLITE_LegacyFileFmt;
      }

      /* Read the schema information out of the schema tables
      */
      Debug.Assert( db.init.busy != 0 );
      {
        string zSql;
        zSql = sqlite3MPrintf( db,
        "SELECT name, rootpage, sql FROM '%q'.%s",
        db.aDb[iDb].zName, zMasterName );
        sqlite3SafetyOff( db );
#if ! SQLITE_OMIT_AUTHORIZATION
{
int (*xAuth)(void*,int,const char*,const char*,const char*,const char*);
xAuth = db.xAuth;
db.xAuth = 0;
#endif
        rc = sqlite3_exec( db, zSql, (dxCallback)sqlite3InitCallback, initData, 0 );
        pzErrMsg = initData.pzErrMsg;
#if ! SQLITE_OMIT_AUTHORIZATION
db.xAuth = xAuth;
}
#endif
        if ( rc == SQLITE_OK ) rc = initData.rc;
        sqlite3SafetyOn( db );
        //sqlite3DbFree( db, ref zSql );
#if !SQLITE_OMIT_ANALYZE
        if ( rc == SQLITE_OK )
        {
          sqlite3AnalysisLoad( db, iDb );
        }
#endif
      }
      //if ( db.mallocFailed != 0 )
      //{
      //  rc = SQLITE_NOMEM;
      //  sqlite3ResetInternalSchema( db, 0 );
      //}
      if ( rc == SQLITE_OK || ( db.flags & SQLITE_RecoveryMode ) != 0 )
      {
        /* Black magic: If the SQLITE_RecoveryMode flag is set, then consider
        ** the schema loaded, even if errors occurred. In this situation the
        ** current sqlite3_prepare() operation will fail, but the following one
        ** will attempt to compile the supplied statement against whatever subset
        ** of the schema was loaded before the error occurred. The primary
        ** purpose of this is to allow access to the sqlite_master table
        ** even when its contents have been corrupted.
        */
        DbSetProperty( db, iDb, DB_SchemaLoaded );
        rc = SQLITE_OK;
      }
/* Jump here for an error that occurs after successfully allocating
** curMain and calling sqlite3BtreeEnter(). For an error that occurs
** before that point, jump to error_out.
*/
initone_error_out:
      if ( openedTransaction != 0 )
      {
        sqlite3BtreeCommit( pDb.pBt );
      }
      sqlite3BtreeLeave( pDb.pBt );

error_out:
      if ( rc == SQLITE_NOMEM || rc == SQLITE_IOERR_NOMEM )
      {
//        db.mallocFailed = 1;
      }
      return rc;
    }

    /*
    ** Initialize all database files - the main database file, the file
    ** used to store temporary tables, and any additional database files
    ** created using ATTACH statements.  Return a success code.  If an
    ** error occurs, write an error message into pzErrMsg.
    **
    ** After a database is initialized, the DB_SchemaLoaded bit is set
    ** bit is set in the flags field of the Db structure. If the database
    ** file was of zero-length, then the DB_Empty flag is also set.
    */
    static int sqlite3Init( sqlite3 db, ref string pzErrMsg )
    {
      int i, rc;
      bool commit_internal = !( ( db.flags & SQLITE_InternChanges ) != 0 );

      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      rc = SQLITE_OK;
      db.init.busy = 1;
      for ( i = 0 ; rc == SQLITE_OK && i < db.nDb ; i++ )
      {
        if ( DbHasProperty( db, i, DB_SchemaLoaded ) || i == 1 ) continue;
        rc = sqlite3InitOne( db, i, ref pzErrMsg );
        if ( rc != 0 )
        {
          sqlite3ResetInternalSchema( db, i );
        }
      }

      /* Once all the other databases have been initialised, load the schema
      ** for the TEMP database. This is loaded last, as the TEMP database
      ** schema may contain references to objects in other databases.
      */
#if !SQLITE_OMIT_TEMPDB
      if ( rc == SQLITE_OK && ALWAYS( db.nDb > 1 )
      && !DbHasProperty( db, 1, DB_SchemaLoaded ) )
      {
        rc = sqlite3InitOne( db, 1, ref pzErrMsg );
        if ( rc != 0 )
        {
          sqlite3ResetInternalSchema( db, 1 );
        }
      }
#endif

      db.init.busy = 0;
      if ( rc == SQLITE_OK && commit_internal )
      {
        sqlite3CommitInternalChanges( db );
      }

      return rc;
    }

    /*
    ** This routine is a no-op if the database schema is already initialised.
    ** Otherwise, the schema is loaded. An error code is returned.
    */
    static int sqlite3ReadSchema( Parse pParse )
    {
      int rc = SQLITE_OK;
      sqlite3 db = pParse.db;
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      if ( 0 == db.init.busy )
      {
        rc = sqlite3Init( db, ref pParse.zErrMsg );
      }
      if ( rc != SQLITE_OK )
      {
        pParse.rc = rc;
        pParse.nErr++;
      }
      return rc;
    }


    /*
    ** Check schema cookies in all databases.  If any cookie is out
    ** of date set pParse->rc to SQLITE_SCHEMA.  If all schema cookies
    ** make no changes to pParse->rc.
    */
    static void schemaIsValid( Parse pParse )
    {
      sqlite3 db = pParse.db;
      int iDb;
      int rc;
      u32 cookie = 0;

      Debug.Assert( pParse.checkSchema!=0 );
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      for ( iDb = 0 ; iDb < db.nDb ; iDb++ )
      {
        int openedTransaction = 0;         /* True if a transaction is opened */
        Btree pBt = db.aDb[iDb].pBt;     /* Btree database to read cookie from */
        if ( pBt == null ) continue;

        /* If there is not already a read-only (or read-write) transaction opened
        ** on the b-tree database, open one now. If a transaction is opened, it 
        ** will be closed immediately after reading the meta-value. */
        if ( !sqlite3BtreeIsInReadTrans( pBt ) )
        {
          rc = sqlite3BtreeBeginTrans( pBt, 0 );
          //if ( rc == SQLITE_NOMEM || rc == SQLITE_IOERR_NOMEM )
          //{
          //    db.mallocFailed = 1;
          //}
          if ( rc != SQLITE_OK ) return;
          openedTransaction = 1;
        }

        /* Read the schema cookie from the database. If it does not match the 
        ** value stored as part of the in the in-memory schema representation,
        ** set Parse.rc to SQLITE_SCHEMA. */
        sqlite3BtreeGetMeta( pBt, BTREE_SCHEMA_VERSION, ref cookie );
        if ( cookie != db.aDb[iDb].pSchema.schema_cookie )
        {
          pParse.rc = SQLITE_SCHEMA;
        }

        /* Close the transaction, if one was opened. */
        if ( openedTransaction!=0 )
        {
          sqlite3BtreeCommit( pBt );
        }
      }
    }

    /*
    ** Convert a schema pointer into the iDb index that indicates
    ** which database file in db.aDb[] the schema refers to.
    **
    ** If the same database is attached more than once, the first
    ** attached database is returned.
    */
    static int sqlite3SchemaToIndex( sqlite3 db, Schema pSchema )
    {
      int i = -1000000;

      /* If pSchema is NULL, then return -1000000. This happens when code in
      ** expr.c is trying to resolve a reference to a transient table (i.e. one
      ** created by a sub-select). In this case the return value of this
      ** function should never be used.
      **
      ** We return -1000000 instead of the more usual -1 simply because using
      ** -1000000 as the incorrect index into db->aDb[] is much
      ** more likely to cause a segfault than -1 (of course there are assert()
      ** statements too, but it never hurts to play the odds).
      */
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      if ( pSchema != null )
      {
        for ( i = 0 ; ALWAYS( i < db.nDb ) ; i++ )
        {
          if ( db.aDb[i].pSchema == pSchema )
          {
            break;
          }
        }
        Debug.Assert( i >= 0 && i < db.nDb );
      }
      return i;
    }

    /*
    ** Compile the UTF-8 encoded SQL statement zSql into a statement handle.
    */
    static int sqlite3Prepare(
    sqlite3 db,               /* Database handle. */
    string zSql,              /* UTF-8 encoded SQL statement. */
    int nBytes,               /* Length of zSql in bytes. */
    int saveSqlFlag,          /* True to copy SQL text into the sqlite3_stmt */
    ref sqlite3_stmt ppStmt,  /* OUT: A pointer to the prepared statement */
    ref string pzTail         /* OUT: End of parsed string */
    )
    {
      Parse pParse;             /* Parsing context */
      string zErrMsg = "";      /* Error message */
      int rc = SQLITE_OK;       /* Result code */
      int i;                    /* Loop counter */

      /* Allocate the parsing context */
      pParse = new Parse();//sqlite3StackAllocZero(db, sizeof(*pParse));
      if ( pParse == null )
      {
        rc = SQLITE_NOMEM;
        goto end_prepare;
      }
      pParse.sLastToken.z = "";
      if ( sqlite3SafetyOn( db ) )
      {
        rc = SQLITE_MISUSE;
        goto end_prepare;
      }
      Debug.Assert( ppStmt == null );//  assert( ppStmt && *ppStmt==0 );
      //Debug.Assert( 0 == db.mallocFailed );
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );

      /* Check to verify that it is possible to get a read lock on all
      ** database schemas.  The inability to get a read lock indicates that
      ** some other database connection is holding a write-lock, which in
      ** turn means that the other connection has made uncommitted changes
      ** to the schema.
      **
      ** Were we to proceed and prepare the statement against the uncommitted
      ** schema changes and if those schema changes are subsequently rolled
      ** back and different changes are made in their place, then when this
      ** prepared statement goes to run the schema cookie would fail to detect
      ** the schema change.  Disaster would follow.
      **
      ** This thread is currently holding mutexes on all Btrees (because
      ** of the sqlite3BtreeEnterAll() in sqlite3LockAndPrepare()) so it
      ** is not possible for another thread to start a new schema change
      ** while this routine is running.  Hence, we do not need to hold
      ** locks on the schema, we just need to make sure nobody else is
      ** holding them.
      **
      ** Note that setting READ_UNCOMMITTED overrides most lock detection,
      ** but it does *not* override schema lock detection, so this all still
      ** works even if READ_UNCOMMITTED is set.
      */
      for ( i = 0 ; i < db.nDb ; i++ )
      {
        Btree pBt = db.aDb[i].pBt;
        if ( pBt != null )
        {
          Debug.Assert( sqlite3BtreeHoldsMutex( pBt ) );
          rc = sqlite3BtreeSchemaLocked( pBt );
          if ( rc != 0 )
          {
            string zDb = db.aDb[i].zName;
            sqlite3Error( db, rc, "database schema is locked: %s", zDb );
            sqlite3SafetyOff( db );
            testcase( db.flags & SQLITE_ReadUncommitted );
            goto end_prepare;
          }
        }
      }

      sqlite3VtabUnlockList( db );

      pParse.db = db;
      if ( nBytes >= 0 && ( nBytes == 0 || zSql[nBytes - 1] != 0 ) )
      {
        string zSqlCopy;
        int mxLen = db.aLimit[SQLITE_LIMIT_SQL_LENGTH];
        testcase( nBytes == mxLen );
        testcase( nBytes == mxLen + 1 );
        if ( nBytes > mxLen )
        {
          sqlite3Error( db, SQLITE_TOOBIG, "statement too long" );
          sqlite3SafetyOff( db );
          rc = sqlite3ApiExit( db, SQLITE_TOOBIG );
          goto end_prepare;
        }
        zSqlCopy = zSql.Substring( 0, nBytes );// sqlite3DbStrNDup(db, zSql, nBytes);
        if ( zSqlCopy != null )
        {
          sqlite3RunParser( pParse, zSqlCopy, ref zErrMsg );
          //sqlite3DbFree( db, ref zSqlCopy );
          //pParse->zTail = &zSql[pParse->zTail-zSqlCopy];
        }
        else
        {
          //pParse->zTail = &zSql[nBytes];
        }
      }
      else
      {
        sqlite3RunParser( pParse, zSql, ref zErrMsg );
      }

      //if ( db.mallocFailed != 0 )
      //{
      //  pParse.rc = SQLITE_NOMEM;
      //}
      if ( pParse.rc == SQLITE_DONE ) pParse.rc = SQLITE_OK;
      if ( pParse.checkSchema != 0)
      {
        schemaIsValid( pParse );
      }
      if ( pParse.rc == SQLITE_SCHEMA )
      {
        sqlite3ResetInternalSchema( db, 0 );
      }
      //if ( db.mallocFailed != 0 )
      //{
      //  pParse.rc = SQLITE_NOMEM;
      //}
      //if (pzTail != null)
      {
        pzTail = pParse.zTail == null ? "" : pParse.zTail.ToString();
      }
      rc = pParse.rc;
#if !SQLITE_OMIT_EXPLAIN
      if ( rc == SQLITE_OK && pParse.pVdbe != null && pParse.explain != 0 )
      {
        string[] azColName = new string[] {
"addr", "opcode", "p1", "p2", "p3", "p4", "p5", "comment",
"order", "from", "detail"
};
        int iFirst, mx;
        if ( pParse.explain == 2 )
        {
          sqlite3VdbeSetNumCols( pParse.pVdbe, 3 );
          iFirst = 8;
          mx = 11;
        }
        else
        {
          sqlite3VdbeSetNumCols( pParse.pVdbe, 8 );
          iFirst = 0;
          mx = 8;
        }
        for ( i = iFirst ; i < mx ; i++ )
        {
          sqlite3VdbeSetColName( pParse.pVdbe, i - iFirst, COLNAME_NAME,
                azColName[i], SQLITE_STATIC );
        }
      }
#endif

      if ( sqlite3SafetyOff( db ) )
      {
        rc = SQLITE_MISUSE;
      }

      Debug.Assert( db.init.busy == 0 || saveSqlFlag == 0 );
      if ( db.init.busy == 0 )
      {
        Vdbe pVdbe = pParse.pVdbe;
        sqlite3VdbeSetSql( pVdbe, zSql, (int)( zSql.Length - ( pParse.zTail == null ? 0 : pParse.zTail.Length ) ), saveSqlFlag );
      }
      if ( pParse.pVdbe != null && ( rc != SQLITE_OK /*|| db.mallocFailed != 0 */ ) )
      {
        sqlite3VdbeFinalize( pParse.pVdbe );
        Debug.Assert( ppStmt == null );
      }
      else
      {
        ppStmt = pParse.pVdbe;
      }

      if ( zErrMsg != "" )
      {
        sqlite3Error( db, rc, "%s", zErrMsg );
        //sqlite3DbFree( db, ref zErrMsg );
      }
      else
      {
        sqlite3Error( db, rc, 0 );
      }

end_prepare:

      //sqlite3StackFree( db, pParse );
      rc = sqlite3ApiExit( db, rc );
      Debug.Assert( ( rc & db.errMask ) == rc );
      return rc;
    }

    static int sqlite3LockAndPrepare(
    sqlite3 db,               /* Database handle. */
    string zSql,              /* UTF-8 encoded SQL statement. */
    int nBytes,               /* Length of zSql in bytes. */
    int saveSqlFlag,         /* True to copy SQL text into the sqlite3_stmt */
    ref sqlite3_stmt ppStmt,  /* OUT: A pointer to the prepared statement */
    ref string pzTail         /* OUT: End of parsed string */
    )
    {
      int rc;
      //  assert( ppStmt!=0 );
      ppStmt = null;
      if ( !sqlite3SafetyCheckOk( db ) )
      {
        return SQLITE_MISUSE;
      }
      sqlite3_mutex_enter( db.mutex );
      sqlite3BtreeEnterAll( db );
      rc = sqlite3Prepare( db, zSql, nBytes, saveSqlFlag, ref ppStmt, ref pzTail );
      if ( rc == SQLITE_SCHEMA )
      {
        sqlite3_finalize( ref ppStmt );
        rc = sqlite3Prepare( db, zSql, nBytes, saveSqlFlag, ref ppStmt, ref  pzTail );
      }
      sqlite3BtreeLeaveAll( db );
      sqlite3_mutex_leave( db.mutex );
      return rc;
    }

    /*
    ** Rerun the compilation of a statement after a schema change.
    **
    ** If the statement is successfully recompiled, return SQLITE_OK. Otherwise,
    ** if the statement cannot be recompiled because another connection has
    ** locked the sqlite3_master table, return SQLITE_LOCKED. If any other error
    ** occurs, return SQLITE_SCHEMA.
    */
    static int sqlite3Reprepare( Vdbe p )
    {
      int rc;
      sqlite3_stmt pNew = new sqlite3_stmt();
      string zSql;
      sqlite3 db;

      Debug.Assert( sqlite3_mutex_held( sqlite3VdbeDb( p ).mutex ) );
      zSql = sqlite3_sql( (sqlite3_stmt)p );
      Debug.Assert( zSql != null );  /* Reprepare only called for prepare_v2() statements */
      db = sqlite3VdbeDb( p );
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      string dummy = "";
      rc = sqlite3LockAndPrepare( db, zSql, -1, 1, ref pNew, ref dummy );
      if ( rc != 0 )
      {
        if ( rc == SQLITE_NOMEM )
        {
  //        db.mallocFailed = 1;
        }
        Debug.Assert( pNew == null );
        return ( rc == SQLITE_LOCKED ) ? SQLITE_LOCKED : SQLITE_SCHEMA;
      }
      else
      {
        Debug.Assert( pNew != null );
      }
      sqlite3VdbeSwap( (Vdbe)pNew, p );
      sqlite3TransferBindings( pNew, (sqlite3_stmt)p );
      sqlite3VdbeResetStepResult( (Vdbe)pNew );
      sqlite3VdbeFinalize( (Vdbe)pNew );
      return SQLITE_OK;
    }


    /*
    ** Two versions of the official API.  Legacy and new use.  In the legacy
    ** version, the original SQL text is not saved in the prepared statement
    ** and so if a schema change occurs, SQLITE_SCHEMA is returned by
    ** sqlite3_step().  In the new version, the original SQL text is retained
    ** and the statement is automatically recompiled if an schema change
    ** occurs.
    */
    public static int sqlite3_prepare(
    sqlite3 db,           /* Database handle. */
    string zSql,          /* UTF-8 encoded SQL statement. */
    int nBytes,           /* Length of zSql in bytes. */
    ref sqlite3_stmt ppStmt,  /* OUT: A pointer to the prepared statement */
    ref string pzTail         /* OUT: End of parsed string */
    )
    {
      int rc;
      rc = sqlite3LockAndPrepare( db, zSql, nBytes, 0, ref  ppStmt, ref pzTail );
      Debug.Assert( rc == SQLITE_OK || ppStmt == null );  /* VERIFY: F13021 */
      return rc;
    }
    public static int sqlite3_prepare_v2(
    sqlite3 db,               /* Database handle. */
    string zSql,              /* UTF-8 encoded SQL statement. */
    int nBytes,               /* Length of zSql in bytes. */
    ref sqlite3_stmt ppStmt,  /* OUT: A pointer to the prepared statement */
    int dummy /* ( No string passed) */
    )
    {
      string pzTail = null;
      int rc;
      rc = sqlite3LockAndPrepare( db, zSql, nBytes, 1, ref  ppStmt, ref pzTail );
      Debug.Assert( rc == SQLITE_OK || ppStmt == null );  /* VERIFY: F13021 */
      return rc;
    }
    public static int sqlite3_prepare_v2(
    sqlite3 db,               /* Database handle. */
    string zSql,              /* UTF-8 encoded SQL statement. */
    int nBytes,               /* Length of zSql in bytes. */
    ref sqlite3_stmt ppStmt,  /* OUT: A pointer to the prepared statement */
    ref string pzTail         /* OUT: End of parsed string */
    )
    {
      int rc;
      rc = sqlite3LockAndPrepare( db, zSql, nBytes, 1, ref  ppStmt, ref pzTail );
      Debug.Assert( rc == SQLITE_OK || ppStmt == null );  /* VERIFY: F13021 */
      return rc;
    }


#if ! SQLITE_OMIT_UTF16

/*
** Compile the UTF-16 encoded SQL statement zSql into a statement handle.
*/
static int sqlite3Prepare16(
sqlite3 db,              /* Database handle. */
string zSql,             /* UTF-8 encoded SQL statement. */
int nBytes,              /* Length of zSql in bytes. */
bool saveSqlFlag,         /* True to save SQL text into the sqlite3_stmt */
ref sqlite3_stmt ppStmt, /* OUT: A pointer to the prepared statement */
ref string pzTail        /* OUT: End of parsed string */
){
/* This function currently works by first transforming the UTF-16
** encoded string to UTF-8, then invoking sqlite3_prepare(). The
** tricky bit is figuring out the pointer to return in pzTail.
*/
string zSql8;
string zTail8 = "";
int rc = SQLITE_OK;

assert( ppStmt );
*ppStmt = 0;
if( !sqlite3SafetyCheckOk(db) ){
return SQLITE_MISUSE;
}
sqlite3_mutex_enter(db.mutex);
zSql8 = sqlite3Utf16to8(db, zSql, nBytes);
if( zSql8 !=""){
rc = sqlite3LockAndPrepare(db, zSql8, -1, saveSqlFlag, ref ppStmt, ref zTail8);
}

if( zTail8 !="" && pzTail !=""){
/* If sqlite3_prepare returns a tail pointer, we calculate the
** equivalent pointer into the UTF-16 string by counting the unicode
** characters between zSql8 and zTail8, and then returning a pointer
** the same number of characters into the UTF-16 string.
*/
Debugger.Break (); // TODO --
//  int chars_parsed = sqlite3Utf8CharLen(zSql8, (int)(zTail8-zSql8));
//  pzTail = (u8 *)zSql + sqlite3Utf16ByteLen(zSql, chars_parsed);
}
//sqlite3DbFree(db,ref zSql8);
rc = sqlite3ApiExit(db, rc);
sqlite3_mutex_leave(db.mutex);
return rc;
}

/*
** Two versions of the official API.  Legacy and new use.  In the legacy
** version, the original SQL text is not saved in the prepared statement
** and so if a schema change occurs, SQLITE_SCHEMA is returned by
** sqlite3_step().  In the new version, the original SQL text is retained
** and the statement is automatically recompiled if an schema change
** occurs.
*/
public static int sqlite3_prepare16(
sqlite3 db,               /* Database handle. */
string zSql,              /* UTF-8 encoded SQL statement. */
int nBytes,               /* Length of zSql in bytes. */
ref sqlite3_stmt ppStmt,  /* OUT: A pointer to the prepared statement */
ref string pzTail         /* OUT: End of parsed string */
){
int rc;
rc = sqlite3Prepare16(db,zSql,nBytes,false,ref ppStmt,ref pzTail);
Debug.Assert( rc==SQLITE_OK || ppStmt==null || ppStmt==null );  /* VERIFY: F13021 */
return rc;
}
public static int sqlite3_prepare16_v2(
sqlite3 db,               /* Database handle. */
string zSql,              /* UTF-8 encoded SQL statement. */
int nBytes,               /* Length of zSql in bytes. */
ref sqlite3_stmt ppStmt,  /* OUT: A pointer to the prepared statement */
ref string pzTail         /* OUT: End of parsed string */
)
{
int rc;
rc = sqlite3Prepare16(db,zSql,nBytes,true,ref ppStmt,ref pzTail);
Debug.Assert( rc==SQLITE_OK || ppStmt==null || ppStmt==null );  /* VERIFY: F13021 */
return rc;
}

#endif // * SQLITE_OMIT_UTF16 */
  }
}
