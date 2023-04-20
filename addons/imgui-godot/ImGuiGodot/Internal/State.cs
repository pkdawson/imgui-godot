using Godot;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;

namespace ImGuiGodot.Internal;

internal interface IRenderer
{
    public string Name { get; }
    public void Init(ImGuiIOPtr io);
    public void InitViewport(Rid vprid);
    public void CloseViewport(Rid vprid);
    public void RenderDrawData(Rid vprid, ImDrawDataPtr drawData);
    public void OnHide();
    public void Shutdown();
}

internal static class State
{
    private static readonly IntPtr _backendName = Marshal.StringToCoTaskMemAnsi("imgui_impl_godot4_net");
    private static IntPtr _rendererName = IntPtr.Zero;
    private static IntPtr _iniFilenameBuffer = IntPtr.Zero;
    internal static IRenderer Renderer { get; private set; }

    public static void Init(IRenderer renderer)
    {
        Renderer = renderer;
        Fonts.Init();

        if (ImGui.GetCurrentContext() != IntPtr.Zero)
        {
            ImGui.DestroyContext();
        }

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();

        io.BackendFlags = 0;
        io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

        if (_rendererName == IntPtr.Zero)
        {
            _rendererName = Marshal.StringToCoTaskMemAnsi(Renderer.Name);
        }

        unsafe
        {
            io.NativePtr->BackendPlatformName = (byte*)_backendName;
            io.NativePtr->BackendRendererName = (byte*)_rendererName;
        }

        Renderer.Init(io);
        Viewports.Init();
    }

    public static unsafe void SetIniFilename(ImGuiIOPtr io, string fileName)
    {
        io.NativePtr->IniFilename = null;

        if (_iniFilenameBuffer != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(_iniFilenameBuffer);
            _iniFilenameBuffer = IntPtr.Zero;
        }

        if (fileName?.Length > 0)
        {
            fileName = ProjectSettings.GlobalizePath(fileName);
            _iniFilenameBuffer = Marshal.StringToCoTaskMemUTF8(fileName);
            io.NativePtr->IniFilename = (byte*)_iniFilenameBuffer;
        }
    }

    public static void Render(Rid vprid)
    {
        ImGui.Render();
        Renderer.RenderDrawData(vprid, ImGui.GetDrawData());

        var io = ImGui.GetIO();
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            ImGui.UpdatePlatformWindows();
            Viewports.RenderViewports();
        }
    }
}
