#define YYFALLBACK
#define YYWILDCARD

using System.Diagnostics;
using u8 = System.Byte;


using YYCODETYPE = System.Int32;
using YYACTIONTYPE = System.Int32;

namespace winPEAS._3rdParty.SQLite.src
{
  using sqlite3ParserTOKENTYPE = CSSQLite.Token;

  public partial class CSSQLite
  {
    /*
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */

    /* Driver template for the LEMON parser generator.
    ** The author disclaims copyright to this source code.
    **
    ** This version of "lempar.c" is modified, slightly, for use by SQLite.
    ** The only modifications are the addition of a couple of NEVER()
    ** macros to disable tests that are needed in the case of a general
    ** LALR(1) grammar but which are always false in the
    ** specific grammar used by SQLite.
    */
    /* First off, code is included that follows the "include" declaration
    ** in the input grammar file. */
    //#include <stdio.h>
    //#line 53 "parse.y"

    //#include "sqliteInt.h"
    /*
    ** Disable all error recovery processing in the parser push-down
    ** automaton.
    */
    //#define YYNOERRORRECOVERY 1
    const int YYNOERRORRECOVERY = 1;

    /*
    ** Make yytestcase() the same as testcase()
    */
    //#define yytestcase(X) testcase(X)
    static void yytestcase<T>(T X) { testcase(X); }

    /*
    ** An instance of this structure holds information about the
    ** LIMIT clause of a SELECT statement.
    */
    public struct LimitVal
    {
      public Expr pLimit;    /* The LIMIT expression.  NULL if there is no limit */
      public Expr pOffset;   /* The OFFSET expression.  NULL if there is none */
    };

    /*
    ** An instance of this structure is used to store the LIKE,
    ** GLOB, NOT LIKE, and NOT GLOB operators.
    */
    public struct LikeOp
    {
      public Token eOperator;  /* "like" or "glob" or "regexp" */
      public bool not;         /* True if the NOT keyword is present */
    };

    /*
    ** An instance of the following structure describes the event of a
    ** TRIGGER.  "a" is the event type, one of TK_UPDATE, TK_INSERT,
    ** TK_DELETE, or TK_INSTEAD.  If the event is of the form
    **
    **      UPDATE ON (a,b,c)
    **
    ** Then the "b" IdList records the list "a,b,c".
    */
#if !SQLITE_OMIT_TRIGGER
    public struct TrigEvent { public int a; public IdList b; };
#endif
    /*
** An instance of this structure holds the ATTACH key and the key type.
*/
    public struct AttachKey { public int type; public Token key; };

    //#line 723 "parse.y"

    /* This is a utility routine used to set the ExprSpan.zStart and
    ** ExprSpan.zEnd values of pOut so that the span covers the complete
    ** range of text beginning with pStart and going to the end of pEnd.
    */
    static void spanSet(ExprSpan pOut, Token pStart, Token pEnd)
    {
      pOut.zStart = pStart.z;
      pOut.zEnd = pEnd.z.Substring(pEnd.n);
    }

    /* Construct a new Expr object from a single identifier.  Use the
    ** new Expr to populate pOut.  Set the span of pOut to be the identifier
    ** that created the expression.
    */
    static void spanExpr(ExprSpan pOut, Parse pParse, int op, Token pValue)
    {
      pOut.pExpr = sqlite3PExpr(pParse, op, 0, 0, pValue);
      pOut.zStart = pValue.z;
      pOut.zEnd = pValue.z.Substring(pValue.n);
    }
    //#line 818 "parse.y"

    /* This routine constructs a binary expression node out of two ExprSpan
    ** objects and uses the result to populate a new ExprSpan object.
    */
    static void spanBinaryExpr(
    ExprSpan pOut,     /* Write the result here */
    Parse pParse,      /* The parsing context.  Errors accumulate here */
    int op,            /* The binary operation */
    ExprSpan pLeft,    /* The left operand */
    ExprSpan pRight    /* The right operand */
    )
    {
      pOut.pExpr = sqlite3PExpr(pParse, op, pLeft.pExpr, pRight.pExpr, 0);
      pOut.zStart = pLeft.zStart;
      pOut.zEnd = pRight.zEnd;
    }
    //#line 870 "parse.y"

    /* Construct an expression node for a unary postfix operator
    */
    static void spanUnaryPostfix(
    ExprSpan pOut,        /* Write the new expression node here */
    Parse pParse,         /* Parsing context to record errors */
    int op,               /* The operator */
    ExprSpan pOperand,    /* The operand */
    Token pPostOp         /* The operand token for setting the span */
    )
    {
      pOut.pExpr = sqlite3PExpr(pParse, op, pOperand.pExpr, 0, 0);
      pOut.zStart = pOperand.zStart;
      pOut.zEnd = pPostOp.z.Substring(pPostOp.n);
    }
    //#line 892 "parse.y"

    /* Construct an expression node for a unary prefix operator
    */
    static void spanUnaryPrefix(
    ExprSpan pOut,        /* Write the new expression node here */
    Parse pParse,         /* Parsing context to record errors */
    int op,               /* The operator */
    ExprSpan pOperand,    /* The operand */
    Token pPreOp          /* The operand token for setting the span */
    )
    {
      pOut.pExpr = sqlite3PExpr(pParse, op, pOperand.pExpr, 0, 0);
      pOut.zStart = pPreOp.z;
      pOut.zEnd = pOperand.zEnd;
    }
    //#line 129 "parse.c"
    /* Next is all token values, in a form suitable for use by makeheaders.
    ** This section will be null unless lemon is run with the -m switch.
    */
    /*
    ** These constants (all generated automatically by the parser generator)
    ** specify the various kinds of tokens (terminals) that the parser
    ** understands.
    **
    ** Each symbol here is a terminal symbol in the grammar.
    */
    /* Make sure the INTERFACE macro is defined.
    */
#if !INTERFACE
    //# define INTERFACE 1
#endif
    /* The next thing included is series of defines which control
** various aspects of the generated parser.
**    YYCODETYPE         is the data type used for storing terminal
**                       and nonterminal numbers.  "unsigned char" is
**                       used if there are fewer than 250 terminals
**                       and nonterminals.  "int" is used otherwise.
**    YYNOCODE           is a number of type YYCODETYPE which corresponds
**                       to no legal terminal or nonterminal number.  This
**                       number is used to fill in empty slots of the hash
**                       table.
**    YYFALLBACK         If defined, this indicates that one or more tokens
**                       have fall-back values which should be used if the
**                       original value of the token will not parse.
**    YYACTIONTYPE       is the data type used for storing terminal
**                       and nonterminal numbers.  "unsigned char" is
**                       used if there are fewer than 250 rules and
**                       states combined.  "int" is used otherwise.
**    sqlite3ParserTOKENTYPE     is the data type used for minor tokens given
**                       directly to the parser from the tokenizer.
**    YYMINORTYPE        is the data type used for all minor tokens.
**                       This is typically a union of many types, one of
**                       which is sqlite3ParserTOKENTYPE.  The entry in the union
**                       for base tokens is called "yy0".
**    YYSTACKDEPTH       is the maximum depth of the parser's stack.  If
**                       zero the stack is dynamically sized using realloc()
**    sqlite3ParserARG_SDECL     A static variable declaration for the %extra_argument
**    sqlite3ParserARG_PDECL     A parameter declaration for the %extra_argument
**    sqlite3ParserARG_STORE     Code to store %extra_argument into yypParser
**    sqlite3ParserARG_FETCH     Code to extract %extra_argument from yypParser
**    YYNSTATE           the combined number of states.
**    YYNRULE            the number of rules in the grammar
**    YYERRORSYMBOL      is the code number of the error symbol.  If not
**                       defined, then do no error processing.
*/
    //#define YYCODETYPE unsigned short char
    const int YYNOCODE = 254;
    //#define YYACTIONTYPE unsigned short int
    const int YYWILDCARD = 65;
    //#define sqlite3ParserTOKENTYPE Token
    public class YYMINORTYPE
    {
      public int yyinit;
      public sqlite3ParserTOKENTYPE yy0 = new sqlite3ParserTOKENTYPE();
      public Select yy3;
      public ExprList yy14;
      public SrcList yy65;
      public LikeOp yy96;
      public Expr yy132;
      public u8 yy186;
      public int yy328;
      public ExprSpan yy346 = new ExprSpan();
#if !SQLITE_OMIT_TRIGGER
      public TrigEvent yy378;
#endif
      public IdList yy408;
      public struct _yy429 { public int value; public int mask;}public _yy429 yy429;
#if !SQLITE_OMIT_TRIGGER
      public TriggerStep yy473;
#endif
      public LimitVal yy476;
    }

#if !YYSTACKDEPTH
    const int YYSTACKDEPTH = 100;
#endif
    //#define sqlite3ParserARG_SDECL Parse pParse;
    //#define sqlite3ParserARG_PDECL ,Parse pParse
    //#define sqlite3ParserARG_FETCH Parse pParse = yypParser.pParse
    //#define sqlite3ParserARG_STORE yypParser.pParse = pParse
    const int YYNSTATE = 629;
    const int YYNRULE = 329;
    //#define YYFALLBACK
    const int YY_NO_ACTION = (YYNSTATE + YYNRULE + 2);
    const int YY_ACCEPT_ACTION = (YYNSTATE + YYNRULE + 1);
    const int YY_ERROR_ACTION = (YYNSTATE + YYNRULE);

    /* The yyzerominor constant is used to initialize instances of
    ** YYMINORTYPE objects to zero. */
    YYMINORTYPE yyzerominor = new YYMINORTYPE();//static const YYMINORTYPE yyzerominor = { 0 };

    /* Define the yytestcase() macro to be a no-op if is not already defined
    ** otherwise.
    **
    ** Applications can choose to define yytestcase() in the %include section
    ** to a macro that can assist in verifying code coverage.  For production
    ** code the yytestcase() macro should be turned off.  But it is useful
    ** for testing.
    */
    //#if !yytestcase
    //# define yytestcase(X)
    //#endif

