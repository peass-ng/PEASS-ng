using System;
using System.Diagnostics;
using System.Text;

namespace CS_SQLite3
{
  public partial class CSSQLite
  {
    /*
    ** 2004 April 13
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** This file contains routines used to translate between UTF-8,
    ** UTF-16, UTF-16BE, and UTF-16LE.
    **
    ** $Id: utf.c,v 1.73 2009/04/01 18:40:32 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    **
    ** Notes on UTF-8:
    **
    **   Byte-0    Byte-1    Byte-2    Byte-3    Value
    **  0xxxxxxx                                 00000000 00000000 0xxxxxxx
    **  110yyyyy  10xxxxxx                       00000000 00000yyy yyxxxxxx
    **  1110zzzz  10yyyyyy  10xxxxxx             00000000 zzzzyyyy yyxxxxxx
    **  11110uuu  10uuzzzz  10yyyyyy  10xxxxxx   000uuuuu zzzzyyyy yyxxxxxx
    **
    **
    ** Notes on UTF-16:  (with wwww+1==uuuuu)
    **
    **      Word-0               Word-1          Value
    **  110110ww wwzzzzyy   110111yy yyxxxxxx    000uuuuu zzzzyyyy yyxxxxxx
    **  zzzzyyyy yyxxxxxx                        00000000 zzzzyyyy yyxxxxxx
    **
    **
    ** BOM or Byte Order Mark:
    **     0xff 0xfe   little-endian utf-16 follows
    **     0xfe 0xff   big-endian utf-16 follows
    **
    */
    //#include "sqliteInt.h"
    //#include <assert.h>
    //#include "vdbeInt.h"

#if !SQLITE_AMALGAMATION
    /*
** The following constant value is used by the SQLITE_BIGENDIAN and
** SQLITE_LITTLEENDIAN macros.
*/
    //const int sqlite3one = 1;
#endif //* SQLITE_AMALGAMATION */

    /*
** This lookup table is used to help decode the first byte of
** a multi-byte UTF8 character.
*/
    static byte[] sqlite3Utf8Trans1 = new byte[]  {
0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x00, 0x00,
};


    //#define WRITE_UTF8(zOut, c) {                          \
    //  if( c<0x00080 ){                                     \
    //    *zOut++ = (u8)(c&0xFF);                            \
    //  }                                                    \
    //  else if( c<0x00800 ){                                \
    //    *zOut++ = 0xC0 + (u8)((c>>6)&0x1F);                \
    //    *zOut++ = 0x80 + (u8)(c & 0x3F);                   \
    //  }                                                    \
    //  else if( c<0x10000 ){                                \
    //    *zOut++ = 0xE0 + (u8)((c>>12)&0x0F);               \
    //    *zOut++ = 0x80 + (u8)((c>>6) & 0x3F);              \
    //    *zOut++ = 0x80 + (u8)(c & 0x3F);                   \
    //  }else{                                               \
    //    *zOut++ = 0xF0 + (u8)((c>>18) & 0x07);             \
    //    *zOut++ = 0x80 + (u8)((c>>12) & 0x3F);             \
    //    *zOut++ = 0x80 + (u8)((c>>6) & 0x3F);              \
    //    *zOut++ = 0x80 + (u8)(c & 0x3F);                   \
    //  }                                                    \
    //}

    //#define WRITE_UTF16LE(zOut, c) {                                    \
    //  if( c<=0xFFFF ){                                                  \
    //    *zOut++ = (u8)(c&0x00FF);                                       \
    //    *zOut++ = (u8)((c>>8)&0x00FF);                                  \
    //  }else{                                                            \
    //    *zOut++ = (u8)(((c>>10)&0x003F) + (((c-0x10000)>>10)&0x00C0));  \
    //    *zOut++ = (u8)(0x00D8 + (((c-0x10000)>>18)&0x03));              \
    //    *zOut++ = (u8)(c&0x00FF);                                       \
    //    *zOut++ = (u8)(0x00DC + ((c>>8)&0x03));                         \
    //  }                                                                 \
    //}

