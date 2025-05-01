#if GODOT_PC
#nullable enable
using Godot;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SharedList = ImGuiGodot.Internal.DisposableList<Godot.Rid,
    ImGuiGodot.Internal.ClonedDrawData>;

namespace ImGuiGodot.Internal;

internal sealed class ClonedDrawData : IDisposable
{
    public ImDrawDataPtr Data { get; private set; }

    public unsafe ClonedDrawData(ImDrawDataPtr inp)
    {
        // deep swap is difficult because ImGui still owns the draw lists
        // TODO: revisit when Godot's threaded renderer is stable

        long ddsize = Marshal.SizeOf<ImDrawData>();

        // start with a shallow copy
        Data = new((ImDrawData*)ImGui.MemAlloc((uint)ddsize));
        Buffer.MemoryCopy(inp.Handle, Data.Handle, ddsize, ddsize);

        // clone the draw data
        int numLists = inp.CmdLists.Size;
        nint cmdListPtrs = (nint)ImGui.MemAlloc((uint)(Marshal.SizeOf<IntPtr>() * numLists));
        Data.Handle->CmdLists = new ImVector<ImDrawListPtr>(numLists, numLists,
            (ImDrawListPtr*)cmdListPtrs);
        for (int i = 0; i < inp.CmdLists.Size; ++i)
        {
            Data.CmdLists[i] = inp.CmdLists[i].CloneOutput();
        }
    }

    public unsafe void Dispose()
    {
        if (Data.Handle == null)
            return;

        for (int i = 0; i < Data.CmdListsCount; ++i)
        {
            Data.CmdLists[i].Destroy();
        }
        Data.Destroy();
        Data = new(null);
    }
}

internal sealed class DisposableList<T, U> : List<Tuple<T, U>>, IDisposable where U : IDisposable
{
    public DisposableList() { }
    public DisposableList(int capacity) : base(capacity) { }

    public void Dispose()
    {
        foreach (var (_, u) in this)
        {
            u.Dispose();
        }
        Clear();
    }
}

internal sealed class RdRendererThreadSafe : RdRenderer, IRenderer
{
    public new string Name => "godot4_net_rd_mt";

#if GODOT4_3_OR_GREATER
    public new void Render()
    {
        var pio = ImGui.GetPlatformIO();
        var newData = new SharedList(pio.Viewports.Size);

        for (int i = 0; i < pio.Viewports.Size; ++i)
        {
            var vp = pio.Viewports[i];
            if (vp.Flags.HasFlag(ImGuiViewportFlags.IsMinimized))
                continue;

            unsafe
            {
                Rid vprid = Util.ConstructRid((ulong)vp.RendererUserData);
                newData.Add(new(vprid, new(vp.DrawData)));
            }
        }

        RenderingServer.CallOnRenderThread(Callable.From(() => DrawOnRenderThread(newData)));
    }

    private void DrawOnRenderThread(SharedList dataArray)
    {
        foreach (var (vprid, clone) in dataArray)
        {
            Rid fb = GetFramebuffer(vprid);
            if (RD.FramebufferIsValid(fb))
            {
                ReplaceTextureRids(clone.Data);
                RenderOne(fb, clone.Data);
            }
        }

        FreeUnusedTextures();
        dataArray.Dispose();
    }
#else
    private SharedList? _dataToDraw;

    public RdRendererThreadSafe()
    {
        // draw on the renderer thread to avoid conflicts
        RenderingServer.FramePreDraw += OnFramePreDraw;
    }

    ~RdRendererThreadSafe()
    {
        RenderingServer.FramePreDraw -= OnFramePreDraw;
    }

    public new void Render()
    {
        var pio = ImGui.GetPlatformIO();
        var newData = new SharedList(pio.Viewports.Size);

        for (int i = 0; i < pio.Viewports.Size; ++i)
        {
            var vp = pio.Viewports[i];
            if (vp.Flags.HasFlag(ImGuiViewportFlags.IsMinimized))
                continue;

            ReplaceTextureRids(vp.DrawData);
            Rid vprid = Util.ConstructRid((ulong)vp.RendererUserData);
            newData.Add(new(GetFramebuffer(vprid), new(vp.DrawData)));
        }

        // if a frame was skipped, free old data
        var oldData = System.Threading.Interlocked.Exchange(ref _dataToDraw, newData);
        oldData?.Dispose();
    }

    private SharedList TakeSharedData()
    {
        var rv = System.Threading.Interlocked.Exchange(ref _dataToDraw, null);
        return rv ?? [];
    }

    private void OnFramePreDraw()
    {
        // take ownership of shared data
        using SharedList dataArray = TakeSharedData();

        foreach (var (fb, clone) in dataArray)
        {
            if (RD.FramebufferIsValid(fb))
                RenderOne(fb, clone.Data);
        }

        FreeUnusedTextures();
    }
#endif
}
#endif
