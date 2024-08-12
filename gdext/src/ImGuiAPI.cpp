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
    return {c.r, c.g, c.b, c.a};
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

inline Array FromImVector(const ImVector_ImGuiSelectionRequest& in)
{
    Array rv;
    for (int i = 0; i < in.Size; ++i)
    {
        Ref<ImGui::Godot::ImGuiSelectionRequestPtr> req;
        req.instantiate();
        req->_SetPtr(&in.Data[i]);
        rv.append(req);
    }
    return rv;
}

inline ImVector_ImGuiSelectionRequest ToImVector(const Array& in)
{
    ERR_FAIL_V_MSG({}, "ToImVector not implemented");
    return {};
}

inline Array SpecsToArray(const ImGuiTableSortSpecs* p)
{
    ERR_FAIL_COND_V(!p, {});

    Array rv;
    for (int i = 0; i < p->SpecsCount; ++i)
    {
        Ref<ImGui::Godot::ImGuiTableColumnSortSpecsPtr> col;
        col.instantiate();
        col->_SetPtr(const_cast<ImGuiTableColumnSortSpecs*>(&p->Specs[i]));
        rv.append(col);
    }
    return rv;
}

const char* sn_to_cstr(const StringName& sn)
{
    static std::unordered_map<int64_t, std::string> stringname_cache;
    const int64_t hash = sn.hash();
    if (!stringname_cache.contains(hash))
    {
        stringname_cache[hash] = std::string(String(sn).utf8().get_data());
    }
    return stringname_cache[hash].c_str();
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

#include "imgui_bindings_impl.gen.h"

} // namespace ImGui::Godot
