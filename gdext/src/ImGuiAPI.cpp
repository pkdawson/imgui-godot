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

std::unordered_map<StringName, std::string> stringname_cache;
const char* sn_to_cstr(const StringName& sn)
{
    if (!stringname_cache.contains(sn))
    {
        stringname_cache[sn] = std::string(String(sn).utf8().get_data());
    }
    return stringname_cache[sn].c_str();
}
} // namespace

namespace ImGui::Godot {

struct ImGui::Impl
{
    bool show_imgui_demo = true;
};

void ImGui::_bind_methods()
{
    REGISTER_IMGUI_ENUMS();
    BIND_IMGUI_STRUCTS();
    BIND_IMGUI_FUNCS();
}

ImGui::ImGui() : impl(std::make_unique<Impl>())
{
}

ImGui::~ImGui()
{
}

//void ImGuiIOPtr::_bind_methods()
//{
//    //ClassDB::bind_method(D_METHOD("_set_ConfigFlags", "flags"), &ImGuiIOPtr::_set_ConfigFlags);
//    //ClassDB::bind_method(D_METHOD("_get_ConfigFlags"), &ImGuiIOPtr::_get_ConfigFlags);
//    //ADD_PROPERTY(PropertyInfo(Variant::INT, "ConfigFlags"), "_set_ConfigFlags", "_get_ConfigFlags");
//}

DEFINE_IMGUI_STRUCTS()

DEFINE_IMGUI_FUNCS()

} // namespace ImGui::Godot
