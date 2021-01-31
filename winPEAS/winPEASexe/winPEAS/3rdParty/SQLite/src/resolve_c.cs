using System.Diagnostics;
using Bitmask = System.UInt64;
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;

namespace winPEAS._3rdParty.SQLite.src
{
    public partial class CSSQLite
  {
    /*
    ** 2008 August 18
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
    ** This file contains routines used for walking the parser tree and
    ** resolve all identifiers by associating them with a particular
    ** table and column.
    **
    ** $Id: resolve.c,v 1.30 2009/06/15 23:15:59 drh Exp $
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

    /*
    ** Turn the pExpr expression into an alias for the iCol-th column of the
    ** result set in pEList.
    **
    ** If the result set column is a simple column reference, then this routine
    ** makes an exact copy.  But for any other kind of expression, this
    ** routine make a copy of the result set column as the argument to the
    ** TK_AS operator.  The TK_AS operator causes the expression to be
    ** evaluated just once and then reused for each alias.
    **
    ** The reason for suppressing the TK_AS term when the expression is a simple
    ** column reference is so that the column reference will be recognized as
    ** usable by indices within the WHERE clause processing logic.
    **
    ** Hack:  The TK_AS operator is inhibited if zType[0]=='G'.  This means
    ** that in a GROUP BY clause, the expression is evaluated twice.  Hence:
    **
    **     SELECT random()%5 AS x, count(*) FROM tab GROUP BY x
    **
    ** Is equivalent to:
    **
    **     SELECT random()%5 AS x, count(*) FROM tab GROUP BY random()%5
    **
    ** The result of random()%5 in the GROUP BY clause is probably different
    ** from the result in the result-set.  We might fix this someday.  Or
    ** then again, we might not...
    */
    static void resolveAlias(
    Parse pParse,         /* Parsing context */
    ExprList pEList,      /* A result set */
    int iCol,             /* A column in the result set.  0..pEList.nExpr-1 */
    Expr pExpr,       /* Transform this into an alias to the result set */
    string zType          /* "GROUP" or "ORDER" or "" */
    )
    {
      Expr pOrig;           /* The iCol-th column of the result set */
      Expr pDup;            /* Copy of pOrig */
      sqlite3 db;           /* The database connection */

      Debug.Assert( iCol >= 0 && iCol < pEList.nExpr );
      pOrig = pEList.a[iCol].pExpr;
      Debug.Assert( pOrig != null );
      Debug.Assert( ( pOrig.flags & EP_Resolved ) != 0 );
      db = pParse.db;
      if ( pOrig.op != TK_COLUMN && ( zType.Length == 0 || zType[0] != 'G' ) )
      {
        pDup = sqlite3ExprDup( db, pOrig, 0 );
        pDup = sqlite3PExpr( pParse, TK_AS, pDup, null, null );
        if ( pDup == null ) return;
        if ( pEList.a[iCol].iAlias == 0 )
        {
          pEList.a[iCol].iAlias = (u16)( ++pParse.nAlias );
        }
        pDup.iTable = pEList.a[iCol].iAlias;
      }
      else if ( ExprHasProperty( pOrig, EP_IntValue ) || pOrig.u.zToken == null )
      {
        pDup = sqlite3ExprDup( db, pOrig, 0 );
        if ( pDup == null ) return;
      }
      else
      {
        string zToken = pOrig.u.zToken;
        Debug.Assert( zToken != null );
        pOrig.u.zToken = null;
        pDup = sqlite3ExprDup( db, pOrig, 0 );
        pOrig.u.zToken = zToken;
        if ( pDup == null ) return;
        Debug.Assert( ( pDup.flags & ( EP_Reduced | EP_TokenOnly ) ) == 0 );
        pDup.flags2 |= EP2_MallocedToken;
        pDup.u.zToken = zToken;// sqlite3DbStrDup( db, zToken );
      }
      if ( ( pExpr.flags & EP_ExpCollate ) != 0 )
      {
        pDup.pColl = pExpr.pColl;
        pDup.flags |= EP_ExpCollate;
      }
      sqlite3ExprClear( db, pExpr );
      pExpr.CopyFrom( pDup ); //memcpy(pExpr, pDup, sizeof(*pExpr));
      //sqlite3DbFree( db, ref pDup );
    }

