using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using PInvoke = Windows.Win32.PInvoke;

namespace DSAP.Game;

internal static class Proxy
{
    private static unsafe delegate* unmanaged<nint, uint, Guid*, nint*, nint, int> _directInput8Create;
    private static readonly ManualResetEventSlim WaitForInitialize = new(true);

#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var lib = NativeLibrary.Load(Path.Combine(systemPath, "dinput8.dll"));
        unsafe
        {
            _directInput8Create = (delegate* unmanaged<nint, uint, Guid*, nint*, nint, int>)NativeLibrary.GetExport(lib, "DirectInput8Create");
        }

        WaitForInitialize.Set();

        Task.Run(() =>
        {
            PInvoke.MessageBox(HWND.Null, "Initialized!", "DSAP.Game", MESSAGEBOX_STYLE.MB_OK);
        });
    }

    [UnmanagedCallersOnly(EntryPoint = "DirectInput8Create")]
    public static unsafe int DirectInput8Create(nint hinst, uint dwVersion, Guid* riidltf, nint* ppvOut, nint punkOuter)
    {
        WaitForInitialize.Wait();
        return _directInput8Create(hinst, dwVersion, riidltf, ppvOut, punkOuter);
    }
}