#include "RdRenderer.h"
#include "common.h"
#include <array>
#include <imgui.h>
#include <unordered_map>
#include <unordered_set>

#pragma warning(push, 0)
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/rd_pipeline_color_blend_state.hpp>
#include <godot_cpp/classes/rd_pipeline_color_blend_state_attachment.hpp>
#include <godot_cpp/classes/rd_pipeline_depth_stencil_state.hpp>
#include <godot_cpp/classes/rd_pipeline_multisample_state.hpp>
#include <godot_cpp/classes/rd_pipeline_rasterization_state.hpp>
#include <godot_cpp/classes/rd_sampler_state.hpp>
#include <godot_cpp/classes/rd_shader_spirv.hpp>
#include <godot_cpp/classes/rd_uniform.hpp>
#include <godot_cpp/classes/rd_vertex_attribute.hpp>
#include <godot_cpp/classes/rendering_device.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#pragma warning(pop)

using godot::Color;
using godot::RDPipelineColorBlendState;
using godot::RDPipelineColorBlendStateAttachment;
using godot::RDPipelineRasterizationState;
using godot::RDSamplerState;
using godot::RDShaderSPIRV;
using godot::RDUniform;
using godot::RDVertexAttribute;
using godot::Ref;
using godot::RenderingDevice;
using godot::RenderingServer;
using godot::ResourceLoader;
using godot::TypedArray;

template <>
struct std::hash<RID>
{
    std::size_t operator()(const RID& rid) const noexcept
    {
        return std::hash<int64_t>{}(rid.get_id());
    }
};

namespace ImGui::Godot {

struct RdRenderer::Impl
{
    RID shader;
    int64_t vtxFormat = 0;
    RID pipeline;
    RID sampler;
    TypedArray<RID> srcBuffers;
    TypedArray<RDUniform> uniforms;
    // TypedArray<RID> storageTextures;
    godot::PackedColorArray clearColors;

    std::unordered_map<RID, RID> framebuffers;
    std::unordered_map<ImTextureID, RID> uniformSets;
    std::unordered_set<ImTextureID> usedTextures;

    godot::PackedInt64Array vtxOffsets;

    RID idxBuffer;
    int idxBufferSize = 0; // size in indices
    RID vtxBuffer;
    int vtxBufferSize = 0; // size in vertices

    void SetupBuffers(ImDrawData* drawData);
    RID GetFramebuffer(RID vprid);

    Impl()
    {
        clearColors.push_back(godot::Color(0, 0, 0, 0));
        srcBuffers.resize(3);
        vtxOffsets.resize(3);
    }
};

RID RdRenderer::Impl::GetFramebuffer(RID vprid)
{
    if (!vprid.is_valid())
        return RID();

    RenderingServer* RS = RenderingServer::get_singleton();
    RenderingDevice* RD = RS->get_rendering_device();
    auto it = framebuffers.find(vprid);
    if (it != framebuffers.end())
    {
        RID fb = it->second;
        if (RD->framebuffer_is_valid(fb))
            return fb;
    }

    RID vptex = RS->texture_get_rd_texture(RS->viewport_get_texture(vprid));
    godot::TypedArray<godot::RID> arr;
    arr.push_back(vptex);
    RID fb = RD->framebuffer_create(arr);
    framebuffers[vprid] = fb;
    return fb;
}

void RdRenderer::Impl::SetupBuffers(ImDrawData* drawData)
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();

    // allocate merged index and vertex buffers
    if (idxBufferSize < drawData->TotalIdxCount)
    {
        if (idxBuffer.get_id() != 0)
            RD->free_rid(idxBuffer);
        idxBuffer = RD->index_buffer_create(drawData->TotalIdxCount, RenderingDevice::INDEX_BUFFER_FORMAT_UINT16);
        idxBufferSize = drawData->TotalIdxCount;
    }

    if (vtxBufferSize < drawData->TotalVtxCount)
    {
        if (vtxBuffer.get_id() != 0)
            RD->free_rid(vtxBuffer);
        vtxBuffer = RD->vertex_buffer_create(drawData->TotalVtxCount * sizeof(ImDrawVert));
        vtxBufferSize = drawData->TotalVtxCount;
    }

    if (drawData->TotalIdxCount == 0)
        return;

    int globalIdxOffset = 0;
    int globalVtxOffset = 0;

    int idxBufSize = drawData->TotalIdxCount * sizeof(ImDrawIdx);
    godot::PackedByteArray idxBuf;
    idxBuf.resize(idxBufSize);

    int vertBufSize = drawData->TotalVtxCount * sizeof(ImDrawVert);
    godot::PackedByteArray vertBuf;
    vertBuf.resize(vertBufSize);

