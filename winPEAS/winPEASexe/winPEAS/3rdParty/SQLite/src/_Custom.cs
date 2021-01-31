    /*
    *************************************************************************
    **  $Header$
    *************************************************************************
    */

    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Management;
    using System.Runtime.InteropServices;
    using System.Text;
    using i64 = System.Int64;
    using u32 = System.UInt32;
    using time_t = System.Int64;

namespace winPEAS._3rdParty.SQLite.src
{
  using sqlite3_value = CSSQLite.Mem;

  public partial class CSSQLite
  {

    static int atoi( byte[] inStr )
    { return atoi( Encoding.UTF8.GetString( inStr ) ); }

    static int atoi( string inStr )
    {
      int i;
      for ( i = 0 ; i < inStr.Length ; i++ )
      {
        if ( !sqlite3Isdigit( inStr[i] ) && inStr[i] != '-' ) break;
      }
      int result = 0;

      return ( Int32.TryParse( inStr.Substring( 0, i ), out result ) ? result : 0 );
    }

    static void fprintf( TextWriter tw, string zFormat, params object[] ap ) { tw.Write( sqlite3_mprintf( zFormat, ap ) ); }
    static void printf( string zFormat, params object[] ap ) { Console.Out.Write( sqlite3_mprintf( zFormat, ap ) ); }


    //Byte Buffer Testing

    static int memcmp( byte[] bA, byte[] bB, int Limit )
    {
      if ( bA.Length < Limit ) return ( bA.Length < bB.Length ) ? -1 : +1;
      if ( bB.Length < Limit ) return +1;
      for ( int i = 0 ; i < Limit ; i++ )
      {
        if ( bA[i] != bB[i] ) return ( bA[i] < bB[i] ) ? -1 : 1;
      }
      return 0;
    }

    //Byte Buffer  & String Testing
    static int memcmp( byte[] bA, string B, int Limit )
    {
      if ( bA.Length < Limit ) return ( bA.Length < B.Length ) ? -1 : +1;
      if ( B.Length < Limit ) return +1;
      for ( int i = 0 ; i < Limit ; i++ )
      {
        if ( bA[i] != B[i] ) return ( bA[i] < B[i] ) ? -1 : 1;
      }
      return 0;
    }

    //Byte Buffer  & String Testing
    static int memcmp( string A, byte[] bB, int Limit )
    {
      if ( A.Length < Limit ) return ( A.Length < bB.Length ) ? -1 : +1;
      if ( bB.Length < Limit ) return +1;
      for ( int i = 0 ; i < Limit ; i++ )
      {
        if ( A[i] != bB[i] ) return ( A[i] < bB[i] ) ? -1 : 1;
      }
      return 0;
    }

    //String with Offset & String Testing
    static int memcmp( byte[] a, int Offset, byte[] b, int Limit )
    {
      if ( a.Length < Offset + Limit ) return ( a.Length - Offset < b.Length ) ? -1 : +1;
      if ( b.Length < Limit ) return +1;
      for ( int i = 0 ; i < Limit ; i++ )
      {
        if ( a[i + Offset] != b[i] ) return ( a[i + Offset] < b[i] ) ? -1 : 1;
      }
      return 0;
    }

    static int memcmp( string a, int Offset, byte[] b, int Limit )
    {
      if ( a.Length < Offset + Limit ) return ( a.Length - Offset < b.Length ) ? -1 : +1;
      if ( b.Length < Limit ) return +1;
      for ( int i = 0 ; i < Limit ; i++ )
      {
        if ( a[i + Offset] != b[i] ) return ( a[i + Offset] < b[i] ) ? -1 : 1;
      }
      return 0;
    }

    static int memcmp( byte[] a, int Offset, string b, int Limit )
    {
      if ( a.Length < Offset + Limit ) return ( a.Length - Offset < b.Length ) ? -1 : +1;
      if ( b.Length < Limit ) return +1;
      for ( int i = 0 ; i < Limit ; i++ )
      {
        if ( a[i + Offset] != b[i] ) return ( a[i + Offset] < b[i] ) ? -1 : 1;
      }
      return 0;
    }


    //String Testing
    static int memcmp( string A, string B, int Limit )
    {
      if ( A.Length < Limit ) return ( A.Length < B.Length ) ? -1 : +1;
      if ( B.Length < Limit ) return +1;
      for ( int i = 0 ; i < Limit ; i++ )
      {
        if ( A[i] != B[i] ) return ( A[i] < B[i] ) ? -1 : 1;
      }
      return 0;
    }

    // ----------------------------
    // ** Convertion routines
    // ----------------------------
    static string vaFORMAT;
    static int vaNEXT;

    static void va_start( object[] ap, string zFormat )
    {
      vaFORMAT = zFormat;
      vaNEXT = 0;
    }

