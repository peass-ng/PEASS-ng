using System;
using System.Diagnostics;
using System.Text;

using Bitmask = System.UInt64;
using u32 = System.UInt32;

namespace CS_SQLite3
{
  public partial class CSSQLite
  {
    /*
    ** 2008 August 16
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains routines used for walking the parser tree for
    ** an SQL statement.
    **
    ** $Id: walker.c,v 1.7 2009/06/15 23:15:59 drh Exp $
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
    ** Walk an expression tree.  Invoke the callback once for each node
    ** of the expression, while decending.  (In other words, the callback
    ** is invoked before visiting children.)
    **
    ** The return value from the callback should be one of the WRC_*
    ** constants to specify how to proceed with the walk.
    **
    **    WRC_Continue      Continue descending down the tree.
    **
    **    WRC_Prune         Do not descend into child nodes.  But allow
    **                      the walk to continue with sibling nodes.
    **
    **    WRC_Abort         Do no more callbacks.  Unwind the stack and
    **                      return the top-level walk call.
    **
    ** The return value from this routine is WRC_Abort to abandon the tree walk
    ** and WRC_Continue to continue.
    */
    static int sqlite3WalkExpr( Walker pWalker, ref Expr pExpr )
    {
      int rc;
      if ( pExpr == null ) return WRC_Continue;
      testcase( ExprHasProperty( pExpr, EP_TokenOnly ) );
      testcase( ExprHasProperty( pExpr, EP_Reduced ) );
      rc = pWalker.xExprCallback( pWalker, ref pExpr );
      if ( rc == WRC_Continue
      && !ExprHasAnyProperty( pExpr, EP_TokenOnly ) )
      {
        if ( sqlite3WalkExpr( pWalker, ref pExpr.pLeft ) != 0 ) return WRC_Abort;
        if ( sqlite3WalkExpr( pWalker, ref pExpr.pRight ) != 0 ) return WRC_Abort;
        if ( ExprHasProperty( pExpr, EP_xIsSelect ) )
        {
          if ( sqlite3WalkSelect( pWalker, pExpr.x.pSelect ) != 0 ) return WRC_Abort;
        }
        else
        {
          if ( sqlite3WalkExprList( pWalker, pExpr.x.pList ) != 0 ) return WRC_Abort;
        }
      }
      return rc & WRC_Abort;
    }

    /*
    ** Call sqlite3WalkExpr() for every expression in list p or until
    ** an abort request is seen.
    */
    static int sqlite3WalkExprList( Walker pWalker, ExprList p )
    {
      int i;
      ExprList_item pItem;
      if ( p != null )
      {
        for ( i = p.nExpr ; i > 0 ; i-- )
        {//, pItem++){
          pItem = p.a[p.nExpr - i];
          if ( sqlite3WalkExpr( pWalker, ref pItem.pExpr ) != 0 ) return WRC_Abort;
        }
      }
      return WRC_Continue;
    }

    /*
    ** Walk all expressions associated with SELECT statement p.  Do
    ** not invoke the SELECT callback on p, but do (of course) invoke
    ** any expr callbacks and SELECT callbacks that come from subqueries.
    ** Return WRC_Abort or WRC_Continue.
    */
    static int sqlite3WalkSelectExpr( Walker pWalker, Select p )
    {
      if ( sqlite3WalkExprList( pWalker, p.pEList ) != 0 ) return WRC_Abort;
      if ( sqlite3WalkExpr( pWalker, ref p.pWhere ) != 0 ) return WRC_Abort;
      if ( sqlite3WalkExprList( pWalker, p.pGroupBy ) != 0 ) return WRC_Abort;
      if ( sqlite3WalkExpr( pWalker, ref p.pHaving ) != 0 ) return WRC_Abort;
      if ( sqlite3WalkExprList( pWalker, p.pOrderBy ) != 0 ) return WRC_Abort;
      if ( sqlite3WalkExpr( pWalker, ref p.pLimit ) != 0 ) return WRC_Abort;
      if ( sqlite3WalkExpr( pWalker, ref p.pOffset ) != 0 ) return WRC_Abort;
      return WRC_Continue;
    }

    /*
    ** Walk the parse trees associated with all subqueries in the
    ** FROM clause of SELECT statement p.  Do not invoke the select
    ** callback on p, but do invoke it on each FROM clause subquery
    ** and on any subqueries further down in the tree.  Return
    ** WRC_Abort or WRC_Continue;
    */
    static int sqlite3WalkSelectFrom( Walker pWalker, Select p )
    {
      SrcList pSrc;
      int i;
      SrcList_item pItem;

      pSrc = p.pSrc;
      if ( ALWAYS( pSrc ) )
      {
        for ( i = pSrc.nSrc ; i > 0 ; i-- )// pItem++ )
        {
          pItem = pSrc.a[pSrc.nSrc - i];
          if ( sqlite3WalkSelect( pWalker, pItem.pSelect ) != 0 )
          {
            return WRC_Abort;
          }
        }
      }
      return WRC_Continue;
    }

    /*
    ** Call sqlite3WalkExpr() for every expression in Select statement p.
    ** Invoke sqlite3WalkSelect() for subqueries in the FROM clause and
    ** on the compound select chain, p.pPrior.
    **
    ** Return WRC_Continue under normal conditions.  Return WRC_Abort if
    ** there is an abort request.
    **
    ** If the Walker does not have an xSelectCallback() then this routine
    ** is a no-op returning WRC_Continue.
    */
    static int sqlite3WalkSelect( Walker pWalker, Select p )
    {
      int rc;
      if ( p == null || pWalker.xSelectCallback == null ) return WRC_Continue;
      rc = WRC_Continue;
      while ( p != null )
      {
        rc = pWalker.xSelectCallback( pWalker, p );
        if ( rc != 0 ) break;
        if ( sqlite3WalkSelectExpr( pWalker, p ) != 0 ) return WRC_Abort;
        if ( sqlite3WalkSelectFrom( pWalker, p ) != 0 ) return WRC_Abort;
        p = p.pPrior;
      }
      return rc & WRC_Abort;
    }
  }
}
