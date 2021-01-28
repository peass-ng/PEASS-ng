using System;
using System.Diagnostics;
using System.Text;

using time_t = System.Int64;
using sqlite3_int64 = System.Int64;
using i64 = System.Int64;
using u64 = System.UInt64;

namespace CS_SQLite3
{
  using sqlite3_value = CSSQLite.Mem;

  public partial class CSSQLite
  {
    /*
    ** 2003 October 31
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains the C functions that implement date and time
    ** functions for SQLite.
    **
    ** There is only one exported symbol in this file - the function
    ** sqlite3RegisterDateTimeFunctions() found at the bottom of the file.
    ** All other code has file scope.
    **
    ** $Id: date.c,v 1.107 2009/05/03 20:23:53 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    **
    ** SQLite processes all times and dates as Julian Day numbers.  The
    ** dates and times are stored as the number of days since noon
    ** in Greenwich on November 24, 4714 B.C. according to the Gregorian
    ** calendar system.
    **
    ** 1970-01-01 00:00:00 is JD 2440587.5
    ** 2000-01-01 00:00:00 is JD 2451544.5
    **
    ** This implemention requires years to be expressed as a 4-digit number
    ** which means that only dates between 0000-01-01 and 9999-12-31 can
    ** be represented, even though julian day numbers allow a much wider
    ** range of dates.
    **
    ** The Gregorian calendar system is used for all dates and times,
    ** even those that predate the Gregorian calendar.  Historians usually
    ** use the Julian calendar for dates prior to 1582-10-15 and for some
    ** dates afterwards, depending on locale.  Beware of this difference.
    **
    ** The conversion algorithms are implemented based on descriptions
    ** in the following text:
    **
    **      Jean Meeus
    **      Astronomical Algorithms, 2nd Edition, 1998
    **      ISBM 0-943396-61-1
    **      Willmann-Bell, Inc
    **      Richmond, Virginia (USA)
    */
    //#include "sqliteInt.h"
    //#include <stdlib.h>
    //#include <assert.h>
    //#include <time.h>

#if !SQLITE_OMIT_DATETIME_FUNCS

    /*
** On recent Windows platforms, the localtime_s() function is available
** as part of the "Secure CRT". It is essentially equivalent to
** localtime_r() available under most POSIX platforms, except that the
** order of the parameters is reversed.
**
** See http://msdn.microsoft.com/en-us/library/a442x3ye(VS.80).aspx.
**
** If the user has not indicated to use localtime_r() or localtime_s()
** already, check for an MSVC build environment that provides
** localtime_s().
*/
#if !(HAVE_LOCALTIME_R) && !(HAVE_LOCALTIME_S) &&      (_MSC_VER) && (_CRT_INSECURE_DEPRECATE)
#define HAVE_LOCALTIME_S
#endif

    /*
** A structure for holding a single date and time.
*/
    //typedef struct DateTime DateTime;
    public class DateTime
    {
      public sqlite3_int64 iJD; /* The julian day number times 86400000 */
      public int Y, M, D;       /* Year, month, and day */
      public int h, m;          /* Hour and minutes */
      public int tz;            /* Timezone offset in minutes */
      public double s;          /* Seconds */
      public byte validYMD;     /* True (1) if Y,M,D are valid */
      public byte validHMS;     /* True (1) if h,m,s are valid */
      public byte validJD;      /* True (1) if iJD is valid */
      public byte validTZ;      /* True (1) if tz is valid */

      public void CopyTo( DateTime ct )
      {
        ct.iJD = iJD;
        ct.Y = Y;
        ct.M = M;
        ct.D = D;
        ct.h = h;
        ct.m = m;
        ct.tz = tz;
        ct.s = s;
        ct.validYMD = validYMD;
        ct.validHMS = validHMS;
        ct.validJD = validJD;
        ct.validTZ = validJD;
      }
    };