    /*
    ** Given the name of a column of the form X.Y.Z or Y.Z or just Z, look up
    ** that name in the set of source tables in pSrcList and make the pExpr
    ** expression node refer back to that source column.  The following changes
    ** are made to pExpr:
    **
    **    pExpr->iDb           Set the index in db->aDb[] of the database X
    **                         (even if X is implied).
    **    pExpr->iTable        Set to the cursor number for the table obtained
    **                         from pSrcList.
    **    pExpr->pTab          Points to the Table structure of X.Y (even if
    **                         X and/or Y are implied.)
    **    pExpr->iColumn       Set to the column number within the table.
    **    pExpr->op            Set to TK_COLUMN.
    **    pExpr->pLeft         Any expression this points to is deleted
    **    pExpr->pRight        Any expression this points to is deleted.
    **
    ** The zDb variable is the name of the database (the "X").  This value may be
    ** NULL meaning that name is of the form Y.Z or Z.  Any available database
    ** can be used.  The zTable variable is the name of the table (the "Y").  This
    ** value can be NULL if zDb is also NULL.  If zTable is NULL it
    ** means that the form of the name is Z and that columns from any table
    ** can be used.
    **
    ** If the name cannot be resolved unambiguously, leave an error message
    ** in pParse and return WRC_Abort.  Return WRC_Prune on success.
    */
    static int lookupName(
    Parse pParse,       /* The parsing context */
    string zDb,         /* Name of the database containing table, or NULL */
    string zTab,        /* Name of table containing column, or NULL */
    string zCol,        /* Name of the column. */
    NameContext pNC,    /* The name context used to resolve the name */
    Expr pExpr          /* Make this EXPR node point to the selected column */
    )
    {
      int i, j;            /* Loop counters */
      int cnt = 0;                      /* Number of matching column names */
      int cntTab = 0;                   /* Number of matching table names */
      sqlite3 db = pParse.db;         /* The database connection */
      SrcList_item pItem;       /* Use for looping over pSrcList items */
      SrcList_item pMatch = null;  /* The matching pSrcList item */
      NameContext pTopNC = pNC;        /* First namecontext in the list */
      Schema pSchema = null;              /* Schema of the expression */

      Debug.Assert( pNC != null ); /* the name context cannot be NULL. */
      Debug.Assert( zCol != null );    /* The Z in X.Y.Z cannot be NULL */
      Debug.Assert( !ExprHasAnyProperty( pExpr, EP_TokenOnly | EP_Reduced ) );

      /* Initialize the node to no-match */
      pExpr.iTable = -1;
      pExpr.pTab = null;
      ExprSetIrreducible( pExpr );

      /* Start at the inner-most context and move outward until a match is found */
      while ( pNC != null && cnt == 0 )
      {
        ExprList pEList;
        SrcList pSrcList = pNC.pSrcList;

        if ( pSrcList != null )
        {
          for ( i = 0 ; i < pSrcList.nSrc ; i++ )//, pItem++ )
          {
            pItem = pSrcList.a[i];
            Table pTab;
            int iDb;
            Column pCol;

            pTab = pItem.pTab;
            Debug.Assert( pTab != null && pTab.zName != null );
            iDb = sqlite3SchemaToIndex( db, pTab.pSchema );
            Debug.Assert( pTab.nCol > 0 );
            if ( zTab != null )
            {
              if ( pItem.zAlias != null )
              {
                string zTabName = pItem.zAlias;
                if ( sqlite3StrICmp( zTabName, zTab ) != 0 ) continue;
              }
              else
              {
                string zTabName = pTab.zName;
                if ( NEVER( zTabName == null ) || sqlite3StrICmp( zTabName, zTab ) != 0 )
                {
                  continue;
                }
                if ( zDb != null && sqlite3StrICmp( db.aDb[iDb].zName, zDb ) != 0 )
                {
                  continue;
                }
              }
            }
            if ( 0 == ( cntTab++ ) )
            {
              pExpr.iTable = pItem.iCursor;
              pExpr.pTab = pTab;
              pSchema = pTab.pSchema;
              pMatch = pItem;
            }
            for ( j = 0 ; j < pTab.nCol ; j++ )//, pCol++ )
            {
              pCol = pTab.aCol[j];
              if ( sqlite3StrICmp( pCol.zName, zCol ) == 0 )
              {
                IdList pUsing;
                cnt++;
                pExpr.iTable = pItem.iCursor;
                pExpr.pTab = pTab;
                pMatch = pItem;
                pSchema = pTab.pSchema;
                /* Substitute the rowid (column -1) for the INTEGER PRIMARY KEY */
                pExpr.iColumn = (short)( j == pTab.iPKey ? -1 : j );
                if ( i < pSrcList.nSrc - 1 )
                {
                  if ( ( pSrcList.a[i + 1].jointype & JT_NATURAL ) != 0 )// pItem[1].jointype
                  {
                    /* If this match occurred in the left table of a natural join,
                    ** then skip the right table to avoid a duplicate match */
                    //pItem++;
                    i++;
                  }
                  else if ( ( pUsing = pSrcList.a[i + 1].pUsing ) != null )//pItem[1].pUsing
                  {
                    /* If this match occurs on a column that is in the USING clause
                    ** of a join, skip the search of the right table of the join
                    ** to avoid a duplicate match there. */
                    int k;
                    for ( k = 0 ; k < pUsing.nId ; k++ )
                    {
                      if ( sqlite3StrICmp( pUsing.a[k].zName, zCol ) == 0 )
                      {
                        //pItem++;
                        i++;
                        break;
                      }
                    }
                  }
                }
                break;
              }
            }
          }
        }

#if !SQLITE_OMIT_TRIGGER
        /* If we have not already resolved the name, then maybe
** it is a new.* or old.* trigger argument reference
*/
        if ( zDb == null && zTab != null && cnt == 0 && pParse.trigStack != null )
        {
          TriggerStack pTriggerStack = pParse.trigStack;
          Table pTab = null;
          u32 piColMask = 0;
          bool bNew = false;
          bool bOld = false;
          if ( pTriggerStack.newIdx != -1 && sqlite3StrICmp( "new", zTab ) == 0 )
          {
            pExpr.iTable = pTriggerStack.newIdx;
            Debug.Assert( pTriggerStack.pTab != null );
            pTab = pTriggerStack.pTab;
            piColMask = pTriggerStack.newColMask;
            bNew = true;
          }
          else if ( pTriggerStack.oldIdx != -1 && sqlite3StrICmp( "old", zTab ) == 0 )
          {
            pExpr.iTable = pTriggerStack.oldIdx;
            Debug.Assert( pTriggerStack.pTab != null );
            pTab = pTriggerStack.pTab;
            piColMask = pTriggerStack.oldColMask;
            bOld = true;
          }

          if ( pTab != null )
          {
            int iCol;
            Column pCol;// = pTab.aCol;

            pSchema = pTab.pSchema;
            cntTab++;
            for ( iCol = 0 ; iCol < pTab.nCol ; iCol++ )//, pCol++)
            {
              pCol = pTab.aCol[iCol];
              if ( sqlite3StrICmp( pCol.zName, zCol ) == 0 )
              {
                cnt++;
                pExpr.iColumn = (short)( iCol == pTab.iPKey ? -1 : iCol );
                pExpr.pTab = pTab;
                testcase( iCol == 31 );
                testcase( iCol == 32 );
                if ( iCol >= 32 )
                {
                  piColMask = 0xffffffff;
                }
                else
                {
                  piColMask |= ( (u32)1 ) << iCol;
                }
                break;
              }
            }
            if ( bOld ) pTriggerStack.oldColMask = piColMask;
            if ( bNew ) pTriggerStack.newColMask = piColMask;
          }
        }
#endif //* !SQLITE_OMIT_TRIGGER) */

        /*
** Perhaps the name is a reference to the ROWID
*/
        if ( cnt == 0 && cntTab == 1 && sqlite3IsRowid( zCol ) )
        {
          cnt = 1;
          pExpr.iColumn = -1;
          pExpr.affinity = SQLITE_AFF_INTEGER;
        }

        /*
        ** If the input is of the form Z (not Y.Z or X.Y.Z) then the name Z
        ** might refer to an result-set alias.  This happens, for example, when
        ** we are resolving names in the WHERE clause of the following command:
        **
        **     SELECT a+b AS x FROM table WHERE x<10;
        **
        ** In cases like this, replace pExpr with a copy of the expression that
        ** forms the result set entry ("a+b" in the example) and return immediately.
        ** Note that the expression in the result set should have already been
        ** resolved by the time the WHERE clause is resolved.
        */
        if ( cnt == 0 && ( pEList = pNC.pEList ) != null && zTab == null )
        {
          for ( j = 0 ; j < pEList.nExpr ; j++ )
          {
            string zAs = pEList.a[j].zName;
            if ( zAs != null && sqlite3StrICmp( zAs, zCol ) == 0 )
            {
              Expr pOrig;
              Debug.Assert( pExpr.pLeft == null && pExpr.pRight == null );
              Debug.Assert( pExpr.x.pList == null );
              Debug.Assert( pExpr.x.pSelect == null );
              pOrig = pEList.a[j].pExpr;
              if ( 0 == pNC.allowAgg && ExprHasProperty( pOrig, EP_Agg ) )
              {
                sqlite3ErrorMsg( pParse, "misuse of aliased aggregate %s", zAs );
                return WRC_Abort;
              }
              resolveAlias( pParse, pEList, j, pExpr, "" );
              cnt = 1;
              pMatch = null;
              Debug.Assert( zTab == null && zDb == null );
              goto lookupname_end;
            }
          }
        }

        /* Advance to the next name context.  The loop will exit when either
        ** we have a match (cnt>0) or when we run out of name contexts.
        */
        if ( cnt == 0 )
        {
          pNC = pNC.pNext;
        }
      }

      /*
      ** If X and Y are NULL (in other words if only the column name Z is
      ** supplied) and the value of Z is enclosed in double-quotes, then
      ** Z is a string literal if it doesn't match any column names.  In that
      ** case, we need to return right away and not make any changes to
      ** pExpr.
      **
      ** Because no reference was made to outer contexts, the pNC.nRef
      ** fields are not changed in any context.
      */
      if ( cnt == 0 && zTab == null && ExprHasProperty( pExpr, EP_DblQuoted ) )
      {
        pExpr.op = TK_STRING;
        pExpr.pTab = null;
        return WRC_Prune;
      }

      /*
      ** cnt==0 means there was not match.  cnt>1 means there were two or
      ** more matches.  Either way, we have an error.
      */
      if ( cnt != 1 )
      {
        string zErr;
        zErr = cnt == 0 ? "no such column" : "ambiguous column name";
        if ( zDb != null )
        {
          sqlite3ErrorMsg( pParse, "%s: %s.%s.%s", zErr, zDb, zTab, zCol );
        }
        else if ( zTab != null )
        {
          sqlite3ErrorMsg( pParse, "%s: %s.%s", zErr, zTab, zCol );
        }
        else
        {
          sqlite3ErrorMsg( pParse, "%s: %s", zErr, zCol );
        }
        pTopNC.nErr++;
      }

      /* If a column from a table in pSrcList is referenced, then record
      ** this fact in the pSrcList.a[].colUsed bitmask.  Column 0 causes
      ** bit 0 to be set.  Column 1 sets bit 1.  And so forth.  If the
      ** column number is greater than the number of bits in the bitmask
      ** then set the high-order bit of the bitmask.
      */
      if ( pExpr.iColumn >= 0 && pMatch != null )
      {
        int n = pExpr.iColumn;
        testcase( n == BMS - 1 );
        if ( n >= BMS )
        {
          n = BMS - 1;
        }
        Debug.Assert( pMatch.iCursor == pExpr.iTable );
        pMatch.colUsed |= ( (Bitmask)1 ) << n;
      }

      /* Clean up and return
      */
      sqlite3ExprDelete( db, ref pExpr.pLeft );
      pExpr.pLeft = null;
      sqlite3ExprDelete( db, ref pExpr.pRight );
      pExpr.pRight = null;
      pExpr.op = TK_COLUMN;
lookupname_end:
      if ( cnt == 1 )
      {
        Debug.Assert( pNC != null );
        sqlite3AuthRead( pParse, pExpr, pSchema, pNC.pSrcList );
        /* Increment the nRef value on all name contexts from TopNC up to
        ** the point where the name matched. */
        for ( ; ; )
        {
          Debug.Assert( pTopNC != null );
          pTopNC.nRef++;
          if ( pTopNC == pNC ) break;
          pTopNC = pTopNC.pNext;
        }
        return WRC_Prune;
      }
      else
      {
        return WRC_Abort;
      }
    }

