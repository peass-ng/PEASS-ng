using System;
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
    ** This module contains C code that generates VDBE code used to process
    ** the WHERE clause of SQL statements.  This module is responsible for
    ** generating the code that loops through a table looking for applicable
    ** rows.  Indices are selected and used to speed the search when doing
    ** so is applicable.  Because this module is responsible for selecting
    ** indices, you might also think of this module as the "query optimizer".
    **
    ** $Id: where.c,v 1.411 2009/07/31 06:14:52 danielk1977 Exp $
    **
    *************************************************************************
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    *************************************************************************
    */
    //#include "sqliteInt.h"

    /*
    ** Trace output macros
    */
#if  (SQLITE_TEST) && (SQLITE_DEBUG)
    static bool sqlite3WhereTrace = false;
#endif
#if  (SQLITE_TEST) && (SQLITE_DEBUG) && TRACE
    //# define WHERETRACE(X)  if(sqlite3WhereTrace) sqlite3DebugPrintf X
    static void WHERETRACE( string X, params object[] ap ) { if ( sqlite3WhereTrace ) sqlite3DebugPrintf( X, ap ); }
#else
//# define WHERETRACE(X)
static void WHERETRACE( string X, params object[] ap ) { }
#endif

    /* Forward reference
*/
    //typedef struct WhereClause WhereClause;
    //typedef struct WhereMaskSet WhereMaskSet;
    //typedef struct WhereOrInfo WhereOrInfo;
    //typedef struct WhereAndInfo WhereAndInfo;
    //typedef struct WhereCost WhereCost;

    /*
    ** The query generator uses an array of instances of this structure to
    ** help it analyze the subexpressions of the WHERE clause.  Each WHERE
    ** clause subexpression is separated from the others by AND operators,
    ** usually, or sometimes subexpressions separated by OR.
    **
    ** All WhereTerms are collected into a single WhereClause structure.
    ** The following identity holds:
    **
    **        WhereTerm.pWC.a[WhereTerm.idx] == WhereTerm
    **
    ** When a term is of the form:
    **
    **              X <op> <expr>
    **
    ** where X is a column name and <op> is one of certain operators,
    ** then WhereTerm.leftCursor and WhereTerm.u.leftColumn record the
    ** cursor number and column number for X.  WhereTerm.eOperator records
    ** the <op> using a bitmask encoding defined by WO_xxx below.  The
    ** use of a bitmask encoding for the operator allows us to search
    ** quickly for terms that match any of several different operators.
    **
    ** A WhereTerm might also be two or more subterms connected by OR:
    **
    **         (t1.X <op> <expr>) OR (t1.Y <op> <expr>) OR ....
    **
    ** In this second case, wtFlag as the TERM_ORINFO set and eOperator==WO_OR
    ** and the WhereTerm.u.pOrInfo field points to auxiliary information that
    ** is collected about the
    **
    ** If a term in the WHERE clause does not match either of the two previous
    ** categories, then eOperator==0.  The WhereTerm.pExpr field is still set
    ** to the original subexpression content and wtFlags is set up appropriately
    ** but no other fields in the WhereTerm object are meaningful.
    **
    ** When eOperator!=0, prereqRight and prereqAll record sets of cursor numbers,
    ** but they do so indirectly.  A single WhereMaskSet structure translates
    ** cursor number into bits and the translated bit is stored in the prereq
    ** fields.  The translation is used in order to maximize the number of
    ** bits that will fit in a Bitmask.  The VDBE cursor numbers might be
    ** spread out over the non-negative integers.  For example, the cursor
    ** numbers might be 3, 8, 9, 10, 20, 23, 41, and 45.  The WhereMaskSet
    ** translates these sparse cursor numbers into consecutive integers
    ** beginning with 0 in order to make the best possible use of the available
    ** bits in the Bitmask.  So, in the example above, the cursor numbers
    ** would be mapped into integers 0 through 7.
    **
    ** The number of terms in a join is limited by the number of bits
    ** in prereqRight and prereqAll.  The default is 64 bits, hence SQLite
    ** is only able to process joins with 64 or fewer tables.
    */
    //typedef struct WhereTerm WhereTerm;
    public class WhereTerm
    {
      public Expr pExpr;              /* Pointer to the subexpression that is this term */
      public int iParent;             /* Disable pWC.a[iParent] when this term disabled */
      public int leftCursor;          /* Cursor number of X in "X <op> <expr>" */
      public class _u
      {
        public int leftColumn;        /* Column number of X in "X <op> <expr>" */
        public WhereOrInfo pOrInfo;   /* Extra information if eOperator==WO_OR */
        public WhereAndInfo pAndInfo; /* Extra information if eOperator==WO_AND */
      }
      public _u u = new _u();
      public u16 eOperator;          /* A WO_xx value describing <op> */
      public u8 wtFlags;             /* TERM_xxx bit flags.  See below */
      public u8 nChild;              /* Number of children that must disable us */
      public WhereClause pWC;        /* The clause this term is part of */
      public Bitmask prereqRight;    /* Bitmask of tables used by pExpr.pRight */
      public Bitmask prereqAll;      /* Bitmask of tables referenced by pExpr */
    };

    /*
    ** Allowed values of WhereTerm.wtFlags
    */
    //#define TERM_DYNAMIC    0x01   /* Need to call sqlite3ExprDelete(db, ref pExpr) */
    //#define TERM_VIRTUAL    0x02   /* Added by the optimizer.  Do not code */
    //#define TERM_CODED      0x04   /* This term is already coded */
    //#define TERM_COPIED     0x08   /* Has a child */
    //#define TERM_ORINFO     0x10   /* Need to free the WhereTerm.u.pOrInfo object */
    //#define TERM_ANDINFO    0x20   /* Need to free the WhereTerm.u.pAndInfo obj */
    //#define TERM_OR_OK      0x40   /* Used during OR-clause processing */
    const int TERM_DYNAMIC = 0x01; /* Need to call sqlite3ExprDelete(db, ref pExpr) */
    const int TERM_VIRTUAL = 0x02; /* Added by the optimizer.  Do not code */
    const int TERM_CODED = 0x04; /* This term is already coded */
    const int TERM_COPIED = 0x08; /* Has a child */
    const int TERM_ORINFO = 0x10; /* Need to free the WhereTerm.u.pOrInfo object */
    const int TERM_ANDINFO = 0x20; /* Need to free the WhereTerm.u.pAndInfo obj */
    const int TERM_OR_OK = 0x40; /* Used during OR-clause processing */
    /*
    ** An instance of the following structure holds all information about a
    ** WHERE clause.  Mostly this is a container for one or more WhereTerms.
    */
    public class WhereClause
    {
      public Parse pParse;                              /* The parser context */
      public WhereMaskSet pMaskSet;                     /* Mapping of table cursor numbers to bitmasks */
      public Bitmask vmask;                             /* Bitmask identifying virtual table cursors */
      public u8 op;                                     /* Split operator.  TK_AND or TK_OR */
      public int nTerm;                                 /* Number of terms */
      public int nSlot;                                 /* Number of entries in a[] */
      public WhereTerm[] a;                             /* Each a[] describes a term of the WHERE cluase */
#if (SQLITE_SMALL_STACK)
public WhereTerm[] aStatic = new WhereTerm[1];    /* Initial static space for a[] */
#else
      public WhereTerm[] aStatic = new WhereTerm[8];    /* Initial static space for a[] */
#endif

      public void CopyTo( WhereClause wc )
      {
        wc.pParse = this.pParse;
        wc.pMaskSet = new WhereMaskSet();
        this.pMaskSet.CopyTo( wc.pMaskSet );
        wc.op = this.op;
        wc.nTerm = this.nTerm;
        wc.nSlot = this.nSlot;
        wc.a = (WhereTerm[])this.a.Clone();
        wc.aStatic = (WhereTerm[])this.aStatic.Clone();
      }
    };

    /*
    ** A WhereTerm with eOperator==WO_OR has its u.pOrInfo pointer set to
    ** a dynamically allocated instance of the following structure.
    */
    public class WhereOrInfo
    {
      public WhereClause wc = new WhereClause();/* Decomposition into subterms */
      public Bitmask indexable;                 /* Bitmask of all indexable tables in the clause */
    };

    /*
    ** A WhereTerm with eOperator==WO_AND has its u.pAndInfo pointer set to
    ** a dynamically allocated instance of the following structure.
    */
    public class WhereAndInfo
    {
      public WhereClause wc = new WhereClause();          /* The subexpression broken out */
    };

    /*
    ** An instance of the following structure keeps track of a mapping
    ** between VDBE cursor numbers and bits of the bitmasks in WhereTerm.
    **
    ** The VDBE cursor numbers are small integers contained in
    ** SrcList_item.iCursor and Expr.iTable fields.  For any given WHERE
    ** clause, the cursor numbers might not begin with 0 and they might
    ** contain gaps in the numbering sequence.  But we want to make maximum
    ** use of the bits in our bitmasks.  This structure provides a mapping
    ** from the sparse cursor numbers into consecutive integers beginning
    ** with 0.
    **
    ** If WhereMaskSet.ix[A]==B it means that The A-th bit of a Bitmask
    ** corresponds VDBE cursor number B.  The A-th bit of a bitmask is 1<<A.
    **
    ** For example, if the WHERE clause expression used these VDBE
    ** cursors:  4, 5, 8, 29, 57, 73.  Then the  WhereMaskSet structure
    ** would map those cursor numbers into bits 0 through 5.
    **
    ** Note that the mapping is not necessarily ordered.  In the example
    ** above, the mapping might go like this:  4.3, 5.1, 8.2, 29.0,
    ** 57.5, 73.4.  Or one of 719 other combinations might be used. It
    ** does not really matter.  What is important is that sparse cursor
    ** numbers all get mapped into bit numbers that begin with 0 and contain
    ** no gaps.
    */
    public class WhereMaskSet
    {
      public int n;                        /* Number of Debug.Assigned cursor values */
      public int[] ix = new int[BMS];       /* Cursor Debug.Assigned to each bit */

      public void CopyTo( WhereMaskSet wms )
      {
        wms.n = this.n;
        wms.ix = (int[])this.ix.Clone();
      }
    }

    /*
    ** A WhereCost object records a lookup strategy and the estimated
    ** cost of pursuing that strategy.
    */
    public class WhereCost
    {
      public WherePlan plan = new WherePlan();/* The lookup strategy */
      public double rCost;                    /* Overall cost of pursuing this search strategy */
      public double nRow;                     /* Estimated number of output rows */
    };

    /*
    ** Bitmasks for the operators that indices are able to exploit.  An
    ** OR-ed combination of these values can be used when searching for
    ** terms in the where clause.
    */
    //#define WO_IN     0x001
    //#define WO_EQ     0x002
    //#define WO_LT     (WO_EQ<<(TK_LT-TK_EQ))
    //#define WO_LE     (WO_EQ<<(TK_LE-TK_EQ))
    //#define WO_GT     (WO_EQ<<(TK_GT-TK_EQ))
    //#define WO_GE     (WO_EQ<<(TK_GE-TK_EQ))
    //#define WO_MATCH  0x040
    //#define WO_ISNULL 0x080
    //#define WO_OR     0x100       /* Two or more OR-connected terms */
    //#define WO_AND    0x200       /* Two or more AND-connected terms */

    //#define WO_ALL    0xfff       /* Mask of all possible WO_* values */
    //#define WO_SINGLE 0x0ff       /* Mask of all non-compound WO_* values */
    const int WO_IN = 0x001;
    const int WO_EQ = 0x002;
    const int WO_LT = ( WO_EQ << ( TK_LT - TK_EQ ) );
    const int WO_LE = ( WO_EQ << ( TK_LE - TK_EQ ) );
    const int WO_GT = ( WO_EQ << ( TK_GT - TK_EQ ) );
    const int WO_GE = ( WO_EQ << ( TK_GE - TK_EQ ) );
    const int WO_MATCH = 0x040;
    const int WO_ISNULL = 0x080;
    const int WO_OR = 0x100;       /* Two or more OR-connected terms */
    const int WO_AND = 0x200;       /* Two or more AND-connected terms */

    const int WO_ALL = 0xfff;       /* Mask of all possible WO_* values */
    const int WO_SINGLE = 0x0ff;       /* Mask of all non-compound WO_* values */
    /*
    ** Value for wsFlags returned by bestIndex() and stored in
    ** WhereLevel.wsFlags.  These flags determine which search
    ** strategies are appropriate.
    **
    ** The least significant 12 bits is reserved as a mask for WO_ values above.
    ** The WhereLevel.wsFlags field is usually set to WO_IN|WO_EQ|WO_ISNULL.
    ** But if the table is the right table of a left join, WhereLevel.wsFlags
    ** is set to WO_IN|WO_EQ.  The WhereLevel.wsFlags field can then be used as
    ** the "op" parameter to findTerm when we are resolving equality constraints.
    ** ISNULL constraints will then not be used on the right table of a left
    ** join.  Tickets #2177 and #2189.
    */
    //#define WHERE_ROWID_EQ     0x00001000  /* rowid=EXPR or rowid IN (...) */
    //#define WHERE_ROWID_RANGE  0x00002000  /* rowid<EXPR and/or rowid>EXPR */
    //#define WHERE_COLUMN_EQ    0x00010000  /* x=EXPR or x IN (...) or x IS NULL */
    //#define WHERE_COLUMN_RANGE 0x00020000  /* x<EXPR and/or x>EXPR */
    //#define WHERE_COLUMN_IN    0x00040000  /* x IN (...) */
    //#define WHERE_COLUMN_NULL  0x00080000  /* x IS NULL */
    //#define WHERE_INDEXED      0x000f0000  /* Anything that uses an index */
    //#define WHERE_IN_ABLE      0x000f1000  /* Able to support an IN operator */
    //#define WHERE_TOP_LIMIT    0x00100000  /* x<EXPR or x<=EXPR constraint */
    //#define WHERE_BTM_LIMIT    0x00200000  /* x>EXPR or x>=EXPR constraint */
    //#define WHERE_IDX_ONLY     0x00800000  /* Use index only - omit table */
    //#define WHERE_ORDERBY      0x01000000  /* Output will appear in correct order */
    //#define WHERE_REVERSE      0x02000000  /* Scan in reverse order */
    //#define WHERE_UNIQUE       0x04000000  /* Selects no more than one row */
    //#define WHERE_VIRTUALTABLE 0x08000000  /* Use virtual-table processing */
    //#define WHERE_MULTI_OR     0x10000000  /* OR using multiple indices */
    const int WHERE_ROWID_EQ = 0x00001000; /* rowid=EXPR or rowid IN (...) */
    const int WHERE_ROWID_RANGE = 0x00002000; /* rowid<EXPR and/or rowid>EXPR */
    const int WHERE_COLUMN_EQ = 0x00010000; /* x=EXPR or x IN (...) */
    const int WHERE_COLUMN_RANGE = 0x00020000; /* x<EXPR and/or x>EXPR */
    const int WHERE_COLUMN_IN = 0x00040000; /* x IN (...) */
    const int WHERE_COLUMN_NULL = 0x00080000; /* x IS NULL */
    const int WHERE_INDEXED = 0x000f0000; /* Anything that uses an index */
    const int WHERE_IN_ABLE = 0x000f1000; /* Able to support an IN operator */
    const int WHERE_TOP_LIMIT = 0x00100000; /* x<EXPR or x<=EXPR constraint */
    const int WHERE_BTM_LIMIT = 0x00200000; /* x>EXPR or x>=EXPR constraint */
    const int WHERE_IDX_ONLY = 0x00800000; /* Use index only - omit table */
    const int WHERE_ORDERBY = 0x01000000; /* Output will appear in correct order */
    const int WHERE_REVERSE = 0x02000000; /* Scan in reverse order */
    const int WHERE_UNIQUE = 0x04000000; /* Selects no more than one row */
    const int WHERE_VIRTUALTABLE = 0x08000000; /* Use virtual-table processing */
    const int WHERE_MULTI_OR = 0x10000000; /* OR using multiple indices */

    /*
    ** Initialize a preallocated WhereClause structure.
    */
    static void whereClauseInit(
    WhereClause pWC,        /* The WhereClause to be initialized */
    Parse pParse,           /* The parsing context */
    WhereMaskSet pMaskSet   /* Mapping from table cursor numbers to bitmasks */
    )
    {
      pWC.pParse = pParse;
      pWC.pMaskSet = pMaskSet;
      pWC.nTerm = 0;
      pWC.nSlot = ArraySize( pWC.aStatic ) - 1;
      pWC.a = pWC.aStatic;
      pWC.vmask = 0;
    }

    /* Forward reference */
    //static void whereClauseClear(WhereClause);

    /*
    ** Deallocate all memory Debug.Associated with a WhereOrInfo object.
    */
    static void whereOrInfoDelete( sqlite3 db, WhereOrInfo p )
    {
      whereClauseClear( p.wc );
      //sqlite3DbFree( db, p );
    }

    /*
    ** Deallocate all memory Debug.Associated with a WhereAndInfo object.
    */
    static void whereAndInfoDelete( sqlite3 db, WhereAndInfo p )
    {
      whereClauseClear( p.wc );
      //sqlite3DbFree( db, p );
    }

    /*
    ** Deallocate a WhereClause structure.  The WhereClause structure
    ** itself is not freed.  This routine is the inverse of whereClauseInit().
    */
    static void whereClauseClear( WhereClause pWC )
    {
      int i;
      WhereTerm a;
      sqlite3 db = pWC.pParse.db;
      for ( i = pWC.nTerm - 1 ; i >= 0 ; i-- )//, a++)
      {
        a = pWC.a[i];
        if ( ( a.wtFlags & TERM_DYNAMIC ) != 0 )
        {
          sqlite3ExprDelete( db, ref a.pExpr );
        }
        if ( ( a.wtFlags & TERM_ORINFO ) != 0 )
        {
          whereOrInfoDelete( db, a.u.pOrInfo );
        }
        else if ( ( a.wtFlags & TERM_ANDINFO ) != 0 )
        {
          whereAndInfoDelete( db, a.u.pAndInfo );
        }
      }
      if ( pWC.a != pWC.aStatic )
      {
        //sqlite3DbFree( db, ref pWC.a );
      }
    }

    /*
    ** Add a single new WhereTerm entry to the WhereClause object pWC.
    ** The new WhereTerm object is constructed from Expr p and with wtFlags.
    ** The index in pWC.a[] of the new WhereTerm is returned on success.
    ** 0 is returned if the new WhereTerm could not be added due to a memory
    ** allocation error.  The memory allocation failure will be recorded in
    ** the db.mallocFailed flag so that higher-level functions can detect it.
    **
    ** This routine will increase the size of the pWC.a[] array as necessary.
    **
    ** If the wtFlags argument includes TERM_DYNAMIC, then responsibility
    ** for freeing the expression p is Debug.Assumed by the WhereClause object pWC.
    ** This is true even if this routine fails to allocate a new WhereTerm.
    **
    ** WARNING:  This routine might reallocate the space used to store
    ** WhereTerms.  All pointers to WhereTerms should be invalidated after
    ** calling this routine.  Such pointers may be reinitialized by referencing
    ** the pWC.a[] array.
    */
    static int whereClauseInsert( WhereClause pWC, Expr p, u8 wtFlags )
    {
      WhereTerm pTerm;
      int idx;
      if ( pWC.nTerm >= pWC.nSlot )
      {
        //WhereTerm pOld = pWC.a;
        sqlite3 db = pWC.pParse.db;
        Array.Resize( ref pWC.a, pWC.nSlot * 2 );
        //pWC.a = sqlite3DbMallocRaw(db, sizeof(pWC.a[0])*pWC.nSlot*2 );
        //if( pWC.a==null ){
        //  if( wtFlags & TERM_DYNAMIC ){
        //    sqlite3ExprDelete(db, ref p);
        //  }
        //  pWC.a = pOld;
        //  return 0;
        //}
        //memcpy(pWC.a, pOld, sizeof(pWC.a[0])*pWC.nTerm);
        //if( pOld!=pWC.aStatic ){
        //  //sqlite3DbFree(db, pOld);
        //}
        //pWC.nSlot = sqlite3DbMallocSize(db, pWC.a)/sizeof(pWC.a[0]);
        pWC.nSlot = pWC.a.Length - 1;
      }
      pWC.a[idx = pWC.nTerm++] = new WhereTerm();
      pTerm = pWC.a[idx];
      pTerm.pExpr = p;
      pTerm.wtFlags = wtFlags;
      pTerm.pWC = pWC;
      pTerm.iParent = -1;
      return idx;
    }

    /*
    ** This routine identifies subexpressions in the WHERE clause where
    ** each subexpression is separated by the AND operator or some other
    ** operator specified in the op parameter.  The WhereClause structure
    ** is filled with pointers to subexpressions.  For example:
    **
    **    WHERE  a=='hello' AND coalesce(b,11)<10 AND (c+12!=d OR c==22)
    **           \________/     \_______________/     \________________/
    **            slot[0]            slot[1]               slot[2]
    **
    ** The original WHERE clause in pExpr is unaltered.  All this routine
    ** does is make slot[] entries point to substructure within pExpr.
    **
    ** In the previous sentence and in the diagram, "slot[]" refers to
    ** the WhereClause.a[] array.  The slot[] array grows as needed to contain
    ** all terms of the WHERE clause.
    */
    static void whereSplit( WhereClause pWC, Expr pExpr, int op )
    {
      pWC.op = (u8)op;
      if ( pExpr == null ) return;
      if ( pExpr.op != op )
      {
        whereClauseInsert( pWC, pExpr, 0 );
      }
      else
      {
        whereSplit( pWC, pExpr.pLeft, op );
        whereSplit( pWC, pExpr.pRight, op );
      }
    }

    /*
    ** Initialize an expression mask set (a WhereMaskSet object)
    */
    //#define initMaskSet(P)  memset(P, 0, sizeof(*P))

    /*
    ** Return the bitmask for the given cursor number.  Return 0 if
    ** iCursor is not in the set.
    */
    static Bitmask getMask( WhereMaskSet pMaskSet, int iCursor )
    {
      int i;
      Debug.Assert( pMaskSet.n <= sizeof( Bitmask ) * 8 );
      for ( i = 0 ; i < pMaskSet.n ; i++ )
      {
        if ( pMaskSet.ix[i] == iCursor )
        {
          return ( (Bitmask)1 ) << i;
        }
      }
      return 0;
    }

    /*
    ** Create a new mask for cursor iCursor.
    **
    ** There is one cursor per table in the FROM clause.  The number of
    ** tables in the FROM clause is limited by a test early in the
    ** sqlite3WhereBegin() routine.  So we know that the pMaskSet.ix[]
    ** array will never overflow.
    */
    static void createMask( WhereMaskSet pMaskSet, int iCursor )
    {
      Debug.Assert( pMaskSet.n < ArraySize( pMaskSet.ix ) );
      pMaskSet.ix[pMaskSet.n++] = iCursor;
    }

    /*
    ** This routine walks (recursively) an expression tree and generates
    ** a bitmask indicating which tables are used in that expression
    ** tree.
    **
    ** In order for this routine to work, the calling function must have
    ** previously invoked sqlite3ResolveExprNames() on the expression.  See
    ** the header comment on that routine for additional information.
    ** The sqlite3ResolveExprNames() routines looks for column names and
    ** sets their opcodes to TK_COLUMN and their Expr.iTable fields to
    ** the VDBE cursor number of the table.  This routine just has to
    ** translate the cursor numbers into bitmask values and OR all
    ** the bitmasks together.
    */
    //static Bitmask exprListTableUsage(WhereMaskSet*, ExprList);
    //static Bitmask exprSelectTableUsage(WhereMaskSet*, Select);
    static Bitmask exprTableUsage( WhereMaskSet pMaskSet, Expr p )
    {
      Bitmask mask = 0;
      if ( p == null ) return 0;
      if ( p.op == TK_COLUMN )
      {
        mask = getMask( pMaskSet, p.iTable );
        return mask;
      }
      mask = exprTableUsage( pMaskSet, p.pRight );
      mask |= exprTableUsage( pMaskSet, p.pLeft );
      if ( ExprHasProperty( p, EP_xIsSelect ) )
      {
        mask |= exprSelectTableUsage( pMaskSet, p.x.pSelect );
      }
      else
      {
        mask |= exprListTableUsage( pMaskSet, p.x.pList );
      }
      return mask;
    }
    static Bitmask exprListTableUsage( WhereMaskSet pMaskSet, ExprList pList )
    {
      int i;
      Bitmask mask = 0;
      if ( pList != null )
      {
        for ( i = 0 ; i < pList.nExpr ; i++ )
        {
          mask |= exprTableUsage( pMaskSet, pList.a[i].pExpr );
        }
      }
      return mask;
    }
    static Bitmask exprSelectTableUsage( WhereMaskSet pMaskSet, Select pS )
    {
      Bitmask mask = 0;
      while ( pS != null )
      {
        mask |= exprListTableUsage( pMaskSet, pS.pEList );
        mask |= exprListTableUsage( pMaskSet, pS.pGroupBy );
        mask |= exprListTableUsage( pMaskSet, pS.pOrderBy );
        mask |= exprTableUsage( pMaskSet, pS.pWhere );
        mask |= exprTableUsage( pMaskSet, pS.pHaving );
        pS = pS.pPrior;
      }
      return mask;
    }

    /*
    ** Return TRUE if the given operator is one of the operators that is
    ** allowed for an indexable WHERE clause term.  The allowed operators are
    ** "=", "<", ">", "<=", ">=", and "IN".
    */
    static bool allowedOp( int op )
    {
      Debug.Assert( TK_GT > TK_EQ && TK_GT < TK_GE );
      Debug.Assert( TK_LT > TK_EQ && TK_LT < TK_GE );
      Debug.Assert( TK_LE > TK_EQ && TK_LE < TK_GE );
      Debug.Assert( TK_GE == TK_EQ + 4 );
      return op == TK_IN || ( op >= TK_EQ && op <= TK_GE ) || op == TK_ISNULL;
    }

    /*
    ** Swap two objects of type TYPE.
    */
    //#define SWAP(TYPE,A,B) {TYPE t=A; A=B; B=t;}

    /*
    ** Commute a comparison operator.  Expressions of the form "X op Y"
    ** are converted into "Y op X".
    **
    ** If a collation sequence is Debug.Associated with either the left or right
    ** side of the comparison, it remains Debug.Associated with the same side after
    ** the commutation. So "Y collate NOCASE op X" becomes
    ** "X collate NOCASE op Y". This is because any collation sequence on
    ** the left hand side of a comparison overrides any collation sequence
    ** attached to the right. For the same reason the EP_ExpCollate flag
    ** is not commuted.
    */
    static void exprCommute( Parse pParse, Expr pExpr )
    {
      u16 expRight = (u16)( pExpr.pRight.flags & EP_ExpCollate );
      u16 expLeft = (u16)( pExpr.pLeft.flags & EP_ExpCollate );
      Debug.Assert( allowedOp( pExpr.op ) && pExpr.op != TK_IN );
      pExpr.pRight.pColl = sqlite3ExprCollSeq( pParse, pExpr.pRight );
      pExpr.pLeft.pColl = sqlite3ExprCollSeq( pParse, pExpr.pLeft );
      SWAP( ref pExpr.pRight.pColl, ref pExpr.pLeft.pColl );
      pExpr.pRight.flags = (u16)( ( pExpr.pRight.flags & ~EP_ExpCollate ) | expLeft );
      pExpr.pLeft.flags = (u16)( ( pExpr.pLeft.flags & ~EP_ExpCollate ) | expRight );
      SWAP( ref pExpr.pRight, ref pExpr.pLeft );
      if ( pExpr.op >= TK_GT )
      {
        Debug.Assert( TK_LT == TK_GT + 2 );
        Debug.Assert( TK_GE == TK_LE + 2 );
        Debug.Assert( TK_GT > TK_EQ );
        Debug.Assert( TK_GT < TK_LE );
        Debug.Assert( pExpr.op >= TK_GT && pExpr.op <= TK_GE );
        pExpr.op = (u8)( ( ( pExpr.op - TK_GT ) ^ 2 ) + TK_GT );
      }
    }

    /*
    ** Translate from TK_xx operator to WO_xx bitmask.
    */
    static u16 operatorMask( int op )
    {
      u16 c;
      Debug.Assert( allowedOp( op ) );
      if ( op == TK_IN )
      {
        c = WO_IN;
      }
      else if ( op == TK_ISNULL )
      {
        c = WO_ISNULL;
      }
      else
      {
        Debug.Assert( ( WO_EQ << ( op - TK_EQ ) ) < 0x7fff );
        c = (u16)( WO_EQ << ( op - TK_EQ ) );
      }
      Debug.Assert( op != TK_ISNULL || c == WO_ISNULL );
      Debug.Assert( op != TK_IN || c == WO_IN );
      Debug.Assert( op != TK_EQ || c == WO_EQ );
      Debug.Assert( op != TK_LT || c == WO_LT );
      Debug.Assert( op != TK_LE || c == WO_LE );
      Debug.Assert( op != TK_GT || c == WO_GT );
      Debug.Assert( op != TK_GE || c == WO_GE );
      return c;
    }

    /*
    ** Search for a term in the WHERE clause that is of the form "X <op> <expr>"
    ** where X is a reference to the iColumn of table iCur and <op> is one of
    ** the WO_xx operator codes specified by the op parameter.
    ** Return a pointer to the term.  Return 0 if not found.
    */
    static WhereTerm findTerm(
    WhereClause pWC,     /* The WHERE clause to be searched */
    int iCur,             /* Cursor number of LHS */
    int iColumn,          /* Column number of LHS */
    Bitmask notReady,     /* RHS must not overlap with this mask */
    u32 op,               /* Mask of WO_xx values describing operator */
    Index pIdx           /* Must be compatible with this index, if not NULL */
    )
    {
      WhereTerm pTerm;
      int k;
      Debug.Assert( iCur >= 0 );
      op &= WO_ALL;
      for ( k = pWC.nTerm ; k != 0 ; k-- )//, pTerm++)
      {
        pTerm = pWC.a[pWC.nTerm - k];
        if ( pTerm.leftCursor == iCur
        && ( pTerm.prereqRight & notReady ) == 0
        && pTerm.u.leftColumn == iColumn
        && ( pTerm.eOperator & op ) != 0
        )
        {
          if ( pIdx != null && pTerm.eOperator != WO_ISNULL )
          {
            Expr pX = pTerm.pExpr;
            CollSeq pColl;
            char idxaff;
            int j;
            Parse pParse = pWC.pParse;

            idxaff = pIdx.pTable.aCol[iColumn].affinity;
            if ( !sqlite3IndexAffinityOk( pX, idxaff ) ) continue;

            /* Figure out the collation sequence required from an index for
            ** it to be useful for optimising expression pX. Store this
            ** value in variable pColl.
            */
            Debug.Assert( pX.pLeft != null );
            pColl = sqlite3BinaryCompareCollSeq( pParse, pX.pLeft, pX.pRight );
            Debug.Assert( pColl != null || pParse.nErr != 0 );

            for ( j = 0 ; pIdx.aiColumn[j] != iColumn ; j++ )
            {
              if ( NEVER( j >= pIdx.nColumn ) ) return null;
            }
            if ( pColl != null && sqlite3StrICmp( pColl.zName, pIdx.azColl[j] ) != 0 ) continue;
          }
          return pTerm;
        }
      }
      return null;
    }

    /* Forward reference */
    //static void exprAnalyze(SrcList*, WhereClause*, int);

    /*
    ** Call exprAnalyze on all terms in a WHERE clause.
    **
    **
    */
    static void exprAnalyzeAll(
    SrcList pTabList,       /* the FROM clause */
    WhereClause pWC         /* the WHERE clause to be analyzed */
    )
    {
      int i;
      for ( i = pWC.nTerm - 1 ; i >= 0 ; i-- )
      {
        exprAnalyze( pTabList, pWC, i );
      }
    }

