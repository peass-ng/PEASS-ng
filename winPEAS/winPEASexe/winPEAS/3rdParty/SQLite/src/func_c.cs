using System;
using System.Diagnostics;
using System.Text;

using sqlite3_int64 = System.Int64;
using i64 = System.Int64;
using u8 = System.Byte;
using u32 = System.UInt32;
using u64 = System.UInt64;

namespace CS_SQLite3
{
  using sqlite3_value = CSSQLite.Mem;
  using sqlite_int64 = System.Int64;

  public partial class CSSQLite
  {
    /*
    ** 2002 February 23
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains the C functions that implement various SQL
    ** functions of SQLite.
    **
    ** There is only one exported symbol in this file - the function
    ** sqliteRegisterBuildinFunctions() found at the bottom of the file.
    ** All other code has file scope.
    **
    ** $Id: func.c,v 1.239 2009/06/19 16:44:41 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
    //#include <stdlib.h>
    //#include <assert.h>
    //#include "vdbeInt.h"


    /*
    ** Return the collating function associated with a function.
    */
    static CollSeq sqlite3GetFuncCollSeq( sqlite3_context context )
    {
      return context.pColl;
    }

    /*
    ** Implementation of the non-aggregate min() and max() functions
    */
    static void minmaxFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      int i;
      int mask;    /* 0 for min() or 0xffffffff for max() */
      int iBest;
      CollSeq pColl;

      Debug.Assert( argc > 1 );
      mask = (int)sqlite3_user_data( context ) == 0 ? 0 : -1;
      pColl = sqlite3GetFuncCollSeq( context );
      Debug.Assert( pColl != null );
      Debug.Assert( mask == -1 || mask == 0 );
      testcase( mask == 0 );
      iBest = 0;
      if ( sqlite3_value_type( argv[0] ) == SQLITE_NULL ) return;
      for ( i = 1 ; i < argc ; i++ )
      {
        if ( sqlite3_value_type( argv[i] ) == SQLITE_NULL ) return;
        if ( ( sqlite3MemCompare( argv[iBest], argv[i], pColl ) ^ mask ) >= 0 )
        {
          iBest = i;
        }
      }
      sqlite3_result_value( context, argv[iBest] );
    }

    /*
    ** Return the type of the argument.
    */
    static void typeofFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] argv
    )
    {
      string z = "";
      UNUSED_PARAMETER( NotUsed );
      switch ( sqlite3_value_type( argv[0] ) )
      {
        case SQLITE_INTEGER: z = "integer"; break;
        case SQLITE_TEXT: z = "text"; break;
        case SQLITE_FLOAT: z = "real"; break;
        case SQLITE_BLOB: z = "blob"; break;
        default: z = "null"; break;
      }
      sqlite3_result_text( context, z, -1, SQLITE_STATIC );
    }


    /*
    ** Implementation of the length() function
    */
    static void lengthFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      int len;

      Debug.Assert( argc == 1 );
      UNUSED_PARAMETER( argc );
      switch ( sqlite3_value_type( argv[0] ) )
      {
        case SQLITE_BLOB:
        case SQLITE_INTEGER:
        case SQLITE_FLOAT:
          {
            sqlite3_result_int( context, sqlite3_value_bytes( argv[0] ) );
            break;
          }
        case SQLITE_TEXT:
          {
            byte[] z = sqlite3_value_blob( argv[0] );
            if ( z == null ) return;
            len = 0;
            int iz = 0;
            while ( iz < z.Length && z[iz] != '\0' )
            {
              len++;
              SQLITE_SKIP_UTF8( z, ref iz );
            }
            sqlite3_result_int( context, len );
            break;
          }
        default:
          {
            sqlite3_result_null( context );
            break;
          }
      }
    }

    /*
    ** Implementation of the abs() function
    */
    static void absFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      Debug.Assert( argc == 1 );
      UNUSED_PARAMETER( argc );
      switch ( sqlite3_value_type( argv[0] ) )
      {
        case SQLITE_INTEGER:
          {
            i64 iVal = sqlite3_value_int64( argv[0] );
            if ( iVal < 0 )
            {
              if ( ( iVal << 1 ) == 0 )
              {
                sqlite3_result_error( context, "integer overflow", -1 );
                return;
              }
              iVal = -iVal;
            }
            sqlite3_result_int64( context, iVal );
            break;
          }
        case SQLITE_NULL:
          {
            sqlite3_result_null( context );
            break;
          }
        default:
          {
            double rVal = sqlite3_value_double( argv[0] );
            if ( rVal < 0 ) rVal = -rVal;
            sqlite3_result_double( context, rVal );
            break;
          }
      }
    }

    /*
    ** Implementation of the substr() function.
    **
    ** substr(x,p1,p2)  returns p2 characters of x[] beginning with p1.
    ** p1 is 1-indexed.  So substr(x,1,1) returns the first character
    ** of x.  If x is text, then we actually count UTF-8 characters.
    ** If x is a blob, then we count bytes.
    **
    ** If p1 is negative, then we begin abs(p1) from the end of x[].
    */
    static void substrFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      string z = "";
      byte[] zBLOB = null;
      string z2;
      int len;
      int p0type;
      int p1, p2;
      int negP2 = 0;

      Debug.Assert( argc == 3 || argc == 2 );
      if ( sqlite3_value_type( argv[1] ) == SQLITE_NULL
      || ( argc == 3 && sqlite3_value_type( argv[2] ) == SQLITE_NULL )
      )
      {
        return;
      }
      p0type = sqlite3_value_type( argv[0] );
      if ( p0type == SQLITE_BLOB )
      {
        len = sqlite3_value_bytes( argv[0] );
        zBLOB = argv[0].zBLOB;
        if ( zBLOB == null ) return;
        Debug.Assert( len == zBLOB.Length );
      }
      else
      {
        z = sqlite3_value_text( argv[0] );
        if ( z == null ) return;
        len = z.Length;
        //len = 0;
        //for ( z2 = z ; z2 != "" ; len++ )
        //{
        //  SQLITE_SKIP_UTF8( ref z2 );
        //}
      }
      p1 = sqlite3_value_int( argv[1] );
      if ( argc == 3 )
      {
        p2 = sqlite3_value_int( argv[2] );
        if ( p2 < 0 )
        {
          p2 = -p2;
          negP2 = 1;
        }
      }
      else
      {
        p2 = ( sqlite3_context_db_handle( context ) ).aLimit[SQLITE_LIMIT_LENGTH];
      }
      if ( p1 < 0 )
      {
        p1 += len;
        if ( p1 < 0 )
        {
          p2 += p1;
          if ( p2 < 0 ) p2 = 0;
          p1 = 0;
        }
      }
      else if ( p1 > 0 )
      {
        p1--;
      }
      else if ( p2 > 0 )
      {
        p2--;
      }
      if ( negP2 != 0 )
      {
        p1 -= p2;
        if ( p1 < 0 )
        {
          p2 += p1;
          p1 = 0;
        }
      }
      Debug.Assert( p1 >= 0 && p2 >= 0 );
      if ( p1 + p2 > len )
      {
        p2 = len - p1;
        if ( p2 < 0 ) p2 = 0;
      }
      if ( p0type != SQLITE_BLOB )
      {
        //while ( z != "" && p1 != 0 )
        //{
        //  SQLITE_SKIP_UTF8( ref z );
        //  p1--;
        //}
        //for ( z2 = z ; z2 != "" && p2 != 0 ; p2-- )
        //{
        //  SQLITE_SKIP_UTF8( ref z2 );
        //}
        sqlite3_result_text( context, z.Length == 0 || p1 > z.Length ? "" : z.Substring( p1, p2 ), (int)p2, SQLITE_TRANSIENT );
      }
      else
      {
        StringBuilder sb = new StringBuilder( zBLOB.Length );
        if ( zBLOB.Length == 0 || p1 > zBLOB.Length ) sb.Length = 0;
        else
        {
          for ( int i = p1 ; i < p1 + p2 ; i++ ) { sb.Append( (char)zBLOB[i] ); }
        }

        sqlite3_result_blob( context, sb.ToString(), (int)p2, SQLITE_TRANSIENT );
      }
    }

    /*
    ** Implementation of the round() function
    */