    //#define WRITE_UTF16BE(zOut, c) {                                    \
    //  if( c<=0xFFFF ){                                                  \
    //    *zOut++ = (u8)((c>>8)&0x00FF);                                  \
    //    *zOut++ = (u8)(c&0x00FF);                                       \
    //  }else{                                                            \
    //    *zOut++ = (u8)(0x00D8 + (((c-0x10000)>>18)&0x03));              \
    //    *zOut++ = (u8)(((c>>10)&0x003F) + (((c-0x10000)>>10)&0x00C0));  \
    //    *zOut++ = (u8)(0x00DC + ((c>>8)&0x03));                         \
    //    *zOut++ = (u8)(c&0x00FF);                                       \
    //  }                                                                 \
    //}

    //#define READ_UTF16LE(zIn, c){                                         \
    //  c = (*zIn++);                                                       \
    //  c += ((*zIn++)<<8);                                                 \
    //  if( c>=0xD800 && c<0xE000 ){                                       \
    //    int c2 = (*zIn++);                                                \
    //    c2 += ((*zIn++)<<8);                                              \
    //    c = (c2&0x03FF) + ((c&0x003F)<<10) + (((c&0x03C0)+0x0040)<<10);   \
    //  }                                                                   \
    //}

    //#define READ_UTF16BE(zIn, c){                                         \
    //  c = ((*zIn++)<<8);                                                  \
    //  c += (*zIn++);                                                      \
    //  if( c>=0xD800 && c<0xE000 ){                                       \
    //    int c2 = ((*zIn++)<<8);                                           \
    //    c2 += (*zIn++);                                                   \
    //    c = (c2&0x03FF) + ((c&0x003F)<<10) + (((c&0x03C0)+0x0040)<<10);   \
    //  }                                                                   \
    //}

    /*
    ** Translate a single UTF-8 character.  Return the unicode value.
    **
    ** During translation, assume that the byte that zTerm points
    ** is a 0x00.
    **
    ** Write a pointer to the next unread byte back into pzNext.
    **
    ** Notes On Invalid UTF-8:
    **
    **  *  This routine never allows a 7-bit character (0x00 through 0x7f) to
    **     be encoded as a multi-byte character.  Any multi-byte character that
    **     attempts to encode a value between 0x00 and 0x7f is rendered as 0xfffd.
    **
    **  *  This routine never allows a UTF16 surrogate value to be encoded.
    **     If a multi-byte character attempts to encode a value between
    **     0xd800 and 0xe000 then it is rendered as 0xfffd.
    **
    **  *  Bytes in the range of 0x80 through 0xbf which occur as the first
    **     byte of a character are interpreted as single-byte characters
    **     and rendered as themselves even though they are technically
    **     invalid characters.
    **
    **  *  This routine accepts an infinite number of different UTF8 encodings
    **     for unicode values 0x80 and greater.  It do not change over-length
    **     encodings to 0xfffd as some systems recommend.
    */
    //#define READ_UTF8(zIn, zTerm, c)                           \
    //  c = *(zIn++);                                            \
    //  if( c>=0xc0 ){                                           \
    //    c = sqlite3Utf8Trans1[c-0xc0];                          \
    //    while( zIn!=zTerm && (*zIn & 0xc0)==0x80 ){            \
    //      c = (c<<6) + (0x3f & *(zIn++));                      \
    //    }                                                      \
    //    if( c<0x80                                             \
    //        || (c&0xFFFFF800)==0xD800                          \
    //        || (c&0xFFFFFFFE)==0xFFFE ){  c = 0xFFFD; }        \
    //  }
    static int sqlite3Utf8Read(
    string zIn,          /* First byte of UTF-8 character */
    ref string pzNext   /* Write first byte past UTF-8 char here */
    )
    {
      //int c;
      /* Same as READ_UTF8() above but without the zTerm parameter.
      ** For this routine, we assume the UTF8 string is always zero-terminated.
      */
      if ( zIn == null || zIn.Length == 0 ) return 0;
      //c = *( zIn++ );
      //if ( c >= 0xc0 )
      //{
      //  c = sqlite3Utf8Trans1[c - 0xc0];
      //  while ( ( *zIn & 0xc0 ) == 0x80 )
      //  {
      //    c = ( c << 6 ) + ( 0x3f & *( zIn++ ) );
      //  }
      //  if ( c < 0x80
      //      || ( c & 0xFFFFF800 ) == 0xD800
      //      || ( c & 0xFFFFFFFE ) == 0xFFFE ) { c = 0xFFFD; }
      //}
      //*pzNext = zIn;
      int zIndex = 0;
      int c = zIn[zIndex++];
      if ( c >= 0xc0 )
      {
        if ( c > 0xff ) c = 0;
        else
        {
          c = sqlite3Utf8Trans1[c - 0xc0];
          while ( zIndex != zIn.Length && ( zIn[zIndex] & 0xc0 ) == 0x80 )
          {
            c = ( c << 6 ) + ( 0x3f & zIn[zIndex++] );
          }
          if ( c < 0x80
          || ( c & 0xFFFFF800 ) == 0xD800
          || ( c & 0xFFFFFFFE ) == 0xFFFE ) { c = 0xFFFD; }
        }
      } pzNext = zIn.Substring( zIndex );
      return c;
    }