    static object va_arg( object[] ap, string sysType )
    {
      vaNEXT += 1;
      if ( ap == null || ap.Length == 0 )
        return "";
      switch ( sysType )
      {
        case "double":
          return Convert.ToDouble( ap[vaNEXT - 1] );
        case "long":
        case "long int":
        case "longlong int":
        case "i64":
          if ( ap[vaNEXT - 1].GetType().BaseType.Name == "Object" ) return (i64)( ap[vaNEXT - 1].GetHashCode() ); ;
          return Convert.ToInt64( ap[vaNEXT - 1] );
        case "int":
          if ( Convert.ToInt64( ap[vaNEXT - 1] ) > 0 && ( Convert.ToUInt32( ap[vaNEXT - 1] ) > Int32.MaxValue ) ) return (Int32)( Convert.ToUInt32( ap[vaNEXT - 1] ) - System.UInt32.MaxValue - 1 );
          else return (Int32)Convert.ToInt32( ap[vaNEXT - 1] );
        case "SrcList":
          return (SrcList)ap[vaNEXT - 1];
        case "char":
          if ( ap[vaNEXT - 1].GetType().Name == "Int32" && (int)ap[vaNEXT - 1] == 0 )
          {
            return (char)'0';
          }
          else
          {
            if ( ap[vaNEXT - 1].GetType().Name == "Int64" )
              if ( (i64)ap[vaNEXT - 1] == 0 )
              {
                return (char)'0';
              }
              else return (char)( (i64)ap[vaNEXT - 1] );
            else
              return (char)ap[vaNEXT - 1];
          }
        case "char*":
        case "string":
          if ( ap[vaNEXT - 1] == null )
          {
            return "NULL";
          }
          else
          {
            if ( ap[vaNEXT - 1].GetType().Name == "Byte[]" )
              if ( Encoding.UTF8.GetString( (byte[])ap[vaNEXT - 1] ) == "\0" )
                return "";
              else
                return Encoding.UTF8.GetString( (byte[])ap[vaNEXT - 1] );
            else if ( ap[vaNEXT - 1].GetType().Name == "Int32" )
              return null;
            else if ( ap[vaNEXT - 1].GetType().Name == "StringBuilder" )
              return (string)ap[vaNEXT - 1].ToString();
            else return (string)ap[vaNEXT - 1];
          }
        case "byte[]":
          if ( ap[vaNEXT - 1] == null )
          {
            return null;
          }
          else
          {
            return (byte[])ap[vaNEXT - 1];
          }
        case "int[]":
          if ( ap[vaNEXT - 1] == null )
          {
            return "NULL";
          }
          else
          {
            return (int[])ap[vaNEXT - 1];
          }
        case "Token":
          return (Token)ap[vaNEXT - 1];
        case "u3216":
          return Convert.ToUInt16( ap[vaNEXT - 1] );
        case "u32":
        case "unsigned int":
          if ( ap[vaNEXT - 1].GetType().IsClass )
          {
            return ap[vaNEXT - 1].GetHashCode();
          }
          else
          {
            return Convert.ToUInt32( ap[vaNEXT - 1] );
          }
        case "u64":
        case "unsigned long":
        case "unsigned long int":
          if ( ap[vaNEXT - 1].GetType().IsClass )
            return Convert.ToUInt64( ap[vaNEXT - 1].GetHashCode() );
          else
            return Convert.ToUInt64( ap[vaNEXT - 1] );
        case "sqlite3_mem_methods":
          return (sqlite3_mem_methods)ap[vaNEXT - 1];
        case "void_function":
          return (void_function)ap[vaNEXT - 1];
        case "MemPage":
          return (MemPage)ap[vaNEXT - 1];
        default:
          Debugger.Break();
          return ap[vaNEXT - 1];
      }
    }
    static void va_end( object[] ap )
    {
      ap = null;
      vaFORMAT = "";
    }


    public static tm localtime( time_t baseTime )
    {
      System.DateTime RefTime = new System.DateTime( 1970, 1, 1, 0, 0, 0, 0 );
      RefTime = RefTime.AddSeconds( Convert.ToDouble( baseTime ) ).ToLocalTime();
      tm tm = new tm();
      tm.tm_sec = RefTime.Second;
      tm.tm_min = RefTime.Minute;
      tm.tm_hour = RefTime.Hour;
      tm.tm_mday = RefTime.Day;
      tm.tm_mon = RefTime.Month;
      tm.tm_year = RefTime.Year;
      tm.tm_wday = (int)RefTime.DayOfWeek;
      tm.tm_yday = RefTime.DayOfYear;
      tm.tm_isdst = RefTime.IsDaylightSavingTime() ? 1 : 0;
      return tm;
    }