    /*
    ** Convert zDate into one or more integers.  Additional arguments
    ** come in groups of 5 as follows:
    **
    **       N       number of digits in the integer
    **       min     minimum allowed value of the integer
    **       max     maximum allowed value of the integer
    **       nextC   first character after the integer
    **       pVal    where to write the integers value.
    **
    ** Conversions continue until one with nextC==0 is encountered.
    ** The function returns the number of successful conversions.
    */
    static int getDigits( string zDate, int N0, int min0, int max0, char nextC0, ref int pVal0, int N1, int min1, int max1, char nextC1, ref int pVal1 )
    {
      int c0 = getDigits( zDate + '\0', N0, min0, max0, nextC0, ref  pVal0 );
      return c0 == 0 ? 0 : c0 + getDigits( zDate.Substring( zDate.IndexOf( nextC0 ) + 1 ) + '\0', N1, min1, max1, nextC1, ref  pVal1 );
    }
    static int getDigits( string zDate, int N0, int min0, int max0, char nextC0, ref int pVal0, int N1, int min1, int max1, char nextC1, ref int pVal1, int N2, int min2, int max2, char nextC2, ref int pVal2 )
    {
      int c0 = getDigits( zDate + '\0', N0, min0, max0, nextC0, ref  pVal0 );
      if ( c0 == 0 ) return 0;
      string zDate1 = zDate.Substring( zDate.IndexOf( nextC0 ) + 1 );
      int c1 = getDigits( zDate1 + '\0', N1, min1, max1, nextC1, ref  pVal1 );
      if ( c1 == 0 ) return c0;
      return c0 + c1 + getDigits( zDate1.Substring( zDate1.IndexOf( nextC1 ) + 1 ) + '\0', N2, min2, max2, nextC2, ref  pVal2 );
    }
    static int getDigits( string zDate, int N, int min, int max, char nextC, ref int pVal )
    {
      //va_list ap;
      int val;
      //int N;
      //int min;
      //int max;
      //char nextC;
      //int pVal;
      int cnt = 0;
      //va_start( ap, zDate );
      int zIndex = 0;
      //do
      //{
      //N = (int)va_arg( ap, "int" );
      //min = (int)va_arg( ap, "int" );
      //max = (int)va_arg( ap, "int" );
      //nextC = (char)va_arg( ap, "char" );
      //pVal = (int)va_arg( ap, "int" );
      val = 0;
      while ( N-- != 0 )
      {
        if ( !sqlite3Isdigit( zDate[zIndex] ) )
        {
          goto end_getDigits;
        }
        val = val * 10 + zDate[zIndex] - '0';
        zIndex++;
      }
      if ( val < min || val > max || zIndex < zDate.Length && ( nextC != 0 && nextC != zDate[zIndex] ) )
      {
        goto end_getDigits;
      }
      pVal = val;
      zIndex++;
      cnt++;
//} while ( nextC != 0 && zIndex < zDate.Length );
end_getDigits:
      //va_end( ap );
      return cnt;
    }

    /*
    ** Read text from z[] and convert into a floating point number.  Return
    ** the number of digits converted.
    */
    //#define getValue sqlite3AtoF

    /*
    ** Parse a timezone extension on the end of a date-time.
    ** The extension is of the form:
    **
    **        (+/-)HH:MM
    **
    **
    ** Or the "zulu" notation:
    **
    **        Z
    **
    ** If the parse is successful, write the number of minutes
    ** of change in p.tz and return 0.  If a parser error occurs,
    ** return non-zero.
    **
    ** A missing specifier is not considered an error.
    */
    static int parseTimezone( string zDate, DateTime p )
    {
      int sgn = 0;
      int nHr = 0; int nMn = 0;
      char c;
      zDate = zDate.Trim();// while ( sqlite3Isspace( *(u8*)zDate ) ) { zDate++; }
      p.tz = 0;
      c = zDate.Length == 0 ? '\0' : zDate[0];
      if ( c == '-' )
      {
        sgn = -1;
      }
      else if ( c == '+' )
      {
        sgn = +1;
      }
      else if ( c == 'Z' || c == 'z' )
      {
        zDate = zDate.Substring( 1 ).Trim();//zDate++;
        goto zulu_time;
      }
      else
      {
        return c != '\0' ? 1 : 0;
      }
      //zDate++;
      if ( getDigits( zDate.Substring( 1 ), 2, 0, 14, ':', ref nHr, 2, 0, 59, '\0', ref nMn ) != 2 )
      {
        return 1;
      }
      //zDate += 5;
      p.tz = sgn * ( nMn + nHr * 60 );
      if ( zDate.Length == 6 ) zDate = "";
      else if ( zDate.Length > 6 ) zDate = zDate.Substring( 6 ).Trim();// while ( sqlite3Isspace( *(u8*)zDate ) ) { zDate++; }
zulu_time:
      return zDate != "" ? 1 : 0;
    }

    /*
    ** Parse times of the form HH:MM or HH:MM:SS or HH:MM:SS.FFFF.
    ** The HH, MM, and SS must each be exactly 2 digits.  The
    ** fractional seconds FFFF can be one or more digits.
    **
    ** Return 1 if there is a parsing error and 0 on success.
    */
    static int parseHhMmSs( string zDate, DateTime p )
    {
      int h = 0; int m = 0; int s = 0;
      double ms = 0.0;
      if ( getDigits( zDate, 2, 0, 24, ':', ref  h, 2, 0, 59, '\0', ref  m ) != 2 )
      {
        return 1;
      }
      int zIndex = 5;// zDate += 5;
      if ( zIndex < zDate.Length && zDate[zIndex] == ':' )
      {
        zIndex++;// zDate++;
        if ( getDigits( zDate.Substring( zIndex ), 2, 0, 59, '\0', ref s ) != 1 )
        {
          return 1;
        }
        zIndex += 2;// zDate += 2;
        if ( zIndex + 1 < zDate.Length && zDate[zIndex] == '.' && sqlite3Isdigit( zDate[zIndex + 1] ) )
        {
          double rScale = 1.0;
          zIndex++;// zDate++;
          while ( zIndex < zDate.Length && sqlite3Isdigit( zDate[zIndex] )
          )
          {
            ms = ms * 10.0 + zDate[zIndex] - '0';
            rScale *= 10.0;
            zIndex++;//zDate++;
          }
          ms /= rScale;
        }
      }
      else
      {
        s = 0;
      }
      p.validJD = 0;
      p.validHMS = 1;
      p.h = h;
      p.m = m;
      p.s = s + ms;
      if ( zIndex < zDate.Length && parseTimezone( zDate.Substring( zIndex ), p ) != 0 ) return 1;
      p.validTZ = (byte)( ( p.tz != 0 ) ? 1 : 0 );
      return 0;
    }

