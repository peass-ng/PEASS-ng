using System;
using System.Diagnostics;
using System.Text;

using Pgno = System.UInt32;
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
    ** to handle INSERT statements in SQLite.
    **
    ** $Id: insert.c,v 1.270 2009/07/24 17:58:53 danielk1977 Exp $
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
    ** Generate code that will open a table for reading.
    */
    static void sqlite3OpenTable(
    Parse p,       /* Generate code into this VDBE */
    int iCur,       /* The cursor number of the table */
    int iDb,        /* The database index in sqlite3.aDb[] */
    Table pTab,    /* The table to be opened */
    int opcode      /* OP_OpenRead or OP_OpenWrite */
    )
    {
      Vdbe v;
      if ( IsVirtual( pTab ) ) return;
      v = sqlite3GetVdbe( p );
      Debug.Assert( opcode == OP_OpenWrite || opcode == OP_OpenRead );
      sqlite3TableLock( p, iDb, pTab.tnum, ( opcode == OP_OpenWrite ) ? (byte)1 : (byte)0, pTab.zName );
      sqlite3VdbeAddOp3( v, opcode, iCur, pTab.tnum, iDb );
      sqlite3VdbeChangeP4( v, -1, ( pTab.nCol ), P4_INT32 );//SQLITE_INT_TO_PTR( pTab.nCol ), P4_INT32 );
      VdbeComment( v, "%s", pTab.zName );
    }

    /*
    ** Set P4 of the most recently inserted opcode to a column affinity
    ** string for index pIdx. A column affinity string has one character
    ** for each column in the table, according to the affinity of the column:
    **
    **  Character      Column affinity
    **  ------------------------------
    **  'a'            TEXT
    **  'b'            NONE
    **  'c'            NUMERIC
    **  'd'            INTEGER
    **  'e'            REAL
    **
    ** An extra 'b' is appended to the end of the string to cover the
    ** rowid that appears as the last column in every index.
    */
    static void sqlite3IndexAffinityStr( Vdbe v, Index pIdx )
    {
      if ( pIdx.zColAff == null || pIdx.zColAff[0] == '\0' )
      {
        /* The first time a column affinity string for a particular index is
        ** required, it is allocated and populated here. It is then stored as
        ** a member of the Index structure for subsequent use.
        **
        ** The column affinity string will eventually be deleted by
        ** sqliteDeleteIndex() when the Index structure itself is cleaned
        ** up.
        */
        int n;
        Table pTab = pIdx.pTable;
        sqlite3 db = sqlite3VdbeDb( v );
        StringBuilder pIdx_zColAff = new StringBuilder( pIdx.nColumn + 2 );// (char *)sqlite3Malloc(pIdx->nColumn+2);
        if ( pIdx_zColAff == null )
        {
  ////        db.mallocFailed = 1;
          return;
        }
        for ( n = 0 ; n < pIdx.nColumn ; n++ )
        {
          pIdx_zColAff.Append( pTab.aCol[pIdx.aiColumn[n]].affinity );
        }
        pIdx_zColAff.Append( SQLITE_AFF_NONE );
        pIdx_zColAff.Append( '\0' );
        pIdx.zColAff = pIdx_zColAff.ToString();
      }
      sqlite3VdbeChangeP4( v, -1, pIdx.zColAff, 0 );
    }

    /*
    ** Set P4 of the most recently inserted opcode to a column affinity
    ** string for table pTab. A column affinity string has one character
    ** for each column indexed by the index, according to the affinity of the
    ** column:
    **
    **  Character      Column affinity
    **  ------------------------------
    **  'a'            TEXT
    **  'b'            NONE
    **  'c'            NUMERIC
    **  'd'            INTEGER
    **  'e'            REAL
    */
    static void sqlite3TableAffinityStr( Vdbe v, Table pTab )
    {
      /* The first time a column affinity string for a particular table
      ** is required, it is allocated and populated here. It is then
      ** stored as a member of the Table structure for subsequent use.
      **
      ** The column affinity string will eventually be deleted by
      ** sqlite3DeleteTable() when the Table structure itself is cleaned up.
      */
      if ( pTab.zColAff == null )
      {
        StringBuilder zColAff;
        int i;
        sqlite3 db = sqlite3VdbeDb( v );

        zColAff = new StringBuilder( pTab.nCol + 1 );// (char*)sqlite3Malloc(db, pTab.nCol + 1);
        if ( zColAff == null )
        {
  ////        db.mallocFailed = 1;
          return;
        }

        for ( i = 0 ; i < pTab.nCol ; i++ )
        {
          zColAff.Append( pTab.aCol[i].affinity );
        }
        //zColAff.Append( '\0' );

        pTab.zColAff = zColAff.ToString();
      }

      sqlite3VdbeChangeP4( v, -1, pTab.zColAff, 0 );
    }

    /*
    ** Return non-zero if the table pTab in database iDb or any of its indices
    ** have been opened at any point in the VDBE program beginning at location
    ** iStartAddr throught the end of the program.  This is used to see if
    ** a statement of the form  "INSERT INTO <iDb, pTab> SELECT ..." can
    ** run without using temporary table for the results of the SELECT.
    */
    static bool readsTable(Parse p, int iStartAddr, int iDb, Table pTab )
    {
      Vdbe v = sqlite3GetVdbe( p );
      int i;
      int iEnd = sqlite3VdbeCurrentAddr( v );
#if !SQLITE_OMIT_VIRTUALTABLE
  VTable pVTab = IsVirtual(pTab) ? sqlite3GetVTable(p,db, pTab) : null;
#endif

      for ( i = iStartAddr ; i < iEnd ; i++ )
      {
        VdbeOp pOp = sqlite3VdbeGetOp( v, i );
        Debug.Assert( pOp != null );
        if ( pOp.opcode == OP_OpenRead && pOp.p3 == iDb )
        {
          Index pIndex;
          int tnum = pOp.p2;
          if ( tnum == pTab.tnum )
          {
            return true;
          }
          for ( pIndex = pTab.pIndex ; pIndex != null ; pIndex = pIndex.pNext )
          {
            if ( tnum == pIndex.tnum )
            {
              return true;
            }
          }
        }
#if !SQLITE_OMIT_VIRTUALTABLE
if( pOp.opcode==OP_VOpen && pOp.p4.pVtab==pVTab){
Debug.Assert( pOp.p4.pVtab!=0 );
Debug.Assert( pOp.p4type==P4_VTAB );
return true;
}
#endif
      }
      return false;
    }

#if !SQLITE_OMIT_AUTOINCREMENT
    /*
** Locate or create an AutoincInfo structure associated with table pTab
** which is in database iDb.  Return the register number for the register
** that holds the maximum rowid.
**
** There is at most one AutoincInfo structure per table even if the
** same table is autoincremented multiple times due to inserts within
** triggers.  A new AutoincInfo structure is created if this is the
** first use of table pTab.  On 2nd and subsequent uses, the original
** AutoincInfo structure is used.
**
** Three memory locations are allocated:
**
**   (1)  Register to hold the name of the pTab table.
**   (2)  Register to hold the maximum ROWID of pTab.
**   (3)  Register to hold the rowid in sqlite_sequence of pTab
**
** The 2nd register is the one that is returned.  That is all the
** insert routine needs to know about.
*/
    static int autoIncBegin(
    Parse pParse,      /* Parsing context */
    int iDb,            /* Index of the database holding pTab */
    Table pTab         /* The table we are writing to */
    )
    {
      int memId = 0;      /* Register holding maximum rowid */
      if ( ( pTab.tabFlags & TF_Autoincrement ) != 0 )
      {
        AutoincInfo pInfo;

        pInfo = pParse.pAinc;
        while ( pInfo != null && pInfo.pTab != pTab ) { pInfo = pInfo.pNext; }
        if ( pInfo == null )
        {
          pInfo = new AutoincInfo();//sqlite3DbMallocRaw(pParse.db, sizeof(*pInfo));
          if ( pInfo == null ) return 0;
          pInfo.pNext = pParse.pAinc;
          pParse.pAinc = pInfo;
          pInfo.pTab = pTab;
          pInfo.iDb = iDb;
          pParse.nMem++;                  /* Register to hold name of table */
          pInfo.regCtr = ++pParse.nMem;  /* Max rowid register */
          pParse.nMem++;                  /* Rowid in sqlite_sequence */
        }
        memId = pInfo.regCtr;
      }
      return memId;
    }

    /*
    ** This routine generates code that will initialize all of the
    ** register used by the autoincrement tracker.
    */
    static void sqlite3AutoincrementBegin( Parse pParse )
    {
      AutoincInfo p;            /* Information about an AUTOINCREMENT */
      sqlite3 db = pParse.db;  /* The database connection */
      Db pDb;                   /* Database only autoinc table */
      int memId;                 /* Register holding max rowid */
      int addr;                  /* A VDBE address */
      Vdbe v = pParse.pVdbe;   /* VDBE under construction */

      Debug.Assert( v != null );   /* We failed long ago if this is not so */
      for ( p = pParse.pAinc ; p != null ; p = p.pNext )
      {
        pDb = db.aDb[p.iDb];
        memId = p.regCtr;
        sqlite3OpenTable( pParse, 0, p.iDb, pDb.pSchema.pSeqTab, OP_OpenRead );
        addr = sqlite3VdbeCurrentAddr( v );
        sqlite3VdbeAddOp4( v, OP_String8, 0, memId - 1, 0, p.pTab.zName, 0 );
        sqlite3VdbeAddOp2( v, OP_Rewind, 0, addr + 9 );
        sqlite3VdbeAddOp3( v, OP_Column, 0, 0, memId );
        sqlite3VdbeAddOp3( v, OP_Ne, memId - 1, addr + 7, memId );
        sqlite3VdbeChangeP5( v, SQLITE_JUMPIFNULL );
        sqlite3VdbeAddOp2( v, OP_Rowid, 0, memId + 1 );
        sqlite3VdbeAddOp3( v, OP_Column, 0, 1, memId );
        sqlite3VdbeAddOp2( v, OP_Goto, 0, addr + 9 );
        sqlite3VdbeAddOp2( v, OP_Next, 0, addr + 2 );
        sqlite3VdbeAddOp2( v, OP_Integer, 0, memId );
        sqlite3VdbeAddOp0( v, OP_Close );
      }
    }

    /*
    ** Update the maximum rowid for an autoincrement calculation.
    **
    ** This routine should be called when the top of the stack holds a
    ** new rowid that is about to be inserted.  If that new rowid is
    ** larger than the maximum rowid in the memId memory cell, then the
    ** memory cell is updated.  The stack is unchanged.
    */
    static void autoIncStep( Parse pParse, int memId, int regRowid )
    {
      if ( memId > 0 )
      {
        sqlite3VdbeAddOp2( pParse.pVdbe, OP_MemMax, memId, regRowid );
      }
    }

    /*
    ** This routine generates the code needed to write autoincrement
    ** maximum rowid values back into the sqlite_sequence register.
    ** Every statement that might do an INSERT into an autoincrement
    ** table (either directly or through triggers) needs to call this
    ** routine just before the "exit" code.
    */
    static void sqlite3AutoincrementEnd( Parse pParse )
    {
      AutoincInfo p;
      Vdbe v = pParse.pVdbe;
      sqlite3 db = pParse.db;

      Debug.Assert( v != null );
      for ( p = pParse.pAinc ; p != null ; p = p.pNext )
      {
        Db pDb = db.aDb[p.iDb];
        int j1, j2, j3, j4, j5;
        int iRec;
        int memId = p.regCtr;

        iRec = sqlite3GetTempReg( pParse );
        sqlite3OpenTable( pParse, 0, p.iDb, pDb.pSchema.pSeqTab, OP_OpenWrite );
        j1 = sqlite3VdbeAddOp1( v, OP_NotNull, memId + 1 );
        j2 = sqlite3VdbeAddOp0( v, OP_Rewind );
        j3 = sqlite3VdbeAddOp3( v, OP_Column, 0, 0, iRec );
        j4 = sqlite3VdbeAddOp3( v, OP_Eq, memId - 1, 0, iRec );
        sqlite3VdbeAddOp2( v, OP_Next, 0, j3 );
        sqlite3VdbeJumpHere( v, j2 );
        sqlite3VdbeAddOp2( v, OP_NewRowid, 0, memId + 1 );
        j5 = sqlite3VdbeAddOp0( v, OP_Goto );
        sqlite3VdbeJumpHere( v, j4 );
        sqlite3VdbeAddOp2( v, OP_Rowid, 0, memId + 1 );
        sqlite3VdbeJumpHere( v, j1 );
        sqlite3VdbeJumpHere( v, j5 );
        sqlite3VdbeAddOp3( v, OP_MakeRecord, memId - 1, 2, iRec );
        sqlite3VdbeAddOp3( v, OP_Insert, 0, iRec, memId + 1 );
        sqlite3VdbeChangeP5( v, OPFLAG_APPEND );
        sqlite3VdbeAddOp0( v, OP_Close );
        sqlite3ReleaseTempReg( pParse, iRec );
      }
    }
