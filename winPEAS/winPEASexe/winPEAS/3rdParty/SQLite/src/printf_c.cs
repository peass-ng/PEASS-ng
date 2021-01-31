using System;
using System.Diagnostics;
using System.Text;

namespace winPEAS._3rdParty.SQLite.src
{
  using etByte = System.Boolean;
  using i64 = System.Int64;
  using LONGDOUBLE_TYPE = System.Double;
  using va_list = System.Object;

  public partial class CSSQLite
  {
    /*
    ** The "printf" code that follows dates from the 1980's.  It is in
    ** the public domain.  The original comments are included here for
    ** completeness.  They are very out-of-date but might be useful as
    ** an historical reference.  Most of the "enhancements" have been backed
    ** out so that the functionality is now the same as standard printf().
    **
    ** $Id: printf.c,v 1.104 2009/06/03 01:24:54 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    **
    **************************************************************************
    **
    ** The following modules is an enhanced replacement for the "printf" subroutines
    ** found in the standard C library.  The following enhancements are
    ** supported:
    **
    **      +  Additional functions.  The standard set of "printf" functions
    **         includes printf, fprintf, sprintf, vprintf, vfprintf, and
    **         vsprintf.  This module adds the following:
    **
    **           *  snprintf -- Works like sprintf, but has an extra argument
    **                          which is the size of the buffer written to.
    **
    **           *  mprintf --  Similar to sprintf.  Writes output to memory
    **                          obtained from malloc.
    **
    **           *  xprintf --  Calls a function to dispose of output.
    **
    **           *  nprintf --  No output, but returns the number of characters
    **                          that would have been output by printf.
    **
    **           *  A v- version (ex: vsnprintf) of every function is also
    **              supplied.
    **
    **      +  A few extensions to the formatting notation are supported:
    **
    **           *  The "=" flag (similar to "-") causes the output to be
    **              be centered in the appropriately sized field.
    **
    **           *  The %b field outputs an integer in binary notation.
    **
    **           *  The %c field now accepts a precision.  The character output
    **              is repeated by the number of times the precision specifies.
    **
    **           *  The %' field works like %c, but takes as its character the
    **              next character of the format string, instead of the next
    **              argument.  For example,  printf("%.78'-")  prints 78 minus
    **              signs, the same as  printf("%.78c",'-').
    **
    **      +  When compiled using GCC on a SPARC, this version of printf is
    **         faster than the library printf for SUN OS 4.1.
    **
    **      +  All functions are fully reentrant.
    **
    */
    //#include "sqliteInt.h"

    /*
    ** Conversion types fall into various categories as defined by the
    ** following enumeration.
    */
    //#define etRADIX       1 /* Integer types.  %d, %x, %o, and so forth */
    //#define etFLOAT       2 /* Floating point.  %f */
    //#define etEXP         3 /* Exponentional notation. %e and %E */
    //#define etGENERIC     4 /* Floating or exponential, depending on exponent. %g */
    //#define etSIZE        5 /* Return number of characters processed so far. %n */
    //#define etSTRING      6 /* Strings. %s */
    //#define etDYNSTRING   7 /* Dynamically allocated strings. %z */
    //#define etPERCENT     8 /* Percent symbol. %% */
    //#define etCHARX       9 /* Characters. %c */
    ///* The rest are extensions, not normally found in printf() */
    //#define etSQLESCAPE  10 /* Strings with '\'' doubled.  %q */
    //#define etSQLESCAPE2 11 /* Strings with '\'' doubled and enclosed in '',
    //                          NULL pointers replaced by SQL NULL.  %Q */
    //#define etTOKEN      12 /* a pointer to a Token structure */
    //#define etSRCLIST    13 /* a pointer to a SrcList */
    //#define etPOINTER    14 /* The %p conversion */
    //#define etSQLESCAPE3 15 /* %w -> Strings with '\"' doubled */
    //#define etORDINAL    16 /* %r -> 1st, 2nd, 3rd, 4th, etc.  English only */

    //#define etINVALID     0 /* Any unrecognized conversion type */

    const int etRADIX = 1; /* Integer types.  %d, %x, %o, and so forth */
    const int etFLOAT = 2; /* Floating point.  %f */
    const int etEXP = 3; /* Exponentional notation. %e and %E */
    const int etGENERIC = 4; /* Floating or exponential, depending on exponent. %g */
    const int etSIZE = 5; /* Return number of characters processed so far. %n */
    const int etSTRING = 6; /* Strings. %s */
    const int etDYNSTRING = 7; /* Dynamically allocated strings. %z */
    const int etPERCENT = 8; /* Percent symbol. %% */
    const int etCHARX = 9; /* Characters. %c */
    /* The rest are extensions, not normally found in printf() */
    const int etSQLESCAPE = 10; /* Strings with '\'' doubled.  %q */
    const int etSQLESCAPE2 = 11; /* Strings with '\'' doubled and enclosed in '',
NULL pointers replaced by SQL NULL.  %Q */
    const int etTOKEN = 12; /* a pointer to a Token structure */
    const int etSRCLIST = 13; /* a pointer to a SrcList */
    const int etPOINTER = 14; /* The %p conversion */
    const int etSQLESCAPE3 = 15; /* %w . Strings with '\"' doubled */
    const int etORDINAL = 16; /* %r . 1st, 2nd, 3rd, 4th, etc.  English only */
    const int etINVALID = 0; /* Any unrecognized conversion type */