    /*
    ** Convert from YYYY-MM-DD HH:MM:SS to julian day.  We always assume
    ** that the YYYY-MM-DD is according to the Gregorian calendar.
    **
    ** Reference:  Meeus page 61
    */
    static void computeJD( DateTime p )
    {
      int Y, M, D, A, B, X1, X2;

      if ( p.validJD != 0 ) return;
      if ( p.validYMD != 0 )
      {
        Y = p.Y;
        M = p.M;
        D = p.D;
      }
      else
      {
        Y = 2000;  /* If no YMD specified, assume 2000-Jan-01 */
        M = 1;
        D = 1;
      }
      if ( M <= 2 )
      {
        Y--;
        M += 12;
      }
      A = Y / 100;
      B = 2 - A + ( A / 4 );
      X1 = (int)( 36525 * ( Y + 4716 ) / 100 );
      X2 = (int)( 306001 * ( M + 1 ) / 10000 );
      p.iJD = (long)( ( X1 + X2 + D + B - 1524.5 ) * 86400000 );
      p.validJD = 1;
      if ( p.validHMS != 0 )
      {
        p.iJD += (long)( p.h * 3600000 + p.m * 60000 + p.s * 1000 );
        if ( p.validTZ != 0 )
        {
          p.iJD -= p.tz * 60000;
          p.validYMD = 0;
          p.validHMS = 0;
          p.validTZ = 0;
        }
      }
    }

    /*
    ** Parse dates of the form
    **
    **     YYYY-MM-DD HH:MM:SS.FFF
    **     YYYY-MM-DD HH:MM:SS
    **     YYYY-MM-DD HH:MM
    **     YYYY-MM-DD
    **
    ** Write the result into the DateTime structure and return 0
    ** on success and 1 if the input string is not a well-formed
    ** date.
    */
    static int parseYyyyMmDd( string zDate, DateTime p )
    {
      int Y = 0; int M = 0; int D = 0; bool neg;

      int zIndex = 0;
      if ( zDate[zIndex] == '-' )
      {
        zIndex++;// zDate++;
        neg = true;
      }
      else
      {
        neg = false;
      }
      if ( getDigits( zDate.Substring( zIndex ), 4, 0, 9999, '-', ref Y, 2, 1, 12, '-', ref M, 2, 1, 31, '\0', ref D ) != 3 )
      {
        return 1;
      }
      zIndex += 10;// zDate += 10;
      while ( zIndex < zDate.Length && ( sqlite3Isspace( zDate[zIndex] ) || 'T' == zDate[zIndex] ) ) { zIndex++; }//zDate++; }
      if ( zIndex < zDate.Length && parseHhMmSs( zDate.Substring( zIndex ), p ) == 0 )
      {
        /* We got the time */
      }
      else if ( zIndex >= zDate.Length )// zDate[zIndex] == '\0')
      {
        p.validHMS = 0;
      }
      else
      {
        return 1;
      }
      p.validJD = 0;
      p.validYMD = 1;
      p.Y = neg ? -Y : Y;
      p.M = M;
      p.D = D;
      if ( p.validTZ != 0 )
      {
        computeJD( p );
      }
      return 0;
    }

    /*
    ** Set the time to the current time reported by the VFS
    */
    static void setDateTimeToCurrent( sqlite3_context context, DateTime p )
    {
      double r = 0;
      sqlite3 db = sqlite3_context_db_handle( context );
      sqlite3OsCurrentTime( db.pVfs, ref r );
      p.iJD = (sqlite3_int64)( r * 86400000.0 + 0.5 );
      p.validJD = 1;
    }

    /*
    ** Attempt to parse the given string into a Julian Day Number.  Return
    ** the number of errors.
    **
    ** The following are acceptable forms for the input string:
    **
    **      YYYY-MM-DD HH:MM:SS.FFF  +/-HH:MM
    **      DDDD.DD
    **      now
    **
    ** In the first form, the +/-HH:MM is always optional.  The fractional
    ** seconds extension (the ".FFF") is optional.  The seconds portion
    ** (":SS.FFF") is option.  The year and date can be omitted as long
    ** as there is a time string.  The time string can be omitted as long
    ** as there is a year and date.
    */
    static int parseDateOrTime(
    sqlite3_context context,
    string zDate,
    ref DateTime p
    )
    {
      int isRealNum = 0;    /* Return from sqlite3IsNumber().  Not used */
      if ( parseYyyyMmDd( zDate, p ) == 0 )
      {
        return 0;
      }
      else if ( parseHhMmSs( zDate, p ) == 0 )
      {
        return 0;
      }
      else if ( sqlite3StrICmp( zDate, "now" ) == 0 )
      {
        setDateTimeToCurrent( context, p );
        return 0;
      }
      else if ( sqlite3IsNumber( zDate, ref isRealNum, SQLITE_UTF8 ) != 0 )
      {
        double r = 0;
        sqlite3AtoF( zDate, ref r );// getValue( zDate, ref r );
        p.iJD = (sqlite3_int64)( r * 86400000.0 + 0.5 );
        p.validJD = 1;
        return 0;
      }
      return 1;
    }

