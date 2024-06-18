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

inline Color ToColor(const ImVec4& v)
{
    return Color(v.x, v.y, v.z, v.w);
}

inline ImVec4 FromColor(const Color& c)
{
    return ImVec4(c.r, c.g, c.b, c.a);
}

inline PackedColorArray ToPackedColorArray(ImVec4* colors, int size)
{
    PackedColorArray rv;
    rv.resize(size);
    for (int i = 0; i < size; ++i)
        rv[i] = ToColor(colors[i]);
    return rv;
}

inline void FromPackedColorArray(const PackedColorArray& in, ImVec4* out)
{
    for (int i = 0; i < in.size(); ++i)
        out[i] = FromColor(in[i]);
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

    Engine::get_singleton()->register_singleton("ImGuiAPI", memnew(::ImGui::Godot::ImGui));
}

void unregister_imgui_api()
{
    ::ImGui::Godot::ImGui* api = (::ImGui::Godot::ImGui*)Engine::get_singleton()->get_singleton("ImGuiAPI");
    Engine::get_singleton()->unregister_singleton("ImGuiAPI");
    memdelete(api);
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