    /*
    ** This routine is callback for sqlite3WalkExpr().
    **
    ** Resolve symbolic names into TK_COLUMN operators for the current
    ** node in the expression tree.  Return 0 to continue the search down
    ** the tree or 2 to abort the tree walk.
    **
    ** This routine also does error checking and name resolution for
    ** function names.  The operator for aggregate functions is changed
    ** to TK_AGG_FUNCTION.
    */
    static int resolveExprStep( Walker pWalker, ref Expr pExpr )
    {
      NameContext pNC;
      Parse pParse;

      pNC = pWalker.u.pNC;
      Debug.Assert( pNC != null );
      pParse = pNC.pParse;
      Debug.Assert( pParse == pWalker.pParse );

      if ( ExprHasAnyProperty( pExpr, EP_Resolved ) ) return WRC_Prune;
      ExprSetProperty( pExpr, EP_Resolved );
#if !NDEBUG
      if ( pNC.pSrcList != null && pNC.pSrcList.nAlloc > 0 )
      {
        SrcList pSrcList = pNC.pSrcList;
        int i;
        for ( i = 0 ; i < pNC.pSrcList.nSrc ; i++ )
        {
          Debug.Assert( pSrcList.a[i].iCursor >= 0 && pSrcList.a[i].iCursor < pParse.nTab );
        }
      }
#endif
      switch ( pExpr.op )
      {

#if (SQLITE_ENABLE_UPDATE_DELETE_LIMIT) && !(SQLITE_OMIT_SUBQUERY)
/* The special operator TK_ROW means use the rowid for the first
** column in the FROM clause.  This is used by the LIMIT and ORDER BY
** clause processing on UPDATE and DELETE statements.
*/
case TK_ROW: {
SrcList pSrcList = pNC.pSrcList;
SrcList_item pItem;
Debug.Assert( pSrcList !=null && pSrcList.nSrc==1 );
pItem = pSrcList.a[0];
pExpr.op = TK_COLUMN;
pExpr.pTab = pItem.pTab;
pExpr.iTable = pItem.iCursor;
pExpr.iColumn = -1;
pExpr.affinity = SQLITE_AFF_INTEGER;
break;
}
#endif //* defined(SQLITE_ENABLE_UPDATE_DELETE_LIMIT) && !defined(SQLITE_OMIT_SUBQUERY) /

        /* A lone identifier is the name of a column.
*/
        case TK_ID:
          {
            return lookupName( pParse, null, null, pExpr.u.zToken, pNC, pExpr );
          }

        /* A table name and column name:     ID.ID
        ** Or a database, table and column:  ID.ID.ID
        */
        case TK_DOT:
          {
            string zColumn;
            string zTable;
            string zDb;
            Expr pRight;

            /* if( pSrcList==0 ) break; */
            pRight = pExpr.pRight;
            if ( pRight.op == TK_ID )
            {
              zDb = null;
              zTable = pExpr.pLeft.u.zToken;
              zColumn = pRight.u.zToken;
            }
            else
            {
              Debug.Assert( pRight.op == TK_DOT );
              zDb = pExpr.pLeft.u.zToken;
              zTable = pRight.pLeft.u.zToken;
              zColumn = pRight.pRight.u.zToken;
            }
            return lookupName( pParse, zDb, zTable, zColumn, pNC, pExpr );
          }

        /* Resolve function names
        */
        case TK_CONST_FUNC:
        case TK_FUNCTION:
          {
            ExprList pList = pExpr.x.pList;    /* The argument list */
            int n = pList != null ? pList.nExpr : 0;  /* Number of arguments */
            bool no_such_func = false;       /* True if no such function exists */
            bool wrong_num_args = false;     /* True if wrong number of arguments */
            bool is_agg = false;             /* True if is an aggregate function */
            int auth;                   /* Authorization to use the function */
            int nId;                    /* Number of characters in function name */
            string zId;                 /* The function name. */
            FuncDef pDef;              /* Information about the function */
            u8 enc = (u8)pParse.db.aDbStatic[0].pSchema.enc;// ENC( pParse.db );   /* The database encoding */

            testcase( pExpr.op == TK_CONST_FUNC );
            Debug.Assert( !ExprHasProperty( pExpr, EP_xIsSelect ) );
            zId = pExpr.u.zToken;
            nId = sqlite3Strlen30( zId );
            pDef = sqlite3FindFunction( pParse.db, zId, nId, n, enc, 0 );
            if ( pDef == null )
            {
              pDef = sqlite3FindFunction( pParse.db, zId, nId, -1, enc, 0 );
              if ( pDef == null )
              {
                no_such_func = true;
              }
              else
              {
                wrong_num_args = true;
              }
            }
            else
            {
              is_agg = pDef.xFunc == null;
            }
#if !SQLITE_OMIT_AUTHORIZATION
if( pDef ){
auth = sqlite3AuthCheck(pParse, SQLITE_FUNCTION, 0, pDef.zName, 0);
if( auth!=SQLITE_OK ){
if( auth==SQLITE_DENY ){
sqlite3ErrorMsg(pParse, "not authorized to use function: %s",
pDef.zName);
pNC.nErr++;
}
pExpr.op = TK_NULL;
return WRC_Prune;
}
}
#endif
            if ( is_agg && 0 == pNC.allowAgg )
            {
              sqlite3ErrorMsg( pParse, "misuse of aggregate function %.*s()", nId, zId );
              pNC.nErr++;
              is_agg = false;
            }
            else if ( no_such_func )
            {
              sqlite3ErrorMsg( pParse, "no such function: %.*s", nId, zId );
              pNC.nErr++;
            }
            else if ( wrong_num_args )
            {
              sqlite3ErrorMsg( pParse, "wrong number of arguments to function %.*s()",
              nId, zId );
              pNC.nErr++;
            }
            if ( is_agg )
            {
              pExpr.op = TK_AGG_FUNCTION;
              pNC.hasAgg = 1;
            }
            if ( is_agg ) pNC.allowAgg = 0;
            sqlite3WalkExprList( pWalker, pList );
            if ( is_agg ) pNC.allowAgg = 1;
            /* FIX ME:  Compute pExpr.affinity based on the expected return
            ** type of the function
            */
            return WRC_Prune;
          }
#if !SQLITE_OMIT_SUBQUERY
        case TK_SELECT:
        case TK_EXISTS:
          {
            testcase( pExpr.op == TK_EXISTS );
            goto case TK_IN;
          }
#endif
        case TK_IN:
          {
            testcase( pExpr.op == TK_IN );
            if ( ExprHasProperty( pExpr, EP_xIsSelect ) )
            {
              int nRef = pNC.nRef;
#if !SQLITE_OMIT_CHECK
              if ( pNC.isCheck != 0 )
              {
                sqlite3ErrorMsg( pParse, "subqueries prohibited in CHECK constraints" );
              }
#endif
              sqlite3WalkSelect( pWalker, pExpr.x.pSelect );
              Debug.Assert( pNC.nRef >= nRef );
              if ( nRef != pNC.nRef )
              {
                ExprSetProperty( pExpr, EP_VarSelect );
              }
            }
            break;
          }
#if !SQLITE_OMIT_CHECK
        case TK_VARIABLE:
          {
            if ( pNC.isCheck != 0 )
            {
              sqlite3ErrorMsg( pParse, "parameters prohibited in CHECK constraints" );
            }
            break;
          }
#endif
      }
      return ( pParse.nErr != 0 /* || pParse.db.mallocFailed != 0 */ ) ? WRC_Abort : WRC_Continue;
    }

