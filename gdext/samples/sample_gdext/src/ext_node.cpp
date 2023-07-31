#include "ext_node.h"
#include "imgui-godot.h"

// #include <godot_cpp/classes/global_constants.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
// #include <godot_cpp/variant/utility_functions.hpp>

using namespace godot;

struct ExtNode::Impl
{
    Ref<Texture2D> tex;
};

ExtNode::ExtNode() : impl(std::make_unique<Impl>())
{
}

ExtNode::~ExtNode()
{
}

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

    impl->tex = ResourceLoader::get_singleton()->load("res://icon.svg");
}

void ExtNode::_process(double delta)
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    ImGui::Begin("ExtNode process");
    ImGui::Text("text 1");
    ImGui::Godot::Image(impl->tex, {128, 128});
    ImGui::End();
}

void ExtNode::imgui_layout()
{
    ImGui::Begin("ExtNode signal");
    ImGui::Text("text 2");
    ImGui::End();
}
