using i64 = System.Int64;
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;
using Pgno = System.UInt32;

namespace winPEAS._3rdParty.SQLite.src
{
  using Op = CSSQLite.VdbeOp;

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
    ** This is the header file for information that is private to the
    ** VDBE.  This information used to all be at the top of the single
    ** source code file "vdbe.c".  When that file became too big (over
    ** 6000 lines long) it was split up into several smaller files and
    ** this header information was factored out.
    **
    ** $Id: vdbeInt.h,v 1.174 2009/06/23 14:15:04 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#if !_VDBEINT_H_
    //#define _VDBEINT_H_

    /*
    ** SQL is translated into a sequence of instructions to be
    ** executed by a virtual machine.  Each instruction is an instance
    ** of the following structure.
    */
    //typedef struct VdbeOp Op;

    /*
    ** Boolean values
    */
    //typedef unsigned char Bool;

    /*
    ** A cursor is a pointer into a single BTree within a database file.
    ** The cursor can seek to a BTree entry with a particular key, or
    ** loop over all entries of the Btree.  You can also insert new BTree
    ** entries or retrieve the key or data from the entry that the cursor
    ** is currently pointing to.
    **
    ** Every cursor that the virtual machine has open is represented by an
    ** instance of the following structure.
    **
    ** If the VdbeCursor.isTriggerRow flag is set it means that this cursor is
    ** really a single row that represents the NEW or OLD pseudo-table of
    ** a row trigger.  The data for the row is stored in VdbeCursor.pData and
    ** the rowid is in VdbeCursor.iKey.
    */
    public class VdbeCursor
    {
      public BtCursor pCursor;     /* The cursor structure of the backend */
      public int iDb;              /* Index of cursor database in db.aDb[] (or -1) */
      public i64 lastRowid;        /* Last rowid from a Next or NextIdx operation */
      public bool zeroed;          /* True if zeroed out and ready for reuse */
      public bool rowidIsValid;    /* True if lastRowid is valid */
      public bool atFirst;         /* True if pointing to first entry */
      public bool useRandomRowid;  /* Generate new record numbers semi-randomly */
      public bool nullRow;         /* True if pointing to a row with no data */
      public bool pseudoTable;     /* This is a NEW or OLD pseudo-tables of a trigger */
      public bool ephemPseudoTable;
      public bool deferredMoveto;  /* A call to sqlite3BtreeMoveto() is needed */
      public bool isTable;         /* True if a table requiring integer keys */
      public bool isIndex;         /* True if an index containing keys only - no data */
      public i64 movetoTarget;     /* Argument to the deferred sqlite3BtreeMoveto() */
      public Btree pBt;            /* Separate file holding temporary table */
      public int nData;            /* Number of bytes in pData */
      public byte[] pData;         /* Data for a NEW or OLD pseudo-table */
      public i64 iKey;             /* Key for the NEW or OLD pseudo-table row */
      public KeyInfo pKeyInfo;     /* Info about index keys needed by index cursors */
      public int nField;           /* Number of fields in the header */
      public int seqCount;         /* Sequence counter */
#if !SQLITE_OMIT_VIRTUALTABLE
public sqlite3_vtab_cursor pVtabCursor;  /* The cursor for a virtual table */
public readonly sqlite3_module pModule; /* Module for cursor pVtabCursor */
#endif

      /* Result of last sqlite3BtreeMoveto() done by an OP_NotExists or
** OP_IsUnique opcode on this cursor. */
      public int seekResult;

      /* Cached information about the header for the data record that the
      ** cursor is currently pointing to.  Only valid if cacheValid is true.
      ** aRow might point to (ephemeral) data for the current row, or it might
      ** be NULL.
      */
      public int cacheStatus;      /* Cache is valid if this matches Vdbe.cacheCtr */
      public Pgno payloadSize;     /* Total number of bytes in the record */
      public u32[] aType;          /* Type values for all entries in the record */
      public u32[] aOffset;        /* Cached offsets to the start of each columns data */
      public int aRow;             /* Pointer to Data for the current row, if all on one page */

    };
    //typedef struct VdbeCursor VdbeCursor;


    /*
    ** A value for VdbeCursor.cacheValid that means the cache is always invalid.
    */
    const int CACHE_STALE = 0;

