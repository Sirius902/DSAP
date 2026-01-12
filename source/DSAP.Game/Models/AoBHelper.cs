using System;
using System.Diagnostics;
using Archipelago.Core.Util;

namespace DSAP.Game.Models;

    public class AoBHelper
    {
        public AoBHelper(string key, byte[] pattern, string mask, ushort operandOffset, ushort operandLength)
        {
            Key = key;
            Pattern = pattern;
            Mask = mask;
            OperandOffset = operandOffset;
            OperandLength = operandLength;
            _pointer = IntPtr.Zero;
        }
        public string Key;
        public byte[] Pattern;
        public string Mask;

        ushort OperandOffset;
        ushort OperandLength;
        private IntPtr _pointer;
        private IntPtr Pointer
        {
            /* Get the position of the singleton/global pointer in memory */
            get
            {
                /* Cache the result. This looks in memory that is static after program load so it will not change. */
                if (_pointer == IntPtr.Zero)
                {
                    /* First, AoB search. This finds an instruction that references the global pointer with relative addressing */
                    IntPtr baseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
                    Memory.CurrentProcId = Process.GetCurrentProcess().Id;
                    IntPtr result = Memory.FindSignature(baseAddress, 0x1000000, this.Pattern, this.Mask);
                    if (result != IntPtr.Zero)
                    {
                        /* Get the relative offset */
                        uint offset = BitConverter.ToUInt32(Memory.ReadByteArray((ulong)(result + OperandOffset), OperandLength), 0);
                        if (offset != 0)
                        {
                            /* Instruction position + instruction size + relative offset = global pointer position */
                            IntPtr globalPtr = (nint)(result + offset + (OperandOffset + OperandLength));
                            _pointer = globalPtr; /* cache the result */
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }
                }
                return _pointer;
            }
            set
            {
                _pointer = value;
            }
        }
        /* Dereference the global pointer - this can't be cached. */
        public IntPtr Address
        { 
            get 
            {
                if (this.Pointer != IntPtr.Zero)
                {
                    ulong result = Memory.ReadULong((ulong)_pointer);
                    return (nint)result;
                }
                return IntPtr.Zero;
            } 
        }
    }