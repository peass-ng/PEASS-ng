using System;
using System.Diagnostics;
using System.Text;

using i64 = System.Int64;

using u8 = System.Byte;
using u32 = System.UInt32;
using u64 = System.UInt64;

using Pgno = System.UInt32;


namespace CS_SQLite3
{
  using sqlite_int64 = System.Int64;
  using System.Globalization;

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
    ** Utility functions used throughout sqlite.
    **
    ** This file contains functions for allocating memory, comparing
    ** strings, and stuff like that.
    **
    ** $Id: util.c,v 1.262 2009/07/28 16:44:26 danielk1977 Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
    //#include <stdarg.h>
#if SQLITE_HAVE_ISNAN
//# include <math.h>
#endif


    /*
** Routine needed to support the testcase() macro.
*/
#if SQLITE_COVERAGE_TEST
void sqlite3Coverage(int x){
static int dummy = 0;
dummy += x;
}
#endif

    /*
** Return true if the floating point value is Not a Number (NaN).
**
** Use the math library isnan() function if compiled with SQLITE_HAVE_ISNAN.
** Otherwise, we have our own implementation that works on most systems.
*/
    static bool sqlite3IsNaN( double x )
    {
      bool rc;   /* The value return */
#if !(SQLITE_HAVE_ISNAN)
      /*
** Systems that support the isnan() library function should probably
** make use of it by compiling with -DSQLITE_HAVE_ISNAN.  But we have
** found that many systems do not have a working isnan() function so
** this implementation is provided as an alternative.
**
** This NaN test sometimes fails if compiled on GCC with -ffast-math.
** On the other hand, the use of -ffast-math comes with the following
** warning:
**
**      This option [-ffast-math] should never be turned on by any
**      -O option since it can result in incorrect output for programs
**      which depend on an exact implementation of IEEE or ISO
**      rules/specifications for math functions.
**
** Under MSVC, this NaN test may fail if compiled with a floating-
** point precision mode other than /fp:precise.  From the MSDN
** documentation:
**
**      The compiler [with /fp:precise] will properly handle comparisons
**      involving NaN. For example, x != x evaluates to true if x is NaN
**      ...
*/
#if __FAST_MATH__
# error SQLite will not work correctly with the -ffast-math option of GCC.
#endif
      double y = x;
      double z = y;
      rc = ( y != z );
#else  //* if defined(SQLITE_HAVE_ISNAN) */
rc = isnan(x);
#endif //* SQLITE_HAVE_ISNAN */
      testcase( rc );
      return rc;
    }


    /*
    ** Compute a string length that is limited to what can be stored in
    ** lower 30 bits of a 32-bit signed integer.
    **
    ** The value returned will never be negative.  Nor will it ever be greater
    ** than the actual length of the string.  For very long strings (greater
    ** than 1GiB) the value returned might be less than the true string length.
    */
    static int sqlite3Strlen30( int z )
    {
      return 0x3fffffff & z;
    }
    static int sqlite3Strlen30( StringBuilder z )
    {
      //const char *z2 = z;
      if ( z == null ) return 0;
      //while( *z2 ){ z2++; }
      //return 0x3fffffff & (int)(z2 - z);
      return 0x3fffffff & z.Length;
    }
    static int sqlite3Strlen30( string z )
    {
      //const char *z2 = z;
      if ( z == null ) return 0;
      //while( *z2 ){ z2++; }
      //return 0x3fffffff & (int)(z2 - z);
      return 0x3fffffff & z.Length;
    }


    /*
    ** Set the most recent error code and error string for the sqlite
    ** handle "db". The error code is set to "err_code".
    **
    ** If it is not NULL, string zFormat specifies the format of the
    ** error string in the style of the printf functions: The following
    ** format characters are allowed:
    **
    **      %s      Insert a string
    **      %z      A string that should be freed after use
    **      %d      Insert an integer
    **      %T      Insert a token
    **      %S      Insert the first element of a SrcList
    **
    ** zFormat and any string tokens that follow it are assumed to be
    ** encoded in UTF-8.
    **
    ** To clear the most recent error for sqlite handle "db", sqlite3Error
    ** should be called with err_code set to SQLITE_OK and zFormat set
    ** to NULL.
    */
    //Overloads
    static void sqlite3Error( sqlite3 db, int err_code, int noString )
    { sqlite3Error( db, err_code, err_code == 0 ?null :""); }

    static void sqlite3Error( sqlite3 db, int err_code, string zFormat, params object[] ap )
    {
      if ( db != null && ( db.pErr != null || ( db.pErr = sqlite3ValueNew( db ) ) != null ) )
      {
        db.errCode = err_code;
        if ( zFormat != null )
        {
          string z;
          va_start( ap, zFormat );
          z = sqlite3VMPrintf( db, zFormat, ap );
          va_end( ap );
          sqlite3ValueSetStr( db.pErr, -1, z, SQLITE_UTF8, (dxDel)SQLITE_DYNAMIC );
        }
        else
        {
          sqlite3ValueSetStr( db.pErr, 0, null, SQLITE_UTF8, SQLITE_STATIC );
        }
      }
    }

    /*
    ** Add an error message to pParse.zErrMsg and increment pParse.nErr.
    ** The following formatting characters are allowed:
    **
    **      %s      Insert a string
    **      %z      A string that should be freed after use
    **      %d      Insert an integer
    **      %T      Insert a token
    **      %S      Insert the first element of a SrcList
    **
    ** This function should be used to report any error that occurs whilst
    ** compiling an SQL statement (i.e. within sqlite3_prepare()). The
    ** last thing the sqlite3_prepare() function does is copy the error
    ** stored by this function into the database handle using sqlite3Error().
    ** Function sqlite3Error() should be used during statement execution
    ** (sqlite3_step() etc.).
    */
    static void sqlite3ErrorMsg( Parse pParse, string zFormat, params object[] ap )
    {
      //va_list ap;
      sqlite3 db = pParse.db;
      pParse.nErr++;
      //sqlite3DbFree( db, ref pParse.zErrMsg );
      va_start( ap, zFormat );
      pParse.zErrMsg = sqlite3VMPrintf( db, zFormat, ap );
      va_end( ap );
      pParse.rc = SQLITE_ERROR;
    }