    /*
    ** An "etByte" is an 8-bit unsigned value.
    */
    //typedef unsigned char etByte;

    /*
    ** Each builtin conversion character (ex: the 'd' in "%d") is described
    ** by an instance of the following structure
    */
    public class et_info
    {   /* Information about each format field */
      public char fmttype;            /* The format field code letter */
      public byte _base;             /* The _base for radix conversion */
      public byte flags;            /* One or more of FLAG_ constants below */
      public byte type;             /* Conversion paradigm */
      public byte charset;          /* Offset into aDigits[] of the digits string */
      public byte prefix;           /* Offset into aPrefix[] of the prefix string */
      /*
      * Constructor
      */
      public et_info( char fmttype,
      byte _base,
      byte flags,
      byte type,
      byte charset,
      byte prefix
      )
      {
        this.fmttype = fmttype;
        this._base = _base;
        this.flags = flags;
        this.type = type;
        this.charset = charset;
        this.prefix = prefix;
      }

    }

    /*
    ** Allowed values for et_info.flags
    */
    const byte FLAG_SIGNED = 1;    /* True if the value to convert is signed */
    const byte FLAG_INTERN = 2;    /* True if for internal use only */
    const byte FLAG_STRING = 4;    /* Allow infinity precision */


    /*
    ** The following table is searched linearly, so it is good to put the
    ** most frequently used conversion types first.
    */
    static string aDigits = "0123456789ABCDEF0123456789abcdef";
    static string aPrefix = "-x0\000X0";
    static et_info[] fmtinfo = new et_info[] {
new et_info(  'd', 10, 1, etRADIX,      0,  0 ),
new et_info(   's',  0, 4, etSTRING,     0,  0 ),
new et_info(   'g',  0, 1, etGENERIC,    30, 0 ),
new et_info(   'z',  0, 4, etDYNSTRING,  0,  0 ),
new et_info(   'q',  0, 4, etSQLESCAPE,  0,  0 ),
new et_info(   'Q',  0, 4, etSQLESCAPE2, 0,  0 ),
new et_info(   'w',  0, 4, etSQLESCAPE3, 0,  0 ),
new et_info(   'c',  0, 0, etCHARX,      0,  0 ),
new et_info(   'o',  8, 0, etRADIX,      0,  2 ),
new et_info(   'u', 10, 0, etRADIX,      0,  0 ),
new et_info(   'x', 16, 0, etRADIX,      16, 1 ),
new et_info(   'X', 16, 0, etRADIX,      0,  4 ),
#if !SQLITE_OMIT_FLOATING_POINT
new et_info(   'f',  0, 1, etFLOAT,      0,  0 ),
new et_info(   'e',  0, 1, etEXP,        30, 0 ),
new et_info(   'E',  0, 1, etEXP,        14, 0 ),
new et_info(   'G',  0, 1, etGENERIC,    14, 0 ),
#endif
new et_info(   'i', 10, 1, etRADIX,      0,  0 ),
new et_info(   'n',  0, 0, etSIZE,       0,  0 ),
new et_info(   '%',  0, 0, etPERCENT,    0,  0 ),
new et_info(   'p', 16, 0, etPOINTER,    0,  1 ),

/* All the rest have the FLAG_INTERN bit set and are thus for internal
** use only */
new et_info(   'T',  0, 2, etTOKEN,      0,  0 ),
new et_info(   'S',  0, 2, etSRCLIST,    0,  0 ),
new et_info(   'r', 10, 3, etORDINAL,    0,  0 ),
};
    /*
    ** If SQLITE_OMIT_FLOATING_POINT is defined, then none of the floating point
    ** conversions will work.
    */
#if  !SQLITE_OMIT_FLOATING_POINT
    /*
** "*val" is a double such that 0.1 <= *val < 10.0
** Return the ascii code for the leading digit of *val, then
** multiply "*val" by 10.0 to renormalize.
**
** Example:
**     input:     *val = 3.14159
**     output:    *val = 1.4159    function return = '3'
**
** The counter *cnt is incremented each time.  After counter exceeds
** 16 (the number of significant digits in a 64-bit float) '0' is
** always returned.
*/
    static char et_getdigit( ref LONGDOUBLE_TYPE val, ref int cnt )
    {
      int digit;
      LONGDOUBLE_TYPE d;
      if ( cnt++ >= 16 ) return '\0';
      digit = (int)val;
      d = digit;
      //digit += '0';
      val = ( val - d ) * 10.0;
      return (char)digit;
    }
#endif // * SQLITE_OMIT_FLOATING_POINT */

    /*
** Append N space characters to the given string buffer.
*/
    static void appendSpace( StrAccum pAccum, int N )
    {
      //static const char zSpaces[] = "                             ";
      //while( N>=zSpaces.Length-1 ){
      //  sqlite3StrAccumAppend(pAccum, zSpaces, zSpaces.Length-1);
      //  N -= zSpaces.Length-1;
      //}
      //if( N>0 ){
      //  sqlite3StrAccumAppend(pAccum, zSpaces, N);
      //}
      pAccum.zText.AppendFormat( "{0," + N + "}", "" );
    }

    /*
    ** On machines with a small stack size, you can redefine the
    ** SQLITE_PRINT_BUF_SIZE to be less than 350.
    */
#if !SQLITE_PRINT_BUF_SIZE
# if (SQLITE_SMALL_STACK)
const int SQLITE_PRINT_BUF_SIZE = 50;
# else
    const int SQLITE_PRINT_BUF_SIZE = 350;
#endif
#endif
    const int etBUFSIZE = SQLITE_PRINT_BUF_SIZE; /* Size of the output buffer */

