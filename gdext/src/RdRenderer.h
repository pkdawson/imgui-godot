#pragma once
#include "Renderer.h"
#include <imgui.h>
#include <memory>

#pragma warning(push, 0)
#include <godot_cpp/variant/rid.hpp>
#pragma warning(pop)

using godot::RID;

namespace ImGui::Godot {

class RdRenderer : public Renderer
{
public:
    RdRenderer();
    ~RdRenderer();

    const char* Name() override
    {
        return "godot4_rd";
    }

    void Render() override;

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