#if  !SQLITE_OMIT_LIKE_OPTIMIZATION
    /*
** Check to see if the given expression is a LIKE or GLOB operator that
** can be optimized using inequality constraints.  Return TRUE if it is
** so and false if not.
**
** In order for the operator to be optimizible, the RHS must be a string
** literal that does not begin with a wildcard.
*/
    static int isLikeOrGlob(
    Parse pParse,         /* Parsing and code generating context */
    Expr pExpr,           /* Test this expression */
    ref int pnPattern,    /* Number of non-wildcard prefix characters */
    ref bool pisComplete, /* True if the only wildcard is % in the last character */
    ref bool pnoCase      /* True if uppercase is equivalent to lowercase */
    )
    {
      string z;                  /* String on RHS of LIKE operator */
      Expr pRight, pLeft;        /* Right and left size of LIKE operator */
      ExprList pList;            /* List of operands to the LIKE operator */
      int c = 0;                 /* One character in z[] */
      int cnt;                   /* Number of non-wildcard prefix characters */
      char[] wc = new char[3];   /* Wildcard characters */
      CollSeq pColl;             /* Collating sequence for LHS */
      sqlite3 db = pParse.db;    /* Data_base connection */

      if ( !sqlite3IsLikeFunction( db, pExpr, ref pnoCase, wc ) )
      {
        return 0;
      }
#if SQLITE_EBCDIC
if( pnoCase ) return 0;
#endif
      pList = pExpr.x.pList;
      pRight = pList.a[0].pExpr;
      if ( pRight.op != TK_STRING )
      {
        return 0;
      }
      pLeft = pList.a[1].pExpr;
      if ( pLeft.op != TK_COLUMN )
      {
        return 0;
      }
      pColl = sqlite3ExprCollSeq( pParse, pLeft );
      Debug.Assert( pColl != null || pLeft.iColumn == -1 );
      if ( pColl == null ) return 0;
      if ( ( pColl.type != SQLITE_COLL_BINARY || pnoCase ) &&
      ( pColl.type != SQLITE_COLL_NOCASE || !pnoCase ) )
      {
        return 0;
      }
      if ( sqlite3ExprAffinity( pLeft ) != SQLITE_AFF_TEXT ) return 0;
      z = pRight.u.zToken;
      if ( ALWAYS( !String.IsNullOrEmpty( z ) ) )
      {
        cnt = 0;
        while ( cnt < z.Length && ( c = z[cnt] ) != 0 && c != wc[0] && c != wc[1] && c != wc[2] )
        {
          cnt++;
        }
        if ( cnt != 0 && c != 0 && 255 != (u8)z[cnt - 1] )
        {
          pisComplete = cnt >= z.Length - 1 ? true : z[cnt] == wc[0] && z[cnt + 1] == 0;
          pnPattern = cnt;
          return 1;
        }
      }
      return 0;
    }
#endif //* SQLITE_OMIT_LIKE_OPTIMIZATION */


#if  !SQLITE_OMIT_VIRTUALTABLE
/*
** Check to see if the given expression is of the form
**
**         column MATCH expr
**
** If it is then return TRUE.  If not, return FALSE.
*/
static int isMatchOfColumn(
Expr pExpr      /* Test this expression */
){
ExprList pList;

if( pExpr.op!=TK_FUNCTION ){
return 0;
}
if(sqlite3StrICmp(pExpr->u.zToken,"match")!=0 ){
return 0;
}
pList = pExpr.x.pList;
if( pList.nExpr!=2 ){
return 0;
}
if( pList.a[1].pExpr.op != TK_COLUMN ){
return 0;
}
return 1;
}
#endif //* SQLITE_OMIT_VIRTUALTABLE */

    /*
** If the pBase expression originated in the ON or USING clause of
** a join, then transfer the appropriate markings over to derived.
*/
    static void transferJoinMarkings( Expr pDerived, Expr pBase )
    {
      pDerived.flags = (u16)( pDerived.flags | pBase.flags & EP_FromJoin );
      pDerived.iRightJoinTable = pBase.iRightJoinTable;
    }

#if  !(SQLITE_OMIT_OR_OPTIMIZATION) && !(SQLITE_OMIT_SUBQUERY)
    /*
** Analyze a term that consists of two or more OR-connected
** subterms.  So in:
**
**     ... WHERE  (a=5) AND (b=7 OR c=9 OR d=13) AND (d=13)
**                          ^^^^^^^^^^^^^^^^^^^^
**
** This routine analyzes terms such as the middle term in the above example.
** A WhereOrTerm object is computed and attached to the term under
** analysis, regardless of the outcome of the analysis.  Hence:
**
**     WhereTerm.wtFlags   |=  TERM_ORINFO
**     WhereTerm.u.pOrInfo  =  a dynamically allocated WhereOrTerm object
**
** The term being analyzed must have two or more of OR-connected subterms.
** A single subterm might be a set of AND-connected sub-subterms.
** Examples of terms under analysis:
**
**     (A)     t1.x=t2.y OR t1.x=t2.z OR t1.y=15 OR t1.z=t3.a+5
**     (B)     x=expr1 OR expr2=x OR x=expr3
**     (C)     t1.x=t2.y OR (t1.x=t2.z AND t1.y=15)
**     (D)     x=expr1 OR (y>11 AND y<22 AND z LIKE '*hello*')
**     (E)     (p.a=1 AND q.b=2 AND r.c=3) OR (p.x=4 AND q.y=5 AND r.z=6)
**
** CASE 1:
**
** If all subterms are of the form T.C=expr for some single column of C
** a single table T (as shown in example B above) then create a new virtual
** term that is an equivalent IN expression.  In other words, if the term
** being analyzed is:
**
**      x = expr1  OR  expr2 = x  OR  x = expr3
**
** then create a new virtual term like this:
**
**      x IN (expr1,expr2,expr3)
**
** CASE 2:
**
** If all subterms are indexable by a single table T, then set
**
**     WhereTerm.eOperator              =  WO_OR
**     WhereTerm.u.pOrInfo.indexable  |=  the cursor number for table T
**
** A subterm is "indexable" if it is of the form
** "T.C <op> <expr>" where C is any column of table T and
** <op> is one of "=", "<", "<=", ">", ">=", "IS NULL", or "IN".
** A subterm is also indexable if it is an AND of two or more
** subsubterms at least one of which is indexable.  Indexable AND
** subterms have their eOperator set to WO_AND and they have
** u.pAndInfo set to a dynamically allocated WhereAndTerm object.
**
** From another point of view, "indexable" means that the subterm could
** potentially be used with an index if an appropriate index exists.
** This analysis does not consider whether or not the index exists; that
** is something the bestIndex() routine will determine.  This analysis
** only looks at whether subterms appropriate for indexing exist.
**
** All examples A through E above all satisfy case 2.  But if a term
** also statisfies case 1 (such as B) we know that the optimizer will
** always prefer case 1, so in that case we pretend that case 2 is not
** satisfied.
**
** It might be the case that multiple tables are indexable.  For example,
** (E) above is indexable on tables P, Q, and R.
**
** Terms that satisfy case 2 are candidates for lookup by using
** separate indices to find rowids for each subterm and composing
** the union of all rowids using a RowSet object.  This is similar
** to "bitmap indices" in other data_base engines.
**
** OTHERWISE:
**
** If neither case 1 nor case 2 apply, then leave the eOperator set to
** zero.  This term is not useful for search.
*/
    static void exprAnalyzeOrTerm(
    SrcList pSrc,            /* the FROM clause */
    WhereClause pWC,         /* the complete WHERE clause */
    int idxTerm               /* Index of the OR-term to be analyzed */
    )
    {
      Parse pParse = pWC.pParse;            /* Parser context */
      sqlite3 db = pParse.db;               /* Data_base connection */
      WhereTerm pTerm = pWC.a[idxTerm];    /* The term to be analyzed */
      Expr pExpr = pTerm.pExpr;             /* The expression of the term */
      WhereMaskSet pMaskSet = pWC.pMaskSet; /* Table use masks */
      int i;                                  /* Loop counters */
      WhereClause pOrWc;        /* Breakup of pTerm into subterms */
      WhereTerm pOrTerm;        /* A Sub-term within the pOrWc */
      WhereOrInfo pOrInfo;      /* Additional information Debug.Associated with pTerm */
      Bitmask chngToIN;         /* Tables that might satisfy case 1 */
      Bitmask indexable;        /* Tables that are indexable, satisfying case 2 */

      /*
      ** Break the OR clause into its separate subterms.  The subterms are
      ** stored in a WhereClause structure containing within the WhereOrInfo
      ** object that is attached to the original OR clause term.
      */
      Debug.Assert( ( pTerm.wtFlags & ( TERM_DYNAMIC | TERM_ORINFO | TERM_ANDINFO ) ) == 0 );
      Debug.Assert( pExpr.op == TK_OR );
      pTerm.u.pOrInfo = pOrInfo = new WhereOrInfo();//sqlite3DbMallocZero(db, sizeof(*pOrInfo));
      if ( pOrInfo == null ) return;
      pTerm.wtFlags |= TERM_ORINFO;
      pOrWc = pOrInfo.wc;
      whereClauseInit( pOrWc, pWC.pParse, pMaskSet );
      whereSplit( pOrWc, pExpr, TK_OR );
      exprAnalyzeAll( pSrc, pOrWc );
//      if ( db.mallocFailed != 0 ) return;
      Debug.Assert( pOrWc.nTerm >= 2 );

      /*
      ** Compute the set of tables that might satisfy cases 1 or 2.
      */
      indexable = ~(Bitmask)0;
      chngToIN = ~( pWC.vmask );
      for ( i = pOrWc.nTerm - 1 ; i >= 0 && indexable != 0 ; i-- )//, pOrTerm++ )
      {
        pOrTerm = pOrWc.a[i];
        if ( ( pOrTerm.eOperator & WO_SINGLE ) == 0 )
        {
          WhereAndInfo pAndInfo;
          Debug.Assert( pOrTerm.eOperator == 0 );
          Debug.Assert( ( pOrTerm.wtFlags & ( TERM_ANDINFO | TERM_ORINFO ) ) == 0 );
          chngToIN = 0;
          pAndInfo = new WhereAndInfo();//sqlite3DbMallocRaw(db, sizeof(*pAndInfo));
          if ( pAndInfo != null )
          {
            WhereClause pAndWC;
            WhereTerm pAndTerm;
            int j;
            Bitmask b = 0;
            pOrTerm.u.pAndInfo = pAndInfo;
            pOrTerm.wtFlags |= TERM_ANDINFO;
            pOrTerm.eOperator = WO_AND;
            pAndWC = pAndInfo.wc;
            whereClauseInit( pAndWC, pWC.pParse, pMaskSet );
            whereSplit( pAndWC, pOrTerm.pExpr, TK_AND );
            exprAnalyzeAll( pSrc, pAndWC );
            //testcase( db.mallocFailed );
            ////if ( 0 == db.mallocFailed )
            {
              for ( j = 0 ; j < pAndWC.nTerm ; j++ )//, pAndTerm++ )
              {
                pAndTerm = pAndWC.a[j];
                Debug.Assert( pAndTerm.pExpr != null );
                if ( allowedOp( pAndTerm.pExpr.op ) )
                {
                  b |= getMask( pMaskSet, pAndTerm.leftCursor );
                }
              }
            }
            indexable &= b;
          }
        }
        else if ( ( pOrTerm.wtFlags & TERM_COPIED ) != 0 )
        {
          /* Skip this term for now.  We revisit it when we process the
          ** corresponding TERM_VIRTUAL term */
        }
        else
        {
          Bitmask b;
          b = getMask( pMaskSet, pOrTerm.leftCursor );
          if ( ( pOrTerm.wtFlags & TERM_VIRTUAL ) != 0 )
          {
            WhereTerm pOther = pOrWc.a[pOrTerm.iParent];
            b |= getMask( pMaskSet, pOther.leftCursor );
          }
          indexable &= b;
          if ( pOrTerm.eOperator != WO_EQ )
          {
            chngToIN = 0;
          }
          else
          {
            chngToIN &= b;
          }
        }
      }

      /*
      ** Record the set of tables that satisfy case 2.  The set might be
      ** empty.
      */
      pOrInfo.indexable = indexable;
      pTerm.eOperator = (u16)( indexable == 0 ? 0 : WO_OR );

      /*
      ** chngToIN holds a set of tables that *might* satisfy case 1.  But
      ** we have to do some additional checking to see if case 1 really
      ** is satisfied.
      **
      ** chngToIN will hold either 0, 1, or 2 bits.  The 0-bit case means
      ** that there is no possibility of transforming the OR clause into an
      ** IN operator because one or more terms in the OR clause contain
      ** something other than == on a column in the single table.  The 1-bit
      ** case means that every term of the OR clause is of the form
      ** "table.column=expr" for some single table.  The one bit that is set
      ** will correspond to the common table.  We still need to check to make
      ** sure the same column is used on all terms.  The 2-bit case is when
      ** the all terms are of the form "table1.column=table2.column".  It
      ** might be possible to form an IN operator with either table1.column
      ** or table2.column as the LHS if either is common to every term of
      ** the OR clause.
      **
      ** Note that terms of the form "table.column1=table.column2" (the
      ** same table on both sizes of the ==) cannot be optimized.
      */
      if ( chngToIN != 0 )
      {
        int okToChngToIN = 0;     /* True if the conversion to IN is valid */
        int iColumn = -1;         /* Column index on lhs of IN operator */
        int iCursor = -1;         /* Table cursor common to all terms */
        int j = 0;                /* Loop counter */

        /* Search for a table and column that appears on one side or the
        ** other of the == operator in every subterm.  That table and column
        ** will be recorded in iCursor and iColumn.  There might not be any
        ** such table and column.  Set okToChngToIN if an appropriate table
        ** and column is found but leave okToChngToIN false if not found.
        */
        for ( j = 0 ; j < 2 && 0 == okToChngToIN ; j++ )
        {
          //pOrTerm = pOrWc.a;
          for ( i = pOrWc.nTerm - 1 ; i >= 0 ; i-- )//, pOrTerm++)
          {
            pOrTerm = pOrWc.a[pOrWc.nTerm - 1 - i];
            Debug.Assert( pOrTerm.eOperator == WO_EQ );
            pOrTerm.wtFlags = (u8)( pOrTerm.wtFlags & ~TERM_OR_OK );
            if ( pOrTerm.leftCursor == iCursor )
            {
              /* This is the 2-bit case and we are on the second iteration and
              ** current term is from the first iteration.  So skip this term. */
              Debug.Assert( j == 1 );
              continue;
            }
            if ( ( chngToIN & getMask( pMaskSet, pOrTerm.leftCursor ) ) == 0 )
            {
              /* This term must be of the form t1.a==t2.b where t2 is in the
              ** chngToIN set but t1 is not.  This term will be either preceeded
              ** or follwed by an inverted copy (t2.b==t1.a).  Skip this term
              ** and use its inversion. */
              testcase( pOrTerm.wtFlags & TERM_COPIED );
              testcase( pOrTerm.wtFlags & TERM_VIRTUAL );
              Debug.Assert( ( pOrTerm.wtFlags & ( TERM_COPIED | TERM_VIRTUAL ) ) != 0 );
              continue;
            }
            iColumn = pOrTerm.u.leftColumn;
            iCursor = pOrTerm.leftCursor;
            break;
          }
          if ( i < 0 )
          {
            /* No candidate table+column was found.  This can only occur
            ** on the second iteration */
            Debug.Assert( j == 1 );
            Debug.Assert( ( chngToIN & ( chngToIN - 1 ) ) == 0 );
            Debug.Assert( chngToIN == getMask( pMaskSet, iCursor ) );
            break;
          }
          testcase( j == 1 );

          /* We have found a candidate table and column.  Check to see if that
          ** table and column is common to every term in the OR clause */
          okToChngToIN = 1;
          for ( ; i >= 0 && okToChngToIN != 0 ; i-- )//, pOrTerm++)
          {
            pOrTerm = pOrWc.a[pOrWc.nTerm - 1 - i];
            Debug.Assert( pOrTerm.eOperator == WO_EQ );
            if ( pOrTerm.leftCursor != iCursor )
            {
              pOrTerm.wtFlags = (u8)( pOrTerm.wtFlags & ~TERM_OR_OK );
            }
            else if ( pOrTerm.u.leftColumn != iColumn )
            {
              okToChngToIN = 0;
            }
            else
            {
              int affLeft, affRight;
              /* If the right-hand side is also a column, then the affinities
              ** of both right and left sides must be such that no type
              ** conversions are required on the right.  (Ticket #2249)
              */
              affRight = sqlite3ExprAffinity( pOrTerm.pExpr.pRight );
              affLeft = sqlite3ExprAffinity( pOrTerm.pExpr.pLeft );
              if ( affRight != 0 && affRight != affLeft )
              {
                okToChngToIN = 0;
              }
              else
              {
                pOrTerm.wtFlags |= TERM_OR_OK;
              }
            }
          }
        }

        /* At this point, okToChngToIN is true if original pTerm satisfies
        ** case 1.  In that case, construct a new virtual term that is
        ** pTerm converted into an IN operator.
        */
        if ( okToChngToIN != 0 )
        {
          Expr pDup;            /* A transient duplicate expression */
          ExprList pList = null;   /* The RHS of the IN operator */
          Expr pLeft = null;       /* The LHS of the IN operator */
          Expr pNew;            /* The complete IN operator */

          for ( i = pOrWc.nTerm - 1 ; i >= 0 ; i-- )//, pOrTerm++)
          {
            pOrTerm = pOrWc.a[pOrWc.nTerm - 1 - i];
            if ( ( pOrTerm.wtFlags & TERM_OR_OK ) == 0 ) continue;
            Debug.Assert( pOrTerm.eOperator == WO_EQ );
            Debug.Assert( pOrTerm.leftCursor == iCursor );
            Debug.Assert( pOrTerm.u.leftColumn == iColumn );
            pDup = sqlite3ExprDup( db, pOrTerm.pExpr.pRight, 0 );
            pList = sqlite3ExprListAppend( pWC.pParse, pList, pDup );
            pLeft = pOrTerm.pExpr.pLeft;
          }
          Debug.Assert( pLeft != null );
          pDup = sqlite3ExprDup( db, pLeft, 0 );
          pNew = sqlite3PExpr( pParse, TK_IN, pDup, null, null );
          if ( pNew != null )
          {
            int idxNew;
            transferJoinMarkings( pNew, pExpr );
            Debug.Assert( !ExprHasProperty( pNew, EP_xIsSelect ) );
            pNew.x.pList = pList;
            idxNew = whereClauseInsert( pWC, pNew, TERM_VIRTUAL | TERM_DYNAMIC );
            testcase( idxNew == 0 );
            exprAnalyze( pSrc, pWC, idxNew );
            pTerm = pWC.a[idxTerm];
            pWC.a[idxNew].iParent = idxTerm;
            pTerm.nChild = 1;
          }
          else
          {
            sqlite3ExprListDelete( db, ref pList );
          }
          pTerm.eOperator = 0;  /* case 1 trumps case 2 */
        }
      }
    }