    /*
    ** Clear the error message in pParse, if any
    */
    static void sqlite3ErrorClear( Parse pParse )
    {
      //sqlite3DbFree( pParse.db, ref  pParse.zErrMsg );
      pParse.nErr = 0;
    }

    /*
    ** Convert an SQL-style quoted string into a normal string by removing
    ** the quote characters.  The conversion is done in-place.  If the
    ** input does not begin with a quote character, then this routine
    ** is a no-op.
    **
    ** The input string must be zero-terminated.  A new zero-terminator
    ** is added to the dequoted string.
    **
    ** The return value is -1 if no dequoting occurs or the length of the
    ** dequoted string, exclusive of the zero terminator, if dequoting does
    ** occur.
    **
    ** 2002-Feb-14: This routine is extended to remove MS-Access style
    ** brackets from around identifers.  For example:  "[a-b-c]" becomes
    ** "a-b-c".
    */
    static int sqlite3Dequote( ref string z )
    {
      char quote;
      int i;
      if ( z == null || z == "" ) return -1;
      quote = z[0];
      switch ( quote )
      {
        case '\'': break;
        case '"': break;
        case '`': break;                /* For MySQL compatibility */
        case '[': quote = ']'; break;  /* For MS SqlServer compatibility */
        default: return -1;
      }
      StringBuilder sbZ = new StringBuilder( z.Length );
      for ( i = 1 ; i < z.Length ; i++ ) //z[i] != 0; i++)
      {
        if ( z[i] == quote )
        {
          if ( i < z.Length - 1 && ( z[i + 1] == quote ) )
          {
            sbZ.Append( quote );
            i++;
          }
          else
          {
            break;
          }
        }
        else
        {
          sbZ.Append( z[i] );
        }
      }
      z = sbZ.ToString();
      return sbZ.Length;
    }

    /* Convenient short-hand */
    //#define UpperToLower sqlite3UpperToLower
    static int[] UpperToLower;

    /*
    ** Some systems have stricmp().  Others have strcasecmp().  Because
    ** there is no consistency, we will define our own.
    */

    static int sqlite3StrICmp( string zLeft, string zRight )
    {
      //register unsigned char *a, *b;
      //a = (unsigned char *)zLeft;
      //b = (unsigned char *)zRight;
      //while( *a!=0 && UpperToLower[*a]==UpperToLower[*b]){ a++; b++; }
      //return UpperToLower[*a] - UpperToLower[*b];
      int a = 0, b = 0;
      while ( a < zLeft.Length && b < zRight.Length && UpperToLower[zLeft[a]] == UpperToLower[zRight[b]] ) { a++; b++; }
      if ( a == zLeft.Length && b == zRight.Length ) return 0;
      else
      {
        if ( a == zLeft.Length ) return -UpperToLower[zRight[b]];
        if ( b == zRight.Length ) return UpperToLower[zLeft[a]];
        return UpperToLower[zLeft[a]] - UpperToLower[zRight[b]];
      }
    }

    static int sqlite3_strnicmp( string zLeft, int offsetLeft, string zRight, int N )
    { return sqlite3StrNICmp(  zLeft,  offsetLeft,  zRight,  N );}

    static int sqlite3StrNICmp( string zLeft, int offsetLeft, string zRight, int N )
    {
      //register unsigned char *a, *b;
      //a = (unsigned char *)zLeft;
      //b = (unsigned char *)zRight;
      int a = 0, b = 0;
      while ( N-- > 0 && zLeft[a + offsetLeft] != 0 && UpperToLower[zLeft[a + offsetLeft]] == UpperToLower[zRight[b]] ) { a++; b++; }
      return N < 0 ? 0 : UpperToLower[zLeft[a + offsetLeft]] - UpperToLower[zRight[b]];
    }

    static int sqlite3StrNICmp( string zLeft, string zRight, int N )
    {
      //register unsigned char *a, *b;
      //a = (unsigned char *)zLeft;
      //b = (unsigned char *)zRight;
      int a = 0, b = 0;
      while ( N-- > 0 && ( zLeft[a] == zRight[b] || ( zLeft[a] != 0 && zLeft[a] < 256 && zRight[b] < 256 && UpperToLower[zLeft[a]] == UpperToLower[zRight[b]] ) ) ) { a++; b++; }
      if ( N < 0 ) return 0;
      else if ( zLeft[a] < 256 && zRight[b] < 256 ) return UpperToLower[zLeft[a]] - UpperToLower[zRight[b]];
      else return zLeft[a] - zRight[b];
    }

    /*
    ** Return TRUE if z is a pure numeric string.  Return FALSE and leave
    ** *realnum unchanged if the string contains any character which is not
    ** part of a number.
    **
    ** If the string is pure numeric, set *realnum to TRUE if the string
    ** contains the '.' character or an "E+000" style exponentiation suffix.
    ** Otherwise set *realnum to FALSE.  Note that just becaue *realnum is
    ** false does not mean that the number can be successfully converted into
    ** an integer - it might be too big.
    **
    ** An empty string is considered non-numeric.
    */
    static int sqlite3IsNumber( string z, ref int realnum, int enc )
    {
      if ( String.IsNullOrEmpty( z ) ) return 0;
      int incr = ( enc == SQLITE_UTF8 ? 1 : 2 );
      int zIndex = 0;
      if ( enc == SQLITE_UTF16BE ) zIndex++;// z++;
      if ( z[zIndex] == '-' || z[zIndex] == '+' ) zIndex += incr;//z += incr;
      if ( zIndex == z.Length || !sqlite3Isdigit( z[zIndex] ) )
      {
        return 0;
      }
      zIndex += incr;//z += incr;
      realnum = 0;
      while ( zIndex < z.Length && sqlite3Isdigit( z[zIndex] ) ) { zIndex += incr; }//z += incr; }
      if ( zIndex < z.Length && z[zIndex] == '.' )
      {
        zIndex += incr;//z += incr;
        if ( !sqlite3Isdigit( z[zIndex] ) ) return 0;
        while ( zIndex < z.Length && sqlite3Isdigit( z[zIndex] ) ) { zIndex += incr; }//z += incr; }
        realnum = 1;
      }
      if ( zIndex < z.Length && ( z[zIndex] == 'e' || z[zIndex] == 'E' ) )
      {
        zIndex += incr;//z += incr;
        if ( zIndex < z.Length && ( z[zIndex] == '+' || z[zIndex] == '-' ) ) zIndex += incr;//z += incr;
        if ( zIndex == z.Length || !sqlite3Isdigit( z[zIndex] ) ) return 0;
        while ( zIndex < z.Length && sqlite3Isdigit( z[zIndex] ) ) { zIndex += incr; }//z += incr; }
        realnum = 1;
      }
      return zIndex == z.Length ? 1 : 0;// z[zIndex] == 0;
    }

