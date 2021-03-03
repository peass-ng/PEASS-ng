using System;
using System.Diagnostics;
using u8 = System.Byte;

namespace winPEAS._3rdParty.SQLite.src
{
  using sqlite3_value = CSSQLite.Mem;

  public partial class CSSQLite
  {
    /*
    ** 2003 April 6
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains code used to implement the ATTACH and DETACH commands.
    **
    ** $Id: attach.c,v 1.93 2009/05/31 21:21:41 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"

#if !SQLITE_OMIT_ATTACH
    /*
** Resolve an expression that was part of an ATTACH or DETACH statement. This
** is slightly different from resolving a normal SQL expression, because simple
** identifiers are treated as strings, not possible column names or aliases.
**
** i.e. if the parser sees:
**
**     ATTACH DATABASE abc AS def
**
** it treats the two expressions as literal strings 'abc' and 'def' instead of
** looking for columns of the same name.
**
** This only applies to the root node of pExpr, so the statement:
**
**     ATTACH DATABASE abc||def AS 'db2'
**
** will fail because neither abc or def can be resolved.
*/
    static int resolveAttachExpr( NameContext pName, Expr pExpr )
    {
      int rc = SQLITE_OK;
      if ( pExpr != null )
      {
        if ( pExpr.op != TK_ID )
        {
          rc = sqlite3ResolveExprNames( pName, ref pExpr );
          if ( rc == SQLITE_OK && sqlite3ExprIsConstant( pExpr ) == 0 )
          {
            sqlite3ErrorMsg( pName.pParse, "invalid name: \"%s\"", pExpr.u.zToken );
            return SQLITE_ERROR;
          }
        }
        else
        {
          pExpr.op = TK_STRING;
        }
      }
      return rc;
    }