#if !SQLITE_OMIT_FLOATING_POINT
    static void roundFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      int n = 0;
      double r;
      string zBuf = "";
      Debug.Assert( argc == 1 || argc == 2 );
      if ( argc == 2 )
      {
        if ( SQLITE_NULL == sqlite3_value_type( argv[1] ) ) return;
        n = sqlite3_value_int( argv[1] );
        if ( n > 30 ) n = 30;
        if ( n < 0 ) n = 0;
      }
      if ( sqlite3_value_type( argv[0] ) == SQLITE_NULL ) return;
      r = sqlite3_value_double( argv[0] );
      zBuf = sqlite3_mprintf( "%.*f", n, r );
      if ( zBuf == null )
      {
        sqlite3_result_error_nomem( context );
      }
      else
      {
        sqlite3AtoF( zBuf, ref r );
        //sqlite3_free( ref zBuf );
        sqlite3_result_double( context, r );
      }
    }
#endif

    /*
** Allocate nByte bytes of space using sqlite3_malloc(). If the
** allocation fails, call sqlite3_result_error_nomem() to notify
** the database handle that malloc() has failed and return NULL.
** If nByte is larger than the maximum string or blob length, then
** raise an SQLITE_TOOBIG exception and return NULL.
*/
    //static void* contextMalloc( sqlite3_context* context, i64 nByte )
    //{
    //  char* z;
    //  sqlite3* db = sqlite3_context_db_handle( context );
    //  assert( nByte > 0 );
    //  testcase( nByte == db->aLimit[SQLITE_LIMIT_LENGTH] );
    //  testcase( nByte == db->aLimit[SQLITE_LIMIT_LENGTH] + 1 );
    //  if ( nByte > db->aLimit[SQLITE_LIMIT_LENGTH] )
    //  {
    //    sqlite3_result_error_toobig( context );
    //    z = 0;
    //  }
    //  else
    //  {
    //    z = sqlite3Malloc( (int)nByte );
    //    if ( !z )
    //    {
    //      sqlite3_result_error_nomem( context );
    //    }
    //  }
    //  return z;
    //}

    /*
    ** Implementation of the upper() and lower() SQL functions.
    */
    static void upperFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      string z1;
      string z2;
      int i, n;
      UNUSED_PARAMETER( argc );
      z2 = sqlite3_value_text( argv[0] );
      n = sqlite3_value_bytes( argv[0] );
      /* Verify that the call to _bytes() does not invalidate the _text() pointer */
      //Debug.Assert( z2 == sqlite3_value_text( argv[0] ) );
      if ( z2 != null )
      {
        //z1 = new byte[n];// contextMalloc(context, ((i64)n)+1);
        //if ( z1 !=null)
        //{
        //  memcpy( z1, z2, n + 1 );
        //for ( i = 0 ; i< z1.Length ; i++ )
        //{
        //(char)sqlite3Toupper( z1[i] );
        //}
        sqlite3_result_text(context, z2.Length == 0 ? "" : z2.Substring(0, n).ToUpper(), -1, null); //sqlite3_free );
        // }
      }
    }

    static void lowerFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      string z1;
      string z2;
      int i, n;
      UNUSED_PARAMETER( argc );
      z2 = sqlite3_value_text( argv[0] );
      n = sqlite3_value_bytes( argv[0] );
      /* Verify that the call to _bytes() does not invalidate the _text() pointer */
      //Debug.Assert( z2 == sqlite3_value_text( argv[0] ) );
      if ( z2 != null )
      {
        //z1 = contextMalloc(context, ((i64)n)+1);
        //if ( z1 )
        //{
        //  memcpy( z1, z2, n + 1 );
        //  for ( i = 0 ; z1[i] ; i++ )
        //  {
        //    z1[i] = (char)sqlite3Tolower( z1[i] );
        //  }
        z1 = z2.Length == 0 ? "" : z2.Substring( 0, n ).ToLower();
        sqlite3_result_text(context, z1, -1, null);//sqlite3_free );
        //}
      }
    }

    /*
    ** Implementation of the IFNULL(), NVL(), and COALESCE() functions.
    ** All three do the same thing.  They return the first non-NULL
    ** argument.
    */
    static void ifnullFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      int i;
      for ( i = 0 ; i < argc ; i++ )
      {
        if ( SQLITE_NULL != sqlite3_value_type( argv[i] ) )
        {
          sqlite3_result_value( context, argv[i] );
          break;
        }
      }
    }

    /*
    ** Implementation of random().  Return a random integer.
    */
    static void randomFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] NotUsed2
    )
    {
      sqlite_int64 r = 0;
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      sqlite3_randomness( sizeof( sqlite_int64 ), ref r );
      if ( r < 0 )
      {
        /* We need to prevent a random number of 0x8000000000000000
        ** (or -9223372036854775808) since when you do abs() of that
        ** number of you get the same value back again.  To do this
        ** in a way that is testable, mask the sign bit off of negative
        ** values, resulting in a positive value.  Then take the
        ** 2s complement of that positive value.  The end result can
        ** therefore be no less than -9223372036854775807.
        */
        r = -( r ^ ( ( (sqlite3_int64)1 ) << 63 ) );
      }
      sqlite3_result_int64( context, r );
    }

    /*
    ** Implementation of randomblob(N).  Return a random blob
    ** that is N bytes long.
    */
    static void randomBlob(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      int n;
      char[] p;
      Debug.Assert( argc == 1 );
      UNUSED_PARAMETER( argc );
      n = sqlite3_value_int( argv[0] );
      if ( n < 1 )
      {
        n = 1;
      }
      p = new char[n]; //contextMalloc( context, n );
      if ( p != null )
      {
        i64 _p = 0;
        for ( int i = 0 ; i < n ; i++ )
        {
          sqlite3_randomness( sizeof( u8 ), ref _p );
          p[i] = (char)( _p & 0x7F );
        }
        sqlite3_result_blob( context, new string( p ), n,  null);//sqlite3_free );
      }
    }

    /*
    ** Implementation of the last_insert_rowid() SQL function.  The return
    ** value is the same as the sqlite3_last_insert_rowid() API function.
    */
    static void last_insert_rowid(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] NotUsed2
    )
    {
      sqlite3 db = sqlite3_context_db_handle( context );
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      sqlite3_result_int64( context, sqlite3_last_insert_rowid( db ) );
    }

    /*
    ** Implementation of the changes() SQL function.  The return value is the
    ** same as the sqlite3_changes() API function.
    */
    static void changes(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] NotUsed2
    )
    {
      sqlite3 db = sqlite3_context_db_handle( context );
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      sqlite3_result_int( context, sqlite3_changes( db ) );
    }

    /*
    ** Implementation of the total_changes() SQL function.  The return value is
    ** the same as the sqlite3_total_changes() API function.
    */
    static void total_changes(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] NotUsed2
    )
    {
      sqlite3 db = (sqlite3)sqlite3_context_db_handle( context );
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      sqlite3_result_int( context, sqlite3_total_changes( db ) );
    }

    /*
    ** A structure defining how to do GLOB-style comparisons.
    */
    struct compareInfo
    {
      public char matchAll;
      public char matchOne;
      public char matchSet;
      public bool noCase;
      public compareInfo( char matchAll, char matchOne, char matchSet, bool noCase )
      {
        this.matchAll = matchAll;
        this.matchOne = matchOne;
        this.matchSet = matchSet;
        this.noCase = noCase;
      }
    };

    /*
    ** For LIKE and GLOB matching on EBCDIC machines, assume that every
    ** character is exactly one byte in size.  Also, all characters are
    ** able to participate in upper-case-to-lower-case mappings in EBCDIC
    ** whereas only characters less than 0x80 do in ASCII.
    */
