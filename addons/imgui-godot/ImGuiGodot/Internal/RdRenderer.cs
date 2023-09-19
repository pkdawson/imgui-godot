using Godot;
using ImGuiNET;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImGuiGodot.Internal;

internal class RdRenderer : IRenderer
{
    protected readonly RenderingDevice RD;
    private readonly Color[] _clearColors = { new Color(0f, 0f, 0f, 0f) };
    private readonly Rid _shader;
    private readonly Rid _pipeline;
    private readonly Rid _sampler;
    private readonly long _vtxFormat;
    private readonly Dictionary<Rid, Rid> _framebuffers = new();
    private readonly float[] _scale = new float[2];
    private readonly float[] _translate = new float[2];
    private readonly byte[] _pcbuf = new byte[16];
    private readonly ArrayPool<byte> _bufPool = ArrayPool<byte>.Create();

    private Rid _idxBuffer;
    private int _idxBufferSize = 0; // size in indices
    private Rid _vtxBuffer;
    private int _vtxBufferSize = 0; // size in vertices

    private readonly Dictionary<IntPtr, Rid> _uniformSets = new(8);
    private readonly HashSet<IntPtr> _usedTextures = new(8);

    private readonly Rect2 _zeroRect = new(new(0f, 0f), new(0f, 0f));
#if GODOT4_1_OR_GREATER
    private readonly Godot.Collections.Array<Rid> _storageTextures = new();
#else
    private readonly Godot.Collections.Array _storageTextures = new();
#endif
    private readonly Godot.Collections.Array<Rid> _srcBuffers = new();
    private readonly long[] _vtxOffsets = new long[3];
    private readonly Godot.Collections.Array<RDUniform> _uniformArray = new();

    public string Name => "imgui_impl_godot4_rd";

    public RdRenderer()
    {
        RD = RenderingServer.GetRenderingDevice();
        if (RD is null)
        {
            throw new PlatformNotSupportedException("failed to get RenderingDevice");
        }

        // set up everything to match the official Vulkan backend as closely as possible

        // compiling from source takes ~400ms, so we use a SPIR-V resource
        using var spirv = ResourceLoader.Load<RDShaderSpirV>("res://addons/imgui-godot/data/ImGuiShaderSPIRV.tres");
        _shader = RD.ShaderCreateFromSpirV(spirv);

#if IMGUI_GODOT_DEV
#pragma warning disable CA2201
        using var src = new RDShaderSource()
        {
            SourceFragment = _fragmentShaderSource,
            SourceVertex = _vertexShaderSource,
        };
        using var freshSpirv = RD.ShaderCompileSpirvFromSource(src);
        if (!System.Linq.Enumerable.SequenceEqual(spirv.BytecodeFragment, freshSpirv.BytecodeFragment))
            throw new Exception("fragment bytecode mismatch");
        if (!System.Linq.Enumerable.SequenceEqual(spirv.BytecodeVertex, freshSpirv.BytecodeVertex))
            throw new Exception("vertex bytecode mismatch");
#pragma warning restore CA2201
#endif

        // create vertex format
        uint vtxStride = (uint)Marshal.SizeOf<ImDrawVert>();

        using RDVertexAttribute attrPoints = new()
        {
            Location = 0,
            Format = RenderingDevice.DataFormat.R32G32Sfloat,
            Stride = vtxStride,
            Offset = 0
        };

        using RDVertexAttribute attrUvs = new()
        {
            Location = 1,
            Format = RenderingDevice.DataFormat.R32G32Sfloat,
            Stride = vtxStride,
            Offset = sizeof(float) * 2
        };

        using RDVertexAttribute attrColors = new()
        {
            Location = 2,
            Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
            Stride = vtxStride,
            Offset = sizeof(float) * 4
        };

        var vattrs = new Godot.Collections.Array<RDVertexAttribute>() { attrPoints, attrUvs, attrColors };
        _vtxFormat = RD.VertexFormatCreate(vattrs);

        // blend state
        using var bsa = new RDPipelineColorBlendStateAttachment
        {
            EnableBlend = true,

            SrcColorBlendFactor = RenderingDevice.BlendFactor.SrcAlpha,
            DstColorBlendFactor = RenderingDevice.BlendFactor.OneMinusSrcAlpha,
            ColorBlendOp = RenderingDevice.BlendOperation.Add,

            SrcAlphaBlendFactor = RenderingDevice.BlendFactor.One,
            DstAlphaBlendFactor = RenderingDevice.BlendFactor.OneMinusSrcAlpha,
            AlphaBlendOp = RenderingDevice.BlendOperation.Add,
        };

        using var blendData = new RDPipelineColorBlendState
        {
            BlendConstant = new Color(0, 0, 0, 0),
        };
        blendData.Attachments.Add(bsa);

        // rasterization state
        using var rasterizationState = new RDPipelineRasterizationState
        {
            FrontFace = RenderingDevice.PolygonFrontFace.CounterClockwise
        };

        // pipeline
        _pipeline = RD.RenderPipelineCreate(
            _shader,
            RD.ScreenGetFramebufferFormat(),
            _vtxFormat,
            RenderingDevice.RenderPrimitive.Triangles,
            rasterizationState,
            new RDPipelineMultisampleState(),
            new RDPipelineDepthStencilState(),
            blendData);

        // sampler used for all textures
        using var samplerState = new RDSamplerState
        {
            MinFilter = RenderingDevice.SamplerFilter.Linear,
            MagFilter = RenderingDevice.SamplerFilter.Linear,
            MipFilter = RenderingDevice.SamplerFilter.Linear,
            RepeatU = RenderingDevice.SamplerRepeatMode.Repeat,
            RepeatV = RenderingDevice.SamplerRepeatMode.Repeat,
            RepeatW = RenderingDevice.SamplerRepeatMode.Repeat
        };
        _sampler = RD.SamplerCreate(samplerState);

        _srcBuffers.Resize(3);
        _uniformArray.Resize(1);
    }