    /*
    ** The string z[] is an ascii representation of a real number.
    ** Convert this string to a double.
    **
    ** This routine assumes that z[] really is a valid number.  If it
    ** is not, the result is undefined.
    **
    ** This routine is used instead of the library atof() function because
    ** the library atof() might want to use "," as the decimal point instead
    ** of "." depending on how locale is set.  But that would cause problems
    ** for SQL.  So this routine always uses "." regardless of locale.
    */
    static int sqlite3AtoF( string z, ref double pResult )
    {
#if !SQLITE_OMIT_FLOATING_POINT
      z = z.Trim() + " ";
      int zDx = 0;
      int sign = 1;
      double v1 = 0.0;
      int nSignificant = 0;
      if ( z.Length > 1 )
      {
        while ( sqlite3Isspace( z[zDx] ) ) zDx++;
        if ( z[zDx] == '-' )
        {
          sign = -1;
          zDx++;
        }
        else if ( z[zDx] == '+' )
        {
          zDx++;
        }
        while ( z[zDx] == '0' )
        {
          zDx++;
        }
        while ( sqlite3Isdigit( z[zDx] ) )
        {
          v1 = v1 * 10.0 + ( z[zDx] - '0' );
          zDx++;
          nSignificant++;
        }
        if ( z[zDx] == '.' )
        {
          double divisor = 1.0;
          zDx++;
          if ( nSignificant == 0 )
          {
            while ( z[zDx] == '0' )
            {
              divisor *= 10.0;
              zDx++;
            }
          }
          while ( sqlite3Isdigit( z[zDx] ) )
          {
            if ( nSignificant < 18 )
            {
              v1 = v1 * 10.0 + ( z[zDx] - '0' );
              divisor *= 10.0;
              nSignificant++;
            }
            zDx++;
          }
          if ( Double.IsInfinity( divisor ) )
          { if ( !Double.TryParse( z.Substring( 0, zDx ), out v1 ) ) v1 = 0; }
          else v1 /= divisor;
        }
        if ( z[zDx] == 'e' || z[zDx] == 'E' )
        {
          int esign = 1;
          int eval = 0;
          double scale = 1.0;
          zDx++;
          if ( z[zDx] == '-' )
          {
            esign = -1;
            zDx++;
          }
          else if ( z[zDx] == '+' )
          {
            zDx++;
          }
          while ( sqlite3Isdigit( z[zDx] ) )
          {
            eval = eval * 10 + z[zDx] - '0';
            zDx++;
          }
          while ( eval >= 64 ) { scale *= 1.0e+64; eval -= 64; }
          while ( eval >= 16 ) { scale *= 1.0e+16; eval -= 16; }
          while ( eval >= 4 ) { scale *= 1.0e+4; eval -= 4; }
          while ( eval >= 1 ) { scale *= 1.0e+1; eval -= 1; }
          if ( esign < 0 )
          {
            v1 /= scale;
          }
          else
          {
            v1 *= scale;
          }
        }
      }
      pResult = (double)( sign < 0 ? -v1 : v1 );
      return (int)( zDx );
#else
return sqlite3Atoi64(z, pResult);
#endif //* SQLITE_OMIT_FLOATING_POINT */
    }

    /*
    ** Compare the 19-character string zNum against the text representation
    ** value 2^63:  9223372036854775808.  Return negative, zero, or positive
    ** if zNum is less than, equal to, or greater than the string.
    **
    ** Unlike memcmp() this routine is guaranteed to return the difference
    ** in the values of the last digit if the only difference is in the
    ** last digit.  So, for example,
    **
    **      compare2pow63("9223372036854775800")
    **
    ** will return -8.
    */
    static int compare2pow63( string zNum )
    {
      int c;
      if ( zNum.Length <= 18 )
        c = string.Compare( zNum, "922337203685477580" );
      else
      {
        c = ( string.Compare( zNum.Substring( 0, 18 ), "922337203685477580" ) == 1 ) ? 10 : 0;
        if ( c == 0 )
        {
          c = zNum[18] - '8';
        }
      }
      return c;
    }


    /*
    ** Return TRUE if zNum is a 64-bit signed integer and write
    ** the value of the integer into pNum.  If zNum is not an integer
    ** or is an integer that is too large to be expressed with 64 bits,
    ** then return false.
    **
    ** When this routine was originally written it dealt with only
    ** 32-bit numbers.  At that time, it was much faster than the
    ** atoi() library routine in RedHat 7.2.
    */
    static bool sqlite3Atoi64( string zNum, ref i64 pNum )
    {
      zNum = zNum.Trim() + " ";
      int i;
      for ( i = 1 ; i < zNum.Length ; i++ ) if ( !sqlite3Isdigit( zNum[i] ) ) break;
      return Int64.TryParse( zNum.Substring( 0, i ), out pNum
      );
      //i64 v = 0;
      //int neg;
      //int i, c;
      //const char *zStart;
      //while( sqlite3Isspace(*(u8*)zNum) ) zNum++;
      //if( *zNum=='-' ){
      //  neg = 1;
      //  zNum++;
      //}else if( *zNum=='+' ){
      //  neg = 0;
      //  zNum++;
      //}else{
      //  neg = 0;
      //}
      //zStart = zNum;
      //while( zNum[0]=='0' ){ zNum++; } /* Skip over leading zeros. Ticket #2454 */
      //for(i=0; (c=zNum[i])>='0' && c<='9'; i++){
      //  v = v*10 + c - '0';
      //}
      //*pNum = neg ? -v : v;
      //if( c!=0 || (i==0 && zStart==zNum) || i>19 ){
      //  /* zNum is empty or contains non-numeric text or is longer
      //  ** than 19 digits (thus guaranting that it is too large) */
      //  return 0;
      //}else if( i<19 ){
      //  /* Less than 19 digits, so we know that it fits in 64 bits */
      //  return 1;
      //}else{
      //  /* 19-digit numbers must be no larger than 9223372036854775807 if positive
      //  ** or 9223372036854775808 if negative.  Note that 9223372036854665808
      //  ** is 2^63. */
      //  return compare2pow63(zNum)<neg;
      //}
    }

