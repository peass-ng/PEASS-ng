namespace winPEAS._3rdParty.SQLite.src
{
  public partial class CSSQLite
  {
    /*
    ** 2008 October 28
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
    ** This file contains a no-op memory allocation drivers for use when
    ** SQLITE_ZERO_MALLOC is defined.  The allocation drivers implemented
    ** here always fail.  SQLite will not operate with these drivers.  These
    ** are merely placeholders.  Real drivers must be substituted using
    ** sqlite3_config() before SQLite will operate.
    **
    ** $Id: mem0.c,v 1.1 2008/10/28 18:58:20 drh Exp $
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
    ** This version of the memory allocator is the default.  It is
    ** used when no other memory allocator is specified using compile-time
    ** macros.
    */
#if SQLITE_ZERO_MALLOC

/*
** No-op versions of all memory allocation routines
*/
static void sqlite3MemMalloc(int nByte){ return 0; }
static void sqlite3MemFree(object pPrior){ return; }
static void sqlite3MemRealloc(object pPrior, int nByte){ return 0; }
static int sqlite3MemSize(object pPrior){ return 0; }
static int sqlite3MemRoundup(int n){ return n; }
static int sqlite3MemInit(object NotUsed){ return SQLITE_OK; }
static void sqlite3MemShutdown(object NotUsed){ return; }

/*
** This routine is the only routine in this file with external linkage.
**
** Populate the low-level memory allocation function pointers in
** sqlite3GlobalConfig.m with pointers to the routines in this file.
*/
void sqlite3MemSetDefault(){
static const sqlite3_mem_methods defaultMethods = {
sqlite3MemMalloc,
sqlite3MemFree,
sqlite3MemRealloc,
sqlite3MemSize,
sqlite3MemRoundup,
sqlite3MemInit,
sqlite3MemShutdown,
0
};
sqlite3_config(SQLITE_CONFIG_MALLOC, &defaultMethods);
}

#endif //* SQLITE_ZERO_MALLOC */
  }
}
