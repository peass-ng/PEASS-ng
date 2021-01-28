using System;
using System.Diagnostics;
using System.Text;

using Bitmask = System.UInt64;
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;

namespace CS_SQLite3
{
  using sqlite3_int64 = System.Int64;
  using MemJournal = CSSQLite.sqlite3_file;

  public partial class CSSQLite
  {
    /*
    ** 2007 August 22
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
    ** This file contains code use to implement an in-memory rollback journal.
    ** The in-memory rollback journal is used to journal transactions for
    ** ":memory:" databases and when the journal_mode=MEMORY pragma is used.
    **
    ** @(#) $Id: memjournal.c,v 1.12 2009/05/04 11:42:30 danielk1977 Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */

    //#include "sqliteInt.h"

    /* Forward references to internal structures */
    //typedef struct MemJournal MemJournal;
    //typedef struct FilePoint FilePoint;
    //typedef struct FileChunk FileChunk;

    /* Space to hold the rollback journal is allocated in increments of
    ** this many bytes.
    **
    ** The size chosen is a little less than a power of two.  That way,
    ** the FileChunk object will have a size that almost exactly fills
    ** a power-of-two allocation.  This mimimizes wasted space in power-of-two
    ** memory allocators.
    */
    //#define JOURNAL_CHUNKSIZE ((int)(1024-sizeof(FileChunk*)))
    const int JOURNAL_CHUNKSIZE = 4096;

    /* Macro to find the minimum of two numeric values.
    */
    //#if ! MIN
    //# define MIN(x,y) ((x)<(y)?(x):(y))
    //#endif
    static int MIN( int x, int y ) { return ( x < y ) ? x : y; }
    static int MIN( int x, u32 y ) { return ( x < y ) ? x : (int)y; }

    /*
    ** The rollback journal is composed of a linked list of these structures.
    */
    public class FileChunk
    {
      public FileChunk pNext;                             /* Next chunk in the journal */
      public byte[] zChunk = new byte[JOURNAL_CHUNKSIZE]; /* Content of this chunk */
    };

    /*
    ** An instance of this object serves as a cursor into the rollback journal.
    ** The cursor can be either for reading or writing.
    */
    public class FilePoint
    {
      public int iOffset;           /* Offset from the beginning of the file */
      public FileChunk pChunk;      /* Specific chunk into which cursor points */
    };

    /*
    ** This subclass is a subclass of sqlite3_file.  Each open memory-journal
    ** is an instance of this class.
    */
    public partial class sqlite3_file
    {
      //public sqlite3_io_methods pMethods; /* Parent class. MUST BE FIRST */
      public FileChunk pFirst;              /* Head of in-memory chunk-list */
      public FilePoint endpoint;            /* Pointer to the end of the file */
      public FilePoint readpoint;           /* Pointer to the end of the last xRead() */
    };

    /*
    ** Read data from the in-memory journal file.  This is the implementation
    ** of the sqlite3_vfs.xRead method.
    */
    static int memjrnlRead(
    sqlite3_file pJfd,     /* The journal file from which to read */
    byte[] zBuf,           /* Put the results here */
    int iAmt,              /* Number of bytes to read */
    sqlite3_int64 iOfst    /* Begin reading at this offset */
    )
    {
      MemJournal p = (MemJournal)pJfd;
      byte[] zOut = zBuf;
      int nRead = iAmt;
      int iChunkOffset;
      FileChunk pChunk;

      /* SQLite never tries to read past the end of a rollback journal file */
      Debug.Assert( iOfst + iAmt <= p.endpoint.iOffset );

      if ( p.readpoint.iOffset != iOfst || iOfst == 0 )
      {
        int iOff = 0;
        for ( pChunk = p.pFirst ;
        ALWAYS( pChunk != null ) && ( iOff + JOURNAL_CHUNKSIZE ) <= iOfst ;
        pChunk = pChunk.pNext
        )
        {
          iOff += JOURNAL_CHUNKSIZE;
        }
      }
      else
      {
        pChunk = p.readpoint.pChunk;
      }

      iChunkOffset = (int)( iOfst % JOURNAL_CHUNKSIZE );
      int izOut = 0;
      do
      {
        int iSpace = JOURNAL_CHUNKSIZE - iChunkOffset;
        int nCopy = MIN( nRead, ( JOURNAL_CHUNKSIZE - iChunkOffset ) );
        Buffer.BlockCopy( pChunk.zChunk, iChunkOffset, zOut, izOut, nCopy ); //memcpy( zOut, pChunk.zChunk[iChunkOffset], nCopy );
        izOut += nCopy;// zOut += nCopy;
        nRead -= iSpace;
        iChunkOffset = 0;
      } while ( nRead >= 0 && ( pChunk = pChunk.pNext ) != null && nRead > 0 );
      p.readpoint.iOffset = (int)( iOfst + iAmt );
      p.readpoint.pChunk = pChunk;

      return SQLITE_OK;
    }

