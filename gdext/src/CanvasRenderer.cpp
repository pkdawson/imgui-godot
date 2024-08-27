#include "CanvasRenderer.h"
#include "common.h"
#include <godot_cpp/classes/rendering_server.hpp>
#include <unordered_map>
#include <vector>

using namespace godot;

namespace ImGui::Godot {

struct CanvasRenderer::Impl
{
    struct ViewportData
    {
        RID canvas;
        RID rootCanvasItem;
    };

    std::unordered_map<RID, std::vector<RID>> canvasItemPools;
    std::unordered_map<RID, ViewportData> vpData;

    PackedVector2Array vertices;
    PackedColorArray colors;
    PackedVector2Array uvs;
    PackedInt32Array indices;

    void RenderOne(RID vprid, ImDrawData* drawData);
    void ClearCanvasItems(RID rootci);
    void ClearCanvasItems();
};

void CanvasRenderer::Impl::RenderOne(RID vprid, ImDrawData* drawData)
{
    RenderingServer* RS = RenderingServer::get_singleton();
    ViewportData& vd = vpData.find(vprid)->second;
    RID parent = vd.rootCanvasItem;

    if (!canvasItemPools.contains(parent))
        canvasItemPools[parent] = {};

    std::vector<RID>& children = canvasItemPools[parent];

    // allocate our CanvasItem pool as needed
    int neededNodes = 0;
    for (ImDrawList* drawList : drawData->CmdLists)
    {
        const auto& cmdBuf = drawList->CmdBuffer;
        neededNodes += cmdBuf.size();
        for (const ImDrawCmd& cmd : cmdBuf)
        {
            if (cmd.ElemCount == 0)
                --neededNodes;
        }
    }

    while (children.size() < neededNodes)
    {
        RID newChild = RS->canvas_item_create();
        RS->canvas_item_set_parent(newChild, parent);
        RS->canvas_item_set_draw_index(newChild, children.size());
        children.push_back(newChild);
    }

    // trim unused nodes
    while (children.size() > neededNodes)
    {
        RS->free_rid(children.back());
        children.pop_back();
    }

    // render
    drawData->ScaleClipRects(ImGui::GetIO().DisplayFramebufferScale);
    int nodeN = 0;

    for (ImDrawList* cmdList : drawData->CmdLists)
    {
        int nVert = cmdList->VtxBuffer.size();

        vertices.resize(nVert);
        colors.resize(nVert);
        uvs.resize(nVert);

        for (int i = 0; i < cmdList->VtxBuffer.size(); ++i)
        {
            const ImDrawVert& v = cmdList->VtxBuffer[i];
            vertices[i] = Vector2(v.pos.x, v.pos.y);
            // need to reverse the color bytes
            uint32_t rgba = v.col;
            float r = (rgba & 0xFF) / 255.f;
            rgba >>= 8;
            float g = (rgba & 0xFF) / 255.f;
            rgba >>= 8;
            float b = (rgba & 0xFF) / 255.f;
            rgba >>= 8;
            float a = (rgba & 0xFF) / 255.f;
            colors[i] = Color(r, g, b, a);
            uvs[i] = Vector2(v.uv.x, v.uv.y);
        }

        for (const ImDrawCmd& drawCmd : cmdList->CmdBuffer)
        {
            if (drawCmd.ElemCount == 0)
                continue;

            indices.resize(drawCmd.ElemCount);
            uint32_t idxOffset = drawCmd.IdxOffset;
            for (uint32_t i = idxOffset, j = 0; i < idxOffset + drawCmd.ElemCount; ++i, ++j)
            {
                indices[j] = cmdList->IdxBuffer[i];
            }

            PackedVector2Array cmdvertices = vertices;
            PackedColorArray cmdcolors = colors;
            PackedVector2Array cmduvs = uvs;
            if (drawCmd.VtxOffset > 0)
            {
                // slow implementation of RendererHasVtxOffset
                const int sliceEnd = cmdList->VtxBuffer.size();
                cmdvertices = vertices.slice(drawCmd.VtxOffset, sliceEnd);
                cmdcolors = colors.slice(drawCmd.VtxOffset, sliceEnd);
                cmduvs = uvs.slice(drawCmd.VtxOffset, sliceEnd);
            }

            RID child = children[nodeN++];

            RID texrid = make_rid(drawCmd.GetTexID());
            RS->canvas_item_clear(child);
            Transform2D xform(1.f, 0.f, 0.f, 1.f, 0.f, 0.f); // identity
            if (drawData->DisplayPos.x != 0.f || drawData->DisplayPos.y != 0.f)
            {
                xform = xform.translated(drawData->DisplayPos).inverse();
            }
            RS->canvas_item_set_transform(child, xform);
            RS->canvas_item_set_clip(child, true);
            RS->canvas_item_set_custom_rect(child,
                                            true,
                                            Rect2(drawCmd.ClipRect.x,
                                                  drawCmd.ClipRect.y,
                                                  drawCmd.ClipRect.z - drawCmd.ClipRect.x,
                                                  drawCmd.ClipRect.w - drawCmd.ClipRect.y));

            RS->canvas_item_add_triangle_array(child, indices, cmdvertices, cmdcolors, cmduvs, {}, {}, texrid);
        }
    }
}

void CanvasRenderer::Impl::ClearCanvasItems(RID rootci)
{
    auto it = canvasItemPools.find(rootci);
    if (it == canvasItemPools.end())
        return;

    RenderingServer* RS = RenderingServer::get_singleton();
    for (RID ci : it->second)
    {
        RS->free_rid(ci);
    }
}

void CanvasRenderer::Impl::ClearCanvasItems()
{
    for (const auto& kv : canvasItemPools)
    {
        ClearCanvasItems(kv.first);
    }
    canvasItemPools.clear();
}

CanvasRenderer::CanvasRenderer() : impl(std::make_unique<Impl>())
{
}

CanvasRenderer::~CanvasRenderer()
{
    RenderingServer* RS = RenderingServer::get_singleton();
    impl->ClearCanvasItems();
    for (const auto& kv : impl->vpData)
    {
        const auto& vd = kv.second;
        RS->free_rid(vd.rootCanvasItem);
        RS->free_rid(vd.canvas);
    }
}

bool CanvasRenderer::Init()
{
    return true;
}

void CanvasRenderer::InitViewport(RID vprid)
{
    RenderingServer* RS = RenderingServer::get_singleton();
    RID canvas = RS->canvas_create();
    RID canvasItem = RS->canvas_item_create();
    RS->viewport_attach_canvas(vprid, canvas);
    RS->canvas_item_set_parent(canvasItem, canvas);

    impl->vpData[vprid] = {canvas, canvasItem};
}

void CanvasRenderer::CloseViewport(RID vprid)
{
    auto it = impl->vpData.find(vprid);
    if (it == impl->vpData.end())
        return;

    RenderingServer* RS = RenderingServer::get_singleton();
    Impl::ViewportData& vd = it->second;
    impl->ClearCanvasItems(vd.rootCanvasItem);

    RS->free_rid(vd.rootCanvasItem);
    RS->free_rid(vd.canvas);
}

void CanvasRenderer::Render()
{
    auto& pio = ImGui::GetPlatformIO();
    for (ImGuiViewport* vp : pio.Viewports)
    {
        RID vprid = make_rid(vp->RendererUserData);
        impl->RenderOne(vprid, vp->DrawData);
    }
}

void CanvasRenderer::OnHide()
{
    impl->ClearCanvasItems();
}

} // namespace ImGui::Godot
