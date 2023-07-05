#include "sample_node.h"
#include "core/config/engine.h"
#include "imgui-godot.h"
#include <imgui.h>

void SampleNode::_bind_methods()
{
}

void SampleNode::_notification(int p_what)
{
#ifdef TOOLS_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    switch (p_what)
    {
    case NOTIFICATION_ENTER_TREE:
        ImGui::Godot::SyncImGuiPtrs();
        break;
    case NOTIFICATION_READY:
        set_process(true);
        break;
    case NOTIFICATION_PROCESS:
        ImGui::Begin("SampleNode");
        ImGui::Text("hello from C++ module");
        ImGui::End();
        break;
    }
}
