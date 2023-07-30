using System;
using System.Runtime.InteropServices;
using Godot;
using ImGuiNET;

namespace ImGuiGodot.Internal;

public class PublicInterfaceNative : IPublicInterface
{
    private readonly GodotObject _gd = Engine.GetSingleton("ImGuiGD");

    public void AddFont(FontFile fontData, int fontSize, bool merge)
    {
        _gd.Call("AddFont", fontData, fontSize, merge);
    }

    public void Connect(Callable callable)
    {
        _gd.Call("Connect", callable);
    }

    public void Init(Window mainWindow, Rid mainSubViewport, Resource cfg)
    {
        _gd.Call("Init", mainWindow, mainSubViewport, cfg);
    }

    public bool ProcessInput(InputEvent evt, Window window)
    {
        return (bool)_gd.Call("ProcessInput", evt, window);
    }

    public void RebuildFontAtlas(float scale)
    {
        _gd.Call("RebuildFontAtlas", scale);
    }

    public void Render()
    {
        _gd.Call("Render");
    }

    public void ResetFonts()
    {
        _gd.Call("ResetFonts");
    }

    public void SetIniFilename(ImGuiIOPtr io, string fileName)
    {
        _gd.Call("SetIniFilename", fileName);
    }

    public void SetJoyAxisDeadZone(float zone)
    {
        throw new NotImplementedException();
    }

    public void SetJoyButtonSwapAB(bool swap)
    {
        throw new NotImplementedException();
    }

    public void SetVisible(bool visible)
    {
        _gd.Call("SetVisible", visible);
    }

    public void Shutdown()
    {
        throw new NotImplementedException();
    }

    public bool SubViewport(SubViewport vp)
    {
        return (bool)_gd.Call("SubViewport", vp);
    }

    public void SyncImGuiPtrs()
    {
        long[] ptrs = (long[])_gd.Call("GetImGuiPtrs",
            ImGui.GetVersion(),
            Marshal.SizeOf<ImGuiIO>(),
            Marshal.SizeOf<ImDrawVert>(),
            sizeof(ushort),
            sizeof(ushort)
            );

        if (ptrs.Length != 3)
        {
            return;
        }

        checked
        {
            ImGui.SetCurrentContext((IntPtr)ptrs[0]);
            ImGui.SetAllocatorFunctions((IntPtr)ptrs[1], (IntPtr)ptrs[2]);
        }
    }

    public bool ToolInit()
    {
        _gd.Call("ToolInit");
        return true;
    }

    public void Update(double delta, Vector2 displaySize)
    {
        throw new NotImplementedException();
    }
}
