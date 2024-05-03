#pragma once
#include <godot_cpp/variant/rid.hpp>
#include <imgui.h>
#include <memory>

using godot::RID;

namespace ImGui::Godot {

class Renderer
{
public:
    Renderer() {}
    virtual ~Renderer() {}

    virtual const char* Name() = 0;

    virtual bool Init() = 0;
    virtual void InitViewport(RID vprid) = 0;
    virtual void CloseViewport(RID vprid) = 0;
    virtual void Render() = 0;
    virtual void OnHide() = 0;
    virtual void OnFramePreDraw() {}
};

} // namespace ImGui::Godot
