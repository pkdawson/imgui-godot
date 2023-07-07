#include "ImGuiLayer.h"
#include "ImGuiLayerHelper.h"
#include "imgui-godot.h"

#pragma warning(push, 0)
#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/gd_script.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

#include <imgui.h>
using namespace godot;

namespace ImGui::Godot {

struct ImGuiLayer::Impl
{
    ImGuiLayerHelper* helper = nullptr;
    Window* window = nullptr;
    RID canvasItem;
};

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
}

void ImGuiLayer::_enter_tree()
{
    Node* parent = get_parent();
    if (!parent)
        return;
    if (parent->get_class() != "ImGuiRoot")
        return;
    if (Engine::get_singleton()->has_singleton("ImGuiLayer"))
        return;

    set_name("ImGuiLayer");
    Engine::get_singleton()->register_singleton("ImGuiLayer", this);
    impl->window = get_window();

    Ref<Resource> cfg = parent->get("Config");
    if (cfg.is_null())
    {
        Ref<GDScript> script = ResourceLoader::get_singleton()->load("res://addons/imgui-godot/scripts/ImGuiConfig.gd");
        cfg = script->new_();
    }

    set_layer(cfg->get("Layer"));

    RenderingServer* RS = RenderingServer::get_singleton();
    impl->canvasItem = RS->canvas_item_create();
    RS->canvas_item_set_parent(impl->canvasItem, get_canvas());

    ImGui::Godot::Init(get_window(), impl->canvasItem, cfg);

    impl->helper = memnew(ImGuiLayerHelper);
    add_child(impl->helper);

    connect("visibility_changed", Callable(this, "on_visibility_changed"));
}

void ImGuiLayer::_ready()
{
    set_process_priority(std::numeric_limits<int32_t>::max());
    set_process(false);

#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
    {
        // skip a frame so tool scripts don't start on exactly frame 1
        ImGui::NewFrame();
        ImGui::Render();
    }
#endif

    set_process(true);
}

void ImGuiLayer::_exit_tree()
{
    Engine::get_singleton()->unregister_singleton("ImGuiLayer");
    ImGui::Godot::Shutdown();
    RenderingServer::get_singleton()->free_rid(impl->canvasItem);
    // impl->helper->queue_free();
}

void ImGuiLayer::_process(double delta)
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    emit_signal("imgui_layout");

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
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    if (is_visible())
    {
        set_process(true);
        impl->helper->set_process(true);
    }
    else
    {
        set_process(false);
        impl->helper->set_process(false);
        RenderingServer::get_singleton()->canvas_item_clear(impl->canvasItem);
    }
}

} // namespace ImGui::Godot
