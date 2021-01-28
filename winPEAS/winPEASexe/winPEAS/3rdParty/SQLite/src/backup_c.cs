using System;
using System.Diagnostics;
using System.Text;

using i64 = System.Int64;
using u8 = System.Byte;
using u32 = System.UInt32;

using Pgno = System.UInt32;


namespace CS_SQLite3
{
  using sqlite3_int64 = System.Int64;
  using DbPage = CSSQLite.PgHdr;
  public partial class CSSQLite
  {
    /*
    ** 2009 January 28
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains the implementation of the sqlite3_backup_XXX()
    ** API functions and the related features.
    **
    ** $Id: backup.c,v 1.19 2009/07/06 19:03:13 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
    //#include "btreeInt.h"

    /* Macro to find the minimum of two numeric values.
    */
#if !MIN
    //# define MIN(x,y) ((x)<(y)?(x):(y))
#endif

    /*
** Structure allocated for each backup operation.
*/
    public class sqlite3_backup
    {
      public sqlite3 pDestDb;         /* Destination database handle */
      public Btree pDest;             /* Destination b-tree file */
      public u32 iDestSchema;         /* Original schema cookie in destination */
      public int bDestLocked;         /* True once a write-transaction is open on pDest */

      public Pgno iNext;              /* Page number of the next source page to copy */
      public sqlite3 pSrcDb;          /* Source database handle */
      public Btree pSrc;              /* Source b-tree file */

      public int rc;                  /* Backup process error code */

      /* These two variables are set by every call to backup_step(). They are
      ** read by calls to backup_remaining() and backup_pagecount().
      */
      public Pgno nRemaining;         /* Number of pages left to copy */
      public Pgno nPagecount;         /* Total number of pages to copy */

      public int isAttached;          /* True once backup has been registered with pager */
      public sqlite3_backup pNext;    /* Next backup associated with source pager */
    };

    /*
    ** THREAD SAFETY NOTES:
    **
    **   Once it has been created using backup_init(), a single sqlite3_backup
    **   structure may be accessed via two groups of thread-safe entry points:
    **
    **     * Via the sqlite3_backup_XXX() API function backup_step() and
    **       backup_finish(). Both these functions obtain the source database
    **       handle mutex and the mutex associated with the source BtShared
    **       structure, in that order.
    **
    **     * Via the BackupUpdate() and BackupRestart() functions, which are
    **       invoked by the pager layer to report various state changes in
    **       the page cache associated with the source database. The mutex
    **       associated with the source database BtShared structure will always
    **       be held when either of these functions are invoked.
    **
    **   The other sqlite3_backup_XXX() API functions, backup_remaining() and
    **   backup_pagecount() are not thread-safe functions. If they are called
    **   while some other thread is calling backup_step() or backup_finish(),
    **   the values returned may be invalid. There is no way for a call to
    **   BackupUpdate() or BackupRestart() to interfere with backup_remaining()
    **   or backup_pagecount().
    **
    **   Depending on the SQLite configuration, the database handles and/or
    **   the Btree objects may have their own mutexes that require locking.
    **   Non-sharable Btrees (in-memory databases for example), do not have
    **   associated mutexes.
    */