    /*
    ** Compute the Year, Month, and Day from the julian day number.
    */
    static void computeYMD( DateTime p )
    {
      int Z, A, B, C, D, E, X1;
      if ( p.validYMD != 0 ) return;
      if ( 0 == p.validJD )
      {
        p.Y = 2000;
        p.M = 1;
        p.D = 1;
      }
      else
      {
        Z = (int)( ( p.iJD + 43200000 ) / 86400000 );
        A = (int)( ( Z - 1867216.25 ) / 36524.25 );
        A = Z + 1 + A - ( A / 4 );
        B = A + 1524;
        C = (int)( ( B - 122.1 ) / 365.25 );
        D = (int)( ( 36525 * C ) / 100 );
        E = (int)( ( B - D ) / 30.6001 );
        X1 = (int)( 30.6001 * E );
        p.D = B - D - X1;
        p.M = E < 14 ? E - 1 : E - 13;
        p.Y = p.M > 2 ? C - 4716 : C - 4715;
      }
      p.validYMD = 1;
    }

    /*
    ** Compute the Hour, Minute, and Seconds from the julian day number.
    */
    static void computeHMS( DateTime p )
    {
      int s;
      if ( p.validHMS != 0 ) return;
      computeJD( p );
      s = (int)( ( p.iJD + 43200000 ) % 86400000 );
      p.s = s / 1000.0;
      s = (int)p.s;
      p.s -= s;
      p.h = s / 3600;
      s -= p.h * 3600;
      p.m = s / 60;
      p.s += s - p.m * 60;
      p.validHMS = 1;
    }

    /*
    ** Compute both YMD and HMS
    */
    static void computeYMD_HMS( DateTime p )
    {
      computeYMD( p );
      computeHMS( p );
    }

    /*
    ** Clear the YMD and HMS and the TZ
    */
    static void clearYMD_HMS_TZ( DateTime p )
    {
      p.validYMD = 0;
      p.validHMS = 0;
      p.validTZ = 0;
    }

#if !SQLITE_OMIT_LOCALTIME
    /*
** Compute the difference (in milliseconds)
** between localtime and UTC (a.k.a. GMT)
** for the time value p where p is in UTC.
*/
    static int localtimeOffset( DateTime p )
    {
      DateTime x; DateTime y = new DateTime();
      time_t t;
      x = p;
      computeYMD_HMS( x );
      if ( x.Y < 1971 || x.Y >= 2038 )
      {
        x.Y = 2000;
        x.M = 1;
        x.D = 1;
        x.h = 0;
        x.m = 0;
        x.s = 0.0;
      }
      else
      {
        int s = (int)( x.s + 0.5 );
        x.s = s;
      }
      x.tz = 0;
      x.validJD = 0;
      computeJD( x );
      t = (long)( x.iJD / 1000 - 210866760000L );//  t = x.iJD/1000 - 21086676*(i64)10000;
#if  HAVE_LOCALTIME_R
{
struct tm sLocal;
localtime_r(&t, sLocal);
y.Y = sLocal.tm_year + 1900;
y.M = sLocal.tm_mon + 1;
y.D = sLocal.tm_mday;
y.h = sLocal.tm_hour;
y.m = sLocal.tm_min;
y.s = sLocal.tm_sec;
}
#elif (HAVE_LOCALTIME_S)
{
struct tm sLocal;
localtime_s(&sLocal, t);
y.Y = sLocal.tm_year + 1900;
y.M = sLocal.tm_mon + 1;
y.D = sLocal.tm_mday;
y.h = sLocal.tm_hour;
y.m = sLocal.tm_min;
y.s = sLocal.tm_sec;
}
#else
      {
        tm pTm;
        sqlite3_mutex_enter( sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER ) );
        pTm = localtime( t );
        y.Y = pTm.tm_year;// +1900;
        y.M = pTm.tm_mon;// +1;
        y.D = pTm.tm_mday;
        y.h = pTm.tm_hour;
        y.m = pTm.tm_min;
        y.s = pTm.tm_sec;
        sqlite3_mutex_leave( sqlite3MutexAlloc( SQLITE_MUTEX_STATIC_MASTER ) );
      }
#endif
      y.validYMD = 1;
      y.validHMS = 1;
      y.validJD = 0;
      y.validTZ = 0;
      computeJD( y );
      return (int)( y.iJD - x.iJD );
    }
#endif //* SQLITE_OMIT_LOCALTIME */