#if (SQLITE_EBCDIC)
//# define sqlite3Utf8Read(A,C)    (*(A++))
//# define GlogUpperToLower(A)     A = sqlite3UpperToLower[A]
#else
    //# define GlogUpperToLower(A)     if( A<0x80 ){ A = sqlite3UpperToLower[A]; }
#endif

    static compareInfo globInfo = new compareInfo( '*', '?', '[', false );
    /* The correct SQL-92 behavior is for the LIKE operator to ignore
    ** case.  Thus  'a' LIKE 'A' would be true. */
    static compareInfo likeInfoNorm = new compareInfo( '%', '_', '\0', true );
    /* If SQLITE_CASE_SENSITIVE_LIKE is defined, then the LIKE operator
    ** is case sensitive causing 'a' LIKE 'A' to be false */
    static compareInfo likeInfoAlt = new compareInfo( '%', '_', '\0', false );

    /*
    ** Compare two UTF-8 strings for equality where the first string can
    ** potentially be a "glob" expression.  Return true (1) if they
    ** are the same and false (0) if they are different.
    **
    ** Globbing rules:
    **
    **      '*'       Matches any sequence of zero or more characters.
    **
    **      '?'       Matches exactly one character.
    **
    **     [...]      Matches one character from the enclosed list of
    **                characters.
    **
    **     [^...]     Matches one character not in the enclosed list.
    **
    ** With the [...] and [^...] matching, a ']' character can be included
    ** in the list by making it the first character after '[' or '^'.  A
    ** range of characters can be specified using '-'.  Example:
    ** "[a-z]" matches any single lower-case letter.  To match a '-', make
    ** it the last character in the list.
    **
    ** This routine is usually quick, but can be N**2 in the worst case.
    **
    ** Hints: to match '*' or '?', put them in "[]".  Like this:
    **
    **         abc[*]xyz        Matches "abc*xyz" only
    */
    static bool patternCompare(
    string zPattern,            /* The glob pattern */
    string zString,             /* The string to compare against the glob */
    compareInfo pInfo,          /* Information about how to do the compare */
    int esc                     /* The escape character */
    )
    {
      int c, c2;
      int invert;
      int seen;
      int matchOne = (int)pInfo.matchOne;
      int matchAll = (int)pInfo.matchAll;
      int matchSet = (int)pInfo.matchSet;
      bool noCase = pInfo.noCase;
      bool prevEscape = false;     /* True if the previous character was 'escape' */
      string inPattern = zPattern; //Entered Pattern

      while ( ( c = sqlite3Utf8Read( zPattern, ref zPattern ) ) != 0 )
      {
        if ( !prevEscape && c == matchAll )
        {
          while ( ( c = sqlite3Utf8Read( zPattern, ref zPattern ) ) == matchAll
          || c == matchOne )
          {
            if ( c == matchOne && sqlite3Utf8Read( zString, ref zString ) == 0 )
            {
              return false;
            }
          }
          if ( c == 0 )
          {
            return true;
          }
          else if ( c == esc )
          {
            c = sqlite3Utf8Read( zPattern, ref zPattern );
            if ( c == 0 )
            {
              return false;
            }
          }
          else if ( c == matchSet )
          {
            Debug.Assert( esc == 0 );         /* This is GLOB, not LIKE */
            Debug.Assert( matchSet < 0x80 );  /* '[' is a single-byte character */
            int len = 0;
            while ( len < zString.Length && patternCompare( inPattern.Substring( inPattern.Length - zPattern.Length - 1 ), zString.Substring( len ), pInfo, esc ) == false )
            {
              SQLITE_SKIP_UTF8( zString, ref len );
            }
            return len < zString.Length;
          }
          while ( ( c2 = sqlite3Utf8Read( zString, ref zString ) ) != 0 )
          {
            if ( noCase )
            {
              if ( c2 < 0x80 ) c2 = sqlite3UpperToLower[c2]; //GlogUpperToLower(c2);
              if ( c < 0x80 ) c = sqlite3UpperToLower[c]; //GlogUpperToLower(c);
              while ( c2 != 0 && c2 != c )
              {
                c2 = sqlite3Utf8Read( zString, ref zString );
                if ( c2 < 0x80 ) c2 = sqlite3UpperToLower[c2]; //GlogUpperToLower(c2);
              }
            }
            else
            {
              while ( c2 != 0 && c2 != c )
              {
                c2 = sqlite3Utf8Read( zString, ref zString );
              }
            }
            if ( c2 == 0 ) return false;
            if ( patternCompare( zPattern, zString, pInfo, esc ) ) return true;
          }
          return false;
        }
        else if ( !prevEscape && c == matchOne )
        {
          if ( sqlite3Utf8Read( zString, ref zString ) == 0 )
          {
            return false;
          }
        }
        else if ( c == matchSet )
        {
          int prior_c = 0;
          Debug.Assert( esc == 0 );    /* This only occurs for GLOB, not LIKE */
          seen = 0;
          invert = 0;
          c = sqlite3Utf8Read( zString, ref zString );
          if ( c == 0 ) return false;
          c2 = sqlite3Utf8Read( zPattern, ref zPattern );
          if ( c2 == '^' )
          {
            invert = 1;
            c2 = sqlite3Utf8Read( zPattern, ref zPattern );
          }
          if ( c2 == ']' )
          {
            if ( c == ']' ) seen = 1;
            c2 = sqlite3Utf8Read( zPattern, ref zPattern );
          }
          while ( c2 != 0 && c2 != ']' )
          {
            if ( c2 == '-' && zPattern[0] != ']' && zPattern[0] != 0 && prior_c > 0 )
            {
              c2 = sqlite3Utf8Read( zPattern, ref zPattern );
              if ( c >= prior_c && c <= c2 ) seen = 1;
              prior_c = 0;
            }
            else
            {
              if ( c == c2 )
              {
                seen = 1;
              }
              prior_c = c2;
            }
            c2 = sqlite3Utf8Read( zPattern, ref zPattern );
          }
          if ( c2 == 0 || ( seen ^ invert ) == 0 )
          {
            return false;
          }
        }
        else if ( esc == c && !prevEscape )
        {
          prevEscape = true;
        }
        else
        {
          c2 = sqlite3Utf8Read( zString, ref zString );
          if ( noCase )
          {
            if ( c < 0x80 ) c = sqlite3UpperToLower[c]; //GlogUpperToLower(c);
            if ( c2 < 0x80 ) c2 = sqlite3UpperToLower[c2]; //GlogUpperToLower(c2);
          }
          if ( c != c2 )
          {
            return false;
          }
          prevEscape = false;
        }
      }
      return zString.Length == 0;
    }

    /*
    ** Count the number of times that the LIKE operator (or GLOB which is
    ** just a variation of LIKE) gets called.  This is used for testing
    ** only.
    */
#if SQLITE_TEST
    //static int sqlite3_like_count = 0;