    /* Next are the tables used to determine what action to take based on the
    ** current state and lookahead token.  These tables are used to implement
    ** functions that take a state number and lookahead value and return an
    ** action integer.
    **
    ** Suppose the action integer is N.  Then the action is determined as
    ** follows
    **
    **   0 <= N < YYNSTATE                  Shift N.  That is, push the lookahead
    **                                      token onto the stack and goto state N.
    **
    **   YYNSTATE <= N < YYNSTATE+YYNRULE   Reduce by rule N-YYNSTATE.
    **
    **   N == YYNSTATE+YYNRULE              A syntax error has occurred.
    **
    **   N == YYNSTATE+YYNRULE+1            The parser accepts its input.
    **
    **   N == YYNSTATE+YYNRULE+2            No such action.  Denotes unused
    **                                      slots in the yy_action[] table.
    **
    ** The action table is constructed as a single large table named yy_action[].
    ** Given state S and lookahead X, the action is computed as
    **
    **      yy_action[ yy_shift_ofst[S] + X ]
    **
    ** If the index value yy_shift_ofst[S]+X is out of range or if the value
    ** yy_lookahead[yy_shift_ofst[S]+X] is not equal to X or if yy_shift_ofst[S]
    ** is equal to YY_SHIFT_USE_DFLT, it means that the action is not in the table
    ** and that yy_default[S] should be used instead.
    **
    ** The formula above is for computing the action when the lookahead is
    ** a terminal symbol.  If the lookahead is a non-terminal (as occurs after
    ** a reduce action) then the yy_reduce_ofst[] array is used in place of
    ** the yy_shift_ofst[] array and YY_REDUCE_USE_DFLT is used in place of
    ** YY_SHIFT_USE_DFLT.
    **
    ** The following are the tables generated in this section:
    **
    **  yy_action[]        A single table containing all actions.
    **  yy_lookahead[]     A table containing the lookahead for each entry in
    **                     yy_action.  Used to detect hash collisions.
    **  yy_shift_ofst[]    For each state, the offset into yy_action for
    **                     shifting terminals.
    **  yy_reduce_ofst[]   For each state, the offset into yy_action for
    **                     shifting non-terminals after a reduce.
    **  yy_default[]       Default action for each state.
    */
    static YYACTIONTYPE[] yy_action = new YYACTIONTYPE[]{
/*     0 */   309,  959,  178,  628,    2,  153,  216,  448,   24,   24,
/*    10 */    24,   24,  497,   26,   26,   26,   26,   27,   27,   28,
/*    20 */    28,   28,   29,  218,  422,  423,  214,  422,  423,  455,
/*    30 */   461,   31,   26,   26,   26,   26,   27,   27,   28,   28,
/*    40 */    28,   29,  218,   30,  492,   32,  137,   23,   22,  315,
/*    50 */   465,  466,  462,  462,   25,   25,   24,   24,   24,   24,
/*    60 */   445,   26,   26,   26,   26,   27,   27,   28,   28,   28,
/*    70 */    29,  218,  309,  218,  318,  448,  521,  499,   45,   26,
/*    80 */    26,   26,   26,   27,   27,   28,   28,   28,   29,  218,
/*    90 */   422,  423,  425,  426,  159,  425,  426,  366,  369,  370,
/*   100 */   318,  455,  461,  394,  523,   21,  188,  504,  371,   27,
/*   110 */    27,   28,   28,   28,   29,  218,  422,  423,  424,   23,
/*   120 */    22,  315,  465,  466,  462,  462,   25,   25,   24,   24,
/*   130 */    24,   24,  564,   26,   26,   26,   26,   27,   27,   28,
/*   140 */    28,   28,   29,  218,  309,  230,  513,  138,  477,  220,
/*   150 */   557,  148,  135,  260,  364,  265,  365,  156,  425,  426,
/*   160 */   245,  610,  337,   30,  269,   32,  137,  448,  608,  609,
/*   170 */   233,  230,  499,  455,  461,   57,  515,  334,  135,  260,
/*   180 */   364,  265,  365,  156,  425,  426,  444,   78,  417,  414,
/*   190 */   269,   23,   22,  315,  465,  466,  462,  462,   25,   25,
/*   200 */    24,   24,   24,   24,  348,   26,   26,   26,   26,   27,
/*   210 */    27,   28,   28,   28,   29,  218,  309,  216,  543,  556,
/*   220 */   486,  130,  498,  607,   30,  337,   32,  137,  351,  396,
/*   230 */   438,   63,  337,  361,  424,  448,  487,  337,  424,  544,
/*   240 */   334,  217,  195,  606,  605,  455,  461,  334,   18,  444,
/*   250 */    85,  488,  334,  347,  192,  565,  444,   78,  316,  472,
/*   260 */   473,  444,   85,   23,   22,  315,  465,  466,  462,  462,
/*   270 */    25,   25,   24,   24,   24,   24,  445,   26,   26,   26,
/*   280 */    26,   27,   27,   28,   28,   28,   29,  218,  309,  353,
/*   290 */   223,  320,  607,  193,  238,  337,  481,   16,  351,  185,
/*   300 */   330,  419,  222,  350,  604,  219,  215,  424,  112,  337,
/*   310 */   334,  157,  606,  408,  213,  563,  538,  455,  461,  444,
/*   320 */    79,  219,  562,  524,  334,  576,  522,  629,  417,  414,
/*   330 */   450,  581,  441,  444,   78,   23,   22,  315,  465,  466,
/*   340 */   462,  462,   25,   25,   24,   24,   24,   24,  445,   26,
/*   350 */    26,   26,   26,   27,   27,   28,   28,   28,   29,  218,
/*   360 */   309,  452,  452,  452,  159,  399,  311,  366,  369,  370,
/*   370 */   337,  251,  404,  407,  219,  355,  556,    4,  371,  422,
/*   380 */   423,  397,  286,  285,  244,  334,  540,  566,   63,  455,
/*   390 */   461,  424,  216,  478,  444,   93,   28,   28,   28,   29,
/*   400 */   218,  413,  477,  220,  578,   40,  545,   23,   22,  315,
/*   410 */   465,  466,  462,  462,   25,   25,   24,   24,   24,   24,
/*   420 */   582,   26,   26,   26,   26,   27,   27,   28,   28,   28,
/*   430 */    29,  218,  309,  546,  337,   30,  517,   32,  137,  378,
/*   440 */   326,  337,  874,  153,  194,  448,    1,  425,  426,  334,
/*   450 */   422,  423,  422,  423,   29,  218,  334,  613,  444,   71,
/*   460 */   210,  455,  461,   66,  581,  444,   93,  422,  423,  626,
/*   470 */   949,  303,  949,  500,  479,  555,  202,   43,  445,   23,
/*   480 */    22,  315,  465,  466,  462,  462,   25,   25,   24,   24,
/*   490 */    24,   24,  436,   26,   26,   26,   26,   27,   27,   28,
/*   500 */    28,   28,   29,  218,  309,  187,  211,  360,  520,  440,
/*   510 */   246,  327,  622,  448,  397,  286,  285,  551,  425,  426,
/*   520 */   425,  426,  334,  159,  337,  216,  366,  369,  370,  494,
/*   530 */   556,  444,    9,  455,  461,  425,  426,  371,  495,  334,
/*   540 */   445,  618,   63,  504,  198,  424,  501,  449,  444,   72,
/*   550 */   474,   23,   22,  315,  465,  466,  462,  462,   25,   25,
/*   560 */    24,   24,   24,   24,  395,   26,   26,   26,   26,   27,
/*   570 */    27,   28,   28,   28,   29,  218,  309,  486,  445,  337,
/*   580 */   537,   60,  224,  479,  343,  202,  398,  337,  439,  554,
/*   590 */   199,  140,  337,  487,  334,  526,  527,  551,  516,  508,
/*   600 */   456,  457,  334,  444,   67,  455,  461,  334,  488,  476,
/*   610 */   528,  444,   76,   39,  424,   41,  444,   97,  579,  527,
/*   620 */   529,  459,  460,   23,   22,  315,  465,  466,  462,  462,
/*   630 */    25,   25,   24,   24,   24,   24,  337,   26,   26,   26,
/*   640 */    26,   27,   27,   28,   28,   28,   29,  218,  309,  337,
/*   650 */   458,  334,  272,  621,  307,  337,  312,  337,  374,   64,
/*   660 */   444,   96,  317,  448,  334,  342,  472,  473,  469,  337,
/*   670 */   334,  508,  334,  444,  101,  359,  252,  455,  461,  444,
/*   680 */    99,  444,  104,  358,  334,  345,  424,  340,  157,  468,
/*   690 */   468,  424,  493,  444,  105,   23,   22,  315,  465,  466,
/*   700 */   462,  462,   25,   25,   24,   24,   24,   24,  337,   26,
/*   710 */    26,   26,   26,   27,   27,   28,   28,   28,   29,  218,
/*   720 */   309,  337,  181,  334,  499,   56,  139,  337,  219,  268,
/*   730 */   384,  448,  444,  129,  382,  387,  334,  168,  337,  389,
/*   740 */   508,  424,  334,  311,  424,  444,  131,  496,  269,  455,
/*   750 */   461,  444,   59,  334,  424,  424,  391,  340,    8,  468,
/*   760 */   468,  263,  444,  102,  390,  290,  321,   23,   22,  315,
/*   770 */   465,  466,  462,  462,   25,   25,   24,   24,   24,   24,
/*   780 */   337,   26,   26,   26,   26,   27,   27,   28,   28,   28,
/*   790 */    29,  218,  309,  337,  138,  334,  416,    2,  268,  337,
/*   800 */   389,  337,  443,  325,  444,   77,  442,  293,  334,  291,
/*   810 */     7,  482,  337,  424,  334,  424,  334,  444,  100,  499,
/*   820 */   339,  455,  461,  444,   68,  444,   98,  334,  254,  504,
/*   830 */   232,  626,  948,  504,  948,  231,  444,  132,   47,   23,
/*   840 */    22,  315,  465,  466,  462,  462,   25,   25,   24,   24,
/*   850 */    24,   24,  337,   26,   26,   26,   26,   27,   27,   28,
/*   860 */    28,   28,   29,  218,  309,  337,  280,  334,  256,  538,
/*   870 */   362,  337,  258,  268,  622,  549,  444,  133,  203,  140,
/*   880 */   334,  424,  548,  337,  180,  158,  334,  292,  424,  444,
/*   890 */   134,  287,  552,  455,  461,  444,   69,  443,  334,  463,
/*   900 */   340,  442,  468,  468,  427,  428,  429,  444,   80,  281,
/*   910 */   322,   23,   33,  315,  465,  466,  462,  462,   25,   25,
/*   920 */    24,   24,   24,   24,  337,   26,   26,   26,   26,   27,
/*   930 */    27,   28,   28,   28,   29,  218,  309,  337,  406,  334,
/*   940 */   212,  268,  550,  337,  268,  389,  329,  177,  444,   81,
/*   950 */   542,  541,  334,  475,  475,  337,  424,  216,  334,  424,
/*   960 */   424,  444,   70,  535,  368,  455,  461,  444,   82,  405,
/*   970 */   334,  261,  392,  340,  445,  468,  468,  587,  323,  444,
/*   980 */    83,  324,  262,  288,   22,  315,  465,  466,  462,  462,
/*   990 */    25,   25,   24,   24,   24,   24,  337,   26,   26,   26,
/*  1000 */    26,   27,   27,   28,   28,   28,   29,  218,  309,  337,
/*  1010 */   211,  334,  294,  356,  340,  337,  468,  468,  532,  533,
/*  1020 */   444,   84,  403,  144,  334,  574,  600,  337,  424,  573,
/*  1030 */   334,  337,  420,  444,   86,  253,  234,  455,  461,  444,
/*  1040 */    87,  430,  334,  383,  445,  431,  334,  274,  196,  331,
/*  1050 */   424,  444,   88,  432,  145,  444,   73,  315,  465,  466,
/*  1060 */   462,  462,   25,   25,   24,   24,   24,   24,  395,   26,
/*  1070 */    26,   26,   26,   27,   27,   28,   28,   28,   29,  218,
/*  1080 */    35,  344,  445,    3,  337,  394,  337,  333,  423,  278,
/*  1090 */   388,  276,  280,  207,  147,   35,  344,  341,    3,  334,
/*  1100 */   424,  334,  333,  423,  308,  623,  280,  424,  444,   74,
/*  1110 */   444,   89,  341,  337,    6,  346,  338,  337,  421,  337,
/*  1120 */   470,  424,   65,  332,  280,  481,  446,  445,  334,  247,
/*  1130 */   346,  424,  334,  424,  334,  594,  280,  444,   90,  424,
/*  1140 */   481,  444,   91,  444,   92,   38,   37,  625,  337,  410,
/*  1150 */    47,  424,  237,  280,   36,  335,  336,  354,  248,  450,
/*  1160 */    38,   37,  514,  334,  572,  381,  572,  596,  424,   36,
/*  1170 */   335,  336,  444,   75,  450,  200,  506,  216,  154,  597,
/*  1180 */   239,  240,  241,  146,  243,  249,  547,  593,  158,  433,
/*  1190 */   452,  452,  452,  453,  454,   10,  598,  280,   20,   46,
/*  1200 */   174,  412,  298,  337,  424,  452,  452,  452,  453,  454,
/*  1210 */    10,  299,  424,   35,  344,  352,    3,  250,  334,  434,
/*  1220 */   333,  423,  337,  172,  280,  581,  208,  444,   17,  171,
/*  1230 */   341,   19,  173,  447,  424,  422,  423,  334,  337,  424,
/*  1240 */   235,  280,  204,  205,  206,   42,  444,   94,  346,  435,
/*  1250 */   136,  451,  221,  334,  308,  624,  424,  349,  481,  490,
/*  1260 */   445,  152,  444,   95,  424,  424,  424,  236,  503,  491,
/*  1270 */   507,  179,  424,  481,  424,  402,  295,  285,   38,   37,
/*  1280 */   271,  310,  158,  424,  296,  424,  216,   36,  335,  336,
/*  1290 */   509,  266,  450,  190,  191,  539,  267,  625,  558,  273,
/*  1300 */   275,   48,  277,  522,  279,  424,  424,  450,  255,  409,
/*  1310 */   424,  424,  257,  424,  424,  424,  284,  424,  386,  424,
/*  1320 */   357,  584,  585,  452,  452,  452,  453,  454,   10,  259,
/*  1330 */   393,  424,  289,  424,  592,  603,  424,  424,  452,  452,
/*  1340 */   452,  297,  300,  301,  505,  424,  617,  424,  363,  424,
/*  1350 */   424,  373,  577,  158,  158,  511,  424,  424,  424,  525,
/*  1360 */   588,  424,  154,  589,  601,   54,   54,  620,  512,  306,
/*  1370 */   319,  530,  531,  535,  264,  107,  228,  536,  534,  375,
/*  1380 */   559,  304,  560,  561,  305,  227,  229,  553,  567,  161,
/*  1390 */   162,  379,  377,  163,   51,  209,  569,  282,  164,  570,
/*  1400 */   385,  143,  580,  116,  119,  183,  400,  590,  401,  121,
/*  1410 */   122,  123,  124,  126,  599,  328,  614,   55,   58,  615,
/*  1420 */   616,  619,   62,  418,  103,  226,  111,  176,  242,  182,
/*  1430 */   437,  313,  201,  314,  670,  671,  672,  149,  150,  467,
/*  1440 */   464,   34,  483,  471,  480,  184,  197,  502,  484,    5,
/*  1450 */   485,  151,  489,   44,  141,   11,  106,  160,  225,  518,
/*  1460 */   519,   49,  510,  108,  367,  270,   12,  155,  109,   50,
/*  1470 */   110,  262,  376,  186,  568,  113,  142,  154,  165,  115,
/*  1480 */    15,  283,  583,  166,  167,  380,  586,  117,   13,  120,
/*  1490 */   372,   52,   53,  118,  591,  169,  114,  170,  595,  125,
/*  1500 */   127,  571,  575,  602,   14,  128,  611,  612,   61,  175,
/*  1510 */   189,  415,  302,  627,  960,  960,  960,  960,  411,
};
    static YYCODETYPE[] yy_lookahead = new YYCODETYPE[]{
/*     0 */    19,  142,  143,  144,  145,   24,  116,   26,   75,   76,
/*    10 */    77,   78,   25,   80,   81,   82,   83,   84,   85,   86,
/*    20 */    87,   88,   89,   90,   26,   27,  160,   26,   27,   48,
/*    30 */    49,   79,   80,   81,   82,   83,   84,   85,   86,   87,
/*    40 */    88,   89,   90,  222,  223,  224,  225,   66,   67,   68,
/*    50 */    69,   70,   71,   72,   73,   74,   75,   76,   77,   78,
/*    60 */   194,   80,   81,   82,   83,   84,   85,   86,   87,   88,
/*    70 */    89,   90,   19,   90,   19,   94,  174,   25,   25,   80,
/*    80 */    81,   82,   83,   84,   85,   86,   87,   88,   89,   90,
/*    90 */    26,   27,   94,   95,   96,   94,   95,   99,  100,  101,
/*   100 */    19,   48,   49,  150,  174,   52,  119,  166,  110,   84,
/*   110 */    85,   86,   87,   88,   89,   90,   26,   27,  165,   66,
/*   120 */    67,   68,   69,   70,   71,   72,   73,   74,   75,   76,
/*   130 */    77,   78,  186,   80,   81,   82,   83,   84,   85,   86,
/*   140 */    87,   88,   89,   90,   19,   90,  205,   95,   84,   85,
/*   150 */   186,   96,   97,   98,   99,  100,  101,  102,   94,   95,
/*   160 */   195,   97,  150,  222,  109,  224,  225,   26,  104,  105,
/*   170 */   217,   90,  120,   48,   49,   50,   86,  165,   97,   98,
/*   180 */    99,  100,  101,  102,   94,   95,  174,  175,    1,    2,
/*   190 */   109,   66,   67,   68,   69,   70,   71,   72,   73,   74,
/*   200 */    75,   76,   77,   78,  191,   80,   81,   82,   83,   84,
/*   210 */    85,   86,   87,   88,   89,   90,   19,  116,   35,  150,
/*   220 */    12,   24,  208,  150,  222,  150,  224,  225,  216,  128,
/*   230 */   161,  162,  150,  221,  165,   94,   28,  150,  165,   56,
/*   240 */   165,  197,  160,  170,  171,   48,   49,  165,  204,  174,
/*   250 */   175,   43,  165,   45,  185,  186,  174,  175,  169,  170,
/*   260 */   171,  174,  175,   66,   67,   68,   69,   70,   71,   72,
/*   270 */    73,   74,   75,   76,   77,   78,  194,   80,   81,   82,
/*   280 */    83,   84,   85,   86,   87,   88,   89,   90,   19,  214,
/*   290 */   215,  108,  150,   25,  148,  150,   64,   22,  216,   24,
/*   300 */   146,  147,  215,  221,  231,  232,  152,  165,  154,  150,
/*   310 */   165,   49,  170,  171,  160,  181,  182,   48,   49,  174,
/*   320 */   175,  232,  188,  165,  165,   21,   94,    0,    1,    2,
/*   330 */    98,   55,  174,  174,  175,   66,   67,   68,   69,   70,
/*   340 */    71,   72,   73,   74,   75,   76,   77,   78,  194,   80,
/*   350 */    81,   82,   83,   84,   85,   86,   87,   88,   89,   90,
/*   360 */    19,  129,  130,  131,   96,   61,  104,   99,  100,  101,
/*   370 */   150,  226,  218,  231,  232,  216,  150,  196,  110,   26,
/*   380 */    27,  105,  106,  107,  158,  165,  183,  161,  162,   48,
/*   390 */    49,  165,  116,  166,  174,  175,   86,   87,   88,   89,
/*   400 */    90,  247,   84,   85,  100,  136,  183,   66,   67,   68,
/*   410 */    69,   70,   71,   72,   73,   74,   75,   76,   77,   78,
/*   420 */    11,   80,   81,   82,   83,   84,   85,   86,   87,   88,
/*   430 */    89,   90,   19,  183,  150,  222,   23,  224,  225,  237,
/*   440 */   220,  150,  138,   24,  160,   26,   22,   94,   95,  165,
/*   450 */    26,   27,   26,   27,   89,   90,  165,  244,  174,  175,
/*   460 */   236,   48,   49,   22,   55,  174,  175,   26,   27,   22,
/*   470 */    23,  163,   25,  120,  166,  167,  168,  136,  194,   66,
/*   480 */    67,   68,   69,   70,   71,   72,   73,   74,   75,   76,
/*   490 */    77,   78,  153,   80,   81,   82,   83,   84,   85,   86,
/*   500 */    87,   88,   89,   90,   19,  196,  160,  150,   23,  173,
/*   510 */   198,  220,   65,   94,  105,  106,  107,  181,   94,   95,
/*   520 */    94,   95,  165,   96,  150,  116,   99,  100,  101,   31,
/*   530 */   150,  174,  175,   48,   49,   94,   95,  110,   40,  165,
/*   540 */   194,  161,  162,  166,  160,  165,  120,  166,  174,  175,
/*   550 */   233,   66,   67,   68,   69,   70,   71,   72,   73,   74,
/*   560 */    75,   76,   77,   78,  218,   80,   81,   82,   83,   84,
/*   570 */    85,   86,   87,   88,   89,   90,   19,   12,  194,  150,
/*   580 */    23,  235,  205,  166,  167,  168,  240,  150,  172,  173,
/*   590 */   206,  207,  150,   28,  165,  190,  191,  181,   23,  150,
/*   600 */    48,   49,  165,  174,  175,   48,   49,  165,   43,  233,
/*   610 */    45,  174,  175,  135,  165,  137,  174,  175,  190,  191,
/*   620 */    55,   69,   70,   66,   67,   68,   69,   70,   71,   72,
/*   630 */    73,   74,   75,   76,   77,   78,  150,   80,   81,   82,
/*   640 */    83,   84,   85,   86,   87,   88,   89,   90,   19,  150,
/*   650 */    98,  165,   23,  250,  251,  150,  155,  150,   19,   22,
/*   660 */   174,  175,  213,   26,  165,  169,  170,  171,   23,  150,
/*   670 */   165,  150,  165,  174,  175,   19,  150,   48,   49,  174,
/*   680 */   175,  174,  175,   27,  165,  228,  165,  112,   49,  114,
/*   690 */   115,  165,  177,  174,  175,   66,   67,   68,   69,   70,
/*   700 */    71,   72,   73,   74,   75,   76,   77,   78,  150,   80,
/*   710 */    81,   82,   83,   84,   85,   86,   87,   88,   89,   90,
/*   720 */    19,  150,   23,  165,   25,   24,  150,  150,  232,  150,
/*   730 */   229,   94,  174,  175,  213,  234,  165,   25,  150,  150,
/*   740 */   150,  165,  165,  104,  165,  174,  175,  177,  109,   48,
/*   750 */    49,  174,  175,  165,  165,  165,   19,  112,   22,  114,
/*   760 */   115,  177,  174,  175,   27,   16,  187,   66,   67,   68,
/*   770 */    69,   70,   71,   72,   73,   74,   75,   76,   77,   78,
/*   780 */   150,   80,   81,   82,   83,   84,   85,   86,   87,   88,
/*   790 */    89,   90,   19,  150,   95,  165,  144,  145,  150,  150,
/*   800 */   150,  150,  113,  213,  174,  175,  117,   58,  165,   60,
/*   810 */    74,   23,  150,  165,  165,  165,  165,  174,  175,  120,
/*   820 */    19,   48,   49,  174,  175,  174,  175,  165,  209,  166,
/*   830 */   241,   22,   23,  166,   25,  187,  174,  175,  126,   66,
/*   840 */    67,   68,   69,   70,   71,   72,   73,   74,   75,   76,
/*   850 */    77,   78,  150,   80,   81,   82,   83,   84,   85,   86,
/*   860 */    87,   88,   89,   90,   19,  150,  150,  165,  205,  182,
/*   870 */    86,  150,  205,  150,   65,  166,  174,  175,  206,  207,
/*   880 */   165,  165,  177,  150,   23,   25,  165,  138,  165,  174,
/*   890 */   175,  241,  166,   48,   49,  174,  175,  113,  165,   98,
/*   900 */   112,  117,  114,  115,    7,    8,    9,  174,  175,  193,
/*   910 */   187,   66,   67,   68,   69,   70,   71,   72,   73,   74,
/*   920 */    75,   76,   77,   78,  150,   80,   81,   82,   83,   84,
/*   930 */    85,   86,   87,   88,   89,   90,   19,  150,   97,  165,
/*   940 */   160,  150,  177,  150,  150,  150,  248,  249,  174,  175,
/*   950 */    97,   98,  165,  129,  130,  150,  165,  116,  165,  165,
/*   960 */   165,  174,  175,  103,  178,   48,   49,  174,  175,  128,
/*   970 */   165,   98,  242,  112,  194,  114,  115,  199,  187,  174,
/*   980 */   175,  187,  109,  242,   67,   68,   69,   70,   71,   72,
/*   990 */    73,   74,   75,   76,   77,   78,  150,   80,   81,   82,
/*  1000 */    83,   84,   85,   86,   87,   88,   89,   90,   19,  150,
/*  1010 */   160,  165,  209,  150,  112,  150,  114,  115,    7,    8,
/*  1020 */   174,  175,  209,    6,  165,   29,  199,  150,  165,   33,
/*  1030 */   165,  150,  149,  174,  175,  150,  241,   48,   49,  174,
/*  1040 */   175,  149,  165,   47,  194,  149,  165,   16,  160,  149,
/*  1050 */   165,  174,  175,   13,  151,  174,  175,   68,   69,   70,
/*  1060 */    71,   72,   73,   74,   75,   76,   77,   78,  218,   80,
/*  1070 */    81,   82,   83,   84,   85,   86,   87,   88,   89,   90,
/*  1080 */    19,   20,  194,   22,  150,  150,  150,   26,   27,   58,
/*  1090 */   240,   60,  150,  160,  151,   19,   20,   36,   22,  165,
/*  1100 */   165,  165,   26,   27,   22,   23,  150,  165,  174,  175,
/*  1110 */   174,  175,   36,  150,   25,   54,  150,  150,  150,  150,
/*  1120 */    23,  165,   25,  159,  150,   64,  194,  194,  165,  199,
/*  1130 */    54,  165,  165,  165,  165,  193,  150,  174,  175,  165,
/*  1140 */    64,  174,  175,  174,  175,   84,   85,   65,  150,  193,
/*  1150 */   126,  165,  217,  150,   93,   94,   95,  123,  200,   98,
/*  1160 */    84,   85,   86,  165,  105,  106,  107,  193,  165,   93,
/*  1170 */    94,   95,  174,  175,   98,    5,   23,  116,   25,  193,
/*  1180 */    10,   11,   12,   13,   14,  201,   23,   17,   25,  150,
/*  1190 */   129,  130,  131,  132,  133,  134,  193,  150,  125,  124,
/*  1200 */    30,  245,   32,  150,  165,  129,  130,  131,  132,  133,
/*  1210 */   134,   41,  165,   19,   20,  122,   22,  202,  165,  150,
/*  1220 */    26,   27,  150,   53,  150,   55,  160,  174,  175,   59,
/*  1230 */    36,   22,   62,  203,  165,   26,   27,  165,  150,  165,
/*  1240 */   193,  150,  105,  106,  107,  135,  174,  175,   54,  150,
/*  1250 */   150,  150,  227,  165,   22,   23,  165,  150,   64,  150,
/*  1260 */   194,  118,  174,  175,  165,  165,  165,  193,  150,  157,
/*  1270 */   150,  157,  165,   64,  165,  105,  106,  107,   84,   85,
/*  1280 */    23,  111,   25,  165,  193,  165,  116,   93,   94,   95,
/*  1290 */   150,  150,   98,   84,   85,  150,  150,   65,  150,  150,
/*  1300 */   150,  104,  150,   94,  150,  165,  165,   98,  210,  139,
/*  1310 */   165,  165,  210,  165,  165,  165,  150,  165,  150,  165,
/*  1320 */   121,  150,  150,  129,  130,  131,  132,  133,  134,  210,
/*  1330 */   150,  165,  150,  165,  150,  150,  165,  165,  129,  130,
/*  1340 */   131,  150,  150,  150,  211,  165,  150,  165,  104,  165,
/*  1350 */   165,   23,   23,   25,   25,  211,  165,  165,  165,  176,
/*  1360 */    23,  165,   25,   23,   23,   25,   25,   23,  211,   25,
/*  1370 */    46,  176,  184,  103,  176,   22,   90,  176,  178,   18,
/*  1380 */   176,  179,  176,  176,  179,  230,  230,  184,  157,  156,
/*  1390 */   156,   44,  157,  156,  135,  157,  157,  238,  156,  239,
/*  1400 */   157,   66,  189,  189,   22,  219,  157,  199,   18,  192,
/*  1410 */   192,  192,  192,  189,  199,  157,   39,  243,  243,  157,
/*  1420 */   157,   37,  246,    1,  164,  180,  180,  249,   15,  219,
/*  1430 */    23,  252,   22,  252,  118,  118,  118,  118,  118,  113,
/*  1440 */    98,   22,   11,   23,   23,   22,   22,  120,   23,   34,
/*  1450 */    23,   25,   23,   25,  118,   25,   22,  102,   50,   23,
/*  1460 */    23,   22,   27,   22,   50,   23,   34,   34,   22,   22,
/*  1470 */    22,  109,   19,   24,   20,  104,   38,   25,  104,   22,
/*  1480 */     5,  138,    1,  118,   34,   42,   27,  108,   22,  119,
/*  1490 */    50,   74,   74,  127,    1,   16,   51,  121,   20,  119,
/*  1500 */   108,   57,   51,  128,   22,  127,   23,   23,   16,   15,
/*  1510 */    22,    3,  140,    4,  253,  253,  253,  253,   63,
};
    const int YY_SHIFT_USE_DFLT = (-111);
    const int YY_SHIFT_MAX = 415;
    static short[] yy_shift_ofst = new short[]{
/*     0 */   187, 1061, 1170, 1061, 1194, 1194,   -2,   64,   64,  -19,
/*    10 */  1194, 1194, 1194, 1194, 1194,  276,    1,  125, 1076, 1194,
/*    20 */  1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194,
/*    30 */  1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194,
/*    40 */  1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194,
/*    50 */  1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194, 1194,  -48,
/*    60 */   409,    1,    1,  141,  318,  318, -110,   53,  197,  269,
/*    70 */   341,  413,  485,  557,  629,  701,  773,  845,  773,  773,
/*    80 */   773,  773,  773,  773,  773,  773,  773,  773,  773,  773,
/*    90 */   773,  773,  773,  773,  773,  773,  917,  989,  989,  -67,
/*   100 */   -67,   -1,   -1,   55,   25,  310,    1,    1,    1,    1,
/*   110 */     1,  639,  304,    1,    1,    1,    1,    1,    1,    1,
/*   120 */     1,    1,    1,    1,    1,    1,    1,    1,    1,  365,
/*   130 */   141,  -17, -111, -111, -111, 1209,   81,  424,  353,  426,
/*   140 */   441,   90,  565,  565,    1,    1,    1,    1,    1,    1,
/*   150 */     1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
/*   160 */     1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
/*   170 */     1,    1,    1,    1,    1,    1,  447,  809,  327,  419,
/*   180 */   419,  419,  841,  101, -110, -110, -110, -111, -111, -111,
/*   190 */   232,  232,  268,  427,  575,  645,  788,  208,  861,  699,
/*   200 */   897,  784,  637,   52,  183,  183,  183,  902,  902,  996,
/*   210 */  1059,  902,  902,  902,  902,  275,  689,  -13,  141,  824,
/*   220 */   824,  478,  498,  498,  656,  498,  262,  498,  141,  498,
/*   230 */   141,  860,  737,  712,  737,  656,  656,  712, 1017, 1017,
/*   240 */  1017, 1017, 1040, 1040, 1089, -110, 1024, 1034, 1075, 1093,
/*   250 */  1073, 1110, 1143, 1143, 1197, 1199, 1197, 1199, 1197, 1199,
/*   260 */  1244, 1244, 1324, 1244, 1270, 1244, 1353, 1286, 1286, 1324,
/*   270 */  1244, 1244, 1244, 1353, 1361, 1143, 1361, 1143, 1361, 1143,
/*   280 */  1143, 1347, 1259, 1361, 1143, 1335, 1335, 1382, 1024, 1143,
/*   290 */  1390, 1390, 1390, 1390, 1024, 1335, 1382, 1143, 1377, 1377,
/*   300 */  1143, 1143, 1384, -111, -111, -111, -111, -111, -111,  552,
/*   310 */   749, 1137, 1031, 1082, 1232,  801, 1097, 1153,  873, 1011,
/*   320 */   853, 1163, 1257, 1328, 1329, 1337, 1340, 1341,  736, 1344,
/*   330 */  1422, 1413, 1407, 1410, 1316, 1317, 1318, 1319, 1320, 1342,
/*   340 */  1326, 1419, 1420, 1421, 1423, 1431, 1424, 1425, 1426, 1427,
/*   350 */  1429, 1428, 1415, 1430, 1432, 1428, 1327, 1434, 1433, 1435,
/*   360 */  1336, 1436, 1437, 1438, 1408, 1439, 1414, 1441, 1442, 1446,
/*   370 */  1447, 1440, 1448, 1355, 1362, 1453, 1454, 1449, 1371, 1443,
/*   380 */  1444, 1445, 1452, 1451, 1343, 1374, 1457, 1475, 1481, 1365,
/*   390 */  1450, 1459, 1379, 1417, 1418, 1366, 1466, 1370, 1493, 1479,
/*   400 */  1376, 1478, 1380, 1392, 1378, 1482, 1375, 1483, 1484, 1492,
/*   410 */  1455, 1494, 1372, 1488, 1508, 1509,
};
    const int YY_REDUCE_USE_DFLT = (-180);
    const int YY_REDUCE_MAX = 308;
    static short[] yy_reduce_ofst = new short[]{
/*     0 */  -141,   82,  154,  284,   12,   75,   69,   73,  142,  -59,
/*    10 */   145,   87,  159,  220,  291,  346,  226,  213,  357,  374,
/*    20 */   429,  437,  442,  486,  499,  505,  507,  519,  558,  571,
/*    30 */   577,  588,  630,  643,  649,  651,  662,  702,  715,  721,
/*    40 */   733,  774,  787,  793,  805,  846,  859,  865,  877,  881,
/*    50 */   934,  936,  963,  967,  969,  998, 1053, 1072, 1088, -179,
/*    60 */   850,  956,  380,  308,   89,  496,  384,    2,    2,    2,
/*    70 */     2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
/*    80 */     2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
/*    90 */     2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
/*   100 */     2,    2,    2,  416,    2,    2,  449,  579,  648,  723,
/*   110 */   791,  134,  501,  716,  521,  794,  589,  -47,  650,  590,
/*   120 */   795,  942,  974,  986, 1003, 1047, 1074,  935, 1091,    2,
/*   130 */   417,    2,    2,    2,    2,  158,  336,  526,  576,  863,
/*   140 */   885,  966,  405,  428,  968, 1039, 1069, 1099, 1100,  966,
/*   150 */  1101, 1107, 1109, 1118, 1120, 1140, 1141, 1145, 1146, 1148,
/*   160 */  1149, 1150, 1152, 1154, 1166, 1168, 1171, 1172, 1180, 1182,
/*   170 */  1184, 1185, 1191, 1192, 1193, 1196,  403,  403,  652,  377,
/*   180 */   663,  667, -134,  780,  888,  933, 1066,   44,  672,  698,
/*   190 */   -98,  -70,  -54,  -36,  -35,  -35,  -35,   13,  -35,   14,
/*   200 */   146,  181,  227,   14,  203,  223,  250,  -35,  -35,  224,
/*   210 */   202,  -35,  -35,  -35,  -35,  339,  309,  312,  381,  317,
/*   220 */   376,  457,  515,  570,  619,  584,  687,  705,  709,  765,
/*   230 */   726,  786,  730,  778,  741,  803,  813,  827,  883,  892,
/*   240 */   896,  900,  903,  943,  964,  932,  930,  958,  984, 1015,
/*   250 */  1030, 1025, 1112, 1114, 1098, 1133, 1102, 1144, 1119, 1157,
/*   260 */  1183, 1195, 1188, 1198, 1200, 1201, 1202, 1155, 1156, 1203,
/*   270 */  1204, 1206, 1207, 1205, 1233, 1231, 1234, 1235, 1237, 1238,
/*   280 */  1239, 1159, 1160, 1242, 1243, 1213, 1214, 1186, 1208, 1249,
/*   290 */  1217, 1218, 1219, 1220, 1215, 1224, 1210, 1258, 1174, 1175,
/*   300 */  1262, 1263, 1176, 1260, 1245, 1246, 1178, 1179, 1181,
};
    static YYACTIONTYPE[] yy_default = new YYACTIONTYPE[] {
/*     0 */   634,  869,  958,  958,  869,  958,  958,  898,  898,  757,
/*    10 */   867,  958,  958,  958,  958,  958,  958,  932,  958,  958,
/*    20 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*    30 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*    40 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*    50 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  841,
/*    60 */   958,  958,  958,  673,  898,  898,  761,  792,  958,  958,
/*    70 */   958,  958,  958,  958,  958,  958,  793,  958,  871,  866,
/*    80 */   862,  864,  863,  870,  794,  783,  790,  797,  772,  911,
/*    90 */   799,  800,  806,  807,  933,  931,  829,  828,  847,  831,
/*   100 */   853,  830,  840,  665,  832,  833,  958,  958,  958,  958,
/*   110 */   958,  726,  660,  958,  958,  958,  958,  958,  958,  958,
/*   120 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  834,
/*   130 */   958,  835,  848,  849,  850,  958,  958,  958,  958,  958,
/*   140 */   958,  958,  958,  958,  640,  958,  958,  958,  958,  958,
/*   150 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*   160 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*   170 */   958,  882,  958,  936,  938,  958,  958,  958,  634,  757,
/*   180 */   757,  757,  958,  958,  958,  958,  958,  751,  761,  950,
/*   190 */   958,  958,  717,  958,  958,  958,  958,  958,  958,  958,
/*   200 */   642,  749,  675,  759,  958,  958,  958,  662,  738,  904,
/*   210 */   958,  923,  921,  740,  802,  958,  749,  758,  958,  958,
/*   220 */   958,  865,  786,  786,  774,  786,  696,  786,  958,  786,
/*   230 */   958,  699,  916,  796,  916,  774,  774,  796,  639,  639,
/*   240 */   639,  639,  650,  650,  716,  958,  796,  787,  789,  779,
/*   250 */   791,  958,  765,  765,  773,  778,  773,  778,  773,  778,
/*   260 */   728,  728,  713,  728,  699,  728,  875,  879,  879,  713,
/*   270 */   728,  728,  728,  875,  657,  765,  657,  765,  657,  765,
/*   280 */   765,  908,  910,  657,  765,  730,  730,  808,  796,  765,
/*   290 */   737,  737,  737,  737,  796,  730,  808,  765,  935,  935,
/*   300 */   765,  765,  943,  683,  701,  701,  950,  955,  955,  958,
/*   310 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*   320 */   958,  958,  958,  958,  958,  958,  958,  958,  884,  958,
/*   330 */   958,  648,  958,  667,  815,  820,  816,  958,  817,  958,
/*   340 */   743,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*   350 */   958,  868,  958,  780,  958,  788,  958,  958,  958,  958,
/*   360 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*   370 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*   380 */   958,  906,  907,  958,  958,  958,  958,  958,  958,  914,
/*   390 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*   400 */   958,  958,  958,  958,  958,  958,  958,  958,  958,  958,
/*   410 */   942,  958,  958,  945,  635,  958,  630,  632,  633,  637,
/*   420 */   638,  641,  667,  668,  670,  671,  672,  643,  644,  645,
/*   430 */   646,  647,  649,  653,  651,  652,  654,  661,  663,  682,
/*   440 */   684,  686,  747,  748,  812,  741,  742,  746,  669,  823,
/*   450 */   814,  818,  819,  821,  822,  836,  837,  839,  845,  852,
/*   460 */   855,  838,  843,  844,  846,  851,  854,  744,  745,  858,
/*   470 */   676,  677,  680,  681,  894,  896,  895,  897,  679,  678,
/*   480 */   824,  827,  860,  861,  924,  925,  926,  927,  928,  856,
/*   490 */   766,  859,  842,  781,  784,  785,  782,  750,  760,  768,
/*   500 */   769,  770,  771,  755,  756,  762,  777,  810,  811,  775,
/*   510 */   776,  763,  764,  752,  753,  754,  857,  813,  825,  826,
/*   520 */   687,  688,  820,  689,  690,  691,  729,  732,  733,  734,
/*   530 */   692,  711,  714,  715,  693,  700,  694,  695,  702,  703,
/*   540 */   704,  707,  708,  709,  710,  705,  706,  876,  877,  880,
/*   550 */   878,  697,  698,  712,  685,  674,  666,  718,  721,  722,
/*   560 */   723,  724,  725,  727,  719,  720,  664,  655,  658,  767,
/*   570 */   900,  909,  905,  901,  902,  903,  659,  872,  873,  731,
/*   580 */   804,  805,  899,  912,  915,  917,  918,  919,  809,  920,
/*   590 */   922,  913,  947,  656,  735,  736,  739,  881,  929,  795,
/*   600 */   798,  801,  803,  883,  885,  887,  889,  890,  891,  892,
/*   610 */   893,  886,  888,  930,  934,  937,  939,  940,  941,  944,
/*   620 */   946,  951,  952,  953,  956,  957,  954,  636,  631,
};
    static int YY_SZ_ACTTAB = yy_action.Length;//(int)(yy_action.Length/sizeof(yy_action[0]))