    public void Init(ImGuiIOPtr io)
    {
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
    }

    public void InitViewport(Rid vprid)
    {
        //RenderingServer.ViewportSetUpdateMode(vprid, RenderingServer.ViewportUpdateMode.Disabled);
        RenderingServer.ViewportSetClearMode(vprid, RenderingServer.ViewportClearMode.Never);
    }

    public void CloseViewport(Rid vprid)
    {
    }

    private void SetupBuffers(ImDrawDataPtr drawData)
    {
        int vertSize = Marshal.SizeOf<ImDrawVert>();
        int globalIdxOffset = 0;
        int globalVtxOffset = 0;

        // set up buffers
        int idxBufSize = drawData.TotalIdxCount * sizeof(ushort);
        byte[] idxBuf = _bufPool.Rent(idxBufSize);
        int vertBufSize = drawData.TotalVtxCount * vertSize;
        byte[] vertBuf = _bufPool.Rent(vertBufSize);
        for (int i = 0; i < drawData.CmdLists.Size; ++i)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            int vertBytes = cmdList.VtxBuffer.Size * vertSize;
            Marshal.Copy(cmdList.VtxBuffer.Data, vertBuf, globalVtxOffset, vertBytes);
            globalVtxOffset += vertBytes;

            int idxBytes = cmdList.IdxBuffer.Size * sizeof(ushort);
            Marshal.Copy(cmdList.IdxBuffer.Data, idxBuf, globalIdxOffset, idxBytes);
            globalIdxOffset += idxBytes;

            // create a uniform set for each texture
            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; ++cmdi)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];
                IntPtr texid = drawCmd.GetTexID();
                if (texid == IntPtr.Zero)
                    continue;
                Rid texrid = Util.ConstructRid((ulong)texid);
                if (!RD.TextureIsValid(texrid))
                    continue;

                _usedTextures.Add(texid);
                if (!_uniformSets.ContainsKey(texid))
                {
                    using RDUniform uniform = new()
                    {
                        Binding = 0,
                        UniformType = RenderingDevice.UniformType.SamplerWithTexture
                    };
                    uniform.AddId(_sampler);
                    uniform.AddId(texrid);
                    _uniformArray[0] = uniform;
                    Rid uniformSet = RD.UniformSetCreate(_uniformArray, _shader, 0);
                    _uniformSets[texid] = uniformSet;
                }
            }
        }
        RD.BufferUpdate(_idxBuffer, 0, (uint)idxBufSize, idxBuf);
        _bufPool.Return(idxBuf);
        RD.BufferUpdate(_vtxBuffer, 0, (uint)vertBufSize, vertBuf);
        _bufPool.Return(vertBuf);
    }

    protected static void ReplaceTextureRids(ImDrawDataPtr drawData)
    {
        for (int i = 0; i < drawData.CmdLists.Size; ++i)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];
            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; ++cmdi)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];
                drawCmd.TextureId = (IntPtr)RenderingServer.TextureGetRdTexture(Util.ConstructRid((ulong)drawCmd.TextureId)).Id;
            }
        }
    }

    protected void FreeUnusedTextures()
    {
        // clean up unused textures
        foreach (IntPtr texid in _uniformSets.Keys)
        {
            if (!_usedTextures.Contains(texid))
            {
                RD.FreeRid(_uniformSets[texid]);
                _uniformSets.Remove(texid);
            }
        }
        _usedTextures.Clear();
    }

    public void RenderDrawData()
    {
        var pio = ImGui.GetPlatformIO();
        for (int i = 0; i < pio.Viewports.Size; ++i)
        {
            var vp = pio.Viewports[i];
            if (!vp.Flags.HasFlag(ImGuiViewportFlags.IsMinimized))
            {
                ReplaceTextureRids(vp.DrawData);
                Rid vprid = Util.ConstructRid((ulong)vp.RendererUserData);
                RenderOne(GetFramebuffer(vprid), vp.DrawData);
            }
        }
        FreeUnusedTextures();
    }

    protected void RenderOne(Rid fb, ImDrawDataPtr drawData)
    {
#if IMGUI_GODOT_DEV
        RD.DrawCommandBeginLabel("ImGui", Colors.Purple);
#endif

        if (!fb.IsValid)
            return;

        int vertSize = Marshal.SizeOf<ImDrawVert>();

        _scale[0] = 2.0f / drawData.DisplaySize.X;
        _scale[1] = 2.0f / drawData.DisplaySize.Y;

        _translate[0] = -1.0f - (drawData.DisplayPos.X * _scale[0]);
        _translate[1] = -1.0f - (drawData.DisplayPos.Y * _scale[1]);

        Buffer.BlockCopy(_scale, 0, _pcbuf, 0, 8);
        Buffer.BlockCopy(_translate, 0, _pcbuf, 8, 8);

        // allocate merged index and vertex buffers
        if (_idxBufferSize < drawData.TotalIdxCount)
        {
            if (_idxBuffer.Id != 0)
                RD.FreeRid(_idxBuffer);
            _idxBuffer = RD.IndexBufferCreate((uint)drawData.TotalIdxCount, RenderingDevice.IndexBufferFormat.Uint16);
            _idxBufferSize = drawData.TotalIdxCount;
        }

        if (_vtxBufferSize < drawData.TotalVtxCount)
        {
            if (_vtxBuffer.Id != 0)
                RD.FreeRid(_vtxBuffer);
            _vtxBuffer = RD.VertexBufferCreate((uint)(drawData.TotalVtxCount * vertSize));
            _vtxBufferSize = drawData.TotalVtxCount;
        }

        // check if our font texture is still valid
        foreach (var kv in _uniformSets)
        {
            if (!RD.UniformSetIsValid(kv.Value))
                _uniformSets.Remove(kv.Key);
        }

        if (drawData.CmdListsCount > 0)
            SetupBuffers(drawData);

        // draw
        long dl = RD.DrawListBegin(fb,
                RenderingDevice.InitialAction.Clear, RenderingDevice.FinalAction.Read,
                RenderingDevice.InitialAction.Clear, RenderingDevice.FinalAction.Read,
                _clearColors, 1f, 0, _zeroRect, _storageTextures);

        RD.DrawListBindRenderPipeline(dl, _pipeline);
        RD.DrawListSetPushConstant(dl, _pcbuf, (uint)_pcbuf.Length);

        int globalIdxOffset = 0;
        int globalVtxOffset = 0;
        for (int i = 0; i < drawData.CmdLists.Size; ++i)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; ++cmdi)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];
                if (drawCmd.ElemCount == 0)
                    continue;
                if (!_uniformSets.ContainsKey(drawCmd.GetTexID()))
                    continue;

                Rid idxArray = RD.IndexArrayCreate(_idxBuffer,
                    (uint)(drawCmd.IdxOffset + globalIdxOffset),
                    drawCmd.ElemCount);

                long voff = (drawCmd.VtxOffset + globalVtxOffset) * vertSize;
                _srcBuffers[0] = _srcBuffers[1] = _srcBuffers[2] = _vtxBuffer;
                _vtxOffsets[0] = _vtxOffsets[1] = _vtxOffsets[2] = voff;
                Rid vtxArray = RD.VertexArrayCreate(
                    (uint)cmdList.VtxBuffer.Size,
                    _vtxFormat,
                    _srcBuffers,
                    _vtxOffsets);

                RD.DrawListBindUniformSet(dl, _uniformSets[drawCmd.GetTexID()], 0);
                RD.DrawListBindIndexArray(dl, idxArray);
                RD.DrawListBindVertexArray(dl, vtxArray);

                var clipRect = new Rect2(
                    drawCmd.ClipRect.X,
                    drawCmd.ClipRect.Y,
                    drawCmd.ClipRect.Z - drawCmd.ClipRect.X,
                    drawCmd.ClipRect.W - drawCmd.ClipRect.Y);
                clipRect.Position -= drawData.DisplayPos.ToVector2I();
                RD.DrawListEnableScissor(dl, clipRect);

                RD.DrawListDraw(dl, true, 1);

                RD.FreeRid(idxArray);
                RD.FreeRid(vtxArray);
            }
            globalIdxOffset += cmdList.IdxBuffer.Size;
            globalVtxOffset += cmdList.VtxBuffer.Size;
        }
        RD.DrawListEnd();