    /*
    ** Return a pointer corresponding to database zDb (i.e. "main", "temp")
    ** in connection handle pDb. If such a database cannot be found, return
    ** a NULL pointer and write an error message to pErrorDb.
    **
    ** If the "temp" database is requested, it may need to be opened by this
    ** function. If an error occurs while doing so, return 0 and write an
    ** error message to pErrorDb.
    */
    static Btree findBtree( sqlite3 pErrorDb, sqlite3 pDb, string zDb )
    {
      int i = sqlite3FindDbName( pDb, zDb );

      if ( i == 1 )
      {
        Parse pParse;
        int rc = 0;
        pParse = new Parse();//sqlite3StackAllocZero(pErrorDb, sizeof(*pParse));
        if ( pParse == null )
        {
          sqlite3Error( pErrorDb, SQLITE_NOMEM, "out of memory" );
          rc = SQLITE_NOMEM;
        }
        else
        {
          pParse.db = pDb;
          if ( sqlite3OpenTempDatabase( pParse ) != 0 )
          {
            sqlite3ErrorClear( pParse );
            sqlite3Error( pErrorDb, pParse.rc, "%s", pParse.zErrMsg );
            rc = SQLITE_ERROR;
          }
          //sqlite3StackFree( pErrorDb, pParse );
        }
        if ( rc != 0 )
        {
          return null;
        }
      }

      if ( i < 0 )
      {
        sqlite3Error( pErrorDb, SQLITE_ERROR, "unknown database %s", zDb );
        return null;
      }

      return pDb.aDb[i].pBt;
    }

    /*
    ** Create an sqlite3_backup process to copy the contents of zSrcDb from
    ** connection handle pSrcDb to zDestDb in pDestDb. If successful, return
    ** a pointer to the new sqlite3_backup object.
    **
    ** If an error occurs, NULL is returned and an error code and error message
    ** stored in database handle pDestDb.
    */
    public static sqlite3_backup sqlite3_backup_init(
    sqlite3 pDestDb,                 /* Database to write to */
    string zDestDb,                  /* Name of database within pDestDb */
    sqlite3 pSrcDb,                  /* Database connection to read from */
    string zSrcDb                    /* Name of database within pSrcDb */
    )
    {
      sqlite3_backup p;                    /* Value to return */

      /* Lock the source database handle. The destination database
      ** handle is not locked in this routine, but it is locked in
      ** sqlite3_backup_step(). The user is required to ensure that no
      ** other thread accesses the destination handle for the duration
      ** of the backup operation.  Any attempt to use the destination
      ** database connection while a backup is in progress may cause
      ** a malfunction or a deadlock.
      */
      sqlite3_mutex_enter( pSrcDb.mutex );
      sqlite3_mutex_enter( pDestDb.mutex );

      if ( pSrcDb == pDestDb )
      {
        sqlite3Error(
        pDestDb, SQLITE_ERROR, "source and destination must be distinct"
        );
        p = null;
      }
      else
      {
        /* Allocate space for a new sqlite3_backup object */
        p = new sqlite3_backup();// (sqlite3_backup)sqlite3_malloc( sizeof( sqlite3_backup ) );
        //if ( null == p )
        //{
        //  sqlite3Error( pDestDb, SQLITE_NOMEM, 0 );
        //}
      }

      /* If the allocation succeeded, populate the new object. */
      if ( p != null )
      {
        // memset( p, 0, sizeof( sqlite3_backup ) );
        p.pSrc = findBtree( pDestDb, pSrcDb, zSrcDb );
        p.pDest = findBtree( pDestDb, pDestDb, zDestDb );
        p.pDestDb = pDestDb;
        p.pSrcDb = pSrcDb;
        p.iNext = 1;
        p.isAttached = 0;

        if ( null == p.pSrc || null == p.pDest )
        {
          /* One (or both) of the named databases did not exist. An error has
          ** already been written into the pDestDb handle. All that is left
          ** to do here is free the sqlite3_backup structure.
          */
          //sqlite3_free( ref p );
          p = null;
        }
      }

      if ( p != null )
      {
        p.pSrc.nBackup++;
      }

      sqlite3_mutex_leave( pDestDb.mutex );
      sqlite3_mutex_leave( pSrcDb.mutex );
      return p;
    }

    /*
    ** Argument rc is an SQLite error code. Return true if this error is
    ** considered fatal if encountered during a backup operation. All errors
    ** are considered fatal except for SQLITE_BUSY and SQLITE_LOCKED.
    */
    static bool isFatalError( int rc )
    {
      return ( rc != SQLITE_OK && rc != SQLITE_BUSY && ALWAYS( rc != SQLITE_LOCKED ) );
    }

