using System.Diagnostics;
using i16 = System.Int16;
using u8 = System.Byte;
using u16 = System.UInt16;

namespace winPEAS._3rdParty.SQLite.src
{
    public partial class CSSQLite
  {
    /*
    ** 2005 May 23
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
    ** This file contains functions used to access the internal hash tables
    ** of user defined functions and collation sequences.
    **
    ** $Id: callback.c,v 1.42 2009/06/17 00:35:31 drh Exp $
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
    ** Invoke the 'collation needed' callback to request a collation sequence
    ** in the database text encoding of name zName, length nName.
    ** If the collation sequence
    */
    static void callCollNeeded( sqlite3 db, string zName )
    {
      Debug.Assert( db.xCollNeeded == null || db.xCollNeeded16 == null );
      if ( db.xCollNeeded != null )
      {
        string zExternal = zName;// sqlite3DbStrDup(db, zName);
        if ( zExternal == null ) return;
        db.xCollNeeded( db.pCollNeededArg, db, db.aDb[0].pSchema.enc, zExternal );//(int)ENC(db), zExternal);
        //sqlite3DbFree( db, ref  zExternal );
      }
#if !SQLITE_OMIT_UTF16
if( db.xCollNeeded16!=null ){
string zExternal;
sqlite3_value pTmp = sqlite3ValueNew(db);
sqlite3ValueSetStr(pTmp, -1, zName, SQLITE_UTF8, SQLITE_STATIC);
zExternal = sqlite3ValueText(pTmp, SQLITE_UTF16NATIVE);
if( zExternal!="" ){
db.xCollNeeded16( db.pCollNeededArg, db, db.aDbStatic[0].pSchema.enc, zExternal );//(int)ENC(db), zExternal);
}
sqlite3ValueFree(ref pTmp);
}
#endif
    }

    /*
    ** This routine is called if the collation factory fails to deliver a
    ** collation function in the best encoding but there may be other versions
    ** of this collation function (for other text encodings) available. Use one
    ** of these instead if they exist. Avoid a UTF-8 <. UTF-16 conversion if
    ** possible.
    */
    static int synthCollSeq( sqlite3 db, CollSeq pColl )
    {
      CollSeq pColl2;
      string z = pColl.zName;
      int i;
      byte[] aEnc = { SQLITE_UTF16BE, SQLITE_UTF16LE, SQLITE_UTF8 };
      for ( i = 0 ; i < 3 ; i++ )
      {
        pColl2 = sqlite3FindCollSeq( db, aEnc[i], z, 0 );
        if ( pColl2.xCmp != null )
        {
          pColl = pColl2.Copy(); //memcpy(pColl, pColl2, sizeof(CollSeq));
          pColl.xDel = null;         /* Do not copy the destructor */
          return SQLITE_OK;
        }
      }
      return SQLITE_ERROR;
    }

    /*
    ** This function is responsible for invoking the collation factory callback
    ** or substituting a collation sequence of a different encoding when the
    ** requested collation sequence is not available in the database native
    ** encoding.
    **
    ** If it is not NULL, then pColl must point to the database native encoding
    ** collation sequence with name zName, length nName.
    **
    ** The return value is either the collation sequence to be used in database
    ** db for collation type name zName, length nName, or NULL, if no collation
    ** sequence can be found.
    **
    ** See also: sqlite3LocateCollSeq(), sqlite3FindCollSeq()
    */
    static CollSeq sqlite3GetCollSeq(
    sqlite3 db,         /* The database connection */
    CollSeq pColl,      /* Collating sequence with native encoding, or NULL */
    string zName        /* Collating sequence name */
    )
    {
      CollSeq p;

      p = pColl;
      if ( p == null )
      {
        p = sqlite3FindCollSeq( db, ENC( db ), zName, 0 );
      }
      if ( p == null || p.xCmp == null )
      {
        /* No collation sequence of this type for this encoding is registered.
        ** Call the collation factory to see if it can supply us with one.
        */
        callCollNeeded( db, zName );
        p = sqlite3FindCollSeq( db, ENC( db ), zName, 0 );
      }
      if ( p != null && p.xCmp == null && synthCollSeq( db, p ) != 0 )
      {
        p = null;
      }
      Debug.Assert( p == null || p.xCmp != null );
      return p;
    }