#else
/*
** If SQLITE_OMIT_AUTOINCREMENT is defined, then the three routines
** above are all no-ops
*/
//# define autoIncBegin(A,B,C) (0)
//# define autoIncStep(A,B,C)
#endif // * SQLITE_OMIT_AUTOINCREMENT */


    /* Forward declaration */
    //static int xferOptimization(
    //  Parse pParse,        /* Parser context */
    //  Table pDest,         /* The table we are inserting into */
    //  Select pSelect,      /* A SELECT statement to use as the data source */
    //  int onError,          /* How to handle constraint errors */
    //  int iDbDest           /* The database of pDest */
    //);

    /*
    ** This routine is call to handle SQL of the following forms:
    **
    **    insert into TABLE (IDLIST) values(EXPRLIST)
    **    insert into TABLE (IDLIST) select
    **
    ** The IDLIST following the table name is always optional.  If omitted,
    ** then a list of all columns for the table is substituted.  The IDLIST
    ** appears in the pColumn parameter.  pColumn is NULL if IDLIST is omitted.
    **
    ** The pList parameter holds EXPRLIST in the first form of the INSERT
    ** statement above, and pSelect is NULL.  For the second form, pList is
    ** NULL and pSelect is a pointer to the select statement used to generate
    ** data for the insert.
    **
    ** The code generated follows one of four templates.  For a simple
    ** select with data coming from a VALUES clause, the code executes
    ** once straight down through.  Pseudo-code follows (we call this
    ** the "1st template"):
    **
    **         open write cursor to <table> and its indices
    **         puts VALUES clause expressions onto the stack
    **         write the resulting record into <table>
    **         cleanup
    **
    ** The three remaining templates assume the statement is of the form
    **
    **   INSERT INTO <table> SELECT ...
    **
    ** If the SELECT clause is of the restricted form "SELECT * FROM <table2>" -
    ** in other words if the SELECT pulls all columns from a single table
    ** and there is no WHERE or LIMIT or GROUP BY or ORDER BY clauses, and
    ** if <table2> and <table1> are distinct tables but have identical
    ** schemas, including all the same indices, then a special optimization
    ** is invoked that copies raw records from <table2> over to <table1>.
    ** See the xferOptimization() function for the implementation of this
    ** template.  This is the 2nd template.
    **
    **         open a write cursor to <table>
    **         open read cursor on <table2>
    **         transfer all records in <table2> over to <table>
    **         close cursors
    **         foreach index on <table>
    **           open a write cursor on the <table> index
    **           open a read cursor on the corresponding <table2> index
    **           transfer all records from the read to the write cursors
    **           close cursors
    **         end foreach
    **
    ** The 3rd template is for when the second template does not apply
    ** and the SELECT clause does not read from <table> at any time.
    ** The generated code follows this template:
    **
    **         EOF <- 0
    **         X <- A
    **         goto B
    **      A: setup for the SELECT
    **         loop over the rows in the SELECT
    **           load values into registers R..R+n
    **           yield X
    **         end loop
    **         cleanup after the SELECT
    **         EOF <- 1
    **         yield X
    **         goto A
    **      B: open write cursor to <table> and its indices
    **      C: yield X
    **         if EOF goto D
    **         insert the select result into <table> from R..R+n
    **         goto C
    **      D: cleanup
    **
    ** The 4th template is used if the insert statement takes its
    ** values from a SELECT but the data is being inserted into a table
    ** that is also read as part of the SELECT.  In the third form,
    ** we have to use a intermediate table to store the results of
    ** the select.  The template is like this:
    **
    **         EOF <- 0
    **         X <- A
    **         goto B
    **      A: setup for the SELECT
    **         loop over the tables in the SELECT
    **           load value into register R..R+n
    **           yield X
    **         end loop
    **         cleanup after the SELECT
    **         EOF <- 1
    **         yield X
    **         halt-error
    **      B: open temp table
    **      L: yield X
    **         if EOF goto M
    **         insert row from R..R+n into temp table
    **         goto L
    **      M: open write cursor to <table> and its indices
    **         rewind temp table
    **      C: loop over rows of intermediate table
    **           transfer values form intermediate table into <table>
    **         end loop
    **      D: cleanup
    */
    // OVERLOADS, so I don't need to rewrite parse.c
    static void sqlite3Insert( Parse pParse, SrcList pTabList, int null_3, int null_4, IdList pColumn, int onError )
    { sqlite3Insert( pParse, pTabList, null, null, pColumn, onError ); }
    static void sqlite3Insert( Parse pParse, SrcList pTabList, int null_3, Select pSelect, IdList pColumn, int onError )
    { sqlite3Insert( pParse, pTabList, null, pSelect, pColumn, onError ); }
    static void sqlite3Insert( Parse pParse, SrcList pTabList, ExprList pList, int null_4, IdList pColumn, int onError )
    { sqlite3Insert( pParse, pTabList, pList, null, pColumn, onError ); }
    static void sqlite3Insert(
    Parse pParse,        /* Parser context */
    SrcList pTabList,    /* Name of table into which we are inserting */
    ExprList pList,      /* List of values to be inserted */
    Select pSelect,      /* A SELECT statement to use as the data source */
    IdList pColumn,      /* Column names corresponding to IDLIST. */
    int onError        /* How to handle constraint errors */
    )
    {
      sqlite3 db;           /* The main database structure */
      Table pTab;           /* The table to insert into.  aka TABLE */
      string zTab;          /* Name of the table into which we are inserting */
      string zDb;           /* Name of the database holding this table */
      int i = 0;
      int j = 0;
      int idx = 0;            /* Loop counters */
      Vdbe v;               /* Generate code into this virtual machine */
      Index pIdx;           /* For looping over indices of the table */
      int nColumn;          /* Number of columns in the data */
      int nHidden = 0;      /* Number of hidden columns if TABLE is virtual */
      int baseCur = 0;      /* VDBE VdbeCursor number for pTab */
      int keyColumn = -1;   /* Column that is the INTEGER PRIMARY KEY */
      int endOfLoop = 0;      /* Label for the end of the insertion loop */
      bool useTempTable = false; /* Store SELECT results in intermediate table */
      int srcTab = 0;       /* Data comes from this temporary cursor if >=0 */
      int addrInsTop = 0;   /* Jump to label "D" */
      int addrCont = 0;     /* Top of insert loop. Label "C" in templates 3 and 4 */
      int addrSelect = 0;   /* Address of coroutine that implements the SELECT */
      SelectDest dest;      /* Destination for SELECT on rhs of INSERT */
      int newIdx = -1;      /* VdbeCursor for the NEW pseudo-table */
      int iDb;              /* Index of database holding TABLE */
      Db pDb;               /* The database containing table being inserted into */
      bool appendFlag = false;   /* True if the insert is likely to be an append */

      /* Register allocations */
      int regFromSelect = 0;  /* Base register for data coming from SELECT */
      int regAutoinc = 0;   /* Register holding the AUTOINCREMENT counter */
      int regRowCount = 0;  /* Memory cell used for the row counter */
      int regIns;           /* Block of regs holding rowid+data being inserted */
      int regRowid;         /* registers holding insert rowid */
      int regData;          /* register holding first column to insert */
      int regRecord;        /* Holds the assemblied row record */
      int regEof = 0;       /* Register recording end of SELECT data */
      int[] aRegIdx = null; /* One register allocated to each index */


#if !SQLITE_OMIT_TRIGGER
      bool isView = false;        /* True if attempting to insert into a view */
      Trigger pTrigger;           /* List of triggers on pTab, if required */
      int tmask = 0;              /* Mask of trigger times */
#endif

      db = pParse.db;
      dest = new SelectDest();// memset( &dest, 0, sizeof( dest ) );

      if ( pParse.nErr != 0 /*|| db.mallocFailed != 0 */ )
      {
        goto insert_cleanup;
      }

      /* Locate the table into which we will be inserting new information.
      */
      Debug.Assert( pTabList.nSrc == 1 );
      zTab = pTabList.a[0].zName;
      if ( NEVER( zTab == null ) ) goto insert_cleanup;
      pTab = sqlite3SrcListLookup( pParse, pTabList );
      if ( pTab == null )
      {
        goto insert_cleanup;
      }
      iDb = sqlite3SchemaToIndex( db, pTab.pSchema );
      Debug.Assert( iDb < db.nDb );
      pDb = db.aDb[iDb];
      zDb = pDb.zName;
#if !SQLITE_OMIT_AUTHORIZATION
if( sqlite3AuthCheck(pParse, SQLITE_INSERT, pTab.zName, 0, zDb) ){
goto insert_cleanup;
}
#endif
      /* Figure out if we have any triggers and if the table being
** inserted into is a view
*/
#if !SQLITE_OMIT_TRIGGER
      pTrigger = sqlite3TriggersExist( pParse, pTab, TK_INSERT, null, ref tmask );
      isView = pTab.pSelect != null;
#else
//# define pTrigger 0
//# define tmask 0
bool isView = false;
#endif
#if  SQLITE_OMIT_VIEW
//# undef isView
isView = false;
#endif
      Debug.Assert( ( pTrigger != null && tmask != 0 ) || ( pTrigger == null && tmask == 0 ) );

#if !SQLITE_OMIT_VIEW
      /* If pTab is really a view, make sure it has been initialized.
      ** ViewGetColumnNames() is a no-op if pTab is not a view (or virtual
      ** module table).
      */
      if ( sqlite3ViewGetColumnNames( pParse, pTab ) != -0 )
      {
        goto insert_cleanup;
      }
#endif

      /* Ensure that:
      *  (a) the table is not read-only, 
      *  (b) that if it is a view then ON INSERT triggers exist
      */
      if ( sqlite3IsReadOnly( pParse, pTab, tmask ) )
      {
        goto insert_cleanup;
      }

      /* Allocate a VDBE
      */
      v = sqlite3GetVdbe( pParse );
      if ( v == null ) goto insert_cleanup;
      if ( pParse.nested == 0 ) sqlite3VdbeCountChanges( v );
      sqlite3BeginWriteOperation( pParse, ( pSelect != null || pTrigger != null ) ? 1 : 0, iDb );

      /* if there are row triggers, allocate a temp table for new.* references. */
      if ( pTrigger != null )
      {
        newIdx = pParse.nTab++;
      }

#if !SQLITE_OMIT_XFER_OPT
      /* If the statement is of the form
**
**       INSERT INTO <table1> SELECT * FROM <table2>;
**
** Then special optimizations can be applied that make the transfer
** very fast and which reduce fragmentation of indices.
**
** This is the 2nd template.
*/
      if ( pColumn == null && xferOptimization( pParse, pTab, pSelect, onError, iDb ) != 0 )
      {
        Debug.Assert( null == pTrigger );
        Debug.Assert( pList == null );
        goto insert_end;
      }
#endif // * SQLITE_OMIT_XFER_OPT */

      /* If this is an AUTOINCREMENT table, look up the sequence number in the
** sqlite_sequence table and store it in memory cell regAutoinc.
*/
      regAutoinc = autoIncBegin( pParse, iDb, pTab );

      /* Figure out how many columns of data are supplied.  If the data
      ** is coming from a SELECT statement, then generate a co-routine that
      ** produces a single row of the SELECT on each invocation.  The
      ** co-routine is the common header to the 3rd and 4th templates.
      */
      if ( pSelect != null )
      {
        /* Data is coming from a SELECT.  Generate code to implement that SELECT
        ** as a co-routine.  The code is common to both the 3rd and 4th
        ** templates:
        **
        **         EOF <- 0
        **         X <- A
        **         goto B
        **      A: setup for the SELECT
        **         loop over the tables in the SELECT
        **           load value into register R..R+n
        **           yield X
        **         end loop
        **         cleanup after the SELECT
        **         EOF <- 1
        **         yield X
        **         halt-error
        **
        ** On each invocation of the co-routine, it puts a single row of the
        ** SELECT result into registers dest.iMem...dest.iMem+dest.nMem-1.
        ** (These output registers are allocated by sqlite3Select().)  When
        ** the SELECT completes, it sets the EOF flag stored in regEof.
        */
        int rc = 0, j1;

        regEof = ++pParse.nMem;
        sqlite3VdbeAddOp2( v, OP_Integer, 0, regEof );      /* EOF <- 0 */
#if SQLITE_DEBUG
        VdbeComment( v, "SELECT eof flag" );
#endif
        sqlite3SelectDestInit( dest, SRT_Coroutine, ++pParse.nMem );
        addrSelect = sqlite3VdbeCurrentAddr( v ) + 2;
        sqlite3VdbeAddOp2( v, OP_Integer, addrSelect - 1, dest.iParm );
        j1 = sqlite3VdbeAddOp2( v, OP_Goto, 0, 0 );
#if SQLITE_DEBUG
        VdbeComment( v, "Jump over SELECT coroutine" );
#endif
        /* Resolve the expressions in the SELECT statement and execute it. */
        rc = sqlite3Select( pParse, pSelect, ref dest );
        Debug.Assert( pParse.nErr == 0 || rc != 0 );
        if ( rc != 0 || NEVER( pParse.nErr != 0 ) /*|| db.mallocFailed != 0 */ )
        {
          goto insert_cleanup;
        }
        sqlite3VdbeAddOp2( v, OP_Integer, 1, regEof );         /* EOF <- 1 */
        sqlite3VdbeAddOp1( v, OP_Yield, dest.iParm );   /* yield X */
        sqlite3VdbeAddOp2( v, OP_Halt, SQLITE_INTERNAL, OE_Abort );
#if SQLITE_DEBUG
        VdbeComment( v, "End of SELECT coroutine" );
#endif
        sqlite3VdbeJumpHere( v, j1 );                          /* label B: */

        regFromSelect = dest.iMem;
        Debug.Assert( pSelect.pEList != null );
        nColumn = pSelect.pEList.nExpr;
        Debug.Assert( dest.nMem == nColumn );

        /* Set useTempTable to TRUE if the result of the SELECT statement
        ** should be written into a temporary table (template 4).  Set to
        ** FALSE if each* row of the SELECT can be written directly into
        ** the destination table (template 3).
        **
        ** A temp table must be used if the table being updated is also one
        ** of the tables being read by the SELECT statement.  Also use a
        ** temp table in the case of row triggers.
        */
        if ( pTrigger != null || readsTable( pParse, addrSelect, iDb, pTab ) )
        {
          useTempTable = true;
        }

        if ( useTempTable )
        {
          /* Invoke the coroutine to extract information from the SELECT
          ** and add it to a transient table srcTab.  The code generated
          ** here is from the 4th template:
          **
          **      B: open temp table
          **      L: yield X
          **         if EOF goto M
          **         insert row from R..R+n into temp table
          **         goto L
          **      M: ...
          */
          int regRec;      /* Register to hold packed record */
          int regTempRowid;    /* Register to hold temp table ROWID */
          int addrTop;     /* Label "L" */
          int addrIf;      /* Address of jump to M */

          srcTab = pParse.nTab++;
          regRec = sqlite3GetTempReg( pParse );
          regTempRowid = sqlite3GetTempReg( pParse );
          sqlite3VdbeAddOp2( v, OP_OpenEphemeral, srcTab, nColumn );
          addrTop = sqlite3VdbeAddOp1( v, OP_Yield, dest.iParm );
          addrIf = sqlite3VdbeAddOp1( v, OP_If, regEof );
          sqlite3VdbeAddOp3( v, OP_MakeRecord, regFromSelect, nColumn, regRec );
          sqlite3VdbeAddOp2( v, OP_NewRowid, srcTab, regTempRowid );
          sqlite3VdbeAddOp3( v, OP_Insert, srcTab, regRec, regTempRowid );
          sqlite3VdbeAddOp2( v, OP_Goto, 0, addrTop );
          sqlite3VdbeJumpHere( v, addrIf );
          sqlite3ReleaseTempReg( pParse, regRec );
          sqlite3ReleaseTempReg( pParse, regTempRowid );
        }
      }
      else
      {
        /* This is the case if the data for the INSERT is coming from a VALUES
        ** clause
        */
        NameContext sNC;
        sNC = new NameContext();// memset( &sNC, 0, sNC ).Length;
        sNC.pParse = pParse;
        srcTab = -1;
        Debug.Assert( !useTempTable );
        nColumn = pList != null ? pList.nExpr : 0;
        for ( i = 0 ; i < nColumn ; i++ )
        {
          if ( sqlite3ResolveExprNames( sNC, ref pList.a[i].pExpr ) != 0 )
          {
            goto insert_cleanup;
          }
        }
      }

      /* Make sure the number of columns in the source data matches the number
      ** of columns to be inserted into the table.
      */
      if ( IsVirtual( pTab ) )
      {
        for ( i = 0 ; i < pTab.nCol ; i++ )
        {
          nHidden += ( IsHiddenColumn( pTab.aCol[i] ) ? 1 : 0 );
        }
      }
      if ( pColumn == null && nColumn != 0 && nColumn != ( pTab.nCol - nHidden ) )
      {
        sqlite3ErrorMsg( pParse,
        "table %S has %d columns but %d values were supplied",
        pTabList, 0, pTab.nCol - nHidden, nColumn );
        goto insert_cleanup;
      }
      if ( pColumn != null && nColumn != pColumn.nId )
      {
        sqlite3ErrorMsg( pParse, "%d values for %d columns", nColumn, pColumn.nId );
        goto insert_cleanup;
      }

      /* If the INSERT statement included an IDLIST term, then make sure
      ** all elements of the IDLIST really are columns of the table and
      ** remember the column indices.
      **
      ** If the table has an INTEGER PRIMARY KEY column and that column
      ** is named in the IDLIST, then record in the keyColumn variable
      ** the index into IDLIST of the primary key column.  keyColumn is
      ** the index of the primary key as it appears in IDLIST, not as
      ** is appears in the original table.  (The index of the primary
      ** key in the original table is pTab.iPKey.)
      */
      if ( pColumn != null )
      {
        for ( i = 0 ; i < pColumn.nId ; i++ )
        {
          pColumn.a[i].idx = -1;
        }
        for ( i = 0 ; i < pColumn.nId ; i++ )
        {
          for ( j = 0 ; j < pTab.nCol ; j++ )
          {
            if ( sqlite3StrICmp( pColumn.a[i].zName, pTab.aCol[j].zName ) == 0 )
            {
              pColumn.a[i].idx = j;
              if ( j == pTab.iPKey )
              {
                keyColumn = i;
              }
              break;
            }
          }
          if ( j >= pTab.nCol )
          {
            if ( sqlite3IsRowid( pColumn.a[i].zName ) )
            {
              keyColumn = i;
            }
            else
            {
              sqlite3ErrorMsg( pParse, "table %S has no column named %s",
              pTabList, 0, pColumn.a[i].zName );
              pParse.nErr++;
              goto insert_cleanup;
            }
          }
        }
      }

      /* If there is no IDLIST term but the table has an integer primary
      ** key, the set the keyColumn variable to the primary key column index
      ** in the original table definition.
      */
      if ( pColumn == null && nColumn > 0 )
      {
        keyColumn = pTab.iPKey;
      }

      /* Open the temp table for FOR EACH ROW triggers
      */
      if ( pTrigger != null )
      {
        sqlite3VdbeAddOp3( v, OP_OpenPseudo, newIdx, 0, pTab.nCol );
      }

      /* Initialize the count of rows to be inserted
      */
      if ( ( db.flags & SQLITE_CountRows ) != 0 )
      {
        regRowCount = ++pParse.nMem;
        sqlite3VdbeAddOp2( v, OP_Integer, 0, regRowCount );
      }

      /* If this is not a view, open the table and and all indices */
      if ( !isView )
      {
        int nIdx;

        baseCur = pParse.nTab;
        nIdx = sqlite3OpenTableAndIndices( pParse, pTab, baseCur, OP_OpenWrite );
        aRegIdx = new int[nIdx + 1];// sqlite3DbMallocRaw( db, sizeof( int ) * ( nIdx + 1 ) );
        if ( aRegIdx == null )
        {
          goto insert_cleanup;
        }
        for ( i = 0 ; i < nIdx ; i++ )
        {
          aRegIdx[i] = ++pParse.nMem;
        }
      }

      /* This is the top of the main insertion loop */
      if ( useTempTable )
      {
        /* This block codes the top of loop only.  The complete loop is the
        ** following pseudocode (template 4):
        **
        **         rewind temp table
        **      C: loop over rows of intermediate table
        **           transfer values form intermediate table into <table>
        **         end loop
        **      D: ...
        */
        addrInsTop = sqlite3VdbeAddOp1( v, OP_Rewind, srcTab );
        addrCont = sqlite3VdbeCurrentAddr( v );
      }
      else if ( pSelect != null )
      {
        /* This block codes the top of loop only.  The complete loop is the
        ** following pseudocode (template 3):
        **
        **      C: yield X
        **         if EOF goto D
        **         insert the select result into <table> from R..R+n
        **         goto C
        **      D: ...
        */
        addrCont = sqlite3VdbeAddOp1( v, OP_Yield, dest.iParm );
        addrInsTop = sqlite3VdbeAddOp1( v, OP_If, regEof );
      }

      /* Allocate registers for holding the rowid of the new row,
      ** the content of the new row, and the assemblied row record.
      */
      regRecord = ++pParse.nMem;
      regRowid = regIns = pParse.nMem + 1;
      pParse.nMem += pTab.nCol + 1;
      if ( IsVirtual( pTab ) )
      {
        regRowid++;
        pParse.nMem++;
      }
      regData = regRowid + 1;

      /* Run the BEFORE and INSTEAD OF triggers, if there are any
      */
      endOfLoop = sqlite3VdbeMakeLabel( v );
#if !SQLITE_OMIT_TRIGGER
      if ( ( tmask & TRIGGER_BEFORE ) != 0 )
      {
        int regTrigRowid;
        int regCols;
        int regRec;

        /* build the NEW.* reference row.  Note that if there is an INTEGER
        ** PRIMARY KEY into which a NULL is being inserted, that NULL will be
        ** translated into a unique ID for the row.  But on a BEFORE trigger,
        ** we do not know what the unique ID will be (because the insert has
        ** not happened yet) so we substitute a rowid of -1
        */
        regTrigRowid = sqlite3GetTempReg( pParse );
        if ( keyColumn < 0 )
        {
          sqlite3VdbeAddOp2( v, OP_Integer, -1, regTrigRowid );
        }
        else
        {
          int j1;
          if ( useTempTable )
          {
            sqlite3VdbeAddOp3( v, OP_Column, srcTab, keyColumn, regTrigRowid );
          }
          else
          {
            Debug.Assert( pSelect == null );  /* Otherwise useTempTable is true */
            sqlite3ExprCode( pParse, pList.a[keyColumn].pExpr, regTrigRowid );
          }
          j1 = sqlite3VdbeAddOp1( v, OP_NotNull, regTrigRowid );
          sqlite3VdbeAddOp2( v, OP_Integer, -1, regTrigRowid );
          sqlite3VdbeJumpHere( v, j1 );
          sqlite3VdbeAddOp1( v, OP_MustBeInt, regTrigRowid );
        }
        /* Cannot have triggers on a virtual table. If it were possible,
        ** this block would have to account for hidden column.
        */
        Debug.Assert( !IsVirtual( pTab ) );
        /* Create the new column data
        */
        regCols = sqlite3GetTempRange( pParse, pTab.nCol );
        for ( i = 0 ; i < pTab.nCol ; i++ )
        {
          if ( pColumn == null )
          {
            j = i;
          }
          else
          {
            for ( j = 0 ; j < pColumn.nId ; j++ )
            {
              if ( pColumn.a[j].idx == i ) break;
            }
          }
          if ( pColumn != null && j >= pColumn.nId )
          {
            sqlite3ExprCode( pParse, pTab.aCol[i].pDflt, regCols + i );
          }
          else if ( useTempTable )
          {
            sqlite3VdbeAddOp3( v, OP_Column, srcTab, j, regCols + i );
          }
          else
          {
            Debug.Assert( pSelect == null ); /* Otherwise useTempTable is true */
            sqlite3ExprCodeAndCache( pParse, pList.a[j].pExpr, regCols + i );
          }
        }
        regRec = sqlite3GetTempReg( pParse );
        sqlite3VdbeAddOp3( v, OP_MakeRecord, regCols, pTab.nCol, regRec );

        /* If this is an INSERT on a view with an INSTEAD OF INSERT trigger,
        ** do not attempt any conversions before assembling the record.
        ** If this is a real table, attempt conversions as required by the
        ** table column affinities.
        */
        if ( !isView )
        {
          sqlite3TableAffinityStr( v, pTab );
        }
        sqlite3VdbeAddOp3( v, OP_Insert, newIdx, regRec, regTrigRowid );
        sqlite3ReleaseTempReg( pParse, regRec );
        sqlite3ReleaseTempReg( pParse, regTrigRowid );
        sqlite3ReleaseTempRange( pParse, regCols, pTab.nCol );

        /* Fire BEFORE or INSTEAD OF triggers */
        u32 Ref0_1 = 0;
        u32 Ref0_2 = 0;
        if ( sqlite3CodeRowTrigger( pParse, pTrigger, TK_INSERT, null, TRIGGER_BEFORE,
        pTab, newIdx, -1, onError, endOfLoop, ref Ref0_1, ref Ref0_2 ) != 0 )
        {
          goto insert_cleanup;
        }
      }
#endif

      /* Push the record number for the new entry onto the stack.  The
** record number is a randomly generate integer created by NewRowid
** except when the table has an INTEGER PRIMARY KEY column, in which
** case the record number is the same as that column.
*/
      if ( !isView )
      {
        if ( IsVirtual( pTab ) )
        {
          /* The row that the VUpdate opcode will delete: none */
          sqlite3VdbeAddOp2( v, OP_Null, 0, regIns );
        }
        if ( keyColumn >= 0 )
        {
          if ( useTempTable )
          {
            sqlite3VdbeAddOp3( v, OP_Column, srcTab, keyColumn, regRowid );
          }
          else if ( pSelect != null )
          {
            sqlite3VdbeAddOp2( v, OP_SCopy, regFromSelect + keyColumn, regRowid );
          }
          else
          {
            VdbeOp pOp;
            sqlite3ExprCode( pParse, pList.a[keyColumn].pExpr, regRowid );
            pOp = sqlite3VdbeGetOp( v, -1 );
            if ( ALWAYS( pOp != null ) && pOp.opcode == OP_Null && !IsVirtual( pTab ) )
            {
              appendFlag = true;
              pOp.opcode = OP_NewRowid;
              pOp.p1 = baseCur;
              pOp.p2 = regRowid;
              pOp.p3 = regAutoinc;
            }
          }
          /* If the PRIMARY KEY expression is NULL, then use OP_NewRowid
          ** to generate a unique primary key value.
          */
          if ( !appendFlag )
          {
            int j1;
            if ( !IsVirtual( pTab ) )
            {
              j1 = sqlite3VdbeAddOp1( v, OP_NotNull, regRowid );
              sqlite3VdbeAddOp3( v, OP_NewRowid, baseCur, regRowid, regAutoinc );
              sqlite3VdbeJumpHere( v, j1 );
            }
            else
            {
              j1 = sqlite3VdbeCurrentAddr( v );
              sqlite3VdbeAddOp2( v, OP_IsNull, regRowid, j1 + 2 );
            }
            sqlite3VdbeAddOp1( v, OP_MustBeInt, regRowid );
          }
        }
        else if ( IsVirtual( pTab ) )
        {
          sqlite3VdbeAddOp2( v, OP_Null, 0, regRowid );
        }
        else
        {
          sqlite3VdbeAddOp3( v, OP_NewRowid, baseCur, regRowid, regAutoinc );
          appendFlag = true;
        }
        autoIncStep( pParse, regAutoinc, regRowid );

        /* Push onto the stack, data for all columns of the new entry, beginning
        ** with the first column.
        */
        nHidden = 0;
        for ( i = 0 ; i < pTab.nCol ; i++ )
        {
          int iRegStore = regRowid + 1 + i;
          if ( i == pTab.iPKey )
          {
            /* The value of the INTEGER PRIMARY KEY column is always a NULL.
            ** Whenever this column is read, the record number will be substituted
            ** in its place.  So will fill this column with a NULL to avoid
            ** taking up data space with information that will never be used. */
            sqlite3VdbeAddOp2( v, OP_Null, 0, iRegStore );
            continue;
          }
          if ( pColumn == null )
          {
            if ( IsHiddenColumn( pTab.aCol[i] ) )
            {
              Debug.Assert( IsVirtual( pTab ) );
              j = -1;
              nHidden++;
            }
            else
            {
              j = i - nHidden;
            }
          }
          else
          {
            for ( j = 0 ; j < pColumn.nId ; j++ )
            {
              if ( pColumn.a[j].idx == i ) break;
            }
          }
          if ( j < 0 || nColumn == 0 || ( pColumn != null && j >= pColumn.nId ) )
          {
            sqlite3ExprCode( pParse, pTab.aCol[i].pDflt, iRegStore );
          }
          else if ( useTempTable )
          {
            sqlite3VdbeAddOp3( v, OP_Column, srcTab, j, iRegStore );
          }
          else if ( pSelect != null )
          {
            sqlite3VdbeAddOp2( v, OP_SCopy, regFromSelect + j, iRegStore );
          }
          else
          {
            sqlite3ExprCode( pParse, pList.a[j].pExpr, iRegStore );
          }
        }

        /* Generate code to check constraints and generate index keys and
        ** do the insertion.
        */
#if !SQLITE_OMIT_VIRTUALTABLE
    if( IsVirtual(pTab) ){
      const char *pVTab = (const char *)sqlite3GetVTable(db, pTab);
      sqlite3VtabMakeWritable(pParse, pTab);
      sqlite3VdbeAddOp4(v, OP_VUpdate, 1, pTab->nCol+2, regIns, pVTab, P4_VTAB);
    }else
#endif
        {
          int isReplace = 0;    /* Set to true if constraints may cause a replace */
          sqlite3GenerateConstraintChecks( pParse, pTab, baseCur, regIns, aRegIdx,
          keyColumn >= 0, false, onError, endOfLoop, ref isReplace
          );
          sqlite3CompleteInsertion(
          pParse, pTab, baseCur, regIns, aRegIdx, false,
          ( tmask & TRIGGER_AFTER ) != 0 ? newIdx : -1, appendFlag, isReplace == 0
          );
        }
      }

      /* Update the count of rows that are inserted
      */
      if ( ( db.flags & SQLITE_CountRows ) != 0 )
      {
        sqlite3VdbeAddOp2( v, OP_AddImm, regRowCount, 1 );
      }

#if !SQLITE_OMIT_TRIGGER
      if ( pTrigger != null )
      {
        /* Code AFTER triggers */
        u32 Ref0_1 = 0;
        u32 Ref0_2 = 0;
        if ( sqlite3CodeRowTrigger( pParse, pTrigger, TK_INSERT, null, TRIGGER_AFTER,
        pTab, newIdx, -1, onError, endOfLoop, ref Ref0_1, ref Ref0_2 ) != 0 )
        {
          goto insert_cleanup;
        }
      }
#endif

      /* The bottom of the main insertion loop, if the data source
** is a SELECT statement.
*/
      sqlite3VdbeResolveLabel( v, endOfLoop );
      if ( useTempTable )
      {
        sqlite3VdbeAddOp2( v, OP_Next, srcTab, addrCont );
        sqlite3VdbeJumpHere( v, addrInsTop );
        sqlite3VdbeAddOp1( v, OP_Close, srcTab );
      }
      else if ( pSelect != null )
      {
        sqlite3VdbeAddOp2( v, OP_Goto, 0, addrCont );
        sqlite3VdbeJumpHere( v, addrInsTop );
      }

      if ( !IsVirtual( pTab ) && !isView )
      {
        /* Close all tables opened */
        sqlite3VdbeAddOp1( v, OP_Close, baseCur );
        for ( idx = 1, pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext, idx++ )
        {
          sqlite3VdbeAddOp1( v, OP_Close, idx + baseCur );
        }
      }

insert_end:
      /* Update the sqlite_sequence table by storing the content of the
      ** maximum rowid counter values recorded while inserting into
      ** autoincrement tables.
      */
      if ( pParse.nested == 0 && pParse.trigStack == null )
      {
        sqlite3AutoincrementEnd( pParse );
      }

      /*
      ** Return the number of rows inserted. If this routine is
      ** generating code because of a call to sqlite3NestedParse(), do not
      ** invoke the callback function.
      */
      if ( ( db.flags & SQLITE_CountRows ) != 0 && pParse.nested == 0 && pParse.trigStack == null )
      {
        sqlite3VdbeAddOp2( v, OP_ResultRow, regRowCount, 1 );
        sqlite3VdbeSetNumCols( v, 1 );
        sqlite3VdbeSetColName( v, 0, COLNAME_NAME, "rows inserted", SQLITE_STATIC );
      }

insert_cleanup:
      sqlite3SrcListDelete( db, ref pTabList );
      sqlite3ExprListDelete( db, ref pList );
      sqlite3SelectDelete( db, ref pSelect );
      sqlite3IdListDelete( db, ref pColumn );
      //sqlite3DbFree( db, ref aRegIdx );
    }

    /*
    ** Generate code to do constraint checks prior to an INSERT or an UPDATE.
    **
    ** The input is a range of consecutive registers as follows:
    **
    **    1.  The rowid of the row to be updated before the update.  This
    **        value is omitted unless we are doing an UPDATE that involves a
    **        change to the record number or writing to a virtual table.
    **
    **    2.  The rowid of the row after the update.
    **
    **    3.  The data in the first column of the entry after the update.
    **
    **    i.  Data from middle columns...
    **
    **    N.  The data in the last column of the entry after the update.
    **
    ** The regRowid parameter is the index of the register containing (2).
    **
    ** The old rowid shown as entry (1) above is omitted unless both isUpdate
    ** and rowidChng are 1.  isUpdate is true for UPDATEs and false for
    ** INSERTs.  RowidChng means that the new rowid is explicitly specified by
    ** the update or insert statement.  If rowidChng is false, it means that
    ** the rowid is computed automatically in an insert or that the rowid value
    ** is not modified by the update.
    **
    ** The code generated by this routine store new index entries into
    ** registers identified by aRegIdx[].  No index entry is created for
    ** indices where aRegIdx[i]==0.  The order of indices in aRegIdx[] is
    ** the same as the order of indices on the linked list of indices
    ** attached to the table.
    **
    ** This routine also generates code to check constraints.  NOT NULL,
    ** CHECK, and UNIQUE constraints are all checked.  If a constraint fails,
    ** then the appropriate action is performed.  There are five possible
    ** actions: ROLLBACK, ABORT, FAIL, REPLACE, and IGNORE.
    **
    **  Constraint type  Action       What Happens
    **  ---------------  ----------   ----------------------------------------
    **  any              ROLLBACK     The current transaction is rolled back and
    **                                sqlite3_exec() returns immediately with a
    **                                return code of SQLITE_CONSTRAINT.
    **
    **  any              ABORT        Back out changes from the current command
    **                                only (do not do a complete rollback) then
    **                                cause sqlite3_exec() to return immediately
    **                                with SQLITE_CONSTRAINT.
    **
    **  any              FAIL         Sqlite_exec() returns immediately with a
    **                                return code of SQLITE_CONSTRAINT.  The
    **                                transaction is not rolled back and any
    **                                prior changes are retained.
    **
    **  any              IGNORE       The record number and data is popped from
    **                                the stack and there is an immediate jump
    **                                to label ignoreDest.
    **
    **  NOT NULL         REPLACE      The NULL value is replace by the default
    **                                value for that column.  If the default value
    **                                is NULL, the action is the same as ABORT.
    **
    **  UNIQUE           REPLACE      The other row that conflicts with the row
    **                                being inserted is removed.
    **
    **  CHECK            REPLACE      Illegal.  The results in an exception.
    **
    ** Which action to take is determined by the overrideError parameter.
    ** Or if overrideError==OE_Default, then the pParse.onError parameter
    ** is used.  Or if pParse.onError==OE_Default then the onError value
    ** for the constraint is used.
    **
    ** The calling routine must open a read/write cursor for pTab with
    ** cursor number "baseCur".  All indices of pTab must also have open
    ** read/write cursors with cursor number baseCur+i for the i-th cursor.
    ** Except, if there is no possibility of a REPLACE action then
    ** cursors do not need to be open for indices where aRegIdx[i]==0.
    */
    static void sqlite3GenerateConstraintChecks(
    Parse pParse,       /* The parser context */
    Table pTab,         /* the table into which we are inserting */
    int baseCur,        /* Index of a read/write cursor pointing at pTab */
    int regRowid,       /* Index of the range of input registers */
    int[] aRegIdx,      /* Register used by each index.  0 for unused indices */
    bool rowidChng,     /* True if the rowid might collide with existing entry */
    bool isUpdate,      /* True for UPDATE, False for INSERT */
    int overrideError,  /* Override onError to this if not OE_Default */
    int ignoreDest,     /* Jump to this label on an OE_Ignore resolution */
    ref int pbMayReplace   /* OUT: Set to true if constraint may cause a replace */
    )
    {

      int i;               /* loop counter */
      Vdbe v;              /* VDBE under constrution */
      int nCol;            /* Number of columns */
      int onError;         /* Conflict resolution strategy */
      int j1;              /* Addresss of jump instruction */
      int j2 = 0, j3;      /* Addresses of jump instructions */
      int regData;         /* Register containing first data column */
      int iCur;            /* Table cursor number */
      Index pIdx;         /* Pointer to one of the indices */
      bool seenReplace = false; /* True if REPLACE is used to resolve INT PK conflict */
      bool hasTwoRowids = ( isUpdate && rowidChng );

      v = sqlite3GetVdbe( pParse );
      Debug.Assert( v != null );
      Debug.Assert( pTab.pSelect == null );  /* This table is not a VIEW */
      nCol = pTab.nCol;
      regData = regRowid + 1;


      /* Test all NOT NULL constraints.
      */
      for ( i = 0 ; i < nCol ; i++ )
      {
        if ( i == pTab.iPKey )
        {
          continue;
        }
        onError = pTab.aCol[i].notNull;
        if ( onError == OE_None ) continue;
        if ( overrideError != OE_Default )
        {
          onError = overrideError;
        }
        else if ( onError == OE_Default )
        {
          onError = OE_Abort;
        }
        if ( onError == OE_Replace && pTab.aCol[i].pDflt == null )
        {
          onError = OE_Abort;
        }
        Debug.Assert( onError == OE_Rollback || onError == OE_Abort || onError == OE_Fail
        || onError == OE_Ignore || onError == OE_Replace );
        switch ( onError )
        {
          case OE_Rollback:
          case OE_Abort:
          case OE_Fail:
            {
              string zMsg;
              j1 = sqlite3VdbeAddOp3( v, OP_HaltIfNull,
                          SQLITE_CONSTRAINT, onError, regData + i );
              zMsg = sqlite3MPrintf( pParse.db, "%s.%s may not be NULL",
              pTab.zName, pTab.aCol[i].zName );
              sqlite3VdbeChangeP4( v, -1, zMsg, P4_DYNAMIC );
              break;
            }
          case OE_Ignore:
            {
              sqlite3VdbeAddOp2( v, OP_IsNull, regData + i, ignoreDest );
              break;
            }
          default:
            {
              Debug.Assert( onError == OE_Replace );
              j1 = sqlite3VdbeAddOp1( v, OP_NotNull, regData + i );
              sqlite3ExprCode( pParse, pTab.aCol[i].pDflt, regData + i );
              sqlite3VdbeJumpHere( v, j1 );
              break;
            }
        }
      }

      /* Test all CHECK constraints
      */
#if !SQLITE_OMIT_CHECK
      if ( pTab.pCheck != null && ( pParse.db.flags & SQLITE_IgnoreChecks ) == 0 )
      {
        int allOk = sqlite3VdbeMakeLabel( v );
        pParse.ckBase = regData;
        sqlite3ExprIfTrue( pParse, pTab.pCheck, allOk, SQLITE_JUMPIFNULL );
        onError = overrideError != OE_Default ? overrideError : OE_Abort;
        if ( onError == OE_Ignore )
        {
          sqlite3VdbeAddOp2( v, OP_Goto, 0, ignoreDest );
        }
        else
        {
          sqlite3VdbeAddOp2( v, OP_Halt, SQLITE_CONSTRAINT, onError );
        }
        sqlite3VdbeResolveLabel( v, allOk );
      }
#endif // * !SQLITE_OMIT_CHECK) */

      /* If we have an INTEGER PRIMARY KEY, make sure the primary key
** of the new record does not previously exist.  Except, if this
** is an UPDATE and the primary key is not changing, that is OK.
*/
      if ( rowidChng )
      {
        onError = pTab.keyConf;
        if ( overrideError != OE_Default )
        {
          onError = overrideError;
        }
        else if ( onError == OE_Default )
        {
          onError = OE_Abort;
        }

        if ( onError != OE_Replace || pTab.pIndex != null )
        {
          if ( isUpdate )
          {
            j2 = sqlite3VdbeAddOp3( v, OP_Eq, regRowid, 0, regRowid - 1 );
          }
          j3 = sqlite3VdbeAddOp3( v, OP_NotExists, baseCur, 0, regRowid );
          switch ( onError )
          {
            default:
              {
                onError = OE_Abort;
                /* Fall thru into the next case */
              }
              goto case OE_Rollback;
            case OE_Rollback:
            case OE_Abort:
            case OE_Fail:
              {
                sqlite3VdbeAddOp4( v, OP_Halt, SQLITE_CONSTRAINT, onError, 0,
                   "PRIMARY KEY must be unique", P4_STATIC );
                break;
              }
            case OE_Replace:
              {
                sqlite3GenerateRowIndexDelete( pParse, pTab, baseCur, 0 );
                seenReplace = true;
                break;
              }
            case OE_Ignore:
              {
                Debug.Assert( !seenReplace );
                sqlite3VdbeAddOp2( v, OP_Goto, 0, ignoreDest );
                break;
              }
          }
          sqlite3VdbeJumpHere( v, j3 );
          if ( isUpdate )
          {
            sqlite3VdbeJumpHere( v, j2 );
          }
        }
      }

      /* Test all UNIQUE constraints by creating entries for each UNIQUE
      ** index and making sure that duplicate entries do not already exist.
      ** Add the new records to the indices as we go.
      */
      for ( iCur = 0, pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext, iCur++ )
      {
        int regIdx;
        int regR;

        if ( aRegIdx[iCur] == 0 ) continue;  /* Skip unused indices */

        /* Create a key for accessing the index entry */
        regIdx = sqlite3GetTempRange( pParse, pIdx.nColumn + 1 );
        for ( i = 0 ; i < pIdx.nColumn ; i++ )
        {
          int idx = pIdx.aiColumn[i];
          if ( idx == pTab.iPKey )
          {
            sqlite3VdbeAddOp2( v, OP_SCopy, regRowid, regIdx + i );
          }
          else
          {
            sqlite3VdbeAddOp2( v, OP_SCopy, regData + idx, regIdx + i );
          }
        }
        sqlite3VdbeAddOp2( v, OP_SCopy, regRowid, regIdx + i );
        sqlite3VdbeAddOp3( v, OP_MakeRecord, regIdx, pIdx.nColumn + 1, aRegIdx[iCur] );
        sqlite3IndexAffinityStr( v, pIdx );
        sqlite3ExprCacheAffinityChange( pParse, regIdx, pIdx.nColumn + 1 );

        /* Find out what action to take in case there is an indexing conflict */
        onError = pIdx.onError;
        if ( onError == OE_None )
        {
          sqlite3ReleaseTempRange( pParse, regIdx, pIdx.nColumn + 1 );
          continue;  /* pIdx is not a UNIQUE index */
        }

        if ( overrideError != OE_Default )
        {
          onError = overrideError;
        }
        else if ( onError == OE_Default )
        {
          onError = OE_Abort;
        }
        if ( seenReplace )
        {
          if ( onError == OE_Ignore ) onError = OE_Replace;
          else if ( onError == OE_Fail ) onError = OE_Abort;
        }


        /* Check to see if the new index entry will be unique */
        regR = sqlite3GetTempReg( pParse );
        sqlite3VdbeAddOp2( v, OP_SCopy, regRowid - ( hasTwoRowids ? 1 : 0 ), regR );
        j3 = sqlite3VdbeAddOp4( v, OP_IsUnique, baseCur + iCur + 1, 0,
        regR, regIdx,//regR, SQLITE_INT_TO_PTR(regIdx),
        P4_INT32 );
        sqlite3ReleaseTempRange( pParse, regIdx, pIdx.nColumn + 1 );

        /* Generate code that executes if the new index entry is not unique */
        Debug.Assert( onError == OE_Rollback || onError == OE_Abort || onError == OE_Fail
        || onError == OE_Ignore || onError == OE_Replace );
        switch ( onError )
        {
          case OE_Rollback:
          case OE_Abort:
          case OE_Fail:
            {
              int j;
              StrAccum errMsg = new StrAccum();
              string zSep;
              string zErr;

              sqlite3StrAccumInit( errMsg, new StringBuilder( 200 ), 0, 200 );
              errMsg.db = pParse.db;
              zSep = pIdx.nColumn > 1 ? "columns " : "column ";
              for ( j = 0 ; j < pIdx.nColumn ; j++ )
              {
                string zCol = pTab.aCol[pIdx.aiColumn[j]].zName;
                sqlite3StrAccumAppend( errMsg, zSep, -1 );
                zSep = ", ";
                sqlite3StrAccumAppend( errMsg, zCol, -1 );
              }
              sqlite3StrAccumAppend( errMsg,
              pIdx.nColumn > 1 ? " are not unique" : " is not unique", -1 );
              zErr = sqlite3StrAccumFinish( errMsg );
              sqlite3VdbeAddOp4( v, OP_Halt, SQLITE_CONSTRAINT, onError, 0, zErr, 0 );
              //sqlite3DbFree( errMsg.db, zErr );
              break;
            }
          case OE_Ignore:
            {
              Debug.Assert( !seenReplace );
              sqlite3VdbeAddOp2( v, OP_Goto, 0, ignoreDest );
              break;
            }
          default:
            {
              Debug.Assert( onError == OE_Replace );
              sqlite3GenerateRowDelete( pParse, pTab, baseCur, regR, 0 );
              seenReplace = true;
              break;
            }
        }
        sqlite3VdbeJumpHere( v, j3 );
        sqlite3ReleaseTempReg( pParse, regR );
      }
      //if ( pbMayReplace )
      {
        pbMayReplace = seenReplace ? 1 : 0;
      }
    }

    /*
    ** This routine generates code to finish the INSERT or UPDATE operation
    ** that was started by a prior call to sqlite3GenerateConstraintChecks.
    ** A consecutive range of registers starting at regRowid contains the
    ** rowid and the content to be inserted.
    **
    ** The arguments to this routine should be the same as the first six
    ** arguments to sqlite3GenerateConstraintChecks.
    */
    static void sqlite3CompleteInsertion(
    Parse pParse,       /* The parser context */
    Table pTab,         /* the table into which we are inserting */
    int baseCur,        /* Index of a read/write cursor pointing at pTab */
    int regRowid,       /* Range of content */
    int[] aRegIdx,      /* Register used by each index.  0 for unused indices */
    bool isUpdate,      /* True for UPDATE, False for INSERT */
    int newIdx,         /* Index of NEW table for triggers.  -1 if none */
    bool appendBias,    /* True if this is likely to be an append */
    bool useSeekResult  /* True to set the USESEEKRESULT flag on OP_[Idx]Insert */
    )
    {
      int i;
      Vdbe v;
      int nIdx;
      Index pIdx;
      u8 pik_flags;
      int regData;
      int regRec;

      v = sqlite3GetVdbe( pParse );
      Debug.Assert( v != null );
      Debug.Assert( pTab.pSelect == null );  /* This table is not a VIEW */
      for ( nIdx = 0, pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext, nIdx++ ) { }
      for ( i = nIdx - 1 ; i >= 0 ; i-- )
      {
        if ( aRegIdx[i] == 0 ) continue;
        sqlite3VdbeAddOp2( v, OP_IdxInsert, baseCur + i + 1, aRegIdx[i] );
        if ( useSeekResult )
        {
          sqlite3VdbeChangeP5( v, OPFLAG_USESEEKRESULT );
        }
      }
      regData = regRowid + 1;
      regRec = sqlite3GetTempReg( pParse );
      sqlite3VdbeAddOp3( v, OP_MakeRecord, regData, pTab.nCol, regRec );
      sqlite3TableAffinityStr( v, pTab );
      sqlite3ExprCacheAffinityChange( pParse, regData, pTab.nCol );
#if !SQLITE_OMIT_TRIGGER
      if ( newIdx >= 0 )
      {
        sqlite3VdbeAddOp3( v, OP_Insert, newIdx, regRec, regRowid );
      }
#endif
      if ( pParse.nested != 0 )
      {
        pik_flags = 0;
      }
      else
      {
        pik_flags = OPFLAG_NCHANGE;
        pik_flags |= ( isUpdate ? OPFLAG_ISUPDATE : OPFLAG_LASTROWID );
      }
      if ( appendBias )
      {
        pik_flags |= OPFLAG_APPEND;
      }
      if ( useSeekResult )
      {
        pik_flags |= OPFLAG_USESEEKRESULT;
      }
      sqlite3VdbeAddOp3( v, OP_Insert, baseCur, regRec, regRowid );
      if ( pParse.nested == 0 )
      {
        sqlite3VdbeChangeP4( v, -1, pTab.zName, P4_STATIC );
      }
      sqlite3VdbeChangeP5( v, pik_flags );
    }

    /*
    ** Generate code that will open cursors for a table and for all
    ** indices of that table.  The "baseCur" parameter is the cursor number used
    ** for the table.  Indices are opened on subsequent cursors.
    **
    ** Return the number of indices on the table.
    */
    static int sqlite3OpenTableAndIndices(
    Parse pParse,   /* Parsing context */
    Table pTab,     /* Table to be opened */
    int baseCur,    /* VdbeCursor number assigned to the table */
    int op          /* OP_OpenRead or OP_OpenWrite */
    )
    {
      int i;
      int iDb;
      Index pIdx;
      Vdbe v;

      if ( IsVirtual( pTab ) ) return 0;
      iDb = sqlite3SchemaToIndex( pParse.db, pTab.pSchema );
      v = sqlite3GetVdbe( pParse );
      Debug.Assert( v != null );
      sqlite3OpenTable( pParse, baseCur, iDb, pTab, op );
      for ( i = 1, pIdx = pTab.pIndex ; pIdx != null ; pIdx = pIdx.pNext, i++ )
      {
        KeyInfo pKey = sqlite3IndexKeyinfo( pParse, pIdx );
        Debug.Assert( pIdx.pSchema == pTab.pSchema );
        sqlite3VdbeAddOp4( v, op, i + baseCur, pIdx.tnum, iDb,
        pKey, P4_KEYINFO_HANDOFF );
#if SQLITE_DEBUG
        VdbeComment( v, "%s", pIdx.zName );
#endif
      }
      if ( pParse.nTab < baseCur + i )
      {
        pParse.nTab = baseCur + i;
      }
      return i - 1;
    }