#endif


    /*
** Implementation of the like() SQL function.  This function implements
** the build-in LIKE operator.  The first argument to the function is the
** pattern and the second argument is the string.  So, the SQL statements:
**
**       A LIKE B
**
** is implemented as like(B,A).
**
** This same function (with a different compareInfo structure) computes
** the GLOB operator.
*/
    static void likeFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      string zA, zB;
      int escape = 0;
      int nPat;
      sqlite3 db = sqlite3_context_db_handle( context );

      zB = sqlite3_value_text( argv[0] );
      zA = sqlite3_value_text( argv[1] );

      /* Limit the length of the LIKE or GLOB pattern to avoid problems
      ** of deep recursion and N*N behavior in patternCompare().
      */
      nPat = sqlite3_value_bytes( argv[0] );
      testcase( nPat == db.aLimit[SQLITE_LIMIT_LIKE_PATTERN_LENGTH] );
      testcase( nPat == db.aLimit[SQLITE_LIMIT_LIKE_PATTERN_LENGTH] + 1 );
      if ( nPat > db.aLimit[SQLITE_LIMIT_LIKE_PATTERN_LENGTH] )
      {
        sqlite3_result_error( context, "LIKE or GLOB pattern too complex", -1 );
        return;
      }
      //Debug.Assert( zB == sqlite3_value_text( argv[0] ) );  /* Encoding did not change */

      if ( argc == 3 )
      {
        /* The escape character string must consist of a single UTF-8 character.
        ** Otherwise, return an error.
        */
        string zEsc = sqlite3_value_text( argv[2] );
        if ( zEsc == null ) return;
        if ( sqlite3Utf8CharLen( zEsc, -1 ) != 1 )
        {
          sqlite3_result_error( context,
          "ESCAPE expression must be a single character", -1 );
          return;
        }
        escape = sqlite3Utf8Read( zEsc, ref zEsc );
      }
      if ( zA != null && zB != null )
      {
        compareInfo pInfo = (compareInfo)sqlite3_user_data( context );
#if SQLITE_TEST
        sqlite3_like_count.iValue++;
#endif
        sqlite3_result_int( context, patternCompare( zB, zA, pInfo, escape ) ? 1 : 0 );
      }
    }

    /*
    ** Implementation of the NULLIF(x,y) function.  The result is the first
    ** argument if the arguments are different.  The result is NULL if the
    ** arguments are equal to each other.
    */
    static void nullifFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] argv
    )
    {
      CollSeq pColl = sqlite3GetFuncCollSeq( context );
      UNUSED_PARAMETER( NotUsed );
      if ( sqlite3MemCompare( argv[0], argv[1], pColl ) != 0 )
      {
        sqlite3_result_value( context, argv[0] );
      }
    }

    /*
    ** Implementation of the VERSION(*) function.  The result is the version
    ** of the SQLite library that is running.
    */
    static void versionFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] NotUsed2
    )
    {
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      sqlite3_result_text( context, sqlite3_version, -1, SQLITE_STATIC );
    }

    /* Array for converting from half-bytes (nybbles) into ASCII hex
    ** digits. */
    static char[] hexdigits = new char[]  {
'0', '1', '2', '3', '4', '5', '6', '7',
'8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
};

    /*
    ** EXPERIMENTAL - This is not an official function.  The interface may
    ** change.  This function may disappear.  Do not write code that depends
    ** on this function.
    **
    ** Implementation of the QUOTE() function.  This function takes a single
    ** argument.  If the argument is numeric, the return value is the same as
    ** the argument.  If the argument is NULL, the return value is the string
    ** "NULL".  Otherwise, the argument is enclosed in single quotes with
    ** single-quote escapes.
    */
    static void quoteFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      Debug.Assert( argc == 1 );
      UNUSED_PARAMETER( argc );

      switch ( sqlite3_value_type( argv[0] ) )
      {
        case SQLITE_INTEGER:
        case SQLITE_FLOAT:
          {
            sqlite3_result_value( context, argv[0] );
            break;
          }
        case SQLITE_BLOB:
          {
            StringBuilder zText;
            byte[] zBlob = sqlite3_value_blob( argv[0] );
            int nBlob = sqlite3_value_bytes( argv[0] );
            Debug.Assert( zBlob.Length == sqlite3_value_blob( argv[0] ).Length ); /* No encoding change */
            zText = new StringBuilder( 2 * nBlob + 4 );//(char*)contextMalloc(context, (2*(i64)nBlob)+4);
            zText.Append( "X'" );
            if ( zText != null )
            {
              int i;
              for ( i = 0 ; i < nBlob ; i++ )
              {
                zText.Append( hexdigits[( zBlob[i] >> 4 ) & 0x0F] );
                zText.Append( hexdigits[( zBlob[i] ) & 0x0F] );
              }
              zText.Append( "'" );
              //zText[( nBlob * 2 ) + 2] = '\'';
              //zText[( nBlob * 2 ) + 3] = '\0';
              //zText[0] = 'X';
              //zText[1] = '\'';
              sqlite3_result_text( context, zText.ToString(), -1, SQLITE_TRANSIENT );
              //sqlite3_free( ref  zText );
            }
            break;
          }
        case SQLITE_TEXT:
          {
            int i, j;
            int n;
            string zArg = sqlite3_value_text( argv[0] );
            StringBuilder z;

            if ( zArg == null || zArg.Length == 0 ) return;
            for ( i = 0, n = 0 ; i < zArg.Length ; i++ ) { if ( zArg[i] == '\'' ) n++; }
            z = new StringBuilder( i + n + 3 );// contextMalloc(context, ((i64)i)+((i64)n)+3);
            if ( z != null )
            {
              z.Append( '\'' );
              for ( i = 0, j = 1 ; i < zArg.Length && zArg[i] != 0 ; i++ )
              {
                z.Append( (char)zArg[i] ); j++;
                if ( zArg[i] == '\'' )
                {
                  z.Append( '\'' ); j++;
                }
              }
              z.Append( '\'' ); j++;
              //z[j] = '\0'; ;
              sqlite3_result_text(context, z.ToString(), j, null);//sqlite3_free );
            }
            break;
          }
        default:
          {
            Debug.Assert( sqlite3_value_type( argv[0] ) == SQLITE_NULL );
            sqlite3_result_text( context, "NULL", 4, SQLITE_STATIC );
            break;
          }
      }
    }

    /*
    ** The hex() function.  Interpret the argument as a blob.  Return
    ** a hexadecimal rendering as text.
    */
    static void hexFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      int i, n;
      byte[] pBlob;
      //string zHex, z;
      Debug.Assert( argc == 1 );
      UNUSED_PARAMETER( argc );
      pBlob = sqlite3_value_blob( argv[0] );
      n = sqlite3_value_bytes( argv[0] );
      Debug.Assert( n == pBlob.Length );  /* No encoding change */
      StringBuilder zHex = new StringBuilder( n * 2 + 1 );
      //  z = zHex = contextMalloc(context, ((i64)n)*2 + 1);
      if ( zHex != null )
      {
        for ( i = 0 ; i < n ; i++ )
        {//, pBlob++){
          byte c = pBlob[i];
          zHex.Append( hexdigits[( c >> 4 ) & 0xf] );
          zHex.Append( hexdigits[c & 0xf] );
        }
        sqlite3_result_text(context, zHex.ToString(), n * 2, null); //sqlite3_free );
      }
    }

    /*
    ** The zeroblob(N) function returns a zero-filled blob of size N bytes.
    */
    static void zeroblobFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      i64 n;
      sqlite3 db = sqlite3_context_db_handle( context );
      Debug.Assert( argc == 1 );
      UNUSED_PARAMETER( argc );
      n = sqlite3_value_int64( argv[0] );
      testcase( n == db.aLimit[SQLITE_LIMIT_LENGTH] );
      testcase( n == db.aLimit[SQLITE_LIMIT_LENGTH] + 1 );
      if ( n > db.aLimit[SQLITE_LIMIT_LENGTH] )
      {
        sqlite3_result_error_toobig( context );
      }
      else
      {
        sqlite3_result_zeroblob( context, (int)n );
      }
    }

    /*
    ** The replace() function.  Three arguments are all strings: call
    ** them A, B, and C. The result is also a string which is derived
    ** from A by replacing every occurance of B with C.  The match
    ** must be exact.  Collating sequences are not used.
    */
    static void replaceFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      string zStr;        /* The input string A */
      string zPattern;    /* The pattern string B */
      string zRep;        /* The replacement string C */
      string zOut;              /* The output */
      int nStr;                /* Size of zStr */
      int nPattern;            /* Size of zPattern */
      int nRep;                /* Size of zRep */
      int nOut;                /* Maximum size of zOut */
      //int loopLimit;           /* Last zStr[] that might match zPattern[] */
      int i, j;                /* Loop counters */

      Debug.Assert( argc == 3 );
      UNUSED_PARAMETER( argc );
      zStr = sqlite3_value_text( argv[0] );
      if ( zStr == null ) return;
      nStr = sqlite3_value_bytes( argv[0] );
      Debug.Assert( zStr == sqlite3_value_text( argv[0] ) );  /* No encoding change */
      zPattern = sqlite3_value_text( argv[1] );
      if ( zPattern == null )
      {
        Debug.Assert( sqlite3_value_type( argv[1] ) == SQLITE_NULL
        //|| sqlite3_context_db_handle( context ).mallocFailed != 0
        );
        return;
      }
      if ( zPattern == "" )
      {
        Debug.Assert( sqlite3_value_type( argv[1] ) != SQLITE_NULL );
        sqlite3_result_value( context, argv[0] );
        return;
      }
      nPattern = sqlite3_value_bytes( argv[1] );
      Debug.Assert( zPattern == sqlite3_value_text( argv[1] ) );  /* No encoding change */
      zRep = sqlite3_value_text( argv[2] );
      if ( zRep == null ) return;
      nRep = sqlite3_value_bytes( argv[2] );
      Debug.Assert( zRep == sqlite3_value_text( argv[2] ) );
      nOut = nStr + 1;
      Debug.Assert( nOut < SQLITE_MAX_LENGTH );
      //zOut = contextMalloc(context, (i64)nOut);
      //if( zOut==0 ){
      //  return;
      //}
      //loopLimit = nStr - nPattern;
      //for(i=j=0; i<=loopLimit; i++){
      //  if( zStr[i]!=zPattern[0] || memcmp(&zStr[i], zPattern, nPattern) ){
      //    zOut[j++] = zStr[i];
      //  }else{
      //    u8 *zOld;
      // sqlite3 db = sqlite3_context_db_handle( context );
      //    nOut += nRep - nPattern;
      //testcase( nOut-1==db->aLimit[SQLITE_LIMIT_LENGTH] );
      //testcase( nOut-2==db->aLimit[SQLITE_LIMIT_LENGTH] );
      //if( nOut-1>db->aLimit[SQLITE_LIMIT_LENGTH] ){
      //      sqlite3_result_error_toobig(context);
      //      //sqlite3DbFree(db,ref  zOut);
      //      return;
      //    }
      //    zOld = zOut;
      //    zOut = sqlite3_realloc(zOut, (int)nOut);
      //    if( zOut==0 ){
      //      sqlite3_result_error_nomem(context);
      //      //sqlite3DbFree(db,ref  zOld);
      //      return;
      //    }
      //    memcpy(&zOut[j], zRep, nRep);
      //    j += nRep;
      //    i += nPattern-1;
      //  }
      //}
      //Debug.Assert( j+nStr-i+1==nOut );
      //memcpy(&zOut[j], zStr[i], nStr-i);
      //j += nStr - i;
      //Debug.Assert( j<=nOut );
      //zOut[j] = 0;
      zOut = zStr.Replace( zPattern, zRep );
      j = zOut.Length;
      sqlite3_result_text(context, zOut, j, null);//sqlite3_free );
    }

    /*
    ** Implementation of the TRIM(), LTRIM(), and RTRIM() functions.
    ** The userdata is 0x1 for left trim, 0x2 for right trim, 0x3 for both.
    */
    static void trimFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      string zIn;           /* Input string */
      string zCharSet;      /* Set of characters to trim */
      int nIn;              /* Number of bytes in input */
      int izIn = 0;         /* C# string pointer */
      int flags;            /* 1: trimleft  2: trimright  3: trim */
      int i;                /* Loop counter */
      int[] aLen = null;    /* Length of each character in zCharSet */
      byte[][] azChar = null; /* Individual characters in zCharSet */
      int nChar = 0;          /* Number of characters in zCharSet */
      byte[] zBytes = null;
      byte[] zBlob = null;

      if ( sqlite3_value_type( argv[0] ) == SQLITE_NULL )
      {
        return;
      }
      zIn = sqlite3_value_text( argv[0] );
      if ( zIn == null ) return;
      nIn = sqlite3_value_bytes( argv[0] );
      zBlob = sqlite3_value_blob( argv[0] );
      //Debug.Assert( zIn == sqlite3_value_text( argv[0] ) );
      if ( argc == 1 )
      {
        int[] lenOne = new int[] { 1 };
        byte[] azOne = new byte[] { (u8)' ' };//static unsigned char * const azOne[] = { (u8*)" " };
        nChar = 1;
        aLen = lenOne;
        azChar = new byte[1][];
        azChar[0] = azOne;
        zCharSet = null;
      }
      else if ( ( zCharSet = sqlite3_value_text( argv[1] ) ) == null )
      {
        return;
      }
      else
      {
        zBytes = sqlite3_value_blob( argv[1] );
        int iz = 0;
        for ( nChar = 0 ; iz < zBytes.Length ; nChar++ )
        {
          SQLITE_SKIP_UTF8( zBytes, ref iz );
        }
        if ( nChar > 0 )
        {
          azChar = new byte[nChar][];//contextMalloc(context, ((i64)nChar)*(sizeof(char*)+1));
          if ( azChar == null )
          {
            return;
          }
          aLen = new int[nChar];

          int iz0 = 0;
          int iz1 = 0;
          for ( int ii = 0 ; ii < nChar ; ii++ )
          {
            SQLITE_SKIP_UTF8( zBytes, ref iz1 );
            aLen[ii] = iz1 - iz0;
            azChar[ii] = new byte[aLen[ii]];
            Buffer.BlockCopy( zBytes, iz0, azChar[ii], 0, azChar[ii].Length );
            iz0 = iz1;
          }
        }
      }
      if ( nChar > 0 )
      {
        flags = (int)sqlite3_user_data( context ); // flags = SQLITE_PTR_TO_INT(sqlite3_user_data(context));
        if ( ( flags & 1 ) != 0 )
        {
          while ( nIn > 0 )
          {
            int len = 0;
            for ( i = 0 ; i < nChar ; i++ )
            {
              len = aLen[i];
              if ( len <= nIn && memcmp( zBlob, izIn, azChar[i], len ) == 0 ) break;
            }
            if ( i >= nChar ) break;
            izIn += len;
            nIn -= len;
          }
        }
        if ( ( flags & 2 ) != 0 )
        {
          while ( nIn > 0 )
          {
            int len = 0;
            for ( i = 0 ; i < nChar ; i++ )
            {
              len = aLen[i];
              if ( len <= nIn && memcmp( zBlob, izIn + nIn - len, azChar[i], len ) == 0 ) break;
            }
            if ( i >= nChar ) break;
            nIn -= len;
          }
        }
        if ( zCharSet != null )
        {
          //sqlite3_free( ref  azChar );
        }
      }
      StringBuilder sb = new StringBuilder( nIn );
      for ( i = 0 ; i < nIn ; i++ ) sb.Append( (char)zBlob[izIn + i] );
      sqlite3_result_text( context, sb.ToString(), nIn, SQLITE_TRANSIENT );
    }

