//  $Header$

using System;
using winPEAS._3rdParty.SQLite.src;

namespace winPEAS._3rdParty.SQLite
{
    using Vdbe = CSSQLite.Vdbe;

  /// <summary>
  /// C#-SQLite wrapper with functions for opening, closing and executing queries.
  /// </summary>
  public class SQLiteVdbe
  {
    private Vdbe vm = null;
    private string LastError = "";
    private int LastResult = 0;

    /// <summary>
    /// Creates new instance of SQLiteVdbe class by compiling a statement
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Vdbe</returns>
    public SQLiteVdbe( SQLiteDatabase db, String query )
    {
      vm = null;

      // prepare and compile 
      CSSQLite.sqlite3_prepare_v2( db.Connection(), query, query.Length, ref vm, 0 );
    }

    /// <summary>
    /// Return Virtual Machine Pointer
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Vdbe</returns>
    public Vdbe VirtualMachine()
    {
      return vm;
    }
    
    /// <summary>
    /// <summary>
    /// BindInteger
    /// </summary>
    /// <param name="index"></param>
    /// <param name="bInteger"></param>
    /// <returns>LastResult</returns>
    public int BindInteger(int index, int bInteger )
    {
      if ( (LastResult = CSSQLite.sqlite3_bind_int( vm, index, bInteger ))== CSSQLite.SQLITE_OK )
      { LastError = ""; }
      else
      {
        LastError = "Error " + LastError + "binding Integer [" + bInteger + "]";
      }
      return LastResult;
    }

    /// <summary>
    /// <summary>
    /// BindLong
    /// </summary>
    /// <param name="index"></param>
    /// <param name="bLong"></param>
    /// <returns>LastResult</returns>
    public int BindLong( int index, long bLong )
    {
      if ( ( LastResult = CSSQLite.sqlite3_bind_int64( vm, index, bLong ) ) == CSSQLite.SQLITE_OK )
      { LastError = ""; }
      else
      {
        LastError = "Error " + LastError + "binding Long [" + bLong + "]";
      }
      return LastResult;
    }

    /// <summary>
    /// BindText
    /// </summary>
    /// <param name="index"></param>
    /// <param name="bLong"></param>
    /// <returns>LastResult</returns>
    public int BindText(  int index, string bText )
    {
      if ( ( LastResult = CSSQLite.sqlite3_bind_text( vm, index, bText ,-1,null) ) == CSSQLite.SQLITE_OK )
      { LastError = ""; }
      else
      {
        LastError = "Error " + LastError + "binding Text [" + bText + "]";
      }
      return LastResult;
    }

    /// <summary>
    /// Execute statement
    /// </summary>
    /// </param>
    /// <returns>LastResult</returns>
    public int ExecuteStep(   )
    {
      // Execute the statement
      int LastResult = CSSQLite.sqlite3_step( vm );

      return LastResult;
    }

    /// <summary>
    /// Returns Result column as Long
    /// </summary>
    /// </param>
    /// <returns>Result column</returns>
    public long Result_Long(int index)
    {
      return CSSQLite.sqlite3_column_int64( vm, index );
    }

    /// <summary>
    /// Returns Result column as Text
    /// </summary>
    /// </param>
    /// <returns>Result column</returns>
    public string Result_Text( int index )
    {
      return CSSQLite.sqlite3_column_text( vm, index );
    }

    
    /// <summary>
    /// Returns Count of Result Rows
    /// </summary>
    /// </param>
    /// <returns>Count of Results</returns>
    public int ResultColumnCount( )
    {
      return vm.pResultSet == null ? 0 : vm.pResultSet.Length;
    }

    /// <summary>
    /// Reset statement
    /// </summary>
    /// </param>
    /// </returns>
    public void Reset()
    {
      // Reset the statment so it's ready to use again
      CSSQLite.sqlite3_reset( vm );
    }
    
    /// <summary>
    /// Closes statement
    /// </summary>
    /// </param>
    /// <returns>LastResult</returns>
    public void Close()
    {
      CSSQLite.sqlite3_finalize( ref vm );
    }
  
  }
}
