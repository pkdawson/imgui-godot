#include "ImGuiLayer.h"
#include "ImGuiGodotHelper.h"
#include "imgui-godot.h"

#pragma warning(push, 0)
#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

#include <imgui.h>
using namespace godot;

namespace ImGui::Godot {

struct ImGuiLayer::Impl
{
    bool show_imgui_demo = true;
    ImGuiGodotHelper* helper = nullptr;
    CanvasLayer* layer = nullptr;
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

    Engine::get_singleton()->register_singleton("ImGuiLayer", this);
    impl->window = get_window();
    impl->layer = memnew(CanvasLayer);
    add_child(impl->layer);
    impl->layer->set_layer(128);

    RenderingServer* RS = RenderingServer::get_singleton();
    impl->canvasItem = RS->canvas_item_create();
    RS->canvas_item_set_parent(impl->canvasItem, impl->layer->get_canvas());

    ImGui::Godot::Init(get_window(), impl->canvasItem);

    impl->helper = memnew(ImGuiGodotHelper);
    add_child(impl->helper);
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

    if (impl->show_imgui_demo)
        ImGui::ShowDemoWindow(&impl->show_imgui_demo);

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

} // namespace ImGui::Godot