#endif //* !SQLITE_OMIT_OR_OPTIMIZATION && !SQLITE_OMIT_SUBQUERY */


    /*
** The input to this routine is an WhereTerm structure with only the
** "pExpr" field filled in.  The job of this routine is to analyze the
** subexpression and populate all the other fields of the WhereTerm
** structure.
**
** If the expression is of the form "<expr> <op> X" it gets commuted
** to the standard form of "X <op> <expr>".
**
** If the expression is of the form "X <op> Y" where both X and Y are
** columns, then the original expression is unchanged and a new virtual
** term of the form "Y <op> X" is added to the WHERE clause and
** analyzed separately.  The original term is marked with TERM_COPIED
** and the new term is marked with TERM_DYNAMIC (because it's pExpr
** needs to be freed with the WhereClause) and TERM_VIRTUAL (because it
** is a commuted copy of a prior term.)  The original term has nChild=1
** and the copy has idxParent set to the index of the original term.
*/
    static void exprAnalyze(
    SrcList pSrc,            /* the FROM clause */
    WhereClause pWC,         /* the WHERE clause */
    int idxTerm               /* Index of the term to be analyzed */
    )
    {
      WhereTerm pTerm;                /* The term to be analyzed */
      WhereMaskSet pMaskSet;          /* Set of table index masks */
      Expr pExpr;                     /* The expression to be analyzed */
      Bitmask prereqLeft;              /* Prerequesites of the pExpr.pLeft */
      Bitmask prereqAll;               /* Prerequesites of pExpr */
      Bitmask extraRight = 0;
      int nPattern = 0;
      bool isComplete = false;
      bool noCase = false;
      int op;                          /* Top-level operator.  pExpr.op */
      Parse pParse = pWC.pParse;     /* Parsing context */
      sqlite3 db = pParse.db;        /* Data_base connection */

      //if ( db.mallocFailed != 0 )
      //{
      //  return;
      //}
      pTerm = pWC.a[idxTerm];
      pMaskSet = pWC.pMaskSet;
      pExpr = pTerm.pExpr;
      prereqLeft = exprTableUsage( pMaskSet, pExpr.pLeft );
      op = pExpr.op;
      if ( op == TK_IN )
      {
        Debug.Assert( pExpr.pRight == null );
        if ( ExprHasProperty( pExpr, EP_xIsSelect ) )
        {
          pTerm.prereqRight = exprSelectTableUsage( pMaskSet, pExpr.x.pSelect );
        }
        else
        {
          pTerm.prereqRight = exprListTableUsage( pMaskSet, pExpr.x.pList );
        }
      }
      else if ( op == TK_ISNULL )
      {
        pTerm.prereqRight = 0;
      }
      else
      {
        pTerm.prereqRight = exprTableUsage( pMaskSet, pExpr.pRight );
      }
      prereqAll = exprTableUsage( pMaskSet, pExpr );
      if ( ExprHasProperty( pExpr, EP_FromJoin ) )
      {
        Bitmask x = getMask( pMaskSet, pExpr.iRightJoinTable );
        prereqAll |= x;
        extraRight = x - 1;  /* ON clause terms may not be used with an index
** on left table of a LEFT JOIN.  Ticket #3015 */
      }
      pTerm.prereqAll = prereqAll;
      pTerm.leftCursor = -1;
      pTerm.iParent = -1;
      pTerm.eOperator = 0;
      if ( allowedOp( op ) && ( pTerm.prereqRight & prereqLeft ) == 0 )
      {
        Expr pLeft = pExpr.pLeft;
        Expr pRight = pExpr.pRight;
        if ( pLeft.op == TK_COLUMN )
        {
          pTerm.leftCursor = pLeft.iTable;
          pTerm.u.leftColumn = pLeft.iColumn;
          pTerm.eOperator = operatorMask( op );
        }
        if ( pRight != null && pRight.op == TK_COLUMN )
        {
          WhereTerm pNew;
          Expr pDup;
          if ( pTerm.leftCursor >= 0 )
          {
            int idxNew;
            pDup = sqlite3ExprDup( db, pExpr, 0 );
            //if ( db.mallocFailed != 0 )
            //{
            //  sqlite3ExprDelete( db, ref pDup );
            //  return;
            //}
            idxNew = whereClauseInsert( pWC, pDup, TERM_VIRTUAL | TERM_DYNAMIC );
            if ( idxNew == 0 ) return;
            pNew = pWC.a[idxNew];
            pNew.iParent = idxTerm;
            pTerm = pWC.a[idxTerm];
            pTerm.nChild = 1;
            pTerm.wtFlags |= TERM_COPIED;
          }
          else
          {
            pDup = pExpr;
            pNew = pTerm;
          }
          exprCommute( pParse, pDup );
          pLeft = pDup.pLeft;
          pNew.leftCursor = pLeft.iTable;
          pNew.u.leftColumn = pLeft.iColumn;
          pNew.prereqRight = prereqLeft;
          pNew.prereqAll = prereqAll;
          pNew.eOperator = operatorMask( pDup.op );
        }
      }

#if  !SQLITE_OMIT_BETWEEN_OPTIMIZATION
      /* If a term is the BETWEEN operator, create two new virtual terms
** that define the range that the BETWEEN implements.  For example:
**
**      a BETWEEN b AND c
**
** is converted into:
**
**      (a BETWEEN b AND c) AND (a>=b) AND (a<=c)
**
** The two new terms are added onto the end of the WhereClause object.
** The new terms are "dynamic" and are children of the original BETWEEN
** term.  That means that if the BETWEEN term is coded, the children are
** skipped.  Or, if the children are satisfied by an index, the original
** BETWEEN term is skipped.
*/
      else if ( pExpr.op == TK_BETWEEN && pWC.op == TK_AND )
      {
        ExprList pList = pExpr.x.pList;
        int i;
        u8[] ops = new u8[] { TK_GE, TK_LE };
        Debug.Assert( pList != null );
        Debug.Assert( pList.nExpr == 2 );
        for ( i = 0 ; i < 2 ; i++ )
        {
          Expr pNewExpr;
          int idxNew;
          pNewExpr = sqlite3PExpr( pParse, ops[i],
                     sqlite3ExprDup( db, pExpr.pLeft, 0 ),
          sqlite3ExprDup( db, pList.a[i].pExpr, 0 ), null );
          idxNew = whereClauseInsert( pWC, pNewExpr, TERM_VIRTUAL | TERM_DYNAMIC );
          testcase( idxNew == 0 );
          exprAnalyze( pSrc, pWC, idxNew );
          pTerm = pWC.a[idxTerm];
          pWC.a[idxNew].iParent = idxTerm;
        }
        pTerm.nChild = 2;
      }
#endif //* SQLITE_OMIT_BETWEEN_OPTIMIZATION */

#if  !(SQLITE_OMIT_OR_OPTIMIZATION) && !(SQLITE_OMIT_SUBQUERY)
      /* Analyze a term that is composed of two or more subterms connected by
** an OR operator.
*/
      else if ( pExpr.op == TK_OR )
      {
        Debug.Assert( pWC.op == TK_AND );
        exprAnalyzeOrTerm( pSrc, pWC, idxTerm );
	  pTerm = pWC.a[idxTerm];
      }
#endif //* SQLITE_OMIT_OR_OPTIMIZATION */

#if  !SQLITE_OMIT_LIKE_OPTIMIZATION
      /* Add constraints to reduce the search space on a LIKE or GLOB
** operator.
**
** A like pattern of the form "x LIKE 'abc%'" is changed into constraints
**
**          x>='abc' AND x<'abd' AND x LIKE 'abc%'
**
** The last character of the prefix "abc" is incremented to form the
** termination condition "abd".
*/
      if ( isLikeOrGlob( pParse, pExpr, ref nPattern, ref isComplete, ref noCase ) != 0
      && pWC.op == TK_AND )
      {
        Expr pLeft, pRight;
        Expr pStr1, pStr2;
        Expr pNewExpr1, pNewExpr2;
        int idxNew1, idxNew2;

        pLeft = pExpr.x.pList.a[1].pExpr;
        pRight = pExpr.x.pList.a[0].pExpr;
        pStr1 = sqlite3Expr( db, TK_STRING, pRight.u.zToken );
        if ( pStr1 != null ) pStr1.u.zToken = pStr1.u.zToken.Substring( 0, nPattern );

        pStr2 = sqlite3ExprDup( db, pStr1, 0 );
        ////if ( 0 == db.mallocFailed )
        {
          int c, pC;    /* Last character before the first wildcard */
          pC = pStr2.u.zToken[nPattern - 1];// (u8*)&pStr2->token.z[nPattern-1];
          c = pC;
          if ( noCase )
          {
            /* The point is to increment the last character before the first
            ** wildcard.  But if we increment '@', that will push it into the
            ** alphabetic range where case conversions will mess up the
            ** inequality.  To avoid this, make sure to also run the full
            ** LIKE on all candidate expressions by clearing the isComplete flag
            */
            if ( c == 'A' - 1 ) isComplete = false;
            c = sqlite3UpperToLower[c];
          }
          pStr2.u.zToken = pStr2.u.zToken.Substring( 0, nPattern - 1 ) + (char)( c + 1 );// pC = c + 1;
        }
        pNewExpr1 = sqlite3PExpr( pParse, TK_GE, sqlite3ExprDup( db, pLeft, 0 ), pStr1, null );
        idxNew1 = whereClauseInsert( pWC, pNewExpr1, TERM_VIRTUAL | TERM_DYNAMIC );
        testcase( idxNew1 == 0 );
        exprAnalyze( pSrc, pWC, idxNew1 );
        pNewExpr2 = sqlite3PExpr( pParse, TK_LT, sqlite3ExprDup( db, pLeft, 0 ), pStr2, null );
        idxNew2 = whereClauseInsert( pWC, pNewExpr2, TERM_VIRTUAL | TERM_DYNAMIC );
        testcase( idxNew2 == 0 );
        exprAnalyze( pSrc, pWC, idxNew2 );
        pTerm = pWC.a[idxTerm];
        if ( isComplete )
        {
          pWC.a[idxNew1].iParent = idxTerm;
          pWC.a[idxNew2].iParent = idxTerm;
          pTerm.nChild = 2;
        }
      }
#endif //* SQLITE_OMIT_LIKE_OPTIMIZATION */

#if  !SQLITE_OMIT_VIRTUALTABLE
/* Add a WO_MATCH auxiliary term to the constraint set if the
** current expression is of the form:  column MATCH expr.
** This information is used by the xBestIndex methods of
** virtual tables.  The native query optimizer does not attempt
** to do anything with MATCH functions.
*/
if ( isMatchOfColumn( pExpr ) )
{
int idxNew;
Expr pRight, pLeft;
WhereTerm pNewTerm;
Bitmask prereqColumn, prereqExpr;

pRight = pExpr.x.pList.a[0].pExpr;
pLeft = pExpr.x.pList.a[1].pExpr;
prereqExpr = exprTableUsage( pMaskSet, pRight );
prereqColumn = exprTableUsage( pMaskSet, pLeft );
if ( ( prereqExpr & prereqColumn ) == null )
{
Expr pNewExpr;
pNewExpr = sqlite3PExpr(pParse, TK_MATCH,
0, sqlite3ExprDup(db, pRight, 0), 0);
idxNew = whereClauseInsert( pWC, pNewExpr, TERM_VIRTUAL | TERM_DYNAMIC );
testcase( idxNew == 0 );
pNewTerm = pWC.a[idxNew];
pNewTerm.prereqRight = prereqExpr;
pNewTerm.leftCursor = pLeft.iTable;
pNewTerm.u.leftColumn = pLeft.iColumn;
pNewTerm.eOperator = WO_MATCH;
pNewTerm.iParent = idxTerm;
pTerm = pWC.a[idxTerm];
pTerm.nChild = 1;
pTerm.wtFlags |= TERM_COPIED;
pNewTerm.prereqAll = pTerm.prereqAll;
}
}
#endif //* SQLITE_OMIT_VIRTUALTABLE */

      /* Prevent ON clause terms of a LEFT JOIN from being used to drive
** an index for tables to the left of the join.
*/
      pTerm.prereqRight |= extraRight;
    }

    /*
    ** Return TRUE if any of the expressions in pList.a[iFirst...] contain
    ** a reference to any table other than the iBase table.
    */
    static bool referencesOtherTables(
    ExprList pList,          /* Search expressions in ths list */
    WhereMaskSet pMaskSet,   /* Mapping from tables to bitmaps */
    int iFirst,               /* Be searching with the iFirst-th expression */
    int iBase                 /* Ignore references to this table */
    )
    {
      Bitmask allowed = ~getMask( pMaskSet, iBase );
      while ( iFirst < pList.nExpr )
      {
        if ( ( exprTableUsage( pMaskSet, pList.a[iFirst++].pExpr ) & allowed ) != 0 )
        {
          return true;
        }
      }
      return false;
    }


    /*
    ** This routine decides if pIdx can be used to satisfy the ORDER BY
    ** clause.  If it can, it returns 1.  If pIdx cannot satisfy the
    ** ORDER BY clause, this routine returns 0.
    **
    ** pOrderBy is an ORDER BY clause from a SELECT statement.  pTab is the
    ** left-most table in the FROM clause of that same SELECT statement and
    ** the table has a cursor number of "_base".  pIdx is an index on pTab.
    **
    ** nEqCol is the number of columns of pIdx that are used as equality
    ** constraints.  Any of these columns may be missing from the ORDER BY
    ** clause and the match can still be a success.
    **
    ** All terms of the ORDER BY that match against the index must be either
    ** ASC or DESC.  (Terms of the ORDER BY clause past the end of a UNIQUE
    ** index do not need to satisfy this constraint.)  The pbRev value is
    ** set to 1 if the ORDER BY clause is all DESC and it is set to 0 if
    ** the ORDER BY clause is all ASC.
    */
    static bool isSortingIndex(
    Parse pParse,          /* Parsing context */
    WhereMaskSet pMaskSet, /* Mapping from table cursor numbers to bitmaps */
    Index pIdx,            /* The index we are testing */
    int _base,             /* Cursor number for the table to be sorted */
    ExprList pOrderBy,     /* The ORDER BY clause */
    int nEqCol,            /* Number of index columns with == constraints */
    ref int pbRev          /* Set to 1 if ORDER BY is DESC */
    )
    {
      int i, j;                       /* Loop counters */
      int sortOrder = 0;              /* XOR of index and ORDER BY sort direction */
      int nTerm;                      /* Number of ORDER BY terms */
      ExprList_item pTerm;    /* A term of the ORDER BY clause */
      sqlite3 db = pParse.db;

      Debug.Assert( pOrderBy != null );
      nTerm = pOrderBy.nExpr;
      Debug.Assert( nTerm > 0 );

      /* Match terms of the ORDER BY clause against columns of
      ** the index.
      **
      ** Note that indices have pIdx.nColumn regular columns plus
      ** one additional column containing the rowid.  The rowid column
      ** of the index is also allowed to match against the ORDER BY
      ** clause.
      */
      for ( i = j = 0 ; j < nTerm && i <= pIdx.nColumn ; i++ )
      {
        pTerm = pOrderBy.a[j];
        Expr pExpr;        /* The expression of the ORDER BY pTerm */
        CollSeq pColl;     /* The collating sequence of pExpr */
        int termSortOrder; /* Sort order for this term */
        int iColumn;       /* The i-th column of the index.  -1 for rowid */
        int iSortOrder;    /* 1 for DESC, 0 for ASC on the i-th index term */
        string zColl;      /* Name of the collating sequence for i-th index term */

        pExpr = pTerm.pExpr;
        if ( pExpr.op != TK_COLUMN || pExpr.iTable != _base )
        {
          /* Can not use an index sort on anything that is not a column in the
          ** left-most table of the FROM clause */
          break;
        }
        pColl = sqlite3ExprCollSeq( pParse, pExpr );
        if ( null == pColl )
        {
          pColl = db.pDfltColl;
        }
        if ( i < pIdx.nColumn )
        {
          iColumn = pIdx.aiColumn[i];
          if ( iColumn == pIdx.pTable.iPKey )
          {
            iColumn = -1;
          }
          iSortOrder = pIdx.aSortOrder[i];
          zColl = pIdx.azColl[i];
        }
        else
        {
          iColumn = -1;
          iSortOrder = 0;
          zColl = pColl.zName;
        }
        if ( pExpr.iColumn != iColumn || sqlite3StrICmp( pColl.zName, zColl ) != 0 )
        {
          /* Term j of the ORDER BY clause does not match column i of the index */
          if ( i < nEqCol )
          {
            /* If an index column that is constrained by == fails to match an
            ** ORDER BY term, that is OK.  Just ignore that column of the index
            */
            continue;
          }
          else if ( i == pIdx.nColumn )
          {
            /* Index column i is the rowid.  All other terms match. */
            break;
          }
          else
          {
            /* If an index column fails to match and is not constrained by ==
            ** then the index cannot satisfy the ORDER BY constraint.
            */
            return false;
          }
        }
        Debug.Assert( pIdx.aSortOrder != null );
        Debug.Assert( pTerm.sortOrder == 0 || pTerm.sortOrder == 1 );
        Debug.Assert( iSortOrder == 0 || iSortOrder == 1 );
        termSortOrder = iSortOrder ^ pTerm.sortOrder;
        if ( i > nEqCol )
        {
          if ( termSortOrder != sortOrder )
          {
            /* Indices can only be used if all ORDER BY terms past the
            ** equality constraints are all either DESC or ASC. */
            return false;
          }
        }
        else
        {
          sortOrder = termSortOrder;
        }
        j++;
        //pTerm++;
        if ( iColumn < 0 && !referencesOtherTables( pOrderBy, pMaskSet, j, _base ) )
        {
          /* If the indexed column is the primary key and everything matches
          ** so far and none of the ORDER BY terms to the right reference other
          ** tables in the join, then we are Debug.Assured that the index can be used
          ** to sort because the primary key is unique and so none of the other
          ** columns will make any difference
          */
          j = nTerm;
        }
      }

      pbRev = sortOrder != 0 ? 1 : 0;
      if ( j >= nTerm )
      {
        /* All terms of the ORDER BY clause are covered by this index so
        ** this index can be used for sorting. */
        return true;
      }
      if ( pIdx.onError != OE_None && i == pIdx.nColumn
      && !referencesOtherTables( pOrderBy, pMaskSet, j, _base ) )
      {
        /* All terms of this index match some prefix of the ORDER BY clause
        ** and the index is UNIQUE and no terms on the tail of the ORDER BY
        ** clause reference other tables in a join.  If this is all true then
        ** the order by clause is superfluous. */
        return true;
      }
      return false;
    }

    /*
    ** Check table to see if the ORDER BY clause in pOrderBy can be satisfied
    ** by sorting in order of ROWID.  Return true if so and set pbRev to be
    ** true for reverse ROWID and false for forward ROWID order.
    */
    static bool sortableByRowid(
    int _base,             /* Cursor number for table to be sorted */
    ExprList pOrderBy,     /* The ORDER BY clause */
    WhereMaskSet pMaskSet, /* Mapping from table cursors to bitmaps */
    ref int pbRev          /* Set to 1 if ORDER BY is DESC */
    )
    {
      Expr p;

      Debug.Assert( pOrderBy != null );
      Debug.Assert( pOrderBy.nExpr > 0 );
      p = pOrderBy.a[0].pExpr;
      if ( p.op == TK_COLUMN && p.iTable == _base && p.iColumn == -1
      && !referencesOtherTables( pOrderBy, pMaskSet, 1, _base ) )
      {
        pbRev = pOrderBy.a[0].sortOrder;
        return true;
      }
      return false;
    }

    /*
    ** Prepare a crude estimate of the logarithm of the input value.
    ** The results need not be exact.  This is only used for estimating
    ** the total cost of performing operations with O(logN) or O(NlogN)
    ** complexity.  Because N is just a guess, it is no great tragedy if
    ** logN is a little off.
    */
    static double estLog( double N )
    {
      double logN = 1;
      double x = 10;
      while ( N > x )
      {
        logN += 1;
        x *= 10;
      }
      return logN;
    }

    /*
    ** Two routines for printing the content of an sqlite3_index_info
    ** structure.  Used for testing and debugging only.  If neither
    ** SQLITE_TEST or SQLITE_DEBUG are defined, then these routines
    ** are no-ops.
    */
