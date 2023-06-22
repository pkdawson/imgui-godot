#include "ImGuiAPI.h"

#pragma warning(push, 0)
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

using namespace godot;

namespace {
inline Vector2 ToVector2(ImVec2 v)
{
    return Vector2(v.x, v.y);
}

inline Color ToColor(ImVec4 v)
{
    return Color(v.x, v.y, v.z, v.w);
}
} // namespace

namespace ImGui::Godot {

struct ImGui::Impl
{
    bool show_imgui_demo = true;
};

void ImGui::_bind_methods()
{
    ClassDB::bind_static_method("ImGui",
                                D_METHOD("Begin", "name", "p_open", "flags"),
                                &ImGui::Begin,
                                DEFVAL(godot::Array()),
                                DEFVAL(WindowFlags_None));

    ClassDB::bind_static_method("ImGui", D_METHOD("GetIO"), &ImGui::GetIO);

    REGISTER_IMGUI_ENUMS();
    BIND_IMGUI_FUNCS();
}

ImGui::ImGui() : impl(std::make_unique<Impl>())
{
}

ImGui::~ImGui()
{
}

bool ImGui::Begin(String name, Array p_open, BitField<WindowFlags> flags)
{
    return ImGui_Begin(name.utf8().get_data(), GDS_PTR(bool, p_open), flags);
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
    io = ImGui_GetIO();
}

void ImGuiIOPtr::_set_ConfigFlags(int32_t flags)
{
    io->ConfigFlags = flags;
}

int32_t ImGuiIOPtr::_get_ConfigFlags()
{
    return io->ConfigFlags;
}

DEFINE_IMGUI_FUNCS()

} // namespace ImGui::Godot