    /*
    ** pEList is a list of expressions which are really the result set of the
    ** a SELECT statement.  pE is a term in an ORDER BY or GROUP BY clause.
    ** This routine checks to see if pE is a simple identifier which corresponds
    ** to the AS-name of one of the terms of the expression list.  If it is,
    ** this routine return an integer between 1 and N where N is the number of
    ** elements in pEList, corresponding to the matching entry.  If there is
    ** no match, or if pE is not a simple identifier, then this routine
    ** return 0.
    **
    ** pEList has been resolved.  pE has not.
    */
    static int resolveAsName(
    Parse pParse,     /* Parsing context for error messages */
    ExprList pEList,  /* List of expressions to scan */
    Expr pE           /* Expression we are trying to match */
    )
    {
      int i;             /* Loop counter */

      UNUSED_PARAMETER( pParse );

      if ( pE.op == TK_ID )
      {
        string zCol = pE.u.zToken;

        for ( i = 0 ; i < pEList.nExpr ; i++ )
        {
          string zAs = pEList.a[i].zName;
          if ( zAs != null && sqlite3StrICmp( zAs, zCol ) == 0 )
          {
            return i + 1;
          }
        }
      }
      return 0;
    }

    /*
    ** pE is a pointer to an expression which is a single term in the
    ** ORDER BY of a compound SELECT.  The expression has not been
    ** name resolved.
    **
    ** At the point this routine is called, we already know that the
    ** ORDER BY term is not an integer index into the result set.  That
    ** case is handled by the calling routine.
    **
    ** Attempt to match pE against result set columns in the left-most
    ** SELECT statement.  Return the index i of the matching column,
    ** as an indication to the caller that it should sort by the i-th column.
    ** The left-most column is 1.  In other words, the value returned is the
    ** same integer value that would be used in the SQL statement to indicate
    ** the column.
    **
    ** If there is no match, return 0.  Return -1 if an error occurs.
    */
    static int resolveOrderByTermToExprList(
    Parse pParse,     /* Parsing context for error messages */
    Select pSelect,   /* The SELECT statement with the ORDER BY clause */
    Expr pE           /* The specific ORDER BY term */
    )
    {
      int i = 0;             /* Loop counter */
      ExprList pEList;  /* The columns of the result set */
      NameContext nc;    /* Name context for resolving pE */

      Debug.Assert( sqlite3ExprIsInteger( pE, ref i ) == 0 );
      pEList = pSelect.pEList;

      /* Resolve all names in the ORDER BY term expression
      */
      nc = new NameContext();// memset( &nc, 0, sizeof( nc ) );
      nc.pParse = pParse;
      nc.pSrcList = pSelect.pSrc;
      nc.pEList = pEList;
      nc.allowAgg = 1;
      nc.nErr = 0;
      if ( sqlite3ResolveExprNames( nc, ref pE ) != 0 )
      {
        sqlite3ErrorClear( pParse );
        return 0;
      }

      /* Try to match the ORDER BY expression against an expression
      ** in the result set.  Return an 1-based index of the matching
      ** result-set entry.
      */
      for ( i = 0 ; i < pEList.nExpr ; i++ )
      {
        if ( sqlite3ExprCompare( pEList.a[i].pExpr, pE ) )
        {
          return i + 1;
        }
      }

      /* If no match, return 0. */
      return 0;
    }

