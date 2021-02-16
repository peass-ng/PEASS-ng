#define SQLITE_MAX_EXPR_DEPTH

using System;
using System.Diagnostics;
using System.Text;
using i64 = System.Int64;
using u8 = System.Byte;
using u32 = System.UInt32;
using u16 = System.UInt16;

namespace winPEAS._3rdParty.SQLite.src
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
    ** This file contains routines used for analyzing expressions and
    ** for generating VDBE code that evaluates expressions in SQLite.
    **
    ** $Id: expr.c,v 1.448 2009/07/27 10:05:05 danielk1977 Exp $
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
    ** Return the 'affinity' of the expression pExpr if any.
    **
    ** If pExpr is a column, a reference to a column via an 'AS' alias,
    ** or a sub-select with a column as the return value, then the
    ** affinity of that column is returned. Otherwise, 0x00 is returned,
    ** indicating no affinity for the expression.
    **
    ** i.e. the WHERE clause expresssions in the following statements all
    ** have an affinity:
    **
    ** CREATE TABLE t1(a);
    ** SELECT * FROM t1 WHERE a;
    ** SELECT a AS b FROM t1 WHERE b;
    ** SELECT * FROM t1 WHERE (select a from t1);
    */
    static char sqlite3ExprAffinity( Expr pExpr )
    {
      int op = pExpr.op;
      if ( op == TK_SELECT )
      {
        Debug.Assert( ( pExpr.flags & EP_xIsSelect ) != 0 );
        return sqlite3ExprAffinity( pExpr.x.pSelect.pEList.a[0].pExpr );
      }
#if !SQLITE_OMIT_CAST
      if ( op == TK_CAST )
      {
        Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
        return sqlite3AffinityType( pExpr.u.zToken );
      }
#endif
      if ( ( op == TK_AGG_COLUMN || op == TK_COLUMN || op == TK_REGISTER )
      && pExpr.pTab != null
      )
      {
        /* op==TK_REGISTER && pExpr.pTab!=0 happens when pExpr was originally
        ** a TK_COLUMN but was previously evaluated and cached in a register */
        int j = pExpr.iColumn;
        if ( j < 0 ) return SQLITE_AFF_INTEGER;
        Debug.Assert( pExpr.pTab != null && j < pExpr.pTab.nCol );
        return pExpr.pTab.aCol[j].affinity;
      }
      return pExpr.affinity;
    }

    /*
    ** Set the collating sequence for expression pExpr to be the collating
    ** sequence named by pToken.   Return a pointer to the revised expression.
    ** The collating sequence is marked as "explicit" using the EP_ExpCollate
    ** flag.  An explicit collating sequence will override implicit
    ** collating sequences.
    */
    static Expr sqlite3ExprSetColl( Parse pParse, Expr pExpr, Token pCollName )
    {
      string zColl;            /* Dequoted name of collation sequence */
      CollSeq pColl;
      sqlite3 db = pParse.db;
      zColl = sqlite3NameFromToken( db, pCollName );
      if ( pExpr != null && zColl != null )
      {
        pColl = sqlite3LocateCollSeq( pParse, zColl );
        if ( pColl != null )
        {
          pExpr.pColl = pColl;
          pExpr.flags |= EP_ExpCollate;
        }
      }
      //sqlite3DbFree( db, ref zColl );
      return pExpr;
    }

    /*
    ** Return the default collation sequence for the expression pExpr. If
    ** there is no default collation type, return 0.
    */
    static CollSeq sqlite3ExprCollSeq( Parse pParse, Expr pExpr )
    {
      CollSeq pColl = null;
      Expr p = pExpr;
      while ( ALWAYS( p != null ) )
      {
        int op;
        pColl = pExpr.pColl;
        if (pColl != null ) break;
        op = p.op;
        if ( ( op == TK_AGG_COLUMN || op == TK_COLUMN || op == TK_REGISTER ) && p.pTab != null )
        {
          /* op==TK_REGISTER && p->pTab!=0 happens when pExpr was originally
          ** a TK_COLUMN but was previously evaluated and cached in a register */
          string zColl;
          int j = p.iColumn;
          if ( j >= 0 )
          {
            sqlite3 db = pParse.db;
            zColl = p.pTab.aCol[j].zColl;
            pColl = sqlite3FindCollSeq( db, ENC( db ), zColl, 0 );
            pExpr.pColl = pColl;
          }
          break;
        }
        if ( op != TK_CAST && op != TK_UPLUS )
        {
          break;
        }
        p = p.pLeft;
      }
      if ( sqlite3CheckCollSeq( pParse, pColl ) != 0 )
      {
        pColl = null;
      }
      return pColl;
    }

    /*
    ** pExpr is an operand of a comparison operator.  aff2 is the
    ** type affinity of the other operand.  This routine returns the
    ** type affinity that should be used for the comparison operator.
    */
    static char sqlite3CompareAffinity( Expr pExpr, char aff2 )
    {
      char aff1 = sqlite3ExprAffinity( pExpr );
      if ( aff1 != '\0' && aff2 != '\0' )
      {
        /* Both sides of the comparison are columns. If one has numeric
        ** affinity, use that. Otherwise use no affinity.
        */
        if ( aff1 >= SQLITE_AFF_NUMERIC || aff2 >= SQLITE_AFF_NUMERIC )
        //        if (sqlite3IsNumericAffinity(aff1) || sqlite3IsNumericAffinity(aff2))
        {
          return SQLITE_AFF_NUMERIC;
        }
        else
        {
          return SQLITE_AFF_NONE;
        }
      }
      else if ( aff1 == '\0' && aff2 == '\0' )
      {
        /* Neither side of the comparison is a column.  Compare the
        ** results directly.
        */
        return SQLITE_AFF_NONE;
      }
      else
      {
        /* One side is a column, the other is not. Use the columns affinity. */
        Debug.Assert( aff1 == 0 || aff2 == 0 );
        return ( aff1 != '\0' ? aff1 : aff2 );
      }
    }

    /*
    ** pExpr is a comparison operator.  Return the type affinity that should
    ** be applied to both operands prior to doing the comparison.
    */
    static char comparisonAffinity( Expr pExpr )
    {
      char aff;
      Debug.Assert( pExpr.op == TK_EQ || pExpr.op == TK_IN || pExpr.op == TK_LT ||
      pExpr.op == TK_GT || pExpr.op == TK_GE || pExpr.op == TK_LE ||
      pExpr.op == TK_NE );
      Debug.Assert( pExpr.pLeft != null );
      aff = sqlite3ExprAffinity( pExpr.pLeft );
      if ( pExpr.pRight != null )
      {
        aff = sqlite3CompareAffinity( pExpr.pRight, aff );
      }
      else if ( ExprHasProperty( pExpr, EP_xIsSelect ) )
      {
        aff = sqlite3CompareAffinity( pExpr.x.pSelect.pEList.a[0].pExpr, aff );
      }
      else if ( aff == '\0' )
      {
        aff = SQLITE_AFF_NONE;
      }
      return aff;
    }

    /*
    ** pExpr is a comparison expression, eg. '=', '<', IN(...) etc.
    ** idx_affinity is the affinity of an indexed column. Return true
    ** if the index with affinity idx_affinity may be used to implement
    ** the comparison in pExpr.
    */
    static bool sqlite3IndexAffinityOk( Expr pExpr, char idx_affinity )
    {
      char aff = comparisonAffinity( pExpr );
      switch ( aff )
      {
        case SQLITE_AFF_NONE:
          return true;
        case SQLITE_AFF_TEXT:
          return idx_affinity == SQLITE_AFF_TEXT;
        default:
          return idx_affinity >= SQLITE_AFF_NUMERIC;// sqlite3IsNumericAffinity(idx_affinity);
      }
    }

    /*
    ** Return the P5 value that should be used for a binary comparison
    ** opcode (OP_Eq, OP_Ge etc.) used to compare pExpr1 and pExpr2.
    */
    static u8 binaryCompareP5( Expr pExpr1, Expr pExpr2, int jumpIfNull )
    {
      u8 aff = (u8)sqlite3ExprAffinity( pExpr2 );
      aff = (u8)( (u8)sqlite3CompareAffinity( pExpr1, (char)aff ) | (u8)jumpIfNull );
      return aff;
    }

    /*
    ** Return a pointer to the collation sequence that should be used by
    ** a binary comparison operator comparing pLeft and pRight.
    **
    ** If the left hand expression has a collating sequence type, then it is
    ** used. Otherwise the collation sequence for the right hand expression
    ** is used, or the default (BINARY) if neither expression has a collating
    ** type.
    **
    ** Argument pRight (but not pLeft) may be a null pointer. In this case,
    ** it is not considered.
    */
    static CollSeq sqlite3BinaryCompareCollSeq(
    Parse pParse,
    Expr pLeft,
    Expr pRight
    )
    {
      CollSeq pColl;
      Debug.Assert( pLeft != null );
      if ( ( pLeft.flags & EP_ExpCollate ) != 0 )
      {
        Debug.Assert( pLeft.pColl != null );
        pColl = pLeft.pColl;
      }
      else if ( pRight != null && ( ( pRight.flags & EP_ExpCollate ) != 0 ) )
      {
        Debug.Assert( pRight.pColl != null );
        pColl = pRight.pColl;
      }
      else
      {
        pColl = sqlite3ExprCollSeq( pParse, pLeft );
        if ( pColl == null )
        {
          pColl = sqlite3ExprCollSeq( pParse, pRight );
        }
      }
      return pColl;
    }

    /*
    ** Generate the operands for a comparison operation.  Before
    ** generating the code for each operand, set the EP_AnyAff
    ** flag on the expression so that it will be able to used a
    ** cached column value that has previously undergone an
    ** affinity change.
    */
    static void codeCompareOperands(
    Parse pParse,        /* Parsing and code generating context */
    Expr pLeft,          /* The left operand */
    ref int pRegLeft,    /* Register where left operand is stored */
    ref int pFreeLeft,   /* Free this register when done */
    Expr pRight,         /* The right operand */
    ref int pRegRight,   /* Register where right operand is stored */
    ref int pFreeRight   /* Write temp register for right operand there */
    )
    {

      while ( pLeft.op == TK_UPLUS ) pLeft = pLeft.pLeft;
      pLeft.flags |= EP_AnyAff;
      pRegLeft = sqlite3ExprCodeTemp( pParse, pLeft, ref pFreeLeft );
      while ( pRight.op == TK_UPLUS ) pRight = pRight.pLeft;
      pRight.flags |= EP_AnyAff;
      pRegRight = sqlite3ExprCodeTemp( pParse, pRight, ref pFreeRight );
    }

    /*
    ** Generate code for a comparison operator.
    */
    static int codeCompare(
    Parse pParse,    /* The parsing (and code generating) context */
    Expr pLeft,      /* The left operand */
    Expr pRight,     /* The right operand */
    int opcode,       /* The comparison opcode */
    int in1, int in2, /* Register holding operands */
    int dest,         /* Jump here if true.  */
    int jumpIfNull    /* If true, jump if either operand is NULL */
    )
    {
      int p5;
      int addr;
      CollSeq p4;

      p4 = sqlite3BinaryCompareCollSeq( pParse, pLeft, pRight );
      p5 = binaryCompareP5( pLeft, pRight, jumpIfNull );
      addr = sqlite3VdbeAddOp4( pParse.pVdbe, opcode, in2, dest, in1,
      p4, P4_COLLSEQ );
      sqlite3VdbeChangeP5( pParse.pVdbe, (u8)p5 );
      if ( ( p5 & SQLITE_AFF_MASK ) != SQLITE_AFF_NONE )
      {
        sqlite3ExprCacheAffinityChange( pParse, in1, 1 );
        sqlite3ExprCacheAffinityChange( pParse, in2, 1 );
      }
      return addr;
    }

#if SQLITE_MAX_EXPR_DEPTH //>0
    /*
** Check that argument nHeight is less than or equal to the maximum
** expression depth allowed. If it is not, leave an error message in
** pParse.
*/
    static int sqlite3ExprCheckHeight( Parse pParse, int nHeight )
    {
      int rc = SQLITE_OK;
      int mxHeight = pParse.db.aLimit[SQLITE_LIMIT_EXPR_DEPTH];
      if ( nHeight > mxHeight )
      {
        sqlite3ErrorMsg( pParse,
        "Expression tree is too large (maximum depth %d)", mxHeight
        );
        rc = SQLITE_ERROR;
      }
      return rc;
    }

    /* The following three functions, heightOfExpr(), heightOfExprList()
    ** and heightOfSelect(), are used to determine the maximum height
    ** of any expression tree referenced by the structure passed as the
    ** first argument.
    **
    ** If this maximum height is greater than the current value pointed
    ** to by pnHeight, the second parameter, then set pnHeight to that
    ** value.
    */
    static void heightOfExpr( Expr p, ref int pnHeight )
    {
      if ( p != null )
      {
        if ( p.nHeight > pnHeight )
        {
          pnHeight = p.nHeight;
        }
      }
    }
    static void heightOfExprList( ExprList p, ref int pnHeight )
    {
      if ( p != null )
      {
        int i;
        for ( i = 0 ; i < p.nExpr ; i++ )
        {
          heightOfExpr( p.a[i].pExpr, ref pnHeight );
        }
      }
    }
    static void heightOfSelect( Select p, ref int pnHeight )
    {
      if ( p != null )
      {
        heightOfExpr( p.pWhere, ref  pnHeight );
        heightOfExpr( p.pHaving, ref  pnHeight );
        heightOfExpr( p.pLimit, ref  pnHeight );
        heightOfExpr( p.pOffset, ref  pnHeight );
        heightOfExprList( p.pEList, ref pnHeight );
        heightOfExprList( p.pGroupBy, ref pnHeight );
        heightOfExprList( p.pOrderBy, ref  pnHeight );
        heightOfSelect( p.pPrior, ref  pnHeight );
      }
    }

    /*
    ** Set the Expr.nHeight variable in the structure passed as an
    ** argument. An expression with no children, Expr.x.pList or
    ** Expr.x.pSelect member has a height of 1. Any other expression
    ** has a height equal to the maximum height of any other
    ** referenced Expr plus one.
    */
    static void exprSetHeight( Expr p )
    {
      int nHeight = 0;
      heightOfExpr( p.pLeft, ref nHeight );
      heightOfExpr( p.pRight, ref nHeight );
      if ( ExprHasProperty( p, EP_xIsSelect ) )
      {
        heightOfSelect( p.x.pSelect, ref nHeight );
      }
      else
      {
        heightOfExprList( p.x.pList, ref nHeight );
      }
      p.nHeight = nHeight + 1;
    }

    /*
    ** Set the Expr.nHeight variable using the exprSetHeight() function. If
    ** the height is greater than the maximum allowed expression depth,
    ** leave an error in pParse.
    */
    static void sqlite3ExprSetHeight( Parse pParse, Expr p )
    {
      exprSetHeight( p );
      sqlite3ExprCheckHeight( pParse, p.nHeight );
    }

    /*
    ** Return the maximum height of any expression tree referenced
    ** by the select statement passed as an argument.
    */
    static int sqlite3SelectExprHeight( Select p )
    {
      int nHeight = 0;
      heightOfSelect( p, ref nHeight );
      return nHeight;
    }