    /*
    ** Internally, the vdbe manipulates nearly all SQL values as Mem
    ** structures. Each Mem struct may cache multiple representations (string,
    ** integer etc.) of the same value.  A value (and therefore Mem structure)
    ** has the following properties:
    **
    ** Each value has a manifest type. The manifest type of the value stored
    ** in a Mem struct is returned by the MemType(Mem*) macro. The type is
    ** one of SQLITE_NULL, SQLITE_INTEGER, SQLITE_REAL, SQLITE_TEXT or
    ** SQLITE_BLOB.
    */
    public class Mem
    {
      public struct union_ip
      {
#if DEBUG_CLASS_MEM || DEBUG_CLASS_ALL
public i64 _i;              /* First operand */
public i64 i
{
get { return _i; }
set { _i = value; }
}
#else
        public i64 i;               /* Integer value. */
#endif
        public int nZero;           /* Used when bit MEM_Zero is set in flags */
        public FuncDef pDef;        /* Used only when flags==MEM_Agg */
        public RowSet pRowSet;      /* Used only when flags==MEM_RowSet */
      };
      public union_ip u;
      public double r;              /* Real value */
      public sqlite3 db;            /* The associated database connection */
      public string z;              /* String value */
      public byte[] zBLOB;          /* BLOB value */
      public int n;                 /* Number of characters in string value, excluding '\0' */
#if DEBUG_CLASS_MEM || DEBUG_CLASS_ALL
public u16 _flags;              /* First operand */
public u16 flags
{
get { return _flags; }
set { _flags = value; }
}
#else
      public u16 flags = MEM_Null;  /* Some combination of MEM_Null, MEM_Str, MEM_Dyn, etc. */
#endif
      public u8 type = SQLITE_NULL; /* One of SQLITE_NULL, SQLITE_TEXT, SQLITE_INTEGER, etc */
      public u8 enc;                /* SQLITE_UTF8, SQLITE_UTF16BE, SQLITE_UTF16LE */
      public dxDel xDel;            /* If not null, call this function to delete Mem.z */
      // Not used under c#
      //public string zMalloc;      /* Dynamic buffer allocated by sqlite3Malloc() */
      public Mem _Mem;              /* Used when C# overload Z as MEM space */
      public SumCtx _SumCtx;        /* Used when C# overload Z as Sum context */
      public StrAccum _StrAccum;    /* Used when C# overload Z as STR context */
      public object _MD5Context;    /* Used when C# overload Z as MD5 context */


      public void CopyTo( Mem ct )
      {
        ct.u = u;
        ct.r = r;
        ct.db = db;
        ct.z = z;
        if ( zBLOB == null ) zBLOB = null;
        else { ct.zBLOB = (byte[])zBLOB.Clone(); }
        ct.n = n;
        ct.flags = flags;
        ct.type = type;
        ct.enc = enc;
        ct.xDel = xDel;
      }

    };

    /* One or more of the following flags are set to indicate the validOK
    ** representations of the value stored in the Mem struct.
    **
    ** If the MEM_Null flag is set, then the value is an SQL NULL value.
    ** No other flags may be set in this case.
    **
    ** If the MEM_Str flag is set then Mem.z points at a string representation.
    ** Usually this is encoded in the same unicode encoding as the main
    ** database (see below for exceptions). If the MEM_Term flag is also
    ** set, then the string is nul terminated. The MEM_Int and MEM_Real
    ** flags may coexist with the MEM_Str flag.
    **
    ** Multiple of these values can appear in Mem.flags.  But only one
    ** at a time can appear in Mem.type.
    */
    //#define MEM_Null      0x0001   /* Value is NULL */
    //#define MEM_Str       0x0002   /* Value is a string */
    //#define MEM_Int       0x0004   /* Value is an integer */
    //#define MEM_Real      0x0008   /* Value is a real number */
    //#define MEM_Blob      0x0010   /* Value is a BLOB */
    //#define MEM_RowSet    0x0020   /* Value is a RowSet object */
    //#define MEM_TypeMask  0x00ff   /* Mask of type bits */
    const int MEM_Null = 0x0001;  /* Value is NULL */
    const int MEM_Str = 0x0002;  /* Value is a string */
    const int MEM_Int = 0x0004;  /* Value is an integer */
    const int MEM_Real = 0x0008;  /* Value is a real number */
    const int MEM_Blob = 0x0010;  /* Value is a BLOB */
    const int MEM_RowSet = 0x0020;  /* Value is a RowSet object */
    const int MEM_TypeMask = 0x00ff;   /* Mask of type bits */

