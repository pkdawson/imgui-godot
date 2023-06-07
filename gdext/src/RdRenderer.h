#pragma once
#include <imgui.h>
#include <memory>

#pragma warning(push, 0)
#include <godot_cpp/variant/rid.hpp>
#pragma warning(pop)

using godot::RID;

namespace ImGui::Godot {

class RdRenderer
{
public:
    RdRenderer();
    ~RdRenderer();

    void RenderDrawData(RID vprid, ImDrawData* drawData);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
