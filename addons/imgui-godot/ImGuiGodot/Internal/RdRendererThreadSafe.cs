using Godot;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;

namespace ImGuiGodot.Internal;

internal sealed class RdRendererThreadSafe : RdRenderer, IRenderer
{
    public new string Name => "imgui_impl_godot4_rd_mt";

    private readonly object _sharedDataLock = new();
    private Tuple<Rid, ImDrawDataPtr>[] _dataToDraw = null;

    public RdRendererThreadSafe() : base()
    {
        // draw on the renderer thread to avoid conflicts
        RenderingServer.FramePreDraw += OnFramePreDraw;
    }

    ~RdRendererThreadSafe()
    {
        RenderingServer.FramePreDraw -= OnFramePreDraw;
    }

    private static unsafe ImDrawDataPtr CopyDrawData(ImDrawDataPtr drawData)
    {
        long ddsize = Marshal.SizeOf<ImDrawData>();
        ImDrawDataPtr rv = new(ImGui.MemAlloc((uint)ddsize));
        Buffer.MemoryCopy(drawData.NativePtr, rv.NativePtr, ddsize, ddsize);
        rv.CmdLists = ImGui.MemAlloc((uint)(Marshal.SizeOf<IntPtr>() * drawData.CmdListsCount));

        for (int i = 0; i < drawData.CmdListsCount; ++i)
        {
            rv.NativePtr->CmdLists[i] = drawData.CmdListsRange[i].CloneOutput().NativePtr;
        }
        return rv;
    }

    private static unsafe void FreeDrawData(ImDrawDataPtr drawData)
    {
        for (int i = 0; i < drawData.CmdListsCount; ++i)
        {
            drawData.CmdListsRange[i].Destroy();
        }
        ImGui.MemFree(drawData.CmdLists);
        ImGui.MemFree((IntPtr)drawData.NativePtr);
    }

    private static void FreeAll(Tuple<Rid, ImDrawDataPtr>[] array)
    {
        foreach (var kv in array)
        {
            FreeDrawData(kv.Item2);
        }
    }

    public new void RenderDrawData()
    {
        var pio = ImGui.GetPlatformIO();
        var newData = new Tuple<Rid, ImDrawDataPtr>[pio.Viewports.Size];

        for (int i = 0; i < pio.Viewports.Size; ++i)
        {
            // TODO: skip minimized windows
            var vp = pio.Viewports[i];
            ReplaceTextureRids(vp.DrawData);
            Rid vprid = Util.ConstructRid((ulong)vp.RendererUserData);
            newData[i] = new(GetFramebuffer(vprid), CopyDrawData(vp.DrawData));
        }

        lock (_sharedDataLock)
        {
            // if a frame was skipped, free old data
            if (_dataToDraw != null)
                FreeAll(_dataToDraw);
            _dataToDraw = newData;
        }
    }

    private void OnFramePreDraw()
    {
        Tuple<Rid, ImDrawDataPtr>[] dataArray = null;
        lock (_sharedDataLock)
        {
            // take ownership of shared data
            dataArray = _dataToDraw;
            _dataToDraw = null;
        }

        if (dataArray == null)
            return;

        foreach (var kv in dataArray)
        {
            if (RD.FramebufferIsValid(kv.Item1))
                RenderOne(kv.Item1, kv.Item2);
        }
        FreeAll(dataArray);
    }
}
