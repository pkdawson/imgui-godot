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

void ImGui::_bind_methods()
{
    ClassDB::bind_static_method("ImGui", D_METHOD("SetNextWindowPos", "pos"), &ImGui::SetNextWindowPos);
    ClassDB::bind_static_method("ImGui", D_METHOD("Begin", "name", "p_open"), &ImGui::Begin, DEFVAL(godot::Array()));
    ClassDB::bind_static_method("ImGui", D_METHOD("End"), &ImGui::End);

    ClassDB::bind_static_method("ImGui", D_METHOD("Text", "text"), &ImGui::Text);

    ClassDB::bind_static_method("ImGui", D_METHOD("GetIO"), &ImGui::GetIO);

    BIND_ENUM_CONSTANT(ConfigFlags_ViewportsEnable);
}

ImGui::ImGui() : impl(std::make_unique<Impl>())
{
}

ImGui::~ImGui()
{
}

void ImGui::SetNextWindowPos(godot::Vector2i pos)
{
    ::ImGui::SetNextWindowPos(pos);
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

Ref<ImGuiIOPtr> ImGui::GetIO()
{
    Ref<ImGuiIOPtr> rv;
    rv.instantiate();
    return rv;
}

void ImGuiIOPtr::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("_set_ConfigFlags", "flags"), &ImGuiIOPtr::_set_ConfigFlags);
    ClassDB::bind_method(D_METHOD("_get_ConfigFlags"), &ImGuiIOPtr::_get_ConfigFlags);
    ADD_PROPERTY(PropertyInfo(Variant::INT, "ConfigFlags"), "_set_ConfigFlags", "_get_ConfigFlags");
}

ImGuiIOPtr::ImGuiIOPtr()
{
    io = &(::ImGui::GetIO());
}

void ImGuiIOPtr::_set_ConfigFlags(int32_t flags)
{
    io->ConfigFlags = flags;
}

int32_t ImGuiIOPtr::_get_ConfigFlags()
{
    return io->ConfigFlags;
}

} // namespace ImGui::Godot