    /*
    ** The root program.  All variations call this core.
    **
    ** INPUTS:
    **   func   This is a pointer to a function taking three arguments
    **            1. A pointer to anything.  Same as the "arg" parameter.
    **            2. A pointer to the list of characters to be output
    **               (Note, this list is NOT null terminated.)
    **            3. An integer number of characters to be output.
    **               (Note: This number might be zero.)
    **
    **   arg    This is the pointer to anything which will be passed as the
    **          first argument to "func".  Use it for whatever you like.
    **
    **   fmt    This is the format string, as in the usual print.
    **
    **   ap     This is a pointer to a list of arguments.  Same as in
    **          vfprint.
    **
    ** OUTPUTS:
    **          The return value is the total number of characters sent to
    **          the function "func".  Returns -1 on a error.
    **
    ** Note that the order in which automatic variables are declared below
    ** seems to make a big difference in determining how fast this beast
    ** will run.
    */
    static void sqlite3VXPrintf(
    StrAccum pAccum,             /* Accumulate results here */
    int useExtended,             /* Allow extended %-conversions */
    string fmt,                   /* Format string */
    va_list[] ap                   /* arguments */
    )
    {
      int c;                     /* Next character in the format string */
      int bufpt;                 /* Pointer to the conversion buffer */
      int precision;             /* Precision of the current field */
      int length;                /* Length of the field */
      int idx;                   /* A general purpose loop counter */
      int width;                 /* Width of the current field */
      etByte flag_leftjustify;   /* True if "-" flag is present */
      etByte flag_plussign;      /* True if "+" flag is present */
      etByte flag_blanksign;     /* True if " " flag is present */
      etByte flag_alternateform; /* True if "#" flag is present */
      etByte flag_altform2;      /* True if "!" flag is present */
      etByte flag_zeropad;       /* True if field width constant starts with zero */
      etByte flag_long;          /* True if "l" flag is present */
      etByte flag_longlong;      /* True if the "ll" flag is present */
      etByte done;               /* Loop termination flag */
      i64 longvalue;
      LONGDOUBLE_TYPE realvalue; /* Value for real types */
      et_info infop;      /* Pointer to the appropriate info structure */
      char[] buf = new char[etBUFSIZE];       /* Conversion buffer */
      char prefix;                /* Prefix character.  "+" or "-" or " " or '\0'. */
      byte xtype = 0;             /* Conversion paradigm */
      // Not used in C# -- string zExtra;              /* Extra memory used for etTCLESCAPE conversions */
#if !SQLITE_OMIT_FLOATING_POINT
      int exp, e2;                /* exponent of real numbers */
      double rounder;             /* Used for rounding floating point values */
      etByte flag_dp;             /* True if decimal point should be shown */
      etByte flag_rtz;            /* True if trailing zeros should be removed */
      etByte flag_exp;            /* True to force display of the exponent */
      int nsd;                    /* Number of significant digits returned */
#endif
      length = 0;
      bufpt = 0;
      int _fmt = 0; // Work around string pointer
      fmt += '\0';

      for ( ; _fmt <= fmt.Length && ( c = fmt[_fmt] ) != 0 ; ++_fmt )
      {
        if ( c != '%' )
        {
          int amt;
          bufpt = _fmt;
          amt = 1;
          while ( _fmt < fmt.Length && ( c = ( fmt[++_fmt] ) ) != '%' && c != 0 ) amt++;
          sqlite3StrAccumAppend( pAccum, fmt.Substring( bufpt, amt ), amt );
          if ( c == 0 ) break;
        }
        if ( _fmt < fmt.Length && ( c = ( fmt[++_fmt] ) ) == 0 )
        {
          sqlite3StrAccumAppend( pAccum, "%", 1 );
          break;
        }
        /* Find out what flags are present */
        flag_leftjustify = flag_plussign = flag_blanksign =
        flag_alternateform = flag_altform2 = flag_zeropad = false;
        done = false;
        do
        {
          switch ( c )
          {
            case '-': flag_leftjustify = true; break;
            case '+': flag_plussign = true; break;
            case ' ': flag_blanksign = true; break;
            case '#': flag_alternateform = true; break;
            case '!': flag_altform2 = true; break;
            case '0': flag_zeropad = true; break;
            default: done = true; break;
          }
        } while ( !done && _fmt < fmt.Length - 1 && ( c = ( fmt[++_fmt] ) ) != 0 );
        /* Get the field width */
        width = 0;
        if ( c == '*' )
        {
          width = (int)va_arg( ap, "int" );
          if ( width < 0 )
          {
            flag_leftjustify = true;
            width = -width;
          }
          c = fmt[++_fmt];
        }
        else
        {
          while ( c >= '0' && c <= '9' )
          {
            width = width * 10 + c - '0';
            c = fmt[++_fmt];
          }
        }
        if ( width > etBUFSIZE - 10 )
        {
          width = etBUFSIZE - 12;
        }
        /* Get the precision */
        if ( c == '.' )
        {
          precision = 0;
          c = fmt[++_fmt];
          if ( c == '*' )
          {
            precision = (int)va_arg( ap, "int" );
            if ( precision < 0 ) precision = -precision;
            c = fmt[++_fmt];
          }
          else
          {
            while ( c >= '0' && c <= '9' )
            {
              precision = precision * 10 + c - '0';
              c = fmt[++_fmt];
            }
          }
        }
        else
        {
          precision = -1;
        }
        /* Get the conversion type modifier */
        if ( c == 'l' )
        {
          flag_long = true;
          c = fmt[++_fmt];
          if ( c == 'l' )
          {
            flag_longlong = true;
            c = fmt[++_fmt];
          }
          else
          {
            flag_longlong = false;
          }
        }
        else
        {
          flag_long = flag_longlong = false;
        }
        /* Fetch the info entry for the field */
        infop = fmtinfo[0];
        xtype = etINVALID;
        for ( idx = 0 ; idx < ArraySize( fmtinfo ) ; idx++ )
        {
          if ( c == fmtinfo[idx].fmttype )
          {
            infop = fmtinfo[idx];
            if ( useExtended != 0 || ( infop.flags & FLAG_INTERN ) == 0 )
            {
              xtype = infop.type;
            }
            else
            {
              return;
            }
            break;
          }
        }
        //zExtra = null;

        /* Limit the precision to prevent overflowing buf[] during conversion */
        if ( precision > etBUFSIZE - 40 && ( infop.flags & FLAG_STRING ) == 0 )
        {
          precision = etBUFSIZE - 40;
        }

        /*
        ** At this point, variables are initialized as follows:
        **
        **   flag_alternateform          TRUE if a '#' is present.
        **   flag_altform2               TRUE if a '!' is present.
        **   flag_plussign               TRUE if a '+' is present.
        **   flag_leftjustify            TRUE if a '-' is present or if the
        **                               field width was negative.
        **   flag_zeropad                TRUE if the width began with 0.
        **   flag_long                   TRUE if the letter 'l' (ell) prefixed
        **                               the conversion character.
        **   flag_longlong               TRUE if the letter 'll' (ell ell) prefixed
        **                               the conversion character.
        **   flag_blanksign              TRUE if a ' ' is present.
        **   width                       The specified field width.  This is
        **                               always non-negative.  Zero is the default.
        **   precision                   The specified precision.  The default
        **                               is -1.
        **   xtype                       The class of the conversion.
        **   infop                       Pointer to the appropriate info struct.
        */
        switch ( xtype )
        {
          case etPOINTER:
            flag_longlong = true;// char*.Length == sizeof(i64);
            flag_long = false;// char*.Length == sizeof(long);
            /* Fall through into the next case */
            goto case etRADIX;
          case etORDINAL:
          case etRADIX:
            if ( ( infop.flags & FLAG_SIGNED ) != 0 )
            {
              i64 v;
              if ( flag_longlong )
              {
                v = (long)va_arg( ap, "i64" );
              }
              else if ( flag_long )
              {
                v = (long)va_arg( ap, "long int" );
              }
              else
              {
                v = (int)va_arg( ap, "int" );
              }
              if ( v < 0 )
              {
                longvalue = -v;
                prefix = '-';
              }
              else
              {
                longvalue = v;
                if ( flag_plussign ) prefix = '+';
                else if ( flag_blanksign ) prefix = ' ';
                else prefix = '\0';
              }
            }
            else
            {
              if ( flag_longlong )
              {
                longvalue = (i64)va_arg( ap, "longlong int" );
              }
              else if ( flag_long )
              {
                longvalue = (i64)va_arg( ap, "long int" );
              }
              else
              {
                longvalue = (i64)va_arg( ap, "long" );
              }
              prefix = '\0';
            }
            if ( longvalue == 0 ) flag_alternateform = false;
            if ( flag_zeropad && precision < width - ( ( prefix != '\0' ) ? 1 : 0 ) )
            {
              precision = width - ( ( prefix != '\0' ) ? 1 : 0 );
            }
            bufpt = buf.Length;//[etBUFSIZE-1];
            char[] _bufOrd = null;
            if ( xtype == etORDINAL )
            {
              char[] zOrd = "thstndrd".ToCharArray();
              int x = (int)( longvalue % 10 );
              if ( x >= 4 || ( longvalue / 10 ) % 10 == 1 )
              {
                x = 0;
              }
              _bufOrd = new char[2];
              _bufOrd[0] = zOrd[x * 2];
              _bufOrd[1] = zOrd[x * 2 + 1];
              //bufpt -= 2;
            }
            {

              char[] _buf;
              switch ( infop._base )
              {
                case 16:
                  _buf = longvalue.ToString( "x" ).ToCharArray();
                  break;
                case 8:
                  _buf = Convert.ToString( (long)longvalue, 8 ).ToCharArray();
                  break;
                default:
                  {
                    if ( flag_zeropad )
                      _buf = longvalue.ToString( new string( '0', width - ( ( prefix != '\0' ) ? 1 : 0 ) ) ).ToCharArray();
                    else
                      _buf = longvalue.ToString().ToCharArray();
                  }
                  break;
              }
              bufpt = buf.Length - _buf.Length - ( _bufOrd == null ? 0 : 2 );
              Array.Copy( _buf, 0, buf, bufpt, _buf.Length );
              if ( _bufOrd != null )
              {
                buf[buf.Length - 1] = _bufOrd[1];
                buf[buf.Length - 2] = _bufOrd[0];
              }
              //char* cset;      /* Use registers for speed */
              //int _base;
              //cset = aDigits[infop.charset];
              //_base = infop._base;
              //do
              //{ /* Convert to ascii */
              //   *(--bufpt) = cset[longvalue % (ulong)_base];
              //  longvalue = longvalue / (ulong)_base;
              //} while (longvalue > 0);
            }
            length = buf.Length - bufpt;//length = (int)(&buf[etBUFSIZE-1]-bufpt);
            for ( idx = precision - length ; idx > 0 ; idx-- )
            {
              buf[( --bufpt )] = '0';                             /* Zero pad */
            }
            if ( prefix != '\0' ) buf[--bufpt] = prefix;   /* Add sign */
            if ( flag_alternateform && infop.prefix != 0 )
            {      /* Add "0" or "0x" */
              int pre;
              char x;
              pre = infop.prefix;
              for ( ; ( x = aPrefix[pre] ) != 0 ; pre++ ) buf[--bufpt] = x;
            }
            length = buf.Length - bufpt;//length = (int)(&buf[etBUFSIZE-1]-bufpt);
            break;
          case etFLOAT:
          case etEXP:
          case etGENERIC:
            realvalue = (double)va_arg( ap, "double" );
#if !SQLITE_OMIT_FLOATING_POINT
            if ( precision < 0 ) precision = 6;         /* Set default precision */
            if ( precision > etBUFSIZE / 2 - 10 ) precision = etBUFSIZE / 2 - 10;
            if ( realvalue < 0.0 )
            {
              realvalue = -realvalue;
              prefix = '-';
            }
            else
            {
              if ( flag_plussign ) prefix = '+';
              else if ( flag_blanksign ) prefix = ' ';
              else prefix = '\0';
            }
            if ( xtype == etGENERIC && precision > 0 ) precision--;
#if FALSE
/* Rounding works like BSD when the constant 0.4999 is used.  Wierd! */
for(idx=precision, rounder=0.4999; idx>0; idx--, rounder*=0.1);
#else
            /* It makes more sense to use 0.5 */
            for ( idx = precision, rounder = 0.5 ; idx > 0 ; idx--, rounder *= 0.1 ) { }
#endif
            if ( xtype == etFLOAT ) realvalue += rounder;
            /* Normalize realvalue to within 10.0 > realvalue >= 1.0 */
            exp = 0;
            double d = 0;
            if ( Double.IsNaN( realvalue ) || !( Double.TryParse( Convert.ToString( realvalue ), out d ) ) )//if( sqlite3IsNaN((double)realvalue) )
            {
              buf = "NaN".ToCharArray();
              length = 3;
              break;
            }
            if ( realvalue > 0.0 )
            {
              while ( realvalue >= 1e32 && exp <= 350 ) { realvalue *= 1e-32; exp += 32; }
              while ( realvalue >= 1e8 && exp <= 350 ) { realvalue *= 1e-8; exp += 8; }
              while ( realvalue >= 10.0 && exp <= 350 ) { realvalue *= 0.1; exp++; }
              while ( realvalue < 1e-8 ) { realvalue *= 1e8; exp -= 8; }
              while ( realvalue < 1.0 ) { realvalue *= 10.0; exp--; }
              if ( exp > 350 )
              {
                if ( prefix == '-' )
                {
                  buf = "-Inf".ToCharArray();
                  bufpt = 4;
                }
                else if ( prefix == '+' )
                {
                  buf = "+Inf".ToCharArray();
                  bufpt = 4;
                }
                else
                {
                  buf = "Inf".ToCharArray();
                  bufpt = 3;
                }
                length = sqlite3Strlen30( bufpt );// sqlite3Strlen30(bufpt);
                bufpt = 0;
                break;
              }
            }
            bufpt = 0;
            /*
            ** If the field type is etGENERIC, then convert to either etEXP
            ** or etFLOAT, as appropriate.
            */
            flag_exp = xtype == etEXP;
            if ( xtype != etFLOAT )
            {
              realvalue += rounder;
              if ( realvalue >= 10.0 ) { realvalue *= 0.1; exp++; }
            }
            if ( xtype == etGENERIC )
            {
              flag_rtz = !flag_alternateform;
              if ( exp < -4 || exp > precision )
              {
                xtype = etEXP;
              }
              else
              {
                precision = precision - exp;
                xtype = etFLOAT;
              }
            }
            else
            {
              flag_rtz = false;
            }
            if ( xtype == etEXP )
            {
              e2 = 0;
            }
            else
            {
              e2 = exp;
            }
            nsd = 0;
            flag_dp = ( precision > 0 ? true : false ) | flag_alternateform | flag_altform2;
            /* The sign in front of the number */
            if ( prefix != '\0' )
            {
              buf[bufpt++] = prefix;
            }
            /* Digits prior to the decimal point */
            if ( e2 < 0 )
            {
              buf[bufpt++] = '0';
            }
            else
            {
              for ( ; e2 >= 0 ; e2-- )
              {
                buf[bufpt++] = (char)( et_getdigit( ref realvalue, ref nsd ) + '0' ); // *(bufpt++) = et_getdigit(ref realvalue, ref nsd);
              }

            }
            /* The decimal point */
            if ( flag_dp )
            {
              buf[bufpt++] = '.';
            }
            /* "0" digits after the decimal point but before the first
            ** significant digit of the number */
            for ( e2++ ; e2 < 0 ; precision--, e2++ )
            {
              Debug.Assert( precision > 0 );
              buf[bufpt++] = '0';
            }
            /* Significant digits after the decimal point */
            while ( ( precision-- ) > 0 )
            {
              buf[bufpt++] = (char)( et_getdigit( ref realvalue, ref nsd ) + '0' ); // *(bufpt++) = et_getdigit(&realvalue, nsd);
            }
            /* Remove trailing zeros and the "." if no digits follow the "." */
            if ( flag_rtz && flag_dp )
            {
              while ( buf[bufpt - 1] == '0' ) buf[--bufpt] = '\0';
              Debug.Assert( bufpt > 0 );
              if ( buf[bufpt - 1] == '.' )
              {
                if ( flag_altform2 )
                {
                  buf[( bufpt++ )] = '0';
                }
                else
                {
                  buf[( --bufpt )] = '0';
                }
              }
            }
            /* Add the "eNNN" suffix */
            if ( flag_exp || xtype == etEXP )
            {
              buf[bufpt++] = aDigits[infop.charset];
              if ( exp < 0 )
              {
                buf[bufpt++] = '-'; exp = -exp;
              }
              else
              {
                buf[bufpt++] = '+';
              }
              if ( exp >= 100 )
              {
                buf[bufpt++] = (char)( exp / 100 + '0' );                /* 100's digit */
                exp %= 100;
              }
              buf[bufpt++] = (char)( exp / 10 + '0' );                     /* 10's digit */
              buf[bufpt++] = (char)( exp % 10 + '0' );                     /* 1's digit */
            }
            //bufpt = 0;

            /* The converted number is in buf[] and zero terminated. Output it.
            ** Note that the number is in the usual order, not reversed as with
            ** integer conversions. */
            length = bufpt;//length = (int)(bufpt-buf);
            bufpt = 0;

            /* Special case:  Add leading zeros if the flag_zeropad flag is
            ** set and we are not left justified */
            if ( flag_zeropad && !flag_leftjustify && length < width )
            {
              int i;
              int nPad = width - length;
              for ( i = width ; i >= nPad ; i-- )
              {
                buf[bufpt + i] = buf[bufpt + i - nPad];
              }
              i = ( prefix != '\0' ? 1 : 0 );
              while ( nPad-- != 0 ) buf[( bufpt++ ) + i] = '0';
              length = width;
              bufpt = 0;
            }
#endif
            break;
          case etSIZE:
            ap[0] = pAccum.nChar; // *(va_arg(ap,int*)) = pAccum.nChar;
            length = width = 0;
            break;
          case etPERCENT:
            buf[0] = '%';
            bufpt = 0;
            length = 1;
            break;
          case etCHARX:
            c = (char)va_arg( ap, "char" );
            buf[0] = (char)c;
            if ( precision >= 0 )
            {
              for ( idx = 1 ; idx < precision ; idx++ ) buf[idx] = (char)c;
              length = precision;
            }
            else
            {
              length = 1;
            }
            bufpt = 0;
            break;
          case etSTRING:
          case etDYNSTRING:
            bufpt = 0;//
            string bufStr = (string)va_arg( ap, "string" );
            if ( bufStr.Length > buf.Length ) buf = new char[bufStr.Length];
            bufStr.ToCharArray().CopyTo( buf, 0 );
            bufpt = bufStr.Length;
            if ( bufpt == 0 )
            {
              buf[0] = '\0';
            }
            else if ( xtype == etDYNSTRING )
            {
              //              zExtra = bufpt;
            }
            if ( precision >= 0 )
            {
              for ( length = 0 ; length < precision && length < bufStr.Length && buf[length] != 0 ; length++ ) { }
              //length += precision;
            }
            else
            {
              length = sqlite3Strlen30( bufpt );
            }
            bufpt = 0;
            break;
          case etSQLESCAPE:
          case etSQLESCAPE2:
          case etSQLESCAPE3:
            {
              int i; int j; int n;
              bool isnull;
              bool needQuote;
              char ch;
              char q = ( ( xtype == etSQLESCAPE3 ) ? '"' : '\'' );   /* Quote character */
              string escarg = (string)va_arg( ap, "char*" ) + '\0';
              isnull = ( escarg == "" || escarg == "NULL\0" );
              if ( isnull ) escarg = ( xtype == etSQLESCAPE2 ) ? "NULL\0" : "(NULL)\0";
              for ( i = n = 0 ; ( ch = escarg[i] ) != 0 ; i++ )
              {
                if ( ch == q ) n++;
              }
              needQuote = !isnull && ( xtype == etSQLESCAPE2 );
              n += i + 1 + ( needQuote ? 2 : 0 );
              if ( n > etBUFSIZE )
              {
                buf = new char[n];//bufpt = zExtra = sqlite3Malloc(n);
                //if ( bufpt == 0 )
                //{
                //  pAccum->mallocFailed = 1;
                //  return;
                //}
                bufpt = 0; //Start of Buffer
              }
              else
              {
                //bufpt = buf;
                bufpt = 0; //Start of Buffer
              }
              j = 0;
              if ( needQuote ) buf[bufpt + j++] = q;
              for ( i = 0 ; ( ch = escarg[i] ) != 0 ; i++ )
              {
                buf[bufpt + j++] = ch;
                if ( ch == q ) buf[bufpt + j++] = ch;
              }
              if ( needQuote ) buf[bufpt + j++] = q;
              buf[bufpt + j] = '\0';
              length = j;
              /* The precision is ignored on %q and %Q */
              /* if( precision>=0 && precision<length ) length = precision; */
              break;
            }
          case etTOKEN:
            {
              Token pToken = (Token)va_arg( ap, "Token" );
              if ( pToken != null )
              {
                sqlite3StrAccumAppend( pAccum, pToken.z.ToString(), (int)pToken.n );
              }
              length = width = 0;
              break;
            }
          case etSRCLIST:
            {
              SrcList pSrc = (SrcList)va_arg( ap, "SrcList" );
              int k = (int)va_arg( ap, "int" );
              SrcList_item pItem = pSrc.a[k];
              Debug.Assert( k >= 0 && k < pSrc.nSrc );
              if ( pItem.zDatabase != null )
              {
                sqlite3StrAccumAppend( pAccum, pItem.zDatabase, -1 );
                sqlite3StrAccumAppend( pAccum, ".", 1 );
              }
              sqlite3StrAccumAppend( pAccum, pItem.zName, -1 );
              length = width = 0;
              break;
            }
          default:
            {
              Debug.Assert( xtype == etINVALID );
              return;
            }
        }/* End switch over the format type */
        /*
        ** The text of the conversion is pointed to by "bufpt" and is
        ** "length" characters long.  The field width is "width".  Do
        ** the output.
        */
        if ( !flag_leftjustify )
        {
          int nspace;
          nspace = width - length;// -2;
          if ( nspace > 0 )
          {
            appendSpace( pAccum, nspace );
          }
        }
        if ( length > 0 )
        {
          sqlite3StrAccumAppend( pAccum, new string( buf, bufpt, length ), length );
        }
        if ( flag_leftjustify )
        {
          int nspace;
          nspace = width - length;
          if ( nspace > 0 )
          {
            appendSpace( pAccum, nspace );
          }
        }
        //if( zExtra ){
        //  //sqlite3DbFree(db,ref  zExtra);
        //}
      }/* End for loop over the format string */
    } /* End of function */