    /*
    ** Generate an ORDER BY or GROUP BY term out-of-range error.
    */
    static void resolveOutOfRangeError(
    Parse pParse,         /* The error context into which to write the error */
    string zType,     /* "ORDER" or "GROUP" */
    int i,                 /* The index (1-based) of the term out of range */
    int mx                 /* Largest permissible value of i */
    )
    {
      sqlite3ErrorMsg( pParse,
      "%r %s BY term out of range - should be " +
      "between 1 and %d", i, zType, mx );
    }

    /*
    ** Analyze the ORDER BY clause in a compound SELECT statement.   Modify
    ** each term of the ORDER BY clause is a constant integer between 1
    ** and N where N is the number of columns in the compound SELECT.
    **
    ** ORDER BY terms that are already an integer between 1 and N are
    ** unmodified.  ORDER BY terms that are integers outside the range of
    ** 1 through N generate an error.  ORDER BY terms that are expressions
    ** are matched against result set expressions of compound SELECT
    ** beginning with the left-most SELECT and working toward the right.
    ** At the first match, the ORDER BY expression is transformed into
    ** the integer column number.
    **
    ** Return the number of errors seen.
    */
    static int resolveCompoundOrderBy(
    Parse pParse,        /* Parsing context.  Leave error messages here */
    Select pSelect       /* The SELECT statement containing the ORDER BY */
    )
    {
      int i;
      ExprList pOrderBy;
      ExprList pEList;
      sqlite3 db;
      int moreToDo = 1;

      pOrderBy = pSelect.pOrderBy;
      if ( pOrderBy == null ) return 0;
      db = pParse.db;
#if SQLITE_MAX_COLUMN
if( pOrderBy.nExpr>db.aLimit[SQLITE_LIMIT_COLUMN] ){
sqlite3ErrorMsg(pParse, "too many terms in ORDER BY clause");
return 1;
}
#endif
      for ( i = 0 ; i < pOrderBy.nExpr ; i++ )
      {
        pOrderBy.a[i].done = 0;
      }
      pSelect.pNext = null;
      while ( pSelect.pPrior != null )
      {
        pSelect.pPrior.pNext = pSelect;
        pSelect = pSelect.pPrior;
      }
      while ( pSelect != null && moreToDo != 0 )
      {
        ExprList_item pItem;
        moreToDo = 0;
        pEList = pSelect.pEList;
        Debug.Assert( pEList != null );
        for ( i = 0 ; i < pOrderBy.nExpr ; i++ )//, pItem++)
        {
          pItem = pOrderBy.a[i];
          int iCol = -1;
          Expr pE, pDup;
          if ( pItem.done != 0 ) continue;
          pE = pItem.pExpr;
          if ( sqlite3ExprIsInteger( pE, ref iCol ) != 0 )
          {
            if ( iCol <= 0 || iCol > pEList.nExpr )
            {
              resolveOutOfRangeError( pParse, "ORDER", i + 1, pEList.nExpr );
              return 1;
            }
          }
          else
          {
            iCol = resolveAsName( pParse, pEList, pE );
            if ( iCol == 0 )
            {
              pDup = sqlite3ExprDup( db, pE, 0 );
              ////if ( 0 == db.mallocFailed )
              {
                Debug.Assert( pDup != null );
                iCol = resolveOrderByTermToExprList( pParse, pSelect, pDup );
              }
              sqlite3ExprDelete( db, ref pDup );
            }
          }
          if ( iCol > 0 )
          {
            CollSeq pColl = pE.pColl;
            int flags = pE.flags & EP_ExpCollate;
            sqlite3ExprDelete( db, ref pE );
            pItem.pExpr = pE = sqlite3Expr( db, TK_INTEGER, null );
            if ( pE == null ) return 1;
            pE.pColl = pColl;
            pE.flags = (u16)( pE.flags | EP_IntValue | flags );
            pE.u.iValue = iCol;
            pItem.iCol = (u16)iCol;
            pItem.done = 1;
          }
          else
          {
            moreToDo = 1;
          }
        }
        pSelect = pSelect.pNext;
      }
      for ( i = 0 ; i < pOrderBy.nExpr ; i++ )
      {
        if ( pOrderBy.a[i].done == 0 )
        {
          sqlite3ErrorMsg( pParse, "%r ORDER BY term does not match any " +
          "column in the result set", i + 1 );
          return 1;
        }
      }
      return 0;
    }

