#include "ImGuiGD.h"
#include "RdRenderer.h"
#include <imgui.h>

static_assert(sizeof(RID) == 8);
static_assert(sizeof(void*) == 8);

namespace {

struct Context
{
    Window* mainWindow = nullptr;
    std::unique_ptr<ImGui::Godot::RdRenderer> renderer;
};

std::unique_ptr<Context> ctx;
} // namespace

namespace ImGui::Godot {

void Init(Window* window)
{
    ctx = std::make_unique<Context>();
    ctx->mainWindow = window;

    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();
    io.Fonts->Build();
    io.DisplaySize = ctx->mainWindow->get_size();

    ctx->renderer = std::make_unique<RdRenderer>();
}

void Update(double delta)
{
    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = ctx->mainWindow->get_size();
    io.DeltaTime = static_cast<float>(delta);

    ImGui::NewFrame();
}

void Render()
{
    ImGui::Render();
    ctx->renderer->RenderDrawData(ctx->mainWindow->get_viewport_rid(), ImGui::GetDrawData());
}

void Shutdown()
{
    ctx.reset();
    if (ImGui::GetCurrentContext())
        ImGui::DestroyContext();
}

} // namespace ImGui::Godot