    /*
    ** Append N bytes of text from z to the StrAccum object.
    */

    static void sqlite3StrAccumAppend( StrAccum p, string z, int N )
    {
      Debug.Assert( z != null || N == 0 );
      if ( p.tooBig != 0 )//|| p.mallocFailed != 0 )
      {
        testcase( p.tooBig );
        //testcase( p.mallocFailed );
        return;
      }
      if ( N < 0 )
      {
        N = sqlite3Strlen30( z );
      }
      if ( N == 0 || NEVER( z == null ) )
      {
        return;
      }
      //if ( p.nChar + N >= p.nAlloc )
      //{
      //  char* zNew;
      //  if ( !p.useMalloc )
      //  {
      //    p.tooBig = 1;
      //    N = p.nAlloc - p.nChar - 1;
      //    if ( N <= 0 )
      //    {
      //      return;
      //    }
      //  }
      //  else
      //  {
      //    i64 szNew = p.nChar;
      //    szNew += N + 1;
      //    if ( szNew > p.mxAlloc )
      //    {
      //      sqlite3StrAccumReset( p );
      //      p.tooBig = 1;
      //      return;
      //    }
      //    else
      //    {
      //      p.nAlloc = (int)szNew;
      //    }
      //    zNew = sqlite3DbMalloc( p.nAlloc );
      //    if ( zNew )
      //    {
      //      memcpy( zNew, p.zText, p.nChar );
      //      sqlite3StrAccumReset( p );
      //      p.zText = zNew;
      //    }
      //    else
      //    {
      //      p.mallocFailed = 1;
      //      sqlite3StrAccumReset( p );
      //      return;
      //    }
      //  }
      //}
      //memcpy( &p.zText[p.nChar], z, N );
      p.zText.Append( z.Substring( 0, N <= z.Length ? N : z.Length ) );
      p.nChar += N;
    }

