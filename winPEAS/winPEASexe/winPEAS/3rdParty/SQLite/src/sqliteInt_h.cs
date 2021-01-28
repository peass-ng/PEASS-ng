#define SQLITE_MAX_EXPR_DEPTH

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using Bitmask = System.UInt64;
using i16 = System.Int16;
using i64 = System.Int64;
using sqlite3_int64 = System.Int64;

using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;
using unsigned = System.UInt64;

using Pgno = System.UInt32;

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
    ** Internal interface definitions for SQLite.
    **
    ** @(#) $Id: sqliteInt.h,v 1.898 2009/08/10 03:57:58 shane Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#if !_SQLITEINT_H_
    //#define _SQLITEINT_H_

    /*
    ** Include the configuration header output by 'configure' if we're using the
    ** autoconf-based build
    */
#if _HAVE_SQLITE_CONFIG_H
//#include "config.h"
#endif
    //#include "sqliteLimit.h"

    /* Disable nuisance warnings on Borland compilers */
    //#if defined(__BORLANDC__)
    //#pragma warn -rch /* unreachable code */
    //#pragma warn -ccc /* Condition is always true or false */
    //#pragma warn -aus /* Assigned value is never used */
    //#pragma warn -csu /* Comparing signed and unsigned */
    //#pragma warn -spa /* Suspicious pointer arithmetic */
    //#endif

    /* Needed for various definitions... */
    //#if !_GNU_SOURCE
    //#define _GNU_SOURCE
    //#endif
    /*
    ** Include standard header files as necessary
    */
#if HAVE_STDINT_H
//#include <stdint.h>
#endif
#if HAVE_INTTYPES_H
//#include <inttypes.h>
#endif

    /*
** This macro is used to "hide" some ugliness in casting an int
** value to a ptr value under the MSVC 64-bit compiler.   Casting
** non 64-bit values to ptr types results in a "hard" error with
** the MSVC 64-bit compiler which this attempts to avoid.
**
** A simple compiler pragma or casting sequence could not be found
** to correct this in all situations, so this macro was introduced.
**
** It could be argued that the intptr_t type could be used in this
** case, but that type is not available on all compilers, or
** requires the #include of specific headers which differs between
** platforms.
**
** Ticket #3860:  The llvm-gcc-4.2 compiler from Apple chokes on
** the ((void*)&((char*)0)[X]) construct.  But MSVC chokes on ((void*)(X)).
** So we have to define the macros in different ways depending on the
** compiler.
*/
    //#if defined(__GNUC__)
    //# if defined(HAVE_STDINT_H)
    //#   define SQLITE_INT_TO_PTR(X)  ((void*)(intptr_t)(X))
    //#   define SQLITE_PTR_TO_INT(X)  ((int)(intptr_t)(X))
    //# else
    //#   define SQLITE_INT_TO_PTR(X)  ((void*)(X))
    //#   define SQLITE_PTR_TO_INT(X)  ((int)(X))
    //# endif
    //#else
    //# define SQLITE_INT_TO_PTR(X)   ((void*)&((char*)0)[X])
    //# define SQLITE_PTR_TO_INT(X)   ((int)(((char*)X)-(char*)0))
    //#endif

    /*
    ** These #defines should enable >2GB file support on POSIX if the
    ** underlying operating system supports it.  If the OS lacks
    ** large file support, or if the OS is windows, these should be no-ops.
    **
    ** Ticket #2739:  The _LARGEFILE_SOURCE macro must appear before any
    ** system #includes.  Hence, this block of code must be the very first
    ** code in all source files.
    **
    ** Large file support can be disabled using the -DSQLITE_DISABLE_LFS switch
    ** on the compiler command line.  This is necessary if you are compiling
    ** on a recent machine (ex: RedHat 7.2) but you want your code to work
    ** on an older machine (ex: RedHat 6.0).  If you compile on RedHat 7.2
    ** without this option, LFS is enable.  But LFS does not exist in the kernel
    ** in RedHat 6.0, so the code won't work.  Hence, for maximum binary
    ** portability you should omit LFS.
    **
    ** Similar is true for Mac OS X.  LFS is only supported on Mac OS X 9 and later.
    */
#if !SQLITE_DISABLE_LFS
const int _LARGE_FILE = 1;//# define _LARGE_FILE       1
#if !_FILE_OFFSET_BITS
const int _FILE_OFFSET_BITS = 64;//#   define _FILE_OFFSET_BITS 64
# endif
const int _LARGEFILE_SOURCE = 1; //# define _LARGEFILE_SOURCE 1
#endif




    /*
** The SQLITE_THREADSAFE macro must be defined as either 0 or 1.
** Older versions of SQLite used an optional THREADSAFE macro.
** We support that for legacy
*/
#if !SQLITE_THREADSAFE
#if THREADSAFE
//# define SQLITE_THREADSAFE THREADSAFE
#else
    //# define SQLITE_THREADSAFE 1
    const int SQLITE_THREADSAFE = 1;
#endif
#else
const int SQLITE_THREADSAFE = 1;
#endif

    /*
** The SQLITE_DEFAULT_MEMSTATUS macro must be defined as either 0 or 1.
** It determines whether or not the features related to
** SQLITE_CONFIG_MEMSTATUS are available by default or not. This value can
** be overridden at runtime using the sqlite3_config() API.
*/
#if !(SQLITE_DEFAULT_MEMSTATUS)
    //# define SQLITE_DEFAULT_MEMSTATUS 1
    const int SQLITE_DEFAULT_MEMSTATUS = 1;
#endif

    /*
** Exactly one of the following macros must be defined in order to
** specify which memory allocation subsystem to use.
**
**     SQLITE_SYSTEM_MALLOC          // Use normal system malloc()
**     SQLITE_MEMDEBUG               // Debugging version of system malloc()
**     SQLITE_MEMORY_SIZE            // internal allocator #1
**     SQLITE_MMAP_HEAP_SIZE         // internal mmap() allocator
**     SQLITE_POW2_MEMORY_SIZE       // internal power-of-two allocator
**
** If none of the above are defined, then set SQLITE_SYSTEM_MALLOC as
** the default.
*/
    //#if defined(SQLITE_SYSTEM_MALLOC)+defined(SQLITE_MEMDEBUG)+\
    //    defined(SQLITE_MEMORY_SIZE)+defined(SQLITE_MMAP_HEAP_SIZE)+\
    //    defined(SQLITE_POW2_MEMORY_SIZE)>1
    //# error "At most one of the following compile-time configuration options\
    // is allows: SQLITE_SYSTEM_MALLOC, SQLITE_MEMDEBUG, SQLITE_MEMORY_SIZE,\
    // SQLITE_MMAP_HEAP_SIZE, SQLITE_POW2_MEMORY_SIZE"
    //#endif
    //#if defined(SQLITE_SYSTEM_MALLOC)+defined(SQLITE_MEMDEBUG)+\
    //    defined(SQLITE_MEMORY_SIZE)+defined(SQLITE_MMAP_HEAP_SIZE)+\
    //    defined(SQLITE_POW2_MEMORY_SIZE)==0
    //# define SQLITE_SYSTEM_MALLOC 1
    //#endif

    /*
    ** If SQLITE_MALLOC_SOFT_LIMIT is not zero, then try to keep the
    ** sizes of memory allocations below this value where possible.
    */
#if !(SQLITE_MALLOC_SOFT_LIMIT)
    const int SQLITE_MALLOC_SOFT_LIMIT = 1024;
#endif

    /*
** We need to define _XOPEN_SOURCE as follows in order to enable
** recursive mutexes on most Unix systems.  But Mac OS X is different.
** The _XOPEN_SOURCE define causes problems for Mac OS X we are told,
** so it is omitted there.  See ticket #2673.
**
** Later we learn that _XOPEN_SOURCE is poorly or incorrectly
** implemented on some systems.  So we avoid defining it at all
** if it is already defined or if it is unneeded because we are
** not doing a threadsafe build.  Ticket #2681.
**
** See also ticket #2741.
*/
#if !_XOPEN_SOURCE && !__DARWIN__ && !__APPLE__ && SQLITE_THREADSAFE
const int _XOPEN_SOURCE = 500;//#define _XOPEN_SOURCE 500  /* Needed to enable pthread recursive mutexes */
#endif

    /*
** The TCL headers are only needed when compiling the TCL bindings.
*/
#if SQLITE_TCL || TCLSH
//# include <tcl.h>
#endif

    /*
** Many people are failing to set -DNDEBUG=1 when compiling SQLite.
** Setting NDEBUG makes the code smaller and run faster.  So the following
** lines are added to automatically set NDEBUG unless the -DSQLITE_DEBUG=1
** option is set.  Thus NDEBUG becomes an opt-in rather than an opt-out
** feature.
*/
#if !NDEBUG && !SQLITE_DEBUG
const int NDEBUG = 1;//# define NDEBUG 1
#endif

    /*
** The testcase() macro is used to aid in coverage testing.  When
** doing coverage testing, the condition inside the argument to
** testcase() must be evaluated both true and false in order to
** get full branch coverage.  The testcase() macro is inserted
** to help ensure adequate test coverage in places where simple
** condition/decision coverage is inadequate.  For example, testcase()
** can be used to make sure boundary values are tested.  For
** bitmask tests, testcase() can be used to make sure each bit
** is significant and used at least once.  On switch statements
** where multiple cases go to the same block of code, testcase()
** can insure that all cases are evaluated.
**
*/
#if SQLITE_COVERAGE_TEST
void sqlite3Coverage(int);
//# define testcase(X)  if( X ){ sqlite3Coverage(__LINE__); }
#else
    //# define testcase(X)
    static void testcase<T>( T X ) { }
#endif

    /*
** The TESTONLY macro is used to enclose variable declarations or
** other bits of code that are needed to support the arguments
** within testcase() and assert() macros.
*/
#if !NDEBUG || SQLITE_COVERAGE_TEST
    //# define TESTONLY(X)  X
    // -- Need workaround for C#, since inline macros don't exist
#else
//# define TESTONLY(X)
#endif

    /*
** Sometimes we need a small amount of code such as a variable initialization
** to setup for a later assert() statement.  We do not want this code to
** appear when assert() is disabled.  The following macro is therefore
** used to contain that setup code.  The "VVA" acronym stands for
** "Verification, Validation, and Accreditation".  In other words, the
** code within VVA_ONLY() will only run during verification processes.
*/
#if !NDEBUG
    //# define VVA_ONLY(X)  X
#else
//# define VVA_ONLY(X)
#endif

    /*
** The ALWAYS and NEVER macros surround boolean expressions which
** are intended to always be true or false, respectively.  Such
** expressions could be omitted from the code completely.  But they
** are included in a few cases in order to enhance the resilience
** of SQLite to unexpected behavior - to make the code "self-healing"
** or "ductile" rather than being "brittle" and crashing at the first
** hint of unplanned behavior.
**
** In other words, ALWAYS and NEVER are added for defensive code.
**
** When doing coverage testing ALWAYS and NEVER are hard-coded to
** be true and false so that the unreachable code then specify will
** not be counted as untested code.
*/
#if SQLITE_COVERAGE_TEST
//# define ALWAYS(X)      (1)
//# define NEVER(X)       (0)
#elif !NDEBUG
    //# define ALWAYS(X)      ((X)?1:(assert(0),0))
    static bool ALWAYS( bool X ) { if ( X != true ) Debug.Assert( false ); return true; }
    static int ALWAYS( int X ) { if ( X == 0 ) Debug.Assert( false ); return 1; }
    static bool ALWAYS<T>( T X ) { if ( X == null ) Debug.Assert( false ); return true; }

    //# define NEVER(X)       ((X)?(assert(0),1):0)
    static bool NEVER( bool X ) { if ( X == true ) Debug.Assert( false ); return false; }
    static byte NEVER( byte X ) { if ( X != 0 ) Debug.Assert( false ); return 0; }
    static int NEVER( int X ) { if ( X != 0 ) Debug.Assert( false ); return 0; }
    static bool NEVER<T>( T X ) { if ( X != null ) Debug.Assert( false ); return false; }
#else
//# define ALWAYS(X)      (X)
    static bool ALWAYS(bool X) { return X; }
    static byte ALWAYS(byte X) { return X; }
    static int ALWAYS(int X) { return X; }
static bool ALWAYS<T>( T X ) { return true; }

//# define NEVER(X)       (X)
static bool NEVER(bool X) { return X; }
static byte NEVER(byte X) { return X; }
static int NEVER(int X) { return X; }
static bool NEVER<T>(T X) { return false; }
#endif

    /*
** The macro unlikely() is a hint that surrounds a boolean
** expression that is usually false.  Macro likely() surrounds
** a boolean expression that is usually true.  GCC is able to
** use these hints to generate better code, sometimes.
*/
#if (__GNUC__) && FALSE
//# define likely(X)    __builtin_expect((X),1)
//# define unlikely(X)  __builtin_expect((X),0)
#else
    //# define likely(X)    !!(X)
    static bool likely( bool X ) { return !!X; }
    //# define unlikely(X)  !!(X)
    static bool unlikely( bool X ) { return !!X; }
#endif

    //#include "sqlite3.h"
    //#include "hash.h"
    //#include "parse.h"
    //#include <stdio.h>
    //#include <stdlib.h>
    //#include <string.h>
    //#include <assert.h>
    //#include <stddef.h>

    /*
    ** If compiling for a processor that lacks floating point support,
    ** substitute integer for floating-point
    */
#if SQLITE_OMIT_FLOATING_POINT
//# define double sqlite_int64
//# define LONGDOUBLE_TYPE sqlite_int64
//#if !SQLITE_BIG_DBL
//#   define SQLITE_BIG_DBL (((sqlite3_int64)1)<<60)
//# endif
//# define SQLITE_OMIT_DATETIME_FUNCS 1
//# define SQLITE_OMIT_TRACE 1
//# undef SQLITE_MIXED_ENDIAN_64BIT_FLOAT
//# undef SQLITE_HAVE_ISNAN
#endif
#if !SQLITE_BIG_DBL
    const double SQLITE_BIG_DBL = ( ( (sqlite3_int64)1 ) << 60 );//# define SQLITE_BIG_DBL (1e99)
#endif

    /*
** OMIT_TEMPDB is set to 1 if SQLITE_OMIT_TEMPDB is defined, or 0
** afterward. Having this macro allows us to cause the C compiler
** to omit code used by TEMP tables without messy #if !statements.
*/
#if SQLITE_OMIT_TEMPDB
//#define OMIT_TEMPDB 1
#else
    static int OMIT_TEMPDB = 0;
#endif

    /*
** If the following macro is set to 1, then NULL values are considered
** distinct when determining whether or not two entries are the same
** in a UNIQUE index.  This is the way PostgreSQL, Oracle, DB2, MySQL,
** OCELOT, and Firebird all work.  The SQL92 spec explicitly says this
** is the way things are suppose to work.
**
** If the following macro is set to 0, the NULLs are indistinct for
** a UNIQUE index.  In this mode, you can only have a single NULL entry
** for a column declared UNIQUE.  This is the way Informix and SQL Server
** work.
*/
    const int NULL_DISTINCT_FOR_UNIQUE = 1;

    /*
    ** The "file format" number is an integer that is incremented whenever
    ** the VDBE-level file format changes.  The following macros define the
    ** the default file format for new databases and the maximum file format
    ** that the library can read.
    */
    public static int SQLITE_MAX_FILE_FORMAT = 4;//#define SQLITE_MAX_FILE_FORMAT 4
#if !SQLITE_DEFAULT_FILE_FORMAT
    static int SQLITE_DEFAULT_FILE_FORMAT = 1;//# define SQLITE_DEFAULT_FILE_FORMAT 1
#endif

    /*
** Provide a default value for SQLITE_TEMP_STORE in case it is not specified
** on the command-line
*/
#if !SQLITE_TEMP_STORE
    static int SQLITE_TEMP_STORE = 1;//#define SQLITE_TEMP_STORE 1
#endif

    /*
** GCC does not define the offsetof() macro so we'll have to do it
** ourselves.
*/
#if !offsetof
    //#define offsetof(STRUCTURE,FIELD) ((int)((char*)&((STRUCTURE*)0)->FIELD))
#endif

    /*
** Check to see if this machine uses EBCDIC.  (Yes, believe it or
** not, there are still machines out there that use EBCDIC.)
*/
#if FALSE //'A' == '\301'
//# define SQLITE_EBCDIC 1
#else
    const int SQLITE_ASCII = 1;//#define SQLITE_ASCII 1
#endif

    /*
** Integers of known sizes.  These typedefs might change for architectures
** where the sizes very.  Preprocessor macros are available so that the
** types can be conveniently redefined at compile-type.  Like this:
**
**         cc '-Du32PTR_TYPE=long long int' ...
*/
    //#if !u32_TYPE
    //# ifdef HAVE_u32_T
    //#  define u32_TYPE u32_t
    //# else
    //#  define u32_TYPE unsigned int
    //# endif
    //#endif
    //#if !u3216_TYPE
    //# ifdef HAVE_u3216_T
    //#  define u3216_TYPE u3216_t
    //# else
    //#  define u3216_TYPE unsigned short int
    //# endif
    //#endif
    //#if !INT16_TYPE
    //# ifdef HAVE_INT16_T
    //#  define INT16_TYPE int16_t
    //# else
    //#  define INT16_TYPE short int
    //# endif
    //#endif
    //#if !u328_TYPE
    //# ifdef HAVE_u328_T
    //#  define u328_TYPE u328_t
    //# else
    //#  define u328_TYPE unsigned char
    //# endif
    //#endif
    //#if !INT8_TYPE
    //# ifdef HAVE_INT8_T
    //#  define INT8_TYPE int8_t
    //# else
    //#  define INT8_TYPE signed char
    //# endif
    //#endif
    //#if !LONGDOUBLE_TYPE
    //# define LONGDOUBLE_TYPE long double
    //#endif
    //typedef sqlite_int64 i64;          /* 8-byte signed integer */
    //typedef sqlite_u3264 u64;         /* 8-byte unsigned integer */
    //typedef u32_TYPE u32;           /* 4-byte unsigned integer */
    //typedef u3216_TYPE u16;           /* 2-byte unsigned integer */
    //typedef INT16_TYPE i16;            /* 2-byte signed integer */
    //typedef u328_TYPE u8;             /* 1-byte unsigned integer */
    //typedef INT8_TYPE i8;              /* 1-byte signed integer */

    /*
    ** SQLITE_MAX_U32 is a u64 constant that is the maximum u64 value
    ** that can be stored in a u32 without loss of data.  The value
    ** is 0x00000000ffffffff.  But because of quirks of some compilers, we
    ** have to specify the value in the less intuitive manner shown:
    */
    //#define SQLITE_MAX_U32  ((((u64)1)<<32)-1)
    const u32 SQLITE_MAX_U32 = (u32)( ( ( (u64)1 ) << 32 ) - 1 );


    /*
    ** Macros to determine whether the machine is big or little endian,
    ** evaluated at runtime.
    */
#if SQLITE_AMALGAMATION
//const int sqlite3one = 1;
#else
    const bool sqlite3one = true;
#endif
#if i386 || __i386__ || _M_IX86
const int ;//#define SQLITE_BIGENDIAN    0
const int ;//#define SQLITE_LITTLEENDIAN 1
const int ;//#define SQLITE_UTF16NATIVE  SQLITE_UTF16LE
#else
    static u8 SQLITE_BIGENDIAN = 0;//#define SQLITE_BIGENDIAN    (*(char *)(&sqlite3one)==0)
    static u8 SQLITE_LITTLEENDIAN = 1;//#define SQLITE_LITTLEENDIAN (*(char *)(&sqlite3one)==1)
    static u8 SQLITE_UTF16NATIVE = ( SQLITE_BIGENDIAN != 0 ? SQLITE_UTF16BE : SQLITE_UTF16LE );//#define SQLITE_UTF16NATIVE (SQLITE_BIGENDIAN?SQLITE_UTF16BE:SQLITE_UTF16LE)
#endif

    /*
** Constants for the largest and smallest possible 64-bit signed integers.
** These macros are designed to work correctly on both 32-bit and 64-bit
** compilers.
*/
    //#define LARGEST_INT64  (0xffffffff|(((i64)0x7fffffff)<<32))
    //#define SMALLEST_INT64 (((i64)-1) - LARGEST_INT64)
    const i64 LARGEST_INT64 = i64.MaxValue;//( 0xffffffff | ( ( (i64)0x7fffffff ) << 32 ) );
    const i64 SMALLEST_INT64 = i64.MinValue;//( ( ( i64 ) - 1 ) - LARGEST_INT64 );

    /*
    ** Round up a number to the next larger multiple of 8.  This is used
    ** to force 8-byte alignment on 64-bit architectures.
    */
    //#define ROUND8(x)     (((x)+7)&~7)
    static int ROUND8( int x ) { return ( x + 7 ) & ~7; }

    /*
    ** Round down to the nearest multiple of 8
    */
    //#define ROUNDDOWN8(x) ((x)&~7)
    static int ROUNDDOWN8( int x ) { return x & ~7; }

    /*
    ** Assert that the pointer X is aligned to an 8-byte boundary.
    */
    //#define EIGHT_BYTE_ALIGNMENT(X)   ((((char*)(X) - (char*)0)&7)==0)

    /*
    ** An instance of the following structure is used to store the busy-handler
    ** callback for a given sqlite handle.
    **
    ** The sqlite.busyHandler member of the sqlite struct contains the busy
    ** callback for the database handle. Each pager opened via the sqlite
    ** handle is passed a pointer to sqlite.busyHandler. The busy-handler
    ** callback is currently invoked only from within pager.c.
    */
    //typedef struct BusyHandler BusyHandler;
    public class BusyHandler
    {
      public dxBusy xFunc;//)(void *,int);  /* The busy callback */
      public object pArg;                   /* First arg to busy callback */
      public int nBusy;                     /* Incremented with each busy call */
    };

    /*
    ** Name of the master database table.  The master database table
    ** is a special table that holds the names and attributes of all
    ** user tables and indices.
    */
    const string MASTER_NAME = "sqlite_master";//#define MASTER_NAME       "sqlite_master"
    const string TEMP_MASTER_NAME = "sqlite_temp_master";//#define TEMP_MASTER_NAME  "sqlite_temp_master"

    /*
    ** The root-page of the master database table.
    */
    const int MASTER_ROOT = 1;//#define MASTER_ROOT       1

    /*
    ** The name of the schema table.
    */
    static string SCHEMA_TABLE( int x ) //#define SCHEMA_TABLE(x)  ((!OMIT_TEMPDB)&&(x==1)?TEMP_MASTER_NAME:MASTER_NAME)
    { return ( ( OMIT_TEMPDB == 0 ) && ( x == 1 ) ? TEMP_MASTER_NAME : MASTER_NAME ); }

    /*
    ** A convenience macro that returns the number of elements in
    ** an array.
    */
    //#define ArraySize(X)    ((int)(sizeof(X)/sizeof(X[0])))
    static int ArraySize<T>( T[] x ) { return x.Length; }

    /*
    ** The following value as a destructor means to use //sqlite3DbFree().
    ** This is an internal extension to SQLITE_STATIC and SQLITE_TRANSIENT.
    */
    //#define SQLITE_DYNAMIC   ((sqlite3_destructor_type)//sqlite3DbFree)
    static dxDel SQLITE_DYNAMIC;

    /*
    ** When SQLITE_OMIT_WSD is defined, it means that the target platform does
    ** not support Writable Static Data (WSD) such as global and static variables.
    ** All variables must either be on the stack or dynamically allocated from
    ** the heap.  When WSD is unsupported, the variable declarations scattered
    ** throughout the SQLite code must become constants instead.  The SQLITE_WSD
    ** macro is used for this purpose.  And instead of referencing the variable
    ** directly, we use its constant as a key to lookup the run-time allocated
    ** buffer that holds real variable.  The constant is also the initializer
    ** for the run-time allocated buffer.
    **
    ** In the usual case where WSD is supported, the SQLITE_WSD and GLOBAL
    ** macros become no-ops and have zero performance impact.
    */
#if SQLITE_OMIT_WSD
//#define SQLITE_WSD const
//#define GLOBAL(t,v) (*(t*)sqlite3_wsd_find((void*)&(v), sizeof(v)))
//#define sqlite3GlobalConfig GLOBAL(struct Sqlite3Config, sqlite3Config)
int sqlite3_wsd_init(int N, int J);
void *sqlite3_wsd_find(void *K, int L);
#else
    //#define SQLITE_WSD
    //#define GLOBAL(t,v) v
    //#define sqlite3GlobalConfig sqlite3Config
    static Sqlite3Config sqlite3GlobalConfig;
#endif

    /*
** The following macros are used to suppress compiler warnings and to
** make it clear to human readers when a function parameter is deliberately
** left unused within the body of a function. This usually happens when
** a function is called via a function pointer. For example the
** implementation of an SQL aggregate step callback may not use the
** parameter indicating the number of arguments passed to the aggregate,
** if it knows that this is enforced elsewhere.
**
** When a function parameter is not used at all within the body of a function,
** it is generally named "NotUsed" or "NotUsed2" to make things even clearer.
** However, these macros may also be used to suppress warnings related to
** parameters that may or may not be used depending on compilation options.
** For example those parameters only used in assert() statements. In these
** cases the parameters are named as per the usual conventions.
*/
    //#define UNUSED_PARAMETER(x) (void)(x)
    static void UNUSED_PARAMETER<T>( T x ) { }

    //#define UNUSED_PARAMETER2(x,y) UNUSED_PARAMETER(x),UNUSED_PARAMETER(y)
    static void UNUSED_PARAMETER2<T1, T2>( T1 x, T2 y ) { UNUSED_PARAMETER( x ); UNUSED_PARAMETER( y ); }

    /*
    ** Forward references to structures
    */
    //typedef struct AggInfo AggInfo;
    //typedef struct AuthContext AuthContext;
    //typedef struct AutoincInfo AutoincInfo;
    //typedef struct Bitvec Bitvec;
    //typedef struct RowSet RowSet;
    //typedef struct CollSeq CollSeq;
    //typedef struct Column Column;
    //typedef struct Db Db;
    //typedef struct Schema Schema;
    //typedef struct Expr Expr;
    //typedef struct ExprList ExprList;
    //typedef struct ExprSpan ExprSpan;
    //typedef struct FKey FKey;
    //typedef struct FuncDef FuncDef;
    //typedef struct IdList IdList;
    //typedef struct Index Index;
    //typedef struct KeyClass KeyClass;
    //typedef struct KeyInfo KeyInfo;
    //typedef struct Lookaside Lookaside;
    //typedef struct LookasideSlot LookasideSlot;
    //typedef struct Module Module;
    //typedef struct NameContext NameContext;
    //typedef struct Parse Parse;
    //typedef struct Savepoint Savepoint;
    //typedef struct Select Select;
    //typedef struct SrcList SrcList;
    //typedef struct StrAccum StrAccum;
    //typedef struct Table Table;
    //typedef struct TableLock TableLock;
    //typedef struct Token Token;
    //typedef struct TriggerStack TriggerStack;
    //typedef struct TriggerStep TriggerStep;
    //typedef struct Trigger Trigger;
    //typedef struct UnpackedRecord UnpackedRecord;
    //typedef struct VTable VTable;
    //typedef struct Walker Walker;
    //typedef struct WherePlan WherePlan;
    //typedef struct WhereInfo WhereInfo;
    //typedef struct WhereLevel WhereLevel;

    /*
    ** Defer sourcing vdbe.h and btree.h until after the "u8" and
    ** "BusyHandler" typedefs. vdbe.h also requires a few of the opaque
    ** pointer types (i.e. FuncDef) defined above.
    */
    //#include "btree.h"
    //#include "vdbe.h"
    //#include "pager.h"
    //#include "pcache_g.h"

    //#include "os.h"
    //#include "mutex.h"

    /*
    ** Each database file to be accessed by the system is an instance
    ** of the following structure.  There are normally two of these structures
    ** in the sqlite.aDb[] array.  aDb[0] is the main database file and
    ** aDb[1] is the database file used to hold temporary tables.  Additional
    ** databases may be attached.
    */
    public class Db
    {
      public string zName;                  /*  Name of this database  */
      public Btree pBt;                     /*  The B Tree structure for this database file  */
      public u8 inTrans;                    /*  0: not writable.  1: Transaction.  2: Checkpoint  */
      public u8 safety_level;               /*  How aggressive at syncing data to disk  */
      public Schema pSchema;                /* Pointer to database schema (possibly shared)  */
    };

    /*
    ** An instance of the following structure stores a database schema.
    **
    ** If there are no virtual tables configured in this schema, the
    ** Schema.db variable is set to NULL. After the first virtual table
    ** has been added, it is set to point to the database connection
    ** used to create the connection. Once a virtual table has been
    ** added to the Schema structure and the Schema.db variable populated,
    ** only that database connection may use the Schema to prepare
    ** statements.
    */
    public class Schema
    {
      public int schema_cookie;         /* Database schema version number for this file */
      public Hash tblHash = new Hash(); /* All tables indexed by name */
      public Hash idxHash = new Hash(); /* All (named) indices indexed by name */
      public Hash trigHash = new Hash();/* All triggers indexed by name */
      public Table pSeqTab;             /* The sqlite_sequence table used by AUTOINCREMENT */
      public u8 file_format;           /* Schema format version for this file */
      public u8 enc;                   /* Text encoding used by this database */
      public u16 flags;                 /* Flags associated with this schema */
      public int cache_size;            /* Number of pages to use in the cache */
#if !SQLITE_OMIT_VIRTUALTABLE
public   sqlite3 db;                    /* "Owner" connection. See comment above */
#endif
      public Schema Copy()
      {
        if ( this == null )
          return null;
        else
        {
          Schema cp = (Schema)MemberwiseClone();
          return cp;
        }
      }
    };

    /*
    ** These macros can be used to test, set, or clear bits in the
    ** Db.flags field.
    */
    //#define DbHasProperty(D,I,P)     (((D)->aDb[I].pSchema->flags&(P))==(P))
    static bool DbHasProperty( sqlite3 D, int I, ushort P ) { return ( D.aDb[I].pSchema.flags & P ) == P; }
    //#define DbHasAnyProperty(D,I,P)  (((D)->aDb[I].pSchema->flags&(P))!=0)
    //#define DbSetProperty(D,I,P)     (D)->aDb[I].pSchema->flags|=(P)
    static void DbSetProperty( sqlite3 D, int I, ushort P ) { D.aDb[I].pSchema.flags = (u16)( D.aDb[I].pSchema.flags | P ); }
    //#define DbClearProperty(D,I,P)   (D)->aDb[I].pSchema->flags&=~(P)
    static void DbClearProperty( sqlite3 D, int I, ushort P ) { D.aDb[I].pSchema.flags = (u16)( D.aDb[I].pSchema.flags & ~P ); }
    /*
    ** Allowed values for the DB.flags field.
    **
    ** The DB_SchemaLoaded flag is set after the database schema has been
    ** read into internal hash tables.
    **
    ** DB_UnresetViews means that one or more views have column names that
    ** have been filled out.  If the schema changes, these column names might
    ** changes and so the view will need to be reset.
    */
    //#define DB_SchemaLoaded    0x0001  /* The schema has been loaded */
    //#define DB_UnresetViews    0x0002  /* Some views have defined column names */
    //#define DB_Empty           0x0004  /* The file is empty (length 0 bytes) */
    const u16 DB_SchemaLoaded = 0x0001;
    const u16 DB_UnresetViews = 0x0002;
    const u16 DB_Empty = 0x0004;

    /*
    ** The number of different kinds of things that can be limited
    ** using the sqlite3_limit() interface.
    */
    //#define SQLITE_N_LIMIT (SQLITE_LIMIT_VARIABLE_NUMBER+1)
    const int SQLITE_N_LIMIT = SQLITE_LIMIT_VARIABLE_NUMBER + 1;

    /*
    ** Lookaside malloc is a set of fixed-size buffers that can be used
    ** to satisfy small transient memory allocation requests for objects
    ** associated with a particular database connection.  The use of
    ** lookaside malloc provides a significant performance enhancement
    ** (approx 10%) by avoiding numerous malloc/free requests while parsing
    ** SQL statements.
    **
    ** The Lookaside structure holds configuration information about the
    ** lookaside malloc subsystem.  Each available memory allocation in
    ** the lookaside subsystem is stored on a linked list of LookasideSlot
    ** objects.
    **
    ** Lookaside allocations are only allowed for objects that are associated
    ** with a particular database connection.  Hence, schema information cannot
    ** be stored in lookaside because in shared cache mode the schema information
    ** is shared by multiple database connections.  Therefore, while parsing
    ** schema information, the Lookaside.bEnabled flag is cleared so that
    ** lookaside allocations are not used to construct the schema objects.
    */
    public class Lookaside
    {
      public int sz;               /* Size of each buffer in bytes */
      public u8 bEnabled;        /* False to disable new lookaside allocations */
      public bool bMalloced;       /* True if pStart obtained from sqlite3_malloc() */
      public int nOut;             /* Number of buffers currently checked out */
      public int mxOut;            /* Highwater mark for nOut */
      public LookasideSlot pFree;  /* List of available buffers */
      public int pStart;           /* First byte of available memory space */
      public int pEnd;             /* First byte past end of available space */
    };
    public class LookasideSlot
    {
      public LookasideSlot pNext;    /* Next buffer in the list of free buffers */
    };

    /*
    ** A hash table for function definitions.
    **
    ** Hash each FuncDef structure into one of the FuncDefHash.a[] slots.
    ** Collisions are on the FuncDef.pHash chain.
    */
    public class FuncDefHash
    {
      public FuncDef[] a = new FuncDef[23];       /* Hash table for functions */
    };

    /*
    ** Each database is an instance of the following structure.
    **
    ** The sqlite.lastRowid records the last insert rowid generated by an
    ** insert statement.  Inserts on views do not affect its value.  Each
    ** trigger has its own context, so that lastRowid can be updated inside
    ** triggers as usual.  The previous value will be restored once the trigger
    ** exits.  Upon entering a before or instead of trigger, lastRowid is no
    ** longer (since after version 2.8.12) reset to -1.
    **
    ** The sqlite.nChange does not count changes within triggers and keeps no
    ** context.  It is reset at start of sqlite3_exec.
    ** The sqlite.lsChange represents the number of changes made by the last
    ** insert, update, or delete statement.  It remains constant throughout the
    ** length of a statement and is then updated by OP_SetCounts.  It keeps a
    ** context stack just like lastRowid so that the count of changes
    ** within a trigger is not seen outside the trigger.  Changes to views do not
    ** affect the value of lsChange.
    ** The sqlite.csChange keeps track of the number of current changes (since
    ** the last statement) and is used to update sqlite_lsChange.
    **
    ** The member variables sqlite.errCode, sqlite.zErrMsg and sqlite.zErrMsg16
    ** store the most recent error code and, if applicable, string. The
    ** internal function sqlite3Error() is used to set these variables
    ** consistently.
    */
    public class sqlite3
    {
      public sqlite3_vfs pVfs;             /* OS Interface */
      public int nDb;                      /* Number of backends currently in use */
      public Db[] aDb = new Db[SQLITE_MAX_ATTACHED];         /* All backends */
      public int flags;                    /* Miscellaneous flags. See below */
      public int openFlags;                /* Flags passed to sqlite3_vfs.xOpen() */
      public int errCode;                  /* Most recent error code (SQLITE_*) */
      public int errMask;                  /* & result codes with this before returning */
      public u8 autoCommit;                /* The auto-commit flag. */
      public u8 temp_store;                /* 1: file 2: memory 0: default */
      // Cannot happen under C#
      //      public u8 mallocFailed;              /* True if we have seen a malloc failure */
      public u8 dfltLockMode;              /* Default locking-mode for attached dbs */
      public u8 dfltJournalMode;           /* Default journal mode for attached dbs */
      public int nextAutovac;              /* Autovac setting after VACUUM if >=0 */
      public int nextPagesize;             /* Pagesize after VACUUM if >0 */
      public int nTable;                   /* Number of tables in the database */
      public CollSeq pDfltColl;            /* The default collating sequence (BINARY) */
      public i64 lastRowid;                /* ROWID of most recent insert (see above) */
      public u32 magic;                    /* Magic number for detect library misuse */
      public int nChange;                  /* Value returned by sqlite3_changes() */
      public int nTotalChange;             /* Value returned by sqlite3_total_changes() */
      public sqlite3_mutex mutex;          /* Connection mutex */
      public int[] aLimit = new int[SQLITE_N_LIMIT];   /* Limits */
      public class sqlite3InitInfo
      {      /* Information used during initialization */
        public int iDb;                    /* When back is being initialized */
        public int newTnum;                /* Rootpage of table being initialized */
        public u8 busy;                    /* TRUE if currently initializing */
        public u8 orphanTrigger;           /* Last statement is orphaned TEMP trigger */
      };
      public sqlite3InitInfo init = new sqlite3InitInfo();
      public int nExtension;               /* Number of loaded extensions */
      public object[] aExtension;          /* Array of shared library handles */
      public Vdbe pVdbe;                   /* List of active virtual machines */
      public int activeVdbeCnt;            /* Number of VDBEs currently executing */
      public int writeVdbeCnt;             /* Number of active VDBEs that are writing */
      public dxTrace xTrace;//)(void*,const char*);        /* Trace function */
      public object pTraceArg;                          /* Argument to the trace function */
      public dxProfile xProfile;//)(void*,const char*,u64);  /* Profiling function */
      public object pProfileArg;                        /* Argument to profile function */
      public object pCommitArg;                 /* Argument to xCommitCallback() */
      public dxCommitCallback xCommitCallback;//)(void*);    /* Invoked at every commit. */
      public object pRollbackArg;               /* Argument to xRollbackCallback() */
      public dxRollbackCallback xRollbackCallback;//)(void*); /* Invoked at every commit. */
      public object pUpdateArg;
      public dxUpdateCallback xUpdateCallback;//)(void*,int, const char*,const char*,sqlite_int64);
      public dxCollNeeded xCollNeeded;//)(void*,sqlite3*,int eTextRep,const char*);
      public dxCollNeeded xCollNeeded16;//)(void*,sqlite3*,int eTextRep,const void*);
      public object pCollNeededArg;
      public sqlite3_value pErr;            /* Most recent error message */
      public string zErrMsg;                /* Most recent error message (UTF-8 encoded) */
      public string zErrMsg16;              /* Most recent error message (UTF-16 encoded) */
      public struct _u1
      {
        public bool isInterrupted;          /* True if sqlite3_interrupt has been called */
        public double notUsed1;            /* Spacer */
      }
      public _u1 u1;
      public Lookaside lookaside = new Lookaside();          /* Lookaside malloc configuration */
#if !SQLITE_OMIT_AUTHORIZATION
public dxAuth xAuth;//)(void*,int,const char*,const char*,const char*,const char*);
/* Access authorization function */
public object pAuthArg;               /* 1st argument to the access auth function */
#endif
#if !SQLITE_OMIT_PROGRESS_CALLBACK
      public dxProgress xProgress;//)(void *);  /* The progress callback */
      public object pProgressArg;               /* Argument to the progress callback */
      public int nProgressOps;                  /* Number of opcodes for progress callback */
#endif
#if !SQLITE_OMIT_VIRTUALTABLE
      public Hash aModule;                  /* populated by sqlite3_create_module() */
      public Table pVTab;                   /* vtab with active Connect/Create method */
      public VTable aVTrans;                /* Virtual tables with open transactions */
      public int nVTrans;                   /* Allocated size of aVTrans */
      public VTable pDisconnect;            /* Disconnect these in next sqlite3_prepare() */
#endif
      public FuncDefHash aFunc = new FuncDefHash();       /* Hash table of connection functions */
      public Hash aCollSeq = new Hash();                  /* All collating sequences */
      public BusyHandler busyHandler = new BusyHandler(); /* Busy callback */
      public int busyTimeout;                             /* Busy handler timeout, in msec */
      public Db[] aDbStatic = new Db[] { new Db(), new Db() };              /* Static space for the 2 default backends */
      public Savepoint pSavepoint;         /* List of active savepoints */
      public int nSavepoint;               /* Number of non-transaction savepoints */
      public int nStatement;               /* Number of nested statement-transactions  */
      public u8 isTransactionSavepoint;    /* True if the outermost savepoint is a TS */
#if SQLITE_ENABLE_UNLOCK_NOTIFY
/* The following variables are all protected by the STATIC_MASTER
** mutex, not by sqlite3.mutex. They are used by code in notify.c.
**
** When X.pUnlockConnection==Y, that means that X is waiting for Y to
** unlock so that it can proceed.
**
** When X.pBlockingConnection==Y, that means that something that X tried
** tried to do recently failed with an SQLITE_LOCKED error due to locks
** held by Y.
*/
sqlite3 *pBlockingConnection; /* Connection that caused SQLITE_LOCKED */
sqlite3 *pUnlockConnection;           /* Connection to watch for unlock */
void *pUnlockArg;                     /* Argument to xUnlockNotify */
void (*xUnlockNotify)(void **, int);  /* Unlock notify callback */
sqlite3 *pNextBlocked;        /* Next in list of all blocked connections */
#endif
    };

    /*
    ** A macro to discover the encoding of a database.
    */
    //#define ENC(db) ((db)->aDb[0].pSchema->enc)
    static u8 ENC( sqlite3 db ) { return db.aDb[0].pSchema.enc; }

    /*
    ** Possible values for the sqlite.flags and or Db.flags fields.
    **
    ** On sqlite.flags, the SQLITE_InTrans value means that we have
    ** executed a BEGIN.  On Db.flags, SQLITE_InTrans means a statement
    ** transaction is active on that particular database file.
    */
    const int SQLITE_VdbeTrace = 0x00000001;//#define SQLITE_VdbeTrace      0x00000001  /* True to trace VDBE execution */
    const int SQLITE_InTrans = 0x00000008;//#define SQLITE_InTrans        0x00000008  /* True if in a transaction */
    const int SQLITE_InternChanges = 0x00000010;//#define SQLITE_InternChanges  0x00000010  /* Uncommitted Hash table changes */
    const int SQLITE_FullColNames = 0x00000020;//#define SQLITE_FullColNames   0x00000020  /* Show full column names on SELECT */
    const int SQLITE_ShortColNames = 0x00000040;//#define SQLITE_ShortColNames  0x00000040  /* Show short columns names */
    const int SQLITE_CountRows = 0x00000080;//#define SQLITE_CountRows      0x00000080  /* Count rows changed by INSERT, */
    //                                          /*   DELETE, or UPDATE and return */
    //                                          /*   the count using a callback. */
    const int SQLITE_NullCallback = 0x00000100;  //#define SQLITE_NullCallback   0x00000100  /* Invoke the callback once if the */
    //                                          /*   result set is empty */
    const int SQLITE_SqlTrace = 0x00000200;      //#define SQLITE_SqlTrace       0x00000200  /* Debug print SQL as it executes */
    const int SQLITE_VdbeListing = 0x00000400;   //#define SQLITE_VdbeListing    0x00000400  /* Debug listings of VDBE programs */
    const int SQLITE_WriteSchema = 0x00000800;   //#define SQLITE_WriteSchema    0x00000800  /* OK to update SQLITE_MASTER */
    const int SQLITE_NoReadlock = 0x00001000;    //#define SQLITE_NoReadlock     0x00001000  /* Readlocks are omitted when
    //                                          ** accessing read-only databases */
    const int SQLITE_IgnoreChecks = 0x00002000;  //#define SQLITE_IgnoreChecks   0x00002000  /* Do not enforce check constraints */
    const int SQLITE_ReadUncommitted = 0x00004000;//#define SQLITE_ReadUncommitted 0x00004000 /* For shared-cache mode */
    const int SQLITE_LegacyFileFmt = 0x00008000; //#define SQLITE_LegacyFileFmt  0x00008000  /* Create new databases in format 1 */
    const int SQLITE_FullFSync = 0x00010000;     //#define SQLITE_FullFSync      0x00010000  /* Use full fsync on the backend */
    const int SQLITE_LoadExtension = 0x00020000; //#define SQLITE_LoadExtension  0x00020000  /* Enable load_extension */

    const int SQLITE_RecoveryMode = 0x00040000;  //#define SQLITE_RecoveryMode   0x00040000  /* Ignore schema errors */
    const int SQLITE_ReverseOrder = 0x00100000;  //#define SQLITE_ReverseOrder   0x00100000  /* Reverse unordered SELECTs */

    /*
    ** Possible values for the sqlite.magic field.
    ** The numbers are obtained at random and have no special meaning, other
    ** than being distinct from one another.
    */
    const int SQLITE_MAGIC_OPEN = 0x1029a697;   //#define SQLITE_MAGIC_OPEN     0xa029a697  /* Database is open */
    const int SQLITE_MAGIC_CLOSED = 0x2f3c2d33; //#define SQLITE_MAGIC_CLOSED   0x9f3c2d33  /* Database is closed */
    const int SQLITE_MAGIC_SICK = 0x3b771290;   //#define SQLITE_MAGIC_SICK     0x4b771290  /* Error and awaiting close */
    const int SQLITE_MAGIC_BUSY = 0x403b7906;   //#define SQLITE_MAGIC_BUSY     0xf03b7906  /* Database currently in use */
    const int SQLITE_MAGIC_ERROR = 0x55357930;  //#define SQLITE_MAGIC_ERROR    0xb5357930  /* An SQLITE_MISUSE error occurred */

    /*
    ** Each SQL function is defined by an instance of the following
    ** structure.  A pointer to this structure is stored in the sqlite.aFunc
    ** hash table.  When multiple functions have the same name, the hash table
    ** points to a linked list of these structures.
    */
    public class FuncDef
    {
      public i16 nArg;           /* Number of arguments.  -1 means unlimited */
      public u8 iPrefEnc;        /* Preferred text encoding (SQLITE_UTF8, 16LE, 16BE) */
      public u8 flags;           /* Some combination of SQLITE_FUNC_* */
      public object pUserData;   /* User data parameter */
      public FuncDef pNext;      /* Next function with same name */
      public dxFunc xFunc;//)(sqlite3_context*,int,sqlite3_value**); /* Regular function */
      public dxStep xStep;//)(sqlite3_context*,int,sqlite3_value**); /* Aggregate step */
      public dxFinal xFinalize;//)(sqlite3_context*);                /* Aggregate finalizer */
      public string zName;       /* SQL name of the function. */
      public FuncDef pHash;      /* Next with a different name but the same hash */


      public FuncDef()
      { }

      public FuncDef( i16 nArg, u8 iPrefEnc, u8 iflags, object pUserData, FuncDef pNext, dxFunc xFunc, dxStep xStep, dxFinal xFinalize, string zName, FuncDef pHash )
      {
        this.nArg = nArg;
        this.iPrefEnc = iPrefEnc;
        this.flags = iflags;
        this.pUserData = pUserData;
        this.pNext = pNext;
        this.xFunc = xFunc;
        this.xStep = xStep;
        this.xFinalize = xFinalize;
        this.zName = zName;
        this.pHash = pHash;
      }
      public FuncDef( string zName, u8 iPrefEnc, i16 nArg, int iArg, u8 iflags, dxFunc xFunc )
      {
        this.nArg = nArg;
        this.iPrefEnc = iPrefEnc;
        this.flags = iflags;
        this.pUserData = iArg;
        this.pNext = null;
        this.xFunc = xFunc;
        this.xStep = null;
        this.xFinalize = null;
        this.zName = zName;
      }

      public FuncDef( string zName, u8 iPrefEnc, i16 nArg, int iArg, u8 iflags, dxStep xStep, dxFinal xFinal )
      {
        this.nArg = nArg;
        this.iPrefEnc = iPrefEnc;
        this.flags = iflags;
        this.pUserData = iArg;
        this.pNext = null;
        this.xFunc = null;
        this.xStep = xStep;
        this.xFinalize = xFinal;
        this.zName = zName;
      }

      public FuncDef( string zName, u8 iPrefEnc, i16 nArg, object arg, dxFunc xFunc, u8 flags )
      {
        this.nArg = nArg;
        this.iPrefEnc = iPrefEnc;
        this.flags = flags;
        this.pUserData = arg;
        this.pNext = null;
        this.xFunc = xFunc;
        this.xStep = null;
        this.xFinalize = null;
        this.zName = zName;
      }

    };

    /*
    ** Possible values for FuncDef.flags
    */
    //#define SQLITE_FUNC_LIKE     0x01  /* Candidate for the LIKE optimization */
    //#define SQLITE_FUNC_CASE     0x02  /* Case-sensitive LIKE-type function */
    //#define SQLITE_FUNC_EPHEM    0x04  /* Ephemeral.  Delete with VDBE */
    //#define SQLITE_FUNC_NEEDCOLL 0x08 /* sqlite3GetFuncCollSeq() might be called */
    //#define SQLITE_FUNC_PRIVATE  0x10 /* Allowed for internal use only */
    //#define SQLITE_FUNC_COUNT    0x20 /* Built-in count(*) aggregate */
    const int SQLITE_FUNC_LIKE = 0x01;    /* Candidate for the LIKE optimization */
    const int SQLITE_FUNC_CASE = 0x02;    /* Case-sensitive LIKE-type function */
    const int SQLITE_FUNC_EPHEM = 0x04;   /* Ephermeral.  Delete with VDBE */
    const int SQLITE_FUNC_NEEDCOLL = 0x08;/* sqlite3GetFuncCollSeq() might be called */
    const int SQLITE_FUNC_PRIVATE = 0x10; /* Allowed for internal use only */
    const int SQLITE_FUNC_COUNT = 0x20;   /* Built-in count(*) aggregate */


    /*
    ** The following three macros, FUNCTION(), LIKEFUNC() and AGGREGATE() are
    ** used to create the initializers for the FuncDef structures.
    **
    **   FUNCTION(zName, nArg, iArg, bNC, xFunc)
    **     Used to create a scalar function definition of a function zName
    **     implemented by C function xFunc that accepts nArg arguments. The
    **     value passed as iArg is cast to a (void*) and made available
    **     as the user-data (sqlite3_user_data()) for the function. If
    **     argument bNC is true, then the SQLITE_FUNC_NEEDCOLL flag is set.
    **
    **   AGGREGATE(zName, nArg, iArg, bNC, xStep, xFinal)
    **     Used to create an aggregate function definition implemented by
    **     the C functions xStep and xFinal. The first four parameters
    **     are interpreted in the same way as the first 4 parameters to
    **     FUNCTION().
    **
    **   LIKEFUNC(zName, nArg, pArg, flags)
    **     Used to create a scalar function definition of a function zName
    **     that accepts nArg arguments and is implemented by a call to C
    **     function likeFunc. Argument pArg is cast to a (void *) and made
    **     available as the function user-data (sqlite3_user_data()). The
    **     FuncDef.flags variable is set to the value passed as the flags
    **     parameter.
    */
    //#define FUNCTION(zName, nArg, iArg, bNC, xFunc) \
    //  {nArg, SQLITE_UTF8, bNC*SQLITE_FUNC_NEEDCOLL, \
    //SQLITE_INT_TO_PTR(iArg), 0, xFunc, 0, 0, #zName, 0}

    static FuncDef FUNCTION( string zName, i16 nArg, int iArg, u8 bNC, dxFunc xFunc )
    { return new FuncDef( zName, SQLITE_UTF8, nArg, iArg, (u8)( bNC * SQLITE_FUNC_NEEDCOLL ), xFunc ); }

    //#define STR_FUNCTION(zName, nArg, pArg, bNC, xFunc) \
    //  {nArg, SQLITE_UTF8, bNC*SQLITE_FUNC_NEEDCOLL, \
    //pArg, 0, xFunc, 0, 0, #zName, 0}

    //#define LIKEFUNC(zName, nArg, arg, flags) \
    //  {nArg, SQLITE_UTF8, flags, (void *)arg, 0, likeFunc, 0, 0, #zName, 0}
    static FuncDef LIKEFUNC( string zName, i16 nArg, object arg, u8 flags )
    { return new FuncDef( zName, SQLITE_UTF8, nArg, arg, likeFunc, flags ); }

    //#define AGGREGATE(zName, nArg, arg, nc, xStep, xFinal) \
    //  {nArg, SQLITE_UTF8, nc*SQLITE_FUNC_NEEDCOLL, \
    //SQLITE_INT_TO_PTR(arg), 0, 0, xStep,xFinal,#zName,0}

    static FuncDef AGGREGATE( string zName, i16 nArg, int arg, u8 nc, dxStep xStep, dxFinal xFinal )
    { return new FuncDef( zName, SQLITE_UTF8, nArg, arg, (u8)( nc * SQLITE_FUNC_NEEDCOLL ), xStep, xFinal ); }

    /*
    ** All current savepoints are stored in a linked list starting at
    ** sqlite3.pSavepoint. The first element in the list is the most recently
    ** opened savepoint. Savepoints are added to the list by the vdbe
    ** OP_Savepoint instruction.
    */
    //struct Savepoint {
    //  char *zName;                        /* Savepoint name (nul-terminated) */
    //  Savepoint *pNext;                   /* Parent savepoint (if any) */
    //};
    public class Savepoint
    {
      public string zName;              /* Savepoint name (nul-terminated) */
      public Savepoint pNext;           /* Parent savepoint (if any) */
    };
    /*
    ** The following are used as the second parameter to sqlite3Savepoint(),
    ** and as the P1 argument to the OP_Savepoint instruction.
    */
    const int SAVEPOINT_BEGIN = 0;   //#define SAVEPOINT_BEGIN      0
    const int SAVEPOINT_RELEASE = 1;   //#define SAVEPOINT_RELEASE    1
    const int SAVEPOINT_ROLLBACK = 2;    //#define SAVEPOINT_ROLLBACK   2

    /*
    ** Each SQLite module (virtual table definition) is defined by an
    ** instance of the following structure, stored in the sqlite3.aModule
    ** hash table.
    */
    public class Module
    {
      public sqlite3_module pModule;          /* Callback pointers */
      public string zName;                    /* Name passed to create_module() */
      public object pAux;                     /* pAux passed to create_module() */
      public dxDestroy xDestroy;//)(void *);  /* Module destructor function */
    };

    /*
** information about each column of an SQL table is held in an instance
** of this structure.
*/
    public class Column
    {
      public string zName;      /* Name of this column */
      public Expr pDflt;        /* Default value of this column */
      public string zDflt;      /* Original text of the default value */
      public string zType;      /* Data type for this column */
      public string zColl;      /* Collating sequence.  If NULL, use the default */
      public u8 notNull;        /* True if there is a NOT NULL constraint */
      public u8 isPrimKey;      /* True if this column is part of the PRIMARY KEY */
      public char affinity;     /* One of the SQLITE_AFF_... values */
#if !SQLITE_OMIT_VIRTUALTABLE
public   u8 isHidden;     /* True if this column is 'hidden' */
#endif
      public Column Copy()
      {
        Column cp = (Column)MemberwiseClone();
        if ( cp.pDflt != null ) cp.pDflt = pDflt.Copy();
        return cp;
      }
    };

    /*
    ** A "Collating Sequence" is defined by an instance of the following
    ** structure. Conceptually, a collating sequence consists of a name and
    ** a comparison routine that defines the order of that sequence.
    **
    ** There may two separate implementations of the collation function, one
    ** that processes text in UTF-8 encoding (CollSeq.xCmp) and another that
    ** processes text encoded in UTF-16 (CollSeq.xCmp16), using the machine
    ** native byte order. When a collation sequence is invoked, SQLite selects
    ** the version that will require the least expensive encoding
    ** translations, if any.
    **
    ** The CollSeq.pUser member variable is an extra parameter that passed in
    ** as the first argument to the UTF-8 comparison function, xCmp.
    ** CollSeq.pUser16 is the equivalent for the UTF-16 comparison function,
    ** xCmp16.
    **
    ** If both CollSeq.xCmp and CollSeq.xCmp16 are NULL, it means that the
    ** collating sequence is undefined.  Indices built on an undefined
    ** collating sequence may not be read or written.
    */
    public class CollSeq
    {
      public string zName;          /* Name of the collating sequence, UTF-8 encoded */
      public u8 enc;                /* Text encoding handled by xCmp() */
      public u8 type;               /* One of the SQLITE_COLL_... values below */
      public object pUser;          /* First argument to xCmp() */
      public dxCompare xCmp;//)(void*,int, const void*, int, const void*);
      public dxDelCollSeq xDel;//)(void*);  /* Destructor for pUser */

      public CollSeq Copy()
      {
        if ( this == null )
          return null;
        else
        {
          CollSeq cp = (CollSeq)MemberwiseClone();
          return cp;
        }
      }
    };

    /*
    ** Allowed values of CollSeq.type:
    */
    const int SQLITE_COLL_BINARY = 1;//#define SQLITE_COLL_BINARY  1  /* The default memcmp() collating sequence */
    const int SQLITE_COLL_NOCASE = 2;//#define SQLITE_COLL_NOCASE  2  /* The built-in NOCASE collating sequence */
    const int SQLITE_COLL_REVERSE = 3;//#define SQLITE_COLL_REVERSE 3  /* The built-in REVERSE collating sequence */
    const int SQLITE_COLL_USER = 0;//#define SQLITE_COLL_USER    0  /* Any other user-defined collating sequence */

    /*
    ** A sort order can be either ASC or DESC.
    */
    const int SQLITE_SO_ASC = 0;//#define SQLITE_SO_ASC       0  /* Sort in ascending order */
    const int SQLITE_SO_DESC = 1;//#define SQLITE_SO_DESC     1  /* Sort in ascending order */

    /*
    ** Column affinity types.
    **
    ** These used to have mnemonic name like 'i' for SQLITE_AFF_INTEGER and
    ** 't' for SQLITE_AFF_TEXT.  But we can save a little space and improve
    ** the speed a little by numbering the values consecutively.
    **
    ** But rather than start with 0 or 1, we begin with 'a'.  That way,
    ** when multiple affinity types are concatenated into a string and
    ** used as the P4 operand, they will be more readable.
    **
    ** Note also that the numeric types are grouped together so that testing
    ** for a numeric type is a single comparison.
    */
    const char SQLITE_AFF_TEXT = 'a';//#define SQLITE_AFF_TEXT     'a'
    const char SQLITE_AFF_NONE = 'b';//#define SQLITE_AFF_NONE     'b'
    const char SQLITE_AFF_NUMERIC = 'c';//#define SQLITE_AFF_NUMERIC  'c'
    const char SQLITE_AFF_INTEGER = 'd';//#define SQLITE_AFF_INTEGER  'd'
    const char SQLITE_AFF_REAL = 'e';//#define SQLITE_AFF_REAL     'e'

    //#define sqlite3IsNumericAffinity(X)  ((X)>=SQLITE_AFF_NUMERIC)

    /*
    ** The SQLITE_AFF_MASK values masks off the significant bits of an
    ** affinity value.
    */
    const int SQLITE_AFF_MASK = 0x67;//#define SQLITE_AFF_MASK     0x67

    /*
    ** Additional bit values that can be ORed with an affinity without
    ** changing the affinity.
    */
    const int SQLITE_JUMPIFNULL = 0x08;//#define SQLITE_JUMPIFNULL   0x08  /* jumps if either operand is NULL */
    const int SQLITE_STOREP2 = 0x10;   //#define SQLITE_STOREP2      0x10  /* Store result in reg[P2] rather than jump */

    /*
    ** An object of this type is created for each virtual table present in
    ** the database schema. 
    **
    ** If the database schema is shared, then there is one instance of this
    ** structure for each database connection (sqlite3*) that uses the shared
    ** schema. This is because each database connection requires its own unique
    ** instance of the sqlite3_vtab* handle used to access the virtual table 
    ** implementation. sqlite3_vtab* handles can not be shared between 
    ** database connections, even when the rest of the in-memory database 
    ** schema is shared, as the implementation often stores the database
    ** connection handle passed to it via the xConnect() or xCreate() method
    ** during initialization internally. This database connection handle may
    ** then used by the virtual table implementation to access real tables 
    ** within the database. So that they appear as part of the callers 
    ** transaction, these accesses need to be made via the same database 
    ** connection as that used to execute SQL operations on the virtual table.
    **
    ** All VTable objects that correspond to a single table in a shared
    ** database schema are initially stored in a linked-list pointed to by
    ** the Table.pVTable member variable of the corresponding Table object.
    ** When an sqlite3_prepare() operation is required to access the virtual
    ** table, it searches the list for the VTable that corresponds to the
    ** database connection doing the preparing so as to use the correct
    ** sqlite3_vtab* handle in the compiled query.
    **
    ** When an in-memory Table object is deleted (for example when the
    ** schema is being reloaded for some reason), the VTable objects are not 
    ** deleted and the sqlite3_vtab* handles are not xDisconnect()ed 
    ** immediately. Instead, they are moved from the Table.pVTable list to
    ** another linked list headed by the sqlite3.pDisconnect member of the
    ** corresponding sqlite3 structure. They are then deleted/xDisconnected 
    ** next time a statement is prepared using said sqlite3*. This is done
    ** to avoid deadlock issues involving multiple sqlite3.mutex mutexes.
    ** Refer to comments above function sqlite3VtabUnlockList() for an
    ** explanation as to why it is safe to add an entry to an sqlite3.pDisconnect
    ** list without holding the corresponding sqlite3.mutex mutex.
    **
    ** The memory for objects of this type is always allocated by 
    ** sqlite3DbMalloc(), using the connection handle stored in VTable.db as 
    ** the first argument.
    */
    public class VTable
    {
      public sqlite3 db;              /* Database connection associated with this table */
      public Module pMod;             /* Pointer to module implementation */
      public sqlite3_vtab pVtab;      /* Pointer to vtab instance */
      public int nRef;                /* Number of pointers to this structure */
      public VTable pNext;            /* Next in linked list (see above) */
    };

    /*
    ** Each SQL table is represented in memory by an instance of the
    ** following structure.
    **
    ** Table.zName is the name of the table.  The case of the original
    ** CREATE TABLE statement is stored, but case is not significant for
    ** comparisons.
    **
    ** Table.nCol is the number of columns in this table.  Table.aCol is a
    ** pointer to an array of Column structures, one for each column.
    **
    ** If the table has an INTEGER PRIMARY KEY, then Table.iPKey is the index of
    ** the column that is that key.   Otherwise Table.iPKey is negative.  Note
    ** that the datatype of the PRIMARY KEY must be INTEGER for this field to
    ** be set.  An INTEGER PRIMARY KEY is used as the rowid for each row of
    ** the table.  If a table has no INTEGER PRIMARY KEY, then a random rowid
    ** is generated for each row of the table.  TF_HasPrimaryKey is set if
    ** the table has any PRIMARY KEY, INTEGER or otherwise.
    **
    ** Table.tnum is the page number for the root BTree page of the table in the
    ** database file.  If Table.iDb is the index of the database table backend
    ** in sqlite.aDb[].  0 is for the main database and 1 is for the file that
    ** holds temporary tables and indices.  If TF_Ephemeral is set
    ** then the table is stored in a file that is automatically deleted
    ** when the VDBE cursor to the table is closed.  In this case Table.tnum
    ** refers VDBE cursor number that holds the table open, not to the root
    ** page number.  Transient tables are used to hold the results of a
    ** sub-query that appears instead of a real table name in the FROM clause
    ** of a SELECT statement.
    */
    public class Table
    {
      public sqlite3 dbMem;     /* DB connection used for lookaside allocations. */
      public string zName;      /* Name of the table or view */
      public int iPKey;         /* If not negative, use aCol[iPKey] as the primary key */
      public int nCol;          /* Number of columns in this table */
      public Column[] aCol;     /* Information about each column */
      public Index pIndex;      /* List of SQL indexes on this table. */
      public int tnum;          /* Root BTree node for this table (see note above) */
      public Select pSelect;    /* NULL for tables.  Points to definition if a view. */
      public u16 nRef;          /* Number of pointers to this Table */
      public u8 tabFlags;       /* Mask of TF_* values */
      public u8 keyConf;        /* What to do in case of uniqueness conflict on iPKey */
      public FKey pFKey;        /* Linked list of all foreign keys in this table */
      public string zColAff;    /* String defining the affinity of each column */
#if !SQLITE_OMIT_CHECK
      public Expr pCheck;       /* The AND of all CHECK constraints */
#endif
#if !SQLITE_OMIT_ALTERTABLE
      public int addColOffset;  /* Offset in CREATE TABLE stmt to add a new column */
#endif
#if !SQLITE_OMIT_VIRTUALTABLE
      public VTable pVTable;      /* List of VTable objects. */
      public int nModuleArg;      /* Number of arguments to the module */
      public string[] azModuleArg;/* Text of all module args. [0] is module name */
#endif
      public Trigger pTrigger;  /* List of SQL triggers on this table */
      public Schema pSchema;    /* Schema that contains this table */
      public Table pNextZombie;  /* Next on the Parse.pZombieTab list */

      public Table Copy()
      {
        if ( this == null )
          return null;
        else
        {
          Table cp = (Table)MemberwiseClone();
          if ( pIndex != null ) cp.pIndex = pIndex.Copy();
          if ( pSelect != null ) cp.pSelect = pSelect.Copy();
          if ( pTrigger != null ) cp.pTrigger = pTrigger.Copy();
          if ( pFKey != null ) cp.pFKey = pFKey.Copy();
#if !SQLITE_OMIT_CHECK
          // Don't Clone Checks, only copy reference via Memberwise Clone above --
          //if ( pCheck != null ) cp.pCheck = pCheck.Copy();
#endif
#if !SQLITE_OMIT_VIRTUALTABLE
if ( pMod != null ) cp.pMod =pMod.Copy();
if ( pVtab != null ) cp.pVtab =pVtab.Copy();
#endif
          // Don't Clone Schema, only copy reference via Memberwise Clone above --
          // if ( pSchema != null ) cp.pSchema=pSchema.Copy();
          // Don't Clone pNextZombie, only copy reference via Memberwise Clone above --
          // if ( pNextZombie != null ) cp.pNextZombie=pNextZombie.Copy();
          return cp;
        }
      }
    };

    /*
    ** Allowed values for Tabe.tabFlags.
    */
    //#define TF_Readonly        0x01    /* Read-only system table */
    //#define TF_Ephemeral       0x02    /* An ephemeral table */
    //#define TF_HasPrimaryKey   0x04    /* Table has a primary key */
    //#define TF_Autoincrement   0x08    /* Integer primary key is autoincrement */
    //#define TF_Virtual         0x10    /* Is a virtual table */
    //#define TF_NeedMetadata    0x20    /* aCol[].zType and aCol[].pColl missing */
    /*
    ** Allowed values for Tabe.tabFlags.
    */
    const int TF_Readonly = 0x01;   /* Read-only system table */
    const int TF_Ephemeral = 0x02;   /* An ephemeral table */
    const int TF_HasPrimaryKey = 0x04;   /* Table has a primary key */
    const int TF_Autoincrement = 0x08;   /* Integer primary key is autoincrement */
    const int TF_Virtual = 0x10;   /* Is a virtual table */
    const int TF_NeedMetadata = 0x20;   /* aCol[].zType and aCol[].pColl missing */

    /*
    ** Test to see whether or not a table is a virtual table.  This is
    ** done as a macro so that it will be optimized out when virtual
    ** table support is omitted from the build.
    */
#if !SQLITE_OMIT_VIRTUALTABLE
//#  define IsVirtual(X)      (((X)->tabFlags & TF_Virtual)!=0)
static bool IsVirtual( Table X) { return (X.tabFlags & TF_Virtual)!=0;}
//#  define IsHiddenColumn(X) ((X)->isHidden)
static bool IsVirtual( Column X) { return X.isHidden!=0;}
#else
    //#  define IsVirtual(X)      0
    static bool IsVirtual( Table T ) { return false; }
    //#  define IsHiddenColumn(X) 0
    static bool IsHiddenColumn( Column C ) { return false; }
#endif

    /*
** Each foreign key constraint is an instance of the following structure.
**
** A foreign key is associated with two tables.  The "from" table is
** the table that contains the REFERENCES clause that creates the foreign
** key.  The "to" table is the table that is named in the REFERENCES clause.
** Consider this example:
**
**     CREATE TABLE ex1(
**       a INTEGER PRIMARY KEY,
**       b INTEGER CONSTRAINT fk1 REFERENCES ex2(x)
**     );
**
** For foreign key "fk1", the from-table is "ex1" and the to-table is "ex2".
**
** Each REFERENCES clause generates an instance of the following structure
** which is attached to the from-table.  The to-table need not exist when
** the from-table is created.  The existence of the to-table is not checked.
*/
    public class FKey
    {
      public Table pFrom;         /* The table that contains the REFERENCES clause */
      public FKey pNextFrom;      /* Next foreign key in pFrom */
      public string zTo;          /* Name of table that the key points to */
      public int nCol;            /* Number of columns in this key */
      public u8 isDeferred;       /* True if constraint checking is deferred till COMMIT */
      public u8 updateConf;       /* How to resolve conflicts that occur on UPDATE */
      public u8 deleteConf;       /* How to resolve conflicts that occur on DELETE */
      public u8 insertConf;       /* How to resolve conflicts that occur on INSERT */
      public class sColMap
      {  /* Mapping of columns in pFrom to columns in zTo */
        public int iFrom;         /* Index of column in pFrom */
        public string zCol;       /* Name of column in zTo.  If 0 use PRIMARY KEY */
      };
      public sColMap[] aCol;      /* One entry for each of nCol column s */

      public FKey Copy()
      {
        if ( this == null )
          return null;
        else
        {
          FKey cp = (FKey)MemberwiseClone();
          if ( pFrom != null ) cp.pFrom = pFrom.Copy();
          if ( pNextFrom != null ) cp.pNextFrom = pNextFrom.Copy();
          Debugger.Break(); // Check on the sCollMap
          return cp;
        }
      }

    };

    /*
    ** SQLite supports many different ways to resolve a constraint
    ** error.  ROLLBACK processing means that a constraint violation
    ** causes the operation in process to fail and for the current transaction
    ** to be rolled back.  ABORT processing means the operation in process
    ** fails and any prior changes from that one operation are backed out,
    ** but the transaction is not rolled back.  FAIL processing means that
    ** the operation in progress stops and returns an error code.  But prior
    ** changes due to the same operation are not backed out and no rollback
    ** occurs.  IGNORE means that the particular row that caused the constraint
    ** error is not inserted or updated.  Processing continues and no error
    ** is returned.  REPLACE means that preexisting database rows that caused
    ** a UNIQUE constraint violation are removed so that the new insert or
    ** update can proceed.  Processing continues and no error is reported.
    **
    ** RESTRICT, SETNULL, and CASCADE actions apply only to foreign keys.
    ** RESTRICT is the same as ABORT for IMMEDIATE foreign keys and the
    ** same as ROLLBACK for DEFERRED keys.  SETNULL means that the foreign
    ** key is set to NULL.  CASCADE means that a DELETE or UPDATE of the
    ** referenced table row is propagated into the row that holds the
    ** foreign key.
    **
    ** The following symbolic values are used to record which type
    ** of action to take.
    */
    const int OE_None = 0;//#define OE_None     0   /* There is no constraint to check */
    const int OE_Rollback = 1;//#define OE_Rollback 1   /* Fail the operation and rollback the transaction */
    const int OE_Abort = 2;//#define OE_Abort    2   /* Back out changes but do no rollback transaction */
    const int OE_Fail = 3;//#define OE_Fail     3   /* Stop the operation but leave all prior changes */
    const int OE_Ignore = 4;//#define OE_Ignore   4   /* Ignore the error. Do not do the INSERT or UPDATE */
    const int OE_Replace = 5;//#define OE_Replace  5   /* Delete existing record, then do INSERT or UPDATE */

    const int OE_Restrict = 6;//#define OE_Restrict 6   /* OE_Abort for IMMEDIATE, OE_Rollback for DEFERRED */
    const int OE_SetNull = 7;//#define OE_SetNull  7   /* Set the foreign key value to NULL */
    const int OE_SetDflt = 8;//#define OE_SetDflt  8   /* Set the foreign key value to its default */
    const int OE_Cascade = 9;//#define OE_Cascade  9   /* Cascade the changes */

    const int OE_Default = 99;//#define OE_Default  99  /* Do whatever the default action is */


    /*
    ** An instance of the following structure is passed as the first
    ** argument to sqlite3VdbeKeyCompare and is used to control the
    ** comparison of the two index keys.
    */
    public class KeyInfo
    {
      public sqlite3 db;          /* The database connection */
      public u8 enc;             /* Text encoding - one of the TEXT_Utf* values */
      public u16 nField;          /* Number of entries in aColl[] */
      public u8[] aSortOrder;   /* If defined an aSortOrder[i] is true, sort DESC */
      public CollSeq[] aColl = new CollSeq[1];  /* Collating sequence for each term of the key */
      public KeyInfo Copy()
      {
        return (KeyInfo)MemberwiseClone();
      }
    };

    /*
    ** An instance of the following structure holds information about a
    ** single index record that has already been parsed out into individual
    ** values.
    **
    ** A record is an object that contains one or more fields of data.
    ** Records are used to store the content of a table row and to store
    ** the key of an index.  A blob encoding of a record is created by
    ** the OP_MakeRecord opcode of the VDBE and is disassembled by the
    ** OP_Column opcode.
    **
    ** This structure holds a record that has already been disassembled
    ** into its constituent fields.
    */
    public class UnpackedRecord
    {
      public KeyInfo pKeyInfo;   /* Collation and sort-order information */
      public u16 nField;         /* Number of entries in apMem[] */
      public u16 flags;          /* Boolean settings.  UNPACKED_... below */
      public i64 rowid;          /* Used by UNPACKED_PREFIX_SEARCH */
      public Mem[] aMem;         /* Values */
    };

    /*
    ** Allowed values of UnpackedRecord.flags
    */
    //#define UNPACKED_NEED_FREE     0x0001  /* Memory is from sqlite3Malloc() */
    //#define UNPACKED_NEED_DESTROY  0x0002  /* apMem[]s should all be destroyed */
    //#define UNPACKED_IGNORE_ROWID  0x0004  /* Ignore trailing rowid on key1 */
    //#define UNPACKED_INCRKEY       0x0008  /* Make this key an epsilon larger */
    //#define UNPACKED_PREFIX_MATCH  0x0010  /* A prefix match is considered OK */
    //#define UNPACKED_PREFIX_SEARCH 0x0020  /* A prefix match is considered OK */
    const int UNPACKED_NEED_FREE = 0x0001;  /* Memory is from sqlite3Malloc() */
    const int UNPACKED_NEED_DESTROY = 0x0002;  /* apMem[]s should all be destroyed */
    const int UNPACKED_IGNORE_ROWID = 0x0004;  /* Ignore trailing rowid on key1 */
    const int UNPACKED_INCRKEY = 0x0008;  /* Make this key an epsilon larger */
    const int UNPACKED_PREFIX_MATCH = 0x0010;  /* A prefix match is considered OK */
    const int UNPACKED_PREFIX_SEARCH = 0x0020; /* A prefix match is considered OK */

    /*
    ** Each SQL index is represented in memory by an
    ** instance of the following structure.
    **
    ** The columns of the table that are to be indexed are described
    ** by the aiColumn[] field of this structure.  For example, suppose
    ** we have the following table and index:
    **
    **     CREATE TABLE Ex1(c1 int, c2 int, c3 text);
    **     CREATE INDEX Ex2 ON Ex1(c3,c1);
    **
    ** In the Table structure describing Ex1, nCol==3 because there are
    ** three columns in the table.  In the Index structure describing
    ** Ex2, nColumn==2 since 2 of the 3 columns of Ex1 are indexed.
    ** The value of aiColumn is {2, 0}.  aiColumn[0]==2 because the
    ** first column to be indexed (c3) has an index of 2 in Ex1.aCol[].
    ** The second column to be indexed (c1) has an index of 0 in
    ** Ex1.aCol[], hence Ex2.aiColumn[1]==0.
    **
    ** The Index.onError field determines whether or not the indexed columns
    ** must be unique and what to do if they are not.  When Index.onError=OE_None,
    ** it means this is not a unique index.  Otherwise it is a unique index
    ** and the value of Index.onError indicate the which conflict resolution
    ** algorithm to employ whenever an attempt is made to insert a non-unique
    ** element.
    */
    public class Index
    {
      public string zName;      /* Name of this index */
      public int nColumn;       /* Number of columns in the table used by this index */
      public int[] aiColumn;    /* Which columns are used by this index.  1st is 0 */
      public int[] aiRowEst;    /* Result of ANALYZE: Est. rows selected by each column */
      public Table pTable;      /* The SQL table being indexed */
      public int tnum;          /* Page containing root of this index in database file */
      public u8 onError;        /* OE_Abort, OE_Ignore, OE_Replace, or OE_None */
      public u8 autoIndex;      /* True if is automatically created (ex: by UNIQUE) */
      public string zColAff;    /* String defining the affinity of each column */
      public Index pNext;       /* The next index associated with the same table */
      public Schema pSchema;    /* Schema containing this index */
      public u8[] aSortOrder;   /* Array of size Index.nColumn. True==DESC, False==ASC */
      public string[] azColl;   /* Array of collation sequence names for index */

      public Index Copy()
      {
        if ( this == null )
          return null;
        else
        {
          Index cp = (Index)MemberwiseClone();
          return cp;
        }
      }
    };

    /*
    ** Each token coming out of the lexer is an instance of
    ** this structure.  Tokens are also used as part of an expression.
    **
    ** Note if Token.z==0 then Token.dyn and Token.n are undefined and
    ** may contain random values.  Do not make any assumptions about Token.dyn
    ** and Token.n when Token.z==0.
    */
    public class Token
    {
#if DEBUG_CLASS_TOKEN || DEBUG_CLASS_ALL
public string _z; /* Text of the token.  Not NULL-terminated! */
public bool dyn;//  : 1;      /* True for malloced memory, false for static */
public Int32 _n;//  : 31;     /* Number of characters in this token */

public string z
{
get { return _z; }
set { _z = value; }
}

public Int32 n
{
get { return _n; }
set { _n = value; }
}
#else
      public string z; /* Text of the token.  Not NULL-terminated! */
      public Int32 n;  /* Number of characters in this token */
#endif
      public Token()
      {
        this.z = null;
        this.n = 0;
      }
      public Token( string z, Int32 n )
      {
        this.z = z;
        this.n = n;
      }
      public Token Copy()
      {
        if ( this == null )
          return null;
        else
        {
          Token cp = (Token)MemberwiseClone();
          if ( z == null || z.Length == 0 )
            cp.n = 0;
          else
            if ( n > z.Length ) cp.n = z.Length;
          return cp;
        }
      }
    }

    /*
    ** An instance of this structure contains information needed to generate
    ** code for a SELECT that contains aggregate functions.
    **
    ** If Expr.op==TK_AGG_COLUMN or TK_AGG_FUNCTION then Expr.pAggInfo is a
    ** pointer to this structure.  The Expr.iColumn field is the index in
    ** AggInfo.aCol[] or AggInfo.aFunc[] of information needed to generate
    ** code for that node.
    **
    ** AggInfo.pGroupBy and AggInfo.aFunc.pExpr point to fields within the
    ** original Select structure that describes the SELECT statement.  These
    ** fields do not need to be freed when deallocating the AggInfo structure.
    */
    public class AggInfo_col
    {    /* For each column used in source tables */
      public Table pTab;             /* Source table */
      public int iTable;              /* VdbeCursor number of the source table */
      public int iColumn;             /* Column number within the source table */
      public int iSorterColumn;       /* Column number in the sorting index */
      public int iMem;                /* Memory location that acts as accumulator */
      public Expr pExpr;             /* The original expression */
    };
    public class AggInfo_func
    {   /* For each aggregate function */
      public Expr pExpr;             /* Expression encoding the function */
      public FuncDef pFunc;          /* The aggregate function implementation */
      public int iMem;                /* Memory location that acts as accumulator */
      public int iDistinct;           /* Ephemeral table used to enforce DISTINCT */
    }
    public class AggInfo
    {
      public u8 directMode;          /* Direct rendering mode means take data directly
** from source tables rather than from accumulators */
      public u8 useSortingIdx;       /* In direct mode, reference the sorting index rather
** than the source table */
      public int sortingIdx;         /* VdbeCursor number of the sorting index */
      public ExprList pGroupBy;     /* The group by clause */
      public int nSortingColumn;     /* Number of columns in the sorting index */
      public AggInfo_col[] aCol;
      public int nColumn;            /* Number of used entries in aCol[] */
      public int nColumnAlloc;       /* Number of slots allocated for aCol[] */
      public int nAccumulator;       /* Number of columns that show through to the output.
** Additional columns are used only as parameters to
** aggregate functions */
      public AggInfo_func[] aFunc;
      public int nFunc;              /* Number of entries in aFunc[] */
      public int nFuncAlloc;         /* Number of slots allocated for aFunc[] */

      public AggInfo Copy()
      {
        if ( this == null )
          return null;
        else
        {
          AggInfo cp = (AggInfo)MemberwiseClone();
          if ( pGroupBy != null ) cp.pGroupBy = pGroupBy.Copy();
          return cp;
        }
      }
    };

    /*
    ** Each node of an expression in the parse tree is an instance
    ** of this structure.
    **
    ** Expr.op is the opcode.  The integer parser token codes are reused
    ** as opcodes here.  For example, the parser defines TK_GE to be an integer
    ** code representing the ">=" operator.  This same integer code is reused
    ** to represent the greater-than-or-equal-to operator in the expression
    ** tree.
    **
    ** If the expression is an SQL literal (TK_INTEGER, TK_FLOAT, TK_BLOB,
    ** or TK_STRING), then Expr.token contains the text of the SQL literal. If
    ** the expression is a variable (TK_VARIABLE), then Expr.token contains the
    ** variable name. Finally, if the expression is an SQL function (TK_FUNCTION),
    ** then Expr.token contains the name of the function.
    **
    ** Expr.pRight and Expr.pLeft are the left and right subexpressions of a
    ** binary operator. Either or both may be NULL.
    **
    ** Expr.x.pList is a list of arguments if the expression is an SQL function,
    ** a CASE expression or an IN expression of the form "<lhs> IN (<y>, <z>...)".
    ** Expr.x.pSelect is used if the expression is a sub-select or an expression of
    ** the form "<lhs> IN (SELECT ...)". If the EP_xIsSelect bit is set in the
    ** Expr.flags mask, then Expr.x.pSelect is valid. Otherwise, Expr.x.pList is
    ** valid.
    **
    ** An expression of the form ID or ID.ID refers to a column in a table.
    ** For such expressions, Expr.op is set to TK_COLUMN and Expr.iTable is
    ** the integer cursor number of a VDBE cursor pointing to that table and
    ** Expr.iColumn is the column number for the specific column.  If the
    ** expression is used as a result in an aggregate SELECT, then the
    ** value is also stored in the Expr.iAgg column in the aggregate so that
    ** it can be accessed after all aggregates are computed.
    **
    ** If the expression is an unbound variable marker (a question mark
    ** character '?' in the original SQL) then the Expr.iTable holds the index
    ** number for that variable.
    **
    ** If the expression is a subquery then Expr.iColumn holds an integer
    ** register number containing the result of the subquery.  If the
    ** subquery gives a constant result, then iTable is -1.  If the subquery
    ** gives a different answer at different times during statement processing
    ** then iTable is the address of a subroutine that computes the subquery.
    **
    ** If the Expr is of type OP_Column, and the table it is selecting from
    ** is a disk table or the "old.*" pseudo-table, then pTab points to the
    ** corresponding table definition.
    **
    ** ALLOCATION NOTES:
    **
    ** Expr objects can use a lot of memory space in database schema.  To
    ** help reduce memory requirements, sometimes an Expr object will be
    ** truncated.  And to reduce the number of memory allocations, sometimes
    ** two or more Expr objects will be stored in a single memory allocation,
    ** together with Expr.zToken strings.
    **
    ** If the EP_Reduced and EP_TokenOnly flags are set when
    ** an Expr object is truncated.  When EP_Reduced is set, then all
    ** the child Expr objects in the Expr.pLeft and Expr.pRight subtrees
    ** are contained within the same memory allocation.  Note, however, that
    ** the subtrees in Expr.x.pList or Expr.x.pSelect are always separately
    ** allocated, regardless of whether or not EP_Reduced is set.
    */
    public class Expr
    {
#if DEBUG_CLASS_EXPR || DEBUG_CLASS_ALL
public u8 _op;                      /* Operation performed by this node */
public u8 op
{
get { return _op; }
set { _op = value; }
}
#else
      public u8 op;                 /* Operation performed by this node */
#endif
      public char affinity;         /* The affinity of the column or 0 if not a column */
#if DEBUG_CLASS_EXPR || DEBUG_CLASS_ALL
public u16 _flags;                            /* Various flags.  EP_* See below */
public u16 flags
{
get { return _flags; }
set { _flags = value; }
}
public struct _u
{
public string _zToken;         /* Token value. Zero terminated and dequoted */
public string zToken
{
get { return _zToken; }
set { _zToken = value; }
}
public int iValue;            /* Integer value if EP_IntValue */
}

#else
      public struct _u
      {
        public string zToken;         /* Token value. Zero terminated and dequoted */
        public int iValue;            /* Integer value if EP_IntValue */
      }
      public u16 flags;             /* Various flags.  EP_* See below */
#endif
      public _u u;

      /* If the EP_TokenOnly flag is set in the Expr.flags mask, then no
      ** space is allocated for the fields below this point. An attempt to
      ** access them will result in a segfault or malfunction.
      *********************************************************************/

      public Expr pLeft;                           /* Left subnode */
      public Expr pRight;                          /* Right subnode */
      public struct _x
      {
        public ExprList pList;                       /* Function arguments or in "<expr> IN (<expr-list)" */
        public Select pSelect;                       /* Used for sub-selects and "<expr> IN (<select>)" */
      }
      public _x x;
      public CollSeq pColl;                        /* The collation type of the column or 0 */

      /* If the EP_Reduced flag is set in the Expr.flags mask, then no
      ** space is allocated for the fields below this point. An attempt to
      ** access them will result in a segfault or malfunction.
      *********************************************************************/

      public int iTable;            /* TK_COLUMN: cursor number of table holding column
   ** TK_REGISTER: register number */
      public i16 iColumn;           /* TK_COLUMN: column index.  -1 for rowid */
      public i16 iAgg;              /* Which entry in pAggInfo->aCol[] or ->aFunc[] */
      public i16 iRightJoinTable;   /* If EP_FromJoin, the right table of the join */
      public u16 flags2;            /* Second set of flags.  EP2_... */
      public AggInfo pAggInfo;      /* Used by TK_AGG_COLUMN and TK_AGG_FUNCTION */
      public Table pTab;            /* Table for TK_COLUMN expressions. */
#if SQLITE_MAX_EXPR_DEPTH //>0
      public int nHeight;           /* Height of the tree headed by this node */
      public Table pZombieTab;      /* List of Table objects to delete after code gen */
#endif

#if DEBUG_CLASS
public int op
{
get { return _op; }
set { _op = value; }
}
#endif
      public void CopyFrom( Expr cf )
      {
        op = cf.op;
        affinity = cf.affinity;
        flags = cf.flags;
        u = cf.u;
        pColl = cf.pColl == null ? null : cf.pColl.Copy();
        iTable = cf.iTable;
        iColumn = cf.iColumn;
        pAggInfo = cf.pAggInfo == null ? null : cf.pAggInfo.Copy();
        iAgg = cf.iAgg;
        iRightJoinTable = cf.iRightJoinTable;
        flags2 = cf.flags2;
        pTab = cf.pTab == null ? null : cf.pTab.Copy();
#if SQLITE_TEST || SQLITE_MAX_EXPR_DEPTH //SQLITE_MAX_EXPR_DEPTH>0
        nHeight = cf.nHeight;
        pZombieTab = cf.pZombieTab;
#endif
        pLeft = cf.pLeft == null ? null : cf.pLeft.Copy();
        pRight = cf.pRight == null ? null : cf.pRight.Copy();
        x.pList = cf.x.pList == null ? null : cf.x.pList.Copy();
        x.pSelect = cf.x.pSelect == null ? null : cf.x.pSelect.Copy();
      }

      public Expr Copy()
      {
        if ( this == null )
          return null;
        else
        {
          Expr cp = Copy_Minimal();
          if ( pLeft != null ) cp.pLeft = pLeft.Copy();
          if ( pRight != null ) cp.pRight = pRight.Copy();
          return cp;
        }
      }
      public Expr Copy_Minimal()
      {
        if ( this == null )
          return null;
        else
        {
          Expr cp = new Expr();
          cp.op = op;
          cp.affinity = affinity;
          cp.flags = flags;
          cp.u = u;
          if ( x.pList != null ) cp.x.pList = x.pList.Copy();
          if ( x.pSelect != null ) cp.x.pSelect = x.pSelect.Copy();
          if ( pColl != null ) cp.pColl = pColl.Copy();
          cp.iTable = iTable;
          cp.iColumn = iColumn;
          if ( pAggInfo != null ) cp.pAggInfo = pAggInfo.Copy();
          cp.iAgg = iAgg;
          cp.iRightJoinTable = iRightJoinTable;
          cp.flags2 = flags2;
          if ( pTab != null ) cp.pTab = pTab.Copy();
#if SQLITE_TEST || SQLITE_MAX_EXPR_DEPTH //SQLITE_MAX_EXPR_DEPTH>0
          cp.nHeight = nHeight;
          cp.pZombieTab = pZombieTab;
#endif
          return cp;
        }
      }
    };

    /*
    ** The following are the meanings of bits in the Expr.flags field.
    */
    //#define EP_FromJoin   0x0001  /* Originated in ON or USING clause of a join */
    //#define EP_Agg        0x0002  /* Contains one or more aggregate functions */
    //#define EP_Resolved   0x0004  /* IDs have been resolved to COLUMNs */
    //#define EP_Error      0x0008  /* Expression contains one or more errors */
    //#define EP_Distinct   0x0010  /* Aggregate function with DISTINCT keyword */
    //#define EP_VarSelect  0x0020  /* pSelect is correlated, not constant */
    //#define EP_DblQuoted  0x0040  /* token.z was originally in "..." */
    //#define EP_InfixFunc  0x0080  /* True for an infix function: LIKE, GLOB, etc */
    //#define EP_ExpCollate 0x0100  /* Collating sequence specified explicitly */
    //#define EP_AnyAff     0x0200  /* Can take a cached column of any affinity */
    //#define EP_FixedDest  0x0400  /* Result needed in a specific register */
    //#define EP_IntValue   0x0800  /* Integer value contained in u.iTable */
    //#define EP_xIsSelect  0x1000  /* x.pSelect is valid (otherwise x.pList is) */

    //#define EP_Reduced    0x2000  /* Expr struct is EXPR_REDUCEDSIZE bytes only */
    //#define EP_TokenOnly  0x4000  /* Expr struct is EXPR_TOKENONLYSIZE bytes only */
    //#define EP_Static     0x8000  /* Held in memory not obtained from malloc() */

    const ushort EP_FromJoin = 0x0001;
    const ushort EP_Agg = 0x0002;
    const ushort EP_Resolved = 0x0004;
    const ushort EP_Error = 0x0008;
    const ushort EP_Distinct = 0x0010;
    const ushort EP_VarSelect = 0x0020;
    const ushort EP_DblQuoted = 0x0040;
    const ushort EP_InfixFunc = 0x0080;
    const ushort EP_ExpCollate = 0x0100;
    const ushort EP_AnyAff = 0x0200;
    const ushort EP_FixedDest = 0x0400;
    const ushort EP_IntValue = 0x0800;
    const ushort EP_xIsSelect = 0x1000;

    const ushort EP_Reduced = 0x2000;
    const ushort EP_TokenOnly = 0x4000;
    const ushort EP_Static = 0x8000;

    /*
    ** The following are the meanings of bits in the Expr.flags2 field.
    */
    //#define EP2_MallocedToken  0x0001  /* Need to //sqlite3DbFree() Expr.zToken */
    //#define EP2_Irreducible    0x0002  /* Cannot EXPRDUP_REDUCE this Expr */
    const ushort EP2_MallocedToken = 0x0001;
    const ushort EP2_Irreducible = 0x0002;

    /*
    ** The pseudo-routine sqlite3ExprSetIrreducible sets the EP2_Irreducible
    ** flag on an expression structure.  This flag is used for VV&A only.  The
    ** routine is implemented as a macro that only works when in debugging mode,
    ** so as not to burden production code.
    */
#if SQLITE_DEBUG
    //# define ExprSetIrreducible(X)  (X)->flags2 |= EP2_Irreducible
    static void ExprSetIrreducible( Expr X ) { X.flags2 |= EP2_Irreducible; }
#else
//# define ExprSetIrreducible(X)
static void ExprSetIrreducible( Expr X ) { }
#endif

    /*
** These macros can be used to test, set, or clear bits in the
** Expr.flags field.
*/
    //#define ExprHasProperty(E,P)     (((E)->flags&(P))==(P))
    static bool ExprHasProperty( Expr E, int P ) { return ( E.flags & P ) == P; }
    //#define ExprHasAnyProperty(E,P)  (((E)->flags&(P))!=0)
    static bool ExprHasAnyProperty( Expr E, int P ) { return ( E.flags & P ) != 0; }
    //#define ExprSetProperty(E,P)     (E)->flags|=(P)
    static void ExprSetProperty( Expr E, int P ) { E.flags = (ushort)( E.flags | P ); }
    //#define ExprClearProperty(E,P)   (E)->flags&=~(P)
    static void ExprClearProperty( Expr E, int P ) { E.flags = (ushort)( E.flags & ~P ); }

    /*
    ** Macros to determine the number of bytes required by a normal Expr
    ** struct, an Expr struct with the EP_Reduced flag set in Expr.flags
    ** and an Expr struct with the EP_TokenOnly flag set.
    */
    //#define EXPR_FULLSIZE           sizeof(Expr)           /* Full size */
    //#define EXPR_REDUCEDSIZE        offsetof(Expr,iTable)  /* Common features */
    //#define EXPR_TOKENONLYSIZE      offsetof(Expr,pLeft)   /* Fewer features */

    // We don't use these in C#, but define them anyway,
    const int EXPR_FULLSIZE = 48;
    const int EXPR_REDUCEDSIZE = 8216;
    const int EXPR_TOKENONLYSIZE = 16392;

    /*
    ** Flags passed to the sqlite3ExprDup() function. See the header comment
    ** above sqlite3ExprDup() for details.
    */
    //#define EXPRDUP_REDUCE         0x0001  /* Used reduced-size Expr nodes */
    const int EXPRDUP_REDUCE = 0x0001;

    /*
    ** A list of expressions.  Each expression may optionally have a
    ** name.  An expr/name combination can be used in several ways, such
    ** as the list of "expr AS ID" fields following a "SELECT" or in the
    ** list of "ID = expr" items in an UPDATE.  A list of expressions can
    ** also be used as the argument to a function, in which case the a.zName
    ** field is not used.
    */
    public class ExprList_item
    {
      public Expr pExpr;          /* The list of expressions */
      public string zName;        /* Token associated with this expression */
      public string zSpan;        /*  Original text of the expression */
      public u8 sortOrder;        /* 1 for DESC or 0 for ASC */
      public u8 done;             /* A flag to indicate when processing is finished */
      public u16 iCol;            /* For ORDER BY, column number in result set */
      public u16 iAlias;          /* Index into Parse.aAlias[] for zName */
    }
    public class ExprList
    {
      public int nExpr;             /* Number of expressions on the list */
      public int nAlloc;            /* Number of entries allocated below */
      public int iECursor;          /* VDBE VdbeCursor associated with this ExprList */
      public ExprList_item[] a;     /* One entry for each expression */

      public ExprList Copy()
      {
        if ( this == null )
          return null;
        else
        {
          ExprList cp = (ExprList)MemberwiseClone();
          a.CopyTo( cp.a, 0 );
          return cp;
        }
      }

    };

    /*
    ** An instance of this structure is used by the parser to record both
    ** the parse tree for an expression and the span of input text for an
    ** expression.
    */
    public class ExprSpan
    {
      public Expr pExpr;            /* The expression parse tree */
      public string zStart;  /* First character of input text */
      public string zEnd;    /* One character past the end of input text */
    };

    /*
    ** An instance of this structure can hold a simple list of identifiers,
    ** such as the list "a,b,c" in the following statements:
    **
    **      INSERT INTO t(a,b,c) VALUES ...;
    **      CREATE INDEX idx ON t(a,b,c);
    **      CREATE TRIGGER trig BEFORE UPDATE ON t(a,b,c) ...;
    **
    ** The IdList.a.idx field is used when the IdList represents the list of
    ** column names after a table name in an INSERT statement.  In the statement
    **
    **     INSERT INTO t(a,b,c) ...
    **
    ** If "a" is the k-th column of table "t", then IdList.a[0].idx==k.
    */
    public class IdList_item
    {
      public string zName;      /* Name of the identifier */
      public int idx;          /* Index in some Table.aCol[] of a column named zName */
    }
    public class IdList
    {
      public IdList_item[] a;
      public int nId;         /* Number of identifiers on the list */
      public int nAlloc;      /* Number of entries allocated for a[] below */

      public IdList Copy()
      {
        if ( this == null )
          return null;
        else
        {
          IdList cp = (IdList)MemberwiseClone();
          a.CopyTo( cp.a, 0 );
          return cp;
        }
      }
    };

    /*
    ** The bitmask datatype defined below is used for various optimizations.
    **
    ** Changing this from a 64-bit to a 32-bit type limits the number of
    ** tables in a join to 32 instead of 64.  But it also reduces the size
    ** of the library by 738 bytes on ix86.
    */
    //typedef u64 Bitmask;

    /*
    ** The number of bits in a Bitmask.  "BMS" means "BitMask Size".
    */
    //#define BMS  ((int)(sizeof(Bitmask)*8))
    const int BMS = ( (int)( sizeof( Bitmask ) * 8 ) );


    /*
    ** The following structure describes the FROM clause of a SELECT statement.
    ** Each table or subquery in the FROM clause is a separate element of
    ** the SrcList.a[] array.
    **
    ** With the addition of multiple database support, the following structure
    ** can also be used to describe a particular table such as the table that
    ** is modified by an INSERT, DELETE, or UPDATE statement.  In standard SQL,
    ** such a table must be a simple name: ID.  But in SQLite, the table can
    ** now be identified by a database name, a dot, then the table name: ID.ID.
    **
    ** The jointype starts out showing the join type between the current table
    ** and the next table on the list.  The parser builds the list this way.
    ** But sqlite3SrcListShiftJoinType() later shifts the jointypes so that each
    ** jointype expresses the join between the table and the previous table.
    */
    public class SrcList_item
    {
      public string zDatabase; /* Name of database holding this table */
      public string zName;     /* Name of the table */
      public string zAlias;    /* The "B" part of a "A AS B" phrase.  zName is the "A" */
      public Table pTab;       /* An SQL table corresponding to zName */
      public Select pSelect;   /* A SELECT statement used in place of a table name */
      public u8 isPopulated;   /* Temporary table associated with SELECT is populated */
      public u8 jointype;      /* Type of join between this able and the previous */
      public u8 notIndexed;    /* True if there is a NOT INDEXED clause */
      public int iCursor;      /* The VDBE cursor number used to access this table */
      public Expr pOn;         /* The ON clause of a join */
      public IdList pUsing;    /* The USING clause of a join */
      public Bitmask colUsed;  /* Bit N (1<<N) set if column N of pTab is used */
      public string zIndex;    /* Identifier from "INDEXED BY <zIndex>" clause */
      public Index pIndex;     /* Index structure corresponding to zIndex, if any */
    }
    public class SrcList
    {
      public i16 nSrc;        /* Number of tables or subqueries in the FROM clause */
      public i16 nAlloc;      /* Number of entries allocated in a[] below */
      public SrcList_item[] a;/* One entry for each identifier on the list */
      public SrcList Copy()
      {
        if ( this == null )
          return null;
        else
        {
          SrcList cp = (SrcList)MemberwiseClone();
          if ( a != null ) a.CopyTo( cp.a, 0 );
          return cp;
        }
      }
    };

    /*
    ** Permitted values of the SrcList.a.jointype field
    */
    const int JT_INNER = 0x0001;   //#define JT_INNER     0x0001    /* Any kind of inner or cross join */
    const int JT_CROSS = 0x0002;   //#define JT_CROSS     0x0002    /* Explicit use of the CROSS keyword */
    const int JT_NATURAL = 0x0004; //#define JT_NATURAL   0x0004    /* True for a "natural" join */
    const int JT_LEFT = 0x0008;    //#define JT_LEFT      0x0008    /* Left outer join */
    const int JT_RIGHT = 0x0010;   //#define JT_RIGHT     0x0010    /* Right outer join */
    const int JT_OUTER = 0x0020;   //#define JT_OUTER     0x0020    /* The "OUTER" keyword is present */
    const int JT_ERROR = 0x0040;   //#define JT_ERROR     0x0040    /* unknown or unsupported join type */


    /*
    ** A WherePlan object holds information that describes a lookup
    ** strategy.
    **
    ** This object is intended to be opaque outside of the where.c module.
    ** It is included here only so that that compiler will know how big it
    ** is.  None of the fields in this object should be used outside of
    ** the where.c module.
    **
    ** Within the union, pIdx is only used when wsFlags&WHERE_INDEXED is true.
    ** pTerm is only used when wsFlags&WHERE_MULTI_OR is true.  And pVtabIdx
    ** is only used when wsFlags&WHERE_VIRTUALTABLE is true.  It is never the
    ** case that more than one of these conditions is true.
    */
    public class WherePlan
    {
      public u32 wsFlags;                   /* WHERE_* flags that describe the strategy */
      public u32 nEq;                       /* Number of == constraints */
      public class _u
      {
        public Index pIdx;                  /* Index when WHERE_INDEXED is true */
        public WhereTerm pTerm;             /* WHERE clause term for OR-search */
        public sqlite3_index_info pVtabIdx; /* Virtual table index to use */
      }
      public _u u = new _u();
    };

    /*
    ** For each nested loop in a WHERE clause implementation, the WhereInfo
    ** structure contains a single instance of this structure.  This structure
    ** is intended to be private the the where.c module and should not be
    ** access or modified by other modules.
    **
    ** The pIdxInfo field is used to help pick the best index on a
    ** virtual table.  The pIdxInfo pointer contains indexing
    ** information for the i-th table in the FROM clause before reordering.
    ** All the pIdxInfo pointers are freed by whereInfoFree() in where.c.
    ** All other information in the i-th WhereLevel object for the i-th table
    ** after FROM clause ordering.
    */
    public class InLoop
    {
      public int iCur;              /* The VDBE cursor used by this IN operator */
      public int addrInTop;         /* Top of the IN loop */
    }
    public class WhereLevel
    {
      public WherePlan plan;       /* query plan for this element of the FROM clause */
      public int iLeftJoin;        /* Memory cell used to implement LEFT OUTER JOIN */
      public int iTabCur;          /* The VDBE cursor used to access the table */
      public int iIdxCur;          /* The VDBE cursor used to access pIdx */
      public int addrBrk;          /* Jump here to break out of the loop */
      public int addrNxt;          /* Jump here to start the next IN combination */
      public int addrCont;         /* Jump here to continue with the next loop cycle */
      public int addrFirst;        /* First instruction of interior of the loop */
      public u8 iFrom;             /* Which entry in the FROM clause */
      public u8 op, p5;            /* Opcode and P5 of the opcode that ends the loop */
      public int p1, p2;           /* Operands of the opcode used to ends the loop */
      public class _u
      {
        public class __in               /* Information that depends on plan.wsFlags */
        {
          public int nIn;              /* Number of entries in aInLoop[] */
          public InLoop[] aInLoop;           /* Information about each nested IN operator */
        }
        public __in _in = new __in();                 /* Used when plan.wsFlags&WHERE_IN_ABLE */
      }
      public _u u = new _u();


      /* The following field is really not part of the current level.  But
      ** we need a place to cache virtual table index information for each
      ** virtual table in the FROM clause and the WhereLevel structure is
      ** a convenient place since there is one WhereLevel for each FROM clause
      ** element.
      */
      public sqlite3_index_info pIdxInfo;  /* Index info for n-th source table */
    };

    /*
    ** Flags appropriate for the wctrlFlags parameter of sqlite3WhereBegin()
    ** and the WhereInfo.wctrlFlags member.
    */
    //#define WHERE_ORDERBY_NORMAL   0x0000 /* No-op */
    //#define WHERE_ORDERBY_MIN      0x0001 /* ORDER BY processing for min() func */
    //#define WHERE_ORDERBY_MAX      0x0002 /* ORDER BY processing for max() func */
    //#define WHERE_ONEPASS_DESIRED  0x0004 /* Want to do one-pass UPDATE/DELETE */
    //#define WHERE_DUPLICATES_OK    0x0008 /* Ok to return a row more than once */
    //#define WHERE_OMIT_OPEN        0x0010  /* Table cursor are already open */
    //#define WHERE_OMIT_CLOSE       0x0020  /* Omit close of table & index cursors */
    //#define WHERE_FORCE_TABLE      0x0040 /* Do not use an index-only search */
    const int WHERE_ORDERBY_NORMAL = 0x0000;
    const int WHERE_ORDERBY_MIN = 0x0001;
    const int WHERE_ORDERBY_MAX = 0x0002;
    const int WHERE_ONEPASS_DESIRED = 0x0004;
    const int WHERE_DUPLICATES_OK = 0x0008;
    const int WHERE_OMIT_OPEN = 0x0010;
    const int WHERE_OMIT_CLOSE = 0x0020;
    const int WHERE_FORCE_TABLE = 0x0040;

    /*
    ** The WHERE clause processing routine has two halves.  The
    ** first part does the start of the WHERE loop and the second
    ** half does the tail of the WHERE loop.  An instance of
    ** this structure is returned by the first half and passed
    ** into the second half to give some continuity.
    */
    public class WhereInfo
    {
      public Parse pParse;          /* Parsing and code generating context */
      public u16 wctrlFlags;        /* Flags originally passed to sqlite3WhereBegin() */
      public u8 okOnePass;          /* Ok to use one-pass algorithm for UPDATE or DELETE */
      public SrcList pTabList;      /* List of tables in the join */
      public int iTop;              /* The very beginning of the WHERE loop */
      public int iContinue;         /* Jump here to continue with next record */
      public int iBreak;            /* Jump here to break out of the loop */
      public int nLevel;            /* Number of nested loop */
      public WhereClause pWC;       /* Decomposition of the WHERE clause */
      public WhereLevel[] a = new WhereLevel[] { new WhereLevel() };     /* Information about each nest loop in the WHERE */
    };

    /*
    ** A NameContext defines a context in which to resolve table and column
    ** names.  The context consists of a list of tables (the pSrcList) field and
    ** a list of named expression (pEList).  The named expression list may
    ** be NULL.  The pSrc corresponds to the FROM clause of a SELECT or
    ** to the table being operated on by INSERT, UPDATE, or DELETE.  The
    ** pEList corresponds to the result set of a SELECT and is NULL for
    ** other statements.
    **
    ** NameContexts can be nested.  When resolving names, the inner-most
    ** context is searched first.  If no match is found, the next outer
    ** context is checked.  If there is still no match, the next context
    ** is checked.  This process continues until either a match is found
    ** or all contexts are check.  When a match is found, the nRef member of
    ** the context containing the match is incremented.
    **
    ** Each subquery gets a new NameContext.  The pNext field points to the
    ** NameContext in the parent query.  Thus the process of scanning the
    ** NameContext list corresponds to searching through successively outer
    ** subqueries looking for a match.
    */
    public class NameContext
    {
      public Parse pParse;       /* The parser */
      public SrcList pSrcList;   /* One or more tables used to resolve names */
      public ExprList pEList;    /* Optional list of named expressions */
      public int nRef;           /* Number of names resolved by this context */
      public int nErr;           /* Number of errors encountered while resolving names */
      public u8 allowAgg;        /* Aggregate functions allowed here */
      public u8 hasAgg;          /* True if aggregates are seen */
      public u8 isCheck;         /* True if resolving names in a CHECK constraint */
      public int nDepth;         /* Depth of subquery recursion. 1 for no recursion */
      public AggInfo pAggInfo;   /* Information about aggregates at this level */
      public NameContext pNext;  /* Next outer name context.  NULL for outermost */
    };

    /*
    ** An instance of the following structure contains all information
    ** needed to generate code for a single SELECT statement.
    **
    ** nLimit is set to -1 if there is no LIMIT clause.  nOffset is set to 0.
    ** If there is a LIMIT clause, the parser sets nLimit to the value of the
    ** limit and nOffset to the value of the offset (or 0 if there is not
    ** offset).  But later on, nLimit and nOffset become the memory locations
    ** in the VDBE that record the limit and offset counters.
    **
    ** addrOpenEphm[] entries contain the address of OP_OpenEphemeral opcodes.
    ** These addresses must be stored so that we can go back and fill in
    ** the P4_KEYINFO and P2 parameters later.  Neither the KeyInfo nor
    ** the number of columns in P2 can be computed at the same time
    ** as the OP_OpenEphm instruction is coded because not
    ** enough information about the compound query is known at that point.
    ** The KeyInfo for addrOpenTran[0] and [1] contains collating sequences
    ** for the result set.  The KeyInfo for addrOpenTran[2] contains collating
    ** sequences for the ORDER BY clause.
    */
    public class Select
    {
      public ExprList pEList;      /* The fields of the result */
      public u8 op;                /* One of: TK_UNION TK_ALL TK_INTERSECT TK_EXCEPT */
      public char affinity;        /* MakeRecord with this affinity for SRT_Set */
      public u16 selFlags;         /* Various SF_* values */
      public SrcList pSrc;         /* The FROM clause */
      public Expr pWhere;          /* The WHERE clause */
      public ExprList pGroupBy;    /* The GROUP BY clause */
      public Expr pHaving;         /* The HAVING clause */
      public ExprList pOrderBy;    /* The ORDER BY clause */
      public Select pPrior;        /* Prior select in a compound select statement */
      public Select pNext;         /* Next select to the left in a compound */
      public Select pRightmost;    /* Right-most select in a compound select statement */
      public Expr pLimit;          /* LIMIT expression. NULL means not used. */
      public Expr pOffset;         /* OFFSET expression. NULL means not used. */
      public int iLimit;
      public int iOffset;          /* Memory registers holding LIMIT & OFFSET counters */
      public int[] addrOpenEphm = new int[3];   /* OP_OpenEphem opcodes related to this select */

      public Select Copy()
      {
        if ( this == null )
          return null;
        else
        {
          Select cp = (Select)MemberwiseClone();
          if ( pEList != null ) cp.pEList = pEList.Copy();
          if ( pSrc != null ) cp.pSrc = pSrc.Copy();
          if ( pWhere != null ) cp.pWhere = pWhere.Copy();
          if ( pGroupBy != null ) cp.pGroupBy = pGroupBy.Copy();
          if ( pHaving != null ) cp.pHaving = pHaving.Copy();
          if ( pOrderBy != null ) cp.pOrderBy = pOrderBy.Copy();
          if ( pPrior != null ) cp.pPrior = pPrior.Copy();
          if ( pNext != null ) cp.pNext = pNext.Copy();
          if ( pRightmost != null ) cp.pRightmost = pRightmost.Copy();
          if ( pLimit != null ) cp.pLimit = pLimit.Copy();
          if ( pOffset != null ) cp.pOffset = pOffset.Copy();
          return cp;
        }
      }
    };

    /*
    ** Allowed values for Select.selFlags.  The "SF" prefix stands for
    ** "Select Flag".
    */
    //#define SF_Distinct        0x0001  /* Output should be DISTINCT */
    //#define SF_Resolved        0x0002  /* Identifiers have been resolved */
    //#define SF_Aggregate       0x0004  /* Contains aggregate functions */
    //#define SF_UsesEphemeral   0x0008  /* Uses the OpenEphemeral opcode */
    //#define SF_Expanded        0x0010  /* sqlite3SelectExpand() called on this */
    //#define SF_HasTypeInfo     0x0020  /* FROM subqueries have Table metadata */
    const int SF_Distinct = 0x0001;  /* Output should be DISTINCT */
    const int SF_Resolved = 0x0002;  /* Identifiers have been resolved */
    const int SF_Aggregate = 0x0004;  /* Contains aggregate functions */
    const int SF_UsesEphemeral = 0x0008;  /* Uses the OpenEphemeral opcode */
    const int SF_Expanded = 0x0010;  /* sqlite3SelectExpand() called on this */
    const int SF_HasTypeInfo = 0x0020;  /* FROM subqueries have Table metadata */


    /*
    ** The results of a select can be distributed in several ways.  The
    ** "SRT" prefix means "SELECT Result Type".
    */
    const int SRT_Union = 1;//#define SRT_Union        1  /* Store result as keys in an index */
    const int SRT_Except = 2;//#define SRT_Except      2  /* Remove result from a UNION index */
    const int SRT_Exists = 3;//#define SRT_Exists      3  /* Store 1 if the result is not empty */
    const int SRT_Discard = 4;//#define SRT_Discard    4  /* Do not save the results anywhere */

    /* The ORDER BY clause is ignored for all of the above */
    //#define IgnorableOrderby(X) ((X->eDest)<=SRT_Discard)

    const int SRT_Output = 5;//#define SRT_Output      5  /* Output each row of result */
    const int SRT_Mem = 6;//#define SRT_Mem            6  /* Store result in a memory cell */
    const int SRT_Set = 7;//#define SRT_Set            7  /* Store results as keys in an index */
    const int SRT_Table = 8;//#define SRT_Table        8  /* Store result as data with an automatic rowid */
    const int SRT_EphemTab = 9;//#define SRT_EphemTab  9  /* Create transient tab and store like SRT_Table /
    const int SRT_Coroutine = 10;//#define SRT_Coroutine   10  /* Generate a single row of result */

    /*
    ** A structure used to customize the behavior of sqlite3Select(). See
    ** comments above sqlite3Select() for details.
    */
    //typedef struct SelectDest SelectDest;
    public class SelectDest
    {
      public u8 eDest;        /* How to dispose of the results */
      public char affinity;    /* Affinity used when eDest==SRT_Set */
      public int iParm;        /* A parameter used by the eDest disposal method */
      public int iMem;         /* Base register where results are written */
      public int nMem;         /* Number of registers allocated */
      public SelectDest()
      {
        this.eDest = 0;
        this.affinity = '\0';
        this.iParm = 0;
        this.iMem = 0;
        this.nMem = 0;
      }
      public SelectDest( u8 eDest, char affinity, int iParm )
      {
        this.eDest = eDest;
        this.affinity = affinity;
        this.iParm = iParm;
        this.iMem = 0;
        this.nMem = 0;
      }
      public SelectDest( u8 eDest, char affinity, int iParm, int iMem, int nMem )
      {
        this.eDest = eDest;
        this.affinity = affinity;
        this.iParm = iParm;
        this.iMem = iMem;
        this.nMem = nMem;
      }
    };

    /*
    ** During code generation of statements that do inserts into AUTOINCREMENT
    ** tables, the following information is attached to the Table.u.autoInc.p
    ** pointer of each autoincrement table to record some side information that
    ** the code generator needs.  We have to keep per-table autoincrement
    ** information in case inserts are down within triggers.  Triggers do not
    ** normally coordinate their activities, but we do need to coordinate the
    ** loading and saving of autoincrement information.
    */
    public class AutoincInfo
    {
      public AutoincInfo pNext;    /* Next info block in a list of them all */
      public Table pTab;           /* Table this info block refers to */
      public int iDb;              /* Index in sqlite3.aDb[] of database holding pTab */
      public int regCtr;           /* Memory register holding the rowid counter */
    };

    /*
    ** Size of the column cache
    */
#if !SQLITE_N_COLCACHE
    //# define SQLITE_N_COLCACHE 10
    const int SQLITE_N_COLCACHE = 10;
#endif

    /*
** An SQL parser context.  A copy of this structure is passed through
** the parser and down into all the parser action routine in order to
** carry around information that is global to the entire parse.
**
** The structure is divided into two parts.  When the parser and code
** generate call themselves recursively, the first part of the structure
** is constant but the second part is reset at the beginning and end of
** each recursion.
**
** The nTableLock and aTableLock variables are only used if the shared-cache
** feature is enabled (if sqlite3Tsd()->useSharedData is true). They are
** used to store the set of table-locks required by the statement being
** compiled. Function sqlite3TableLock() is used to add entries to the
** list.
*/
    public class yColCache
    {
      public int iTable;           /* Table cursor number */
      public int iColumn;          /* Table column number */
      public bool affChange;       /* True if this register has had an affinity change */
      public u8 tempReg;           /* iReg is a temp register that needs to be freed */
      public int iLevel;           /* Nesting level */
      public int iReg;             /* Reg with value of this column. 0 means none. */
      public int lru;              /* Least recently used entry has the smallest value */
    }
    public class Parse
    {
      public sqlite3 db;          /* The main database structure */
      public int rc;              /* Return code from execution */
      public string zErrMsg;      /* An error message */
      public Vdbe pVdbe;          /* An engine for executing database bytecode */
      public u8 colNamesSet;      /* TRUE after OP_ColumnName has been issued to pVdbe */
      public u8 nameClash;        /* A permanent table name clashes with temp table name */
      public u8 checkSchema;      /* Causes schema cookie check after an error */
      public u8 nested;           /* Number of nested calls to the parser/code generator */
      public u8 parseError;       /* True after a parsing error.  Ticket #1794 */
      public u8 nTempReg;         /* Number of temporary registers in aTempReg[] */
      public u8 nTempInUse;       /* Number of aTempReg[] currently checked out */
      public int[] aTempReg = new int[8];     /* Holding area for temporary registers */
      public int nRangeReg;       /* Size of the temporary register block */
      public int iRangeReg;       /* First register in temporary register block */
      public int nErr;            /* Number of errors seen */
      public int nTab;            /* Number of previously allocated VDBE cursors */
      public int nMem;            /* Number of memory cells used so far */
      public int nSet;            /* Number of sets used so far */
      public int ckBase;          /* Base register of data during check constraints */
      public int iCacheLevel;     /* ColCache valid when aColCache[].iLevel<=iCacheLevel */
      public int iCacheCnt;       /* Counter used to generate aColCache[].lru values */
      public u8 nColCache;        /* Number of entries in the column cache */
      public u8 iColCache;        /* Next entry of the cache to replace */
      public yColCache[] aColCache = new yColCache[SQLITE_N_COLCACHE];     /* One for each valid column cache entry */
      public u32 writeMask;       /* Start a write transaction on these databases */
      public u32 cookieMask;      /* Bitmask of schema verified databases */
      public int cookieGoto;      /* Address of OP_Goto to cookie verifier subroutine */
      public int[] cookieValue = new int[SQLITE_MAX_ATTACHED + 2];  /* Values of cookies to verify */
#if !SQLITE_OMIT_SHARED_CACHE
public int nTableLock;         /* Number of locks in aTableLock */
public TableLock[] aTableLock; /* Required table locks for shared-cache mode */
#endif
      public int regRowid;           /* Register holding rowid of CREATE TABLE entry */
      public int regRoot;            /* Register holding root page number for new objects */
      public AutoincInfo pAinc;      /* Information about AUTOINCREMENT counters */

      /* Above is constant between recursions.  Below is reset before and after
      ** each recursion */

      public int nVar;                       /* Number of '?' variables seen in the SQL so far */
      public int nVarExpr;                   /* Number of used slots in apVarExpr[] */
      public int nVarExprAlloc;              /* Number of allocated slots in apVarExpr[] */
      public Expr[] apVarExpr;               /* Pointers to :aaa and $aaaa wildcard expressions */
      public int nAlias;                     /* Number of aliased result set columns */
      public int nAliasAlloc;                /* Number of allocated slots for aAlias[] */
      public int[] aAlias;                   /* Register used to hold aliased result */
      public u8 explain;                     /* True if the EXPLAIN flag is found on the query */
      public Token sNameToken;               /* Token with unqualified schema object name */
      public Token sLastToken = new Token(); /* The last token parsed */
      public StringBuilder zTail;            /* All SQL text past the last semicolon parsed */
      public Table pNewTable;                /* A table being constructed by CREATE TABLE */
      public Trigger pNewTrigger;            /* Trigger under construct by a CREATE TRIGGER */
      public TriggerStack trigStack;         /* Trigger actions being coded */
      public string zAuthContext;            /* The 6th parameter to db.xAuth callbacks */
#if !SQLITE_OMIT_VIRTUALTABLE
public Token sArg;                /* Complete text of a module argument */
public u8 declareVtab;            /* True if inside sqlite3_declare_vtab() */
public int nVtabLock;             /* Number of virtual tables to lock */
public Table[] apVtabLock;        /* Pointer to virtual tables needing locking */
#endif
      public int nHeight;             /* Expression tree height of current sub-select */
      public Table pZombieTab;        /* List of Table objects to delete after code gen */

      // We need to create instances of the col cache
      public Parse()
      {
        for ( int i = 0 ; i < this.aColCache.Length ; i++ ) { this.aColCache[i] = new yColCache(); }
      }

      public void ResetMembers() // Need to clear all the following variables during each recursion
      {
        nVar = 0;
        nVarExpr = 0;
        nVarExprAlloc = 0;
        apVarExpr = null;
        nAlias = 0;
        nAliasAlloc = 0;
        aAlias = null;
        explain = 0;
        sNameToken = new Token();
        sLastToken = new Token();
        zTail.Length = 0;
        pNewTable = null;
        pNewTrigger = null;
        trigStack = null;
        zAuthContext = null;
#if !SQLITE_OMIT_VIRTUALTABLE
sArg = new Token();
declareVtab = 0;
nVtabLock = 0;
apVtabLoc = null;
#endif
        nHeight = 0;
        pZombieTab = null;
      }
      Parse[] SaveBuf = new Parse[10];  //For Recursion Storage
      public void RestoreMembers()  // Need to clear all the following variables during each recursion
      {
        if ( SaveBuf[nested] != null )
          nVar = SaveBuf[nested].nVar;
        nVarExpr = SaveBuf[nested].nVarExpr;
        nVarExprAlloc = SaveBuf[nested].nVarExprAlloc;
        apVarExpr = SaveBuf[nested].apVarExpr;
        nAlias = SaveBuf[nested].nAlias;
        nAliasAlloc = SaveBuf[nested].nAliasAlloc;
        aAlias = SaveBuf[nested].aAlias;
        explain = SaveBuf[nested].explain;
        sNameToken = SaveBuf[nested].sNameToken;
        sLastToken = SaveBuf[nested].sLastToken;
        zTail = SaveBuf[nested].zTail;
        pNewTable = SaveBuf[nested].pNewTable;
        pNewTrigger = SaveBuf[nested].pNewTrigger;
        trigStack = SaveBuf[nested].trigStack;
        zAuthContext = SaveBuf[nested].zAuthContext;
#if !SQLITE_OMIT_VIRTUALTABLE
sArg = SaveBuf[nested].sArg              ;
declareVtab = SaveBuf[nested].declareVtab;
nVtabLock = SaveBuf[nested].nVtabLock;
apVtabLock = SaveBuf[nested].apVtabLock;
#endif
        nHeight = SaveBuf[nested].nHeight;
        pZombieTab = SaveBuf[nested].pZombieTab;
        SaveBuf[nested] = null;
      }
      public void SaveMembers() // Need to clear all the following variables during each recursion
      {
        SaveBuf[nested] = new Parse();
        SaveBuf[nested].nVar = nVar;
        SaveBuf[nested].nVarExpr = nVarExpr;
        SaveBuf[nested].nVarExprAlloc = nVarExprAlloc;
        SaveBuf[nested].apVarExpr = apVarExpr;
        SaveBuf[nested].nAlias = nAlias;
        SaveBuf[nested].nAliasAlloc = nAliasAlloc;
        SaveBuf[nested].aAlias = aAlias;
        SaveBuf[nested].explain = explain;
        SaveBuf[nested].sNameToken = sNameToken;
        SaveBuf[nested].sLastToken = sLastToken;
        SaveBuf[nested].zTail = zTail;
        SaveBuf[nested].pNewTable = pNewTable;
        SaveBuf[nested].pNewTrigger = pNewTrigger;
        SaveBuf[nested].trigStack = trigStack;
        SaveBuf[nested].zAuthContext = zAuthContext;
#if !SQLITE_OMIT_VIRTUALTABLE
SaveBuf[nested].sArg = sArg             ;
SaveBuf[nested].declareVtab = declareVtab;
SaveBuf[nested].nVtabLock = nVtabLock   ;
SaveBuf[nested].apVtabLock = apVtabLock ;
#endif
        SaveBuf[nested].nHeight = nHeight;
        SaveBuf[nested].pZombieTab = pZombieTab;
      }
    };

#if SQLITE_OMIT_VIRTUALTABLE
    static bool IN_DECLARE_VTAB = false;//#define IN_DECLARE_VTAB 0
#else
//  int ;//#define IN_DECLARE_VTAB (pParse.declareVtab)
#endif

    /*
** An instance of the following structure can be declared on a stack and used
** to save the Parse.zAuthContext value so that it can be restored later.
*/
    public class AuthContext
    {
      public string zAuthContext;   /* Put saved Parse.zAuthContext here */
      public Parse pParse;              /* The Parse structure */
    };

    /*
    ** Bitfield flags for P5 value in OP_Insert and OP_Delete
    */
    //#define OPFLAG_NCHANGE   1    /* Set to update db->nChange */
    //#define OPFLAG_LASTROWID 2    /* Set to update db->lastRowid */
    //#define OPFLAG_ISUPDATE  4    /* This OP_Insert is an sql UPDATE */
    //#define OPFLAG_APPEND    8    /* This is likely to be an append */
    //#define OPFLAG_USESEEKRESULT 16    /* Try to avoid a seek in BtreeInsert() */
    const byte OPFLAG_NCHANGE = 1;
    const byte OPFLAG_LASTROWID = 2;
    const byte OPFLAG_ISUPDATE = 4;
    const byte OPFLAG_APPEND = 8;
    const byte OPFLAG_USESEEKRESULT = 16;

    /*
    * Each trigger present in the database schema is stored as an instance of
    * struct Trigger.
    *
    * Pointers to instances of struct Trigger are stored in two ways.
    * 1. In the "trigHash" hash table (part of the sqlite3* that represents the
    *    database). This allows Trigger structures to be retrieved by name.
    * 2. All triggers associated with a single table form a linked list, using the
    *    pNext member of struct Trigger. A pointer to the first element of the
    *    linked list is stored as the "pTrigger" member of the associated
    *    struct Table.
    *
    * The "step_list" member points to the first element of a linked list
    * containing the SQL statements specified as the trigger program.
    */
    public class Trigger
    {
      public string name;             /* The name of the trigger                        */
      public string table;            /* The table or view to which the trigger applies */
      public u8 op;                   /* One of TK_DELETE, TK_UPDATE, TK_INSERT         */
      public u8 tr_tm;                /* One of TRIGGER_BEFORE, TRIGGER_AFTER */
      public Expr pWhen;              /* The WHEN clause of the expression (may be NULL) */
      public IdList pColumns;         /* If this is an UPDATE OF <column-list> trigger,
the <column-list> is stored here */
      public Schema pSchema;          /* Schema containing the trigger */
      public Schema pTabSchema;       /* Schema containing the table */
      public TriggerStep step_list;   /* Link list of trigger program steps             */
      public Trigger pNext;           /* Next trigger associated with the table */

      public Trigger Copy()
      {
        if ( this == null )
          return null;
        else
        {
          Trigger cp = (Trigger)MemberwiseClone();
          if ( pWhen != null ) cp.pWhen = pWhen.Copy();
          if ( pColumns != null ) cp.pColumns = pColumns.Copy();
          if ( pSchema != null ) cp.pSchema = pSchema.Copy();
          if ( pTabSchema != null ) cp.pTabSchema = pTabSchema.Copy();
          if ( step_list != null ) cp.step_list = step_list.Copy();
          if ( pNext != null ) cp.pNext = pNext.Copy();
          return cp;
        }
      }
    };

    /*
    ** A trigger is either a BEFORE or an AFTER trigger.  The following constants
    ** determine which.
    **
    ** If there are multiple triggers, you might of some BEFORE and some AFTER.
    ** In that cases, the constants below can be ORed together.
    */
    const u8 TRIGGER_BEFORE = 1;//#define TRIGGER_BEFORE  1
    const u8 TRIGGER_AFTER = 2;//#define TRIGGER_AFTER   2

    /*
    * An instance of struct TriggerStep is used to store a single SQL statement
    * that is a part of a trigger-program.
    *
    * Instances of struct TriggerStep are stored in a singly linked list (linked
    * using the "pNext" member) referenced by the "step_list" member of the
    * associated struct Trigger instance. The first element of the linked list is
    * the first step of the trigger-program.
    *
    * The "op" member indicates whether this is a "DELETE", "INSERT", "UPDATE" or
    * "SELECT" statement. The meanings of the other members is determined by the
    * value of "op" as follows:
    *
    * (op == TK_INSERT)
    * orconf    -> stores the ON CONFLICT algorithm
    * pSelect   -> If this is an INSERT INTO ... SELECT ... statement, then
    *              this stores a pointer to the SELECT statement. Otherwise NULL.
    * target    -> A token holding the quoted name of the table to insert into.
    * pExprList -> If this is an INSERT INTO ... VALUES ... statement, then
    *              this stores values to be inserted. Otherwise NULL.
    * pIdList   -> If this is an INSERT INTO ... (<column-names>) VALUES ...
    *              statement, then this stores the column-names to be
    *              inserted into.
    *
    * (op == TK_DELETE)
    * target    -> A token holding the quoted name of the table to delete from.
    * pWhere    -> The WHERE clause of the DELETE statement if one is specified.
    *              Otherwise NULL.
    *
    * (op == TK_UPDATE)
    * target    -> A token holding the quoted name of the table to update rows of.
    * pWhere    -> The WHERE clause of the UPDATE statement if one is specified.
    *              Otherwise NULL.
    * pExprList -> A list of the columns to update and the expressions to update
    *              them to. See sqlite3Update() documentation of "pChanges"
    *              argument.
    *
    */
    public class TriggerStep
    {
      public u8 op;               /* One of TK_DELETE, TK_UPDATE, TK_INSERT, TK_SELECT */
      public u8 orconf;           /* OE_Rollback etc. */
      public Trigger pTrig;       /* The trigger that this step is a part of */
      public Select pSelect;      /* SELECT statment or RHS of INSERT INTO .. SELECT ... */
      public Token target;        /* Target table for DELETE, UPDATE, INSERT */
      public Expr pWhere;         /* The WHERE clause for DELETE or UPDATE steps */
      public ExprList pExprList;  /* SET clause for UPDATE.  VALUES clause for INSERT */
      public IdList pIdList;      /* Column names for INSERT */
      public TriggerStep pNext;   /* Next in the link-list */
      public TriggerStep pLast;   /* Last element in link-list. Valid for 1st elem only */

      public TriggerStep()
      {
        target = new Token();
      }
      public TriggerStep Copy()
      {
        if ( this == null )
          return null;
        else
        {
          TriggerStep cp = (TriggerStep)MemberwiseClone();
          return cp;
        }
      }
    };

    /*
    * An instance of struct TriggerStack stores information required during code
    * generation of a single trigger program. While the trigger program is being
    * coded, its associated TriggerStack instance is pointed to by the
    * "pTriggerStack" member of the Parse structure.
    *
    * The pTab member points to the table that triggers are being coded on. The
    * newIdx member contains the index of the vdbe cursor that points at the temp
    * table that stores the new.* references. If new.* references are not valid
    * for the trigger being coded (for example an ON DELETE trigger), then newIdx
    * is set to -1. The oldIdx member is analogous to newIdx, for old.* references.
    *
    * The ON CONFLICT policy to be used for the trigger program steps is stored
    * as the orconf member. If this is OE_Default, then the ON CONFLICT clause
    * specified for individual triggers steps is used.
    *
    * struct TriggerStack has a "pNext" member, to allow linked lists to be
    * constructed. When coding nested triggers (triggers fired by other triggers)
    * each nested trigger stores its parent trigger's TriggerStack as the "pNext"
    * pointer. Once the nested trigger has been coded, the pNext value is restored
    * to the pTriggerStack member of the Parse stucture and coding of the parent
    * trigger continues.
    *
    * Before a nested trigger is coded, the linked list pointed to by the
    * pTriggerStack is scanned to ensure that the trigger is not about to be coded
    * recursively. If this condition is detected, the nested trigger is not coded.
    */
    public class TriggerStack
    {
      public Table pTab;         /* Table that triggers are currently being coded on */
      public int newIdx;          /* Index of vdbe cursor to "new" temp table */
      public int oldIdx;          /* Index of vdbe cursor to "old" temp table */
      public u32 newColMask;
      public u32 oldColMask;
      public int orconf;          /* Current orconf policy */
      public int ignoreJump;      /* where to jump to for a RAISE(IGNORE) */
      public Trigger pTrigger;   /* The trigger currently being coded */
      public TriggerStack pNext; /* Next trigger down on the trigger stack */
    };

    /*
    ** The following structure contains information used by the sqliteFix...
    ** routines as they walk the parse tree to make database references
    ** explicit.
    */
    //typedef struct DbFixer DbFixer;
    public class DbFixer
    {
      public Parse pParse;       /* The parsing context.  Error messages written here */
      public string zDb;         /* Make sure all objects are contained in this database */
      public string zType;       /* Type of the container - used for error messages */
      public Token pName;        /* Name of the container - used for error messages */
    };

    /*
    ** An objected used to accumulate the text of a string where we
    ** do not necessarily know how big the string will be in the end.
    */
    public class StrAccum
    {
      public sqlite3 db;          /* Optional database for lookaside.  Can be NULL */
      public StringBuilder zBase = new StringBuilder();     /* A base allocation.  Not from malloc. */
      public StringBuilder zText = new StringBuilder();     /* The string collected so far */
      public int nChar;                                     /* Length of the string so far */
      public int nAlloc;                                    /* Amount of space allocated in zText */
      public int mxAlloc;         /* Maximum allowed string length */
      // Cannot happen under C#
      //public u8 mallocFailed;     /* Becomes true if any memory allocation fails */
      public u8 useMalloc;        /* True if zText is enlargeable using realloc */
      public u8 tooBig;           /* Becomes true if string size exceeds limits */
      public Mem Context;
    };

    /*
    ** A pointer to this structure is used to communicate information
    ** from sqlite3Init and OP_ParseSchema into the sqlite3InitCallback.
    */
    public class InitData
    {
      public sqlite3 db;        /* The database being initialized */
      public int iDb;            /* 0 for main database.  1 for TEMP, 2.. for ATTACHed */
      public string pzErrMsg;    /* Error message stored here */
      public int rc;             /* Result code stored here */
    }

    /*
    ** Structure containing global configuration data for the SQLite library.
    **
    ** This structure also contains some state information.
    */
    public class Sqlite3Config
    {
      public bool bMemstat;                    /* True to enable memory status */
      public bool bCoreMutex;                  /* True to enable core mutexing */
      public bool bFullMutex;                   /* True to enable full mutexing */
      public int mxStrlen;                     /* Maximum string length */
      public int szLookaside;                  /* Default lookaside buffer size */
      public int nLookaside;                   /* Default lookaside buffer count */
      public sqlite3_mem_methods m;            /* Low-level memory allocation interface */
      public sqlite3_mutex_methods mutex;      /* Low-level mutex interface */
      public sqlite3_pcache_methods pcache;    /* Low-level page-cache interface */
      public byte[] pHeap;                     /* Heap storage space */
      public int nHeap;                        /* Size of pHeap[] */
      public int mnReq, mxReq;                 /* Min and max heap requests sizes */
      public byte[] pScratch;                  /* Scratch memory */
      public int szScratch;                    /* Size of each scratch buffer */
      public int nScratch;                     /* Number of scratch buffers */
      public MemPage pPage;                    /* Page cache memory */
      public int szPage;                       /* Size of each page in pPage[] */
      public int nPage;                        /* Number of pages in pPage[] */
      public int mxParserStack;                /* maximum depth of the parser stack */
      public bool sharedCacheEnabled;           /* true if shared-cache mode enabled */
      /* The above might be initialized to non-zero.  The following need to always
      ** initially be zero, however. */
      public int isInit;                       /* True after initialization has finished */
      public int inProgress;                   /* True while initialization in progress */
      public int isMallocInit;                 /* True after malloc is initialized */
      public sqlite3_mutex pInitMutex;         /* Mutex used by sqlite3_initialize() */
      public int nRefInitMutex;                /* Number of users of pInitMutex */

      public Sqlite3Config( int bMemstat, int bCoreMutex, bool bFullMutex, int mxStrlen, int szLookaside, int nLookaside
      , sqlite3_mem_methods m
      , sqlite3_mutex_methods mutex
      , sqlite3_pcache_methods pcache
      , byte[] pHeap
      , int nHeap,
      int mnReq, int mxReq
      , byte[] pScratch
      , int szScratch
      , int nScratch
      , MemPage pPage
      , int szPage
      , int nPage
      , int mxParserStack
      , bool sharedCacheEnabled
      , int isInit
      , int inProgress
      , int isMallocInit
      , sqlite3_mutex pInitMutex
      , int nRefInitMutex
      )
      {
        this.bMemstat = bMemstat != 0;
        this.bCoreMutex = bCoreMutex != 0;
        this.bFullMutex = bFullMutex;
        this.mxStrlen = mxStrlen;
        this.szLookaside = szLookaside;
        this.nLookaside = nLookaside;
        this.m = m;
        this.mutex = mutex;
        this.pcache = pcache;
        this.pHeap = pHeap;
        this.nHeap = nHeap;
        this.mnReq = mnReq;
        this.mxReq = mxReq;
        this.pScratch = pScratch;
        this.szScratch = szScratch;
        this.nScratch = nScratch;
        this.pPage = pPage;
        this.szPage = szPage;
        this.nPage = nPage;
        this.mxParserStack = mxParserStack;
        this.sharedCacheEnabled = sharedCacheEnabled;
        this.isInit = isInit;
        this.inProgress = inProgress;
        this.isMallocInit = isMallocInit;
        this.pInitMutex = pInitMutex;
        this.nRefInitMutex = nRefInitMutex;
      }
    };

    /*
    ** Context pointer passed down through the tree-walk.
    */
    public class Walker
    {
      public dxExprCallback xExprCallback; //)(Walker*, Expr*);     /* Callback for expressions */
      public dxSelectCallback xSelectCallback; //)(Walker*,Select*);  /* Callback for SELECTs */
      public Parse pParse;                            /* Parser context.  */
      public struct uw
      {                              /* Extra data for callback */
        public NameContext pNC;                       /* Naming context */
        public int i;                                 /* Integer value */
      }
      public uw u;
    };

    /* Forward declarations */
    //int sqlite3WalkExpr(Walker*, Expr*);
    //int sqlite3WalkExprList(Walker*, ExprList*);
    //int sqlite3WalkSelect(Walker*, Select*);
    //int sqlite3WalkSelectExpr(Walker*, Select*);
    //int sqlite3WalkSelectFrom(Walker*, Select*);

    /*
    ** Return code from the parse-tree walking primitives and their
    ** callbacks.
    */
    //#define WRC_Continue    0   /* Continue down into children */
    //#define WRC_Prune       1   /* Omit children but continue walking siblings */
    //#define WRC_Abort       2   /* Abandon the tree walk */
    const int WRC_Continue = 0;
    const int WRC_Prune = 1;
    const int WRC_Abort = 2;


    /*
    ** Assuming zIn points to the first byte of a UTF-8 character,
    ** advance zIn to point to the first byte of the next UTF-8 character.
    */
    //#define SQLITE_SKIP_UTF8(zIn) {                        \
    //  if( (*(zIn++))>=0xc0 ){                              \
    //    while( (*zIn & 0xc0)==0x80 ){ zIn++; }             \
    //  }                                                    \
    //}
    static void SQLITE_SKIP_UTF8( string zIn, ref int iz )
    {
      iz++;
      if ( iz < zIn.Length && zIn[iz - 1] >= 0xC0 )
      {
        while ( iz < zIn.Length && ( zIn[iz] & 0xC0 ) == 0x80 ) { iz++; }
      }
    }
    static void SQLITE_SKIP_UTF8(
    byte[] zIn, ref int iz )
    {
      iz++;
      if ( iz < zIn.Length && zIn[iz - 1] >= 0xC0 )
      {
        while ( iz < zIn.Length && ( zIn[iz] & 0xC0 ) == 0x80 ) { iz++; }
      }
    }

    /*
    ** The SQLITE_CORRUPT_BKPT macro can be either a constant (for production
    ** builds) or a function call (for debugging).  If it is a function call,
    ** it allows the operator to set a breakpoint at the spot where database
    ** corruption is first detected.
    */
#if SQLITE_DEBUG || DEBUG
    static int SQLITE_CORRUPT_BKPT()
    {
       return sqlite3Corrupt();
    }
#else
//#define SQLITE_CORRUPT_BKPT SQLITE_CORRUPT
const int SQLITE_CORRUPT_BKPT = SQLITE_CORRUPT;
#endif

    /*
** The ctype.h header is needed for non-ASCII systems.  It is also
** needed by FTS3 when FTS3 is included in the amalgamation.
*/
    //#if !defined(SQLITE_ASCII) || \
    //    (defined(SQLITE_ENABLE_FTS3) && defined(SQLITE_AMALGAMATION))
    //# include <ctype.h>
    //#endif


    /*
    ** The following macros mimic the standard library functions toupper(),
    ** isspace(), isalnum(), isdigit() and isxdigit(), respectively. The
    ** sqlite versions only work for ASCII characters, regardless of locale.
    */
#if SQLITE_ASCII
    //# define sqlite3Toupper(x)  ((x)&~(sqlite3CtypeMap[(unsigned char)(x)]&0x20))

    //# define sqlite3Isspace(x)   (sqlite3CtypeMap[(unsigned char)(x)]&0x01)
    static bool sqlite3Isspace( byte x ) { return ( sqlite3CtypeMap[(byte)( x )] & 0x01 ) != 0; }
    static bool sqlite3Isspace( char x ) { return x < 256 && ( sqlite3CtypeMap[(byte)( x )] & 0x01 ) != 0; }

    //# define sqlite3Isalnum(x)   (sqlite3CtypeMap[(unsigned char)(x)]&0x06)
    static bool sqlite3Isalnum( byte x ) { return ( sqlite3CtypeMap[(byte)( x )] & 0x06 ) != 0; }
    static bool sqlite3Isalnum( char x ) { return x < 256 && ( sqlite3CtypeMap[(byte)( x )] & 0x06 ) != 0; }

    //# define sqlite3Isalpha(x)   (sqlite3CtypeMap[(unsigned char)(x)]&0x02)

    //# define sqlite3Isdigit(x)   (sqlite3CtypeMap[(unsigned char)(x)]&0x04)
    static bool sqlite3Isdigit( byte x ) { return ( sqlite3CtypeMap[( (byte)x )] & 0x04 ) != 0; }
    static bool sqlite3Isdigit( char x ) { return x < 256 && ( sqlite3CtypeMap[( (byte)x )] & 0x04 ) != 0; }

    //# define sqlite3Isxdigit(x)  (sqlite3CtypeMap[(unsigned char)(x)]&0x08)
    static bool sqlite3Isxdigit( byte x ) { return ( sqlite3CtypeMap[( (byte)x )] & 0x08 ) != 0; }
    static bool sqlite3Isxdigit( char x ) { return x < 256 && ( sqlite3CtypeMap[( (byte)x )] & 0x08 ) != 0; }

    //# define sqlite3Tolower(x)   (sqlite3UpperToLower[(unsigned char)(x)])
#else
//# define sqlite3Toupper(x)   toupper((unsigned char)(x))
//# define sqlite3Isspace(x)   isspace((unsigned char)(x))
//# define sqlite3Isalnum(x)   isalnum((unsigned char)(x))
//# define sqlite3Isalpha(x)   isalpha((unsigned char)(x))
//# define sqlite3Isdigit(x)   isdigit((unsigned char)(x))
//# define sqlite3Isxdigit(x)  isxdigit((unsigned char)(x))
//# define sqlite3Tolower(x)   tolower((unsigned char)(x))
#endif

    /*
** Internal function prototypes
*/
    //int sqlite3StrICmp(const char *, const char *);
    //int sqlite3IsNumber(const char*, int*, u8);
    //int sqlite3Strlen30(const char*);
    //#define sqlite3StrNICmp sqlite3_strnicmp

    //int sqlite3MallocInit(void);
    //void sqlite3MallocEnd(void);
    //void *sqlite3Malloc(int);
    //void *sqlite3MallocZero(int);
    //void *sqlite3DbMallocZero(sqlite3*, int);
    //void *sqlite3DbMallocRaw(sqlite3*, int);
    //char *sqlite3DbStrDup(sqlite3*,const char*);
    //char *sqlite3DbStrNDup(sqlite3*,const char*, int);
    //void *sqlite3Realloc(void*, int);
    //void *sqlite3DbReallocOrFree(sqlite3 *, void *, int);
    //void *sqlite3DbRealloc(sqlite3 *, void *, int);
    //void //sqlite3DbFree(sqlite3*, void*);
    //int sqlite3MallocSize(void*);
    //int sqlite3DbMallocSize(sqlite3*, void*);
    //void *sqlite3ScratchMalloc(int);
    //void //sqlite3ScratchFree(void*);
    //void *sqlite3PageMalloc(int);
    //void sqlite3PageFree(void*);
    //void sqlite3MemSetDefault(void);
    //void sqlite3BenignMallocHooks(void (*)(void), void (*)(void));
    //int sqlite3MemoryAlarm(void (*)(void*, sqlite3_int64, int), void*, sqlite3_int64);

    /*
    ** On systems with ample stack space and that support alloca(), make
    ** use of alloca() to obtain space for large automatic objects.  By default,
    ** obtain space from malloc().
    **
    ** The alloca() routine never returns NULL.  This will cause code paths
    ** that deal with sqlite3StackAlloc() failures to be unreachable.
    */
#if SQLITE_USE_ALLOCA
//# define sqlite3StackAllocRaw(D,N)   alloca(N)
//# define sqlite3StackAllocZero(D,N)  memset(alloca(N), 0, N)
//# define //sqlite3StackFree(D,P)
#else
#if FALSE
    //# define sqlite3StackAllocRaw(D,N)   sqlite3DbMallocRaw(D,N)
    static void sqlite3StackAllocRaw( sqlite3 D, int N ) { sqlite3DbMallocRaw( D, N ); }
    //# define sqlite3StackAllocZero(D,N)  sqlite3DbMallocZero(D,N)
    static void sqlite3StackAllocZero( sqlite3 D, int N ) { sqlite3DbMallocZero( D, N ); }
    //# define //sqlite3StackFree(D,P)       //sqlite3DbFree(D,P)
    static void //sqlite3StackFree( sqlite3 D, object P ) {sqlite3DbFree( D, P ); }
#endif
#endif

#if SQLITE_ENABLE_MEMSYS3
const sqlite3_mem_methods *sqlite3MemGetMemsys3(void);
#endif
#if SQLITE_ENABLE_MEMSYS5
const sqlite3_mem_methods *sqlite3MemGetMemsys5(void);
#endif

#if !SQLITE_MUTEX_OMIT
//  sqlite3_mutex_methods *sqlite3DefaultMutex(void);
//  sqlite3_mutex *sqlite3MutexAlloc(int);
//  int sqlite3MutexInit(void);
//  int sqlite3MutexEnd(void);
#endif

    //int sqlite3StatusValue(int);
    //void sqlite3StatusAdd(int, int);
    //void sqlite3StatusSet(int, int);

    //int sqlite3IsNaN(double);

    //void sqlite3VXPrintf(StrAccum*, int, const char*, va_list);
    //char *sqlite3MPrintf(sqlite3*,const char*, ...);
    //char *sqlite3VMPrintf(sqlite3*,const char*, va_list);
    //char *sqlite3MAppendf(sqlite3*,char*,const char*,...);
#if SQLITE_TEST || SQLITE_DEBUG
    //  void sqlite3DebugPrintf(const char*, ...);
#endif
#if SQLITE_TEST
    //  void *sqlite3TestTextToPtr(const char*);
#endif
    //void sqlite3SetString(char **, sqlite3*, const char*, ...);
    //void sqlite3ErrorMsg(Parse*, const char*, ...);
    //void sqlite3ErrorClear(Parse*);
    //int sqlite3Dequote(char*);
    //int sqlite3KeywordCode(const unsigned char*, int);
    //int sqlite3RunParser(Parse*, const char*, char **);
    //void sqlite3FinishCoding(Parse*);
    //int sqlite3GetTempReg(Parse*);
    //void sqlite3ReleaseTempReg(Parse*,int);
    //int sqlite3GetTempRange(Parse*,int);
    //void sqlite3ReleaseTempRange(Parse*,int,int);
    //Expr *sqlite3ExprAlloc(sqlite3*,int,const Token*,int);
    //Expr *sqlite3Expr(sqlite3*,int,const char*);
    //void sqlite3ExprAttachSubtrees(sqlite3*,Expr*,Expr*,Expr*);
    //Expr *sqlite3PExpr(Parse*, int, Expr*, Expr*, const Token*);
    //Expr *sqlite3ExprAnd(sqlite3*,Expr*, Expr*);
    //Expr *sqlite3ExprFunction(Parse*,ExprList*, Token*);
    //void sqlite3ExprAssignVarNumber(Parse*, Expr*);
    //void sqlite3ExprClear(sqlite3*, Expr*);
    //void sqlite3ExprDelete(sqlite3*, Expr*);
    //ExprList *sqlite3ExprListAppend(Parse*,ExprList*,Expr*);
    //void sqlite3ExprListSetName(Parse*,ExprList*,Token*,int);
    //void sqlite3ExprListSetSpan(Parse*,ExprList*,ExprSpan*);
    //void sqlite3ExprListDelete(sqlite3*, ExprList*);
    //int sqlite3Init(sqlite3*, char**);
    //int sqlite3InitCallback(void*, int, char**, char**);
    //void sqlite3Pragma(Parse*,Token*,Token*,Token*,int);
    //void sqlite3ResetInternalSchema(sqlite3*, int);
    //void sqlite3BeginParse(Parse*,int);
    //void sqlite3CommitInternalChanges(sqlite3*);
    //Table *sqlite3ResultSetOfSelect(Parse*,Select*);
    //void sqlite3OpenMasterTable(Parse *, int);
    //void sqlite3StartTable(Parse*,Token*,Token*,int,int,int,int);
    //void sqlite3AddColumn(Parse*,Token*);
    //void sqlite3AddNotNull(Parse*, int);
    //void sqlite3AddPrimaryKey(Parse*, ExprList*, int, int, int);
    //void sqlite3AddCheckConstraint(Parse*, Expr*);
    //void sqlite3AddColumnType(Parse*,Token*);
    //void sqlite3AddDefaultValue(Parse*,ExprSpan*);
    //void sqlite3AddCollateType(Parse*, Token*);
    //void sqlite3EndTable(Parse*,Token*,Token*,Select*);

    //Bitvec *sqlite3BitvecCreate(u32);
    //int sqlite3BitvecTest(Bitvec*, u32);
    //int sqlite3BitvecSet(Bitvec*, u32);
    //void sqlite3BitvecClear(Bitvec*, u32, void*);
    //void sqlite3BitvecDestroy(Bitvec*);
    //u32 sqlite3BitvecSize(Bitvec*);
    //int sqlite3BitvecBuiltinTest(int,int*);

    //RowSet *sqlite3RowSetInit(sqlite3*, void*, unsigned int);
    //void sqlite3RowSetClear(RowSet*);
    //void sqlite3RowSetInsert(RowSet*, i64);
    //int sqlite3RowSetTest(RowSet*, u8 iBatch, i64);
    //int sqlite3RowSetNext(RowSet*, i64*);

    //void sqlite3CreateView(Parse*,Token*,Token*,Token*,Select*,int,int);

    //#if !(SQLITE_OMIT_VIEW) || !SQLITE_OMIT_VIRTUALTABLE)
    //  int sqlite3ViewGetColumnNames(Parse*,Table*);
    //#else
    //# define sqlite3ViewGetColumnNames(A,B) 0
    //#endif

    //void sqlite3DropTable(Parse*, SrcList*, int, int);
    //void sqlite3DeleteTable(Table*);
    //#if ! SQLITE_OMIT_AUTOINCREMENT
    //  void sqlite3AutoincrementBegin(Parse *pParse);
    //  void sqlite3AutoincrementEnd(Parse *pParse);
    //#else
    //# define sqlite3AutoincrementBegin(X)
    //# define sqlite3AutoincrementEnd(X)
    //#endif
    //void sqlite3Insert(Parse*, SrcList*, ExprList*, Select*, IdList*, int);
    //void *sqlite3ArrayAllocate(sqlite3*,void*,int,int,int*,int*,int*);
    //IdList *sqlite3IdListAppend(sqlite3*, IdList*, Token*);
    //int sqlite3IdListIndex(IdList*,const char*);
    //SrcList *sqlite3SrcListEnlarge(sqlite3*, SrcList*, int, int);
    //SrcList *sqlite3SrcListAppend(sqlite3*, SrcList*, Token*, Token*);
    //SrcList *sqlite3SrcListAppendFromTerm(Parse*, SrcList*, Token*, Token*,
    //                                      Token*, Select*, Expr*, IdList*);
    //void sqlite3SrcListIndexedBy(Parse *, SrcList *, Token *);
    //int sqlite3IndexedByLookup(Parse *, struct SrcList_item *);
    //void sqlite3SrcListShiftJoinType(SrcList*);
    //void sqlite3SrcListAssignCursors(Parse*, SrcList*);
    //void sqlite3IdListDelete(sqlite3*, IdList*);
    //void sqlite3SrcListDelete(sqlite3*, SrcList*);
    //void sqlite3CreateIndex(Parse*,Token*,Token*,SrcList*,ExprList*,int,Token*,
    //                        Token*, int, int);
    //void sqlite3DropIndex(Parse*, SrcList*, int);
    //int sqlite3Select(Parse*, Select*, SelectDest*);
    //Select *sqlite3SelectNew(Parse*,ExprList*,SrcList*,Expr*,ExprList*,
    //                         Expr*,ExprList*,int,Expr*,Expr*);
    //void sqlite3SelectDelete(sqlite3*, Select*);
    //Table *sqlite3SrcListLookup(Parse*, SrcList*);
    //int sqlite3IsReadOnly(Parse*, Table*, int);
    //void sqlite3OpenTable(Parse*, int iCur, int iDb, Table*, int);
#if (SQLITE_ENABLE_UPDATE_DELETE_LIMIT) && !(SQLITE_OMIT_SUBQUERY)
//Expr *sqlite3LimitWhere(Parse *, SrcList *, Expr *, ExprList *, Expr *, Expr *, char *);
#endif
    //void sqlite3DeleteFrom(Parse*, SrcList*, Expr*);
    //void sqlite3Update(Parse*, SrcList*, ExprList*, Expr*, int);
    //WhereInfo *sqlite3WhereBegin(Parse*, SrcList*, Expr*, ExprList**, u16);
    //void sqlite3WhereEnd(WhereInfo*);
    //int sqlite3ExprCodeGetColumn(Parse*, Table*, int, int, int, int);
    //void sqlite3ExprCodeMove(Parse*, int, int, int);
    //void sqlite3ExprCodeCopy(Parse*, int, int, int);
    //void sqlite3ExprCacheStore(Parse*, int, int, int);
    //void sqlite3ExprCachePush(Parse*);
    //void sqlite3ExprCachePop(Parse*, int);
    //void sqlite3ExprCacheRemove(Parse*, int);
    //void sqlite3ExprCacheClear(Parse*);
    //void sqlite3ExprCacheAffinityChange(Parse*, int, int);
    //void sqlite3ExprHardCopy(Parse*,int,int);
    //int sqlite3ExprCode(Parse*, Expr*, int);
    //int sqlite3ExprCodeTemp(Parse*, Expr*, int*);
    //int sqlite3ExprCodeTarget(Parse*, Expr*, int);
    //int sqlite3ExprCodeAndCache(Parse*, Expr*, int);
    //void sqlite3ExprCodeConstants(Parse*, Expr*);
    //int sqlite3ExprCodeExprList(Parse*, ExprList*, int, int);
    //void sqlite3ExprIfTrue(Parse*, Expr*, int, int);
    //void sqlite3ExprIfFalse(Parse*, Expr*, int, int);
    //Table *sqlite3FindTable(sqlite3*,const char*, const char*);
    //Table *sqlite3LocateTable(Parse*,int isView,const char*, const char*);
    //Index *sqlite3FindIndex(sqlite3*,const char*, const char*);
    //void sqlite3UnlinkAndDeleteTable(sqlite3*,int,const char*);
    //void sqlite3UnlinkAndDeleteIndex(sqlite3*,int,const char*);
    //void sqlite3Vacuum(Parse*);
    //int sqlite3RunVacuum(char**, sqlite3*);
    //char *sqlite3NameFromToken(sqlite3*, Token*);
    //int sqlite3ExprCompare(Expr*, Expr*);
    //void sqlite3ExprAnalyzeAggregates(NameContext*, Expr*);
    //void sqlite3ExprAnalyzeAggList(NameContext*,ExprList*);
    //Vdbe *sqlite3GetVdbe(Parse*);
    //Expr *sqlite3CreateIdExpr(Parse *, const char*);
    //void sqlite3PrngSaveState(void);
    //void sqlite3PrngRestoreState(void);
    //void sqlite3PrngResetState(void);
    //void sqlite3RollbackAll(sqlite3*);
    //void sqlite3CodeVerifySchema(Parse*, int);
    //void sqlite3BeginTransaction(Parse*, int);
    //void sqlite3CommitTransaction(Parse*);
    //void sqlite3RollbackTransaction(Parse*);
    //void sqlite3Savepoint(Parse*, int, Token*);
    //void sqlite3CloseSavepoints(sqlite3 *);
    //int sqlite3ExprIsConstant(Expr*);
    //int sqlite3ExprIsConstantNotJoin(Expr*);
    //int sqlite3ExprIsConstantOrFunction(Expr*);
    //int sqlite3ExprIsInteger(Expr*, int*);
    //int sqlite3IsRowid(const char*);
    //void sqlite3GenerateRowDelete(Parse*, Table*, int, int, int);
    //void sqlite3GenerateRowIndexDelete(Parse*, Table*, int, int*);
    //int sqlite3GenerateIndexKey(Parse*, Index*, int, int, int);
    //void sqlite3GenerateConstraintChecks(Parse*,Table*,int,int,
    //                                     int*,int,int,int,int,int*);
    //void sqlite3CompleteInsertion(Parse*, Table*, int, int, int*, int, int,int,int);
    //int sqlite3OpenTableAndIndices(Parse*, Table*, int, int);
    //void sqlite3BeginWriteOperation(Parse*, int, int);
    //Expr *sqlite3ExprDup(sqlite3*,Expr*,int);
    //ExprList *sqlite3ExprListDup(sqlite3*,ExprList*,int);
    //SrcList *sqlite3SrcListDup(sqlite3*,SrcList*,int);
    //IdList *sqlite3IdListDup(sqlite3*,IdList*);
    //Select *sqlite3SelectDup(sqlite3*,Select*,int);
    //void sqlite3FuncDefInsert(FuncDefHash*, FuncDef*);
    //FuncDef *sqlite3FindFunction(sqlite3*,const char*,int,int,u8,int);
    //void sqlite3RegisterBuiltinFunctions(sqlite3*);
    //void sqlite3RegisterDateTimeFunctions(void);
    //void sqlite3RegisterGlobalFunctions(void);
    //#if SQLITE_DEBUG
    //  int sqlite3SafetyOn(sqlite3*);
    //  int sqlite3SafetyOff(sqlite3*);
    //#else
    //# define sqlite3SafetyOn(A) 0
    //# define sqlite3SafetyOff(A) 0
    //#endif
    //int sqlite3SafetyCheckOk(sqlite3*);
    //int sqlite3SafetyCheckSickOrOk(sqlite3*);
    //void sqlite3ChangeCookie(Parse*, int);
#if !(SQLITE_OMIT_VIEW) && !(SQLITE_OMIT_TRIGGER)
    //void sqlite3MaterializeView(Parse*, Table*, Expr*, int);
#endif

#if !SQLITE_OMIT_TRIGGER
    //void sqlite3BeginTrigger(Parse*, Token*,Token*,int,int,IdList*,SrcList*,
    //                         Expr*,int, int);
    //void sqlite3FinishTrigger(Parse*, TriggerStep*, Token*);
    //void sqlite3DropTrigger(Parse*, SrcList*, int);
    //Trigger *sqlite3TriggersExist(Parse *, Table*, int, ExprList*, int *pMask);
    //Trigger *sqlite3TriggerList(Parse *, Table *);
    //int sqlite3CodeRowTrigger(Parse*, Trigger *, int, ExprList*, int, Table *,
    //                          int, int, int, int, u32*, u32*);
    //void sqliteViewTriggers(Parse*, Table*, Expr*, int, ExprList*);
    //void sqlite3DeleteTriggerStep(sqlite3*, TriggerStep*);
    //TriggerStep *sqlite3TriggerSelectStep(sqlite3*,Select*);
    //TriggerStep *sqlite3TriggerInsertStep(sqlite3*,Token*, IdList*,
    //                                      ExprList*,Select*,u8);
    //TriggerStep *sqlite3TriggerUpdateStep(sqlite3*,Token*,ExprList*, Expr*, u8);
    //TriggerStep *sqlite3TriggerDeleteStep(sqlite3*,Token*, Expr*);
    //void sqlite3DeleteTrigger(sqlite3*, Trigger*);
    //void sqlite3UnlinkAndDeleteTrigger(sqlite3*,int,const char*);
#else
//# define sqlite3TriggersExist(B,C,D,E,F) 0
//# define sqlite3DeleteTrigger(A,B)
//# define sqlite3DropTriggerPtr(A,B)
//# define sqlite3UnlinkAndDeleteTrigger(A,B,C)
//# define sqlite3CodeRowTrigger(A,B,C,D,E,F,G,H,I,J,K,L) 0
//# define sqlite3TriggerList(X, Y) 0
#endif

    //int sqlite3JoinType(Parse*, Token*, Token*, Token*);
    //void sqlite3CreateForeignKey(Parse*, ExprList*, Token*, ExprList*, int);
    //void sqlite3DeferForeignKey(Parse*, int);
#if !SQLITE_OMIT_AUTHORIZATION
void sqlite3AuthRead(Parse*,Expr*,Schema*,SrcList*);
int sqlite3AuthCheck(Parse*,int, const char*, const char*, const char*);
void sqlite3AuthContextPush(Parse*, AuthContext*, const char*);
void sqlite3AuthContextPop(AuthContext*);
#else
    //# define sqlite3AuthRead(a,b,c,d)
    static void sqlite3AuthRead( Parse a, Expr b, Schema c, SrcList d ) { }
    static int sqlite3AuthCheck( Parse a, int b, string c, byte[] d, byte[] e ) { return SQLITE_OK; }//# define sqlite3AuthCheck(a,b,c,d,e)    SQLITE_OK
    //# define sqlite3AuthContextPush(a,b,c)
    //# define sqlite3AuthContextPop(a)  ((void)(a))
#endif
    //void sqlite3Attach(Parse*, Expr*, Expr*, Expr*);
    //void sqlite3Detach(Parse*, Expr*);
    //int sqlite3BtreeFactory(const sqlite3 db, const char *zFilename,
    //                       int omitJournal, int nCache, int flags, Btree **ppBtree);
    //int sqlite3FixInit(DbFixer*, Parse*, int, const char*, const Token*);
    //int sqlite3FixSrcList(DbFixer*, SrcList*);
    //int sqlite3FixSelect(DbFixer*, Select*);
    //int sqlite3FixExpr(DbFixer*, Expr*);
    //int sqlite3FixExprList(DbFixer*, ExprList*);
    //int sqlite3FixTriggerStep(DbFixer*, TriggerStep*);
    //int sqlite3AtoF(const char *z, double*);
    //int sqlite3GetInt32(const char *, int*);
    //int sqlite3FitsIn64Bits(const char *, int);
    //int sqlite3Utf16ByteLen(const void pData, int nChar);
    //int sqlite3Utf8CharLen(const char pData, int nByte);
    //int sqlite3Utf8Read(const u8*, const u8**);

    /*
    ** Routines to read and write variable-length integers.  These used to
    ** be defined locally, but now we use the varint routines in the util.c
    ** file.  Code should use the MACRO forms below, as the Varint32 versions
    ** are coded to assume the single byte case is already handled (which
    ** the MACRO form does).
    */
    //int sqlite3PutVarint(unsigned char*, u64);
    //int putVarint32(unsigned char*, u32);
    //u8 sqlite3GetVarint(const unsigned char *, u64 *);
    //u8 sqlite3GetVarint32(const unsigned char *, u32 *);
    //int sqlite3VarintLen(u64 v);

    /*
    ** The header of a record consists of a sequence variable-length integers.
    ** These integers are almost always small and are encoded as a single byte.
    ** The following macros take advantage this fact to provide a fast encode
    ** and decode of the integers in a record header.  It is faster for the common
    ** case where the integer is a single byte.  It is a little slower when the
    ** integer is two or more bytes.  But overall it is faster.
    **
    ** The following expressions are equivalent:
    **
    **     x = sqlite3GetVarint32( A, B );
    **     x = putVarint32( A, B );
    **
    **     x = getVarint32( A, B );
    **     x = putVarint32( A, B );
    **
    */
    //#define getVarint32(A,B)  (u8)((*(A)<(u8)0x80) ? ((B) = (u32)*(A)),1 : sqlite3GetVarint32((A), (u32 *)&(B)))
    //#define putVarint32(A,B)  (u8)(((u32)(B)<(u32)0x80) ? (*(A) = (unsigned char)(B)),1 : sqlite3PutVarint32((A), (B)))
    //#define getVarint    sqlite3GetVarint
    //#define putVarint    sqlite3PutVarint


    //void sqlite3IndexAffinityStr(Vdbe *, Index *);
    //void sqlite3TableAffinityStr(Vdbe *, Table *);
    //char sqlite3CompareAffinity(Expr pExpr, char aff2);
    //int sqlite3IndexAffinityOk(Expr pExpr, char idx_affinity);
    //char sqlite3ExprAffinity(Expr pExpr);
    //int sqlite3Atoi64(const char*, i64*);
    //void sqlite3Error(sqlite3*, int, const char*,...);
    //void *sqlite3HexToBlob(sqlite3*, const char *z, int n);
    //int sqlite3TwoPartName(Parse *, Token *, Token *, Token **);
    //const char *sqlite3ErrStr(int);
    //int sqlite3ReadSchema(Parse pParse);
    //CollSeq *sqlite3FindCollSeq(sqlite3*,u8 enc, const char*,int);
    //CollSeq *sqlite3LocateCollSeq(Parse *pParse, const char*zName);
    //CollSeq *sqlite3ExprCollSeq(Parse pParse, Expr pExpr);
    //Expr *sqlite3ExprSetColl(Parse pParse, Expr *, Token *);
    //int sqlite3CheckCollSeq(Parse *, CollSeq *);
    //int sqlite3CheckObjectName(Parse *, const char *);
    //void sqlite3VdbeSetChanges(sqlite3 *, int);

    //const void *sqlite3ValueText(sqlite3_value*, u8);
    //int sqlite3ValueBytes(sqlite3_value*, u8);
    //void sqlite3ValueSetStr(sqlite3_value*, int, const void *,u8,
    //                      //  void(*)(void*));
    //void sqlite3ValueFree(sqlite3_value*);
    //sqlite3_value *sqlite3ValueNew(sqlite3 *);
    //char *sqlite3Utf16to8(sqlite3 *, const void*, int);
    //int sqlite3ValueFromExpr(sqlite3 *, Expr *, u8, u8, sqlite3_value **);
    //void sqlite3ValueApplyAffinity(sqlite3_value *, u8, u8);
    //#if !SQLITE_AMALGAMATION
    //extern const unsigned char sqlite3UpperToLower[];
    //extern const unsigned char sqlite3CtypeMap[];
    //extern struct Sqlite3Config sqlite3Config;
    //extern FuncDefHash sqlite3GlobalFunctions;
    //extern int sqlite3PendingByte;
    //#endif
    //void sqlite3RootPageMoved(Db*, int, int);
    //void sqlite3Reindex(Parse*, Token*, Token*);
    //void sqlite3AlterFunctions(sqlite3*);
    //void sqlite3AlterRenameTable(Parse*, SrcList*, Token*);
    //int sqlite3GetToken(const unsigned char *, int *);
    //void sqlite3NestedParse(Parse*, const char*, ...);
    //void sqlite3ExpirePreparedStatements(sqlite3*);
    //void sqlite3CodeSubselect(Parse *, Expr *, int, int);
    //void sqlite3SelectPrep(Parse*, Select*, NameContext*);
    //int sqlite3ResolveExprNames(NameContext*, Expr*);
    //void sqlite3ResolveSelectNames(Parse*, Select*, NameContext*);
    //int sqlite3ResolveOrderGroupBy(Parse*, Select*, ExprList*, const char*);
    //void sqlite3ColumnDefault(Vdbe *, Table *, int, int);
    //void sqlite3AlterFinishAddColumn(Parse *, Token *);
    //void sqlite3AlterBeginAddColumn(Parse *, SrcList *);
    //CollSeq *sqlite3GetCollSeq(sqlite3*, CollSeq *, const char*);
    //char sqlite3AffinityType(const char*);
    //void sqlite3Analyze(Parse*, Token*, Token*);
    //int sqlite3InvokeBusyHandler(BusyHandler*);
    //int sqlite3FindDb(sqlite3*, Token*);
    //int sqlite3FindDbName(sqlite3 *, const char *);
    //int sqlite3AnalysisLoad(sqlite3*,int iDB);
    //void sqlite3DefaultRowEst(Index*);
    //void sqlite3RegisterLikeFunctions(sqlite3*, int);
    //int sqlite3IsLikeFunction(sqlite3*,Expr*,int*,char*);
    //void sqlite3MinimumFileFormat(Parse*, int, int);
    //void sqlite3SchemaFree(void *);
    //Schema *sqlite3SchemaGet(sqlite3 *, Btree *);
    //int sqlite3SchemaToIndex(sqlite3 db, Schema *);
    //KeyInfo *sqlite3IndexKeyinfo(Parse *, Index *);
    //int sqlite3CreateFunc(sqlite3 *, const char *, int, int, void *,
    //  void (*)(sqlite3_context*,int,sqlite3_value **),
    //  void (*)(sqlite3_context*,int,sqlite3_value **), void (*)(sqlite3_context*));
    //int sqlite3ApiExit(sqlite3 db, int);
    //int sqlite3OpenTempDatabase(Parse *);

    //void sqlite3StrAccumAppend(StrAccum*,const char*,int);
    //char *sqlite3StrAccumFinish(StrAccum*);
    //void sqlite3StrAccumReset(StrAccum*);
    //void sqlite3SelectDestInit(SelectDest*,int,int);

    //void sqlite3BackupRestart(sqlite3_backup *);
    //void sqlite3BackupUpdate(sqlite3_backup *, Pgno, const u8 *);

    /*
    ** The interface to the LEMON-generated parser
    */
    //void *sqlite3ParserAlloc(void*(*)(size_t));
    //void sqlite3ParserFree(void*, void(*)(void*));
    //void sqlite3Parser(void*, int, Token, Parse*);
#if YYTRACKMAXSTACKDEPTH
int sqlite3ParserStackPeak(void*);
#endif

    //void sqlite3AutoLoadExtensions(sqlite3*);
#if !SQLITE_OMIT_LOAD_EXTENSION
    //void sqlite3CloseExtensions(sqlite3*);
#else
//# define sqlite3CloseExtensions(X)
#endif

#if !SQLITE_OMIT_SHARED_CACHE
//void sqlite3TableLock(Parse *, int, int, u8, const char *);
#else
    //#define sqlite3TableLock(v,w,x,y,z)
    static void sqlite3TableLock( Parse p, int p1, int p2, u8 p3, byte[] p4 ) { }
    static void sqlite3TableLock( Parse p, int p1, int p2, u8 p3, string p4 ) { }
#endif

#if SQLITE_TEST
    ///int sqlite3Utf8To8(unsigned char*);
#endif

#if SQLITE_OMIT_VIRTUALTABLE
    //#  define sqlite3VtabClear(Y)
    static void sqlite3VtabClear( Table Y ) { }

    //#  define sqlite3VtabSync(X,Y) SQLITE_OK
    static int sqlite3VtabSync( sqlite3 X, string Y ) { return SQLITE_OK; }

    //#  define sqlite3VtabRollback(X)
    static void sqlite3VtabRollback( sqlite3 X ) { }

    //#  define sqlite3VtabCommit(X)
    static void sqlite3VtabCommit( sqlite3 X ) { }

    //#  define sqlite3VtabInSync(db) 0
    //#  define sqlite3VtabLock(X) 
    static void sqlite3VtabLock( VTable X ) { }

    //#  define sqlite3VtabUnlock(X)
    static void sqlite3VtabUnlock( VTable X ) { }

    //#  define sqlite3VtabUnlockList(X)
    static void sqlite3VtabUnlockList( sqlite3 X ) { }

    static void sqlite3VtabArgExtend( Parse p, Token t ) { }//#  define sqlite3VtabArgExtend(P, T)
    static void sqlite3VtabArgInit( Parse p ) { }//#  define sqlite3VtabArgInit(P)
    static void sqlite3VtabBeginParse( Parse p, Token t1, Token t2, Token t3 ) { }//#  define sqlite3VtabBeginParse(P, T, T1, T2)
    static void sqlite3VtabFinishParse<T>( Parse P, T t ) { }//#  define sqlite3VtabFinishParse(P, T)
    static bool sqlite3VtabInSync( sqlite3 db ) { return false; }

    static VTable sqlite3GetVTable(sqlite3 db , Table T) {return null;}
#else
//void sqlite3VtabClear(Table*);
//int sqlite3VtabSync(sqlite3 db, int rc);
//int sqlite3VtabRollback(sqlite3 db);
//int sqlite3VtabCommit(sqlite3 db);
//void sqlite3VtabLock(VTable *);
//void sqlite3VtabUnlock(VTable *);
//void sqlite3VtabUnlockList(sqlite3*);
//#  define sqlite3VtabInSync(db) ((db)->nVTrans>0 && (db)->aVTrans==0)
static bool sqlite3VtabInSync( sqlite3 db ) { return ( db.nVTrans > 0 && db.aVTrans == 0 ); }
#endif
    //void sqlite3VtabMakeWritable(Parse*,Table*);
    //void sqlite3VtabBeginParse(Parse*, Token*, Token*, Token*);
    //void sqlite3VtabFinishParse(Parse*, Token*);
    //void sqlite3VtabArgInit(Parse*);
    //void sqlite3VtabArgExtend(Parse*, Token*);
    //int sqlite3VtabCallCreate(sqlite3*, int, const char *, char **);
    //int sqlite3VtabCallConnect(Parse*, Table*);
    //int sqlite3VtabCallDestroy(sqlite3*, int, const char *);
    //int sqlite3VtabBegin(sqlite3 *, VTable *);
    //FuncDef *sqlite3VtabOverloadFunction(sqlite3 *,FuncDef*, int nArg, Expr*);
    //void sqlite3InvalidFunction(sqlite3_context*,int,sqlite3_value**);
    //int sqlite3TransferBindings(sqlite3_stmt *, sqlite3_stmt *);
    //int sqlite3Reprepare(Vdbe*);
    //void sqlite3ExprListCheckLength(Parse*, ExprList*, const char*);
    //CollSeq *sqlite3BinaryCompareCollSeq(Parse *, Expr *, Expr *);
    //int sqlite3TempInMemory(const sqlite3*);
    //VTable *sqlite3GetVTable(sqlite3*, Table*);


    /*
    ** Available fault injectors.  Should be numbered beginning with 0.
    */
    const int SQLITE_FAULTINJECTOR_MALLOC = 0;//#define SQLITE_FAULTINJECTOR_MALLOC     0
    const int SQLITE_FAULTINJECTOR_COUNT = 1;//#define SQLITE_FAULTINJECTOR_COUNT      1

    /*
    ** The interface to the code in fault.c used for identifying "benign"
    ** malloc failures. This is only present if SQLITE_OMIT_BUILTIN_TEST
    ** is not defined.
    */
#if !SQLITE_OMIT_BUILTIN_TEST
    //void sqlite3BeginBenignMalloc(void);
    //void sqlite3EndBenignMalloc(void);
#else
//#define sqlite3BeginBenignMalloc()
//#define sqlite3EndBenignMalloc()
#endif

    const int IN_INDEX_ROWID = 1;//#define IN_INDEX_ROWID           1
    const int IN_INDEX_EPH = 2;//#define IN_INDEX_EPH             2
    const int IN_INDEX_INDEX = 3;//#define IN_INDEX_INDEX           3
    //int sqlite3FindInIndex(Parse *, Expr *, int*);

#if SQLITE_ENABLE_ATOMIC_WRITE
//  int sqlite3JournalOpen(sqlite3_vfs *, const char *, sqlite3_file *, int, int);
//  int sqlite3JournalSize(sqlite3_vfs *);
//  int sqlite3JournalCreate(sqlite3_file *);
#else
    //#define sqlite3JournalSize(pVfs) ((pVfs)->szOsFile)
    static int sqlite3JournalSize( sqlite3_vfs pVfs ) { return pVfs.szOsFile; }
#endif

    //void sqlite3MemJournalOpen(sqlite3_file *);
    //int sqlite3MemJournalSize(void);
    //int sqlite3IsMemJournal(sqlite3_file *);

#if SQLITE_MAX_EXPR_DEPTH//>0
    //  void sqlite3ExprSetHeight(Parse pParse, Expr p);
    //  int sqlite3SelectExprHeight(Select *);
    //int sqlite3ExprCheckHeight(Parse*, int);
#else
//#define sqlite3ExprSetHeight(x,y)
//#define sqlite3SelectExprHeight(x) 0
//#define sqlite3ExprCheckHeight(x,y)
#endif

    //u32 sqlite3Get4byte(const u8*);
    //void sqlite3sqlite3Put4byte(u8*, u32);

#if SQLITE_ENABLE_UNLOCK_NOTIFY
void sqlite3ConnectionBlocked(sqlite3 *, sqlite3 *);
void sqlite3ConnectionUnlocked(sqlite3 *db);
void sqlite3ConnectionClosed(sqlite3 *db);
#else
    static void sqlite3ConnectionBlocked( sqlite3 x, sqlite3 y ) { } //#define sqlite3ConnectionBlocked(x,y)
    static void sqlite3ConnectionUnlocked( sqlite3 x ) { }                   //#define sqlite3ConnectionUnlocked(x)
    static void sqlite3ConnectionClosed( sqlite3 x ) { }                     //#define sqlite3ConnectionClosed(x)
#endif

#if SQLITE_DEBUG
    //  void sqlite3ParserTrace(FILE*, char *);
#endif

    /*
** If the SQLITE_ENABLE IOTRACE exists then the global variable
** sqlite3IoTrace is a pointer to a printf-like routine used to
** print I/O tracing messages.
*/
#if SQLITE_ENABLE_IOTRACE
static bool SQLite3IoTrace = false;
//#define IOTRACE(A)  if( sqlite3IoTrace ){ sqlite3IoTrace A; }
static void IOTRACE( string X, params object[] ap ) { if ( SQLite3IoTrace ) { printf( X, ap ); } }

//  void sqlite3VdbeIOTraceSql(Vdbe);
//SQLITE_EXTERN void (*sqlite3IoTrace)(const char*,...);
#else
    //#define IOTRACE(A)
    static void IOTRACE( string F, params object[] ap ) { }
    //#define sqlite3VdbeIOTraceSql(X)
    static void sqlite3VdbeIOTraceSql( Vdbe X ) { }
#endif

    //#endif
  }
}
