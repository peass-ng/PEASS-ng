namespace winPEAS._3rdParty.SQLite.src
{
  public partial class CSSQLite
  {
    /*
    ** 2007 August 14
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains the C functions that implement mutexes.
    **
    ** This file contains code that is common across all mutex implementations.
    **
    ** $Id: mutex.c,v 1.31 2009/07/16 18:21:18 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"

#if !SQLITE_MUTEX_OMIT
/*
** Initialize the mutex system.
*/
static int sqlite3MutexInit()
{
int rc = SQLITE_OK;
if (  sqlite3GlobalConfig.bCoreMutex   )
{
if (  sqlite3GlobalConfig.mutex.xMutexAlloc != null )
{
/* If the xMutexAlloc method has not been set, then the user did not
** install a mutex implementation via sqlite3_config() prior to
** sqlite3_initialize() being called. This block copies pointers to
** the default implementation into the sqlite3Config structure.
**
*/
sqlite3_mutex_methods p = sqlite3DefaultMutex();
sqlite3_mutex_methods pTo = sqlite3GlobalConfig.mutex;

 memcpy(pTo, pFrom, offsetof(sqlite3_mutex_methods, xMutexAlloc));
      memcpy(&pTo->xMutexFree, &pFrom->xMutexFree,
             sizeof(*pTo) - offsetof(sqlite3_mutex_methods, xMutexFree));
      pTo->xMutexAlloc = pFrom->xMutexAlloc;
}
    rc =  sqlite3GlobalConfig.mutex.xMutexInit();
}

return rc;
}

/*
** Shutdown the mutex system. This call frees resources allocated by
** sqlite3MutexInit().
*/
static int sqlite3MutexEnd()
{
int rc = SQLITE_OK;
if( sqlite3GlobalConfig.mutex.xMutexEnd ){
rc = sqlite3GlobalConfig.mutex.xMutexEnd();
}
return rc;
}

/*
** Retrieve a pointer to a static mutex or allocate a new dynamic one.
*/
static sqlite3_mutex sqlite3_mutex_alloc( int id )
{
#if !SQLITE_OMIT_AUTOINIT
if ( sqlite3_initialize() != 0 ) return null;
#endif
return  sqlite3GlobalConfig.mutex.xMutexAlloc( id );
}

static sqlite3_mutex sqlite3MutexAlloc( int id )
{
if ( ! sqlite3GlobalConfig.bCoreMutex   )
{
return null;
}
return  sqlite3GlobalConfig.mutex.xMutexAlloc( id );
}

/*
** Free a dynamic mutex.
*/
static void sqlite3_mutex_free( ref sqlite3_mutex p )
{
if ( p != null )
{
sqlite3GlobalConfig.mutex.xMutexFree( p );
}
}

/*
** Obtain the mutex p. If some other thread already has the mutex, block
** until it can be obtained.
*/
static void sqlite3_mutex_enter( sqlite3_mutex p )
{
if ( p != null )
{
sqlite3GlobalConfig.mutex.xMutexEnter( p );
}
}

/*
** Obtain the mutex p. If successful, return SQLITE_OK. Otherwise, if another
** thread holds the mutex and it cannot be obtained, return SQLITE_BUSY.
*/
static int sqlite3_mutex_try( sqlite3_mutex p )
{
int rc = SQLITE_OK;
if ( p != null )
{
return  sqlite3GlobalConfig.mutex.xMutexTry( p );
}
return rc;
}

/*
** The sqlite3_mutex_leave() routine exits a mutex that was previously
** entered by the same thread.  The behavior is undefined if the mutex
** is not currently entered. If a NULL pointer is passed as an argument
** this function is a no-op.
*/
static void sqlite3_mutex_leave( sqlite3_mutex p )
{
if ( p != null )
{
sqlite3GlobalConfig.mutex.xMutexLeave( p );
}
}

#if !NDEBUG
/*
** The sqlite3_mutex_held() and sqlite3_mutex_notheld() routine are
** intended for use inside Debug.Assert() statements.
*/
static bool sqlite3_mutex_held( sqlite3_mutex p )
{
return ( p == null ||  sqlite3GlobalConfig.mutex.xMutexHeld( p ) != 0 ) ;
}
static bool sqlite3_mutex_notheld( sqlite3_mutex p )
{
return ( p == null ||  sqlite3GlobalConfig.mutex.xMutexNotheld( p ) != 0 ) ;
}
#endif

#endif //* SQLITE_OMIT_MUTEX */
  }
}