    /*
** Process a modifier to a date-time stamp.  The modifiers are
** as follows:
**
**     NNN days
**     NNN hours
**     NNN minutes
**     NNN.NNNN seconds
**     NNN months
**     NNN years
**     start of month
**     start of year
**     start of week
**     start of day
**     weekday N
**     unixepoch
**     localtime
**     utc
**
** Return 0 on success and 1 if there is any kind of error.
*/
    static int parseModifier( string zMod, DateTime p )
    {
      int rc = 1;
      int n;
      double r = 0;
      StringBuilder z = new StringBuilder( zMod.ToLower() );
      string zBuf;//[30];
      //z = zBuf;
      //for(n=0; n<ArraySize(zBuf)-1 && zMod[n]; n++){
      //  z.Append( zMod.Substring( n ).ToLower() );
      //}
      //z[n] = 0;
      switch ( z[0] )
#if !SQLITE_OMIT_LOCALTIME
      {
        case 'l':
          {
            /*    localtime
            **
            ** Assuming the current time value is UTC (a.k.a. GMT), shift it to
            ** show local time.
            */
            if ( z.ToString() == "localtime" )
            {
              computeJD( p );
              p.iJD += localtimeOffset( p );
              clearYMD_HMS_TZ( p );
              rc = 0;
            }
            break;
          }
#endif
        case 'u':
          {
            /*
            **    unixepoch
            **
            ** Treat the current value of p.iJD as the number of
            ** seconds since 1970.  Convert to a real julian day number.
            */
            if ( z.ToString() == "unixepoch" && p.validJD != 0 )
            {
              p.iJD = (long)( ( p.iJD + 43200 ) / 86400 + 210866760000000L );//p->iJD = p->iJD/86400 + 21086676*(i64)10000000;
              clearYMD_HMS_TZ( p );
              rc = 0;
            }
#if   !SQLITE_OMIT_LOCALTIME
            else if ( z.ToString() == "utc" )
            {
              int c1;
              computeJD( p );
              c1 = localtimeOffset( p );
              p.iJD -= (long)c1;
              clearYMD_HMS_TZ( p );
              p.iJD += (long)( c1 - localtimeOffset( p ) );
              rc = 0;
            }
#endif
            break;
          }
        case 'w':
          {
            /*
            **    weekday N
            **
            ** Move the date to the same time on the next occurrence of
            ** weekday N where 0==Sunday, 1==Monday, and so forth.  If the
            ** date is already on the appropriate weekday, this is a no-op.
            */
            if ( z.ToString().StartsWith( "weekday " ) && sqlite3AtoF( z.ToString().Substring( 8 ), ref r ) != 0 //getValue( z[8], ref r ) > 0
            && ( n = (int)r ) == r && n >= 0 && r < 7 )
            {
              sqlite3_int64 Z;
              computeYMD_HMS( p );
              p.validTZ = 0;
              p.validJD = 0;
              computeJD( p );
              Z = ( ( p.iJD + 129600000 ) / 86400000 ) % 7;
              if ( Z > n ) Z -= 7;
              p.iJD += ( n - Z ) * 86400000;
              clearYMD_HMS_TZ( p );
              rc = 0;
            }
            break;
          }
        case 's':
          {
            /*
            **    start of TTTTT
            **
            ** Move the date backwards to the beginning of the current day,
            ** or month or year.
            */
            if ( z.Length <= 9 ) z.Length = 0; else z.Remove( 0, 9 );//z += 9;
            computeYMD( p );
            p.validHMS = 1;
            p.h = p.m = 0;
            p.s = 0.0;
            p.validTZ = 0;
            p.validJD = 0;
            if ( z.ToString() == "month" )
            {
              p.D = 1;
              rc = 0;
            }
            else if ( z.ToString() == "year" )
            {
              computeYMD( p );
              p.M = 1;
              p.D = 1;
              rc = 0;
            }
            else if ( z.ToString() == "day" )
            {
              rc = 0;
            }
            break;
          }
        case '+':
        case '-':
        case '0':
        case '1':
        case '2':
        case '3':
        case '4':
        case '5':
        case '6':
        case '7':
        case '8':
        case '9':
          {
            double rRounder;
            n = sqlite3AtoF( z.ToString(), ref r );//getValue( z, ref r );
            Debug.Assert( n >= 1 );
            if ( z[n] == ':' )
            {
              /* A modifier of the form (+|-)HH:MM:SS.FFF adds (or subtracts) the
              ** specified number of hours, minutes, seconds, and fractional seconds
              ** to the time.  The ".FFF" may be omitted.  The ":SS.FFF" may be
              ** omitted.
              */
              string z2 = z.ToString();
              DateTime tx;
              sqlite3_int64 day;
              int z2Index = 0;
              if ( !sqlite3Isdigit( z2[z2Index] ) ) z2Index++;// z2++;
              tx = new DateTime();// memset( &tx, 0, sizeof(tx));
              if ( parseHhMmSs( z2.Substring( z2Index ), tx ) != 0 ) break;
              computeJD( tx );
              tx.iJD -= 43200000;
              day = tx.iJD / 86400000;
              tx.iJD -= day * 86400000;
              if ( z[0] == '-' ) tx.iJD = -tx.iJD;
              computeJD( p );
              clearYMD_HMS_TZ( p );
              p.iJD += tx.iJD;
              rc = 0;
              break;
            }
            //z += n;
            while ( sqlite3Isspace( z[n] ) ) n++;// z++;
            z = z.Remove( 0, n );
            n = sqlite3Strlen30( z );
            if ( n > 10 || n < 3 ) break;
            if ( z[n - 1] == 's' ) { z.Length = --n; }// z[n - 1] = '\0'; n--; }
            computeJD( p );
            rc = 0;
            rRounder = r < 0 ? -0.5 : +0.5;
            if ( n == 3 && z.ToString() == "day" )
            {
              p.iJD += (long)( r * 86400000.0 + rRounder );
            }
            else if ( n == 4 && z.ToString() == "hour" )
            {
              p.iJD += (long)( r * ( 86400000.0 / 24.0 ) + rRounder );
            }
            else if ( n == 6 && z.ToString() == "minute" )
            {
              p.iJD += (long)( r * ( 86400000.0 / ( 24.0 * 60.0 ) ) + rRounder );
            }
            else if ( n == 6 && z.ToString() == "second" )
            {
              p.iJD += (long)( r * ( 86400000.0 / ( 24.0 * 60.0 * 60.0 ) ) + rRounder );
            }
            else if ( n == 5 && z.ToString() == "month" )
            {
              int x, y;
              computeYMD_HMS( p );
              p.M += (int)r;
              x = p.M > 0 ? ( p.M - 1 ) / 12 : ( p.M - 12 ) / 12;
              p.Y += x;
              p.M -= x * 12;
              p.validJD = 0;
              computeJD( p );
              y = (int)r;
              if ( y != r )
              {
                p.iJD += (long)( ( r - y ) * 30.0 * 86400000.0 + rRounder );
              }
            }
            else if ( n == 4 && z.ToString() == "year" )
            {
              int y = (int)r;
              computeYMD_HMS( p );
              p.Y += y;
              p.validJD = 0;
              computeJD( p );
              if ( y != r )
              {
                p.iJD += (sqlite3_int64)( ( r - y ) * 365.0 * 86400000.0 + rRounder );
              }
            }
            else
            {
              rc = 1;
            }
            clearYMD_HMS_TZ( p );
            break;
          }
        default:
          {
            break;
          }
      }
      return rc;
    }