    /* The next table maps tokens into fallback tokens.  If a construct
    ** like the following:
    **
    **      %fallback ID X Y Z.
    **
    ** appears in the grammar, then ID becomes a fallback token for X, Y,
    ** and Z.  Whenever one of the tokens X, Y, or Z is input to the parser
    ** but it does not parse, the type of the token is changed to ID and
    ** the parse is retried before an error is thrown.
    */
#if YYFALLBACK
    static YYCODETYPE[] yyFallback = new YYCODETYPE[]{
0,  /*          $ => nothing */
0,  /*       SEMI => nothing */
26,  /*    EXPLAIN => ID */
26,  /*      QUERY => ID */
26,  /*       PLAN => ID */
26,  /*      BEGIN => ID */
0,  /* TRANSACTION => nothing */
26,  /*   DEFERRED => ID */
26,  /*  IMMEDIATE => ID */
26,  /*  EXCLUSIVE => ID */
0,  /*     COMMIT => nothing */
26,  /*        END => ID */
26,  /*   ROLLBACK => ID */
26,  /*  SAVEPOINT => ID */
26,  /*    RELEASE => ID */
0,  /*         TO => nothing */
0,  /*      TABLE => nothing */
0,  /*     CREATE => nothing */
26,  /*         IF => ID */
0,  /*        NOT => nothing */
0,  /*     EXISTS => nothing */
26,  /*       TEMP => ID */
0,  /*         LP => nothing */
0,  /*         RP => nothing */
0,  /*         AS => nothing */
0,  /*      COMMA => nothing */
0,  /*         ID => nothing */
0,  /*    INDEXED => nothing */
26,  /*      ABORT => ID */
26,  /*      AFTER => ID */
26,  /*    ANALYZE => ID */
26,  /*        ASC => ID */
26,  /*     ATTACH => ID */
26,  /*     BEFORE => ID */
26,  /*         BY => ID */
26,  /*    CASCADE => ID */
26,  /*       CAST => ID */
26,  /*   COLUMNKW => ID */
26,  /*   CONFLICT => ID */
26,  /*   DATABASE => ID */
26,  /*       DESC => ID */
26,  /*     DETACH => ID */
26,  /*       EACH => ID */
26,  /*       FAIL => ID */
26,  /*        FOR => ID */
26,  /*     IGNORE => ID */
26,  /*  INITIALLY => ID */
26,  /*    INSTEAD => ID */
26,  /*    LIKE_KW => ID */
26,  /*      MATCH => ID */
26,  /*        KEY => ID */
26,  /*         OF => ID */
26,  /*     OFFSET => ID */
26,  /*     PRAGMA => ID */
26,  /*      RAISE => ID */
26,  /*    REPLACE => ID */
26,  /*   RESTRICT => ID */
26,  /*        ROW => ID */
26,  /*    TRIGGER => ID */
26,  /*     VACUUM => ID */
26,  /*       VIEW => ID */
26,  /*    VIRTUAL => ID */
26,  /*    REINDEX => ID */
26,  /*     RENAME => ID */
26,  /*   CTIME_KW => ID */
};
#endif // * YYFALLBACK */

    /* The following structure represents a single element of the
** parser's stack.  Information stored includes:
**
**   +  The state number for the parser at this level of the stack.
**
**   +  The value of the token stored at this level of the stack.
**      (In other words, the "major" token.)
**
**   +  The semantic value stored at this level of the stack.  This is
**      the information used by the action routines in the grammar.
**      It is sometimes called the "minor" token.
*/
    public class yyStackEntry
    {
      public YYACTIONTYPE stateno;       /* The state-number */
      public YYCODETYPE major;         /* The major token value.  This is the code
** number for the token at this stack level */
      public YYMINORTYPE minor; /* The user-supplied minor token value.  This
** is the value of the token  */
    };
    //typedef struct yyStackEntry yyStackEntry;

    /* The state of the parser is completely contained in an instance of
    ** the following structure */
    public class yyParser
    {
      public int yyidx;                    /* Index of top element in stack */
#if YYTRACKMAXSTACKDEPTH
int yyidxMax;                 /* Maximum value of yyidx */
#endif
      public int yyerrcnt;                 /* Shifts left before out of the error */
      public Parse pParse;  // sqlite3ParserARG_SDECL                /* A place to hold %extra_argument */
#if YYSTACKDEPTH//<=0
public int yystksz;                  /* Current side of the stack */
public yyStackEntry *yystack;        /* The parser's stack */
#else
      public yyStackEntry[] yystack = new yyStackEntry[YYSTACKDEPTH];  /* The parser's stack */
#endif
    };
    //typedef struct yyParser yyParser;

#if !NDEBUG
    //#include <stdio.h>
    static TextWriter yyTraceFILE = null;
    static string yyTracePrompt = "";
#endif // * NDEBUG */

#if !NDEBUG
    /*
** Turn parser tracing on by giving a stream to which to write the trace
** and a prompt to preface each trace message.  Tracing is turned off
** by making either argument NULL
**
** Inputs:
** <ul>
** <li> A FILE* to which trace output should be written.
**      If NULL, then tracing is turned off.
** <li> A prefix string written at the beginning of every
**      line of trace output.  If NULL, then tracing is
**      turned off.
** </ul>
**
** Outputs:
** None.
*/
    static void sqlite3ParserTrace(TextWriter TraceFILE, string zTracePrompt)
    {
      yyTraceFILE = TraceFILE;
      yyTracePrompt = zTracePrompt;
      if (yyTraceFILE == null) yyTracePrompt = "";
      else if (yyTracePrompt == "") yyTraceFILE = null;
    }
#endif // * NDEBUG */

#if !NDEBUG
    /* For tracing shifts, the names of all terminals and nonterminals
** are required.  The following table supplies these names */
    static string[] yyTokenName = {
"$",             "SEMI",          "EXPLAIN",       "QUERY",
"PLAN",          "BEGIN",         "TRANSACTION",   "DEFERRED",
"IMMEDIATE",     "EXCLUSIVE",     "COMMIT",        "END",
"ROLLBACK",      "SAVEPOINT",     "RELEASE",       "TO",
"TABLE",         "CREATE",        "IF",            "NOT",
"EXISTS",        "TEMP",          "LP",            "RP",
"AS",            "COMMA",         "ID",            "INDEXED",
"ABORT",         "AFTER",         "ANALYZE",       "ASC",
"ATTACH",        "BEFORE",        "BY",            "CASCADE",
"CAST",          "COLUMNKW",      "CONFLICT",      "DATABASE",
"DESC",          "DETACH",        "EACH",          "FAIL",
"FOR",           "IGNORE",        "INITIALLY",     "INSTEAD",
"LIKE_KW",       "MATCH",         "KEY",           "OF",
"OFFSET",        "PRAGMA",        "RAISE",         "REPLACE",
"RESTRICT",      "ROW",           "TRIGGER",       "VACUUM",
"VIEW",          "VIRTUAL",       "REINDEX",       "RENAME",
"CTIME_KW",      "ANY",           "OR",            "AND",
"IS",            "BETWEEN",       "IN",            "ISNULL",
"NOTNULL",       "NE",            "EQ",            "GT",
"LE",            "LT",            "GE",            "ESCAPE",
"BITAND",        "BITOR",         "LSHIFT",        "RSHIFT",
"PLUS",          "MINUS",         "STAR",          "SLASH",
"REM",           "CONCAT",        "COLLATE",       "UMINUS",
"UPLUS",         "BITNOT",        "STRING",        "JOIN_KW",
"CONSTRAINT",    "DEFAULT",       "NULL",          "PRIMARY",
"UNIQUE",        "CHECK",         "REFERENCES",    "AUTOINCR",
"ON",            "DELETE",        "UPDATE",        "INSERT",
"SET",           "DEFERRABLE",    "FOREIGN",       "DROP",
"UNION",         "ALL",           "EXCEPT",        "INTERSECT",
"SELECT",        "DISTINCT",      "DOT",           "FROM",
"JOIN",          "USING",         "ORDER",         "GROUP",
"HAVING",        "LIMIT",         "WHERE",         "INTO",
"VALUES",        "INTEGER",       "FLOAT",         "BLOB",
"REGISTER",      "VARIABLE",      "CASE",          "WHEN",
"THEN",          "ELSE",          "INDEX",         "ALTER",
"ADD",           "error",         "input",         "cmdlist",
"ecmd",          "explain",       "cmdx",          "cmd",
"transtype",     "trans_opt",     "nm",            "savepoint_opt",
"create_table",  "create_table_args",  "createkw",      "temp",
"ifnotexists",   "dbnm",          "columnlist",    "conslist_opt",
"select",        "column",        "columnid",      "type",
"carglist",      "id",            "ids",           "typetoken",
"typename",      "signed",        "plus_num",      "minus_num",
"carg",          "ccons",         "term",          "expr",
"onconf",        "sortorder",     "autoinc",       "idxlist_opt",
"refargs",       "defer_subclause",  "refarg",        "refact",
"init_deferred_pred_opt",  "conslist",      "tcons",         "idxlist",
"defer_subclause_opt",  "orconf",        "resolvetype",   "raisetype",
"ifexists",      "fullname",      "oneselect",     "multiselect_op",
"distinct",      "selcollist",    "from",          "where_opt",
"groupby_opt",   "having_opt",    "orderby_opt",   "limit_opt",
"sclp",          "as",            "seltablist",    "stl_prefix",
"joinop",        "indexed_opt",   "on_opt",        "using_opt",
"joinop2",       "inscollist",    "sortlist",      "sortitem",
"nexprlist",     "setlist",       "insert_cmd",    "inscollist_opt",
"itemlist",      "exprlist",      "likeop",        "escape",
"between_op",    "in_op",         "case_operand",  "case_exprlist",
"case_else",     "uniqueflag",    "collate",       "nmnum",
"plus_opt",      "number",        "trigger_decl",  "trigger_cmd_list",
"trigger_time",  "trigger_event",  "foreach_clause",  "when_clause",
"trigger_cmd",   "trnm",          "tridxby",       "database_kw_opt",
"key_opt",       "add_column_fullname",  "kwcolumn_opt",  "create_vtab", 
"vtabarglist",   "vtabarg",       "vtabargtoken",  "lp",          
"anylist", };
#endif // * NDEBUG */

#if !NDEBUG
    /* For tracing reduce actions, the names of all rules are required.
*/
    static string[] yyRuleName = {
/*   0 */ "input ::= cmdlist",
/*   1 */ "cmdlist ::= cmdlist ecmd",
/*   2 */ "cmdlist ::= ecmd",
/*   3 */ "ecmd ::= SEMI",
/*   4 */ "ecmd ::= explain cmdx SEMI",
/*   5 */ "explain ::=",
/*   6 */ "explain ::= EXPLAIN",
/*   7 */ "explain ::= EXPLAIN QUERY PLAN",
/*   8 */ "cmdx ::= cmd",
/*   9 */ "cmd ::= BEGIN transtype trans_opt",
/*  10 */ "trans_opt ::=",
/*  11 */ "trans_opt ::= TRANSACTION",
/*  12 */ "trans_opt ::= TRANSACTION nm",
/*  13 */ "transtype ::=",
/*  14 */ "transtype ::= DEFERRED",
/*  15 */ "transtype ::= IMMEDIATE",
/*  16 */ "transtype ::= EXCLUSIVE",
/*  17 */ "cmd ::= COMMIT trans_opt",
/*  18 */ "cmd ::= END trans_opt",
/*  19 */ "cmd ::= ROLLBACK trans_opt",
/*  20 */ "savepoint_opt ::= SAVEPOINT",
/*  21 */ "savepoint_opt ::=",
/*  22 */ "cmd ::= SAVEPOINT nm",
/*  23 */ "cmd ::= RELEASE savepoint_opt nm",
/*  24 */ "cmd ::= ROLLBACK trans_opt TO savepoint_opt nm",
/*  25 */ "cmd ::= create_table create_table_args",
/*  26 */ "create_table ::= createkw temp TABLE ifnotexists nm dbnm",
/*  27 */ "createkw ::= CREATE",
/*  28 */ "ifnotexists ::=",
/*  29 */ "ifnotexists ::= IF NOT EXISTS",
/*  30 */ "temp ::= TEMP",
/*  31 */ "temp ::=",
/*  32 */ "create_table_args ::= LP columnlist conslist_opt RP",
/*  33 */ "create_table_args ::= AS select",
/*  34 */ "columnlist ::= columnlist COMMA column",
/*  35 */ "columnlist ::= column",
/*  36 */ "column ::= columnid type carglist",
/*  37 */ "columnid ::= nm",
/*  38 */ "id ::= ID",
/*  39 */ "id ::= INDEXED",
/*  40 */ "ids ::= ID|STRING",
/*  41 */ "nm ::= id",
/*  42 */ "nm ::= STRING",
/*  43 */ "nm ::= JOIN_KW",
/*  44 */ "type ::=",
/*  45 */ "type ::= typetoken",
/*  46 */ "typetoken ::= typename",
/*  47 */ "typetoken ::= typename LP signed RP",
/*  48 */ "typetoken ::= typename LP signed COMMA signed RP",
/*  49 */ "typename ::= ids",
/*  50 */ "typename ::= typename ids",
/*  51 */ "signed ::= plus_num",
/*  52 */ "signed ::= minus_num",
/*  53 */ "carglist ::= carglist carg",
/*  54 */ "carglist ::=",
/*  55 */ "carg ::= CONSTRAINT nm ccons",
/*  56 */ "carg ::= ccons",
/*  57 */ "ccons ::= DEFAULT term",
/*  58 */ "ccons ::= DEFAULT LP expr RP",
/*  59 */ "ccons ::= DEFAULT PLUS term",
/*  60 */ "ccons ::= DEFAULT MINUS term",
/*  61 */ "ccons ::= DEFAULT id",
/*  62 */ "ccons ::= NULL onconf",
/*  63 */ "ccons ::= NOT NULL onconf",
/*  64 */ "ccons ::= PRIMARY KEY sortorder onconf autoinc",
/*  65 */ "ccons ::= UNIQUE onconf",
/*  66 */ "ccons ::= CHECK LP expr RP",
/*  67 */ "ccons ::= REFERENCES nm idxlist_opt refargs",
/*  68 */ "ccons ::= defer_subclause",
/*  69 */ "ccons ::= COLLATE ids",
/*  70 */ "autoinc ::=",
/*  71 */ "autoinc ::= AUTOINCR",
/*  72 */ "refargs ::=",
/*  73 */ "refargs ::= refargs refarg",
/*  74 */ "refarg ::= MATCH nm",
/*  75 */ "refarg ::= ON DELETE refact",
/*  76 */ "refarg ::= ON UPDATE refact",
/*  77 */ "refarg ::= ON INSERT refact",
/*  78 */ "refact ::= SET NULL",
/*  79 */ "refact ::= SET DEFAULT",
/*  80 */ "refact ::= CASCADE",
/*  81 */ "refact ::= RESTRICT",
/*  82 */ "defer_subclause ::= NOT DEFERRABLE init_deferred_pred_opt",
/*  83 */ "defer_subclause ::= DEFERRABLE init_deferred_pred_opt",
/*  84 */ "init_deferred_pred_opt ::=",
/*  85 */ "init_deferred_pred_opt ::= INITIALLY DEFERRED",
/*  86 */ "init_deferred_pred_opt ::= INITIALLY IMMEDIATE",
/*  87 */ "conslist_opt ::=",
/*  88 */ "conslist_opt ::= COMMA conslist",
/*  89 */ "conslist ::= conslist COMMA tcons",
/*  90 */ "conslist ::= conslist tcons",
/*  91 */ "conslist ::= tcons",
/*  92 */ "tcons ::= CONSTRAINT nm",
/*  93 */ "tcons ::= PRIMARY KEY LP idxlist autoinc RP onconf",
/*  94 */ "tcons ::= UNIQUE LP idxlist RP onconf",
/*  95 */ "tcons ::= CHECK LP expr RP onconf",
/*  96 */ "tcons ::= FOREIGN KEY LP idxlist RP REFERENCES nm idxlist_opt refargs defer_subclause_opt",
/*  97 */ "defer_subclause_opt ::=",
/*  98 */ "defer_subclause_opt ::= defer_subclause",
/*  99 */ "onconf ::=",
/* 100 */ "onconf ::= ON CONFLICT resolvetype",
/* 101 */ "orconf ::=",
/* 102 */ "orconf ::= OR resolvetype",
/* 103 */ "resolvetype ::= raisetype",
/* 104 */ "resolvetype ::= IGNORE",
/* 105 */ "resolvetype ::= REPLACE",
/* 106 */ "cmd ::= DROP TABLE ifexists fullname",
/* 107 */ "ifexists ::= IF EXISTS",
/* 108 */ "ifexists ::=",
/* 109 */ "cmd ::= createkw temp VIEW ifnotexists nm dbnm AS select",
/* 110 */ "cmd ::= DROP VIEW ifexists fullname",
/* 111 */ "cmd ::= select",
/* 112 */ "select ::= oneselect",
/* 113 */ "select ::= select multiselect_op oneselect",
/* 114 */ "multiselect_op ::= UNION",
/* 115 */ "multiselect_op ::= UNION ALL",
/* 116 */ "multiselect_op ::= EXCEPT|INTERSECT",
/* 117 */ "oneselect ::= SELECT distinct selcollist from where_opt groupby_opt having_opt orderby_opt limit_opt",
/* 118 */ "distinct ::= DISTINCT",
/* 119 */ "distinct ::= ALL",
/* 120 */ "distinct ::=",
/* 121 */ "sclp ::= selcollist COMMA",
/* 122 */ "sclp ::=",
/* 123 */ "selcollist ::= sclp expr as",
/* 124 */ "selcollist ::= sclp STAR",
/* 125 */ "selcollist ::= sclp nm DOT STAR",
/* 126 */ "as ::= AS nm",
/* 127 */ "as ::= ids",
/* 128 */ "as ::=",
/* 129 */ "from ::=",
/* 130 */ "from ::= FROM seltablist",
/* 131 */ "stl_prefix ::= seltablist joinop",
/* 132 */ "stl_prefix ::=",
/* 133 */ "seltablist ::= stl_prefix nm dbnm as indexed_opt on_opt using_opt",
/* 134 */ "seltablist ::= stl_prefix LP select RP as on_opt using_opt",
/* 135 */ "seltablist ::= stl_prefix LP seltablist RP as on_opt using_opt",
/* 136 */ "dbnm ::=",
/* 137 */ "dbnm ::= DOT nm",
/* 138 */ "fullname ::= nm dbnm",
/* 139 */ "joinop ::= COMMA|JOIN",
/* 140 */ "joinop ::= JOIN_KW JOIN",
/* 141 */ "joinop ::= JOIN_KW nm JOIN",
/* 142 */ "joinop ::= JOIN_KW nm nm JOIN",
/* 143 */ "on_opt ::= ON expr",
/* 144 */ "on_opt ::=",
/* 145 */ "indexed_opt ::=",
/* 146 */ "indexed_opt ::= INDEXED BY nm",
/* 147 */ "indexed_opt ::= NOT INDEXED",
/* 148 */ "using_opt ::= USING LP inscollist RP",
/* 149 */ "using_opt ::=",
/* 150 */ "orderby_opt ::=",
/* 151 */ "orderby_opt ::= ORDER BY sortlist",
/* 152 */ "sortlist ::= sortlist COMMA sortitem sortorder",
/* 153 */ "sortlist ::= sortitem sortorder",
/* 154 */ "sortitem ::= expr",
/* 155 */ "sortorder ::= ASC",
/* 156 */ "sortorder ::= DESC",
/* 157 */ "sortorder ::=",
/* 158 */ "groupby_opt ::=",
/* 159 */ "groupby_opt ::= GROUP BY nexprlist",
/* 160 */ "having_opt ::=",
/* 161 */ "having_opt ::= HAVING expr",
/* 162 */ "limit_opt ::=",
/* 163 */ "limit_opt ::= LIMIT expr",
/* 164 */ "limit_opt ::= LIMIT expr OFFSET expr",
/* 165 */ "limit_opt ::= LIMIT expr COMMA expr",
/* 166 */ "cmd ::= DELETE FROM fullname indexed_opt where_opt",
/* 167 */ "where_opt ::=",
/* 168 */ "where_opt ::= WHERE expr",
/* 169 */ "cmd ::= UPDATE orconf fullname indexed_opt SET setlist where_opt",
/* 170 */ "setlist ::= setlist COMMA nm EQ expr",
/* 171 */ "setlist ::= nm EQ expr",
/* 172 */ "cmd ::= insert_cmd INTO fullname inscollist_opt VALUES LP itemlist RP",
/* 173 */ "cmd ::= insert_cmd INTO fullname inscollist_opt select",
/* 174 */ "cmd ::= insert_cmd INTO fullname inscollist_opt DEFAULT VALUES",
/* 175 */ "insert_cmd ::= INSERT orconf",
/* 176 */ "insert_cmd ::= REPLACE",
/* 177 */ "itemlist ::= itemlist COMMA expr",
/* 178 */ "itemlist ::= expr",
/* 179 */ "inscollist_opt ::=",
/* 180 */ "inscollist_opt ::= LP inscollist RP",
/* 181 */ "inscollist ::= inscollist COMMA nm",
/* 182 */ "inscollist ::= nm",
/* 183 */ "expr ::= term",
/* 184 */ "expr ::= LP expr RP",
/* 185 */ "term ::= NULL",
/* 186 */ "expr ::= id",
/* 187 */ "expr ::= JOIN_KW",
/* 188 */ "expr ::= nm DOT nm",
/* 189 */ "expr ::= nm DOT nm DOT nm",
/* 190 */ "term ::= INTEGER|FLOAT|BLOB",
/* 191 */ "term ::= STRING",
/* 192 */ "expr ::= REGISTER",
/* 193 */ "expr ::= VARIABLE",
/* 194 */ "expr ::= expr COLLATE ids",
/* 195 */ "expr ::= CAST LP expr AS typetoken RP",
/* 196 */ "expr ::= ID LP distinct exprlist RP",
/* 197 */ "expr ::= ID LP STAR RP",
/* 198 */ "term ::= CTIME_KW",
/* 199 */ "expr ::= expr AND expr",
/* 200 */ "expr ::= expr OR expr",
/* 201 */ "expr ::= expr LT|GT|GE|LE expr",
/* 202 */ "expr ::= expr EQ|NE expr",
/* 203 */ "expr ::= expr BITAND|BITOR|LSHIFT|RSHIFT expr",
/* 204 */ "expr ::= expr PLUS|MINUS expr",
/* 205 */ "expr ::= expr STAR|SLASH|REM expr",
/* 206 */ "expr ::= expr CONCAT expr",
/* 207 */ "likeop ::= LIKE_KW",
/* 208 */ "likeop ::= NOT LIKE_KW",
/* 209 */ "likeop ::= MATCH",
/* 210 */ "likeop ::= NOT MATCH",
/* 211 */ "escape ::= ESCAPE expr",
/* 212 */ "escape ::=",
/* 213 */ "expr ::= expr likeop expr escape",
/* 214 */ "expr ::= expr ISNULL|NOTNULL",
/* 215 */ "expr ::= expr IS NULL",
/* 216 */ "expr ::= expr NOT NULL",
/* 217 */ "expr ::= expr IS NOT NULL",
/* 218 */ "expr ::= NOT expr",
/* 219 */ "expr ::= BITNOT expr",
/* 220 */ "expr ::= MINUS expr",
/* 221 */ "expr ::= PLUS expr",
/* 222 */ "between_op ::= BETWEEN",
/* 223 */ "between_op ::= NOT BETWEEN",
/* 224 */ "expr ::= expr between_op expr AND expr",
/* 225 */ "in_op ::= IN",
/* 226 */ "in_op ::= NOT IN",
/* 227 */ "expr ::= expr in_op LP exprlist RP",
/* 228 */ "expr ::= LP select RP",
/* 229 */ "expr ::= expr in_op LP select RP",
/* 230 */ "expr ::= expr in_op nm dbnm",
/* 231 */ "expr ::= EXISTS LP select RP",
/* 232 */ "expr ::= CASE case_operand case_exprlist case_else END",
/* 233 */ "case_exprlist ::= case_exprlist WHEN expr THEN expr",
/* 234 */ "case_exprlist ::= WHEN expr THEN expr",
/* 235 */ "case_else ::= ELSE expr",
/* 236 */ "case_else ::=",
/* 237 */ "case_operand ::= expr",
/* 238 */ "case_operand ::=",
/* 239 */ "exprlist ::= nexprlist",
/* 240 */ "exprlist ::=",
/* 241 */ "nexprlist ::= nexprlist COMMA expr",
/* 242 */ "nexprlist ::= expr",
/* 243 */ "cmd ::= createkw uniqueflag INDEX ifnotexists nm dbnm ON nm LP idxlist RP",
/* 244 */ "uniqueflag ::= UNIQUE",
/* 245 */ "uniqueflag ::=",
/* 246 */ "idxlist_opt ::=",
/* 247 */ "idxlist_opt ::= LP idxlist RP",
/* 248 */ "idxlist ::= idxlist COMMA nm collate sortorder",
/* 249 */ "idxlist ::= nm collate sortorder",
/* 250 */ "collate ::=",
/* 251 */ "collate ::= COLLATE ids",
/* 252 */ "cmd ::= DROP INDEX ifexists fullname",
/* 253 */ "cmd ::= VACUUM",
/* 254 */ "cmd ::= VACUUM nm",
/* 255 */ "cmd ::= PRAGMA nm dbnm",
/* 256 */ "cmd ::= PRAGMA nm dbnm EQ nmnum",
/* 257 */ "cmd ::= PRAGMA nm dbnm LP nmnum RP",
/* 258 */ "cmd ::= PRAGMA nm dbnm EQ minus_num",
/* 259 */ "cmd ::= PRAGMA nm dbnm LP minus_num RP",
/* 260 */ "nmnum ::= plus_num",
/* 261 */ "nmnum ::= nm",
/* 262 */ "nmnum ::= ON",
/* 263 */ "nmnum ::= DELETE",
/* 264 */ "nmnum ::= DEFAULT",
/* 265 */ "plus_num ::= plus_opt number",
/* 266 */ "minus_num ::= MINUS number",
/* 267 */ "number ::= INTEGER|FLOAT",
/* 268 */ "plus_opt ::= PLUS",
/* 269 */ "plus_opt ::=",
/* 270 */ "cmd ::= createkw trigger_decl BEGIN trigger_cmd_list END",
/* 271 */ "trigger_decl ::= temp TRIGGER ifnotexists nm dbnm trigger_time trigger_event ON fullname foreach_clause when_clause",
/* 272 */ "trigger_time ::= BEFORE",
/* 273 */ "trigger_time ::= AFTER",
/* 274 */ "trigger_time ::= INSTEAD OF",
/* 275 */ "trigger_time ::=",
/* 276 */ "trigger_event ::= DELETE|INSERT",
/* 277 */ "trigger_event ::= UPDATE",
/* 278 */ "trigger_event ::= UPDATE OF inscollist",
/* 279 */ "foreach_clause ::=",
/* 280 */ "foreach_clause ::= FOR EACH ROW",
/* 281 */ "when_clause ::=",
/* 282 */ "when_clause ::= WHEN expr",
/* 283 */ "trigger_cmd_list ::= trigger_cmd_list trigger_cmd SEMI",
/* 284 */ "trigger_cmd_list ::= trigger_cmd SEMI",
/* 285 */ "trnm ::= nm",
/* 286 */ "trnm ::= nm DOT nm",
/* 287 */ "tridxby ::=",
/* 288 */ "tridxby ::= INDEXED BY nm",
/* 289 */ "tridxby ::= NOT INDEXED",
/* 290 */ "trigger_cmd ::= UPDATE orconf trnm tridxby SET setlist where_opt",
/* 291 */ "trigger_cmd ::= insert_cmd INTO trnm inscollist_opt VALUES LP itemlist RP",
/* 292 */ "trigger_cmd ::= insert_cmd INTO trnm inscollist_opt select",
/* 293 */ "trigger_cmd ::= DELETE FROM trnm tridxby where_opt",
/* 294 */ "trigger_cmd ::= select",
/* 295 */ "expr ::= RAISE LP IGNORE RP",
/* 296 */ "expr ::= RAISE LP raisetype COMMA nm RP",
/* 297 */ "raisetype ::= ROLLBACK",
/* 298 */ "raisetype ::= ABORT",
/* 299 */ "raisetype ::= FAIL",
/* 300 */ "cmd ::= DROP TRIGGER ifexists fullname",
/* 301 */ "cmd ::= ATTACH database_kw_opt expr AS expr key_opt",
/* 302 */ "cmd ::= DETACH database_kw_opt expr",
/* 303 */ "key_opt ::=",
/* 304 */ "key_opt ::= KEY expr",
/* 305 */ "database_kw_opt ::= DATABASE",
/* 306 */ "database_kw_opt ::=",
/* 307 */ "cmd ::= REINDEX",
/* 308 */ "cmd ::= REINDEX nm dbnm",
/* 309 */ "cmd ::= ANALYZE",
/* 310 */ "cmd ::= ANALYZE nm dbnm",
/* 311 */ "cmd ::= ALTER TABLE fullname RENAME TO nm",
/* 312 */ "cmd ::= ALTER TABLE add_column_fullname ADD kwcolumn_opt column",
/* 313 */ "add_column_fullname ::= fullname",
/* 314 */ "kwcolumn_opt ::=",
/* 315 */ "kwcolumn_opt ::= COLUMNKW",
/* 316 */ "cmd ::= create_vtab",
/* 317 */ "cmd ::= create_vtab LP vtabarglist RP",
/* 318 */ "create_vtab ::= createkw VIRTUAL TABLE nm dbnm USING nm",
/* 319 */ "vtabarglist ::= vtabarg",
/* 320 */ "vtabarglist ::= vtabarglist COMMA vtabarg",
/* 321 */ "vtabarg ::=",
/* 322 */ "vtabarg ::= vtabarg vtabargtoken",
/* 323 */ "vtabargtoken ::= ANY",
/* 324 */ "vtabargtoken ::= lp anylist RP",
/* 325 */ "lp ::= LP",
/* 326 */ "anylist ::=",
/* 327 */ "anylist ::= anylist LP anylist RP",
/* 328 */ "anylist ::= anylist ANY",
};
#endif // * NDEBUG */


#if YYSTACKDEPTH//<=0
/*
** Try to increase the size of the parser stack.
*/
static void yyGrowStack(yyParser p){
int newSize;
//yyStackEntry pNew;

newSize = p.yystksz*2 + 100;
//pNew = realloc(p.yystack, newSize*sizeof(pNew[0]));
//if( pNew !=null){
p.yystack = Array.Resize(p.yystack,newSize); //pNew;
p.yystksz = newSize;
#if !NDEBUG
if( yyTraceFILE ){
fprintf(yyTraceFILE,"%sStack grows to %d entries!\n",
yyTracePrompt, p.yystksz);
}
#endif
//}
}
#endif