    /*
    ** Finish off a string by making sure it is zero-terminated.
    ** Return a pointer to the resulting string.  Return a NULL
    ** pointer if any kind of error was encountered.
    */
    static string sqlite3StrAccumFinish( StrAccum p )
    {
      //if (p.zText.Length > 0)
      //{
      //  p.zText[p.nChar] = 0;
      //  if (p.useMalloc && p.zText == p.zBase)
      //  {
      //    p.zText = sqlite3DbMalloc(p.nChar + 1);
      //    if (p.zText)
      //    {
      //      memcpy(p.zText, p.zBase, p.nChar + 1);
      //    }
      //    else
      //    {
      //      p.mallocFailed = 1;
      //    }
      //  }
      //}
      return p.zText.ToString();
    }

    /*
    ** Reset an StrAccum string.  Reclaim all malloced memory.
    */
    static void sqlite3StrAccumReset( StrAccum p )
    {
      if ( p.zText.ToString() != p.zBase.ToString() )
      {
        //sqlite3DbFree( p.db, ref p.zText );
      }
      p.zText = new StringBuilder();
    }

    /*
    ** Initialize a string accumulator
    */
    static void sqlite3StrAccumInit( StrAccum p, StringBuilder zBase, int n, int mx )
    {
      p.zText = p.zBase = zBase;
      p.db = null;
      p.nChar = 0;
      p.nAlloc = n;
      p.mxAlloc = mx;
      p.useMalloc = 1;
      p.tooBig = 0;
      //p.mallocFailed = 0;
    }

