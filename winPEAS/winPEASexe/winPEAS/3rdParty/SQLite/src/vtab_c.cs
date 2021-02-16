namespace winPEAS._3rdParty.SQLite.src
{
  public partial class CSSQLite
  {
    /*
    ** 2006 June 10
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains code used to help implement virtual tables.
    **
    ** $Id: vtab.c,v 1.94 2009/08/08 18:01:08 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
#if !SQLITE_OMIT_VIRTUALTABLE
//#include "sqliteInt.h"

/*
** The actual function that does the work of creating a new module.
** This function implements the sqlite3_create_module() and
** sqlite3_create_module_v2() interfaces.
*/
static int createModule(
  sqlite3 *db,                    /* Database in which module is registered */
  const char *zName,              /* Name assigned to this module */
  const sqlite3_module *pModule,  /* The definition of the module */
  void *pAux,                     /* Context pointer for xCreate/xConnect */
  void (*xDestroy)(void *)        /* Module destructor function */
){
  int rc, nName;
  Module *pMod;

  sqlite3_mutex_enter(db->mutex);
  nName = sqlite3Strlen30(zName);
  pMod = (Module *)sqlite3DbMallocRaw(db, sizeof(Module) + nName + 1);
  if( pMod ){
    Module *pDel;
    char *zCopy = (char *)(&pMod[1]);
    memcpy(zCopy, zName, nName+1);
    pMod->zName = zCopy;
    pMod->pModule = pModule;
    pMod->pAux = pAux;
    pMod->xDestroy = xDestroy;
    pDel = (Module *)sqlite3HashInsert(&db->aModule, zCopy, nName, (void*)pMod);
    if( pDel && pDel->xDestroy ){
      pDel->xDestroy(pDel->pAux);
    }
    sqlite3DbFree(db, pDel);
    if( pDel==pMod ){
      db->mallocFailed = 1;
    }
    sqlite3ResetInternalSchema(db, 0);
  }else if( xDestroy ){
    xDestroy(pAux);
  }
  rc = sqlite3ApiExit(db, SQLITE_OK);
  sqlite3_mutex_leave(db->mutex);
  return rc;
}


/*
** External API function used to create a new virtual-table module.
*/
int sqlite3_create_module(
  sqlite3 *db,                    /* Database in which module is registered */
  const char *zName,              /* Name assigned to this module */
  const sqlite3_module *pModule,  /* The definition of the module */
  void *pAux                      /* Context pointer for xCreate/xConnect */
){
  return createModule(db, zName, pModule, pAux, 0);
}

/*
** External API function used to create a new virtual-table module.
*/
int sqlite3_create_module_v2(
  sqlite3 *db,                    /* Database in which module is registered */
  const char *zName,              /* Name assigned to this module */
  const sqlite3_module *pModule,  /* The definition of the module */
  void *pAux,                     /* Context pointer for xCreate/xConnect */
  void (*xDestroy)(void *)        /* Module destructor function */
){
  return createModule(db, zName, pModule, pAux, xDestroy);
}

/*
** Lock the virtual table so that it cannot be disconnected.
** Locks nest.  Every lock should have a corresponding unlock.
** If an unlock is omitted, resources leaks will occur.  
**
** If a disconnect is attempted while a virtual table is locked,
** the disconnect is deferred until all locks have been removed.
*/
void sqlite3VtabLock(VTable *pVTab){
  pVTab->nRef++;
}


/*
** pTab is a pointer to a Table structure representing a virtual-table.
** Return a pointer to the VTable object used by connection db to access 
** this virtual-table, if one has been created, or NULL otherwise.
*/
VTable *sqlite3GetVTable(sqlite3 *db, Table *pTab){
  VTable *pVtab;
  assert( IsVirtual(pTab) );
  for(pVtab=pTab->pVTable; pVtab && pVtab->db!=db; pVtab=pVtab->pNext);
  return pVtab;
}

/*
** Decrement the ref-count on a virtual table object. When the ref-count
** reaches zero, call the xDisconnect() method to delete the object.
*/
void sqlite3VtabUnlock(VTable *pVTab){
  sqlite3 *db = pVTab->db;

  assert( db );
  assert( pVTab->nRef>0 );
  assert( sqlite3SafetyCheckOk(db) );

  pVTab->nRef--;
  if( pVTab->nRef==0 ){
    sqlite3_vtab *p = pVTab->pVtab;
    if( p ){
#if SQLITE_DEBUG
      if( pVTab->db->magic==SQLITE_MAGIC_BUSY ){
        (void)sqlite3SafetyOff(db);
        p->pModule->xDisconnect(p);
        (void)sqlite3SafetyOn(db);
      } else
#endif
      {
        p->pModule->xDisconnect(p);
      }
    }
    sqlite3DbFree(db, pVTab);
  }
}

/*
** Table p is a virtual table. This function moves all elements in the
** p->pVTable list to the sqlite3.pDisconnect lists of their associated
** database connections to be disconnected at the next opportunity. 
** Except, if argument db is not NULL, then the entry associated with
** connection db is left in the p->pVTable list.
*/
static VTable *vtabDisconnectAll(sqlite3 *db, Table *p){
  VTable *pRet = 0;
  VTable *pVTable = p->pVTable;
  p->pVTable = 0;

  /* Assert that the mutex (if any) associated with the BtShared database 
  ** that contains table p is held by the caller. See header comments 
  ** above function sqlite3VtabUnlockList() for an explanation of why
  ** this makes it safe to access the sqlite3.pDisconnect list of any
  ** database connection that may have an entry in the p->pVTable list.  */
  assert( db==0 ||
    sqlite3BtreeHoldsMutex(db->aDb[sqlite3SchemaToIndex(db, p->pSchema)].pBt) 
  );

  while( pVTable ){
    sqlite3 *db2 = pVTable->db;
    VTable *pNext = pVTable->pNext;
    assert( db2 );
    if( db2==db ){
      pRet = pVTable;
      p->pVTable = pRet;
      pRet->pNext = 0;
    }else{
      pVTable->pNext = db2->pDisconnect;
      db2->pDisconnect = pVTable;
    }
    pVTable = pNext;
  }

  assert( !db || pRet );
  return pRet;
}


/*
** Disconnect all the virtual table objects in the sqlite3.pDisconnect list.
**
** This function may only be called when the mutexes associated with all
** shared b-tree databases opened using connection db are held by the 
** caller. This is done to protect the sqlite3.pDisconnect list. The
** sqlite3.pDisconnect list is accessed only as follows:
**
**   1) By this function. In this case, all BtShared mutexes and the mutex
**      associated with the database handle itself must be held.
**
**   2) By function vtabDisconnectAll(), when it adds a VTable entry to
**      the sqlite3.pDisconnect list. In this case either the BtShared mutex
**      associated with the database the virtual table is stored in is held
**      or, if the virtual table is stored in a non-sharable database, then
**      the database handle mutex is held.
**
** As a result, a sqlite3.pDisconnect cannot be accessed simultaneously 
** by multiple threads. It is thread-safe.
*/
void sqlite3VtabUnlockList(sqlite3 *db){
  VTable *p = db->pDisconnect;
  db->pDisconnect = 0;

  assert( sqlite3BtreeHoldsAllMutexes(db) );
  assert( sqlite3_mutex_held(db->mutex) );

  if( p ){
    sqlite3ExpirePreparedStatements(db);
    do {
      VTable *pNext = p->pNext;
      sqlite3VtabUnlock(p);
      p = pNext;
    }while( p );
  }
}

/*
** Clear any and all virtual-table information from the Table record.
** This routine is called, for example, just before deleting the Table
** record.
**
** Since it is a virtual-table, the Table structure contains a pointer
** to the head of a linked list of VTable structures. Each VTable 
** structure is associated with a single sqlite3* user of the schema.
** The reference count of the VTable structure associated with database 
** connection db is decremented immediately (which may lead to the 
** structure being xDisconnected and free). Any other VTable structures
** in the list are moved to the sqlite3.pDisconnect list of the associated 
** database connection.
*/
void sqlite3VtabClear(Table *p){
  vtabDisconnectAll(0, p);
  if( p->azModuleArg ){
    int i;
    for(i=0; i<p->nModuleArg; i++){
      sqlite3DbFree(p->dbMem, p->azModuleArg[i]);
    }
    sqlite3DbFree(p->dbMem, p->azModuleArg);
  }
}

/*
** Add a new module argument to pTable->azModuleArg[].
** The string is not copied - the pointer is stored.  The
** string will be freed automatically when the table is
** deleted.
*/
static void addModuleArgument(sqlite3 *db, Table *pTable, char *zArg){
  int i = pTable->nModuleArg++;
  int nBytes = sizeof(char *)*(1+pTable->nModuleArg);
  char **azModuleArg;
  azModuleArg = sqlite3DbRealloc(db, pTable->azModuleArg, nBytes);
  if( azModuleArg==0 ){
    int j;
    for(j=0; j<i; j++){
      sqlite3DbFree(db, pTable->azModuleArg[j]);
    }
    sqlite3DbFree(db, zArg);
    sqlite3DbFree(db, pTable->azModuleArg);
    pTable->nModuleArg = 0;
  }else{
    azModuleArg[i] = zArg;
    azModuleArg[i+1] = 0;
  }
  pTable->azModuleArg = azModuleArg;
}

/*
** The parser calls this routine when it first sees a CREATE VIRTUAL TABLE
** statement.  The module name has been parsed, but the optional list
** of parameters that follow the module name are still pending.
*/
void sqlite3VtabBeginParse(
  Parse *pParse,        /* Parsing context */
  Token *pName1,        /* Name of new table, or database name */
  Token *pName2,        /* Name of new table or NULL */
  Token *pModuleName    /* Name of the module for the virtual table */
){
  int iDb;              /* The database the table is being created in */
  Table *pTable;        /* The new virtual table */
  sqlite3 *db;          /* Database connection */

  sqlite3StartTable(pParse, pName1, pName2, 0, 0, 1, 0);
  pTable = pParse->pNewTable;
  if( pTable==0 ) return;
  assert( 0==pTable->pIndex );

  db = pParse->db;
  iDb = sqlite3SchemaToIndex(db, pTable->pSchema);
  assert( iDb>=0 );

  pTable->tabFlags |= TF_Virtual;
  pTable->nModuleArg = 0;
  addModuleArgument(db, pTable, sqlite3NameFromToken(db, pModuleName));
  addModuleArgument(db, pTable, sqlite3DbStrDup(db, db->aDb[iDb].zName));
  addModuleArgument(db, pTable, sqlite3DbStrDup(db, pTable->zName));
  pParse->sNameToken.n = (int)(&pModuleName->z[pModuleName->n] - pName1->z);

#if !SQLITE_OMIT_AUTHORIZATION
  /* Creating a virtual table invokes the authorization callback twice.
  ** The first invocation, to obtain permission to INSERT a row into the
  ** sqlite_master table, has already been made by sqlite3StartTable().
  ** The second call, to obtain permission to create the table, is made now.
  */
  if( pTable->azModuleArg ){
    sqlite3AuthCheck(pParse, SQLITE_CREATE_VTABLE, pTable->zName, 
            pTable->azModuleArg[0], pParse->db->aDb[iDb].zName);
  }
#endif
}

/*
** This routine takes the module argument that has been accumulating
** in pParse->zArg[] and appends it to the list of arguments on the
** virtual table currently under construction in pParse->pTable.
*/
static void addArgumentToVtab(Parse *pParse){
  if( pParse->sArg.z && ALWAYS(pParse->pNewTable) ){
    const char *z = (const char*)pParse->sArg.z;
    int n = pParse->sArg.n;
    sqlite3 *db = pParse->db;
    addModuleArgument(db, pParse->pNewTable, sqlite3DbStrNDup(db, z, n));
  }
}

/*
** The parser calls this routine after the CREATE VIRTUAL TABLE statement
** has been completely parsed.
*/
void sqlite3VtabFinishParse(Parse *pParse, Token *pEnd){
  Table *pTab = pParse->pNewTable;  /* The table being constructed */
  sqlite3 *db = pParse->db;         /* The database connection */

  if( pTab==0 ) return;
  addArgumentToVtab(pParse);
  pParse->sArg.z = 0;
  if( pTab->nModuleArg<1 ) return;
  
  /* If the CREATE VIRTUAL TABLE statement is being entered for the
  ** first time (in other words if the virtual table is actually being
  ** created now instead of just being read out of sqlite_master) then
  ** do additional initialization work and store the statement text
  ** in the sqlite_master table.
  */
  if( !db->init.busy ){
    char *zStmt;
    char *zWhere;
    int iDb;
    Vdbe *v;

    /* Compute the complete text of the CREATE VIRTUAL TABLE statement */
    if( pEnd ){
      pParse->sNameToken.n = (int)(pEnd->z - pParse->sNameToken.z) + pEnd->n;
    }
    zStmt = sqlite3MPrintf(db, "CREATE VIRTUAL TABLE %T", &pParse->sNameToken);

    /* A slot for the record has already been allocated in the 
    ** SQLITE_MASTER table.  We just need to update that slot with all
    ** the information we've collected.  
    **
    ** The VM register number pParse->regRowid holds the rowid of an
    ** entry in the sqlite_master table tht was created for this vtab
    ** by sqlite3StartTable().
    */
    iDb = sqlite3SchemaToIndex(db, pTab->pSchema);
    sqlite3NestedParse(pParse,
      "UPDATE %Q.%s "
         "SET type='table', name=%Q, tbl_name=%Q, rootpage=0, sql=%Q "
       "WHERE rowid=#%d",
      db->aDb[iDb].zName, SCHEMA_TABLE(iDb),
      pTab->zName,
      pTab->zName,
      zStmt,
      pParse->regRowid
    );
    sqlite3DbFree(db, zStmt);
    v = sqlite3GetVdbe(pParse);
    sqlite3ChangeCookie(pParse, iDb);

    sqlite3VdbeAddOp2(v, OP_Expire, 0, 0);
    zWhere = sqlite3MPrintf(db, "name='%q'", pTab->zName);
    sqlite3VdbeAddOp4(v, OP_ParseSchema, iDb, 1, 0, zWhere, P4_DYNAMIC);
    sqlite3VdbeAddOp4(v, OP_VCreate, iDb, 0, 0, 
                         pTab->zName, sqlite3Strlen30(pTab->zName) + 1);
  }

  /* If we are rereading the sqlite_master table create the in-memory
  ** record of the table. The xConnect() method is not called until
  ** the first time the virtual table is used in an SQL statement. This
  ** allows a schema that contains virtual tables to be loaded before
  ** the required virtual table implementations are registered.  */
  else {
    Table *pOld;
    Schema *pSchema = pTab->pSchema;
    const char *zName = pTab->zName;
    int nName = sqlite3Strlen30(zName);
    pOld = sqlite3HashInsert(&pSchema->tblHash, zName, nName, pTab);
    if( pOld ){
      db->mallocFailed = 1;
      assert( pTab==pOld );  /* Malloc must have failed inside HashInsert() */
      return;
    }
    pSchema->db = pParse->db;
    pParse->pNewTable = 0;
  }
}

/*
** The parser calls this routine when it sees the first token
** of an argument to the module name in a CREATE VIRTUAL TABLE statement.
*/
void sqlite3VtabArgInit(Parse *pParse){
  addArgumentToVtab(pParse);
  pParse->sArg.z = 0;
  pParse->sArg.n = 0;
}

/*
** The parser calls this routine for each token after the first token
** in an argument to the module name in a CREATE VIRTUAL TABLE statement.
*/
void sqlite3VtabArgExtend(Parse *pParse, Token *p){
  Token *pArg = &pParse->sArg;
  if( pArg->z==0 ){
    pArg->z = p->z;
    pArg->n = p->n;
  }else{
    assert(pArg->z < p->z);
    pArg->n = (int)(&p->z[p->n] - pArg->z);
  }
}

/*
** Invoke a virtual table constructor (either xCreate or xConnect). The
** pointer to the function to invoke is passed as the fourth parameter
** to this procedure.
*/
static int vtabCallConstructor(
  sqlite3 *db, 
  Table *pTab,
  Module *pMod,
  int (*xConstruct)(sqlite3*,void*,int,const char*const*,sqlite3_vtab**,char**),
  char **pzErr
){
  VTable *pVTable;
  int rc;
  const char *const*azArg = (const char *const*)pTab->azModuleArg;
  int nArg = pTab->nModuleArg;
  char *zErr = 0;
  char *zModuleName = sqlite3MPrintf(db, "%s", pTab->zName);

  if( !zModuleName ){
    return SQLITE_NOMEM;
  }

  pVTable = sqlite3DbMallocZero(db, sizeof(VTable));
  if( !pVTable ){
    sqlite3DbFree(db, zModuleName);
    return SQLITE_NOMEM;
  }
  pVTable->db = db;
  pVTable->pMod = pMod;

  assert( !db->pVTab );
  assert( xConstruct );
  db->pVTab = pTab;

  /* Invoke the virtual table constructor */
  (void)sqlite3SafetyOff(db);
  rc = xConstruct(db, pMod->pAux, nArg, azArg, &pVTable->pVtab, &zErr);
  (void)sqlite3SafetyOn(db);
  if( rc==SQLITE_NOMEM ) db->mallocFailed = 1;

  if( SQLITE_OK!=rc ){
    if( zErr==0 ){
      *pzErr = sqlite3MPrintf(db, "vtable constructor failed: %s", zModuleName);
    }else {
      *pzErr = sqlite3MPrintf(db, "%s", zErr);
      sqlite3DbFree(db, zErr);
    }
    sqlite3DbFree(db, pVTable);
  }else if( ALWAYS(pVTable->pVtab) ){
    /* Justification of ALWAYS():  A correct vtab constructor must allocate
    ** the sqlite3_vtab object if successful.  */
    pVTable->pVtab->pModule = pMod->pModule;
    pVTable->nRef = 1;
    if( db->pVTab ){
      const char *zFormat = "vtable constructor did not declare schema: %s";
      *pzErr = sqlite3MPrintf(db, zFormat, pTab->zName);
      sqlite3VtabUnlock(pVTable);
      rc = SQLITE_ERROR;
    }else{
      int iCol;
      /* If everything went according to plan, link the new VTable structure
      ** into the linked list headed by pTab->pVTable. Then loop through the 
      ** columns of the table to see if any of them contain the token "hidden".
      ** If so, set the Column.isHidden flag and remove the token from
      ** the type string.  */
      pVTable->pNext = pTab->pVTable;
      pTab->pVTable = pVTable;

      for(iCol=0; iCol<pTab->nCol; iCol++){
        char *zType = pTab->aCol[iCol].zType;
        int nType;
        int i = 0;
        if( !zType ) continue;
        nType = sqlite3Strlen30(zType);
        if( sqlite3StrNICmp("hidden", zType, 6)||(zType[6] && zType[6]!=' ') ){
          for(i=0; i<nType; i++){
            if( (0==sqlite3StrNICmp(" hidden", &zType[i], 7))
             && (zType[i+7]=='\0' || zType[i+7]==' ')
            ){
              i++;
              break;
            }
          }
        }
        if( i<nType ){
          int j;
          int nDel = 6 + (zType[i+6] ? 1 : 0);
          for(j=i; (j+nDel)<=nType; j++){
            zType[j] = zType[j+nDel];
          }
          if( zType[i]=='\0' && i>0 ){
            assert(zType[i-1]==' ');
            zType[i-1] = '\0';
          }
          pTab->aCol[iCol].isHidden = 1;
        }
      }
    }
  }

  sqlite3DbFree(db, zModuleName);
  db->pVTab = 0;
  return rc;
}

/*
** This function is invoked by the parser to call the xConnect() method
** of the virtual table pTab. If an error occurs, an error code is returned 
** and an error left in pParse.
**
** This call is a no-op if table pTab is not a virtual table.
*/
int sqlite3VtabCallConnect(Parse *pParse, Table *pTab){
  sqlite3 *db = pParse->db;
  const char *zMod;
  Module *pMod;
  int rc;

  assert( pTab );
  if( (pTab->tabFlags & TF_Virtual)==0 || sqlite3GetVTable(db, pTab) ){
    return SQLITE_OK;
  }

  /* Locate the required virtual table module */
  zMod = pTab->azModuleArg[0];
  pMod = (Module*)sqlite3HashFind(&db->aModule, zMod, sqlite3Strlen30(zMod));

  if( !pMod ){
    const char *zModule = pTab->azModuleArg[0];
    sqlite3ErrorMsg(pParse, "no such module: %s", zModule);
    rc = SQLITE_ERROR;
  }else{
    char *zErr = 0;
    rc = vtabCallConstructor(db, pTab, pMod, pMod->pModule->xConnect, &zErr);
    if( rc!=SQLITE_OK ){
      sqlite3ErrorMsg(pParse, "%s", zErr);
    }
    sqlite3DbFree(db, zErr);
  }

  return rc;
}

/*
** Add the virtual table pVTab to the array sqlite3.aVTrans[].
*/
static int addToVTrans(sqlite3 *db, VTable *pVTab){
  const int ARRAY_INCR = 5;

  /* Grow the sqlite3.aVTrans array if required */
  if( (db->nVTrans%ARRAY_INCR)==0 ){
    VTable **aVTrans;
    int nBytes = sizeof(sqlite3_vtab *) * (db->nVTrans + ARRAY_INCR);
    aVTrans = sqlite3DbRealloc(db, (void *)db->aVTrans, nBytes);
    if( !aVTrans ){
      return SQLITE_NOMEM;
    }
    memset(&aVTrans[db->nVTrans], 0, sizeof(sqlite3_vtab *)*ARRAY_INCR);
    db->aVTrans = aVTrans;
  }

  /* Add pVtab to the end of sqlite3.aVTrans */
  db->aVTrans[db->nVTrans++] = pVTab;
  sqlite3VtabLock(pVTab);
  return SQLITE_OK;
}

/*
** This function is invoked by the vdbe to call the xCreate method
** of the virtual table named zTab in database iDb. 
**
** If an error occurs, *pzErr is set to point an an English language
** description of the error and an SQLITE_XXX error code is returned.
** In this case the caller must call sqlite3DbFree(db, ) on *pzErr.
*/
int sqlite3VtabCallCreate(sqlite3 *db, int iDb, const char *zTab, char **pzErr){
  int rc = SQLITE_OK;
  Table *pTab;
  Module *pMod;
  const char *zMod;

  pTab = sqlite3FindTable(db, zTab, db->aDb[iDb].zName);
  assert( pTab && (pTab->tabFlags & TF_Virtual)!=0 && !pTab->pVTable );

  /* Locate the required virtual table module */
  zMod = pTab->azModuleArg[0];
  pMod = (Module*)sqlite3HashFind(&db->aModule, zMod, sqlite3Strlen30(zMod));

  /* If the module has been registered and includes a Create method, 
  ** invoke it now. If the module has not been registered, return an 
  ** error. Otherwise, do nothing.
  */
  if( !pMod ){
    *pzErr = sqlite3MPrintf(db, "no such module: %s", zMod);
    rc = SQLITE_ERROR;
  }else{
    rc = vtabCallConstructor(db, pTab, pMod, pMod->pModule->xCreate, pzErr);
  }

  /* Justification of ALWAYS():  The xConstructor method is required to
  ** create a valid sqlite3_vtab if it returns SQLITE_OK. */
  if( rc==SQLITE_OK && ALWAYS(sqlite3GetVTable(db, pTab)) ){
      rc = addToVTrans(db, sqlite3GetVTable(db, pTab));
  }

  return rc;
}

/*
** This function is used to set the schema of a virtual table.  It is only
** valid to call this function from within the xCreate() or xConnect() of a
** virtual table module.
*/
int sqlite3_declare_vtab(sqlite3 *db, const char *zCreateTable){
  Parse *pParse;

  int rc = SQLITE_OK;
  Table *pTab;
  char *zErr = 0;

  sqlite3_mutex_enter(db->mutex);
  pTab = db->pVTab;
  if( !pTab ){
    sqlite3Error(db, SQLITE_MISUSE, 0);
    sqlite3_mutex_leave(db->mutex);
    return SQLITE_MISUSE;
  }
  assert( (pTab->tabFlags & TF_Virtual)!=0 );

  pParse = sqlite3StackAllocZero(db, sizeof(*pParse));
  if( pParse==0 ){
    rc = SQLITE_NOMEM;
  }else{
    pParse->declareVtab = 1;
    pParse->db = db;
  
    if( 
        SQLITE_OK == sqlite3RunParser(pParse, zCreateTable, &zErr) && 
        pParse->pNewTable && 
        !pParse->pNewTable->pSelect && 
        (pParse->pNewTable->tabFlags & TF_Virtual)==0
    ){
      if( !pTab->aCol ){
        pTab->aCol = pParse->pNewTable->aCol;
        pTab->nCol = pParse->pNewTable->nCol;
        pParse->pNewTable->nCol = 0;
        pParse->pNewTable->aCol = 0;
      }
      db->pVTab = 0;
    } else {
      sqlite3Error(db, SQLITE_ERROR, zErr);
      sqlite3DbFree(db, zErr);
      rc = SQLITE_ERROR;
    }
    pParse->declareVtab = 0;
  
    if( pParse->pVdbe ){
      sqlite3VdbeFinalize(pParse->pVdbe);
    }
    sqlite3DeleteTable(pParse->pNewTable);
    sqlite3StackFree(db, pParse);
  }

  assert( (rc&0xff)==rc );
  rc = sqlite3ApiExit(db, rc);
  sqlite3_mutex_leave(db->mutex);
  return rc;
}

/*
** This function is invoked by the vdbe to call the xDestroy method
** of the virtual table named zTab in database iDb. This occurs
** when a DROP TABLE is mentioned.
**
** This call is a no-op if zTab is not a virtual table.
*/
int sqlite3VtabCallDestroy(sqlite3 *db, int iDb, const char *zTab){
  int rc = SQLITE_OK;
  Table *pTab;

  pTab = sqlite3FindTable(db, zTab, db->aDb[iDb].zName);
  if( ALWAYS(pTab!=0 && pTab->pVTable!=0) ){
    VTable *p = vtabDisconnectAll(db, pTab);

    rc = sqlite3SafetyOff(db);
    assert( rc==SQLITE_OK );
    rc = p->pMod->pModule->xDestroy(p->pVtab);
    (void)sqlite3SafetyOn(db);

    /* Remove the sqlite3_vtab* from the aVTrans[] array, if applicable */
    if( rc==SQLITE_OK ){
      assert( pTab->pVTable==p && p->pNext==0 );
      p->pVtab = 0;
      pTab->pVTable = 0;
      sqlite3VtabUnlock(p);
    }
  }

  return rc;
}

/*
** This function invokes either the xRollback or xCommit method
** of each of the virtual tables in the sqlite3.aVTrans array. The method
** called is identified by the second argument, "offset", which is
** the offset of the method to call in the sqlite3_module structure.
**
** The array is cleared after invoking the callbacks. 
*/
static void callFinaliser(sqlite3 *db, int offset){
  int i;
  if( db->aVTrans ){
    for(i=0; i<db->nVTrans; i++){
      VTable *pVTab = db->aVTrans[i];
      sqlite3_vtab *p = pVTab->pVtab;
      if( p ){
        int (*x)(sqlite3_vtab *);
        x = *(int (**)(sqlite3_vtab *))((char *)p->pModule + offset);
        if( x ) x(p);
      }
      sqlite3VtabUnlock(pVTab);
    }
    sqlite3DbFree(db, db->aVTrans);
    db->nVTrans = 0;
    db->aVTrans = 0;
  }
}

/*
** Invoke the xSync method of all virtual tables in the sqlite3.aVTrans
** array. Return the error code for the first error that occurs, or
** SQLITE_OK if all xSync operations are successful.
**
** Set *pzErrmsg to point to a buffer that should be released using 
** sqlite3DbFree() containing an error message, if one is available.
*/
int sqlite3VtabSync(sqlite3 *db, char **pzErrmsg){
  int i;
  int rc = SQLITE_OK;
  int rcsafety;
  VTable **aVTrans = db->aVTrans;

  rc = sqlite3SafetyOff(db);
  db->aVTrans = 0;
  for(i=0; rc==SQLITE_OK && i<db->nVTrans; i++){
    int (*x)(sqlite3_vtab *);
    sqlite3_vtab *pVtab = aVTrans[i]->pVtab;
    if( pVtab && (x = pVtab->pModule->xSync)!=0 ){
      rc = x(pVtab);
      sqlite3DbFree(db, *pzErrmsg);
      *pzErrmsg = pVtab->zErrMsg;
      pVtab->zErrMsg = 0;
    }
  }
  db->aVTrans = aVTrans;
  rcsafety = sqlite3SafetyOn(db);

  if( rc==SQLITE_OK ){
    rc = rcsafety;
  }
  return rc;
}

/*
** Invoke the xRollback method of all virtual tables in the 
** sqlite3.aVTrans array. Then clear the array itself.
*/
int sqlite3VtabRollback(sqlite3 *db){
  callFinaliser(db, offsetof(sqlite3_module,xRollback));
  return SQLITE_OK;
}

/*
** Invoke the xCommit method of all virtual tables in the 
** sqlite3.aVTrans array. Then clear the array itself.
*/
int sqlite3VtabCommit(sqlite3 *db){
  callFinaliser(db, offsetof(sqlite3_module,xCommit));
  return SQLITE_OK;
}

/*
** If the virtual table pVtab supports the transaction interface
** (xBegin/xRollback/xCommit and optionally xSync) and a transaction is
** not currently open, invoke the xBegin method now.
**
** If the xBegin call is successful, place the sqlite3_vtab pointer
** in the sqlite3.aVTrans array.
*/
int sqlite3VtabBegin(sqlite3 *db, VTable *pVTab){
  int rc = SQLITE_OK;
  const sqlite3_module *pModule;

  /* Special case: If db->aVTrans is NULL and db->nVTrans is greater
  ** than zero, then this function is being called from within a
  ** virtual module xSync() callback. It is illegal to write to 
  ** virtual module tables in this case, so return SQLITE_LOCKED.
  */
  if( sqlite3VtabInSync(db) ){
    return SQLITE_LOCKED;
  }
  if( !pVTab ){
    return SQLITE_OK;
  } 
  pModule = pVTab->pVtab->pModule;

  if( pModule->xBegin ){
    int i;


    /* If pVtab is already in the aVTrans array, return early */
    for(i=0; i<db->nVTrans; i++){
      if( db->aVTrans[i]==pVTab ){
        return SQLITE_OK;
      }
    }

    /* Invoke the xBegin method */
    rc = pModule->xBegin(pVTab->pVtab);
    if( rc==SQLITE_OK ){
      rc = addToVTrans(db, pVTab);
    }
  }
  return rc;
}

/*
** The first parameter (pDef) is a function implementation.  The
** second parameter (pExpr) is the first argument to this function.
** If pExpr is a column in a virtual table, then let the virtual
** table implementation have an opportunity to overload the function.
**
** This routine is used to allow virtual table implementations to
** overload MATCH, LIKE, GLOB, and REGEXP operators.
**
** Return either the pDef argument (indicating no change) or a 
** new FuncDef structure that is marked as ephemeral using the
** SQLITE_FUNC_EPHEM flag.
*/
FuncDef *sqlite3VtabOverloadFunction(
  sqlite3 *db,    /* Database connection for reporting malloc problems */
  FuncDef *pDef,  /* Function to possibly overload */
  int nArg,       /* Number of arguments to the function */
  Expr *pExpr     /* First argument to the function */
){
  Table *pTab;
  sqlite3_vtab *pVtab;
  sqlite3_module *pMod;
  void (*xFunc)(sqlite3_context*,int,sqlite3_value**) = 0;
  void *pArg = 0;
  FuncDef *pNew;
  int rc = 0;
  char *zLowerName;
  unsigned char *z;


  /* Check to see the left operand is a column in a virtual table */
  if( NEVER(pExpr==0) ) return pDef;
  if( pExpr->op!=TK_COLUMN ) return pDef;
  pTab = pExpr->pTab;
  if( NEVER(pTab==0) ) return pDef;
  if( (pTab->tabFlags & TF_Virtual)==0 ) return pDef;
  pVtab = sqlite3GetVTable(db, pTab)->pVtab;
  assert( pVtab!=0 );
  assert( pVtab->pModule!=0 );
  pMod = (sqlite3_module *)pVtab->pModule;
  if( pMod->xFindFunction==0 ) return pDef;
 
  /* Call the xFindFunction method on the virtual table implementation
  ** to see if the implementation wants to overload this function 
  */
  zLowerName = sqlite3DbStrDup(db, pDef->zName);
  if( zLowerName ){
    for(z=(unsigned char*)zLowerName; *z; z++){
      *z = sqlite3UpperToLower[*z];
    }
    rc = pMod->xFindFunction(pVtab, nArg, zLowerName, &xFunc, &pArg);
    sqlite3DbFree(db, zLowerName);
  }
  if( rc==0 ){
    return pDef;
  }

  /* Create a new ephemeral function definition for the overloaded
  ** function */
  pNew = sqlite3DbMallocZero(db, sizeof(*pNew)
                             + sqlite3Strlen30(pDef->zName) + 1);
  if( pNew==0 ){
    return pDef;
  }
  *pNew = *pDef;
  pNew->zName = (char *)&pNew[1];
  memcpy(pNew->zName, pDef->zName, sqlite3Strlen30(pDef->zName)+1);
  pNew->xFunc = xFunc;
  pNew->pUserData = pArg;
  pNew->flags |= SQLITE_FUNC_EPHEM;
  return pNew;
}

/*
** Make sure virtual table pTab is contained in the pParse->apVirtualLock[]
** array so that an OP_VBegin will get generated for it.  Add pTab to the
** array if it is missing.  If pTab is already in the array, this routine
** is a no-op.
*/
void sqlite3VtabMakeWritable(Parse *pParse, Table *pTab){
  int i, n;
  Table **apVtabLock;

  assert( IsVirtual(pTab) );
  for(i=0; i<pParse->nVtabLock; i++){
    if( pTab==pParse->apVtabLock[i] ) return;
  }
  n = (pParse->nVtabLock+1)*sizeof(pParse->apVtabLock[0]);
  apVtabLock = sqlite3_realloc(pParse->apVtabLock, n);
  if( apVtabLock ){
    pParse->apVtabLock = apVtabLock;
    pParse->apVtabLock[pParse->nVtabLock++] = pTab;
  }else{
    pParse->db->mallocFailed = 1;
  }
}

#endif //* SQLITE_OMIT_VIRTUALTABLE */

  }
}