    /*
** This function allocates a new parser.
** The only argument is a pointer to a function which works like
** malloc.
**
** Inputs:
** A pointer to the function used to allocate memory.
**
** Outputs:
** A pointer to a parser.  This pointer is used in subsequent calls
** to sqlite3Parser and sqlite3ParserFree.
*/
    static yyParser sqlite3ParserAlloc()
    {//void *(*mallocProc)(size_t)){
      yyParser pParser = new yyParser();
      //pParser = (yyParser*)(*mallocProc)( (size_t)yyParser.Length );
      if (pParser != null)
      {
        pParser.yyidx = -1;
#if YYTRACKMAXSTACKDEPTH
pParser.yyidxMax=0;
#endif

#if YYSTACKDEPTH//<=0
pParser.yystack = NULL;
pParser.yystksz = 0;
yyGrowStack(pParser);
#endif
      }
      return pParser;
    }

    /* The following function deletes the value associated with a
    ** symbol.  The symbol can be either a terminal or nonterminal.
    ** "yymajor" is the symbol code, and "yypminor" is a pointer to
    ** the value.
    */
    static void yy_destructor(
    yyParser yypParser,    /* The parser */
    YYCODETYPE yymajor,    /* Type code for object to destroy */
    YYMINORTYPE yypminor   /* The object to be destroyed */
    )
    {
      Parse pParse = yypParser.pParse; // sqlite3ParserARG_FETCH;
      switch (yymajor)
      {
        /* Here is inserted the actions which take place when a
        ** terminal or non-terminal is destroyed.  This can happen
        ** when the symbol is popped from the stack during a
        ** reduce or during error processing or when a parser is
        ** being destroyed before it is finished parsing.
        **
        ** Note: during a reduce, the only symbols destroyed are those
        ** which appear on the RHS of the rule, but which are not used
        ** inside the C code.
        */
        case 160: /* select */
        case 194: /* oneselect */
          {
            //#line 404 "parse.y"
            sqlite3SelectDelete(pParse.db, ref (yypminor.yy3));
            //#line 1373 "parse.c"
          }
          break;
        case 174: /* term */
        case 175: /* expr */
        case 223: /* escape */
          {
            //#line 721 "parse.y"
            sqlite3ExprDelete(pParse.db, ref (yypminor.yy346).pExpr);
            //#line 1382 "parse.c"
          }
          break;
        case 179: /* idxlist_opt */
        case 187: /* idxlist */
        case 197: /* selcollist */
        case 200: /* groupby_opt */
        case 202: /* orderby_opt */
        case 204: /* sclp */
        case 214: /* sortlist */
        case 216: /* nexprlist */
        case 217: /* setlist */
        case 220: /* itemlist */
        case 221: /* exprlist */
        case 227: /* case_exprlist */
          {
            //#line 1062 "parse.y"
            sqlite3ExprListDelete(pParse.db, ref  (yypminor.yy14));
            //#line 1400 "parse.c"
          }
          break;
        case 193: /* fullname */
        case 198: /* from */
        case 206: /* seltablist */
        case 207: /* stl_prefix */
          {
            //#line 535 "parse.y"
            sqlite3SrcListDelete(pParse.db, ref  (yypminor.yy65));
            //#line 1410 "parse.c"
          }
          break;
        case 199: /* where_opt */
        case 201: /* having_opt */
        case 210: /* on_opt */
        case 215: /* sortitem */
        case 226: /* case_operand */
        case 228: /* case_else */
        case 239: /* when_clause */
        case 242: /* key_opt */
          {
            //#line 645 "parse.y"
            sqlite3ExprDelete(pParse.db, ref (yypminor.yy132));
            //#line 1424 "parse.c"
          }
          break;
        case 211: /* using_opt */
        case 213: /* inscollist */
        case 219: /* inscollist_opt */
          {
            //#line 567 "parse.y"
            sqlite3IdListDelete(pParse.db, ref (yypminor.yy408));
            //#line 1433 "parse.c"
          }
          break;
        case 235: /* trigger_cmd_list */
        case 240: /* trigger_cmd */
          {
            //#line 1169 "parse.y"
            sqlite3DeleteTriggerStep(pParse.db, ref (yypminor.yy473));
            //#line 1441 "parse.c"
          }
          break;
        case 237: /* trigger_event */
          {
            //#line 1155 "parse.y"
            sqlite3IdListDelete(pParse.db, ref (yypminor.yy378).b);
            //#line 1448 "parse.c"
          }
          break;
        default: break;   /* If no destructor action specified: do nothing */
      }
    }

    /*
    ** Pop the parser's stack once.
    **
    ** If there is a destructor routine associated with the token which
    ** is popped from the stack, then call it.
    **
    ** Return the major token number for the symbol popped.
    */
    static int yy_pop_parser_stack(yyParser pParser)
    {
      YYCODETYPE yymajor;
      yyStackEntry yytos = pParser.yystack[pParser.yyidx];

      /* There is no mechanism by which the parser stack can be popped below
      ** empty in SQLite.  */
      if (NEVER(pParser.yyidx < 0)) return 0;
#if !NDEBUG
      if (yyTraceFILE != null && pParser.yyidx >= 0)
      {
        fprintf(yyTraceFILE, "%sPopping %s\n",
        yyTracePrompt,
        yyTokenName[yytos.major]);
      }
#endif
      yymajor = yytos.major;
      yy_destructor(pParser, yymajor, yytos.minor);
      pParser.yyidx--;
      return yymajor;
    }

    /*
    ** Deallocate and destroy a parser.  Destructors are all called for
    ** all stack elements before shutting the parser down.
    **
    ** Inputs:
    ** <ul>
    ** <li>  A pointer to the parser.  This should be a pointer
    **       obtained from sqlite3ParserAlloc.
    ** <li>  A pointer to a function used to reclaim memory obtained
    **       from malloc.
    ** </ul>
    */
    static void sqlite3ParserFree(
    yyParser p,                    /* The parser to be deleted */
    dxDel freeProc//)(void*)     /* Function used to reclaim memory */
    )
    {
      yyParser pParser = p;
      /* In SQLite, we never try to destroy a parser that was not successfully
      ** created in the first place. */
      if (NEVER(pParser == null)) return;
      while (pParser.yyidx >= 0) yy_pop_parser_stack(pParser);
#if YYSTACKDEPTH//<=0
pParser.yystack = null;//free(pParser.yystack);
#endif
      pParser = null;// freeProc(ref pParser);
    }

    /*
    ** Return the peak depth of the stack for a parser.
    */
#if YYTRACKMAXSTACKDEPTH
int sqlite3ParserStackPeak(void p){
yyParser pParser = (yyParser*)p;
return pParser.yyidxMax;
}
#endif

    /*
** Find the appropriate action for a parser given the terminal
** look-ahead token iLookAhead.
**
** If the look-ahead token is YYNOCODE, then check to see if the action is
** independent of the look-ahead.  If it is, return the action, otherwise
** return YY_NO_ACTION.
*/
    static int yy_find_shift_action(
    yyParser pParser,         /* The parser */
    YYCODETYPE iLookAhead     /* The look-ahead token */
    )
    {
      int i;
      int stateno = pParser.yystack[pParser.yyidx].stateno;

      if (stateno > YY_SHIFT_MAX || (i = yy_shift_ofst[stateno]) == YY_SHIFT_USE_DFLT)
      {
        return yy_default[stateno];
      }
      Debug.Assert(iLookAhead != YYNOCODE);
      i += iLookAhead;
      if (i < 0 || i >= YY_SZ_ACTTAB || yy_lookahead[i] != iLookAhead)
      {
        /* The user of ";" instead of "\000" as a statement terminator in SQLite
        ** means that we always have a look-ahead token. */
        if (iLookAhead > 0)
        {
#if YYFALLBACK
          YYCODETYPE iFallback;            /* Fallback token */
          if (iLookAhead < yyFallback.Length //yyFallback.Length/sizeof(yyFallback[0])
          && (iFallback = yyFallback[iLookAhead]) != 0)
          {
#if !NDEBUG
            if (yyTraceFILE != null)
            {
              fprintf(yyTraceFILE, "%sFALLBACK %s => %s\n",
              yyTracePrompt, yyTokenName[iLookAhead], yyTokenName[iFallback]);
            }
#endif
            return yy_find_shift_action(pParser, iFallback);
          }
#endif
#if YYWILDCARD
          {
            int j = i - iLookAhead + YYWILDCARD;
            if (j >= 0 && j < YY_SZ_ACTTAB && yy_lookahead[j] == YYWILDCARD)
            {
#if !NDEBUG
              if (yyTraceFILE != null)
              {
                Debugger.Break(); // TODO --
                //fprintf(yyTraceFILE, "%sWILDCARD %s => %s\n",
                //   yyTracePrompt, yyTokenName[iLookAhead], yyTokenName[YYWILDCARD]);
              }
#endif // * NDEBUG */
              return yy_action[j];
            }
          }
#endif // * YYWILDCARD */
        }
        return yy_default[stateno];
      }
      else
      {
        return yy_action[i];
      }
    }

    /*
    ** Find the appropriate action for a parser given the non-terminal
    ** look-ahead token iLookAhead.
    **
    ** If the look-ahead token is YYNOCODE, then check to see if the action is
    ** independent of the look-ahead.  If it is, return the action, otherwise
    ** return YY_NO_ACTION.
    */
    static int yy_find_reduce_action(
    int stateno,              /* Current state number */
    YYCODETYPE iLookAhead     /* The look-ahead token */
    )
    {
      int i;
#if YYERRORSYMBOL
if( stateno>YY_REDUCE_MAX ){
return yy_default[stateno];
}
#else
      Debug.Assert(stateno <= YY_REDUCE_MAX);
#endif
      i = yy_reduce_ofst[stateno];
      Debug.Assert(i != YY_REDUCE_USE_DFLT);
      Debug.Assert(iLookAhead != YYNOCODE);
      i += iLookAhead;
#if YYERRORSYMBOL
if( i<0 || i>=YY_SZ_ACTTAB || yy_lookahead[i]!=iLookAhead ){
return yy_default[stateno];
}
#else
      Debug.Assert(i >= 0 && i < YY_SZ_ACTTAB);
      Debug.Assert(yy_lookahead[i] == iLookAhead);
#endif
      return yy_action[i];
    }

    /*
    ** The following routine is called if the stack overflows.
    */
    static void yyStackOverflow(yyParser yypParser, YYMINORTYPE yypMinor)
    {
      Parse pParse = yypParser.pParse; // sqlite3ParserARG_FETCH;
      yypParser.yyidx--;
#if !NDEBUG
      if (yyTraceFILE != null)
      {
        Debugger.Break(); // TODO --
        //fprintf(yyTraceFILE, "%sStack Overflow!\n", yyTracePrompt);
      }
#endif
      while (yypParser.yyidx >= 0) yy_pop_parser_stack(yypParser);
      /* Here code is inserted which will execute if the parser
      ** stack every overflows */
      //#line 40 "parse.y"

      UNUSED_PARAMETER(yypMinor); /* Silence some compiler warnings */
      sqlite3ErrorMsg(pParse, "parser stack overflow");
      pParse.parseError = 1;
      //#line 1632  "parse.c"
      yypParser.pParse = pParse;//      sqlite3ParserARG_STORE; /* Suppress warning about unused %extra_argument var */
    }