    /*
    ** Print into memory obtained from sqliteMalloc().  Use the internal
    ** %-conversion extensions.
    */
    static string sqlite3VMPrintf( sqlite3 db, string zFormat, params va_list[] ap )
    {
      if ( zFormat == null ) return null;
      if ( ap.Length == 0 ) return zFormat;
      string z;
      StringBuilder zBase = new StringBuilder( SQLITE_PRINT_BUF_SIZE );
      StrAccum acc = new StrAccum();
      Debug.Assert( db != null );
      sqlite3StrAccumInit( acc, zBase, zBase.Capacity, //zBase).Length;
      db.aLimit[SQLITE_LIMIT_LENGTH] );
      acc.db = db;
      sqlite3VXPrintf( acc, 1, zFormat, ap );
      z = sqlite3StrAccumFinish( acc );
//      if ( acc.mallocFailed != 0 )
//      {
//////        db.mallocFailed = 1;
//      }
      return z;
    }

    /*
    ** Print into memory obtained from sqliteMalloc().  Use the internal
    ** %-conversion extensions.
    */
    static string sqlite3MPrintf( sqlite3 db, string zFormat, params va_list[] ap )
    {
      //va_list ap;
      string z;
      va_start( ap, zFormat );
      z = sqlite3VMPrintf( db, zFormat, ap );
      va_end( ap );
      return z;
    }