    /*
    ** This routine is called on a collation sequence before it is used to
    ** check that it is defined. An undefined collation sequence exists when
    ** a database is loaded that contains references to collation sequences
    ** that have not been defined by sqlite3_create_collation() etc.
    **
    ** If required, this routine calls the 'collation needed' callback to
    ** request a definition of the collating sequence. If this doesn't work,
    ** an equivalent collating sequence that uses a text encoding different
    ** from the main database is substituted, if one is available.
    */
    static int sqlite3CheckCollSeq( Parse pParse, CollSeq pColl )
    {
      if ( pColl != null )
      {
        string zName = pColl.zName;
        CollSeq p = sqlite3GetCollSeq( pParse.db, pColl, zName );
        if ( null == p )
        {
          sqlite3ErrorMsg( pParse, "no such collation sequence: %s", zName );
          pParse.nErr++;
          return SQLITE_ERROR;
        }
//
        //Debug.Assert(p == pColl);
        if (p != pColl) // Had to lookup appropriate sequence
        {
          pColl.enc = p.enc;
          pColl.pUser= p.pUser;
          pColl.type = p.type;
          pColl.xCmp = p.xCmp;
          pColl.xDel = p.xDel;
        } 

      }
      return SQLITE_OK;
    }



    /*
    ** Locate and return an entry from the db.aCollSeq hash table. If the entry
    ** specified by zName and nName is not found and parameter 'create' is
    ** true, then create a new entry. Otherwise return NULL.
    **
    ** Each pointer stored in the sqlite3.aCollSeq hash table contains an
    ** array of three CollSeq structures. The first is the collation sequence
    ** prefferred for UTF-8, the second UTF-16le, and the third UTF-16be.
    **
    ** Stored immediately after the three collation sequences is a copy of
    ** the collation sequence name. A pointer to this string is stored in
    ** each collation sequence structure.
    */
    static CollSeq[] findCollSeqEntry(
    sqlite3 db,         /* Database connection */
    string zName,       /* Name of the collating sequence */
    int create          /* Create a new entry if true */
    )
    {
      CollSeq[] pColl;
      int nName = sqlite3Strlen30( zName );
      pColl = (CollSeq[])sqlite3HashFind( db.aCollSeq, zName, nName );

      if ( ( null == pColl ) && create != 0 )
      {
        pColl = new CollSeq[3]; //sqlite3DbMallocZero(db, 3*sizeof(*pColl) + nName + 1 );
        if ( pColl != null )
        {
          CollSeq pDel = null;
          pColl[0] = new CollSeq();
          pColl[0].zName = zName;
          pColl[0].enc = SQLITE_UTF8;
          pColl[1] = new CollSeq();
          pColl[1].zName = zName;
          pColl[1].enc = SQLITE_UTF16LE;
          pColl[2] = new CollSeq();
          pColl[2].zName = zName;
          pColl[2].enc = SQLITE_UTF16BE;
          //memcpy(pColl[0].zName, zName, nName);
          //pColl[0].zName[nName] = 0;
          pDel = (CollSeq)sqlite3HashInsert( ref db.aCollSeq, pColl[0].zName, nName, pColl );

          /* If a malloc() failure occurred in sqlite3HashInsert(), it will
          ** return the pColl pointer to be deleted (because it wasn't added
          ** to the hash table).
          */
          Debug.Assert( pDel == null || pDel == pColl[0] );
          if ( pDel != null )
          {
    ////        db.mallocFailed = 1;
            pDel = null; //was  //sqlite3DbFree(db,ref  pDel);
            pColl = null;
          }
        }
      }
      return pColl;
    }

    /*
    ** Parameter zName points to a UTF-8 encoded string nName bytes long.
    ** Return the CollSeq* pointer for the collation sequence named zName
    ** for the encoding 'enc' from the database 'db'.
    **
    ** If the entry specified is not found and 'create' is true, then create a
    ** new entry.  Otherwise return NULL.
    **
    ** A separate function sqlite3LocateCollSeq() is a wrapper around
    ** this routine.  sqlite3LocateCollSeq() invokes the collation factory
    ** if necessary and generates an error message if the collating sequence
    ** cannot be found.
    **
    ** See also: sqlite3LocateCollSeq(), sqlite3GetCollSeq()
    */
    static CollSeq sqlite3FindCollSeq(
    sqlite3 db,
    u8 enc,
    string zName,
    u8 create
    )
    {
      CollSeq[] pColl;
      if ( zName != null )
      {
        pColl = findCollSeqEntry( db, zName, create );
      }
      else
      {
        pColl = new CollSeq[enc];
        pColl[enc - 1] = db.pDfltColl;
      }
      Debug.Assert( SQLITE_UTF8 == 1 && SQLITE_UTF16LE == 2 && SQLITE_UTF16BE == 3 );
      Debug.Assert( enc >= SQLITE_UTF8 && enc <= SQLITE_UTF16BE );
      if ( pColl != null )
      {
        enc -= 1; // if (pColl != null) pColl += enc - 1;
        return pColl[enc];
      }
      else return null;
    }