#else
//#define exprSetHeight(y)
#endif //* SQLITE_MAX_EXPR_DEPTH>0 */

    /*
** This routine is the core allocator for Expr nodes.
**
** Construct a new expression node and return a pointer to it.  Memory
** for this node and for the pToken argument is a single allocation
** obtained from sqlite3DbMalloc().  The calling function
** is responsible for making sure the node eventually gets freed.
**
** If dequote is true, then the token (if it exists) is dequoted.
** If dequote is false, no dequoting is performance.  The deQuote
** parameter is ignored if pToken is NULL or if the token does not
** appear to be quoted.  If the quotes were of the form "..." (double-quotes)
** then the EP_DblQuoted flag is set on the expression node.
**
** Special case:  If op==TK_INTEGER and pToken points to a string that
** can be translated into a 32-bit integer, then the token is not
** stored in u.zToken.  Instead, the integer values is written
** into u.iValue and the EP_IntValue flag is set.  No extra storage
** is allocated to hold the integer text and the dequote flag is ignored.
*/
    static Expr sqlite3ExprAlloc(
    sqlite3 db,           /* Handle for sqlite3DbMallocZero() (may be null) */
    int op,               /* Expression opcode */
    Token pToken,         /* Token argument.  Might be NULL */
    int dequote           /* True to dequote */
    )
    {
      Expr pNew;
      int nExtra = 0;
      int iValue = 0;

      if ( pToken != null )
      {
        if ( op != TK_INTEGER || pToken.z == null || pToken.z.Length == 0
        || sqlite3GetInt32( pToken.z.ToString(), ref iValue ) == false )
        {
          nExtra = pToken.n + 1;
        }
      }
      pNew = new Expr();//sqlite3DbMallocZero(db, sizeof(Expr)+nExtra);
      if ( pNew != null )
      {
        pNew.op = (u8)op;
        pNew.iAgg = -1;
        if ( pToken != null )
        {
          if ( nExtra == 0 )
          {
            pNew.flags |= EP_IntValue;
            pNew.u.iValue = iValue;
          }
          else
          {
            int c;
            //pNew.u.zToken = (char*)&pNew[1];
            if ( pToken.n > 0 ) pNew.u.zToken = pToken.z.Substring( 0, pToken.n );//memcpy(pNew.u.zToken, pToken.z, pToken.n);
            //pNew.u.zToken[pToken.n] = 0;
            if ( dequote != 0 && nExtra >= 3
            && ( ( c = pToken.z[0] ) == '\'' || c == '"' || c == '[' || c == '`' ) )
            {
#if DEBUG_CLASS_EXPR || DEBUG_CLASS_ALL
sqlite3Dequote(ref pNew.u._zToken);
#else
              sqlite3Dequote( ref pNew.u.zToken );
#endif
              if ( c == '"' ) pNew.flags |= EP_DblQuoted;
            }
          }
        }
#if SQLITE_MAX_EXPR_DEPTH//>0
        pNew.nHeight = 1;
#endif
      }
      return pNew;
    }

    /*
    ** Allocate a new expression node from a zero-terminated token that has
    ** already been dequoted.
    */
    static Expr sqlite3Expr(
    sqlite3 db,           /* Handle for sqlite3DbMallocZero() (may be null) */
    int op,               /* Expression opcode */
    string zToken         /* Token argument.  Might be NULL */
    )
    {
      Token x = new Token();
      x.z = zToken;
      x.n = !String.IsNullOrEmpty( zToken ) ? sqlite3Strlen30( zToken ) : 0;
      return sqlite3ExprAlloc( db, op, x, 0 );
    }

    /*
    ** Attach subtrees pLeft and pRight to the Expr node pRoot.
    **
    ** If pRoot==NULL that means that a memory allocation error has occurred.
    ** In that case, delete the subtrees pLeft and pRight.
    */
    static void sqlite3ExprAttachSubtrees(
    sqlite3 db,
    Expr pRoot,
    Expr pLeft,
    Expr pRight
    )
    {
      if ( pRoot == null )
      {
        //Debug.Assert( db.mallocFailed != 0 );
        sqlite3ExprDelete( db, ref pLeft );
        sqlite3ExprDelete( db, ref pRight );
      }
      else
      {
        if ( pRight != null )
        {
          pRoot.pRight = pRight;
          if ( ( pRight.flags & EP_ExpCollate ) != 0 )
          {
            pRoot.flags |= EP_ExpCollate;
            pRoot.pColl = pRight.pColl;
          }
        }
        if ( pLeft != null )
        {
          pRoot.pLeft = pLeft;
          if ( ( pLeft.flags & EP_ExpCollate ) != 0 )
          {
            pRoot.flags |= EP_ExpCollate;
            pRoot.pColl = pLeft.pColl;
          }
        }
        exprSetHeight( pRoot );
      }
    }

    /*
    ** Allocate a Expr node which joins as many as two subtrees.
    **
    ** One or both of the subtrees can be NULL.  Return a pointer to the new
    ** Expr node.  Or, if an OOM error occurs, set pParse->db->mallocFailed,
    ** free the subtrees and return NULL.
    */
    // OVERLOADS, so I don't need to rewrite parse.c
    static Expr sqlite3PExpr( Parse pParse, int op, int null_3, int null_4, int null_5 )
    {
      return sqlite3PExpr( pParse, op, null, null, null );
    }
    static Expr sqlite3PExpr( Parse pParse, int op, int null_3, int null_4, Token pToken )
    {
      return sqlite3PExpr( pParse, op, null, null, pToken );
    }
    static Expr sqlite3PExpr( Parse pParse, int op, Expr pLeft, int null_4, int null_5 )
    {
      return sqlite3PExpr( pParse, op, pLeft, null, null );
    }
    static Expr sqlite3PExpr( Parse pParse, int op, Expr pLeft, int null_4, Token pToken )
    {
      return sqlite3PExpr( pParse, op, pLeft, null, pToken );
    }
    static Expr sqlite3PExpr( Parse pParse, int op, Expr pLeft, Expr pRight, int null_5 )
    {
      return sqlite3PExpr( pParse, op, pLeft, pRight, null );
    }
    static Expr sqlite3PExpr(
    Parse pParse,          /* Parsing context */
    int op,                 /* Expression opcode */
    Expr pLeft,            /* Left operand */
    Expr pRight,           /* Right operand */
    Token pToken     /* Argument Token */
    )
    {
      Expr p = sqlite3ExprAlloc( pParse.db, op, pToken, 1 );
      sqlite3ExprAttachSubtrees( pParse.db, p, pLeft, pRight );
      return p;
    }


    /*
    ** When doing a nested parse, you can include terms in an expression
    ** that look like this:   #1 #2 ...  These terms refer to registers
    ** in the virtual machine.  #N is the N-th register.
    **
    ** This routine is called by the parser to deal with on of those terms.
    ** It immediately generates code to store the value in a memory location.
    ** The returns an expression that will code to extract the value from
    ** that memory location as needed.
    */
    static Expr sqlite3RegisterExpr( Parse pParse, Token pToken )
    {
      Vdbe v = pParse.pVdbe;
      Expr p;
      if ( pParse.nested == 0 )
      {
        sqlite3ErrorMsg( pParse, "near \"%T\": syntax error", pToken );
        return sqlite3PExpr( pParse, TK_NULL, null, null, null );
      }
      if ( v == null ) return null;
      p = sqlite3PExpr( pParse, TK_REGISTER, null, null, pToken );
      if ( p == null )
      {
        return null;  /* Malloc failed */
      }
      p.u.iValue = atoi( pToken.z.Substring( 1 ) ); ;//atoi((char*)&pToken - z[1]);
      return p;
    }

    /*
    ** Join two expressions using an AND operator.  If either expression is
    ** NULL, then just return the other expression.
    */
    static Expr sqlite3ExprAnd( sqlite3 db, Expr pLeft, Expr pRight )
    {
      if ( pLeft == null )
      {
        return pRight;
      }
      else if ( pRight == null )
      {
        return pLeft;
      }
      else
      {
        Expr pNew = sqlite3ExprAlloc( db, TK_AND, null, 0 );
        sqlite3ExprAttachSubtrees( db, pNew, pLeft, pRight );
        return pNew;
      }
    }

    /*
    ** Construct a new expression node for a function with multiple
    ** arguments.
    */
    // OVERLOADS, so I don't need to rewrite parse.c
    static Expr sqlite3ExprFunction( Parse pParse, int null_2, Token pToken )
    {
      return sqlite3ExprFunction( pParse, null, pToken );
    }
    static Expr sqlite3ExprFunction( Parse pParse, ExprList pList, int null_3 )
    {
      return sqlite3ExprFunction( pParse, pList, null );
    }
    static Expr sqlite3ExprFunction( Parse pParse, ExprList pList, Token pToken )
    {
      Expr pNew;
      sqlite3 db = pParse.db;
      Debug.Assert( pToken != null );
      pNew = sqlite3ExprAlloc( db, TK_FUNCTION, pToken, 1 );
      if ( pNew == null )
      {
        sqlite3ExprListDelete( db, ref pList ); /* Avoid memory leak when malloc fails */
        return null;
      }
      pNew.x.pList = pList;
      Debug.Assert( !ExprHasProperty( pNew, EP_xIsSelect ) );

      sqlite3ExprSetHeight( pParse, pNew );
      return pNew;
    }

    /*
    ** Assign a variable number to an expression that encodes a wildcard
    ** in the original SQL statement.
    **
    ** Wildcards consisting of a single "?" are assigned the next sequential
    ** variable number.
    **
    ** Wildcards of the form "?nnn" are assigned the number "nnn".  We make
    ** sure "nnn" is not too be to avoid a denial of service attack when
    ** the SQL statement comes from an external source.
    **
    ** Wildcards of the form ":aaa", "@aaa" or "$aaa" are assigned the same number
    ** as the previous instance of the same wildcard.  Or if this is the first
    ** instance of the wildcard, the next sequenial variable number is
    ** assigned.
    */
    static void sqlite3ExprAssignVarNumber( Parse pParse, Expr pExpr )
    {
      sqlite3 db = pParse.db;
      string z;

      if ( pExpr == null ) return;
      Debug.Assert( !ExprHasAnyProperty( pExpr, EP_IntValue | EP_Reduced | EP_TokenOnly ) );
      z = pExpr.u.zToken;
      Debug.Assert( z != null );
      Debug.Assert( z.Length != 0 );
      if ( z.Length == 1 )
      {
        /* Wildcard of the form "?".  Assign the next variable number */
        Debug.Assert( z[0] == '?' );
        pExpr.iTable = ++pParse.nVar;
      }
      else if ( z[0] == '?' )
      {
        /* Wildcard of the form "?nnn".  Convert "nnn" to an integer and
        ** use it as the variable number */
        int i;
        pExpr.iTable = i = atoi( z.Substring( 1 ) );//atoi((char*)&z[1]);
        testcase( i == 0 );
        testcase( i == 1 );
        testcase( i == db.aLimit[SQLITE_LIMIT_VARIABLE_NUMBER] - 1 );
        testcase( i == db.aLimit[SQLITE_LIMIT_VARIABLE_NUMBER] );
        if ( i < 1 || i > db.aLimit[SQLITE_LIMIT_VARIABLE_NUMBER] )
        {
          sqlite3ErrorMsg( pParse, "variable number must be between ?1 and ?%d",
          db.aLimit[SQLITE_LIMIT_VARIABLE_NUMBER] );
        }
        if ( i > pParse.nVar )
        {
          pParse.nVar = i;
        }
      }
      else
      {
        /* Wildcards like ":aaa", "$aaa" or "@aaa".  Reuse the same variable
        ** number as the prior appearance of the same name, or if the name
        ** has never appeared before, reuse the same variable number
        */
        int i;
        int n;
        n = sqlite3Strlen30( z );
        for ( i = 0 ; i < pParse.nVarExpr ; i++ )
        {
          Expr pE = pParse.apVarExpr[i];
          Debug.Assert( pE != null );
          if ( memcmp( pE.u.zToken, z, n ) == 0 && pE.u.zToken.Length == n )
          {
            pExpr.iTable = pE.iTable;
            break;
          }
        }
        if ( i >= pParse.nVarExpr )
        {
          pExpr.iTable = ++pParse.nVar;
          if ( pParse.nVarExpr >= pParse.nVarExprAlloc - 1 )
          {
            pParse.nVarExprAlloc += pParse.nVarExprAlloc + 10;
            pParse.apVarExpr = new Expr[pParse.nVarExprAlloc];
            //sqlite3DbReallocOrFree(
            //  db,
            //  pParse.apVarExpr,
            //  pParse.nVarExprAlloc*sizeof(pParse.apVarExpr[0])
            //);
          }
          //if ( 0 == db.mallocFailed )
          {
            Debug.Assert( pParse.apVarExpr != null );
            pParse.apVarExpr[pParse.nVarExpr++] = pExpr;
          }
        }
      }
      if ( pParse.nErr == 0 && pParse.nVar > db.aLimit[SQLITE_LIMIT_VARIABLE_NUMBER] )
      {
        sqlite3ErrorMsg( pParse, "too many SQL variables" );
      }
    }

    /*
    ** Clear an expression structure without deleting the structure itself.
    ** Substructure is deleted.
    */
    static void sqlite3ExprClear( sqlite3 db, Expr p )
    {
      Debug.Assert( p != null );
      if ( !ExprHasAnyProperty( p, EP_TokenOnly ) )
      {
        sqlite3ExprDelete( db, ref p.pLeft );
        sqlite3ExprDelete( db, ref p.pRight );
        if ( !ExprHasProperty( p, EP_Reduced ) && ( p.flags2 & EP2_MallocedToken ) != 0 )
        {
#if DEBUG_CLASS_EXPR || DEBUG_CLASS_ALL
//sqlite3DbFree( db, ref p.u._zToken );
#else
          //sqlite3DbFree( db, ref p.u.zToken );
#endif
        }
        if ( ExprHasProperty( p, EP_xIsSelect ) )
        {
          sqlite3SelectDelete( db, ref p.x.pSelect );
        }
        else
        {
          sqlite3ExprListDelete( db, ref p.x.pList );
        }
      }
    }

    /*
    ** Recursively delete an expression tree.
    */
    static void sqlite3ExprDelete( sqlite3 db, ref Expr p )
    {
      if ( p == null ) return;
      sqlite3ExprClear( db, p );
      if ( !ExprHasProperty( p, EP_Static ) )
      {
        //sqlite3DbFree( db, ref p );
      }
    }

    /*
    ** Return the number of bytes allocated for the expression structure
    ** passed as the first argument. This is always one of EXPR_FULLSIZE,
    ** EXPR_REDUCEDSIZE or EXPR_TOKENONLYSIZE.
    */
    static int exprStructSize( Expr p )
    {
      if ( ExprHasProperty( p, EP_TokenOnly ) ) return EXPR_TOKENONLYSIZE;
      if ( ExprHasProperty( p, EP_Reduced ) ) return EXPR_REDUCEDSIZE;
      return EXPR_FULLSIZE;
    }

    /*
    ** The dupedExpr*Size() routines each return the number of bytes required
    ** to store a copy of an expression or expression tree.  They differ in
    ** how much of the tree is measured.
    **
    **     dupedExprStructSize()     Size of only the Expr structure
    **     dupedExprNodeSize()       Size of Expr + space for token
    **     dupedExprSize()           Expr + token + subtree components
    **
    ***************************************************************************
    **
    ** The dupedExprStructSize() function returns two values OR-ed together:
    ** (1) the space required for a copy of the Expr structure only and
    ** (2) the EP_xxx flags that indicate what the structure size should be.
    ** The return values is always one of:
    **
    **      EXPR_FULLSIZE
    **      EXPR_REDUCEDSIZE   | EP_Reduced
    **      EXPR_TOKENONLYSIZE | EP_TokenOnly
    **
    ** The size of the structure can be found by masking the return value
    ** of this routine with 0xfff.  The flags can be found by masking the
    ** return value with EP_Reduced|EP_TokenOnly.
    **
    ** Note that with flags==EXPRDUP_REDUCE, this routines works on full-size
    ** (unreduced) Expr objects as they or originally constructed by the parser.
    ** During expression analysis, extra information is computed and moved into
    ** later parts of teh Expr object and that extra information might get chopped
    ** off if the expression is reduced.  Note also that it does not work to
    ** make a EXPRDUP_REDUCE copy of a reduced expression.  It is only legal
    ** to reduce a pristine expression tree from the parser.  The implementation
    ** of dupedExprStructSize() contain multiple assert() statements that attempt
    ** to enforce this constraint.
    */
    static int dupedExprStructSize( Expr p, int flags )
    {
      int nSize;
      Debug.Assert( flags == EXPRDUP_REDUCE || flags == 0 ); /* Only one flag value allowed */
      if ( 0 == ( flags & EXPRDUP_REDUCE ) )
      {
        nSize = EXPR_FULLSIZE;
      }
      else
      {
        Debug.Assert( !ExprHasAnyProperty( p, EP_TokenOnly | EP_Reduced ) );
        Debug.Assert( !ExprHasProperty( p, EP_FromJoin ) );
        Debug.Assert( ( p.flags2 & EP2_MallocedToken ) == 0 );
        Debug.Assert( ( p.flags2 & EP2_Irreducible ) == 0 );
        if ( p.pLeft != null || p.pRight != null || p.pColl != null || p.x.pList != null || p.x.pSelect != null )
        {
          nSize = EXPR_REDUCEDSIZE | EP_Reduced;
        }
        else
        {
          nSize = EXPR_TOKENONLYSIZE | EP_TokenOnly;
        }
      }
      return nSize;
    }

    /*
    ** This function returns the space in bytes required to store the copy
    ** of the Expr structure and a copy of the Expr.u.zToken string (if that
    ** string is defined.)
    */
    static int dupedExprNodeSize( Expr p, int flags )
    {
      int nByte = dupedExprStructSize( p, flags ) & 0xfff;
      if ( !ExprHasProperty( p, EP_IntValue ) && p.u.zToken != null )
      {
        nByte += sqlite3Strlen30( p.u.zToken ) + 1;
      }
      return ROUND8( nByte );
    }

    /*
    ** Return the number of bytes required to create a duplicate of the
    ** expression passed as the first argument. The second argument is a
    ** mask containing EXPRDUP_XXX flags.
    **
    ** The value returned includes space to create a copy of the Expr struct
    ** itself and the buffer referred to by Expr.u.zToken, if any.
    **
    ** If the EXPRDUP_REDUCE flag is set, then the return value includes
    ** space to duplicate all Expr nodes in the tree formed by Expr.pLeft
    ** and Expr.pRight variables (but not for any structures pointed to or
    ** descended from the Expr.x.pList or Expr.x.pSelect variables).
    */
    static int dupedExprSize( Expr p, int flags )
    {
      int nByte = 0;
      if ( p != null )
      {
        nByte = dupedExprNodeSize( p, flags );
        if ( ( flags & EXPRDUP_REDUCE ) != 0 )
        {
          nByte += dupedExprSize( p.pLeft, flags ) + dupedExprSize( p.pRight, flags );
        }
      }
      return nByte;
    }

    /*
    ** This function is similar to sqlite3ExprDup(), except that if pzBuffer
    ** is not NULL then *pzBuffer is assumed to point to a buffer large enough
    ** to store the copy of expression p, the copies of p->u.zToken
    ** (if applicable), and the copies of the p->pLeft and p->pRight expressions,
    ** if any. Before returning, *pzBuffer is set to the first byte passed the
    ** portion of the buffer copied into by this function.
    */
    static Expr exprDup( sqlite3 db, Expr p, int flags, ref Expr pzBuffer )
    {
      Expr pNew = null;                      /* Value to return */
      if ( p != null )
      {
        bool isReduced = ( flags & EXPRDUP_REDUCE ) != 0;
        Expr zAlloc = new Expr();
        u32 staticFlag = 0;

        Debug.Assert( pzBuffer == null || isReduced );

        /* Figure out where to write the new Expr structure. */
        //if ( pzBuffer !=null)
        //{
        //  zAlloc = pzBuffer;
        //  staticFlag = EP_Static;
        //}
        //else
        //{
        //  zAlloc = new Expr();//sqlite3DbMallocRaw( db, dupedExprSize( p, flags ) );
        //}
        pNew = p.Copy_Minimal();// (Expr*)zAlloc;

        if ( pNew != null )
        {
          /* Set nNewSize to the size allocated for the structure pointed to
          ** by pNew. This is either EXPR_FULLSIZE, EXPR_REDUCEDSIZE or
          ** EXPR_TOKENONLYSIZE. nToken is set to the number of bytes consumed
          ** by the copy of the p->u.zToken string (if any).
          */
          int nStructSize = dupedExprStructSize( p, flags );
          int nNewSize = nStructSize & 0xfff;
          int nToken;
          if ( !ExprHasProperty( p, EP_IntValue ) && !String.IsNullOrEmpty( p.u.zToken ) )
          {
            nToken = sqlite3Strlen30( p.u.zToken );
          }
          else
          {
            nToken = 0;
          }
          if ( isReduced )
          {
            Debug.Assert( !ExprHasProperty( p, EP_Reduced ) );
            //memcpy( zAlloc, p, nNewSize );
          }
          else
          {
            int nSize = exprStructSize( p );
            //memcpy( zAlloc, p, nSize );
            //memset( &zAlloc[nSize], 0, EXPR_FULLSIZE - nSize );
          }

          /* Set the EP_Reduced, EP_TokenOnly, and EP_Static flags appropriately. */
          unchecked { pNew.flags &= (ushort)( ~( EP_Reduced | EP_TokenOnly | EP_Static ) ); }
          pNew.flags |= (ushort)( nStructSize & ( EP_Reduced | EP_TokenOnly ) );
          pNew.flags |= (ushort)staticFlag;

          /* Copy the p->u.zToken string, if any. */
          if ( nToken != 0 )
          {
            string zToken;// = pNew.u.zToken = (char*)&zAlloc[nNewSize];
            zToken = p.u.zToken.Substring( 0, nToken );// memcpy( zToken, p.u.zToken, nToken );
          }

          if ( 0 == ( ( p.flags | pNew.flags ) & EP_TokenOnly ) )
          {
            /* Fill in the pNew.x.pSelect or pNew.x.pList member. */
            if ( ExprHasProperty( p, EP_xIsSelect ) )
            {
              pNew.x.pSelect = sqlite3SelectDup( db, p.x.pSelect, isReduced ? 1 : 0 );
            }
            else
            {
              pNew.x.pList = sqlite3ExprListDup( db, p.x.pList, isReduced ? 1 : 0 );
            }
          }

          /* Fill in pNew.pLeft and pNew.pRight. */
          if ( ExprHasAnyProperty( pNew, EP_Reduced | EP_TokenOnly ) )
          {
            //zAlloc += dupedExprNodeSize( p, flags );
            if ( ExprHasProperty( pNew, EP_Reduced ) )
            {
              pNew.pLeft = exprDup( db, p.pLeft, EXPRDUP_REDUCE, ref zAlloc );
              pNew.pRight = exprDup( db, p.pRight, EXPRDUP_REDUCE, ref zAlloc );
            }
            if ( pzBuffer != null )
            {
              pzBuffer = zAlloc;
            }
          }
          else
          {
            pNew.flags2 = 0;
            if ( !ExprHasAnyProperty( p, EP_TokenOnly ) )
            {
              pNew.pLeft = sqlite3ExprDup( db, p.pLeft, 0 );
              pNew.pRight = sqlite3ExprDup( db, p.pRight, 0 );
            }
          }
        }
      }
      return pNew;
    }

    /*
    ** The following group of routines make deep copies of expressions,
    ** expression lists, ID lists, and select statements.  The copies can
    ** be deleted (by being passed to their respective ...Delete() routines)
    ** without effecting the originals.
    **
    ** The expression list, ID, and source lists return by sqlite3ExprListDup(),
    ** sqlite3IdListDup(), and sqlite3SrcListDup() can not be further expanded
    ** by subsequent calls to sqlite*ListAppend() routines.
    **
    ** Any tables that the SrcList might point to are not duplicated.
    **
    ** The flags parameter contains a combination of the EXPRDUP_XXX flags.
    ** If the EXPRDUP_REDUCE flag is set, then the structure returned is a
    ** truncated version of the usual Expr structure that will be stored as
    ** part of the in-memory representation of the database schema.
    */
    static Expr sqlite3ExprDup( sqlite3 db, Expr p, int flags )
    {
      Expr ExprDummy = null;
      return exprDup( db, p, flags, ref ExprDummy );
    }

    static ExprList sqlite3ExprListDup( sqlite3 db, ExprList p, int flags )
    {
      ExprList pNew;
      ExprList_item pItem;
      ExprList_item pOldItem;
      int i;
      if ( p == null ) return null;
      pNew = new ExprList();//sqlite3DbMallocRaw(db, sizeof(*pNew) );
      if ( pNew == null ) return null;
      pNew.iECursor = 0;
      pNew.nExpr = pNew.nAlloc = p.nExpr;
      pNew.a = new ExprList_item[p.nExpr];//sqlite3DbMallocRaw(db,  p.nExpr*sizeof(p.a[0]) );
      //if( pItem==null ){
      //  //sqlite3DbFree(db,ref pNew);
      //  return null;
      //}
      //pOldItem = p.a;
      for ( i = 0 ; i < p.nExpr ; i++ )
      {//pItem++, pOldItem++){
        pItem = pNew.a[i] = new ExprList_item();
        pOldItem = p.a[i];
        Expr pOldExpr = pOldItem.pExpr;
        pItem.pExpr = sqlite3ExprDup( db, pOldExpr, flags );
        pItem.zName = pOldItem.zName;// sqlite3DbStrDup(db, pOldItem.zName);
        pItem.zSpan = pOldItem.zSpan;// sqlite3DbStrDup( db, pOldItem.zSpan );
        pItem.sortOrder = pOldItem.sortOrder;
        pItem.done = 0;
        pItem.iCol = pOldItem.iCol;
        pItem.iAlias = pOldItem.iAlias;
      }
      return pNew;
    }

    /*
    ** If cursors, triggers, views and subqueries are all omitted from
    ** the build, then none of the following routines, except for
    ** sqlite3SelectDup(), can be called. sqlite3SelectDup() is sometimes
    ** called with a NULL argument.
    */