    /*
    ** Process time function arguments.  argv[0] is a date-time stamp.
    ** argv[1] and following are modifiers.  Parse them all and write
    ** the resulting time into the DateTime structure p.  Return 0
    ** on success and 1 if there are any errors.
    **
    ** If there are zero parameters (if even argv[0] is undefined)
    ** then assume a default value of "now" for argv[0].
    */
    static int isDate(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv,
    ref  DateTime p
    )
    {
      int i;
      string z;
      int eType;
      p = new DateTime();//memset(p, 0, sizeof(*p));
      if ( argc == 0 )
      {
        setDateTimeToCurrent( context, p );
      }
      else if ( ( eType = sqlite3_value_type( argv[0] ) ) == SQLITE_FLOAT
      || eType == SQLITE_INTEGER )
      {
        p.iJD = (long)( sqlite3_value_double( argv[0] ) * 86400000.0 + 0.5 );
        p.validJD = 1;
      }
      else
      {
        z = sqlite3_value_text( argv[0] );
        if ( String.IsNullOrEmpty( z ) || parseDateOrTime( context, z, ref p ) != 0 )
        {
          return 1;
        }
      }
      for ( i = 1 ; i < argc ; i++ )
      {
        if ( String.IsNullOrEmpty( z = sqlite3_value_text( argv[i] ) ) || parseModifier( z, p ) != 0 )
        {
          return 1;
        }
      }
      return 0;
    }


    /*
    ** The following routines implement the various date and time functions
    ** of SQLite.
    */

