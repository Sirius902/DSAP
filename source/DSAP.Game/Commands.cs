using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.Memory;
using DSAP.Game.Models;

namespace DSAP.Game;

internal static class Commands
{
    /* aka GameDataMan */
    private static AoBHelper BaseBAoB = new("BaseB",
        [0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x45, 0x33, 0xED, 0x48, 0x8B, 0xF1, 0x48, 0x85, 0xC0],
        "xxx????xxxxxxxxx", 3, 4);
    
    /*  ----------Code To Emulate--------------
        mov rcx,[BaseB]
        mov edx,1
        sub rsp,38
        call 0x1404867e0
        add rsp,38
        ret
    */
    /*  ----------Homeward Bone injected ASM--------------
        0:  48 c7 c1 78 56 34 12    mov    rcx,0x12345678
        7:  00
        8:  ba 01 00 00 00          mov    edx,0x1
        d:  49 bb e0 67 48 40 01    movabs r11,0x1404867e0
        14: 00 00 00
        17: 48 83 ec 38             sub    rsp,0x38
        1b: 41 ff d3                call   r11
        1e: 48 83 c4 38             add    rsp,0x38
        22: c3                      ret
     */
    public static void ExecuteHomewardBone()
    {
        var command = new byte[] {
            0x48, 0xC7, 0xC1, 0x78, 0x56, 0x34, 0x12,
            0xBA, 0x01, 0x00, 0x00, 0x00,
            0x49, 0xBB, 0xE0, 0x67, 0x48, 0x40, 0x01, 0x00, 0x00, 0x00,
            0x48, 0x83, 0xEC, 0x38,
            0x41, 0xFF, 0xD3,
            0x48, 0x83, 0xC4, 0x38,
            0xC3
        };

        Array.Copy(BitConverter.GetBytes(GetBaseBAddress()), 0, command, 0x3, 4);

        ExecuteCommand(command);
    }

    private static nint GetBaseBAddress()
    {
        return BaseBAoB.Address;
    }

    private static unsafe void ExecuteCommand(byte[] code)
    {
        var addr = Windows.Win32.PInvoke.VirtualAlloc(null, (nuint)code.Length, VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE, PAGE_PROTECTION_FLAGS.PAGE_READWRITE);
        if (addr is null)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            fixed (byte* pCode = code)
            {
                Unsafe.CopyBlock(addr, pCode, (uint)code.Length);
            }

            if (!Windows.Win32.PInvoke.VirtualProtect(
                    addr,
                    (nuint)code.Length,
                    PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READ,
                    out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var functionPtr = (delegate* unmanaged<void>)addr;
            functionPtr();
        }
        finally
        {
            Windows.Win32.PInvoke.VirtualFree(addr, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
        }
    }
}