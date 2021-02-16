using i64 = System.Int64;
using u8 = System.Byte;

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
    ** Header file for the Virtual DataBase Engine (VDBE)
    **
    ** This header defines the interface to the virtual database engine
    ** or VDBE.  The VDBE implements an abstract machine that runs a
    ** simple program to access and modify the underlying database.
    **
    ** $Id: vdbe.h,v 1.142 2009/07/24 17:58:53 danielk1977 Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#if !_SQLITE_VDBE_H_
    //#define _SQLITE_VDBE_H_
    //#include <stdio.h>

    /*
    ** A single VDBE is an opaque structure named "Vdbe".  Only routines
    ** in the source file sqliteVdbe.c are allowed to see the insides
    ** of this structure.
    */
    //typedef struct Vdbe Vdbe;

    /*
    ** The names of the following types declared in vdbeInt.h are required
    ** for the VdbeOp definition.
    */
    //typedef struct VdbeFunc VdbeFunc;
    //typedef struct Mem Mem;

    /*
    ** A single instruction of the virtual machine has an opcode
    ** and as many as three operands.  The instruction is recorded
    ** as an instance of the following structure:
    */
    public class union_p4
    {             /* forth parameter */
      public int i;                /* Integer value if p4type==P4_INT32 */
      public object p;             /* Generic pointer */
      //public string z;           /* Pointer to data for string (char array) types */
      public string z;             // In C# string is unicode, so use byte[] instead
      public i64 pI64;             /* Used when p4type is P4_INT64 */
      public double pReal;         /* Used when p4type is P4_REAL */
      public FuncDef pFunc;        /* Used when p4type is P4_FUNCDEF */
      public VdbeFunc pVdbeFunc;   /* Used when p4type is P4_VDBEFUNC */
      public CollSeq pColl;        /* Used when p4type is P4_COLLSEQ */
      public Mem pMem;             /* Used when p4type is P4_MEM */
      public VTable pVtab;         /* Used when p4type is P4_VTAB */
      public KeyInfo pKeyInfo;     /* Used when p4type is P4_KEYINFO */
      public int[] ai;             /* Used when p4type is P4_INTARRAY */
      public dxDel pFuncDel;       /* Used when p4type is P4_FUNCDEL */
    } ;
    public class VdbeOp
    {
      public u8 opcode;           /* What operation to perform */
      public int p4type;          /* One of the P4_xxx constants for p4 */
      public u8 opflags;          /* Not currently used */
      public u8 p5;               /* Fifth parameter is an unsigned character */
#if DEBUG_CLASS_VDBEOP || DEBUG_CLASS_ALL
public int _p1;              /* First operand */
public int p1
{
get { return _p1; }
set { _p1 = value; }
}

public int _p2;              /* Second parameter (often the jump destination) */
public int p2
{
get { return _p2; }
set { _p2 = value; }
}

public int _p3;              /* The third parameter */
public int p3
{
get { return _p3; }
set { _p3 = value; }
}
#else
      public int p1;              /* First operand */
      public int p2;              /* Second parameter (often the jump destination) */
      public int p3;              /* The third parameter */
#endif
      public union_p4 p4 = new union_p4();
#if SQLITE_DEBUG || DEBUG
      public string zComment;     /* Comment to improve readability */
#endif
#if VDBE_PROFILE
public int cnt;             /* Number of times this instruction was executed */
public u64 cycles;         /* Total time spend executing this instruction */
#endif
    };
    //typedef struct VdbeOp VdbeOp;

    /*
    ** A smaller version of VdbeOp used for the VdbeAddOpList() function because
    ** it takes up less space.
    */
    public struct VdbeOpList
    {
      public u8 opcode;  /* What operation to perform */
      public int p1;     /* First operand */
      public int p2;     /* Second parameter (often the jump destination) */
      public int p3;     /* Third parameter */
      public VdbeOpList( u8 opcode, int p1, int p2, int p3 )
      {
        this.opcode = opcode;
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
      }

    };
    //typedef struct VdbeOpList VdbeOpList;

    /*
    ** Allowed values of VdbeOp.p3type
    */
    const int P4_NOTUSED = 0;   /* The P4 parameter is not used */
    const int P4_DYNAMIC = ( -1 );  /* Pointer to a string obtained from sqliteMalloc=(); */
    const int P4_STATIC = ( -2 );  /* Pointer to a static string */
    const int P4_COLLSEQ = ( -4 );  /* P4 is a pointer to a CollSeq structure */
    const int P4_FUNCDEF = ( -5 );  /* P4 is a pointer to a FuncDef structure */
    const int P4_KEYINFO = ( -6 );  /* P4 is a pointer to a KeyInfo structure */
    const int P4_VDBEFUNC = ( -7 );  /* P4 is a pointer to a VdbeFunc structure */
    const int P4_MEM = ( -8 );  /* P4 is a pointer to a Mem*    structure */
    const int P4_TRANSIENT = ( -9 ); /* P4 is a pointer to a transient string */
    const int P4_VTAB = ( -10 ); /* P4 is a pointer to an sqlite3_vtab structure */
    const int P4_MPRINTF = ( -11 ); /* P4 is a string obtained from sqlite3_mprintf=(); */
    const int P4_REAL = ( -12 ); /* P4 is a 64-bit floating point value */
    const int P4_INT64 = ( -13 ); /* P4 is a 64-bit signed integer */
    const int P4_INT32 = ( -14 ); /* P4 is a 32-bit signed integer */
    const int P4_INTARRAY = ( -15 ); /* #define P4_INTARRAY (-15) /* P4 is a vector of 32-bit integers */

    /* When adding a P4 argument using P4_KEYINFO, a copy of the KeyInfo structure
    ** is made.  That copy is freed when the Vdbe is finalized.  But if the
    ** argument is P4_KEYINFO_HANDOFF, the passed in pointer is used.  It still
    ** gets freed when the Vdbe is finalized so it still should be obtained
    ** from a single sqliteMalloc().  But no copy is made and the calling
    ** function should *not* try to free the KeyInfo.
    */
    const int P4_KEYINFO_HANDOFF = ( -16 );  // #define P4_KEYINFO_HANDOFF (-16)
    const int P4_KEYINFO_STATIC = ( -17 );   // #define P4_KEYINFO_STATIC  (-17)

    /*
    ** The Vdbe.aColName array contains 5n Mem structures, where n is the
    ** number of columns of data returned by the statement.
    */
    //#define COLNAME_NAME     0
    //#define COLNAME_DECLTYPE 1
    //#define COLNAME_DATABASE 2
    //#define COLNAME_TABLE    3
    //#define COLNAME_COLUMN   4
    //#if SQLITE_ENABLE_COLUMN_METADATA
    //# define COLNAME_N        5      /* Number of COLNAME_xxx symbols */
    //#else
    //# ifdef SQLITE_OMIT_DECLTYPE
    //#   define COLNAME_N      1      /* Store only the name */
    //# else
    //#   define COLNAME_N      2      /* Store the name and decltype */
    //# endif
    //#endif
    const int COLNAME_NAME = 0;
    const int COLNAME_DECLTYPE = 1;
    const int COLNAME_DATABASE = 2;
    const int COLNAME_TABLE = 3;
    const int COLNAME_COLUMN = 4;
