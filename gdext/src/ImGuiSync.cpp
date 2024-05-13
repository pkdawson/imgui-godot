#include "ImGuiSync.h"
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <imgui.h>

namespace ImGui::Godot {

void ImGuiSync::_bind_methods()
{
    ClassDB::bind_static_method("ImGuiSync", D_METHOD("GetImGuiPtrs"), &ImGuiSync::GetImGuiPtrs);
}

PackedInt64Array ImGuiSync::GetImGuiPtrs(String version, int ioSize, int vertSize, int idxSize, int charSize)
{
    if (version != String(ImGui::GetVersion()) || ioSize != sizeof(ImGuiIO) || vertSize != sizeof(ImDrawVert) ||
        idxSize != sizeof(ImDrawIdx) || charSize != sizeof(ImWchar))
    {
        UtilityFunctions::push_error("ImGui version mismatch, use v", ImGui::GetVersion(), "-docking");
        return {};
    }

    ImGuiMemAllocFunc alloc_func = nullptr;
    ImGuiMemFreeFunc free_func = nullptr;
    void* user_data = nullptr;

    ImGui::GetAllocatorFunctions(&alloc_func, &free_func, &user_data);

    PackedInt64Array rv;
    rv.resize(3);
    rv[0] = reinterpret_cast<int64_t>(ImGui::GetCurrentContext());
    rv[1] = reinterpret_cast<int64_t>(alloc_func);
    rv[2] = reinterpret_cast<int64_t>(free_func);
    return rv;
}

} // namespace ImGui::Godot
