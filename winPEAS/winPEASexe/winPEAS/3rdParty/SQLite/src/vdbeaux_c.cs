using System;
using System.Diagnostics;
using System.Text;

using FILE = System.IO.TextWriter;
using i32 = System.Int32;
using i64 = System.Int64;
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;

using Pgno = System.UInt32;

namespace CS_SQLite3
{
  using Op = CSSQLite.VdbeOp;
  using sqlite3_stmt = CSSQLite.Vdbe;
  using sqlite3_value = CSSQLite.Mem;

  public partial class CSSQLite
  {
    /*
    ** 2003 September 6
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains code used for creating, destroying, and populating
    ** a VDBE (or an "sqlite3_stmt" as it is known to the outside world.)  Prior
    ** to version 2.8.7, all this code was combined into the vdbe.c source file.
    ** But that file was getting too big so this subroutines were split out.
    **
    ** $Id: vdbeaux.c,v 1.480 2009/08/08 18:01:08 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
    //#include "vdbeInt.h"



    /*
    ** When debugging the code generator in a symbolic debugger, one can
    ** set the sqlite3VdbeAddopTrace to 1 and all opcodes will be printed
    ** as they are added to the instruction stream.
    */
#if  SQLITE_DEBUG
    static bool sqlite3VdbeAddopTrace = false;
#endif


    /*
** Create a new virtual database engine.
*/
    static Vdbe sqlite3VdbeCreate( sqlite3 db )
    {
      Vdbe p;
      p = new Vdbe();// sqlite3DbMallocZero(db, Vdbe).Length;
      if ( p == null ) return null;
      p.db = db;
      if ( db.pVdbe != null )
      {
        db.pVdbe.pPrev = p;
      }
      p.pNext = db.pVdbe;
      p.pPrev = null;
      db.pVdbe = p;
      p.magic = VDBE_MAGIC_INIT;
      return p;
    }

    /*
    ** Remember the SQL string for a prepared statement.
    */
    static void sqlite3VdbeSetSql( Vdbe p, string z, int n, int isPrepareV2 )
    {
      if ( p == null ) return;
#if SQLITE_OMIT_TRACE
if( !isPrepareV2 ) return;
#endif
      Debug.Assert( p.zSql == "" );
      p.zSql = z.Substring( 0, n );// sqlite3DbStrNDup(p.db, z, n);
      p.isPrepareV2 = isPrepareV2 != 0;
    }

    /*
    ** Return the SQL associated with a prepared statement
    */
    static string sqlite3_sql( sqlite3_stmt pStmt )
    {
      Vdbe p = (Vdbe)pStmt;
      return ( p.isPrepareV2 ? p.zSql : "" );
    }

    /*
    ** Swap all content between two VDBE structures.
    */
    static void sqlite3VdbeSwap( Vdbe pA, Vdbe pB )
    {
      Vdbe tmp = new Vdbe(); Vdbe pTmp = new Vdbe();
      string zTmp;
      pA.CopyTo( tmp );
      pB.CopyTo( pA );
      tmp.CopyTo( pB );
      pTmp = pA.pNext;
      pA.pNext = pB.pNext;
      pB.pNext = pTmp;
      pTmp = pA.pPrev;
      pA.pPrev = pB.pPrev;
      pB.pPrev = pTmp;
      zTmp = pA.zSql;
      pA.zSql = pB.zSql;
      pB.zSql = zTmp;
    }

#if  SQLITE_DEBUG
    /*
** Turn tracing on or off
*/
    static void sqlite3VdbeTrace( Vdbe p, FILE trace )
    {
      p.trace = trace;
    }
#endif

    /*
** Resize the Vdbe.aOp array so that it is at least one op larger than
** it was.
**
** If an out-of-memory error occurs while resizing the array, return
** SQLITE_NOMEM. In this case Vdbe.aOp and Vdbe.nOpAlloc remain
** unchanged (this is so that any opcodes already allocated can be
** correctly deallocated along with the rest of the Vdbe).
*/
    static int growOpArray( Vdbe p )
    {
      //VdbeOp pNew;
      int nNew = ( p.nOpAlloc != 0 ? p.nOpAlloc * 2 : 1024 / 4 );//(int)(1024/sizeof(Op)));
      // pNew = sqlite3DbRealloc( p.db, p.aOp, nNew * sizeof( Op ) );
      //if (pNew != null)
      //{
      //      p.nOpAlloc = sqlite3DbMallocSize(p.db, pNew)/sizeof(Op);
      //  p.aOp = pNew;
      //}
      p.nOpAlloc = nNew;
      if ( p.aOp == null ) p.aOp = new VdbeOp[nNew]; else Array.Resize( ref p.aOp, nNew );
      return ( p.aOp != null ? SQLITE_OK : SQLITE_NOMEM ); //  return (pNew ? SQLITE_OK : SQLITE_NOMEM);
    }

    /*
    ** Add a new instruction to the list of instructions current in the
    ** VDBE.  Return the address of the new instruction.
    **
    ** Parameters:
    **
    **    p               Pointer to the VDBE
    **
    **    op              The opcode for this instruction
    **
    **    p1, p2, p3      Operands
    **
    ** Use the sqlite3VdbeResolveLabel() function to fix an address and
    ** the sqlite3VdbeChangeP4() function to change the value of the P4
    ** operand.
    */
    static int sqlite3VdbeAddOp3( Vdbe p, int op, int p1, int p2, int p3 )
    {
      int i;
      VdbeOp pOp;

      i = p.nOp;
      Debug.Assert( p.magic == VDBE_MAGIC_INIT );
      Debug.Assert( op > 0 && op < 0xff );
      if ( p.nOpAlloc <= i )
      {
        if ( growOpArray( p ) != 0 )
        {
          return 1;
        }
      }
      p.nOp++;
      if ( p.aOp[i] == null ) p.aOp[i] = new VdbeOp();
      pOp = p.aOp[i];
      pOp.opcode = (u8)op;
      pOp.p5 = 0;
      pOp.p1 = p1;
      pOp.p2 = p2;
      pOp.p3 = p3;
      pOp.p4.p = null;
      pOp.p4type = P4_NOTUSED;
      p.expired = false;
      //sqlite3VdbePrintOp(null, i, p.aOp[i]);
#if  SQLITE_DEBUG
      pOp.zComment = null;
      if ( sqlite3VdbeAddopTrace ) sqlite3VdbePrintOp( null, i, p.aOp[i] );
#endif
#if VDBE_PROFILE
pOp.cycles = 0;
pOp.cnt = 0;
#endif
      return i;
    }
    static int sqlite3VdbeAddOp0( Vdbe p, int op )
    {
      return sqlite3VdbeAddOp3( p, op, 0, 0, 0 );
    }
    static int sqlite3VdbeAddOp1( Vdbe p, int op, int p1 )
    {
      return sqlite3VdbeAddOp3( p, op, p1, 0, 0 );
    }
    static int sqlite3VdbeAddOp2( Vdbe p, int op, int p1, bool b2 )
    {
      return sqlite3VdbeAddOp2( p, op, p1, (int)( b2 ? 1 : 0 ) );
    }

    static int sqlite3VdbeAddOp2( Vdbe p, int op, int p1, int p2 )
    {
      return sqlite3VdbeAddOp3( p, op, p1, p2, 0 );
    }


