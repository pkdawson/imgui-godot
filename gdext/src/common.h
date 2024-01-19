#pragma once

#pragma warning(push, 0)
#include <godot_cpp/variant/rid.hpp>
#pragma warning(pop)

#include <imgui.h>

using godot::RID;

namespace ImGui::Godot {

inline RID make_rid(int64_t id)
{
    // ugly, may break in the future
    RID rv;
    *(int64_t*)(rv._native_ptr()) = id;
    assert(rv.get_id() == id);
    return rv;
}

inline RID make_rid(ImTextureID id)
{
    return make_rid(reinterpret_cast<int64_t>(id));
}

} // namespace ImGui::Godot