    /*
    ** The string zNum represents an unsigned integer.  The zNum string
    ** consists of one or more digit characters and is terminated by
    ** a zero character.  Any stray characters in zNum result in undefined
    ** behavior.
    **
    ** If the unsigned integer that zNum represents will fit in a
    ** 64-bit signed integer, return TRUE.  Otherwise return FALSE.
    **
    ** If the negFlag parameter is true, that means that zNum really represents
    ** a negative number.  (The leading "-" is omitted from zNum.)  This
    ** parameter is needed to determine a boundary case.  A string
    ** of "9223373036854775808" returns false if negFlag is false or true
    ** if negFlag is true.
    **
    ** Leading zeros are ignored.
    */
    static bool sqlite3FitsIn64Bits( string zNum, bool negFlag )
    {
      Int64 pNum;
      Debug.Assert( zNum[0] >= '0' && zNum[0] <= '9' ); /* zNum is an unsigned number */
      bool result = negFlag ? Int64.TryParse( "-" + zNum, out pNum ) : Int64.TryParse( zNum, out pNum );
      // if ( result && negFlag && pNum == Int64.MaxValue  ) result = false;
      return result;
      //int i;
      //int neg = 0;
      //if (negFlag != 0) neg = 1 - neg;
      //while (*zNum == '0')
      //{
      //  zNum++;   /* Skip leading zeros.  Ticket #2454 */
      //}
      //for (i = 0;  zNum[i]; i++){ assert( zNum[i]>='0' && zNum[i]<='9' ); }
      //if (i < 19)
      //{
      /* Guaranteed to fit if less than 19 digits */
      //  return 1;
      //}
      //else if (i > 19)
      //{
      /* Guaranteed to be too big if greater than 19 digits */
      //  return 0;
      //}
      //else
      //{
      /* Compare against 2^63. */
      //  if (compare2pow63(new string(zNum)) < neg) return 1; else return 0;
      //}
    }

    /*
    ** If zNum represents an integer that will fit in 32-bits, then set
    ** pValue to that integer and return true.  Otherwise return false.
    **
    ** Any non-numeric characters that following zNum are ignored.
    ** This is different from sqlite3Atoi64() which requires the
    ** input number to be zero-terminated.
    */
    static bool sqlite3GetInt32( string zNum, ref int pValue )
    {
      sqlite_int64 v = 0;
      int iZnum = 0;
      int i, c;
      int neg = 0;
      if ( zNum[iZnum] == '-' )
      {
        neg = 1;
        iZnum++;
      }
      else if ( zNum[iZnum] == '+' )
      {
        iZnum++;
      }
      while ( iZnum < zNum.Length && zNum[iZnum] == '0' ) iZnum++;
      for ( i = 0 ; i < 11 && i + iZnum < zNum.Length && ( c = zNum[iZnum + i] - '0' ) >= 0 && c <= 9 ; i++ )
      {
        v = v * 10 + c;
      }

      /* The longest decimal representation of a 32 bit integer is 10 digits:
      **
      **             1234567890
      **     2^31 . 2147483648
      */
      if ( i > 10 )
      {
        return false;
      }
      if ( v - neg > 2147483647 )
      {
        return false;
      }
      if ( neg != 0 )
      {
        v = -v;
      }
      pValue = (int)v;
      return true;
    }

    /*
    ** The variable-length integer encoding is as follows:
    **
    ** KEY:
    **         A = 0xxxxxxx    7 bits of data and one flag bit
    **         B = 1xxxxxxx    7 bits of data and one flag bit
    **         C = xxxxxxxx    8 bits of data
    **
    **  7 bits - A
    ** 14 bits - BA
    ** 21 bits - BBA
    ** 28 bits - BBBA
    ** 35 bits - BBBBA
    ** 42 bits - BBBBBA
    ** 49 bits - BBBBBBA
    ** 56 bits - BBBBBBBA
    ** 64 bits - BBBBBBBBC
    */

