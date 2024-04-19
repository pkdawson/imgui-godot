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

    virtual bool Init() = 0;
    virtual void InitViewport(RID vprid) = 0;
    virtual void CloseViewport(RID vprid) = 0;
    virtual void Render() = 0;
    virtual void OnHide() = 0;
    virtual void OnFramePreDraw()
    {
    }
};

} // namespace ImGui::Godot