    /*
    ** An SQL user-function registered to do the work of an ATTACH statement. The
    ** three arguments to the function come directly from an attach statement:
    **
    **     ATTACH DATABASE x AS y KEY z
    **
    **     SELECT sqlite_attach(x, y, z)
    **
    ** If the optional "KEY z" syntax is omitted, an SQL NULL is passed as the
    ** third argument.
    */
    static void attachFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] argv
    )
    {
      int i;
      int rc = 0;
      sqlite3 db = sqlite3_context_db_handle( context );
      string zName;
      string zFile;
      Db aNew = null;
      string zErrDyn = "";

      UNUSED_PARAMETER( NotUsed );

      zFile = argv[0].z != null && ( argv[0].z.Length > 0 ) ? sqlite3_value_text( argv[0] ) : "";
      zName = argv[1].z != null && ( argv[1].z.Length > 0 ) ? sqlite3_value_text( argv[1] ) : "";
      //if( zFile==null ) zFile = "";
      //if ( zName == null ) zName = "";


      /* Check for the following errors:
      **
      **     * Too many attached databases,
      **     * Transaction currently open
      **     * Specified database name already being used.
      */
      if ( db.nDb >= db.aLimit[SQLITE_LIMIT_ATTACHED] + 2 )
      {
        zErrDyn = sqlite3MPrintf( db, "too many attached databases - max %d",
        db.aLimit[SQLITE_LIMIT_ATTACHED]
        );
        goto attach_error;
      }
      if ( 0 == db.autoCommit )
      {
        zErrDyn = sqlite3MPrintf( db, "cannot ATTACH database within transaction" );
        goto attach_error;
      }
      for ( i = 0 ; i < db.nDb ; i++ )
      {
        string z = db.aDb[i].zName;
        Debug.Assert( z != null && zName != null );
        if ( sqlite3StrICmp( z, zName ) == 0 )
        {
          zErrDyn = sqlite3MPrintf( db, "database %s is already in use", zName );
          goto attach_error;
        }
      }

      /* Allocate the new entry in the db.aDb[] array and initialise the schema
      ** hash tables.
      */
      /* Allocate the new entry in the db.aDb[] array and initialise the schema
      ** hash tables.
      */
      //if( db.aDb==db.aDbStatic ){
      //  aNew = sqlite3DbMallocRaw(db, sizeof(db.aDb[0])*3 );
      //  if( aNew==0 ) return;
      //  memcpy(aNew, db.aDb, sizeof(db.aDb[0])*2);
      //}else {
      if ( db.aDb.Length <= db.nDb ) Array.Resize( ref db.aDb, db.nDb + 1 );//aNew = sqlite3DbRealloc(db, db.aDb, sizeof(db.aDb[0])*(db.nDb+1) );
      if ( db.aDb == null ) return;   // if( aNew==0 ) return;
      //}
      db.aDb[db.nDb] = new Db();//db.aDb = aNew;
      aNew = db.aDb[db.nDb];//memset(aNew, 0, sizeof(*aNew));
      //  memset(aNew, 0, sizeof(*aNew));

      /* Open the database file. If the btree is successfully opened, use
      ** it to obtain the database schema. At this point the schema may
      ** or may not be initialised.
      */
      rc = sqlite3BtreeFactory( db, zFile, false, SQLITE_DEFAULT_CACHE_SIZE,
      db.openFlags | SQLITE_OPEN_MAIN_DB,
      ref aNew.pBt );
      db.nDb++;
      if ( rc == SQLITE_CONSTRAINT )
      {
        rc = SQLITE_ERROR;
        zErrDyn = sqlite3MPrintf( db, "database is already attached" );
      }
      else if ( rc == SQLITE_OK )
      {
        Pager pPager;
        aNew.pSchema = sqlite3SchemaGet( db, aNew.pBt );
        if ( aNew.pSchema == null )
        {
          rc = SQLITE_NOMEM;
        }
        else if ( aNew.pSchema.file_format != 0 && aNew.pSchema.enc != ENC( db ) )
        {
          zErrDyn = sqlite3MPrintf( db,
          "attached databases must use the same text encoding as main database" );
          rc = SQLITE_ERROR;
        }
        pPager = sqlite3BtreePager( aNew.pBt );
        sqlite3PagerLockingMode( pPager, db.dfltLockMode );
        sqlite3PagerJournalMode( pPager, db.dfltJournalMode );
      }
      aNew.zName = zName;// sqlite3DbStrDup( db, zName );
      aNew.safety_level = 3;

#if SQLITE_HAS_CODEC
{
extern int sqlite3CodecAttach(sqlite3*, int, const void*, int);
extern void sqlite3CodecGetKey(sqlite3*, int, void**, int*);
int nKey;
char *zKey;
int t = sqlite3_value_type(argv[2]);
switch( t ){
case SQLITE_INTEGER:
case SQLITE_FLOAT:
zErrDyn = sqlite3DbStrDup(db, "Invalid key value");
rc = SQLITE_ERROR;
break;

case SQLITE_TEXT:
case SQLITE_BLOB:
nKey = sqlite3_value_bytes(argv[2]);
zKey = (char *)sqlite3_value_blob(argv[2]);
sqlite3CodecAttach(db, db.nDb-1, zKey, nKey);
break;

case SQLITE_NULL:
/* No key specified.  Use the key from the main database */
sqlite3CodecGetKey(db, 0, (void**)&zKey, nKey);
sqlite3CodecAttach(db, db.nDb-1, zKey, nKey);
break;
}
}
#endif

      /* If the file was opened successfully, read the schema for the new database.
** If this fails, or if opening the file failed, then close the file and
** remove the entry from the db.aDb[] array. i.e. put everything back the way
** we found it.
*/
      if ( rc == SQLITE_OK )
      {
        sqlite3SafetyOn( db );
        sqlite3BtreeEnterAll( db );
        rc = sqlite3Init( db, ref zErrDyn );
        sqlite3BtreeLeaveAll( db );
        sqlite3SafetyOff( db );
      }
      if ( rc != 0 )
      {
        int iDb = db.nDb - 1;
        Debug.Assert( iDb >= 2 );
        if ( db.aDb[iDb].pBt != null )
        {
          sqlite3BtreeClose( ref db.aDb[iDb].pBt );
          db.aDb[iDb].pBt = null;
          db.aDb[iDb].pSchema = null;
        }
        sqlite3ResetInternalSchema( db, 0 );
        db.nDb = iDb;
        if ( rc == SQLITE_NOMEM || rc == SQLITE_IOERR_NOMEM )
        {
  ////        db.mallocFailed = 1;
          //sqlite3DbFree( db, zErrDyn );
          zErrDyn = sqlite3MPrintf( db, "out of memory" );
        }
        else if ( zErrDyn == "" )
        {
          zErrDyn = sqlite3MPrintf( db, "unable to open database: %s", zFile );
        }
        goto attach_error;
      }

      return;

attach_error:
      /* Return an error if we get here */
      if ( zErrDyn != "" )
      {
        sqlite3_result_error( context, zErrDyn, -1 );
        //sqlite3DbFree( db, ref zErrDyn );
      }
      if ( rc != 0 ) sqlite3_result_error_code( context, rc );
    }

    /*
    ** An SQL user-function registered to do the work of an DETACH statement. The
    ** three arguments to the function come directly from a detach statement:
    **
    **     DETACH DATABASE x
    **
    **     SELECT sqlite_detach(x)
    */
    static void detachFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] argv
    )
    {
      string zName = zName = argv[0].z != null && ( argv[0].z.Length > 0 ) ? sqlite3_value_text( argv[0] ) : "";//(sqlite3_value_text(argv[0]);
      sqlite3 db = sqlite3_context_db_handle( context );
      int i;
      Db pDb = null;
      string zErr = "";

      UNUSED_PARAMETER( NotUsed );

      if ( zName == null ) zName = "";
      for ( i = 0 ; i < db.nDb ; i++ )
      {
        pDb = db.aDb[i];
        if ( pDb.pBt == null ) continue;
        if ( sqlite3StrICmp( pDb.zName, zName ) == 0 ) break;
      }

      if ( i >= db.nDb )
      {
        sqlite3_snprintf( 200, ref zErr, "no such database: %s", zName );
        goto detach_error;
      }
      if ( i < 2 )
      {
        sqlite3_snprintf( 200, ref zErr, "cannot detach database %s", zName );
        goto detach_error;
      }
      if ( 0 == db.autoCommit )
      {
        sqlite3_snprintf( 200, ref zErr,
        "cannot DETACH database within transaction" );
        goto detach_error;
      }
      if ( sqlite3BtreeIsInReadTrans( pDb.pBt ) || sqlite3BtreeIsInBackup( pDb.pBt ) )
      {
        sqlite3_snprintf( 200, ref zErr, "database %s is locked", zName );
        goto detach_error;
      }

      sqlite3BtreeClose( ref pDb.pBt );
      pDb.pBt = null;
      pDb.pSchema = null;
      sqlite3ResetInternalSchema( db, 0 );
      return;

detach_error:
      sqlite3_result_error( context, zErr, -1 );
    }

    /*
    ** This procedure generates VDBE code for a single invocation of either the
    ** sqlite_detach() or sqlite_attach() SQL user functions.
    */
    static void codeAttach(
    Parse pParse,       /* The parser context */
    int type,           /* Either SQLITE_ATTACH or SQLITE_DETACH */
    FuncDef pFunc,      /* FuncDef wrapper for detachFunc() or attachFunc() */
    Expr pAuthArg,      /* Expression to pass to authorization callback */
    Expr pFilename,     /* Name of database file */
    Expr pDbname,       /* Name of the database to use internally */
    Expr pKey           /* Database key for encryption extension */
    )
    {
      int rc;
      NameContext sName;
      Vdbe v;
      sqlite3 db = pParse.db;
      int regArgs;

      sName = new NameContext();// memset( &sName, 0, sizeof(NameContext));
      sName.pParse = pParse;

      if (
      SQLITE_OK != ( rc = resolveAttachExpr( sName, pFilename ) ) ||
      SQLITE_OK != ( rc = resolveAttachExpr( sName, pDbname ) ) ||
      SQLITE_OK != ( rc = resolveAttachExpr( sName, pKey ) )
      )
      {
        pParse.nErr++;
        goto attach_end;
      }

#if !SQLITE_OMIT_AUTHORIZATION
if( pAuthArg ){
char *zAuthArg = pAuthArg->u.zToken;
if( NEVER(zAuthArg==0) ){
goto attach_end;
}
rc = sqlite3AuthCheck(pParse, type, zAuthArg, 0, 0);
if(rc!=SQLITE_OK ){
goto attach_end;
}
}
#endif //* SQLITE_OMIT_AUTHORIZATION */

      v = sqlite3GetVdbe( pParse );
      regArgs = sqlite3GetTempRange( pParse, 4 );
      sqlite3ExprCode( pParse, pFilename, regArgs );
      sqlite3ExprCode( pParse, pDbname, regArgs + 1 );
      sqlite3ExprCode( pParse, pKey, regArgs + 2 );

      Debug.Assert( v != null /*|| db.mallocFailed != 0 */ );
      if ( v != null )
      {
        sqlite3VdbeAddOp3( v, OP_Function, 0, regArgs + 3 - pFunc.nArg, regArgs + 3 );
        Debug.Assert( pFunc.nArg == -1 || ( pFunc.nArg & 0xff ) == pFunc.nArg );
        sqlite3VdbeChangeP5( v, (u8)( pFunc.nArg ) );
        sqlite3VdbeChangeP4( v, -1, pFunc, P4_FUNCDEF );

        /* Code an OP_Expire. For an ATTACH statement, set P1 to true (expire this
        ** statement only). For DETACH, set it to false (expire all existing
        ** statements).
        */
        sqlite3VdbeAddOp1( v, OP_Expire, ( type == SQLITE_ATTACH ) ? 1 : 0 );
      }

attach_end:
      sqlite3ExprDelete( db, ref pFilename );
      sqlite3ExprDelete( db, ref pDbname );
      sqlite3ExprDelete( db, ref pKey );
    }

    /*
    ** Called by the parser to compile a DETACH statement.
    **
    **     DETACH pDbname
    */
    static void sqlite3Detach( Parse pParse, Expr pDbname )
    {
      FuncDef detach_func = new FuncDef(
      1,                   /* nArg */
      SQLITE_UTF8,         /* iPrefEnc */
      0,                   /* flags */
      null,                /* pUserData */
      null,                /* pNext */
      detachFunc,          /* xFunc */
      null,                /* xStep */
      null,                /* xFinalize */
      "sqlite_detach",     /* zName */
      null                 /* pHash */
      );
      codeAttach( pParse, SQLITE_DETACH, detach_func, pDbname, null, null, pDbname );
    }

    /*
    ** Called by the parser to compile an ATTACH statement.
    **
    **     ATTACH p AS pDbname KEY pKey
    */
    static void sqlite3Attach( Parse pParse, Expr p, Expr pDbname, Expr pKey )
    {
      FuncDef attach_func = new FuncDef(
      3,                /* nArg */
      SQLITE_UTF8,      /* iPrefEnc */
      0,                /* flags */
      null,             /* pUserData */
      null,             /* pNext */
      attachFunc,       /* xFunc */
      null,             /* xStep */
      null,             /* xFinalize */
      "sqlite_attach",  /* zName */
      null              /* pHash */
      );
      codeAttach( pParse, SQLITE_ATTACH, attach_func, p, p, pDbname, pKey );
    }
