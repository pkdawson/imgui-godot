#pragma once
#include "RdRenderer.h"
#include <godot_cpp/variant/rid.hpp>
#include <imgui.h>
#include <memory>

using godot::RID;

namespace ImGui::Godot {

class RdRendererThreadSafe : public RdRenderer
{
public:
    RdRendererThreadSafe();
    virtual ~RdRendererThreadSafe();

    const char* Name() override { return "godot4_rd_mt"; }

    void Render() override;
    void OnFramePreDraw() override;

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