    /*
    ** Add an opcode that includes the p4 value as a pointer.
    */
    //P4_INT32
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, i32 pP4, int p4type )
    {
      union_p4 _p4 = new union_p4(); _p4.i = pP4;
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    //char
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, char pP4, int p4type )
    {
      union_p4 _p4 = new union_p4(); _p4.z = pP4.ToString();
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    //String
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, string pP4, int p4type )
    {
      //      Debug.Assert( pP4 != null );
      union_p4 _p4 = new union_p4(); _p4.z = pP4;
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, byte[] pP4, int p4type )
    {
      Debug.Assert( op == OP_Null || pP4 != null );
      union_p4 _p4 = new union_p4(); _p4.z = Encoding.UTF8.GetString( pP4 );
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    //P4_INTARRAY
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, int[] pP4, int p4type )
    {
      Debug.Assert( pP4 != null );
      union_p4 _p4 = new union_p4(); _p4.ai = pP4;
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }
    //P4_INT64
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, i64 pP4, int p4type )
    {
      union_p4 _p4 = new union_p4(); _p4.pI64 = pP4;
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    //DOUBLE (REAL)
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, double pP4, int p4type )
    {
      union_p4 _p4 = new union_p4(); _p4.pReal = pP4;
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    //FUNCDEF
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, FuncDef pP4, int p4type )
    {
      union_p4 _p4 = new union_p4(); _p4.pFunc = pP4;
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    //CollSeq
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, CollSeq pP4, int p4type )
    {
      union_p4 _p4 = new union_p4(); _p4.pColl = pP4;
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    //KeyInfo
    static int sqlite3VdbeAddOp4( Vdbe p, int op, int p1, int p2, int p3, KeyInfo pP4, int p4type )
    {
      union_p4 _p4 = new union_p4(); _p4.pKeyInfo = pP4;
      int addr = sqlite3VdbeAddOp3( p, op, p1, p2, p3 );
      sqlite3VdbeChangeP4( p, addr, _p4, p4type );
      return addr;
    }

    //  static int sqlite3VdbeAddOp4(
    //  Vdbe p,               /* Add the opcode to this VM */
    //  int op,               /* The new opcode */
    //  int p1,               /* The P1 operand */
    //  int p2,               /* The P2 operand */
    //  int p3,               /* The P3 operand */
    //  union_p4 _p4,         /* The P4 operand */
    //  int p4type            /* P4 operand type */
    //)
    //  {
    //    int addr = sqlite3VdbeAddOp3(p, op, p1, p2, p3);
    //    sqlite3VdbeChangeP4(p, addr, _p4, p4type);
    //    return addr;
    //  }

    /*
    ** Create a new symbolic label for an instruction that has yet to be
    ** coded.  The symbolic label is really just a negative number.  The
    ** label can be used as the P2 value of an operation.  Later, when
    ** the label is resolved to a specific address, the VDBE will scan
    ** through its operation list and change all values of P2 which match
    ** the label into the resolved address.
    **
    ** The VDBE knows that a P2 value is a label because labels are
    ** always negative and P2 values are suppose to be non-negative.
    ** Hence, a negative P2 value is a label that has yet to be resolved.
    **
    ** Zero is returned if a malloc() fails.
    */
    static int sqlite3VdbeMakeLabel( Vdbe p )
    {
      int i;
      i = p.nLabel++;
      Debug.Assert( p.magic == VDBE_MAGIC_INIT );
      if ( i >= p.nLabelAlloc )
      {
        int n = p.nLabelAlloc * 2 + 5;
        Array.Resize( ref p.aLabel, n );
        //p.aLabel = sqlite3DbReallocOrFree(p.db, p.aLabel,
        //                                       n*sizeof(p.aLabel[0]));
        p.nLabelAlloc = p.aLabel.Length;//sqlite3DbMallocSize(p.db, p.aLabel)/sizeof(p.aLabel[0]);
      }
      if ( p.aLabel != null )
      {
        p.aLabel[i] = -1;
      }
      return -1 - i;
    }

    /*
    ** Resolve label "x" to be the address of the next instruction to
    ** be inserted.  The parameter "x" must have been obtained from
    ** a prior call to sqlite3VdbeMakeLabel().
    */
    static void sqlite3VdbeResolveLabel( Vdbe p, int x )
    {
      int j = -1 - x;
      Debug.Assert( p.magic == VDBE_MAGIC_INIT );
      Debug.Assert( j >= 0 && j < p.nLabel );
      if ( p.aLabel != null )
      {
        p.aLabel[j] = p.nOp;
      }
    }

    /*
    ** Loop through the program looking for P2 values that are negative
    ** on jump instructions.  Each such value is a label.  Resolve the
    ** label by setting the P2 value to its correct non-zero value.
    **
    ** This routine is called once after all opcodes have been inserted.
    **
    ** Variable pMaxFuncArgs is set to the maximum value of any P2 argument
    ** to an OP_Function, OP_AggStep or OP_VFilter opcode. This is used by
    ** sqlite3VdbeMakeReady() to size the Vdbe.apArg[] array.
    **
    ** This routine also does the following optimization:  It scans for
    ** instructions that might cause a statement rollback.  Such instructions
    ** are:
    **
    **   *  OP_Halt with P1=SQLITE_CONSTRAINT and P2=OE_Abort.
    **   *  OP_Destroy
    **   *  OP_VUpdate
    **   *  OP_VRename
    **
    ** If no such instruction is found, then every Statement instruction
    ** is changed to a Noop.  In this way, we avoid creating the statement
    ** journal file unnecessarily.
    */
    static void resolveP2Values( Vdbe p, ref int pMaxFuncArgs )
    {
      int i;
      int nMaxArgs = 0;
      Op pOp;
      int[] aLabel = p.aLabel;
      bool doesStatementRollback = false;
      bool hasStatementBegin = false;
      p.readOnly = true;
      p.usesStmtJournal = false;
      for ( i = 0 ; i < p.nOp ; i++ )
      {
        pOp = p.aOp[i];
        int opcode = pOp.opcode;

        if ( opcode == OP_Function || opcode == OP_AggStep )
        {
          if ( pOp.p5 > nMaxArgs ) nMaxArgs = pOp.p5;
        }
        else if ( opcode == OP_VUpdate )
        {
          if ( pOp.p2 > nMaxArgs ) nMaxArgs = pOp.p2;
        }
        if ( opcode == OP_Halt )
        {
          if ( pOp.p1 == SQLITE_CONSTRAINT && pOp.p2 == OE_Abort )
          {
            doesStatementRollback = true;
          }
        }
        else if ( opcode == OP_Statement )
        {
          hasStatementBegin = true;
          p.usesStmtJournal = true;
        }
        else if ( opcode == OP_Destroy )
        {
          doesStatementRollback = true;
        }
        else if ( opcode == OP_Transaction && pOp.p2 != 0 )
        {
          p.readOnly = false;
#if ! SQLITE_OMIT_VIRTUALTABLE
}else if( opcode==OP_VUpdate || opcode==OP_VRename ){
doesStatementRollback = 1;
}else if( opcode==OP_VFilter ){
int n;
Debug.Assert( i < p.nOp - 3 );
Debug.Assert( pOp[-1].opcode==OP_Integer );
n = pOp[-1].p1;
if( n>nMaxArgs ) nMaxArgs = n;
#endif
        }

        if ( sqlite3VdbeOpcodeHasProperty( opcode, OPFLG_JUMP ) && pOp.p2 < 0 )
        {
          Debug.Assert( -1 - pOp.p2 < p.nLabel );
          pOp.p2 = aLabel[-1 - pOp.p2];
        }
      }
      //sqlite3DbFree( p.db, ref p.aLabel );

      pMaxFuncArgs = nMaxArgs;

      /* If we never rollback a statement transaction, then statement
      ** transactions are not needed.  So change every OP_Statement
      ** opcode into an OP_Noop.  This avoid a call to sqlite3OsOpenExclusive()
      ** which can be expensive on some platforms.
      */
      if ( hasStatementBegin && !doesStatementRollback )
      {
        p.usesStmtJournal = false;
        for ( i = 0 ; i < p.nOp ; i++ )
        {
          pOp = p.aOp[i];
          if ( pOp.opcode == OP_Statement )
          {
            pOp.opcode = OP_Noop;
          }
        }
      }
    }

    /*
    ** Return the address of the next instruction to be inserted.
    */
    static int sqlite3VdbeCurrentAddr( Vdbe p )
    {
      Debug.Assert( p.magic == VDBE_MAGIC_INIT );
      return p.nOp;
    }

    /*
    ** Add a whole list of operations to the operation stack.  Return the
    ** address of the first operation added.
    */
    static int sqlite3VdbeAddOpList( Vdbe p, int nOp, VdbeOpList[] aOp )
    {
      int addr;
      Debug.Assert( p.magic == VDBE_MAGIC_INIT );
      if ( p.nOp + nOp > p.nOpAlloc && growOpArray( p ) != 0 )
      {
        return 0;
      }
      addr = p.nOp;
      if ( ALWAYS( nOp > 0 ) )
      {
        int i;
        VdbeOpList pIn;
        for ( i = 0 ; i < nOp ; i++ )
        {
          pIn = aOp[i];
          int p2 = pIn.p2;
          if ( p.aOp[i + addr] == null ) p.aOp[i + addr] = new VdbeOp();
          VdbeOp pOut = p.aOp[i + addr];
          pOut.opcode = pIn.opcode;
          pOut.p1 = pIn.p1;
          if ( p2 < 0 && sqlite3VdbeOpcodeHasProperty( pOut.opcode, OPFLG_JUMP ) )
          {
            pOut.p2 = addr + ( -1 - p2 );// ADDR(p2);
          }
          else
          {
            pOut.p2 = p2;
          }
          pOut.p3 = pIn.p3;
          pOut.p4type = P4_NOTUSED;
          pOut.p4.p = null;
          pOut.p5 = 0;
#if  SQLITE_DEBUG
          pOut.zComment = null;
          if ( sqlite3VdbeAddopTrace )
          {
            sqlite3VdbePrintOp( null, i + addr, p.aOp[i + addr] );
          }
#endif
        }
        p.nOp += nOp;
      }
      return addr;
    }

    /*
    ** Change the value of the P1 operand for a specific instruction.
    ** This routine is useful when a large program is loaded from a
    ** static array using sqlite3VdbeAddOpList but we want to make a
    ** few minor changes to the program.
    */
    static void sqlite3VdbeChangeP1( Vdbe p, int addr, int val )
    {
      Debug.Assert( p != null );
      Debug.Assert( addr >= 0 );
      if ( p.nOp > addr )
      {
        p.aOp[addr].p1 = val;
      }
    }

    /*
    ** Change the value of the P2 operand for a specific instruction.
    ** This routine is useful for setting a jump destination.
    */
    static void sqlite3VdbeChangeP2( Vdbe p, int addr, int val )
    {
      Debug.Assert( p != null );
      Debug.Assert( addr >= 0 );
      if ( p.nOp > addr )
      {
        p.aOp[addr].p2 = val;
      }
    }

    /*
    ** Change the value of the P3 operand for a specific instruction.
    */
    static void sqlite3VdbeChangeP3( Vdbe p, int addr, int val )
    {
      Debug.Assert( p != null );
      Debug.Assert( addr >= 0 );
      if ( p.nOp > addr )
      {
        p.aOp[addr].p3 = val;
      }
    }

    /*
    ** Change the value of the P5 operand for the most recently
    ** added operation.
    */
    static void sqlite3VdbeChangeP5( Vdbe p, u8 val )
    {
      Debug.Assert( p != null );
      if ( p.aOp != null )
      {
        Debug.Assert( p.nOp > 0 );
        p.aOp[p.nOp - 1].p5 = val;
      }
    }

    /*
    ** Change the P2 operand of instruction addr so that it points to
    ** the address of the next instruction to be coded.
    */
    static void sqlite3VdbeJumpHere( Vdbe p, int addr )
    {
      sqlite3VdbeChangeP2( p, addr, p.nOp );
    }


    /*
    ** If the input FuncDef structure is ephemeral, then free it.  If
    ** the FuncDef is not ephermal, then do nothing.
    */
    static void freeEphemeralFunction( sqlite3 db, FuncDef pDef )
    {
      if ( ALWAYS( pDef ) && ( pDef.flags & SQLITE_FUNC_EPHEM ) != 0 )
      {
        pDef = null;
        //sqlite3DbFree( db, ref  pDef );
      }
    }

    /*
    ** Delete a P4 value if necessary.
    */
    static void freeP4( sqlite3 db, int p4type, object p4 )
    {
      if ( p4 != null )
      {
        switch ( p4type )
        {
          case P4_REAL:
          case P4_INT64:
          case P4_MPRINTF:
          case P4_DYNAMIC:
          case P4_KEYINFO:
          case P4_INTARRAY:
          case P4_KEYINFO_HANDOFF:
            {
              //sqlite3DbFree( db, ref p4 );
              break;
            }
          case P4_VDBEFUNC:
            {
              VdbeFunc pVdbeFunc = (VdbeFunc)p4;
              freeEphemeralFunction( db, pVdbeFunc.pFunc );
              sqlite3VdbeDeleteAuxData( pVdbeFunc, 0 );
              //sqlite3DbFree( db, ref pVdbeFunc );
              break;
            }
          case P4_FUNCDEF:
            {
              freeEphemeralFunction( db, (FuncDef)p4 );
              break;
            }
          case P4_MEM:
            {
              p4 = null;// sqlite3ValueFree(ref (sqlite3_value)p4);
              break;
            }
          case P4_VTAB:
            {
              sqlite3VtabUnlock( (VTable)p4 );
              break;
            }
        }
      }
    }


    /*
    ** Change N opcodes starting at addr to No-ops.
    */
    static void sqlite3VdbeChangeToNoop( Vdbe p, int addr, int N )
    {
      if ( p.aOp != null )
      {
        sqlite3 db = p.db;
        while ( N-- > 0 )
        {
          VdbeOp pOp = p.aOp[addr + N];
          freeP4( db, pOp.p4type, pOp.p4.p );
          pOp = p.aOp[addr + N] = new VdbeOp();//memset(pOp, 0, sizeof(pOp[0]));
          pOp.opcode = OP_Noop;
          //pOp++;
        }
      }
    }

    /*
    ** Change the value of the P4 operand for a specific instruction.
    ** This routine is useful when a large program is loaded from a
    ** static array using sqlite3VdbeAddOpList but we want to make a
    ** few minor changes to the program.
    **
    ** If n>=0 then the P4 operand is dynamic, meaning that a copy of
    ** the string is made into memory obtained from sqlite3Malloc().
    ** A value of n==0 means copy bytes of zP4 up to and including the
    ** first null byte.  If n>0 then copy n+1 bytes of zP4.
    **
    ** If n==P4_KEYINFO it means that zP4 is a pointer to a KeyInfo structure.
    ** A copy is made of the KeyInfo structure into memory obtained from
    ** sqlite3Malloc, to be freed when the Vdbe is finalized.
    ** n==P4_KEYINFO_HANDOFF indicates that zP4 points to a KeyInfo structure
    ** stored in memory that the caller has obtained from sqlite3Malloc. The
    ** caller should not free the allocation, it will be freed when the Vdbe is
    ** finalized.
    **
    ** Other values of n (P4_STATIC, P4_COLLSEQ etc.) indicate that zP4 points
    ** to a string or structure that is guaranteed to exist for the lifetime of
    ** the Vdbe. In these cases we can just copy the pointer.
    **
    ** If addr<0 then change P4 on the most recently inserted instruction.
    */

    //P4_COLLSEQ
    static void sqlite3VdbeChangeP4( Vdbe p, int addr, CollSeq pColl, int n )
    {
      union_p4 _p4 = new union_p4(); _p4.pColl = pColl;
      sqlite3VdbeChangeP4( p, addr, _p4, n );
    }
    //P4_FUNCDEF
    static void sqlite3VdbeChangeP4( Vdbe p, int addr, FuncDef pFunc, int n )
    {
      union_p4 _p4 = new union_p4(); _p4.pFunc = pFunc;
      sqlite3VdbeChangeP4( p, addr, _p4, n );
    }
    //P4_INT32
    static void sqlite3VdbeChangeP4( Vdbe p, int addr, int i32n, int n )
    {
      union_p4 _p4 = new union_p4(); _p4.i = i32n;
      sqlite3VdbeChangeP4( p, addr, _p4, n );
    }

    //P4_KEYINFO
    static void sqlite3VdbeChangeP4( Vdbe p, int addr, KeyInfo pKeyInfo, int n )
    {
      union_p4 _p4 = new union_p4(); _p4.pKeyInfo = pKeyInfo;
      sqlite3VdbeChangeP4( p, addr, _p4, n );
    }
    //CHAR
    static void sqlite3VdbeChangeP4( Vdbe p, int addr, char c, int n )
    {
      union_p4 _p4 = new union_p4(); _p4.z = c.ToString();
      sqlite3VdbeChangeP4( p, addr, _p4, n );
    }

    //MEM
    static void sqlite3VdbeChangeP4( Vdbe p, int addr, Mem m, int n )
    {
      union_p4 _p4 = new union_p4(); _p4.pMem = m;
      sqlite3VdbeChangeP4( p, addr, _p4, n );
    }

    //STRING

    //STRING + Type
    static void sqlite3VdbeChangeP4( Vdbe p, int addr, string z, dxDel P4_Type )
    {
      union_p4 _p4 = new union_p4();
      _p4.z = z;
      sqlite3VdbeChangeP4( p, addr, _p4, P4_DYNAMIC );
    }

    static void sqlite3VdbeChangeP4( Vdbe p, int addr, string z, int n )
    {
      union_p4 _p4 = new union_p4();
      if ( n > 0 && n <= z.Length ) _p4.z = z.Substring( 0, n );
      else _p4.z = z;
      sqlite3VdbeChangeP4( p, addr, _p4, n );
    }

    static void sqlite3VdbeChangeP4( Vdbe p, int addr, union_p4 _p4, int n )
    {
      Op pOp;
      sqlite3 db;
      Debug.Assert( p != null );
      db = p.db;
      Debug.Assert( p.magic == VDBE_MAGIC_INIT );
      if ( p.aOp == null /*|| db.mallocFailed != 0 */)
      {
        if ( n != P4_KEYINFO && n != P4_VTAB )
        {
          freeP4( db, n, _p4 );
        }
        return;
      }
      Debug.Assert( p.nOp > 0 );
      Debug.Assert( addr < p.nOp );
      if ( addr < 0 )
      {
        addr = p.nOp - 1;
      }
      pOp = p.aOp[addr];
      freeP4( db, pOp.p4type, pOp.p4.p );
      pOp.p4.p = null;
      if ( n == P4_INT32 )
      {
        /* Note: this cast is safe, because the origin data point was an int
        ** that was cast to a (const char *). */
        pOp.p4.i = _p4.i; // SQLITE_PTR_TO_INT(zP4);
        pOp.p4type = P4_INT32;
      }
      else if ( n == P4_INT64 )
      {
        pOp.p4.pI64 = _p4.pI64;
        pOp.p4type = n;
      }
      else if ( n == P4_REAL )
      {
        pOp.p4.pReal = _p4.pReal;
        pOp.p4type = n;
      }
      else if ( _p4 == null )
      {
        pOp.p4.p = null;
        pOp.p4type = P4_NOTUSED;
      }
      else if ( n == P4_KEYINFO )
      {
        KeyInfo pKeyInfo;
        int nField, nByte;

        nField = _p4.pKeyInfo.nField;
        //nByte = sizeof(*pKeyInfo) + (nField-1)*sizeof(pKeyInfo.aColl[0]) + nField;
        pKeyInfo = new KeyInfo();//sqlite3Malloc( nByte );
        pOp.p4.pKeyInfo = pKeyInfo;
        if ( pKeyInfo != null )
        {
          //u8 *aSortOrder;
          //memcpy(pKeyInfo, zP4, nByte);
          //aSortOrder = pKeyInfo.aSortOrder;
          //if( aSortOrder ){
          //  pKeyInfo.aSortOrder = (unsigned char*)&pKeyInfo.aColl[nField];
          //  memcpy(pKeyInfo.aSortOrder, aSortOrder, nField);
          //}
          pKeyInfo = _p4.pKeyInfo.Copy();
          pOp.p4type = P4_KEYINFO;
        }
        else
        {
          //p.db.mallocFailed = 1;
          pOp.p4type = P4_NOTUSED;
        }
        pOp.p4.pKeyInfo = _p4.pKeyInfo;
        pOp.p4type = P4_KEYINFO;
      }
      else if ( n == P4_KEYINFO_HANDOFF || n == P4_KEYINFO_STATIC )
      {
        pOp.p4.pKeyInfo = _p4.pKeyInfo;
        pOp.p4type = P4_KEYINFO;
      }
      else if ( n == P4_FUNCDEF )
      {
        pOp.p4.pFunc = _p4.pFunc;
        pOp.p4type = P4_FUNCDEF;
      }
      else if ( n == P4_COLLSEQ )
      {
        pOp.p4.pColl = _p4.pColl;
        pOp.p4type = P4_COLLSEQ;
      }
      else if ( n == P4_DYNAMIC || n == P4_STATIC )
      {
        pOp.p4.z = _p4.z;
        pOp.p4type = P4_DYNAMIC;
      }
      else if ( n == P4_MEM )
      {
        pOp.p4.pMem = _p4.pMem;
        pOp.p4type = P4_MEM;
      }
      else if ( n == P4_INTARRAY )
      {
        pOp.p4.ai = _p4.ai;
        pOp.p4type = P4_INTARRAY;
      }
      else if ( n == P4_VTAB )
      {
        pOp.p4.pVtab = _p4.pVtab;
        pOp.p4type = P4_VTAB;
        sqlite3VtabLock( _p4.pVtab );
        Debug.Assert( ( _p4.pVtab ).db == p.db );
      }
      else if ( n < 0 )
      {
        pOp.p4.p = _p4.p;
        pOp.p4type = n;
      }
      else
      {
        //if (n == 0) n =  n = sqlite3Strlen30(zP4);
        pOp.p4.z = _p4.z;// sqlite3DbStrNDup(p.db, zP4, n);
        pOp.p4type = P4_DYNAMIC;
      }
    }

#if !NDEBUG
    /*
** Change the comment on the the most recently coded instruction.  Or
** insert a No-op and add the comment to that new instruction.  This
** makes the code easier to read during debugging.  None of this happens
** in a production build.
*/
    static void sqlite3VdbeComment( Vdbe p, string zFormat, params object[] ap )
    {
      //      va_list ap;
      Debug.Assert( p.nOp > 0 || p.aOp == null );
      Debug.Assert( p.aOp == null || p.aOp[p.nOp - 1].zComment == null /* || p.db.mallocFailed != 0 */);
      if ( p.nOp != 0 )
      {
        string pz;// = p.aOp[p.nOp-1].zComment;
        va_start( ap, zFormat );
        //sqlite3DbFree(db,ref pz);
        pz = sqlite3VMPrintf( p.db, zFormat, ap );
        p.aOp[p.nOp - 1].zComment = pz;
        va_end( ap );
      }
    }
    static void sqlite3VdbeNoopComment( Vdbe p, string zFormat, params object[] ap )
    {
      //va_list ap;
      sqlite3VdbeAddOp0( p, OP_Noop );
      Debug.Assert( p.nOp > 0 || p.aOp == null );
      Debug.Assert( p.aOp == null || p.aOp[p.nOp - 1].zComment == null /* || p.db.mallocFailed != 0 */);
      if ( p.nOp != 0 )
      {
        string pz; // = p.aOp[p.nOp - 1].zComment;
        va_start( ap, zFormat );
        //sqlite3DbFree(db,ref pz);
        pz = sqlite3VMPrintf( p.db, zFormat, ap );
        p.aOp[p.nOp - 1].zComment = pz;
        va_end( ap );
      }
    }
#else
#endif  //* NDEBUG */


    /*
** Return the opcode for a given address.  If the address is -1, then
** return the most recently inserted opcode.
**
** If a memory allocation error has occurred prior to the calling of this
** routine, then a pointer to a dummy VdbeOp will be returned.  That opcode
** is readable and writable, but it has no effect.  The return of a dummy
** opcode allows the call to continue functioning after a OOM fault without
** having to check to see if the return from this routine is a valid pointer.
**
** About the #if SQLITE_OMIT_TRACE:  Normally, this routine is never called
** unless p->nOp>0.  This is because in the absense of SQLITE_OMIT_TRACE,
** an OP_Trace instruction is always inserted by sqlite3VdbeGet() as soon as
** a new VDBE is created.  So we are free to set addr to p->nOp-1 without
** having to double-check to make sure that the result is non-negative. But
** if SQLITE_OMIT_TRACE is defined, the OP_Trace is omitted and we do need to
** check the value of p->nOp-1 before continuing.
*/
    static VdbeOp sqlite3VdbeGetOp( Vdbe p, int addr )
    {
      Debug.Assert( p.magic == VDBE_MAGIC_INIT );
      if ( addr < 0 )
      {
#if SQLITE_OMIT_TRACE
      VdbeOp dummy = null;
if( p.nOp==0 ) return dummy;
#endif
        addr = p.nOp - 1;
      }
      Debug.Assert( ( addr >= 0 && addr < p.nOp ) /* || p.db.mallocFailed != 0 */);
      //if ( p.db.mallocFailed != 0 )
      //{
      //  return dummy;
      //}
      //else
      {
        return p.aOp[addr];
      }
    }

#if !SQLITE_OMIT_EXPLAIN || !NDEBUG || VDBE_PROFILE || SQLITE_DEBUG
    /*
** Compute a string that describes the P4 parameter for an opcode.
** Use zTemp for any required temporary buffer space.
*/
    static string displayP4( Op pOp, string zBuffer, int nTemp )
    {
      StringBuilder zTemp = new StringBuilder( 100 );
      Debug.Assert( nTemp >= 20 );
      switch ( pOp.p4type )
      {
        case P4_KEYINFO_STATIC:
        case P4_KEYINFO:
          {
            int i, j;
            KeyInfo pKeyInfo = pOp.p4.pKeyInfo;
            sqlite3_snprintf( nTemp, ref zTemp, "keyinfo(%d", pKeyInfo.nField );
            i = sqlite3Strlen30( zTemp );
            for ( j = 0 ; j < pKeyInfo.nField ; j++ )
            {
              CollSeq pColl = pKeyInfo.aColl[j];
              if ( pColl != null )
              {
                int n = sqlite3Strlen30( pColl.zName );
                if ( i + n > nTemp )
                {
                  zTemp.Append( ",..." ); // memcpy( &zTemp[i], ",...", 4 );
                  break;
                }
                zTemp.Append( "," );// zTemp[i++] = ',';
                if ( pKeyInfo.aSortOrder != null && pKeyInfo.aSortOrder[j] != 0 )
                {
                  zTemp.Append( "-" );// zTemp[i++] = '-';
                }
                zTemp.Append( pColl.zName );// memcpy( &zTemp[i], pColl.zName, n + 1 );
                i += n;
              }
              else if ( i + 4 < nTemp )
              {
                zTemp.Append( ",nil" );// memcpy( &zTemp[i], ",nil", 4 );
                i += 4;
              }
            }
            zTemp.Append( ")" );// zTemp[i++] = ')';
            //zTemp[i] = 0;
            Debug.Assert( i < nTemp );
            break;
          }
        case P4_COLLSEQ:
          {
            CollSeq pColl = pOp.p4.pColl;
            sqlite3_snprintf( nTemp, ref zTemp, "collseq(%.20s)", ( pColl != null ? pColl.zName : "null" ) );
            break;
          }
        case P4_FUNCDEF:
          {
            FuncDef pDef = pOp.p4.pFunc;
            sqlite3_snprintf( nTemp, ref zTemp, "%s(%d)", pDef.zName, pDef.nArg );
            break;
          }
        case P4_INT64:
          {
            sqlite3_snprintf( nTemp, ref zTemp, "%lld", pOp.p4.pI64 );
            break;
          }
        case P4_INT32:
          {
            sqlite3_snprintf( nTemp, ref zTemp, "%d", pOp.p4.i );
            break;
          }
        case P4_REAL:
          {
            sqlite3_snprintf( nTemp, ref zTemp, "%.16g", pOp.p4.pReal );
            break;
          }
        case P4_MEM:
          {
            Mem pMem = pOp.p4.pMem;
            Debug.Assert( ( pMem.flags & MEM_Null ) == 0 );
            if ( ( pMem.flags & MEM_Str ) != 0 )
            {
              zTemp.Append( pMem.z );
            }
            else if ( ( pMem.flags & MEM_Int ) != 0 )
            {
              sqlite3_snprintf( nTemp, ref zTemp, "%lld", pMem.u.i );
            }
            else if ( ( pMem.flags & MEM_Real ) != 0 )
            {
              sqlite3_snprintf( nTemp, ref zTemp, "%.16g", pMem.r );
            }
            break;
          }
#if ! SQLITE_OMIT_VIRTUALTABLE
case P4_VTAB: {
sqlite3_vtab pVtab = pOp.p4.pVtab.pVtab;
sqlite3_snprintf(nTemp, ref zTemp, "vtab:%p:%p", pVtab, pVtab.pModule);
break;
}
#endif
        case P4_INTARRAY:
          {
            sqlite3_snprintf( nTemp, ref zTemp, "intarray" );
            break;
          }
        default:
          {
            if ( pOp.p4.z != null ) zTemp.Append( pOp.p4.z );
            //if ( zTemp == null )
            //{
            //  zTemp = "";
            //}
            break;
          }
      }
      Debug.Assert( zTemp != null );
      return zTemp.ToString();
    }
#endif

    /*
** Declare to the Vdbe that the BTree object at db.aDb[i] is used.
**
*/
    static void sqlite3VdbeUsesBtree( Vdbe p, int i )
    {
      int mask;
      Debug.Assert( i >= 0 && i < p.db.nDb && i < sizeof( u32 ) * 8 );
      Debug.Assert( i < sizeof( int ) * 8 );
      mask = (int)( (u32)1 ) << i;
      if ( ( p.btreeMask & mask ) == 0 )
      {
        p.btreeMask |= mask;
        sqlite3BtreeMutexArrayInsert( p.aMutex, p.db.aDb[i].pBt );
      }
    }


#if VDBE_PROFILE || SQLITE_DEBUG
    /*
** Print a single opcode.  This routine is used for debugging only.
*/
    static void sqlite3VdbePrintOp( FILE pOut, int pc, Op pOp )
    {
      string zP4;
      string zPtr = null;
      string zFormat1 = "%4d %-13s %4d %4d %4d %-4s %.2X %s\n";
      if ( pOut == null ) pOut = System.Console.Out;
      zP4 = displayP4( pOp, zPtr, 50 );
      string zOut = "";
      sqlite3_snprintf( 999, ref zOut, zFormat1, pc,
      sqlite3OpcodeName( pOp.opcode ), pOp.p1, pOp.p2, pOp.p3, zP4, pOp.p5,
#if  SQLITE_DEBUG
 pOp.zComment != null ? pOp.zComment : ""
#else
""
#endif
 );
      pOut.Write( zOut );
      //fflush(pOut);
    }
#endif

    /*
** Release an array of N Mem elements
*/
    static void releaseMemArray( Mem[] p, int N )
    {
      if ( p != null && p[0] != null && N != 0 )
      {
        Mem pEnd;
        sqlite3 db = p[0].db;
        //u8 malloc_failed =  db.mallocFailed;
        for ( int i = 0 ; i < N ; i++ )//pEnd =  p[N] ; p < pEnd ; p++ )
        {
          pEnd = p[i];
          Debug.Assert( //( p[1] ) == pEnd ||
          N == 1 || p[0].db == p[1].db );

          /* This block is really an inlined version of sqlite3VdbeMemRelease()
          ** that takes advantage of the fact that the memory cell value is
          ** being set to NULL after releasing any dynamic resources.
          **
          ** The justification for duplicating code is that according to
          ** callgrind, this causes a certain test case to hit the CPU 4.7
          ** percent less (x86 linux, gcc version 4.1.2, -O6) than if
          ** sqlite3MemRelease() were called from here. With -O2, this jumps
          ** to 6.6 percent. The test case is inserting 1000 rows into a table
          ** with no indexes using a single prepared INSERT statement, bind()
          ** and reset(). Inserts are grouped into a transaction.
          */
          if ( ( pEnd.flags & ( MEM_Agg | MEM_Dyn ) ) != 0 )
          {
            sqlite3VdbeMemRelease( pEnd );
          }
          //else if ( pEnd.zMalloc != null )
          //{
          //  //sqlite3DbFree( db, ref pEnd.zMalloc );
          //  pEnd.zMalloc = 0;
          //}
          pEnd._Mem = null;
          pEnd.z = null;
          pEnd.n = 0;
          pEnd.zBLOB = null;
          pEnd.flags = MEM_Null;
        }
//        db.mallocFailed = malloc_failed;
      }
    }

#if SQLITE_ENABLE_MEMORY_MANAGEMENT
int sqlite3VdbeReleaseBuffers(Vdbe *p){
int ii;
int nFree = 0;
assert( sqlite3_mutex_held(p.db.mutex) );
for(ii=1; ii<=p.nMem; ii++){
Mem *pMem = &p.aMem[ii];
if( pMem.flags & MEM_RowSet ){
sqlite3RowSetClear(pMem.u.pRowSet);
}
if( pMem.z && pMem.flags&MEM_Dyn ){
assert( !pMem.xDel );
nFree += sqlite3DbMallocSize(pMem.db, pMem.z);
sqlite3VdbeMemRelease(pMem);
}
}
return nFree;
}
#endif

#if ! SQLITE_OMIT_EXPLAIN
    /*
** Give a listing of the program in the virtual machine.
**
** The interface is the same as sqlite3VdbeExec().  But instead of
** running the code, it invokes the callback once for each instruction.
** This feature is used to implement "EXPLAIN".
**
** When p.explain==1, each instruction is listed.  When
** p.explain==2, only OP_Explain instructions are listed and these
** are shown in a different format.  p.explain==2 is used to implement
** EXPLAIN QUERY PLAN.
*/
    static int sqlite3VdbeList(
    Vdbe p                   /* The VDBE */
    )
    {
      sqlite3 db = p.db;
      int i;
      int rc = SQLITE_OK;
      p.pResultSet = new Mem[p.nMem];//Mem* pMem = p.pResultSet = p.aMem[1];
      Mem pMem;
      Debug.Assert( p.explain != 0 );
      Debug.Assert( p.magic == VDBE_MAGIC_RUN );
#if SQL_DEBUG
Debug.Assert(db.magic == SQLITE_MAGIC_BUSY);
#endif
      Debug.Assert( p.rc == SQLITE_OK || p.rc == SQLITE_BUSY || p.rc == SQLITE_NOMEM );
      /* Even though this opcode does not use dynamic strings for
      ** the result, result columns may become dynamic if the user calls
      ** sqlite3_column_text16(), causing a translation to UTF-16 encoding.
      */
      releaseMemArray( p.pResultSet, p.nMem );

      if ( p.rc == SQLITE_NOMEM )
      {
        /* This happens if a malloc() inside a call to sqlite3_column_text() or
        ** sqlite3_column_text16() failed.  */
////        db.mallocFailed = 1;
        return SQLITE_ERROR;
      }

      int i_pMem = 0; if ( p.pResultSet[i_pMem] == null ) p.pResultSet[i_pMem] = new Mem();
      pMem = p.pResultSet[i_pMem++];
      do
      {
        i = p.pc++;
      } while ( i < p.nOp && p.explain == 2 && p.aOp[i].opcode != OP_Explain );
      if ( i >= p.nOp )
      {
        p.rc = SQLITE_OK;
        rc = SQLITE_DONE;
      }
      else if ( db.u1.isInterrupted )
      {
        p.rc = SQLITE_INTERRUPT;
        rc = SQLITE_ERROR;
        sqlite3SetString( ref p.zErrMsg, db, sqlite3ErrStr( p.rc ) );
      }
      else
      {
        string z;
        Op pOp = p.aOp[i];
        if ( p.explain == 1 )
        {
          pMem.flags = MEM_Int;
          pMem.type = SQLITE_INTEGER;
          pMem.u.i = i;                                /* Program counter */
          if ( p.pResultSet[i_pMem] == null ) p.pResultSet[i_pMem] = new Mem();
          pMem = p.pResultSet[i_pMem++]; //pMem++;

          pMem.flags = MEM_Static | MEM_Str | MEM_Term;
          pMem.z = sqlite3OpcodeName( pOp.opcode );  /* Opcode */
          Debug.Assert( pMem.z != null );
          pMem.n = sqlite3Strlen30( pMem.z );
          pMem.type = SQLITE_TEXT;
          pMem.enc = SQLITE_UTF8;
          if ( p.pResultSet[i_pMem] == null ) p.pResultSet[i_pMem] = new Mem();
          pMem = p.pResultSet[i_pMem++]; //pMem++;
        }

        pMem.flags = MEM_Int;
        pMem.u.i = pOp.p1;                          /* P1 */
        pMem.type = SQLITE_INTEGER;
        if ( p.pResultSet[i_pMem] == null ) p.pResultSet[i_pMem] = new Mem();
        pMem = p.pResultSet[i_pMem++]; //pMem++;

        pMem.flags = MEM_Int;
        pMem.u.i = pOp.p2;                          /* P2 */
        pMem.type = SQLITE_INTEGER;
        if ( p.pResultSet[i_pMem] == null ) p.pResultSet[i_pMem] = new Mem();
        pMem = p.pResultSet[i_pMem++]; //pMem++;

        if ( p.explain == 1 )
        {
          pMem.flags = MEM_Int;
          pMem.u.i = pOp.p3;                          /* P3 */
          pMem.type = SQLITE_INTEGER;
          if ( p.pResultSet[i_pMem] == null ) p.pResultSet[i_pMem] = new Mem();
          pMem = p.pResultSet[i_pMem++]; //pMem++;
        }

        if ( sqlite3VdbeMemGrow( pMem, 32, 0 ) != 0 )
        {                                                     /* P4 */
          //Debug.Assert( p.db.mallocFailed != 0 );
          return SQLITE_ERROR;
        }
        pMem.flags = MEM_Dyn | MEM_Str | MEM_Term;
        z = displayP4( pOp, pMem.z, 32 );
        if ( z != pMem.z )
        {
          sqlite3VdbeMemSetStr( pMem, z, -1, SQLITE_UTF8, null );
        }
        else
        {
          Debug.Assert( pMem.z != null );
          pMem.n = sqlite3Strlen30( pMem.z );
          pMem.enc = SQLITE_UTF8;
        }
        pMem.type = SQLITE_TEXT;
        if ( p.pResultSet[i_pMem] == null ) p.pResultSet[i_pMem] = new Mem();
        pMem = p.pResultSet[i_pMem++]; //pMem++;

        if ( p.explain == 1 )
        {
          if ( sqlite3VdbeMemGrow( pMem, 4, 0 ) != 0 )
          {
            //Debug.Assert( p.db.mallocFailed != 0 );
            return SQLITE_ERROR;
          }
          pMem.flags = MEM_Dyn | MEM_Str | MEM_Term;
          pMem.n = 2;
          pMem.z = pOp.p5.ToString( "x2" );  //sqlite3_snprintf( 3, pMem.z, "%.2x", pOp.p5 );   /* P5 */
          pMem.type = SQLITE_TEXT;
          pMem.enc = SQLITE_UTF8;
          if ( p.pResultSet[i_pMem] == null ) p.pResultSet[i_pMem] = new Mem();
          pMem = p.pResultSet[i_pMem++]; // pMem++;

#if SQLITE_DEBUG
          if ( pOp.zComment != null )
          {
            pMem.flags = MEM_Str | MEM_Term;
            pMem.z = pOp.zComment;
            pMem.n = pMem.z == null ? 0 : sqlite3Strlen30( pMem.z );
            pMem.enc = SQLITE_UTF8;
            pMem.type = SQLITE_TEXT;
          }
          else
#endif
          {
            pMem.flags = MEM_Null;                       /* Comment */
            pMem.type = SQLITE_NULL;
          }
        }

        p.nResColumn = (u16)( 8 - 5 * ( p.explain - 1 ) );
        p.rc = SQLITE_OK;
        rc = SQLITE_ROW;
      }
      return rc;
    }
#endif // * SQLITE_OMIT_EXPLAIN */

#if  SQLITE_DEBUG
    /*
** Print the SQL that was used to generate a VDBE program.
*/
    static void sqlite3VdbePrintSql( Vdbe p )
    {
      int nOp = p.nOp;
      VdbeOp pOp;
      if ( nOp < 1 ) return;
      pOp = p.aOp[0];
      if ( pOp.opcode == OP_Trace && pOp.p4.z != null )
      {
        string z = pOp.p4.z;
        z = z.Trim();// while ( sqlite3Isspace( *(u8*)z ) ) z++;
        Console.Write( "SQL: [%s]\n", z );
      }
    }
#endif

#if !SQLITE_OMIT_TRACE && SQLITE_ENABLE_IOTRACE
/*
** Print an IOTRACE message showing SQL content.
*/
static void sqlite3VdbeIOTraceSql( Vdbe p )
{
int nOp = p.nOp;
VdbeOp pOp;
if ( SQLite3IoTrace == false ) return;
if ( nOp < 1 ) return;
pOp = p.aOp[0];
if ( pOp.opcode == OP_Trace && pOp.p4.z != null )
{
int i, j;
string z = "";//char z[1000];
sqlite3_snprintf( 1000, ref  z, "%s", pOp.p4.z );
//for(i=0; sqlite3Isspace(z[i]); i++){}
//for(j=0; z[i]; i++){
//if( sqlite3Isspace(z[i]) ){
//if( z[i-1]!=' ' ){
//z[j++] = ' ';
//}
//}else{
//z[j++] = z[i];
//}
//}
//z[j] = 0;
//z = z.Trim( z );
sqlite3IoTrace( "SQL %s\n", z.Trim() );
}
}
#endif // * !SQLITE_OMIT_TRACE  && SQLITE_ENABLE_IOTRACE */

    /*
** Allocate space from a fixed size buffer.  Make *pp point to the
** allocated space.  (Note:  pp is a char* rather than a void** to
** work around the pointer aliasing rules of C.)  *pp should initially
** be zero.  If *pp is not zero, that means that the space has already
** been allocated and this routine is a noop.
**
** nByte is the number of bytes of space needed.
**
** *ppFrom point to available space and pEnd points to the end of the
** available space.
**
** *pnByte is a counter of the number of bytes of space that have failed
** to allocate.  If there is insufficient space in *ppFrom to satisfy the
** request, then increment *pnByte by the amount of the request.
*/
    //static void allocSpace(
    //ref u8[] pp,            /* IN/OUT: Set *pp to point to allocated buffer */
    //int nByte,              /* Number of bytes to allocate */
    //ref u8[] ppFrom,        /* IN/OUT: Allocate from *ppFrom */
    //u8 pEnd,                /* Pointer to 1 byte past the end of *ppFrom buffer */
    //ref int pnByte          /* If allocation cannot be made, increment *pnByte */
    //)
    //{
      //assert( EIGHT_BYTE_ALIGNMENT(*ppFrom) );
      //if ( ( *(void**)pp ) == 0 )
      //{
      //  nByte = ROUND8( nByte );
      //  if( &(*ppFrom)[nByte] <= pEnd ){
      //    *(void**)pp = (void*)*ppFrom;
      //    *ppFrom += nByte;
      //  }
      //  else
      //  {
      //    *pnByte += nByte;
      //  }
      //}
    //}

    /*
    ** Prepare a virtual machine for execution.  This involves things such
    ** as allocating stack space and initializing the program counter.
    ** After the VDBE has be prepped, it can be executed by one or more
    ** calls to sqlite3VdbeExec().
    **
    ** This is the only way to move a VDBE from VDBE_MAGIC_INIT to
    ** VDBE_MAGIC_RUN.
    **
    ** This function may be called more than once on a single virtual machine.
    ** The first call is made while compiling the SQL statement. Subsequent
    ** calls are made as part of the process of resetting a statement to be
    ** re-executed (from a call to sqlite3_reset()). The nVar, nMem, nCursor
    ** and isExplain parameters are only passed correct values the first time
    ** the function is called. On subsequent calls, from sqlite3_reset(), nVar
    ** is passed -1 and nMem, nCursor and isExplain are all passed zero.
    */
    static void sqlite3VdbeMakeReady(
    Vdbe p,                        /* The VDBE */
    int nVar,                      /* Number of '?' see in the SQL statement */
    int nMem,                      /* Number of memory cells to allocate */
    int nCursor,                   /* Number of cursors to allocate */
    int isExplain                 /* True if the EXPLAIN keywords is present */
    )
    {
      int n;
      sqlite3 db = p.db;

      Debug.Assert( p != null );
      Debug.Assert( p.magic == VDBE_MAGIC_INIT );

      /* There should be at least one opcode.
      */
      Debug.Assert( p.nOp > 0 );

      /* Set the magic to VDBE_MAGIC_RUN sooner rather than later. */
      p.magic = VDBE_MAGIC_RUN;

      /* For each cursor required, also allocate a memory cell. Memory
      ** cells (nMem+1-nCursor)..nMem, inclusive, will never be used by
      ** the vdbe program. Instead they are used to allocate space for
      ** VdbeCursor/BtCursor structures. The blob of memory associated with
      ** cursor 0 is stored in memory cell nMem. Memory cell (nMem-1)
      ** stores the blob of memory associated with cursor 1, etc.
      **
      ** See also: allocateCursor().
      */
      nMem += nCursor;

      /* Allocate space for memory registers, SQL variables, VDBE cursors and
      ** an array to marshal SQL function arguments in. This is only done the
      ** first time this function is called for a given VDBE, not when it is
      ** being called from sqlite3_reset() to reset the virtual machine.
      */
      if ( nVar >= 0 /* &&  ALWAYS(db->mallocFailed==0) */ )
      {
        //u8 zCsr = (u8)p.aOp[p.nOp];
        //u8 zEnd = (u8)p.aOp[p.nOpAlloc];
        int nByte;
        int nArg = 0;       /* Maximum number of args passed to a user function. */
        resolveP2Values( p, ref nArg );
        if ( isExplain != 0 && nMem < 10 )
        {
          nMem = 10;
        }
        //memset(zCsr, 0, zEnd-zCsr);
        //zCsr += ( zCsr - (u8*)0 ) & 7;
        //assert( EIGHT_BYTE_ALIGNMENT( zCsr ) );
        //
        // C# -- Replace allocation with individual Dims
        //
        //do
        //{
        //  nByte = 0;
        //  allocSpace( (char*)&p.aMem, nMem * sizeof( Mem ), &zCsr, zEnd, &nByte );
        //  allocSpace( (char*)&p.aVar, nVar * sizeof( Mem ), &zCsr, zEnd, &nByte );
        //  allocSpace( (char*)&p.apArg, nArg * sizeof( Mem* ), &zCsr, zEnd, &nByte );
        //  allocSpace( (char*)&p.azVar, nVar * sizeof( char* ), &zCsr, zEnd, &nByte );
        //  allocSpace( (char*)&p.apCsr,
        //             nCursor * sizeof( VdbeCursor* ), &zCsr, zEnd, &nByte
        //  );
        //  if ( nByte )
        //  {
        //    p.pFree = sqlite3DbMallocZero( db, nByte );
        //  }
        //  zCsr = p.pFree;
        //  zEnd = &zCsr[nByte];
        //} while ( nByte && !db.mallocFailed );


        // C# -- Replace allocation with individual Dims
        p.aMem = new Mem[nMem + 1];
        for ( n = 0 ; n <= nMem ; n++ )
        { p.aMem[n] = new Mem(); }//p.aMem--;
        /* aMem[] goes from 1..nMem */
        p.nMem = nMem;      /*       not from 0..nMem-1 */
        //
        p.aVar = new Mem[nVar == 0 ? 1 : nVar]; //p.aVar = p.aMem[nMem + 1];
        for ( n = 0 ; n < nVar ; n++ )
        { p.aVar[n] = new Mem(); }
        p.nVar = (u16)nVar;
        p.okVar = 0;
        //
        p.apArg = new Mem[nArg == 0 ? 1 : nArg];//p.apArg = (Mem**)p.aVar[nVar];
        //

        p.azVar = new string[nArg == 0 ? 1 : nArg]; //p.azVar = (char**)p.apArg[nArg];
        for ( n = 0 ; n < nArg ; n++ )
        { p.azVar[n] = ""; }
        //
        p.apCsr = new VdbeCursor[nCursor == 0 ? 1 : nCursor];//p.apCsr = (VdbeCursor**)p.azVar[nVar];
        p.apCsr[0] = new VdbeCursor();
        p.nCursor = (u16)nCursor;
        if ( p.aVar != null )
        {
          p.nVar = (u16)nVar;
          //
          for ( n = 0 ; n < nVar ; n++ )
          {
            p.aVar[n].flags = MEM_Null;
            p.aVar[n].db = db;
          }
        }
        if ( p.aMem != null )
        {
          //p.aMem--;                      /* aMem[] goes from 1..nMem */
          p.nMem = nMem;                 /*       not from 0..nMem-1 */
          for ( n = 0 ; n <= nMem ; n++ )
          {
            p.aMem[n].flags = MEM_Null;
            p.aMem[n].n = 0;
            p.aMem[n].z = null;
            p.aMem[n].zBLOB = null;
            p.aMem[n].db = db;
          }
        }
      }

#if  SQLITE_DEBUG
      for ( n = 1 ; n < p.nMem ; n++ )
      {
        Debug.Assert( p.aMem[n].db == db );
      }
#endif

      p.pc = -1;
      p.rc = SQLITE_OK;
      p.errorAction = OE_Abort;
      p.explain |= isExplain;
      p.magic = VDBE_MAGIC_RUN;
      p.nChange = 0;
      p.cacheCtr = 1;
      p.minWriteFileFormat = 255;
      p.iStatement = 0;
#if  VDBE_PROFILE
{
int i;
for ( i = 0 ; i < p.nOp ; i++ )
{
p.aOp[i].cnt = 0;
p.aOp[i].cycles = 0;
}
}
#endif
    }

    /*
    ** Close a VDBE cursor and release all the resources that cursor
    ** happens to hold.
    */
    static void sqlite3VdbeFreeCursor( Vdbe p, VdbeCursor pCx )
    {
      if ( pCx == null )
      {
        return;
      }

      if ( pCx.pBt != null )
      {
        sqlite3BtreeClose( ref  pCx.pBt );
        /* The pCx.pCursor will be close automatically, if it exists, by
        ** the call above. */
      }
      else if ( pCx.pCursor != null )
      {
        sqlite3BtreeCloseCursor( pCx.pCursor );
      }
#if ! SQLITE_OMIT_VIRTUALTABLE
if( pCx.pVtabCursor ){
sqlite3_vtab_cursor pVtabCursor = pCx.pVtabCursor;
const sqlite3_module pModule = pCx.pModule;
p.inVtabMethod = 1;
sqlite3SafetyOff(p.db);
pModule.xClose(pVtabCursor);
sqlite3SafetyOn(p.db);
p.inVtabMethod = 0;
}
#endif
      if ( !pCx.ephemPseudoTable )
      {
        //sqlite3DbFree( p.db, ref pCx.pData );
      }
    }

    /*
    ** Close all cursors.
    */
    static void closeAllCursors( Vdbe p )
    {
      int i;
      if ( p.apCsr == null ) return;
      for ( i = 0 ; i < p.nCursor ; i++ )
      {
        VdbeCursor pC = p.apCsr[i];
        if ( pC != null )
        {
          sqlite3VdbeFreeCursor( p, pC );
          p.apCsr[i] = null;
        }
      }
    }

    /*
    ** Clean up the VM after execution.
    **
    ** This routine will automatically close any cursors, lists, and/or
    ** sorters that were left open.  It also deletes the values of
    ** variables in the aVar[] array.
    */
    static void Cleanup( Vdbe p )
    {
      int i;
      sqlite3 db = p.db;
      Mem pMem;
      closeAllCursors( p );
      for ( i = 1 ; i <= p.nMem ; i++ )
      {
        pMem = p.aMem[1];
        if ( ( pMem.flags & MEM_RowSet ) != 0 )
        {
          sqlite3RowSetClear( pMem.u.pRowSet );
        }
        MemSetTypeFlag( pMem, MEM_Null );
      }
      releaseMemArray( p.aMem, p.nMem );
      if ( p.contextStack != null )
      {
        //sqlite3DbFree( db, ref p.contextStack );
      }
      p.contextStack = null;
      p.contextStackDepth = 0;
      p.contextStackTop = 0;
      //sqlite3DbFree( db, ref p.zErrMsg );
      p.pResultSet = null;
    }

    /*
    ** Set the number of result columns that will be returned by this SQL
    ** statement. This is now set at compile time, rather than during
    ** execution of the vdbe program so that sqlite3_column_count() can
    ** be called on an SQL statement before sqlite3_step().
    */
    static void sqlite3VdbeSetNumCols( Vdbe p, int nResColumn )
    {
      Mem pColName;
      int n;
      sqlite3 db = p.db;

      releaseMemArray( p.aColName, p.nResColumn * COLNAME_N );
      //sqlite3DbFree( db, ref p.aColName );
      n = nResColumn * COLNAME_N;
      p.nResColumn = (u16)nResColumn;
      p.aColName = new Mem[n];// (Mem*)sqlite3DbMallocZero(db, Mem.Length * n);
      //if (p.aColName == 0) return;
      while ( n-- > 0 )
      {
        p.aColName[n] = new Mem();
        pColName = p.aColName[n];
        pColName.flags = MEM_Null;
        pColName.db = p.db;
      }
    }

    /*
    ** Set the name of the idx'th column to be returned by the SQL statement.
    ** zName must be a pointer to a nul terminated string.
    **
    ** This call must be made after a call to sqlite3VdbeSetNumCols().
    **
    ** The final parameter, xDel, must be one of SQLITE_DYNAMIC, SQLITE_STATIC
    ** or SQLITE_TRANSIENT. If it is SQLITE_DYNAMIC, then the buffer pointed
    ** to by zName will be freed by //sqlite3DbFree() when the vdbe is destroyed.
    */


    static int sqlite3VdbeSetColName(
    Vdbe p,                 /* Vdbe being configured */
    int idx,                /* Index of column zName applies to */
    int var,                /* One of the COLNAME_* constants */
    string zName,           /* Pointer to buffer containing name */
    dxDel xDel              /* Memory management strategy for zName */
    )
    {
      int rc;
      Mem pColName;
      Debug.Assert( idx < p.nResColumn );
      Debug.Assert( var < COLNAME_N );
      //if ( p.db.mallocFailed != 0 )
      //{
      //  Debug.Assert( null == zName || xDel != SQLITE_DYNAMIC );
      //  return SQLITE_NOMEM;
      //}
      Debug.Assert( p.aColName != null );
      pColName = p.aColName[idx + var * p.nResColumn];
      rc = sqlite3VdbeMemSetStr( pColName, zName, -1, SQLITE_UTF8, xDel );
      Debug.Assert( rc != 0 || null == zName || ( pColName.flags & MEM_Term ) != 0 );
      return rc;
    }

    /*
    ** A read or write transaction may or may not be active on database handle
    ** db. If a transaction is active, commit it. If there is a
    ** write-transaction spanning more than one database file, this routine
    ** takes care of the master journal trickery.
    */
    static int vdbeCommit( sqlite3 db, Vdbe p )
    {
      int i;
      int nTrans = 0;  /* Number of databases with an active write-transaction */
      int rc = SQLITE_OK;
      bool needXcommit = false;

#if SQLITE_OMIT_VIRTUALTABLE
      /* With this option, sqlite3VtabSync() is defined to be simply
** SQLITE_OK so p is not used.
*/
      UNUSED_PARAMETER( p );
#endif
      /* Before doing anything else, call the xSync() callback for any
** virtual module tables written in this transaction. This has to
** be done before determining whether a master journal file is
** required, as an xSync() callback may add an attached database
** to the transaction.
*/
      rc = sqlite3VtabSync( db, p.zErrMsg );
      if ( rc != SQLITE_OK )
      {
        return rc;
      }

      /* This loop determines (a) if the commit hook should be invoked and
      ** (b) how many database files have open write transactions, not
      ** including the temp database. (b) is important because if more than
      ** one database file has an open write transaction, a master journal
      ** file is required for an atomic commit.
      */
      for ( i = 0 ; i < db.nDb ; i++ )
      {
        Btree pBt = db.aDb[i].pBt;
        if ( sqlite3BtreeIsInTrans( pBt ) )
        {
          needXcommit = true;
          if ( i != 1 ) nTrans++;
        }
      }

      /* If there are any write-transactions at all, invoke the commit hook */
      if ( needXcommit && db.xCommitCallback != null )
      {
        sqlite3SafetyOff( db );
        rc = db.xCommitCallback( db.pCommitArg );
        sqlite3SafetyOn( db );
        if ( rc != 0 )
        {
          return SQLITE_CONSTRAINT;
        }
      }

      /* The simple case - no more than one database file (not counting the
      ** TEMP database) has a transaction active.   There is no need for the
      ** master-journal.
      **
      ** If the return value of sqlite3BtreeGetFilename() is a zero length
      ** string, it means the main database is :memory: or a temp file.  In
      ** that case we do not support atomic multi-file commits, so use the
      ** simple case then too.
      */
      if ( 0 == sqlite3Strlen30( sqlite3BtreeGetFilename( db.aDb[0].pBt ) )
      || nTrans <= 1 )
      {
        for ( i = 0 ; rc == SQLITE_OK && i < db.nDb ; i++ )
        {
          Btree pBt = db.aDb[i].pBt;
          if ( pBt != null )
          {
            rc = sqlite3BtreeCommitPhaseOne( pBt, null );
          }
        }

        /* Do the commit only if all databases successfully complete phase 1.
        ** If one of the BtreeCommitPhaseOne() calls fails, this indicates an
        ** IO error while deleting or truncating a journal file. It is unlikely,
        ** but could happen. In this case abandon processing and return the error.
        */
        for ( i = 0 ; rc == SQLITE_OK && i < db.nDb ; i++ )
        {
          Btree pBt = db.aDb[i].pBt;
          if ( pBt != null )
          {
            rc = sqlite3BtreeCommitPhaseTwo( pBt );
          }
        }
        if ( rc == SQLITE_OK )
        {
          sqlite3VtabCommit( db );
        }
      }

          /* The complex case - There is a multi-file write-transaction active.
          ** This requires a master journal file to ensure the transaction is
          ** committed atomicly.
          */
#if ! SQLITE_OMIT_DISKIO
      else
      {
        sqlite3_vfs pVfs = db.pVfs;
        bool needSync = false;
        string zMaster = "";   /* File-name for the master journal */
        string zMainFile = sqlite3BtreeGetFilename( db.aDb[0].pBt );
        sqlite3_file pMaster = null;
        i64 offset = 0;
        int res = 0;

        /* Select a master journal file name */
        do
        {
          i64 iRandom = 0;
          //sqlite3DbFree(db,ref zMaster);
          sqlite3_randomness( sizeof( u32 ), ref iRandom );//random.Length
          zMaster = sqlite3MPrintf( db, "%s-mj%08X", zMainFile, iRandom & 0x7fffffff );
          //if (!zMaster)
          //{
          //  return SQLITE_NOMEM;
          //}
          rc = sqlite3OsAccess( pVfs, zMaster, SQLITE_ACCESS_EXISTS, ref res );
        } while ( rc == SQLITE_OK && res == 1 );
        if ( rc == SQLITE_OK )
        {
          /* Open the master journal. */
          rc = sqlite3OsOpenMalloc( ref pVfs, zMaster, ref pMaster,
          SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE |
          SQLITE_OPEN_EXCLUSIVE | SQLITE_OPEN_MASTER_JOURNAL, ref rc
          );
        } if ( rc != SQLITE_OK )
        {
          //sqlite3DbFree( db, ref zMaster );
          return rc;
        }

        /* Write the name of each database file in the transaction into the new
        ** master journal file. If an error occurs at this point close
        ** and delete the master journal file. All the individual journal files
        ** still have 'null' as the master journal pointer, so they will roll
        ** back independently if a failure occurs.
        */
        for ( i = 0 ; i < db.nDb ; i++ )
        {
          Btree pBt = db.aDb[i].pBt;
          if ( i == 1 ) continue;   /* Ignore the TEMP database */
          if ( sqlite3BtreeIsInTrans( pBt ) )
          {
            string zFile = sqlite3BtreeGetJournalname( pBt );
            if ( zFile[0] == 0 ) continue;  /* Ignore :memory: databases */
            if ( !needSync && 0 == sqlite3BtreeSyncDisabled( pBt ) )
            {
              needSync = true;
            }
            rc = sqlite3OsWrite( pMaster, Encoding.UTF8.GetBytes( zFile ), sqlite3Strlen30( zFile ), offset );
            offset += sqlite3Strlen30( zFile );
            if ( rc != SQLITE_OK )
            {
              sqlite3OsCloseFree( pMaster );
              sqlite3OsDelete( pVfs, zMaster, 0 );
              //sqlite3DbFree( db, ref zMaster );
              return rc;
            }
          }
        }

        /* Sync the master journal file. If the IOCAP_SEQUENTIAL device
        ** flag is set this is not required.
        */
        if ( needSync
        && 0 == ( sqlite3OsDeviceCharacteristics( pMaster ) & SQLITE_IOCAP_SEQUENTIAL )
        && SQLITE_OK != ( rc = sqlite3OsSync( pMaster, SQLITE_SYNC_NORMAL ) )
        )
        {
          sqlite3OsCloseFree( pMaster );
          sqlite3OsDelete( pVfs, zMaster, 0 );
          //sqlite3DbFree( db, ref zMaster );
          return rc;
        }

        /* Sync all the db files involved in the transaction. The same call
        ** sets the master journal pointer in each individual journal. If
        ** an error occurs here, do not delete the master journal file.
        **
        ** If the error occurs during the first call to
        ** sqlite3BtreeCommitPhaseOne(), then there is a chance that the
        ** master journal file will be orphaned. But we cannot delete it,
        ** in case the master journal file name was written into the journal
        ** file before the failure occurred.
        */
        for ( i = 0 ; rc == SQLITE_OK && i < db.nDb ; i++ )
        {
          Btree pBt = db.aDb[i].pBt;
          if ( pBt != null )
          {
            rc = sqlite3BtreeCommitPhaseOne( pBt, zMaster );
          }
        }
        sqlite3OsCloseFree( pMaster );
        if ( rc != SQLITE_OK )
        {
          //sqlite3DbFree( db, ref zMaster );
          return rc;
        }

        /* Delete the master journal file. This commits the transaction. After
        ** doing this the directory is synced again before any individual
        ** transaction files are deleted.
        */
        rc = sqlite3OsDelete( pVfs, zMaster, 1 );
        //sqlite3DbFree( db, ref zMaster );
        if ( rc != 0 )
        {
          return rc;
        }

        /* All files and directories have already been synced, so the following
        ** calls to sqlite3BtreeCommitPhaseTwo() are only closing files and
        ** deleting or truncating journals. If something goes wrong while
        ** this is happening we don't really care. The integrity of the
        ** transaction is already guaranteed, but some stray 'cold' journals
        ** may be lying around. Returning an error code won't help matters.
        */
#if SQLITE_TEST
        disable_simulated_io_errors();
#endif
        sqlite3BeginBenignMalloc();
        for ( i = 0 ; i < db.nDb ; i++ )
        {
          Btree pBt = db.aDb[i].pBt;
          if ( pBt != null )
          {
            sqlite3BtreeCommitPhaseTwo( pBt );
          }
        }
        sqlite3EndBenignMalloc();
#if SQLITE_TEST
        enable_simulated_io_errors();
#endif
        sqlite3VtabCommit( db );
      }
#endif

      return rc;
    }

    /*
    ** This routine checks that the sqlite3.activeVdbeCnt count variable
    ** matches the number of vdbe's in the list sqlite3.pVdbe that are
    ** currently active. An Debug.Assertion fails if the two counts do not match.
    ** This is an internal self-check only - it is not an essential processing
    ** step.
    **
    ** This is a no-op if NDEBUG is defined.
    */
#if !NDEBUG
    static void checkActiveVdbeCnt( sqlite3 db )
    {
      Vdbe p;
      int cnt = 0;
      int nWrite = 0;
      p = db.pVdbe;
      while ( p != null )
      {
        if ( p.magic == VDBE_MAGIC_RUN && p.pc >= 0 )
        {
          cnt++;
          if ( p.readOnly == false ) nWrite++;
        }
        p = p.pNext;
      }
      Debug.Assert( cnt == db.activeVdbeCnt );
      Debug.Assert( nWrite == db.writeVdbeCnt );
    }
#else
//#define checkActiveVdbeCnt(x)
static void checkActiveVdbeCnt( sqlite3 db ){}
#endif

    /*
** For every Btree that in database connection db which
** has been modified, "trip" or invalidate each cursor in
** that Btree might have been modified so that the cursor
** can never be used again.  This happens when a rollback
*** occurs.  We have to trip all the other cursors, even
** cursor from other VMs in different database connections,
** so that none of them try to use the data at which they
** were pointing and which now may have been changed due
** to the rollback.
**
** Remember that a rollback can delete tables complete and
** reorder rootpages.  So it is not sufficient just to save
** the state of the cursor.  We have to invalidate the cursor
** so that it is never used again.
*/
    static void invalidateCursorsOnModifiedBtrees( sqlite3 db )
    {
      int i;
      for ( i = 0 ; i < db.nDb ; i++ )
      {
        Btree p = db.aDb[i].pBt;
        if ( p != null && sqlite3BtreeIsInTrans( p ) )
        {
          sqlite3BtreeTripAllCursors( p, SQLITE_ABORT );
        }
      }
    }

    /*
    ** If the Vdbe passed as the first argument opened a statement-transaction,
    ** close it now. Argument eOp must be either SAVEPOINT_ROLLBACK or
    ** SAVEPOINT_RELEASE. If it is SAVEPOINT_ROLLBACK, then the statement
    ** transaction is rolled back. If eOp is SAVEPOINT_RELEASE, then the
    ** statement transaction is commtted.
    **
    ** If an IO error occurs, an SQLITE_IOERR_XXX error code is returned.
    ** Otherwise SQLITE_OK.
    */
    static int sqlite3VdbeCloseStatement( Vdbe p, int eOp )
    {
      sqlite3 db = p.db;
      int rc = SQLITE_OK;
        /* If p->iStatement is greater than zero, then this Vdbe opened a 
        ** statement transaction that should be closed here. The only exception
        ** is that an IO error may have occured, causing an emergency rollback.
        ** In this case (db->nStatement==0), and there is nothing to do.
        */
        if ( db.nStatement !=0 && p.iStatement!=0 )
        {
          int i;
        int iSavepoint = p.iStatement - 1;

        Debug.Assert( eOp == SAVEPOINT_ROLLBACK || eOp == SAVEPOINT_RELEASE );
        Debug.Assert( db.nStatement > 0 );
        Debug.Assert( p.iStatement == ( db.nStatement + db.nSavepoint ) );

        for ( i = 0 ; i < db.nDb ; i++ )
        {
          int rc2 = SQLITE_OK;
          Btree pBt = db.aDb[i].pBt;
          if ( pBt != null )
          {
            if ( eOp == SAVEPOINT_ROLLBACK )
            {
              rc2 = sqlite3BtreeSavepoint( pBt, SAVEPOINT_ROLLBACK, iSavepoint );
            }
            if ( rc2 == SQLITE_OK )
            {
              rc2 = sqlite3BtreeSavepoint( pBt, SAVEPOINT_RELEASE, iSavepoint );
            }
            if ( rc == SQLITE_OK )
            {
              rc = rc2;
            }
          }
        }
        db.nStatement--;
        p.iStatement = 0;
      }
      return rc;
    }

    /*
    ** If SQLite is compiled to support shared-cache mode and to be threadsafe,
    ** this routine obtains the mutex associated with each BtShared structure
    ** that may be accessed by the VM passed as an argument. In doing so it
    ** sets the BtShared.db member of each of the BtShared structures, ensuring
    ** that the correct busy-handler callback is invoked if required.
    **
    ** If SQLite is not threadsafe but does support shared-cache mode, then
    ** sqlite3BtreeEnterAll() is invoked to set the BtShared.db variables
    ** of all of BtShared structures accessible via the database handle
    ** associated with the VM. Of course only a subset of these structures
    ** will be accessed by the VM, and we could use Vdbe.btreeMask to figure
    ** that subset out, but there is no advantage to doing so.
    **
    ** If SQLite is not threadsafe and does not support shared-cache mode, this
    ** function is a no-op.
    */
#if !SQLITE_OMIT_SHARED_CACHE
static void sqlite3VdbeMutexArrayEnter(Vdbe p){
#if SQLITE_THREADSAFE
sqlite3BtreeMutexArrayEnter(&p->aMutex);
#else
sqlite3BtreeEnterAll(p.db);
#endif
}
#endif


    /*
** This routine is called the when a VDBE tries to halt.  If the VDBE
** has made changes and is in autocommit mode, then commit those
** changes.  If a rollback is needed, then do the rollback.
**
** This routine is the only way to move the state of a VM from
** SQLITE_MAGIC_RUN to SQLITE_MAGIC_HALT.  It is harmless to
** call this on a VM that is in the SQLITE_MAGIC_HALT state.
**
** Return an error code.  If the commit could not complete because of
** lock contention, return SQLITE_BUSY.  If SQLITE_BUSY is returned, it
** means the close did not happen and needs to be repeated.
*/
    static int sqlite3VdbeHalt( Vdbe p )
    {
      int rc;                         /* Used to store transient return codes */
      sqlite3 db = p.db;

      /* This function contains the logic that determines if a statement or
      ** transaction will be committed or rolled back as a result of the
      ** execution of this virtual machine.
      **
      ** If any of the following errors occur:
      **
      **     SQLITE_NOMEM
      **     SQLITE_IOERR
      **     SQLITE_FULL
      **     SQLITE_INTERRUPT
      **
      ** Then the internal cache might have been left in an inconsistent
      ** state.  We need to rollback the statement transaction, if there is
      ** one, or the complete transaction if there is no statement transaction.
      */

      //if ( p.db.mallocFailed != 0 )
      //{
      //  p.rc = SQLITE_NOMEM;
      //}
      closeAllCursors( p );
      if ( p.magic != VDBE_MAGIC_RUN )
      {
        return SQLITE_OK;
      }
      checkActiveVdbeCnt( db );

      /* No commit or rollback needed if the program never started */
      if ( p.pc >= 0 )
      {
        int mrc;   /* Primary error code from p.rc */
        int eStatementOp = 0;
        bool isSpecialError = false;            /* Set to true if a 'special' error */

        /* Lock all btrees used by the statement */
        sqlite3VdbeMutexArrayEnter( p );
        /* Check for one of the special errors */
        mrc = p.rc & 0xff;
        Debug.Assert( p.rc != SQLITE_IOERR_BLOCKED );  /* This error no longer exists */
        isSpecialError = mrc == SQLITE_NOMEM || mrc == SQLITE_IOERR
        || mrc == SQLITE_INTERRUPT || mrc == SQLITE_FULL;
        if ( isSpecialError )
        {
          /* If the query was read-only, we need do no rollback at all. Otherwise,
          ** proceed with the special handling.
          */
          if ( !p.readOnly || mrc != SQLITE_INTERRUPT )
          {
            if ( ( mrc == SQLITE_NOMEM || mrc == SQLITE_FULL ) && p.usesStmtJournal )
            {
              eStatementOp = SAVEPOINT_ROLLBACK;
            }
            else
            {
              /* We are forced to roll back the active transaction. Before doing
              ** so, abort any other statements this handle currently has active.
              */
              invalidateCursorsOnModifiedBtrees( db );
              sqlite3RollbackAll( db );
              sqlite3CloseSavepoints( db );
              db.autoCommit = 1;
            }
          }
        }

        /* If the auto-commit flag is set and this is the only active writer
        ** VM, then we do either a commit or rollback of the current transaction.
        **
        ** Note: This block also runs if one of the special errors handled
        ** above has occurred.
        */
        if ( !sqlite3VtabInSync( db )
        && db.autoCommit != 0
        && db.writeVdbeCnt == ( ( p.readOnly == false ) ? 1 : 0 )
        )
        {
          if ( p.rc == SQLITE_OK || ( p.errorAction == OE_Fail && !isSpecialError ) )
          {
            /* The auto-commit flag is true, and the vdbe program was
            ** successful or hit an 'OR FAIL' constraint. This means a commit
            ** is required.
            */
            rc = vdbeCommit( db, p );
            if ( rc == SQLITE_BUSY )
            {
              sqlite3BtreeMutexArrayLeave( p.aMutex );
              return SQLITE_BUSY;
            }
            else if ( rc != SQLITE_OK )
            {
              p.rc = rc;
              sqlite3RollbackAll( db );
            }
            else
            {
              sqlite3CommitInternalChanges( db );
            }
          }
          else
          {
            sqlite3RollbackAll( db );
          }
          db.nStatement = 0;
        }
        else if ( eStatementOp == 0 )
        {
          if ( p.rc == SQLITE_OK || p.errorAction == OE_Fail )
          {
            eStatementOp = SAVEPOINT_RELEASE;
          }
          else if ( p.errorAction == OE_Abort )
          {
            eStatementOp = SAVEPOINT_ROLLBACK;
          }
          else
          {
            invalidateCursorsOnModifiedBtrees( db );
            sqlite3RollbackAll( db );
            sqlite3CloseSavepoints( db );
            db.autoCommit = 1;
          }
        }

        /* If eStatementOp is non-zero, then a statement transaction needs to
        ** be committed or rolled back. Call sqlite3VdbeCloseStatement() to
        ** do so. If this operation returns an error, and the current statement
        ** error code is SQLITE_OK or SQLITE_CONSTRAINT, then set the error
        ** code to the new value.
        */
        if ( eStatementOp != 0 )
        {
          rc = sqlite3VdbeCloseStatement( p, eStatementOp );
          if ( rc != 0 && ( p.rc == SQLITE_OK || p.rc == SQLITE_CONSTRAINT ) )
          {
            p.rc = rc;
            //sqlite3DbFree(db, p.zErrMsg );
            p.zErrMsg = null;
          }
        }

        /* If this was an INSERT, UPDATE or DELETE and no statement transaction
        ** has been rolled back, update the database connection change-counter.
        */
        if ( p.changeCntOn)
        {
          if ( eStatementOp != SAVEPOINT_ROLLBACK )
          {
            sqlite3VdbeSetChanges( db, p.nChange );
          }
          else
          {
            sqlite3VdbeSetChanges( db, 0 );
          }
          p.nChange = 0;
        }

        /* Rollback or commit any schema changes that occurred. */
        if ( p.rc != SQLITE_OK && ( db.flags & SQLITE_InternChanges ) != 0 )
        {
          sqlite3ResetInternalSchema( db, 0 );
          db.flags = ( db.flags | SQLITE_InternChanges );
        }

        /* Release the locks */
        sqlite3BtreeMutexArrayLeave( p.aMutex );
      }

      /* We have successfully halted and closed the VM.  Record this fact. */
      if ( p.pc >= 0 )
      {
        db.activeVdbeCnt--;
        if ( !p.readOnly )
        {
          db.writeVdbeCnt--;
        }
        Debug.Assert( db.activeVdbeCnt >= db.writeVdbeCnt );
      }
      p.magic = VDBE_MAGIC_HALT;
      checkActiveVdbeCnt( db );
      //if ( p.db.mallocFailed != 0 )
      //{
      //  p.rc = SQLITE_NOMEM;
      //}
      /* If the auto-commit flag is set to true, then any locks that were held
      ** by connection db have now been released. Call sqlite3ConnectionUnlocked()
      ** to invoke any required unlock-notify callbacks.
      */
      if ( db.autoCommit != 0 )
      {
        sqlite3ConnectionUnlocked( db );
      }

      Debug.Assert( db.activeVdbeCnt > 0 || db.autoCommit == 0 || db.nStatement == 0 );
      return SQLITE_OK;
    }


    /*
    ** Each VDBE holds the result of the most recent sqlite3_step() call
    ** in p.rc.  This routine sets that result back to SQLITE_OK.
    */
    static void sqlite3VdbeResetStepResult( Vdbe p )
    {
      p.rc = SQLITE_OK;
    }

    /*
    ** Clean up a VDBE after execution but do not delete the VDBE just yet.
    ** Write any error messages into pzErrMsg.  Return the result code.
    **
    ** After this routine is run, the VDBE should be ready to be executed
    ** again.
    **
    ** To look at it another way, this routine resets the state of the
    ** virtual machine from VDBE_MAGIC_RUN or VDBE_MAGIC_HALT back to
    ** VDBE_MAGIC_INIT.
    */
    static int sqlite3VdbeReset( Vdbe p )
    {
      sqlite3 db;
      db = p.db;

      /* If the VM did not run to completion or if it encountered an
      ** error, then it might not have been halted properly.  So halt
      ** it now.
      */
      sqlite3SafetyOn( db );
      sqlite3VdbeHalt( p );
      sqlite3SafetyOff( db );

      /* If the VDBE has be run even partially, then transfer the error code
      ** and error message from the VDBE into the main database structure.  But
      ** if the VDBE has just been set to run but has not actually executed any
      ** instructions yet, leave the main database error information unchanged.
      */
      if ( p.pc >= 0 )
      {
        //if ( p.zErrMsg != 0 ) // Always exists under C#
        {
          sqlite3BeginBenignMalloc();
          sqlite3ValueSetStr( db.pErr, -1, p.zErrMsg == null ? "" : p.zErrMsg, SQLITE_UTF8, SQLITE_TRANSIENT );
          sqlite3EndBenignMalloc();
          db.errCode = p.rc;
          //sqlite3DbFree( db, ref p.zErrMsg );
          p.zErrMsg = "";
        }
        //else if ( p.rc != 0 )
        //{
        //  sqlite3Error( db, p.rc, 0 );
        //}
        //else
        //{
        //  sqlite3Error( db, SQLITE_OK, 0 );
        //}
      }
      else if ( p.rc != 0 && p.expired )
      {
        /* The expired flag was set on the VDBE before the first call
        ** to sqlite3_step(). For consistency (since sqlite3_step() was
        ** called), set the database error in this case as well.
        */
        sqlite3Error( db, p.rc, 0 );
        sqlite3ValueSetStr( db.pErr, -1, p.zErrMsg, SQLITE_UTF8, SQLITE_TRANSIENT );
        //sqlite3DbFree( db, ref p.zErrMsg );
        p.zErrMsg = "";
      }

      /* Reclaim all memory used by the VDBE
      */
      Cleanup( p );

      /* Save profiling information from this VDBE run.
      */
#if  VDBE_PROFILE && TODO
{
FILE *out = fopen("vdbe_profile.out", "a");
if( out ){
int i;
fprintf(out, "---- ");
for(i=0; i<p.nOp; i++){
fprintf(out, "%02x", p.aOp[i].opcode);
}
fprintf(out, "\n");
for(i=0; i<p.nOp; i++){
fprintf(out, "%6d %10lld %8lld ",
p.aOp[i].cnt,
p.aOp[i].cycles,
p.aOp[i].cnt>0 ? p.aOp[i].cycles/p.aOp[i].cnt : 0
);
sqlite3VdbePrintOp(out, i, p.aOp[i]);
}
fclose(out);
}
}
#endif
      p.magic = VDBE_MAGIC_INIT;
      return p.rc & db.errMask;
    }

    /*
    ** Clean up and delete a VDBE after execution.  Return an integer which is
    ** the result code.  Write any error message text into pzErrMsg.
    */
    static int sqlite3VdbeFinalize( Vdbe p )
    {
      int rc = SQLITE_OK;
      if ( p.magic == VDBE_MAGIC_RUN || p.magic == VDBE_MAGIC_HALT )
      {
        rc = sqlite3VdbeReset( p );
        Debug.Assert( ( rc & p.db.errMask ) == rc );
      }
      sqlite3VdbeDelete( ref p );
      return rc;
    }

    /*
    ** Call the destructor for each auxdata entry in pVdbeFunc for which
    ** the corresponding bit in mask is clear.  Auxdata entries beyond 31
    ** are always destroyed.  To destroy all auxdata entries, call this
    ** routine with mask==0.
    */
    static void sqlite3VdbeDeleteAuxData( VdbeFunc pVdbeFunc, int mask )
    {
      int i;
      for ( i = 0 ; i < pVdbeFunc.nAux ; i++ )
      {
        AuxData pAux = pVdbeFunc.apAux[i];
        if ( ( i > 31 || ( mask & ( ( (u32)1 ) << i ) ) == 0 && pAux.pAux != null ) )
        {
          if ( pAux.xDelete != null )
          {
            pAux.xDelete( ref pAux.pAux );
          }
          pAux.pAux = null;
        }
      }
    }

    /*
    ** Delete an entire VDBE.
    */
    static void sqlite3VdbeDelete( ref Vdbe p )
    {
      int i;
      sqlite3 db;
      if (NEVER( p == null )) return;
      Cleanup( p );
      db = p.db;
      if ( p.pPrev != null )
      {
        p.pPrev.pNext = p.pNext;
      }
      else
      {
        Debug.Assert( db.pVdbe == p );
        db.pVdbe = p.pNext;
      }
      if ( p.pNext != null )
      {
        p.pNext.pPrev = p.pPrev;
      }
      if ( p.aOp != null )
      {
        Op pOp;
        for ( i = 0 ; i < p.nOp ; i++ )//pOp++)
        {
          pOp = p.aOp[i];
          freeP4( db, pOp.p4type, pOp.p4type == P4_VDBEFUNC ? pOp.p4.pVdbeFunc : pOp.p4.pFunc );
#if  SQLITE_DEBUG
          //sqlite3DbFree( db, ref pOp.zComment );
#endif
        }
      }
      releaseMemArray( p.aVar, p.nVar );
      //sqlite3DbFree( db, ref p.aLabel );
      releaseMemArray( p.aColName, p.nResColumn * COLNAME_N );
      //sqlite3DbFree( db, ref p.aColName );
      //sqlite3DbFree( db, ref p.zSql );
      p.magic = VDBE_MAGIC_DEAD;
      //sqlite3DbFree( db, ref p.aOp );
      //sqlite3DbFree( db, ref  p.pFree );
      //sqlite3DbFree( db, ref  p );
    }

    /*
    ** Make sure the cursor p is ready to read or write the row to which it
    ** was last positioned.  Return an error code if an OOM fault or I/O error
    ** prevents us from positioning the cursor to its correct position.
    **
    ** If a MoveTo operation is pending on the given cursor, then do that
    ** MoveTo now.  If no move is pending, check to see if the row has been
    ** deleted out from under the cursor and if it has, mark the row as
    ** a NULL row.
    **
    ** If the cursor is already pointing to the correct row and that row has
    ** not been deleted out from under the cursor, then this routine is a no-op.
    */
    static int sqlite3VdbeCursorMoveto( VdbeCursor p )
    {
      if ( p.deferredMoveto )
      {
        int res = 0; int rc;
#if  SQLITE_TEST
        //extern int sqlite3_search_count;
#endif
        Debug.Assert( p.isTable );
        rc = sqlite3BtreeMovetoUnpacked( p.pCursor, null, p.movetoTarget, 0, ref res );
        if ( rc != 0 ) return rc;
        p.lastRowid = p.movetoTarget;
        p.rowidIsValid = ALWAYS( res == 0 ) ? true : false;
        if ( NEVER( res < 0 ) )
        {
          rc = sqlite3BtreeNext( p.pCursor, ref res );
          if ( rc != 0 ) return rc;
        }
#if  SQLITE_TEST
        sqlite3_search_count.iValue++;
#endif
        p.deferredMoveto = false;
        p.cacheStatus = CACHE_STALE;
      }
      else if (ALWAYS( p.pCursor != null ))
      {
        int hasMoved = 0;
        int rc = sqlite3BtreeCursorHasMoved( p.pCursor, ref hasMoved );
        if ( rc != 0 ) return rc;
        if ( hasMoved != 0 )
        {
          p.cacheStatus = CACHE_STALE;
          p.nullRow = true;
        }
      }
      return SQLITE_OK;
    }

    /*
    ** The following functions:
    **
    ** sqlite3VdbeSerialType()
    ** sqlite3VdbeSerialTypeLen()
    ** sqlite3VdbeSerialLen()
    ** sqlite3VdbeSerialPut()
    ** sqlite3VdbeSerialGet()
    **
    ** encapsulate the code that serializes values for storage in SQLite
    ** data and index records. Each serialized value consists of a
    ** 'serial-type' and a blob of data. The serial type is an 8-byte unsigned
    ** integer, stored as a varint.
    **
    ** In an SQLite index record, the serial type is stored directly before
    ** the blob of data that it corresponds to. In a table record, all serial
    ** types are stored at the start of the record, and the blobs of data at
    ** the end. Hence these functions allow the caller to handle the
    ** serial-type and data blob seperately.
    **
    ** The following table describes the various storage classes for data:
    **
    **   serial type        bytes of data      type
    **   --------------     ---------------    ---------------
    **      0                     0            NULL
    **      1                     1            signed integer
    **      2                     2            signed integer
    **      3                     3            signed integer
    **      4                     4            signed integer
    **      5                     6            signed integer
    **      6                     8            signed integer
    **      7                     8            IEEE float
    **      8                     0            Integer constant 0
    **      9                     0            Integer constant 1
    **     10,11                               reserved for expansion
    **    N>=12 and even       (N-12)/2        BLOB
    **    N>=13 and odd        (N-13)/2        text
    **
    ** The 8 and 9 types were added in 3.3.0, file format 4.  Prior versions
    ** of SQLite will not understand those serial types.
    */

    /*
    ** Return the serial-type for the value stored in pMem.
    */
    static u32 sqlite3VdbeSerialType( Mem pMem, int file_format )
    {
      int flags = pMem.flags;
      int n;

      if ( ( flags & MEM_Null ) != 0 )
      {
        return 0;
      }
      if ( ( flags & MEM_Int ) != 0 )
      {
        /* Figure out whether to use 1, 2, 4, 6 or 8 bytes. */
        const i64 MAX_6BYTE = ( ( ( (i64)0x00008000 ) << 32 ) - 1 );
        i64 i = pMem.u.i;
        u64 u;
        if ( file_format >= 4 && ( i & 1 ) == i )
        {
          return 8 + (u32)i;
        }
        u = (ulong)( i < 0 ? -i : i );
        if ( u <= 127 ) return 1;
        if ( u <= 32767 ) return 2;
        if ( u <= 8388607 ) return 3;
        if ( u <= 2147483647 ) return 4;
        if ( u <= MAX_6BYTE ) return 5;
        return 6;
      }
      if ( ( flags & MEM_Real ) != 0 )
      {
        return 7;
      }
      Debug.Assert( /* pMem.db.mallocFailed != 0 || */ ( flags & ( MEM_Str | MEM_Blob ) ) != 0 );
      n = pMem.n;
      if ((flags & MEM_Zero)!=0)
      {
        n += pMem.u.nZero;
      }
      else if ((flags & MEM_Blob) != 0)
      {
        n = pMem.zBLOB != null ? pMem.zBLOB.Length : pMem.z != null ? pMem.z.Length : 0;
      }
      else
      {
        if (pMem.z != null) n = Encoding.UTF8.GetByteCount(pMem.n < pMem.z.Length ? pMem.z.Substring(0, pMem.n) : pMem.z);
        else n = pMem.zBLOB.Length;
        pMem.n = n;
      }

      Debug.Assert( n >= 0 );
      return (u32)( ( n * 2 ) + 12 + ( ( ( flags & MEM_Str ) != 0 ) ? 1 : 0 ) );
    }

    /*
    ** Return the length of the data corresponding to the supplied serial-type.
    */
    static u32 sqlite3VdbeSerialTypeLen( u32 serial_type )
    {
      if ( serial_type >= 12 )
      {
        return (u32)( ( serial_type - 12 ) / 2 );
      }
      else
      {
        u32[] aSize = new u32[] { 0, 1, 2, 3, 4, 6, 8, 8, 0, 0, 0, 0 };
        return aSize[serial_type];
      }
    }

    /*
    ** If we are on an architecture with mixed-endian floating
    ** points (ex: ARM7) then swap the lower 4 bytes with the
    ** upper 4 bytes.  Return the result.
    **
    ** For most architectures, this is a no-op.
    **
    ** (later):  It is reported to me that the mixed-endian problem
    ** on ARM7 is an issue with GCC, not with the ARM7 chip.  It seems
    ** that early versions of GCC stored the two words of a 64-bit
    ** float in the wrong order.  And that error has been propagated
    ** ever since.  The blame is not necessarily with GCC, though.
    ** GCC might have just copying the problem from a prior compiler.
    ** I am also told that newer versions of GCC that follow a different
    ** ABI get the byte order right.
    **
    ** Developers using SQLite on an ARM7 should compile and run their
    ** application using -DSQLITE_DEBUG=1 at least once.  With DEBUG
    ** enabled, some Debug.Asserts below will ensure that the byte order of
    ** floating point values is correct.
    **
    ** (2007-08-30)  Frank van Vugt has studied this problem closely
    ** and has send his findings to the SQLite developers.  Frank
    ** writes that some Linux kernels offer floating point hardware
    ** emulation that uses only 32-bit mantissas instead of a full
    ** 48-bits as required by the IEEE standard.  (This is the
    ** CONFIG_FPE_FASTFPE option.)  On such systems, floating point
    ** byte swapping becomes very complicated.  To avoid problems,
    ** the necessary byte swapping is carried out using a 64-bit integer
    ** rather than a 64-bit float.  Frank assures us that the code here
    ** works for him.  We, the developers, have no way to independently
    ** verify this, but Frank seems to know what he is talking about
    ** so we trust him.
    */
#if  SQLITE_MIXED_ENDIAN_64BIT_FLOAT
//static u64 floatSwap(u64 in){
//  union {
//    u64 r;
//    u32 i[2];
//  } u;
//  u32 t;

//  u.r = in;
//  t = u.i[0];
//  u.i[0] = u.i[1];
//  u.i[1] = t;
//  return u.r;
//}
//# define swapMixedEndianFloat(X)  X = floatSwap(X)
#else
    //# define swapMixedEndianFloat(X)
#endif

    /*
** Write the serialized data blob for the value stored in pMem into
** buf. It is assumed that the caller has allocated sufficient space.
** Return the number of bytes written.
**
** nBuf is the amount of space left in buf[].  nBuf must always be
** large enough to hold the entire field.  Except, if the field is
** a blob with a zero-filled tail, then buf[] might be just the right
** size to hold everything except for the zero-filled tail.  If buf[]
** is only big enough to hold the non-zero prefix, then only write that
** prefix into buf[].  But if buf[] is large enough to hold both the
** prefix and the tail then write the prefix and set the tail to all
** zeros.
**
** Return the number of bytes actually written into buf[].  The number
** of bytes in the zero-filled tail is included in the return value only
** if those bytes were zeroed in buf[].
*/
    static u32 sqlite3VdbeSerialPut( byte[] buf, int offset, int nBuf, Mem pMem, int file_format )
    {
      u32 serial_type = sqlite3VdbeSerialType( pMem, file_format );
      u32 len;

      /* Integer and Real */
      if ( serial_type <= 7 && serial_type > 0 )
      {
        u64 v;
        u32 i;
        if ( serial_type == 7 )
        {
          //Debug.Assert( sizeof( v) == sizeof(pMem.r));
          v = (ulong)BitConverter.DoubleToInt64Bits( pMem.r );// memcpy( &v, pMem.r, v ).Length;
#if  SQLITE_MIXED_ENDIAN_64BIT_FLOAT
swapMixedEndianFloat( v );
#endif
        }
        else
        {
          v = (ulong)pMem.u.i;
        }
        len = i = sqlite3VdbeSerialTypeLen( serial_type );
        Debug.Assert( len <= (u32)nBuf );
        while ( i-- != 0 )
        {
          buf[offset + i] = (u8)( v & 0xFF );
          v >>= 8;
        }
        return len;
      }

      /* String or blob */
      if ( serial_type >= 12 )
      {
        Debug.Assert( pMem.n + ( ( pMem.flags & MEM_Zero ) != 0 ? pMem.u.nZero : 0 ) == (int)sqlite3VdbeSerialTypeLen( serial_type ) );
        Debug.Assert( pMem.n <= nBuf );
        if ( ( len = (u32)pMem.n ) != 0 )
          if (pMem.zBLOB==null && String.IsNullOrEmpty(pMem.z)) 
          {}
        else if ( ( pMem.flags & MEM_Blob ) != 0 || pMem.z == null )
            Buffer.BlockCopy( pMem.zBLOB, 0, buf, offset, (int)len );//memcpy( buf, pMem.z, len );
          else
            Buffer.BlockCopy( Encoding.UTF8.GetBytes( pMem.z ), 0, buf, offset, (int)len );//memcpy( buf, pMem.z, len );
        if ( ( pMem.flags & MEM_Zero ) != 0 )
        {
          len += (u32)pMem.u.nZero;
          Debug.Assert( nBuf >= 0 );
          if ( len > (u32)nBuf )
          {
            len = (u32)nBuf;
          }
          Array.Clear( buf, offset + pMem.n, (int)( len - pMem.n ) );// memset( &buf[pMem.n], 0, len - pMem.n );
        }
        return len;
      }

      /* NULL or constants 0 or 1 */
      return 0;
    }

    /*
    ** Deserialize the data blob pointed to by buf as serial type serial_type
    ** and store the result in pMem.  Return the number of bytes read.
    */
    static u32 sqlite3VdbeSerialGet(
    byte[] buf,         /* Buffer to deserialize from */
    int offset,         /* Offset into Buffer */
    u32 serial_type,    /* Serial type to deserialize */
    Mem pMem            /* Memory cell to write value into */
    )
    {
      switch ( serial_type )
      {
        case 10:   /* Reserved for future use */
        case 11:   /* Reserved for future use */
        case 0:
          {  /* NULL */
            pMem.flags = MEM_Null;
            pMem.n = 0;
            pMem.z = null;
            pMem.zBLOB = null;
            break;
          }
        case 1:
          { /* 1-byte signed integer */
            pMem.u.i = (sbyte)buf[offset + 0];
            pMem.flags = MEM_Int;
            return 1;
          }
        case 2:
          { /* 2-byte signed integer */
            pMem.u.i = (int)( ( ( (sbyte)buf[offset + 0] ) << 8 ) | buf[offset + 1] );
            pMem.flags = MEM_Int;
            return 2;
          }
        case 3:
          { /* 3-byte signed integer */
            pMem.u.i = (int)( ( ( (sbyte)buf[offset + 0] ) << 16 ) | ( buf[offset + 1] << 8 ) | buf[offset + 2] );
            pMem.flags = MEM_Int;
            return 3;
          }
        case 4:
          { /* 4-byte signed integer */
            pMem.u.i = (int)( ( (sbyte)buf[offset + 0] << 24 ) | ( buf[offset + 1] << 16 ) | ( buf[offset + 2] << 8 ) | buf[offset + 3] );
            pMem.flags = MEM_Int;
            return 4;
          }
        case 5:
          { /* 6-byte signed integer */
            u64 x = (ulong)( ( ( (sbyte)buf[offset + 0] ) << 8 ) | buf[offset + 1] );
            u32 y = (u32)( ( buf[offset + 2] << 24 ) | ( buf[offset + 3] << 16 ) | ( buf[offset + 4] << 8 ) | buf[offset + 5] );
            x = ( x << 32 ) | y;
            pMem.u.i = (i64)x;
            pMem.flags = MEM_Int;
            return 6;
          }
        case 6:   /* 8-byte signed integer */
        case 7:
          { /* IEEE floating point */
            u64 x;
            u32 y;
#if !NDEBUG && !SQLITE_OMIT_FLOATING_POINT
            /* Verify that integers and floating point values use the same
** byte order.  Or, that if SQLITE_MIXED_ENDIAN_64BIT_FLOAT is
** defined that 64-bit floating point values really are mixed
** endian.
*/
            const u64 t1 = ( (u64)0x3ff00000 ) << 32;
            const double r1 = 1.0;
            u64 t2 = t1;
#if  SQLITE_MIXED_ENDIAN_64BIT_FLOAT
swapMixedEndianFloat(t2);
#endif
            Debug.Assert( sizeof( double ) == sizeof( u64 ) && memcmp( BitConverter.GetBytes( r1 ), BitConverter.GetBytes( t2 ), sizeof( double ) ) == 0 );//Debug.Assert( sizeof(r1)==sizeof(t2) && memcmp(&r1, t2, sizeof(r1))==0 );
#endif

            x = (u64)( ( buf[offset + 0] << 24 ) | ( buf[offset + 1] << 16 ) | ( buf[offset + 2] << 8 ) | buf[offset + 3] );
            y = (u32)( ( buf[offset + 4] << 24 ) | ( buf[offset + 5] << 16 ) | ( buf[offset + 6] << 8 ) | buf[offset + 7] );
            x = ( x << 32 ) | y;
            if ( serial_type == 6 )
            {
              pMem.u.i = (i64)x;
              pMem.flags = MEM_Int;
            }
            else
            {
              Debug.Assert( sizeof( i64 ) == 8 && sizeof( double ) == 8 );
#if  SQLITE_MIXED_ENDIAN_64BIT_FLOAT
swapMixedEndianFloat(x);
#endif
              pMem.r = BitConverter.Int64BitsToDouble( (long)x );// memcpy(pMem.r, x, sizeof(x))
              pMem.flags = (u16)( sqlite3IsNaN( pMem.r ) ? MEM_Null : MEM_Real );
            }
            return 8;
          }
        case 8:    /* Integer 0 */
        case 9:
          {  /* Integer 1 */
            pMem.u.i = serial_type - 8;
            pMem.flags = MEM_Int;
            return 0;
          }
        default:
          {
            u32 len = ( serial_type - 12 ) / 2;
            pMem.n = (int)len;
            pMem.xDel = null;
            if ( ( serial_type & 0x01 ) != 0 )
            {
              pMem.flags = MEM_Str | MEM_Ephem;
              pMem.z = Encoding.UTF8.GetString( buf, offset, (int)len );//memcpy( buf, pMem.z, len );
              pMem.n = pMem.z.Length;
              pMem.zBLOB = null;
            }
            else
            {
              pMem.z = null;
              pMem.zBLOB = new byte[len];
              pMem.flags = MEM_Blob | MEM_Ephem;
              Buffer.BlockCopy( buf, offset, pMem.zBLOB, 0, (int)len );//memcpy( buf, pMem.z, len );
            }
            return len;
          }
      }
      return 0;
    }

    static int sqlite3VdbeSerialGet(
    byte[] buf,     /* Buffer to deserialize from */
    u32 serial_type,              /* Serial type to deserialize */
    Mem pMem                     /* Memory cell to write value into */
    )
    {
      switch ( serial_type )
      {
        case 10:   /* Reserved for future use */
        case 11:   /* Reserved for future use */
        case 0:
          {  /* NULL */
            pMem.flags = MEM_Null;
            break;
          }
        case 1:
          { /* 1-byte signed integer */
            pMem.u.i = (sbyte)buf[0];
            pMem.flags = MEM_Int;
            return 1;
          }
        case 2:
          { /* 2-byte signed integer */
            pMem.u.i = (int)( ( ( buf[0] ) << 8 ) | buf[1] );
            pMem.flags = MEM_Int;
            return 2;
          }
        case 3:
          { /* 3-byte signed integer */
            pMem.u.i = (int)( ( ( buf[0] ) << 16 ) | ( buf[1] << 8 ) | buf[2] );
            pMem.flags = MEM_Int;
            return 3;
          }
        case 4:
          { /* 4-byte signed integer */
            pMem.u.i = (int)( ( buf[0] << 24 ) | ( buf[1] << 16 ) | ( buf[2] << 8 ) | buf[3] );
            pMem.flags = MEM_Int;
            return 4;
          }
        case 5:
          { /* 6-byte signed integer */
            u64 x = (ulong)( ( ( buf[0] ) << 8 ) | buf[1] );
            u32 y = (u32)( ( buf[2] << 24 ) | ( buf[3] << 16 ) | ( buf[4] << 8 ) | buf[5] );
            x = ( x << 32 ) | y;
            pMem.u.i = (i64)x;
            pMem.flags = MEM_Int;
            return 6;
          }
        case 6:   /* 8-byte signed integer */
        case 7:
          { /* IEEE floating point */
            u64 x;
            u32 y;
#if !NDEBUG && !SQLITE_OMIT_FLOATING_POINT
            /* Verify that integers and floating point values use the same
** byte order.  Or, that if SQLITE_MIXED_ENDIAN_64BIT_FLOAT is
** defined that 64-bit floating point values really are mixed
** endian.
*/
            const u64 t1 = ( (u64)0x3ff00000 ) << 32;
            const double r1 = 1.0;
            u64 t2 = t1;
#if  SQLITE_MIXED_ENDIAN_64BIT_FLOAT
swapMixedEndianFloat(t2);
#endif
            Debug.Assert( sizeof( double ) == sizeof( u64 ) && memcmp( BitConverter.GetBytes( r1 ), BitConverter.GetBytes( t2 ), sizeof( double ) ) == 0 );//Debug.Assert( sizeof(r1)==sizeof(t2) && memcmp(&r1, t2, sizeof(r1))==0 );
#endif

            x = (u64)( ( buf[0] << 24 ) | ( buf[1] << 16 ) | ( buf[2] << 8 ) | buf[3] );
            y = (u32)( ( buf[4] << 24 ) | ( buf[5] << 16 ) | ( buf[6] << 8 ) | buf[7] );
            x = ( x << 32 ) | y;
            if ( serial_type == 6 )
            {
              pMem.u.i = (i64)x;
              pMem.flags = MEM_Int;
            }
            else
            {
              Debug.Assert( sizeof( i64 ) == 8 && sizeof( double ) == 8 );
#if  SQLITE_MIXED_ENDIAN_64BIT_FLOAT
swapMixedEndianFloat(x);
#endif
              pMem.r = BitConverter.Int64BitsToDouble( (long)x );// memcpy(pMem.r, x, sizeof(x))
              pMem.flags = MEM_Real;
            }
            return 8;
          }
        case 8:    /* Integer 0 */
        case 9:
          {  /* Integer 1 */
            pMem.u.i = serial_type - 8;
            pMem.flags = MEM_Int;
            return 0;
          }
        default:
          {
            int len = (int)( ( serial_type - 12 ) / 2 );
            pMem.xDel = null;
            if ( ( serial_type & 0x01 ) != 0 )
            {
              pMem.flags = MEM_Str | MEM_Ephem;
              pMem.z = Encoding.UTF8.GetString( buf, 0, len );//memcpy( buf, pMem.z, len );
              pMem.n = pMem.z.Length;// len;
              pMem.zBLOB = null;
            }
            else
            {
              pMem.flags = MEM_Blob | MEM_Ephem;
              pMem.zBLOB = new byte[len];
              buf.CopyTo( pMem.zBLOB, 0 );
              pMem.n = len;// len;
              pMem.z = null;
            }
            return len;
          }
      }
      return 0;
    }

    /*
    ** Given the nKey-byte encoding of a record in pKey[], parse the
    ** record into a UnpackedRecord structure.  Return a pointer to
    ** that structure.
    **
    ** The calling function might provide szSpace bytes of memory
    ** space at pSpace.  This space can be used to hold the returned
    ** VDbeParsedRecord structure if it is large enough.  If it is
    ** not big enough, space is obtained from sqlite3Malloc().
    **
    ** The returned structure should be closed by a call to
    ** sqlite3VdbeDeleteUnpackedRecord().
    */
    static UnpackedRecord sqlite3VdbeRecordUnpack(
    KeyInfo pKeyInfo,   /* Information about the record format */
    int nKey,           /* Size of the binary record */
    byte[] pKey,        /* The binary record */
    UnpackedRecord pSpace, //  char *pSpace,          /* Unaligned space available to hold the object */
    int szSpace         /* Size of pSpace[] in bytes */
    )
    {
      byte[] aKey = pKey;
      UnpackedRecord p;     /* The unpacked record that we will return */
      int nByte;            /* Memory space needed to hold p, in bytes */
      int d;
      u32 idx;
      int u;                /* Unsigned loop counter */
      int szHdr = 0;
      Mem pMem;
      int nOff;           /* Increase pSpace by this much to 8-byte align it */

      /*
      ** We want to shift the pointer pSpace up such that it is 8-byte aligned.
      ** Thus, we need to calculate a value, nOff, between 0 and 7, to shift
      ** it by.  If pSpace is already 8-byte aligned, nOff should be zero.
      */
      //nOff = ( 8 - ( SQLITE_PTR_TO_INT( pSpace ) & 7 ) ) & 7;
      //pSpace += nOff;
      //szSpace -= nOff;
      //nByte = ROUND8( sizeof( UnpackedRecord ) ) + sizeof( Mem ) * ( pKeyInfo->nField + 1 );
      //if ( nByte > szSpace)
      //{
      //  p = new UnpackedRecord();//sqlite3DbMallocRaw(pKeyInfo.db, nByte);
      //  if ( p == null ) return null;
      //  p.flags = UNPACKED_NEED_FREE | UNPACKED_NEED_DESTROY;
      //}
      //else
      {
        p = pSpace;//(UnpackedRecord*)pSpace;
        p.flags = UNPACKED_NEED_DESTROY;
      }
      p.pKeyInfo = pKeyInfo;
      p.nField = (u16)( pKeyInfo.nField + 1 );
      //p->aMem = pMem = (Mem*)&( (char*)p )[ROUND8( sizeof( UnpackedRecord ) )];
      //assert( EIGHT_BYTE_ALIGNMENT( pMem ) );
      p.aMem = new Mem[p.nField + 1];
      idx = (u32)getVarint32( aKey, 0, ref szHdr );// GetVarint( aKey, szHdr );
      d = (int)szHdr;
      u = 0;
      while ( idx < (int)szHdr && u < p.nField && d <= nKey )
      {
        p.aMem[u] = new Mem();
        pMem = p.aMem[u];
        u32 serial_type = 0;

        idx += (u32)getVarint32( aKey, idx, ref serial_type );// GetVarint( aKey + idx, serial_type );
        pMem.enc = pKeyInfo.enc;
        pMem.db = pKeyInfo.db;
        pMem.flags = 0;
        //pMem.zMalloc = null;
        d += (int)sqlite3VdbeSerialGet( aKey, d, serial_type, pMem );
        //pMem++;
        u++;
      }
      Debug.Assert( u <= pKeyInfo.nField + 1 );
      p.nField = (u16)u;
      return p;// (void*)p;
    }

    /*
    ** This routine destroys a UnpackedRecord object.
    */
    static void sqlite3VdbeDeleteUnpackedRecord( UnpackedRecord p )
    {
      int i;
      Mem pMem;
      Debug.Assert( p != null );
      Debug.Assert( ( p.flags & UNPACKED_NEED_DESTROY ) != 0 );
      //for ( i = 0, pMem = p->aMem ; i < p->nField ; i++, pMem++ )
      //{
      //  /* The unpacked record is always constructed by the
      //  ** sqlite3VdbeUnpackRecord() function above, which makes all
      //  ** strings and blobs static.  And none of the elements are
      //  ** ever transformed, so there is never anything to delete.
      //  */
      //  if ( NEVER( pMem->zMalloc ) ) sqlite3VdbeMemRelease( pMem );
      //}
      if ( ( p.flags & UNPACKED_NEED_FREE ) != 0 )
      {
        p = null;//sqlite3DbFree( p.pKeyInfo.db, ref p );
      }
    }

    /*
    ** This function compares the two table rows or index records
    ** specified by {nKey1, pKey1} and pPKey2.  It returns a negative, zero
    ** or positive integer if key1 is less than, equal to or
    ** greater than key2.  The {nKey1, pKey1} key must be a blob
    ** created by th OP_MakeRecord opcode of the VDBE.  The pPKey2
    ** key must be a parsed key such as obtained from
    ** sqlite3VdbeParseRecord.
    **
    ** Key1 and Key2 do not have to contain the same number of fields.
    ** The key with fewer fields is usually compares less than the
    ** longer key.  However if the UNPACKED_INCRKEY flags in pPKey2 is set
    ** and the common prefixes are equal, then key1 is less than key2.
    ** Or if the UNPACKED_MATCH_PREFIX flag is set and the prefixes are
    ** equal, then the keys are considered to be equal and
    ** the parts beyond the common prefix are ignored.
    **
    ** If the UNPACKED_IGNORE_ROWID flag is set, then the last byte of
    ** the header of pKey1 is ignored.  It is assumed that pKey1 is
    ** an index key, and thus ends with a rowid value.  The last byte
    ** of the header will therefore be the serial type of the rowid:
    ** one of 1, 2, 3, 4, 5, 6, 8, or 9 - the integer serial types.
    ** The serial type of the final rowid will always be a single byte.
    ** By ignoring this last byte of the header, we force the comparison
    ** to ignore the rowid at the end of key1.
    */

    // ALTERNATE FORM for C#
    static int sqlite3VdbeRecordCompare(
    int nKey1, byte[] pKey1,    /* Left key */
    UnpackedRecord pPKey2       /* Right key */
    )
    {
      return sqlite3VdbeRecordCompare( nKey1, pKey1, 0, pPKey2 );
    }

    static int sqlite3VdbeRecordCompare(
    int nKey1, byte[] pKey1,    /* Left key */
    int offset,
    UnpackedRecord pPKey2       /* Right key */
    )
    {
      int d1;            /* Offset into aKey[] of next data element */
      u32 idx1;          /* Offset into aKey[] of next header element */
      u32 szHdr1;        /* Number of bytes in header */
      int i = 0;
      int nField;
      int rc = 0;
      byte[] aKey1 = new byte[pKey1.Length - offset];
      Buffer.BlockCopy( pKey1, offset, aKey1, 0, aKey1.Length );
      KeyInfo pKeyInfo;

      Mem mem1 = new Mem();
      pKeyInfo = pPKey2.pKeyInfo;
      mem1.enc = pKeyInfo.enc;
      mem1.db = pKeyInfo.db;
      mem1.flags = 0;
      mem1.u.i = 0;  /* not needed, here to silence compiler warning */
      //mem1.zMalloc = null;

      idx1 = (u32)( ( szHdr1 = aKey1[0] ) <= 0x7f ? 1 : getVarint32( aKey1, 0, ref szHdr1 ) );// GetVarint( aKey1, szHdr1 );
      d1 = (int)szHdr1;
      if ( ( pPKey2.flags & UNPACKED_IGNORE_ROWID ) != 0 )
      {
        szHdr1--;
      }
      nField = pKeyInfo.nField;
      while ( idx1 < szHdr1 && i < pPKey2.nField )
      {
        u32 serial_type1;

        /* Read the serial types for the next element in each key. */
        idx1 += (u32)( ( serial_type1 = aKey1[idx1] ) <= 0x7f ? 1 : getVarint32( aKey1, idx1, ref serial_type1 ) ); //GetVarint( aKey1 + idx1, serial_type1 );
        if ( d1 >= nKey1 && sqlite3VdbeSerialTypeLen( serial_type1 ) > 0 ) break;

        /* Extract the values to be compared.
        */
        d1 += (int)sqlite3VdbeSerialGet( aKey1, d1, serial_type1, mem1 );

        /* Do the comparison
        */
        rc = sqlite3MemCompare( mem1, pPKey2.aMem[i], i < nField ? pKeyInfo.aColl[i] : null );
        if ( rc != 0 )
        {
          break;
        }
        i++;
      }
      /* No memory allocation is ever used on mem1. */
      //if ( NEVER( mem1.zMalloc ) ) sqlite3VdbeMemRelease( &mem1 );

      /* If the PREFIX_SEARCH flag is set and all fields except the final
      ** rowid field were equal, then clear the PREFIX_SEARCH flag and set
      ** pPKey2->rowid to the value of the rowid field in (pKey1, nKey1).
      ** This is used by the OP_IsUnique opcode.
      */
      if ( ( pPKey2.flags & UNPACKED_PREFIX_SEARCH ) != 0 && i == ( pPKey2.nField - 1 ) )
      {
        Debug.Assert( idx1 == szHdr1 && rc != 0 );
        Debug.Assert( ( mem1.flags & MEM_Int ) != 0 );
        pPKey2.flags = (ushort)( pPKey2.flags & ~UNPACKED_PREFIX_SEARCH );
        pPKey2.rowid = mem1.u.i;
      }

      if ( rc == 0 )
      {
        /* rc==0 here means that one of the keys ran out of fields and
        ** all the fields up to that point were equal. If the UNPACKED_INCRKEY
        ** flag is set, then break the tie by treating key2 as larger.
        ** If the UPACKED_PREFIX_MATCH flag is set, then keys with common prefixes
        ** are considered to be equal.  Otherwise, the longer key is the
        ** larger.  As it happens, the pPKey2 will always be the longer
        ** if there is a difference.
        */
        if ( ( pPKey2.flags & UNPACKED_INCRKEY ) != 0 )
        {
          rc = -1;
        }
        else if ( ( pPKey2.flags & UNPACKED_PREFIX_MATCH ) != 0 )
        {
          /* Leave rc==0 */
        }
        else if ( idx1 < szHdr1 )
        {
          rc = 1;
        }
      }
      else if ( pKeyInfo.aSortOrder != null && i < pKeyInfo.nField
      && pKeyInfo.aSortOrder[i] != 0 )
      {
        rc = -rc;
      }

      return rc;
    }

    /*
    ** pCur points at an index entry created using the OP_MakeRecord opcode.
    ** Read the rowid (the last field in the record) and store it in *rowid.
    ** Return SQLITE_OK if everything works, or an error code otherwise.
    **
    ** pCur might be pointing to text obtained from a corrupt database file.
    ** So the content cannot be trusted.  Do appropriate checks on the content.
    */
    static int sqlite3VdbeIdxRowid( sqlite3 db, BtCursor pCur, ref i64 rowid )
    {
      i64 nCellKey = 0;
      int rc;
      u32 szHdr = 0;        /* Size of the header */
      u32 typeRowid = 0;    /* Serial type of the rowid */
      u32 lenRowid;       /* Size of the rowid */
      Mem m = new Mem(); Mem v = new Mem();

      /* Get the size of the index entry.  Only indices entries of less
      ** than 2GiB are support - anything large must be database corruption.
      ** Any corruption is detected in sqlite3BtreeParseCellPtr(), though, so
      ** this code can safely assume that nCellKey is 32-bits  
      */
      Debug.Assert( sqlite3BtreeCursorIsValid( pCur ) );
      rc = sqlite3BtreeKeySize( pCur, ref nCellKey );
      Debug.Assert( rc == SQLITE_OK );     /* pCur is always valid so KeySize cannot fail */
      Debug.Assert( ( (u32)nCellKey & SQLITE_MAX_U32 ) == (u64)nCellKey );

      /* Read in the complete content of the index entry */
      m.flags = 0;
      m.db = db;
      //m.zMalloc = null;
      rc = sqlite3VdbeMemFromBtree( pCur, 0, (int)nCellKey, true, m );
      if ( rc != 0 )
      {
        return rc;
      }

      /* The index entry must begin with a header size */
      getVarint32( m.zBLOB, 0, ref szHdr );
      testcase( szHdr == 3 );
      testcase( szHdr == m.n );
      if ( unlikely( szHdr < 3 || (int)szHdr > m.n ) )
      {
        goto idx_rowid_corruption;
      }

      /* The last field of the index should be an integer - the ROWID.
      ** Verify that the last entry really is an integer. */
      getVarint32( m.zBLOB, szHdr - 1, ref typeRowid );
      testcase( typeRowid == 1 );
      testcase( typeRowid == 2 );
      testcase( typeRowid == 3 );
      testcase( typeRowid == 4 );
      testcase( typeRowid == 5 );
      testcase( typeRowid == 6 );
      testcase( typeRowid == 8 );
      testcase( typeRowid == 9 );
      if ( unlikely( typeRowid < 1 || typeRowid > 9 || typeRowid == 7 ) )
      {
        goto idx_rowid_corruption;
      }
      lenRowid = (u32)sqlite3VdbeSerialTypeLen( typeRowid );
      testcase( (u32)m.n == szHdr + lenRowid );
      if ( unlikely( (u32)m.n < szHdr + lenRowid ) )
      {
        goto idx_rowid_corruption;
      }

      /* Fetch the integer off the end of the index record */
      sqlite3VdbeSerialGet( m.zBLOB, (int)( m.n - lenRowid ), typeRowid, v );
      rowid = v.u.i;
      sqlite3VdbeMemRelease( m );
      return SQLITE_OK;

      /* Jump here if database corruption is detected after m has been
      ** allocated.  Free the m object and return SQLITE_CORRUPT. */
idx_rowid_corruption:
      //testcase( m.zMalloc != 0 );
      sqlite3VdbeMemRelease( m );
#if SQLITE_DEBUG
      return SQLITE_CORRUPT_BKPT();
#else
return SQLITE_CORRUPT_BKPT;
#endif
    }

    /*
    ** Compare the key of the index entry that cursor pC is pointing to against
    ** the key string in pUnpacked.  Write into *pRes a number
    ** that is negative, zero, or positive if pC is less than, equal to,
    ** or greater than pUnpacked.  Return SQLITE_OK on success.
    **
    ** pUnpacked is either created without a rowid or is truncated so that it
    ** omits the rowid at the end.  The rowid at the end of the index entry
    ** is ignored as well.  Hence, this routine only compares the prefixes 
    ** of the keys prior to the final rowid, not the entire key.
    */
    static int sqlite3VdbeIdxKeyCompare(
    VdbeCursor pC,              /* The cursor to compare against */
    UnpackedRecord pUnpacked,   /* Unpacked version of key to compare against */
    ref int res                 /* Write the comparison result here */
    )
    {
      i64 nCellKey = 0;
      int rc;
      BtCursor pCur = pC.pCursor;
      Mem m = new Mem();

      Debug.Assert( sqlite3BtreeCursorIsValid( pCur ) );
      rc = sqlite3BtreeKeySize( pCur, ref nCellKey );
      Debug.Assert( rc == SQLITE_OK );    /* pCur is always valid so KeySize cannot fail */
      /* nCellKey will always be between 0 and 0xffffffff because of the say
      ** that btreeParseCellPtr() and sqlite3GetVarint32() are implemented */
      if ( nCellKey <= 0 || nCellKey > 0x7fffffff )
      {
        res = 0;
        return SQLITE_CORRUPT;
      }
      m.flags = 0;
      m.db = null;
      //m.zMalloc = null;
      rc = sqlite3VdbeMemFromBtree( pC.pCursor, 0, (int)nCellKey, true, m );
      if ( rc != 0 )
      {
        return rc;
      }
      Debug.Assert( ( pUnpacked.flags & UNPACKED_IGNORE_ROWID ) != 0 );
      res = sqlite3VdbeRecordCompare( m.n, m.zBLOB, pUnpacked );
      sqlite3VdbeMemRelease( m );
      return SQLITE_OK;
    }

    /*
    ** This routine sets the value to be returned by subsequent calls to
    ** sqlite3_changes() on the database handle 'db'.
    */
    static void sqlite3VdbeSetChanges( sqlite3 db, int nChange )
    {
      Debug.Assert( sqlite3_mutex_held( db.mutex ) );
      db.nChange = nChange;
      db.nTotalChange += nChange;
    }

    /*
    ** Set a flag in the vdbe to update the change counter when it is finalised
    ** or reset.
    */
    static void sqlite3VdbeCountChanges( Vdbe v )
    {
      v.changeCntOn = true;
    }

    /*
    ** Mark every prepared statement associated with a database connection
    ** as expired.
    **
    ** An expired statement means that recompilation of the statement is
    ** recommend.  Statements expire when things happen that make their
    ** programs obsolete.  Removing user-defined functions or collating
    ** sequences, or changing an authorization function are the types of
    ** things that make prepared statements obsolete.
    */
    static void sqlite3ExpirePreparedStatements( sqlite3 db )
    {
      Vdbe p;
      for ( p = db.pVdbe ; p != null ; p = p.pNext )
      {
        p.expired = true;
      }
    }

    /*
    ** Return the database associated with the Vdbe.
    */
    static sqlite3 sqlite3VdbeDb( Vdbe v )
    {
      return v.db;
    }
  }
}
