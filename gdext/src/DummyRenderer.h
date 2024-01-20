#pragma once
#include "Renderer.h"

namespace ImGui::Godot {

class DummyRenderer : public Renderer
{
public:
    DummyRenderer()
    {
    }

    ~DummyRenderer()
    {
    }

    const char* Name() override
    {
        return "godot4_dummy";
    }

    void InitViewport(RID vprid) override
    {
    }

    void CloseViewport(RID vprid) override
    {
    }

    void Render() override
    {
    }

    void OnHide() override
    {
    }
};

} // namespace ImGui::Godot
