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
    virtual ~RdRenderer();

    virtual const char* Name() override
    {
        return "godot4_rd";
    }

    void InitViewport(RID vprid) override
    {
    }
    void CloseViewport(RID vprid) override
    {
    }
    virtual void Render() override;
    void OnHide() override
    {
    }


protected:
    void Render(RID fb, ImDrawData* drawData);
    static void ReplaceTextureRIDs(ImDrawData* drawData);
    RID GetFramebuffer(RID vprid);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
