#pragma once
#include <imgui.h>
#include <memory>

#pragma warning(push, 0)
#include <godot_cpp/variant/rid.hpp>
#pragma warning(pop)

using godot::RID;

namespace ImGui::Godot {

class Renderer
{
public:
    Renderer()
    {
    }

    virtual ~Renderer()
    {
    }

    virtual const char* Name() = 0;
    virtual void RenderDrawData(RID vprid, ImDrawData* drawData) = 0;
};

} // namespace ImGui::Godot
