#include "ImGuiLayerHelper.h"
#include "Context.h"
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
    Window* window;
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
    set_name("ImGuiLayerHelper");
    set_process_priority(std::numeric_limits<int32_t>::min());
    set_process_mode(Node::PROCESS_MODE_ALWAYS);
    impl->window = get_window();
}

void ImGuiLayerHelper::_exit_tree()
{
}

void ImGuiLayerHelper::_process(double delta)
{
    ImGui::Godot::Update(delta, impl->window->get_size());
}

} // namespace ImGui::Godot