#if SQLITE_ENABLE_COLUMN_METADATA
const int COLNAME_N = 5;     /* Number of COLNAME_xxx symbols */
#else
# if SQLITE_OMIT_DECLTYPE
const int COLNAME_N = 1;     /* Number of COLNAME_xxx symbols */
# else
    const int COLNAME_N = 2;
# endif
#endif

    /*
** The following macro converts a relative address in the p2 field
** of a VdbeOp structure into a negative number so that
** sqlite3VdbeAddOpList() knows that the address is relative.  Calling
** the macro again restores the address.
*/
    //#define ADDR(X)  (-1-(X))
    static int ADDR( int x ) { return -1 - x; }
    /*
    ** The makefile scans the vdbe.c source file and creates the "opcodes.h"
    ** header file that defines a number for each opcode used by the VDBE.
    */
    //#include "opcodes.h"

    /*
    ** Prototypes for the VDBE interface.  See comments on the implementation
    ** for a description of what each of these routines does.
    */
    /*
    ** Prototypes for the VDBE interface.  See comments on the implementation
    ** for a description of what each of these routines does.
    */
    //Vdbe *sqlite3VdbeCreate(sqlite3*);
    //int sqlite3VdbeAddOp0(Vdbe*,int);
    //int sqlite3VdbeAddOp1(Vdbe*,int,int);
    //int sqlite3VdbeAddOp2(Vdbe*,int,int,int);
    //int sqlite3VdbeAddOp3(Vdbe*,int,int,int,int);
    //int sqlite3VdbeAddOp4(Vdbe*,int,int,int,int,const char *zP4,int);
    //int sqlite3VdbeAddOpList(Vdbe*, int nOp, VdbeOpList const *aOp);
    //void sqlite3VdbeChangeP1(Vdbe*, int addr, int P1);
    //void sqlite3VdbeChangeP2(Vdbe*, int addr, int P2);
    //void sqlite3VdbeChangeP3(Vdbe*, int addr, int P3);
    //void sqlite3VdbeChangeP5(Vdbe*, u8 P5);
    //void sqlite3VdbeJumpHere(Vdbe*, int addr);
    //void sqlite3VdbeChangeToNoop(Vdbe*, int addr, int N);
    //void sqlite3VdbeChangeP4(Vdbe*, int addr, const char *zP4, int N);
    //void sqlite3VdbeUsesBtree(Vdbe*, int);
    //VdbeOp *sqlite3VdbeGetOp(Vdbe*, int);
    //int sqlite3VdbeMakeLabel(Vdbe*);
    //void sqlite3VdbeDelete(Vdbe*);
    //void sqlite3VdbeMakeReady(Vdbe*,int,int,int,int);
    //int sqlite3VdbeFinalize(Vdbe*);
    //void sqlite3VdbeResolveLabel(Vdbe*, int);
    //int sqlite3VdbeCurrentAddr(Vdbe*);
    //#if SQLITE_DEBUG
    //  void sqlite3VdbeTrace(Vdbe*,FILE*);
    //#endif
    //void sqlite3VdbeResetStepResult(Vdbe*);
    //int sqlite3VdbeReset(Vdbe*);
    //void sqlite3VdbeSetNumCols(Vdbe*,int);
    //int sqlite3VdbeSetColName(Vdbe*, int, int, const char *, void(*)(void*));
    //void sqlite3VdbeCountChanges(Vdbe*);
    //sqlite3 *sqlite3VdbeDb(Vdbe*);
    //void sqlite3VdbeSetSql(Vdbe*, const char *z, int n, int);
    //void sqlite3VdbeSwap(Vdbe*,Vdbe*);