    /* Whenever Mem contains a valid string or blob representation, one of
    ** the following flags must be set to determine the memory management
    ** policy for Mem.z.  The MEM_Term flag tells us whether or not the
    ** string is \000 or \u0000 terminated
    //    */
    //#define MEM_Term      0x0200   /* String rep is nul terminated */
    //#define MEM_Dyn       0x0400   /* Need to call sqliteFree() on Mem.z */
    //#define MEM_Static    0x0800   /* Mem.z points to a static string */
    //#define MEM_Ephem     0x1000   /* Mem.z points to an ephemeral string */
    //#define MEM_Agg       0x2000   /* Mem.z points to an agg function context */
    //#define MEM_Zero      0x4000   /* Mem.i contains count of 0s appended to blob */
//#ifdef SQLITE_OMIT_INCRBLOB
//  #undef MEM_Zero
//  #define MEM_Zero 0x0000
//#endif
    const int MEM_Term = 0x0200;   
    const int MEM_Dyn = 0x0400;   
    const int MEM_Static = 0x0800; 
    const int MEM_Ephem = 0x1000;  
    const int MEM_Agg = 0x2000;   
#if !SQLITE_OMIT_INCRBLOB
    const int MEM_Zero = 0x4000;  
#else
    const int MEM_Zero = 0x0000;  
#endif

    /*
    ** Clear any existing type flags from a Mem and replace them with f
    */
    //#define MemSetTypeFlag(p, f) \
    //   ((p)->flags = ((p)->flags&~(MEM_TypeMask|MEM_Zero))|f)
    static void MemSetTypeFlag( Mem p, int f ) { p.flags = (u16)( p.flags & ~( MEM_TypeMask | MEM_Zero ) | f ); }// TODO -- Convert back to inline for speed

#if  SQLITE_OMIT_INCRBLOB
    //#undef MEM_Zero
#endif

    /* A VdbeFunc is just a FuncDef (defined in sqliteInt.h) that contains
** additional information about auxiliary information bound to arguments
** of the function.  This is used to implement the sqlite3_get_auxdata()
** and sqlite3_set_auxdata() APIs.  The "auxdata" is some auxiliary data
** that can be associated with a constant argument to a function.  This
** allows functions such as "regexp" to compile their constant regular
** expression argument once and reused the compiled code for multiple
** invocations.
*/
    public class AuxData
    {
      public string pAux;                     /* Aux data for the i-th argument */
      public dxDel xDelete; //(void *);      /* Destructor for the aux data */
    };
    public class VdbeFunc : FuncDef
    {
      public FuncDef pFunc;                   /* The definition of the function */
      public int nAux;                         /* Number of entries allocated for apAux[] */
      public AuxData[] apAux = new AuxData[2]; /* One slot for each function argument */
    };

    /*
    ** The "context" argument for a installable function.  A pointer to an
    ** instance of this structure is the first argument to the routines used
    ** implement the SQL functions.
    **
    ** There is a typedef for this structure in sqlite.h.  So all routines,
    ** even the public interface to SQLite, can use a pointer to this structure.
    ** But this file is the only place where the internal details of this
    ** structure are known.
    **
    ** This structure is defined inside of vdbeInt.h because it uses substructures
    ** (Mem) which are only defined there.
    */
    public class sqlite3_context
    {
      public FuncDef pFunc;        /* Pointer to function information.  MUST BE FIRST */
      public VdbeFunc pVdbeFunc;   /* Auxilary data, if created. */
      public Mem s = new Mem();    /* The return value is stored here */
      public Mem pMem;             /* Memory cell used to store aggregate context */
      public int isError;          /* Error code returned by the function. */
      public CollSeq pColl;        /* Collating sequence */
    };

