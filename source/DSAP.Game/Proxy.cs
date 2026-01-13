using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DSAP.Game;

internal static class Proxy
{
    private static unsafe delegate* unmanaged<nint, uint, Guid*, nint*, nint, int> _directInput8Create;
    private static unsafe delegate* unmanaged<void> _steamApiRunCallbacks;

    private static readonly ManualResetEventSlim WaitForFunctionPointers = new(false);

    private static readonly Queue<GameAction> PendingRetryActions = new();

#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        try
        {
            var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var lib = NativeLibrary.Load(Path.Combine(systemPath, "dinput8.dll"));
            unsafe
            {
                _directInput8Create = (delegate* unmanaged<nint, uint, Guid*, nint*, nint, int>)NativeLibrary.GetExport(lib, "DirectInput8Create");
            }

            HookSteamApiRunCallbacks();

            WaitForFunctionPointers.Set();

            Task.Run(async () =>
            {
                try
                {
                    var builder = WebApplication.CreateSlimBuilder();
                    builder.WebHost.ConfigureKestrel(options =>
                    {
                        options.ListenLocalhost(15950, o => o.Protocols = HttpProtocols.Http2);
                    });

                    builder.Services.AddGrpc();
                    var app = builder.Build();
                    app.MapGrpcService<ArchipelagoService>();

                    await app.RunAsync();
                }
                catch (Exception e)
                {
                    ShowError($"The background server crashed. Client connections will fail.\n\n{e.Message}");
                }
            });
        }
        catch (Exception e)
        {
            ShowError($"Failed to initialize DSAP Proxy.\n\n{e.Message}");
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "DirectInput8Create")]
    public static unsafe int DirectInput8Create(nint hinst, uint dwVersion, Guid* riidltf, nint* ppvOut, nint punkOuter)
    {
        WaitForFunctionPointers.Wait();
        return _directInput8Create(hinst, dwVersion, riidltf, ppvOut, punkOuter);
    }

    private static void ShowError(string message)
    {
        Windows.Win32.PInvoke.MessageBox(HWND.Null, message, Assembly.GetExecutingAssembly().FullName, MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR | MESSAGEBOX_STYLE.MB_TOPMOST);
    }

    private static unsafe void HookSteamApiRunCallbacks()
    {
        var importTableEntry = Hooking.Helpers.FromImport(null, "steam_api64.dll", "SteamAPI_RunCallbacks", 0);

        if (!Windows.Win32.PInvoke.VirtualProtect(
                importTableEntry,
                (nuint)Unsafe.SizeOf<nint>(),
                PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE,
                out var oldProtect))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        _steamApiRunCallbacks = (delegate* unmanaged<void>)Unsafe.Read<nint>(importTableEntry);
        Unsafe.Write(importTableEntry, (nint)(delegate* unmanaged<void>)(&ProcessActions));

        Windows.Win32.PInvoke.VirtualProtect(importTableEntry, (nuint)Unsafe.SizeOf<nint>(), oldProtect, out _);
    }

    [UnmanagedCallersOnly]
    private static void ProcessActions()
    {
        if (PendingRetryActions.TryDequeue(out var action))
        {
            ProcessAction(action);
        }

        if (ActionQueue.PendingActions.TryDequeue(out action))
        {
            ProcessAction(action);
        }

        WaitForFunctionPointers.Wait();
        unsafe
        {
            _steamApiRunCallbacks();
        }
    }

    private static void ProcessAction(GameAction action)
    {
        try
        {
            if (action.ActionToRun())
            {
                action.CompletionSource.SetResult();
            }
            else
            {
                PendingRetryActions.Enqueue(action);
            }
        }
        catch (Exception e)
        {
            ShowError($"Failed to process action.\n\n{e.Message}");
        }
    }
}