    /*
    **    julianday( TIMESTRING, MOD, MOD, ...)
    **
    ** Return the julian day number of the date specified in the arguments
    */
    static void juliandayFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      DateTime x = null;
      if ( isDate( context, argc, argv, ref x ) == 0 )
      {
        computeJD( x );
        sqlite3_result_double( context, x.iJD / 86400000.0 );
      }
    }

    /*
    **    datetime( TIMESTRING, MOD, MOD, ...)
    **
    ** Return YYYY-MM-DD HH:MM:SS
    */
    static void datetimeFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      DateTime x = null;
      if ( isDate( context, argc, argv, ref x ) == 0 )
      {
        string zBuf = "";//[100];
        computeYMD_HMS( x );
        sqlite3_snprintf( 100, ref zBuf, "%04d-%02d-%02d %02d:%02d:%02d",
        x.Y, x.M, x.D, x.h, x.m, (int)( x.s ) );
        sqlite3_result_text( context, zBuf, -1, SQLITE_TRANSIENT );
      }
    }

    /*
    **    time( TIMESTRING, MOD, MOD, ...)
    **
    ** Return HH:MM:SS
    */
    static void timeFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      DateTime x = new DateTime();
      if ( isDate( context, argc, argv, ref x ) == 0 )
      {
        string zBuf = "";//[100];
        computeHMS( x );
        sqlite3_snprintf( 100, ref zBuf, "%02d:%02d:%02d", x.h, x.m, (int)x.s );
        sqlite3_result_text( context, zBuf, -1, SQLITE_TRANSIENT );
      }
    }

    /*
    **    date( TIMESTRING, MOD, MOD, ...)
    **
    ** Return YYYY-MM-DD
    */
    static void dateFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      DateTime x = null;
      if ( isDate( context, argc, argv, ref x ) == 0 )
      {
        string zBuf = "";//[100];
        computeYMD( x );
        sqlite3_snprintf( 100, ref zBuf, "%04d-%02d-%02d", x.Y, x.M, x.D );
        sqlite3_result_text( context, zBuf, -1, SQLITE_TRANSIENT );
      }
    }

    /*
    **    strftime( FORMAT, TIMESTRING, MOD, MOD, ...)
    **
    ** Return a string described by FORMAT.  Conversions as follows:
    **
    **   %d  day of month
    **   %f  ** fractional seconds  SS.SSS
    **   %H  hour 00-24
    **   %j  day of year 000-366
    **   %J  ** Julian day number
    **   %m  month 01-12
    **   %M  minute 00-59
    **   %s  seconds since 1970-01-01
    **   %S  seconds 00-59
    **   %w  day of week 0-6  sunday==0
    **   %W  week of year 00-53
    **   %Y  year 0000-9999
    **   %%  %
    */
    static void strftimeFunc(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      {
        DateTime x = new DateTime();
        u64 n;
        int i, j;
        StringBuilder z;
        sqlite3 db;
        string zFmt = sqlite3_value_text( argv[0] );
        StringBuilder zBuf = new StringBuilder( 100 );
        sqlite3_value[] argv1 = new sqlite3_value[argc - 1];
        for ( i = 0 ; i < argc - 1 ; i++ ) { argv1[i] = new sqlite3_value(); argv[i + 1].CopyTo( argv1[i] ); }
        if ( String.IsNullOrEmpty( zFmt ) || isDate( context, argc - 1, argv1, ref x ) != 0 ) return;
        db = sqlite3_context_db_handle( context );
        for ( i = 0, n = 1 ; i < zFmt.Length ; i++, n++ )
        {
          if ( zFmt[i] == '%' )
          {
            switch ( (char)zFmt[i + 1] )
            {
              case 'd':
              case 'H':
              case 'm':
              case 'M':
              case 'S':
              case 'W':
                n++;
                break;
              /* fall thru */
              case 'w':
              case '%':
                break;
              case 'f':
                n += 8;
                break;
              case 'j':
                n += 3;
                break;
              case 'Y':
                n += 8;
                break;
              case 's':
              case 'J':
                n += 50;
                break;
              default:
                return;  /* ERROR.  return a NULL */
            }
            i++;
          }
        }
        testcase( n == (u64)( zBuf.Length - 1 ) );
        testcase( n == (u64)zBuf.Length );
        testcase( n == (u64)db.aLimit[SQLITE_LIMIT_LENGTH] + 1 );
        testcase( n == (u64)db.aLimit[SQLITE_LIMIT_LENGTH] );
        if ( n < (u64)zBuf.Capacity )
        {
          z = zBuf;
        }
        else if ( n > (u64)db.aLimit[SQLITE_LIMIT_LENGTH] )
        {
          sqlite3_result_error_toobig( context );
          return;
        }
        else
        {
          z = new StringBuilder( (int)n );// sqlite3DbMallocRaw( db, n );
          //if ( z == 0 )
          //{
          //  sqlite3_result_error_nomem( context );
          //  return;
          //}
        }
        computeJD( x );
        computeYMD_HMS( x );
        for ( i = j = 0 ; i < zFmt.Length ; i++ )
        {
          if ( zFmt[i] != '%' )
          {
            z.Append( (char)zFmt[i] );
          }
          else
          {
            i++;
            string zTemp = "";
            switch ( (char)zFmt[i] )
            {
              case 'd': sqlite3_snprintf( 3, ref zTemp, "%02d", x.D ); z.Append( zTemp ); j += 2; break;
              case 'f':
                {
                  double s = x.s;
                  if ( s > 59.999 ) s = 59.999;
                  sqlite3_snprintf( 7, ref zTemp, "%06.3f", s ); z.Append( zTemp );
                  j = sqlite3Strlen30( z );
                  break;
                }
              case 'H': sqlite3_snprintf( 3, ref zTemp, "%02d", x.h ); z.Append( zTemp ); j += 2; break;
              case 'W': /* Fall thru */
              case 'j':
                {
                  int nDay;             /* Number of days since 1st day of year */
                  DateTime y = new DateTime();
                  x.CopyTo( y );
                  y.validJD = 0;
                  y.M = 1;
                  y.D = 1;
                  computeJD( y );
                  nDay = (int)( ( x.iJD - y.iJD + 43200000 ) / 86400000 );
                  if ( zFmt[i] == 'W' )
                  {
                    int wd;   /* 0=Monday, 1=Tuesday, ... 6=Sunday */
                    wd = (int)( ( ( x.iJD + 43200000 ) / 86400000 ) % 7 );
                    sqlite3_snprintf( 3, ref zTemp, "%02d", ( nDay + 7 - wd ) / 7 ); z.Append( zTemp );
                    j += 2;
                  }
                  else
                  {
                    sqlite3_snprintf( 4, ref zTemp, "%03d", nDay + 1 ); z.Append( zTemp );
                    j += 3;
                  }
                  break;
                }
              case 'J':
                {
                  sqlite3_snprintf( 20, ref zTemp, "%.16g", x.iJD / 86400000.0 ); z.Append( zTemp );
                  j = sqlite3Strlen30( z );
                  break;
                }
              case 'm': sqlite3_snprintf( 3, ref zTemp, "%02d", x.M ); z.Append( zTemp ); j += 2; break;
              case 'M': sqlite3_snprintf( 3, ref zTemp, "%02d", x.m ); z.Append( zTemp ); j += 2; break;
              case 's':
                {
                  sqlite3_snprintf( 30, ref zTemp, "%lld",
                               (i64)( x.iJD / 1000 - 21086676 * (i64)10000 ) ); z.Append( zTemp );
                  j = sqlite3Strlen30( z );
                  break;
                }
              case 'S': sqlite3_snprintf( 3, ref zTemp, "%02d", (int)x.s ); z.Append( zTemp ); j += 2; break;
              case 'w':
                {
                  z.Append( ( ( ( x.iJD + 129600000 ) / 86400000 ) % 7 ) );
                  break;
                }
              case 'Y':
                {
                  sqlite3_snprintf( 5, ref zTemp, "%04d", x.Y ); z.Append( zTemp ); j = sqlite3Strlen30( z );
                  break;
                }
              default: z.Append( '%' ); break;
            }
          }
        }
        //z[j] = 0;
        sqlite3_result_text( context, z.ToString(), -1,
        z == zBuf ? SQLITE_TRANSIENT : SQLITE_DYNAMIC );
      }
    }

    /*
    ** current_time()
    **
    ** This function returns the same value as time('now').
    */
    static void ctimeFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] NotUsed2
    )
    {
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      timeFunc( context, 0, null );
    }

    /*
    ** current_date()
    **
    ** This function returns the same value as date('now').
    */
    static void cdateFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] NotUsed2
    )
    {
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      dateFunc( context, 0, null );
    }

    /*
    ** current_timestamp()
    **
    ** This function returns the same value as datetime('now').
    */
    static void ctimestampFunc(
    sqlite3_context context,
    int NotUsed,
    sqlite3_value[] NotUsed2
    )
    {
      UNUSED_PARAMETER2( NotUsed, NotUsed2 );
      datetimeFunc( context, 0, null );
    }