#if SQLITE_SOUNDEX
/*
** Compute the soundex encoding of a word.
*/
static void soundexFunc(
sqlite3_context context,
int argc,
sqlite3_value[] argv
)
{
Debug.Assert(false); // TODO -- func_c
char zResult[8];
const u8 *zIn;
int i, j;
static const unsigned char iCode[] = {
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
0, 0, 1, 2, 3, 0, 1, 2, 0, 0, 2, 2, 4, 5, 5, 0,
1, 2, 6, 2, 3, 0, 1, 0, 2, 0, 2, 0, 0, 0, 0, 0,
0, 0, 1, 2, 3, 0, 1, 2, 0, 0, 2, 2, 4, 5, 5, 0,
1, 2, 6, 2, 3, 0, 1, 0, 2, 0, 2, 0, 0, 0, 0, 0,
};
Debug.Assert( argc==1 );
zIn = (u8*)sqlite3_value_text(argv[0]);
if( zIn==0 ) zIn = (u8*)"";
for(i=0; zIn[i] && !sqlite3Isalpha(zIn[i]); i++){}
if( zIn[i] ){
u8 prevcode = iCode[zIn[i]&0x7f];
zResult[0] = sqlite3Toupper(zIn[i]);
for(j=1; j<4 && zIn[i]; i++){
int code = iCode[zIn[i]&0x7f];
if( code>0 ){
if( code!=prevcode ){
prevcode = code;
zResult[j++] = code + '0';
}
}else{
prevcode = 0;
}
}
while( j<4 ){
zResult[j++] = '0';
}
zResult[j] = 0;
sqlite3_result_text(context, zResult, 4, SQLITE_TRANSIENT);
}else{
sqlite3_result_text(context, "?000", 4, SQLITE_STATIC);
}
}
#endif