    for (int i = 0; i < drawData->CmdListsCount; ++i)
    {
        ImDrawList* cmdList = drawData->CmdLists[i];

        std::copy(cmdList->IdxBuffer.begin(),
                  cmdList->IdxBuffer.end(),
                  reinterpret_cast<ImDrawIdx*>(idxBuf.ptrw() + globalIdxOffset));
        std::copy(cmdList->VtxBuffer.begin(),
                  cmdList->VtxBuffer.end(),
                  reinterpret_cast<ImDrawVert*>(vertBuf.ptrw() + globalVtxOffset));

        globalIdxOffset += cmdList->IdxBuffer.size_in_bytes();
        globalVtxOffset += cmdList->VtxBuffer.size_in_bytes();

        for (int cmdi = 0; cmdi < cmdList->CmdBuffer.Size; ++cmdi)
        {
            const ImDrawCmd& drawCmd = cmdList->CmdBuffer[cmdi];
            ImTextureID texid = drawCmd.GetTexID();
            if (!texid)
                continue;

            usedTextures.insert(texid);
            if (!uniformSets.contains(texid))
            {
                RID texrid = RenderingServer::get_singleton()->texture_get_rd_texture(make_rid(texid));
                Ref<RDUniform> uniform = memnew(RDUniform);
                uniform->set_binding(0);
                uniform->set_uniform_type(RenderingDevice::UNIFORM_TYPE_SAMPLER_WITH_TEXTURE);
                uniform->add_id(sampler);
                uniform->add_id(texrid);

                uniforms[0] = uniform;

                RID uniformSet = RD->uniform_set_create(uniforms, shader, 0);
                uniformSets[texid] = uniformSet;
            }
        }
    }

    RD->buffer_update(idxBuffer, 0, idxBuf.size(), idxBuf);
    RD->buffer_update(vtxBuffer, 0, vertBuf.size(), vertBuf);
}

RdRenderer::RdRenderer() : impl(std::make_unique<Impl>())
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();
    Ref<RDShaderSPIRV> spirv =
        ResourceLoader::get_singleton()->load("res://addons/imgui-godot-native/ImGuiShaderSPIRV.tres");

    impl->shader = RD->shader_create_from_spirv(spirv);

    TypedArray<RDVertexAttribute> vattrs;

    RDVertexAttribute attr_points;
    attr_points.set_location(0);
    attr_points.set_format(RenderingDevice::DATA_FORMAT_R32G32_SFLOAT);
    attr_points.set_stride(sizeof(ImDrawVert));
    attr_points.set_offset(0);

    RDVertexAttribute attr_uvs;
    attr_uvs.set_location(1);
    attr_uvs.set_format(RenderingDevice::DATA_FORMAT_R32G32_SFLOAT);
    attr_uvs.set_stride(sizeof(ImDrawVert));
    attr_uvs.set_offset(sizeof(float) * 2);

    RDVertexAttribute attr_colors;
    attr_colors.set_location(2);
    attr_colors.set_format(RenderingDevice::DATA_FORMAT_R8G8B8A8_UNORM);
    attr_colors.set_stride(sizeof(ImDrawVert));
    attr_colors.set_offset(sizeof(float) * 4);

    vattrs.append(&attr_points);
    vattrs.append(&attr_uvs);
    vattrs.append(&attr_colors);

    impl->vtxFormat = RD->vertex_format_create(vattrs);

    RDPipelineColorBlendStateAttachment bsa;
    bsa.set_enable_blend(true);
    bsa.set_src_color_blend_factor(RenderingDevice::BLEND_FACTOR_SRC_ALPHA);
    bsa.set_dst_color_blend_factor(RenderingDevice::BLEND_FACTOR_ONE_MINUS_SRC_ALPHA);
    bsa.set_color_blend_op(RenderingDevice::BLEND_OP_ADD);
    bsa.set_src_alpha_blend_factor(RenderingDevice::BLEND_FACTOR_ONE);
    bsa.set_dst_alpha_blend_factor(RenderingDevice::BLEND_FACTOR_ONE_MINUS_SRC_ALPHA);
    bsa.set_alpha_blend_op(RenderingDevice::BLEND_OP_ADD);

    Ref<RDPipelineColorBlendState> blend = memnew(RDPipelineColorBlendState);
    blend->set_blend_constant(Color(0.0f, 0.0f, 0.0f, 0.0f));
    blend->get_attachments().append(&bsa);

    Ref<RDPipelineRasterizationState> raster_state = memnew(RDPipelineRasterizationState);
    raster_state->set_front_face(RenderingDevice::POLYGON_FRONT_FACE_COUNTER_CLOCKWISE);

    impl->pipeline = RD->render_pipeline_create(impl->shader,
                                                RD->screen_get_framebuffer_format(),
                                                impl->vtxFormat,
                                                RenderingDevice::RENDER_PRIMITIVE_TRIANGLES,
                                                raster_state,
                                                {},
                                                {},
                                                blend);

    RDSamplerState sampler_state;
    sampler_state.set_min_filter(RenderingDevice::SAMPLER_FILTER_LINEAR);
    sampler_state.set_mag_filter(RenderingDevice::SAMPLER_FILTER_LINEAR);
    sampler_state.set_mip_filter(RenderingDevice::SAMPLER_FILTER_LINEAR);
    sampler_state.set_repeat_u(RenderingDevice::SAMPLER_REPEAT_MODE_REPEAT);
    sampler_state.set_repeat_v(RenderingDevice::SAMPLER_REPEAT_MODE_REPEAT);
    sampler_state.set_repeat_w(RenderingDevice::SAMPLER_REPEAT_MODE_REPEAT);

    impl->sampler = RD->sampler_create(&sampler_state);

    impl->srcBuffers.resize(3);
    impl->uniforms.resize(1);
}

