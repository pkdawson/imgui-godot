#include "ImGuiGD.h"
#include <imgui.h>

namespace {

struct Context
{
    Window* mainWindow = nullptr;
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
}

void Shutdown()
{
    ctx.reset();
    if (ImGui::GetCurrentContext())
        ImGui::DestroyContext();
}

} // namespace ImGui::Godot