#if ! SQLITE_OMIT_LOAD_EXTENSION
    /*
** A function that loads a shared-library extension then returns NULL.
*/
    static void loadExt(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      string zFile = sqlite3_value_text( argv[0] );
      string zProc;
      sqlite3 db = (sqlite3)sqlite3_context_db_handle( context );
      string zErrMsg = "";

      if ( argc == 2 )
      {
        zProc = sqlite3_value_text( argv[1] );
      }
      else
      {
        zProc = "";
      }
      if ( zFile != null && sqlite3_load_extension( db, zFile, zProc, ref zErrMsg ) != 0 )
      {
        sqlite3_result_error( context, zErrMsg, -1 );
        //sqlite3DbFree( db, ref  zErrMsg );
      }
    }
#endif

    /*
** An instance of the following structure holds the context of a
** sum() or avg() aggregate computation.
*/
    //typedef struct SumCtx SumCtx;
    public class SumCtx
    {
      public double rSum;      /* Floating point sum */
      public i64 iSum;         /* Integer sum */
      public i64 cnt;          /* Number of elements summed */
      public int overflow;     /* True if integer overflow seen */
      public bool approx;      /* True if non-integer value was input to the sum */
      public Mem _M;
      public Mem Context
      {
        get { return _M; }
        set
        {
          _M = value;
          if ( _M == null || _M.z == null )
            iSum = 0;
          else iSum = Convert.ToInt64( _M.z );
        }
      }
    };

    /*
    ** Routines used to compute the sum, average, and total.
    **
    ** The SUM() function follows the (broken) SQL standard which means
    ** that it returns NULL if it sums over no inputs.  TOTAL returns
    ** 0.0 in that case.  In addition, TOTAL always returns a float where
    ** SUM might return an integer if it never encounters a floating point
    ** value.  TOTAL never fails, but SUM might through an exception if
    ** it overflows an integer.
    */
    static void sumStep(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      SumCtx p;

      int type;
      Debug.Assert( argc == 1 );
      UNUSED_PARAMETER( argc );
      Mem pMem = sqlite3_aggregate_context( context, -1 );//sizeof(*p));
      if ( pMem._SumCtx == null ) pMem._SumCtx = new SumCtx();
      p = pMem._SumCtx;
      if ( p.Context == null ) p.Context = pMem;
      type = sqlite3_value_numeric_type( argv[0] );
      if ( p != null && type != SQLITE_NULL )
      {
        p.cnt++;
        if ( type == SQLITE_INTEGER )
        {
          i64 v = sqlite3_value_int64( argv[0] );
          p.rSum += v;
          if ( !( p.approx | p.overflow != 0 ) )
          {
            i64 iNewSum = p.iSum + v;
            int s1 = (int)( p.iSum >> ( sizeof( i64 ) * 8 - 1 ) );
            int s2 = (int)( v >> ( sizeof( i64 ) * 8 - 1 ) );
            int s3 = (int)( iNewSum >> ( sizeof( i64 ) * 8 - 1 ) );
            p.overflow = ( ( s1 & s2 & ~s3 ) | ( ~s1 & ~s2 & s3 ) ) != 0 ? 1 : 0;
            p.iSum = iNewSum;
          }
        }
        else
        {
          p.rSum += sqlite3_value_double( argv[0] );
          p.approx = true;
        }
      }
    }
    static void sumFinalize( sqlite3_context context )
    {
      SumCtx p = null;
      Mem pMem = sqlite3_aggregate_context( context, 0 );
      if ( pMem != null ) p = pMem._SumCtx;
      if ( p != null && p.cnt > 0 )
      {
        if ( p.overflow != 0 )
        {
          sqlite3_result_error( context, "integer overflow", -1 );
        }
        else if ( p.approx )
        {
          sqlite3_result_double( context, p.rSum );
        }
        else
        {
          sqlite3_result_int64( context, p.iSum );
        }
      }
    }

    static void avgFinalize( sqlite3_context context )
    {
      SumCtx p = null;
      Mem pMem = sqlite3_aggregate_context( context, 0 );
      if ( pMem != null ) p = pMem._SumCtx;
      if ( p != null && p.cnt > 0 )
      {
        sqlite3_result_double( context, p.rSum / (double)p.cnt );
      }
    }

    static void totalFinalize( sqlite3_context context )
    {
      SumCtx p = null;
      Mem pMem = sqlite3_aggregate_context( context, 0 );
      if ( pMem != null ) p = pMem._SumCtx;
      /* (double)0 In case of SQLITE_OMIT_FLOATING_POINT... */
      sqlite3_result_double( context, p != null ? p.rSum : (double)0 );
    }

    /*
    ** The following structure keeps track of state information for the
    ** count() aggregate function.
    */
    //typedef struct CountCtx CountCtx;
    public class CountCtx
    {
      i64 _n;
      Mem _M;
      public Mem Context
      {
        get { return _M; }
        set
        {
          _M = value;
          if ( _M == null || _M.z == null )
            _n = 0;
          else _n = Convert.ToInt64( _M.z );
        }
      }
      public i64 n
      {
        get { return _n; }
        set
        {
          _n = value;
          if ( _M != null ) _M.z = _n.ToString();
        }
      }
    }

    /*
    ** Routines to implement the count() aggregate function.
    */
    static void countStep(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      CountCtx p = new CountCtx();
      p.Context = sqlite3_aggregate_context( context, -1 );//sizeof(*p));
      if ( ( argc == 0 || SQLITE_NULL != sqlite3_value_type( argv[0] ) ) && p.Context != null )
      {
        p.n++;
      }
#if !SQLITE_OMIT_DEPRECATED
      /* The sqlite3_aggregate_count() function is deprecated.  But just to make
** sure it still operates correctly, verify that its count agrees with our
** internal count when using count(*) and when the total count can be
** expressed as a 32-bit integer. */
      Debug.Assert( argc == 1 || p == null || p.n > 0x7fffffff
      || p.n == sqlite3_aggregate_count( context ) );
#endif
    }

    static void countFinalize( sqlite3_context context )
    {
      CountCtx p = new CountCtx();
      p.Context = sqlite3_aggregate_context( context, 0 );
      sqlite3_result_int64( context, p != null ? p.n : 0 );
    }

    /*
    ** Routines to implement min() and max() aggregate functions.
    */
    static void minmaxStep(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] argv
    )
    {
      Mem pArg = (Mem)argv[0];
      Mem pBest;
      UNUSED_PARAMETER( NotUsed );

      if ( sqlite3_value_type( argv[0] ) == SQLITE_NULL ) return;
      pBest = (Mem)sqlite3_aggregate_context( context, -1 );//sizeof(*pBest));
      if ( pBest == null ) return;

      if ( pBest.flags != 0 )
      {
        bool max;
        int cmp;
        CollSeq pColl = sqlite3GetFuncCollSeq( context );
        /* This step function is used for both the min() and max() aggregates,
        ** the only difference between the two being that the sense of the
        ** comparison is inverted. For the max() aggregate, the
        ** sqlite3_context_db_handle() function returns (void *)-1. For min() it
        ** returns (void *)db, where db is the sqlite3* database pointer.
        ** Therefore the next statement sets variable 'max' to 1 for the max()
        ** aggregate, or 0 for min().
        */
        max = sqlite3_context_db_handle( context ) != null && (int)sqlite3_user_data( context ) != 0;
        cmp = sqlite3MemCompare( pBest, pArg, pColl );
        if ( ( max && cmp < 0 ) || ( !max && cmp > 0 ) )
        {
          sqlite3VdbeMemCopy( pBest, pArg );
        }
      }
      else
      {
        sqlite3VdbeMemCopy( pBest, pArg );
      }
    }

    static void minMaxFinalize( sqlite3_context context )
    {
      sqlite3_value pRes;
      pRes = (sqlite3_value)sqlite3_aggregate_context( context, 0 );
      if ( pRes != null )
      {
        if ( ALWAYS( pRes.flags != 0 ) )
        {
          sqlite3_result_value( context, pRes );
        }
        sqlite3VdbeMemRelease( pRes );
      }
    }

    /*
    ** group_concat(EXPR, ?SEPARATOR?)
    */
    static void groupConcatStep(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      string zVal;
      StrAccum pAccum;
      string zSep;
      int nVal, nSep;
      Debug.Assert( argc == 1 || argc == 2 );
      if ( sqlite3_value_type( argv[0] ) == SQLITE_NULL ) return;
      Mem pMem = sqlite3_aggregate_context( context, -1 );//sizeof(*pAccum));
      if ( pMem._StrAccum == null ) pMem._StrAccum = new StrAccum();
      pAccum = pMem._StrAccum;
      if ( pAccum.Context == null ) pAccum.Context = pMem;
      if ( pAccum != null )
      {
        sqlite3 db = sqlite3_context_db_handle( context );
        int firstTerm = pAccum.useMalloc == 0 ? 1 : 0;
        pAccum.useMalloc = 1;
        pAccum.mxAlloc = db.aLimit[SQLITE_LIMIT_LENGTH];
        if ( 0 == firstTerm )
        {
          if ( argc == 2 )
          {
            zSep = sqlite3_value_text( argv[1] );
            nSep = sqlite3_value_bytes( argv[1] );
          }
          else
          {
            zSep = ",";
            nSep = 1;
          }
          sqlite3StrAccumAppend( pAccum, zSep, nSep );
        }
        zVal = sqlite3_value_text( argv[0] );
        nVal = sqlite3_value_bytes( argv[0] );
        sqlite3StrAccumAppend( pAccum, zVal, nVal );
      }
    }

    static void groupConcatFinalize( sqlite3_context context )
    {
      StrAccum pAccum = null;
      Mem pMem = sqlite3_aggregate_context( context, 0 );
      if ( pMem != null )
      {
        if ( pMem._StrAccum == null ) pMem._StrAccum = new StrAccum();
        pAccum = pMem._StrAccum;
      }
      if ( pAccum != null )
      {
        if ( pAccum.tooBig != 0 )
        {
          sqlite3_result_error_toobig( context );
        }
        //else if ( pAccum.mallocFailed != 0 )
        //{
        //  sqlite3_result_error_nomem( context );
        //}
        else
        {
          sqlite3_result_text( context, sqlite3StrAccumFinish( pAccum ), -1,
          null); //sqlite3_free );
        }
      }
    }

    /*
    ** This function registered all of the above C functions as SQL
    ** functions.  This should be the only routine in this file with
    ** external linkage.
    */
    public struct sFuncs
    {
      public string zName;
      public sbyte nArg;
      public u8 argType;           /* 1: 0, 2: 1, 3: 2,...  N:  N-1. */
      public u8 eTextRep;          /* 1: UTF-16.  0: UTF-8 */
      public u8 needCollSeq;
      public dxFunc xFunc; //(sqlite3_context*,int,sqlite3_value **);

      // Constructor
      public sFuncs( string zName, sbyte nArg, u8 argType, u8 eTextRep, u8 needCollSeq, dxFunc xFunc )
      {
        this.zName = zName;
        this.nArg = nArg;
        this.argType = argType;
        this.eTextRep = eTextRep;
        this.needCollSeq = needCollSeq;
        this.xFunc = xFunc;
      }
    };

    public struct sAggs
    {
      public string zName;
      public sbyte nArg;
      public u8 argType;
      public u8 needCollSeq;
      public dxStep xStep; //(sqlite3_context*,int,sqlite3_value**);
      public dxFinal xFinalize; //(sqlite3_context*);
      // Constructor
      public sAggs( string zName, sbyte nArg, u8 argType, u8 needCollSeq, dxStep xStep, dxFinal xFinalize )
      {
        this.zName = zName;
        this.nArg = nArg;
        this.argType = argType;
        this.needCollSeq = needCollSeq;
        this.xStep = xStep;
        this.xFinalize = xFinalize;
      }
    }
    static void sqlite3RegisterBuiltinFunctions( sqlite3 db )
    {
#if !SQLITE_OMIT_ALTERTABLE
      sqlite3AlterFunctions( db );
#endif
      ////if ( 0 == db.mallocFailed )
      {
        int rc = sqlite3_overload_function( db, "MATCH", 2 );
        Debug.Assert( rc == SQLITE_NOMEM || rc == SQLITE_OK );
        if ( rc == SQLITE_NOMEM )
        {
  ////        db.mallocFailed = 1;
        }
      }
    }

    /*
    ** Set the LIKEOPT flag on the 2-argument function with the given name.
    */
    static void setLikeOptFlag( sqlite3 db, string zName, int flagVal )
    {
      FuncDef pDef;
      pDef = sqlite3FindFunction( db, zName, sqlite3Strlen30( zName ),
      2, SQLITE_UTF8, 0 );
      if ( ALWAYS( pDef != null ) )
      {
        pDef.flags = (byte)flagVal;
      }
    }

    /*
    ** Register the built-in LIKE and GLOB functions.  The caseSensitive
    ** parameter determines whether or not the LIKE operator is case
    ** sensitive.  GLOB is always case sensitive.
    */
    static void sqlite3RegisterLikeFunctions( sqlite3 db, int caseSensitive )
    {
      compareInfo pInfo;
      if ( caseSensitive != 0 )
      {
        pInfo = likeInfoAlt;
      }
      else
      {
        pInfo = likeInfoNorm;
      }
      sqlite3CreateFunc( db, "like", 2, SQLITE_ANY, pInfo, (dxFunc)likeFunc, null, null );
      sqlite3CreateFunc( db, "like", 3, SQLITE_ANY, pInfo, (dxFunc)likeFunc, null, null );
      sqlite3CreateFunc( db, "glob", 2, SQLITE_ANY,
      globInfo, (dxFunc)likeFunc, null, null );
      setLikeOptFlag( db, "glob", SQLITE_FUNC_LIKE | SQLITE_FUNC_CASE );
      setLikeOptFlag( db, "like",
      caseSensitive != 0 ? ( SQLITE_FUNC_LIKE | SQLITE_FUNC_CASE ) : SQLITE_FUNC_LIKE );
    }

    /*
    ** pExpr points to an expression which implements a function.  If
    ** it is appropriate to apply the LIKE optimization to that function
    ** then set aWc[0] through aWc[2] to the wildcard characters and
    ** return TRUE.  If the function is not a LIKE-style function then
    ** return FALSE.
    */
    static bool sqlite3IsLikeFunction( sqlite3 db, Expr pExpr, ref bool pIsNocase, char[] aWc )
    {
      FuncDef pDef;
      if ( pExpr.op != TK_FUNCTION
      || null == pExpr.x.pList
      || pExpr.x.pList.nExpr != 2
      )
      {
        return false;
      }
      Debug.Assert( !ExprHasProperty( pExpr, EP_xIsSelect ) );
      pDef = sqlite3FindFunction( db, pExpr.u.zToken, sqlite3Strlen30( pExpr.u.zToken ),
                      2, SQLITE_UTF8, 0 );
      if ( NEVER( pDef == null ) || ( pDef.flags & SQLITE_FUNC_LIKE ) == 0 )
      {
        return false;
      }

      /* The memcpy() statement assumes that the wildcard characters are
      ** the first three statements in the compareInfo structure.  The
      ** Debug.Asserts() that follow verify that assumption
      */
      //memcpy( aWc, pDef.pUserData, 3 );
      aWc[0] = ( (compareInfo)pDef.pUserData ).matchAll;
      aWc[1] = ( (compareInfo)pDef.pUserData ).matchOne;
      aWc[2] = ( (compareInfo)pDef.pUserData ).matchSet;
      // Debug.Assert((char*)&likeInfoAlt == (char*)&likeInfoAlt.matchAll);
      // Debug.Assert(&((char*)&likeInfoAlt)[1] == (char*)&likeInfoAlt.matchOne);
      // Debug.Assert(&((char*)&likeInfoAlt)[2] == (char*)&likeInfoAlt.matchSet);
      pIsNocase = ( pDef.flags & SQLITE_FUNC_CASE ) == 0;
      return true;
    }

    /*
    ** All all of the FuncDef structures in the aBuiltinFunc[] array above
    ** to the global function hash table.  This occurs at start-time (as
    ** a consequence of calling sqlite3_initialize()).
    **
    ** After this routine runs
    */
    static void sqlite3RegisterGlobalFunctions()
    {
      /*
      ** The following array holds FuncDef structures for all of the functions
      ** defined in this file.
      **
      ** The array cannot be constant since changes are made to the
      ** FuncDef.pHash elements at start-time.  The elements of this array
      ** are read-only after initialization is complete.
      */
      FuncDef[] aBuiltinFunc =  {
FUNCTION("ltrim",              1, 1, 0, trimFunc         ),
FUNCTION("ltrim",              2, 1, 0, trimFunc         ),
FUNCTION("rtrim",              1, 2, 0, trimFunc         ),
FUNCTION("rtrim",              2, 2, 0, trimFunc         ),
FUNCTION("trim",               1, 3, 0, trimFunc         ),
FUNCTION("trim",               2, 3, 0, trimFunc         ),
FUNCTION("min",               -1, 0, 1, minmaxFunc       ),
FUNCTION("min",                0, 0, 1, null                ),
AGGREGATE("min",               1, 0, 1, minmaxStep,      minMaxFinalize ),
FUNCTION("max",               -1, 1, 1, minmaxFunc       ),
FUNCTION("max",                0, 1, 1, null                ),
AGGREGATE("max",               1, 1, 1, minmaxStep,      minMaxFinalize ),
FUNCTION("typeof",             1, 0, 0, typeofFunc       ),
FUNCTION("length",             1, 0, 0, lengthFunc       ),
FUNCTION("substr",             2, 0, 0, substrFunc       ),
FUNCTION("substr",             3, 0, 0, substrFunc       ),
FUNCTION("abs",                1, 0, 0, absFunc          ),
#if !SQLITE_OMIT_FLOATING_POINT
FUNCTION("round",              1, 0, 0, roundFunc        ),
FUNCTION("round",              2, 0, 0, roundFunc        ),
#endif
FUNCTION("upper",              1, 0, 0, upperFunc        ),
FUNCTION("lower",              1, 0, 0, lowerFunc        ),
FUNCTION("coalesce",           1, 0, 0, null                ),
FUNCTION("coalesce",          -1, 0, 0, ifnullFunc       ),
FUNCTION("coalesce",           0, 0, 0, null                ),
FUNCTION("hex",                1, 0, 0, hexFunc          ),
FUNCTION("ifnull",             2, 0, 1, ifnullFunc       ),
FUNCTION("random",             0, 0, 0, randomFunc       ),
FUNCTION("randomblob",         1, 0, 0, randomBlob       ),
FUNCTION("nullif",             2, 0, 1, nullifFunc       ),
FUNCTION("sqlite_version",     0, 0, 0, versionFunc      ),
FUNCTION("quote",              1, 0, 0, quoteFunc        ),
FUNCTION("last_insert_rowid",  0, 0, 0, last_insert_rowid),
FUNCTION("changes",            0, 0, 0, changes          ),
FUNCTION("total_changes",      0, 0, 0, total_changes    ),
FUNCTION("replace",            3, 0, 0, replaceFunc      ),
FUNCTION("zeroblob",           1, 0, 0, zeroblobFunc     ),
#if SQLITE_SOUNDEX
FUNCTION("soundex",            1, 0, 0, soundexFunc      ),
#endif
#if !SQLITE_OMIT_LOAD_EXTENSION
FUNCTION("load_extension",     1, 0, 0, loadExt          ),
FUNCTION("load_extension",     2, 0, 0, loadExt          ),
#endif
AGGREGATE("sum",               1, 0, 0, sumStep,         sumFinalize    ),
AGGREGATE("total",             1, 0, 0, sumStep,         totalFinalize    ),
AGGREGATE("avg",               1, 0, 0, sumStep,         avgFinalize    ),
/*AGGREGATE("count",             0, 0, 0, countStep,       countFinalize  ), */
/* AGGREGATE(count,             0, 0, 0, countStep,       countFinalize  ), */
new FuncDef( 0,SQLITE_UTF8,SQLITE_FUNC_COUNT,null,null,null,countStep,countFinalize,"count",null),
AGGREGATE("count",             1, 0, 0, countStep,       countFinalize  ),
AGGREGATE("group_concat",      1, 0, 0, groupConcatStep, groupConcatFinalize),
AGGREGATE("group_concat",      2, 0, 0, groupConcatStep, groupConcatFinalize),

LIKEFUNC("glob", 2, globInfo, SQLITE_FUNC_LIKE|SQLITE_FUNC_CASE),
#if SQLITE_CASE_SENSITIVE_LIKE
LIKEFUNC("like", 2, likeInfoAlt, SQLITE_FUNC_LIKE|SQLITE_FUNC_CASE),
LIKEFUNC("like", 3, likeInfoAlt, SQLITE_FUNC_LIKE|SQLITE_FUNC_CASE),
#else
LIKEFUNC("like", 2, likeInfoNorm, SQLITE_FUNC_LIKE),
LIKEFUNC("like", 3, likeInfoNorm, SQLITE_FUNC_LIKE),
#endif
};
      int i;
#if SQLITE_OMIT_WSD
FuncDefHash pHash = GLOBAL( FuncDefHash, sqlite3GlobalFunctions );
FuncDef[] aFunc = (FuncDef[])GLOBAL( FuncDef, aBuiltinFunc );
#else
      FuncDefHash pHash = sqlite3GlobalFunctions;
      FuncDef[] aFunc = aBuiltinFunc;
#endif
      for ( i = 0 ; i < ArraySize( aBuiltinFunc ) ; i++ )
      {
        sqlite3FuncDefInsert( pHash, aFunc[i] );
      }
      sqlite3RegisterDateTimeFunctions();
    }
  }
}