    /*
    ** Check every term in the ORDER BY or GROUP BY clause pOrderBy of
    ** the SELECT statement pSelect.  If any term is reference to a
    ** result set expression (as determined by the ExprList.a.iCol field)
    ** then convert that term into a copy of the corresponding result set
    ** column.
    **
    ** If any errors are detected, add an error message to pParse and
    ** return non-zero.  Return zero if no errors are seen.
    */
    static int sqlite3ResolveOrderGroupBy(
    Parse pParse,        /* Parsing context.  Leave error messages here */
    Select pSelect,      /* The SELECT statement containing the clause */
    ExprList pOrderBy,   /* The ORDER BY or GROUP BY clause to be processed */
    string zType         /* "ORDER" or "GROUP" */
    )
    {
      int i;
      sqlite3 db = pParse.db;
      ExprList pEList;
      ExprList_item pItem;

      if ( pOrderBy == null /* || pParse.db.mallocFailed != 0 */ ) return 0;
#if SQLITE_MAX_COLUMN
if( pOrderBy.nExpr>db.aLimit[SQLITE_LIMIT_COLUMN] ){
sqlite3ErrorMsg(pParse, "too many terms in %s BY clause", zType);
return 1;
}
#endif
      pEList = pSelect.pEList;
      Debug.Assert( pEList != null );  /* sqlite3SelectNew() guarantees this */
      for ( i = 0 ; i < pOrderBy.nExpr ; i++ )//, pItem++)
      {
        pItem = pOrderBy.a[i];
        if ( pItem.iCol != 0 )
        {
          if ( pItem.iCol > pEList.nExpr )
          {
            resolveOutOfRangeError( pParse, zType, i + 1, pEList.nExpr );
            return 1;
          }
          resolveAlias( pParse, pEList, pItem.iCol - 1, pItem.pExpr, zType );
        }
      }
      return 0;
    }

    /*
    ** pOrderBy is an ORDER BY or GROUP BY clause in SELECT statement pSelect.
    ** The Name context of the SELECT statement is pNC.  zType is either
    ** "ORDER" or "GROUP" depending on which type of clause pOrderBy is.
    **
    ** This routine resolves each term of the clause into an expression.
    ** If the order-by term is an integer I between 1 and N (where N is the
    ** number of columns in the result set of the SELECT) then the expression
    ** in the resolution is a copy of the I-th result-set expression.  If
    ** the order-by term is an identify that corresponds to the AS-name of
    ** a result-set expression, then the term resolves to a copy of the
    ** result-set expression.  Otherwise, the expression is resolved in
    ** the usual way - using sqlite3ResolveExprNames().
    **
    ** This routine returns the number of errors.  If errors occur, then
    ** an appropriate error message might be left in pParse.  (OOM errors
    ** excepted.)
    */
    static int resolveOrderGroupBy(
    NameContext pNC,     /* The name context of the SELECT statement */
    Select pSelect,      /* The SELECT statement holding pOrderBy */
    ExprList pOrderBy,   /* An ORDER BY or GROUP BY clause to resolve */
    string zType         /* Either "ORDER" or "GROUP", as appropriate */
    )
    {
      int i;                         /* Loop counter */
      int iCol;                      /* Column number */
      ExprList_item pItem;   /* A term of the ORDER BY clause */
      Parse pParse;                 /* Parsing context */
      int nResult;                   /* Number of terms in the result set */

      if ( pOrderBy == null ) return 0;
      nResult = pSelect.pEList.nExpr;
      pParse = pNC.pParse;
      for ( i = 0 ; i < pOrderBy.nExpr ; i++ )//, pItem++ )
      {
        pItem = pOrderBy.a[i];
        Expr pE = pItem.pExpr;
        iCol = resolveAsName( pParse, pSelect.pEList, pE );
        if ( iCol > 0 )
        {
          /* If an AS-name match is found, mark this ORDER BY column as being
          ** a copy of the iCol-th result-set column.  The subsequent call to
          ** sqlite3ResolveOrderGroupBy() will convert the expression to a
          ** copy of the iCol-th result-set expression. */
          pItem.iCol = (u16)iCol;
          continue;
        }
        if ( sqlite3ExprIsInteger( pE, ref iCol ) != 0 )
        {
          /* The ORDER BY term is an integer constant.  Again, set the column
          ** number so that sqlite3ResolveOrderGroupBy() will convert the
          ** order-by term to a copy of the result-set expression */
          if ( iCol < 1 )
          {
            resolveOutOfRangeError( pParse, zType, i + 1, nResult );
            return 1;
          }
          pItem.iCol = (u16)iCol;
          continue;
        }

        /* Otherwise, treat the ORDER BY term as an ordinary expression */
        pItem.iCol = 0;
        if ( sqlite3ResolveExprNames( pNC, ref pE ) != 0 )
        {
          return 1;
        }
      }
      return sqlite3ResolveOrderGroupBy( pParse, pSelect, pOrderBy, zType );
    }