    /*
    ** A Set structure is used for quick testing to see if a value
    ** is part of a small set.  Sets are used to implement code like
    ** this:
    **            x.y IN ('hi','hoo','hum')
    */
    //typedef struct Set Set;
    public class Set
    {
      Hash hash;             /* A set is just a hash table */
      HashElem prev;         /* Previously accessed hash elemen */
    };

    /*
    ** A Context stores the last insert rowid, the last statement change count,
    ** and the current statement change count (i.e. changes since last statement).
    ** The current keylist is also stored in the context.
    ** Elements of Context structure type make up the ContextStack, which is
    ** updated by the ContextPush and ContextPop opcodes (used by triggers).
    ** The context is pushed before executing a trigger a popped when the
    ** trigger finishes.
    */
    //typedef struct Context Context;
    public class Context
    {
      public i64 lastRowid;    /* Last insert rowid (sqlite3.lastRowid) */
      public int nChange;      /* Statement changes (Vdbe.nChanges)     */
    };

    /*
    ** An instance of the virtual machine.  This structure contains the complete
    ** state of the virtual machine.
    **
    ** The "sqlite3_stmt" structure pointer that is returned by sqlite3_compile()
    ** is really a pointer to an instance of this structure.
    **
    ** The Vdbe.inVtabMethod variable is set to non-zero for the duration of
    ** any virtual table method invocations made by the vdbe program. It is
    ** set to 2 for xDestroy method calls and 1 for all other methods. This
    ** variable is used for two purposes: to allow xDestroy methods to execute
    ** "DROP TABLE" statements and to prevent some nasty side effects of
    ** malloc failure when SQLite is invoked recursively by a virtual table
    ** method function.
    */
    public class Vdbe
    {
      public sqlite3 db;             /* The database connection that owns this statement */
      public Vdbe pPrev;             /* Linked list of VDBEs with the same Vdbe.db */
      public Vdbe pNext;             /* Linked list of VDBEs with the same Vdbe.db */
      public int nOp;                /* Number of instructions in the program */
      public int nOpAlloc;           /* Number of slots allocated for aOp[] */
      public Op[] aOp;               /* Space to hold the virtual machine's program */
      public int nLabel;             /* Number of labels used */
      public int nLabelAlloc;        /* Number of slots allocated in aLabel[] */
      public int[] aLabel;           /* Space to hold the labels */
      public Mem[] apArg;            /* Arguments to currently executing user function */
      public Mem[] aColName;         /* Column names to return */
      public Mem[] pResultSet;       /* Pointer to an array of results */
      public u16 nResColumn;         /* Number of columns in one row of the result set */
      public u16 nCursor;            /* Number of slots in apCsr[] */
      public VdbeCursor[] apCsr;     /* One element of this array for each open cursor */
      public u8 errorAction;         /* Recovery action to do in case of an error */
      public u8 okVar;               /* True if azVar[] has been initialized */
      public u16 nVar;               /* Number of entries in aVar[] */
      public Mem[] aVar;             /* Values for the OP_Variable opcode. */
      public string[] azVar;         /* Name of variables */
      public u32 magic;              /* Magic number for sanity checking */
      public int nMem;               /* Number of memory locations currently allocated */
      public Mem[] aMem;             /* The memory locations */
      public int cacheCtr;           /* VdbeCursor row cache generation counter */
      public int contextStackTop;    /* Index of top element in the context stack */
      public int contextStackDepth;  /* The size of the "context" stack */
      public Context[] contextStack; /* Stack used by opcodes ContextPush & ContextPop*/
      public int pc;                 /* The program counter */
      public int rc;                 /* Value to return */
      public string zErrMsg;         /* Error message written here */
      public int explain;            /* True if EXPLAIN present on SQL command */
      public bool changeCntOn;       /* True to update the change-counter */
      public bool expired;           /* True if the VM needs to be recompiled */
      public int minWriteFileFormat; /* Minimum file format for writable database files */
      public int inVtabMethod;       /* See comments above */
      public bool usesStmtJournal;   /* True if uses a statement journal */
      public bool readOnly;          /* True for read-only statements */
      public int nChange;            /* Number of db changes made since last reset */
      public bool isPrepareV2;       /* True if prepared with prepare_v2() */
      public int btreeMask;          /* Bitmask of db.aDb[] entries referenced */
      public u64 startTime;          /* Time when query started - used for profiling */
      public BtreeMutexArray aMutex; /* An array of Btree used here and needing locks */
      public int[] aCounter = new int[2]; /* Counters used by sqlite3_stmt_status() */
      public string zSql = "";       /* Text of the SQL statement that generated this */
      public object pFree;           /* Free this when deleting the vdbe */
      public int iStatement;         /* Statement number (or 0 if has not opened stmt) */
#if SQLITE_DEBUG
      public FILE trace;                  /* Write an execution trace here, if not NULL */
#endif