    /*
    ** Parameter zSrcData points to a buffer containing the data for
    ** page iSrcPg from the source database. Copy this data into the
    ** destination database.
    */
    static int backupOnePage( sqlite3_backup p, Pgno iSrcPg, byte[] zSrcData )
    {
      Pager pDestPager = sqlite3BtreePager( p.pDest );
      int nSrcPgsz = sqlite3BtreeGetPageSize( p.pSrc );
      int nDestPgsz = sqlite3BtreeGetPageSize( p.pDest );
      int nCopy = MIN( nSrcPgsz, nDestPgsz );
      i64 iEnd = (i64)iSrcPg * (i64)nSrcPgsz;

      int rc = SQLITE_OK;
      i64 iOff;

      Debug.Assert( p.bDestLocked != 0 );
      Debug.Assert( !isFatalError( p.rc ) );
      Debug.Assert( iSrcPg != PENDING_BYTE_PAGE( p.pSrc.pBt ) );
      Debug.Assert( zSrcData != null );

      /* Catch the case where the destination is an in-memory database and the
      ** page sizes of the source and destination differ.
      */
      if ( nSrcPgsz != nDestPgsz && sqlite3PagerIsMemdb( sqlite3BtreePager( p.pDest ) ) )
      {
        rc = SQLITE_READONLY;
      }

      /* This loop runs once for each destination page spanned by the source
      ** page. For each iteration, variable iOff is set to the byte offset
      ** of the destination page.
      */
      for ( iOff = iEnd - (i64)nSrcPgsz ; rc == SQLITE_OK && iOff < iEnd ; iOff += nDestPgsz )
      {
        DbPage pDestPg = null;
        u32 iDest = (u32)( iOff / nDestPgsz ) + 1;
        if ( iDest == PENDING_BYTE_PAGE( p.pDest.pBt ) ) continue;
        if ( SQLITE_OK == ( rc = sqlite3PagerGet( pDestPager, iDest, ref pDestPg ) )
        && SQLITE_OK == ( rc = sqlite3PagerWrite( pDestPg ) )
        )
        {
          //string zIn = &zSrcData[iOff%nSrcPgsz];
          byte[] zDestData = sqlite3PagerGetData( pDestPg );
          //string zOut = &zDestData[iOff % nDestPgsz];

          /* Copy the data from the source page into the destination page.
          ** Then clear the Btree layer MemPage.isInit flag. Both this module
          ** and the pager code use this trick (clearing the first byte
          ** of the page 'extra' space to invalidate the Btree layers
          ** cached parse of the page). MemPage.isInit is marked
          ** "MUST BE FIRST" for this purpose.
          */
          Buffer.BlockCopy( zSrcData, (int)( iOff % nSrcPgsz ), zDestData, (int)( iOff % nDestPgsz ), nCopy );// memcpy( zOut, zIn, nCopy );
          sqlite3PagerGetExtra( pDestPg ).isInit = 0;// ( sqlite3PagerGetExtra( pDestPg ) )[0] = 0;
        }
        sqlite3PagerUnref( pDestPg );
      }

      return rc;
    }

    /*
    ** If pFile is currently larger than iSize bytes, then truncate it to
    ** exactly iSize bytes. If pFile is not larger than iSize bytes, then
    ** this function is a no-op.
    **
    ** Return SQLITE_OK if everything is successful, or an SQLite error
    ** code if an error occurs.
    */
    static int backupTruncateFile( sqlite3_file pFile, int iSize )
    {
      int iCurrent = 0;
      int rc = sqlite3OsFileSize( pFile, ref iCurrent );
      if ( rc == SQLITE_OK && iCurrent > iSize )
      {
        rc = sqlite3OsTruncate( pFile, iSize );
      }
      return rc;
    }

