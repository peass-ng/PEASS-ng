using System;
using System.Diagnostics;

using u8 = System.Byte;
using u32 = System.UInt32;

namespace CS_SQLite3
{
  using sqlite3_value = CSSQLite.Mem;

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
    ** This file contains C code routines that are called by the parser
    ** to handle UPDATE statements.
    **
    ** $Id: update.c,v 1.207 2009/08/08 18:01:08 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"

#if !SQLITE_OMIT_VIRTUALTABLE
/* Forward declaration */
//static void updateVirtualTable(
//Parse pParse,       /* The parsing context */
//SrcList pSrc,       /* The virtual table to be modified */
//Table pTab,         /* The virtual table */
//ExprList pChanges,  /* The columns to change in the UPDATE statement */
//Expr pRowidExpr,    /* Expression used to recompute the rowid */
//int aXRef,          /* Mapping from columns of pTab to entries in pChanges */
//Expr pWhere         /* WHERE clause of the UPDATE statement */
//);
#endif // * SQLITE_OMIT_VIRTUALTABLE */

    /*
** The most recently coded instruction was an OP_Column to retrieve the
** i-th column of table pTab. This routine sets the P4 parameter of the
** OP_Column to the default value, if any.
**
** The default value of a column is specified by a DEFAULT clause in the
** column definition. This was either supplied by the user when the table
** was created, or added later to the table definition by an ALTER TABLE
** command. If the latter, then the row-records in the table btree on disk
** may not contain a value for the column and the default value, taken
** from the P4 parameter of the OP_Column instruction, is returned instead.
** If the former, then all row-records are guaranteed to include a value
** for the column and the P4 value is not required.
**
** Column definitions created by an ALTER TABLE command may only have
** literal default values specified: a number, null or a string. (If a more
** complicated default expression value was provided, it is evaluated
** when the ALTER TABLE is executed and one of the literal values written
** into the sqlite_master table.)
**
** Therefore, the P4 parameter is only required if the default value for
** the column is a literal number, string or null. The sqlite3ValueFromExpr()
** function is capable of transforming these types of expressions into
** sqlite3_value objects.
**
** If parameter iReg is not negative, code an OP_RealAffinity instruction
** on register iReg. This is used when an equivalent integer value is
** stored in place of an 8-byte floating point value in order to save
** space.
*/
    static void sqlite3ColumnDefault( Vdbe v, Table pTab, int i, int iReg )
    {
      Debug.Assert( pTab != null );
      if ( null == pTab.pSelect )
      {
        sqlite3_value pValue = new sqlite3_value();
        int enc = ENC( sqlite3VdbeDb( v ) );
        Column pCol = pTab.aCol[i];
#if SQLITE_DEBUG
        VdbeComment( v, "%s.%s", pTab.zName, pCol.zName );
#endif
        Debug.Assert( i < pTab.nCol );
        sqlite3ValueFromExpr( sqlite3VdbeDb( v ), pCol.pDflt, enc,
        pCol.affinity, ref pValue );
        if ( pValue != null )
        {
          sqlite3VdbeChangeP4( v, -1, pValue, P4_MEM );
        }
#if !SQLITE_OMIT_FLOATING_POINT
        if ( iReg >= 0 && pTab.aCol[i].affinity == SQLITE_AFF_REAL )
        {
          sqlite3VdbeAddOp1( v, OP_RealAffinity, iReg );
        }
#endif
      }
    }