#endif // * SQLITE_OMIT_ATTACH */

    /*
** Initialize a DbFixer structure.  This routine must be called prior
** to passing the structure to one of the sqliteFixAAAA() routines below.
**
** The return value indicates whether or not fixation is required.  TRUE
** means we do need to fix the database references, FALSE means we do not.
*/
    static int sqlite3FixInit(
    DbFixer pFix,       /* The fixer to be initialized */
    Parse pParse,       /* Error messages will be written here */
    int iDb,            /* This is the database that must be used */
    string zType,       /* "view", "trigger", or "index" */
    Token pName         /* Name of the view, trigger, or index */
    )
    {
      sqlite3 db;

      if ( NEVER( iDb < 0 ) || iDb == 1 ) return 0;
      db = pParse.db;
      Debug.Assert( db.nDb > iDb );
      pFix.pParse = pParse;
      pFix.zDb = db.aDb[iDb].zName;
      pFix.zType = zType;
      pFix.pName = pName;
      return 1;
    }

    /*
    ** The following set of routines walk through the parse tree and assign
    ** a specific database to all table references where the database name
    ** was left unspecified in the original SQL statement.  The pFix structure
    ** must have been initialized by a prior call to sqlite3FixInit().
    **
    ** These routines are used to make sure that an index, trigger, or
    ** view in one database does not refer to objects in a different database.
    ** (Exception: indices, triggers, and views in the TEMP database are
    ** allowed to refer to anything.)  If a reference is explicitly made
    ** to an object in a different database, an error message is added to
    ** pParse.zErrMsg and these routines return non-zero.  If everything
    ** checks out, these routines return 0.
    */
    static int sqlite3FixSrcList(
    DbFixer pFix,       /* Context of the fixation */
    SrcList pList       /* The Source list to check and modify */
    )
    {
      int i;
      string zDb;
      SrcList_item pItem;

      if ( NEVER( pList == null ) ) return 0;
      zDb = pFix.zDb;
      for ( i = 0 ; i < pList.nSrc ; i++ )
      {//, pItem++){
        pItem = pList.a[i];
        if ( pItem.zDatabase == null )
        {
          pItem.zDatabase = zDb;// sqlite3DbStrDup( pFix.pParse.db, zDb );
        }
        else if ( sqlite3StrICmp( pItem.zDatabase, zDb ) != 0 )
        {
          sqlite3ErrorMsg( pFix.pParse,
          "%s %T cannot reference objects in database %s",
          pFix.zType, pFix.pName, pItem.zDatabase );
          return 1;
        }
#if !SQLITE_OMIT_VIEW || !SQLITE_OMIT_TRIGGER
        if ( sqlite3FixSelect( pFix, pItem.pSelect ) != 0 ) return 1;
        if ( sqlite3FixExpr( pFix, pItem.pOn ) != 0 ) return 1;
#endif
      }
      return 0;
    }
