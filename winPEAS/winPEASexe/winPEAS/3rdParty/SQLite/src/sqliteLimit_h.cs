namespace winPEAS._3rdParty.SQLite.src
{
  public partial class CSSQLite
  {
    /*
    ** 2007 May 7
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
    ** This file defines various limits of what SQLite can process.
    **
    ** @(#) $Id: sqliteLimit.h,v 1.10 2009/01/10 16:15:09 danielk1977 Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */

    /*
    ** The maximum length of a TEXT or BLOB in bytes.   This also
    ** limits the size of a row in a table or index.
    **
    ** The hard limit is the ability of a 32-bit signed integer
    ** to count the size: 2^31-1 or 2147483647.
    */
#if !SQLITE_MAX_LENGTH
    const int SQLITE_MAX_LENGTH = 1000000000;
#endif

    /*
** This is the maximum number of
**
**    * Columns in a table
**    * Columns in an index
**    * Columns in a view
**    * Terms in the SET clause of an UPDATE statement
**    * Terms in the result set of a SELECT statement
**    * Terms in the GROUP BY or ORDER BY clauses of a SELECT statement.
**    * Terms in the VALUES clause of an INSERT statement
**
** The hard upper limit here is 32676.  Most database people will
** tell you that in a well-normalized database, you usually should
** not have more than a dozen or so columns in any table.  And if
** that is the case, there is no point in having more than a few
** dozen values in any of the other situations described above.
*/
#if !SQLITE_MAX_COLUMN
    const int SQLITE_MAX_COLUMN = 2000;
#endif

    /*
** The maximum length of a single SQL statement in bytes.
**
** It used to be the case that setting this value to zero would
** turn the limit off.  That is no longer true.  It is not possible
** to turn this limit off.
*/
#if !SQLITE_MAX_SQL_LENGTH
    const int SQLITE_MAX_SQL_LENGTH = 1000000000;
#endif

    /*
** The maximum depth of an expression tree. This is limited to
** some extent by SQLITE_MAX_SQL_LENGTH. But sometime you might
** want to place more severe limits on the complexity of an
** expression.
**
** A value of 0 used to mean that the limit was not enforced.
** But that is no longer true.  The limit is now strictly enforced
** at all times.
*/
#if !SQLITE_MAX_EXPR_DEPTH
    const int SQLITE_MAX_EXPR_DEPTH = 1000;
#endif

    /*
** The maximum number of terms in a compound SELECT statement.
** The code generator for compound SELECT statements does one
** level of recursion for each term.  A stack overflow can result
** if the number of terms is too large.  In practice, most SQL
** never has more than 3 or 4 terms.  Use a value of 0 to disable
** any limit on the number of terms in a compount SELECT.
*/
#if !SQLITE_MAX_COMPOUND_SELECT
    const int SQLITE_MAX_COMPOUND_SELECT = 250;
#endif

    /*
** The maximum number of opcodes in a VDBE program.
** Not currently enforced.
*/
#if !SQLITE_MAX_VDBE_OP
    const int SQLITE_MAX_VDBE_OP = 25000;
#endif

    /*
** The maximum number of arguments to an SQL function.
*/
#if !SQLITE_MAX_FUNCTION_ARG
    const int SQLITE_MAX_FUNCTION_ARG = 127;//# define SQLITE_MAX_FUNCTION_ARG 127
#endif

    /*
** The maximum number of in-memory pages to use for the main database
** table and for temporary tables.  The SQLITE_DEFAULT_CACHE_SIZE
*/
#if !SQLITE_DEFAULT_CACHE_SIZE
    const int SQLITE_DEFAULT_CACHE_SIZE = 2000;
#endif
#if !SQLITE_DEFAULT_TEMP_CACHE_SIZE
    const int SQLITE_DEFAULT_TEMP_CACHE_SIZE = 500;
#endif

    /*
** The maximum number of attached databases.  This must be between 0
** and 30.  The upper bound on 30 is because a 32-bit integer bitmap
** is used internally to track attached databases.
*/
#if !SQLITE_MAX_ATTACHED
    const int SQLITE_MAX_ATTACHED = 10;
#endif


    /*
** The maximum value of a ?nnn wildcard that the parser will accept.
*/
#if !SQLITE_MAX_VARIABLE_NUMBER
    const int SQLITE_MAX_VARIABLE_NUMBER = 999;
#endif

    /* Maximum page size.  The upper bound on this value is 32768.  This a limit
** imposed by the necessity of storing the value in a 2-byte unsigned integer
** and the fact that the page size must be a power of 2.
**
** If this limit is changed, then the compiled library is technically
** incompatible with an SQLite library compiled with a different limit. If
** a process operating on a database with a page-size of 65536 bytes
** crashes, then an instance of SQLite compiled with the default page-size
** limit will not be able to rollback the aborted transaction. This could
** lead to database corruption.
*/
#if !SQLITE_MAX_PAGE_SIZE
    const int SQLITE_MAX_PAGE_SIZE = 32768;
#endif


    /*
** The default size of a database page.
*/
#if !SQLITE_DEFAULT_PAGE_SIZE
    const int SQLITE_DEFAULT_PAGE_SIZE = 1024;
#endif
#if SQLITE_DEFAULT_PAGE_SIZE //SQLITE_DEFAULT_PAGE_SIZE>SQLITE_MAX_PAGE_SIZE
# undef SQLITE_DEFAULT_PAGE_SIZE
const int SQLITE_DEFAULT_PAGE_SIZE SQLITE_MAX_PAGE_SIZE
#endif

    /*
** Ordinarily, if no value is explicitly provided, SQLite creates databases
** with page size SQLITE_DEFAULT_PAGE_SIZE. However, based on certain
** device characteristics (sector-size and atomic write() support),
** SQLite may choose a larger value. This constant is the maximum value
** SQLite will choose on its own.
*/
#if !SQLITE_MAX_DEFAULT_PAGE_SIZE
    const int SQLITE_MAX_DEFAULT_PAGE_SIZE = 8192;
#endif
#if SQLITE_MAX_DEFAULT_PAGE_SIZE //SQLITE_MAX_DEFAULT_PAGE_SIZE>SQLITE_MAX_PAGE_SIZE
# undef SQLITE_MAX_DEFAULT_PAGE_SIZE
const int SQLITE_MAX_DEFAULT_PAGE_SIZE SQLITE_MAX_PAGE_SIZE
#endif


    /*
** Maximum number of pages in one database file.
**
** This is really just the default value for the max_page_count pragma.
** This value can be lowered (or raised) at run-time using that the
** max_page_count macro.
*/
#if !SQLITE_MAX_PAGE_COUNT
    const int SQLITE_MAX_PAGE_COUNT = 1073741823;
#endif

    /*
** Maximum length (in bytes) of the pattern in a LIKE or GLOB
** operator.
*/
#if !SQLITE_MAX_LIKE_PATTERN_LENGTH
    const int SQLITE_MAX_LIKE_PATTERN_LENGTH = 50000;
#endif
  }
}
