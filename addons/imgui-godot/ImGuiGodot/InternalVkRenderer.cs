#if IMGUI_GODOT_DEV
using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImGuiGodot;

internal class InternalVkRenderer : IRenderer
{
    private readonly RenderingDevice RD;
    private readonly Color[] clearColors = new[] { Colors.Transparent };
    //private readonly Color[] clearColors = new[] { new Color(0.45098f, 0.54902f, 0.60f) };
    private readonly RID _shader;
    private readonly RID _pipeline;
    private readonly RID _sampler;
    private readonly long _vtxFormat;
    private readonly Dictionary<RID, RID> _framebuffers = new();

    public InternalVkRenderer()
    {
        RD = RenderingServer.GetRenderingDevice();

        var src = new RDShaderSource();
        src.SourceVertex = vertexShaderSource;
        src.SourceFragment = fragmentShaderSource;
        var spirv = RD.ShaderCompileSpirvFromSource(src);
        _shader = RD.ShaderCreateFromSpirv(spirv);

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
        var blendData = new RDPipelineColorBlendState();
        blendData.Attachments.Add(bsa);
        blendData.BlendConstant = new Color(0, 0, 0, 0);

        var rasterizationState = new RDPipelineRasterizationState
        {
            FrontFace = RenderingDevice.PolygonFrontFace.CounterClockwise
        };

        _pipeline = RD.RenderPipelineCreate(
            _shader,
            RD.ScreenGetFramebufferFormat(),
            _vtxFormat,
            RenderingDevice.RenderPrimitive.Triangles,
            rasterizationState,
            new RDPipelineMultisampleState(),
            new RDPipelineDepthStencilState(),
            blendData);

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

    public void RenderDrawData(Viewport vp, ImDrawDataPtr drawData)
    {
        RD.DrawCommandBeginLabel("ImGui", Colors.Purple);
        RID fb = GetFramebuffer(vp);

        int vertSize = Marshal.SizeOf<ImDrawVert>();

        float[] scale = new float[2];
        scale[0] = 2.0f / drawData.DisplaySize.X;
        scale[1] = 2.0f / drawData.DisplaySize.Y;

        float[] translate = new float[2];
        translate[0] = -1.0f - (drawData.DisplayPos.X * scale[0]);
        translate[1] = -1.0f - (drawData.DisplayPos.Y * scale[1]);

        byte[] pcbuf = new byte[16];
        Buffer.BlockCopy(scale, 0, pcbuf, 0, 8);
        Buffer.BlockCopy(translate, 0, pcbuf, 8, 8);

        var vtxBuffers = new RID[drawData.CmdListsCount];
        var vtxArrays = new RID[drawData.CmdListsCount];
        var idxBuffers = new RID[drawData.CmdListsCount];
        var idxArrays = new List<RID[]>(drawData.CmdListsCount);
        var uniformSets = new Dictionary<IntPtr, RID>();

        for (int i = 0; i < drawData.CmdListsCount; ++i)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];

            int vertBytes = cmdList.VtxBuffer.Size * vertSize;
            byte[] vertBuf = new byte[vertBytes];
            Marshal.Copy(cmdList.VtxBuffer.Data, vertBuf, 0, vertBytes);

            int idxBytes = cmdList.IdxBuffer.Size * sizeof(ushort);
            byte[] idxBuf = new byte[idxBytes];
            Marshal.Copy(cmdList.IdxBuffer.Data, idxBuf, 0, idxBytes);

            vtxBuffers[i] = RD.VertexBufferCreate((uint)vertBuf.Length, vertBuf);
            vtxArrays[i] = RD.VertexArrayCreate((uint)cmdList.VtxBuffer.Size, _vtxFormat,
                new() { vtxBuffers[i], vtxBuffers[i], vtxBuffers[i] });

            idxBuffers[i] = RD.IndexBufferCreate((uint)cmdList.IdxBuffer.Size, RenderingDevice.IndexBufferFormat.Uint16, idxBuf);

            // create an index array for each draw command
            idxArrays.Add(new RID[cmdList.CmdBuffer.Size]);
            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; ++cmdi)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];
                idxArrays[i][cmdi] = RD.IndexArrayCreate(idxBuffers[i], drawCmd.IdxOffset, drawCmd.ElemCount);

                if (drawCmd.VtxOffset > 0)
                {
                    // TODO: ...
                }

                IntPtr texid = drawCmd.GetTexID();
                if (!uniformSets.ContainsKey(texid))
                {
                    RID texrid = RenderingServer.TextureGetRdTexture(Internal.ConstructRID((ulong)texid));

                    RDUniform uniform = new();
                    uniform.Binding = 0;
                    uniform.UniformType = RenderingDevice.UniformType.SamplerWithTexture;
                    uniform.AddId(_sampler);
                    uniform.AddId(texrid);
                    RID uniformSet = RD.UniformSetCreate(new() { uniform }, _shader, 0);
                    uniformSets.Add(texid, uniformSet);
                }
            }
        }

        var dl = RD.DrawListBegin(fb,
            RenderingDevice.InitialAction.Clear, RenderingDevice.FinalAction.Read,
            RenderingDevice.InitialAction.Clear, RenderingDevice.FinalAction.Read,
            clearColors);
        RD.DrawListBindRenderPipeline(dl, _pipeline);
        RD.DrawListSetPushConstant(dl, pcbuf, (uint)pcbuf.Length);

        for (int i = 0; i < drawData.CmdListsCount; ++i)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];

            RD.DrawListBindVertexArray(dl, vtxArrays[i]);

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; ++cmdi)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                if (drawCmd.ElemCount == 0)
                    continue;

                RD.DrawListBindUniformSet(dl, uniformSets[drawCmd.GetTexID()], 0);
                RD.DrawListBindIndexArray(dl, idxArrays[i][cmdi]);

                RD.DrawListEnableScissor(dl, new Rect2(
                    drawCmd.ClipRect.X,
                    drawCmd.ClipRect.Y,
                    drawCmd.ClipRect.Z - drawCmd.ClipRect.X,
                    drawCmd.ClipRect.W - drawCmd.ClipRect.Y));

                RD.DrawListDraw(dl, true, 1);

                RD.FreeRid(idxArrays[i][cmdi]);
            }

            RD.FreeRid(vtxArrays[i]);
            RD.FreeRid(vtxBuffers[i]);
            RD.FreeRid(idxBuffers[i]);
        }
        RD.DrawListEnd();
        RD.DrawCommandEndLabel();
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
#endif
