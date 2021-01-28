namespace CS_SQLite3
{
  public partial class CSSQLite
  {
    /* Automatically generated.  Do not edit */
    /* See the mkopcodeh.awk script for details */
    /* Automatically generated.  Do not edit */
    /* See the mkopcodeh.awk script for details */
    //#define OP_VNext                                1
    //#define OP_Affinity                             2
    //#define OP_Column                               3
    //#define OP_SetCookie                            4
    //#define OP_Seek                                 5
    //#define OP_Real                               130   /* same as TK_FLOAT    */
    //#define OP_Sequence                             6
    //#define OP_Savepoint                            7
    //#define OP_Ge                                  78   /* same as TK_GE       */
    //#define OP_RowKey                               8
    //#define OP_SCopy                                9
    //#define OP_Eq                                  74   /* same as TK_EQ       */
    //#define OP_OpenWrite                           10
    //#define OP_NotNull                             72   /* same as TK_NOTNULL  */
    //#define OP_If                                  11
    //#define OP_ToInt                              144   /* same as TK_TO_INT   */
    //#define OP_String8                             94   /* same as TK_STRING   */
    //#define OP_CollSeq                             12
    //#define OP_OpenRead                            13
    //#define OP_Expire                              14
    //#define OP_AutoCommit                          15
    //#define OP_Gt                                  75   /* same as TK_GT       */
    //#define OP_Pagecount                           16
    //#define OP_IntegrityCk                         17
    //#define OP_Sort                                18
    //#define OP_Copy                                20
    //#define OP_Trace                               21
    //#define OP_Function                            22
    //#define OP_IfNeg                               23
    //#define OP_And                                 67   /* same as TK_AND      */
    //#define OP_Subtract                            85   /* same as TK_MINUS    */
    //#define OP_Noop                                24
    //#define OP_Return                              25
    //#define OP_Remainder                           88   /* same as TK_REM      */
    //#define OP_NewRowid                            26
    //#define OP_Multiply                            86   /* same as TK_STAR     */
    //#define OP_Variable                            27
    //#define OP_String                              28
    //#define OP_RealAffinity                        29
    //#define OP_VRename                             30
    //#define OP_ParseSchema                         31
    //#define OP_VOpen                               32
    //#define OP_Close                               33
    //#define OP_CreateIndex                         34
    //#define OP_IsUnique                            35
    //#define OP_NotFound                            36
    //#define OP_Int64                               37
    //#define OP_MustBeInt                           38
    //#define OP_Halt                                39
    //#define OP_Rowid                               40
    //#define OP_IdxLT                               41
    //#define OP_AddImm                              42
    //#define OP_Statement                           43
    //#define OP_RowData                             44
    //#define OP_MemMax                              45
    //#define OP_Or                                  66   /* same as TK_OR       */
    //#define OP_NotExists                           46
    //#define OP_Gosub                               47
    //#define OP_Divide                              87   /* same as TK_SLASH    */
    //#define OP_Integer                             48
    //#define OP_ToNumeric                          143   /* same as TK_TO_NUMERIC*/
    //#define OP_Prev                                49
    //#define OP_RowSetRead                          50
    //#define OP_Concat                              89   /* same as TK_CONCAT   */
    //#define OP_RowSetAdd                           51
    //#define OP_BitAnd                              80   /* same as TK_BITAND   */
    //#define OP_VColumn                             52
    //#define OP_CreateTable                         53
    //#define OP_Last                                54
    //#define OP_SeekLe                              55
    //#define OP_IsNull                              71   /* same as TK_ISNULL   */
    //#define OP_IncrVacuum                          56
    //#define OP_IdxRowid                            57
    //#define OP_ShiftRight                          83   /* same as TK_RSHIFT   */
    //#define OP_ResetCount                          58
    //#define OP_ContextPush                         59
    //#define OP_Yield                               60
    //#define OP_DropTrigger                         61
    //#define OP_DropIndex                           62
    //#define OP_IdxGE                               63
    //#define OP_IdxDelete                           64
    //#define OP_Vacuum                              65
    //#define OP_IfNot                               68
    //#define OP_DropTable                           69
    //#define OP_SeekLt                              70
    //#define OP_MakeRecord                          79
    //#define OP_ToBlob                             142   /* same as TK_TO_BLOB  */
    //#define OP_ResultRow                           90
    //#define OP_Delete                              91
    //#define OP_AggFinal                            92
    //#define OP_Compare                             95
    //#define OP_ShiftLeft                           82   /* same as TK_LSHIFT   */
    //#define OP_Goto                                96
    //#define OP_TableLock                           97
    //#define OP_Clear                               98
    //#define OP_Le                                  76   /* same as TK_LE       */
    //#define OP_VerifyCookie                        99
    //#define OP_AggStep                            100
    //#define OP_ToText                             141   /* same as TK_TO_TEXT  */
    //#define OP_Not                                 19   /* same as TK_NOT      */
    //#define OP_ToReal                             145   /* same as TK_TO_REAL  */
    //#define OP_SetNumColumns                      101
    //#define OP_Transaction                        102
    //#define OP_VFilter                            103
    //#define OP_Ne                                  73   /* same as TK_NE       */
    //#define OP_VDestroy                           104
    //#define OP_ContextPop                         105
    //#define OP_BitOr                               81   /* same as TK_BITOR    */
    //#define OP_Next                               106
    //#define OP_Count                              107
    //#define OP_IdxInsert                          108
    //#define OP_Lt                                  77   /* same as TK_LT       */
    //#define OP_SeekGe                             109
    //#define OP_Insert                             110
    //#define OP_Destroy                            111
    //#define OP_ReadCookie                         112
    //#define OP_RowSetTest                         113
    //#define OP_LoadAnalysis                       114
    //#define OP_Explain                            115
    //#define OP_HaltIfNull                         116
    //#define OP_OpenPseudo                         117
    //#define OP_OpenEphemeral                      118
    //#define OP_Null                               119
    //#define OP_Move                               120
    //#define OP_Blob                               121
    //#define OP_Add                                 84   /* same as TK_PLUS     */
    //#define OP_Rewind                             122
    //#define OP_SeekGt                             123
    //#define OP_VBegin                             124
    //#define OP_VUpdate                            125
    //#define OP_IfZero                             126
    //#define OP_BitNot                              93   /* same as TK_BITNOT   */
    //#define OP_VCreate                            127
    //#define OP_Found                              128
    //#define OP_IfPos                              129
    //#define OP_NullRow                            131
    //#define OP_Jump                               132
    //#define OP_Permutation                        133

    const int OP_VNext = 1;
    const int OP_Affinity = 2;
    const int OP_Column = 3;
    const int OP_SetCookie = 4;
    const int OP_Seek = 5;
    const int OP_Real = 130;   /* same as TK_FLOAT=*/
    const int OP_Sequence = 6;
    const int OP_Savepoint = 7;
    const int OP_Ge = 78; /* same as TK_GE=   */
    const int OP_RowKey = 8;
    const int OP_SCopy = 9;
    const int OP_Eq = 74;   /* same as TK_EQ=   */
    const int OP_OpenWrite = 10;
    const int OP_NotNull = 72;  /* same as TK_NOTNULL  */
    const int OP_If = 11;
    const int OP_ToInt = 144; /* same as TK_TO_INT   */
    const int OP_String8 = 94;  /* same as TK_STRING   */
    const int OP_CollSeq = 12;
    const int OP_OpenRead = 13;
    const int OP_Expire = 14;
    const int OP_AutoCommit = 15;
    const int OP_Gt = 75; /* same as TK_GT=   */
    const int OP_Pagecount = 16;
    const int OP_IntegrityCk = 17;
    const int OP_Sort = 18;
    const int OP_Copy = 20;
    const int OP_Trace = 21;
    const int OP_Function = 22;
    const int OP_IfNeg = 23;
    const int OP_And = 67;/* same as TK_AND=  */
    const int OP_Subtract = 85;  /* same as TK_MINUS=*/
    const int OP_Noop = 24;
    const int OP_Return = 25;
    const int OP_Remainder = 88; /* same as TK_REM=  */
    const int OP_NewRowid = 26;
    const int OP_Multiply = 86; /* same as TK_STAR= */
    const int OP_Variable = 27;
    const int OP_String = 28;
    const int OP_RealAffinity = 29;
    const int OP_VRename = 30;
    const int OP_ParseSchema = 31;
    const int OP_VOpen = 32;
    const int OP_Close = 33;
    const int OP_CreateIndex = 34;
    const int OP_IsUnique = 35;
    const int OP_NotFound = 36;
    const int OP_Int64 = 37;
    const int OP_MustBeInt = 38;
    const int OP_Halt = 39;
    const int OP_Rowid = 40;
    const int OP_IdxLT = 41;
    const int OP_AddImm = 42;
    const int OP_Statement = 43;
    const int OP_RowData = 44;
    const int OP_MemMax = 45;
    const int OP_Or = 66; /* same as TK_OR=   */
    const int OP_NotExists = 46;
    const int OP_Gosub = 47;
    const int OP_Divide = 87;/* same as TK_SLASH=*/
    const int OP_Integer = 48;
    const int OP_ToNumeric = 143; /* same as TK_TO_NUMERIC*/
    const int OP_Prev = 49;
    const int OP_RowSetRead = 50;
    const int OP_Concat = 89;  /* same as TK_CONCAT   */
    const int OP_RowSetAdd = 51;
    const int OP_BitAnd = 80;   /* same as TK_BITAND   */
    const int OP_VColumn = 52;
    const int OP_CreateTable = 53;
    const int OP_Last = 54;
    const int OP_SeekLe = 55;
    const int OP_IsNull = 71; /* same as TK_ISNULL   */
    const int OP_IncrVacuum = 56;
    const int OP_IdxRowid = 57;
    const int OP_ShiftRight = 83;  /* same as TK_RSHIFT   */
    const int OP_ResetCount = 58;
    const int OP_ContextPush = 59;
    const int OP_Yield = 60;
    const int OP_DropTrigger = 61;
    const int OP_DropIndex = 62;
    const int OP_IdxGE = 63;
    const int OP_IdxDelete = 64;
    const int OP_Vacuum = 65;
    const int OP_IfNot = 68;
    const int OP_DropTable = 69;
    const int OP_SeekLt = 70;
    const int OP_MakeRecord = 79;
    const int OP_ToBlob = 142; /* same as TK_TO_BLOB  */
    const int OP_ResultRow = 90;
    const int OP_Delete = 91;
    const int OP_AggFinal = 92;
    const int OP_Compare = 95;
    const int OP_ShiftLeft = 82; /* same as TK_LSHIFT   */
    const int OP_Goto = 96;
    const int OP_TableLock = 97;
    const int OP_Clear = 98;
    const int OP_Le = 76; /* same as TK_LE=   */
    const int OP_VerifyCookie = 99;
    const int OP_AggStep = 100;
    const int OP_ToText = 141; /* same as TK_TO_TEXT  */
    const int OP_Not = 19;  /* same as TK_NOT=  */
    const int OP_ToReal = 145;/* same as TK_TO_REAL  */
    const int OP_SetNumColumns = 101;
    const int OP_Transaction = 102;
    const int OP_VFilter = 103;
    const int OP_Ne = 73; /* same as TK_NE=   */
    const int OP_VDestroy = 104;
    const int OP_ContextPop = 105;
    const int OP_BitOr = 81; /* same as TK_BITOR=*/
    const int OP_Next = 106;
    const int OP_Count = 107;
    const int OP_IdxInsert = 108;
    const int OP_Lt = 77;  /* same as TK_LT=   */
    const int OP_SeekGe = 109;
    const int OP_Insert = 110;
    const int OP_Destroy = 111;
    const int OP_ReadCookie = 112;
    const int OP_RowSetTest = 113;
    const int OP_LoadAnalysis = 114;
    const int OP_Explain = 115;
    const int OP_HaltIfNull = 116;
    const int OP_OpenPseudo = 117;
    const int OP_OpenEphemeral = 118;
    const int OP_Null = 119;
    const int OP_Move = 120;
    const int OP_Blob = 121;
    const int OP_Add = 84; /* same as TK_PLUS= */
    const int OP_Rewind = 122;
    const int OP_SeekGt = 123;
    const int OP_VBegin = 124;
    const int OP_VUpdate = 125;
    const int OP_IfZero = 126;
    const int OP_BitNot = 93;/* same as TK_BITNOT   */
    const int OP_VCreate = 127;
    const int OP_Found = 128;
    const int OP_IfPos = 129;
    const int OP_NullRow = 131;
    const int OP_Jump = 132;
    const int OP_Permutation = 133;

    /* The following opcode values are never used */
    //#define OP_NotUsed_134                        134
    //#define OP_NotUsed_135                        135
    //#define OP_NotUsed_136                        136
    //#define OP_NotUsed_137                        137
    //#define OP_NotUsed_138                        138
    //#define OP_NotUsed_139                        139
    //#define OP_NotUsed_140                        140

    /* The following opcode values are never used */
    const int OP_NotUsed_134 = 134;
    const int OP_NotUsed_135 = 135;
    const int OP_NotUsed_136 = 136;
    const int OP_NotUsed_137 = 137;
    const int OP_NotUsed_138 = 138;
    const int OP_NotUsed_139 = 139;
    const int OP_NotUsed_140 = 140;


    /* Properties such as "out2" or "jump" that are specified in
    ** comments following the "case" for each opcode in the vdbe.c
    ** are encoded into bitvectors as follows:
    */
    //#define OPFLG_JUMP            0x0001  /* jump:  P2 holds jmp target */
    //#define OPFLG_OUT2_PRERELEASE 0x0002  /* out2-prerelease: */
    //#define OPFLG_IN1             0x0004  /* in1:   P1 is an input */
    //#define OPFLG_IN2             0x0008  /* in2:   P2 is an input */
    //#define OPFLG_IN3             0x0010  /* in3:   P3 is an input */
    //#define OPFLG_OUT3            0x0020  /* out3:  P3 is an output */

    const int OPFLG_JUMP = 0x0001;            /* jump:  P2 holds jmp target */
    const int OPFLG_OUT2_PRERELEASE = 0x0002; /* out2-prerelease: */
    const int OPFLG_IN1 = 0x0004;             /* in1:   P1 is an input */
    const int OPFLG_IN2 = 0x0008;             /* in2:   P2 is an input */
    const int OPFLG_IN3 = 0x0010;             /* in3:   P3 is an input */
    const int OPFLG_OUT3 = 0x0020;            /* out3:  P3 is an output */

    public static int[] OPFLG_INITIALIZER = new int[]{
/*   0 */ 0x00, 0x01, 0x00, 0x00, 0x10, 0x08, 0x02, 0x00,
/*   8 */ 0x00, 0x04, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00,
/*  16 */ 0x02, 0x00, 0x01, 0x04, 0x04, 0x00, 0x00, 0x05,
/*  24 */ 0x00, 0x04, 0x02, 0x00, 0x02, 0x04, 0x00, 0x00,
/*  32 */ 0x00, 0x00, 0x02, 0x11, 0x11, 0x02, 0x05, 0x00,
/*  40 */ 0x02, 0x11, 0x04, 0x00, 0x00, 0x0c, 0x11, 0x01,
/*  48 */ 0x02, 0x01, 0x21, 0x08, 0x00, 0x02, 0x01, 0x11,
/*  56 */ 0x01, 0x02, 0x00, 0x00, 0x04, 0x00, 0x00, 0x11,
/*  64 */ 0x00, 0x00, 0x2c, 0x2c, 0x05, 0x00, 0x11, 0x05,
/*  72 */ 0x05, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x00,
/*  80 */ 0x2c, 0x2c, 0x2c, 0x2c, 0x2c, 0x2c, 0x2c, 0x2c,
/*  88 */ 0x2c, 0x2c, 0x00, 0x00, 0x00, 0x04, 0x02, 0x00,
/*  96 */ 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
/* 104 */ 0x00, 0x00, 0x01, 0x02, 0x08, 0x11, 0x00, 0x02,
/* 112 */ 0x02, 0x15, 0x00, 0x00, 0x10, 0x00, 0x00, 0x02,
/* 120 */ 0x00, 0x02, 0x01, 0x11, 0x00, 0x00, 0x05, 0x00,
/* 128 */ 0x11, 0x05, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00,
/* 136 */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x04, 0x04,
/* 144 */ 0x04, 0x04,
};
  }
}