    /* During the search for the best function definition, this procedure
    ** is called to test how well the function passed as the first argument
    ** matches the request for a function with nArg arguments in a system
    ** that uses encoding enc. The value returned indicates how well the
    ** request is matched. A higher value indicates a better match.
    **
    ** The returned value is always between 0 and 6, as follows:
    **
    ** 0: Not a match, or if nArg<0 and the function is has no implementation.
    ** 1: A variable arguments function that prefers UTF-8 when a UTF-16
    **    encoding is requested, or vice versa.
    ** 2: A variable arguments function that uses UTF-16BE when UTF-16LE is
    **    requested, or vice versa.
    ** 3: A variable arguments function using the same text encoding.
    ** 4: A function with the exact number of arguments requested that
    **    prefers UTF-8 when a UTF-16 encoding is requested, or vice versa.
    ** 5: A function with the exact number of arguments requested that
    **    prefers UTF-16LE when UTF-16BE is requested, or vice versa.
    ** 6: An exact match.
    **
    */
    static int matchQuality( FuncDef p, int nArg, int enc )
    {
      int match = 0;
      if ( p.nArg == -1 || p.nArg == nArg
      || ( nArg == -1 && ( p.xFunc != null || p.xStep != null ) )
      )
      {
        match = 1;
        if ( p.nArg == nArg || nArg == -1 )
        {
          match = 4;
        }
        if ( enc == p.iPrefEnc )
        {
          match += 2;
        }
        else if ( ( enc == SQLITE_UTF16LE && p.iPrefEnc == SQLITE_UTF16BE ) ||
        ( enc == SQLITE_UTF16BE && p.iPrefEnc == SQLITE_UTF16LE ) )
        {
          match += 1;
        }
      }
      return match;
    }

    /*
    ** Search a FuncDefHash for a function with the given name.  Return
    ** a pointer to the matching FuncDef if found, or 0 if there is no match.
    */
    static FuncDef functionSearch(
    FuncDefHash pHash,  /* Hash table to search */
    int h,              /* Hash of the name */
    string zFunc,       /* Name of function */
    int nFunc           /* Number of bytes in zFunc */
    )
    {
      FuncDef p;
      for ( p = pHash.a[h] ; p != null ; p = p.pHash )
      {
        if ( sqlite3StrNICmp( p.zName, zFunc, nFunc ) == 0 && p.zName.Length == nFunc )
        {
          return p;
        }
      }
      return null;
    }

    /*
    ** Insert a new FuncDef into a FuncDefHash hash table.
    */
    static void sqlite3FuncDefInsert(
    FuncDefHash pHash,  /* The hash table into which to insert */
    FuncDef pDef        /* The function definition to insert */
    )
    {
      FuncDef pOther;
      int nName = sqlite3Strlen30( pDef.zName );
      u8 c1 = (u8)pDef.zName[0];
      int h = ( sqlite3UpperToLower[c1] + nName ) % ArraySize( pHash.a );
      pOther = functionSearch( pHash, h, pDef.zName, nName );
      if ( pOther != null )
      {
        Debug.Assert( pOther != pDef && pOther.pNext != pDef );
        pDef.pNext = pOther.pNext;
        pOther.pNext = pDef;
      }
      else
      {
        pDef.pNext = null;
        pDef.pHash = pHash.a[h];
        pHash.a[h] = pDef;
      }
    }

    /*
    ** Locate a user function given a name, a number of arguments and a flag
    ** indicating whether the function prefers UTF-16 over UTF-8.  Return a
    ** pointer to the FuncDef structure that defines that function, or return
    ** NULL if the function does not exist.
    **
    ** If the createFlag argument is true, then a new (blank) FuncDef
    ** structure is created and liked into the "db" structure if a
    ** no matching function previously existed.  When createFlag is true
    ** and the nArg parameter is -1, then only a function that accepts
    ** any number of arguments will be returned.
    **
    ** If createFlag is false and nArg is -1, then the first valid
    ** function found is returned.  A function is valid if either xFunc
    ** or xStep is non-zero.
    **
    ** If createFlag is false, then a function with the required name and
    ** number of arguments may be returned even if the eTextRep flag does not
    ** match that requested.
    */