    /*
    ** If the TRANSLATE_TRACE macro is defined, the value of each Mem is
    ** printed on stderr on the way into and out of sqlite3VdbeMemTranslate().
    */
    /* #define TRANSLATE_TRACE 1 */

#if ! SQLITE_OMIT_UTF16

/*
** This routine transforms the internal text encoding used by pMem to
** desiredEnc. It is an error if the string is already of the desired
** encoding, or if pMem does not contain a string value.
*/
static int sqlite3VdbeMemTranslate(Mem pMem, int desiredEnc){
int len;                    /* Maximum length of output string in bytes */
Debugger.Break (); // TODO -
//unsigned char *zOut;                  /* Output buffer */
//unsigned char *zIn;                   /* Input iterator */
//unsigned char *zTerm;                 /* End of input */
//unsigned char *z;                     /* Output iterator */
//unsigned int c;

Debug.Assert( pMem.db==null || sqlite3_mutex_held(pMem.db.mutex) );
Debug.Assert( (pMem.flags&MEM_Str )!=0);
Debug.Assert( pMem.enc!=desiredEnc );
Debug.Assert( pMem.enc!=0 );
Debug.Assert( pMem.n>=0 );

#if TRANSLATE_TRACE && SQLITE_DEBUG
{
char zBuf[100];
sqlite3VdbeMemPrettyPrint(pMem, zBuf);
fprintf(stderr, "INPUT:  %s\n", zBuf);
}
#endif

/* If the translation is between UTF-16 little and big endian, then
** all that is required is to swap the byte order. This case is handled
** differently from the others.
*/
Debugger.Break (); // TODO -
//if( pMem->enc!=SQLITE_UTF8 && desiredEnc!=SQLITE_UTF8 ){
//  u8 temp;
//  int rc;
//  rc = sqlite3VdbeMemMakeWriteable(pMem);
//  if( rc!=SQLITE_OK ){
//    Debug.Assert( rc==SQLITE_NOMEM );
//    return SQLITE_NOMEM;
//  }
//  zIn = (u8*)pMem.z;
//  zTerm = &zIn[pMem->n&~1];
//  while( zIn<zTerm ){
//    temp = *zIn;
//    *zIn = *(zIn+1);
//    zIn++;
//    *zIn++ = temp;
//  }
//  pMem->enc = desiredEnc;
//  goto translate_out;
//}

/* Set len to the maximum number of bytes required in the output buffer. */
if( desiredEnc==SQLITE_UTF8 ){
/* When converting from UTF-16, the maximum growth results from
** translating a 2-byte character to a 4-byte UTF-8 character.
** A single byte is required for the output string
** nul-terminator.
*/
pMem->n &= ~1;
len = pMem.n * 2 + 1;
}else{
/* When converting from UTF-8 to UTF-16 the maximum growth is caused
** when a 1-byte UTF-8 character is translated into a 2-byte UTF-16
** character. Two bytes are required in the output buffer for the
** nul-terminator.
*/
len = pMem.n * 2 + 2;
}

/* Set zIn to point at the start of the input buffer and zTerm to point 1
** byte past the end.
**
** Variable zOut is set to point at the output buffer, space obtained
** from sqlite3Malloc().
*/
Debugger.Break (); // TODO -
//zIn = (u8*)pMem.z;
//zTerm = &zIn[pMem->n];
//zOut = sqlite3DbMallocRaw(pMem->db, len);
//if( !zOut ){
//  return SQLITE_NOMEM;
//}
//z = zOut;

//if( pMem->enc==SQLITE_UTF8 ){
//  if( desiredEnc==SQLITE_UTF16LE ){
//    /* UTF-8 -> UTF-16 Little-endian */
//    while( zIn<zTerm ){
///* c = sqlite3Utf8Read(zIn, zTerm, (const u8**)&zIn); */
//READ_UTF8(zIn, zTerm, c);
//      WRITE_UTF16LE(z, c);
//    }
//  }else{
//    Debug.Assert( desiredEnc==SQLITE_UTF16BE );
//    /* UTF-8 -> UTF-16 Big-endian */
//    while( zIn<zTerm ){
///* c = sqlite3Utf8Read(zIn, zTerm, (const u8**)&zIn); */
//READ_UTF8(zIn, zTerm, c);
//      WRITE_UTF16BE(z, c);
//    }
//  }
//  pMem->n = (int)(z - zOut);
//  *z++ = 0;
//}else{
//  Debug.Assert( desiredEnc==SQLITE_UTF8 );
//  if( pMem->enc==SQLITE_UTF16LE ){
//    /* UTF-16 Little-endian -> UTF-8 */
//    while( zIn<zTerm ){
//      READ_UTF16LE(zIn, c);
//      WRITE_UTF8(z, c);
//    }
//  }else{
//    /* UTF-16 Big-endian -> UTF-8 */
//    while( zIn<zTerm ){
//      READ_UTF16BE(zIn, c);
//      WRITE_UTF8(z, c);
//    }
//  }
//  pMem->n = (int)(z - zOut);
//}
//*z = 0;
//Debug.Assert( (pMem->n+(desiredEnc==SQLITE_UTF8?1:2))<=len );

//sqlite3VdbeMemRelease(pMem);
//pMem->flags &= ~(MEM_Static|MEM_Dyn|MEM_Ephem);
//pMem->enc = desiredEnc;
//pMem->flags |= (MEM_Term|MEM_Dyn);
//pMem.z = (char*)zOut;
//pMem.zMalloc = pMem.z;

translate_out:
#if TRANSLATE_TRACE && SQLITE_DEBUG
{
char zBuf[100];
sqlite3VdbeMemPrettyPrint(pMem, zBuf);
fprintf(stderr, "OUTPUT: %s\n", zBuf);
}
#endif
return SQLITE_OK;
}

/*
** This routine checks for a byte-order mark at the beginning of the
** UTF-16 string stored in pMem. If one is present, it is removed and
** the encoding of the Mem adjusted. This routine does not do any
** byte-swapping, it just sets Mem.enc appropriately.
**
** The allocation (static, dynamic etc.) and encoding of the Mem may be
** changed by this function.
*/
static int sqlite3VdbeMemHandleBom(Mem pMem){
int rc = SQLITE_OK;
int bom = 0;
byte[] b01 = new byte[2];
Encoding.Unicode.GetBytes( pMem.z, 0, 1,b01,0 );
assert( pMem->n>=0 );
if( pMem->n>1 ){
//  u8 b1 = *(u8 *)pMem.z;
//  u8 b2 = *(((u8 *)pMem.z) + 1);
if( b01[0]==0xFE && b01[1]==0xFF ){//  if( b1==0xFE && b2==0xFF ){
bom = SQLITE_UTF16BE;
}
if( b01[0]==0xFF && b01[1]==0xFE ){  //  if( b1==0xFF && b2==0xFE ){
bom = SQLITE_UTF16LE;
}
}

if( bom!=0 ){
rc = sqlite3VdbeMemMakeWriteable(pMem);
if( rc==SQLITE_OK ){
pMem.n -= 2;
Debugger.Break (); // TODO -
//memmove(pMem.z, pMem.z[2], pMem.n);
//pMem.z[pMem.n] = '\0';
//pMem.z[pMem.n+1] = '\0';
pMem.flags |= MEM_Term;
pMem.enc = bom;
}
}
return rc;
}
#endif // * SQLITE_OMIT_UTF16 */

