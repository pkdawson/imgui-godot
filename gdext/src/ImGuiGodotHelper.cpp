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

struct ImGuiGodotHelper::Impl
{
};

ImGuiGodotHelper::ImGuiGodotHelper() : impl(std::make_unique<Impl>())
{
}

ImGuiGodotHelper::~ImGuiGodotHelper()
{
}

void ImGuiGodotHelper::_bind_methods()
{
}

void ImGuiGodotHelper::_enter_tree()
{
}

void ImGuiGodotHelper::_ready()
{
    set_process_mode(PROCESS_MODE_DISABLED);
    set_process_priority(std::numeric_limits<int32_t>::min());

#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    set_process_mode(PROCESS_MODE_ALWAYS);
}

void ImGuiGodotHelper::_exit_tree()
{
}

void ImGuiGodotHelper::_process(double delta)
{
    ImGuiGodot_Update(delta);
}

} // namespace ImGui::Godot