#if IMGUI_GODOT_DEV
        RD.DrawCommandEndLabel();
#endif
    }

    public void OnHide()
    {
    }

    public void Shutdown()
    {
        RD.FreeRid(_sampler);
        RD.FreeRid(_shader);
        if (_idxBuffer.Id != 0)
            RD.FreeRid(_idxBuffer);
        if (_vtxBuffer.Id != 0)
            RD.FreeRid(_vtxBuffer);
    }

    protected Rid GetFramebuffer(Rid vprid)
    {
        if (!vprid.IsValid)
            return new Rid();

        if (_framebuffers.TryGetValue(vprid, out Rid fb))
        {
            if (RD.FramebufferIsValid(fb))
                return fb;
        }

        Rid vptex = RenderingServer.TextureGetRdTexture(RenderingServer.ViewportGetTexture(vprid));
        fb = RD.FramebufferCreate(new() { vptex });
        _framebuffers[vprid] = fb;
        return fb;
    }

#if IMGUI_GODOT_DEV
    // shader source borrowed from imgui_impl_vulkan.cpp
    private static readonly string _vertexShaderSource = @"#version 450 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUV;
layout(location = 2) in vec4 aColor;
layout(push_constant) uniform uPushConstant { vec2 uScale; vec2 uTranslate; } pc;

out gl_PerVertex { vec4 gl_Position; };
layout(location = 0) out struct { vec4 Color; vec2 UV; } Out;

void main()
{
    Out.Color = aColor;
    Out.UV = aUV;
    gl_Position = vec4(aPos * pc.uScale + pc.uTranslate, 0, 1);
}";

    private static readonly string _fragmentShaderSource = @"#version 450 core
layout(location = 0) out vec4 fColor;
layout(set=0, binding=0) uniform sampler2D sTexture;
layout(location = 0) in struct { vec4 Color; vec2 UV; } In;
void main()
{
    fColor = In.Color * texture(sTexture, In.UV.st);
}";
#endif
}
