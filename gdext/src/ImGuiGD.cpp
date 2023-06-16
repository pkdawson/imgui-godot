#include "ImGuiGD.h"
#include "RdRenderer.h"
#include <imgui.h>

using ImGui::Godot::RdRenderer;
using godot::Window;

namespace {

struct Context
{
    Window* mainWindow = nullptr;
    std::unique_ptr<RdRenderer> renderer;
};

std::unique_ptr<Context> ctx;
} // namespace

void IGN_API ImGuiGodot_Init(Window* window)
{
    ctx = std::make_unique<Context>();
    ctx->mainWindow = window;

    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();
    io.Fonts->Build();
    io.DisplaySize = ctx->mainWindow->get_size();

    ctx->renderer = std::make_unique<RdRenderer>();
}

void IGN_API ImGuiGodot_Update(double delta)
{
    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = ctx->mainWindow->get_size();
    io.DeltaTime = static_cast<float>(delta);

    ImGui::NewFrame();
}

void IGN_API ImGuiGodot_Render()
{
    ImGui::Render();
    ctx->renderer->RenderDrawData(ctx->mainWindow->get_viewport_rid(), ImGui::GetDrawData());
}

void IGN_API ImGuiGodot_Shutdown()
{
    ctx.reset();
    if (ImGui::GetCurrentContext())
        ImGui::DestroyContext();
}