#if !SQLITE_OMIT_VIEW || !SQLITE_OMIT_TRIGGER  || !SQLITE_OMIT_SUBQUERY
    static SrcList sqlite3SrcListDup( sqlite3 db, SrcList p, int flags )
    {
      SrcList pNew;
      int i;
      int nByte;
      if ( p == null ) return null;
      //nByte = sizeof(*p) + (p.nSrc>0 ? sizeof(p.a[0]) * (p.nSrc-1) : 0);
      pNew = new SrcList();//sqlite3DbMallocRaw(db, nByte );
      if ( p.nSrc > 0 ) pNew.a = new SrcList_item[p.nSrc];
      if ( pNew == null ) return null;
      pNew.nSrc = pNew.nAlloc = p.nSrc;
      for ( i = 0 ; i < p.nSrc ; i++ )
      {
        pNew.a[i] = new SrcList_item();
        SrcList_item pNewItem = pNew.a[i];
        SrcList_item pOldItem = p.a[i];
        Table pTab;
        pNewItem.zDatabase = pOldItem.zDatabase;// sqlite3DbStrDup(db, pOldItem.zDatabase);
        pNewItem.zName = pOldItem.zName;// sqlite3DbStrDup(db, pOldItem.zName);
        pNewItem.zAlias = pOldItem.zAlias;// sqlite3DbStrDup(db, pOldItem.zAlias);
        pNewItem.jointype = pOldItem.jointype;
        pNewItem.iCursor = pOldItem.iCursor;
        pNewItem.isPopulated = pOldItem.isPopulated;
        pNewItem.zIndex = pOldItem.zIndex;// sqlite3DbStrDup( db, pOldItem.zIndex );
        pNewItem.notIndexed = pOldItem.notIndexed;
        pNewItem.pIndex = pOldItem.pIndex;
        pTab = pNewItem.pTab = pOldItem.pTab;
        if ( pTab != null )
        {
          pTab.nRef++;
        }
        pNewItem.pSelect = sqlite3SelectDup( db, pOldItem.pSelect, flags );
        pNewItem.pOn = sqlite3ExprDup( db, pOldItem.pOn, flags );
        pNewItem.pUsing = sqlite3IdListDup( db, pOldItem.pUsing );
        pNewItem.colUsed = pOldItem.colUsed;
      }
      return pNew;
    }

    static IdList sqlite3IdListDup( sqlite3 db, IdList p )
    {
      IdList pNew;
      int i;
      if ( p == null ) return null;
      pNew = new IdList();//sqlite3DbMallocRaw(db, sizeof(*pNew) );
      if ( pNew == null ) return null;
      pNew.nId = pNew.nAlloc = p.nId;
      pNew.a = new IdList_item[p.nId];//sqlite3DbMallocRaw(db, p.nId*sizeof(p.a[0]) );
      if ( pNew.a == null )
      {
        //sqlite3DbFree( db, ref pNew );
        return null;
      }
      for ( i = 0 ; i < p.nId ; i++ )
      {
        pNew.a[i] = new IdList_item();
        IdList_item pNewItem = pNew.a[i];
        IdList_item pOldItem = p.a[i];
        pNewItem.zName = pOldItem.zName;// sqlite3DbStrDup(db, pOldItem.zName);
        pNewItem.idx = pOldItem.idx;
      }
      return pNew;
    }

    static Select sqlite3SelectDup( sqlite3 db, Select p, int flags )
    {
      Select pNew;
      if ( p == null ) return null;
      pNew = new Select();//sqlite3DbMallocRaw(db, sizeof(*p) );
      if ( pNew == null ) return null;
      pNew.pEList = sqlite3ExprListDup( db, p.pEList, flags );
      pNew.pSrc = sqlite3SrcListDup( db, p.pSrc, flags );
      pNew.pWhere = sqlite3ExprDup( db, p.pWhere, flags );
      pNew.pGroupBy = sqlite3ExprListDup( db, p.pGroupBy, flags );
      pNew.pHaving = sqlite3ExprDup( db, p.pHaving, flags );
      pNew.pOrderBy = sqlite3ExprListDup( db, p.pOrderBy, flags );
      pNew.op = p.op;
      pNew.pPrior = sqlite3SelectDup( db, p.pPrior, flags );
      pNew.pLimit = sqlite3ExprDup( db, p.pLimit, flags );
      pNew.pOffset = sqlite3ExprDup( db, p.pOffset, flags );
      pNew.iLimit = 0;
      pNew.iOffset = 0;
      pNew.selFlags = (u16)( p.selFlags & ~SF_UsesEphemeral );
      pNew.pRightmost = null;
      pNew.addrOpenEphm[0] = -1;
      pNew.addrOpenEphm[1] = -1;
      pNew.addrOpenEphm[2] = -1;
      return pNew;
    }
#else
Select sqlite3SelectDup(sqlite3 db, Select p, int flags){
Debug.Assert( p==null );
return null;
}
#endif


    /*
** Add a new element to the end of an expression list.  If pList is
** initially NULL, then create a new expression list.
**
** If a memory allocation error occurs, the entire list is freed and
** NULL is returned.  If non-NULL is returned, then it is guaranteed
** that the new entry was successfully appended.
*/
    // OVERLOADS, so I don't need to rewrite parse.c
    static ExprList sqlite3ExprListAppend( Parse pParse, int null_2, Expr pExpr )
    {
      return sqlite3ExprListAppend( pParse, null, pExpr );
    }
    static ExprList sqlite3ExprListAppend(
    Parse pParse,          /* Parsing context */
    ExprList pList,        /* List to which to append. Might be NULL */
    Expr pExpr             /* Expression to be appended. Might be NULL */
    )
    {
      sqlite3 db = pParse.db;
      if ( pList == null )
      {
        pList = new ExprList();  //sqlite3DbMallocZero(db, ExprList).Length;
        if ( pList == null )
        {
          goto no_mem;
        }
        Debug.Assert( pList.nAlloc == 0 );
      }
      if ( pList.nAlloc <= pList.nExpr )
      {
        ExprList_item a;
        int n = pList.nAlloc * 2 + 4;
        //a = sqlite3DbRealloc(db, pList.a, n*sizeof(pList.a[0]));
        //if( a==0 ){
        //  goto no_mem;
        //}
        Array.Resize( ref pList.a, n );// = a;
        pList.nAlloc = pList.a.Length;// sqlite3DbMallocSize(db, a)/sizeof(a[0]);
      }
      Debug.Assert( pList.a != null );
      if ( true )
      {
        pList.a[pList.nExpr] = new ExprList_item(); ;
        ExprList_item pItem = pList.a[pList.nExpr++];
        //pItem = new ExprList_item();//memset(pItem, 0, sizeof(*pItem));
        pItem.pExpr = pExpr;
      }
      return pList;

no_mem:
      /* Avoid leaking memory if malloc has failed. */
      sqlite3ExprDelete( db, ref pExpr );
      sqlite3ExprListDelete( db, ref pList );
      return null;
    }

    /*
    ** Set the ExprList.a[].zName element of the most recently added item
    ** on the expression list.
    **
    ** pList might be NULL following an OOM error.  But pName should never be
    ** NULL.  If a memory allocation fails, the pParse.db.mallocFailed flag
    ** is set.
    */
    static void sqlite3ExprListSetName(
    Parse pParse,          /* Parsing context */
    ExprList pList,        /* List to which to add the span. */
    Token pName,           /* Name to be added */
    int dequote            /* True to cause the name to be dequoted */
    )
    {
      Debug.Assert( pList != null /* || pParse.db.mallocFailed != 0 */ );
      if ( pList != null )
      {
        ExprList_item pItem;
        Debug.Assert( pList.nExpr > 0 );
        pItem = pList.a[pList.nExpr - 1];
        Debug.Assert( pItem.zName == null );
        pItem.zName = pName.z.Substring( 0, pName.n );//sqlite3DbStrNDup(pParse.db, pName.z, pName.n);
        if ( dequote != 0 && !String.IsNullOrEmpty( pItem.zName ) ) sqlite3Dequote( ref pItem.zName );
      }
    }

    /*
    ** Set the ExprList.a[].zSpan element of the most recently added item
    ** on the expression list.
    **
    ** pList might be NULL following an OOM error.  But pSpan should never be
    ** NULL.  If a memory allocation fails, the pParse.db.mallocFailed flag
    ** is set.
    */
    static void sqlite3ExprListSetSpan(
    Parse pParse,          /* Parsing context */
    ExprList pList,        /* List to which to add the span. */
    ExprSpan pSpan         /* The span to be added */
    )
    {
      sqlite3 db = pParse.db;
      Debug.Assert( pList != null /*|| db.mallocFailed != 0 */ );
      if ( pList != null )
      {
        ExprList_item pItem = pList.a[pList.nExpr - 1];
        Debug.Assert( pList.nExpr > 0 );
        Debug.Assert( /* db.mallocFailed != 0 || */ pItem.pExpr == pSpan.pExpr );
        //sqlite3DbFree( db, pItem.zSpan );
        pItem.zSpan = pSpan.zStart.Substring( 0, pSpan.zStart.Length <= pSpan.zEnd.Length ? pSpan.zStart.Length : pSpan.zStart.Length - pSpan.zEnd.Length );// sqlite3DbStrNDup( db, pSpan.zStart,
        //(int)( pSpan.zEnd- pSpan.zStart) );
      }
    }

    /*
    ** If the expression list pEList contains more than iLimit elements,
    ** leave an error message in pParse.
    */
    static void sqlite3ExprListCheckLength(
    Parse pParse,
    ExprList pEList,
    string zObject
    )
    {
      int mx = pParse.db.aLimit[SQLITE_LIMIT_COLUMN];
      testcase( pEList != null && pEList.nExpr == mx );
      testcase( pEList != null && pEList.nExpr == mx + 1 );
      if ( pEList != null && pEList.nExpr > mx )
      {
        sqlite3ErrorMsg( pParse, "too many columns in %s", zObject );
      }
    }


    /*
    ** Delete an entire expression list.
    */
    static void sqlite3ExprListDelete( sqlite3 db, ref ExprList pList )
    {
      int i;
      ExprList_item pItem;
      if ( pList == null ) return;
      Debug.Assert( pList.a != null || ( pList.nExpr == 0 && pList.nAlloc == 0 ) );
      Debug.Assert( pList.nExpr <= pList.nAlloc );
      for ( i = 0 ; i < pList.nExpr ; i++ )
      {
        if ( ( pItem = pList.a[i] ) != null )
        {
          sqlite3ExprDelete( db, ref pItem.pExpr );
          //sqlite3DbFree( db, ref pItem.zName );
          //sqlite3DbFree( db, ref pItem.zSpan );
        }
      }
      //sqlite3DbFree( db, ref pList.a );
      //sqlite3DbFree( db, ref pList );
    }

    /*
    ** These routines are Walker callbacks.  Walker.u.pi is a pointer
    ** to an integer.  These routines are checking an expression to see
    ** if it is a constant.  Set *Walker.u.pi to 0 if the expression is
    ** not constant.
    **
    ** These callback routines are used to implement the following:
    **
    **     sqlite3ExprIsConstant()
    **     sqlite3ExprIsConstantNotJoin()
    **     sqlite3ExprIsConstantOrFunction()
    **
    */
    static int exprNodeIsConstant( Walker pWalker, ref Expr pExpr )
    {
      /* If pWalker.u.i is 3 then any term of the expression that comes from
      ** the ON or USING clauses of a join disqualifies the expression
      ** from being considered constant. */
      if ( pWalker.u.i == 3 && ExprHasAnyProperty( pExpr, EP_FromJoin ) )
      {
        pWalker.u.i = 0;
        return WRC_Abort;
      }

      switch ( pExpr.op )
      {
        /* Consider functions to be constant if all their arguments are constant
        ** and pWalker.u.i==2 */
        case TK_FUNCTION:
          if ( ( pWalker.u.i ) == 2 ) return 0;
          goto case TK_ID;
        /* Fall through */
        case TK_ID:
        case TK_COLUMN:
        case TK_AGG_FUNCTION:
        case TK_AGG_COLUMN:
          testcase( pExpr.op == TK_ID );
          testcase( pExpr.op == TK_COLUMN );
          testcase( pExpr.op == TK_AGG_FUNCTION );
          testcase( pExpr.op == TK_AGG_COLUMN );
          pWalker.u.i = 0;
          return WRC_Abort;
        default:
          testcase( pExpr.op == TK_SELECT ); /* selectNodeIsConstant will disallow */
          testcase( pExpr.op == TK_EXISTS ); /* selectNodeIsConstant will disallow */
          return WRC_Continue;
      }
    }

    static int selectNodeIsConstant( Walker pWalker, Select NotUsed )
    {
      UNUSED_PARAMETER( NotUsed );
      pWalker.u.i = 0;
      return WRC_Abort;
    }
    static int exprIsConst( Expr p, int initFlag )
    {
      Walker w = new Walker();
      w.u.i = initFlag;
      w.xExprCallback = exprNodeIsConstant;
      w.xSelectCallback = selectNodeIsConstant;
      sqlite3WalkExpr( w, ref p );
      return w.u.i;
    }

    /*
    ** Walk an expression tree.  Return 1 if the expression is constant
    ** and 0 if it involves variables or function calls.
    **
    ** For the purposes of this function, a double-quoted string (ex: "abc")
    ** is considered a variable but a single-quoted string (ex: 'abc') is
    ** a constant.
    */
    static int sqlite3ExprIsConstant( Expr p )
    {
      return exprIsConst( p, 1 );
    }

    /*
    ** Walk an expression tree.  Return 1 if the expression is constant
    ** that does no originate from the ON or USING clauses of a join.
    ** Return 0 if it involves variables or function calls or terms from
    ** an ON or USING clause.
    */
    static int sqlite3ExprIsConstantNotJoin( Expr p )
    {
      return exprIsConst( p, 3 );
    }

    /*
    ** Walk an expression tree.  Return 1 if the expression is constant
    ** or a function call with constant arguments.  Return and 0 if there
    ** are any variables.
    **
    ** For the purposes of this function, a double-quoted string (ex: "abc")
    ** is considered a variable but a single-quoted string (ex: 'abc') is
    ** a constant.
    */
    static int sqlite3ExprIsConstantOrFunction( Expr p )
    {
      return exprIsConst( p, 2 );
    }

    /*
    ** If the expression p codes a constant integer that is small enough
    ** to fit in a 32-bit integer, return 1 and put the value of the integer
    ** in pValue.  If the expression is not an integer or if it is too big
    ** to fit in a signed 32-bit integer, return 0 and leave pValue unchanged.
    */
    static int sqlite3ExprIsInteger( Expr p, ref int pValue )
    {
      int rc = 0;
      if ( ( p.flags & EP_IntValue ) != 0 )
      {
        pValue = (int)p.u.iValue;
        return 1;
      }
      switch ( p.op )
      {
        case TK_INTEGER:
          {
            rc = sqlite3GetInt32( p.u.zToken, ref pValue ) ? 1 : 0;
            Debug.Assert( rc == 0 );
            break;
          }
        case TK_UPLUS:
          {
            rc = sqlite3ExprIsInteger( p.pLeft, ref  pValue );
            break;
          }
        case TK_UMINUS:
          {
            int v = 0;
            if ( sqlite3ExprIsInteger( p.pLeft, ref v ) != 0 )
            {
              pValue = -v;
              rc = 1;
            }
            break;
          }
        default: break;
      }
      if ( rc != 0 )
      {
        Debug.Assert( ExprHasAnyProperty( p, EP_Reduced | EP_TokenOnly )
        || ( p.flags2 & EP2_MallocedToken ) == 0 );
        p.op = TK_INTEGER;
        p.flags |= EP_IntValue;
        p.u.iValue = pValue;
      }
      return rc;
    }

    /*
    ** Return TRUE if the given string is a row-id column name.
    */
    static bool sqlite3IsRowid( string z )
    {
      if ( sqlite3StrICmp( z, "_ROWID_" ) == 0 ) return true;
      if ( sqlite3StrICmp( z, "ROWID" ) == 0 ) return true;
      if ( sqlite3StrICmp( z, "OID" ) == 0 ) return true;
      return false;
    }


    /*
    ** Return true if we are able to the IN operator optimization on a
    ** query of the form
    **
    **       x IN (SELECT ...)
    **
    ** Where the SELECT... clause is as specified by the parameter to this
    ** routine.
    **
    ** The Select object passed in has already been preprocessed and no
    ** errors have been found.
    */
