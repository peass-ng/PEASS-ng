namespace winPEAS._3rdParty.SQLite.src
{
  using sqlite_u3264 = System.UInt64;

  public partial class CSSQLite
  {
    /*
    ** 2008 May 27
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    ******************************************************************************
    **
    ** This file contains inline asm code for retrieving "high-performance"
    ** counters for x86 class CPUs.
    **
    ** $Id: hwtime.h,v 1.3 2008/08/01 14:33:15 shane Exp $
    **
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  $Header$
    *************************************************************************
    */
    //#if !_HWTIME_H_
    //#define _HWTIME_H_

    /*
    ** The following routine only works on pentium-class (or newer) processors.
    ** It uses the RDTSC opcode to read the cycle count value out of the
    ** processor and returns that value.  This can be used for high-res
    ** profiling.
    */
#if ((__GNUC__) || (_MSC_VER)) &&       ((i386) || (__i386__) || (_M_IX86))

#if (__GNUC__)

__inline__ sqlite_u3264 sqlite3Hwtime(void){
unsigned int lo, hi;
__asm__ __volatile__ ("rdtsc" : "=a" (lo), "=d" (hi));
return (sqlite_u3264)hi << 32 | lo;
}

#elif  (_MSC_VER)

__declspec(naked) __inline sqlite_u3264 __cdecl sqlite3Hwtime(void){
__asm {
rdtsc
ret       ; return value at EDX:EAX
}
}

#endif

#elif ((__GNUC__) && (__x86_64__))

__inline__ sqlite_u3264 sqlite3Hwtime(void){
unsigned long val;
__asm__ __volatile__ ("rdtsc" : "=A" (val));
return val;
}

#elif ( (__GNUC__) &&  (__ppc__))

__inline__ sqlite_u3264 sqlite3Hwtime(void){
unsigned long long retval;
unsigned long junk;
__asm__ __volatile__ ("\n\
1:      mftbu   %1\n\
mftb    %L0\n\
mftbu   %0\n\
cmpw    %0,%1\n\
bne     1b"
: "=r" (retval), "=r" (junk));
return retval;
}

#else

    //#error Need implementation of sqlite3Hwtime() for your platform.

    /*
    ** To compile without implementing sqlite3Hwtime() for your platform,
    ** you can remove the above #error and use the following
    ** stub function.  You will lose timing support for many
    ** of the debugging and testing utilities, but it should at
    ** least compile and run.
    */
    static sqlite_u3264 sqlite3Hwtime() { return (sqlite_u3264)System.DateTime.Now.Ticks; }// (sqlite_u3264)0 ); }

#endif

    //#endif /* !_HWTIME_H_) */
  }
}