    /*
    ** Register this backup object with the associated source pager for
    ** callbacks when pages are changed or the cache invalidated.
    */
    static void attachBackupObject( sqlite3_backup p )
    {
      sqlite3_backup pp;
      Debug.Assert( sqlite3BtreeHoldsMutex( p.pSrc ) );
      pp = sqlite3PagerBackupPtr( sqlite3BtreePager( p.pSrc ) );
      p.pNext = pp;
      sqlite3BtreePager( p.pSrc ).pBackup = p; //*pp = p;
      p.isAttached = 1;
    }

    /*
    ** Copy nPage pages from the source b-tree to the destination.
    */
    public static int sqlite3_backup_step( sqlite3_backup p, int nPage )
    {
      int rc;

      sqlite3_mutex_enter( p.pSrcDb.mutex );
      sqlite3BtreeEnter( p.pSrc );
      if ( p.pDestDb != null )
      {
        sqlite3_mutex_enter( p.pDestDb.mutex );
      }

      rc = p.rc;
      if ( !isFatalError( rc ) )
      {
        Pager pSrcPager = sqlite3BtreePager( p.pSrc );    /* Source pager */
        Pager pDestPager = sqlite3BtreePager( p.pDest );   /* Dest pager */
        int ii;                            /* Iterator variable */
        int nSrcPage = -1;                 /* Size of source db in pages */
        int bCloseTrans = 0;               /* True if src db requires unlocking */

        /* If the source pager is currently in a write-transaction, return
        ** SQLITE_BUSY immediately.
        */
        if ( p.pDestDb != null && p.pSrc.pBt.inTransaction == TRANS_WRITE )
        {
          rc = SQLITE_BUSY;
        }
        else
        {
          rc = SQLITE_OK;
        }

        /* Lock the destination database, if it is not locked already. */
        if ( SQLITE_OK == rc && p.bDestLocked == 0
        && SQLITE_OK == ( rc = sqlite3BtreeBeginTrans( p.pDest, 2 ) )
        )
        {
          p.bDestLocked = 1;
          sqlite3BtreeGetMeta( p.pDest, BTREE_SCHEMA_VERSION, ref p.iDestSchema );
        }

        /* If there is no open read-transaction on the source database, open
        ** one now. If a transaction is opened here, then it will be closed
        ** before this function exits.
        */
        if ( rc == SQLITE_OK && !sqlite3BtreeIsInReadTrans( p.pSrc ) )
        {
          rc = sqlite3BtreeBeginTrans( p.pSrc, 0 );
          bCloseTrans = 1;
        }

        /* Now that there is a read-lock on the source database, query the
        ** source pager for the number of pages in the database.
        */
        if ( rc == SQLITE_OK )
        {
          rc = sqlite3PagerPagecount( pSrcPager, ref nSrcPage );
        }
        for ( ii = 0 ; ( nPage < 0 || ii < nPage ) && p.iNext <= (Pgno)nSrcPage && 0 == rc ; ii++ )
        {
          Pgno iSrcPg = p.iNext;                 /* Source page number */
          if ( iSrcPg != PENDING_BYTE_PAGE( p.pSrc.pBt ) )
          {
            DbPage pSrcPg = null;                             /* Source page object */
            rc = sqlite3PagerGet( pSrcPager, (u32)iSrcPg, ref pSrcPg );
            if ( rc == SQLITE_OK )
            {
              rc = backupOnePage( p, iSrcPg, sqlite3PagerGetData( pSrcPg ) );
              sqlite3PagerUnref( pSrcPg );
            }
          }
          p.iNext++;
        }
        if ( rc == SQLITE_OK )
        {
          p.nPagecount = (u32)nSrcPage;
          p.nRemaining = (u32)( nSrcPage + 1 - p.iNext );
          if ( p.iNext > (Pgno)nSrcPage )
          {
            rc = SQLITE_DONE;
          }
          else if ( 0 == p.isAttached )
          {
            attachBackupObject( p );
          }
        }


          /* Update the schema version field in the destination database. This
          ** is to make sure that the schema-version really does change in
          ** the case where the source and destination databases have the
          ** same schema version.
          */
        if ( rc == SQLITE_DONE
         && ( rc = sqlite3BtreeUpdateMeta( p.pDest, 1, p.iDestSchema + 1 ) ) == SQLITE_OK
        )
        {
          int nSrcPagesize = sqlite3BtreeGetPageSize( p.pSrc );
          int nDestPagesize = sqlite3BtreeGetPageSize( p.pDest );
          int nDestTruncate;
          if ( p.pDestDb != null )
          {
            sqlite3ResetInternalSchema( p.pDestDb, 0 );
          }

          /* Set nDestTruncate to the final number of pages in the destination
          ** database. The complication here is that the destination page
          ** size may be different to the source page size.
          **
          ** If the source page size is smaller than the destination page size,
          ** round up. In this case the call to sqlite3OsTruncate() below will
          ** fix the size of the file. However it is important to call
          ** sqlite3PagerTruncateImage() here so that any pages in the
          ** destination file that lie beyond the nDestTruncate page mark are
          ** journalled by PagerCommitPhaseOne() before they are destroyed
          ** by the file truncation.
          */
          if ( nSrcPagesize < nDestPagesize )
          {
            int ratio = nDestPagesize / nSrcPagesize;
            nDestTruncate = ( nSrcPage + ratio - 1 ) / ratio;
            if ( nDestTruncate == (int)PENDING_BYTE_PAGE( p.pDest.pBt ) )
            {
              nDestTruncate--;
            }
          }
          else
          {
            nDestTruncate = nSrcPage * ( nSrcPagesize / nDestPagesize );
          }
          sqlite3PagerTruncateImage( pDestPager, (u32)nDestTruncate );

          if ( nSrcPagesize < nDestPagesize )
          {
            /* If the source page-size is smaller than the destination page-size,
            ** two extra things may need to happen:
            **
            **   * The destination may need to be truncated, and
            **
            **   * Data stored on the pages immediately following the
            **     pending-byte page in the source database may need to be
            **     copied into the destination database.
            */
            u32 iSize = (u32)nSrcPagesize * (u32)nSrcPage;
            sqlite3_file pFile = sqlite3PagerFile( pDestPager );

            Debug.Assert( pFile != null );
            Debug.Assert( (i64)nDestTruncate * (i64)nDestPagesize >= iSize || (
            nDestTruncate == (int)( PENDING_BYTE_PAGE( p.pDest.pBt ) - 1 )
            && iSize >= PENDING_BYTE && iSize <= PENDING_BYTE + nDestPagesize
            ) );
            if ( SQLITE_OK == ( rc = sqlite3PagerCommitPhaseOne( pDestPager, null, true ) )
            && SQLITE_OK == ( rc = backupTruncateFile( pFile, (int)iSize ) )
            && SQLITE_OK == ( rc = sqlite3PagerSync( pDestPager ) )
            )
            {
              i64 iOff;
              i64 iEnd = MIN( PENDING_BYTE + nDestPagesize, iSize );
              for (
              iOff = PENDING_BYTE + nSrcPagesize ;
              rc == SQLITE_OK && iOff < iEnd ;
              iOff += nSrcPagesize
              )
              {
                PgHdr pSrcPg = null;
                u32 iSrcPg = (u32)( ( iOff / nSrcPagesize ) + 1 );
                rc = sqlite3PagerGet( pSrcPager, iSrcPg, ref pSrcPg );
                if ( rc == SQLITE_OK )
                {
                  byte[] zData = sqlite3PagerGetData( pSrcPg );
                  rc = sqlite3OsWrite( pFile, zData, nSrcPagesize, iOff );
                }
                sqlite3PagerUnref( pSrcPg );
              }
            }
          }
          else
          {
            rc = sqlite3PagerCommitPhaseOne( pDestPager, null, false );
          }

          /* Finish committing the transaction to the destination database. */
          if ( SQLITE_OK == rc
          && SQLITE_OK == ( rc = sqlite3BtreeCommitPhaseTwo( p.pDest ) )
          )
          {
            rc = SQLITE_DONE;
          }
        }

        /* If bCloseTrans is true, then this function opened a read transaction
        ** on the source database. Close the read transaction here. There is
        ** no need to check the return values of the btree methods here, as
        ** "committing" a read-only transaction cannot fail.
        */
        if ( bCloseTrans != 0 )
        {
#if !NDEBUG || SQLITE_COVERAGE_TEST
          //TESTONLY( int rc2 );
          //TESTONLY( rc2  = ) sqlite3BtreeCommitPhaseOne(p.pSrc, 0);
          //TESTONLY( rc2 |= ) sqlite3BtreeCommitPhaseTwo(p.pSrc);
          int rc2;
          rc2 = sqlite3BtreeCommitPhaseOne( p.pSrc, "" );
          rc2 |= sqlite3BtreeCommitPhaseTwo( p.pSrc );
          Debug.Assert( rc2 == SQLITE_OK );
#else
sqlite3BtreeCommitPhaseOne(p.pSrc, null);
sqlite3BtreeCommitPhaseTwo(p.pSrc);
#endif
        }

        p.rc = rc;
      }
      if ( p.pDestDb != null )
      {
        sqlite3_mutex_leave( p.pDestDb.mutex );
      }
      sqlite3BtreeLeave( p.pSrc );
      sqlite3_mutex_leave( p.pSrcDb.mutex );
      return rc;
    }