#endif // * !SQLITE_OMIT_DATETIME_FUNCS) */

#if  SQLITE_OMIT_DATETIME_FUNCS
/*
** If the library is compiled to omit the full-scale date and time
** handling (to get a smaller binary), the following minimal version
** of the functions current_time(), current_date() and current_timestamp()
** are included instead. This is to support column declarations that
** include "DEFAULT CURRENT_TIME" etc.
**
** This function uses the C-library functions time(), gmtime()
** and strftime(). The format string to pass to strftime() is supplied
** as the user-data for the function.
*/
//static void currentTimeFunc(
//  sqlite3_context *context,
//  int argc,
//  sqlite3_value[] argv
//){
time_t t;
char *zFormat = (char *)sqlite3_user_data(context);
sqlite3 db;
double rT;
char zBuf[20];
UNUSED_PARAMETER(argc);
UNUSED_PARAMETER(argv);
db = sqlite3_context_db_handle(context);
sqlite3OsCurrentTime(db.pVfs, rT);
#if !SQLITE_OMIT_FLOATING_POINT
t = 86400.0*(rT - 2440587.5) + 0.5;
#else
/* without floating point support, rT will have
** already lost fractional day precision.
*/
t = 86400 * (rT - 2440587) - 43200;
#endif
#if HAVE_GMTIME_R
//  {
//    struct tm sNow;
//    gmtime_r(&t, sNow);
//    strftime(zBuf, 20, zFormat, sNow);
//  }
#else
//  {
//    struct tm pTm;
//    sqlite3_mutex_enter(sqlite3MutexAlloc(SQLITE_MUTEX_STATIC_MASTER));
//    pTm = gmtime(&t);
//    strftime(zBuf, 20, zFormat, pTm);
//    sqlite3_mutex_leave(sqlite3MutexAlloc(SQLITE_MUTEX_STATIC_MASTER));
//  }
#endif

//  sqlite3_result_text(context, zBuf, -1, SQLITE_TRANSIENT);
//}
#endif


    /*
** This function registered all of the above C functions as SQL
** functions.  This should be the only routine in this file with
** external linkage.
*/
    static void sqlite3RegisterDateTimeFunctions()
    {
      FuncDef[] aDateTimeFuncs = new FuncDef[]  {
#if !SQLITE_OMIT_DATETIME_FUNCS
FUNCTION("julianday",        -1, 0, 0, (dxFunc)juliandayFunc ),
FUNCTION("date",             -1, 0, 0, (dxFunc)dateFunc      ),
FUNCTION("time",             -1, 0, 0, (dxFunc)timeFunc      ),
FUNCTION("datetime",         -1, 0, 0, (dxFunc)datetimeFunc  ),
FUNCTION("strftime",         -1, 0, 0, (dxFunc)strftimeFunc  ),
FUNCTION("current_time",      0, 0, 0, (dxFunc)ctimeFunc     ),
FUNCTION("current_timestamp", 0, 0, 0, (dxFunc)ctimestampFunc),
FUNCTION("current_date",      0, 0, 0, (dxFunc)cdateFunc     ),
#else
STR_FUNCTION("current_time",      0, "%H:%M:%S",          0, currentTimeFunc),
STR_FUNCTION("current_timestamp", 0, "%Y-%m-%d",          0, currentTimeFunc),
STR_FUNCTION("current_date",      0, "%Y-%m-%d %H:%M:%S", 0, currentTimeFunc),
#endif
};
      int i;
#if SQLITE_OMIT_WSD
FuncDefHash pHash = GLOBAL( FuncDefHash, sqlite3GlobalFunctions );
FuncDef[] aFunc = (FuncDef)GLOBAL( FuncDef, aDateTimeFuncs );
#else
      FuncDefHash pHash = sqlite3GlobalFunctions;
      FuncDef[] aFunc = aDateTimeFuncs;
#endif
      for ( i = 0 ; i < ArraySize( aDateTimeFuncs ) ; i++ )
      {
        sqlite3FuncDefInsert( pHash, aFunc[i] );
      }
    }
  }
}