    /*
** pZ is a UTF-8 encoded unicode string. If nByte is less than zero,
** return the number of unicode characters in pZ up to (but not including)
** the first 0x00 byte. If nByte is not less than zero, return the
** number of unicode characters in the first nByte of pZ (or up to
** the first 0x00, whichever comes first).
*/
    static int sqlite3Utf8CharLen( string zIn, int nByte )
    {
      //int r = 0;
      //string z = zIn;
      if ( zIn.Length == 0 ) return 0;
      int zInLength = zIn.Length;
      int zTerm = ( nByte >= 0 && nByte <= zInLength ) ? nByte : zInLength;
      ////Debug.Assert( z<=zTerm );
      //for ( int i = 0 ; i < zTerm ; i++ )      //while( *z!=0 && z<zTerm ){
      //{
      //  SQLITE_SKIP_UTF8( ref z);//  SQLITE_SKIP_UTF8(z);
      //  r++;
      //}
      //return r;
      if ( zTerm == zInLength )
        return zInLength - ( zIn[zTerm - 1] == 0 ? 1 : 0 );
      else
        return nByte;
    }

    /* This test function is not currently used by the automated test-suite.
    ** Hence it is only available in debug builds.
    */
#if SQLITE_TEST && SQLITE_DEBUG
    /*
** Translate UTF-8 to UTF-8.
**
** This has the effect of making sure that the string is well-formed
** UTF-8.  Miscoded characters are removed.
**
** The translation is done in-place (since it is impossible for the
** correct UTF-8 encoding to be longer than a malformed encoding).
*/
    //int sqlite3Utf8To8(unsigned char *zIn){
    //  unsigned char *zOut = zIn;
    //  unsigned char *zStart = zIn;
    //  u32 c;