    static FuncDef sqlite3FindFunction(
    sqlite3 db,           /* An open database */
    string zName,         /* Name of the function.  Not null-terminated */
    int nName,            /* Number of characters in the name */
    int nArg,             /* Number of arguments.  -1 means any number */
    u8 enc,              /* Preferred text encoding */
    u8 createFlag       /* Create new entry if true and does not otherwise exist */
    )
    {
      FuncDef p;            /* Iterator variable */
      FuncDef pBest = null; /* Best match found so far */
      int bestScore = 0;
      int h;              /* Hash value */

      Debug.Assert( enc == SQLITE_UTF8 || enc == SQLITE_UTF16LE || enc == SQLITE_UTF16BE );
      h = ( sqlite3UpperToLower[(u8)zName[0]] + nName ) % ArraySize( db.aFunc.a );


      /* First search for a match amongst the application-defined functions.
      */
      p = functionSearch( db.aFunc, h, zName, nName );
      while ( p != null )
      {
        int score = matchQuality( p, nArg, enc );
        if ( score > bestScore )
        {
          pBest = p;
          bestScore = score;

        }
        p = p.pNext;
      }


      /* If no match is found, search the built-in functions.
      **
      ** Except, if createFlag is true, that means that we are trying to
      ** install a new function.  Whatever FuncDef structure is returned will
      ** have fields overwritten with new information appropriate for the
      ** new function.  But the FuncDefs for built-in functions are read-only.
      ** So we must not search for built-ins when creating a new function.
      */
      if ( 0 == createFlag && pBest == null )
      {
#if SQLITE_OMIT_WSD
FuncDefHash pHash = GLOBAL( FuncDefHash, sqlite3GlobalFunctions );
#else
        FuncDefHash pHash = sqlite3GlobalFunctions;
#endif
        p = functionSearch( pHash, h, zName, nName );
        while ( p != null )
        {
          int score = matchQuality( p, nArg, enc );
          if ( score > bestScore )
          {
            pBest = p;
            bestScore = score;
          }
          p = p.pNext;
        }
      }

      /* If the createFlag parameter is true and the search did not reveal an
      ** exact match for the name, number of arguments and encoding, then add a
      ** new entry to the hash table and return it.
      */
      if ( createFlag != 0 && ( bestScore < 6 || pBest.nArg != nArg ) &&
      ( pBest = new FuncDef() ) != null )
      { //sqlite3DbMallocZero(db, sizeof(*pBest)+nName+1))!=0 ){
        //pBest.zName = (char *)&pBest[1];
        pBest.nArg = (i16)nArg;
        pBest.iPrefEnc = enc;
        pBest.zName = zName; //memcpy(pBest.zName, zName, nName);
        //pBest.zName[nName] = 0;
        sqlite3FuncDefInsert( db.aFunc, pBest );
      }

      if ( pBest != null && ( pBest.xStep != null || pBest.xFunc != null || createFlag != 0 ) )
      {
        return pBest;
      }
      return null;
    }

    /*
    ** Free all resources held by the schema structure. The void* argument points
    ** at a Schema struct. This function does not call //sqlite3DbFree(db, ) on the
    ** pointer itself, it just cleans up subsiduary resources (i.e. the contents
    ** of the schema hash tables).
    **
    ** The Schema.cache_size variable is not cleared.
    */
    static void sqlite3SchemaFree( Schema p )
    {
      Hash temp1;
      Hash temp2;
      HashElem pElem;
      Schema pSchema = p;

      temp1 = pSchema.tblHash;
      temp2 = pSchema.trigHash;
      sqlite3HashInit( pSchema.trigHash );
      sqlite3HashClear( pSchema.idxHash );
      for ( pElem = sqliteHashFirst( temp2 ) ; pElem != null ; pElem = sqliteHashNext( pElem ) )
      {
        Trigger pTrigger = (Trigger)sqliteHashData( pElem );
        sqlite3DeleteTrigger( null, ref pTrigger );
      }
      sqlite3HashClear( temp2 );
      sqlite3HashInit( pSchema.trigHash );
      for ( pElem = temp1.first ; pElem != null ; pElem = pElem.next )//sqliteHashFirst(&temp1); pElem; pElem = sqliteHashNext(pElem))
      {
        Table pTab = (Table)pElem.data; //sqliteHashData(pElem);
        Debug.Assert( pTab.dbMem == null );
        sqlite3DeleteTable( ref pTab );
      }
      sqlite3HashClear( temp1 );
      pSchema.pSeqTab = null;
      pSchema.flags = (u16)( pSchema.flags & ~DB_SchemaLoaded );
    }

    /*
    ** Find and return the schema associated with a BTree.  Create
    ** a new one if necessary.
    */
    static Schema sqlite3SchemaGet( sqlite3 db, Btree pBt )
    {
      Schema p;
      if ( pBt != null )
      {
        p = sqlite3BtreeSchema( pBt, -1, (dxFreeSchema)sqlite3SchemaFree );//Schema.Length, sqlite3SchemaFree);
      }
      else
      {
        p = new Schema(); // (Schema*)sqlite3MallocZero(Schema).Length;
      }
      if ( p == null )
      {
////        db.mallocFailed = 1;
      }
      else if ( 0 == p.file_format )
      {
        sqlite3HashInit( p.tblHash );
        sqlite3HashInit( p.idxHash );
        sqlite3HashInit( p.trigHash );
        p.enc = SQLITE_UTF8;
      }
      return p;
    }
  }
}
