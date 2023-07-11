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

    void Render() override
    {
    }
};

} // namespace ImGui::Godot
