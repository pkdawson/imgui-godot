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
    UtilityFunctions::print("ImGuiLayer()");
}

ImGuiLayer::~ImGuiLayer()
{
    UtilityFunctions::print("~ImGuiLayer()");
}

void ImGuiLayer::_bind_methods()
{
    ADD_SIGNAL(MethodInfo("imgui_layout"));

    ClassDB::bind_method(D_METHOD("GetImGuiPtrs", "version", "ioSize", "vertSize", "idxSize"),
                         &ImGuiLayer::GetImGuiPtrs);
}

void ImGuiLayer::_enter_tree()
{
    UtilityFunctions::print("igl ET");
    Node* parent = get_parent();
    UtilityFunctions::print("parent = ", parent);
    if (!parent)
        return;
    UtilityFunctions::print("parent class = ", parent->get_class());
    if (parent->get_class() != "Window")
        return;
    UtilityFunctions::print(Engine::get_singleton()->has_singleton("ImGuiLayer"));
    if (Engine::get_singleton()->has_singleton("ImGuiLayer"))
        return;

    Engine::get_singleton()->register_singleton("ImGuiLayer", this);
    UtilityFunctions::print(Engine::get_singleton()->has_singleton("ImGuiLayer"));
    impl->window = get_window();
    impl->layer = memnew(CanvasLayer);
    add_child(impl->layer);
    impl->layer->set_layer(128);

    RenderingServer* RS = RenderingServer::get_singleton();
    impl->canvasItem = RS->canvas_item_create();
    RS->canvas_item_set_parent(impl->canvasItem, impl->layer->get_canvas());

    ImGui::Godot::Init(get_window(), impl->canvasItem);

    UtilityFunctions::print("add igh");
    impl->helper = memnew(ImGuiGodotHelper);
    add_child(impl->helper);
}

void ImGuiLayer::_ready()
{
    set_process_priority(std::numeric_limits<int32_t>::max());
    set_process(false);

#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    set_process(true);
}

void ImGuiLayer::_exit_tree()
{
    UtilityFunctions::print("exit tree");
    Engine::get_singleton()->unregister_singleton("ImGuiLayer");
    ImGui::Godot::Shutdown();
    RenderingServer::get_singleton()->free_rid(impl->canvasItem);
    //impl->helper->queue_free();
}

void ImGuiLayer::_process(double delta)
{
    emit_signal("imgui_layout");

    if (impl->show_imgui_demo)
        ImGui::ShowDemoWindow(&impl->show_imgui_demo);

    // ImGui::Begin("Cpp Window");
    // ImGui::Text("hello from C++");
    // ImGui::End();

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

PackedInt64Array ImGuiLayer::GetImGuiPtrs(String version, int ioSize, int vertSize, int idxSize)
{
    if (version != String(ImGui::GetVersion()) || ioSize != sizeof(ImGuiIO) || vertSize != sizeof(ImDrawVert) ||
        idxSize != sizeof(ImDrawIdx))
    {
        UtilityFunctions::printerr("ImGui version mismatch, use ", ImGui::GetVersion(), "-docking");
        return {};
    }

    ImGuiMemAllocFunc alloc_func = nullptr;
    ImGuiMemFreeFunc free_func = nullptr;
    void* user_data = nullptr;

    ImGui::GetAllocatorFunctions(&alloc_func, &free_func, &user_data);

    PackedInt64Array rv;
    rv.resize(3);
    rv[0] = reinterpret_cast<int64_t>(alloc_func);
    rv[1] = reinterpret_cast<int64_t>(free_func);
    rv[2] = reinterpret_cast<int64_t>(ImGui::GetCurrentContext());
    return rv;
}

} // namespace ImGui::Godot
