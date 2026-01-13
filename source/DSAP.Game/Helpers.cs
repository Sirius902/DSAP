using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Archipelago.Core.Util;
using DSAP.Game.Models;

namespace DSAP.Game;

public static class Helpers
{
    /* aka GameDataMan */
    private static AoBHelper BaseBAoB = new("BaseB",
        [0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x45, 0x33, 0xED, 0x48, 0x8B, 0xF1, 0x48, 0x85, 0xC0],
        "xxx????xxxxxxxxx", 3, 4);

    public static bool IsInGame() => GetIngameTime() != 0;

    public static nint GetBaseAddress() => Process.GetCurrentProcess().MainModule!.BaseAddress;

    public static nint GetBaseBAddress() => BaseBAoB.Address;

    public static ulong GetChrBaseClassOffset()
    {
        var baseAddress = GetBaseAddress();
        var pattern = new byte[] { 0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x45, 0x33, 0xED, 0x48, 0x8B, 0xF1, 0x48, 0x85, 0xC0 };
        const string mask = "xxx????xxxxxxxxx";
        var getCBCAddress = Memory.FindSignature(baseAddress, 0x1000000, pattern, mask);

        var offset = BitConverter.ToInt32(Memory.ReadByteArray((ulong)(getCBCAddress + 3), 4), 0);
        var chrBaseClassAddress = getCBCAddress + offset + 7;

        return (ulong)chrBaseClassAddress;
    }

    private static uint GetIngameTime()
    {
        var baseB = GetBaseBAddress();
        if (baseB == 0) return 0;
        var next = baseB + 0xA4;
        unsafe
        {
            return Unsafe.Read<uint>((void*)next);
        }
    }
}