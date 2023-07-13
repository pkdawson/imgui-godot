#pragma once
#include "RdRenderer.h"
#include <imgui.h>
#include <memory>

#pragma warning(push, 0)
#include <godot_cpp/variant/rid.hpp>
#pragma warning(pop)

using godot::RID;

namespace ImGui::Godot {

class RdRendererThreadSafe : public RdRenderer
{
public:
    RdRendererThreadSafe();
    virtual ~RdRendererThreadSafe();

    const char* Name() override
    {
        return "godot4_rd_mt";
    }

    void Render() override;
    void OnFramePreDraw() override;

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
