#include "ImGuiLayer.h"
#include "Context.h"
#include "ImGuiControllerHelper.h"

#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/gd_script.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/packed_scene.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <imgui.h>
#include <imgui_internal.h>

using namespace godot;

namespace ImGui::Godot {

struct ImGuiLayer::Impl
{
    RID subViewportRid;
    Vector2i subViewportSize{0, 0};
    RID canvasItem;
    Transform2D finalTransform{1.f, 0.f, 0.f, 1.f, 0.f, 0.f}; // identity
    bool visible = true;
    Viewport* parentViewport = nullptr;

    static RID AddLayerSubViewport(Node* parent);
};

RID ImGuiLayer::Impl::AddLayerSubViewport(Node* parent)
{
    RenderingServer* RS = RenderingServer::get_singleton();
    RID svp = RS->viewport_create();
    RS->viewport_set_transparent_background(svp, true);
    RS->viewport_set_update_mode(svp, RenderingServer::VIEWPORT_UPDATE_ALWAYS);
    RS->viewport_set_clear_mode(svp, RenderingServer::VIEWPORT_CLEAR_ALWAYS);
    RS->viewport_set_active(svp, true);
    RS->viewport_set_parent_viewport(svp, parent->get_window()->get_viewport_rid());
    return svp;
}

ImGuiLayer::ImGuiLayer() : impl(std::make_unique<Impl>())
{
}

ImGuiLayer::~ImGuiLayer()
{
}

void ImGuiLayer::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("on_visibility_changed"), &ImGuiLayer::on_visibility_changed);
}

void ImGuiLayer::_enter_tree()
{
    Context* ctx = GetContext();
    set_name("ImGuiLayer");
    set_layer(ctx->layerNum);

    impl->parentViewport = get_viewport();
    impl->subViewportRid = Impl::AddLayerSubViewport(this);

    RenderingServer* RS = RenderingServer::get_singleton();
    impl->canvasItem = RS->canvas_item_create();
    RS->canvas_item_set_parent(impl->canvasItem, get_canvas());

    ctx->renderer->InitViewport(impl->subViewportRid);
    ctx->viewports->SetMainWindow(get_window(), impl->subViewportRid);
}

void ImGuiLayer::_ready()
{
    connect("visibility_changed", Callable(this, "on_visibility_changed"));
}

void ImGuiLayer::_exit_tree()
{
    RenderingServer::get_singleton()->free_rid(impl->canvasItem);
    RenderingServer::get_singleton()->free_rid(impl->subViewportRid);
}

void ImGuiLayer::on_visibility_changed()
{
    impl->visible = is_visible();
    if (impl->visible)
    {
        set_process_input(true);
    }
    else
    {
        set_process_input(false);
        ImGui::Godot::GetContext()->renderer->OnHide();
        impl->subViewportSize = {0, 0};
        RenderingServer::get_singleton()->canvas_item_clear(impl->canvasItem);
    }
}

void ImGuiLayer::_input(const Ref<InputEvent>& event)
{
    if (GetContext()->input->ProcessInput(event))
    {
        impl->parentViewport->set_input_as_handled();
    }
}

Vector2i ImGuiLayer::UpdateViewport()
{
    Vector2i vpSize;
    if (Window* w = Object::cast_to<Window>(impl->parentViewport))
        vpSize = w->get_size();
    else
        vpSize = Object::cast_to<SubViewport>(impl->parentViewport)->get_size();

    if (impl->visible)
    {
        const Transform2D ft = impl->parentViewport->get_final_transform();

        if (impl->subViewportSize != vpSize ||
            impl->finalTransform != ft
#ifdef DEBUG_ENABLED
            // force redraw on every frame in editor
            || Engine::get_singleton()->is_editor_hint()
#endif
        )
        {
            // this is more or less how SubViewportContainer works
            impl->subViewportSize = vpSize;
            impl->finalTransform = ft;

            RenderingServer* RS = RenderingServer::get_singleton();
            RS->viewport_set_size(impl->subViewportRid, impl->subViewportSize.x, impl->subViewportSize.y);
            const RID vptex = RS->viewport_get_texture(impl->subViewportRid);
            RS->canvas_item_clear(impl->canvasItem);
            RS->canvas_item_set_transform(impl->canvasItem, ft.affine_inverse());
            RS->canvas_item_add_texture_rect(impl->canvasItem,
                                             Rect2(0, 0, impl->subViewportSize.x, impl->subViewportSize.y),
                                             vptex);
        }
    }

    return vpSize;
}

} // namespace ImGui::Godot
