/*
**  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
**  C#-SQLite is an independent reimplementation of the SQLite software library
**
*************************************************************************
**  Repository path : $HeadURL: https://sqlitecs.googlecode.com/svn/trunk/C%23SQLite/src/parse_h.cs $
**  Revision        : $Revision$
**  Last Change Date: $LastChangedDate: 2009-08-04 13:34:52 -0700 (Tue, 04 Aug 2009) $
**  Last Changed By : $LastChangedBy: noah.hart $
*************************************************************************
*/

namespace CS_SQLite3
{
  public partial class CSSQLite
  {
    //#define TK_SEMI                            1
    //#define TK_EXPLAIN                         2
    //#define TK_QUERY                           3
    //#define TK_PLAN                            4
    //#define TK_BEGIN                           5
    //#define TK_TRANSACTION                     6
    //#define TK_DEFERRED                        7
    //#define TK_IMMEDIATE                       8
    //#define TK_EXCLUSIVE                       9
    //#define TK_COMMIT                         10
    //#define TK_END                            11
    //#define TK_ROLLBACK                       12
    //#define TK_SAVEPOINT                      13
    //#define TK_RELEASE                        14
    //#define TK_TO                             15
    //#define TK_TABLE                          16
    //#define TK_CREATE                         17
    //#define TK_IF                             18
    //#define TK_NOT                            19
    //#define TK_EXISTS                         20
    //#define TK_TEMP                           21
    //#define TK_LP                             22
    //#define TK_RP                             23
    //#define TK_AS                             24
    //#define TK_COMMA                          25
    //#define TK_ID                             26
    //#define TK_INDEXED                        27
    //#define TK_ABORT                          28
    //#define TK_AFTER                          29
    //#define TK_ANALYZE                        30
    //#define TK_ASC                            31
    //#define TK_ATTACH                         32
    //#define TK_BEFORE                         33
    //#define TK_BY                             34
    //#define TK_CASCADE                        35
    //#define TK_CAST                           36
    //#define TK_COLUMNKW                       37
    //#define TK_CONFLICT                       38
    //#define TK_DATABASE                       39
    //#define TK_DESC                           40
    //#define TK_DETACH                         41
    //#define TK_EACH                           42
    //#define TK_FAIL                           43
    //#define TK_FOR                            44
    //#define TK_IGNORE                         45
    //#define TK_INITIALLY                      46
    //#define TK_INSTEAD                        47
    //#define TK_LIKE_KW                        48
    //#define TK_MATCH                          49
    //#define TK_KEY                            50
    //#define TK_OF                             51
    //#define TK_OFFSET                         52
    //#define TK_PRAGMA                         53
    //#define TK_RAISE                          54
    //#define TK_REPLACE                        55
    //#define TK_RESTRICT                       56
    //#define TK_ROW                            57
    //#define TK_TRIGGER                        58
    //#define TK_VACUUM                         59
    //#define TK_VIEW                           60
    //#define TK_VIRTUAL                        61
    //#define TK_REINDEX                        62
    //#define TK_RENAME                         63
    //#define TK_CTIME_KW                       64
    //#define TK_ANY                            65
    //#define TK_OR                             66
    //#define TK_AND                            67
    //#define TK_IS                             68
    //#define TK_BETWEEN                        69
    //#define TK_IN                             70
    //#define TK_ISNULL                         71
    //#define TK_NOTNULL                        72
    //#define TK_NE                             73
    //#define TK_EQ                             74
    //#define TK_GT                             75
    //#define TK_LE                             76
    //#define TK_LT                             77
    //#define TK_GE                             78
    //#define TK_ESCAPE                         79
    //#define TK_BITAND                         80
    //#define TK_BITOR                          81
    //#define TK_LSHIFT                         82
    //#define TK_RSHIFT                         83
    //#define TK_PLUS                           84
    //#define TK_MINUS                          85
    //#define TK_STAR                           86
    //#define TK_SLASH                          87
    //#define TK_REM                            88
    //#define TK_CONCAT                         89
    //#define TK_COLLATE                        90
    //#define TK_UMINUS                         91
    //#define TK_UPLUS                          92
    //#define TK_BITNOT                         93
    //#define TK_STRING                         94
    //#define TK_JOIN_KW                        95
    //#define TK_CONSTRAINT                     96
    //#define TK_DEFAULT                        97
    //#define TK_NULL                           98
    //#define TK_PRIMARY                        99
    //#define TK_UNIQUE                         100
    //#define TK_CHECK                          101
    //#define TK_REFERENCES                     102
    //#define TK_AUTOINCR                       103
    //#define TK_ON                             104
    //#define TK_DELETE                         105
    //#define TK_UPDATE                         106
    //#define TK_INSERT                         107
    //#define TK_SET                            108
    //#define TK_DEFERRABLE                     109
    //#define TK_FOREIGN                        110
    //#define TK_DROP                           111
    //#define TK_UNION                          112
    //#define TK_ALL                            113
    //#define TK_EXCEPT                         114
    //#define TK_INTERSECT                      115
    //#define TK_SELECT                         116
    //#define TK_DISTINCT                       117
    //#define TK_DOT                            118
    //#define TK_FROM                           119
    //#define TK_JOIN                           120
    //#define TK_USING                          121
    //#define TK_ORDER                          122
    //#define TK_GROUP                          123
    //#define TK_HAVING                         124
    //#define TK_LIMIT                          125
    //#define TK_WHERE                          126
    //#define TK_INTO                           127
    //#define TK_VALUES                         128
    //#define TK_INTEGER                        129
    //#define TK_FLOAT                          130
    //#define TK_BLOB                           131
    //#define TK_REGISTER                       132
    //#define TK_VARIABLE                       133
    //#define TK_CASE                           134
    //#define TK_WHEN                           135
    //#define TK_THEN                           136
    //#define TK_ELSE                           137
    //#define TK_INDEX                          138
    //#define TK_ALTER                          139
    //#define TK_ADD                            140
    //#define TK_TO_TEXT                        141
    //#define TK_TO_BLOB                        142
    //#define TK_TO_NUMERIC                     143
    //#define TK_TO_INT                         144
    //#define TK_TO_REAL                        145
    //#define TK_END_OF_FILE                    146
    //#define TK_ILLEGAL                        147
    //#define TK_SPACE                          148
    //#define TK_UNCLOSED_STRING                149
    //#define TK_FUNCTION                       150
    //#define TK_COLUMN                         151
    //#define TK_AGG_FUNCTION                   152
    //#define TK_AGG_COLUMN                     153
    //#define TK_CONST_FUNC                     154
    const int TK_SEMI = 1;
    const int TK_EXPLAIN = 2;
    const int TK_QUERY = 3;
    const int TK_PLAN = 4;
    const int TK_BEGIN = 5;
    const int TK_TRANSACTION = 6;
    const int TK_DEFERRED = 7;
    const int TK_IMMEDIATE = 8;
    const int TK_EXCLUSIVE = 9;
    const int TK_COMMIT = 10;
    const int TK_END = 11;
    const int TK_ROLLBACK = 12;
    const int TK_SAVEPOINT = 13;
    const int TK_RELEASE = 14;
    const int TK_TO = 15;
    const int TK_TABLE = 16;
    const int TK_CREATE = 17;
    const int TK_IF = 18;
    const int TK_NOT = 19;
    const int TK_EXISTS = 20;
    const int TK_TEMP = 21;
    const int TK_LP = 22;
    const int TK_RP = 23;
    const int TK_AS = 24;
    const int TK_COMMA = 25;
    const int TK_ID = 26;
    const int TK_INDEXED = 27;
    const int TK_ABORT = 28;
    const int TK_AFTER = 29;
    const int TK_ANALYZE = 30;
    const int TK_ASC = 31;
    const int TK_ATTACH = 32;
    const int TK_BEFORE = 33;
    const int TK_BY = 34;
    const int TK_CASCADE = 35;
    const int TK_CAST = 36;
    const int TK_COLUMNKW = 37;
    const int TK_CONFLICT = 38;
    const int TK_DATABASE = 39;
    const int TK_DESC = 40;
    const int TK_DETACH = 41;
    const int TK_EACH = 42;
    const int TK_FAIL = 43;
    const int TK_FOR = 44;
    const int TK_IGNORE = 45;
    const int TK_INITIALLY = 46;
    const int TK_INSTEAD = 47;
    const int TK_LIKE_KW = 48;
    const int TK_MATCH = 49;
    const int TK_KEY = 50;
    const int TK_OF = 51;
    const int TK_OFFSET = 52;
    const int TK_PRAGMA = 53;
    const int TK_RAISE = 54;
    const int TK_REPLACE = 55;
    const int TK_RESTRICT = 56;
    const int TK_ROW = 57;
    const int TK_TRIGGER = 58;
    const int TK_VACUUM = 59;
    const int TK_VIEW = 60;
    const int TK_VIRTUAL = 61;
    const int TK_REINDEX = 62;
    const int TK_RENAME = 63;
    const int TK_CTIME_KW = 64;
    const int TK_ANY = 65;
    const int TK_OR = 66;
    const int TK_AND = 67;
    const int TK_IS = 68;
    const int TK_BETWEEN = 69;
    const int TK_IN = 70;
    const int TK_ISNULL = 71;
    const int TK_NOTNULL = 72;
    const int TK_NE = 73;
    const int TK_EQ = 74;
    const int TK_GT = 75;
    const int TK_LE = 76;
    const int TK_LT = 77;
    const int TK_GE = 78;
    const int TK_ESCAPE = 79;
    const int TK_BITAND = 80;
    const int TK_BITOR = 81;
    const int TK_LSHIFT = 82;
    const int TK_RSHIFT = 83;
    const int TK_PLUS = 84;
    const int TK_MINUS = 85;
    const int TK_STAR = 86;
    const int TK_SLASH = 87;
    const int TK_REM = 88;
    const int TK_CONCAT = 89;
    const int TK_COLLATE = 90;
    const int TK_UMINUS = 91;
    const int TK_UPLUS = 92;
    const int TK_BITNOT = 93;
    const int TK_STRING = 94;
    const int TK_JOIN_KW = 95;
    const int TK_CONSTRAINT = 96;
    const int TK_DEFAULT = 97;
    const int TK_NULL = 98;
    const int TK_PRIMARY = 99;
    const int TK_UNIQUE = 100;
    const int TK_CHECK = 101;
    const int TK_REFERENCES = 102;
    const int TK_AUTOINCR = 103;
    const int TK_ON = 104;
    const int TK_DELETE = 105;
    const int TK_UPDATE = 106;
    const int TK_INSERT = 107;
    const int TK_SET = 108;
    const int TK_DEFERRABLE = 109;
    const int TK_FOREIGN = 110;
    const int TK_DROP = 111;
    const int TK_UNION = 112;
    const int TK_ALL = 113;
    const int TK_EXCEPT = 114;
    const int TK_INTERSECT = 115;
    const int TK_SELECT = 116;
    const int TK_DISTINCT = 117;
    const int TK_DOT = 118;
    const int TK_FROM = 119;
    const int TK_JOIN = 120;
    const int TK_USING = 121;
    const int TK_ORDER = 122;
    const int TK_GROUP = 123;
    const int TK_HAVING = 124;
    const int TK_LIMIT = 125;
    const int TK_WHERE = 126;
    const int TK_INTO = 127;
    const int TK_VALUES = 128;
    const int TK_INTEGER = 129;
    const int TK_FLOAT = 130;
    const int TK_BLOB = 131;
    const int TK_REGISTER = 132;
    const int TK_VARIABLE = 133;
    const int TK_CASE = 134;
    const int TK_WHEN = 135;
    const int TK_THEN = 136;
    const int TK_ELSE = 137;
    const int TK_INDEX = 138;
    const int TK_ALTER = 139;
    const int TK_ADD = 140;
    const int TK_TO_TEXT = 141;
    const int TK_TO_BLOB = 142;
    const int TK_TO_NUMERIC = 143;
    const int TK_TO_INT = 144;
    const int TK_TO_REAL = 145;
    const int TK_END_OF_FILE = 146;
    const int TK_ILLEGAL = 147;
    const int TK_SPACE = 148;
    const int TK_UNCLOSED_STRING = 149;
    const int TK_FUNCTION = 150;
    const int TK_COLUMN = 151;
    const int TK_AGG_FUNCTION = 152;
    const int TK_AGG_COLUMN = 153;
    const int TK_CONST_FUNC = 154;
  }
}