    /*
    ** Process an UPDATE statement.
    **
    **   UPDATE OR IGNORE table_wxyz SET a=b, c=d WHERE e<5 AND f NOT NULL;
    **          \_______/ \________/     \______/       \________________/
    *            onError   pTabList      pChanges             pWhere
    */
    static void sqlite3Update(
    Parse pParse,         /* The parser context */
    SrcList pTabList,     /* The table in which we should change things */
    ExprList pChanges,    /* Things to be changed */
    Expr pWhere,          /* The WHERE clause.  May be null */
    int onError           /* How to handle constraint errors */
    )
    {
      int i, j;                   /* Loop counters */
      Table pTab;                 /* The table to be updated */
      int addr = 0;               /* VDBE instruction address of the start of the loop */
      WhereInfo pWInfo;           /* Information about the WHERE clause */
      Vdbe v;                     /* The virtual database engine */
      Index pIdx;                 /* For looping over indices */
      int nIdx;                   /* Number of indices that need updating */
      int iCur;                   /* VDBE Cursor number of pTab */
      sqlite3 db;                 /* The database structure */
      int[] aRegIdx = null;       /* One register assigned to each index to be updated */
      int[] aXRef = null;         /* aXRef[i] is the index in pChanges.a[] of the
** an expression for the i-th column of the table.
** aXRef[i]==-1 if the i-th column is not changed. */
      bool chngRowid;             /* True if the record number is being changed */
      Expr pRowidExpr = null;     /* Expression defining the new record number */
      bool openAll = false;       /* True if all indices need to be opened */
      AuthContext sContext;       /* The authorization context */
      NameContext sNC;            /* The name-context to resolve expressions in */
      int iDb;                    /* Database containing the table being updated */
      int j1;                     /* Addresses of jump instructions */
      u8 okOnePass;               /* True for one-pass algorithm without the FIFO */

#if !SQLITE_OMIT_TRIGGER
      bool isView = false;         /* Trying to update a view */
      Trigger pTrigger;            /* List of triggers on pTab, if required */
#endif
      int iBeginAfterTrigger = 0;  /* Address of after trigger program */
      int iEndAfterTrigger = 0;    /* Exit of after trigger program */
      int iBeginBeforeTrigger = 0; /* Address of before trigger program */
      int iEndBeforeTrigger = 0;   /* Exit of before trigger program */
      u32 old_col_mask = 0;        /* Mask of OLD.* columns in use */
      u32 new_col_mask = 0;        /* Mask of NEW.* columns in use */

      int newIdx = -1;             /* index of trigger "new" temp table       */
      int oldIdx = -1;             /* index of trigger "old" temp table       */

      /* Register Allocations */
      int regRowCount = 0;         /* A count of rows changed */
      int regOldRowid;             /* The old rowid */
      int regNewRowid;             /* The new rowid */
      int regData;                 /* New data for the row */
      int regRowSet = 0;           /* Rowset of rows to be updated */

      sContext = new AuthContext(); //memset( &sContext, 0, sizeof( sContext ) );
      db = pParse.db;
      if ( pParse.nErr != 0 /*|| db.mallocFailed != 0 */ )
      {
        goto update_cleanup;
      }
      Debug.Assert( pTabList.nSrc == 1 );

      /* Locate the table which we want to update.
      */
      pTab = sqlite3SrcListLookup( pParse, pTabList );
      if ( pTab == null ) goto update_cleanup;
      iDb = sqlite3SchemaToIndex( pParse.db, pTab.pSchema );

      /* Figure out if we have any triggers and if the table being
      ** updated is a view
      */
#if !SQLITE_OMIT_TRIGGER
      int iDummy = 0;
      pTrigger = sqlite3TriggersExist( pParse, pTab, TK_UPDATE, pChanges, ref iDummy );
      isView = pTab.pSelect != null;
#else
const Trigger pTrigger = null;
#if !SQLITE_OMIT_VIEW
const bool isView = false;
#endif
#endif
#if SQLITE_OMIT_VIEW
//    # undef isView
const bool isView = false;
#endif

      if ( sqlite3ViewGetColumnNames( pParse, pTab ) != 0 )
      {
        goto update_cleanup;
      }
      if ( sqlite3IsReadOnly( pParse, pTab, ( pTrigger != null ? 1 : 0 ) ) )
      {
        goto update_cleanup;
      }
      aXRef = new int[pTab.nCol];// sqlite3DbMallocRaw(db, sizeof(int) * pTab.nCol);
      //if ( aXRef == null ) goto update_cleanup;
      for ( i = 0 ; i < pTab.nCol ; i++ ) aXRef[i] = -1;

      /* If there are FOR EACH ROW triggers, allocate cursors for the
      ** special OLD and NEW tables
      */
      if ( pTrigger != null )
      {
        newIdx = pParse.nTab++;
        oldIdx = pParse.nTab++;
      }

      /* Allocate a cursors for the main database table and for all indices.
      ** The index cursors might not be used, but if they are used they
      ** need to occur right after the database cursor.  So go ahead and
      ** allocate enough space, just in case.
      */
      pTabList.a[0].iCursor = iCur = pParse.nTab++;
      for ( pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext )
      {
        pParse.nTab++;
      }

      /* Initialize the name-context */
      sNC = new NameContext();// memset(&sNC, 0, sNC).Length;
      sNC.pParse = pParse;
      sNC.pSrcList = pTabList;

      /* Resolve the column names in all the expressions of the
      ** of the UPDATE statement.  Also find the column index
      ** for each column to be updated in the pChanges array.  For each
      ** column to be updated, make sure we have authorization to change
      ** that column.
      */
      chngRowid = false;
      for ( i = 0 ; i < pChanges.nExpr ; i++ )
      {
        if ( sqlite3ResolveExprNames( sNC, ref pChanges.a[i].pExpr ) != 0 )
        {
          goto update_cleanup;
        }
        for ( j = 0 ; j < pTab.nCol ; j++ )
        {
          if ( sqlite3StrICmp( pTab.aCol[j].zName, pChanges.a[i].zName ) == 0 )
          {
            if ( j == pTab.iPKey )
            {
              chngRowid = true;
              pRowidExpr = pChanges.a[i].pExpr;
            }
            aXRef[j] = i;
            break;
          }
        }
        if ( j >= pTab.nCol )
        {
          if ( sqlite3IsRowid( pChanges.a[i].zName ) )
          {
            chngRowid = true;
            pRowidExpr = pChanges.a[i].pExpr;
          }
          else
          {
            sqlite3ErrorMsg( pParse, "no such column: %s", pChanges.a[i].zName );
            goto update_cleanup;
          }
        }
#if !SQLITE_OMIT_AUTHORIZATION
{
int rc;
rc = sqlite3AuthCheck(pParse, SQLITE_UPDATE, pTab.zName,
pTab.aCol[j].zName, db.aDb[iDb].zName);
if( rc==SQLITE_DENY ){
goto update_cleanup;
}else if( rc==SQLITE_IGNORE ){
aXRef[j] = -1;
}
}
#endif
      }

      /* Allocate memory for the array aRegIdx[].  There is one entry in the
      ** array for each index associated with table being updated.  Fill in
      ** the value with a register number for indices that are to be used
      ** and with zero for unused indices.
      */
      for ( nIdx = 0, pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext, nIdx++ ) { }
      if ( nIdx > 0 )
      {
        aRegIdx = new int[nIdx]; // sqlite3DbMallocRaw(db, Index*.Length * nIdx);
        if ( aRegIdx == null ) goto update_cleanup;
      }
      for ( j = 0, pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext, j++ )
      {
        int reg;
        if ( chngRowid )
        {
          reg = ++pParse.nMem;
        }
        else
        {
          reg = 0;
          for ( i = 0 ; i < pIdx.nColumn ; i++ )
          {
            if ( aXRef[pIdx.aiColumn[i]] >= 0 )
            {
              reg = ++pParse.nMem;
              break;
            }
          }
        }
        aRegIdx[j] = reg;
      }

      /* Allocate a block of register used to store the change record
      ** sent to sqlite3GenerateConstraintChecks().  There are either
      ** one or two registers for holding the rowid.  One rowid register
      ** is used if chngRowid is false and two are used if chngRowid is
      ** true.  Following these are pTab.nCol register holding column
      ** data.
      */
      regOldRowid = regNewRowid = pParse.nMem + 1;
      pParse.nMem += pTab.nCol + 1;
      if ( chngRowid )
      {
        regNewRowid++;
        pParse.nMem++;
      }
      regData = regNewRowid + 1;


      /* Begin generating code.
      */
      v = sqlite3GetVdbe( pParse );
      if ( v == null ) goto update_cleanup;
      if ( pParse.nested == 0 ) sqlite3VdbeCountChanges( v );
      sqlite3BeginWriteOperation( pParse, 1, iDb );

#if !SQLITE_OMIT_VIRTUALTABLE
/* Virtual tables must be handled separately */
if ( IsVirtual( pTab ) )
{
updateVirtualTable( pParse, pTabList, pTab, pChanges, pRowidExpr, aXRef, pWhere );
pWhere = null;
pTabList = null;
goto update_cleanup;
}
#endif

      /* Start the view context
*/
#if !SQLITE_OMIT_AUTHORIZATION
if( isView ){
sqlite3AuthContextPush(pParse, sContext, pTab.zName);
}
#endif
      /* Generate the code for triggers.
*/
      if ( pTrigger != null )
      {
        int iGoto;

        /* Create pseudo-tables for NEW and OLD
        */
        sqlite3VdbeAddOp3( v, OP_OpenPseudo, oldIdx, 0, pTab.nCol );
        sqlite3VdbeAddOp3( v, OP_OpenPseudo, newIdx, 0, pTab.nCol );

        iGoto = sqlite3VdbeAddOp2( v, OP_Goto, 0, 0 );
        addr = sqlite3VdbeMakeLabel( v );
        iBeginBeforeTrigger = sqlite3VdbeCurrentAddr( v );
        if ( sqlite3CodeRowTrigger( pParse, pTrigger, TK_UPDATE, pChanges,
        TRIGGER_BEFORE, pTab, newIdx, oldIdx, onError, addr,
        ref old_col_mask, ref new_col_mask ) != 0 )
        {
          goto update_cleanup;
        }
        iEndBeforeTrigger = sqlite3VdbeAddOp2( v, OP_Goto, 0, 0 );
        iBeginAfterTrigger = sqlite3VdbeCurrentAddr( v );
#if !SQLITE_OMIT_TRIGGER
        if ( sqlite3CodeRowTrigger( pParse, pTrigger, TK_UPDATE, pChanges, TRIGGER_AFTER, pTab,
        newIdx, oldIdx, onError, addr, ref old_col_mask, ref new_col_mask ) != 0 )
        {
          goto update_cleanup;
        }
#endif
        iEndAfterTrigger = sqlite3VdbeAddOp2( v, OP_Goto, 0, 0 );
        sqlite3VdbeJumpHere( v, iGoto );
      }

      /* If we are trying to update a view, realize that view into
      ** a ephemeral table.
      */
#if !(SQLITE_OMIT_VIEW) && !(SQLITE_OMIT_TRIGGER)
      if ( isView )
      {
        sqlite3MaterializeView( pParse, pTab, pWhere, iCur );
      }
#endif

      /* Resolve the column names in all the expressions in the
** WHERE clause.
*/
      if ( sqlite3ResolveExprNames( sNC, ref pWhere ) != 0 )
      {
        goto update_cleanup;
      }

      /* Begin the database scan
      */
      sqlite3VdbeAddOp2( v, OP_Null, 0, regOldRowid );
      ExprList NullOrderby = null;
      pWInfo = sqlite3WhereBegin( pParse, pTabList, pWhere, ref NullOrderby, WHERE_ONEPASS_DESIRED );
      if ( pWInfo == null ) goto update_cleanup;
      okOnePass = pWInfo.okOnePass;

      /* Remember the rowid of every item to be updated.
      */
      sqlite3VdbeAddOp2( v, OP_Rowid, iCur, regOldRowid );
      if ( 0 == okOnePass )
      {
        regRowSet = ++pParse.nMem;
        sqlite3VdbeAddOp2( v, OP_RowSetAdd, regRowSet, regOldRowid );
      }

      /* End the database scan loop.
      */
      sqlite3WhereEnd( pWInfo );

      /* Initialize the count of updated rows
      */
      if ( ( db.flags & SQLITE_CountRows ) != 0 && pParse.trigStack == null )
      {
        regRowCount = ++pParse.nMem;
        sqlite3VdbeAddOp2( v, OP_Integer, 0, regRowCount );
      }

      if ( !isView )
      {
        /*
        ** Open every index that needs updating.  Note that if any
        ** index could potentially invoke a REPLACE conflict resolution
        ** action, then we need to open all indices because we might need
        ** to be deleting some records.
        */
        if ( 0 == okOnePass ) sqlite3OpenTable( pParse, iCur, iDb, pTab, OP_OpenWrite );
        if ( onError == OE_Replace )
        {
          openAll = true;
        }
        else
        {
          openAll = false;
          for ( pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext )
          {
            if ( pIdx.onError == OE_Replace )
            {
              openAll = true;
              break;
            }
          }
        }
        for ( i = 0, pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext, i++ )
        {
          if ( openAll || aRegIdx[i] > 0 )
          {
            KeyInfo pKey = sqlite3IndexKeyinfo( pParse, pIdx );
            sqlite3VdbeAddOp4( v, OP_OpenWrite, iCur + i + 1, pIdx.tnum, iDb,
            pKey, P4_KEYINFO_HANDOFF );
            Debug.Assert( pParse.nTab > iCur + i + 1 );
          }
        }
      }

      /* Jump back to this point if a trigger encounters an IGNORE constraint. */
      if ( pTrigger != null )
      {
        sqlite3VdbeResolveLabel( v, addr );
      }

      /* Top of the update loop */
      if ( okOnePass != 0 )
      {
        int a1 = sqlite3VdbeAddOp1( v, OP_NotNull, regOldRowid );
        addr = sqlite3VdbeAddOp0( v, OP_Goto );
        sqlite3VdbeJumpHere( v, a1 );
      }
      else
      {
        addr = sqlite3VdbeAddOp3( v, OP_RowSetRead, regRowSet, 0, regOldRowid );
      }

      if ( pTrigger != null )
      {
        int regRowid;
        int regRow;
        int regCols;

        /* Make cursor iCur point to the record that is being updated.
        */
        sqlite3VdbeAddOp3( v, OP_NotExists, iCur, addr, regOldRowid );

        /* Generate the OLD table
        */
        regRowid = sqlite3GetTempReg( pParse );
        regRow = sqlite3GetTempReg( pParse );
        sqlite3VdbeAddOp2( v, OP_Rowid, iCur, regRowid );
        if ( old_col_mask == 0 )
        {
          sqlite3VdbeAddOp2( v, OP_Null, 0, regRow );
        }
        else
        {
          sqlite3VdbeAddOp2( v, OP_RowData, iCur, regRow );
        }
        sqlite3VdbeAddOp3( v, OP_Insert, oldIdx, regRow, regRowid );

        /* Generate the NEW table
        */
        if ( chngRowid )
        {
          sqlite3ExprCodeAndCache( pParse, pRowidExpr, regRowid );
          sqlite3VdbeAddOp1( v, OP_MustBeInt, regRowid );
        }
        else
        {
          sqlite3VdbeAddOp2( v, OP_Rowid, iCur, regRowid );
        }
        regCols = sqlite3GetTempRange( pParse, pTab.nCol );
        for ( i = 0 ; i < pTab.nCol ; i++ )
        {
          if ( i == pTab.iPKey )
          {
            sqlite3VdbeAddOp2( v, OP_Null, 0, regCols + i );
            continue;
          }
          j = aXRef[i];
          if ( ( i < 32 && ( new_col_mask & ( (u32)1 << i ) ) != 0 ) || new_col_mask == 0xffffffff )
          {
            if ( j < 0 )
            {
              sqlite3VdbeAddOp3( v, OP_Column, iCur, i, regCols + i );
              sqlite3ColumnDefault( v, pTab, i, -1 );
            }
            else
            {
              sqlite3ExprCodeAndCache( pParse, pChanges.a[j].pExpr, regCols + i );
            }
          }
          else
          {
            sqlite3VdbeAddOp2( v, OP_Null, 0, regCols + i );
          }
        }
        sqlite3VdbeAddOp3( v, OP_MakeRecord, regCols, pTab.nCol, regRow );
        if ( !isView )
        {
          sqlite3TableAffinityStr( v, pTab );
          sqlite3ExprCacheAffinityChange( pParse, regCols, pTab.nCol );
        }
        sqlite3ReleaseTempRange( pParse, regCols, pTab.nCol );
        /* if( pParse.nErr ) goto update_cleanup; */
        sqlite3VdbeAddOp3( v, OP_Insert, newIdx, regRow, regRowid );
        sqlite3ReleaseTempReg( pParse, regRowid );
        sqlite3ReleaseTempReg( pParse, regRow );

        sqlite3VdbeAddOp2( v, OP_Goto, 0, iBeginBeforeTrigger );
        sqlite3VdbeJumpHere( v, iEndBeforeTrigger );
      }

      if ( !isView )
      {

        /* Loop over every record that needs updating.  We have to load
        ** the old data for each record to be updated because some columns
        ** might not change and we will need to copy the old value.
        ** Also, the old data is needed to delete the old index entries.
        ** So make the cursor point at the old record.
        */
        sqlite3VdbeAddOp3( v, OP_NotExists, iCur, addr, regOldRowid );

        /* If the record number will change, push the record number as it
        ** will be after the update. (The old record number is currently
        ** on top of the stack.)
        */
        if ( chngRowid )
        {
          sqlite3ExprCode( pParse, pRowidExpr, regNewRowid );
          sqlite3VdbeAddOp1( v, OP_MustBeInt, regNewRowid );
        }

        /* Compute new data for this record.
        */
        for ( i = 0 ; i < pTab.nCol ; i++ )
        {
          if ( i == pTab.iPKey )
          {
            sqlite3VdbeAddOp2( v, OP_Null, 0, regData + i );
            continue;
          }
          j = aXRef[i];
          if ( j < 0 )
          {
            sqlite3VdbeAddOp3( v, OP_Column, iCur, i, regData + i );
            sqlite3ColumnDefault( v, pTab, i, regData + i );
          }
          else
          {
            sqlite3ExprCode( pParse, pChanges.a[j].pExpr, regData + i );
          }
        }

        /* Do constraint checks
        */
        iDummy = 0;
        sqlite3GenerateConstraintChecks( pParse, pTab, iCur, regNewRowid,
           aRegIdx, chngRowid, true,
           onError, addr, ref iDummy );

        /* Delete the old indices for the current record.
        */
        j1 = sqlite3VdbeAddOp3( v, OP_NotExists, iCur, 0, regOldRowid );
        sqlite3GenerateRowIndexDelete( pParse, pTab, iCur, aRegIdx );

        /* If changing the record number, delete the old record.
        */
        if ( chngRowid )
        {
          sqlite3VdbeAddOp2( v, OP_Delete, iCur, 0 );
        }
        sqlite3VdbeJumpHere( v, j1 );

        /* Create the new index entries and the new record.
        */
        sqlite3CompleteInsertion( pParse, pTab, iCur, regNewRowid,
        aRegIdx, true, -1, false, false );
      }

      /* Increment the row counter
      */
      if ( ( db.flags & SQLITE_CountRows ) != 0 && pParse.trigStack == null )
      {
        sqlite3VdbeAddOp2( v, OP_AddImm, regRowCount, 1 );
      }

      /* If there are triggers, close all the cursors after each iteration
      ** through the loop.  The fire the after triggers.
      */
      if ( pTrigger != null )
      {
        sqlite3VdbeAddOp2( v, OP_Goto, 0, iBeginAfterTrigger );
        sqlite3VdbeJumpHere( v, iEndAfterTrigger );
      }

      /* Repeat the above with the next record to be updated, until
      ** all record selected by the WHERE clause have been updated.
      */
      sqlite3VdbeAddOp2( v, OP_Goto, 0, addr );
      sqlite3VdbeJumpHere( v, addr );

      /* Close all tables */
      for ( i = 0, pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext, i++ )
      {
        if ( openAll || aRegIdx[i] > 0 )
        {
          sqlite3VdbeAddOp2( v, OP_Close, iCur + i + 1, 0 );
        }
      }
      sqlite3VdbeAddOp2( v, OP_Close, iCur, 0 );
      if ( pTrigger != null )
      {
        sqlite3VdbeAddOp2( v, OP_Close, newIdx, 0 );
        sqlite3VdbeAddOp2( v, OP_Close, oldIdx, 0 );
      }

      /* Update the sqlite_sequence table by storing the content of the
      ** maximum rowid counter values recorded while inserting into
      ** autoincrement tables.
      */
      if ( pParse.nested == 0 && pParse.trigStack == null )
      {
        sqlite3AutoincrementEnd( pParse );
      }

      /*
      ** Return the number of rows that were changed. If this routine is
      ** generating code because of a call to sqlite3NestedParse(), do not
      ** invoke the callback function.
      */
      if ( ( db.flags & SQLITE_CountRows ) != 0 && pParse.trigStack == null && pParse.nested == 0 )
      {
        sqlite3VdbeAddOp2( v, OP_ResultRow, regRowCount, 1 );
        sqlite3VdbeSetNumCols( v, 1 );
        sqlite3VdbeSetColName( v, 0, COLNAME_NAME, "rows updated", SQLITE_STATIC );
      }

update_cleanup:
#if !SQLITE_OMIT_AUTHORIZATION
sqlite3AuthContextPop(sContext);
#endif
      //sqlite3DbFree( db, ref  aRegIdx );
      //sqlite3DbFree( db, ref  aXRef );
      sqlite3SrcListDelete( db, ref pTabList );
      sqlite3ExprListDelete( db, ref pChanges );
      sqlite3ExprDelete( db, ref pWhere );
      return;
    }

#if !SQLITE_OMIT_VIRTUALTABLE
/*
** Generate code for an UPDATE of a virtual table.
**
** The strategy is that we create an ephemerial table that contains
** for each row to be changed:
**
**   (A)  The original rowid of that row.
**   (B)  The revised rowid for the row. (note1)
**   (C)  The content of every column in the row.
**
** Then we loop over this ephemeral table and for each row in
** the ephermeral table call VUpdate.
**
** When finished, drop the ephemeral table.
**
** (note1) Actually, if we know in advance that (A) is always the same
** as (B) we only store (A), then duplicate (A) when pulling
** it out of the ephemeral table before calling VUpdate.
*/
static void updateVirtualTable(
Parse pParse,       /* The parsing context */
SrcList pSrc,       /* The virtual table to be modified */
Table pTab,         /* The virtual table */
ExprList pChanges,  /* The columns to change in the UPDATE statement */
Expr pRowid,        /* Expression used to recompute the rowid */
int aXRef,          /* Mapping from columns of pTab to entries in pChanges */
Expr pWhere         /* WHERE clause of the UPDATE statement */
)
{
Vdbe v = pParse.pVdbe;  /* Virtual machine under construction */
ExprList pEList = 0;     /* The result set of the SELECT statement */
Select pSelect = 0;      /* The SELECT statement */
Expr pExpr;              /* Temporary expression */
int ephemTab;             /* Table holding the result of the SELECT */
int i;                    /* Loop counter */
int addr;                 /* Address of top of loop */
int iReg;                 /* First register in set passed to OP_VUpdate */
sqlite3 db = pParse.db; /* Database connection */
const char *pVTab = (const char*)sqlite3GetVTable(db, pTab);
SelectDest dest;

/* Construct the SELECT statement that will find the new values for
** all updated rows.
*/
pEList = sqlite3ExprListAppend(pParse, 0,
sqlite3CreateIdExpr(pParse, "_rowid_"));
if( pRowid ){
pEList = sqlite3ExprListAppend(pParse, pEList,
sqlite3ExprDup(db, pRowid,0), 0);
}
Debug.Assert( pTab.iPKey<0 );
for(i=0; i<pTab.nCol; i++){
if( aXRef[i]>=0 ){
pExpr = sqlite3ExprDup(db, pChanges.a[aXRef[i]].pExpr,0);
}else{
pExpr = sqlite3CreateIdExpr(pParse, pTab.aCol[i].zName);
}
pEList = sqlite3ExprListAppend(pParse, pEList, pExpr);
}
pSelect = sqlite3SelectNew(pParse, pEList, pSrc, pWhere, 0, 0, 0, 0, 0, 0);

/* Create the ephemeral table into which the update results will
** be stored.
*/
Debug.Assert( v );
ephemTab = pParse.nTab++;
sqlite3VdbeAddOp2(v, OP_OpenEphemeral, ephemTab, pTab.nCol+1+(pRowid!=0));

/* fill the ephemeral table
*/
sqlite3SelectDestInit(dest, SRT_Table, ephemTab);
sqlite3Select(pParse, pSelect, ref dest);

/* Generate code to scan the ephemeral table and call VUpdate. */
iReg = ++pParse.nMem;
pParse.nMem += pTab.nCol+1;
addr = sqlite3VdbeAddOp2(v, OP_Rewind, ephemTab, 0);
sqlite3VdbeAddOp3(v, OP_Column,  ephemTab, 0, iReg);
sqlite3VdbeAddOp3(v, OP_Column, ephemTab, (pRowid
1:0), iReg+1);
for(i=0; i<pTab.nCol; i++){
sqlite3VdbeAddOp3(v, OP_Column, ephemTab, i+1+(pRowid!=0), iReg+2+i);
}
sqlite3VtabMakeWritable(pParse, pTab);
sqlite3VdbeAddOp4(v, OP_VUpdate, 0, pTab.nCol+2, iReg, pVTab, P4_VTAB);
sqlite3VdbeAddOp2(v, OP_Next, ephemTab, addr+1);
sqlite3VdbeJumpHere(v, addr);
sqlite3VdbeAddOp2(v, OP_Close, ephemTab, 0);

/* Cleanup */
sqlite3SelectDelete(pSelect);
}
#endif // * SQLITE_OMIT_VIRTUALTABLE */

    /* Make sure "isView" gets undefined in case this file becomes part of
** the amalgamation - so that subsequent files do not see isView as a
** macro. */
    //#undef isView
  }
}
