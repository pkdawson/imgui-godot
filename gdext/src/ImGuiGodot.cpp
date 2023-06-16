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
    ImGuiGodot_Init(get_window());

    impl->helper = memnew(ImGuiGodotHelper);
    add_child(impl->helper);
}

void ImGuiGodot::_ready()
{
    set_process(false);
    set_process_priority(std::numeric_limits<int32_t>::min());

#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    set_process(true);
    impl->helper->set_process_priority(std::numeric_limits<int32_t>::max());
}

void ImGuiGodot::_exit_tree()
{
    ImGuiGodot_Shutdown();
}

void ImGuiGodot::_process(double delta)
{
    ImGuiGodot_Update(delta);

    emit_signal("imgui_layout");

    if (impl->show_imgui_demo)
        ImGui::ShowDemoWindow(&impl->show_imgui_demo);

    ImGui::Begin("Cpp Window");
    ImGui::Text("hello from C++");
    ImGui::End();
}

} // namespace ImGui::Godot
