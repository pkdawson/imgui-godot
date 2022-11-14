using Godot;
using ImGuiNET;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImGuiGodot;

internal class InternalRdRenderer : IRenderer
{
    private readonly RenderingDevice RD;
    private readonly Color[] _clearColors = new[] { new Color(0f, 0f, 0f, 0f) };
    private readonly RID _shader;
    private readonly RID _pipeline;
    private readonly RID _sampler;
    private readonly long _vtxFormat;
    private readonly Dictionary<RID, RID> _framebuffers = new();
    private readonly float[] _scale = new float[2];
    private readonly float[] _translate = new float[2];
    private readonly byte[] _pcbuf = new byte[16];
    private readonly ArrayPool<byte> _bufPool = ArrayPool<byte>.Create();
    private readonly ArrayPool<RID> _ridArrayPool = ArrayPool<RID>.Create();

    private readonly Dictionary<IntPtr, RID> _uniformSets = new(8);
    private readonly List<RID[]> _idxArrays = new(32);
    private readonly List<Dictionary<uint, RID>> _vtxArrays = new(32);
    private readonly HashSet<IntPtr> _usedTextures = new(8);

    private readonly Rect2 _zeroRect = new(new(0f, 0f), new(0f, 0f));
    private readonly Godot.Collections.Array _storageTextures = new();
    private readonly Godot.Collections.Array<RID> _srcBuffers = new();
    private readonly long[] _vtxOffsets = new long[3];
    private readonly Godot.Collections.Array<RDUniform> _uniformArray = new();

    public string Name => "imgui_impl_godot4_rd";

