#include "RdRenderer.h"
#include "common.h"
#include <array>
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/rd_attachment_format.hpp>
#include <godot_cpp/classes/rd_pipeline_color_blend_state.hpp>
#include <godot_cpp/classes/rd_pipeline_color_blend_state_attachment.hpp>
#include <godot_cpp/classes/rd_pipeline_depth_stencil_state.hpp>
#include <godot_cpp/classes/rd_pipeline_multisample_state.hpp>
#include <godot_cpp/classes/rd_pipeline_rasterization_state.hpp>
#include <godot_cpp/classes/rd_sampler_state.hpp>
#include <godot_cpp/classes/rd_shader_file.hpp>
#include <godot_cpp/classes/rd_shader_spirv.hpp>
#include <godot_cpp/classes/rd_uniform.hpp>
#include <godot_cpp/classes/rd_vertex_attribute.hpp>
#include <godot_cpp/classes/rendering_device.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <imgui.h>
#include <unordered_map>
#include <unordered_set>

using namespace godot;

namespace ImGui::Godot {

struct RdRenderer::Impl
{
    RID shader;
    int64_t vtxFormat = 0;
    RID pipeline;
    RID sampler;
    TypedArray<RID> srcBuffers;
    TypedArray<RDUniform> uniforms;
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

    Impl()
    {
        clearColors.push_back(godot::Color(0, 0, 0, 0));
        srcBuffers.resize(3);
        vtxOffsets.resize(3);
    }
};

RID RdRenderer::GetFramebuffer(RID vprid)
{
    if (!vprid.is_valid())
        return RID();

    const RenderingServer* RS = RenderingServer::get_singleton();
    RenderingDevice* RD = RS->get_rendering_device();
    auto it = impl->framebuffers.find(vprid);
    if (it != impl->framebuffers.end())
    {
        RID fb = it->second;
        if (RD->framebuffer_is_valid(fb))
            return fb;
    }

    const RID vptex = RS->texture_get_rd_texture(RS->viewport_get_texture(vprid));
    godot::TypedArray<godot::RID> arr;
    arr.push_back(vptex);
    RID fb = RD->framebuffer_create(arr);
    impl->framebuffers[vprid] = fb;
    return fb;
}

void RdRenderer::Impl::SetupBuffers(ImDrawData* drawData)
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();

    int globalIdxOffset = 0;
    int globalVtxOffset = 0;

    const int idxBufSize = drawData->TotalIdxCount * sizeof(ImDrawIdx);
    godot::PackedByteArray idxBuf;
    idxBuf.resize(idxBufSize);

    const int vertBufSize = drawData->TotalVtxCount * sizeof(ImDrawVert);
    godot::PackedByteArray vertBuf;
    vertBuf.resize(vertBufSize);

    for (int i = 0; i < drawData->CmdListsCount; ++i)
    {
        ImDrawList* cmdList = drawData->CmdLists[i];

        std::copy(cmdList->VtxBuffer.begin(),
                  cmdList->VtxBuffer.end(),
                  reinterpret_cast<ImDrawVert*>(vertBuf.ptrw() + globalVtxOffset));
        globalVtxOffset += cmdList->VtxBuffer.size_in_bytes();

        std::copy(cmdList->IdxBuffer.begin(),
                  cmdList->IdxBuffer.end(),
                  reinterpret_cast<ImDrawIdx*>(idxBuf.ptrw() + globalIdxOffset));
        globalIdxOffset += cmdList->IdxBuffer.size_in_bytes();

        // create a uniform set for each texture
        for (int cmdi = 0; cmdi < cmdList->CmdBuffer.Size; ++cmdi)
        {
            const ImDrawCmd& drawCmd = cmdList->CmdBuffer[cmdi];
            const ImTextureID texid = drawCmd.GetTexID();
            if (!texid)
                continue;
            const RID texrid = make_rid(texid);
            if (!RD->texture_is_valid(texrid))
                continue;

            usedTextures.insert(texid);
            if (!uniformSets.contains(texid))
            {
                Ref<RDUniform> uniform;
                uniform.instantiate();
                uniform->set_binding(0);
                uniform->set_uniform_type(RenderingDevice::UNIFORM_TYPE_SAMPLER_WITH_TEXTURE);
                uniform->add_id(sampler);
                uniform->add_id(texrid);

                uniforms[0] = uniform;
                uniformSets[texid] = RD->uniform_set_create(uniforms, shader, 0);
            }
        }
    }

    RD->buffer_update(idxBuffer, 0, idxBuf.size(), idxBuf);
    RD->buffer_update(vtxBuffer, 0, vertBuf.size(), vertBuf);
}

RdRenderer::RdRenderer() : impl(std::make_unique<Impl>())
{
}

bool RdRenderer::Init()
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();
    if (!RD)
        return false;

    // set up everything to match the official Vulkan backend as closely as possible

    Ref<RDShaderFile> shaderFile =
        ResourceLoader::get_singleton()->load("res://addons/imgui-godot/data/ImGuiShader.glsl");

    impl->shader = RD->shader_create_from_spirv(shaderFile->get_spirv());

    if (!impl->shader.is_valid())
        return false;

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

    Ref<RDPipelineColorBlendState> blend;
    blend.instantiate();
    blend->set_blend_constant(Color(0.0f, 0.0f, 0.0f, 0.0f));
    blend->get_attachments().append(&bsa);

    Ref<RDPipelineRasterizationState> raster_state;
    raster_state.instantiate();
    raster_state->set_front_face(RenderingDevice::POLYGON_FRONT_FACE_COUNTER_CLOCKWISE);

    Ref<RDAttachmentFormat> af;
    af.instantiate();
    af->set_format(RenderingDevice::DATA_FORMAT_R8G8B8A8_UNORM);
    af->set_samples(RenderingDevice::TEXTURE_SAMPLES_1);
    af->set_usage_flags(RenderingDevice::TEXTURE_USAGE_COLOR_ATTACHMENT_BIT);

    TypedArray<RDAttachmentFormat> afs;
    afs.push_back(af);

    const int64_t fb_format = RD->framebuffer_format_create(afs);

    impl->pipeline = RD->render_pipeline_create(impl->shader,
                                                fb_format,
                                                impl->vtxFormat,
                                                RenderingDevice::RENDER_PRIMITIVE_TRIANGLES,
                                                raster_state,
                                                {},
                                                {},
                                                blend);

    if (!impl->pipeline.is_valid())
        return false;

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
    return true;
}