    /*
    ** Like sqlite3MPrintf(), but call //sqlite3DbFree() on zStr after formatting
    ** the string and before returnning.  This routine is intended to be used
    ** to modify an existing string.  For example:
    **
    **       x = sqlite3MPrintf(db, x, "prefix %s suffix", x);
    **
    */
    static string sqlite3MAppendf( sqlite3 db, string zStr, string zFormat, params  va_list[] ap )
    {
      //va_list ap;
      string z;
      va_start( ap, zFormat );
      z = sqlite3VMPrintf( db, zFormat, ap );
      va_end( ap );
      //sqlite3DbFree( db, zStr );
      return z;
    }

    /*
    ** Print into memory obtained from sqlite3Malloc().  Omit the internal
    ** %-conversion extensions.
    */
    static string sqlite3_vmprintf( string zFormat, params  va_list[] ap )
    {
      string z;
      StringBuilder zBase = new StringBuilder( SQLITE_PRINT_BUF_SIZE );
      StrAccum acc = new StrAccum();
#if !SQLITE_OMIT_AUTOINIT
      if ( sqlite3_initialize() != 0 ) return "";
#endif
      sqlite3StrAccumInit( acc, zBase, zBase.Length, SQLITE_PRINT_BUF_SIZE );//zBase).Length;
      sqlite3VXPrintf( acc, 0, zFormat, ap );
      z = sqlite3StrAccumFinish( acc );
      return z;
    }

