using System;
using System.Diagnostics;
using System.Text;

using u8 = System.Byte;
using u32 = System.UInt32;

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
    ** This file contains C code routines that are called by the parser
    ** in order to generate code for DELETE FROM statements.
    **
    ** $Id: delete.c,v 1.207 2009/08/08 18:01:08 drh Exp $
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
    ** Look up every table that is named in pSrc.  If any table is not found,
    ** add an error message to pParse.zErrMsg and return NULL.  If all tables
    ** are found, return a pointer to the last table.
    */
    static Table sqlite3SrcListLookup( Parse pParse, SrcList pSrc )
    {
      SrcList_item pItem = pSrc.a[0];
      Table pTab;
      Debug.Assert( pItem != null && pSrc.nSrc == 1 );
      pTab = sqlite3LocateTable( pParse, 0, pItem.zName, pItem.zDatabase );
      sqlite3DeleteTable( ref pItem.pTab );
      pItem.pTab = pTab;
      if ( pTab != null )
      {
        pTab.nRef++;
      }
      if ( sqlite3IndexedByLookup( pParse, pItem ) != 0 )
      {
        pTab = null;
      }
      return pTab;
    }

    /*
    ** Check to make sure the given table is writable.  If it is not
    ** writable, generate an error message and return 1.  If it is
    ** writable return 0;
    */
    static bool sqlite3IsReadOnly( Parse pParse, Table pTab, int viewOk )
    {
      /* A table is not writable under the following circumstances:
      **
      **   1) It is a virtual table and no implementation of the xUpdate method
      **      has been provided, or
      **   2) It is a system table (i.e. sqlite_master), this call is not
      **      part of a nested parse and writable_schema pragma has not
      **      been specified.
      **
      ** In either case leave an error message in pParse and return non-zero.
      */
      if (
         ( IsVirtual( pTab )
          && sqlite3GetVTable( pParse.db, pTab ).pMod.pModule.xUpdate == null )
        || ( ( pTab.tabFlags & TF_Readonly ) != 0
      && ( pParse.db.flags & SQLITE_WriteSchema ) == 0
      && pParse.nested == 0 )
      )
      {
        sqlite3ErrorMsg( pParse, "table %s may not be modified", pTab.zName );
        return true;
      }

#if !SQLITE_OMIT_VIEW
      if ( viewOk == 0 && pTab.pSelect != null )
      {
        sqlite3ErrorMsg( pParse, "cannot modify %s because it is a view", pTab.zName );
        return true;
      }
#endif
      return false;
    }


#if !SQLITE_OMIT_VIEW && !SQLITE_OMIT_TRIGGER
    /*
** Evaluate a view and store its result in an ephemeral table.  The
** pWhere argument is an optional WHERE clause that restricts the
** set of rows in the view that are to be added to the ephemeral table.
*/
    static void sqlite3MaterializeView(
    Parse pParse,      /* Parsing context */
    Table pView,       /* View definition */
    Expr pWhere,       /* Optional WHERE clause to be added */
    int iCur           /* VdbeCursor number for ephemerial table */
    )
    {
      SelectDest dest = new SelectDest();
      Select pDup;
      sqlite3 db = pParse.db;

      pDup = sqlite3SelectDup( db, pView.pSelect, 0 );
      if ( pWhere != null )
      {
        SrcList pFrom;

        pWhere = sqlite3ExprDup( db, pWhere, 0 );
        pFrom = sqlite3SrcListAppend( db, null, null, null );
        //if ( pFrom != null )
        //{
          Debug.Assert( pFrom.nSrc == 1 );
          pFrom.a[0].zAlias = pView.zName;// sqlite3DbStrDup( db, pView.zName );
          pFrom.a[0].pSelect = pDup;
          Debug.Assert( pFrom.a[0].pOn == null );
          Debug.Assert( pFrom.a[0].pUsing == null );
        //}
        //else
        //{
        //  sqlite3SelectDelete( db, ref pDup );
        //}
        pDup = sqlite3SelectNew( pParse, null, pFrom, pWhere, null, null, null, 0, null, null );
      }
      sqlite3SelectDestInit( dest, SRT_EphemTab, iCur );
      sqlite3Select( pParse, pDup, ref dest );
      sqlite3SelectDelete( db, ref pDup );
    }