    /*
    ** Write data to the file.
    */
    static int memjrnlWrite(
    sqlite3_file pJfd,    /* The journal file into which to write */
    byte[] zBuf,          /* Take data to be written from here */
    int iAmt,             /* Number of bytes to write */
    sqlite3_int64 iOfst   /* Begin writing at this offset into the file */
    )
    {
      MemJournal p = (MemJournal)pJfd;
      int nWrite = iAmt;
      byte[] zWrite = zBuf;
      int izWrite = 0;

      /* An in-memory journal file should only ever be appended to. Random
      ** access writes are not required by sqlite.
      */
      Debug.Assert( iOfst == p.endpoint.iOffset );
      UNUSED_PARAMETER( iOfst );

      while ( nWrite > 0 )
      {
        FileChunk pChunk = p.endpoint.pChunk;
        int iChunkOffset = (int)( p.endpoint.iOffset % JOURNAL_CHUNKSIZE );
        int iSpace = MIN( nWrite, JOURNAL_CHUNKSIZE - iChunkOffset );

        if ( iChunkOffset == 0 )
        {
          /* New chunk is required to extend the file. */
          FileChunk pNew = new FileChunk();// sqlite3_malloc( sizeof( FileChunk ) );
          if ( null == pNew )
          {
            return SQLITE_IOERR_NOMEM;
          }
          pNew.pNext = null;
          if ( pChunk != null )
          {
            Debug.Assert( p.pFirst != null );
            pChunk.pNext = pNew;
          }
          else
          {
            Debug.Assert( null == p.pFirst );
            p.pFirst = pNew;
          }
          p.endpoint.pChunk = pNew;
        }

        Buffer.BlockCopy( zWrite, izWrite, p.endpoint.pChunk.zChunk, iChunkOffset, iSpace ); //memcpy( &p.endpoint.pChunk.zChunk[iChunkOffset], zWrite, iSpace );
        izWrite += iSpace;//zWrite += iSpace;
        nWrite -= iSpace;
        p.endpoint.iOffset += iSpace;
      }

      return SQLITE_OK;
    }

    /*
    ** Truncate the file.
    */
    static int memjrnlTruncate( sqlite3_file pJfd, sqlite3_int64 size )
    {
      MemJournal p = (MemJournal)pJfd;
      FileChunk pChunk;
      Debug.Assert( size == 0 );
      UNUSED_PARAMETER( size );
      pChunk = p.pFirst;
      while ( pChunk != null )
      {
        FileChunk pTmp = pChunk;
        pChunk = pChunk.pNext;
        //sqlite3_free( ref pTmp );
      }
      sqlite3MemJournalOpen( pJfd );
      return SQLITE_OK;
    }

    /*
    ** Close the file.
    */
    static int memjrnlClose( MemJournal pJfd )
    {
      memjrnlTruncate( pJfd, 0 );
      return SQLITE_OK;
    }


    /*
    ** Sync the file.
    **
    ** Syncing an in-memory journal is a no-op.  And, in fact, this routine
    ** is never called in a working implementation.  This implementation
    ** exists purely as a contingency, in case some malfunction in some other
    ** part of SQLite causes Sync to be called by mistake.
    */
    static int memjrnlSync( sqlite3_file NotUsed, int NotUsed2 )
    {   /*NO_TEST*/
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );                      /*NO_TEST*/
      Debug.Assert( false );                                       /*NO_TEST*/
      return SQLITE_OK;                                            /*NO_TEST*/
    }                                                              /*NO_TEST*/

    /*
    ** Query the size of the file in bytes.
    */
    static int memjrnlFileSize( sqlite3_file pJfd, ref int pSize )
    {
      MemJournal p = (MemJournal)pJfd;
      pSize = p.endpoint.iOffset;
      return SQLITE_OK;
    }

    /*
    ** Table of methods for MemJournal sqlite3_file object.
    */
    static sqlite3_io_methods MemJournalMethods = new sqlite3_io_methods(
    1,                /* iVersion */
    (dxClose)memjrnlClose,       /* xClose */
    (dxRead)memjrnlRead,         /* xRead */
    (dxWrite)memjrnlWrite,       /* xWrite */
    (dxTruncate)memjrnlTruncate, /* xTruncate */
    (dxSync)memjrnlSync,         /* xSync */
    (dxFileSize)memjrnlFileSize, /* xFileSize */
    null,                        /* xLock */
    null,                        /* xUnlock */
    null,                        /* xCheckReservedLock */
    null,                        /* xFileControl */
    null,                        /* xSectorSize */
    null                         /* xDeviceCharacteristics */
    );

    /*
    ** Open a journal file.
    */
    static void sqlite3MemJournalOpen( sqlite3_file pJfd )
    {
      MemJournal p = (MemJournal)pJfd;
      //memset( p, 0, sqlite3MemJournalSize() );
      p.pFirst = null;
      p.endpoint = new FilePoint();
      p.readpoint = new FilePoint();
      p.pMethods = MemJournalMethods;
    }

    /*
    ** Return true if the file-handle passed as an argument is
    ** an in-memory journal
    */
    static bool sqlite3IsMemJournal( sqlite3_file pJfd )
    {
      return pJfd.pMethods == MemJournalMethods;
    }

    /*
    ** Return the number of bytes required to store a MemJournal that uses vfs
    ** pVfs to create the underlying on-disk files.
    */
    static int sqlite3MemJournalSize()
    {
      return 3096; // sizeof( MemJournal );
    }
  }
}