#if !SQLITE_OMIT_VIEW || !SQLITE_OMIT_TRIGGER
    static int sqlite3FixSelect(
    DbFixer pFix,       /* Context of the fixation */
    Select pSelect      /* The SELECT statement to be fixed to one database */
    )
    {
      while ( pSelect != null )
      {
        if ( sqlite3FixExprList( pFix, pSelect.pEList ) != 0 )
        {
          return 1;
        }
        if ( sqlite3FixSrcList( pFix, pSelect.pSrc ) != 0 )
        {
          return 1;
        }
        if ( sqlite3FixExpr( pFix, pSelect.pWhere ) != 0 )
        {
          return 1;
        }
        if ( sqlite3FixExpr( pFix, pSelect.pHaving ) != 0 )
        {
          return 1;
        }
        pSelect = pSelect.pPrior;
      }
      return 0;
    }
    static int sqlite3FixExpr(
    DbFixer pFix,     /* Context of the fixation */
    Expr pExpr        /* The expression to be fixed to one database */
    )
    {
      while ( pExpr != null )
      {
        if ( ExprHasAnyProperty( pExpr, EP_TokenOnly ) ) break;
        if ( ExprHasProperty( pExpr, EP_xIsSelect ) )
        {
          if ( sqlite3FixSelect( pFix, pExpr.x.pSelect ) != 0 ) return 1;
        }
        else
        {
          if ( sqlite3FixExprList( pFix, pExpr.x.pList ) != 0 ) return 1;
        }
        if ( sqlite3FixExpr( pFix, pExpr.pRight ) != 0 )
        {
          return 1;
        }
        pExpr = pExpr.pLeft;
      }
      return 0;
    }
    static int sqlite3FixExprList(
    DbFixer pFix,     /* Context of the fixation */
    ExprList pList    /* The expression to be fixed to one database */
    )
    {
      int i;
      ExprList_item pItem;
      if ( pList == null ) return 0;
      for ( i = 0 ; i < pList.nExpr ; i++ )//, pItem++ )
      {
        pItem = pList.a[i];
        if ( sqlite3FixExpr( pFix, pItem.pExpr ) != 0 )
        {
          return 1;
        }
      }
      return 0;
    }
#endif

#if !SQLITE_OMIT_TRIGGER
    static int sqlite3FixTriggerStep(
    DbFixer pFix,     /* Context of the fixation */
    TriggerStep pStep /* The trigger step be fixed to one database */
    )
    {
      while ( pStep != null )
      {
        if ( sqlite3FixSelect( pFix, pStep.pSelect ) != 0 )
        {
          return 1;
        }
        if ( sqlite3FixExpr( pFix, pStep.pWhere ) != 0 )
        {
          return 1;
        }
        if ( sqlite3FixExprList( pFix, pStep.pExprList ) != 0 )
        {
          return 1;
        }
        pStep = pStep.pNext;
      }
      return 0;
    }
#endif

  }
}
