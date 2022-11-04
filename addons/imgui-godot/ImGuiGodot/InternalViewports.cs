#if IMGUI_GODOT_DEV
using Godot;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;

namespace ImGuiGodot;

internal static class InternalViewports
{
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void ImGuiPlatformIO_Set_Platform_GetWindowPos(ImGuiPlatformIO* platform_io, IntPtr funcPtr);
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void ImGuiPlatformIO_Set_Platform_GetWindowSize(ImGuiPlatformIO* platform_io, IntPtr funcPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_CreateWindow(ImGuiViewportPtr vp);
    private static readonly Platform_CreateWindow _createWindow = Godot_CreateWindow;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_DestroyWindow(ImGuiViewportPtr vp);
    private static readonly Platform_DestroyWindow _destroyWindow = Godot_DestroyWindow;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_ShowWindow(ImGuiViewportPtr vp);
    private static readonly Platform_ShowWindow _showWindow = Godot_ShowWindow;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_SetWindowPos(ImGuiViewportPtr vp, Vector2 pos);
    private static readonly Platform_SetWindowPos _setWindowPos = Godot_SetWindowPos;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_GetWindowPos(ImGuiViewportPtr vp, out Vector2 pos);
    private static readonly Platform_GetWindowPos _getWindowPos = Godot_GetWindowPos;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_SetWindowSize(ImGuiViewportPtr vp, Vector2 pos);
    private static readonly Platform_SetWindowSize _setWindowSize = Godot_SetWindowSize;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_GetWindowSize(ImGuiViewportPtr vp, out Vector2 size);
    private static readonly Platform_GetWindowSize _getWindowSize = Godot_GetWindowSize;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_SetWindowFocus(ImGuiViewportPtr vp);
    private static readonly Platform_SetWindowFocus _setWindowFocus = Godot_SetWindowFocus;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool Platform_GetWindowFocus(ImGuiViewportPtr vp);
    private static readonly Platform_GetWindowFocus _getWindowFocus = Godot_GetWindowFocus;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool Platform_GetWindowMinimized(ImGuiViewportPtr vp);
    private static readonly Platform_GetWindowMinimized _getWindowMinimized = Godot_GetWindowMinimized;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Platform_SetWindowTitle(ImGuiViewportPtr vp, string title);
    private static readonly Platform_SetWindowTitle _setWindowTitle = Godot_SetWindowTitle;

    //private static bool _wantUpdateMonitors = true;
    private static GodotImGuiWindow _mainWindow;

    private static Vector2 ToVec2(this Vector2i v)
    {
        return new Vector2(v.x, v.y);
    }

    private static Vector2i ToVector2i(this Vector2 v)
    {
        return new Vector2i((int)v.X, (int)v.Y);
    }

    private class GodotImGuiWindow : IDisposable
    {
        private readonly GCHandle _gcHandle;
        private readonly ImGuiViewportPtr _vp;

        public RID CanvasRid { get; init; }
        public RID RootCanvasItem { get; init; }
        private bool _focused;
        private readonly Window _godotWindow;

        public GodotImGuiWindow(ImGuiViewportPtr vp)
        {
            _gcHandle = GCHandle.Alloc(this);
            _vp = vp;
            _vp.PlatformHandle = (IntPtr)_gcHandle;

            Rect2i winRect = new(_vp.Pos.ToVector2i(), _vp.Size.ToVector2i());
            GD.Print(winRect);

            ImGuiLayer.Instance.GetViewport().GuiEmbedSubwindows = false;

            _godotWindow = new Window()
            {
                // Borderless = true,
                Position = winRect.Position,
                Size = winRect.Size,
                Transparent = true,
                TransparentBg = true
            };

            _godotWindow.FocusEntered += () => _focused = true;
            _godotWindow.FocusExited += () => _focused = false;
            _godotWindow.CloseRequested += () => _vp.PlatformRequestClose = true;
            _godotWindow.SizeChanged += () => _vp.PlatformRequestResize = true;
            _godotWindow.WindowInput += OnWindowInput;

            ImGuiLayer.Instance.AddChild(_godotWindow);

            // need to do this after AddChild
            _godotWindow.Transparent = true;

            CanvasRid = RenderingServer.CanvasCreate();
            RootCanvasItem = RenderingServer.CanvasItemCreate();
            RenderingServer.ViewportAttachCanvas(_godotWindow.GetViewportRid(), CanvasRid);
            RenderingServer.ViewportSetTransparentBackground(_godotWindow.GetViewportRid(), true);
            RenderingServer.CanvasItemSetParent(RootCanvasItem, CanvasRid);

            RenderingServer.CanvasItemAddRect(RootCanvasItem, new(0, 0, 100, 100), Colors.Red);
        }

        public GodotImGuiWindow(ImGuiViewportPtr vp, Window gw)
        {
            _gcHandle = GCHandle.Alloc(this);
            _vp = vp;
            _vp.PlatformHandle = (IntPtr)_gcHandle;
            _godotWindow = gw;
        }

        public void Dispose()
        {
            if (_godotWindow.GetParent() != null)
            {
                RenderingServer.FreeRid(RootCanvasItem);
                RenderingServer.FreeRid(CanvasRid);
                _godotWindow.QueueFree();
            }
            _gcHandle.Free();
        }

        private void OnWindowInput(InputEvent evt)
        {
            Internal.ProcessInput(evt);
        }

        public void ShowWindow()
        {
            _godotWindow.Show();
        }

        public void SetWindowPos(Vector2i pos)
        {
            _godotWindow.Position = pos;
        }

        public Vector2i GetWindowPos()
        {
            return _godotWindow.Position;
        }

        public void SetWindowSize(Vector2i size)
        {
            _godotWindow.Size = size;
        }

        public Vector2i GetWindowSize()
        {
            return _godotWindow.Size;
        }

        public void SetWindowFocus()
        {
            _godotWindow.MoveToForeground();
        }

        public bool GetWindowFocus()
        {
            return _focused;
        }

        public bool GetWindowMinimized()
        {
            return _godotWindow.Mode.HasFlag(Window.ModeEnum.Minimized);
        }

        public void SetWindowTitle(string title)
        {
            _godotWindow.Title = title;
        }
    }

    private static void UpdateMonitors()
    {
        var pio = ImGui.GetPlatformIO();
        int screenCount = DisplayServer.GetScreenCount();

        // workaround for lack of ImVector constructor
        unsafe
        {
            int bytes = screenCount * sizeof(ImGuiPlatformMonitor);
            if (pio.NativePtr->Monitors.Data != IntPtr.Zero)
                ImGui.MemFree(pio.NativePtr->Monitors.Data);
            *&pio.NativePtr->Monitors.Data = ImGui.MemAlloc((uint)bytes);
            *&pio.NativePtr->Monitors.Capacity = screenCount;
            *&pio.NativePtr->Monitors.Size = screenCount;
        }

        for (int i = 0; i < screenCount; ++i)
        {
            var monitor = pio.Monitors[i];
            monitor.MainPos = DisplayServer.ScreenGetPosition(i).ToVec2();
            monitor.MainSize = DisplayServer.ScreenGetSize(i).ToVec2();
            monitor.DpiScale = DisplayServer.ScreenGetScale(i);

            var r = DisplayServer.ScreenGetUsableRect(i);
            monitor.WorkPos = r.Position.ToVec2();
            monitor.WorkSize = r.Size.ToVec2();
        }
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
    }

    public static void Init(ImGuiIOPtr io)
    {
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;

        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        _mainWindow = new(ImGui.GetMainViewport(), ImGuiLayer.Instance.GetViewport() as Window);

        InitPlatformInterface();
        UpdateMonitors();
    }

    private static void Godot_CreateWindow(ImGuiViewportPtr vp)
    {
        new GodotImGuiWindow(vp);
    }

    private static void Godot_DestroyWindow(ImGuiViewportPtr vp)
    {
        if (vp.PlatformHandle != IntPtr.Zero)
        {
            var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
            window.Dispose();
            vp.PlatformHandle = IntPtr.Zero;
        }
    }

    private static void Godot_ShowWindow(ImGuiViewportPtr vp)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        window.ShowWindow();
    }

