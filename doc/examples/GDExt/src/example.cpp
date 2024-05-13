#include "example.h"
#include <godot_cpp/classes/resource_loader.hpp>
#include <imgui-godot.h>

using namespace godot;

void Example::_bind_methods()
{
}

Example::Example()
{
}

Example::~Example()
{
}

void Example::_ready()
{
    _img = ResourceLoader::get_singleton()->load("res://robot_eye.tres");
}

void Example::_process(double delta)
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    static int x = 0;

    ImGui::SetNextWindowSize({200, 200}, ImGuiCond_Once);
    ImGui::Begin("GDExtension Example");
    ImGui::DragInt("x", &x);
    ImGui::Text("x = %d", x);
    ImGui::Separator();
    ImGui::Image(_img, {64, 64});
    ImGui::End();
}