#endif //* !SQLITE_OMIT_VIEW) && !SQLITE_OMIT_TRIGGER) */

#if (SQLITE_ENABLE_UPDATE_DELETE_LIMIT) && !(SQLITE_OMIT_SUBQUERY)
/*
** Generate an expression tree to implement the WHERE, ORDER BY,
** and LIMIT/OFFSET portion of DELETE and UPDATE statements.
**
**     DELETE FROM table_wxyz WHERE a<5 ORDER BY a LIMIT 1;
**                            \__________________________/
**                               pLimitWhere (pInClause)
*/
Expr sqlite3LimitWhere(
Parse pParse,               /* The parser context */
SrcList pSrc,               /* the FROM clause -- which tables to scan */
Expr pWhere,                /* The WHERE clause.  May be null */
ExprList pOrderBy,          /* The ORDER BY clause.  May be null */
Expr pLimit,                /* The LIMIT clause.  May be null */
Expr pOffset,               /* The OFFSET clause.  May be null */
char zStmtType              /* Either DELETE or UPDATE.  For error messages. */
){
Expr pWhereRowid = null;    /* WHERE rowid .. */
Expr pInClause = null;      /* WHERE rowid IN ( select ) */
Expr pSelectRowid = null;   /* SELECT rowid ... */
ExprList pEList = null;     /* Expression list contaning only pSelectRowid */
SrcList pSelectSrc = null;  /* SELECT rowid FROM x ... (dup of pSrc) */
Select pSelect = null;      /* Complete SELECT tree */

/* Check that there isn't an ORDER BY without a LIMIT clause.
*/
if( pOrderBy!=null && (pLimit == null) ) {
sqlite3ErrorMsg(pParse, "ORDER BY without LIMIT on %s", zStmtType);
pParse.parseError = 1;
goto limit_where_cleanup_2;
}

/* We only need to generate a select expression if there
** is a limit/offset term to enforce.
*/
if ( pLimit == null )
{
/* if pLimit is null, pOffset will always be null as well. */
Debug.Assert( pOffset == null );
return pWhere;
}

/* Generate a select expression tree to enforce the limit/offset
** term for the DELETE or UPDATE statement.  For example:
**   DELETE FROM table_a WHERE col1=1 ORDER BY col2 LIMIT 1 OFFSET 1
** becomes:
**   DELETE FROM table_a WHERE rowid IN (
**     SELECT rowid FROM table_a WHERE col1=1 ORDER BY col2 LIMIT 1 OFFSET 1
**   );
*/

pSelectRowid = sqlite3PExpr( pParse, TK_ROW, null, null, null );
if( pSelectRowid == null ) goto limit_where_cleanup_2;
pEList = sqlite3ExprListAppend( pParse, null, pSelectRowid);
if( pEList == null ) goto limit_where_cleanup_2;

/* duplicate the FROM clause as it is needed by both the DELETE/UPDATE tree
** and the SELECT subtree. */
pSelectSrc = sqlite3SrcListDup(pParse.db, pSrc,0);
if( pSelectSrc == null ) {
sqlite3ExprListDelete(pParse.db, pEList);
goto limit_where_cleanup_2;
}

/* generate the SELECT expression tree. */
pSelect = sqlite3SelectNew( pParse, pEList, pSelectSrc, pWhere, null, null,
pOrderBy, 0, pLimit, pOffset );
if( pSelect == null ) return null;

/* now generate the new WHERE rowid IN clause for the DELETE/UDPATE */
pWhereRowid = sqlite3PExpr( pParse, TK_ROW, null, null, null );
if( pWhereRowid == null ) goto limit_where_cleanup_1;
pInClause = sqlite3PExpr( pParse, TK_IN, pWhereRowid, null, null );
if( pInClause == null ) goto limit_where_cleanup_1;

pInClause->x.pSelect = pSelect;
pInClause->flags |= EP_xIsSelect;
sqlite3ExprSetHeight(pParse, pInClause);
return pInClause;

/* something went wrong. clean up anything allocated. */
limit_where_cleanup_1:
sqlite3SelectDelete(pParse.db, pSelect);
return null;

limit_where_cleanup_2:
sqlite3ExprDelete(pParse.db, ref pWhere);
sqlite3ExprListDelete(pParse.db, pOrderBy);
sqlite3ExprDelete(pParse.db, ref pLimit);
sqlite3ExprDelete(pParse.db, ref pOffset);
return null;
}
#endif //* defined(SQLITE_ENABLE_UPDATE_DELETE_LIMIT) && !defined(SQLITE_OMIT_SUBQUERY) */

    /*
** Generate code for a DELETE FROM statement.
**
**     DELETE FROM table_wxyz WHERE a<5 AND b NOT NULL;
**                 \________/       \________________/
**                  pTabList              pWhere
*/
    static void sqlite3DeleteFrom(
    Parse pParse,          /* The parser context */
    SrcList pTabList,      /* The table from which we should delete things */
    Expr pWhere            /* The WHERE clause.  May be null */
    )
    {
      Vdbe v;                /* The virtual database engine */
      Table pTab;            /* The table from which records will be deleted */
      string zDb;            /* Name of database holding pTab */
      int end, addr = 0;     /* A couple addresses of generated code */
      int i;                 /* Loop counter */
      WhereInfo pWInfo;      /* Information about the WHERE clause */
      Index pIdx;            /* For looping over indices of the table */
      int iCur;              /* VDBE VdbeCursor number for pTab */
      sqlite3 db;            /* Main database structure */
      AuthContext sContext;  /* Authorization context */
      int oldIdx = -1;       /* VdbeCursor for the OLD table of AFTER triggers */
      NameContext sNC;       /* Name context to resolve expressions in */
      int iDb;               /* Database number */
      int memCnt = -1;        /* Memory cell used for change counting */
      int rcauth;            /* Value returned by authorization callback */

#if !SQLITE_OMIT_TRIGGER
      bool isView;                 /* True if attempting to delete from a view */
      Trigger pTrigger;            /* List of table triggers, if required */
#endif
      int iBeginAfterTrigger = 0;      /* Address of after trigger program */
      int iEndAfterTrigger = 0;        /* Exit of after trigger program */
      int iBeginBeforeTrigger = 0;     /* Address of before trigger program */
      int iEndBeforeTrigger = 0;       /* Exit of before trigger program */
      u32 old_col_mask = 0;        /* Mask of OLD.* columns in use */

      sContext = new AuthContext();//memset(&sContext, 0, sizeof(sContext));

      db = pParse.db;
      if ( pParse.nErr != 0 /*|| db.mallocFailed != 0 */ )
      {
        goto delete_from_cleanup;
      }
      Debug.Assert( pTabList.nSrc == 1 );

      /* Locate the table which we want to delete.  This table has to be
      ** put in an SrcList structure because some of the subroutines we
      ** will be calling are designed to work with multiple tables and expect
      ** an SrcList* parameter instead of just a Table* parameter.
      */
      pTab = sqlite3SrcListLookup( pParse, pTabList );
      if ( pTab == null ) goto delete_from_cleanup;

      /* Figure out if we have any triggers and if the table being
      ** deleted from is a view
      */
#if !SQLITE_OMIT_TRIGGER
      int iDummy = 0;
      pTrigger = sqlite3TriggersExist( pParse, pTab, TK_DELETE, null, ref iDummy );
      isView = pTab.pSelect != null;
#else
const Trigger pTrigger = null;
isView = false;
#endif
#if SQLITE_OMIT_VIEW
//# undef isView
isView = false;
#endif

      /* If pTab is really a view, make sure it has been initialized.
*/
      if ( sqlite3ViewGetColumnNames( pParse, pTab ) != 0 )
      {
        goto delete_from_cleanup;
      }

      if ( sqlite3IsReadOnly( pParse, pTab, ( pTrigger != null ? 1 : 0 ) ) )
      {
        goto delete_from_cleanup;
      }
      iDb = sqlite3SchemaToIndex( db, pTab.pSchema );
      Debug.Assert( iDb < db.nDb );
      zDb = db.aDb[iDb].zName;
#if !SQLITE_OMIT_AUTHORIZATION
rcauth = sqlite3AuthCheck(pParse, SQLITE_DELETE, pTab->zName, 0, zDb);
#else
      rcauth = SQLITE_OK;
#endif
      Debug.Assert( rcauth == SQLITE_OK || rcauth == SQLITE_DENY || rcauth == SQLITE_IGNORE );
      if ( rcauth == SQLITE_DENY )
      {
        goto delete_from_cleanup;
      }
      Debug.Assert( !isView || pTrigger != null );

      /* Allocate a cursor used to store the old.* data for a trigger.
      */
      if ( pTrigger != null )
      {
        oldIdx = pParse.nTab++;
      }

      /* Assign  cursor number to the table and all its indices.
      */
      Debug.Assert( pTabList.nSrc == 1 );
      iCur = pTabList.a[0].iCursor = pParse.nTab++;
      for ( pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext )
      {
        pParse.nTab++;
      }

#if !SQLITE_OMIT_AUTHORIZATION
/* Start the view context
*/
if( isView ){
sqlite3AuthContextPush(pParse, sContext, pTab.zName);
}
#endif
      /* Begin generating code.
*/
      v = sqlite3GetVdbe( pParse );
      if ( v == null )
      {
        goto delete_from_cleanup;
      }
      if ( pParse.nested == 0 ) sqlite3VdbeCountChanges( v );
      sqlite3BeginWriteOperation( pParse, pTrigger != null ? 1 : 0, iDb );

#if !SQLITE_OMIT_TRIGGER
      if ( pTrigger != null )
      {
        int orconf = ( ( pParse.trigStack != null ) ? pParse.trigStack.orconf : OE_Default );
        int iGoto = sqlite3VdbeAddOp0( v, OP_Goto );
        addr = sqlite3VdbeMakeLabel( v );

        iBeginBeforeTrigger = sqlite3VdbeCurrentAddr( v );
        u32 Ref_0 = 0;
        sqlite3CodeRowTrigger( pParse, pTrigger, TK_DELETE, null,
        TRIGGER_BEFORE, pTab, -1, oldIdx, orconf, addr, ref old_col_mask, ref Ref_0 );
        iEndBeforeTrigger = sqlite3VdbeAddOp0( v, OP_Goto );

        iBeginAfterTrigger = sqlite3VdbeCurrentAddr( v );
        Ref_0 = 0;
        sqlite3CodeRowTrigger( pParse, pTrigger, TK_DELETE, null,
        TRIGGER_AFTER, pTab, -1, oldIdx, orconf, addr, ref old_col_mask, ref Ref_0 );
        iEndAfterTrigger = sqlite3VdbeAddOp0( v, OP_Goto );

        sqlite3VdbeJumpHere( v, iGoto );
      }
#endif

      /* If we are trying to delete from a view, realize that view into
** a ephemeral table.
*/
#if !(SQLITE_OMIT_VIEW) && !(SQLITE_OMIT_TRIGGER)
      if ( isView )
      {
        sqlite3MaterializeView( pParse, pTab, pWhere, iCur );
      }

      /* Resolve the column names in the WHERE clause.
      */
      sNC = new NameContext();// memset( &sNC, 0, sizeof( sNC ) );
      sNC.pParse = pParse;
      sNC.pSrcList = pTabList;
      if ( sqlite3ResolveExprNames( sNC, ref pWhere ) != 0 )
      {
        goto delete_from_cleanup;
      }
#endif

      /* Initialize the counter of the number of rows deleted, if
** we are counting rows.
*/
      if ( ( db.flags & SQLITE_CountRows ) != 0 )
      {
        memCnt = ++pParse.nMem;
        sqlite3VdbeAddOp2( v, OP_Integer, 0, memCnt );
      }

#if !SQLITE_OMIT_TRUNCATE_OPTIMIZATION
      /* Special case: A DELETE without a WHERE clause deletes everything.
** It is easier just to erase the whole table.  Note, however, that
** this means that the row change count will be incorrect.
*/
      if ( rcauth == SQLITE_OK && pWhere == null && null == pTrigger && !IsVirtual( pTab ) )
      {
        Debug.Assert( !isView );
        sqlite3VdbeAddOp4( v, OP_Clear, pTab.tnum, iDb, memCnt,
              pTab.zName, P4_STATIC );
        for ( pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext )
        {
          Debug.Assert( pIdx.pSchema == pTab.pSchema );
          sqlite3VdbeAddOp2( v, OP_Clear, pIdx.tnum, iDb );
        }
      }
      else
#endif //* SQLITE_OMIT_TRUNCATE_OPTIMIZATION */
      /* The usual case: There is a WHERE clause so we have to scan through
** the table and pick which records to delete.
*/
      {
        int iRowid = ++pParse.nMem;     /* Used for storing rowid values. */
        int iRowSet = ++pParse.nMem;    /* Register for rowset of rows to delete */
        int regRowid;                   /* Actual register containing rowids */

        /* Collect rowids of every row to be deleted.
        */
        sqlite3VdbeAddOp2( v, OP_Null, 0, iRowSet );
        ExprList elDummy = null;
        pWInfo = sqlite3WhereBegin( pParse, pTabList, pWhere, ref elDummy, WHERE_DUPLICATES_OK );
        if ( pWInfo == null ) goto delete_from_cleanup;
        regRowid = sqlite3ExprCodeGetColumn( pParse, pTab, -1, iCur, iRowid, false );
        sqlite3VdbeAddOp2( v, OP_RowSetAdd, iRowSet, regRowid );
        if ( ( db.flags & SQLITE_CountRows ) != 0 )
        {
          sqlite3VdbeAddOp2( v, OP_AddImm, memCnt, 1 );
        }

        sqlite3WhereEnd( pWInfo );

        /* Open the pseudo-table used to store OLD if there are triggers.
        */
        if ( pTrigger != null )
        {
          sqlite3VdbeAddOp3( v, OP_OpenPseudo, oldIdx, 0, pTab.nCol );
        }

        /* Delete every item whose key was written to the list during the
        ** database scan.  We have to delete items after the scan is complete
        ** because deleting an item can change the scan order.
        */
        end = sqlite3VdbeMakeLabel( v );

        if ( !isView )
        {
          /* Open cursors for the table we are deleting from and
          ** all its indices.
          */
          sqlite3OpenTableAndIndices( pParse, pTab, iCur, OP_OpenWrite );
        }

        /* This is the beginning of the delete loop. If a trigger encounters
        ** an IGNORE constraint, it jumps back to here.
        */
        if ( pTrigger != null )
        {
          sqlite3VdbeResolveLabel( v, addr );
        }
        addr = sqlite3VdbeAddOp3( v, OP_RowSetRead, iRowSet, end, iRowid );

        if ( pTrigger != null )
        {
          int iData = ++pParse.nMem;   /* For storing row data of OLD table */

          /* If the record is no longer present in the table, jump to the
          ** next iteration of the loop through the contents of the fifo.
          */
          sqlite3VdbeAddOp3( v, OP_NotExists, iCur, addr, iRowid );

          /* Populate the OLD.* pseudo-table */
          if ( old_col_mask != 0 )
          {
            sqlite3VdbeAddOp2( v, OP_RowData, iCur, iData );
          }
          else
          {
            sqlite3VdbeAddOp2( v, OP_Null, 0, iData );
          }
          sqlite3VdbeAddOp3( v, OP_Insert, oldIdx, iData, iRowid );

          /* Jump back and run the BEFORE triggers */
          sqlite3VdbeAddOp2( v, OP_Goto, 0, iBeginBeforeTrigger );
          sqlite3VdbeJumpHere( v, iEndBeforeTrigger );
        }

        if ( !isView )
        {
          /* Delete the row */
#if !SQLITE_OMIT_VIRTUALTABLE
if( IsVirtual(pTab) ){
const char *pVTab = (const char *)sqlite3GetVTable(db, pTab);
sqlite3VtabMakeWritable(pParse, pTab);
sqlite3VdbeAddOp4(v, OP_VUpdate, 0, 1, iRowid, pVTab, P4_VTAB);
}else

#endif
          {
            sqlite3GenerateRowDelete( pParse, pTab, iCur, iRowid, pParse.nested == 0 ? 1 : 0 );
          }
        }

        /* If there are row triggers, close all cursors then invoke
        ** the AFTER triggers
        */
        if ( pTrigger != null )
        {
          /* Jump back and run the AFTER triggers */
          sqlite3VdbeAddOp2( v, OP_Goto, 0, iBeginAfterTrigger );
          sqlite3VdbeJumpHere( v, iEndAfterTrigger );
        }

        /* End of the delete loop */
        sqlite3VdbeAddOp2( v, OP_Goto, 0, addr );
        sqlite3VdbeResolveLabel( v, end );

        /* Close the cursors after the loop if there are no row triggers */
        if ( !isView && !IsVirtual( pTab ) )
        {
          for ( i = 1, pIdx = pTab.pIndex ; pIdx != null ; i++, pIdx = pIdx.pNext )
          {
            sqlite3VdbeAddOp2( v, OP_Close, iCur + i, pIdx.tnum );
          }
          sqlite3VdbeAddOp1( v, OP_Close, iCur );
        }
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
      ** Return the number of rows that were deleted. If this routine is
      ** generating code because of a call to sqlite3NestedParse(), do not
      ** invoke the callback function.
      */
      if ( ( db.flags & SQLITE_CountRows ) != 0 && pParse.nested == 0 && pParse.trigStack == null )
      {
        sqlite3VdbeAddOp2( v, OP_ResultRow, memCnt, 1 );
        sqlite3VdbeSetNumCols( v, 1 );
        sqlite3VdbeSetColName( v, 0, COLNAME_NAME, "rows deleted", SQLITE_STATIC );
      }

delete_from_cleanup:
#if !SQLITE_OMIT_AUTHORIZATION
sqlite3AuthContextPop(sContext);
#endif
      sqlite3SrcListDelete( db, ref pTabList );
      sqlite3ExprDelete( db, ref pWhere );
      return;
    }

    /*
    ** This routine generates VDBE code that causes a single row of a
    ** single table to be deleted.
    **
    ** The VDBE must be in a particular state when this routine is called.
    ** These are the requirements:
    **
    **   1.  A read/write cursor pointing to pTab, the table containing the row
    **       to be deleted, must be opened as cursor number "base".
    **
    **   2.  Read/write cursors for all indices of pTab must be open as
    **       cursor number base+i for the i-th index.
    **
    **   3.  The record number of the row to be deleted must be stored in
    **       memory cell iRowid.
    **
    ** This routine pops the top of the stack to remove the record number
    ** and then generates code to remove both the table record and all index
    ** entries that point to that record.
    */
    static void sqlite3GenerateRowDelete(
    Parse pParse,     /* Parsing context */
    Table pTab,       /* Table containing the row to be deleted */
    int iCur,          /* VdbeCursor number for the table */
    int iRowid,        /* Memory cell that contains the rowid to delete */
    int count          /* Increment the row change counter */
    )
    {
      int addr;
      Vdbe v;

      v = pParse.pVdbe;
      addr = sqlite3VdbeAddOp3( v, OP_NotExists, iCur, 0, iRowid );
      sqlite3GenerateRowIndexDelete( pParse, pTab, iCur, 0 );
      sqlite3VdbeAddOp2( v, OP_Delete, iCur, ( count > 0 ? (int)OPFLAG_NCHANGE : 0 ) );
      if ( count > 0 )
      {
        sqlite3VdbeChangeP4( v, -1, pTab.zName, P4_STATIC );
      }
      sqlite3VdbeJumpHere( v, addr );
    }

    /*
    ** This routine generates VDBE code that causes the deletion of all
    ** index entries associated with a single row of a single table.
    **
    ** The VDBE must be in a particular state when this routine is called.
    ** These are the requirements:
    **
    **   1.  A read/write cursor pointing to pTab, the table containing the row
    **       to be deleted, must be opened as cursor number "iCur".
    **
    **   2.  Read/write cursors for all indices of pTab must be open as
    **       cursor number iCur+i for the i-th index.
    **
    **   3.  The "iCur" cursor must be pointing to the row that is to be
    **       deleted.
    */
    static void sqlite3GenerateRowIndexDelete(
    Parse pParse,     /* Parsing and code generating context */
    Table pTab,       /* Table containing the row to be deleted */
    int iCur,         /* VdbeCursor number for the table */
    int nothing       /* Only delete if aRegIdx!=0 && aRegIdx[i]>0 */
    )
    {
      int[] aRegIdx = null;
      sqlite3GenerateRowIndexDelete( pParse, pTab, iCur, aRegIdx );
    }
    static void sqlite3GenerateRowIndexDelete(
    Parse pParse,     /* Parsing and code generating context */
    Table pTab,       /* Table containing the row to be deleted */
    int iCur,          /* VdbeCursor number for the table */
    int[] aRegIdx       /* Only delete if aRegIdx!=0 && aRegIdx[i]>0 */
    )
    {
      int i;
      Index pIdx;
      int r1;

      for ( i = 1, pIdx = pTab.pIndex ; pIdx != null ; i++, pIdx = pIdx.pNext )
      {
        if ( aRegIdx != null && aRegIdx[i - 1] == 0 ) continue;
        r1 = sqlite3GenerateIndexKey( pParse, pIdx, iCur, 0, false );
        sqlite3VdbeAddOp3( pParse.pVdbe, OP_IdxDelete, iCur + i, r1, pIdx.nColumn + 1 );
      }
    }

    /*
    ** Generate code that will assemble an index key and put it in register
    ** regOut.  The key with be for index pIdx which is an index on pTab.
    ** iCur is the index of a cursor open on the pTab table and pointing to
    ** the entry that needs indexing.
    **
    ** Return a register number which is the first in a block of
    ** registers that holds the elements of the index key.  The
    ** block of registers has already been deallocated by the time
    ** this routine returns.
    */
    static int sqlite3GenerateIndexKey(
    Parse pParse,     /* Parsing context */
    Index pIdx,       /* The index for which to generate a key */
    int iCur,         /* VdbeCursor number for the pIdx.pTable table */
    int regOut,       /* Write the new index key to this register */
    bool doMakeRec    /* Run the OP_MakeRecord instruction if true */
    )
    {
      Vdbe v = pParse.pVdbe;
      int j;
      Table pTab = pIdx.pTable;
      int regBase;
      int nCol;

      nCol = pIdx.nColumn;
      regBase = sqlite3GetTempRange( pParse, nCol + 1 );
      sqlite3VdbeAddOp2( v, OP_Rowid, iCur, regBase + nCol );
      for ( j = 0 ; j < nCol ; j++ )
      {
        int idx = pIdx.aiColumn[j];
        if ( idx == pTab.iPKey )
        {
          sqlite3VdbeAddOp2( v, OP_SCopy, regBase + nCol, regBase + j );
        }
        else
        {
          sqlite3VdbeAddOp3( v, OP_Column, iCur, idx, regBase + j );
          sqlite3ColumnDefault( v, pTab, idx, -1 );
        }
      }
      if ( doMakeRec )
      {
        sqlite3VdbeAddOp3( v, OP_MakeRecord, regBase, nCol + 1, regOut );
        sqlite3IndexAffinityStr( v, pIdx );
        sqlite3ExprCacheAffinityChange( pParse, regBase, nCol + 1 );
      }
      sqlite3ReleaseTempRange( pParse, regBase, nCol + 1 );
      return regBase;
    }
    /* Make sure "isView" gets undefined in case this file becomes part of
    ** the amalgamation - so that subsequent files do not see isView as a
    ** macro. */
    //#undef isView
  }
}
