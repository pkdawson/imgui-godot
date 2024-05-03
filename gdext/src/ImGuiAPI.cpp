#include "ImGuiAPI.h"
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

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

const char* sn_to_cstr(const StringName& sn)
{
    static std::unordered_map<StringName, std::string> stringname_cache;
    if (!stringname_cache.contains(sn))
    {
        stringname_cache[sn] = std::string(String(sn).utf8().get_data());
    }
    return stringname_cache[sn].c_str();
}
} // namespace

namespace ImGui::Godot {

void register_imgui_api()
{
    ClassDB::register_class<::ImGui::Godot::ImGui>();
}

void ImGui::_bind_methods()
{
    REGISTER_IMGUI_ENUMS();
    BIND_IMGUI_STRUCTS();
    BIND_IMGUI_FUNCS();
}

DEFINE_IMGUI_STRUCTS()

DEFINE_IMGUI_FUNCS()

} // namespace ImGui::Godot
