#include "ImGuiControllerHelper.h"
#include "Context.h"
using namespace godot;

namespace ImGui::Godot {

ImGuiControllerHelper::ImGuiControllerHelper()
{
}

ImGuiControllerHelper::~ImGuiControllerHelper()
{
}

void ImGuiControllerHelper::_bind_methods()
{
}

void ImGuiControllerHelper::_enter_tree()
{
}

void ImGuiControllerHelper::_ready()
{
    set_name("ImGuiControllerHelper");
    set_process_priority(std::numeric_limits<int32_t>::min());
    set_process_mode(Node::PROCESS_MODE_ALWAYS);
}

void ImGuiControllerHelper::_exit_tree()
{
}

void ImGuiControllerHelper::_process(double delta)
{
    Context* ctx = GetContext();
    ctx->inProcessFrame = true;
    ctx->Update(delta, ctx->viewportSize);
}

} // namespace ImGui::Godot