    public InternalRdRenderer()
    {
        RD = RenderingServer.GetRenderingDevice();

        // set up everything to match the official Vulkan backend as closely as possible

        // compile shader
        var src = new RDShaderSource
        {
            SourceVertex = vertexShaderSource,
            SourceFragment = fragmentShaderSource
        };
        var spirv = RD.ShaderCompileSpirvFromSource(src);
        _shader = RD.ShaderCreateFromSpirv(spirv);

        // create vertex format
        uint vtxStride = (uint)Marshal.SizeOf<ImDrawVert>();

        RDVertexAttribute attrPoints = new()
        {
            Location = 0,
            Format = RenderingDevice.DataFormat.R32g32Sfloat,
            Stride = vtxStride,
            Offset = 0
        };

        RDVertexAttribute attrUvs = new()
        {
            Location = 1,
            Format = RenderingDevice.DataFormat.R32g32Sfloat,
            Stride = vtxStride,
            Offset = sizeof(float) * 2
        };

        RDVertexAttribute attrColors = new()
        {
            Location = 2,
            Format = RenderingDevice.DataFormat.R8g8b8a8Unorm,
            Stride = vtxStride,
            Offset = sizeof(float) * 4
        };

        var vattrs = new Godot.Collections.Array<RDVertexAttribute>() { attrPoints, attrUvs, attrColors };
        _vtxFormat = RD.VertexFormatCreate(vattrs);

        // blend state
        var bsa = new RDPipelineColorBlendStateAttachment
        {
            EnableBlend = true,

            SrcColorBlendFactor = RenderingDevice.BlendFactor.SrcAlpha,
            DstColorBlendFactor = RenderingDevice.BlendFactor.OneMinusSrcAlpha,
            ColorBlendOp = RenderingDevice.BlendOperation.Add,

            SrcAlphaBlendFactor = RenderingDevice.BlendFactor.One,
            DstAlphaBlendFactor = RenderingDevice.BlendFactor.OneMinusSrcAlpha,
            AlphaBlendOp = RenderingDevice.BlendOperation.Add,
        };

        var blendData = new RDPipelineColorBlendState
        {
            BlendConstant = new Color(0, 0, 0, 0),
        };
        blendData.Attachments.Add(bsa);

        // rasterization state
        var rasterizationState = new RDPipelineRasterizationState
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
        var samplerState = new RDSamplerState
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

    public void InitViewport(Viewport vp)
    {
        if (vp is SubViewport svp)
        {
            svp.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            svp.RenderTargetClearMode = SubViewport.ClearMode.Never;
        }
    }

    public void CloseViewport(Viewport vp)
    {
    }

    private void ResizeAndClear(List<Dictionary<uint, RID>> dicts, int len)
    {
        // never remove elements, for better performance with multi-viewports

        while (dicts.Count < len)
        {
            dicts.Add(new());
        }

        for (int i = 0; i < len; ++i)
        {
            dicts[i].Clear();
        }
    }

    public void RenderDrawData(Viewport vp, ImDrawDataPtr drawData)
    {
        RD.DrawCommandBeginLabel("ImGui", Colors.Purple);
        RID fb = GetFramebuffer(vp);

        int vertSize = Marshal.SizeOf<ImDrawVert>();

        _scale[0] = 2.0f / drawData.DisplaySize.X;
        _scale[1] = 2.0f / drawData.DisplaySize.Y;

        _translate[0] = -1.0f - (drawData.DisplayPos.X * _scale[0]);
        _translate[1] = -1.0f - (drawData.DisplayPos.Y * _scale[1]);

        Buffer.BlockCopy(_scale, 0, _pcbuf, 0, 8);
        Buffer.BlockCopy(_translate, 0, _pcbuf, 8, 8);

        var vtxBuffers = _ridArrayPool.Rent(drawData.CmdListsCount);
        var idxBuffers = _ridArrayPool.Rent(drawData.CmdListsCount);
        ResizeAndClear(_vtxArrays, drawData.CmdListsCount);
        _idxArrays.Clear();
        _usedTextures.Clear();

        foreach (var kv in _uniformSets)
        {
            if (!RD.UniformSetIsValid(kv.Value))
                _uniformSets.Remove(kv.Key);
        }

        for (int i = 0; i < drawData.CmdListsCount; ++i)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];

            int vertBytes = cmdList.VtxBuffer.Size * vertSize;
            byte[] vertBuf = _bufPool.Rent(vertBytes);
            Marshal.Copy(cmdList.VtxBuffer.Data, vertBuf, 0, vertBytes);

            int idxBytes = cmdList.IdxBuffer.Size * sizeof(ushort);
            byte[] idxBuf = _bufPool.Rent(idxBytes);
            Marshal.Copy(cmdList.IdxBuffer.Data, idxBuf, 0, idxBytes);

            vtxBuffers[i] = RD.VertexBufferCreate((uint)vertBytes);
            RD.BufferUpdate(vtxBuffers[i], 0, (uint)vertBytes, vertBuf);

            idxBuffers[i] = RD.IndexBufferCreate((uint)cmdList.IdxBuffer.Size, RenderingDevice.IndexBufferFormat.Uint16);
            RD.BufferUpdate(idxBuffers[i], 0, (uint)idxBytes, idxBuf);

            _bufPool.Return(idxBuf);
            _bufPool.Return(vertBuf);

            // create an index array for each draw command
            _idxArrays.Add(_ridArrayPool.Rent(cmdList.CmdBuffer.Size));
            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; ++cmdi)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];
                if (drawCmd.ElemCount == 0)
                    continue;

                _idxArrays[i][cmdi] = RD.IndexArrayCreate(idxBuffers[i], drawCmd.IdxOffset, drawCmd.ElemCount);

                if (!_vtxArrays[i].ContainsKey(drawCmd.VtxOffset))
                {
                    long voff = drawCmd.VtxOffset * vertSize;
                    _srcBuffers[0] = _srcBuffers[1] = _srcBuffers[2] = vtxBuffers[i];
#if IMGUI_GODOT_DEV
                    _vtxOffsets[0] = _vtxOffsets[1] = _vtxOffsets[2] = voff;
                    _vtxArrays[i][drawCmd.VtxOffset] = RD.VertexArrayCreate(
                        (uint)cmdList.VtxBuffer.Size,
                        _vtxFormat,
                        _srcBuffers,
                        _vtxOffsets);
#else
                    // TODO: offsets workaround
                    _vtxArrays[i][drawCmd.VtxOffset] = RD.VertexArrayCreate(
                        (uint)cmdList.VtxBuffer.Size,
                        _vtxFormat,
                        _srcBuffers);
#endif
                }

                IntPtr texid = drawCmd.GetTexID();
                _usedTextures.Add(texid);
                if (!_uniformSets.ContainsKey(texid))
                {
                    RID texrid = RenderingServer.TextureGetRdTexture(Internal.ConstructRID((ulong)texid));
                    using RDUniform uniform = new()
                    {
                        Binding = 0,
                        UniformType = RenderingDevice.UniformType.SamplerWithTexture
                    };
                    uniform.AddId(_sampler);
                    uniform.AddId(texrid);
                    _uniformArray[0] = uniform;
                    RID uniformSet = RD.UniformSetCreate(_uniformArray, _shader, 0);
                    _uniformSets[texid] = uniformSet;
                }
            }
        }

        var dl = RD.DrawListBegin(fb,
            RenderingDevice.InitialAction.Clear, RenderingDevice.FinalAction.Read,
            RenderingDevice.InitialAction.Clear, RenderingDevice.FinalAction.Read,
            _clearColors, 1f, 0, _zeroRect, _storageTextures);
        RD.DrawListBindRenderPipeline(dl, _pipeline);
        RD.DrawListSetPushConstant(dl, _pcbuf, (uint)_pcbuf.Length);

        for (int i = 0; i < drawData.CmdListsCount; ++i)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; ++cmdi)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                if (drawCmd.ElemCount == 0)
                    continue;

                RD.DrawListBindUniformSet(dl, _uniformSets[drawCmd.GetTexID()], 0);
                RD.DrawListBindIndexArray(dl, _idxArrays[i][cmdi]);
                RD.DrawListBindVertexArray(dl, _vtxArrays[i][drawCmd.VtxOffset]);

                RD.DrawListEnableScissor(dl, new Rect2(
                    drawCmd.ClipRect.X,
                    drawCmd.ClipRect.Y,
                    drawCmd.ClipRect.Z - drawCmd.ClipRect.X,
                    drawCmd.ClipRect.W - drawCmd.ClipRect.Y));

                RD.DrawListDraw(dl, true, 1);

                RD.FreeRid(_idxArrays[i][cmdi]);
            }

            _ridArrayPool.Return(_idxArrays[i]);
            foreach (RID rid in _vtxArrays[i].Values)
            {
                RD.FreeRid(rid);
            }
            RD.FreeRid(vtxBuffers[i]);
            RD.FreeRid(idxBuffers[i]);
        }
        RD.DrawListEnd();
        RD.DrawCommandEndLabel();

        foreach (IntPtr texid in _uniformSets.Keys)
        {
            if (!_usedTextures.Contains(texid))
            {
                RD.FreeRid(_uniformSets[texid]);
                _uniformSets.Remove(texid);
            }
        }

        _ridArrayPool.Return(vtxBuffers);
        _ridArrayPool.Return(idxBuffers);
    }

    public void OnHide()
    {
    }

    public void Shutdown()
    {
        RD.FreeRid(_sampler);
        RD.FreeRid(_shader);
    }

    private RID GetFramebuffer(Viewport vp)
    {
        RID vprid = vp.GetViewportRid();
        if (_framebuffers.TryGetValue(vprid, out RID fb))
        {
            if (RD.FramebufferIsValid(fb))
                return fb;
        }

        RID vptex = RenderingServer.TextureGetRdTexture(vp.GetTexture().GetRid());
        fb = RD.FramebufferCreate(new() { vptex });
        _framebuffers[vprid] = fb;
        return fb;
    }

    // shader source borrowed from imgui_impl_vulkan.cpp
    private static readonly string vertexShaderSource = @"#version 450 core
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

    private static readonly string fragmentShaderSource = @"#version 450 core
layout(location = 0) out vec4 fColor;
layout(set=0, binding=0) uniform sampler2D sTexture;
layout(location = 0) in struct { vec4 Color; vec2 UV; } In;
void main()
{
    fColor = In.Color * texture(sTexture, In.UV.st);
}";
}
