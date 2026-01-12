using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DSAP.Game.Hooking.Internal;

namespace DSAP.Game.Hooking;

// https://github.com/goatcorp/Dalamud/blob/c1df0da9beaec38c7132e519c60c04e56ec0caef/Dalamud/Hooking/Hook.cs
public static class Helpers
{
#pragma warning disable SA1310
    // ReSharper disable once InconsistentNaming
    private const ulong IMAGE_ORDINAL_FLAG64 = 0x8000000000000000;
#pragma warning restore SA1310

    /// <summary>
    /// Creates a hook by rewriting import table address.
    /// </summary>
    /// <param name="module">Module to check for. Current process' main module if null.</param>
    /// <param name="moduleName">Name of the DLL, including the extension.</param>
    /// <param name="functionName">Decorated name of the function.</param>
    /// <param name="hintOrOrdinal">Hint or ordinal. 0 to unspecify.</param>
    /// <returns>The address of the function pointer in the import table.</returns>
    internal static unsafe nint* FromImport(ProcessModule? module, string moduleName, string functionName, uint hintOrOrdinal)
    {
        module ??= Process.GetCurrentProcess().MainModule;
        if (module == null)
            throw new InvalidOperationException("Current module is null?");
        var pDos = (PeHeader.IMAGE_DOS_HEADER*)module.BaseAddress;
        var pNt = (PeHeader.IMAGE_FILE_HEADER*)(module.BaseAddress + (int)pDos->e_lfanew + 4);
        var isPe64 = pNt->SizeOfOptionalHeader == Marshal.SizeOf<PeHeader.IMAGE_OPTIONAL_HEADER64>();
        PeHeader.IMAGE_DATA_DIRECTORY* pDataDirectory;
        if (isPe64)
        {
            var pOpt = (PeHeader.IMAGE_OPTIONAL_HEADER64*)(module.BaseAddress + (int)pDos->e_lfanew + 4 + Marshal.SizeOf<PeHeader.IMAGE_FILE_HEADER>());
            pDataDirectory = &pOpt->ImportTable;
        }
        else
        {
            var pOpt = (PeHeader.IMAGE_OPTIONAL_HEADER32*)(module.BaseAddress + (int)pDos->e_lfanew + 4 + Marshal.SizeOf<PeHeader.IMAGE_FILE_HEADER>());
            pDataDirectory = &pOpt->ImportTable;
        }

        var moduleNameLowerWithNullTerminator = (moduleName + "\0").ToLowerInvariant();
        foreach (ref var importDescriptor in new Span<PeHeader.IMAGE_IMPORT_DESCRIPTOR>(
                     (PeHeader.IMAGE_IMPORT_DESCRIPTOR*)(module.BaseAddress + (int)pDataDirectory->VirtualAddress),
                     (int)(pDataDirectory->Size / Marshal.SizeOf<PeHeader.IMAGE_IMPORT_DESCRIPTOR>())))
        {
            // Having all zero values signals the end of the table. We didn't find anything.
            if (importDescriptor.Characteristics == 0)
                throw new MissingMethodException("Specified dll not found");

            // Skip invalid entries, just in case.
            if (importDescriptor.Name == 0)
                continue;

            // Name must be contained in this directory.
            if (importDescriptor.Name < pDataDirectory->VirtualAddress)
                continue;
            var currentDllNameWithNullTerminator = Marshal.PtrToStringUTF8(
                module.BaseAddress + (int)importDescriptor.Name,
                (int)Math.Min(pDataDirectory->Size + pDataDirectory->VirtualAddress - importDescriptor.Name, moduleNameLowerWithNullTerminator.Length));

            // Is this entry about the DLL that we're looking for? (Case insensitive)
            if (!currentDllNameWithNullTerminator.Equals(moduleNameLowerWithNullTerminator, StringComparison.InvariantCultureIgnoreCase))
                continue;

            return (nint*)FromImportHelper64(module.BaseAddress, ref importDescriptor, ref *pDataDirectory, functionName, hintOrOrdinal);
        }

        throw new MissingMethodException("Specified dll not found");
    }

    private static unsafe IntPtr FromImportHelper64(IntPtr baseAddress, ref PeHeader.IMAGE_IMPORT_DESCRIPTOR desc, ref PeHeader.IMAGE_DATA_DIRECTORY dir, string functionName, uint hintOrOrdinal)
    {
        var importLookupsOversizedSpan = new Span<ulong>((ulong*)(baseAddress + (int)desc.OriginalFirstThunk), (int)((dir.Size - desc.OriginalFirstThunk) / Marshal.SizeOf<ulong>()));
        var importAddressesOversizedSpan = new Span<ulong>((ulong*)(baseAddress + (int)desc.FirstThunk), (int)((dir.Size - desc.FirstThunk) / Marshal.SizeOf<ulong>()));

        var functionNameWithNullTerminator = functionName + "\0";
        for (int i = 0, i_ = Math.Min(importLookupsOversizedSpan.Length, importAddressesOversizedSpan.Length); i < i_ && importLookupsOversizedSpan[i] != 0 && importAddressesOversizedSpan[i] != 0; i++)
        {
            var importLookup = importLookupsOversizedSpan[i];

            // Is this entry importing by ordinals? A lot of socket functions are the case.
            if ((importLookup & IMAGE_ORDINAL_FLAG64) != 0)
            {
                var ordinal = importLookup & ~IMAGE_ORDINAL_FLAG64;

                // Is this the entry?
                if (hintOrOrdinal == 0 || ordinal != hintOrOrdinal)
                    continue;

                // Is this entry not importing by ordinals, and are we using hint exclusively to find the entry?
            }
            else
            {
                var hint = Marshal.ReadInt16(baseAddress + (int)importLookup);

                if (functionName.Length == 0)
                {
                    // Is this the entry?
                    if (hint != hintOrOrdinal)
                        continue;
                }
                else
                {
                    // Name must be contained in this directory.
                    var currentFunctionNameWithNullTerminator = Marshal.PtrToStringUTF8(
                        baseAddress + (int)importLookup + 2,
                        (int)Math.Min((ulong)dir.VirtualAddress + dir.Size - (ulong)baseAddress - importLookup - 2, (ulong)functionNameWithNullTerminator.Length));

                    // Is this entry about the function that we're looking for?
                    if (currentFunctionNameWithNullTerminator != functionNameWithNullTerminator)
                        continue;
                }
            }

            return baseAddress + (int)desc.FirstThunk + (i * Marshal.SizeOf<ulong>());
        }

        throw new MissingMethodException("Specified method not found");
    }
}