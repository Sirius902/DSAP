using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DSAP.Game;

public static class Helpers
{
    public static bool IsInGame() => GetIngameTime() != 0;
    
    private static nint OffsetPointer(nint ptr, uint offset)
    {
        var newAddress = ptr;
        return ptr + (nint)offset;
    }
    
    private static uint GetIngameTime()
    {
        var baseB = Process.GetCurrentProcess().MainModule!.BaseAddress;
        if (baseB == 0) return 0;
        var next = OffsetPointer(baseB, 0xA4);
        unsafe
        {
            return Unsafe.Read<uint>((void*)next);
        }
    }
}