    /*
    ** Perform a shift action.
    */
    static void yy_shift(
    yyParser yypParser,          /* The parser to be shifted */
    int yyNewState,               /* The new state to shift in */
    int yyMajor,                  /* The major token to shift in */
    YYMINORTYPE yypMinor         /* Pointer to the minor token to shift in */
    )
    {
      yyStackEntry yytos = new yyStackEntry();
      yypParser.yyidx++;
#if YYTRACKMAXSTACKDEPTH
if( yypParser.yyidx>yypParser.yyidxMax ){
yypParser.yyidxMax = yypParser.yyidx;
}
#endif
#if !YYSTACKDEPTH//was YYSTACKDEPTH>0
      if (yypParser.yyidx >= YYSTACKDEPTH)
      {
        yyStackOverflow(yypParser, yypMinor);
        return;
      }
#else
if( yypParser.yyidx>=yypParser.yystksz ){
yyGrowStack(yypParser);
if( yypParser.yyidx>=yypParser.yystksz ){
yyStackOverflow(yypParser, yypMinor);
return;
}
}
#endif
      yypParser.yystack[yypParser.yyidx] = yytos;//yytos = yypParser.yystack[yypParser.yyidx];
      yytos.stateno = (YYACTIONTYPE)yyNewState;
      yytos.major = (YYCODETYPE)yyMajor;
      yytos.minor = yypMinor;
#if !NDEBUG
      if (yyTraceFILE != null && yypParser.yyidx > 0)
      {
        int i;
        fprintf(yyTraceFILE, "%sShift %d\n", yyTracePrompt, yyNewState);
        fprintf(yyTraceFILE, "%sStack:", yyTracePrompt);
        for (i = 1; i <= yypParser.yyidx; i++)
          fprintf(yyTraceFILE, " %s", yyTokenName[yypParser.yystack[i].major]);
        fprintf(yyTraceFILE, "\n");
      }
#endif
    }
    /* The following table contains information about every rule that
    ** is used during the reduce.
    */
    public struct _yyRuleInfo
    {
      public YYCODETYPE lhs;         /* Symbol on the left-hand side of the rule */
      public byte nrhs;     /* Number of right-hand side symbols in the rule */
      public _yyRuleInfo(YYCODETYPE lhs, byte nrhs)
      {
        this.lhs = lhs;
        this.nrhs = nrhs;
      }

    }
    static _yyRuleInfo[] yyRuleInfo = new _yyRuleInfo[]{
new _yyRuleInfo( 142, 1 ),
new _yyRuleInfo( 143, 2 ),
new _yyRuleInfo( 143, 1 ),
new _yyRuleInfo( 144, 1 ),
new _yyRuleInfo( 144, 3 ),
new _yyRuleInfo( 145, 0 ),
new _yyRuleInfo( 145, 1 ),
new _yyRuleInfo( 145, 3 ),
new _yyRuleInfo( 146, 1 ),
new _yyRuleInfo( 147, 3 ),
new _yyRuleInfo( 149, 0 ),
new _yyRuleInfo( 149, 1 ),
new _yyRuleInfo( 149, 2 ),
new _yyRuleInfo( 148, 0 ),
new _yyRuleInfo( 148, 1 ),
new _yyRuleInfo( 148, 1 ),
new _yyRuleInfo( 148, 1 ),
new _yyRuleInfo( 147, 2 ),
new _yyRuleInfo( 147, 2 ),
new _yyRuleInfo( 147, 2 ),
new _yyRuleInfo( 151, 1 ),
new _yyRuleInfo( 151, 0 ),
new _yyRuleInfo( 147, 2 ),
new _yyRuleInfo( 147, 3 ),
new _yyRuleInfo( 147, 5 ),
new _yyRuleInfo( 147, 2 ),
new _yyRuleInfo( 152, 6 ),
new _yyRuleInfo( 154, 1 ),
new _yyRuleInfo( 156, 0 ),
new _yyRuleInfo( 156, 3 ),
new _yyRuleInfo( 155, 1 ),
new _yyRuleInfo( 155, 0 ),
new _yyRuleInfo( 153, 4 ),
new _yyRuleInfo( 153, 2 ),
new _yyRuleInfo( 158, 3 ),
new _yyRuleInfo( 158, 1 ),
new _yyRuleInfo( 161, 3 ),
new _yyRuleInfo( 162, 1 ),
new _yyRuleInfo( 165, 1 ),
new _yyRuleInfo( 165, 1 ),
new _yyRuleInfo( 166, 1 ),
new _yyRuleInfo( 150, 1 ),
new _yyRuleInfo( 150, 1 ),
new _yyRuleInfo( 150, 1 ),
new _yyRuleInfo( 163, 0 ),
new _yyRuleInfo( 163, 1 ),
new _yyRuleInfo( 167, 1 ),
new _yyRuleInfo( 167, 4 ),
new _yyRuleInfo( 167, 6 ),
new _yyRuleInfo( 168, 1 ),
new _yyRuleInfo( 168, 2 ),
new _yyRuleInfo( 169, 1 ),
new _yyRuleInfo( 169, 1 ),
new _yyRuleInfo( 164, 2 ),
new _yyRuleInfo( 164, 0 ),
new _yyRuleInfo( 172, 3 ),
new _yyRuleInfo( 172, 1 ),
new _yyRuleInfo( 173, 2 ),
new _yyRuleInfo( 173, 4 ),
new _yyRuleInfo( 173, 3 ),
new _yyRuleInfo( 173, 3 ),
new _yyRuleInfo( 173, 2 ),
new _yyRuleInfo( 173, 2 ),
new _yyRuleInfo( 173, 3 ),
new _yyRuleInfo( 173, 5 ),
new _yyRuleInfo( 173, 2 ),
new _yyRuleInfo( 173, 4 ),
new _yyRuleInfo( 173, 4 ),
new _yyRuleInfo( 173, 1 ),
new _yyRuleInfo( 173, 2 ),
new _yyRuleInfo( 178, 0 ),
new _yyRuleInfo( 178, 1 ),
new _yyRuleInfo( 180, 0 ),
new _yyRuleInfo( 180, 2 ),
new _yyRuleInfo( 182, 2 ),
new _yyRuleInfo( 182, 3 ),
new _yyRuleInfo( 182, 3 ),
new _yyRuleInfo( 182, 3 ),
new _yyRuleInfo( 183, 2 ),
new _yyRuleInfo( 183, 2 ),
new _yyRuleInfo( 183, 1 ),
new _yyRuleInfo( 183, 1 ),
new _yyRuleInfo( 181, 3 ),
new _yyRuleInfo( 181, 2 ),
new _yyRuleInfo( 184, 0 ),
new _yyRuleInfo( 184, 2 ),
new _yyRuleInfo( 184, 2 ),
new _yyRuleInfo( 159, 0 ),
new _yyRuleInfo( 159, 2 ),
new _yyRuleInfo( 185, 3 ),
new _yyRuleInfo( 185, 2 ),
new _yyRuleInfo( 185, 1 ),
new _yyRuleInfo( 186, 2 ),
new _yyRuleInfo( 186, 7 ),
new _yyRuleInfo( 186, 5 ),
new _yyRuleInfo( 186, 5 ),
new _yyRuleInfo( 186, 10 ),
new _yyRuleInfo( 188, 0 ),
new _yyRuleInfo( 188, 1 ),
new _yyRuleInfo( 176, 0 ),
new _yyRuleInfo( 176, 3 ),
new _yyRuleInfo( 189, 0 ),
new _yyRuleInfo( 189, 2 ),
new _yyRuleInfo( 190, 1 ),
new _yyRuleInfo( 190, 1 ),
new _yyRuleInfo( 190, 1 ),
new _yyRuleInfo( 147, 4 ),
new _yyRuleInfo( 192, 2 ),
new _yyRuleInfo( 192, 0 ),
new _yyRuleInfo( 147, 8 ),
new _yyRuleInfo( 147, 4 ),
new _yyRuleInfo( 147, 1 ),
new _yyRuleInfo( 160, 1 ),
new _yyRuleInfo( 160, 3 ),
new _yyRuleInfo( 195, 1 ),
new _yyRuleInfo( 195, 2 ),
new _yyRuleInfo( 195, 1 ),
new _yyRuleInfo( 194, 9 ),
new _yyRuleInfo( 196, 1 ),
new _yyRuleInfo( 196, 1 ),
new _yyRuleInfo( 196, 0 ),
new _yyRuleInfo( 204, 2 ),
new _yyRuleInfo( 204, 0 ),
new _yyRuleInfo( 197, 3 ),
new _yyRuleInfo( 197, 2 ),
new _yyRuleInfo( 197, 4 ),
new _yyRuleInfo( 205, 2 ),
new _yyRuleInfo( 205, 1 ),
new _yyRuleInfo( 205, 0 ),
new _yyRuleInfo( 198, 0 ),
new _yyRuleInfo( 198, 2 ),
new _yyRuleInfo( 207, 2 ),
new _yyRuleInfo( 207, 0 ),
new _yyRuleInfo( 206, 7 ),
new _yyRuleInfo( 206, 7 ),
new _yyRuleInfo( 206, 7 ),
new _yyRuleInfo( 157, 0 ),
new _yyRuleInfo( 157, 2 ),
new _yyRuleInfo( 193, 2 ),
new _yyRuleInfo( 208, 1 ),
new _yyRuleInfo( 208, 2 ),
new _yyRuleInfo( 208, 3 ),
new _yyRuleInfo( 208, 4 ),
new _yyRuleInfo( 210, 2 ),
new _yyRuleInfo( 210, 0 ),
new _yyRuleInfo( 209, 0 ),
new _yyRuleInfo( 209, 3 ),
new _yyRuleInfo( 209, 2 ),
new _yyRuleInfo( 211, 4 ),
new _yyRuleInfo( 211, 0 ),
new _yyRuleInfo( 202, 0 ),
new _yyRuleInfo( 202, 3 ),
new _yyRuleInfo( 214, 4 ),
new _yyRuleInfo( 214, 2 ),
new _yyRuleInfo( 215, 1 ),
new _yyRuleInfo( 177, 1 ),
new _yyRuleInfo( 177, 1 ),
new _yyRuleInfo( 177, 0 ),
new _yyRuleInfo( 200, 0 ),
new _yyRuleInfo( 200, 3 ),
new _yyRuleInfo( 201, 0 ),
new _yyRuleInfo( 201, 2 ),
new _yyRuleInfo( 203, 0 ),
new _yyRuleInfo( 203, 2 ),
new _yyRuleInfo( 203, 4 ),
new _yyRuleInfo( 203, 4 ),
new _yyRuleInfo( 147, 5 ),
new _yyRuleInfo( 199, 0 ),
new _yyRuleInfo( 199, 2 ),
new _yyRuleInfo( 147, 7 ),
new _yyRuleInfo( 217, 5 ),
new _yyRuleInfo( 217, 3 ),
new _yyRuleInfo( 147, 8 ),
new _yyRuleInfo( 147, 5 ),
new _yyRuleInfo( 147, 6 ),
new _yyRuleInfo( 218, 2 ),
new _yyRuleInfo( 218, 1 ),
new _yyRuleInfo( 220, 3 ),
new _yyRuleInfo( 220, 1 ),
new _yyRuleInfo( 219, 0 ),
new _yyRuleInfo( 219, 3 ),
new _yyRuleInfo( 213, 3 ),
new _yyRuleInfo( 213, 1 ),
new _yyRuleInfo( 175, 1 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 174, 1 ),
new _yyRuleInfo( 175, 1 ),
new _yyRuleInfo( 175, 1 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 5 ),
new _yyRuleInfo( 174, 1 ),
new _yyRuleInfo( 174, 1 ),
new _yyRuleInfo( 175, 1 ),
new _yyRuleInfo( 175, 1 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 6 ),
new _yyRuleInfo( 175, 5 ),
new _yyRuleInfo( 175, 4 ),
new _yyRuleInfo( 174, 1 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 222, 1 ),
new _yyRuleInfo( 222, 2 ),
new _yyRuleInfo( 222, 1 ),
new _yyRuleInfo( 222, 2 ),
new _yyRuleInfo( 223, 2 ),
new _yyRuleInfo( 223, 0 ),
new _yyRuleInfo( 175, 4 ),
new _yyRuleInfo( 175, 2 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 4 ),
new _yyRuleInfo( 175, 2 ),
new _yyRuleInfo( 175, 2 ),
new _yyRuleInfo( 175, 2 ),
new _yyRuleInfo( 175, 2 ),
new _yyRuleInfo( 224, 1 ),
new _yyRuleInfo( 224, 2 ),
new _yyRuleInfo( 175, 5 ),
new _yyRuleInfo( 225, 1 ),
new _yyRuleInfo( 225, 2 ),
new _yyRuleInfo( 175, 5 ),
new _yyRuleInfo( 175, 3 ),
new _yyRuleInfo( 175, 5 ),
new _yyRuleInfo( 175, 4 ),
new _yyRuleInfo( 175, 4 ),
new _yyRuleInfo( 175, 5 ),
new _yyRuleInfo( 227, 5 ),
new _yyRuleInfo( 227, 4 ),
new _yyRuleInfo( 228, 2 ),
new _yyRuleInfo( 228, 0 ),
new _yyRuleInfo( 226, 1 ),
new _yyRuleInfo( 226, 0 ),
new _yyRuleInfo( 221, 1 ),
new _yyRuleInfo( 221, 0 ),
new _yyRuleInfo( 216, 3 ),
new _yyRuleInfo( 216, 1 ),
new _yyRuleInfo( 147, 11 ),
new _yyRuleInfo( 229, 1 ),
new _yyRuleInfo( 229, 0 ),
new _yyRuleInfo( 179, 0 ),
new _yyRuleInfo( 179, 3 ),
new _yyRuleInfo( 187, 5 ),
new _yyRuleInfo( 187, 3 ),
new _yyRuleInfo( 230, 0 ),
new _yyRuleInfo( 230, 2 ),
new _yyRuleInfo( 147, 4 ),
new _yyRuleInfo( 147, 1 ),
new _yyRuleInfo( 147, 2 ),
new _yyRuleInfo( 147, 3 ),
new _yyRuleInfo( 147, 5 ),
new _yyRuleInfo( 147, 6 ),
new _yyRuleInfo( 147, 5 ),
new _yyRuleInfo( 147, 6 ),
new _yyRuleInfo( 231, 1 ),
new _yyRuleInfo( 231, 1 ),
new _yyRuleInfo( 231, 1 ),
new _yyRuleInfo( 231, 1 ),
new _yyRuleInfo( 231, 1 ),
new _yyRuleInfo( 170, 2 ),
new _yyRuleInfo( 171, 2 ),
new _yyRuleInfo( 233, 1 ),
new _yyRuleInfo( 232, 1 ),
new _yyRuleInfo( 232, 0 ),
new _yyRuleInfo( 147, 5 ),
new _yyRuleInfo( 234, 11 ),
new _yyRuleInfo( 236, 1 ),
new _yyRuleInfo( 236, 1 ),
new _yyRuleInfo( 236, 2 ),
new _yyRuleInfo( 236, 0 ),
new _yyRuleInfo( 237, 1 ),
new _yyRuleInfo( 237, 1 ),
new _yyRuleInfo( 237, 3 ),
new _yyRuleInfo( 238, 0 ),
new _yyRuleInfo( 238, 3 ),
new _yyRuleInfo( 239, 0 ),
new _yyRuleInfo( 239, 2 ),
new _yyRuleInfo( 235, 3 ),
new _yyRuleInfo( 235, 2 ),
new _yyRuleInfo( 241, 1 ),
new _yyRuleInfo( 241, 3 ),
new _yyRuleInfo( 242, 0 ),
new _yyRuleInfo( 242, 3 ),
new _yyRuleInfo( 242, 2 ),
new _yyRuleInfo( 240, 7 ),
new _yyRuleInfo( 240, 8 ),
new _yyRuleInfo( 240, 5 ),
new _yyRuleInfo( 240, 5 ),
new _yyRuleInfo( 240, 1 ),
new _yyRuleInfo( 175, 4 ),
new _yyRuleInfo( 175, 6 ),
new _yyRuleInfo( 191, 1 ),
new _yyRuleInfo( 191, 1 ),
new _yyRuleInfo( 191, 1 ),
new _yyRuleInfo( 147, 4 ),
new _yyRuleInfo( 147, 6 ),
new _yyRuleInfo( 147, 3 ),
new _yyRuleInfo( 244, 0 ),
new _yyRuleInfo( 244, 2 ),
new _yyRuleInfo( 243, 1 ),
new _yyRuleInfo( 243, 0 ),
new _yyRuleInfo( 147, 1 ),
new _yyRuleInfo( 147, 3 ),
new _yyRuleInfo( 147, 1 ),
new _yyRuleInfo( 147, 3 ),
new _yyRuleInfo( 147, 6 ),
new _yyRuleInfo( 147, 6 ),
new _yyRuleInfo( 245, 1 ),
new _yyRuleInfo( 246, 0 ),
new _yyRuleInfo( 246, 1 ),
new _yyRuleInfo( 147, 1 ),
new _yyRuleInfo( 147, 4 ),
new _yyRuleInfo( 247, 7 ),
new _yyRuleInfo( 248, 1 ),
new _yyRuleInfo( 248, 3 ),
new _yyRuleInfo( 249, 0 ),
new _yyRuleInfo( 249, 2 ),
new _yyRuleInfo( 250, 1 ),
new _yyRuleInfo( 250, 3 ),
new _yyRuleInfo( 251, 1 ),
new _yyRuleInfo( 252, 0 ),
new _yyRuleInfo( 252, 4 ),
new _yyRuleInfo( 252, 2 ),
};

    //static void yy_accept(yyParser*);  /* Forward Declaration */

