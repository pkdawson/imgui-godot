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
};

CanvasRenderer::CanvasRenderer()
{
}

CanvasRenderer::~CanvasRenderer()
{
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
}

void CanvasRenderer::Render()
{
}

void CanvasRenderer::OnHide()
{
}

} // namespace ImGui::Godot
