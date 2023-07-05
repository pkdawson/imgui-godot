#include "another_node.h"
#include "core/config/engine.h"
#include "imgui-godot.h"
#include <imgui.h>

void AnotherNode::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("imgui_layout"), &AnotherNode::imgui_layout);
}

void AnotherNode::_notification(int p_what)
{
#ifdef TOOLS_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    switch (p_what)
    {
    case NOTIFICATION_READY:
        ImGui::Godot::Connect(Callable(this, "imgui_layout"));
        break;
    }
}

void AnotherNode::imgui_layout()
{
    ImGui::Begin("AnotherNode signal");
    ImGui::Text("another C++ module");
    ImGui::End();
}