      public Vdbe Copy()
      {
        Vdbe cp = (Vdbe)MemberwiseClone();
        return cp;
      }
      public void CopyTo( Vdbe ct )
      {
        ct.db = db;
        ct.pPrev = pPrev;
        ct.pNext = pNext;
        ct.nOp = nOp;
        ct.nOpAlloc = nOpAlloc;
        ct.aOp = aOp;
        ct.nLabel = nLabel;
        ct.nLabelAlloc = nLabelAlloc;
        ct.aLabel = aLabel;
        ct.apArg = apArg;
        ct.aColName = aColName;
        ct.nCursor = nCursor;
        ct.apCsr = apCsr;
        ct.nVar = nVar;
        ct.aVar = aVar;
        ct.azVar = azVar;
        ct.okVar = okVar;
        ct.magic = magic;
        ct.nMem = nMem;
        ct.aMem = aMem;
        ct.cacheCtr = cacheCtr;
        ct.contextStackTop = contextStackTop;
        ct.contextStackDepth = contextStackDepth;
        ct.contextStack = contextStack;
        ct.pc = pc;
        ct.rc = rc;
        ct.errorAction = errorAction;
        ct.nResColumn = nResColumn;
        ct.zErrMsg = zErrMsg;
        ct.pResultSet = pResultSet;
        ct.explain = explain;
        ct.changeCntOn = changeCntOn;
        ct.expired = expired;
        ct.minWriteFileFormat = minWriteFileFormat;
        ct.inVtabMethod = inVtabMethod;
        ct.usesStmtJournal = usesStmtJournal;
        ct.readOnly = readOnly;
        ct.nChange = nChange;
        ct.isPrepareV2 = isPrepareV2;
        ct.startTime = startTime;
        ct.btreeMask = btreeMask;
        ct.aMutex = aMutex;
        aCounter.CopyTo( ct.aCounter, 0 );
        ct.zSql = zSql;
        ct.pFree = pFree;
#if SQLITE_DEBUG
        ct.trace = trace;
#endif
        ct.iStatement = iStatement;

#if SQLITE_SSE
ct.fetchId=fetchId;
ct.lru=lru;
#endif
#if SQLITE_ENABLE_MEMORY_MANAGEMENT
ct.pLruPrev=pLruPrev;
ct.pLruNext=pLruNext;
#endif
      }
    };

    /*
    ** The following are allowed values for Vdbe.magic
    */
    //#define VDBE_MAGIC_INIT     0x26bceaa5    /* Building a VDBE program */
    //#define VDBE_MAGIC_RUN      0xbdf20da3    /* VDBE is ready to execute */
    //#define VDBE_MAGIC_HALT     0x519c2973    /* VDBE has completed execution */
    //#define VDBE_MAGIC_DEAD     0xb606c3c8    /* The VDBE has been deallocated */
    const u32 VDBE_MAGIC_INIT = 0x26bceaa5;   /* Building a VDBE program */
    const u32 VDBE_MAGIC_RUN = 0xbdf20da3;   /* VDBE is ready to execute */
    const u32 VDBE_MAGIC_HALT = 0x519c2973;   /* VDBE has completed execution */
    const u32 VDBE_MAGIC_DEAD = 0xb606c3c8;   /* The VDBE has been deallocated */
    /*
    ** Function prototypes
    */
    //void sqlite3VdbeFreeCursor(Vdbe *, VdbeCursor*);
    //void sqliteVdbePopStack(Vdbe*,int);
    //int sqlite3VdbeCursorMoveto(VdbeCursor*);
    //#if defined(SQLITE_DEBUG) || defined(VDBE_PROFILE)
    //void sqlite3VdbePrintOp(FILE*, int, Op*);
    //#endif
    //u32 sqlite3VdbeSerialTypeLen(u32);
    //u32 sqlite3VdbeSerialType(Mem*, int);
    //u32sqlite3VdbeSerialPut(unsigned char*, int, Mem*, int);
    //u32 sqlite3VdbeSerialGet(const unsigned char*, u32, Mem*);
    //void sqlite3VdbeDeleteAuxData(VdbeFunc*, int);