#if  !(SQLITE_OMIT_VIRTUALTABLE) && (SQLITE_DEBUG)
static void TRACE_IDX_INPUTS( sqlite3_index_info p )
{
int i;
if ( !sqlite3WhereTrace ) return;
for ( i = 0 ; i < p.nConstraint ; i++ )
{
sqlite3DebugPrintf( "  constraint[%d]: col=%d termid=%d op=%d usabled=%d\n",
i,
p.aConstraint[i].iColumn,
p.aConstraint[i].iTermOffset,
p.aConstraint[i].op,
p.aConstraint[i].usable );
}
for ( i = 0 ; i < p.nOrderBy ; i++ )
{
sqlite3DebugPrintf( "  orderby[%d]: col=%d desc=%d\n",
i,
p.aOrderBy[i].iColumn,
p.aOrderBy[i].desc );
}
}
static void TRACE_IDX_OUTPUTS( sqlite3_index_info p )
{
int i;
if ( !sqlite3WhereTrace ) return;
for ( i = 0 ; i < p.nConstraint ; i++ )
{
sqlite3DebugPrintf( "  usage[%d]: argvIdx=%d omit=%d\n",
i,
p.aConstraintUsage[i].argvIndex,
p.aConstraintUsage[i].omit );
}
sqlite3DebugPrintf( "  idxNum=%d\n", p.idxNum );
sqlite3DebugPrintf( "  idxStr=%s\n", p.idxStr );
sqlite3DebugPrintf( "  orderByConsumed=%d\n", p.orderByConsumed );
sqlite3DebugPrintf( "  estimatedCost=%g\n", p.estimatedCost );
}
#else
    //#define TRACE_IDX_INPUTS(A)
    //#define TRACE_IDX_OUTPUTS(A)
#endif

    /*
** Required because bestIndex() is called by bestOrClauseIndex()
*/
    //static void bestIndex(
    //Parse*, WhereClause*, struct SrcList_item*, Bitmask, ExprList*, WhereCost*);

    /*
    ** This routine attempts to find an scanning strategy that can be used
    ** to optimize an 'OR' expression that is part of a WHERE clause.
    **
    ** The table associated with FROM clause term pSrc may be either a
    ** regular B-Tree table or a virtual table.
    */
    static void bestOrClauseIndex(
    Parse pParse,               /* The parsing context */
    WhereClause pWC,            /* The WHERE clause */
    SrcList_item pSrc,         /* The FROM clause term to search */
    Bitmask notReady,           /* Mask of cursors that are not available */
    ExprList pOrderBy,          /* The ORDER BY clause */
    WhereCost pCost             /* Lowest cost query plan */
    )
    {
#if !SQLITE_OMIT_OR_OPTIMIZATION
      int iCur = pSrc.iCursor;   /* The cursor of the table to be accessed */
      Bitmask maskSrc = getMask( pWC.pMaskSet, iCur );  /* Bitmask for pSrc */
      WhereTerm pWCEnd = pWC.a[pWC.nTerm];        /* End of pWC.a[] */
      WhereTerm pTerm;                 /* A single term of the WHERE clause */

      /* Search the WHERE clause terms for a usable WO_OR term. */
      for ( int _pt = 0 ; _pt < pWC.nTerm ; _pt++ )//<pWCEnd; pTerm++)
      {
        pTerm = pWC.a[_pt];
        if ( pTerm.eOperator == WO_OR
        && ( ( pTerm.prereqAll & ~maskSrc ) & notReady ) == 0
        && ( pTerm.u.pOrInfo.indexable & maskSrc ) != 0
        )
        {
          WhereClause pOrWC = pTerm.u.pOrInfo.wc;
          WhereTerm pOrWCEnd = pOrWC.a[pOrWC.nTerm];
          WhereTerm pOrTerm;
          int flags = WHERE_MULTI_OR;
          double rTotal = 0;
          double nRow = 0;

          for ( int _pOrWC = 0 ; _pOrWC < pOrWC.nTerm ; _pOrWC++ )//pOrTerm = pOrWC.a ; pOrTerm < pOrWCEnd ; pOrTerm++ )
          {
            pOrTerm = pOrWC.a[_pOrWC];
            WhereCost sTermCost = null;
#if  (SQLITE_TEST) && (SQLITE_DEBUG)
            WHERETRACE( "... Multi-index OR testing for term %d of %d....\n",
            _pOrWC, pOrWC.nTerm - _pOrWC//( pOrTerm - pOrWC.a ), ( pTerm - pWC.a )
            );
#endif
            if ( pOrTerm.eOperator == WO_AND )
            {
              WhereClause pAndWC = pOrTerm.u.pAndInfo.wc;
              bestIndex( pParse, pAndWC, pSrc, notReady, null, ref sTermCost );
            }
            else if ( pOrTerm.leftCursor == iCur )
            {
              WhereClause tempWC = new WhereClause();
              tempWC.pParse = pWC.pParse;
              tempWC.pMaskSet = pWC.pMaskSet;
              tempWC.op = TK_AND;
              tempWC.a = new WhereTerm[2];
              tempWC.a[0] = pOrTerm;
              tempWC.nTerm = 1;
              bestIndex( pParse, tempWC, pSrc, notReady, null, ref sTermCost );
            }
            else
            {
              continue;
            }
            rTotal += sTermCost.rCost;
            nRow += sTermCost.nRow;
            if ( rTotal >= pCost.rCost ) break;
          }

          /* If there is an ORDER BY clause, increase the scan cost to account
          ** for the cost of the sort. */
          if ( pOrderBy != null )
          {
            rTotal += nRow * estLog( nRow );
#if  (SQLITE_TEST) && (SQLITE_DEBUG)
            WHERETRACE( "... sorting increases OR cost to %.9g\n", rTotal );
#endif
          }

          /* If the cost of scanning using this OR term for optimization is
          ** less than the current cost stored in pCost, replace the contents
          ** of pCost. */
#if  (SQLITE_TEST) && (SQLITE_DEBUG)
          WHERETRACE( "... multi-index OR cost=%.9g nrow=%.9g\n", rTotal, nRow );
#endif
          if ( rTotal < pCost.rCost )
          {
            pCost.rCost = rTotal;
            pCost.nRow = nRow;
            pCost.plan.wsFlags = (uint)flags;
            pCost.plan.u.pTerm = pTerm;
          }
        }
      }
#endif //* SQLITE_OMIT_OR_OPTIMIZATION */
    }

#if !SQLITE_OMIT_VIRTUALTABLE
/*
** Allocate and populate an sqlite3_index_info structure. It is the
** responsibility of the caller to eventually release the structure
** by passing the pointer returned by this function to //sqlite3_free().
*/
static sqlite3_index_info *allocateIndexInfo(
Parse *pParse,
WhereClause *pWC,
struct SrcList_item *pSrc,
ExprList *pOrderBy
){
int i, j;
int nTerm;
struct sqlite3_index_constraint *pIdxCons;
struct sqlite3_index_orderby *pIdxOrderBy;
struct sqlite3_index_constraint_usage *pUsage;
WhereTerm *pTerm;
int nOrderBy;
sqlite3_index_info *pIdxInfo;

WHERETRACE("Recomputing index info for %s...\n", pSrc.pTab.zName);

/* Count the number of possible WHERE clause constraints referring
** to this virtual table */
for(i=nTerm=0, pTerm=pWC.a; i<pWC.nTerm; i++, pTerm++){
if( pTerm.leftCursor != pSrc.iCursor ) continue;
Debug.Assert( (pTerm.eOperator&(pTerm.eOperator-1))==null );
testcase( pTerm.eOperator==WO_IN );
testcase( pTerm.eOperator==WO_ISNULL );
if( pTerm.eOperator & (WO_IN|WO_ISNULL) ) continue;
nTerm++;
}

/* If the ORDER BY clause contains only columns in the current
** virtual table then allocate space for the aOrderBy part of
** the sqlite3_index_info structure.
*/
nOrderBy = 0;
if( pOrderBy ){
for(i=0; i<pOrderBy.nExpr; i++){
Expr pExpr = pOrderBy.a[i].pExpr;
if( pExpr.op!=TK_COLUMN || pExpr.iTable!=pSrc.iCursor ) break;
}
if( i==pOrderBy.nExpr ){
nOrderBy = pOrderBy.nExpr;
}
}

/* Allocate the sqlite3_index_info structure
*/
pIdxInfo = new sqlite3_index_info();
//sqlite3DbMallocZero(pParse.db, sizeof(*pIdxInfo)
//+ (sizeof(*pIdxCons) + sizeof(*pUsage))*nTerm
//+ sizeof(*pIdxOrderBy)*nOrderBy );
if( pIdxInfo==null ){
sqlite3ErrorMsg(pParse, "out of memory");
/* (double)0 In case of SQLITE_OMIT_FLOATING_POINT... */
return 0;
}

/* Initialize the structure.  The sqlite3_index_info structure contains
** many fields that are declared "const" to prevent xBestIndex from
** changing them.  We have to do some funky casting in order to
** initialize those fields.
*/
pIdxCons = (sqlite3_index_constraint)pIdxInfo[1];
pIdxOrderBy = (sqlite3_index_orderby)pIdxCons[nTerm];
pUsage = (sqlite3_index_constraint_usage)pIdxOrderBy[nOrderBy];
pIdxInfo.nConstraint = nTerm;
pIdxInfo.nOrderBy = nOrderBy;
pIdxInfo.aConstraint = pIdxCons;
pIdxInfo.aOrderBy = pIdxOrderBy;
pIdxInfo.aConstraintUsage =
pUsage;

for(i=j=0, pTerm=pWC.a; i<pWC.nTerm; i++, pTerm++){
if( pTerm.leftCursor != pSrc.iCursor ) continue;
Debug.Assert( (pTerm.eOperator&(pTerm.eOperator-1))==null );
testcase( pTerm.eOperator==WO_IN );
testcase( pTerm.eOperator==WO_ISNULL );
if( pTerm.eOperator & (WO_IN|WO_ISNULL) ) continue;
pIdxCons[j].iColumn = pTerm.u.leftColumn;
pIdxCons[j].iTermOffset = i;
pIdxCons[j].op = (u8)pTerm.eOperator;
/* The direct Debug.Assignment in the previous line is possible only because
** the WO_ and SQLITE_INDEX_CONSTRAINT_ codes are identical.  The
** following Debug.Asserts verify this fact. */
Debug.Assert( WO_EQ==SQLITE_INDEX_CONSTRAINT_EQ );
Debug.Assert( WO_LT==SQLITE_INDEX_CONSTRAINT_LT );
Debug.Assert( WO_LE==SQLITE_INDEX_CONSTRAINT_LE );
Debug.Assert( WO_GT==SQLITE_INDEX_CONSTRAINT_GT );
Debug.Assert( WO_GE==SQLITE_INDEX_CONSTRAINT_GE );
Debug.Assert( WO_MATCH==SQLITE_INDEX_CONSTRAINT_MATCH );
Debug.Assert( pTerm.eOperator & (WO_EQ|WO_LT|WO_LE|WO_GT|WO_GE|WO_MATCH) );
j++;
}
for(i=0; i<nOrderBy; i++){
Expr pExpr = pOrderBy.a[i].pExpr;
pIdxOrderBy[i].iColumn = pExpr.iColumn;
pIdxOrderBy[i].desc = pOrderBy.a[i].sortOrder;
}

return pIdxInfo;
}

/*
** The table object reference passed as the second argument to this function
** must represent a virtual table. This function invokes the xBestIndex()
** method of the virtual table with the sqlite3_index_info pointer passed
** as the argument.
**
** If an error occurs, pParse is populated with an error message and a
** non-zero value is returned. Otherwise, 0 is returned and the output
** part of the sqlite3_index_info structure is left populated.
**
** Whether or not an error is returned, it is the responsibility of the
** caller to eventually free p.idxStr if p.needToFreeIdxStr indicates
** that this is required.
*/
static int vtabBestIndex(Parse *pParse, Table *pTab, sqlite3_index_info *p){
sqlite3_vtab *pVtab = sqlite3GetVTable(pParse->db, pTab)->pVtab;
int i;
int rc;

(void)sqlite3SafetyOff(pParse.db);
WHERETRACE("xBestIndex for %s\n", pTab.zName);
TRACE_IDX_INPUTS(p);
rc = pVtab.pModule.xBestIndex(pVtab, p);
TRACE_IDX_OUTPUTS(p);
(void)sqlite3SafetyOn(pParse.db);

if( rc!=SQLITE_OK ){
if( rc==SQLITE_NOMEM ){
pParse.db.mallocFailed = 1;
}else if( !pVtab.zErrMsg ){
sqlite3ErrorMsg(pParse, "%s", sqlite3ErrStr(rc));
}else{
sqlite3ErrorMsg(pParse, "%s", pVtab.zErrMsg);
}
}
//sqlite3DbFree(pParse.db, pVtab.zErrMsg);
pVtab.zErrMsg = 0;

for(i=0; i<p.nConstraint; i++){
if( !p.aConstraint[i].usable && p.aConstraintUsage[i].argvIndex>0 ){
sqlite3ErrorMsg(pParse,
"table %s: xBestIndex returned an invalid plan", pTab.zName);
}
}

return pParse.nErr;
}