    /*
    ** Write a 64-bit variable-length integer to memory starting at p[0].
    ** The length of data write will be between 1 and 9 bytes.  The number
    ** of bytes written is returned.
    **
    ** A variable-length integer consists of the lower 7 bits of each byte
    ** for all bytes that have the 8th bit set and one byte with the 8th
    ** bit clear.  Except, if we get to the 9th byte, it stores the full
    ** 8 bits and is the last byte.
    */
    static int getVarint( byte[] p, ref u32 v )
    {
      v = p[0];
      if ( v <= 0x7F ) return 1;
      u64 u64_v = 0;
      int result = sqlite3GetVarint( p, 0, ref u64_v );
      v = (u32)u64_v;
      return result;
    }
    static int getVarint( byte[] p, int offset, ref u32 v )
    {
      v = p[offset + 0];
      if ( v <= 0x7F ) return 1;
      u64 u64_v = 0;
      int result = sqlite3GetVarint( p, offset, ref u64_v );
      v = (u32)u64_v;
      return result;
    }
    static int getVarint( byte[] p, int offset, ref int v )
    {
      v = p[offset + 0];
      if ( v <= 0x7F ) return 1;
      u64 u64_v = 0;
      int result = sqlite3GetVarint( p, offset, ref u64_v );
      v = (int)u64_v;
      return result;
    }
    static int getVarint( byte[] p, int offset, ref i64 v )
    {
      v = p[offset + 0];
      if ( v <= 0x7F ) return 1;
      u64 u64_v = 0;
      int result = sqlite3GetVarint( p, offset, ref u64_v );
      v = (i64)u64_v;
      return result;
    }
    static int getVarint( byte[] p, int offset, ref u64 v )
    {
      v = p[offset + 0];
      if ( v <= 0x7F ) return 1;
      int result = sqlite3GetVarint( p, offset, ref v );
      return result;
    }
    static int getVarint32( byte[] p, ref u32 v )
    { //(*B=*(A))<=0x7f?1:sqlite3GetVarint32(A,B))
      v = p[0];
      if ( v <= 0x7F ) return 1;
      return sqlite3GetVarint32( p, 0, ref v );
    }
    static int getVarint32( string s, u32 offset, ref int v )
    { //(*B=*(A))<=0x7f?1:sqlite3GetVarint32(A,B))
      v = s[(int)offset];
      if ( v <= 0x7F ) return 1;
      byte[] p = new byte[4];
      p[0] = (u8)s[(int)offset + 0];
      p[1] = (u8)s[(int)offset + 1];
      p[2] = (u8)s[(int)offset + 2];
      p[3] = (u8)s[(int)offset + 3];
      u32 u32_v = 0;
      int result = sqlite3GetVarint32( p, 0, ref u32_v );
      v = (int)u32_v;
      return sqlite3GetVarint32( p, 0, ref v );
    }
    static int getVarint32( string s, u32 offset, ref u32 v )
    { //(*B=*(A))<=0x7f?1:sqlite3GetVarint32(A,B))
      v = s[(int)offset];
      if ( v <= 0x7F ) return 1;
      byte[] p = new byte[4];
      p[0] = (u8)s[(int)offset + 0];
      p[1] = (u8)s[(int)offset + 1];
      p[2] = (u8)s[(int)offset + 2];
      p[3] = (u8)s[(int)offset + 3];
      return sqlite3GetVarint32( p, 0, ref v );
    }
    static int getVarint32( byte[] p, u32 offset, ref u32 v )
    { //(*B=*(A))<=0x7f?1:sqlite3GetVarint32(A,B))
      v = p[offset];
      if ( v <= 0x7F ) return 1;
      return sqlite3GetVarint32( p, (int)offset, ref v );
    }
    static int getVarint32( byte[] p, int offset, ref u32 v )
    { //(*B=*(A))<=0x7f?1:sqlite3GetVarint32(A,B))
      v = p[offset];
      if ( v <= 0x7F ) return 1;
      return sqlite3GetVarint32( p, offset, ref v );
    }
    static int getVarint32( byte[] p, int offset, ref int v )
    { //(*B=*(A))<=0x7f?1:sqlite3GetVarint32(A,B))
      v = p[offset + 0];
      if ( v <= 0x7F ) return 1;
      u32 u32_v = 0;
      int result = sqlite3GetVarint32( p, offset, ref u32_v );
      v = (int)u32_v;
      return result;
    }
    static int putVarint( byte[] p, int offset, int v )
    { return putVarint( p, offset, (u64)v ); }
    static int putVarint( byte[] p, int offset, u64 v )
    {
      return sqlite3PutVarint( p, offset, v );
    }
    static int sqlite3PutVarint( byte[] p, int offset, int v )
    { return sqlite3PutVarint( p, offset, (u64)v ); }
    static int sqlite3PutVarint( byte[] p, int offset, u64 v )
    {
      int i, j, n;
      u8[] buf = new u8[10];
      if ( ( v & ( ( (u64)0xff000000 ) << 32 ) ) != 0 )
      {
        p[offset + 8] = (byte)v;
        v >>= 8;
        for ( i = 7 ; i >= 0 ; i-- )
        {
          p[offset + i] = (byte)( ( v & 0x7f ) | 0x80 );
          v >>= 7;
        }
        return 9;
      }
      n = 0;
      do
      {
        buf[n++] = (byte)( ( v & 0x7f ) | 0x80 );
        v >>= 7;
      } while ( v != 0 );
      buf[0] &= 0x7f;
      Debug.Assert( n <= 9 );
      for ( i = 0, j = n - 1 ; j >= 0 ; j--, i++ )
      {
        p[offset + i] = buf[j];
      }
      return n;
    }

    /*
    ** This routine is a faster version of sqlite3PutVarint() that only
    ** works for 32-bit positive integers and which is optimized for
    ** the common case of small integers.
    */
    static int putVarint32( byte[] p, int offset, int v )
    {
#if !putVarint32
      if ( ( v & ~0x7f ) == 0 )
      {
        p[offset] = (byte)v;
        return 1;
      }
#endif
      if ( ( v & ~0x3fff ) == 0 )
      {
        p[offset] = (byte)( ( v >> 7 ) | 0x80 );
        p[offset + 1] = (byte)( v & 0x7f );
        return 2;
      }
      return sqlite3PutVarint( p, offset, v );
    }

    static int putVarint32( byte[] p, int v )
    {
      if ( ( v & ~0x7f ) == 0 )
      {
        p[0] = (byte)v;
        return 1;
      }
      else if ( ( v & ~0x3fff ) == 0 )
      {
        p[0] = (byte)( ( v >> 7 ) | 0x80 );
        p[1] = (byte)( v & 0x7f );
        return 2;
      }
      else
      {
        return sqlite3PutVarint( p, 0, v );
      }
    }