    /*
    ** Resolve names in the SELECT statement p and all of its descendents.
    */
    static int resolveSelectStep( Walker pWalker, Select p )
    {
      NameContext pOuterNC;  /* Context that contains this SELECT */
      NameContext sNC;       /* Name context of this SELECT */
      bool isCompound;       /* True if p is a compound select */
      int nCompound;         /* Number of compound terms processed so far */
      Parse pParse;          /* Parsing context */
      ExprList pEList;       /* Result set expression list */
      int i;                 /* Loop counter */
      ExprList pGroupBy;     /* The GROUP BY clause */
      Select pLeftmost;      /* Left-most of SELECT of a compound */
      sqlite3 db;            /* Database connection */


      Debug.Assert( p != null );
      if ( ( p.selFlags & SF_Resolved ) != 0 )
      {
        return WRC_Prune;
      }
      pOuterNC = pWalker.u.pNC;
      pParse = pWalker.pParse;
      db = pParse.db;

      /* Normally sqlite3SelectExpand() will be called first and will have
      ** already expanded this SELECT.  However, if this is a subquery within
      ** an expression, sqlite3ResolveExprNames() will be called without a
      ** prior call to sqlite3SelectExpand().  When that happens, let
      ** sqlite3SelectPrep() do all of the processing for this SELECT.
      ** sqlite3SelectPrep() will invoke both sqlite3SelectExpand() and
      ** this routine in the correct order.
      */
      if ( ( p.selFlags & SF_Expanded ) == 0 )
      {
        sqlite3SelectPrep( pParse, p, pOuterNC );
        return ( pParse.nErr != 0 /*|| db.mallocFailed != 0 */ ) ? WRC_Abort : WRC_Prune;
      }

      isCompound = p.pPrior != null;
      nCompound = 0;
      pLeftmost = p;
      while ( p != null )
      {
        Debug.Assert( ( p.selFlags & SF_Expanded ) != 0 );
        Debug.Assert( ( p.selFlags & SF_Resolved ) == 0 );
        p.selFlags |= SF_Resolved;

        /* Resolve the expressions in the LIMIT and OFFSET clauses. These
        ** are not allowed to refer to any names, so pass an empty NameContext.
        */
        sNC = new NameContext();// memset( &sNC, 0, sizeof( sNC ) );
        sNC.pParse = pParse;
        if ( sqlite3ResolveExprNames( sNC, ref p.pLimit ) != 0 ||
        sqlite3ResolveExprNames( sNC, ref p.pOffset ) != 0 )
        {
          return WRC_Abort;
        }

        /* Set up the local name-context to pass to sqlite3ResolveExprNames() to
        ** resolve the result-set expression list.
        */
        sNC.allowAgg = 1;
        sNC.pSrcList = p.pSrc;
        sNC.pNext = pOuterNC;

        /* Resolve names in the result set. */
        pEList = p.pEList;
        Debug.Assert( pEList != null );
        for ( i = 0 ; i < pEList.nExpr ; i++ )
        {
          Expr pX = pEList.a[i].pExpr;
          if ( sqlite3ResolveExprNames( sNC, ref pX ) != 0 )
          {
            return WRC_Abort;
          }
        }

        /* Recursively resolve names in all subqueries
        */
        for ( i = 0 ; i < p.pSrc.nSrc ; i++ )
        {
          SrcList_item pItem = p.pSrc.a[i];
          if ( pItem.pSelect != null )
          {
            string zSavedContext = pParse.zAuthContext;
            if ( pItem.zName != null ) pParse.zAuthContext = pItem.zName;
            sqlite3ResolveSelectNames( pParse, pItem.pSelect, pOuterNC );
            pParse.zAuthContext = zSavedContext;
            if ( pParse.nErr != 0 /*|| db.mallocFailed != 0 */ ) return WRC_Abort;
          }
        }

        /* If there are no aggregate functions in the result-set, and no GROUP BY
        ** expression, do not allow aggregates in any of the other expressions.
        */
        Debug.Assert( ( p.selFlags & SF_Aggregate ) == 0 );
        pGroupBy = p.pGroupBy;
        if ( pGroupBy != null || sNC.hasAgg != 0 )
        {
          p.selFlags |= SF_Aggregate;
        }
        else
        {
          sNC.allowAgg = 0;
        }

        /* If a HAVING clause is present, then there must be a GROUP BY clause.
        */
        if ( p.pHaving != null && pGroupBy == null )
        {
          sqlite3ErrorMsg( pParse, "a GROUP BY clause is required before HAVING" );
          return WRC_Abort;
        }

        /* Add the expression list to the name-context before parsing the
        ** other expressions in the SELECT statement. This is so that
        ** expressions in the WHERE clause (etc.) can refer to expressions by
        ** aliases in the result set.
        **
        ** Minor point: If this is the case, then the expression will be
        ** re-evaluated for each reference to it.
        */
        sNC.pEList = p.pEList;
        if ( sqlite3ResolveExprNames( sNC, ref p.pWhere ) != 0 ||
        sqlite3ResolveExprNames( sNC, ref p.pHaving ) != 0
        )
        {
          return WRC_Abort;
        }

        /* The ORDER BY and GROUP BY clauses may not refer to terms in
        ** outer queries
        */
        sNC.pNext = null;
        sNC.allowAgg = 1;

        /* Process the ORDER BY clause for singleton SELECT statements.
        ** The ORDER BY clause for compounds SELECT statements is handled
        ** below, after all of the result-sets for all of the elements of
        ** the compound have been resolved.
        */
        if ( !isCompound && resolveOrderGroupBy( sNC, p, p.pOrderBy, "ORDER" ) != 0 )
        {
          return WRC_Abort;
        }
        //if ( db.mallocFailed != 0 )
        //{
        //  return WRC_Abort;
        //}

        /* Resolve the GROUP BY clause.  At the same time, make sure
        ** the GROUP BY clause does not contain aggregate functions.
        */
        if ( pGroupBy != null )
        {
          ExprList_item pItem;

          if ( resolveOrderGroupBy( sNC, p, pGroupBy, "GROUP" ) != 0 /*|| db.mallocFailed != 0 */ )
          {
            return WRC_Abort;
          }
          for ( i = 0 ; i < pGroupBy.nExpr ; i++ )//, pItem++)
          {
            pItem = pGroupBy.a[i];
            if ( ( pItem.pExpr.flags & EP_Agg ) != 0 )//HasProperty(pItem.pExpr, EP_Agg) )
            {
              sqlite3ErrorMsg( pParse, "aggregate functions are not allowed in " +
              "the GROUP BY clause" );
              return WRC_Abort;
            }
          }
        }

        /* Advance to the next term of the compound
        */
        p = p.pPrior;
        nCompound++;
      }

      /* Resolve the ORDER BY on a compound SELECT after all terms of
      ** the compound have been resolved.
      */
      if ( isCompound && resolveCompoundOrderBy( pParse, pLeftmost ) != 0 )
      {
        return WRC_Abort;
      }

      return WRC_Prune;
    }