#if !SQLITE_OMIT_SUBQUERY
    static int isCandidateForInOpt( Select p )
    {
      SrcList pSrc;
      ExprList pEList;
      Table pTab;
      if ( p == null ) return 0;                   /* right-hand side of IN is SELECT */
      if ( p.pPrior != null ) return 0;              /* Not a compound SELECT */
      if ( ( p.selFlags & ( SF_Distinct | SF_Aggregate ) ) != 0 )
      {
        testcase( ( p.selFlags & ( SF_Distinct | SF_Aggregate ) ) == SF_Distinct );
        testcase( ( p.selFlags & ( SF_Distinct | SF_Aggregate ) ) == SF_Aggregate );
        return 0; /* No DISTINCT keyword and no aggregate functions */
      }
      Debug.Assert( p.pGroupBy == null );         /* Has no GROUP BY clause */
      if ( p.pLimit != null ) return 0;           /* Has no LIMIT clause */
      Debug.Assert( p.pOffset == null );          /* No LIMIT means no OFFSET */

      if ( p.pWhere != null ) return 0;           /* Has no WHERE clause */
      pSrc = p.pSrc;
      Debug.Assert( pSrc != null );
      if ( pSrc.nSrc != 1 ) return 0;             /* Single term in FROM clause */
      if ( pSrc.a[0].pSelect != null ) return 0;  /* FROM is not a subquery or view */
      pTab = pSrc.a[0].pTab;
      if ( NEVER( pTab == null ) ) return 0;
      Debug.Assert( pTab.pSelect == null );       /* FROM clause is not a view */
      if ( IsVirtual( pTab ) ) return 0;          /* FROM clause not a virtual table */
      pEList = p.pEList;
      if ( pEList.nExpr != 1 ) return 0;          /* One column in the result set */
      if ( pEList.a[0].pExpr.op != TK_COLUMN ) return 0; /* Result is a column */
      return 1;
    }
#endif //* SQLITE_OMIT_SUBQUERY */

    /*
** This function is used by the implementation of the IN (...) operator.
** It's job is to find or create a b-tree structure that may be used
** either to test for membership of the (...) set or to iterate through
** its members, skipping duplicates.
**
** The index of the cursor opened on the b-tree (database table, database index
** or ephermal table) is stored in pX->iTable before this function returns.
** The returned value of this function indicates the b-tree type, as follows:
**
**   IN_INDEX_ROWID - The cursor was opened on a database table.
**   IN_INDEX_INDEX - The cursor was opened on a database index.
**   IN_INDEX_EPH -   The cursor was opened on a specially created and
**                    populated epheremal table.
**
** An existing b-tree may only be used if the SELECT is of the simple
** form:
**
**     SELECT <column> FROM <table>
**
** If the prNotFound parameter is 0, then the b-tree will be used to iterate
** through the set members, skipping any duplicates. In this case an
** epheremal table must be used unless the selected <column> is guaranteed
** to be unique - either because it is an INTEGER PRIMARY KEY or it
** has a UNIQUE constraint or UNIQUE index.
**
** If the prNotFound parameter is not 0, then the b-tree will be used
** for fast set membership tests. In this case an epheremal table must
** be used unless <column> is an INTEGER PRIMARY KEY or an index can
** be found with <column> as its left-most column.
**
** When the b-tree is being used for membership tests, the calling function
** needs to know whether or not the structure contains an SQL NULL
** value in order to correctly evaluate expressions like "X IN (Y, Z)".
** If there is a chance that the b-tree might contain a NULL value at
** runtime, then a register is allocated and the register number written
** to *prNotFound. If there is no chance that the b-tree contains a
** NULL value, then *prNotFound is left unchanged.
**
** If a register is allocated and its location stored in *prNotFound, then
** its initial value is NULL. If the b-tree does not remain constant
** for the duration of the query (i.e. the SELECT that generates the b-tree
** is a correlated subquery) then the value of the allocated register is
** reset to NULL each time the b-tree is repopulated. This allows the
** caller to use vdbe code equivalent to the following:
**
**   if( register==NULL ){
**     has_null = <test if data structure contains null>
**     register = 1
**   }
**
** in order to avoid running the <test if data structure contains null>
** test more often than is necessary.
*/
#if !SQLITE_OMIT_SUBQUERY
    static int sqlite3FindInIndex( Parse pParse, Expr pX, ref int prNotFound )
    {
      Select p;                             /* SELECT to the right of IN operator */
      int eType = 0;                        /* Type of RHS table. IN_INDEX_* */
      int iTab = pParse.nTab++;             /* Cursor of the RHS table */
      bool mustBeUnique = ( prNotFound != 0 );   /* True if RHS must be unique */

      /* Check to see if an existing table or index can be used to
      ** satisfy the query.  This is preferable to generating a new
      ** ephemeral table.
      */
      p = ( ExprHasProperty( pX, EP_xIsSelect ) ? pX.x.pSelect : null );
      if ( ALWAYS( pParse.nErr == 0 ) && isCandidateForInOpt( p ) != 0 )
      {
        sqlite3 db = pParse.db;               /* Database connection */
        Expr pExpr = p.pEList.a[0].pExpr;     /* Expression <column> */
        int iCol = pExpr.iColumn;             /* Index of column <column> */
        Vdbe v = sqlite3GetVdbe( pParse );      /* Virtual machine being coded */
        Table pTab = p.pSrc.a[0].pTab;        /* Table <table>. */
        int iDb;                              /* Database idx for pTab */

        /* Code an OP_VerifyCookie and OP_TableLock for <table>. */
        iDb = sqlite3SchemaToIndex( db, pTab.pSchema );
        sqlite3CodeVerifySchema( pParse, iDb );
        sqlite3TableLock( pParse, iDb, pTab.tnum, 0, pTab.zName );

        /* This function is only called from two places. In both cases the vdbe
        ** has already been allocated. So assume sqlite3GetVdbe() is always
        ** successful here.
        */
        Debug.Assert( v != null );
        if ( iCol < 0 )
        {
          int iMem = ++pParse.nMem;
          int iAddr;
          sqlite3VdbeUsesBtree( v, iDb );

          iAddr = sqlite3VdbeAddOp1( v, OP_If, iMem );
          sqlite3VdbeAddOp2( v, OP_Integer, 1, iMem );

          sqlite3OpenTable( pParse, iTab, iDb, pTab, OP_OpenRead );
          eType = IN_INDEX_ROWID;

          sqlite3VdbeJumpHere( v, iAddr );
        }
        else
        {
          Index pIdx;                         /* Iterator variable */
          /* The collation sequence used by the comparison. If an index is to
          ** be used in place of a temp.table, it must be ordered according
          ** to this collation sequence. */
          CollSeq pReq = sqlite3BinaryCompareCollSeq( pParse, pX.pLeft, pExpr );

          /* Check that the affinity that will be used to perform the
          ** comparison is the same as the affinity of the column. If
          ** it is not, it is not possible to use any index.
          */
          char aff = comparisonAffinity( pX );
          bool affinity_ok = ( pTab.aCol[iCol].affinity == aff || aff == SQLITE_AFF_NONE );

          for ( pIdx = pTab.pIndex ; pIdx != null && eType == 0 && affinity_ok ; pIdx = pIdx.pNext )
          {
            if ( ( pIdx.aiColumn[0] == iCol )
            && ( sqlite3FindCollSeq( db, ENC( db ), pIdx.azColl[0], 0 ) == pReq )
            && ( mustBeUnique == false || ( pIdx.nColumn == 1 && pIdx.onError != OE_None ) )
            )
            {
              int iMem = ++pParse.nMem;
              int iAddr;
              KeyInfo pKey;

              pKey = sqlite3IndexKeyinfo( pParse, pIdx );
              iDb = sqlite3SchemaToIndex( db, pIdx.pSchema );
              sqlite3VdbeUsesBtree( v, iDb );

              iAddr = sqlite3VdbeAddOp1( v, OP_If, iMem );
              sqlite3VdbeAddOp2( v, OP_Integer, 1, iMem );

              sqlite3VdbeAddOp4( v, OP_OpenRead, iTab, pIdx.tnum, iDb,
              pKey, P4_KEYINFO_HANDOFF );
#if SQLITE_DEBUG
              VdbeComment( v, "%s", pIdx.zName );
#endif
              eType = IN_INDEX_INDEX;

              sqlite3VdbeJumpHere( v, iAddr );
              if ( //prNotFound != null &&         -- always exists under C#
              pTab.aCol[iCol].notNull == 0 )
              {
                prNotFound = ++pParse.nMem;
              }
            }
          }
        }
      }

      if ( eType == 0 )
      {
        /* Could not found an existing able or index to use as the RHS b-tree.
        ** We will have to generate an ephemeral table to do the job.
        */
        int rMayHaveNull = 0;
        eType = IN_INDEX_EPH;
        if ( prNotFound != -1 )  // Klude to show prNotFound not available
        {
          prNotFound = rMayHaveNull = ++pParse.nMem;
        }
        else
          if ( pX.pLeft.iColumn < 0 && !ExprHasAnyProperty( pX, EP_xIsSelect ) )
          {
            eType = IN_INDEX_ROWID;
          }
        sqlite3CodeSubselect( pParse, pX, rMayHaveNull, eType == IN_INDEX_ROWID );
      }
      else
      {
        pX.iTable = iTab;
      }
      return eType;
    }
#endif

    /*
** Generate code for scalar subqueries used as an expression
** and IN operators.  Examples:
**
**     (SELECT a FROM b)          -- subquery
**     EXISTS (SELECT a FROM b)   -- EXISTS subquery
**     x IN (4,5,11)              -- IN operator with list on right-hand side
**     x IN (SELECT a FROM b)     -- IN operator with subquery on the right
**
** The pExpr parameter describes the expression that contains the IN
** operator or subquery.
**
** If parameter isRowid is non-zero, then expression pExpr is guaranteed
** to be of the form "<rowid> IN (?, ?, ?)", where <rowid> is a reference
** to some integer key column of a table B-Tree. In this case, use an
** intkey B-Tree to store the set of IN(...) values instead of the usual
** (slower) variable length keys B-Tree.
**
** If rMayHaveNull is non-zero, that means that the operation is an IN
** (not a SELECT or EXISTS) and that the RHS might contains NULLs.
** Furthermore, the IN is in a WHERE clause and that we really want
** to iterate over the RHS of the IN operator in order to quickly locate
** all corresponding LHS elements.  All this routine does is initialize
** the register given by rMayHaveNull to NULL.  Calling routines will take
** care of changing this register value to non-NULL if the RHS is NULL-free.
**
** If rMayHaveNull is zero, that means that the subquery is being used
** for membership testing only.  There is no need to initialize any
** registers to indicate the presense or absence of NULLs on the RHS.
*/
#if !SQLITE_OMIT_SUBQUERY
    static void sqlite3CodeSubselect(
    Parse pParse,          /* Parsing context */
    Expr pExpr,            /* The IN, SELECT, or EXISTS operator */
    int rMayHaveNull,      /* Register that records whether NULLs exist in RHS */
    bool isRowid           /* If true, LHS of IN operator is a rowid */
    )
    {
      int testAddr = 0;                       /* One-time test address */
      Vdbe v = sqlite3GetVdbe( pParse );
      if ( NEVER( v == null ) ) return;
      sqlite3ExprCachePush( pParse );

      /* This code must be run in its entirety every time it is encountered
      ** if any of the following is true:
      **
      **    *  The right-hand side is a correlated subquery
      **    *  The right-hand side is an expression list containing variables
      **    *  We are inside a trigger
      **
      ** If all of the above are false, then we can run this code just once
      ** save the results, and reuse the same result on subsequent invocations.
      */
      if ( !ExprHasAnyProperty( pExpr, EP_VarSelect ) && null == pParse.trigStack )
      {
        int mem = ++pParse.nMem;
        sqlite3VdbeAddOp1( v, OP_If, mem );
        testAddr = sqlite3VdbeAddOp2( v, OP_Integer, 1, mem );
        Debug.Assert( testAddr > 0 /* || pParse.db.mallocFailed != 0 */ );
      }

      switch ( pExpr.op )
      {
        case TK_IN:
          {
            char affinity;
            KeyInfo keyInfo;
            int addr;        /* Address of OP_OpenEphemeral instruction */
            Expr pLeft = pExpr.pLeft;

            if ( rMayHaveNull != 0 )
            {
              sqlite3VdbeAddOp2( v, OP_Null, 0, rMayHaveNull );
            }

            affinity = sqlite3ExprAffinity( pLeft );

            /* Whether this is an 'x IN(SELECT...)' or an 'x IN(<exprlist>)'
            ** expression it is handled the same way. A virtual table is
            ** filled with single-field index keys representing the results
            ** from the SELECT or the <exprlist>.
            **
            ** If the 'x' expression is a column value, or the SELECT...
            ** statement returns a column value, then the affinity of that
            ** column is used to build the index keys. If both 'x' and the
            ** SELECT... statement are columns, then numeric affinity is used
            ** if either column has NUMERIC or INTEGER affinity. If neither
            ** 'x' nor the SELECT... statement are columns, then numeric affinity
            ** is used.
            */
            pExpr.iTable = pParse.nTab++;
            addr = sqlite3VdbeAddOp2( v, OP_OpenEphemeral, (int)pExpr.iTable, !isRowid );
            keyInfo = new KeyInfo();// memset( &keyInfo, 0, sizeof(keyInfo ));
            keyInfo.nField = 1;

            if ( ExprHasProperty( pExpr, EP_xIsSelect ) )
            {
              /* Case 1:     expr IN (SELECT ...)
              **
              ** Generate code to write the results of the select into the temporary
              ** table allocated and opened above.
              */
              SelectDest dest = new SelectDest();
              ExprList pEList;

              Debug.Assert( !isRowid );
              sqlite3SelectDestInit( dest, SRT_Set, pExpr.iTable );
              dest.affinity = (char)affinity;
              Debug.Assert( ( pExpr.iTable & 0x0000FFFF ) == pExpr.iTable );
              if ( sqlite3Select( pParse, pExpr.x.pSelect, ref dest ) != 0 )
              {
                return;
              }
              pEList = pExpr.x.pSelect.pEList;
              if ( ALWAYS( pEList != null ) && pEList.nExpr > 0 )
              {
                keyInfo.aColl[0] = sqlite3BinaryCompareCollSeq( pParse, pExpr.pLeft,
                pEList.a[0].pExpr );
              }
            }
            else if ( pExpr.x.pList != null )
            {
              /* Case 2:     expr IN (exprlist)
              **
              ** For each expression, build an index key from the evaluation and
              ** store it in the temporary table. If <expr> is a column, then use
              ** that columns affinity when building index keys. If <expr> is not
              ** a column, use numeric affinity.
              */
              int i;
              ExprList pList = pExpr.x.pList;
              ExprList_item pItem;
              int r1, r2, r3;

              if ( affinity == '\0' )
              {
                affinity = SQLITE_AFF_NONE;
              }
              keyInfo.aColl[0] = sqlite3ExprCollSeq( pParse, pExpr.pLeft );

              /* Loop through each expression in <exprlist>. */
              r1 = sqlite3GetTempReg( pParse );
              r2 = sqlite3GetTempReg( pParse );
              sqlite3VdbeAddOp2( v, OP_Null, 0, r2 );
              for ( i = 0 ; i < pList.nExpr ; i++ )
              {//, pItem++){
                pItem = pList.a[i];
                Expr pE2 = pItem.pExpr;

                /* If the expression is not constant then we will need to
                ** disable the test that was generated above that makes sure
                ** this code only executes once.  Because for a non-constant
                ** expression we need to rerun this code each time.
                */
                if ( testAddr != 0 && sqlite3ExprIsConstant( pE2 ) == 0 )
                {
                  sqlite3VdbeChangeToNoop( v, testAddr - 1, 2 );
                  testAddr = 0;
                }

                /* Evaluate the expression and insert it into the temp table */
                r3 = sqlite3ExprCodeTarget( pParse, pE2, r1 );
                if ( isRowid )
                {
                  sqlite3VdbeAddOp2( v, OP_MustBeInt, r3, sqlite3VdbeCurrentAddr( v ) + 2 );
                  sqlite3VdbeAddOp3( v, OP_Insert, pExpr.iTable, r2, r3 );
                }
                else
                {
                  sqlite3VdbeAddOp4( v, OP_MakeRecord, r3, 1, r2, affinity, 1 );
                  sqlite3ExprCacheAffinityChange( pParse, r3, 1 );
                  sqlite3VdbeAddOp2( v, OP_IdxInsert, pExpr.iTable, r2 );
                }
              }
              sqlite3ReleaseTempReg( pParse, r1 );
              sqlite3ReleaseTempReg( pParse, r2 );
            }
            if ( !isRowid )
            {
              sqlite3VdbeChangeP4( v, addr, keyInfo, P4_KEYINFO );
            }
            break;
          }

        case TK_EXISTS:
        case TK_SELECT:
        default:
          {
            /* If this has to be a scalar SELECT.  Generate code to put the
            ** value of this select in a memory cell and record the number
            ** of the memory cell in iColumn.  If this is an EXISTS, write
            ** an integer 0 (not exists) or 1 (exists) into a memory cell
            ** and record that memory cell in iColumn.
            */
            Token one = new Token( "1", 1 );    /* Token for literal value 1 */
            Select pSel;                        /* SELECT statement to encode */
            SelectDest dest = new SelectDest(); /* How to deal with SELECt result */

            testcase( pExpr.op == TK_EXISTS );
            testcase( pExpr.op == TK_SELECT );
            Debug.Assert( pExpr.op == TK_EXISTS || pExpr.op == TK_SELECT );

            Debug.Assert( ExprHasProperty( pExpr, EP_xIsSelect ) );
            pSel = pExpr.x.pSelect;
            sqlite3SelectDestInit( dest, 0, ++pParse.nMem );
            if ( pExpr.op == TK_SELECT )
            {
              dest.eDest = SRT_Mem;
              sqlite3VdbeAddOp2( v, OP_Null, 0, dest.iParm );
#if SQLITE_DEBUG
              VdbeComment( v, "Init subquery result" );
#endif
            }
            else
            {
              dest.eDest = SRT_Exists;
              sqlite3VdbeAddOp2( v, OP_Integer, 0, dest.iParm );
#if SQLITE_DEBUG
              VdbeComment( v, "Init EXISTS result" );
#endif
            }
            sqlite3ExprDelete( pParse.db, ref pSel.pLimit );
            pSel.pLimit = sqlite3PExpr( pParse, TK_INTEGER, null, null, one );
            if ( sqlite3Select( pParse, pSel, ref dest ) != 0 )
            {
              return;
            }
            pExpr.iColumn = (short)dest.iParm;
            ExprSetIrreducible( pExpr );
            break;
          }
      }

      if ( testAddr != 0 )
      {
        sqlite3VdbeJumpHere( v, testAddr - 1 );
      }
      sqlite3ExprCachePop( pParse, 1 );

      return;
    }