    /*
    ** Read a 64-bit variable-length integer from memory starting at p[0].
    ** Return the number of bytes read.  The value is stored in *v.
    */
    static u8 sqlite3GetVarint( byte[] p, int offset, ref u64 v )
    {
      u32 a, b, s;

      a = p[offset + 0];
      /* a: p0 (unmasked) */
      if ( 0 == ( a & 0x80 ) )
      {
        v = a;
        return 1;
      }

      //p++;
      b = p[offset + 1];
      /* b: p1 (unmasked) */
      if ( 0 == ( b & 0x80 ) )
      {
        a &= 0x7f;
        a = a << 7;
        a |= b;
        v = a;
        return 2;
      }

      //p++;
      a = a << 14;
      a |= p[offset + 2];
      /* a: p0<<14 | p2 (unmasked) */
      if ( 0 == ( a & 0x80 ) )
      {
        a &= ( 0x7f << 14 ) | ( 0x7f );
        b &= 0x7f;
        b = b << 7;
        a |= b;
        v = a;
        return 3;
      }

      /* CSE1 from below */
      a &= ( 0x7f << 14 ) | ( 0x7f );
      //p++;
      b = b << 14;
      b |= p[offset + 3];
      /* b: p1<<14 | p3 (unmasked) */
      if ( 0 == ( b & 0x80 ) )
      {
        b &= ( 0x7f << 14 ) | ( 0x7f );
        /* moved CSE1 up */
        /* a &= (0x7f<<14)|(0x7f); */
        a = a << 7;
        a |= b;
        v = a;
        return 4;
      }

      /* a: p0<<14 | p2 (masked) */
      /* b: p1<<14 | p3 (unmasked) */
      /* 1:save off p0<<21 | p1<<14 | p2<<7 | p3 (masked) */
      /* moved CSE1 up */
      /* a &= (0x7f<<14)|(0x7f); */
      b &= ( 0x7f << 14 ) | ( 0x7f );
      s = a;
      /* s: p0<<14 | p2 (masked) */

      //p++;
      a = a << 14;
      a |= p[offset + 4];
      /* a: p0<<28 | p2<<14 | p4 (unmasked) */
      if ( 0 == ( a & 0x80 ) )
      {
        /* we can skip these cause they were (effectively) done above in calc'ing s */
        /* a &= (0x1f<<28)|(0x7f<<14)|(0x7f); */
        /* b &= (0x7f<<14)|(0x7f); */
        b = b << 7;
        a |= b;
        s = s >> 18;
        v = ( (u64)s ) << 32 | a;
        return 5;
      }

      /* 2:save off p0<<21 | p1<<14 | p2<<7 | p3 (masked) */
      s = s << 7;
      s |= b;
      /* s: p0<<21 | p1<<14 | p2<<7 | p3 (masked) */

      //p++;
      b = b << 14;
      b |= p[offset + 5];
      /* b: p1<<28 | p3<<14 | p5 (unmasked) */
      if ( 0 == ( b & 0x80 ) )
      {
        /* we can skip this cause it was (effectively) done above in calc'ing s */
        /* b &= (0x1f<<28)|(0x7f<<14)|(0x7f); */
        a &= ( 0x7f << 14 ) | ( 0x7f );
        a = a << 7;
        a |= b;
        s = s >> 18;
        v = ( (u64)s ) << 32 | a;
        return 6;
      }

      //p++;
      a = a << 14;
      a |= p[offset + 6];
      /* a: p2<<28 | p4<<14 | p6 (unmasked) */
      if ( 0 == ( a & 0x80 ) )
      {
        a &= ( (u32)0x1f << 28 ) | ( 0x7f << 14 ) | ( 0x7f );
        b &= ( 0x7f << 14 ) | ( 0x7f );
        b = b << 7;
        a |= b;
        s = s >> 11;
        v = ( (u64)s ) << 32 | a;
        return 7;
      }

      /* CSE2 from below */
      a &= ( 0x7f << 14 ) | ( 0x7f );
      //p++;
      b = b << 14;
      b |= p[offset + 7];
      /* b: p3<<28 | p5<<14 | p7 (unmasked) */
      if ( 0 == ( b & 0x80 ) )
      {
        b &= ( (u32)0x1f << 28 ) | ( 0x7f << 14 ) | ( 0x7f );
        /* moved CSE2 up */
        /* a &= (0x7f<<14)|(0x7f); */
        a = a << 7;
        a |= b;
        s = s >> 4;
        v = ( (u64)s ) << 32 | a;
        return 8;
      }

      //p++;
      a = a << 15;
      a |= p[offset + 8];
      /* a: p4<<29 | p6<<15 | p8 (unmasked) */

      /* moved CSE2 up */
      /* a &= (0x7f<<29)|(0x7f<<15)|(0xff); */
      b &= ( 0x7f << 14 ) | ( 0x7f );
      b = b << 8;
      a |= b;

      s = s << 4;
      b = p[offset + 4];
      b &= 0x7f;
      b = b >> 3;
      s |= b;

      v = ( (u64)s ) << 32 | a;

      return 9;
    }


