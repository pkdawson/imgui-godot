#include "another_node.h"
#include "core/config/engine.h"
#include <imgui.h>

void AnotherNode::_bind_methods()
{
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
        set_process(true);
        break;
    case NOTIFICATION_PROCESS:
        ImGui::Begin("AnotherNode");
        ImGui::Text("another C++ module");
        ImGui::End();
        break;
    }
}
