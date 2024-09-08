#pragma once
#include <godot_cpp/variant/rid.hpp>
#include <imgui.h>

using godot::RID;

namespace ImGui::Godot {

inline RID make_rid(int64_t id)
{
    // HACK: only way to set a RID value
    RID rv;
    memcpy(rv._native_ptr(), &id, sizeof(int64_t));
    return rv;
}

inline RID make_rid(ImTextureID id)
{
    return make_rid(reinterpret_cast<int64_t>(id));
}

} // namespace ImGui::Godot

template <>
struct std::hash<RID>
{
    std::size_t operator()(const RID& rid) const noexcept { return rid.get_id(); }
};
