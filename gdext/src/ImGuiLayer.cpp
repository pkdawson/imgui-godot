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
#include <imgui_internal.h>

using namespace godot;

namespace ImGui::Godot {

struct ImGuiLayer::Impl
{
    ImGuiLayerHelper* helper = nullptr;
    Window* window = nullptr;
    RID canvasItem;
    bool wantHide = false;
    Ref<Resource> cfg;
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
    impl->cfg = cfg;

    set_layer(cfg->get("Layer"));

    RenderingServer* RS = RenderingServer::get_singleton();
    impl->canvasItem = RS->canvas_item_create();
    RS->canvas_item_set_parent(impl->canvasItem, get_canvas());

    impl->helper = memnew(ImGuiLayerHelper);
    add_child(impl->helper);

#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    ImGui::Godot::Init(get_window(), impl->canvasItem, cfg);
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
    }
#endif

    connect("visibility_changed", Callable(this, "on_visibility_changed"));
}

void ImGuiLayer::_exit_tree()
{
    Engine::get_singleton()->unregister_singleton("ImGuiLayer");
    ImGui::Godot::Shutdown();
    RenderingServer::get_singleton()->free_rid(impl->canvasItem);
}

void ImGuiLayer::_process(double delta)
{
    emit_signal("imgui_layout");
    ImGui::Godot::Render();

    if (impl->wantHide)
    {
        impl->wantHide = false;
        set_process(false);
        set_physics_process(true);
        impl->helper->set_process(false);
        set_process_input(false);
        RenderingServer::get_singleton()->canvas_item_clear(impl->canvasItem);

        // hide viewport windows immediately
        if (ImGui::GetIO().ConfigFlags & ImGuiConfigFlags_ViewportsEnable)
        {
            ImGui::NewFrame();
            ImGui::EndFrame();
            ImGui::UpdatePlatformWindows();
        }
        ImGui::NewFrame();
    }
}

void ImGuiLayer::_physics_process(double delta)
{
    static int count = 0;
    if (++count > 60)
    {
        count = 0;
        ImGui::EndFrame();
        if (ImGui::GetIO().ConfigFlags & ImGuiConfigFlags_ViewportsEnable)
        {
            ImGui::UpdatePlatformWindows();
        }
        ImGui::NewFrame();
    }
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
    if (is_visible())
    {
        set_process(true);
        set_physics_process(false);
        impl->helper->set_process(true);
        set_process_input(true);
    }
    else
    {
        impl->wantHide = true;
    }
}

void ImGuiLayer::NewFrame(double delta)
{
    if (ImGui::GetCurrentContext()->WithinFrameScope)
    {
        ImGui::EndFrame();
        if (ImGui::GetIO().ConfigFlags & ImGuiConfigFlags_ViewportsEnable)
        {
            ImGui::UpdatePlatformWindows();
        }
    }
    ImGui::Godot::Update(delta);
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