    /*
    ** Release all resources associated with an sqlite3_backup* handle.
    */
    public static int sqlite3_backup_finish( sqlite3_backup p )
    {
      sqlite3_backup pp;                 /* Ptr to head of pagers backup list */
      sqlite3_mutex mutex;               /* Mutex to protect source database */
      int rc;                            /* Value to return */

      /* Enter the mutexes */
      if ( p == null ) return SQLITE_OK;
      sqlite3_mutex_enter( p.pSrcDb.mutex );
      sqlite3BtreeEnter( p.pSrc );
      mutex = p.pSrcDb.mutex;
      if ( p.pDestDb != null )
      {
        sqlite3_mutex_enter( p.pDestDb.mutex );
      }

      /* Detach this backup from the source pager. */
      if ( p.pDestDb != null )
      {
        p.pSrc.nBackup--;
      }
      if ( p.isAttached != 0 )
      {
        pp = sqlite3PagerBackupPtr( sqlite3BtreePager( p.pSrc ) );
        while ( pp != p )
        {
          pp = ( pp ).pNext;
        }
        sqlite3BtreePager( p.pSrc ).pBackup = p.pNext;
      }

      /* If a transaction is still open on the Btree, roll it back. */
      sqlite3BtreeRollback( p.pDest );

      /* Set the error code of the destination database handle. */
      rc = ( p.rc == SQLITE_DONE ) ? SQLITE_OK : p.rc;
      sqlite3Error( p.pDestDb, rc, 0 );

      /* Exit the mutexes and free the backup context structure. */
      if ( p.pDestDb != null )
      {
        sqlite3_mutex_leave( p.pDestDb.mutex );
      }
      sqlite3BtreeLeave( p.pSrc );
      if ( p.pDestDb != null )
      {
        //sqlite3_free( ref p );
      }
      sqlite3_mutex_leave( mutex );
      return rc;
    }

