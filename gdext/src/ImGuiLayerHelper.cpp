#include "ImGuiLayerHelper.h"
#include "ImGuiLayer.h"
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
    ImGuiLayer* igl = nullptr;
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
    impl->igl = Object::cast_to<ImGuiLayer>(get_parent());
}

void ImGuiLayerHelper::_ready()
{
    set_process_priority(std::numeric_limits<int32_t>::min());
}

void ImGuiLayerHelper::_exit_tree()
{
}

void ImGuiLayerHelper::_process(double delta)
{
    impl->igl->NewFrame(delta);
}

} // namespace ImGui::Godot
