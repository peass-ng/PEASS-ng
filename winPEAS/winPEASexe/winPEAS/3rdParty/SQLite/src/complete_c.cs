namespace winPEAS._3rdParty.SQLite.src
{

  using u8 = System.Byte;

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
    ** An tokenizer for SQL
    **
    ** This file contains C code that implements the sqlite3_complete() API.
    ** This code used to be part of the tokenizer.c source file.  But by
    ** separating it out, the code will be automatically omitted from
    ** static links that do not use it.
    **
    ** $Id: complete.c,v 1.8 2009/04/28 04:46:42 drh Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#include "sqliteInt.h"
#if !SQLITE_OMIT_COMPLETE

    /*
** This is defined in tokenize.c.  We just have to import the definition.
*/
#if !SQLITE_AMALGAMATION
#if  SQLITE_ASCII
    //extern const char sqlite3IsAsciiIdChar[];
    //#define IdChar(C)  (((c=C)&0x80)!=0 || (c>0x1f && sqlite3IsAsciiIdChar[c-0x20]))
    static bool IdChar( u8 C ) { u8 c; return ( ( c = C ) & 0x80 ) != 0 || ( c > 0x1f && sqlite3IsAsciiIdChar[c - 0x20] ); }
#endif
#if  SQLITE_EBCDIC
//extern const char sqlite3IsEbcdicIdChar[];
//#define IdChar(C)  (((c=C)>=0x42 && sqlite3IsEbcdicIdChar[c-0x40]))
#endif
#endif // * SQLITE_AMALGAMATION */


    /*
** Token types used by the sqlite3_complete() routine.  See the header
** comments on that procedure for additional information.
*/
    const int tkSEMI = 0;
    const int tkWS = 1;
    const int tkOTHER = 2;
    const int tkEXPLAIN = 3;
    const int tkCREATE = 4;
    const int tkTEMP = 5;
    const int tkTRIGGER = 6;
    const int tkEND = 7;

    /*
    ** Return TRUE if the given SQL string ends in a semicolon.
    **
    ** Special handling is require for CREATE TRIGGER statements.
    ** Whenever the CREATE TRIGGER keywords are seen, the statement
    ** must end with ";END;".
    **
    ** This implementation uses a state machine with 7 states:
    **
    **   (0) START     At the beginning or end of an SQL statement.  This routine
    **                 returns 1 if it ends in the START state and 0 if it ends
    **                 in any other state.
    **
    **   (1) NORMAL    We are in the middle of statement which ends with a single
    **                 semicolon.
    **
    **   (2) EXPLAIN   The keyword EXPLAIN has been seen at the beginning of
    **                 a statement.
    **
    **   (3) CREATE    The keyword CREATE has been seen at the beginning of a
    **                 statement, possibly preceeded by EXPLAIN and/or followed by
    **                 TEMP or TEMPORARY
    **
    **   (4) TRIGGER   We are in the middle of a trigger definition that must be
    **                 ended by a semicolon, the keyword END, and another semicolon.
    **
    **   (5) SEMI      We've seen the first semicolon in the ";END;" that occurs at
    **                 the end of a trigger definition.
    **
    **   (6) END       We've seen the ";END" of the ";END;" that occurs at the end
    **                 of a trigger difinition.
    **
    ** Transitions between states above are determined by tokens extracted
    ** from the input.  The following tokens are significant:
    **
    **   (0) tkSEMI      A semicolon.
    **   (1) tkWS        Whitespace
    **   (2) tkOTHER     Any other SQL token.
    **   (3) tkEXPLAIN   The "explain" keyword.
    **   (4) tkCREATE    The "create" keyword.
    **   (5) tkTEMP      The "temp" or "temporary" keyword.
    **   (6) tkTRIGGER   The "trigger" keyword.
    **   (7) tkEND       The "end" keyword.
    **
    ** Whitespace never causes a state transition and is always ignored.
    **
    ** If we compile with SQLITE_OMIT_TRIGGER, all of the computation needed
    ** to recognize the end of a trigger can be omitted.  All we have to do
    ** is look for a semicolon that is not part of an string or comment.
    */
    public static int sqlite3_complete( string zSql )
    {
      int state = 0;   /* Current state, using numbers defined in header comment */
      int token;       /* Value of the next token */

#if !SQLITE_OMIT_TRIGGER
      /* A complex statement machine used to detect the end of a CREATE TRIGGER
** statement.  This is the normal case.
*/
      u8[][] trans = new u8[][]       {
/* Token:                                                */
/* State:       **  SEMI  WS  OTHER EXPLAIN  CREATE  TEMP  TRIGGER  END  */
/* 0   START: */ new u8[] {    0,  0,     1,      2,      3,    1,       1,   1,  },
/* 1  NORMAL: */  new u8[]{    0,  1,     1,      1,      1,    1,       1,   1,  },
/* 2 EXPLAIN: */  new u8[]{    0,  2,     2,      1,      3,    1,       1,   1,  },
/* 3  CREATE: */  new u8[]{    0,  3,     1,      1,      1,    3,       4,   1,  },
/* 4 TRIGGER: */  new u8[]{    5,  4,     4,      4,      4,    4,       4,   4,  },
/* 5    SEMI: */  new u8[]{    5,  5,     4,      4,      4,    4,       4,   6,  },
/* 6     END: */  new u8[]{    0,  6,     4,      4,      4,    4,       4,   4,  },
};
#else
/* If triggers are not suppored by this compile then the statement machine
** used to detect the end of a statement is much simplier
*/
static const u8 trans[2][3] = {
/* Token:           */
/* State:       **  SEMI  WS  OTHER */
/* 0   START: */ {    0,  0,     1, },
/* 1  NORMAL: */ {    0,  1,     1, },
};
#endif // * SQLITE_OMIT_TRIGGER */

      int zIdx = 0;
      while ( zIdx < zSql.Length )
      {
        switch ( zSql[zIdx] )
        {
          case ';':
            {  /* A semicolon */
              token = tkSEMI;
              break;
            }
          case ' ':
          case '\r':
          case '\t':
          case '\n':
          case '\f':
            {  /* White space is ignored */
              token = tkWS;
              break;
            }
          case '/':
            {   /* C-style comments */
              if ( zSql[zIdx + 1] != '*' )
              {
                token = tkOTHER;
                break;
              }
              zIdx += 2;
              while ( zIdx < zSql.Length && zSql[zIdx] != '*' || zIdx < zSql.Length - 1 && zSql[zIdx + 1] != '/' ) { zIdx++; }
              if ( zIdx == zSql.Length ) return 0;
              zIdx++;
              token = tkWS;
              break;
            }
          case '-':
            {   /* SQL-style comments from "--" to end of line */
              if ( zSql[zIdx + 1] != '-' )
              {
                token = tkOTHER;
                break;
              }
              while ( zIdx < zSql.Length && zSql[zIdx] != '\n' ) { zIdx++; }
              if ( zIdx == zSql.Length ) return state == 0 ? 1 : 0;
              token = tkWS;
              break;
            }
          case '[':
            {   /* Microsoft-style identifiers in [...] */
              zIdx++;
              while ( zIdx < zSql.Length && zSql[zIdx] != ']' ) { zIdx++; }
              if ( zIdx == zSql.Length ) return 0;
              token = tkOTHER;
              break;
            }
          case '`':     /* Grave-accent quoted symbols used by MySQL */
          case '"':     /* single- and double-quoted strings */
          case '\'':
            {
              int c = zSql[zIdx];
              zIdx++;
              while ( zIdx < zSql.Length && zSql[zIdx] != c ) { zIdx++; }
              if ( zIdx == zSql.Length ) return 0;
              token = tkOTHER;
              break;
            }
          default:
            {
              int c;
              if ( IdChar( (u8)zSql[zIdx] ) )
              {
                /* Keywords and unquoted identifiers */
                int nId;
                for ( nId = 1 ; ( zIdx + nId ) < zSql.Length && IdChar( (u8)zSql[zIdx + nId] ) ; nId++ ) { }
#if  SQLITE_OMIT_TRIGGER
token = tkOTHER;
#else
                switch ( zSql[zIdx] )
                {
                  case 'c':
                  case 'C':
                    {
                      if ( nId == 6 && sqlite3StrNICmp( zSql, zIdx, "create", 6 ) == 0 )
                      {
                        token = tkCREATE;
                      }
                      else
                      {
                        token = tkOTHER;
                      }
                      break;
                    }
                  case 't':
                  case 'T':
                    {
                      if ( nId == 7 && sqlite3StrNICmp( zSql, zIdx, "trigger", 7 ) == 0 )
                      {
                        token = tkTRIGGER;
                      }
                      else if ( nId == 4 && sqlite3StrNICmp( zSql, zIdx, "temp", 4 ) == 0 )
                      {
                        token = tkTEMP;
                      }
                      else if ( nId == 9 && sqlite3StrNICmp( zSql, zIdx, "temporary", 9 ) == 0 )
                      {
                        token = tkTEMP;
                      }
                      else
                      {
                        token = tkOTHER;
                      }
                      break;
                    }
                  case 'e':
                  case 'E':
                    {
                      if ( nId == 3 && sqlite3StrNICmp( zSql, zIdx, "end", 3 ) == 0 )
                      {
                        token = tkEND;
                      }
                      else
#if ! SQLITE_OMIT_EXPLAIN
                        if ( nId == 7 && sqlite3StrNICmp( zSql, zIdx, "explain", 7 ) == 0 )
                        {
                          token = tkEXPLAIN;
                        }
                        else
#endif
                        {
                          token = tkOTHER;
                        }
                      break;
                    }
                  default:
                    {
                      token = tkOTHER;
                      break;
                    }
                }
#endif // * SQLITE_OMIT_TRIGGER */
                zIdx += nId - 1;
              }
              else
              {
                /* Operators and special symbols */
                token = tkOTHER;
              }
              break;
            }
        }
        state = trans[state][token];
        zIdx++;
      }
      return ( state == 0 ) ? 1 : 0;
    }

#if ! SQLITE_OMIT_UTF16
/*
** This routine is the same as the sqlite3_complete() routine described
** above, except that the parameter is required to be UTF-16 encoded, not
** UTF-8.
*/
int sqlite3_complete16(const void *zSql){
sqlite3_value pVal;
char const *zSql8;
int rc = SQLITE_NOMEM;

#if !SQLITE_OMIT_AUTOINIT
rc = sqlite3_initialize();
if( rc !=0) return rc;
#endif
pVal = sqlite3ValueNew(0);
sqlite3ValueSetStr(pVal, -1, zSql, SQLITE_UTF16NATIVE, SQLITE_STATIC);
zSql8 = sqlite3ValueText(pVal, SQLITE_UTF8);
if( zSql8 ){
rc = sqlite3_complete(zSql8);
}else{
rc = SQLITE_NOMEM;
}
sqlite3ValueFree(pVal);
return sqlite3ApiExit(0, rc);
}
#endif // * SQLITE_OMIT_UTF16 */
#endif // * SQLITE_OMIT_COMPLETE */
  }
}