/*
** Compute the best index for a virtual table.
**
** The best index is computed by the xBestIndex method of the virtual
** table module.  This routine is really just a wrapper that sets up
** the sqlite3_index_info structure that is used to communicate with
** xBestIndex.
**
** In a join, this routine might be called multiple times for the
** same virtual table.  The sqlite3_index_info structure is created
** and initialized on the first invocation and reused on all subsequent
** invocations.  The sqlite3_index_info structure is also used when
** code is generated to access the virtual table.  The whereInfoDelete()
** routine takes care of freeing the sqlite3_index_info structure after
** everybody has finished with it.
*/
static void bestVirtualIndex(
Parse *pParse,                  /* The parsing context */
WhereClause *pWC,               /* The WHERE clause */
struct SrcList_item *pSrc,      /* The FROM clause term to search */
Bitmask notReady,               /* Mask of cursors that are not available */
ExprList *pOrderBy,             /* The order by clause */
WhereCost *pCost,               /* Lowest cost query plan */
sqlite3_index_info **ppIdxInfo  /* Index information passed to xBestIndex */
){
Table *pTab = pSrc.pTab;
sqlite3_index_info *pIdxInfo;
struct sqlite3_index_constraint *pIdxCons;
struct sqlite3_index_constraint_usage *pUsage;
WhereTerm *pTerm;
int i, j;
int nOrderBy;

/* Make sure wsFlags is initialized to some sane value. Otherwise, if the
** malloc in allocateIndexInfo() fails and this function returns leaving
** wsFlags in an uninitialized state, the caller may behave unpredictably.
*/
memset(pCost, 0, sizeof(*pCost));
pCost.plan.wsFlags = WHERE_VIRTUALTABLE;

/* If the sqlite3_index_info structure has not been previously
** allocated and initialized, then allocate and initialize it now.
*/
pIdxInfo = *ppIdxInfo;
if( pIdxInfo==0 ){
*ppIdxInfo = pIdxInfo = allocateIndexInfo(pParse, pWC, pSrc, pOrderBy);
}
if( pIdxInfo==0 ){
return;
}

/* At this point, the sqlite3_index_info structure that pIdxInfo points
** to will have been initialized, either during the current invocation or
** during some prior invocation.  Now we just have to customize the
** details of pIdxInfo for the current invocation and pDebug.Ass it to
** xBestIndex.
*/

/* The module name must be defined. Also, by this point there must
** be a pointer to an sqlite3_vtab structure. Otherwise
** sqlite3ViewGetColumnNames() would have picked up the error.
*/
Debug.Assert( pTab.azModuleArg && pTab.azModuleArg[0] );
Debug.Assert( sqlite3GetVTable(pParse.db, pTab) );

/* Set the aConstraint[].usable fields and initialize all
** output variables to zero.
**
** aConstraint[].usable is true for constraints where the right-hand
** side contains only references to tables to the left of the current
** table.  In other words, if the constraint is of the form:
**
**           column = expr
**
** and we are evaluating a join, then the constraint on column is
** only valid if all tables referenced in expr occur to the left
** of the table containing column.
**
** The aConstraints[] array contains entries for all constraints
** on the current table.  That way we only have to compute it once
** even though we might try to pick the best index multiple times.
** For each attempt at picking an index, the order of tables in the
** join might be different so we have to recompute the usable flag
** each time.
*/
pIdxCons = pIdxInfo.aConstraint;
pUsage = pIdxInfo.aConstraintUsage;
for(i=0; i<pIdxInfo.nConstraint; i++, pIdxCons++){
j = pIdxCons.iTermOffset;
pTerm = pWC.a[j];
pIdxCons.usable =  (pTerm.prereqRight & notReady)==null ?1:0;
pUsage[i] = new sqlite3_index_constraint_usage();
}
// memset(pUsage, 0, sizeof(pUsage[0])*pIdxInfo.nConstraint);
if( pIdxInfo.needToFreeIdxStr ){
//sqlite3_free(pIdxInfo.idxStr);
}
pIdxInfo.idxStr = 0;
pIdxInfo.idxNum = 0;
pIdxInfo.needToFreeIdxStr = 0;
pIdxInfo.orderByConsumed = 0;
/* ((double)2) In case of SQLITE_OMIT_FLOATING_POINT... */
pIdxInfo.estimatedCost = SQLITE_BIG_DBL / ((double)2);
nOrderBy = pIdxInfo.nOrderBy;
if( !pOrderBy ){
pIdxInfo.nOrderBy = 0;
}

if( vtabBestIndex(pParse, pTab, pIdxInfo) ){
return;
}

/* The cost is not allowed to be larger than SQLITE_BIG_DBL (the
** inital value of lowestCost in this loop. If it is, then the
** (cost<lowestCost) test below will never be true.
**
** Use "(double)2" instead of "2.0" in case OMIT_FLOATING_POINT
** is defined.
*/
if( (SQLITE_BIG_DBL/((double)2))<pIdxInfo.estimatedCost ){
pCost.rCost = (SQLITE_BIG_DBL/((double)2));
}else{
pCost.rCost = pIdxInfo.estimatedCost;
}
pCost.plan.u.pVtabIdx = pIdxInfo;
if( pIdxInfo->orderByConsumed ){
pCost.plan.wsFlags |= WHERE_ORDERBY;
}
pCost.plan.nEq = 0;
pIdxInfo.nOrderBy = nOrderBy;

/* Try to find a more efficient access pattern by using multiple indexes
** to optimize an OR expression within the WHERE clause.
*/
bestOrClauseIndex(pParse, pWC, pSrc, notReady, pOrderBy, pCost);
}
#endif //* SQLITE_OMIT_VIRTUALTABLE */

    /*
** Find the query plan for accessing a particular table.  Write the
** best query plan and its cost into the WhereCost object supplied as the
** last parameter.
**
** The lowest cost plan wins.  The cost is an estimate of the amount of
** CPU and disk I/O need to process the request using the selected plan.
** Factors that influence cost include:
**
**    *  The estimated number of rows that will be retrieved.  (The
**       fewer the better.)
**
**    *  Whether or not sorting must occur.
**
**    *  Whether or not there must be separate lookups in the
**       index and in the main table.
**
** If there was an INDEXED BY clause (pSrc.pIndex) attached to the table in
** the SQL statement, then this function only considers plans using the
** named index. If no such plan is found, then the returned cost is
** SQLITE_BIG_DBL. If a plan is found that uses the named index,
** then the cost is calculated in the usual way.
**
** If a NOT INDEXED clause (pSrc.notIndexed!=0) was attached to the table
** in the SELECT statement, then no indexes are considered. However, the
** selected plan may still take advantage of the tables built-in rowid
** index.
*/
    static void bestBtreeIndex(
    Parse pParse,              /* The parsing context */
    WhereClause pWC,           /* The WHERE clause */
    SrcList_item pSrc,         /* The FROM clause term to search */
    Bitmask notReady,          /* Mask of cursors that are not available */
    ExprList pOrderBy,         /* The ORDER BY clause */
    ref WhereCost pCost            /* Lowest cost query plan */
    )
    {
      WhereTerm pTerm;           /* A single term of the WHERE clause */
      int iCur = pSrc.iCursor;   /* The cursor of the table to be accessed */
      Index pProbe;              /* An index we are evaluating */
      int rev = 0;                 /* True to scan in reverse order */
      u32 wsFlags;               /* Flags Debug.Associated with pProbe */
      int nEq;                   /* Number of == or IN constraints */
      u32 eqTermMask;            /* Mask of valid equality operators */
      double cost;               /* Cost of using pProbe */
      double nRow;               /* Estimated number of rows in result set */
      int i;                     /* Loop counter */

      WHERETRACE( "bestIndex: tbl=%s notReady=%llx\n", pSrc.pTab.zName, notReady );
      pProbe = pSrc.pTab.pIndex;
      if ( pSrc.notIndexed != 0 )
      {
        pProbe = null;
      }

      /* If the table has no indices and there are no terms in the where
      ** clause that refer to the ROWID, then we will never be able to do
      ** anything other than a full table scan on this table.  We might as
      ** well put it first in the join order.  That way, perhaps it can be
      ** referenced by other tables in the join.
      */
      pCost = new WhereCost();//memset(pCost, 0, sizeof(*pCost));
      if ( pProbe == null &&
      findTerm( pWC, iCur, -1, 0, WO_EQ | WO_IN | WO_LT | WO_LE | WO_GT | WO_GE, null ) == null &&
      ( pOrderBy == null || !sortableByRowid( iCur, pOrderBy, pWC.pMaskSet, ref rev ) ) )
      {
        if ( ( pParse.db.flags & SQLITE_ReverseOrder ) != 0 )
        {
          /* For application testing, randomly reverse the output order for
          ** SELECT statements that omit the ORDER BY clause.  This will help
          ** to find cases where
          */
          pCost.plan.wsFlags |= WHERE_REVERSE;
        }
        return;
      }
      pCost.rCost = SQLITE_BIG_DBL;

      /* Check for a rowid=EXPR or rowid IN (...) constraints. If there was
      ** an INDEXED BY clause attached to this table, skip this step.
      */
      if ( null == pSrc.pIndex )
      {
        pTerm = findTerm( pWC, iCur, -1, notReady, WO_EQ | WO_IN, null );
        if ( pTerm != null )
        {
          Expr pExpr;
          pCost.plan.wsFlags = WHERE_ROWID_EQ;
          if ( ( pTerm.eOperator & WO_EQ ) != 0 )
          {
            /* Rowid== is always the best pick.  Look no further.  Because only
            ** a single row is generated, output is always in sorted order */
            pCost.plan.wsFlags = WHERE_ROWID_EQ | WHERE_UNIQUE;
            pCost.plan.nEq = 1;
            WHERETRACE( "... best is rowid\n" );
            pCost.rCost = 0;
            pCost.nRow = 1;
            return;
          }
          else if ( !ExprHasProperty( ( pExpr = pTerm.pExpr ), EP_xIsSelect )
          && pExpr.x.pList != null
          )
          {
            /* Rowid IN (LIST): cost is NlogN where N is the number of list
            ** elements.  */
            pCost.rCost = pCost.nRow = pExpr.x.pList.nExpr;
            pCost.rCost *= estLog( pCost.rCost );
          }
          else
          {
            /* Rowid IN (SELECT): cost is NlogN where N is the number of rows
            ** in the result of the inner select.  We have no way to estimate
            ** that value so make a wild guess. */
            pCost.nRow = 100;
            pCost.rCost = 200;
          }
          WHERETRACE( "... rowid IN cost: %.9g\n", pCost.rCost );
        }

        /* Estimate the cost of a table scan.  If we do not know how many
        ** entries are in the table, use 1 million as a guess.
        */
        cost = pProbe != null ? pProbe.aiRowEst[0] : 1000000;
        WHERETRACE( "... table scan _base cost: %.9g\n", cost );
        wsFlags = WHERE_ROWID_RANGE;

        /* Check for constraints on a range of rowids in a table scan.
        */
        pTerm = findTerm( pWC, iCur, -1, notReady, WO_LT | WO_LE | WO_GT | WO_GE, null );
        if ( pTerm != null )
        {
          if ( findTerm( pWC, iCur, -1, notReady, WO_LT | WO_LE, null ) != null )
          {
            wsFlags |= WHERE_TOP_LIMIT;
            cost /= 3;  /* Guess that rowid<EXPR eliminates two-thirds of rows */
          }
          if ( findTerm( pWC, iCur, -1, notReady, WO_GT | WO_GE, null ) != null )
          {
            wsFlags |= WHERE_BTM_LIMIT;
            cost /= 3;  /* Guess that rowid>EXPR eliminates two-thirds of rows */
          }
          WHERETRACE( "... rowid range reduces cost to %.9g\n", cost );
        }
        else
        {
          wsFlags = 0;
        }
        nRow = cost;

        /* If the table scan does not satisfy the ORDER BY clause, increase
        ** the cost by NlogN to cover the expense of sorting. */
        if ( pOrderBy != null )
        {
          if ( sortableByRowid( iCur, pOrderBy, pWC.pMaskSet, ref rev ) )
          {
            wsFlags |= WHERE_ORDERBY | WHERE_ROWID_RANGE;
            if ( rev != 0 )
            {
              wsFlags |= WHERE_REVERSE;
            }
          }
          else
          {
            cost += cost * estLog( cost );
            WHERETRACE( "... sorting increases cost to %.9g\n", cost );
          }
        }
        else if ( ( pParse.db.flags & SQLITE_ReverseOrder ) != 0 )
        {
          /* For application testing, randomly reverse the output order for
          ** SELECT statements that omit the ORDER BY clause.  This will help
          ** to find cases where
          */
          wsFlags |= WHERE_REVERSE;
        }
        /* Remember this case if it is the best so far */
        if ( cost < pCost.rCost )
        {
          pCost.rCost = cost;
          pCost.nRow = nRow;
          pCost.plan.wsFlags = wsFlags;
        }
      }

      bestOrClauseIndex( pParse, pWC, pSrc, notReady, pOrderBy, pCost );

      /* If the pSrc table is the right table of a LEFT JOIN then we may not
      ** use an index to satisfy IS NULL constraints on that table.  This is
      ** because columns might end up being NULL if the table does not match -
      ** a circumstance which the index cannot help us discover.  Ticket #2177.
      */
      if ( ( pSrc.jointype & JT_LEFT ) != 0 )
      {
        eqTermMask = WO_EQ | WO_IN;
      }
      else
      {
        eqTermMask = WO_EQ | WO_IN | WO_ISNULL;
      }

      /* Look at each index.
      */
      if ( pSrc.pIndex != null )
      {
        pProbe = pSrc.pIndex;
      }
      for ( ; pProbe != null ; pProbe = ( pSrc.pIndex != null ? null : pProbe.pNext ) )
      {
        double inMultiplier = 1;  /* Number of equality look-ups needed */
        int inMultIsEst = 0;      /* True if inMultiplier is an estimate */

#if (SQLITE_TEST) && (SQLITE_DEBUG)
        WHERETRACE( "... index %s:\n", pProbe.zName );
#endif

        /* Count the number of columns in the index that are satisfied
** by x=EXPR or x IS NULL constraints or x IN (...) constraints.
** For a term of the form x=EXPR or x IS NULL we only have to do
** a single binary search.  But for x IN (...) we have to do a
** number of binary searched
** equal to the number of entries on the RHS of the IN operator.
** The inMultipler variable with try to estimate the number of
** binary searches needed.
*/
        wsFlags = 0;
        for ( i = 0 ; i < pProbe.nColumn ; i++ )
        {
          int j = pProbe.aiColumn[i];
          pTerm = findTerm( pWC, iCur, j, notReady, (uint)eqTermMask, pProbe );
          if ( pTerm == null ) break;
          wsFlags |= WHERE_COLUMN_EQ;
          if ( ( pTerm.eOperator & WO_IN ) != 0 )
          {
            Expr pExpr = pTerm.pExpr;
            wsFlags |= WHERE_COLUMN_IN;
            if ( ExprHasProperty( pExpr, EP_xIsSelect ) )
            {
              inMultiplier *= 25;
              inMultIsEst = 1;
            }
            else if ( pExpr.x.pList != null )
            {
              inMultiplier *= pExpr.x.pList.nExpr + 1;
            }
          }
          else if ( ( pTerm.eOperator & WO_ISNULL ) != 0 )
          {
            wsFlags |= WHERE_COLUMN_NULL;
          }
        }
        nRow = pProbe.aiRowEst[i] * inMultiplier;
        /* If inMultiplier is an estimate and that estimate results in an
        ** nRow it that is more than half number of rows in the table,
        ** then reduce inMultipler */
        if ( inMultIsEst != 0 && nRow * 2 > pProbe.aiRowEst[0] )
        {
          nRow = pProbe.aiRowEst[0] / 2;
          inMultiplier = nRow / pProbe.aiRowEst[i];
        }
        cost = nRow + inMultiplier * estLog( pProbe.aiRowEst[0] );
        nEq = i;
        if ( pProbe.onError != OE_None && nEq == pProbe.nColumn )
        {
          testcase( wsFlags & WHERE_COLUMN_IN );
          testcase( wsFlags & WHERE_COLUMN_NULL );
          if ( ( wsFlags & ( WHERE_COLUMN_IN | WHERE_COLUMN_NULL ) ) == 0 )
          {
            wsFlags |= WHERE_UNIQUE;
          }
        }
#if (SQLITE_TEST) && (SQLITE_DEBUG)
        WHERETRACE( "...... nEq=%d inMult=%.9g nRow=%.9g cost=%.9g\n",
        nEq, inMultiplier, nRow, cost );
#endif

        /* Look for range constraints.  Assume that each range constraint
** makes the search space 1/3rd smaller.
*/
        if ( nEq < pProbe.nColumn )
        {
          int j = pProbe.aiColumn[nEq];
          pTerm = findTerm( pWC, iCur, j, notReady, WO_LT | WO_LE | WO_GT | WO_GE, pProbe );
          if ( pTerm != null )
          {
            wsFlags |= WHERE_COLUMN_RANGE;
            if ( findTerm( pWC, iCur, j, notReady, WO_LT | WO_LE, pProbe ) != null )
            {
              wsFlags |= WHERE_TOP_LIMIT;
              cost /= 3;
              nRow /= 3;
            }
            if ( findTerm( pWC, iCur, j, notReady, WO_GT | WO_GE, pProbe ) != null )
            {
              wsFlags |= WHERE_BTM_LIMIT;
              cost /= 3;
              nRow /= 3;
            }
#if (SQLITE_TEST) && (SQLITE_DEBUG)
            WHERETRACE( "...... range reduces nRow to %.9g and cost to %.9g\n",
            nRow, cost );
#endif
          }
        }

        /* Add the additional cost of sorting if that is a factor.
        */
        if ( pOrderBy != null )
        {
          if ( ( wsFlags & ( WHERE_COLUMN_IN | WHERE_COLUMN_NULL ) ) == 0
          && isSortingIndex( pParse, pWC.pMaskSet, pProbe, iCur, pOrderBy, nEq, ref rev )
          )
          {
            if ( wsFlags == 0 )
            {
              wsFlags = WHERE_COLUMN_RANGE;
            }
            wsFlags |= WHERE_ORDERBY;
            if ( rev != 0 )
            {
              wsFlags |= WHERE_REVERSE;
            }
          }
          else
          {
            cost += cost * estLog( cost );
#if (SQLITE_TEST) && (SQLITE_DEBUG)
            WHERETRACE( "...... orderby increases cost to %.9g\n", cost );
#endif
          }
        }
        else if ( wsFlags != 0 && ( pParse.db.flags & SQLITE_ReverseOrder ) != 0 )
        {
          /* For application testing, randomly reverse the output order for
          ** SELECT statements that omit the ORDER BY clause.  This will help
          ** to find cases where
          */
          wsFlags |= WHERE_REVERSE;
        }

        /* Check to see if we can get away with using just the index without
        ** ever reading the table.  If that is the case, then halve the
        ** cost of this index.
        */
        if ( wsFlags != 0 && pSrc.colUsed < ( ( (Bitmask)1 ) << ( BMS - 1 ) ) )
        {
          Bitmask m = pSrc.colUsed;
          int j;
          for ( j = 0 ; j < pProbe.nColumn ; j++ )
          {
            int x = pProbe.aiColumn[j];
            if ( x < BMS - 1 )
            {
              m &= ~( ( (Bitmask)1 ) << x );
            }
          }
          if ( m == 0 )
          {
            wsFlags |= WHERE_IDX_ONLY;
            cost /= 2;
            WHERETRACE( "...... idx-only reduces cost to %.9g\n", cost );
          }
        }

        /* If this index has achieved the lowest cost so far, then use it.
        */
        if ( wsFlags != 0 && cost < pCost.rCost )
        {
          pCost.rCost = cost;
          pCost.nRow = nRow;
          pCost.plan.wsFlags = wsFlags;
          pCost.plan.nEq = (u32)nEq;
          Debug.Assert( ( pCost.plan.wsFlags & WHERE_INDEXED ) != 0 );
          pCost.plan.u.pIdx = pProbe;
        }
      }

      /* Report the best result
      */
      pCost.plan.wsFlags = (u32)( pCost.plan.wsFlags | eqTermMask );
      WHERETRACE( "best index is %s, nrow=%.9g, cost=%.9g, wsFlags=%x, nEq=%d\n",
      ( pCost.plan.wsFlags & WHERE_INDEXED ) != 0 ?
      pCost.plan.u.pIdx.zName : "(none)", pCost.nRow,
      pCost.rCost, pCost.plan.wsFlags, pCost.plan.nEq );
    }

    /*
    ** Find the query plan for accessing table pSrc.pTab. Write the
    ** best query plan and its cost into the WhereCost object supplied
    ** as the last parameter. This function may calculate the cost of
    ** both real and virtual table scans.
    */
    static void bestIndex(
    Parse pParse,               /* The parsing context */
    WhereClause pWC,            /* The WHERE clause */
    SrcList_item pSrc,          /* The FROM clause term to search */
    Bitmask notReady,           /* Mask of cursors that are not available */
    ExprList pOrderBy,          /* The ORDER BY clause */
    ref WhereCost pCost         /* Lowest cost query plan */
    )
    {
#if !SQLITE_OMIT_VIRTUALTABLE
if ( IsVirtual( pSrc.pTab ) )
{
sqlite3_index_info p = null;
bestVirtualIndex(pParse, pWC, pSrc, notReady, pOrderBy, pCost, p);
if( p.needToFreeIdxStr !=0){
//sqlite3_free(ref p.idxStr);
}
//sqlite3DbFree(pParse.db, p);
}
else
#endif
      {
        bestBtreeIndex( pParse, pWC, pSrc, notReady, pOrderBy, ref pCost );
      }
    }

    /*
    ** Disable a term in the WHERE clause.  Except, do not disable the term
    ** if it controls a LEFT OUTER JOIN and it did not originate in the ON
    ** or USING clause of that join.
    **
    ** Consider the term t2.z='ok' in the following queries:
    **
    **   (1)  SELECT * FROM t1 LEFT JOIN t2 ON t1.a=t2.x WHERE t2.z='ok'
    **   (2)  SELECT * FROM t1 LEFT JOIN t2 ON t1.a=t2.x AND t2.z='ok'
    **   (3)  SELECT * FROM t1, t2 WHERE t1.a=t2.x AND t2.z='ok'
    **
    ** The t2.z='ok' is disabled in the in (2) because it originates
    ** in the ON clause.  The term is disabled in (3) because it is not part
    ** of a LEFT OUTER JOIN.  In (1), the term is not disabled.
    **
    ** Disabling a term causes that term to not be tested in the inner loop
    ** of the join.  Disabling is an optimization.  When terms are satisfied
    ** by indices, we disable them to prevent redundant tests in the inner
    ** loop.  We would get the correct results if nothing were ever disabled,
    ** but joins might run a little slower.  The trick is to disable as much
    ** as we can without disabling too much.  If we disabled in (1), we'd get
    ** the wrong answer.  See ticket #813.
    */
    static void disableTerm( WhereLevel pLevel, WhereTerm pTerm )
    {
      if ( pTerm != null
      && ALWAYS( ( pTerm.wtFlags & TERM_CODED ) == 0 )
      && ( pLevel.iLeftJoin == 0 || ExprHasProperty( pTerm.pExpr, EP_FromJoin ) ) )
      {
        pTerm.wtFlags |= TERM_CODED;
        if ( pTerm.iParent >= 0 )
        {
          WhereTerm pOther = pTerm.pWC.a[pTerm.iParent];
          if ( ( --pOther.nChild ) == 0 )
          {
            disableTerm( pLevel, pOther );
          }
        }
      }
    }

    /*
    ** Apply the affinities Debug.Associated with the first n columns of index
    ** pIdx to the values in the n registers starting at _base.
    */
    static void codeApplyAffinity( Parse pParse, int _base, int n, Index pIdx )
    {
      if ( n > 0 )
      {
        Vdbe v = pParse.pVdbe;
        Debug.Assert( v != null );
        sqlite3VdbeAddOp2( v, OP_Affinity, _base, n );
        sqlite3IndexAffinityStr( v, pIdx );
        sqlite3ExprCacheAffinityChange( pParse, _base, n );
      }
    }


    /*
    ** Generate code for a single equality term of the WHERE clause.  An equality
    ** term can be either X=expr or X IN (...).   pTerm is the term to be
    ** coded.
    **
    ** The current value for the constraint is left in register iReg.
    **
    ** For a constraint of the form X=expr, the expression is evaluated and its
    ** result is left on the stack.  For constraints of the form X IN (...)
    ** this routine sets up a loop that will iterate over all values of X.
    */
    static int codeEqualityTerm(
    Parse pParse,      /* The parsing context */
    WhereTerm pTerm,   /* The term of the WHERE clause to be coded */
    WhereLevel pLevel, /* When level of the FROM clause we are working on */
    int iTarget         /* Attempt to leave results in this register */
    )
    {
      Expr pX = pTerm.pExpr;
      Vdbe v = pParse.pVdbe;
      int iReg;                  /* Register holding results */

      Debug.Assert( iTarget > 0 );
      if ( pX.op == TK_EQ )
      {
        iReg = sqlite3ExprCodeTarget( pParse, pX.pRight, iTarget );
      }
      else if ( pX.op == TK_ISNULL )
      {
        iReg = iTarget;
        sqlite3VdbeAddOp2( v, OP_Null, 0, iReg );
#if  !SQLITE_OMIT_SUBQUERY
      }
      else
      {
        int eType;
        int iTab;
        InLoop pIn;

        Debug.Assert( pX.op == TK_IN );
        iReg = iTarget;
        int iDummy = -1; eType = sqlite3FindInIndex( pParse, pX, ref iDummy );
        iTab = pX.iTable;
        sqlite3VdbeAddOp2( v, OP_Rewind, iTab, 0 );
        Debug.Assert( ( pLevel.plan.wsFlags & WHERE_IN_ABLE ) != 0 );
        if ( pLevel.u._in.nIn == 0 )
        {
          pLevel.addrNxt = sqlite3VdbeMakeLabel( v );
        }
        pLevel.u._in.nIn++;
        if ( pLevel.u._in.aInLoop == null ) pLevel.u._in.aInLoop = new InLoop[pLevel.u._in.nIn];
        else Array.Resize( ref pLevel.u._in.aInLoop, pLevel.u._in.nIn );
        //sqlite3DbReallocOrFree(pParse.db, pLevel.u._in.aInLoop,
        //                       sizeof(pLevel.u._in.aInLoop[0])*pLevel.u._in.nIn);
        //pIn = pLevel.u._in.aInLoop;
        if ( pLevel.u._in.aInLoop != null )//(pIn )
        {
          pLevel.u._in.aInLoop[pLevel.u._in.nIn - 1] = new InLoop();
          pIn = pLevel.u._in.aInLoop[pLevel.u._in.nIn - 1];//pIn++
          pIn.iCur = iTab;
          if ( eType == IN_INDEX_ROWID )
          {
            pIn.addrInTop = sqlite3VdbeAddOp2( v, OP_Rowid, iTab, iReg );
          }
          else
          {
            pIn.addrInTop = sqlite3VdbeAddOp3( v, OP_Column, iTab, 0, iReg );
          }
          sqlite3VdbeAddOp1( v, OP_IsNull, iReg );
        }
        else
        {
          pLevel.u._in.nIn = 0;
        }
#endif
      }
      disableTerm( pLevel, pTerm );
      return iReg;
    }

    /*
    ** Generate code that will evaluate all == and IN constraints for an
    ** index.  The values for all constraints are left on the stack.
    **
    ** For example, consider table t1(a,b,c,d,e,f) with index i1(a,b,c).
    ** Suppose the WHERE clause is this:  a==5 AND b IN (1,2,3) AND c>5 AND c<10
    ** The index has as many as three equality constraints, but in this
    ** example, the third "c" value is an inequality.  So only two
    ** constraints are coded.  This routine will generate code to evaluate
    ** a==5 and b IN (1,2,3).  The current values for a and b will be stored
    ** in consecutive registers and the index of the first register is returned.
    **
    ** In the example above nEq==2.  But this subroutine works for any value
    ** of nEq including 0.  If nEq==null, this routine is nearly a no-op.
    ** The only thing it does is allocate the pLevel.iMem memory cell.
    **
    ** This routine always allocates at least one memory cell and returns
    ** the index of that memory cell. The code that
    ** calls this routine will use that memory cell to store the termination
    ** key value of the loop.  If one or more IN operators appear, then
    ** this routine allocates an additional nEq memory cells for internal
    ** use.
    */
    static int codeAllEqualityTerms(
    Parse pParse,        /* Parsing context */
    WhereLevel pLevel,   /* Which nested loop of the FROM we are coding */
    WhereClause pWC,     /* The WHERE clause */
    Bitmask notReady,     /* Which parts of FROM have not yet been coded */
    int nExtraReg         /* Number of extra registers to allocate */
    )
    {
      int nEq = (int)pLevel.plan.nEq;   /* The number of == or IN constraints to code */
      Vdbe v = pParse.pVdbe;      /* The vm under construction */
      Index pIdx;                  /* The index being used for this loop */
      int iCur = pLevel.iTabCur;   /* The cursor of the table */
      WhereTerm pTerm;             /* A single constraint term */
      int j;                        /* Loop counter */
      int regBase;                  /* Base register */
      int nReg;                     /* Number of registers to allocate */

      /* This module is only called on query plans that use an index. */
      Debug.Assert( ( pLevel.plan.wsFlags & WHERE_INDEXED ) != 0 );
      pIdx = pLevel.plan.u.pIdx;

      /* Figure out how many memory cells we will need then allocate them.
      */
      regBase = pParse.nMem + 1;
      nReg = (int)( pLevel.plan.nEq + nExtraReg );
      pParse.nMem += nReg;

      /* Evaluate the equality constraints
      */
      Debug.Assert( pIdx.nColumn >= nEq );
      for ( j = 0 ; j < nEq ; j++ )
      {
        int r1;
        int k = pIdx.aiColumn[j];
        pTerm = findTerm( pWC, iCur, k, notReady, pLevel.plan.wsFlags, pIdx );
        if ( NEVER( pTerm == null ) ) break;
        Debug.Assert( ( pTerm.wtFlags & TERM_CODED ) == 0 );
        r1 = codeEqualityTerm( pParse, pTerm, pLevel, regBase + j );
        if ( r1 != regBase + j )
        {
          if ( nReg == 1 )
          {
            sqlite3ReleaseTempReg( pParse, regBase );
            regBase = r1;
          }
          else
          {
            sqlite3VdbeAddOp2( v, OP_SCopy, r1, regBase + j );
          }
        }
        testcase( pTerm.eOperator & WO_ISNULL );
        testcase( pTerm.eOperator & WO_IN );
        if ( ( pTerm.eOperator & ( WO_ISNULL | WO_IN ) ) == 0 )
        {
          sqlite3VdbeAddOp2( v, OP_IsNull, regBase + j, pLevel.addrBrk );
        }
      }
      return regBase;
    }

    /*
    ** Generate code for the start of the iLevel-th loop in the WHERE clause
    ** implementation described by pWInfo.
    */
    static Bitmask codeOneLoopStart(
    WhereInfo pWInfo,     /* Complete information about the WHERE clause */
    int iLevel,           /* Which level of pWInfo.a[] should be coded */
    u16 wctrlFlags,       /* One of the WHERE_* flags defined in sqliteInt.h */
    Bitmask notReady      /* Which tables are currently available */
    )
    {
      int j, k;                 /* Loop counters */
      int iCur;                 /* The VDBE cursor for the table */
      int addrNxt;              /* Where to jump to continue with the next IN case */
      int omitTable;            /* True if we use the index only */
      int bRev;                 /* True if we need to scan in reverse order */
      WhereLevel pLevel;        /* The where level to be coded */
      WhereClause pWC;          /* Decomposition of the entire WHERE clause */
      WhereTerm pTerm;          /* A WHERE clause term */
      Parse pParse;             /* Parsing context */
      Vdbe v;                   /* The prepared stmt under constructions */
      SrcList_item pTabItem;    /* FROM clause term being coded */
      int addrBrk;              /* Jump here to break out of the loop */
      int addrCont;             /* Jump here to continue with next cycle */
      int iRowidReg = 0;        /* Rowid is stored in this register, if not zero */
      int iReleaseReg = 0;      /* Temp register to free before returning */

      pParse = pWInfo.pParse;
      v = pParse.pVdbe;
      pWC = pWInfo.pWC;
      pLevel = pWInfo.a[iLevel];
      pTabItem = pWInfo.pTabList.a[pLevel.iFrom];
      iCur = pTabItem.iCursor;
      bRev = ( pLevel.plan.wsFlags & WHERE_REVERSE ) != 0 ? 1 : 0;
      omitTable = ( ( pLevel.plan.wsFlags & WHERE_IDX_ONLY ) != 0
      && ( wctrlFlags & WHERE_FORCE_TABLE ) == 0 ) ? 1 : 0;

      /* Create labels for the "break" and "continue" instructions
      ** for the current loop.  Jump to addrBrk to break out of a loop.
      ** Jump to cont to go immediately to the next iteration of the
      ** loop.
      **
      ** When there is an IN operator, we also have a "addrNxt" label that
      ** means to continue with the next IN value combination.  When
      ** there are no IN operators in the constraints, the "addrNxt" label
      ** is the same as "addrBrk".
      */
      addrBrk = pLevel.addrBrk = pLevel.addrNxt = sqlite3VdbeMakeLabel( v );
      addrCont = pLevel.addrCont = sqlite3VdbeMakeLabel( v );

      /* If this is the right table of a LEFT OUTER JOIN, allocate and
      ** initialize a memory cell that records if this table matches any
      ** row of the left table of the join.
      */
      if ( pLevel.iFrom > 0 && ( pTabItem.jointype & JT_LEFT ) != 0 )// Check value of pTabItem[0].jointype
      {
        pLevel.iLeftJoin = ++pParse.nMem;
        sqlite3VdbeAddOp2( v, OP_Integer, 0, pLevel.iLeftJoin );
#if SQLITE_DEBUG
        VdbeComment( v, "init LEFT JOIN no-match flag" );
#endif
      }

#if  !SQLITE_OMIT_VIRTUALTABLE
if ( ( pLevel.plan.wsFlags & WHERE_VIRTUALTABLE ) != null )
{
/* Case 0:  The table is a virtual-table.  Use the VFilter and VNext
**          to access the data.
*/
int iReg;   /* P3 Value for OP_VFilter */
sqlite3_index_info pVtabIdx = pLevel.plan.u.pVtabIdx;
int nConstraint = pVtabIdx.nConstraint;
sqlite3_index_constraint_usage* aUsage =
pVtabIdx.aConstraintUsage;
const sqlite3_index_constraint* aConstraint =
pVtabIdx.aConstraint;

iReg = sqlite3GetTempRange( pParse, nConstraint + 2 );
for ( j = 1 ; j <= nConstraint ; j++ )
{
for ( k = 0 ; k < nConstraint ; k++ )
{
if ( aUsage[k].argvIndex == j )
{
int iTerm = aConstraint[k].iTermOffset;
sqlite3ExprCode( pParse, pWC.a[iTerm].pExpr.pRight, iReg + j + 1 );
break;
}
}
if ( k == nConstraint ) break;
}
sqlite3VdbeAddOp2( v, OP_Integer, pVtabIdx.idxNum, iReg );
sqlite3VdbeAddOp2( v, OP_Integer, j - 1, iReg + 1 );
sqlite3VdbeAddOp4( v, OP_VFilter, iCur, addrBrk, iReg, pVtabIdx.idxStr,
pVtabIdx.needToFreeIdxStr ? P4_MPRINTF : P4_STATIC );
pVtabIdx.needToFreeIdxStr = 0;
for ( j = 0 ; j < nConstraint ; j++ )
{
if ( aUsage[j].omit )
{
int iTerm = aConstraint[j].iTermOffset;
disableTerm( pLevel, &pWC.a[iTerm] );
}
}
pLevel.op = OP_VNext;
pLevel.p1 = iCur;
pLevel.p2 = sqlite3VdbeCurrentAddr( v );
sqlite3ReleaseTempRange( pParse, iReg, nConstraint + 2 );
}
else
#endif //* SQLITE_OMIT_VIRTUALTABLE */

      if ( ( pLevel.plan.wsFlags & WHERE_ROWID_EQ ) != 0 )
      {
        /* Case 1:  We can directly reference a single row using an
        **          equality comparison against the ROWID field.  Or
        **          we reference multiple rows using a "rowid IN (...)"
        **          construct.
        */
        iReleaseReg = sqlite3GetTempReg( pParse );
        pTerm = findTerm( pWC, iCur, -1, notReady, WO_EQ | WO_IN, null );
        Debug.Assert( pTerm != null );
        Debug.Assert( pTerm.pExpr != null );
        Debug.Assert( pTerm.leftCursor == iCur );
        Debug.Assert( omitTable == 0 );
        iRowidReg = codeEqualityTerm( pParse, pTerm, pLevel, iReleaseReg );
        addrNxt = pLevel.addrNxt;
        sqlite3VdbeAddOp2( v, OP_MustBeInt, iRowidReg, addrNxt );
        sqlite3VdbeAddOp3( v, OP_NotExists, iCur, addrNxt, iRowidReg );
        sqlite3ExprCacheStore( pParse, iCur, -1, iRowidReg );
#if SQLITE_DEBUG
        VdbeComment( v, "pk" );
#endif
        pLevel.op = OP_Noop;
      }
      else if ( ( pLevel.plan.wsFlags & WHERE_ROWID_RANGE ) != 0 )
      {
        /* Case 2:  We have an inequality comparison against the ROWID field.
        */
        int testOp = OP_Noop;
        int start;
        int memEndValue = 0;
        WhereTerm pStart, pEnd;

        Debug.Assert( omitTable == 0 );
        pStart = findTerm( pWC, iCur, -1, notReady, WO_GT | WO_GE, null );
        pEnd = findTerm( pWC, iCur, -1, notReady, WO_LT | WO_LE, null );
        if ( bRev != 0 )
        {
          pTerm = pStart;
          pStart = pEnd;
          pEnd = pTerm;
        }
        if ( pStart != null )
        {
          Expr pX;             /* The expression that defines the start bound */
          int r1, rTemp = 0;        /* Registers for holding the start boundary */

          /* The following constant maps TK_xx codes into corresponding
          ** seek opcodes.  It depends on a particular ordering of TK_xx
          */
          u8[] aMoveOp = new u8[]{
/* TK_GT */  OP_SeekGt,
/* TK_LE */  OP_SeekLe,
/* TK_LT */  OP_SeekLt,
/* TK_GE */  OP_SeekGe
};
          Debug.Assert( TK_LE == TK_GT + 1 );      /* Make sure the ordering.. */
          Debug.Assert( TK_LT == TK_GT + 2 );      /*  ... of the TK_xx values... */
          Debug.Assert( TK_GE == TK_GT + 3 );      /*  ... is correcct. */

          pX = pStart.pExpr;
          Debug.Assert( pX != null );
          Debug.Assert( pStart.leftCursor == iCur );
          r1 = sqlite3ExprCodeTemp( pParse, pX.pRight, ref rTemp );
          sqlite3VdbeAddOp3( v, aMoveOp[pX.op - TK_GT], iCur, addrBrk, r1 );
#if SQLITE_DEBUG
          VdbeComment( v, "pk" );
#endif
          sqlite3ExprCacheAffinityChange( pParse, r1, 1 );
          sqlite3ReleaseTempReg( pParse, rTemp );
          disableTerm( pLevel, pStart );
        }
        else
        {
          sqlite3VdbeAddOp2( v, bRev != 0 ? OP_Last : OP_Rewind, iCur, addrBrk );
        }
        if ( pEnd != null )
        {
          Expr pX;
          pX = pEnd.pExpr;
          Debug.Assert( pX != null );
          Debug.Assert( pEnd.leftCursor == iCur );
          memEndValue = ++pParse.nMem;
          sqlite3ExprCode( pParse, pX.pRight, memEndValue );
          if ( pX.op == TK_LT || pX.op == TK_GT )
          {
            testOp = bRev != 0 ? OP_Le : OP_Ge;
          }
          else
          {
            testOp = bRev != 0 ? OP_Lt : OP_Gt;
          }
          disableTerm( pLevel, pEnd );
        }
        start = sqlite3VdbeCurrentAddr( v );
        pLevel.op = (u8)( bRev != 0 ? OP_Prev : OP_Next );
        pLevel.p1 = iCur;
        pLevel.p2 = start;
        pLevel.p5 = (u8)( ( pStart == null && pEnd == null ) ? 1 : 0 );
        if ( testOp != OP_Noop )
        {
          iRowidReg = iReleaseReg = sqlite3GetTempReg( pParse );
          sqlite3VdbeAddOp2( v, OP_Rowid, iCur, iRowidReg );
          sqlite3ExprCacheStore( pParse, iCur, -1, iRowidReg );
          sqlite3VdbeAddOp3( v, testOp, memEndValue, addrBrk, iRowidReg );
          sqlite3VdbeChangeP5( v, SQLITE_AFF_NUMERIC | SQLITE_JUMPIFNULL );
        }
      }
      else if ( ( pLevel.plan.wsFlags & ( WHERE_COLUMN_RANGE | WHERE_COLUMN_EQ ) ) != 0 )
      {
        /* Case 3: A scan using an index.
        **
        **         The WHERE clause may contain zero or more equality
        **         terms ("==" or "IN" operators) that refer to the N
        **         left-most columns of the index. It may also contain
        **         inequality constraints (>, <, >= or <=) on the indexed
        **         column that immediately follows the N equalities. Only
        **         the right-most column can be an inequality - the rest must
        **         use the "==" and "IN" operators. For example, if the
        **         index is on (x,y,z), then the following clauses are all
        **         optimized:
        **
        **            x=5
        **            x=5 AND y=10
        **            x=5 AND y<10
        **            x=5 AND y>5 AND y<10
        **            x=5 AND y=5 AND z<=10
        **
        **         The z<10 term of the following cannot be used, only
        **         the x=5 term:
        **
        **            x=5 AND z<10
        **
        **         N may be zero if there are inequality constraints.
        **         If there are no inequality constraints, then N is at
        **         least one.
        **
        **         This case is also used when there are no WHERE clause
        **         constraints but an index is selected anyway, in order
        **         to force the output order to conform to an ORDER BY.
        */
        int[] aStartOp = new int[]  {
0,
0,
OP_Rewind,           /* 2: (!start_constraints && startEq &&  !bRev) */
OP_Last,             /* 3: (!start_constraints && startEq &&   bRev) */
OP_SeekGt,           /* 4: (start_constraints  && !startEq && !bRev) */
OP_SeekLt,           /* 5: (start_constraints  && !startEq &&  bRev) */
OP_SeekGe,           /* 6: (start_constraints  &&  startEq && !bRev) */
OP_SeekLe            /* 7: (start_constraints  &&  startEq &&  bRev) */
};
        int[] aEndOp = new int[]  {
OP_Noop,             /* 0: (!end_constraints) */
OP_IdxGE,            /* 1: (end_constraints && !bRev) */
OP_IdxLT             /* 2: (end_constraints && bRev) */
};
        int nEq = (int)pLevel.plan.nEq;
        int isMinQuery = 0;          /* If this is an optimized SELECT min(x).. */
        int regBase;                 /* Base register holding constraint values */
        int r1;                      /* Temp register */
        WhereTerm pRangeStart = null;  /* Inequality constraint at range start */
        WhereTerm pRangeEnd = null;    /* Inequality constraint at range end */
        int startEq;                 /* True if range start uses ==, >= or <= */
        int endEq;                   /* True if range end uses ==, >= or <= */
        int start_constraints;       /* Start of range is constrained */
        int nConstraint;             /* Number of constraint terms */
        Index pIdx;         /* The index we will be using */
        int iIdxCur;         /* The VDBE cursor for the index */
        int nExtraReg = 0;   /* Number of extra registers needed */
        int op;              /* Instruction opcode */

        pIdx = pLevel.plan.u.pIdx;
        iIdxCur = pLevel.iIdxCur;
        k = pIdx.aiColumn[nEq];     /* Column for inequality constraints */

        /* If this loop satisfies a sort order (pOrderBy) request that
        ** was pDebug.Assed to this function to implement a "SELECT min(x) ..."
        ** query, then the caller will only allow the loop to run for
        ** a single iteration. This means that the first row returned
        ** should not have a NULL value stored in 'x'. If column 'x' is
        ** the first one after the nEq equality constraints in the index,
        ** this requires some special handling.
        */
        if ( ( wctrlFlags & WHERE_ORDERBY_MIN ) != 0
        && ( ( pLevel.plan.wsFlags & WHERE_ORDERBY ) != 0 )
        && ( pIdx.nColumn > nEq )
        )
        {
          /* Debug.Assert( pOrderBy.nExpr==1 ); */
          /* Debug.Assert( pOrderBy.a[0].pExpr.iColumn==pIdx.aiColumn[nEq] ); */
          isMinQuery = 1;
          nExtraReg = 1;
        }

        /* Find any inequality constraint terms for the start and end
        ** of the range.
        */
        if ( ( pLevel.plan.wsFlags & WHERE_TOP_LIMIT ) != 0 )
        {
          pRangeEnd = findTerm( pWC, iCur, k, notReady, ( WO_LT | WO_LE ), pIdx );
          nExtraReg = 1;
        }
        if ( ( pLevel.plan.wsFlags & WHERE_BTM_LIMIT ) != 0 )
        {
          pRangeStart = findTerm( pWC, iCur, k, notReady, ( WO_GT | WO_GE ), pIdx );
          nExtraReg = 1;
        }

        /* Generate code to evaluate all constraint terms using == or IN
        ** and store the values of those terms in an array of registers
        ** starting at regBase.
        */
        regBase = codeAllEqualityTerms( pParse, pLevel, pWC, notReady, nExtraReg );
        addrNxt = pLevel.addrNxt;


        /* If we are doing a reverse order scan on an ascending index, or
        ** a forward order scan on a descending index, interchange the
        ** start and end terms (pRangeStart and pRangeEnd).
        */
        if ( bRev == ( ( pIdx.aSortOrder[nEq] == SQLITE_SO_ASC ) ? 1 : 0 ) )
        {
          SWAP( ref pRangeEnd, ref pRangeStart );
        }

        testcase( pRangeStart != null && ( pRangeStart.eOperator & WO_LE ) != 0 );
        testcase( pRangeStart != null && ( pRangeStart.eOperator & WO_GE ) != 0 );
        testcase( pRangeEnd != null && ( pRangeEnd.eOperator & WO_LE ) != 0 );
        testcase( pRangeEnd != null && ( pRangeEnd.eOperator & WO_GE ) != 0 );
        startEq = ( null == pRangeStart || ( pRangeStart.eOperator & ( WO_LE | WO_GE ) ) != 0 ) ? 1 : 0;
        endEq = ( null == pRangeEnd || ( pRangeEnd.eOperator & ( WO_LE | WO_GE ) ) != 0 ) ? 1 : 0;
        start_constraints = ( pRangeStart != null || nEq > 0 ) ? 1 : 0;

        /* Seek the index cursor to the start of the range. */
        nConstraint = nEq;
        if ( pRangeStart != null )
        {
          sqlite3ExprCode( pParse, pRangeStart.pExpr.pRight, regBase + nEq );
          sqlite3VdbeAddOp2( v, OP_IsNull, regBase + nEq, addrNxt );
          nConstraint++;
        }
        else if ( isMinQuery != 0 )
        {
          sqlite3VdbeAddOp2( v, OP_Null, 0, regBase + nEq );
          nConstraint++;
          startEq = 0;
          start_constraints = 1;
        }
        codeApplyAffinity( pParse, regBase, nConstraint, pIdx );
        op = aStartOp[( start_constraints << 2 ) + ( startEq << 1 ) + bRev];
        Debug.Assert( op != 0 );
        testcase( op == OP_Rewind );
        testcase( op == OP_Last );
        testcase( op == OP_SeekGt );
        testcase( op == OP_SeekGe );
        testcase( op == OP_SeekLe );
        testcase( op == OP_SeekLt );
        sqlite3VdbeAddOp4( v, op, iIdxCur, addrNxt, regBase,
        ( nConstraint ), P4_INT32 );//    SQLITE_INT_TO_PTR(nConstraint)

        /* Load the value for the inequality constraint at the end of the
        ** range (if any).
        */
        nConstraint = nEq;
        if ( pRangeEnd != null )
        {
          sqlite3ExprCacheRemove( pParse, regBase + nEq );
          sqlite3ExprCode( pParse, pRangeEnd.pExpr.pRight, regBase + nEq );
          sqlite3VdbeAddOp2( v, OP_IsNull, regBase + nEq, addrNxt );
          codeApplyAffinity( pParse, regBase, nEq + 1, pIdx );
          nConstraint++;
        }

        /* Top of the loop body */
        pLevel.p2 = sqlite3VdbeCurrentAddr( v );

        /* Check if the index cursor is past the end of the range. */
        op = aEndOp[( ( pRangeEnd != null || nEq != 0 ) ? 1 : 0 ) * ( 1 + bRev )];
        testcase( op == OP_Noop );
        testcase( op == OP_IdxGE );
        testcase( op == OP_IdxLT );
        if ( op != OP_Noop )
        {
          sqlite3VdbeAddOp4( v, op, iIdxCur, addrNxt, regBase,
          ( nConstraint ), P4_INT32 );//    SQLITE_INT_TO_PTR(nConstraint)
          sqlite3VdbeChangeP5( v, (u8)( endEq != bRev ? 1 : 0 ) );
        }

        /* If there are inequality constraints, check that the value
        ** of the table column that the inequality contrains is not NULL.
        ** If it is, jump to the next iteration of the loop.
        */
        r1 = sqlite3GetTempReg( pParse );
        testcase( pLevel.plan.wsFlags & WHERE_BTM_LIMIT );
        testcase( pLevel.plan.wsFlags & WHERE_TOP_LIMIT );
        if ( ( pLevel.plan.wsFlags & ( WHERE_BTM_LIMIT | WHERE_TOP_LIMIT ) ) != 0 )
        {
          sqlite3VdbeAddOp3( v, OP_Column, iIdxCur, nEq, r1 );
          sqlite3VdbeAddOp2( v, OP_IsNull, r1, addrCont );
        }
        sqlite3ReleaseTempReg( pParse, r1 );

        /* Seek the table cursor, if required */
        disableTerm( pLevel, pRangeStart );
        disableTerm( pLevel, pRangeEnd );
        if ( 0 == omitTable )
        {
          iRowidReg = iReleaseReg = sqlite3GetTempReg( pParse );
          sqlite3VdbeAddOp2( v, OP_IdxRowid, iIdxCur, iRowidReg );
          sqlite3ExprCacheStore( pParse, iCur, -1, iRowidReg );
          sqlite3VdbeAddOp2( v, OP_Seek, iCur, iRowidReg );  /* Deferred seek */
        }

        /* Record the instruction used to terminate the loop. Disable
        ** WHERE clause terms made redundant by the index range scan.
        */
        pLevel.op = (u8)( bRev != 0 ? OP_Prev : OP_Next );
        pLevel.p1 = iIdxCur;
      }
      else

#if  !SQLITE_OMIT_OR_OPTIMIZATION
        if ( ( pLevel.plan.wsFlags & WHERE_MULTI_OR ) != 0 )
        {
          /* Case 4:  Two or more separately indexed terms connected by OR
          **
          ** Example:
          **
          **   CREATE TABLE t1(a,b,c,d);
          **   CREATE INDEX i1 ON t1(a);
          **   CREATE INDEX i2 ON t1(b);
          **   CREATE INDEX i3 ON t1(c);
          **
          **   SELECT * FROM t1 WHERE a=5 OR b=7 OR (c=11 AND d=13)
          **
          ** In the example, there are three indexed terms connected by OR.
          ** The top of the loop looks like this:
          **
          **          Null       1                # Zero the rowset in reg 1
          **
          ** Then, for each indexed term, the following. The arguments to
          ** RowSetTest are such that the rowid of the current row is inserted
          ** into the RowSet. If it is already present, control skips the
          ** Gosub opcode and jumps straight to the code generated by WhereEnd().
          **
          **        sqlite3WhereBegin(<term>)
          **          RowSetTest                  # Insert rowid into rowset
          **          Gosub      2 A
          **        sqlite3WhereEnd()
          **
          ** Following the above, code to terminate the loop. Label A, the target
          ** of the Gosub above, jumps to the instruction right after the Goto.
          **
          **          Null       1                # Zero the rowset in reg 1
          **          Goto       B                # The loop is finished.
          **
          **       A: <loop body>                 # Return data, whatever.
          **
          **          Return     2                # Jump back to the Gosub
          **
          **       B: <after the loop>
          **
          */
          WhereClause pOrWc;    /* The OR-clause broken out into subterms */
          WhereTerm pFinal;    /* Final subterm within the OR-clause. */
          SrcList oneTab = new SrcList();        /* Shortened table list */

          int regReturn = ++pParse.nMem;           /* Register used with OP_Gosub */
          int regRowset = 0;                       /* Register for RowSet object */
          int regRowid = 0;                        /* Register holding rowid */
          int iLoopBody = sqlite3VdbeMakeLabel( v );  /* Start of loop body */
          int iRetInit;                             /* Address of regReturn init */
          int ii;
          pTerm = pLevel.plan.u.pTerm;
          Debug.Assert( pTerm != null );
          Debug.Assert( pTerm.eOperator == WO_OR );
          Debug.Assert( ( pTerm.wtFlags & TERM_ORINFO ) != 0 );
          pOrWc = pTerm.u.pOrInfo.wc;
          pFinal = pOrWc.a[pOrWc.nTerm - 1];
          /* Set up a SrcList containing just the table being scanned by this loop. */
          oneTab.nSrc = 1;
          oneTab.nAlloc = 1;
          oneTab.a = new SrcList_item[1];
          oneTab.a[0] = pTabItem;
          /* Initialize the rowset register to contain NULL. An SQL NULL is
          ** equivalent to an empty rowset.
          **
          ** Also initialize regReturn to contain the address of the instruction
          ** immediately following the OP_Return at the bottom of the loop. This
          ** is required in a few obscure LEFT JOIN cases where control jumps
          ** over the top of the loop into the body of it. In this case the
          ** correct response for the end-of-loop code (the OP_Return) is to
          ** fall through to the next instruction, just as an OP_Next does if
          ** called on an uninitialized cursor.
          */
          if ( ( wctrlFlags & WHERE_DUPLICATES_OK ) == 0 )
          {
            regRowset = ++pParse.nMem;
            regRowid = ++pParse.nMem;
            sqlite3VdbeAddOp2( v, OP_Null, 0, regRowset );
          }
          iRetInit = sqlite3VdbeAddOp2( v, OP_Integer, 0, regReturn );

          for ( ii = 0 ; ii < pOrWc.nTerm ; ii++ )
          {
            WhereTerm pOrTerm = pOrWc.a[ii];
            if ( pOrTerm.leftCursor == iCur || pOrTerm.eOperator == WO_AND )
            {
              WhereInfo pSubWInfo;          /* Info for single OR-term scan */

              /* Loop through table entries that match term pOrTerm. */
              ExprList elDummy = null;
              pSubWInfo = sqlite3WhereBegin( pParse, oneTab, pOrTerm.pExpr, ref elDummy,
              WHERE_OMIT_OPEN | WHERE_OMIT_CLOSE | WHERE_FORCE_TABLE );
              if ( pSubWInfo != null )
              {
                if ( ( wctrlFlags & WHERE_DUPLICATES_OK ) == 0 )
                {
                  int iSet = ( ( ii == pOrWc.nTerm - 1 ) ? -1 : ii );
                  int r;
                  r = sqlite3ExprCodeGetColumn( pParse, pTabItem.pTab, -1, iCur,
                  regRowid, false );
                  sqlite3VdbeAddOp4( v, OP_RowSetTest, regRowset,
                  sqlite3VdbeCurrentAddr( v ) + 2,
                  r, iSet, P4_INT32 );//SQLITE_INT_TO_PTR(iSet), P4_INT32);
                }
                sqlite3VdbeAddOp2( v, OP_Gosub, regReturn, iLoopBody );

                /* Finish the loop through table entries that match term pOrTerm. */
                sqlite3WhereEnd( pSubWInfo );
              }
            }
          }
          sqlite3VdbeChangeP1( v, iRetInit, sqlite3VdbeCurrentAddr( v ) );
          /* sqlite3VdbeAddOp2(v, OP_Null, 0, regRowset); */
          sqlite3VdbeAddOp2( v, OP_Goto, 0, pLevel.addrBrk );
          sqlite3VdbeResolveLabel( v, iLoopBody );

          pLevel.op = OP_Return;
          pLevel.p1 = regReturn;
          disableTerm( pLevel, pTerm );
        }
        else
#endif //* SQLITE_OMIT_OR_OPTIMIZATION */

        {
          /* Case 5:  There is no usable index.  We must do a complete
          **          scan of the entire table.
          */
          u8[] aStep = new u8[] { OP_Next, OP_Prev };
          u8[] aStart = new u8[] { OP_Rewind, OP_Last };
          Debug.Assert( bRev == 0 || bRev == 1 );
          Debug.Assert( omitTable == 0 );
          pLevel.op = aStep[bRev];
          pLevel.p1 = iCur;
          pLevel.p2 = 1 + sqlite3VdbeAddOp2( v, aStart[bRev], iCur, addrBrk );
          pLevel.p5 = SQLITE_STMTSTATUS_FULLSCAN_STEP;
        }
      notReady &= ~getMask( pWC.pMaskSet, iCur );

      /* Insert code to test every subexpression that can be completely
      ** computed using the current set of tables.
      */
      k = 0;
      for ( j = pWC.nTerm ; j > 0 ; j-- )//, pTerm++)
      {
        pTerm = pWC.a[pWC.nTerm - j];
        Expr pE;
        testcase( pTerm.wtFlags & TERM_VIRTUAL );
        testcase( pTerm.wtFlags & TERM_CODED );
        if ( ( pTerm.wtFlags & ( TERM_VIRTUAL | TERM_CODED ) ) != 0 ) continue;
        if ( ( pTerm.prereqAll & notReady ) != 0 ) continue;
        pE = pTerm.pExpr;
        Debug.Assert( pE != null );
        if ( pLevel.iLeftJoin != 0 && !( ( pE.flags & EP_FromJoin ) == EP_FromJoin ) )// !ExprHasProperty(pE, EP_FromJoin) ){
        {
          continue;
        }
        sqlite3ExprIfFalse( pParse, pE, addrCont, SQLITE_JUMPIFNULL );
        k = 1;
        pTerm.wtFlags |= TERM_CODED;
      }

      /* For a LEFT OUTER JOIN, generate code that will record the fact that
      ** at least one row of the right table has matched the left table.
      */
      if ( pLevel.iLeftJoin != 0 )
      {
        pLevel.addrFirst = sqlite3VdbeCurrentAddr( v );
        sqlite3VdbeAddOp2( v, OP_Integer, 1, pLevel.iLeftJoin );
#if SQLITE_DEBUG
        VdbeComment( v, "record LEFT JOIN hit" );
#endif
        sqlite3ExprCacheClear( pParse );
        for ( j = 0 ; j < pWC.nTerm ; j++ )//, pTerm++)
        {
          pTerm = pWC.a[j];
          testcase( pTerm.wtFlags & TERM_VIRTUAL );
          testcase( pTerm.wtFlags & TERM_CODED );
          if ( ( pTerm.wtFlags & ( TERM_VIRTUAL | TERM_CODED ) ) != 0 ) continue;
          if ( ( pTerm.prereqAll & notReady ) != 0 ) continue;
          Debug.Assert( pTerm.pExpr != null );
          sqlite3ExprIfFalse( pParse, pTerm.pExpr, addrCont, SQLITE_JUMPIFNULL );
          pTerm.wtFlags |= TERM_CODED;
        }
      }

      sqlite3ReleaseTempReg( pParse, iReleaseReg );
      return notReady;
    }

