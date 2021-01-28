using u8 = System.Byte;
using u32 = System.UInt32;

namespace CS_SQLite3
{
  public partial class CSSQLite
  {
    /*
    ** 2001 September 22
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This is the header file for the generic hash-table implemenation
    ** used in SQLite.
    **
    ** $Id: hash.h,v 1.15 2009/05/02 13:29:38 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#if !_SQLITE_HASH_H_
    //#define _SQLITE_HASH_H_

    /* Forward declarations of structures. */
    //typedef struct Hash Hash;
    //typedef struct HashElem HashElem;

    /* A complete hash table is an instance of the following structure.
    ** The internals of this structure are intended to be opaque -- client
    ** code should not attempt to access or modify the fields of this structure
    ** directly.  Change this structure only by using the routines below.
    ** However, some of the "procedures" and "functions" for modifying and
    ** accessing this structure are really macros, so we can't really make
    ** this structure opaque.
    **
    ** All elements of the hash table are on a single doubly-linked list.
    ** Hash.first points to the head of this list.
    **
    ** There are Hash.htsize buckets.  Each bucket points to a spot in
    ** the global doubly-linked list.  The contents of the bucket are the
    ** element pointed to plus the next _ht.count-1 elements in the list.
    **
    ** Hash.htsize and Hash.ht may be zero.  In that case lookup is done
    ** by a linear search of the global list.  For small tables, the
    ** Hash.ht table is never allocated because if there are few elements
    ** in the table, it is faster to do a linear search than to manage
    ** the hash table.
    */
    public class _ht
    {            /* the hash table */
      public int count;               /* Number of entries with this hash */
      public HashElem chain;         /* Pointer to first entry with this hash */
    };

    public class Hash
    {
      public u32 htsize = 31;     /* Number of buckets in the hash table */
      public u32 count;           /* Number of entries in this table */
      public HashElem first;      /* The first element of the array */
      public _ht[] ht;
      public Hash Copy()
      {
        if ( this == null )
          return null;
        else
        {
          Hash cp = (Hash)MemberwiseClone();
          return cp;
        }
      }
    };

    /* Each element in the hash table is an instance of the following
    ** structure.  All elements are stored on a single doubly-linked list.
    **
    ** Again, this structure is intended to be opaque, but it can't really
    ** be opaque because it is used by macros.
    */
    public class HashElem
    {
      public HashElem next;
      public HashElem prev;          /* Next and previous elements in the table */
      public object data;            /* Data associated with this element */
      public string pKey;
      public int nKey;               /* Key associated with this element */
    };

    /*
    ** Access routines.  To delete, insert a NULL pointer.
    */
    //void sqlite3HashInit(Hash*);
    //void *sqlite3HashInsert(Hash*, const char *pKey, int nKey, void *pData);
    //void *sqlite3HashFind(const Hash*, const char *pKey, int nKey);
    //void sqlite3HashClear(Hash*);

    /*
    ** Macros for looping over all elements of a hash table.  The idiom is
    ** like this:
    **
    **   Hash h;
    **   HashElem p;
    **   ...
    **   for(p=sqliteHashFirst(&h); p; p=sqliteHashNext(p)){
    **     SomeStructure pData = sqliteHashData(p);
    **     // do something with pData
    **   }
    */
    //#define sqliteHashFirst(H)  ((H).first)
    static HashElem sqliteHashFirst( Hash H ) { return H.first; }
    //#define sqliteHashNext(E)   ((E).next)
    static HashElem sqliteHashNext( HashElem E ) { return E.next; }
    //#define sqliteHashData(E)   ((E).data)
    static object sqliteHashData( HashElem E ) { return E.data; }
    /* #define sqliteHashKey(E)    ((E)->pKey) // NOT USED */
    /* #define sqliteHashKeysize(E) ((E)->nKey)  // NOT USED */

    /*
    ** Number of entries in a hash table
    */
    /* #define sqliteHashCount(H)  ((H)->count) // NOT USED */

    //#endif // * _SQLITE_HASH_H_ */
  }
}