#endif // * SQLITE_OMIT_SUBQUERY */

    /*
** Duplicate an 8-byte value
*/
    //static char *dup8bytes(Vdbe v, const char *in){
    //  char *out = sqlite3DbMallocRaw(sqlite3VdbeDb(v), 8);
    //  if( out ){
    //    memcpy(out, in, 8);
    //  }
    //  return out;
    //}

    /*
    ** Generate an instruction that will put the floating point
    ** value described by z[0..n-1] into register iMem.
    **
    ** The z[] string will probably not be zero-terminated.  But the
    ** z[n] character is guaranteed to be something that does not look
    ** like the continuation of the number.
    */
    static void codeReal( Vdbe v, string z, bool negateFlag, int iMem )
    {
      if ( ALWAYS( !String.IsNullOrEmpty( z ) ) )
      {
        double value = 0;
        //char *zV;
        sqlite3AtoF( z, ref value );
        if ( sqlite3IsNaN( value ) )
        {
          sqlite3VdbeAddOp2( v, OP_Null, 0, iMem );
        }
        else
        {
          if ( negateFlag ) value = -value;
          //zV = dup8bytes(v,  value);
          sqlite3VdbeAddOp4( v, OP_Real, 0, iMem, 0, value, P4_REAL );
        }
      }
    }

    /*
    ** Generate an instruction that will put the integer describe by
    ** text z[0..n-1] into register iMem.
    **
    ** The z[] string will probably not be zero-terminated.  But the
    ** z[n] character is guaranteed to be something that does not look
    ** like the continuation of the number.
    */
    static void codeInteger( Vdbe v, Expr pExpr, bool negFlag, int iMem )
    {
      if ( ( pExpr.flags & EP_IntValue ) != 0 )
      {
        int i = pExpr.u.iValue;
        if ( negFlag ) i = -i;
        sqlite3VdbeAddOp2( v, OP_Integer, i, iMem );
      }
      else
      {
        string z = pExpr.u.zToken;
        Debug.Assert( !String.IsNullOrEmpty( z ) );
        if ( sqlite3FitsIn64Bits( z, negFlag ) )
        {
          i64 value = 0;
          //string zV;
          sqlite3Atoi64( negFlag ? "-" + z : z, ref value );
          //if ( negFlag ) value = -value;
          //zV = dup8bytes( v, (char*)&value );
          //sqlite3VdbeAddOp4( v, OP_Int64, 0, iMem, 0, zV, P4_INT64 );
          sqlite3VdbeAddOp4( v, OP_Int64, 0, iMem, 0, value, P4_INT64 );
        }
        else
        {
          codeReal( v, z, negFlag, iMem );
        }
      }
    }

    /*
    ** Clear a cache entry.
    */
    static void cacheEntryClear( Parse pParse, yColCache p )
    {
      if ( p.tempReg != 0 )
      {
        if ( pParse.nTempReg < ArraySize( pParse.aTempReg ) )
        {
          pParse.aTempReg[pParse.nTempReg++] = p.iReg;
        }
        p.tempReg = 0;
      }
    }


    /*
    ** Record in the column cache that a particular column from a
    ** particular table is stored in a particular register.
    */
    static void sqlite3ExprCacheStore( Parse pParse, int iTab, int iCol, int iReg )
    {
      int i;
      int minLru;
      int idxLru;
      yColCache p;

      Debug.Assert( iReg > 0 );  /* Register numbers are always positive */
      Debug.Assert( iCol >= -1 && iCol < 32768 );  /* Finite column numbers */

      /* First replace any existing entry */
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )//p=pParse.aColCache... p++)
      {
        p = pParse.aColCache[i];
        if ( p.iReg != 0 && p.iTable == iTab && p.iColumn == iCol )
        {
          cacheEntryClear( pParse, p );
          p.iLevel = pParse.iCacheLevel;
          p.iReg = iReg;
          p.affChange = false;
          p.lru = pParse.iCacheCnt++;
          return;
        }
      }

      /* Find an empty slot and replace it */
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )//p=pParse.aColCache... p++)
      {
        p = pParse.aColCache[i];
        if ( p.iReg == 0 )
        {
          p.iLevel = pParse.iCacheLevel;
          p.iTable = iTab;
          p.iColumn = iCol;
          p.iReg = iReg;
          p.affChange = false;
          p.tempReg = 0;
          p.lru = pParse.iCacheCnt++;
          return;
        }
      }

      /* Replace the last recently used */
      minLru = 0x7fffffff;
      idxLru = -1;
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )//p=pParse.aColCache..., p++)
      {
        p = pParse.aColCache[i];
        if ( p.lru < minLru )
        {
          idxLru = i;
          minLru = p.lru;
        }
      }
      if ( ALWAYS( idxLru >= 0 ) )
      {
        p = pParse.aColCache[idxLru];
        p.iLevel = pParse.iCacheLevel;
        p.iTable = iTab;
        p.iColumn = iCol;
        p.iReg = iReg;
        p.affChange = false;
        p.tempReg = 0;
        p.lru = pParse.iCacheCnt++;
        return;
      }
    }

    /*
    ** Indicate that a register is being overwritten.  Purge the register
    ** from the column cache.
    */
    static void sqlite3ExprCacheRemove( Parse pParse, int iReg )
    {
      int i;
      yColCache p;
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )//p=pParse.aColCache... p++)
      {
        p = pParse.aColCache[i];
        if ( p.iReg == iReg )
        {
          cacheEntryClear( pParse, p );
          p.iReg = 0;
        }
      }
    }

    /*
    ** Remember the current column cache context.  Any new entries added
    ** added to the column cache after this call are removed when the
    ** corresponding pop occurs.
    */
    static void sqlite3ExprCachePush( Parse pParse )
    {
      pParse.iCacheLevel++;
    }

    /*
    ** Remove from the column cache any entries that were added since the
    ** the previous N Push operations.  In other words, restore the cache
    ** to the state it was in N Pushes ago.
    */
    static void sqlite3ExprCachePop( Parse pParse, int N )
    {
      int i;
      yColCache p;
      Debug.Assert( N > 0 );
      Debug.Assert( pParse.iCacheLevel >= N );
      pParse.iCacheLevel -= N;
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )// p++)
      {
        p = pParse.aColCache[i];
        if ( p.iReg != 0 && p.iLevel > pParse.iCacheLevel )
        {
          cacheEntryClear( pParse, p );
          p.iReg = 0;
        }
      }
    }

    /*
    ** When a cached column is reused, make sure that its register is
    ** no longer available as a temp register.  ticket #3879:  that same
    ** register might be in the cache in multiple places, so be sure to
    ** get them all.
    */
    static void sqlite3ExprCachePinRegister( Parse pParse, int iReg )
    {
      int i;
      yColCache p;
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )//p=pParse->aColCache; i<SQLITE_N_COLCACHE; i++, p++)
      {
        p = pParse.aColCache[i];
        if ( p.iReg == iReg )
        {
          p.tempReg = 0;
        }
      }
    }

    /*
    ** Generate code that will extract the iColumn-th column from
    ** table pTab and store the column value in a register.  An effort
    ** is made to store the column value in register iReg, but this is
    ** not guaranteed.  The location of the column value is returned.
    **
    ** There must be an open cursor to pTab in iTable when this routine
    ** is called.  If iColumn<0 then code is generated that extracts the rowid.
    **
    ** This routine might attempt to reuse the value of the column that
    ** has already been loaded into a register.  The value will always
    ** be used if it has not undergone any affinity changes.  But if
    ** an affinity change has occurred, then the cached value will only be
    ** used if allowAffChng is true.
    */
    static int sqlite3ExprCodeGetColumn(
    Parse pParse,     /* Parsing and code generating context */
    Table pTab,       /* Description of the table we are reading from */
    int iColumn,      /* Index of the table column */
    int iTable,       /* The cursor pointing to the table */
    int iReg,         /* Store results here */
    bool allowAffChng /* True if prior affinity changes are OK */
    )
    {
      Vdbe v = pParse.pVdbe;
      int i;
      yColCache p;

      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )
      {// p=pParse.aColCache, p++
        p = pParse.aColCache[i];
        if ( p.iReg > 0 && p.iTable == iTable && p.iColumn == iColumn
        && ( !p.affChange || allowAffChng ) )
        {
          p.lru = pParse.iCacheCnt++;
          sqlite3ExprCachePinRegister( pParse, p.iReg );
          return p.iReg;
        }
      }
      Debug.Assert( v != null );
      if ( iColumn < 0 )
      {
        sqlite3VdbeAddOp2( v, OP_Rowid, iTable, iReg );
      }
      else if ( ALWAYS( pTab != null ) )
      {
        int op = IsVirtual( pTab ) ? OP_VColumn : OP_Column;
        sqlite3VdbeAddOp3( v, op, iTable, iColumn, iReg );
        sqlite3ColumnDefault( v, pTab, iColumn, iReg );
      }
      sqlite3ExprCacheStore( pParse, iTable, iColumn, iReg );
      return iReg;
    }

    /*
    ** Clear all column cache entries.
    */
    static void sqlite3ExprCacheClear( Parse pParse )
    {
      int i;
      yColCache p;

      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )// p=pParse.aColCache... p++)
      {
        p = pParse.aColCache[i];
        if ( p.iReg != 0 )
        {
          cacheEntryClear( pParse, p );
          p.iReg = 0;
        }
      }
    }

    /*
    ** Record the fact that an affinity change has occurred on iCount
    ** registers starting with iStart.
    */
    static void sqlite3ExprCacheAffinityChange( Parse pParse, int iStart, int iCount )
    {
      int iEnd = iStart + iCount - 1;
      int i;
      yColCache p;
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )// p=pParse.aColCache... p++)
      {
        p = pParse.aColCache[i];
        int r = p.iReg;
        if ( r >= iStart && r <= iEnd )
        {
          p.affChange = true;
        }
      }
    }

    /*
    ** Generate code to move content from registers iFrom...iFrom+nReg-1
    ** over to iTo..iTo+nReg-1. Keep the column cache up-to-date.
    */
    static void sqlite3ExprCodeMove( Parse pParse, int iFrom, int iTo, int nReg )
    {
      int i;
      yColCache p;
      if ( NEVER( iFrom == iTo ) ) return;
      sqlite3VdbeAddOp3( pParse.pVdbe, OP_Move, iFrom, iTo, nReg );
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )// p=pParse.aColCache... p++)
      {
        p = pParse.aColCache[i];
        int x = p.iReg;
        if ( x >= iFrom && x < iFrom + nReg )
        {
          p.iReg += iTo - iFrom;
        }
      }
    }

    /*
    ** Generate code to copy content from registers iFrom...iFrom+nReg-1
    ** over to iTo..iTo+nReg-1.
    */
    static void sqlite3ExprCodeCopy( Parse pParse, int iFrom, int iTo, int nReg )
    {
      int i;
      if ( NEVER( iFrom == iTo ) ) return;
      for ( i = 0 ; i < nReg ; i++ )
      {
        sqlite3VdbeAddOp2( pParse.pVdbe, OP_Copy, iFrom + i, iTo + i );
      }
    }

    /*
    ** Return true if any register in the range iFrom..iTo (inclusive)
    ** is used as part of the column cache.
    */
    static int usedAsColumnCache( Parse pParse, int iFrom, int iTo )
    {
      int i;
      yColCache p;
      for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )//p=pParse.aColCache... p++)
      {
        p = pParse.aColCache[i];
        int r = p.iReg;
        if ( r >= iFrom && r <= iTo ) return 1;
      }
      return 0;
    }


    /*
    ** If the last instruction coded is an ephemeral copy of any of
    ** the registers in the nReg registers beginning with iReg, then
    ** convert the last instruction from OP_SCopy to OP_Copy.
    */
    static void sqlite3ExprHardCopy( Parse pParse, int iReg, int nReg )
    {
      VdbeOp pOp;
      Vdbe v;

      //Debug.Assert( pParse.db.mallocFailed == 0 );
      v = pParse.pVdbe;
      Debug.Assert( v != null );
      pOp = sqlite3VdbeGetOp( v, -1 );
      Debug.Assert( pOp != null );
      if ( pOp.opcode == OP_SCopy && pOp.p1 >= iReg && pOp.p1 < iReg + nReg )
      {
        pOp.opcode = OP_Copy;
      }
    }

    /*
    ** Generate code to store the value of the iAlias-th alias in register
    ** target.  The first time this is called, pExpr is evaluated to compute
    ** the value of the alias.  The value is stored in an auxiliary register
    ** and the number of that register is returned.  On subsequent calls,
    ** the register number is returned without generating any code.
    **
    ** Note that in order for this to work, code must be generated in the
    ** same order that it is executed.
    **
    ** Aliases are numbered starting with 1.  So iAlias is in the range
    ** of 1 to pParse.nAlias inclusive.
    **
    ** pParse.aAlias[iAlias-1] records the register number where the value
    ** of the iAlias-th alias is stored.  If zero, that means that the
    ** alias has not yet been computed.
    */
    static int codeAlias( Parse pParse, int iAlias, Expr pExpr, int target )
    {
#if FALSE
sqlite3 db = pParse.db;
int iReg;
if ( pParse.nAliasAlloc < pParse.nAlias )
{
pParse.aAlias = new int[pParse.nAlias]; //sqlite3DbReallocOrFree(db, pParse.aAlias,
//sizeof(pParse.aAlias[0])*pParse.nAlias );
testcase( db.mallocFailed != 0 && pParse.nAliasAlloc > 0 );
if ( db.mallocFailed != 0 ) return 0;
//memset(&pParse.aAlias[pParse.nAliasAlloc], 0,
//       (pParse.nAlias-pParse.nAliasAlloc)*sizeof(pParse.aAlias[0]));
pParse.nAliasAlloc = pParse.nAlias;
}
Debug.Assert( iAlias > 0 && iAlias <= pParse.nAlias );
iReg = pParse.aAlias[iAlias - 1];
if ( iReg == 0 )
{
if ( pParse.iCacheLevel != 0 )
{
iReg = sqlite3ExprCodeTarget( pParse, pExpr, target );
}
else
{
iReg = ++pParse.nMem;
sqlite3ExprCode( pParse, pExpr, iReg );
pParse.aAlias[iAlias - 1] = iReg;
}
}
return iReg;
#else
      UNUSED_PARAMETER( iAlias );
      return sqlite3ExprCodeTarget( pParse, pExpr, target );
#endif
    }

    /*
    ** Generate code into the current Vdbe to evaluate the given
    ** expression.  Attempt to store the results in register "target".
    ** Return the register where results are stored.
    **
    ** With this routine, there is no guarantee  that results will
    ** be stored in target.  The result might be stored in some other
    ** register if it is convenient to do so.  The calling function
    ** must check the return code and move the results to the desired
    ** register.
    */
    static int sqlite3ExprCodeTarget( Parse pParse, Expr pExpr, int target )
    {
      Vdbe v = pParse.pVdbe;    /* The VM under construction */
      int op;                   /* The opcode being coded */
      int inReg = target;       /* Results stored in register inReg */
      int regFree1 = 0;         /* If non-zero free this temporary register */
      int regFree2 = 0;         /* If non-zero free this temporary register */
      int r1 = 0, r2 = 0, r3 = 0, r4 = 0;       /* Various register numbers */
      sqlite3 db = pParse.db; /* The database connection */

      Debug.Assert( target > 0 && target <= pParse.nMem );
      if ( v == null )
      {
        //Debug.Assert( pParse.db.mallocFailed != 0 );
        return 0;
      }

      if ( pExpr == null )
      {
        op = TK_NULL;
      }
      else
      {
        op = pExpr.op;
      }
      switch ( op )
      {
        case TK_AGG_COLUMN:
          {
            AggInfo pAggInfo = pExpr.pAggInfo;
            AggInfo_col pCol = pAggInfo.aCol[pExpr.iAgg];
            if ( pAggInfo.directMode == 0 )
            {
              Debug.Assert( pCol.iMem > 0 );
              inReg = pCol.iMem;
              break;
            }
            else if ( pAggInfo.useSortingIdx != 0 )
            {
              sqlite3VdbeAddOp3( v, OP_Column, pAggInfo.sortingIdx,
              pCol.iSorterColumn, target );
              break;
            }
            /* Otherwise, fall thru into the TK_COLUMN case */
          }
          goto case TK_COLUMN;
        case TK_COLUMN:
          {
            if ( pExpr.iTable < 0 )
            {
              /* This only happens when coding check constraints */
              Debug.Assert( pParse.ckBase > 0 );
              inReg = pExpr.iColumn + pParse.ckBase;
            }
            else
            {
              testcase( ( pExpr.flags & EP_AnyAff ) != 0 );
              inReg = sqlite3ExprCodeGetColumn( pParse, pExpr.pTab,
              pExpr.iColumn, pExpr.iTable, target,
              ( pExpr.flags & EP_AnyAff ) != 0 );
            }
            break;
          }
        case TK_INTEGER:
          {
            codeInteger( v, pExpr, false, target );
            break;
          }
        case TK_FLOAT:
          {
            Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
            codeReal( v, pExpr.u.zToken, false, target );
            break;
          }
        case TK_STRING:
          {
            Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
            sqlite3VdbeAddOp4( v, OP_String8, 0, target, 0, pExpr.u.zToken, 0 );
            break;
          }
        case TK_NULL:
          {
            sqlite3VdbeAddOp2( v, OP_Null, 0, target );
            break;
          }
#if !SQLITE_OMIT_BLOB_LITERAL
        case TK_BLOB:
          {
            int n;
            string z;
            byte[] zBlob;
            Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
            Debug.Assert( pExpr.u.zToken[0] == 'x' || pExpr.u.zToken[0] == 'X' );
            Debug.Assert( pExpr.u.zToken[1] == '\'' );
            z = pExpr.u.zToken.Substring( 2 );
            n = sqlite3Strlen30( z ) - 1;
            Debug.Assert( z[n] == '\'' );
            zBlob = sqlite3HexToBlob( sqlite3VdbeDb( v ), z, n );
            sqlite3VdbeAddOp4( v, OP_Blob, n / 2, target, 0, zBlob, P4_DYNAMIC );
            break;
          }
#endif
        case TK_VARIABLE:
          {
            VdbeOp pOp;
            Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
            Debug.Assert( pExpr.u.zToken != null );
            Debug.Assert( pExpr.u.zToken.Length != 0 );
            if ( pExpr.u.zToken.Length == 1
            && ( pOp = sqlite3VdbeGetOp( v, -1 ) ).opcode == OP_Variable
            && pOp.p1 + pOp.p3 == pExpr.iTable
            && pOp.p2 + pOp.p3 == target
            && pOp.p4.z == null
            )
            {
              /* If the previous instruction was a copy of the previous unnamed
              ** parameter into the previous register, then simply increment the
              ** repeat count on the prior instruction rather than making a new
              ** instruction.
              */
              pOp.p3++;
            }
            else
            {
              sqlite3VdbeAddOp3( v, OP_Variable, pExpr.iTable, target, 1 );
              if ( pExpr.u.zToken.Length > 1 )
              {
                sqlite3VdbeChangeP4( v, -1, pExpr.u.zToken, 0 );
              }
            }
            break;
          }
        case TK_REGISTER:
          {
            inReg = pExpr.iTable;
            break;
          }
        case TK_AS:
          {
            inReg = codeAlias( pParse, pExpr.iTable, pExpr.pLeft, target );
            break;
          }
#if !SQLITE_OMIT_CAST
        case TK_CAST:
          {
            /* Expressions of the form:   CAST(pLeft AS token) */
            int aff, to_op;
            inReg = sqlite3ExprCodeTarget( pParse, pExpr.pLeft, target );
            Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
            aff = sqlite3AffinityType( pExpr.u.zToken );
            to_op = aff - SQLITE_AFF_TEXT + OP_ToText;
            Debug.Assert( to_op == OP_ToText || aff != SQLITE_AFF_TEXT );
            Debug.Assert( to_op == OP_ToBlob || aff != SQLITE_AFF_NONE );
            Debug.Assert( to_op == OP_ToNumeric || aff != SQLITE_AFF_NUMERIC );
            Debug.Assert( to_op == OP_ToInt || aff != SQLITE_AFF_INTEGER );
            Debug.Assert( to_op == OP_ToReal || aff != SQLITE_AFF_REAL );
            testcase( to_op == OP_ToText );
            testcase( to_op == OP_ToBlob );
            testcase( to_op == OP_ToNumeric );
            testcase( to_op == OP_ToInt );
            testcase( to_op == OP_ToReal );
            if ( inReg != target )
            {
              sqlite3VdbeAddOp2( v, OP_SCopy, inReg, target );
              inReg = target;
            }
            sqlite3VdbeAddOp1( v, to_op, inReg );
            testcase( usedAsColumnCache( pParse, inReg, inReg ) != 0 );
            sqlite3ExprCacheAffinityChange( pParse, inReg, 1 );
            break;
          }
#endif // * SQLITE_OMIT_CAST */
        case TK_LT:
        case TK_LE:
        case TK_GT:
        case TK_GE:
        case TK_NE:
        case TK_EQ:
          {
            Debug.Assert( TK_LT == OP_Lt );
            Debug.Assert( TK_LE == OP_Le );
            Debug.Assert( TK_GT == OP_Gt );
            Debug.Assert( TK_GE == OP_Ge );
            Debug.Assert( TK_EQ == OP_Eq );
            Debug.Assert( TK_NE == OP_Ne );
            testcase( op == TK_LT );
            testcase( op == TK_LE );
            testcase( op == TK_GT );
            testcase( op == TK_GE );
            testcase( op == TK_EQ );
            testcase( op == TK_NE );
            codeCompareOperands( pParse, pExpr.pLeft, ref r1, ref regFree1,
            pExpr.pRight, ref r2, ref regFree2 );
            codeCompare( pParse, pExpr.pLeft, pExpr.pRight, op,
            r1, r2, inReg, SQLITE_STOREP2 );
            testcase( regFree1 == 0 );
            testcase( regFree2 == 0 );
            break;
          }
        case TK_AND:
        case TK_OR:
        case TK_PLUS:
        case TK_STAR:
        case TK_MINUS:
        case TK_REM:
        case TK_BITAND:
        case TK_BITOR:
        case TK_SLASH:
        case TK_LSHIFT:
        case TK_RSHIFT:
        case TK_CONCAT:
          {
            Debug.Assert( TK_AND == OP_And );
            Debug.Assert( TK_OR == OP_Or );
            Debug.Assert( TK_PLUS == OP_Add );
            Debug.Assert( TK_MINUS == OP_Subtract );
            Debug.Assert( TK_REM == OP_Remainder );
            Debug.Assert( TK_BITAND == OP_BitAnd );
            Debug.Assert( TK_BITOR == OP_BitOr );
            Debug.Assert( TK_SLASH == OP_Divide );
            Debug.Assert( TK_LSHIFT == OP_ShiftLeft );
            Debug.Assert( TK_RSHIFT == OP_ShiftRight );
            Debug.Assert( TK_CONCAT == OP_Concat );
            testcase( op == TK_AND );
            testcase( op == TK_OR );
            testcase( op == TK_PLUS );
            testcase( op == TK_MINUS );
            testcase( op == TK_REM );
            testcase( op == TK_BITAND );
            testcase( op == TK_BITOR );
            testcase( op == TK_SLASH );
            testcase( op == TK_LSHIFT );
            testcase( op == TK_RSHIFT );
            testcase( op == TK_CONCAT );
            r1 = sqlite3ExprCodeTemp( pParse, pExpr.pLeft, ref regFree1 );
            r2 = sqlite3ExprCodeTemp( pParse, pExpr.pRight, ref regFree2 );
            sqlite3VdbeAddOp3( v, op, r2, r1, target );
            testcase( regFree1 == 0 );
            testcase( regFree2 == 0 );
            break;
          }
        case TK_UMINUS:
          {
            Expr pLeft = pExpr.pLeft;
            Debug.Assert( pLeft != null );
            if ( pLeft.op == TK_FLOAT )
            {
              Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
              codeReal( v, pLeft.u.zToken, true, target );
            }
            else if ( pLeft.op == TK_INTEGER )
            {
              codeInteger( v, pLeft, true, target );
            }
            else
            {
              regFree1 = r1 = sqlite3GetTempReg( pParse );
              sqlite3VdbeAddOp2( v, OP_Integer, 0, r1 );
              r2 = sqlite3ExprCodeTemp( pParse, pExpr.pLeft, ref regFree2 );
              sqlite3VdbeAddOp3( v, OP_Subtract, r2, r1, target );
              testcase( regFree2 == 0 );
            }
            inReg = target;
            break;
          }
        case TK_BITNOT:
        case TK_NOT:
          {
            Debug.Assert( TK_BITNOT == OP_BitNot );
            Debug.Assert( TK_NOT == OP_Not );
            testcase( op == TK_BITNOT );
            testcase( op == TK_NOT );
            r1 = sqlite3ExprCodeTemp( pParse, pExpr.pLeft, ref regFree1 );
            testcase( regFree1 == 0 );
            inReg = target;
            sqlite3VdbeAddOp2( v, op, r1, inReg );
            break;
          }
        case TK_ISNULL:
        case TK_NOTNULL:
          {
            int addr;
            Debug.Assert( TK_ISNULL == OP_IsNull );
            Debug.Assert( TK_NOTNULL == OP_NotNull );
            testcase( op == TK_ISNULL );
            testcase( op == TK_NOTNULL );
            sqlite3VdbeAddOp2( v, OP_Integer, 1, target );
            r1 = sqlite3ExprCodeTemp( pParse, pExpr.pLeft, ref regFree1 );
            testcase( regFree1 == 0 );
            addr = sqlite3VdbeAddOp1( v, op, r1 );
            sqlite3VdbeAddOp2( v, OP_AddImm, target, -1 );
            sqlite3VdbeJumpHere( v, addr );
            break;
          }
        case TK_AGG_FUNCTION:
          {
            AggInfo pInfo = pExpr.pAggInfo;
            if ( pInfo == null )
            {
              Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
              sqlite3ErrorMsg( pParse, "misuse of aggregate: %s()", pExpr.u.zToken );
            }
            else
            {
              inReg = pInfo.aFunc[pExpr.iAgg].iMem;
            }
            break;
          }
        case TK_CONST_FUNC:
        case TK_FUNCTION:
          {
            ExprList pFarg;        /* List of function arguments */
            int nFarg;             /* Number of function arguments */
            FuncDef pDef;          /* The function definition object */
            int nId;               /* Length of the function name in bytes */
            string zId;            /* The function name */
            int constMask = 0;     /* Mask of function arguments that are constant */
            int i;                 /* Loop counter */
            u8 enc = ENC( db );    /* The text encoding used by this database */
            CollSeq pColl = null;  /* A collating sequence */

            Debug.Assert( !ExprHasProperty( pExpr, EP_xIsSelect ) );
            testcase( op == TK_CONST_FUNC );
            testcase( op == TK_FUNCTION );
            if ( ExprHasAnyProperty( pExpr, EP_TokenOnly ) )
            {
              pFarg = null;
            }
            else
            {
              pFarg = pExpr.x.pList;
            }
            nFarg = pFarg != null ? pFarg.nExpr : 0;
            Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
            zId = pExpr.u.zToken;
            nId = sqlite3Strlen30( zId );
            pDef = sqlite3FindFunction( pParse.db, zId, nId, nFarg, enc, 0 );
            Debug.Assert( pDef != null );
            if ( pFarg != null )
            {
              r1 = sqlite3GetTempRange( pParse, nFarg );
              sqlite3ExprCodeExprList( pParse, pFarg, r1, true );
            }
            else
            {
              r1 = 0;
            }
#if !SQLITE_OMIT_VIRTUALTABLE
/* Possibly overload the function if the first argument is
** a virtual table column.
**
** For infix functions (LIKE, GLOB, REGEXP, and MATCH) use the
** second argument, not the first, as the argument to test to
** see if it is a column in a virtual table.  This is done because
** the left operand of infix functions (the operand we want to
** control overloading) ends up as the second argument to the
** function.  The expression "A glob B" is equivalent to
** "glob(B,A).  We want to use the A in "A glob B" to test
** for function overloading.  But we use the B term in "glob(B,A)".
*/
if ( nFarg >= 2 && ( pExpr.flags & EP_InfixFunc ) )
{
pDef = sqlite3VtabOverloadFunction( db, pDef, nFarg, pFarg.a[1].pExpr );
}
else if ( nFarg > 0 )
{
pDef = sqlite3VtabOverloadFunction( db, pDef, nFarg, pFarg.a[0].pExpr );
}
#endif
            for ( i = 0 ; i < nFarg ; i++ )
            {
              if ( i < 32 && sqlite3ExprIsConstant( pFarg.a[i].pExpr ) != 0 )
              {
                constMask |= ( 1 << i );
              }
              if ( ( pDef.flags & SQLITE_FUNC_NEEDCOLL ) != 0 && null == pColl )
              {
                pColl = sqlite3ExprCollSeq( pParse, pFarg.a[i].pExpr );
              }
            }
            if ( ( pDef.flags & SQLITE_FUNC_NEEDCOLL ) != 0 )
            {
              if ( null == pColl ) pColl = db.pDfltColl;
              sqlite3VdbeAddOp4( v, OP_CollSeq, 0, 0, 0, pColl, P4_COLLSEQ );
            }
            sqlite3VdbeAddOp4( v, OP_Function, constMask, r1, target,
            pDef, P4_FUNCDEF );
            sqlite3VdbeChangeP5( v, (u8)nFarg );
            if ( nFarg != 0 )
            {
              sqlite3ReleaseTempRange( pParse, r1, nFarg );
            }
            sqlite3ExprCacheAffinityChange( pParse, r1, nFarg );
            break;
          }
#if !SQLITE_OMIT_SUBQUERY
        case TK_EXISTS:
        case TK_SELECT:
          {
            testcase( op == TK_EXISTS );
            testcase( op == TK_SELECT );
            sqlite3CodeSubselect( pParse, pExpr, 0, false );
            inReg = pExpr.iColumn;
            break;
          }
        case TK_IN:
          {
            int rNotFound = 0;
            int rMayHaveNull = 0;
            int j2, j3, j4, j5;
            char affinity;
            int eType;

            VdbeNoopComment( v, "begin IN expr r%d", target );
            eType = sqlite3FindInIndex( pParse, pExpr, ref rMayHaveNull );
            if ( rMayHaveNull != 0 )
            {
              rNotFound = ++pParse.nMem;
            }

            /* Figure out the affinity to use to create a key from the results
            ** of the expression. affinityStr stores a static string suitable for
            ** P4 of OP_MakeRecord.
            */
            affinity = comparisonAffinity( pExpr );

            /* Code the <expr> from "<expr> IN (...)". The temporary table
            ** pExpr.iTable contains the values that make up the (...) set.
            */
            sqlite3ExprCachePush( pParse );
            sqlite3ExprCode( pParse, pExpr.pLeft, target );
            j2 = sqlite3VdbeAddOp1( v, OP_IsNull, target );
            if ( eType == IN_INDEX_ROWID )
            {
              j3 = sqlite3VdbeAddOp1( v, OP_MustBeInt, target );
              j4 = sqlite3VdbeAddOp3( v, OP_NotExists, pExpr.iTable, 0, target );
              sqlite3VdbeAddOp2( v, OP_Integer, 1, target );
              j5 = sqlite3VdbeAddOp0( v, OP_Goto );
              sqlite3VdbeJumpHere( v, j3 );
              sqlite3VdbeJumpHere( v, j4 );
              sqlite3VdbeAddOp2( v, OP_Integer, 0, target );
            }
            else
            {
              r2 = regFree2 = sqlite3GetTempReg( pParse );

              /* Create a record and test for set membership. If the set contains
              ** the value, then jump to the end of the test code. The target
              ** register still contains the true (1) value written to it earlier.
              */
              sqlite3VdbeAddOp4( v, OP_MakeRecord, target, 1, r2, affinity, 1 );
              sqlite3VdbeAddOp2( v, OP_Integer, 1, target );
              j5 = sqlite3VdbeAddOp3( v, OP_Found, pExpr.iTable, 0, r2 );

              /* If the set membership test fails, then the result of the
              ** "x IN (...)" expression must be either 0 or NULL. If the set
              ** contains no NULL values, then the result is 0. If the set
              ** contains one or more NULL values, then the result of the
              ** expression is also NULL.
              */
              if ( rNotFound == 0 )
              {
                /* This branch runs if it is known at compile time (now) that
                ** the set contains no NULL values. This happens as the result
                ** of a "NOT NULL" constraint in the database schema. No need
                ** to test the data structure at runtime in this case.
                */
                sqlite3VdbeAddOp2( v, OP_Integer, 0, target );
              }
              else
              {
                /* This block populates the rNotFound register with either NULL
                ** or 0 (an integer value). If the data structure contains one
                ** or more NULLs, then set rNotFound to NULL. Otherwise, set it
                ** to 0. If register rMayHaveNull is already set to some value
                ** other than NULL, then the test has already been run and
                ** rNotFound is already populated.
                */
                byte[] nullRecord = { 0x02, 0x00 };
                j3 = sqlite3VdbeAddOp1( v, OP_NotNull, rMayHaveNull );
                sqlite3VdbeAddOp2( v, OP_Null, 0, rNotFound );
                sqlite3VdbeAddOp4( v, OP_Blob, 2, rMayHaveNull, 0,
                nullRecord, P4_STATIC );
                j4 = sqlite3VdbeAddOp3( v, OP_Found, pExpr.iTable, 0, rMayHaveNull );
                sqlite3VdbeAddOp2( v, OP_Integer, 0, rNotFound );
                sqlite3VdbeJumpHere( v, j4 );
                sqlite3VdbeJumpHere( v, j3 );

                /* Copy the value of register rNotFound (which is either NULL or 0)
                ** into the target register. This will be the result of the
                ** expression.
                */
                sqlite3VdbeAddOp2( v, OP_Copy, rNotFound, target );
              }
            }
            sqlite3VdbeJumpHere( v, j2 );
            sqlite3VdbeJumpHere( v, j5 );
            sqlite3ExprCachePop( pParse, 1 );
            VdbeComment( v, "end IN expr r%d", target );
            break;
          }
#endif
        /*
**    x BETWEEN y AND z
**
** This is equivalent to
**
**    x>=y AND x<=z
**
** X is stored in pExpr.pLeft.
** Y is stored in pExpr.x.pList.a[0].pExpr.
** Z is stored in pExpr.x.pList.a[1].pExpr.
*/
        case TK_BETWEEN:
          {
            Expr pLeft = pExpr.pLeft;
            ExprList_item pLItem = pExpr.x.pList.a[0];
            Expr pRight = pLItem.pExpr;
            codeCompareOperands( pParse, pLeft, ref r1, ref regFree1,
            pRight, ref r2, ref regFree2 );

            testcase( regFree1 == 0 );
            testcase( regFree2 == 0 );
            r3 = sqlite3GetTempReg( pParse );
            r4 = sqlite3GetTempReg( pParse );
            codeCompare( pParse, pLeft, pRight, OP_Ge,
            r1, r2, r3, SQLITE_STOREP2 );
            pLItem = pExpr.x.pList.a[1];// pLItem++;
            pRight = pLItem.pExpr;
            sqlite3ReleaseTempReg( pParse, regFree2 );
            r2 = sqlite3ExprCodeTemp( pParse, pRight, ref regFree2 );
            testcase( regFree2 == 0 );
            codeCompare( pParse, pLeft, pRight, OP_Le, r1, r2, r4, SQLITE_STOREP2 );
            sqlite3VdbeAddOp3( v, OP_And, r3, r4, target );
            sqlite3ReleaseTempReg( pParse, r3 );
            sqlite3ReleaseTempReg( pParse, r4 );
            break;
          }
        case TK_UPLUS:
          {
            inReg = sqlite3ExprCodeTarget( pParse, pExpr.pLeft, target );
            break;
          }

        /*
        ** Form A:
        **   CASE x WHEN e1 THEN r1 WHEN e2 THEN r2 ... WHEN eN THEN rN ELSE y END
        **
        ** Form B:
        **   CASE WHEN e1 THEN r1 WHEN e2 THEN r2 ... WHEN eN THEN rN ELSE y END
        **
        ** Form A is can be transformed into the equivalent form B as follows:
        **   CASE WHEN x=e1 THEN r1 WHEN x=e2 THEN r2 ...
        **        WHEN x=eN THEN rN ELSE y END
        **
        ** X (if it exists) is in pExpr.pLeft.
        ** Y is in pExpr.pRight.  The Y is also optional.  If there is no
        ** ELSE clause and no other term matches, then the result of the
        ** exprssion is NULL.
        ** Ei is in pExpr.x.pList.a[i*2] and Ri is pExpr.x.pList.a[i*2+1].
        **
        ** The result of the expression is the Ri for the first matching Ei,
        ** or if there is no matching Ei, the ELSE term Y, or if there is
        ** no ELSE term, NULL.
        */
        default: Debug.Assert( op == TK_CASE );
          {
            int endLabel;                     /* GOTO label for end of CASE stmt */
            int nextCase;                     /* GOTO label for next WHEN clause */
            int nExpr;                        /* 2x number of WHEN terms */
            int i;                            /* Loop counter */
            ExprList pEList;                  /* List of WHEN terms */
            ExprList_item[] aListelem;        /* Array of WHEN terms */
            Expr opCompare = new Expr();      /* The X==Ei expression */
            Expr cacheX;                      /* Cached expression X */
            Expr pX;                          /* The X expression */
            Expr pTest = null;                /* X==Ei (form A) or just Ei (form B) */
#if !NDEBUG
            int iCacheLevel = pParse.iCacheLevel;
            //VVA_ONLY( int iCacheLevel = pParse.iCacheLevel; )
#endif
            Debug.Assert( !ExprHasProperty( pExpr, EP_xIsSelect ) && pExpr.x.pList != null );
            Debug.Assert( ( pExpr.x.pList.nExpr % 2 ) == 0 );
            Debug.Assert( pExpr.x.pList.nExpr > 0 );
            pEList = pExpr.x.pList;
            aListelem = pEList.a;
            nExpr = pEList.nExpr;
            endLabel = sqlite3VdbeMakeLabel( v );
            if ( ( pX = pExpr.pLeft ) != null )
            {
              cacheX = pX;
              testcase( pX.op == TK_COLUMN );
              testcase( pX.op == TK_REGISTER );
              cacheX.iTable = sqlite3ExprCodeTemp( pParse, pX, ref regFree1 );
              testcase( regFree1 == 0 );
              cacheX.op = TK_REGISTER;
              opCompare.op = TK_EQ;
              opCompare.pLeft = cacheX;
              pTest = opCompare;
            }
            for ( i = 0 ; i < nExpr ; i = i + 2 )
            {
              sqlite3ExprCachePush( pParse );
              if ( pX != null )
              {
                Debug.Assert( pTest != null );
                opCompare.pRight = aListelem[i].pExpr;
              }
              else
              {
                pTest = aListelem[i].pExpr;
              }
              nextCase = sqlite3VdbeMakeLabel( v );
              testcase( pTest.op == TK_COLUMN );
              sqlite3ExprIfFalse( pParse, pTest, nextCase, SQLITE_JUMPIFNULL );
              testcase( aListelem[i + 1].pExpr.op == TK_COLUMN );
              testcase( aListelem[i + 1].pExpr.op == TK_REGISTER );
              sqlite3ExprCode( pParse, aListelem[i + 1].pExpr, target );
              sqlite3VdbeAddOp2( v, OP_Goto, 0, endLabel );
              sqlite3ExprCachePop( pParse, 1 );
              sqlite3VdbeResolveLabel( v, nextCase );
            }
            if ( pExpr.pRight != null )
            {
              sqlite3ExprCachePush( pParse );
              sqlite3ExprCode( pParse, pExpr.pRight, target );
              sqlite3ExprCachePop( pParse, 1 );
            }
            else
            {
              sqlite3VdbeAddOp2( v, OP_Null, 0, target );
            }
#if !NDEBUG
            Debug.Assert( /* db.mallocFailed != 0 || */ pParse.nErr > 0
            || pParse.iCacheLevel == iCacheLevel );
#endif
            sqlite3VdbeResolveLabel( v, endLabel );
            break;
          }
#if !SQLITE_OMIT_TRIGGER
        case TK_RAISE:
          {
            if ( pParse.trigStack == null )
            {
              sqlite3ErrorMsg( pParse,
              "RAISE() may only be used within a trigger-program" );
              return 0;
            }
            if ( pExpr.affinity != OE_Ignore )
            {
              Debug.Assert( pExpr.affinity == OE_Rollback ||
              pExpr.affinity == OE_Abort ||
              pExpr.affinity == OE_Fail );
              Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
              sqlite3VdbeAddOp4( v, OP_Halt, SQLITE_CONSTRAINT, pExpr.affinity, 0,
              Encoding.UTF8.GetBytes( pExpr.u.zToken ), 0 );
            }
            else
            {
              Debug.Assert( pExpr.affinity == OE_Ignore );
              sqlite3VdbeAddOp2( v, OP_ContextPop, 0, 0 );
              sqlite3VdbeAddOp2( v, OP_Goto, 0, pParse.trigStack.ignoreJump );
#if SQLITE_DEBUG
              VdbeComment( v, "raise(IGNORE)" );
#endif
            }
            break;
          }
#endif
      }
      sqlite3ReleaseTempReg( pParse, regFree1 );
      sqlite3ReleaseTempReg( pParse, regFree2 );
      return inReg;
    }

    /*
    ** Generate code to evaluate an expression and store the results
    ** into a register.  Return the register number where the results
    ** are stored.
    **
    ** If the register is a temporary register that can be deallocated,
    ** then write its number into pReg.  If the result register is not
    ** a temporary, then set pReg to zero.
    */
    static int sqlite3ExprCodeTemp( Parse pParse, Expr pExpr, ref int pReg )
    {
      int r1 = sqlite3GetTempReg( pParse );
      int r2 = sqlite3ExprCodeTarget( pParse, pExpr, r1 );
      if ( r2 == r1 )
      {
        pReg = r1;
      }
      else
      {
        sqlite3ReleaseTempReg( pParse, r1 );
        pReg = 0;
      }
      return r2;
    }

    /*
    ** Generate code that will evaluate expression pExpr and store the
    ** results in register target.  The results are guaranteed to appear
    ** in register target.
    */
    static int sqlite3ExprCode( Parse pParse, Expr pExpr, int target )
    {
      int inReg;

      Debug.Assert( target > 0 && target <= pParse.nMem );
      inReg = sqlite3ExprCodeTarget( pParse, pExpr, target );
      Debug.Assert( pParse.pVdbe != null /* || pParse.db.mallocFailed != 0 */ );
      if ( inReg != target && pParse.pVdbe != null )
      {
        sqlite3VdbeAddOp2( pParse.pVdbe, OP_SCopy, inReg, target );
      }
      return target;
    }

    /*
    ** Generate code that evalutes the given expression and puts the result
    ** in register target.
    **
    ** Also make a copy of the expression results into another "cache" register
    ** and modify the expression so that the next time it is evaluated,
    ** the result is a copy of the cache register.
    **
    ** This routine is used for expressions that are used multiple
    ** times.  They are evaluated once and the results of the expression
    ** are reused.
    */
    static int sqlite3ExprCodeAndCache( Parse pParse, Expr pExpr, int target )
    {
      Vdbe v = pParse.pVdbe;
      int inReg;
      inReg = sqlite3ExprCode( pParse, pExpr, target );
      Debug.Assert( target > 0 );
      /* This routine is called for terms to INSERT or UPDATE.  And the only
      ** other place where expressions can be converted into TK_REGISTER is
      ** in WHERE clause processing.  So as currently implemented, there is
      ** no way for a TK_REGISTER to exist here.  But it seems prudent to
      ** keep the ALWAYS() in case the conditions above change with future
      ** modifications or enhancements. */
      if ( ALWAYS( pExpr.op != TK_REGISTER ) )
      {
        int iMem;
        iMem = ++pParse.nMem;
        sqlite3VdbeAddOp2( v, OP_Copy, inReg, iMem );
        pExpr.iTable = iMem;
        pExpr.op = TK_REGISTER;
      }
      return inReg;
    }

    /*
    ** Return TRUE if pExpr is an constant expression that is appropriate
    ** for factoring out of a loop.  Appropriate expressions are:
    **
    **    *  Any expression that evaluates to two or more opcodes.
    **
    **    *  Any OP_Integer, OP_Real, OP_String, OP_Blob, OP_Null,
    **       or OP_Variable that does not need to be placed in a
    **       specific register.
    **
    ** There is no point in factoring out single-instruction constant
    ** expressions that need to be placed in a particular register.
    ** We could factor them out, but then we would end up adding an
    ** OP_SCopy instruction to move the value into the correct register
    ** later.  We might as well just use the original instruction and
    ** avoid the OP_SCopy.
    */
    static int isAppropriateForFactoring( Expr p )
    {
      if ( sqlite3ExprIsConstantNotJoin( p ) == 0 )
      {
        return 0;  /* Only constant expressions are appropriate for factoring */
      }
      if ( ( p.flags & EP_FixedDest ) == 0 )
      {
        return 1;  /* Any constant without a fixed destination is appropriate */
      }
      while ( p.op == TK_UPLUS ) p = p.pLeft;
      switch ( p.op )
      {
#if !SQLITE_OMIT_BLOB_LITERAL
        case TK_BLOB:
#endif
        case TK_VARIABLE:
        case TK_INTEGER:
        case TK_FLOAT:
        case TK_NULL:
        case TK_STRING:
          {
            testcase( p.op == TK_BLOB );
            testcase( p.op == TK_VARIABLE );
            testcase( p.op == TK_INTEGER );
            testcase( p.op == TK_FLOAT );
            testcase( p.op == TK_NULL );
            testcase( p.op == TK_STRING );
            /* Single-instruction constants with a fixed destination are
            ** better done in-line.  If we factor them, they will just end
            ** up generating an OP_SCopy to move the value to the destination
            ** register. */
            return 0;
          }
        case TK_UMINUS:
          {
            if ( p.pLeft.op == TK_FLOAT || p.pLeft.op == TK_INTEGER )
            {
              return 0;
            }
            break;
          }
        default:
          {
            break;
          }
      }
      return 1;
    }

    /*
    ** If pExpr is a constant expression that is appropriate for
    ** factoring out of a loop, then evaluate the expression
    ** into a register and convert the expression into a TK_REGISTER
    ** expression.
    */
    static int evalConstExpr( Walker pWalker, ref Expr pExpr )
    {
      Parse pParse = pWalker.pParse;
      switch ( pExpr.op )
      {
        case TK_REGISTER:
          {
            return WRC_Prune;
          }
        case TK_FUNCTION:
        case TK_AGG_FUNCTION:
        case TK_CONST_FUNC:
          {
            /* The arguments to a function have a fixed destination.
            ** Mark them this way to avoid generated unneeded OP_SCopy
            ** instructions.
            */
            ExprList pList = pExpr.x.pList;
            Debug.Assert( !ExprHasProperty( pExpr, EP_xIsSelect ) );
            if ( pList != null )
            {
              int i = pList.nExpr;
              ExprList_item pItem;//= pList.a;
              for ( ; i > 0 ; i-- )
              {//, pItem++){
                pItem = pList.a[pList.nExpr - i];
                if ( ALWAYS( pItem.pExpr != null ) ) pItem.pExpr.flags |= EP_FixedDest;
              }
            }
            break;
          }
      }
      if ( isAppropriateForFactoring( pExpr ) != 0 )
      {
        int r1 = ++pParse.nMem;
        int r2;
        r2 = sqlite3ExprCodeTarget( pParse, pExpr, r1 );
        if ( NEVER( r1 != r2 ) ) sqlite3ReleaseTempReg( pParse, r1 );
        pExpr.op = TK_REGISTER;
        pExpr.iTable = r2;
        return WRC_Prune;
      }
      return WRC_Continue;
    }

    /*
    ** Preevaluate constant subexpressions within pExpr and store the
    ** results in registers.  Modify pExpr so that the constant subexpresions
    ** are TK_REGISTER opcodes that refer to the precomputed values.
    */
    static void sqlite3ExprCodeConstants( Parse pParse, Expr pExpr )
    {
      Walker w = new Walker();
      w.xExprCallback = (dxExprCallback)evalConstExpr;
      w.xSelectCallback = null;
      w.pParse = pParse;
      sqlite3WalkExpr( w, ref pExpr );
    }

    /*
    ** Generate code that pushes the value of every element of the given
    ** expression list into a sequence of registers beginning at target.
    **
    ** Return the number of elements evaluated.
    */
    static int sqlite3ExprCodeExprList(
    Parse pParse,     /* Parsing context */
    ExprList pList,   /* The expression list to be coded */
    int target,       /* Where to write results */
    bool doHardCopy   /* Make a hard copy of every element */
    )
    {
      ExprList_item pItem;
      int i, n;
      Debug.Assert( pList != null );
      Debug.Assert( target > 0 );
      n = pList.nExpr;
      for ( i = 0 ; i < n ; i++ )// pItem++)
      {
        pItem = pList.a[i];
        if ( pItem.iAlias != 0 )
        {
          int iReg = codeAlias( pParse, pItem.iAlias, pItem.pExpr, target + i );
          Vdbe v = sqlite3GetVdbe( pParse );
          if ( iReg != target + i )
          {
            sqlite3VdbeAddOp2( v, OP_SCopy, iReg, target + i );
          }
        }
        else
        {
          sqlite3ExprCode( pParse, pItem.pExpr, target + i );
        }
        if ( doHardCopy /* && 0 == pParse.db.mallocFailed */ )
        {
          sqlite3ExprHardCopy( pParse, target, n );
        }
      }
      return n;
    }

    /*
    ** Generate code for a boolean expression such that a jump is made
    ** to the label "dest" if the expression is true but execution
    ** continues straight thru if the expression is false.
    **
    ** If the expression evaluates to NULL (neither true nor false), then
    ** take the jump if the jumpIfNull flag is SQLITE_JUMPIFNULL.
    **
    ** This code depends on the fact that certain token values (ex: TK_EQ)
    ** are the same as opcode values (ex: OP_Eq) that implement the corresponding
    ** operation.  Special comments in vdbe.c and the mkopcodeh.awk script in
    ** the make process cause these values to align.  Assert()s in the code
    ** below verify that the numbers are aligned correctly.
    */
    static void sqlite3ExprIfTrue( Parse pParse, Expr pExpr, int dest, int jumpIfNull )
    {
      Vdbe v = pParse.pVdbe;
      int op = 0;
      int regFree1 = 0;
      int regFree2 = 0;
      int r1 = 0, r2 = 0;

      Debug.Assert( jumpIfNull == SQLITE_JUMPIFNULL || jumpIfNull == 0 );
      if ( NEVER( v == null ) ) return;  /* Existance of VDBE checked by caller */
      if ( NEVER( pExpr == null ) ) return;  /* No way this can happen */
      op = pExpr.op;
      switch ( op )
      {
        case TK_AND:
          {
            int d2 = sqlite3VdbeMakeLabel( v );
            testcase( jumpIfNull == 0 );
            sqlite3ExprCachePush( pParse );
            sqlite3ExprIfFalse( pParse, pExpr.pLeft, d2, jumpIfNull ^ SQLITE_JUMPIFNULL );
            sqlite3ExprIfTrue( pParse, pExpr.pRight, dest, jumpIfNull );
            sqlite3VdbeResolveLabel( v, d2 );
            sqlite3ExprCachePop( pParse, 1 );
            break;
          }
        case TK_OR:
          {
            testcase( jumpIfNull == 0 );
            sqlite3ExprIfTrue( pParse, pExpr.pLeft, dest, jumpIfNull );
            sqlite3ExprIfTrue( pParse, pExpr.pRight, dest, jumpIfNull );
            break;
          }
        case TK_NOT:
          {
            testcase( jumpIfNull == 0 );
            sqlite3ExprIfFalse( pParse, pExpr.pLeft, dest, jumpIfNull );
            break;
          }
        case TK_LT:
        case TK_LE:
        case TK_GT:
        case TK_GE:
        case TK_NE:
        case TK_EQ:
          {
            Debug.Assert( TK_LT == OP_Lt );
            Debug.Assert( TK_LE == OP_Le );
            Debug.Assert( TK_GT == OP_Gt );
            Debug.Assert( TK_GE == OP_Ge );
            Debug.Assert( TK_EQ == OP_Eq );
            Debug.Assert( TK_NE == OP_Ne );
            testcase( op == TK_LT );
            testcase( op == TK_LE );
            testcase( op == TK_GT );
            testcase( op == TK_GE );
            testcase( op == TK_EQ );
            testcase( op == TK_NE );
            testcase( jumpIfNull == 0 );
            codeCompareOperands( pParse, pExpr.pLeft, ref r1, ref regFree1,
            pExpr.pRight, ref r2, ref regFree2 );
            codeCompare( pParse, pExpr.pLeft, pExpr.pRight, op,
            r1, r2, dest, jumpIfNull );
            testcase( regFree1 == 0 );
            testcase( regFree2 == 0 );
            break;
          }
        case TK_ISNULL:
        case TK_NOTNULL:
          {
            Debug.Assert( TK_ISNULL == OP_IsNull );
            Debug.Assert( TK_NOTNULL == OP_NotNull );
            testcase( op == TK_ISNULL );
            testcase( op == TK_NOTNULL );
            r1 = sqlite3ExprCodeTemp( pParse, pExpr.pLeft, ref regFree1 );
            sqlite3VdbeAddOp2( v, op, r1, dest );
            testcase( regFree1 == 0 );
            break;
          }
        case TK_BETWEEN:
          {
            /*    x BETWEEN y AND z
            **
            ** Is equivalent to
            **
            **    x>=y AND x<=z
            **
            ** Code it as such, taking care to do the common subexpression
            ** elementation of x.
            */
            Expr exprAnd = new Expr();
            Expr compLeft = new Expr();
            Expr compRight = new Expr();
            Expr exprX = new Expr();

            Debug.Assert( !ExprHasProperty( pExpr, EP_xIsSelect ) );
            exprX = pExpr.pLeft.Copy();
            exprAnd.op = TK_AND;
            exprAnd.pLeft = compLeft;
            exprAnd.pRight = compRight;
            compLeft.op = TK_GE;
            compLeft.pLeft = exprX;
            compLeft.pRight = pExpr.x.pList.a[0].pExpr;
            compRight.op = TK_LE;
            compRight.pLeft = exprX;
            compRight.pRight = pExpr.x.pList.a[1].pExpr;
            exprX.iTable = sqlite3ExprCodeTemp( pParse, exprX, ref regFree1 );
            testcase( regFree1 == 0 );
            exprX.op = TK_REGISTER;
            testcase( jumpIfNull == 0 );
            sqlite3ExprIfTrue( pParse, exprAnd, dest, jumpIfNull );
            break;
          }
        default:
          {
            r1 = sqlite3ExprCodeTemp( pParse, pExpr, ref regFree1 );
            sqlite3VdbeAddOp3( v, OP_If, r1, dest, jumpIfNull != 0 ? 1 : 0 );
            testcase( regFree1 == 0 );
            testcase( jumpIfNull == 0 );
            break;
          }
      }
      sqlite3ReleaseTempReg( pParse, regFree1 );
      sqlite3ReleaseTempReg( pParse, regFree2 );
    }

    /*
    ** Generate code for a boolean expression such that a jump is made
    ** to the label "dest" if the expression is false but execution
    ** continues straight thru if the expression is true.
    **
    ** If the expression evaluates to NULL (neither true nor false) then
    ** jump if jumpIfNull is SQLITE_JUMPIFNULL or fall through if jumpIfNull
    ** is 0.
    */
    static void sqlite3ExprIfFalse( Parse pParse, Expr pExpr, int dest, int jumpIfNull )
    {
      Vdbe v = pParse.pVdbe;
      int op = 0;
      int regFree1 = 0;
      int regFree2 = 0;
      int r1 = 0, r2 = 0;

      Debug.Assert( jumpIfNull == SQLITE_JUMPIFNULL || jumpIfNull == 0 );
      if ( NEVER( v == null ) ) return; /* Existance of VDBE checked by caller */
      if ( pExpr == null ) return;

      /* The value of pExpr.op and op are related as follows:
      **
      **       pExpr.op            op
      **       ---------          ----------
      **       TK_ISNULL          OP_NotNull
      **       TK_NOTNULL         OP_IsNull
      **       TK_NE              OP_Eq
      **       TK_EQ              OP_Ne
      **       TK_GT              OP_Le
      **       TK_LE              OP_Gt
      **       TK_GE              OP_Lt
      **       TK_LT              OP_Ge
      **
      ** For other values of pExpr.op, op is undefined and unused.
      ** The value of TK_ and OP_ constants are arranged such that we
      ** can compute the mapping above using the following expression.
      ** Assert()s verify that the computation is correct.
      */
      op = ( ( pExpr.op + ( TK_ISNULL & 1 ) ) ^ 1 ) - ( TK_ISNULL & 1 );

      /* Verify correct alignment of TK_ and OP_ constants
      */
      Debug.Assert( pExpr.op != TK_ISNULL || op == OP_NotNull );
      Debug.Assert( pExpr.op != TK_NOTNULL || op == OP_IsNull );
      Debug.Assert( pExpr.op != TK_NE || op == OP_Eq );
      Debug.Assert( pExpr.op != TK_EQ || op == OP_Ne );
      Debug.Assert( pExpr.op != TK_LT || op == OP_Ge );
      Debug.Assert( pExpr.op != TK_LE || op == OP_Gt );
      Debug.Assert( pExpr.op != TK_GT || op == OP_Le );
      Debug.Assert( pExpr.op != TK_GE || op == OP_Lt );

      switch ( pExpr.op )
      {
        case TK_AND:
          {
            testcase( jumpIfNull == 0 );
            sqlite3ExprIfFalse( pParse, pExpr.pLeft, dest, jumpIfNull );
            sqlite3ExprIfFalse( pParse, pExpr.pRight, dest, jumpIfNull );
            break;
          }
        case TK_OR:
          {
            int d2 = sqlite3VdbeMakeLabel( v );
            testcase( jumpIfNull == 0 );
            sqlite3ExprCachePush( pParse );
            sqlite3ExprIfTrue( pParse, pExpr.pLeft, d2, jumpIfNull ^ SQLITE_JUMPIFNULL );
            sqlite3ExprIfFalse( pParse, pExpr.pRight, dest, jumpIfNull );
            sqlite3VdbeResolveLabel( v, d2 );
            sqlite3ExprCachePop( pParse, 1 );
            break;
          }
        case TK_NOT:
          {
            sqlite3ExprIfTrue( pParse, pExpr.pLeft, dest, jumpIfNull );
            break;
          }
        case TK_LT:
        case TK_LE:
        case TK_GT:
        case TK_GE:
        case TK_NE:
        case TK_EQ:
          {
            testcase( op == TK_LT );
            testcase( op == TK_LE );
            testcase( op == TK_GT );
            testcase( op == TK_GE );
            testcase( op == TK_EQ );
            testcase( op == TK_NE );
            testcase( jumpIfNull == 0 );
            codeCompareOperands( pParse, pExpr.pLeft, ref  r1, ref  regFree1,
            pExpr.pRight, ref r2, ref regFree2 );
            codeCompare( pParse, pExpr.pLeft, pExpr.pRight, op,
            r1, r2, dest, jumpIfNull );
            testcase( regFree1 == 0 );
            testcase( regFree2 == 0 );
            break;
          }
        case TK_ISNULL:
        case TK_NOTNULL:
          {
            testcase( op == TK_ISNULL );
            testcase( op == TK_NOTNULL );
            r1 = sqlite3ExprCodeTemp( pParse, pExpr.pLeft, ref regFree1 );
            sqlite3VdbeAddOp2( v, op, r1, dest );
            testcase( regFree1 == 0 );
            break;
          }
        case TK_BETWEEN:
          {
            /*    x BETWEEN y AND z
            **
            ** Is equivalent to
            **
            **    x>=y AND x<=z
            **
            ** Code it as such, taking care to do the common subexpression
            ** elementation of x.
            */
            Expr exprAnd = new Expr();
            Expr compLeft = new Expr();
            Expr compRight = new Expr();
            Expr exprX = new Expr();

            Debug.Assert( !ExprHasProperty( pExpr, EP_xIsSelect ) );
            exprX = pExpr.pLeft;
            exprAnd.op = TK_AND;
            exprAnd.pLeft = compLeft;
            exprAnd.pRight = compRight;
            compLeft.op = TK_GE;
            compLeft.pLeft = exprX;
            compLeft.pRight = pExpr.x.pList.a[0].pExpr;
            compRight.op = TK_LE;
            compRight.pLeft = exprX;
            compRight.pRight = pExpr.x.pList.a[1].pExpr;
            exprX.iTable = sqlite3ExprCodeTemp( pParse, exprX, ref regFree1 );
            testcase( regFree1 == 0 );
            exprX.op = TK_REGISTER;
            testcase( jumpIfNull == 0 );
            sqlite3ExprIfFalse( pParse, exprAnd, dest, jumpIfNull );
            break;
          }
        default:
          {
            r1 = sqlite3ExprCodeTemp( pParse, pExpr, ref regFree1 );
            sqlite3VdbeAddOp3( v, OP_IfNot, r1, dest, jumpIfNull != 0 ? 1 : 0 );
            testcase( regFree1 == 0 );
            testcase( jumpIfNull == 0 );
            break;
          }
      }
      sqlite3ReleaseTempReg( pParse, regFree1 );
      sqlite3ReleaseTempReg( pParse, regFree2 );
    }

    /*
    ** Do a deep comparison of two expression trees.  Return TRUE (non-zero)
    ** if they are identical and return FALSE if they differ in any way.
    **
    ** Sometimes this routine will return FALSE even if the two expressions
    ** really are equivalent.  If we cannot prove that the expressions are
    ** identical, we return FALSE just to be safe.  So if this routine
    ** returns false, then you do not really know for certain if the two
    ** expressions are the same.  But if you get a TRUE return, then you
    ** can be sure the expressions are the same.  In the places where
    ** this routine is used, it does not hurt to get an extra FALSE - that
    ** just might result in some slightly slower code.  But returning
    ** an incorrect TRUE could lead to a malfunction.
    */
    static bool sqlite3ExprCompare( Expr pA, Expr pB )
    {
      int i;
      if ( pA == null || pB == null )
      {
        return pB == pA;
      }
      Debug.Assert( !ExprHasAnyProperty( pA, EP_TokenOnly | EP_Reduced ) );
      Debug.Assert( !ExprHasAnyProperty( pB, EP_TokenOnly | EP_Reduced ) );
      if ( ExprHasProperty( pA, EP_xIsSelect ) || ExprHasProperty( pB, EP_xIsSelect ) )
      {
        return false;
      }
      if ( ( pA.flags & EP_Distinct ) != ( pB.flags & EP_Distinct ) ) return false;
      if ( pA.op != pB.op ) return false;
      if ( !sqlite3ExprCompare( pA.pLeft, pB.pLeft ) ) return false;
      if ( !sqlite3ExprCompare( pA.pRight, pB.pRight ) ) return false;
      if ( pA.x.pList != null && pB.x.pList != null )
      {
        if ( pA.x.pList.nExpr != pB.x.pList.nExpr ) return false;
        for ( i = 0 ; i < pA.x.pList.nExpr ; i++ )
        {
          Expr pExprA = pA.x.pList.a[i].pExpr;
          Expr pExprB = pB.x.pList.a[i].pExpr;
          if ( !sqlite3ExprCompare( pExprA, pExprB ) ) return false;
        }
      }
      else if ( pA.x.pList != null || pB.x.pList != null )
      {
        return false;
      }
      if ( pA.iTable != pB.iTable || pA.iColumn != pB.iColumn ) return false;
      if ( ExprHasProperty( pA, EP_IntValue ) )
      {
        if ( !ExprHasProperty( pB, EP_IntValue ) || pA.u.iValue != pB.u.iValue )
        {
          return false;
        }
      }
      else if ( pA.op != TK_COLUMN && pA.u.zToken != null )
      {
        if ( ExprHasProperty( pB, EP_IntValue ) || NEVER( pB.u.zToken == null ) ) return false;
        if ( sqlite3StrICmp( pA.u.zToken, pB.u.zToken ) != 0 )
        {
          return false;
        }
      }
      return true;
    }


    /*
    ** Add a new element to the pAggInfo.aCol[] array.  Return the index of
    ** the new element.  Return a negative number if malloc fails.
    */
    static int addAggInfoColumn( sqlite3 db, AggInfo pInfo )
    {
      int i = 0;
      pInfo.aCol = sqlite3ArrayAllocate(
      db,
      pInfo.aCol,
      -1,//sizeof(pInfo.aCol[0]),
      3,
      ref pInfo.nColumn,
      ref pInfo.nColumnAlloc,
      ref i
      );
      return i;
    }

    /*
    ** Add a new element to the pAggInfo.aFunc[] array.  Return the index of
    ** the new element.  Return a negative number if malloc fails.
    */
    static int addAggInfoFunc( sqlite3 db, AggInfo pInfo )
    {
      int i = 0;
      pInfo.aFunc = sqlite3ArrayAllocate(
      db,
      pInfo.aFunc,
      -1,//sizeof(pInfo.aFunc[0]),
      3,
      ref pInfo.nFunc,
      ref pInfo.nFuncAlloc,
      ref i
      );
      return i;
    }

    /*
    ** This is the xExprCallback for a tree walker.  It is used to
    ** implement sqlite3ExprAnalyzeAggregates().  See sqlite3ExprAnalyzeAggregates
    ** for additional information.
    */
    static int analyzeAggregate( Walker pWalker, ref Expr pExpr )
    {
      int i;
      NameContext pNC = pWalker.u.pNC;
      Parse pParse = pNC.pParse;
      SrcList pSrcList = pNC.pSrcList;
      AggInfo pAggInfo = pNC.pAggInfo;

      switch ( pExpr.op )
      {
        case TK_AGG_COLUMN:
        case TK_COLUMN:
          {
            testcase( pExpr.op == TK_AGG_COLUMN );
            testcase( pExpr.op == TK_COLUMN );
            /* Check to see if the column is in one of the tables in the FROM
            ** clause of the aggregate query */
            if ( ALWAYS( pSrcList != null ) )
            {
              SrcList_item pItem;// = pSrcList.a;
              for ( i = 0 ; i < pSrcList.nSrc ; i++ )
              {//, pItem++){
                pItem = pSrcList.a[i];
                AggInfo_col pCol;
                Debug.Assert( !ExprHasAnyProperty( pExpr, EP_TokenOnly | EP_Reduced ) );
                if ( pExpr.iTable == pItem.iCursor )
                {
                  /* If we reach this point, it means that pExpr refers to a table
                  ** that is in the FROM clause of the aggregate query.
                  **
                  ** Make an entry for the column in pAggInfo.aCol[] if there
                  ** is not an entry there already.
                  */
                  int k;
                  //pCol = pAggInfo.aCol;
                  for ( k = 0 ; k < pAggInfo.nColumn ; k++ )
                  {//, pCol++){
                    pCol = pAggInfo.aCol[k];
                    if ( pCol.iTable == pExpr.iTable &&
                    pCol.iColumn == pExpr.iColumn )
                    {
                      break;
                    }
                  }
                  if ( ( k >= pAggInfo.nColumn )
                  && ( k = addAggInfoColumn( pParse.db, pAggInfo ) ) >= 0
                  )
                  {
                    pCol = pAggInfo.aCol[k];
                    pCol.pTab = pExpr.pTab;
                    pCol.iTable = pExpr.iTable;
                    pCol.iColumn = pExpr.iColumn;
                    pCol.iMem = ++pParse.nMem;
                    pCol.iSorterColumn = -1;
                    pCol.pExpr = pExpr;
                    if ( pAggInfo.pGroupBy != null )
                    {
                      int j, n;
                      ExprList pGB = pAggInfo.pGroupBy;
                      ExprList_item pTerm;// = pGB.a;
                      n = pGB.nExpr;
                      for ( j = 0 ; j < n ; j++ )
                      {//, pTerm++){
                        pTerm = pGB.a[j];
                        Expr pE = pTerm.pExpr;
                        if ( pE.op == TK_COLUMN && pE.iTable == pExpr.iTable &&
                        pE.iColumn == pExpr.iColumn )
                        {
                          pCol.iSorterColumn = j;
                          break;
                        }
                      }
                    }
                    if ( pCol.iSorterColumn < 0 )
                    {
                      pCol.iSorterColumn = pAggInfo.nSortingColumn++;
                    }
                  }
                  /* There is now an entry for pExpr in pAggInfo.aCol[] (either
                  ** because it was there before or because we just created it).
                  ** Convert the pExpr to be a TK_AGG_COLUMN referring to that
                  ** pAggInfo.aCol[] entry.
                  */
                  ExprSetIrreducible( pExpr );
                  pExpr.pAggInfo = pAggInfo;
                  pExpr.op = TK_AGG_COLUMN;
                  pExpr.iAgg = (short)k;
                  break;
                } /* endif pExpr.iTable==pItem.iCursor */
              } /* end loop over pSrcList */
            }
            return WRC_Prune;
          }
        case TK_AGG_FUNCTION:
          {
            /* The pNC.nDepth==0 test causes aggregate functions in subqueries
            ** to be ignored */
            if ( pNC.nDepth == 0 )
            {
              /* Check to see if pExpr is a duplicate of another aggregate
              ** function that is already in the pAggInfo structure
              */
              AggInfo_func pItem;// = pAggInfo.aFunc;
              for ( i = 0 ; i < pAggInfo.nFunc ; i++ )
              {//, pItem++){
                pItem = pAggInfo.aFunc[i];
                if ( sqlite3ExprCompare( pItem.pExpr, pExpr ) )
                {
                  break;
                }
              }
              if ( i >= pAggInfo.nFunc )
              {
                /* pExpr is original.  Make a new entry in pAggInfo.aFunc[]
                */
                u8 enc = pParse.db.aDbStatic[0].pSchema.enc;// ENC(pParse.db);
                i = addAggInfoFunc( pParse.db, pAggInfo );
                if ( i >= 0 )
                {
                  Debug.Assert( !ExprHasProperty( pExpr, EP_xIsSelect ) );
                  pItem = pAggInfo.aFunc[i];
                  pItem.pExpr = pExpr;
                  pItem.iMem = ++pParse.nMem;
                  Debug.Assert( !ExprHasProperty( pExpr, EP_IntValue ) );
                  pItem.pFunc = sqlite3FindFunction( pParse.db,
                  pExpr.u.zToken, sqlite3Strlen30( pExpr.u.zToken ),
                  pExpr.x.pList != null ? pExpr.x.pList.nExpr : 0, enc, 0 );
                  if ( ( pExpr.flags & EP_Distinct ) != 0 )
                  {
                    pItem.iDistinct = pParse.nTab++;
                  }
                  else
                  {
                    pItem.iDistinct = -1;
                  }
                }
              }
              /* Make pExpr point to the appropriate pAggInfo.aFunc[] entry
              */
              Debug.Assert( !ExprHasAnyProperty( pExpr, EP_TokenOnly | EP_Reduced ) );
              ExprSetIrreducible( pExpr );
              pExpr.iAgg = (short)i;
              pExpr.pAggInfo = pAggInfo;
              return WRC_Prune;
            }
            break;
          }
      }
      return WRC_Continue;
    }

    static int analyzeAggregatesInSelect( Walker pWalker, Select pSelect )
    {
      NameContext pNC = pWalker.u.pNC;
      if ( pNC.nDepth == 0 )
      {
        pNC.nDepth++;
        sqlite3WalkSelect( pWalker, pSelect );
        pNC.nDepth--;
        return WRC_Prune;
      }
      else
      {
        return WRC_Continue;
      }
    }


    /*
    ** Analyze the given expression looking for aggregate functions and
    ** for variables that need to be added to the pParse.aAgg[] array.
    ** Make additional entries to the pParse.aAgg[] array as necessary.
    **
    ** This routine should only be called after the expression has been
    ** analyzed by sqlite3ResolveExprNames().
    */
    static void sqlite3ExprAnalyzeAggregates( NameContext pNC, ref  Expr pExpr )
    {
      Walker w = new Walker();
      w.xExprCallback = (dxExprCallback)analyzeAggregate;
      w.xSelectCallback = (dxSelectCallback)analyzeAggregatesInSelect;
      w.u.pNC = pNC;
      Debug.Assert( pNC.pSrcList != null );
      sqlite3WalkExpr( w, ref pExpr );
    }

    /*
    ** Call sqlite3ExprAnalyzeAggregates() for every expression in an
    ** expression list.  Return the number of errors.
    **
    ** If an error is found, the analysis is cut short.
    */
    static void sqlite3ExprAnalyzeAggList( NameContext pNC, ExprList pList )
    {
      ExprList_item pItem;
      int i;
      if ( pList != null )
      {
        for ( i = 0 ; i < pList.nExpr ; i++ )//, pItem++)
        {
          pItem = pList.a[i];
          sqlite3ExprAnalyzeAggregates( pNC, ref pItem.pExpr );
        }
      }
    }

    /*
    ** Allocate a single new register for use to hold some intermediate result.
    */
    static int sqlite3GetTempReg( Parse pParse )
    {
      if ( pParse.nTempReg == 0 )
      {
        return ++pParse.nMem;
      }
      return pParse.aTempReg[--pParse.nTempReg];
    }

    /*
    ** Deallocate a register, making available for reuse for some other
    ** purpose.
    **
    ** If a register is currently being used by the column cache, then
    ** the dallocation is deferred until the column cache line that uses
    ** the register becomes stale.
    */
    static void sqlite3ReleaseTempReg( Parse pParse, int iReg )
    {
      if ( iReg != 0 && pParse.nTempReg < ArraySize( pParse.aTempReg ) )
      {
        int i;
        yColCache p;
        for ( i = 0 ; i < SQLITE_N_COLCACHE ; i++ )//p=pParse.aColCache... p++)
        {
          p = pParse.aColCache[i];
          if ( p.iReg == iReg )
          {
            p.tempReg = 1;
            return;
          }
        }
        pParse.aTempReg[pParse.nTempReg++] = iReg;
      }
    }

    /*
    ** Allocate or deallocate a block of nReg consecutive registers
    */
    static int sqlite3GetTempRange( Parse pParse, int nReg )
    {
      int i, n;
      i = pParse.iRangeReg;
      n = pParse.nRangeReg;
      if ( nReg <= n && usedAsColumnCache( pParse, i, i + n - 1 ) == 0 )
      {
        pParse.iRangeReg += nReg;
        pParse.nRangeReg -= nReg;
      }
      else
      {
        i = pParse.nMem + 1;
        pParse.nMem += nReg;
      }
      return i;
    }
    static void sqlite3ReleaseTempRange( Parse pParse, int iReg, int nReg )
    {
      if ( nReg > pParse.nRangeReg )
      {
        pParse.nRangeReg = nReg;
        pParse.iRangeReg = iReg;
      }
    }
  }
}