void RdRenderer::Render(RID fb, ImDrawData* drawData)
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();

    if (!fb.is_valid())
        return;

    godot::PackedFloat32Array pcfloats;
    pcfloats.resize(4);
    pcfloats[0] = 2.0f / drawData->DisplaySize.x;
    pcfloats[1] = 2.0f / drawData->DisplaySize.y;
    pcfloats[2] = -1.0f - (drawData->DisplayPos.x * pcfloats[0]);
    pcfloats[3] = -1.0f - (drawData->DisplayPos.y * pcfloats[1]);
    godot::PackedByteArray pcbuf = pcfloats.to_byte_array();

    // allocate merged index and vertex buffers
    if (impl->idxBufferSize < drawData->TotalIdxCount)
    {
        if (impl->idxBuffer.get_id() != 0)
            RD->free_rid(impl->idxBuffer);
        impl->idxBuffer = RD->index_buffer_create(drawData->TotalIdxCount, RenderingDevice::INDEX_BUFFER_FORMAT_UINT16);
        impl->idxBufferSize = drawData->TotalIdxCount;
    }

    if (impl->vtxBufferSize < drawData->TotalVtxCount)
    {
        if (impl->vtxBuffer.get_id() != 0)
            RD->free_rid(impl->vtxBuffer);
        impl->vtxBuffer = RD->vertex_buffer_create(drawData->TotalVtxCount * sizeof(ImDrawVert));
        impl->vtxBufferSize = drawData->TotalVtxCount;
    }

    // check if our font texture is still valid
    std::erase_if(impl->uniformSets, [RD](const auto& kv) { return !RD->uniform_set_is_valid(kv.second); });

    if (drawData->CmdListsCount > 0)
        impl->SetupBuffers(drawData);

    // draw
    const int64_t dl = RD->draw_list_begin(fb,
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
            const ImDrawCmd& drawCmd = cmdList->CmdBuffer[cmdi];
            if (drawCmd.ElemCount == 0)
                continue;
            if (!impl->uniformSets.contains(drawCmd.GetTexID()))
                continue;

            const RID idxArray =
                RD->index_array_create(impl->idxBuffer, drawCmd.IdxOffset + globalIdxOffset, drawCmd.ElemCount);

            const int64_t voff = (drawCmd.VtxOffset + globalVtxOffset) * sizeof(ImDrawVert);
            impl->srcBuffers[0] = impl->srcBuffers[1] = impl->srcBuffers[2] = impl->vtxBuffer;
            impl->vtxOffsets[0] = impl->vtxOffsets[1] = impl->vtxOffsets[2] = voff;
            const RID vtxArray =
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

RdRenderer::~RdRenderer()
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();
    RD->free_rid(impl->shader);
    RD->free_rid(impl->sampler);
    if (impl->idxBuffer.is_valid())
        RD->free_rid(impl->idxBuffer);
    if (impl->vtxBuffer.is_valid())
        RD->free_rid(impl->vtxBuffer);
}

void RdRenderer::InitViewport(RID vprid)
{
    RenderingServer::get_singleton()->viewport_set_clear_mode(vprid, RenderingServer::VIEWPORT_CLEAR_NEVER);
}

void RdRenderer::FreeUnusedTextures()
{
    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();

    // clean up unused textures
    std::vector<ImTextureID> keys;
    keys.reserve(impl->uniformSets.size());
    for (const auto& kv : impl->uniformSets)
        keys.push_back(kv.first);

    for (ImTextureID texid : keys)
    {
        if (!impl->usedTextures.contains(texid))
        {
            RD->free_rid(impl->uniformSets[texid]);
            impl->uniformSets.erase(texid);
        }
    }
    impl->usedTextures.clear();
}

void RdRenderer::Render()
{
    auto& pio = ImGui::GetPlatformIO();
    for (ImGuiViewport* vp : pio.Viewports)
    {
        if (!(vp->Flags & ImGuiViewportFlags_IsMinimized))
        {
            ReplaceTextureRIDs(vp->DrawData);
            const RID vprid = make_rid(vp->RendererUserData);
            Render(GetFramebuffer(vprid), vp->DrawData);
        }
    }
    FreeUnusedTextures();
}

void RdRenderer::ReplaceTextureRIDs(ImDrawData* drawData)
{
    const RenderingServer* RS = RenderingServer::get_singleton();
    for (int i = 0; i < drawData->CmdListsCount; ++i)
    {
        ImDrawList* cmdList = drawData->CmdLists[i];
        for (ImDrawCmd& drawCmd : cmdList->CmdBuffer)
        {
            drawCmd.TextureId = (ImTextureID)RS->texture_get_rd_texture(make_rid(drawCmd.TextureId)).get_id();
        }
    }
}

} // namespace ImGui::Godot