    /*
    ** Perform a reduce action and the shift that must immediately
    ** follow the reduce.
    */
    static void yy_reduce(
    yyParser yypParser,         /* The parser */
    int yyruleno                 /* Number of the rule by which to reduce */
    )
    {
      int yygoto;                     /* The next state */
      int yyact;                      /* The next action */
      YYMINORTYPE yygotominor;        /* The LHS of the rule reduced */
      yymsp yymsp; // yyStackEntry[] yymsp = new yyStackEntry[0];            /* The top of the parser's stack */
      int yysize;                     /* Amount to pop the stack */
      Parse pParse = yypParser.pParse; //sqlite3ParserARG_FETCH;

      yymsp = new yymsp(ref yypParser, yypParser.yyidx); //      yymsp[0] = yypParser.yystack[yypParser.yyidx];
#if !NDEBUG
      if (yyTraceFILE != null && yyruleno >= 0
      && yyruleno < yyRuleName.Length)
      { //(int)(yyRuleName.Length/sizeof(yyRuleName[0])) ){
        fprintf(yyTraceFILE, "%sReduce [%s].\n", yyTracePrompt,
        yyRuleName[yyruleno]);
      }
#endif // * NDEBUG */

      /* Silence complaints from purify about yygotominor being uninitialized
** in some cases when it is copied into the stack after the following
** switch.  yygotominor is uninitialized when a rule reduces that does
** not set the value of its left-hand side nonterminal.  Leaving the
** value of the nonterminal uninitialized is utterly harmless as long
** as the value is never used.  So really the only thing this code
** accomplishes is to quieten purify.
**
** 2007-01-16:  The wireshark project (www.wireshark.org) reports that
** without this code, their parser segfaults.  I'm not sure what there
** parser is doing to make this happen.  This is the second bug report
** from wireshark this week.  Clearly they are stressing Lemon in ways
** that it has not been previously stressed...  (SQLite ticket #2172)
*/
      yygotominor = new YYMINORTYPE(); //memset(yygotominor, 0, yygotominor).Length;
      switch (yyruleno)
      {
        /* Beginning here are the reduction cases.  A typical example
        ** follows:
        **   case 0:
        **  //#line <lineno> <grammarfile>
        **     { ... }           // User supplied code
        **  //#line <lineno> <thisfile>
        **     break;
        */
        case 5: /* explain ::= */
          //#line 109 "parse.y"
          { sqlite3BeginParse(pParse, 0); }
          //#line 2075 "parse.c"
          break;
        case 6: /* explain ::= EXPLAIN */
          //#line 111 "parse.y"
          { sqlite3BeginParse(pParse, 1); }
          //#line 2080 "parse.c"
          break;
        case 7: /* explain ::= EXPLAIN QUERY PLAN */
          //#line 112 "parse.y"
          { sqlite3BeginParse(pParse, 2); }
          //#line 2085 "parse.c"
          break;
        case 8: /* cmdx ::= cmd */
          //#line 114 "parse.y"
          { sqlite3FinishCoding(pParse); }
          //#line 2090 "parse.c"
          break;
        case 9: /* cmd ::= BEGIN transtype trans_opt */
          //#line 119 "parse.y"
          { sqlite3BeginTransaction(pParse, yymsp[-1].minor.yy328); }
          //#line 2095 "parse.c"
          break;
        case 13: /* transtype ::= */
          //#line 124 "parse.y"
          { yygotominor.yy328 = TK_DEFERRED; }
          //#line 2100 "parse.c"
          break;
        case 14: /* transtype ::= DEFERRED */
        case 15: /* transtype ::= IMMEDIATE */ //yytestcase(yyruleno==15);
        case 16: /* transtype ::= EXCLUSIVE */ //yytestcase(yyruleno==16);
        case 114: /* multiselect_op ::= UNION */ //yytestcase(yyruleno==114);
        case 116: /* multiselect_op ::= EXCEPT|INTERSECT */ //yytestcase(yyruleno==116);
          //#line 125 "parse.y"
          { yygotominor.yy328 = yymsp[0].major; }
          //#line 2109 "parse.c"
          break;
        case 17: /* cmd ::= COMMIT trans_opt */
        case 18: /* cmd ::= END trans_opt */ //yytestcase(yyruleno==18);
          //#line 128 "parse.y"
          { sqlite3CommitTransaction(pParse); }
          //#line 2115 "parse.c"
          break;
        case 19: /* cmd ::= ROLLBACK trans_opt */
          //#line 130 "parse.y"
          { sqlite3RollbackTransaction(pParse); }
          //#line 2120 "parse.c"
          break;
        case 22: /* cmd ::= SAVEPOINT nm */
          //#line 134 "parse.y"
          {
            sqlite3Savepoint(pParse, SAVEPOINT_BEGIN, yymsp[0].minor.yy0);
          }
          //#line 2127 "parse.c"
          break;
        case 23: /* cmd ::= RELEASE savepoint_opt nm */
          //#line 137 "parse.y"
          {
            sqlite3Savepoint(pParse, SAVEPOINT_RELEASE, yymsp[0].minor.yy0);
          }
          //#line 2134 "parse.c"
          break;
        case 24: /* cmd ::= ROLLBACK trans_opt TO savepoint_opt nm */
          //#line 140 "parse.y"
          {
            sqlite3Savepoint(pParse, SAVEPOINT_ROLLBACK, yymsp[0].minor.yy0);
          }
          //#line 2141 "parse.c"
          break;
        case 26: /* create_table ::= createkw temp TABLE ifnotexists nm dbnm */
          //#line 147 "parse.y"
          {
            sqlite3StartTable(pParse, yymsp[-1].minor.yy0, yymsp[0].minor.yy0, yymsp[-4].minor.yy328, 0, 0, yymsp[-2].minor.yy328);
          }
          //#line 2148 "parse.c"
          break;
        case 27: /* createkw ::= CREATE */
          //#line 150 "parse.y"
          {
            pParse.db.lookaside.bEnabled = 0;
            yygotominor.yy0 = yymsp[0].minor.yy0;
          }
          //#line 2156 "parse.c"
          break;
        case 28: /* ifnotexists ::= */
        case 31: /* temp ::= */ //yytestcase(yyruleno==31);
        case 70: /* autoinc ::= */ //yytestcase(yyruleno==70);
        case 84: /* init_deferred_pred_opt ::= */ //yytestcase(yyruleno==84);
        case 86: /* init_deferred_pred_opt ::= INITIALLY IMMEDIATE */ //yytestcase(yyruleno==86);
        case 97: /* defer_subclause_opt ::= */ //yytestcase(yyruleno==97);
        case 108: /* ifexists ::= */ //yytestcase(yyruleno==108);
        case 119: /* distinct ::= ALL */ //yytestcase(yyruleno==119);
        case 120: /* distinct ::= */ //yytestcase(yyruleno==120);
        case 222: /* between_op ::= BETWEEN */ //yytestcase(yyruleno==222);
        case 225: /* in_op ::= IN */ //yytestcase(yyruleno==225);
          //#line 155 "parse.y"
          { yygotominor.yy328 = 0; }
          //#line 2171 "parse.c"
          break;
        case 29: /* ifnotexists ::= IF NOT EXISTS */
        case 30: /* temp ::= TEMP */ //yytestcase(yyruleno==30);
        case 71: /* autoinc ::= AUTOINCR */ //yytestcase(yyruleno==71);
        case 85: /* init_deferred_pred_opt ::= INITIALLY DEFERRED */ //yytestcase(yyruleno==85);
        case 107: /* ifexists ::= IF EXISTS */ //yytestcase(yyruleno==107);
        case 118: /* distinct ::= DISTINCT */ //yytestcase(yyruleno==118);
        case 223: /* between_op ::= NOT BETWEEN */ //yytestcase(yyruleno==223);
        case 226: /* in_op ::= NOT IN */ //yytestcase(yyruleno==226);
          //#line 156 "parse.y"
          { yygotominor.yy328 = 1; }
          //#line 2183 "parse.c"
          break;
        case 32: /* create_table_args ::= LP columnlist conslist_opt RP */
          //#line 162 "parse.y"
          {
            sqlite3EndTable(pParse, yymsp[-1].minor.yy0, yymsp[0].minor.yy0, 0);
          }
          //#line 2190 "parse.c"
          break;
        case 33: /* create_table_args ::= AS select */
          //#line 165 "parse.y"
          {
            sqlite3EndTable(pParse, 0, 0, yymsp[0].minor.yy3);
            sqlite3SelectDelete(pParse.db, ref yymsp[0].minor.yy3);
          }
          //#line 2198 "parse.c"
          break;
        case 36: /* column ::= columnid type carglist */
          //#line 177 "parse.y"
          {
            //yygotominor.yy0.z = yymsp[-2].minor.yy0.z;
            //yygotominor.yy0.n = (int)(pParse.sLastToken.z-yymsp[-2].minor.yy0.z) + pParse.sLastToken.n;
            yygotominor.yy0.n = (int)(yymsp[-2].minor.yy0.z.Length - pParse.sLastToken.z.Length) + pParse.sLastToken.n;
            yygotominor.yy0.z = yymsp[-2].minor.yy0.z.Substring(0, yygotominor.yy0.n);
          }
          //#line 2206 "parse.c"
          break;
        case 37: /* columnid ::= nm */
          //#line 181 "parse.y"
          {
            sqlite3AddColumn(pParse, yymsp[0].minor.yy0);
            yygotominor.yy0 = yymsp[0].minor.yy0;
          }
          //#line 2214 "parse.c"
          break;
        case 38: /* id ::= ID */
        case 39: /* id ::= INDEXED */ //yytestcase(yyruleno==39);
        case 40: /* ids ::= ID|STRING */ //yytestcase(yyruleno==40);
        case 41: /* nm ::= id */ //yytestcase(yyruleno==41);
        case 42: /* nm ::= STRING */ //yytestcase(yyruleno==42);
        case 43: /* nm ::= JOIN_KW */ //yytestcase(yyruleno==43);
        case 46: /* typetoken ::= typename */ //yytestcase(yyruleno==46);
        case 49: /* typename ::= ids */ //yytestcase(yyruleno==49);
        case 126: /* as ::= AS nm */ //yytestcase(yyruleno==126);
        case 127: /* as ::= ids */ //yytestcase(yyruleno==127);
        case 137: /* dbnm ::= DOT nm */ //yytestcase(yyruleno==137);
        case 146: /* indexed_opt ::= INDEXED BY nm */ //yytestcase(yyruleno==146);
        case 251: /* collate ::= COLLATE ids */ //yytestcase(yyruleno==251);
        case 260: /* nmnum ::= plus_num */ //yytestcase(yyruleno==260);
        case 261: /* nmnum ::= nm */ //yytestcase(yyruleno==261);
        case 262: /* nmnum ::= ON */ //yytestcase(yyruleno==262);
        case 263: /* nmnum ::= DELETE */ //yytestcase(yyruleno==263);
        case 264: /* nmnum ::= DEFAULT */ //yytestcase(yyruleno==264);
        case 265: /* plus_num ::= plus_opt number */ //yytestcase(yyruleno==265);
        case 266: /* minus_num ::= MINUS number */ //yytestcase(yyruleno==266);
        case 267: /* number ::= INTEGER|FLOAT */ //yytestcase(yyruleno==267);
        case 285: /* trnm ::= nm */ //yytestcase( yyruleno == 285 );
          //#line 191 "parse.y"
          { yygotominor.yy0 = yymsp[0].minor.yy0; }
          //#line 2240 "parse.c"
          break;
        case 45: /* type ::= typetoken */
          //#line 253 "parse.y"
          { sqlite3AddColumnType(pParse, yymsp[0].minor.yy0); }
          //#line 2245 "parse.c"
          break;
        case 47: /* typetoken ::= typename LP signed RP */
          //#line 255 "parse.y"
          {
            //yygotominor.yy0.z = yymsp[-3].minor.yy0.z;
            //yygotominor.yy0.n = (int)( yymsp[0].minor.yy0.z[yymsp[0].minor.yy0.n] - yymsp[-3].minor.yy0.z );
            yygotominor.yy0.n = yymsp[-3].minor.yy0.z.Length - yymsp[0].minor.yy0.z.Length + yymsp[0].minor.yy0.n;
            yygotominor.yy0.z = yymsp[-3].minor.yy0.z.Substring(0, yygotominor.yy0.n);
          }
          //#line 2253 "parse.c"
          break;
        case 48: /* typetoken ::= typename LP signed COMMA signed RP */
          //#line 259 "parse.y"
          {
            //yygotominor.yy0.z = yymsp[-5].minor.yy0.z;
            //yygotominor.yy0.n = (int)(yymsp[0].minor.yy0.z[yymsp[0].minor.yy0.n] - yymsp[-5].minor.yy0.z);
            yygotominor.yy0.n = yymsp[-5].minor.yy0.z.Length - yymsp[0].minor.yy0.z.Length + 1;
            yygotominor.yy0.z = yymsp[-5].minor.yy0.z.Substring(0, yygotominor.yy0.n);
          }
          //#line 2261 "parse.c"
          break;
        case 50: /* typename ::= typename ids */
          //#line 265 "parse.y"
          {
            //yygotominor.yy0.z=yymsp[-1].minor.yy0.z; yygotominor.yy0.n=yymsp[0].minor.yy0.n+(int)(yymsp[0].minor.yy0.z-yymsp[-1].minor.yy0.z);
            yygotominor.yy0.z = yymsp[-1].minor.yy0.z;
            yygotominor.yy0.n = yymsp[0].minor.yy0.n + (int)(yymsp[-1].minor.yy0.z.Length - yymsp[0].minor.yy0.z.Length);
          }
          //#line 2266 "parse.c"
          break;
        case 57: /* ccons ::= DEFAULT term */
        case 59: /* ccons ::= DEFAULT PLUS term */ //yytestcase(yyruleno==59);
          //#line 276 "parse.y"
          { sqlite3AddDefaultValue(pParse, yymsp[0].minor.yy346); }
          //#line 2272 "parse.c"
          break;
        case 58: /* ccons ::= DEFAULT LP expr RP */
          //#line 277 "parse.y"
          { sqlite3AddDefaultValue(pParse, yymsp[-1].minor.yy346); }
          //#line 2277 "parse.c"
          break;
        case 60: /* ccons ::= DEFAULT MINUS term */
          //#line 279 "parse.y"
          {
            ExprSpan v = new ExprSpan();
            v.pExpr = sqlite3PExpr(pParse, TK_UMINUS, yymsp[0].minor.yy346.pExpr, 0, 0);
            v.zStart = yymsp[-1].minor.yy0.z;
            v.zEnd = yymsp[0].minor.yy346.zEnd;
            sqlite3AddDefaultValue(pParse, v);
          }
          //#line 2288 "parse.c"
          break;
        case 61: /* ccons ::= DEFAULT id */
          //#line 286 "parse.y"
          {
            ExprSpan v = new ExprSpan();
            spanExpr(v, pParse, TK_STRING, yymsp[0].minor.yy0);
            sqlite3AddDefaultValue(pParse, v);
          }
          //#line 2297 "parse.c"
          break;
        case 63: /* ccons ::= NOT NULL onconf */
          //#line 296 "parse.y"
          { sqlite3AddNotNull(pParse, yymsp[0].minor.yy328); }
          //#line 2302 "parse.c"
          break;
        case 64: /* ccons ::= PRIMARY KEY sortorder onconf autoinc */
          //#line 298 "parse.y"
          { sqlite3AddPrimaryKey(pParse, 0, yymsp[-1].minor.yy328, yymsp[0].minor.yy328, yymsp[-2].minor.yy328); }
          //#line 2307 "parse.c"
          break;
        case 65: /* ccons ::= UNIQUE onconf */
          //#line 299 "parse.y"
          { sqlite3CreateIndex(pParse, 0, 0, 0, 0, yymsp[0].minor.yy328, 0, 0, 0, 0); }
          //#line 2312 "parse.c"
          break;
        case 66: /* ccons ::= CHECK LP expr RP */
          //#line 300 "parse.y"
          { sqlite3AddCheckConstraint(pParse, yymsp[-1].minor.yy346.pExpr); }
          //#line 2317 "parse.c"
          break;
        case 67: /* ccons ::= REFERENCES nm idxlist_opt refargs */
          //#line 302 "parse.y"
          { sqlite3CreateForeignKey(pParse, 0, yymsp[-2].minor.yy0, yymsp[-1].minor.yy14, yymsp[0].minor.yy328); }
          //#line 2322 "parse.c"
          break;
        case 68: /* ccons ::= defer_subclause */
          //#line 303 "parse.y"
          { sqlite3DeferForeignKey(pParse, yymsp[0].minor.yy328); }
          //#line 2327 "parse.c"
          break;
        case 69: /* ccons ::= COLLATE ids */
          //#line 304 "parse.y"
          { sqlite3AddCollateType(pParse, yymsp[0].minor.yy0); }
          //#line 2332 "parse.c"
          break;
        case 72: /* refargs ::= */
          //#line 317 "parse.y"
          { yygotominor.yy328 = OE_Restrict * 0x010101; }
          //#line 2337 "parse.c"
          break;
        case 73: /* refargs ::= refargs refarg */
          //#line 318 "parse.y"
          { yygotominor.yy328 = (yymsp[-1].minor.yy328 & ~yymsp[0].minor.yy429.mask) | yymsp[0].minor.yy429.value; }
          //#line 2342 "parse.c"
          break;
        case 74: /* refarg ::= MATCH nm */
          //#line 320 "parse.y"
          { yygotominor.yy429.value = 0; yygotominor.yy429.mask = 0x000000; }
          //#line 2347 "parse.c"
          break;
        case 75: /* refarg ::= ON DELETE refact */
          //#line 321 "parse.y"
          { yygotominor.yy429.value = yymsp[0].minor.yy328; yygotominor.yy429.mask = 0x0000ff; }
          //#line 2352 "parse.c"
          break;
        case 76: /* refarg ::= ON UPDATE refact */
          //#line 322 "parse.y"
          { yygotominor.yy429.value = yymsp[0].minor.yy328 << 8; yygotominor.yy429.mask = 0x00ff00; }
          //#line 2357 "parse.c"
          break;
        case 77: /* refarg ::= ON INSERT refact */
          //#line 323 "parse.y"
          { yygotominor.yy429.value = yymsp[0].minor.yy328 << 16; yygotominor.yy429.mask = 0xff0000; }
          //#line 2362 "parse.c"
          break;
        case 78: /* refact ::= SET NULL */
          //#line 325 "parse.y"
          { yygotominor.yy328 = OE_SetNull; }
          //#line 2367 "parse.c"
          break;
        case 79: /* refact ::= SET DEFAULT */
          //#line 326 "parse.y"
          { yygotominor.yy328 = OE_SetDflt; }
          //#line 2372 "parse.c"
          break;
        case 80: /* refact ::= CASCADE */
          //#line 327 "parse.y"
          { yygotominor.yy328 = OE_Cascade; }
          //#line 2377 "parse.c"
          break;
        case 81: /* refact ::= RESTRICT */
          //#line 328 "parse.y"
          { yygotominor.yy328 = OE_Restrict; }
          //#line 2382 "parse.c"
          break;
        case 82: /* defer_subclause ::= NOT DEFERRABLE init_deferred_pred_opt */
        case 83: /* defer_subclause ::= DEFERRABLE init_deferred_pred_opt */ //yytestcase(yyruleno==83);
        case 98: /* defer_subclause_opt ::= defer_subclause */ //yytestcase(yyruleno==98);
        case 100: /* onconf ::= ON CONFLICT resolvetype */ //yytestcase(yyruleno==100);
        case 103: /* resolvetype ::= raisetype */ //yytestcase(yyruleno==103);
          //#line 330 "parse.y"
          { yygotominor.yy328 = yymsp[0].minor.yy328; }
          //#line 2391 "parse.c"
          break;
        case 87: /* conslist_opt ::= */
          //#line 340 "parse.y"
          { yygotominor.yy0.n = 0; yygotominor.yy0.z = null; }
          //#line 2396 "parse.c"
          break;
        case 88: /* conslist_opt ::= COMMA conslist */
          //#line 341 "parse.y"
          { yygotominor.yy0 = yymsp[-1].minor.yy0; }
          //#line 2401 "parse.c"
          break;
        case 93: /* tcons ::= PRIMARY KEY LP idxlist autoinc RP onconf */
          //#line 347 "parse.y"
          { sqlite3AddPrimaryKey(pParse, yymsp[-3].minor.yy14, yymsp[0].minor.yy328, yymsp[-2].minor.yy328, 0); }
          //#line 2406 "parse.c"
          break;
        case 94: /* tcons ::= UNIQUE LP idxlist RP onconf */
          //#line 349 "parse.y"
          { sqlite3CreateIndex(pParse, 0, 0, 0, yymsp[-2].minor.yy14, yymsp[0].minor.yy328, 0, 0, 0, 0); }
          //#line 2411 "parse.c"
          break;
        case 95: /* tcons ::= CHECK LP expr RP onconf */
          //#line 351 "parse.y"
          { sqlite3AddCheckConstraint(pParse, yymsp[-2].minor.yy346.pExpr); }
          //#line 2416 "parse.c"
          break;
        case 96: /* tcons ::= FOREIGN KEY LP idxlist RP REFERENCES nm idxlist_opt refargs defer_subclause_opt */
          //#line 353 "parse.y"
          {
            sqlite3CreateForeignKey(pParse, yymsp[-6].minor.yy14, yymsp[-3].minor.yy0, yymsp[-2].minor.yy14, yymsp[-1].minor.yy328);
            sqlite3DeferForeignKey(pParse, yymsp[0].minor.yy328);
          }
          //#line 2424 "parse.c"
          break;
        case 99: /* onconf ::= */
          //#line 367 "parse.y"
          { yygotominor.yy328 = OE_Default; }
          //#line 2429 "parse.c"
          break;
        case 101: /* orconf ::= */
          //#line 369 "parse.y"
          { yygotominor.yy186 = OE_Default; }
          //#line 2434 "parse.c"
          break;
        case 102: /* orconf ::= OR resolvetype */
          //#line 370 "parse.y"
          { yygotominor.yy186 = (u8)yymsp[0].minor.yy328; }
          //#line 2439 "parse.c"
          break;
        case 104: /* resolvetype ::= IGNORE */
          //#line 372 "parse.y"
          { yygotominor.yy328 = OE_Ignore; }
          //#line 2444 "parse.c"
          break;
        case 105: /* resolvetype ::= REPLACE */
          //#line 373 "parse.y"
          { yygotominor.yy328 = OE_Replace; }
          //#line 2449 "parse.c"
          break;
        case 106: /* cmd ::= DROP TABLE ifexists fullname */
          //#line 377 "parse.y"
          {
            sqlite3DropTable(pParse, yymsp[0].minor.yy65, 0, yymsp[-1].minor.yy328);
          }
          //#line 2456 "parse.c"
          break;
        case 109: /* cmd ::= createkw temp VIEW ifnotexists nm dbnm AS select */
          //#line 387 "parse.y"
          {
            sqlite3CreateView(pParse, yymsp[-7].minor.yy0, yymsp[-3].minor.yy0, yymsp[-2].minor.yy0, yymsp[0].minor.yy3, yymsp[-6].minor.yy328, yymsp[-4].minor.yy328);
          }
          //#line 2463 "parse.c"
          break;
        case 110: /* cmd ::= DROP VIEW ifexists fullname */
          //#line 390 "parse.y"
          {
            sqlite3DropTable(pParse, yymsp[0].minor.yy65, 1, yymsp[-1].minor.yy328);
          }
          //#line 2470 "parse.c"
          break;
        case 111: /* cmd ::= select */
          //#line 397 "parse.y"
          {
            SelectDest dest = new SelectDest(SRT_Output, '\0', 0, 0, 0);
            sqlite3Select(pParse, yymsp[0].minor.yy3, ref dest);
            sqlite3SelectDelete(pParse.db, ref yymsp[0].minor.yy3);
          }
          //#line 2479 "parse.c"
          break;
        case 112: /* select ::= oneselect */
          //#line 408 "parse.y"
          { yygotominor.yy3 = yymsp[0].minor.yy3; }
          //#line 2484 "parse.c"
          break;
        case 113: /* select ::= select multiselect_op oneselect */
          //#line 410 "parse.y"
          {
            if (yymsp[0].minor.yy3 != null)
            {
              yymsp[0].minor.yy3.op = (u8)yymsp[-1].minor.yy328;
              yymsp[0].minor.yy3.pPrior = yymsp[-2].minor.yy3;
            }
            else
            {
              sqlite3SelectDelete(pParse.db, ref yymsp[-2].minor.yy3);
            }
            yygotominor.yy3 = yymsp[0].minor.yy3;
          }
          //#line 2497 "parse.c"
          break;
        case 115: /* multiselect_op ::= UNION ALL */
          //#line 421 "parse.y"
          { yygotominor.yy328 = TK_ALL; }
          //#line 2502 "parse.c"
          break;
        case 117: /* oneselect ::= SELECT distinct selcollist from where_opt groupby_opt having_opt orderby_opt limit_opt */
          //#line 425 "parse.y"
          {
            yygotominor.yy3 = sqlite3SelectNew(pParse, yymsp[-6].minor.yy14, yymsp[-5].minor.yy65, yymsp[-4].minor.yy132, yymsp[-3].minor.yy14, yymsp[-2].minor.yy132, yymsp[-1].minor.yy14, yymsp[-7].minor.yy328, yymsp[0].minor.yy476.pLimit, yymsp[0].minor.yy476.pOffset);
          }
          //#line 2509 "parse.c"
          break;
        case 121: /* sclp ::= selcollist COMMA */
        case 247: /* idxlist_opt ::= LP idxlist RP */ //yytestcase(yyruleno==247);
          //#line 446 "parse.y"
          { yygotominor.yy14 = yymsp[-1].minor.yy14; }
          //#line 2515 "parse.c"
          break;
        case 122: /* sclp ::= */
        case 150: /* orderby_opt ::= */ //yytestcase(yyruleno==150);
        case 158: /* groupby_opt ::= */ //yytestcase(yyruleno==158);
        case 240: /* exprlist ::= */ //yytestcase(yyruleno==240);
        case 246: /* idxlist_opt ::= */ //yytestcase(yyruleno==246);
          //#line 447 "parse.y"
          { yygotominor.yy14 = null; }
          //#line 2524 "parse.c"
          break;
        case 123: /* selcollist ::= sclp expr as */
          //#line 448 "parse.y"
          {
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yymsp[-2].minor.yy14, yymsp[-1].minor.yy346.pExpr);
            if (yymsp[0].minor.yy0.n > 0) sqlite3ExprListSetName(pParse, yygotominor.yy14, yymsp[0].minor.yy0, 1);
            sqlite3ExprListSetSpan(pParse, yygotominor.yy14, yymsp[-1].minor.yy346);
          }
          //#line 2533 "parse.c"
          break;
        case 124: /* selcollist ::= sclp STAR */
          //#line 453 "parse.y"
          {
            Expr p = sqlite3Expr(pParse.db, TK_ALL, null);
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yymsp[-1].minor.yy14, p);
          }
          //#line 2541 "parse.c"
          break;
        case 125: /* selcollist ::= sclp nm DOT STAR */
          //#line 457 "parse.y"
          {
            Expr pRight = sqlite3PExpr(pParse, TK_ALL, 0, 0, yymsp[0].minor.yy0);
            Expr pLeft = sqlite3PExpr(pParse, TK_ID, 0, 0, yymsp[-2].minor.yy0);
            Expr pDot = sqlite3PExpr(pParse, TK_DOT, pLeft, pRight, 0);
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yymsp[-3].minor.yy14, pDot);
          }
          //#line 2551 "parse.c"
          break;
        case 128: /* as ::= */
          //#line 470 "parse.y"
          { yygotominor.yy0.n = 0; }
          //#line 2556 "parse.c"
          break;
        case 129: /* from ::= */
          //#line 482 "parse.y"
          { yygotominor.yy65 = new SrcList(); }//sqlite3DbMallocZero(pParse.db, sizeof(*yygotominor.yy65));
          //#line 2561 "parse.c"
          break;
        case 130: /* from ::= FROM seltablist */
          //#line 483 "parse.y"
          {
            yygotominor.yy65 = yymsp[0].minor.yy65;
            sqlite3SrcListShiftJoinType(yygotominor.yy65);
          }
          //#line 2569 "parse.c"
          break;
        case 131: /* stl_prefix ::= seltablist joinop */
          //#line 491 "parse.y"
          {
            yygotominor.yy65 = yymsp[-1].minor.yy65;
            if (ALWAYS(yygotominor.yy65 != null && yygotominor.yy65.nSrc > 0)) yygotominor.yy65.a[yygotominor.yy65.nSrc - 1].jointype = (u8)yymsp[0].minor.yy328;
          }
          //#line 2577 "parse.c"
          break;
        case 132: /* stl_prefix ::= */
          //#line 495 "parse.y"
          { yygotominor.yy65 = null; }
          //#line 2582 "parse.c"
          break;
        case 133: /* seltablist ::= stl_prefix nm dbnm as indexed_opt on_opt using_opt */
          //#line 496 "parse.y"
          {
            yygotominor.yy65 = sqlite3SrcListAppendFromTerm(pParse, yymsp[-6].minor.yy65, yymsp[-5].minor.yy0, yymsp[-4].minor.yy0, yymsp[-3].minor.yy0, 0, yymsp[-1].minor.yy132, yymsp[0].minor.yy408);
            sqlite3SrcListIndexedBy(pParse, yygotominor.yy65, yymsp[-2].minor.yy0);
          }
          //#line 2590 "parse.c"
          break;
        case 134: /* seltablist ::= stl_prefix LP select RP as on_opt using_opt */
          //#line 502 "parse.y"
          {
            yygotominor.yy65 = sqlite3SrcListAppendFromTerm(pParse, yymsp[-6].minor.yy65, 0, 0, yymsp[-2].minor.yy0, yymsp[-4].minor.yy3, yymsp[-1].minor.yy132, yymsp[0].minor.yy408);
          }
          //#line 2597 "parse.c"
          break;
        case 135: /* seltablist ::= stl_prefix LP seltablist RP as on_opt using_opt */
          //#line 506 "parse.y"
          {
            if (yymsp[-6].minor.yy65 == null && yymsp[-2].minor.yy0.n == 0 && yymsp[-1].minor.yy132 == null && yymsp[0].minor.yy408 == null)
            {
              yygotominor.yy65 = yymsp[-4].minor.yy65;
            }
            else
            {
              Select pSubquery;
              sqlite3SrcListShiftJoinType(yymsp[-4].minor.yy65);
              pSubquery = sqlite3SelectNew(pParse, 0, yymsp[-4].minor.yy65, 0, 0, 0, 0, 0, 0, 0);
              yygotominor.yy65 = sqlite3SrcListAppendFromTerm(pParse, yymsp[-6].minor.yy65, 0, 0, yymsp[-2].minor.yy0, pSubquery, yymsp[-1].minor.yy132, yymsp[0].minor.yy408);
            }
          }
          //#line 2611 "parse.c"
          break;
        case 136: /* dbnm ::= */
        case 145: /* indexed_opt ::= */ //yytestcase(yyruleno==145);
          //#line 531 "parse.y"
          { yygotominor.yy0.z = null; yygotominor.yy0.n = 0; }
          //#line 2617 "parse.c"
          break;
        case 138: /* fullname ::= nm dbnm */
          //#line 536 "parse.y"
          { yygotominor.yy65 = sqlite3SrcListAppend(pParse.db, 0, yymsp[-1].minor.yy0, yymsp[0].minor.yy0); }
          //#line 2622 "parse.c"
          break;
        case 139: /* joinop ::= COMMA|JOIN */
          //#line 540 "parse.y"
          { yygotominor.yy328 = JT_INNER; }
          //#line 2627 "parse.c"
          break;
        case 140: /* joinop ::= JOIN_KW JOIN */
          //#line 541 "parse.y"
          { yygotominor.yy328 = sqlite3JoinType(pParse, yymsp[-1].minor.yy0, 0, 0); }
          //#line 2632 "parse.c"
          break;
        case 141: /* joinop ::= JOIN_KW nm JOIN */
          //#line 542 "parse.y"
          { yygotominor.yy328 = sqlite3JoinType(pParse, yymsp[-2].minor.yy0, yymsp[-1].minor.yy0, 0); }
          //#line 2637 "parse.c"
          break;
        case 142: /* joinop ::= JOIN_KW nm nm JOIN */
          //#line 544 "parse.y"
          { yygotominor.yy328 = sqlite3JoinType(pParse, yymsp[-3].minor.yy0, yymsp[-2].minor.yy0, yymsp[-1].minor.yy0); }
          //#line 2642 "parse.c"
          break;
        case 143: /* on_opt ::= ON expr */
        case 154: /* sortitem ::= expr */ //yytestcase(yyruleno==154);
        case 161: /* having_opt ::= HAVING expr */ //yytestcase(yyruleno==161);
        case 168: /* where_opt ::= WHERE expr */ //yytestcase(yyruleno==168);
        case 235: /* case_else ::= ELSE expr */ //yytestcase(yyruleno==235);
        case 237: /* case_operand ::= expr */ //yytestcase(yyruleno==237);
          //#line 548 "parse.y"
          { yygotominor.yy132 = yymsp[0].minor.yy346.pExpr; }
          //#line 2652 "parse.c"
          break;
        case 144: /* on_opt ::= */
        case 160: /* having_opt ::= */ //yytestcase(yyruleno==160);
        case 167: /* where_opt ::= */ //yytestcase(yyruleno==167);
        case 236: /* case_else ::= */ //yytestcase(yyruleno==236);
        case 238: /* case_operand ::= */ //yytestcase(yyruleno==238);
          //#line 549 "parse.y"
          { yygotominor.yy132 = null; }
          //#line 2661 "parse.c"
          break;
        case 147: /* indexed_opt ::= NOT INDEXED */
          //#line 564 "parse.y"
          { yygotominor.yy0.z = null; yygotominor.yy0.n = 1; }
          //#line 2666 "parse.c"
          break;
        case 148: /* using_opt ::= USING LP inscollist RP */
        case 180: /* inscollist_opt ::= LP inscollist RP */ //yytestcase(yyruleno==180);
          //#line 568 "parse.y"
          { yygotominor.yy408 = yymsp[-1].minor.yy408; }
          //#line 2672 "parse.c"
          break;
        case 149: /* using_opt ::= */
        case 179: /* inscollist_opt ::= */ //yytestcase(yyruleno==179);
          //#line 569 "parse.y"null
          { yygotominor.yy408 = null; }
          //#line 2678 "parse.c"
          break;
        case 151: /* orderby_opt ::= ORDER BY sortlist */
        case 159: /* groupby_opt ::= GROUP BY nexprlist */ //yytestcase(yyruleno==159);
        case 239: /* exprlist ::= nexprlist */ //yytestcase(yyruleno==239);
          //#line 580 "parse.y"
          { yygotominor.yy14 = yymsp[0].minor.yy14; }
          //#line 2685 "parse.c"
          break;
        case 152: /* sortlist ::= sortlist COMMA sortitem sortorder */
          //#line 581 "parse.y"
          {
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yymsp[-3].minor.yy14, yymsp[-1].minor.yy132);
            if (yygotominor.yy14 != null) yygotominor.yy14.a[yygotominor.yy14.nExpr - 1].sortOrder = (u8)yymsp[0].minor.yy328;
          }
          //#line 2693 "parse.c"
          break;
        case 153: /* sortlist ::= sortitem sortorder */
          //#line 585 "parse.y"
          {
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, 0, yymsp[-1].minor.yy132);
            if (yygotominor.yy14 != null && ALWAYS(yygotominor.yy14.a)) yygotominor.yy14.a[0].sortOrder = (u8)yymsp[0].minor.yy328;
          }
          //#line 2701 "parse.c"
          break;
        case 155: /* sortorder ::= ASC */
        case 157: /* sortorder ::= */ //yytestcase(yyruleno==157);
          //#line 593 "parse.y"
          { yygotominor.yy328 = SQLITE_SO_ASC; }
          //#line 2707 "parse.c"
          break;
        case 156: /* sortorder ::= DESC */
          //#line 594 "parse.y"
          { yygotominor.yy328 = SQLITE_SO_DESC; }
          //#line 2712 "parse.c"
          break;
        case 162: /* limit_opt ::= */
          //#line 620 "parse.y"
          { yygotominor.yy476.pLimit = null; yygotominor.yy476.pOffset = null; }
          //#line 2717 "parse.c"
          break;
        case 163: /* limit_opt ::= LIMIT expr */
          //#line 621 "parse.y"
          { yygotominor.yy476.pLimit = yymsp[0].minor.yy346.pExpr; yygotominor.yy476.pOffset = null; }
          //#line 2722 "parse.c"
          break;
        case 164: /* limit_opt ::= LIMIT expr OFFSET expr */
          //#line 623 "parse.y"
          { yygotominor.yy476.pLimit = yymsp[-2].minor.yy346.pExpr; yygotominor.yy476.pOffset = yymsp[0].minor.yy346.pExpr; }
          //#line 2727 "parse.c"
          break;
        case 165: /* limit_opt ::= LIMIT expr COMMA expr */
          //#line 625 "parse.y"
          { yygotominor.yy476.pOffset = yymsp[-2].minor.yy346.pExpr; yygotominor.yy476.pLimit = yymsp[0].minor.yy346.pExpr; }
          //#line 2732 "parse.c"
          break;
        case 166: /* cmd ::= DELETE FROM fullname indexed_opt where_opt */
          //#line 638 "parse.y"
          {
            sqlite3SrcListIndexedBy(pParse, yymsp[-2].minor.yy65, yymsp[-1].minor.yy0);
            sqlite3DeleteFrom(pParse, yymsp[-2].minor.yy65, yymsp[0].minor.yy132);
          }
          //#line 2740 "parse.c"
          break;
        case 169: /* cmd ::= UPDATE orconf fullname indexed_opt SET setlist where_opt */
          //#line 661 "parse.y"
          {
            sqlite3SrcListIndexedBy(pParse, yymsp[-4].minor.yy65, yymsp[-3].minor.yy0);
            sqlite3ExprListCheckLength(pParse, yymsp[-1].minor.yy14, "set list");
            sqlite3Update(pParse, yymsp[-4].minor.yy65, yymsp[-1].minor.yy14, yymsp[0].minor.yy132, yymsp[-5].minor.yy186);
          }
          //#line 2749 "parse.c"
          break;
        case 170: /* setlist ::= setlist COMMA nm EQ expr */
          //#line 671 "parse.y"
          {
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yymsp[-4].minor.yy14, yymsp[0].minor.yy346.pExpr);
            sqlite3ExprListSetName(pParse, yygotominor.yy14, yymsp[-2].minor.yy0, 1);
          }
          //#line 2757 "parse.c"
          break;
        case 171: /* setlist ::= nm EQ expr */
          //#line 675 "parse.y"
          {
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, 0, yymsp[0].minor.yy346.pExpr);
            sqlite3ExprListSetName(pParse, yygotominor.yy14, yymsp[-2].minor.yy0, 1);
          }
          //#line 2765 "parse.c"
          break;
        case 172: /* cmd ::= insert_cmd INTO fullname inscollist_opt VALUES LP itemlist RP */
          //#line 684 "parse.y"
          { sqlite3Insert(pParse, yymsp[-5].minor.yy65, yymsp[-1].minor.yy14, 0, yymsp[-4].minor.yy408, yymsp[-7].minor.yy186); }
          //#line 2770 "parse.c"
          break;
        case 173: /* cmd ::= insert_cmd INTO fullname inscollist_opt select */
          //#line 686 "parse.y"
          { sqlite3Insert(pParse, yymsp[-2].minor.yy65, 0, yymsp[0].minor.yy3, yymsp[-1].minor.yy408, yymsp[-4].minor.yy186); }
          //#line 2775 "parse.c"
          break;
        case 174: /* cmd ::= insert_cmd INTO fullname inscollist_opt DEFAULT VALUES */
          //#line 688 "parse.y"
          { sqlite3Insert(pParse, yymsp[-3].minor.yy65, 0, 0, yymsp[-2].minor.yy408, yymsp[-5].minor.yy186); }
          //#line 2780 "parse.c"
          break;
        case 175: /* insert_cmd ::= INSERT orconf */
          //#line 691 "parse.y"
          { yygotominor.yy186 = yymsp[0].minor.yy186; }
          //#line 2785 "parse.c"
          break;
        case 176: /* insert_cmd ::= REPLACE */
          //#line 692 "parse.y"
          { yygotominor.yy186 = OE_Replace; }
          //#line 2790 "parse.c"
          break;
        case 177: /* itemlist ::= itemlist COMMA expr */
        case 241: /* nexprlist ::= nexprlist COMMA expr */ //yytestcase(yyruleno==241);
          //#line 699 "parse.y"
          { yygotominor.yy14 = sqlite3ExprListAppend(pParse, yymsp[-2].minor.yy14, yymsp[0].minor.yy346.pExpr); }
          //#line 2796 "parse.c"
          break;
        case 178: /* itemlist ::= expr */
        case 242: /* nexprlist ::= expr */ //yytestcase(yyruleno==242);
          //#line 701 "parse.y"
          { yygotominor.yy14 = sqlite3ExprListAppend(pParse, 0, yymsp[0].minor.yy346.pExpr); }
          //#line 2802 "parse.c"
          break;
        case 181: /* inscollist ::= inscollist COMMA nm */
          //#line 711 "parse.y"
          { yygotominor.yy408 = sqlite3IdListAppend(pParse.db, yymsp[-2].minor.yy408, yymsp[0].minor.yy0); }
          //#line 2807 "parse.c"
          break;
        case 182: /* inscollist ::= nm */
          //#line 713 "parse.y"
          { yygotominor.yy408 = sqlite3IdListAppend(pParse.db, 0, yymsp[0].minor.yy0); }
          //#line 2812 "parse.c"
          break;
        case 183: /* expr ::= term */
        case 211: /* escape ::= ESCAPE expr */ //yytestcase(yyruleno==211);
          //#line 744 "parse.y"
          { yygotominor.yy346 = yymsp[0].minor.yy346; }
          //#line 2818 "parse.c"
          break;
        case 184: /* expr ::= LP expr RP */
          //#line 745 "parse.y"
          { yygotominor.yy346.pExpr = yymsp[-1].minor.yy346.pExpr; spanSet(yygotominor.yy346, yymsp[-2].minor.yy0, yymsp[0].minor.yy0); }
          //#line 2823 "parse.c"
          break;
        case 185: /* term ::= NULL */
        case 190: /* term ::= INTEGER|FLOAT|BLOB */ //yytestcase(yyruleno==190);
        case 191: /* term ::= STRING */ //yytestcase(yyruleno==191);
          //#line 746 "parse.y"
          { spanExpr(yygotominor.yy346, pParse, yymsp[0].major, yymsp[0].minor.yy0); }
          //#line 2830 "parse.c"
          break;
        case 186: /* expr ::= id */
        case 187: /* expr ::= JOIN_KW */ //yytestcase(yyruleno==187);
          //#line 747 "parse.y"
          { spanExpr(yygotominor.yy346, pParse, TK_ID, yymsp[0].minor.yy0); }
          //#line 2836 "parse.c"
          break;
        case 188: /* expr ::= nm DOT nm */
          //#line 749 "parse.y"
          {
            Expr temp1 = sqlite3PExpr(pParse, TK_ID, 0, 0, yymsp[-2].minor.yy0);
            Expr temp2 = sqlite3PExpr(pParse, TK_ID, 0, 0, yymsp[0].minor.yy0);
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_DOT, temp1, temp2, 0);
            spanSet(yygotominor.yy346, yymsp[-2].minor.yy0, yymsp[0].minor.yy0);
          }
          //#line 2846 "parse.c"
          break;
        case 189: /* expr ::= nm DOT nm DOT nm */
          //#line 755 "parse.y"
          {
            Expr temp1 = sqlite3PExpr(pParse, TK_ID, 0, 0, yymsp[-4].minor.yy0);
            Expr temp2 = sqlite3PExpr(pParse, TK_ID, 0, 0, yymsp[-2].minor.yy0);
            Expr temp3 = sqlite3PExpr(pParse, TK_ID, 0, 0, yymsp[0].minor.yy0);
            Expr temp4 = sqlite3PExpr(pParse, TK_DOT, temp2, temp3, 0);
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_DOT, temp1, temp4, 0);
            spanSet(yygotominor.yy346, yymsp[-4].minor.yy0, yymsp[0].minor.yy0);
          }
          //#line 2858 "parse.c"
          break;
        case 192: /* expr ::= REGISTER */
          //#line 765 "parse.y"
          {
            /* When doing a nested parse, one can include terms in an expression
            ** that look like this:   #1 #2 ...  These terms refer to registers
            ** in the virtual machine.  #N is the N-th register. */
            if (pParse.nested == 0)
            {
              sqlite3ErrorMsg(pParse, "near \"%T\": syntax error", yymsp[0].minor.yy0);
              yygotominor.yy346.pExpr = null;
            }
            else
            {
              yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_REGISTER, 0, 0, yymsp[0].minor.yy0);
              if (yygotominor.yy346.pExpr != null) sqlite3GetInt32(yymsp[0].minor.yy0.z.Substring(1), ref yygotominor.yy346.pExpr.iTable);
            }
            spanSet(yygotominor.yy346, yymsp[0].minor.yy0, yymsp[0].minor.yy0);
          }
          //#line 2875 "parse.c"
          break;
        case 193: /* expr ::= VARIABLE */
          //#line 778 "parse.y"
          {
            spanExpr(yygotominor.yy346, pParse, TK_VARIABLE, yymsp[0].minor.yy0);
            sqlite3ExprAssignVarNumber(pParse, yygotominor.yy346.pExpr);
            spanSet(yygotominor.yy346, yymsp[0].minor.yy0, yymsp[0].minor.yy0);
          }
          //#line 2884 "parse.c"
          break;
        case 194: /* expr ::= expr COLLATE ids */
          //#line 783 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3ExprSetColl(pParse, yymsp[-2].minor.yy346.pExpr, yymsp[0].minor.yy0);
            yygotominor.yy346.zStart = yymsp[-2].minor.yy346.zStart;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n);
          }
          //#line 2893 "parse.c"
          break;
        case 195: /* expr ::= CAST LP expr AS typetoken RP */
          //#line 789 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_CAST, yymsp[-3].minor.yy346.pExpr, 0, yymsp[-1].minor.yy0);
            spanSet(yygotominor.yy346, yymsp[-5].minor.yy0, yymsp[0].minor.yy0);
          }
          //#line 2901 "parse.c"
          break;
        case 196: /* expr ::= ID LP distinct exprlist RP */
          //#line 794 "parse.y"
          {
            if (yymsp[-1].minor.yy14 != null && yymsp[-1].minor.yy14.nExpr > pParse.db.aLimit[SQLITE_LIMIT_FUNCTION_ARG])
            {
              sqlite3ErrorMsg(pParse, "too many arguments on function %T", yymsp[-4].minor.yy0);
            }
            yygotominor.yy346.pExpr = sqlite3ExprFunction(pParse, yymsp[-1].minor.yy14, yymsp[-4].minor.yy0);
            spanSet(yygotominor.yy346, yymsp[-4].minor.yy0, yymsp[0].minor.yy0);
            if (yymsp[-2].minor.yy328 != 0 && yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.flags |= EP_Distinct;
            }
          }
          //#line 2915 "parse.c"
          break;
        case 197: /* expr ::= ID LP STAR RP */
          //#line 804 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3ExprFunction(pParse, 0, yymsp[-3].minor.yy0);
            spanSet(yygotominor.yy346, yymsp[-3].minor.yy0, yymsp[0].minor.yy0);
          }
          //#line 2923 "parse.c"
          break;
        case 198: /* term ::= CTIME_KW */
          //#line 808 "parse.y"
          {
            /* The CURRENT_TIME, CURRENT_DATE, and CURRENT_TIMESTAMP values are
            ** treated as functions that return constants */
            yygotominor.yy346.pExpr = sqlite3ExprFunction(pParse, 0, yymsp[0].minor.yy0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.op = TK_CONST_FUNC;
            }
            spanSet(yygotominor.yy346, yymsp[0].minor.yy0, yymsp[0].minor.yy0);
          }
          //#line 2936 "parse.c"
          break;
        case 199: /* expr ::= expr AND expr */
        case 200: /* expr ::= expr OR expr */ //yytestcase(yyruleno==200);
        case 201: /* expr ::= expr LT|GT|GE|LE expr */ //yytestcase(yyruleno==201);
        case 202: /* expr ::= expr EQ|NE expr */ //yytestcase(yyruleno==202);
        case 203: /* expr ::= expr BITAND|BITOR|LSHIFT|RSHIFT expr */ //yytestcase(yyruleno==203);
        case 204: /* expr ::= expr PLUS|MINUS expr */ //yytestcase(yyruleno==204);
        case 205: /* expr ::= expr STAR|SLASH|REM expr */ //yytestcase(yyruleno==205);
        case 206: /* expr ::= expr CONCAT expr */ //yytestcase(yyruleno==206);
          //#line 835 "parse.y"
          { spanBinaryExpr(yygotominor.yy346, pParse, yymsp[-1].major, yymsp[-2].minor.yy346, yymsp[0].minor.yy346); }
          //#line 2948 "parse.c"
          break;
        case 207: /* likeop ::= LIKE_KW */
        case 209: /* likeop ::= MATCH */ //yytestcase(yyruleno==209);
          //#line 848 "parse.y"
          { yygotominor.yy96.eOperator = yymsp[0].minor.yy0; yygotominor.yy96.not = false; }
          //#line 2954 "parse.c"
          break;
        case 208: /* likeop ::= NOT LIKE_KW */
        case 210: /* likeop ::= NOT MATCH */ //yytestcase(yyruleno==210);
          //#line 849 "parse.y"
          { yygotominor.yy96.eOperator = yymsp[0].minor.yy0; yygotominor.yy96.not = true; }
          //#line 2960 "parse.c"
          break;
        case 212: /* escape ::= */
          //#line 855 "parse.y"
          { yygotominor.yy346 = new ExprSpan(); }// memset( yygotominor.yy346, 0, sizeof( yygotominor.yy346 ) ); 
          //#line 2965 "parse.c"
          break;
        case 213: /* expr ::= expr likeop expr escape */
          //#line 856 "parse.y"
          {
            ExprList pList;
            pList = sqlite3ExprListAppend(pParse, 0, yymsp[-1].minor.yy346.pExpr);
            pList = sqlite3ExprListAppend(pParse, pList, yymsp[-3].minor.yy346.pExpr);
            if (yymsp[0].minor.yy346.pExpr != null)
            {
              pList = sqlite3ExprListAppend(pParse, pList, yymsp[0].minor.yy346.pExpr);
            }
            yygotominor.yy346.pExpr = sqlite3ExprFunction(pParse, pList, yymsp[-2].minor.yy96.eOperator);
            if (yymsp[-2].minor.yy96.not) yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_NOT, yygotominor.yy346.pExpr, 0, 0);
            yygotominor.yy346.zStart = yymsp[-3].minor.yy346.zStart;
            yygotominor.yy346.zEnd = yymsp[-1].minor.yy346.zEnd;
            if (yygotominor.yy346.pExpr != null) yygotominor.yy346.pExpr.flags |= EP_InfixFunc;
          }
          //#line 2982 "parse.c"
          break;
        case 214: /* expr ::= expr ISNULL|NOTNULL */
          //#line 886 "parse.y"
          { spanUnaryPostfix(yygotominor.yy346, pParse, yymsp[0].major, yymsp[-1].minor.yy346, yymsp[0].minor.yy0); }
          //#line 2987 "parse.c"
          break;
        case 215: /* expr ::= expr IS NULL */
          //#line 887 "parse.y"
          { spanUnaryPostfix(yygotominor.yy346, pParse, TK_ISNULL, yymsp[-2].minor.yy346, yymsp[0].minor.yy0); }
          //#line 2992 "parse.c"
          break;
        case 216: /* expr ::= expr NOT NULL */
          //#line 888 "parse.y"
          { spanUnaryPostfix(yygotominor.yy346, pParse, TK_NOTNULL, yymsp[-2].minor.yy346, yymsp[0].minor.yy0); }
          //#line 2997 "parse.c"
          break;
        case 217: /* expr ::= expr IS NOT NULL */
          //#line 890 "parse.y"
          { spanUnaryPostfix(yygotominor.yy346, pParse, TK_NOTNULL, yymsp[-3].minor.yy346, yymsp[0].minor.yy0); }
          //#line 3002 "parse.c"
          break;
        case 218: /* expr ::= NOT expr */
        case 219: /* expr ::= BITNOT expr */ //yytestcase(yyruleno==219);
          //#line 910 "parse.y"
          { spanUnaryPrefix(yygotominor.yy346, pParse, yymsp[-1].major, yymsp[0].minor.yy346, yymsp[-1].minor.yy0); }
          //#line 3008 "parse.c"
          break;
        case 220: /* expr ::= MINUS expr */
          //#line 913 "parse.y"
          { spanUnaryPrefix(yygotominor.yy346, pParse, TK_UMINUS, yymsp[0].minor.yy346, yymsp[-1].minor.yy0); }
          //#line 3013 "parse.c"
          break;
        case 221: /* expr ::= PLUS expr */
          //#line 915 "parse.y"
          { spanUnaryPrefix(yygotominor.yy346, pParse, TK_UPLUS, yymsp[0].minor.yy346, yymsp[-1].minor.yy0); }
          //#line 3018 "parse.c"
          break;
        case 224: /* expr ::= expr between_op expr AND expr */
          //#line 920 "parse.y"
          {
            ExprList pList = sqlite3ExprListAppend(pParse, 0, yymsp[-2].minor.yy346.pExpr);
            pList = sqlite3ExprListAppend(pParse, pList, yymsp[0].minor.yy346.pExpr);
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_BETWEEN, yymsp[-4].minor.yy346.pExpr, 0, 0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.x.pList = pList;
            }
            else
            {
              sqlite3ExprListDelete(pParse.db, ref pList);
            }
            if (yymsp[-3].minor.yy328 != 0) yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_NOT, yygotominor.yy346.pExpr, 0, 0);
            yygotominor.yy346.zStart = yymsp[-4].minor.yy346.zStart;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy346.zEnd;
          }
          //#line 3035 "parse.c"
          break;
        case 227: /* expr ::= expr in_op LP exprlist RP */
          //#line 937 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_IN, yymsp[-4].minor.yy346.pExpr, 0, 0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.x.pList = yymsp[-1].minor.yy14;
              sqlite3ExprSetHeight(pParse, yygotominor.yy346.pExpr);
            }
            else
            {
              sqlite3ExprListDelete(pParse.db, ref yymsp[-1].minor.yy14);
            }
            if (yymsp[-3].minor.yy328 != 0) yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_NOT, yygotominor.yy346.pExpr, 0, 0);
            yygotominor.yy346.zStart = yymsp[-4].minor.yy346.zStart;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n);
          }
          //#line 3051 "parse.c"
          break;
        case 228: /* expr ::= LP select RP */
          //#line 949 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_SELECT, 0, 0, 0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.x.pSelect = yymsp[-1].minor.yy3;
              ExprSetProperty(yygotominor.yy346.pExpr, EP_xIsSelect);
              sqlite3ExprSetHeight(pParse, yygotominor.yy346.pExpr);
            }
            else
            {
              sqlite3SelectDelete(pParse.db, ref yymsp[-1].minor.yy3);
            }
            yygotominor.yy346.zStart = yymsp[-2].minor.yy0.z;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n);
          }
          //#line 3067 "parse.c"
          break;
        case 229: /* expr ::= expr in_op LP select RP */
          //#line 961 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_IN, yymsp[-4].minor.yy346.pExpr, 0, 0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.x.pSelect = yymsp[-1].minor.yy3;
              ExprSetProperty(yygotominor.yy346.pExpr, EP_xIsSelect);
              sqlite3ExprSetHeight(pParse, yygotominor.yy346.pExpr);
            }
            else
            {
              sqlite3SelectDelete(pParse.db, ref yymsp[-1].minor.yy3);
            }
            if (yymsp[-3].minor.yy328 != 0) yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_NOT, yygotominor.yy346.pExpr, 0, 0);
            yygotominor.yy346.zStart = yymsp[-4].minor.yy346.zStart;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n);
          }
          //#line 3084 "parse.c"
          break;
        case 230: /* expr ::= expr in_op nm dbnm */
          //#line 974 "parse.y"
          {
            SrcList pSrc = sqlite3SrcListAppend(pParse.db, 0, yymsp[-1].minor.yy0, yymsp[0].minor.yy0);
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_IN, yymsp[-3].minor.yy346.pExpr, 0, 0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.x.pSelect = sqlite3SelectNew(pParse, 0, pSrc, 0, 0, 0, 0, 0, 0, 0);
              ExprSetProperty(yygotominor.yy346.pExpr, EP_xIsSelect);
              sqlite3ExprSetHeight(pParse, yygotominor.yy346.pExpr);
            }
            else
            {
              sqlite3SrcListDelete(pParse.db, ref pSrc);
            }
            if (yymsp[-2].minor.yy328 != 0) yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_NOT, yygotominor.yy346.pExpr, 0, 0);
            yygotominor.yy346.zStart = yymsp[-3].minor.yy346.zStart;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z != null ? yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n) : yymsp[-1].minor.yy0.z.Substring(yymsp[-1].minor.yy0.n);
          }
          //#line 3102 "parse.c"
          break;
        case 231: /* expr ::= EXISTS LP select RP */
          //#line 988 "parse.y"
          {
            Expr p = yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_EXISTS, 0, 0, 0);
            if (p != null)
            {
              p.x.pSelect = yymsp[-1].minor.yy3;
              ExprSetProperty(p, EP_xIsSelect);
              sqlite3ExprSetHeight(pParse, p);
            }
            else
            {
              sqlite3SelectDelete(pParse.db, ref yymsp[-1].minor.yy3);
            }
            yygotominor.yy346.zStart = yymsp[-3].minor.yy0.z;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n);
          }
          //#line 3118 "parse.c"
          break;
        case 232: /* expr ::= CASE case_operand case_exprlist case_else END */
          //#line 1003 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_CASE, yymsp[-3].minor.yy132, yymsp[-1].minor.yy132, 0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.x.pList = yymsp[-2].minor.yy14;
              sqlite3ExprSetHeight(pParse, yygotominor.yy346.pExpr);
            }
            else
            {
              sqlite3ExprListDelete(pParse.db, ref yymsp[-2].minor.yy14);
            }
            yygotominor.yy346.zStart = yymsp[-4].minor.yy0.z;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n);
          }
          //#line 3133 "parse.c"
          break;
        case 233: /* case_exprlist ::= case_exprlist WHEN expr THEN expr */
          //#line 1016 "parse.y"
          {
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yymsp[-4].minor.yy14, yymsp[-2].minor.yy346.pExpr);
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yygotominor.yy14, yymsp[0].minor.yy346.pExpr);
          }
          //#line 3141 "parse.c"
          break;
        case 234: /* case_exprlist ::= WHEN expr THEN expr */
          //#line 1020 "parse.y"
          {
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, 0, yymsp[-2].minor.yy346.pExpr);
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yygotominor.yy14, yymsp[0].minor.yy346.pExpr);
          }
          //#line 3149 "parse.c"
          break;
        case 243: /* cmd ::= createkw uniqueflag INDEX ifnotexists nm dbnm ON nm LP idxlist RP */
          //#line 1049 "parse.y"
          {
            sqlite3CreateIndex(pParse, yymsp[-6].minor.yy0, yymsp[-5].minor.yy0,
            sqlite3SrcListAppend(pParse.db, 0, yymsp[-3].minor.yy0, 0), yymsp[-1].minor.yy14, yymsp[-9].minor.yy328,
            yymsp[-10].minor.yy0, yymsp[0].minor.yy0, SQLITE_SO_ASC, yymsp[-7].minor.yy328);
          }
          //#line 3158 "parse.c"
          break;
        case 244: /* uniqueflag ::= UNIQUE */
        case 298: /* raisetype ::= ABORT */ //yytestcase(yyruleno==298);
          //#line 1056 "parse.y"
          { yygotominor.yy328 = OE_Abort; }
          //#line 3164 "parse.c"
          break;
        case 245: /* uniqueflag ::= */
          //#line 1057 "parse.y"
          { yygotominor.yy328 = OE_None; }
          //#line 3169 "parse.c"
          break;
        case 248: /* idxlist ::= idxlist COMMA nm collate sortorder */
          //#line 1066 "parse.y"
          {
            Expr p = null;
            if (yymsp[-1].minor.yy0.n > 0)
            {
              p = sqlite3Expr(pParse.db, TK_COLUMN, null);
              sqlite3ExprSetColl(pParse, p, yymsp[-1].minor.yy0);
            }
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, yymsp[-4].minor.yy14, p);
            sqlite3ExprListSetName(pParse, yygotominor.yy14, yymsp[-2].minor.yy0, 1);
            sqlite3ExprListCheckLength(pParse, yygotominor.yy14, "index");
            if (yygotominor.yy14 != null) yygotominor.yy14.a[yygotominor.yy14.nExpr - 1].sortOrder = (u8)yymsp[0].minor.yy328;
          }
          //#line 3184 "parse.c"
          break;
        case 249: /* idxlist ::= nm collate sortorder */
          //#line 1077 "parse.y"
          {
            Expr p = null;
            if (yymsp[-1].minor.yy0.n > 0)
            {
              p = sqlite3PExpr(pParse, TK_COLUMN, 0, 0, 0);
              sqlite3ExprSetColl(pParse, p, yymsp[-1].minor.yy0);
            }
            yygotominor.yy14 = sqlite3ExprListAppend(pParse, 0, p);
            sqlite3ExprListSetName(pParse, yygotominor.yy14, yymsp[-2].minor.yy0, 1);
            sqlite3ExprListCheckLength(pParse, yygotominor.yy14, "index");
            if (yygotominor.yy14 != null) yygotominor.yy14.a[yygotominor.yy14.nExpr - 1].sortOrder = (u8)yymsp[0].minor.yy328;
          }
          //#line 3199 "parse.c"
          break;
        case 250: /* collate ::= */
          //#line 1090 "parse.y"
          { yygotominor.yy0.z = null; yygotominor.yy0.n = 0; }
          //#line 3204 "parse.c"
          break;
        case 252: /* cmd ::= DROP INDEX ifexists fullname */
          //#line 1096 "parse.y"
          { sqlite3DropIndex(pParse, yymsp[0].minor.yy65, yymsp[-1].minor.yy328); }
          //#line 3209 "parse.c"
          break;
        case 253: /* cmd ::= VACUUM */
        case 254: /* cmd ::= VACUUM nm */ //yytestcase(yyruleno==254);
          //#line 1102 "parse.y"
          { sqlite3Vacuum(pParse); }
          //#line 3215 "parse.c"
          break;
        case 255: /* cmd ::= PRAGMA nm dbnm */
          //#line 1110 "parse.y"
          { sqlite3Pragma(pParse, yymsp[-1].minor.yy0, yymsp[0].minor.yy0, 0, 0); }
          //#line 3220 "parse.c"
          break;
        case 256: /* cmd ::= PRAGMA nm dbnm EQ nmnum */
          //#line 1111 "parse.y"
          { sqlite3Pragma(pParse, yymsp[-3].minor.yy0, yymsp[-2].minor.yy0, yymsp[0].minor.yy0, 0); }
          //#line 3225 "parse.c"
          break;
        case 257: /* cmd ::= PRAGMA nm dbnm LP nmnum RP */
          //#line 1112 "parse.y"
          { sqlite3Pragma(pParse, yymsp[-4].minor.yy0, yymsp[-3].minor.yy0, yymsp[-1].minor.yy0, 0); }
          //#line 3230 "parse.c"
          break;
        case 258: /* cmd ::= PRAGMA nm dbnm EQ minus_num */
          //#line 1114 "parse.y"
          { sqlite3Pragma(pParse, yymsp[-3].minor.yy0, yymsp[-2].minor.yy0, yymsp[0].minor.yy0, 1); }
          //#line 3235 "parse.c"
          break;
        case 259: /* cmd ::= PRAGMA nm dbnm LP minus_num RP */
          //#line 1116 "parse.y"
          { sqlite3Pragma(pParse, yymsp[-4].minor.yy0, yymsp[-3].minor.yy0, yymsp[-1].minor.yy0, 1); }
          //#line 3240 "parse.c"
          break;
        case 270: /* cmd ::= createkw trigger_decl BEGIN trigger_cmd_list END */
          //#line 1134 "parse.y"
          {
            Token all = new Token();
            //all.z = yymsp[-3].minor.yy0.z;
            //all.n = (int)(yymsp[0].minor.yy0.z - yymsp[-3].minor.yy0.z) + yymsp[0].minor.yy0.n;
            all.n = (int)(yymsp[-3].minor.yy0.z.Length - yymsp[0].minor.yy0.z.Length) + yymsp[0].minor.yy0.n;
            all.z = yymsp[-3].minor.yy0.z.Substring(0, all.n);
            sqlite3FinishTrigger(pParse, yymsp[-1].minor.yy473, all);
          }
          //#line 3250 "parse.c"
          break;
        case 271: /* trigger_decl ::= temp TRIGGER ifnotexists nm dbnm trigger_time trigger_event ON fullname foreach_clause when_clause */
          //#line 1143 "parse.y"
          {
            sqlite3BeginTrigger(pParse, yymsp[-7].minor.yy0, yymsp[-6].minor.yy0, yymsp[-5].minor.yy328, yymsp[-4].minor.yy378.a, yymsp[-4].minor.yy378.b, yymsp[-2].minor.yy65, yymsp[0].minor.yy132, yymsp[-10].minor.yy328, yymsp[-8].minor.yy328);
            yygotominor.yy0 = (yymsp[-6].minor.yy0.n == 0 ? yymsp[-7].minor.yy0 : yymsp[-6].minor.yy0);
          }
          //#line 3258 "parse.c"
          break;
        case 272: /* trigger_time ::= BEFORE */
        case 275: /* trigger_time ::= */ //yytestcase(yyruleno==275);
          //#line 1149 "parse.y"
          { yygotominor.yy328 = TK_BEFORE; }
          //#line 3264 "parse.c"
          break;
        case 273: /* trigger_time ::= AFTER */
          //#line 1150 "parse.y"
          { yygotominor.yy328 = TK_AFTER; }
          //#line 3269 "parse.c"
          break;
        case 274: /* trigger_time ::= INSTEAD OF */
          //#line 1151 "parse.y"
          { yygotominor.yy328 = TK_INSTEAD; }
          //#line 3274 "parse.c"
          break;
        case 276: /* trigger_event ::= DELETE|INSERT */
        case 277: /* trigger_event ::= UPDATE */ //yytestcase(yyruleno==277);
          //#line 1156 "parse.y"
          { yygotominor.yy378.a = yymsp[0].major; yygotominor.yy378.b = null; }
          //#line 3280 "parse.c"
          break;
        case 278: /* trigger_event ::= UPDATE OF inscollist */
          //#line 1158 "parse.y"
          { yygotominor.yy378.a = TK_UPDATE; yygotominor.yy378.b = yymsp[0].minor.yy408; }
          //#line 3285 "parse.c"
          break;
        case 281: /* when_clause ::= */
        case 303: /* key_opt ::= */ //yytestcase(yyruleno==303);
          //#line 1165 "parse.y"
          { yygotominor.yy132 = null; }
          //#line 3291 "parse.c"
          break;
        case 282: /* when_clause ::= WHEN expr */
        case 304: /* key_opt ::= KEY expr */ //yytestcase(yyruleno==304);
          //#line 1166 "parse.y"
          { yygotominor.yy132 = yymsp[0].minor.yy346.pExpr; }
          //#line 3297 "parse.c"
          break;
        case 283: /* trigger_cmd_list ::= trigger_cmd_list trigger_cmd SEMI */
          //#line 1170 "parse.y"
          {
            Debug.Assert(yymsp[-2].minor.yy473 != null);
            yymsp[-2].minor.yy473.pLast.pNext = yymsp[-1].minor.yy473;
            yymsp[-2].minor.yy473.pLast = yymsp[-1].minor.yy473;
            yygotominor.yy473 = yymsp[-2].minor.yy473;
          }
          //#line 3307 "parse.c"
          break;
        case 284: /* trigger_cmd_list ::= trigger_cmd SEMI */
          //#line 1176 "parse.y"
          {
            Debug.Assert(yymsp[-1].minor.yy473 != null);
            yymsp[-1].minor.yy473.pLast = yymsp[-1].minor.yy473;
            yygotominor.yy473 = yymsp[-1].minor.yy473;
          }
          //#line 3316 "parse.c"
          break;
        case 286: /* trnm ::= nm DOT nm */
          //#line 1188 "parse.y"
          {
            yygotominor.yy0 = yymsp[0].minor.yy0;
            sqlite3ErrorMsg(pParse,
            "qualified table names are not allowed on INSERT, UPDATE, and DELETE " +
            "statements within triggers");
          }
          //#line 3326 "parse.c"
          break;
        case 288: /* tridxby ::= INDEXED BY nm */
          //#line 1200 "parse.y"
          {
            sqlite3ErrorMsg(pParse,
            "the INDEXED BY clause is not allowed on UPDATE or DELETE statements " +
            "within triggers");
          }
          //#line 3335 "parse.c"
          break;
        case 289: /* tridxby ::= NOT INDEXED */
          //#line 1205 "parse.y"
          {
            sqlite3ErrorMsg(pParse,
            "the NOT INDEXED clause is not allowed on UPDATE or DELETE statements " +
            "within triggers");
          }
          //#line 3344 "parse.c"
          break;
        case 290: /* trigger_cmd ::= UPDATE orconf trnm tridxby SET setlist where_opt */
          //#line 1218 "parse.y"
          { yygotominor.yy473 = sqlite3TriggerUpdateStep(pParse.db, yymsp[-4].minor.yy0, yymsp[-1].minor.yy14, yymsp[0].minor.yy132, yymsp[-5].minor.yy186); }
          //#line 3349 "parse.c"
          break;
        case 291: /* trigger_cmd ::= insert_cmd INTO trnm inscollist_opt VALUES LP itemlist RP */
          //#line 1223 "parse.y"
          { yygotominor.yy473 = sqlite3TriggerInsertStep(pParse.db, yymsp[-5].minor.yy0, yymsp[-4].minor.yy408, yymsp[-1].minor.yy14, 0, yymsp[-7].minor.yy186); }
          //#line 3354 "parse.c"
          break;
        case 292: /* trigger_cmd ::= insert_cmd INTO trnm inscollist_opt select */
          //#line 1226 "parse.y"
          { yygotominor.yy473 = sqlite3TriggerInsertStep(pParse.db, yymsp[-2].minor.yy0, yymsp[-1].minor.yy408, 0, yymsp[0].minor.yy3, yymsp[-4].minor.yy186); }
          //#line 3359 "parse.c"
          break;
        case 293: /* trigger_cmd ::= DELETE FROM trnm tridxby where_opt */
          //#line 1230 "parse.y"
          { yygotominor.yy473 = sqlite3TriggerDeleteStep(pParse.db, yymsp[-2].minor.yy0, yymsp[0].minor.yy132); }
          //#line 3364 "parse.c"
          break;
        case 294: /* trigger_cmd ::= select */
          //#line 1233 "parse.y"
          { yygotominor.yy473 = sqlite3TriggerSelectStep(pParse.db, yymsp[0].minor.yy3); }
          //#line 3369 "parse.c"
          break;
        case 295: /* expr ::= RAISE LP IGNORE RP */
          //#line 1236 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_RAISE, 0, 0, 0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.affinity = (char)OE_Ignore;
            }
            yygotominor.yy346.zStart = yymsp[-3].minor.yy0.z;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n);
          }
          //#line 3381 "parse.c"
          break;
        case 296: /* expr ::= RAISE LP raisetype COMMA nm RP */
          //#line 1244 "parse.y"
          {
            yygotominor.yy346.pExpr = sqlite3PExpr(pParse, TK_RAISE, 0, 0, yymsp[-1].minor.yy0);
            if (yygotominor.yy346.pExpr != null)
            {
              yygotominor.yy346.pExpr.affinity = (char)yymsp[-3].minor.yy328;
            }
            yygotominor.yy346.zStart = yymsp[-5].minor.yy0.z;
            yygotominor.yy346.zEnd = yymsp[0].minor.yy0.z.Substring(yymsp[0].minor.yy0.n);
          }
          //#line 3393 "parse.c"
          break;
        case 297: /* raisetype ::= ROLLBACK */
          //#line 1255 "parse.y"
          { yygotominor.yy328 = OE_Rollback; }
          //#line 3398 "parse.c"
          break;
        case 299: /* raisetype ::= FAIL */
          //#line 1257 "parse.y"
          { yygotominor.yy328 = OE_Fail; }
          //#line 3403 "parse.c"
          break;
        case 300: /* cmd ::= DROP TRIGGER ifexists fullname */
          //#line 1262 "parse.y"
          {
            sqlite3DropTrigger(pParse, yymsp[0].minor.yy65, yymsp[-1].minor.yy328);
          }
          //#line 3410 "parse.c"
          break;
        case 301: /* cmd ::= ATTACH database_kw_opt expr AS expr key_opt */
          //#line 1269 "parse.y"
          {
            sqlite3Attach(pParse, yymsp[-3].minor.yy346.pExpr, yymsp[-1].minor.yy346.pExpr, yymsp[0].minor.yy132);
          }
          //#line 3417 "parse.c"
          break;
        case 302: /* cmd ::= DETACH database_kw_opt expr */
          //#line 1272 "parse.y"
          {
            sqlite3Detach(pParse, yymsp[0].minor.yy346.pExpr);
          }
          //#line 3424 "parse.c"
          break;
        case 307: /* cmd ::= REINDEX */
          //#line 1287 "parse.y"
          { sqlite3Reindex(pParse, 0, 0); }
          //#line 3429 "parse.c"
          break;
        case 308: /* cmd ::= REINDEX nm dbnm */
          //#line 1288 "parse.y"
          { sqlite3Reindex(pParse, yymsp[-1].minor.yy0, yymsp[0].minor.yy0); }
          //#line 3434 "parse.c"
          break;
        case 309: /* cmd ::= ANALYZE */
          //#line 1293 "parse.y"
          { sqlite3Analyze(pParse, 0, 0); }
          //#line 3439 "parse.c"
          break;
        case 310: /* cmd ::= ANALYZE nm dbnm */
          //#line 1294 "parse.y"
          { sqlite3Analyze(pParse, yymsp[-1].minor.yy0, yymsp[0].minor.yy0); }
          //#line 3444 "parse.c"
          break;
        case 311: /* cmd ::= ALTER TABLE fullname RENAME TO nm */
          //#line 1299 "parse.y"
          {
            sqlite3AlterRenameTable(pParse, yymsp[-3].minor.yy65, yymsp[0].minor.yy0);
          }
          //#line 3451 "parse.c"
          break;
        case 312: /* cmd ::= ALTER TABLE add_column_fullname ADD kwcolumn_opt column */
          //#line 1302 "parse.y"
          {
            sqlite3AlterFinishAddColumn(pParse, yymsp[0].minor.yy0);
          }
          //#line 3458 "parse.c"
          break;
        case 313: /* add_column_fullname ::= fullname */
          //#line 1305 "parse.y"
          {
            pParse.db.lookaside.bEnabled = 0;
            sqlite3AlterBeginAddColumn(pParse, yymsp[0].minor.yy65);
          }
          //#line 3466 "parse.c"
          break;
        case 316: /* cmd ::= create_vtab */
          //#line 1315 "parse.y"
          { sqlite3VtabFinishParse(pParse, 0); }
          //#line 3471 "parse.c"
          break;
        case 317: /* cmd ::= create_vtab LP vtabarglist RP */
          //#line 1316 "parse.y"
          { sqlite3VtabFinishParse(pParse, yymsp[0].minor.yy0); }
          //#line 3476 "parse.c"
          break;
        case 318: /* create_vtab ::= createkw VIRTUAL TABLE nm dbnm USING nm */
          //#line 1317 "parse.y"
          {
            sqlite3VtabBeginParse(pParse, yymsp[-3].minor.yy0, yymsp[-2].minor.yy0, yymsp[0].minor.yy0);
          }
          //#line 3483 "parse.c"
          break;
        case 321: /* vtabarg ::= */
          //#line 1322 "parse.y"
          { sqlite3VtabArgInit(pParse); }
          //#line 3488 "parse.c"
          break;
        case 323: /* vtabargtoken ::= ANY */
        case 324: /* vtabargtoken ::= lp anylist RP */ //yytestcase(yyruleno==324);
        case 325: /* lp ::= LP */ //yytestcase(yyruleno==325);
          //#line 1324 "parse.y"
          { sqlite3VtabArgExtend(pParse, yymsp[0].minor.yy0); }
          //#line 3495 "parse.c"
          break;
        default:
          /* (0) input ::= cmdlist */
          //yytestcase(yyruleno==0);
          /* (1) cmdlist ::= cmdlist ecmd */
          //yytestcase(yyruleno==1);
          /* (2) cmdlist ::= ecmd */
          //yytestcase(yyruleno==2);
          /* (3) ecmd ::= SEMI */
          //yytestcase(yyruleno==3);
          /* (4) ecmd ::= explain cmdx SEMI */
          //yytestcase(yyruleno==4);
          /* (10) trans_opt ::= */
          //yytestcase(yyruleno==10);
          /* (11) trans_opt ::= TRANSACTION */
          //yytestcase(yyruleno==11);
          /* (12) trans_opt ::= TRANSACTION nm */
          //yytestcase(yyruleno==12);
          /* (20) savepoint_opt ::= SAVEPOINT */
          //yytestcase(yyruleno==20);
          /* (21) savepoint_opt ::= */
          //yytestcase(yyruleno==21);
          /* (25) cmd ::= create_table create_table_args */
          //yytestcase(yyruleno==25);
          /* (34) columnlist ::= columnlist COMMA column */
          //yytestcase(yyruleno==34);
          /* (35) columnlist ::= column */
          //yytestcase(yyruleno==35);
          /* (44) type ::= */
          //yytestcase(yyruleno==44);
          /* (51) signed ::= plus_num */
          //yytestcase(yyruleno==51);
          /* (52) signed ::= minus_num */
          //yytestcase(yyruleno==52);
          /* (53) carglist ::= carglist carg */
          //yytestcase(yyruleno==53);
          /* (54) carglist ::= */
          //yytestcase(yyruleno==54);
          /* (55) carg ::= CONSTRAINT nm ccons */
          //yytestcase(yyruleno==55);
          /* (56) carg ::= ccons */
          //yytestcase(yyruleno==56);
          /* (62) ccons ::= NULL onconf */
          //yytestcase(yyruleno==62);
          /* (89) conslist ::= conslist COMMA tcons */
          //yytestcase(yyruleno==89);
          /* (90) conslist ::= conslist tcons */
          //yytestcase(yyruleno==90);
          /* (91) conslist ::= tcons */
          //yytestcase(yyruleno==91);
          /* (92) tcons ::= CONSTRAINT nm */
          //yytestcase(yyruleno==92);
          /* (268) plus_opt ::= PLUS */
          //yytestcase(yyruleno==268);
          /* (269) plus_opt ::= */
          //yytestcase(yyruleno==269);
          /* (279) foreach_clause ::= */
          //yytestcase(yyruleno==279);
          /* (280) foreach_clause ::= FOR EACH ROW */
          //yytestcase(yyruleno==280);
          /* (287) tridxby ::= */
          //yytestcase(yyruleno==287);
          /* (305) database_kw_opt ::= DATABASE */
          //yytestcase(yyruleno==305);
          /* (306) database_kw_opt ::= */
          //yytestcase(yyruleno==306);
          /* (314) kwcolumn_opt ::= */
          //yytestcase(yyruleno==314);
          /* (315) kwcolumn_opt ::= COLUMNKW */
          //yytestcase(yyruleno==315);
          /* (319) vtabarglist ::= vtabarg */
          //yytestcase(yyruleno==319);
          /* (320) vtabarglist ::= vtabarglist COMMA vtabarg */
          //yytestcase(yyruleno==320);
          /* (322) vtabarg ::= vtabarg vtabargtoken */
          //yytestcase(yyruleno==322);
          /* (326) anylist ::= */
          //yytestcase(yyruleno==326);
          /* (327) anylist ::= anylist LP anylist RP */
          //yytestcase(yyruleno==327);
          /* (328) anylist ::= anylist ANY */
          //yytestcase(yyruleno==328);
          break;
      };
      yygoto = yyRuleInfo[yyruleno].lhs;
      yysize = yyRuleInfo[yyruleno].nrhs;
      yypParser.yyidx -= yysize;
      yyact = yy_find_reduce_action(yymsp[-yysize].stateno, (YYCODETYPE)yygoto);
      if (yyact < YYNSTATE)
      {
#if NDEBUG
/* If we are not debugging and the reduce action popped at least
** one element off the stack, then we can push the new element back
** onto the stack here, and skip the stack overflow test in yy_shift().
** That gives a significant speed improvement. */
if( yysize!=0 ){
yypParser.yyidx++;
yymsp._yyidx -= yysize - 1;
yymsp[0].stateno = (YYACTIONTYPE)yyact;
yymsp[0].major = (YYCODETYPE)yygoto;
yymsp[0].minor = yygotominor;
}else
#endif
        {
          yy_shift(yypParser, yyact, yygoto, yygotominor);
        }
      }
      else
      {
        Debug.Assert(yyact == YYNSTATE + YYNRULE + 1);
        yy_accept(yypParser);
      }
    }

    /*
    ** The following code executes when the parse fails
    */