    /*
    ** Read a 32-bit variable-length integer from memory starting at p[0].
    ** Return the number of bytes read.  The value is stored in *v.
    **
    ** If the varint stored in p[0] is larger than can fit in a 32-bit unsigned
    ** integer, then set *v to 0xffffffff.
    **
    ** A MACRO version, getVarint32, is provided which inlines the
    ** single-byte case.  All code should use the MACRO version as
    ** this function assumes the single-byte case has already been handled.
    */
    static u8 sqlite3GetVarint32( byte[] p, ref int v )
    {
      u32 u32_v = 0;
      u8 result = sqlite3GetVarint32( p, 0, ref u32_v );
      v = (int)u32_v;
      return result;
    }
    static u8 sqlite3GetVarint32( byte[] p, int offset, ref int v )
    {
      u32 u32_v = 0;
      u8 result = sqlite3GetVarint32( p, offset, ref u32_v );
      v = (int)u32_v;
      return result;
    }
    static u8 sqlite3GetVarint32( byte[] p, ref u32 v )
    { return sqlite3GetVarint32( p, 0, ref v ); }
    static u8 sqlite3GetVarint32( byte[] p, int offset, ref u32 v )
    {
      u32 a, b;

      /* The 1-byte case.  Overwhelmingly the most common.  Handled inline
      ** by the getVarin32() macro */
      a = p[offset + 0];
      /* a: p0 (unmasked) */
      //#if getVarint32
      //  if ( 0==( a&0x80))
      //  {
      /* Values between 0 and 127 */
      //    v = a;
      //    return 1;
      //  }
      //#endif

      /* The 2-byte case */
      //p++;
      b = p[offset + 1];
      /* b: p1 (unmasked) */
      if ( 0 == ( b & 0x80 ) )
      {
        /* Values between 128 and 16383 */
        a &= 0x7f;
        a = a << 7;
        v = a | b;
        return 2;
      }

      /* The 3-byte case */
      //p++;
      a = a << 14;
      a |= p[offset + 2];
      /* a: p0<<14 | p2 (unmasked) */
      if ( 0 == ( a & 0x80 ) )
      {
        /* Values between 16384 and 2097151 */
        a &= ( 0x7f << 14 ) | ( 0x7f );
        b &= 0x7f;
        b = b << 7;
        v = a | b;
        return 3;
      }

      /* A 32-bit varint is used to store size information in btrees.
      ** Objects are rarely larger than 2MiB limit of a 3-byte varint.
      ** A 3-byte varint is sufficient, for example, to record the size
      ** of a 1048569-byte BLOB or string.
      **
      ** We only unroll the first 1-, 2-, and 3- byte cases.  The very
      ** rare larger cases can be handled by the slower 64-bit varint
      ** routine.
      */
#if TRUE
      {
        u64 v64 = 0;
        u8 n;

        //p -= 2;
        n = sqlite3GetVarint( p, offset, ref v64 );
        Debug.Assert( n > 3 && n <= 9 );
        if ( ( v64 & SQLITE_MAX_U32 ) != v64 )
        {
          v = 0xffffffff;
        }
        else
        {
          v = (u32)v64;
        } return n;
      }
#else
/* For following code (kept for historical record only) shows an
** unrolling for the 3- and 4-byte varint cases.  This code is
** slightly faster, but it is also larger and much harder to test.
*/
//p++;
b = b << 14;
b |= p[offset + 3];
/* b: p1<<14 | p3 (unmasked) */
if ( 0 == ( b & 0x80 ) )
{
/* Values between 2097152 and 268435455 */
b &= ( 0x7f << 14 ) | ( 0x7f );
a &= ( 0x7f << 14 ) | ( 0x7f );
a = a << 7;
v = a | b;
return 4;
}

//p++;
a = a << 14;
a |= p[offset + 4];
/* a: p0<<28 | p2<<14 | p4 (unmasked) */
if ( 0 == ( a & 0x80 ) )
{
/* Values  between 268435456 and 34359738367 */
a &= ( (u32)0x1f << 28 ) | ( 0x7f << 14 ) | ( 0x7f );
b &= ( (u32)0x1f << 28 ) | ( 0x7f << 14 ) | ( 0x7f );
b = b << 7;
v = a | b;
return 5;
}

/* We can only reach this point when reading a corrupt database
** file.  In that case we are not in any hurry.  Use the (relatively
** slow) general-purpose sqlite3GetVarint() routine to extract the
** value. */
{
u64 v64 = 0;
int n;

//p -= 4;
n = sqlite3GetVarint( p, offset, ref v64 );
Debug.Assert( n > 5 && n <= 9 );
v = (u32)v64;
return n;
}
#endif
    }


    /*
    ** Return the number of bytes that will be needed to store the given
    ** 64-bit integer.
    */
    static int sqlite3VarintLen( u64 v )
    {
      int i = 0;
      do
      {
        i++;
        v >>= 7;
      } while ( v != 0 && ALWAYS( i < 9 ) );
      return i;
    }


    /*
    ** Read or write a four-byte big-endian integer value.
    */
    static u32 sqlite3Get4byte( u8[] p, int p_offset, int offset )
    {
      offset += p_offset;
      return (u32)( ( p[0 + offset] << 24 ) | ( p[1 + offset] << 16 ) | ( p[2 + offset] << 8 ) | p[3 + offset] );
    }
    static u32 sqlite3Get4byte( u8[] p, int offset )
    {
      return (u32)( ( p[0 + offset] << 24 ) | ( p[1 + offset] << 16 ) | ( p[2 + offset] << 8 ) | p[3 + offset] );
    }
    static u32 sqlite3Get4byte( u8[] p, u32 offset )
    {
      return (u32)( ( p[0 + offset] << 24 ) | ( p[1 + offset] << 16 ) | ( p[2 + offset] << 8 ) | p[3 + offset] );
    }
    static u32 sqlite3Get4byte( u8[] p )
    {
      return (u32)( ( p[0] << 24 ) | ( p[1] << 16 ) | ( p[2] << 8 ) | p[3] );
    }
    static void sqlite3Put4byte( byte[] p, int v )
    {
      p[0] = (byte)( v >> 24 & 0xFF );
      p[1] = (byte)( v >> 16 & 0xFF );
      p[2] = (byte)( v >> 8 & 0xFF );
      p[3] = (byte)( v & 0xFF );
    }
    static void sqlite3Put4byte( byte[] p, int offset, int v )
    {
      p[0 + offset] = (byte)( v >> 24 & 0xFF );
      p[1 + offset] = (byte)( v >> 16 & 0xFF );
      p[2 + offset] = (byte)( v >> 8 & 0xFF );
      p[3 + offset] = (byte)( v & 0xFF );
    }
    static void sqlite3Put4byte( byte[] p, u32 offset, u32 v )
    {
      p[0 + offset] = (byte)( v >> 24 & 0xFF );
      p[1 + offset] = (byte)( v >> 16 & 0xFF );
      p[2 + offset] = (byte)( v >> 8 & 0xFF );
      p[3 + offset] = (byte)( v & 0xFF );
    }
    static void sqlite3Put4byte( byte[] p, int offset, u64 v )
    {
      p[0 + offset] = (byte)( v >> 24 & 0xFF );
      p[1 + offset] = (byte)( v >> 16 & 0xFF );
      p[2 + offset] = (byte)( v >> 8 & 0xFF );
      p[3 + offset] = (byte)( v & 0xFF );
    }
    static void sqlite3Put4byte( byte[] p, u64 v )
    {
      p[0] = (byte)( v >> 24 & 0xFF );
      p[1] = (byte)( v >> 16 & 0xFF );
      p[2] = (byte)( v >> 8 & 0xFF );
      p[3] = (byte)( v & 0xFF );
    }



#if !SQLITE_OMIT_BLOB_LITERAL || SQLITE_HAS_CODEC
    /*
** Translate a single byte of Hex into an integer.
** This routinen only works if h really is a valid hexadecimal
** character:  0..9a..fA..F
*/
    static int hexToInt( int h )
    {
      Debug.Assert( ( h >= '0' && h <= '9' ) || ( h >= 'a' && h <= 'f' ) || ( h >= 'A' && h <= 'F' ) );
#if SQLITE_ASCII
      h += 9 * ( 1 & ( h >> 6 ) );
#endif
#if SQLITE_EBCDIC
h += 9*(1&~(h>>4));
#endif
      return h & 0xf;
    }
#endif // * !SQLITE_OMIT_BLOB_LITERAL || SQLITE_HAS_CODEC */

#if !SQLITE_OMIT_BLOB_LITERAL || SQLITE_HAS_CODEC
    /*
** Convert a BLOB literal of the form "x'hhhhhh'" into its binary
** value.  Return a pointer to its binary value.  Space to hold the
** binary value has been obtained from malloc and must be freed by
** the calling routine.
*/
    static byte[] sqlite3HexToBlob( sqlite3 db, string z, int n )
    {
      StringBuilder zBlob;
      int i;

      zBlob = new StringBuilder( n / 2 + 1 );// (char*)sqlite3DbMallocRaw(db, n / 2 + 1);
      n--;
      if ( zBlob != null )
      {
        for ( i = 0 ; i < n ; i += 2 )
        {
          zBlob.Append( Convert.ToChar( ( hexToInt( z[i] ) << 4 ) | hexToInt( z[i + 1] ) ) );
        }
        //zBlob[i / 2] = '\0'; ;
      }
      return Encoding.UTF8.GetBytes( zBlob.ToString() );
    }
#endif // * !SQLITE_OMIT_BLOB_LITERAL || SQLITE_HAS_CODEC */



