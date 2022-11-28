#include "MyCppNode.h"
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <imgui.h>
#include <imgui_freetype.h>
#include <implot.h>
using namespace godot;

struct MyCppNode::Impl
{
    bool show_imgui_demo = true;
    bool show_implot_demo = true;
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
    set_process(false);
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    Node* igl = get_node_or_null("/root/ImGuiLayer");
    if (!igl)
    {
        UtilityFunctions::push_error("couldn't get /root/ImGuiLayer");
        return;
    }

    Variant rv = igl->call("GetImGuiPtrs",
                           ImGui::GetVersion(),
                           static_cast<int64_t>(sizeof(ImGuiIO)),
                           static_cast<int64_t>(sizeof(ImDrawVert)),
                           static_cast<int64_t>(sizeof(ImDrawIdx)));

    if (!rv)
    {
        UtilityFunctions::push_error("GetImGuiPtrs failed");
        return;
    }

    PackedInt64Array imgui_ptrs = rv;

    if (imgui_ptrs.size() < 3)
        return;

    int64_t ctx = imgui_ptrs[0];
    int64_t mem_alloc = imgui_ptrs[1];
    int64_t mem_free = imgui_ptrs[2];

    ImGui::SetCurrentContext((ImGuiContext*)ctx);
    ImGui::SetAllocatorFunctions((ImGuiMemAllocFunc)mem_alloc, (ImGuiMemFreeFunc)mem_free);

    // will be used in the next RebuildFontAtlas
    ImGui::GetIO().Fonts->FontBuilderIO = ImGuiFreeType::GetBuilderForFreeType();

    ImPlot::CreateContext();
    set_process(true);
}

void MyCppNode::_exit_tree()
{
    ImPlot::DestroyContext();
}

void MyCppNode::_process(double delta)
{
    if (impl->show_imgui_demo)
        ImGui::ShowDemoWindow(&impl->show_imgui_demo);

    if (impl->show_implot_demo)
        ImPlot::ShowDemoWindow(&impl->show_implot_demo);

    ImGui::Begin("Cpp Window");
    ImGui::Text("hello from C++");
    ImGui::End();
}
