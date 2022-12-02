using Godot;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;

namespace ImGuiGodot.Internal;

internal interface IRenderer
{
    public string Name { get; }
    public void Init(ImGuiIOPtr io);
    public void InitViewport(Viewport vp);
    public void CloseViewport(Viewport vp);
    public void RenderDrawData(Viewport vp, ImDrawDataPtr drawData);
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
        InternalViewports.Init();
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

    public static void AddLayerSubViewport(Node parent, out SubViewportContainer subViewportContainer, out SubViewport subViewport)
    {
        subViewportContainer = new SubViewportContainer
        {
            Name = "ImGuiLayer_SubViewportContainer",
            AnchorsPreset = 15,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Stretch = true
        };

        subViewport = new SubViewport
        {
            Name = "ImGuiLayer_SubViewport",
            TransparentBg = true,
            HandleInputLocally = false,
            GuiDisableInput = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };

        subViewportContainer.AddChild(subViewport);
        parent.AddChild(subViewportContainer);
    }

    public static void Render(Viewport vp)
    {
        ImGui.Render();
        Renderer.RenderDrawData(vp, ImGui.GetDrawData());

        var io = ImGui.GetIO();
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            ImGui.UpdatePlatformWindows();
            InternalViewports.RenderViewports();
        }
    }
}
