#include "ImGuiLayerHelper.h"
#include "imgui-godot.h"

#pragma warning(push, 0)
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

#include <imgui.h>
using namespace godot;

namespace ImGui::Godot {

struct ImGuiLayerHelper::Impl
{
};

ImGuiLayerHelper::ImGuiLayerHelper() : impl(std::make_unique<Impl>())
{
}

ImGuiLayerHelper::~ImGuiLayerHelper()
{
}

void ImGuiLayerHelper::_bind_methods()
{
}

void ImGuiLayerHelper::_enter_tree()
{
}

void ImGuiLayerHelper::_ready()
{
    set_process_priority(std::numeric_limits<int32_t>::min());
    set_process(false);

    // #ifdef DEBUG_ENABLED
    //     if (Engine::get_singleton()->is_editor_hint())
    //         return;
    // #endif

    set_process(true);
}

void ImGuiLayerHelper::_exit_tree()
{
}

void ImGuiLayerHelper::_process(double delta)
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    ImGui::Godot::Update(delta);
}

} // namespace ImGui::Godot