    public static long ToUnixtime( System.DateTime date )
    {
      System.DateTime unixStartTime = new System.DateTime( 1970, 1, 1, 0, 0, 0, 0 );
      System.TimeSpan timeSpan = date - unixStartTime;
      return Convert.ToInt64( timeSpan.TotalSeconds );
    }

    public static System.DateTime ToCSharpTime( long unixTime )
    {
      System.DateTime unixStartTime = new System.DateTime( 1970, 1, 1, 0, 0, 0, 0 );
      return unixStartTime.AddSeconds( Convert.ToDouble( unixTime ) );
    }

    public struct tm
    {
      public int tm_sec;     /* seconds after the minute - [0,59] */
      public int tm_min;     /* minutes after the hour - [0,59] */
      public int tm_hour;    /* hours since midnight - [0,23] */
      public int tm_mday;    /* day of the month - [1,31] */
      public int tm_mon;     /* months since January - [0,11] */
      public int tm_year;    /* years since 1900 */
      public int tm_wday;    /* days since Sunday - [0,6] */
      public int tm_yday;    /* days since January 1 - [0,365] */
      public int tm_isdst;   /* daylight savings time flag */
    };

    public struct FILETIME
    {
      public u32 dwLowDateTime;
      public u32 dwHighDateTime;
    }

    // Example (C#)
    public static int GetbytesPerSector( StringBuilder diskPath )
    {
      ManagementObjectSearcher mosLogicalDisks = new ManagementObjectSearcher( "select * from Win32_LogicalDisk where DeviceID = '" + diskPath.ToString().Remove( diskPath.Length - 1, 1 ) + "'");
      try
      {
        foreach ( ManagementObject moLogDisk in mosLogicalDisks.Get() )
        {
          ManagementObjectSearcher mosDiskDrives = new ManagementObjectSearcher( "select * from Win32_DiskDrive where SystemName = '" + moLogDisk["SystemName"] + "'" );
          foreach ( ManagementObject moPDisk in mosDiskDrives.Get() )
          {
            return int.Parse( moPDisk["BytesPerSector"].ToString() );
          }
        }
      }
      catch { }
      return 0;
    }
    
    [DllImport( "kernel32.dll" )]
    public static extern bool GetSystemTimeAsFileTime( ref FILETIME sysfiletime );

    static void SWAP<T>( ref T A, ref T B ) { T t = A; A = B; B = t; }

    static void x_CountStep(
    sqlite3_context context,
    int argc,
    sqlite3_value[] argv
    )
    {
      SumCtx p;

      int type;
      Debug.Assert( argc <= 1 );
      Mem pMem = sqlite3_aggregate_context( context, -1 );//sizeof(*p));
      if ( pMem._SumCtx == null ) pMem._SumCtx = new SumCtx();
      p = pMem._SumCtx;
      if ( p.Context == null ) p.Context = pMem;
      if ( argc == 0 || SQLITE_NULL == sqlite3_value_type( argv[0] ) )
      {
        p.cnt++;
        p.iSum += 1;
      }
      else
      {
        type = sqlite3_value_numeric_type( argv[0] );
        if ( p != null && type != SQLITE_NULL )
        {
          p.cnt++;
          if ( type == SQLITE_INTEGER )
          {
            i64 v = sqlite3_value_int64( argv[0] );
            if ( v == 40 || v == 41 )
            {
              sqlite3_result_error( context, "value of " + v + " handed to x_count", -1 );
              return;
            }
            else
            {
              p.iSum += v;
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
          }
          else
          {
            p.rSum += sqlite3_value_double( argv[0] );
            p.approx = true;
          }
        }
      }
    }
    static void x_CountFinalize( sqlite3_context context )
    {
      SumCtx p;
      Mem pMem = sqlite3_aggregate_context( context, 0 );
      p = pMem._SumCtx;
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
        else if ( p.iSum == 42 )
        {
          sqlite3_result_error( context, "x_count totals to 42", -1 );
        }
        else
        {
          sqlite3_result_int64( context, p.iSum );
        }
      }
    }
#if SQLITE_MUTEX_W32
//---------------------WIN32 Definitions
static int GetCurrentThreadId()
{
return Thread.CurrentThread.ManagedThreadId;
}
static long InterlockedIncrement(long location)
{
Interlocked.Increment(ref  location);
return location;
}

static void EnterCriticalSection(Mutex mtx)
{
Monitor.Enter(mtx);
}
static void InitializeCriticalSection(Mutex mtx)
{
Monitor.Enter(mtx);
}
static void DeleteCriticalSection(Mutex mtx)
{
Monitor.Exit(mtx);
}
static void LeaveCriticalSection(Mutex mtx)
{
Monitor.Exit(mtx);
}

}
#endif
  }
}