RdRenderer::~RdRenderer()
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();
    RD->free_rid(impl->shader);
    RD->free_rid(impl->sampler);
    RD->free_rid(impl->idxBuffer);
    RD->free_rid(impl->vtxBuffer);
}

void RdRenderer::RenderDrawData(RID vprid, ImDrawData* drawData)
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();

    impl->SetupBuffers(drawData);

    RID fb = impl->GetFramebuffer(vprid);

    godot::PackedFloat32Array pcfloats;
    pcfloats.resize(4);
    pcfloats[0] = 2.0f / drawData->DisplaySize.x;
    pcfloats[1] = 2.0f / drawData->DisplaySize.y;
    pcfloats[2] = -1.0f - (drawData->DisplayPos.x * pcfloats[0]);
    pcfloats[3] = -1.0f - (drawData->DisplayPos.y * pcfloats[1]);
    godot::PackedByteArray pcbuf = pcfloats.to_byte_array();

    int64_t dl = RD->draw_list_begin(fb,
                                     RenderingDevice::INITIAL_ACTION_CLEAR,
                                     RenderingDevice::FINAL_ACTION_READ,
                                     RenderingDevice::INITIAL_ACTION_CLEAR,
                                     RenderingDevice::FINAL_ACTION_READ,
                                     impl->clearColors);

    RD->draw_list_bind_render_pipeline(dl, impl->pipeline);
    RD->draw_list_set_push_constant(dl, pcbuf, pcbuf.size());

    int globalIdxOffset = 0;
    int globalVtxOffset = 0;
    for (int i = 0; i < drawData->CmdListsCount; ++i)
    {
        ImDrawList* cmdList = drawData->CmdLists[i];

        for (int cmdi = 0; cmdi < cmdList->CmdBuffer.Size; ++cmdi)
        {
            ImDrawCmd& drawCmd = cmdList->CmdBuffer[cmdi];
            if (drawCmd.ElemCount == 0)
                continue;
            if (!impl->uniformSets.contains(drawCmd.GetTexID()))
                continue;

            RID idxArray =
                RD->index_array_create(impl->idxBuffer, drawCmd.IdxOffset + globalIdxOffset, drawCmd.ElemCount);

            int64_t voff = (drawCmd.VtxOffset + globalVtxOffset) * sizeof(ImDrawVert);
            impl->srcBuffers[0] = impl->srcBuffers[1] = impl->srcBuffers[2] = impl->vtxBuffer;
            impl->vtxOffsets[0] = impl->vtxOffsets[1] = impl->vtxOffsets[2] = voff;
            RID vtxArray =
                RD->vertex_array_create(cmdList->VtxBuffer.Size, impl->vtxFormat, impl->srcBuffers, impl->vtxOffsets);

            RD->draw_list_bind_uniform_set(dl, impl->uniformSets[drawCmd.GetTexID()], 0);
            RD->draw_list_bind_index_array(dl, idxArray);
            RD->draw_list_bind_vertex_array(dl, vtxArray);

            godot::Rect2 clipRect = {drawCmd.ClipRect.x,
                                     drawCmd.ClipRect.y,
                                     drawCmd.ClipRect.z - drawCmd.ClipRect.x,
                                     drawCmd.ClipRect.w - drawCmd.ClipRect.y};
            clipRect.position -= godot::Vector2i(drawData->DisplayPos.x, drawData->DisplayPos.y);
            RD->draw_list_enable_scissor(dl, clipRect);

            RD->draw_list_draw(dl, true, 1);

            RD->free_rid(idxArray);
            RD->free_rid(vtxArray);
        }
        globalIdxOffset += cmdList->IdxBuffer.Size;
        globalVtxOffset += cmdList->VtxBuffer.Size;
    }
    RD->draw_list_end();
}

} // namespace ImGui::Godot