#if  SQLITE_TEST
    /*
** The following global variable is incremented whenever the
** transfer optimization is used.  This is used for testing
** purposes only - to make sure the transfer optimization really
** is happening when it is suppose to.
*/
    //static int sqlite3_xferopt_count = 0;
#endif // * SQLITE_TEST */


#if !SQLITE_OMIT_XFER_OPT
    /*
** Check to collation names to see if they are compatible.
*/
    static bool xferCompatibleCollation( string z1, string z2 )
    {
      if ( z1 == null )
      {
        return z2 == null;
      }
      if ( z2 == null )
      {
        return false;
      }
      return sqlite3StrICmp( z1, z2 ) == 0;
    }


    /*
    ** Check to see if index pSrc is compatible as a source of data
    ** for index pDest in an insert transfer optimization.  The rules
    ** for a compatible index:
    **
    **    *   The index is over the same set of columns
    **    *   The same DESC and ASC markings occurs on all columns
    **    *   The same onError processing (OE_Abort, OE_Ignore, etc)
    **    *   The same collating sequence on each column
    */
    static bool xferCompatibleIndex( Index pDest, Index pSrc )
    {
      int i;
      Debug.Assert( pDest != null && pSrc != null );
      Debug.Assert( pDest.pTable != pSrc.pTable );
      if ( pDest.nColumn != pSrc.nColumn )
      {
        return false;   /* Different number of columns */
      }
      if ( pDest.onError != pSrc.onError )
      {
        return false;   /* Different conflict resolution strategies */
      }
      for ( i = 0 ; i < pSrc.nColumn ; i++ )
      {
        if ( pSrc.aiColumn[i] != pDest.aiColumn[i] )
        {
          return false;   /* Different columns indexed */
        }
        if ( pSrc.aSortOrder[i] != pDest.aSortOrder[i] )
        {
          return false;   /* Different sort orders */
        }
        if ( !xferCompatibleCollation( pSrc.azColl[i], pDest.azColl[i] ) )
        {
          return false;   /* Different collating sequences */
        }
      }

      /* If no test above fails then the indices must be compatible */
      return true;
    }

    /*
    ** Attempt the transfer optimization on INSERTs of the form
    **
    **     INSERT INTO tab1 SELECT * FROM tab2;
    **
    ** This optimization is only attempted if
    **
    **    (1)  tab1 and tab2 have identical schemas including all the
    **         same indices and constraints
    **
    **    (2)  tab1 and tab2 are different tables
    **
    **    (3)  There must be no triggers on tab1
    **
    **    (4)  The result set of the SELECT statement is "*"
    **
    **    (5)  The SELECT statement has no WHERE, HAVING, ORDER BY, GROUP BY,
    **         or LIMIT clause.
    **
    **    (6)  The SELECT statement is a simple (not a compound) select that
    **         contains only tab2 in its FROM clause
    **
    ** This method for implementing the INSERT transfers raw records from
    ** tab2 over to tab1.  The columns are not decoded.  Raw records from
    ** the indices of tab2 are transfered to tab1 as well.  In so doing,
    ** the resulting tab1 has much less fragmentation.
    **
    ** This routine returns TRUE if the optimization is attempted.  If any
    ** of the conditions above fail so that the optimization should not
    ** be attempted, then this routine returns FALSE.
    */
    static int xferOptimization(
    Parse pParse,         /* Parser context */
    Table pDest,          /* The table we are inserting into */
    Select pSelect,       /* A SELECT statement to use as the data source */
    int onError,          /* How to handle constraint errors */
    int iDbDest           /* The database of pDest */
    )
    {
      ExprList pEList;                 /* The result set of the SELECT */
      Table pSrc;                      /* The table in the FROM clause of SELECT */
      Index pSrcIdx, pDestIdx;         /* Source and destination indices */
      SrcList_item pItem;              /* An element of pSelect.pSrc */
      int i;                           /* Loop counter */
      int iDbSrc;                      /* The database of pSrc */
      int iSrc, iDest;                 /* Cursors from source and destination */
      int addr1, addr2;                /* Loop addresses */
      int emptyDestTest;               /* Address of test for empty pDest */
      int emptySrcTest;                /* Address of test for empty pSrc */
      Vdbe v;                          /* The VDBE we are building */
      KeyInfo pKey;                    /* Key information for an index */
      int regAutoinc;                  /* Memory register used by AUTOINC */
      bool destHasUniqueIdx = false;   /* True if pDest has a UNIQUE index */
      int regData, regRowid;           /* Registers holding data and rowid */

      if ( pSelect == null )
      {
        return 0;   /* Must be of the form  INSERT INTO ... SELECT ... */
      }
#if !SQLITE_OMIT_TRIGGER
      if ( sqlite3TriggerList( pParse, pDest ) != null )
      {
        return 0;   /* tab1 must not have triggers */
      }
#endif

      if ( ( pDest.tabFlags & TF_Virtual ) != 0 )
      {
        return 0;   /* tab1 must not be a virtual table */
      }
      if ( onError == OE_Default )
      {
        onError = OE_Abort;
      }
      if ( onError != OE_Abort && onError != OE_Rollback )
      {
        return 0;   /* Cannot do OR REPLACE or OR IGNORE or OR FAIL */
      }
      Debug.Assert( pSelect.pSrc != null );   /* allocated even if there is no FROM clause */
      if ( pSelect.pSrc.nSrc != 1 )
      {
        return 0;   /* FROM clause must have exactly one term */
      }
      if ( pSelect.pSrc.a[0].pSelect != null )
      {
        return 0;   /* FROM clause cannot contain a subquery */
      }
      if ( pSelect.pWhere != null )
      {
        return 0;   /* SELECT may not have a WHERE clause */
      }
      if ( pSelect.pOrderBy != null )
      {
        return 0;   /* SELECT may not have an ORDER BY clause */
      }
      /* Do not need to test for a HAVING clause.  If HAVING is present but
      ** there is no ORDER BY, we will get an error. */
      if ( pSelect.pGroupBy != null )
      {
        return 0;   /* SELECT may not have a GROUP BY clause */
      }
      if ( pSelect.pLimit != null )
      {
        return 0;   /* SELECT may not have a LIMIT clause */
      }
      Debug.Assert( pSelect.pOffset == null );  /* Must be so if pLimit==0 */
      if ( pSelect.pPrior != null )
      {
        return 0;   /* SELECT may not be a compound query */
      }
      if ( ( pSelect.selFlags & SF_Distinct ) != 0 )
      {
        return 0;   /* SELECT may not be DISTINCT */
      }
      pEList = pSelect.pEList;
      Debug.Assert( pEList != null );
      if ( pEList.nExpr != 1 )
      {
        return 0;   /* The result set must have exactly one column */
      }
      Debug.Assert( pEList.a[0].pExpr != null );
      if ( pEList.a[0].pExpr.op != TK_ALL )
      {
        return 0;   /* The result set must be the special operator "*" */
      }

      /* At this point we have established that the statement is of the
      ** correct syntactic form to participate in this optimization.  Now
      ** we have to check the semantics.
      */
      pItem = pSelect.pSrc.a[0];
      pSrc = sqlite3LocateTable( pParse, 0, pItem.zName, pItem.zDatabase );
      if ( pSrc == null )
      {
        return 0;   /* FROM clause does not contain a real table */
      }
      if ( pSrc == pDest )
      {
        return 0;   /* tab1 and tab2 may not be the same table */
      }
      if ( ( pSrc.tabFlags & TF_Virtual ) != 0 )
      {
        return 0;   /* tab2 must not be a virtual table */
      }
      if ( pSrc.pSelect != null )
      {
        return 0;   /* tab2 may not be a view */
      }
      if ( pDest.nCol != pSrc.nCol )
      {
        return 0;   /* Number of columns must be the same in tab1 and tab2 */
      }
      if ( pDest.iPKey != pSrc.iPKey )
      {
        return 0;   /* Both tables must have the same INTEGER PRIMARY KEY */
      }
      for ( i = 0 ; i < pDest.nCol ; i++ )
      {
        if ( pDest.aCol[i].affinity != pSrc.aCol[i].affinity )
        {
          return 0;    /* Affinity must be the same on all columns */
        }
        if ( !xferCompatibleCollation( pDest.aCol[i].zColl, pSrc.aCol[i].zColl ) )
        {
          return 0;    /* Collating sequence must be the same on all columns */
        }
        if ( pDest.aCol[i].notNull != 0 && pSrc.aCol[i].notNull == 0 )
        {
          return 0;    /* tab2 must be NOT NULL if tab1 is */
        }
      }
      for ( pDestIdx = pDest.pIndex ; pDestIdx != null ; pDestIdx = pDestIdx.pNext )
      {
        if ( pDestIdx.onError != OE_None )
        {
          destHasUniqueIdx = true;
        }
        for ( pSrcIdx = pSrc.pIndex ; pSrcIdx != null ; pSrcIdx = pSrcIdx.pNext )
        {
          if ( xferCompatibleIndex( pDestIdx, pSrcIdx ) ) break;
        }
        if ( pSrcIdx == null )
        {
          return 0;    /* pDestIdx has no corresponding index in pSrc */
        }
      }
#if !SQLITE_OMIT_CHECK
      if ( pDest.pCheck != null && !sqlite3ExprCompare( pSrc.pCheck, pDest.pCheck ) )
      {
        return 0;   /* Tables have different CHECK constraints.  Ticket #2252 */
      }
#endif

      /* If we get this far, it means either:
**
**    *   We can always do the transfer if the table contains an
**        an integer primary key
**
**    *   We can conditionally do the transfer if the destination
**        table is empty.
*/
#if  SQLITE_TEST
      sqlite3_xferopt_count.iValue++;
#endif
      iDbSrc = sqlite3SchemaToIndex( pParse.db, pSrc.pSchema );
      v = sqlite3GetVdbe( pParse );
      sqlite3CodeVerifySchema( pParse, iDbSrc );
      iSrc = pParse.nTab++;
      iDest = pParse.nTab++;
      regAutoinc = autoIncBegin( pParse, iDbDest, pDest );
      sqlite3OpenTable( pParse, iDest, iDbDest, pDest, OP_OpenWrite );
      if ( ( pDest.iPKey < 0 && pDest.pIndex != null ) || destHasUniqueIdx )
      {
        /* If tables do not have an INTEGER PRIMARY KEY and there
        ** are indices to be copied and the destination is not empty,
        ** we have to disallow the transfer optimization because the
        ** the rowids might change which will mess up indexing.
        **
        ** Or if the destination has a UNIQUE index and is not empty,
        ** we also disallow the transfer optimization because we cannot
        ** insure that all entries in the union of DEST and SRC will be
        ** unique.
        */
        addr1 = sqlite3VdbeAddOp2( v, OP_Rewind, iDest, 0 );
        emptyDestTest = sqlite3VdbeAddOp2( v, OP_Goto, 0, 0 );
        sqlite3VdbeJumpHere( v, addr1 );
      }
      else
      {
        emptyDestTest = 0;
      }
      sqlite3OpenTable( pParse, iSrc, iDbSrc, pSrc, OP_OpenRead );
      emptySrcTest = sqlite3VdbeAddOp2( v, OP_Rewind, iSrc, 0 );
      regData = sqlite3GetTempReg( pParse );
      regRowid = sqlite3GetTempReg( pParse );
      if ( pDest.iPKey >= 0 )
      {
        addr1 = sqlite3VdbeAddOp2( v, OP_Rowid, iSrc, regRowid );
        addr2 = sqlite3VdbeAddOp3( v, OP_NotExists, iDest, 0, regRowid );
        sqlite3VdbeAddOp4( v, OP_Halt, SQLITE_CONSTRAINT, onError, 0,
        "PRIMARY KEY must be unique", P4_STATIC );
        sqlite3VdbeJumpHere( v, addr2 );
        autoIncStep( pParse, regAutoinc, regRowid );
      }
      else if ( pDest.pIndex == null )
      {
        addr1 = sqlite3VdbeAddOp2( v, OP_NewRowid, iDest, regRowid );
      }
      else
      {
        addr1 = sqlite3VdbeAddOp2( v, OP_Rowid, iSrc, regRowid );
        Debug.Assert( ( pDest.tabFlags & TF_Autoincrement ) == 0 );
      }
      sqlite3VdbeAddOp2( v, OP_RowData, iSrc, regData );
      sqlite3VdbeAddOp3( v, OP_Insert, iDest, regData, regRowid );
      sqlite3VdbeChangeP5( v, OPFLAG_NCHANGE | OPFLAG_LASTROWID | OPFLAG_APPEND );
      sqlite3VdbeChangeP4( v, -1, pDest.zName, 0 );
      sqlite3VdbeAddOp2( v, OP_Next, iSrc, addr1 );
      for ( pDestIdx = pDest.pIndex ; pDestIdx != null ; pDestIdx = pDestIdx.pNext )
      {
        for ( pSrcIdx = pSrc.pIndex ; pSrcIdx != null ; pSrcIdx = pSrcIdx.pNext )
        {
          if ( xferCompatibleIndex( pDestIdx, pSrcIdx ) ) break;
        }
        Debug.Assert( pSrcIdx != null );
        sqlite3VdbeAddOp2( v, OP_Close, iSrc, 0 );
        sqlite3VdbeAddOp2( v, OP_Close, iDest, 0 );
        pKey = sqlite3IndexKeyinfo( pParse, pSrcIdx );
        sqlite3VdbeAddOp4( v, OP_OpenRead, iSrc, pSrcIdx.tnum, iDbSrc,
        pKey, P4_KEYINFO_HANDOFF );
#if SQLITE_DEBUG
        VdbeComment( v, "%s", pSrcIdx.zName );
#endif
        pKey = sqlite3IndexKeyinfo( pParse, pDestIdx );
        sqlite3VdbeAddOp4( v, OP_OpenWrite, iDest, pDestIdx.tnum, iDbDest,
        pKey, P4_KEYINFO_HANDOFF );
#if SQLITE_DEBUG
        VdbeComment( v, "%s", pDestIdx.zName );
#endif
        addr1 = sqlite3VdbeAddOp2( v, OP_Rewind, iSrc, 0 );
        sqlite3VdbeAddOp2( v, OP_RowKey, iSrc, regData );
        sqlite3VdbeAddOp3( v, OP_IdxInsert, iDest, regData, 1 );
        sqlite3VdbeAddOp2( v, OP_Next, iSrc, addr1 + 1 );
        sqlite3VdbeJumpHere( v, addr1 );
      }
      sqlite3VdbeJumpHere( v, emptySrcTest );
      sqlite3ReleaseTempReg( pParse, regRowid );
      sqlite3ReleaseTempReg( pParse, regData );
      sqlite3VdbeAddOp2( v, OP_Close, iSrc, 0 );
      sqlite3VdbeAddOp2( v, OP_Close, iDest, 0 );
      if ( emptyDestTest != 0 )
      {
        sqlite3VdbeAddOp2( v, OP_Halt, SQLITE_OK, 0 );
        sqlite3VdbeJumpHere( v, emptyDestTest );
        sqlite3VdbeAddOp2( v, OP_Close, iDest, 0 );
        return 0;
      }
      else
      {
        return 1;
      }
    }
#endif // * SQLITE_OMIT_XFER_OPT */
    /* Make sure "isView" gets undefined in case this file becomes part of
** the amalgamation - so that subsequent files do not see isView as a
** macro. */
    //#undef isView
  }
}