    private static void Godot_SetWindowPos(ImGuiViewportPtr vp, Vector2 pos)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        window.SetWindowPos(pos.ToVector2i());
    }

    private static void Godot_GetWindowPos(ImGuiViewportPtr vp, out Vector2 pos)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        pos = window.GetWindowPos().ToVec2();
    }

    private static void Godot_SetWindowSize(ImGuiViewportPtr vp, Vector2 size)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        window.SetWindowSize(size.ToVector2i());
    }

    private static void Godot_GetWindowSize(ImGuiViewportPtr vp, out Vector2 size)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        size = window.GetWindowSize().ToVec2();
    }

    private static void Godot_SetWindowFocus(ImGuiViewportPtr vp)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        window.SetWindowFocus();
    }

    private static bool Godot_GetWindowFocus(ImGuiViewportPtr vp)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        return window.GetWindowFocus();
    }

    private static bool Godot_GetWindowMinimized(ImGuiViewportPtr vp)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        return window.GetWindowMinimized();
    }

    private static void Godot_SetWindowTitle(ImGuiViewportPtr vp, string title)
    {
        var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
        window.SetWindowTitle(title);
    }

    public static void RenderViewports()
    {
        var pio = ImGui.GetPlatformIO();
        for (int i = 1; i < pio.Viewports.Size; i++)
        {
            var vp = pio.Viewports[i];
            var window = (GodotImGuiWindow)GCHandle.FromIntPtr(vp.PlatformHandle).Target;
            Internal.RenderDrawData(vp.DrawData, window.RootCanvasItem);
        }
    }
}
#endif
