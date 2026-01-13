using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DSAP.Game;

internal static class Commands
{
    private static HomewardBoneDelegate? _homewardBone;
    private static AddItemDelegate? _addItem;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void HomewardBoneDelegate(nint arg0, int arg1);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int AddItemDelegate(nint arg0, int category, uint id, uint quantity, bool arg4, bool arg5, bool arg6, bool arg7);

    /*  ----------Code To Emulate--------------
        mov rcx,[BaseB]
        mov edx,1
        sub rsp,38
        call 0x1404867e0
        add rsp,38
        ret
    */
    public static unsafe void HomewardBone()
    {
        _homewardBone ??= Marshal.GetDelegateForFunctionPointer<HomewardBoneDelegate>((nint)0x1404867e0);
        var arg0 = Unsafe.Read<nint>((void*)Helpers.GetBaseBAddress());
        _homewardBone(arg0, 1);
    }

    public static unsafe void AddItem(int category, uint id, uint quantity, bool showMessage = false)
    {
        _addItem ??= Marshal.GetDelegateForFunctionPointer<AddItemDelegate>((nint)0x1407479e0);
        var rax = Unsafe.Read<nint>((void*)Helpers.GetChrBaseClassOffset());
        var r15 = Unsafe.Read<nint>((void*)(rax + 0x10));
        var arg0 = r15 + 0x280;
        _addItem(arg0, category, id, quantity, showMessage, showMessage, showMessage, showMessage);
    }
}