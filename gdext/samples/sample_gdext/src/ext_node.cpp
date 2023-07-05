#include "ext_node.h"
#include "imgui-godot.h"

// #include <godot_cpp/classes/global_constants.hpp>
#include <godot_cpp/core/class_db.hpp>
// #include <godot_cpp/variant/utility_functions.hpp>

using namespace godot;

void ExtNode::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("imgui_layout"), &ExtNode::imgui_layout);
}

void ExtNode::_ready()
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    ImGui::Godot::SyncImGuiPtrs();
    ImGui::Godot::Connect(Callable(this, "imgui_layout"));
}

void ExtNode::_process(double delta)
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    ImGui::Begin("ExtNode process");
    ImGui::Text("text 1");
    ImGui::End();
}

void ExtNode::imgui_layout()
{
    ImGui::Begin("ExtNode signal");
    ImGui::Text("text 2");
    ImGui::End();
}