    /*
** Change the sqlite.magic from SQLITE_MAGIC_OPEN to SQLITE_MAGIC_BUSY.
** Return an error (non-zero) if the magic was not SQLITE_MAGIC_OPEN
** when this routine is called.
**
** This routine is called when entering an SQLite API.  The SQLITE_MAGIC_OPEN
** value indicates that the database connection passed into the API is
** open and is not being used by another thread.  By changing the value
** to SQLITE_MAGIC_BUSY we indicate that the connection is in use.
** sqlite3SafetyOff() below will change the value back to SQLITE_MAGIC_OPEN
** when the API exits.
**
** This routine is a attempt to detect if two threads use the
** same sqlite* pointer at the same time.  There is a race
** condition so it is possible that the error is not detected.
** But usually the problem will be seen.  The result will be an
** error which can be used to debug the application that is
** using SQLite incorrectly.
**
** Ticket #202:  If db.magic is not a valid open value, take care not
** to modify the db structure at all.  It could be that db is a stale
** pointer.  In other words, it could be that there has been a prior
** call to sqlite3_close(db) and db has been deallocated.  And we do
** not want to write into deallocated memory.
*/
#if SQLITE_DEBUG
    static bool sqlite3SafetyOn( sqlite3 db )
    {
      if ( db.magic == SQLITE_MAGIC_OPEN )
      {
        db.magic = SQLITE_MAGIC_BUSY;
        Debug.Assert( sqlite3_mutex_held( db.mutex ) );
        return false;
      }
      else if ( db.magic == SQLITE_MAGIC_BUSY )
      {
        db.magic = SQLITE_MAGIC_ERROR;
        db.u1.isInterrupted = true;
      }
      return true;
    }
#else
static bool sqlite3SafetyOn( sqlite3 db ) {return false;}
#endif

    /*
** Change the magic from SQLITE_MAGIC_BUSY to SQLITE_MAGIC_OPEN.
** Return an error (non-zero) if the magic was not SQLITE_MAGIC_BUSY
** when this routine is called.
*/
#if SQLITE_DEBUG
    static bool sqlite3SafetyOff( sqlite3 db )
    {
      if ( db.magic == SQLITE_MAGIC_BUSY )
      {
        db.magic = SQLITE_MAGIC_OPEN;
        Debug.Assert( sqlite3_mutex_held( db.mutex ) );
        return false;
      }
      else
      {
        db.magic = SQLITE_MAGIC_ERROR;
        db.u1.isInterrupted = true;
        return true;
      }
    }
#else
static bool sqlite3SafetyOff( sqlite3 db ) { return false; }
#endif

    /*
** Check to make sure we have a valid db pointer.  This test is not
** foolproof but it does provide some measure of protection against
** misuse of the interface such as passing in db pointers that are
** NULL or which have been previously closed.  If this routine returns
** 1 it means that the db pointer is valid and 0 if it should not be
** dereferenced for any reason.  The calling function should invoke
** SQLITE_MISUSE immediately.
**
** sqlite3SafetyCheckOk() requires that the db pointer be valid for
** use.  sqlite3SafetyCheckSickOrOk() allows a db pointer that failed to
** open properly and is not fit for general use but which can be
** used as an argument to sqlite3_errmsg() or sqlite3_close().
*/
    static bool sqlite3SafetyCheckOk( sqlite3 db )
    {
      u32 magic;
      if ( db == null ) return false;
      magic = db.magic;
      if ( magic != SQLITE_MAGIC_OPEN
#if SQLITE_DEBUG
 && magic != SQLITE_MAGIC_BUSY
#endif
 )
      {
        return false;
      }
      else
      {
        return true;
      }
    }

    static bool sqlite3SafetyCheckSickOrOk( sqlite3 db )
    {
      u32 magic;
      magic = db.magic;
      if ( magic != SQLITE_MAGIC_SICK &&
      magic != SQLITE_MAGIC_OPEN &&
      magic != SQLITE_MAGIC_BUSY ) return false;
      return true;
    }
  }
}
