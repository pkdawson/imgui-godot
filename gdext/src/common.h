#pragma once
#include <godot_cpp/variant/rid.hpp>
#include <imgui.h>

using godot::RID;

namespace ImGui::Godot {

inline RID make_rid(int64_t id)
{
    // ugly, may break in the future
    RID rv;
    *reinterpret_cast<int64_t*>(rv._native_ptr()) = id;
    IM_ASSERT(rv.get_id() == id);
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
    std::size_t operator()(const RID& rid) const noexcept { return std::hash<int64_t>{}(rid.get_id()); }
};