    /*
    ** Return the number of pages still to be backed up as of the most recent
    ** call to sqlite3_backup_step().
    */
    static int sqlite3_backup_remaining( sqlite3_backup p )
    {
      return (int)p.nRemaining;
    }

    /*
    ** Return the total number of pages in the source database as of the most
    ** recent call to sqlite3_backup_step().
    */
    static int sqlite3_backup_pagecount( sqlite3_backup p )
    {
      return (int)p.nPagecount;
    }

    /*
    ** This function is called after the contents of page iPage of the
    ** source database have been modified. If page iPage has already been
    ** copied into the destination database, then the data written to the
    ** destination is now invalidated. The destination copy of iPage needs
    ** to be updated with the new data before the backup operation is
    ** complete.
    **
    ** It is assumed that the mutex associated with the BtShared object
    ** corresponding to the source database is held when this function is
    ** called.
    */
    static void sqlite3BackupUpdate( sqlite3_backup pBackup, Pgno iPage, byte[] aData )
    {
      sqlite3_backup p;                   /* Iterator variable */
      for ( p = pBackup ; p != null ; p = p.pNext )
      {
        Debug.Assert( sqlite3_mutex_held( p.pSrc.pBt.mutex ) );
        if ( !isFatalError( p.rc ) && iPage < p.iNext )
        {
          /* The backup process p has already copied page iPage. But now it
          ** has been modified by a transaction on the source pager. Copy
          ** the new data into the backup.
          */
          int rc = backupOnePage( p, iPage, aData );
          Debug.Assert( rc != SQLITE_BUSY && rc != SQLITE_LOCKED );
          if ( rc != SQLITE_OK )
          {
            p.rc = rc;
          }
        }
      }
    }

