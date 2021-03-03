namespace winPEAS._3rdParty.SQLite.src
{
  public partial class CSSQLite
  {
    /*
    ** 2007 August 27
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
    ** $Id: btmutex.c,v 1.17 2009/07/20 12:33:33 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    **
    ** This file contains code used to implement mutexes on Btree objects.
    ** This code really belongs in btree.c.  But btree.c is getting too
    ** big and we want to break it down some.  This packaged seemed like
    ** a good breakout.
    */
    //#include "btreeInt.h"
#if !SQLITE_OMIT_SHARED_CACHE
#if SQLITE_THREADSAFE

/*
** Obtain the BtShared mutex associated with B-Tree handle p. Also,
** set BtShared.db to the database handle associated with p and the
** p->locked boolean to true.
*/
static void lockBtreeMutex(Btree *p){
assert( p->locked==0 );
assert( sqlite3_mutex_notheld(p->pBt->mutex) );
assert( sqlite3_mutex_held(p->db->mutex) );

sqlite3_mutex_enter(p->pBt->mutex);
p->pBt->db = p->db;
p->locked = 1;
}

/*
** Release the BtShared mutex associated with B-Tree handle p and
** clear the p->locked boolean.
*/
static void unlockBtreeMutex(Btree *p){
assert( p->locked==1 );
assert( sqlite3_mutex_held(p->pBt->mutex) );
assert( sqlite3_mutex_held(p->db->mutex) );
assert( p->db==p->pBt->db );

sqlite3_mutex_leave(p->pBt->mutex);
p->locked = 0;
}

/*
** Enter a mutex on the given BTree object.
**
** If the object is not sharable, then no mutex is ever required
** and this routine is a no-op.  The underlying mutex is non-recursive.
** But we keep a reference count in Btree.wantToLock so the behavior
** of this interface is recursive.
**
** To avoid deadlocks, multiple Btrees are locked in the same order
** by all database connections.  The p->pNext is a list of other
** Btrees belonging to the same database connection as the p Btree
** which need to be locked after p.  If we cannot get a lock on
** p, then first unlock all of the others on p->pNext, then wait
** for the lock to become available on p, then relock all of the
** subsequent Btrees that desire a lock.
*/
void sqlite3BtreeEnter(Btree *p){
Btree *pLater;

/* Some basic sanity checking on the Btree.  The list of Btrees
** connected by pNext and pPrev should be in sorted order by
** Btree.pBt value. All elements of the list should belong to
** the same connection. Only shared Btrees are on the list. */
assert( p->pNext==0 || p->pNext->pBt>p->pBt );
assert( p->pPrev==0 || p->pPrev->pBt<p->pBt );
assert( p->pNext==0 || p->pNext->db==p->db );
assert( p->pPrev==0 || p->pPrev->db==p->db );
assert( p->sharable || (p->pNext==0 && p->pPrev==0) );

/* Check for locking consistency */
assert( !p->locked || p->wantToLock>0 );
assert( p->sharable || p->wantToLock==0 );

/* We should already hold a lock on the database connection */
assert( sqlite3_mutex_held(p->db->mutex) );

/* Unless the database is sharable and unlocked, then BtShared.db
** should already be set correctly. */
assert( (p->locked==0 && p->sharable) || p->pBt->db==p->db );

if( !p->sharable ) return;
p->wantToLock++;
if( p->locked ) return;

/* In most cases, we should be able to acquire the lock we
** want without having to go throught the ascending lock
** procedure that follows.  Just be sure not to block.
*/
if( sqlite3_mutex_try(p->pBt->mutex)==SQLITE_OK ){
p->pBt->db = p->db;
p->locked = 1;
return;
}

/* To avoid deadlock, first release all locks with a larger
** BtShared address.  Then acquire our lock.  Then reacquire
** the other BtShared locks that we used to hold in ascending
** order.
*/
for(pLater=p->pNext; pLater; pLater=pLater->pNext){
assert( pLater->sharable );
assert( pLater->pNext==0 || pLater->pNext->pBt>pLater->pBt );
assert( !pLater->locked || pLater->wantToLock>0 );
if( pLater->locked ){
unlockBtreeMutex(pLater);
}
}
lockBtreeMutex(p);
for(pLater=p->pNext; pLater; pLater=pLater->pNext){
if( pLater->wantToLock ){
lockBtreeMutex(pLater);
}
}
}

/*
** Exit the recursive mutex on a Btree.
*/
void sqlite3BtreeLeave(Btree *p){
if( p->sharable ){
assert( p->wantToLock>0 );
p->wantToLock--;
if( p->wantToLock==0 ){
unlockBtreeMutex(p);
}
}
}

#if !NDEBUG
/*
** Return true if the BtShared mutex is held on the btree, or if the
** B-Tree is not marked as sharable.
**
** This routine is used only from within assert() statements.
*/
int sqlite3BtreeHoldsMutex(Btree *p){
assert( p->sharable==0 || p->locked==0 || p->wantToLock>0 );
assert( p->sharable==0 || p->locked==0 || p->db==p->pBt->db );
assert( p->sharable==0 || p->locked==0 || sqlite3_mutex_held(p->pBt->mutex) );
assert( p->sharable==0 || p->locked==0 || sqlite3_mutex_held(p->db->mutex) );

return (p->sharable==0 || p->locked);
}
#endif


#if !SQLITE_OMIT_INCRBLOB
/*
** Enter and leave a mutex on a Btree given a cursor owned by that
** Btree.  These entry points are used by incremental I/O and can be
** omitted if that module is not used.
*/
void sqlite3BtreeEnterCursor(BtCursor *pCur){
sqlite3BtreeEnter(pCur->pBtree);
}
void sqlite3BtreeLeaveCursor(BtCursor *pCur){
sqlite3BtreeLeave(pCur->pBtree);
}
#endif //* SQLITE_OMIT_INCRBLOB */


/*
** Enter the mutex on every Btree associated with a database
** connection.  This is needed (for example) prior to parsing
** a statement since we will be comparing table and column names
** against all schemas and we do not want those schemas being
** reset out from under us.
**
** There is a corresponding leave-all procedures.
**
** Enter the mutexes in accending order by BtShared pointer address
** to avoid the possibility of deadlock when two threads with
** two or more btrees in common both try to lock all their btrees
** at the same instant.
*/
void sqlite3BtreeEnterAll(sqlite3 *db){
int i;
Btree *p, *pLater;
assert( sqlite3_mutex_held(db->mutex) );
for(i=0; i<db->nDb; i++){
p = db->aDb[i].pBt;
assert( !p || (p->locked==0 && p->sharable) || p->pBt->db==p->db );
if( p && p->sharable ){
p->wantToLock++;
if( !p->locked ){
assert( p->wantToLock==1 );
while( p->pPrev ) p = p->pPrev;
/* Reason for ALWAYS:  There must be at least on unlocked Btree in
** the chain.  Otherwise the !p->locked test above would have failed */
while( p->locked && ALWAYS(p->pNext) ) p = p->pNext;
for(pLater = p->pNext; pLater; pLater=pLater->pNext){
if( pLater->locked ){
unlockBtreeMutex(pLater);
}
}
while( p ){
lockBtreeMutex(p);
p = p->pNext;
}
}
}
}
}
void sqlite3BtreeLeaveAll(sqlite3 *db){
int i;
Btree *p;
assert( sqlite3_mutex_held(db->mutex) );
for(i=0; i<db->nDb; i++){
p = db->aDb[i].pBt;
if( p && p->sharable ){
assert( p->wantToLock>0 );
p->wantToLock--;
if( p->wantToLock==0 ){
unlockBtreeMutex(p);
}
}
}
}

#if !NDEBUG
/*
** Return true if the current thread holds the database connection
** mutex and all required BtShared mutexes.
**
** This routine is used inside assert() statements only.
*/
int sqlite3BtreeHoldsAllMutexes(sqlite3 *db){
int i;
if( !sqlite3_mutex_held(db->mutex) ){
return 0;
}
for(i=0; i<db->nDb; i++){
Btree *p;
p = db->aDb[i].pBt;
if( p && p->sharable &&
(p->wantToLock==0 || !sqlite3_mutex_held(p->pBt->mutex)) ){
return 0;
}
}
return 1;
}
#endif //* NDEBUG */

/*
** Add a new Btree pointer to a BtreeMutexArray.
** if the pointer can possibly be shared with
** another database connection.
**
** The pointers are kept in sorted order by pBtree->pBt.  That
** way when we go to enter all the mutexes, we can enter them
** in order without every having to backup and retry and without
** worrying about deadlock.
**
** The number of shared btrees will always be small (usually 0 or 1)
** so an insertion sort is an adequate algorithm here.
*/
void sqlite3BtreeMutexArrayInsert(BtreeMutexArray *pArray, Btree *pBtree){
int i, j;
BtShared *pBt;
if( pBtree==0 || pBtree->sharable==0 ) return;
#if !NDEBUG
{
for(i=0; i<pArray->nMutex; i++){
assert( pArray->aBtree[i]!=pBtree );
}
}
#endif
assert( pArray->nMutex>=0 );
assert( pArray->nMutex<ArraySize(pArray->aBtree)-1 );
pBt = pBtree->pBt;
for(i=0; i<pArray->nMutex; i++){
assert( pArray->aBtree[i]!=pBtree );
if( pArray->aBtree[i]->pBt>pBt ){
for(j=pArray->nMutex; j>i; j--){
pArray->aBtree[j] = pArray->aBtree[j-1];
}
pArray->aBtree[i] = pBtree;
pArray->nMutex++;
return;
}
}
pArray->aBtree[pArray->nMutex++] = pBtree;
}

/*
** Enter the mutex of every btree in the array.  This routine is
** called at the beginning of sqlite3VdbeExec().  The mutexes are
** exited at the end of the same function.
*/
void sqlite3BtreeMutexArrayEnter(BtreeMutexArray *pArray){
int i;
for(i=0; i<pArray->nMutex; i++){
Btree *p = pArray->aBtree[i];
/* Some basic sanity checking */
assert( i==0 || pArray->aBtree[i-1]->pBt<p->pBt );
assert( !p->locked || p->wantToLock>0 );

/* We should already hold a lock on the database connection */
assert( sqlite3_mutex_held(p->db->mutex) );

/* The Btree is sharable because only sharable Btrees are entered
** into the array in the first place. */
assert( p->sharable );

p->wantToLock++;
if( !p->locked ){
lockBtreeMutex(p);
}
}
}

/*
** Leave the mutex of every btree in the group.
*/
void sqlite3BtreeMutexArrayLeave(BtreeMutexArray *pArray){
int i;
for(i=0; i<pArray->nMutex; i++){
Btree *p = pArray->aBtree[i];
/* Some basic sanity checking */
assert( i==0 || pArray->aBtree[i-1]->pBt<p->pBt );
assert( p->locked);
assert( p->wantToLock>0 );

/* We should already hold a lock on the database connection */
assert( sqlite3_mutex_held(p->db->mutex) );

p->wantToLock--;
if( p->wantToLock==0){
unlockBtreeMutex(p);
}
}
}

#else
static void sqlite3BtreeEnter( Btree p )
{
p.pBt.db = p.db;
}
static void sqlite3BtreeEnterAll( sqlite3 db )
{
int i;
for ( i = 0 ; i < db.nDb ; i++ )
{
Btree p = db.aDb[i].pBt;
if ( p != null )
{
p.pBt.db = p.db;
}
}
}
#endif //* if SQLITE_THREADSAFE */
#endif //* ifndef SQLITE_OMIT_SHARED_CACHE */

  }
}