#if !YYNOERRORRECOVERY
    static void yy_parse_failed(
    yyParser yypParser           /* The parser */
    )
    {
      Parse pParse = yypParser.pParse; //       sqlite3ParserARG_FETCH;
#if !NDEBUG
      if (yyTraceFILE != null)
      {
        Debugger.Break(); // TODO --        fprintf(yyTraceFILE, "%sFail!\n", yyTracePrompt);
      }
#endif
      while (yypParser.yyidx >= 0) yy_pop_parser_stack(yypParser);
      /* Here code is inserted which will be executed whenever the
      ** parser fails */
      yypParser.pParse = pParse;//      sqlite3ParserARG_STORE; /* Suppress warning about unused %extra_argument variable */
    }
#endif //* YYNOERRORRECOVERY */

    /*
** The following code executes when a syntax error first occurs.
*/
    static void yy_syntax_error(
    yyParser yypParser,           /* The parser */
    int yymajor,                   /* The major type of the error token */
    YYMINORTYPE yyminor            /* The minor type of the error token */
    )
    {
      Parse pParse = yypParser.pParse; //       sqlite3ParserARG_FETCH;
      //#define TOKEN (yyminor.yy0)
      //#line 34 "parse.y"

      UNUSED_PARAMETER(yymajor);  /* Silence some compiler warnings */
      Debug.Assert(yyminor.yy0.z.Length > 0); //TOKEN.z[0]);  /* The tokenizer always gives us a token */
      sqlite3ErrorMsg(pParse, "near \"%T\": syntax error", yyminor.yy0);//&TOKEN);
      pParse.parseError = 1;
      //#line 3603 "parse.c"
      yypParser.pParse = pParse; // sqlite3ParserARG_STORE; /* Suppress warning about unused %extra_argument variable */
    }

    /*
    ** The following is executed when the parser accepts
    */
    static void yy_accept(
    yyParser yypParser           /* The parser */
    )
    {
      Parse pParse = yypParser.pParse; //       sqlite3ParserARG_FETCH;
#if !NDEBUG
      if (yyTraceFILE != null)
      {
        fprintf(yyTraceFILE, "%sAccept!\n", yyTracePrompt);
      }
#endif
      while (yypParser.yyidx >= 0) yy_pop_parser_stack(yypParser);
      /* Here code is inserted which will be executed whenever the
      ** parser accepts */
      yypParser.pParse = pParse;//      sqlite3ParserARG_STORE; /* Suppress warning about unused %extra_argument variable */
    }

    /* The main parser program.
    ** The first argument is a pointer to a structure obtained from
    ** "sqlite3ParserAlloc" which describes the current state of the parser.
    ** The second argument is the major token number.  The third is
    ** the minor token.  The fourth optional argument is whatever the
    ** user wants (and specified in the grammar) and is available for
    ** use by the action routines.
    **
    ** Inputs:
    ** <ul>
    ** <li> A pointer to the parser (an opaque structure.)
    ** <li> The major token number.
    ** <li> The minor token number.
    ** <li> An option argument of a grammar-specified type.
    ** </ul>
    **
    ** Outputs:
    ** None.
    */
    static void sqlite3Parser(
    yyParser yyp,                   /* The parser */
    int yymajor,                     /* The major token code number */
    sqlite3ParserTOKENTYPE yyminor  /* The value for the token */
    , Parse pParse //sqlite3ParserARG_PDECL           /* Optional %extra_argument parameter */
    )
    {
      YYMINORTYPE yyminorunion = new YYMINORTYPE();
      int yyact;            /* The parser action. */
      bool yyendofinput;     /* True if we are at the end of input */
#if YYERRORSYMBOL
int yyerrorhit = 0;   /* True if yymajor has invoked an error */
#endif
      yyParser yypParser;  /* The parser */

      /* (re)initialize the parser, if necessary */
      yypParser = yyp;
      if (yypParser.yyidx < 0)
      {
#if YYSTACKDEPTH//<=0
if( yypParser.yystksz <=0 ){
memset(yyminorunion, 0, yyminorunion).Length;
yyStackOverflow(yypParser, yyminorunion);
return;
}
#endif
        yypParser.yyidx = 0;
        yypParser.yyerrcnt = -1;
        yypParser.yystack[0] = new yyStackEntry();
        yypParser.yystack[0].stateno = 0;
        yypParser.yystack[0].major = 0;
      }
      yyminorunion.yy0 = yyminor.Copy();
      yyendofinput = (yymajor == 0);
      yypParser.pParse = pParse;//      sqlite3ParserARG_STORE;

#if !NDEBUG
      if (yyTraceFILE != null)
      {
        fprintf(yyTraceFILE, "%sInput %s\n", yyTracePrompt, yyTokenName[yymajor]);
      }
#endif

      do
      {
        yyact = yy_find_shift_action(yypParser, (YYCODETYPE)yymajor);
        if (yyact < YYNSTATE)
        {
          Debug.Assert(!yyendofinput);  /* Impossible to shift the $ token */
          yy_shift(yypParser, yyact, yymajor, yyminorunion);
          yypParser.yyerrcnt--;
          yymajor = YYNOCODE;
        }
        else if (yyact < YYNSTATE + YYNRULE)
        {
          yy_reduce(yypParser, yyact - YYNSTATE);
        }
        else
        {
          Debug.Assert(yyact == YY_ERROR_ACTION);
#if YYERRORSYMBOL
int yymx;
#endif
#if !NDEBUG
          if (yyTraceFILE != null)
          {
            Debugger.Break(); // TODO --            fprintf(yyTraceFILE, "%sSyntax Error!\n", yyTracePrompt);
          }
#endif
#if YYERRORSYMBOL
/* A syntax error has occurred.
** The response to an error depends upon whether or not the
** grammar defines an error token "ERROR".
**
** This is what we do if the grammar does define ERROR:
**
**  * Call the %syntax_error function.
**
**  * Begin popping the stack until we enter a state where
**    it is legal to shift the error symbol, then shift
**    the error symbol.
**
**  * Set the error count to three.
**
**  * Begin accepting and shifting new tokens.  No new error
**    processing will occur until three tokens have been
**    shifted successfully.
**
*/
if( yypParser.yyerrcnt<0 ){
yy_syntax_error(yypParser,yymajor,yyminorunion);
}
yymx = yypParser.yystack[yypParser.yyidx].major;
if( yymx==YYERRORSYMBOL || yyerrorhit ){
#if !NDEBUG
if( yyTraceFILE ){
Debug.Assert(false); // TODO --                      fprintf(yyTraceFILE,"%sDiscard input token %s\n",
yyTracePrompt,yyTokenName[yymajor]);
}
#endif
yy_destructor(yypParser,(YYCODETYPE)yymajor,yyminorunion);
yymajor = YYNOCODE;
}else{
while(
yypParser.yyidx >= 0 &&
yymx != YYERRORSYMBOL &&
(yyact = yy_find_reduce_action(
yypParser.yystack[yypParser.yyidx].stateno,
YYERRORSYMBOL)) >= YYNSTATE
){
yy_pop_parser_stack(yypParser);
}
if( yypParser.yyidx < 0 || yymajor==0 ){
yy_destructor(yypParser, (YYCODETYPE)yymajor,yyminorunion);
yy_parse_failed(yypParser);
yymajor = YYNOCODE;
}else if( yymx!=YYERRORSYMBOL ){
YYMINORTYPE u2;
u2.YYERRSYMDT = 0;
yy_shift(yypParser,yyact,YYERRORSYMBOL,u2);
}
}
yypParser.yyerrcnt = 3;
yyerrorhit = 1;
#elif (YYNOERRORRECOVERY)
/* If the YYNOERRORRECOVERY macro is defined, then do not attempt to
** do any kind of error recovery.  Instead, simply invoke the syntax
** error routine and continue going as if nothing had happened.
**
** Applications can set this macro (for example inside %include) if
** they intend to abandon the parse upon the first syntax error seen.
*/
yy_syntax_error(yypParser,yymajor,yyminorunion);
yy_destructor(yypParser,(YYCODETYPE)yymajor,yyminorunion);
yymajor = YYNOCODE;
#else  // * YYERRORSYMBOL is not defined */
          /* This is what we do if the grammar does not define ERROR:
**
**  * Report an error message, and throw away the input token.
**
**  * If the input token is $, then fail the parse.
**
** As before, subsequent error messages are suppressed until
** three input tokens have been successfully shifted.
*/
          if (yypParser.yyerrcnt <= 0)
          {
            yy_syntax_error(yypParser, yymajor, yyminorunion);
          }
          yypParser.yyerrcnt = 3;
          yy_destructor(yypParser, (YYCODETYPE)yymajor, yyminorunion);
          if (yyendofinput)
          {
            yy_parse_failed(yypParser);
          }
          yymajor = YYNOCODE;
#endif
        }
      } while (yymajor != YYNOCODE && yypParser.yyidx >= 0);
      return;
    }
    public class yymsp
    {
      public yyParser _yyParser;
      public int _yyidx;
      // CONSTRUCTOR
      public yymsp(ref yyParser pointer_to_yyParser, int yyidx) //' Parser and Stack Index
      {
        this._yyParser = pointer_to_yyParser;
        this._yyidx = yyidx;
      }
      // Default Value
      public yyStackEntry this[int offset]
      {
        get { return _yyParser.yystack[_yyidx + offset]; }
      }
    }
  }
}
