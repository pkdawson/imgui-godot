#include "ImGuiLayer.h"
#include "Context.h"
#include "ImGuiLayerHelper.h"

#pragma warning(push, 0)
#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/gd_script.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/packed_scene.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

#include <imgui.h>
#include <imgui_internal.h>

using namespace godot;

namespace ImGui::Godot {

struct ImGuiLayer::Impl
{
    ImGuiLayerHelper* helper = nullptr;
    Window* window = nullptr;
    RID subViewportRid;
    Vector2i subViewportSize{0, 0};
    RID canvasItem;
    Transform2D finalTransform{1.f, 0.f, 0.f, 1.f, 0.f, 0.f}; // identity
    bool visible = true;
    Ref<Resource> cfg;

    static RID AddLayerSubViewport(Node* parent);
    void CheckContentScale();
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

void ImGuiLayer::Impl::CheckContentScale()
{
    if (window->get_content_scale_mode() == Window::CONTENT_SCALE_MODE_VIEWPORT)
    {
        UtilityFunctions::printerr("imgui-godot: scale mode `viewport` is unsupported");
    }
}

ImGuiLayer::ImGuiLayer() : impl(std::make_unique<Impl>())
{
}

ImGuiLayer::~ImGuiLayer()
{
}

void ImGuiLayer::_bind_methods()
{
    ADD_SIGNAL(MethodInfo("imgui_layout"));
    ClassDB::bind_method(D_METHOD("on_visibility_changed"), &ImGuiLayer::on_visibility_changed);
    ClassDB::bind_method(D_METHOD("on_frame_pre_draw"), &ImGuiLayer::on_frame_pre_draw);
}

void ImGuiLayer::_enter_tree()
{
    Node* parent = get_parent();
    if (!parent)
        return;
    // if (parent->get_class() != "ImGuiRoot")
    //     return;
    if (Engine::get_singleton()->has_singleton("ImGuiLayer"))
        return;

    set_name("ImGuiLayer");
    Engine::get_singleton()->register_singleton("ImGuiLayer", this);
    impl->window = get_window();

    impl->CheckContentScale();

    RenderingServer* RS = RenderingServer::get_singleton();

    impl->subViewportRid = Impl::AddLayerSubViewport(this);
    impl->canvasItem = RS->canvas_item_create();
    RS->canvas_item_set_parent(impl->canvasItem, get_canvas());

    Ref<PackedScene> cfgPackedScene = ResourceLoader::get_singleton()->load("res://addons/imgui-godot/Config.tscn");
    Node* cfgScene = cfgPackedScene->instantiate();
    Ref<Resource> cfg = cfgScene->get("Config");
    memdelete(cfgScene);

    if (cfg.is_null())
    {
        Ref<GDScript> script = ResourceLoader::get_singleton()->load("res://addons/imgui-godot/scripts/ImGuiConfig.gd");
        cfg = script->new_();
    }
    impl->cfg = cfg;

    set_layer(cfg->get("Layer"));

    impl->helper = memnew(ImGuiLayerHelper);
    add_child(impl->helper);

#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    ImGui::Godot::Init(get_window(), impl->subViewportRid, cfg);
}

void ImGuiLayer::_ready()
{
    set_process_priority(std::numeric_limits<int32_t>::max());
    set_physics_process(false);

#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
    {
        set_visible(false);
        set_process(false);
        impl->helper->set_process(false);
        set_process_input(false);
    }
#endif

    connect("visibility_changed", Callable(this, "on_visibility_changed"));
}

void ImGuiLayer::_exit_tree()
{
    Engine::get_singleton()->unregister_singleton("ImGuiLayer");
    ImGui::Godot::Shutdown();
    RenderingServer::get_singleton()->free_rid(impl->canvasItem);
    RenderingServer::get_singleton()->free_rid(impl->subViewportRid);
}

void ImGuiLayer::_process(double delta)
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
    {
        // verify signal connections
        auto conns = get_signal_connection_list("imgui_layout");
        for (int i = 0; i < conns.size(); ++i)
        {
            const Dictionary& conn = conns[i];
            const Callable& cb = conn["callable"];
            if (!cb.is_valid())
                disconnect("imgui_layout", cb);
        }
    }
#endif

    if (impl->visible)
    {
        Vector2i winSize = impl->window->get_size();
        Transform2D ft = impl->window->get_final_transform();

        if (impl->subViewportSize != winSize || impl->finalTransform != ft)
        {
            // this is more or less how SubViewportContainer works
            impl->subViewportSize = winSize;
            impl->finalTransform = ft;

            RenderingServer* RS = RenderingServer::get_singleton();
            RS->viewport_set_size(impl->subViewportRid, impl->subViewportSize.x, impl->subViewportSize.y);
            RID vptex = RS->viewport_get_texture(impl->subViewportRid);
            RS->canvas_item_clear(impl->canvasItem);
            RS->canvas_item_set_transform(impl->canvasItem, ft.affine_inverse());
            RS->canvas_item_add_texture_rect(impl->canvasItem,
                                             Rect2(0, 0, impl->subViewportSize.x, impl->subViewportSize.y),
                                             vptex);
        }

        emit_signal("imgui_layout");
    }

    ImGui::Godot::Render();
}

void ImGuiLayer::_input(const Ref<InputEvent>& event)
{
    if (ImGui::Godot::ProcessInput(event, impl->window))
    {
        impl->window->set_input_as_handled();
    }
}

void ImGuiLayer::_notification(int p_what)
{
    // quick filter
    if (p_what > 2000)
    {
        ImGui::Godot::ProcessNotification(p_what);
    }
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

void ImGuiLayer::on_frame_pre_draw()
{
    ImGui::Godot::OnFramePreDraw();
}

void ImGuiLayer::ToolInit()
{
    if (!is_visible())
    {
        ImGui::Godot::Init(get_window(), impl->canvasItem, impl->cfg);
        set_visible(true);
    }
}

} // namespace ImGui::Godot