    /*
    ** Restart the backup process. This is called when the pager layer
    ** detects that the database has been modified by an external database
    ** connection. In this case there is no way of knowing which of the
    ** pages that have been copied into the destination database are still
    ** valid and which are not, so the entire process needs to be restarted.
    **
    ** It is assumed that the mutex associated with the BtShared object
    ** corresponding to the source database is held when this function is
    ** called.
    */
    static void sqlite3BackupRestart( sqlite3_backup pBackup )
    {
      sqlite3_backup p;                   /* Iterator variable */
      for ( p = pBackup ; p != null ; p = p.pNext )
      {
        Debug.Assert( sqlite3_mutex_held( p.pSrc.pBt.mutex ) );
        p.iNext = 1;
      }
    }

#if !SQLITE_OMIT_VACUUM
    /*
** Copy the complete content of pBtFrom into pBtTo.  A transaction
** must be active for both files.
**
** The size of file pTo may be reduced by this operation. If anything
** goes wrong, the transaction on pTo is rolled back. If successful, the
** transaction is committed before returning.
*/
    static int sqlite3BtreeCopyFile( Btree pTo, Btree pFrom )
    {
      int rc;
      sqlite3_backup b;
      sqlite3BtreeEnter( pTo );
      sqlite3BtreeEnter( pFrom );

      /* Set up an sqlite3_backup object. sqlite3_backup.pDestDb must be set
      ** to 0. This is used by the implementations of sqlite3_backup_step()
      ** and sqlite3_backup_finish() to detect that they are being called
      ** from this function, not directly by the user.
      */
      b = new sqlite3_backup();// memset( &b, 0, sizeof( b ) );
      b.pSrcDb = pFrom.db;
      b.pSrc = pFrom;
      b.pDest = pTo;
      b.iNext = 1;

      /* 0x7FFFFFFF is the hard limit for the number of pages in a database
      ** file. By passing this as the number of pages to copy to
      ** sqlite3_backup_step(), we can guarantee that the copy finishes
      ** within a single call (unless an error occurs). The Debug.Assert() statement
      ** checks this assumption - (p.rc) should be set to either SQLITE_DONE
      ** or an error code.
      */
      sqlite3_backup_step( b, 0x7FFFFFFF );
      Debug.Assert( b.rc != SQLITE_OK );
      rc = sqlite3_backup_finish( b );
      if ( rc == SQLITE_OK )
      {
        pTo.pBt.pageSizeFixed = false;
      }

      sqlite3BtreeLeave( pFrom );
      sqlite3BtreeLeave( pTo );
      return rc;
    }
#endif //* SQLITE_OMIT_VACUUM */
  }
}