#if SQLITE_ENABLE_MEMORY_MANAGEMENT
//int sqlite3VdbeReleaseMemory(int);
#endif
    //UnpackedRecord *sqlite3VdbeRecordUnpack(KeyInfo*,int,const void*,char*,int);
    //void sqlite3VdbeDeleteUnpackedRecord(UnpackedRecord*);
    //int sqlite3VdbeRecordCompare(int,const void*,UnpackedRecord*);


#if !NDEBUG
    //void sqlite3VdbeComment(Vdbe*, const char*, ...);
    static void VdbeComment( Vdbe v, string zFormat, params object[] ap ) { sqlite3VdbeComment( v, zFormat, ap ); }//# define VdbeComment(X)  sqlite3VdbeComment X
    //void sqlite3VdbeNoopComment(Vdbe*, const char*, ...);
    static void VdbeNoopComment( Vdbe v, string zFormat, params object[] ap ) { sqlite3VdbeNoopComment( v, zFormat, ap ); }//# define VdbeNoopComment(X)  sqlite3VdbeNoopComment X
#else
//# define VdbeComment(X)
static void VdbeComment( Vdbe v, string zFormat, params object[] ap ) { }
//# define VdbeNoopComment(X)
static void VdbeNoopComment( Vdbe v, string zFormat, params object[] ap ) { }
#endif
  }
}
