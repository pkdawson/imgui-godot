#include "mycppnode.h"
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <godot_cpp/classes/input_event.hpp>
#include <imgui.h>
#include <implot.h>
using namespace godot;

struct MyCppNode::Impl
{
};

MyCppNode::MyCppNode() : impl(std::make_unique<Impl>())
{
}

MyCppNode::~MyCppNode()
{
}

void MyCppNode::_bind_methods()
{
}

void MyCppNode::_ready()
{
    // TODO: don't run in editor mode
    set_process(false);
    Node* igl = get_node_or_null("/root/ImGuiLayer");
    if (!igl)
        return;

    PackedInt64Array imgui_ptrs = igl->call(
        "GetImGuiPtrs",
        ImGui::GetVersion(),
        sizeof(ImGuiIO),
        sizeof(ImDrawVert),
        sizeof(ImDrawIdx)
        );

    if (imgui_ptrs.size() < 3)
        return;

    int64_t ctx = imgui_ptrs[0];
    int64_t mem_alloc = imgui_ptrs[1];
    int64_t mem_free = imgui_ptrs[2];

    ImGui::SetCurrentContext((ImGuiContext*)ctx);
    ImGui::SetAllocatorFunctions((ImGuiMemAllocFunc)mem_alloc, (ImGuiMemFreeFunc)mem_free);
    ImPlot::CreateContext();
    set_process(true);
}

void MyCppNode::_exit_tree()
{
    ImPlot::DestroyContext();
}

void MyCppNode::_process(double delta)
{
    ImPlot::ShowDemoWindow();

    ImGui::Begin("Cpp Window");
    ImGui::Text("hello from C++");
    ImGui::End();
}
