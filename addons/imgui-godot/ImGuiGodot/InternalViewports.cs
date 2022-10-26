#if IMGUI_GODOT_DEV
using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;

namespace ImGuiGodot;

internal static class InternalViewports
{
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static unsafe extern void ImGuiPlatformIO_Set_Platform_GetWindowPos(ImGuiPlatformIO* platform_io, IntPtr funcPtr);
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static unsafe extern void ImGuiPlatformIO_Set_Platform_GetWindowSize(ImGuiPlatformIO* platform_io, IntPtr funcPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_CreateWindow(ImGuiViewportPtr viewport);
    private static Platform_CreateWindow _createWindow = Godot_CreateWindow;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_DestroyWindow(ImGuiViewportPtr viewport);
    private static Platform_DestroyWindow _destroyWindow = Godot_DestroyWindow;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_ShowWindow(ImGuiViewportPtr viewport);
    private static Platform_ShowWindow _showWindow = Godot_ShowWindow;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_SetWindowPos(ImGuiViewportPtr viewport, Vector2 pos);
    private static Platform_SetWindowPos _setWindowPos = Godot_SetWindowPos;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_GetWindowPos(ImGuiViewportPtr viewport, out Vector2 pos);
    private static Platform_GetWindowPos _getWindowPos = Godot_GetWindowPos;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_SetWindowSize(ImGuiViewportPtr viewport, Vector2 pos);
    private static Platform_SetWindowSize _setWindowSize = Godot_SetWindowSize;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_GetWindowSize(ImGuiViewportPtr viewport, out Vector2 size);
    private static Platform_GetWindowSize _getWindowSize = Godot_GetWindowSize;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_SetWindowFocus(ImGuiViewportPtr viewport);
    private static Platform_SetWindowFocus _setWindowFocus = Godot_SetWindowFocus;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool Platform_GetWindowFocus(ImGuiViewportPtr viewport);
    private static Platform_GetWindowFocus _getWindowFocus = Godot_GetWindowFocus;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool Platform_GetWindowMinimized(ImGuiViewportPtr viewport);
    private static Platform_GetWindowMinimized _getWindowMinimized = Godot_GetWindowMinimized;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_SetWindowTitle(ImGuiViewportPtr viewport, string title);
    private static Platform_SetWindowTitle _setWindowTitle = Godot_SetWindowTitle;

    private static Dictionary<RID, Window> _windows = new();
    //private static bool _wantUpdateMonitors = true;
    private static IntPtr _monitorArrayBuf = IntPtr.Zero;

    private static Vector2 ToVec2(this Vector2i v)
    {
        return new Vector2(v.x, v.y);
    }

    private static unsafe void UpdateMonitors()
    {
        int screenCount = DisplayServer.GetScreenCount();
        ImGuiPlatformMonitor[] monitorArray = new ImGuiPlatformMonitor[screenCount];
        var pio = ImGui.GetPlatformIO().NativePtr;

        for (int i = 0; i < screenCount; ++i)
        {
            monitorArray[i].DpiScale = 1.0f;
            monitorArray[i].MainPos = DisplayServer.ScreenGetPosition(i).ToVec2();
            monitorArray[i].MainSize = DisplayServer.ScreenGetSize(i).ToVec2();
            monitorArray[i].DpiScale = DisplayServer.ScreenGetScale(i);

            var r = DisplayServer.ScreenGetUsableRect(i);
            monitorArray[i].WorkPos = r.Position.ToVec2();
            monitorArray[i].WorkSize = r.Size.ToVec2();
        }

        if (_monitorArrayBuf != IntPtr.Zero)
            ImGui.MemFree(_monitorArrayBuf);
        int bytes = monitorArray.Length * sizeof(ImGuiPlatformMonitor);
        _monitorArrayBuf = ImGui.MemAlloc((uint)bytes);
        fixed (ImGuiPlatformMonitor* p = monitorArray)
        {
            Buffer.MemoryCopy(p, (byte*)_monitorArrayBuf, bytes, bytes);
        }
        *&pio->Monitors.Capacity = screenCount;
        *&pio->Monitors.Size = screenCount;
        *&pio->Monitors.Data = _monitorArrayBuf;
    }

    private static unsafe void InitPlatformInterface()
    {
        var pio = ImGui.GetPlatformIO().NativePtr;

        pio->Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
        pio->Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
        pio->Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
        pio->Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
        //pio->Platform_GetWindowPos = Marshal.GetFunctionPointerForDelegate(_getWindowPos);
        pio->Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
        //pio->Platform_GetWindowSize = Marshal.GetFunctionPointerForDelegate(_getWindowSize);
        pio->Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
        pio->Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
        pio->Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
        pio->Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);

        ImGuiPlatformIO_Set_Platform_GetWindowPos(pio, Marshal.GetFunctionPointerForDelegate(_getWindowPos));
        ImGuiPlatformIO_Set_Platform_GetWindowSize(pio, Marshal.GetFunctionPointerForDelegate(_getWindowSize));

        UpdateMonitors();
    }

    public static void Init(ImGuiIOPtr io, Window mainWindow)
    {
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;

        RID vprid = mainWindow.GetViewportRid();
        _windows[vprid] = mainWindow;

        var igvp = ImGui.GetMainViewport();
        igvp.PlatformHandle = (IntPtr)vprid.Id;

        InitPlatformInterface();
    }

    private static void Godot_CreateWindow(ImGuiViewportPtr viewport)
    {
    }

    private static void Godot_DestroyWindow(ImGuiViewportPtr viewport)
    {
    }

    private static void Godot_ShowWindow(ImGuiViewportPtr viewport)
    {
    }

    private static void Godot_SetWindowPos(ImGuiViewportPtr viewport, Vector2 pos)
    {
    }

    private static void Godot_GetWindowPos(ImGuiViewportPtr viewport, out Vector2 pos)
    {
        pos = new(0.0f, 0.0f);
    }

    private static void Godot_SetWindowSize(ImGuiViewportPtr viewport, Vector2 size)
    {
    }

    private static void Godot_GetWindowSize(ImGuiViewportPtr viewport, out Vector2 size)
    {
        size = new(0.0f, 0.0f);
    }

    private static void Godot_SetWindowFocus(ImGuiViewportPtr viewport)
    {
    }

    private static bool Godot_GetWindowFocus(ImGuiViewportPtr viewport)
    {
        return false;
    }

    private static bool Godot_GetWindowMinimized(ImGuiViewportPtr viewport)
    {
        return false;
    }

    private static void Godot_SetWindowTitle(ImGuiViewportPtr viewport, string title)
    {
    }
}
#endif