    /*
    ** Print into memory obtained from sqlite3Malloc()().  Omit the internal
    ** %-conversion extensions.
    */
    public static string sqlite3_mprintf( string zFormat, params va_list[] ap )
    { //, ...){
      //va_list ap;
      string z;
#if  !SQLITE_OMIT_AUTOINIT
      if ( sqlite3_initialize() != 0 ) return "";
#endif
      va_start( ap, zFormat );
      z = sqlite3_vmprintf( zFormat, ap );
      va_end( ap );
      return z;
    }

    /*
    ** sqlite3_snprintf() works like snprintf() except that it ignores the
    ** current locale settings.  This is important for SQLite because we
    ** are not able to use a "," as the decimal point in place of "." as
    ** specified by some locales.
    */
    public static string sqlite3_snprintf( int n, ref StringBuilder zBuf, string zFormat, params va_list[] ap )
    {
      StringBuilder zBase = new StringBuilder( SQLITE_PRINT_BUF_SIZE );
      //va_list ap;
      StrAccum acc = new StrAccum();

      if ( n <= 0 )
      {
        return zBuf.ToString();
      }
      sqlite3StrAccumInit( acc, zBase, n, 0 );
      acc.useMalloc = 0;
      va_start( ap, zFormat );
      sqlite3VXPrintf( acc, 0, zFormat, ap );
      va_end( ap );
      zBuf.Length = 0;
      zBuf.Append( sqlite3StrAccumFinish( acc ) );
      if ( n - 1 < zBuf.Length ) zBuf.Length = n - 1;
      return zBuf.ToString();
    }

    public static string sqlite3_snprintf( int n, ref string zBuf, string zFormat, params va_list[] ap )
    {
      string z;
      StringBuilder zBase = new StringBuilder( SQLITE_PRINT_BUF_SIZE );
      //va_list ap;
      StrAccum acc = new StrAccum();

      if ( n <= 0 )
      {
        return zBuf;
      }
      sqlite3StrAccumInit( acc, zBase, n, 0 );
      acc.useMalloc = 0;
      va_start( ap, zFormat );
      sqlite3VXPrintf( acc, 0, zFormat, ap );
      va_end( ap );
      z = sqlite3StrAccumFinish( acc );
      return ( zBuf = z );
    }

#if SQLITE_DEBUG || DEBUG || TRACE
    /*
** A version of printf() that understands %lld.  Used for debugging.
** The printf() built into some versions of windows does not understand %lld
** and segfaults if you give it a long long int.
*/
    static void sqlite3DebugPrintf( string zFormat, params va_list[] ap )
    {
      //va_list ap;
      StrAccum acc = new StrAccum();
      StringBuilder zBuf = new StringBuilder( SQLITE_PRINT_BUF_SIZE );
      sqlite3StrAccumInit( acc, zBuf, zBuf.Capacity, 0 );
      acc.useMalloc = 0;
      va_start( ap, zFormat );
      sqlite3VXPrintf( acc, 0, zFormat, ap );
      va_end( ap );
      sqlite3StrAccumFinish( acc );
      Console.Write( zBuf.ToString() );
      //fflush(stdout);
    }
#endif
  }
}