    //int sqlite2BtreeKeyCompare(BtCursor *, const void *, int, int, int *);
    //int sqlite3VdbeIdxKeyCompare(VdbeCursor*,UnpackedRecord*,int*);
    //int sqlite3VdbeIdxRowid(sqlite3 *, i64 *);
    //int sqlite3MemCompare(const Mem*, const Mem*, const CollSeq*);
    //int sqlite3VdbeExec(Vdbe*);
    //int sqlite3VdbeList(Vdbe*);
    //int sqlite3VdbeHalt(Vdbe*);
    //int sqlite3VdbeChangeEncoding(Mem *, int);
    //int sqlite3VdbeMemTooBig(Mem*);
    //int sqlite3VdbeMemCopy(Mem*, const Mem*);
    //void sqlite3VdbeMemShallowCopy(Mem*, const Mem*, int);
    //void sqlite3VdbeMemMove(Mem*, Mem*);
    //int sqlite3VdbeMemNulTerminate(Mem*);
    //int sqlite3VdbeMemSetStr(Mem*, const char*, int, u8, void(*)(void*));
    //void sqlite3VdbeMemSetInt64(Mem*, i64);
    //void sqlite3VdbeMemSetDouble(Mem*, double);
    //void sqlite3VdbeMemSetNull(Mem*);
    //void sqlite3VdbeMemSetZeroBlob(Mem*,int);
    //void sqlite3VdbeMemSetRowSet(Mem*);
    //int sqlite3VdbeMemMakeWriteable(Mem*);
    //int sqlite3VdbeMemStringify(Mem*, int);
    //i64 sqlite3VdbeIntValue(Mem*);
    //int sqlite3VdbeMemIntegerify(Mem*);
    //double sqlite3VdbeRealValue(Mem*);
    //void sqlite3VdbeIntegerAffinity(Mem*);
    //int sqlite3VdbeMemRealify(Mem*);
    //int sqlite3VdbeMemNumerify(Mem*);
    //int sqlite3VdbeMemFromBtree(BtCursor*,int,int,int,Mem*);
    //void sqlite3VdbeMemRelease(Mem p);
    //void sqlite3VdbeMemReleaseExternal(Mem p);
    //int sqlite3VdbeMemFinalize(Mem*, FuncDef*);
    //const char *sqlite3OpcodeName(int);
    //int sqlite3VdbeOpcodeHasProperty(int, int);
    //int sqlite3VdbeMemGrow(Mem pMem, int n, int preserve);
    //int sqlite3VdbeCloseStatement(Vdbe *, int);
    //#if SQLITE_ENABLE_MEMORY_MANAGEMENT
    //int sqlite3VdbeReleaseBuffers(Vdbe p);
    //#endif

#if !SQLITE_OMIT_SHARED_CACHE
//void sqlite3VdbeMutexArrayEnter(Vdbe *p);
#else
    //# define sqlite3VdbeMutexArrayEnter(p)
    static void sqlite3VdbeMutexArrayEnter( Vdbe p ) { }
#endif

    //int sqlite3VdbeMemTranslate(Mem*, u8);
    //#if SQLITE_DEBUG
    //  void sqlite3VdbePrintSql(Vdbe*);
    //  void sqlite3VdbeMemPrettyPrint(Mem pMem, char *zBuf);
    //#endif
    //int sqlite3VdbeMemHandleBom(Mem pMem);

#if !SQLITE_OMIT_INCRBLOB
//  int sqlite3VdbeMemExpandBlob(Mem *);
#else
    //  #define sqlite3VdbeMemExpandBlob(x) SQLITE_OK
    static int sqlite3VdbeMemExpandBlob( Mem x ) { return SQLITE_OK; }
#endif

    //#endif /* !_VDBEINT_H_) */
  }
}
