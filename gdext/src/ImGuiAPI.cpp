#include "ImGuiAPI.h"

#pragma warning(push, 0)
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

#include <imgui.h>
using namespace godot;

namespace ImGui::Godot {

struct ImGui::Impl
{
    bool show_imgui_demo = true;
};

ImGui::ImGui() : impl(std::make_unique<Impl>())
{
}

ImGui::~ImGui()
{
}

bool ImGui::Begin(String name, Array p_open)
{
    return ::ImGui::Begin(name.utf8().get_data());
}

void ImGui::End()
{
    ::ImGui::End();
}

void ImGui::Text(String text)
{
    ::ImGui::Text(text.utf8().get_data());
}

void ImGui::_bind_methods()
{
    ClassDB::bind_static_method("ImGui", D_METHOD("Begin", "name", "p_open"), &ImGui::Begin, DEFVAL(godot::Array()));
    ClassDB::bind_static_method("ImGui", D_METHOD("End"), &ImGui::End);

    ClassDB::bind_static_method("ImGui", D_METHOD("Text", "text"), &ImGui::Text);
}


} // namespace ImGui::Godot