    /*
    ** This routine walks an expression tree and resolves references to
    ** table columns and result-set columns.  At the same time, do error
    ** checking on function usage and set a flag if any aggregate functions
    ** are seen.
    **
    ** To resolve table columns references we look for nodes (or subtrees) of the
    ** form X.Y.Z or Y.Z or just Z where
    **
    **      X:   The name of a database.  Ex:  "main" or "temp" or
    **           the symbolic name assigned to an ATTACH-ed database.
    **
    **      Y:   The name of a table in a FROM clause.  Or in a trigger
    **           one of the special names "old" or "new".
    **
    **      Z:   The name of a column in table Y.
    **
    ** The node at the root of the subtree is modified as follows:
    **
    **    Expr.op        Changed to TK_COLUMN
    **    Expr.pTab      Points to the Table object for X.Y
    **    Expr.iColumn   The column index in X.Y.  -1 for the rowid.
    **    Expr.iTable    The VDBE cursor number for X.Y
    **
    **
    ** To resolve result-set references, look for expression nodes of the
    ** form Z (with no X and Y prefix) where the Z matches the right-hand
    ** size of an AS clause in the result-set of a SELECT.  The Z expression
    ** is replaced by a copy of the left-hand side of the result-set expression.
    ** Table-name and function resolution occurs on the substituted expression
    ** tree.  For example, in:
    **
    **      SELECT a+b AS x, c+d AS y FROM t1 ORDER BY x;
    **
    ** The "x" term of the order by is replaced by "a+b" to render:
    **
    **      SELECT a+b AS x, c+d AS y FROM t1 ORDER BY a+b;
    **
    ** Function calls are checked to make sure that the function is
    ** defined and that the correct number of arguments are specified.
    ** If the function is an aggregate function, then the pNC.hasAgg is
    ** set and the opcode is changed from TK_FUNCTION to TK_AGG_FUNCTION.
    ** If an expression contains aggregate functions then the EP_Agg
    ** property on the expression is set.
    **
    ** An error message is left in pParse if anything is amiss.  The number
    ** if errors is returned.
    */
    static int sqlite3ResolveExprNames(
    NameContext pNC,       /* Namespace to resolve expressions in. */
    ref Expr pExpr         /* The expression to be analyzed. */
    )
    {
      u8 savedHasAgg;
      Walker w = new Walker();

      if ( pExpr == null ) return 0;
#if SQLITE_MAX_EXPR_DEPTH//>0
{
Parse pParse = pNC.pParse;
if( sqlite3ExprCheckHeight(pParse, pExpr.nHeight+pNC.pParse.nHeight) ){
return 1;
}
pParse.nHeight += pExpr.nHeight;
}
#endif
      savedHasAgg = pNC.hasAgg;
      pNC.hasAgg = 0;
      w.xExprCallback = resolveExprStep;
      w.xSelectCallback = resolveSelectStep;
      w.pParse = pNC.pParse;
      w.u.pNC = pNC;
      sqlite3WalkExpr( w, ref pExpr );
#if SQLITE_MAX_EXPR_DEPTH//>0
pNC.pParse.nHeight -= pExpr.nHeight;
#endif
      if ( pNC.nErr > 0 || w.pParse.nErr > 0 )
      {
        ExprSetProperty( pExpr, EP_Error );
      }
      if ( pNC.hasAgg != 0 )
      {
        ExprSetProperty( pExpr, EP_Agg );
      }
      else if ( savedHasAgg != 0 )
      {
        pNC.hasAgg = 1;
      }
      return ExprHasProperty( pExpr, EP_Error ) ? 1 : 0;
    }


    /*
    ** Resolve all names in all expressions of a SELECT and in all
    ** decendents of the SELECT, including compounds off of p.pPrior,
    ** subqueries in expressions, and subqueries used as FROM clause
    ** terms.
    **
    ** See sqlite3ResolveExprNames() for a description of the kinds of
    ** transformations that occur.
    **
    ** All SELECT statements should have been expanded using
    ** sqlite3SelectExpand() prior to invoking this routine.
    */
    static void sqlite3ResolveSelectNames(
    Parse pParse,         /* The parser context */
    Select p,             /* The SELECT statement being coded. */
    NameContext pOuterNC  /* Name context for parent SELECT statement */
    )
    {
      Walker w = new Walker();

      Debug.Assert( p != null );
      w.xExprCallback = resolveExprStep;
      w.xSelectCallback = resolveSelectStep;
      w.pParse = pParse;
      w.u.pNC = pOuterNC;
      sqlite3WalkSelect( w, p );
    }
  }
}