    //  while( zIn[0] ){
    //    c = sqlite3Utf8Read(zIn, (const u8**)&zIn);
    //    if( c!=0xfffd ){
    //      WRITE_UTF8(zOut, c);
    //    }
    //  }
    //  *zOut = 0;
    //  return (int)(zOut - zStart);
    //}
#endif

#if ! SQLITE_OMIT_UTF16
/*
** Convert a UTF-16 string in the native encoding into a UTF-8 string.
** Memory to hold the UTF-8 string is obtained from sqlite3Malloc and must
** be freed by the calling function.
**
** NULL is returned if there is an allocation error.
*/
static string sqlite3Utf16to8(sqlite3 db, string z, int nByte){
Debugger.Break (); // TODO -
Mem m = new Mem();
//  memset(&m, 0, sizeof(m));
//  m.db = db;
//  sqlite3VdbeMemSetStr(&m, z, nByte, SQLITE_UTF16NATIVE, SQLITE_STATIC);
//  sqlite3VdbeChangeEncoding(&m, SQLITE_UTF8);
//  if( db.mallocFailed !=0{
//    sqlite3VdbeMemRelease(&m);
//    m.z = 0;
//  }
//  Debug.Assert( (m.flags & MEM_Term)!=0 || db.mallocFailed !=0);
//  Debug.Assert( (m.flags & MEM_Str)!=0 || db.mallocFailed !=0);
return m.z;// ( m.flags & MEM_Dyn ) != 0 ? m.z : sqlite3DbStrDup( db, m.z );
}

/*
** pZ is a UTF-16 encoded unicode string. If nChar is less than zero,
** return the number of bytes up to (but not including), the first pair
** of consecutive 0x00 bytes in pZ. If nChar is not less than zero,
** then return the number of bytes in the first nChar unicode characters
** in pZ (or up until the first pair of 0x00 bytes, whichever comes first).
*/
int sqlite3Utf16ByteLen(const void *zIn, int nChar){
int c;
unsigned char const *z = zIn;
int n = 0;
if( SQLITE_UTF16NATIVE==SQLITE_UTF16BE ){
/* Using an "if (SQLITE_UTF16NATIVE==SQLITE_UTF16BE)" construct here
** and in other parts of this file means that at one branch will
** not be covered by coverage testing on any single host. But coverage
** will be complete if the tests are run on both a little-endian and
** big-endian host. Because both the UTF16NATIVE and SQLITE_UTF16BE
** macros are constant at compile time the compiler can determine
** which branch will be followed. It is therefore assumed that no runtime
** penalty is paid for this "if" statement.
*/
while( n<nChar ){
READ_UTF16BE(z, c);
n++;
}
}else{
while( n<nChar ){
READ_UTF16LE(z, c);
n++;
}
}
return (int)(z-(unsigned char const *)zIn);
}

#if SQLITE_TEST
/*
** This routine is called from the TCL test function "translate_selftest".
** It checks that the primitives for serializing and deserializing
** characters in each encoding are inverses of each other.
*/
/*
** This routine is called from the TCL test function "translate_selftest".
** It checks that the primitives for serializing and deserializing
** characters in each encoding are inverses of each other.
*/
void sqlite3UtfSelfTest(void){
unsigned int i, t;
unsigned char zBuf[20];
unsigned char *z;
int n;
unsigned int c;

for(i=0; i<0x00110000; i++){
z = zBuf;
WRITE_UTF8(z, i);
n = (int)(z-zBuf);
assert( n>0 && n<=4 );
z[0] = 0;
z = zBuf;
c = sqlite3Utf8Read(z, (const u8**)&z);
t = i;
if( i>=0xD800 && i<=0xDFFF ) t = 0xFFFD;
if( (i&0xFFFFFFFE)==0xFFFE ) t = 0xFFFD;
assert( c==t );
assert( (z-zBuf)==n );
}
for(i=0; i<0x00110000; i++){
if( i>=0xD800 && i<0xE000 ) continue;
z = zBuf;
WRITE_UTF16LE(z, i);
n = (int)(z-zBuf);
assert( n>0 && n<=4 );
z[0] = 0;
z = zBuf;
READ_UTF16LE(z, c);
assert( c==i );
assert( (z-zBuf)==n );
}
for(i=0; i<0x00110000; i++){
if( i>=0xD800 && i<0xE000 ) continue;
z = zBuf;
WRITE_UTF16BE(z, i);
n = (int)(z-zBuf);
assert( n>0 && n<=4 );
z[0] = 0;
z = zBuf;
READ_UTF16BE(z, c);
assert( c==i );
assert( (z-zBuf)==n );
}
}
#endif // * SQLITE_TEST */
#endif // * SQLITE_OMIT_UTF16 */
  }
}
