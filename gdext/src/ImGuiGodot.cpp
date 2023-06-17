#include "ImGuiGodot.h"
#include "ImGuiGodotHelper.h"
#include "ImGuiGD.h"

#pragma warning(push, 0)
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

#include <imgui.h>
using namespace godot;

namespace ImGui::Godot {

struct ImGuiGodot::Impl
{
    bool show_imgui_demo = true;
    ImGuiGodotHelper* helper = nullptr;
    CanvasLayer* layer = nullptr;
};

ImGuiGodot::ImGuiGodot() : impl(std::make_unique<Impl>())
{
}

ImGuiGodot::~ImGuiGodot()
{
}

void ImGuiGodot::_bind_methods()
{
    ADD_SIGNAL(MethodInfo("imgui_layout"));
}

void ImGuiGodot::_enter_tree()
{
    impl->layer = memnew(CanvasLayer);
    add_child(impl->layer);
    impl->layer->set_layer(128);

    ImGuiGodot_Init(get_window(), impl->layer);

    impl->helper = memnew(ImGuiGodotHelper);
    add_child(impl->helper);
}

void ImGuiGodot::_ready()
{
    set_process_mode(PROCESS_MODE_DISABLED);
    set_process_priority(std::numeric_limits<int32_t>::max());

#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    set_process_mode(PROCESS_MODE_ALWAYS);
}

void ImGuiGodot::_exit_tree()
{
    ImGuiGodot_Shutdown();
}

void ImGuiGodot::_process(double delta)
{
    emit_signal("imgui_layout");

    //if (impl->show_imgui_demo)
    //    ImGui::ShowDemoWindow(&impl->show_imgui_demo);

    //ImGui::Begin("Cpp Window");
    //ImGui::Text("hello from C++");
    //ImGui::End();

    ImGuiGodot_Render();
}

} // namespace ImGui::Godot
