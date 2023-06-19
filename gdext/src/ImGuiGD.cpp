#include "ImGuiGD.h"
#include "common.h"
#include "imgui-godot.h"

using namespace godot;

namespace ImGui::Godot {

void ImGuiGD::_bind_methods()
{
    ClassDB::bind_static_method("ImGuiGD", D_METHOD("Connect", "callable"), &ImGuiGD::Connect);
}

void ImGuiGD::Connect(const godot::Callable& callable)
{
    ImGui::Godot::Connect(callable);
}

} // namespace ImGui::Godot