#if  (SQLITE_TEST)
    /*
** The following variable holds a text description of query plan generated
** by the most recent call to sqlite3WhereBegin().  Each call to WhereBegin
** overwrites the previous.  This information is used for testing and
** analysis only.
*/
    //char sqlite3_query_plan[BMS*2*40];  /* Text of the join */
    static int nQPlan = 0;              /* Next free slow in _query_plan[] */

#endif //* SQLITE_TEST */


    /*
** Free a WhereInfo structure
*/
    static void whereInfoFree( sqlite3 db, WhereInfo pWInfo )
    {
      if ( pWInfo != null )
      {
        int i;
        for ( i = 0 ; i < pWInfo.nLevel ; i++ )
        {
          sqlite3_index_info pInfo = pWInfo.a[i].pIdxInfo;
          if ( pInfo != null )
          {
            /* Debug.Assert( pInfo.needToFreeIdxStr==0 || db.mallocFailed ); */
            if ( pInfo.needToFreeIdxStr != 0 )
            {
              //sqlite3_free( ref pInfo.idxStr );
            }
            //sqlite3DbFree( db, pInfo );
          }
        }
        whereClauseClear( pWInfo.pWC );
        //sqlite3DbFree( db, pWInfo );
      }
    }


    /*
    ** Generate the beginning of the loop used for WHERE clause processing.
    ** The return value is a pointer to an opaque structure that contains
    ** information needed to terminate the loop.  Later, the calling routine
    ** should invoke sqlite3WhereEnd() with the return value of this function
    ** in order to complete the WHERE clause processing.
    **
    ** If an error occurs, this routine returns NULL.
    **
    ** The basic idea is to do a nested loop, one loop for each table in
    ** the FROM clause of a select.  (INSERT and UPDATE statements are the
    ** same as a SELECT with only a single table in the FROM clause.)  For
    ** example, if the SQL is this:
    **
    **       SELECT * FROM t1, t2, t3 WHERE ...;
    **
    ** Then the code generated is conceptually like the following:
    **
    **      foreach row1 in t1 do       \    Code generated
    **        foreach row2 in t2 do      |-- by sqlite3WhereBegin()
    **          foreach row3 in t3 do   /
    **            ...
    **          end                     \    Code generated
    **        end                        |-- by sqlite3WhereEnd()
    **      end                         /
    **
    ** Note that the loops might not be nested in the order in which they
    ** appear in the FROM clause if a different order is better able to make
    ** use of indices.  Note also that when the IN operator appears in
    ** the WHERE clause, it might result in additional nested loops for
    ** scanning through all values on the right-hand side of the IN.
    **
    ** There are Btree cursors Debug.Associated with each table.  t1 uses cursor
    ** number pTabList.a[0].iCursor.  t2 uses the cursor pTabList.a[1].iCursor.
    ** And so forth.  This routine generates code to open those VDBE cursors
    ** and sqlite3WhereEnd() generates the code to close them.
    **
    ** The code that sqlite3WhereBegin() generates leaves the cursors named
    ** in pTabList pointing at their appropriate entries.  The [...] code
    ** can use OP_Column and OP_Rowid opcodes on these cursors to extract
    ** data from the various tables of the loop.
    **
    ** If the WHERE clause is empty, the foreach loops must each scan their
    ** entire tables.  Thus a three-way join is an O(N^3) operation.  But if
    ** the tables have indices and there are terms in the WHERE clause that
    ** refer to those indices, a complete table scan can be avoided and the
    ** code will run much faster.  Most of the work of this routine is checking
    ** to see if there are indices that can be used to speed up the loop.
    **
    ** Terms of the WHERE clause are also used to limit which rows actually
    ** make it to the "..." in the middle of the loop.  After each "foreach",
    ** terms of the WHERE clause that use only terms in that loop and outer
    ** loops are evaluated and if false a jump is made around all subsequent
    ** inner loops (or around the "..." if the test occurs within the inner-
    ** most loop)
    **
    ** OUTER JOINS
    **
    ** An outer join of tables t1 and t2 is conceptally coded as follows:
    **
    **    foreach row1 in t1 do
    **      flag = 0
    **      foreach row2 in t2 do
    **        start:
    **          ...
    **          flag = 1
    **      end
    **      if flag==null then
    **        move the row2 cursor to a null row
    **        goto start
    **      fi
    **    end
    **
    ** ORDER BY CLAUSE PROCESSING
    **
    ** ppOrderBy is a pointer to the ORDER BY clause of a SELECT statement,
    ** if there is one.  If there is no ORDER BY clause or if this routine
    ** is called from an UPDATE or DELETE statement, then ppOrderBy is NULL.
    **
    ** If an index can be used so that the natural output order of the table
    ** scan is correct for the ORDER BY clause, then that index is used and
    ** ppOrderBy is set to NULL.  This is an optimization that prevents an
    ** unnecessary sort of the result set if an index appropriate for the
    ** ORDER BY clause already exists.
    **
    ** If the where clause loops cannot be arranged to provide the correct
    ** output order, then the ppOrderBy is unchanged.
    */
    static WhereInfo sqlite3WhereBegin(
    Parse pParse,           /* The parser context */
    SrcList pTabList,       /* A list of all tables to be scanned */
    Expr pWhere,            /* The WHERE clause */
    ref ExprList ppOrderBy, /* An ORDER BY clause, or NULL */
    u16 wctrlFlags          /* One of the WHERE_* flags defined in sqliteInt.h */
    )
    {
      int i;                     /* Loop counter */
      int nByteWInfo;            /* Num. bytes allocated for WhereInfo struct */
      WhereInfo pWInfo;          /* Will become the return value of this function */
      Vdbe v = pParse.pVdbe;     /* The virtual data_base engine */
      Bitmask notReady;          /* Cursors that are not yet positioned */
      WhereMaskSet pMaskSet;     /* The expression mask set */
      WhereClause pWC = new WhereClause();               /* Decomposition of the WHERE clause */
      SrcList_item pTabItem;     /* A single entry from pTabList */
      WhereLevel pLevel;         /* A single level in the pWInfo list */
      int iFrom;                 /* First unused FROM clause element */
      int andFlags;              /* AND-ed combination of all pWC.a[].wtFlags */
      sqlite3 db;                /* Data_base connection */

      /* The number of tables in the FROM clause is limited by the number of
      ** bits in a Bitmask
      */
      if ( pTabList.nSrc > BMS )
      {
        sqlite3ErrorMsg( pParse, "at most %d tables in a join", BMS );
        return null;
      }

      /* Allocate and initialize the WhereInfo structure that will become the
      ** return value. A single allocation is used to store the WhereInfo
      ** struct, the contents of WhereInfo.a[], the WhereClause structure
      ** and the WhereMaskSet structure. Since WhereClause contains an 8-byte
      ** field (type Bitmask) it must be aligned on an 8-byte boundary on
      ** some architectures. Hence the ROUND8() below.
      */
      db = pParse.db;
      pWInfo = new WhereInfo();
      //nByteWInfo = ROUND8( sizeof( WhereInfo ) + ( pTabList.nSrc - 1 ) * sizeof( WhereLevel ) );
      //pWInfo = sqlite3DbMallocZero( db,
      //    nByteWInfo +
      //    sizeof( WhereClause ) +
      //    sizeof( WhereMaskSet )
      //);
      pWInfo.a = new WhereLevel[pTabList.nSrc];
      //if ( db.mallocFailed != 0 )
      //{
      //  goto whereBeginError;
      //}
      pWInfo.nLevel = pTabList.nSrc;
      pWInfo.pParse = pParse;
      pWInfo.pTabList = pTabList;
      pWInfo.iBreak = sqlite3VdbeMakeLabel( v );
      pWInfo.pWC = pWC = new WhereClause();// (WhereClause )((u8 )pWInfo)[nByteWInfo];
      pWInfo.wctrlFlags = wctrlFlags;
      //pMaskSet = (WhereMaskSet)pWC[1];

      /* Split the WHERE clause into separate subexpressions where each
      ** subexpression is separated by an AND operator.
      */
      pMaskSet = new WhereMaskSet();//initMaskSet(pMaskSet);
      whereClauseInit( pWC, pParse, pMaskSet );
      sqlite3ExprCodeConstants( pParse, pWhere );
      whereSplit( pWC, pWhere, TK_AND );

      /* Special case: a WHERE clause that is constant.  Evaluate the
      ** expression and either jump over all of the code or fall thru.
      */
      if ( pWhere != null && ( pTabList.nSrc == 0 || sqlite3ExprIsConstantNotJoin( pWhere ) != 0 ) )
      {
        sqlite3ExprIfFalse( pParse, pWhere, pWInfo.iBreak, SQLITE_JUMPIFNULL );
        pWhere = null;
      }

      /* Assign a bit from the bitmask to every term in the FROM clause.
      **
      ** When assigning bitmask values to FROM clause cursors, it must be
      ** the case that if X is the bitmask for the N-th FROM clause term then
      ** the bitmask for all FROM clause terms to the left of the N-th term
      ** is (X-1).   An expression from the ON clause of a LEFT JOIN can use
      ** its Expr.iRightJoinTable value to find the bitmask of the right table
      ** of the join.  Subtracting one from the right table bitmask gives a
      ** bitmask for all tables to the left of the join.  Knowing the bitmask
      ** for all tables to the left of a left join is important.  Ticket #3015.
      **
      ** Configure the WhereClause.vmask variable so that bits that correspond
      ** to virtual table cursors are set. This is used to selectively disable
      ** the OR-to-IN transformation in exprAnalyzeOrTerm(). It is not helpful
      ** with virtual tables.
      */
      Debug.Assert( pWC.vmask == 0 && pMaskSet.n == 0 );
      for ( i = 0 ; i < pTabList.nSrc ; i++ )
      {
        createMask( pMaskSet, pTabList.a[i].iCursor );
#if !SQLITE_OMIT_VIRTUALTABLE
if ( ALWAYS( pTabList.a[i].pTab ) && IsVirtual( pTabList.a[i].pTab ) )
{
pWC.vmask |= ( (Bitmask)1 << i );
}
#endif
      }
#if  !NDEBUG
      {
        Bitmask toTheLeft = 0;
        for ( i = 0 ; i < pTabList.nSrc ; i++ )
        {
          Bitmask m = getMask( pMaskSet, pTabList.a[i].iCursor );
          Debug.Assert( ( m - 1 ) == toTheLeft );
          toTheLeft |= m;
        }
      }
#endif

      /* Analyze all of the subexpressions.  Note that exprAnalyze() might
** add new virtual terms onto the end of the WHERE clause.  We do not
** want to analyze these virtual terms, so start analyzing at the end
** and work forward so that the added virtual terms are never processed.
*/
      exprAnalyzeAll( pTabList, pWC );
      //if ( db.mallocFailed != 0 )
      //{
      //  goto whereBeginError;
      //}

      /* Chose the best index to use for each table in the FROM clause.
      **
      ** This loop fills in the following fields:
      **
      **   pWInfo.a[].pIdx      The index to use for this level of the loop.
      **   pWInfo.a[].wsFlags   WHERE_xxx flags Debug.Associated with pIdx
      **   pWInfo.a[].nEq       The number of == and IN constraints
      **   pWInfo.a[].iFrom     Which term of the FROM clause is being coded
      **   pWInfo.a[].iTabCur   The VDBE cursor for the data_base table
      **   pWInfo.a[].iIdxCur   The VDBE cursor for the index
      **   pWInfo.a[].pTerm     When wsFlags==WO_OR, the OR-clause term
      **
      ** This loop also figures out the nesting order of tables in the FROM
      ** clause.
      */
      notReady = ~(Bitmask)0;
      pTabItem = pTabList.a != null ? pTabList.a[0] : null; //pTabItem = pTabList.a;
      //pLevel = pWInfo.a;
      andFlags = ~0;
#if (SQLITE_TEST) && (SQLITE_DEBUG)
      WHERETRACE( "*** Optimizer Start ***\n" );
#endif
      for ( i = iFrom = 0 ; i < pTabList.nSrc ; i++ )//, pLevel++ )
      {
        pWInfo.a[i] = new WhereLevel();
        pLevel = pWInfo.a[i];
        WhereCost bestPlan;         /* Most efficient plan seen so far */
        Index pIdx;                /* Index for FROM table at pTabItem */
        int j;                      /* For looping over FROM tables */
        int bestJ = 0;              /* The value of j */
        Bitmask m;                  /* Bitmask value for j or bestJ */
        int once = 0;               /* True when first table is seen */

        bestPlan = new WhereCost();// memset( &bestPlan, 0, sizeof( bestPlan ) );
        bestPlan.rCost = SQLITE_BIG_DBL;
        for ( j = iFrom ; j < pTabList.nSrc ; j++ )//, pTabItem++)
        {
          pTabItem = pTabList.a[j];
          int doNotReorder;       /* True if this table should not be reordered */
          WhereCost sCost = null; /* Cost information from best[Virtual]Index() */
          ExprList pOrderBy;      /* ORDER BY clause for index to optimize */

          doNotReorder = ( pTabItem.jointype & ( JT_LEFT | JT_CROSS ) ) != 0 ? 1 : 0;
          if ( ( once != 0 && doNotReorder != 0 ) ) break;
          m = getMask( pMaskSet, pTabItem.iCursor );
          if ( ( m & notReady ) == 0 )
          {
            if ( j == iFrom ) iFrom++;
            continue;
          }
          pOrderBy = ( ( i == 0 && ppOrderBy != null ) ? ppOrderBy : null );
          Debug.Assert( pTabItem.pTab != null );
#if  !SQLITE_OMIT_VIRTUALTABLE
if( IsVirtual(pTabItem.pTab) ){
sqlite3_index_info **pp = &pWInfo.a[j].pIdxInfo;
bestVirtualIndex(pParse, pWC, pTabItem, notReady, pOrderBy, &sCost, pp);
}else
#endif
          {
            bestBtreeIndex( pParse, pWC, pTabItem, notReady, pOrderBy, ref sCost );
          }
          if ( once == 0 || sCost.rCost < bestPlan.rCost )
          {
            once = 1;
            bestPlan = sCost;
            bestJ = j;
          }
          if ( doNotReorder != 0 ) break;
        }
        Debug.Assert( once != 0 );
        Debug.Assert( ( notReady & getMask( pMaskSet, pTabList.a[bestJ].iCursor ) ) != 0 );
#if (SQLITE_TEST) && (SQLITE_DEBUG)
        WHERETRACE( "*** Optimizer selects table %d for loop %d\n", bestJ,
        i );//pLevel - pWInfo.a );
#endif
        if ( ( bestPlan.plan.wsFlags & WHERE_ORDERBY ) != 0 )
        {
          ppOrderBy = null;
        }
        andFlags = (int)( andFlags & bestPlan.plan.wsFlags );
        pLevel.plan = bestPlan.plan;
        if ( ( bestPlan.plan.wsFlags & WHERE_INDEXED ) != 0 )
        {
          pLevel.iIdxCur = pParse.nTab++;
        }
        else
        {
          pLevel.iIdxCur = -1;
        }
        notReady &= ~getMask( pMaskSet, pTabList.a[bestJ].iCursor );
        pLevel.iFrom = (u8)bestJ;

        /* Check that if the table scanned by this loop iteration had an
        ** INDEXED BY clause attached to it, that the named index is being
        ** used for the scan. If not, then query compilation has failed.
        ** Return an error.
        */
        pIdx = pTabList.a[bestJ].pIndex;
        if ( pIdx != null )
        {
          if ( ( bestPlan.plan.wsFlags & WHERE_INDEXED ) == 0 )
          {
            sqlite3ErrorMsg( pParse, "cannot use index: %s", pIdx.zName );
            goto whereBeginError;
          }
          else
          {
            /* If an INDEXED BY clause is used, the bestIndex() function is
            ** guaranteed to find the index specified in the INDEXED BY clause
            ** if it find an index at all. */
            Debug.Assert( bestPlan.plan.u.pIdx == pIdx );
          }
        }
      }
#if (SQLITE_TEST) && (SQLITE_DEBUG)
      WHERETRACE( "*** Optimizer Finished ***\n" );
#endif
      if ( pParse.nErr != 0 /*|| db.mallocFailed != 0 */ )
      {
        goto whereBeginError;
      }

      /* If the total query only selects a single row, then the ORDER BY
      ** clause is irrelevant.
      */
      if ( ( andFlags & WHERE_UNIQUE ) != 0 && ppOrderBy != null )
      {
        ppOrderBy = null;
      }

      /* If the caller is an UPDATE or DELETE statement that is requesting
      ** to use a one-pDebug.Ass algorithm, determine if this is appropriate.
      ** The one-pDebug.Ass algorithm only works if the WHERE clause constraints
      ** the statement to update a single row.
      */
      Debug.Assert( ( wctrlFlags & WHERE_ONEPASS_DESIRED ) == 0 || pWInfo.nLevel == 1 );
      if ( ( wctrlFlags & WHERE_ONEPASS_DESIRED ) != 0 && ( andFlags & WHERE_UNIQUE ) != 0 )
      {
        pWInfo.okOnePass = 1;
        pWInfo.a[0].plan.wsFlags = (u32)( pWInfo.a[0].plan.wsFlags & ~WHERE_IDX_ONLY );
      }

      /* Open all tables in the pTabList and any indices selected for
      ** searching those tables.
      */
      sqlite3CodeVerifySchema( pParse, -1 ); /* Insert the cookie verifier Goto */
      for ( i = 0 ; i < pTabList.nSrc ; i++ )//, pLevel++ )
      {
        pLevel = pWInfo.a[i];
        Table pTab;     /* Table to open */
        int iDb;         /* Index of data_base containing table/index */

#if  !SQLITE_OMIT_EXPLAIN
        if ( pParse.explain == 2 )
        {
          string zMsg;
          SrcList_item pItem = pTabList.a[pLevel.iFrom];
          zMsg = sqlite3MPrintf( db, "TABLE %s", pItem.zName );
          if ( pItem.zAlias != null )
          {
            zMsg = sqlite3MAppendf( db, zMsg, "%s AS %s", zMsg, pItem.zAlias );
          }
          if ( ( pLevel.plan.wsFlags & WHERE_INDEXED ) != 0 )
          {
            zMsg = sqlite3MAppendf( db, zMsg, "%s WITH INDEX %s",
            zMsg, pLevel.plan.u.pIdx.zName );
          }
          else if ( ( pLevel.plan.wsFlags & WHERE_MULTI_OR ) != 0 )
          {
            zMsg = sqlite3MAppendf( db, zMsg, "%s VIA MULTI-INDEX UNION", zMsg );
          }
          else if ( ( pLevel.plan.wsFlags & ( WHERE_ROWID_EQ | WHERE_ROWID_RANGE ) ) != 0 )
          {
            zMsg = sqlite3MAppendf( db, zMsg, "%s USING PRIMARY KEY", zMsg );
          }
#if  !SQLITE_OMIT_VIRTUALTABLE
else if( (pLevel.plan.wsFlags & WHERE_VIRTUALTABLE)!=null ){
sqlite3_index_info pVtabIdx = pLevel.plan.u.pVtabIdx;
zMsg = sqlite3MAppendf(db, zMsg, "%s VIRTUAL TABLE INDEX %d:%s", zMsg,
pVtabIdx.idxNum, pVtabIdx.idxStr);
}
#endif
          if ( ( pLevel.plan.wsFlags & WHERE_ORDERBY ) != 0 )
          {
            zMsg = sqlite3MAppendf( db, zMsg, "%s ORDER BY", zMsg );
          }
          sqlite3VdbeAddOp4( v, OP_Explain, i, pLevel.iFrom, 0, zMsg, P4_DYNAMIC );
        }
#endif //* SQLITE_OMIT_EXPLAIN */
        pTabItem = pTabList.a[pLevel.iFrom];
        pTab = pTabItem.pTab;
        iDb = sqlite3SchemaToIndex( db, pTab.pSchema );
        if ( ( pTab.tabFlags & TF_Ephemeral ) != 0 || pTab.pSelect != null ) continue;
#if  !SQLITE_OMIT_VIRTUALTABLE
if( (pLevel.plan.wsFlags & WHERE_VIRTUALTABLE)!=null ){
 VTable pVTab = sqlite3GetVTable(db, pTab);
int iCur = pTabItem.iCursor;
sqlite3VdbeAddOp4(v, OP_VOpen, iCur, 0, 0,
pVTab, P4_VTAB);
}else
#endif
        if ( ( pLevel.plan.wsFlags & WHERE_IDX_ONLY ) == 0
        && ( wctrlFlags & WHERE_OMIT_OPEN ) == 0 )
        {
          int op = pWInfo.okOnePass != 0 ? OP_OpenWrite : OP_OpenRead;
          sqlite3OpenTable( pParse, pTabItem.iCursor, iDb, pTab, op );
          if ( 0 == pWInfo.okOnePass && pTab.nCol < BMS )
          {
            Bitmask b = pTabItem.colUsed;
            int n = 0;
            for ( ; b != 0 ; b = b >> 1, n++ ) { }
            sqlite3VdbeChangeP4( v, sqlite3VdbeCurrentAddr( v ) - 1, n, P4_INT32 );
            Debug.Assert( n <= pTab.nCol );
          }
        }
        else
        {
          sqlite3TableLock( pParse, iDb, pTab.tnum, 0, pTab.zName );
        }
        pLevel.iTabCur = pTabItem.iCursor;
        if ( ( pLevel.plan.wsFlags & WHERE_INDEXED ) != 0 )
        {
          Index pIx = pLevel.plan.u.pIdx;
          KeyInfo pKey = sqlite3IndexKeyinfo( pParse, pIx );
          int iIdxCur = pLevel.iIdxCur;
          Debug.Assert( pIx.pSchema == pTab.pSchema );
          Debug.Assert( iIdxCur >= 0 );
          sqlite3VdbeAddOp4( v, OP_OpenRead, iIdxCur, pIx.tnum, iDb,
          pKey, P4_KEYINFO_HANDOFF );
#if SQLITE_DEBUG
          VdbeComment( v, "%s", pIx.zName );
#endif
        }
        sqlite3CodeVerifySchema( pParse, iDb );
      }
      pWInfo.iTop = sqlite3VdbeCurrentAddr( v );

      /* Generate the code to do the search.  Each iteration of the for
      ** loop below generates code for a single nested loop of the VM
      ** program.
      */
      notReady = ~(Bitmask)0;
      for ( i = 0 ; i < pTabList.nSrc ; i++ )
      {
        notReady = codeOneLoopStart( pWInfo, i, wctrlFlags, notReady );
        pWInfo.iContinue = pWInfo.a[i].addrCont;
      }

#if SQLITE_TEST  //* For testing and debugging use only */
      /* Record in the query plan information about the current table
** and the index used to access it (if any).  If the table itself
** is not used, its name is just '{}'.  If no index is used
** the index is listed as "{}".  If the primary key is used the
** index name is '*'.
*/
      sqlite3_query_plan.sValue = "";
      for ( i = 0 ; i < pTabList.nSrc ; i++ )
      {
        string z;
        int n;
        pLevel = pWInfo.a[i];
        pTabItem = pTabList.a[pLevel.iFrom];
        z = pTabItem.zAlias;
        if ( z == null ) z = pTabItem.pTab.zName;
        n = sqlite3Strlen30( z );
        if ( true ) //n+nQPlan < sizeof(sqlite3_query_plan)-10 )
        {
          if ( ( pLevel.plan.wsFlags & WHERE_IDX_ONLY ) != 0 )
          {
            sqlite3_query_plan.Append( "{}" ); //memcpy( &sqlite3_query_plan[nQPlan], "{}", 2 );
            nQPlan += 2;
          }
          else
          {
            sqlite3_query_plan.Append( z ); //memcpy( &sqlite3_query_plan[nQPlan], z, n );
            nQPlan += n;
          }
          sqlite3_query_plan.Append( " " ); nQPlan++; //sqlite3_query_plan[nQPlan++] = ' ';
        }
        testcase( pLevel.plan.wsFlags & WHERE_ROWID_EQ );
        testcase( pLevel.plan.wsFlags & WHERE_ROWID_RANGE );
        if ( ( pLevel.plan.wsFlags & ( WHERE_ROWID_EQ | WHERE_ROWID_RANGE ) ) != 0 )
        {
          sqlite3_query_plan.Append( "* " ); //memcpy(&sqlite3_query_plan[nQPlan], "* ", 2);
          nQPlan += 2;
        }
        else if ( ( pLevel.plan.wsFlags & WHERE_INDEXED ) != 0 )
        {
          n = sqlite3Strlen30( pLevel.plan.u.pIdx.zName );
          if ( true ) //n+nQPlan < sizeof(sqlite3_query_plan)-2 )//if( n+nQPlan < sizeof(sqlite3_query_plan)-2 )
          {
            sqlite3_query_plan.Append( pLevel.plan.u.pIdx.zName ); //memcpy(&sqlite3_query_plan[nQPlan], pLevel.plan.u.pIdx.zName, n);
            nQPlan += n;
            sqlite3_query_plan.Append( " " ); //sqlite3_query_plan[nQPlan++] = ' ';
          }
        }
        else
        {
          sqlite3_query_plan.Append( "{} " ); //memcpy( &sqlite3_query_plan[nQPlan], "{} ", 3 );
          nQPlan += 3;
        }
      }
      //while( nQPlan>0 && sqlite3_query_plan[nQPlan-1]==' ' ){
      //  sqlite3_query_plan[--nQPlan] = 0;
      //}
      //sqlite3_query_plan[nQPlan] = 0;
      sqlite3_query_plan.Trim();
      nQPlan = 0;
#endif //* SQLITE_TEST // Testing and debugging use only */

      /* Record the continuation address in the WhereInfo structure.  Then
** clean up and return.
*/
      return pWInfo;

      /* Jump here if malloc fails */
whereBeginError:
      whereInfoFree( db, pWInfo );
      return null;
    }

    /*
    ** Generate the end of the WHERE loop.  See comments on
    ** sqlite3WhereBegin() for additional information.
    */
    static void sqlite3WhereEnd( WhereInfo pWInfo )
    {
      Parse pParse = pWInfo.pParse;
      Vdbe v = pParse.pVdbe;
      int i;
      WhereLevel pLevel;
      SrcList pTabList = pWInfo.pTabList;
      sqlite3 db = pParse.db;

      /* Generate loop termination code.
      */
      sqlite3ExprCacheClear( pParse );
      for ( i = pTabList.nSrc - 1 ; i >= 0 ; i-- )
      {
        pLevel = pWInfo.a[i];
        sqlite3VdbeResolveLabel( v, pLevel.addrCont );
        if ( pLevel.op != OP_Noop )
        {
          sqlite3VdbeAddOp2( v, pLevel.op, pLevel.p1, pLevel.p2 );
          sqlite3VdbeChangeP5( v, pLevel.p5 );
        }
        if ( ( pLevel.plan.wsFlags & WHERE_IN_ABLE ) != 0 && pLevel.u._in.nIn > 0 )
        {
          InLoop pIn;
          int j;
          sqlite3VdbeResolveLabel( v, pLevel.addrNxt );
          for ( j = pLevel.u._in.nIn ; j > 0 ; j-- )//, pIn--)
          {
            pIn = pLevel.u._in.aInLoop[j - 1];
            sqlite3VdbeJumpHere( v, pIn.addrInTop + 1 );
            sqlite3VdbeAddOp2( v, OP_Next, pIn.iCur, pIn.addrInTop );
            sqlite3VdbeJumpHere( v, pIn.addrInTop - 1 );
          }
          //sqlite3DbFree( db, pLevel.u._in.aInLoop );
        }
        sqlite3VdbeResolveLabel( v, pLevel.addrBrk );
        if ( pLevel.iLeftJoin != 0 )
        {
          int addr;
          addr = sqlite3VdbeAddOp1( v, OP_IfPos, pLevel.iLeftJoin );
          sqlite3VdbeAddOp1( v, OP_NullRow, pTabList.a[i].iCursor );
          if ( pLevel.iIdxCur >= 0 )
          {
            sqlite3VdbeAddOp1( v, OP_NullRow, pLevel.iIdxCur );
          }
          if ( pLevel.op == OP_Return )
          {
            sqlite3VdbeAddOp2( v, OP_Gosub, pLevel.p1, pLevel.addrFirst );
          }
          else
          {
            sqlite3VdbeAddOp2( v, OP_Goto, 0, pLevel.addrFirst );
          }
          sqlite3VdbeJumpHere( v, addr );
        }
      }

      /* The "break" point is here, just past the end of the outer loop.
      ** Set it.
      */
      sqlite3VdbeResolveLabel( v, pWInfo.iBreak );

      /* Close all of the cursors that were opened by sqlite3WhereBegin.
      */
      for ( i = 0 ; i < pTabList.nSrc ; i++ )//, pLevel++)
      {
        pLevel = pWInfo.a[i];
        SrcList_item pTabItem = pTabList.a[pLevel.iFrom];
        Table pTab = pTabItem.pTab;
        Debug.Assert( pTab != null );
        if ( ( pTab.tabFlags & TF_Ephemeral ) != 0 || pTab.pSelect != null ) continue;
        if ( ( pWInfo.wctrlFlags & WHERE_OMIT_CLOSE ) == 0 )
        {
          if ( 0 == pWInfo.okOnePass && ( pLevel.plan.wsFlags & WHERE_IDX_ONLY ) == 0 )
          {
            sqlite3VdbeAddOp1( v, OP_Close, pTabItem.iCursor );
          }
          if ( ( pLevel.plan.wsFlags & WHERE_INDEXED ) != 0 )
          {
            sqlite3VdbeAddOp1( v, OP_Close, pLevel.iIdxCur );
          }
        }

        /* If this scan uses an index, make code substitutions to read data
        ** from the index in preference to the table. Sometimes, this means
        ** the table need never be read from. This is a performance boost,
        ** as the vdbe level waits until the table is read before actually
        ** seeking the table cursor to the record corresponding to the current
        ** position in the index.
        **
        ** Calls to the code generator in between sqlite3WhereBegin and
        ** sqlite3WhereEnd will have created code that references the table
        ** directly.  This loop scans all that code looking for opcodes
        ** that reference the table and converts them into opcodes that
        ** reference the index.
        */
        if ( ( pLevel.plan.wsFlags & WHERE_INDEXED ) != 0 )///* && 0 == db.mallocFailed */ )
        {
          int k, j, last;
          VdbeOp pOp;
          Index pIdx = pLevel.plan.u.pIdx;
          int useIndexOnly = ( pLevel.plan.wsFlags & WHERE_IDX_ONLY ) != 0 ? 1 : 0;

          Debug.Assert( pIdx != null );
          //pOp = sqlite3VdbeGetOp( v, pWInfo.iTop );
          last = sqlite3VdbeCurrentAddr( v );
          for ( k = pWInfo.iTop ; k < last ; k++ )//, pOp++ )
          {
            pOp = sqlite3VdbeGetOp( v, k );
            if ( pOp.p1 != pLevel.iTabCur ) continue;
            if ( pOp.opcode == OP_Column )
            {
              for ( j = 0 ; j < pIdx.nColumn ; j++ )
              {
                if ( pOp.p2 == pIdx.aiColumn[j] )
                {
                  pOp.p2 = j;
                  pOp.p1 = pLevel.iIdxCur;
                  break;
                }
              }
              Debug.Assert( 0 == useIndexOnly || j < pIdx.nColumn );
            }
            else if ( pOp.opcode == OP_Rowid )
            {
              pOp.p1 = pLevel.iIdxCur;
              pOp.opcode = OP_IdxRowid;
            }
            else if ( pOp.opcode == OP_NullRow && useIndexOnly != 0 )
            {
              pOp.opcode = OP_Noop;
            }
          }
        }
      }

      /* Final cleanup
      */
      whereInfoFree( db, pWInfo );
      return;
    }
  }
}
