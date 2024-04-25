#include "mynode.h"
#include "core/config/engine.h"
#include <cstdio>
#include <imgui-godot.h>

MyNode::MyNode()
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif
    set_process(true);
}

MyNode::~MyNode()
{
}

void MyNode::_bind_methods()
{
}

void MyNode::_notification(int what)
{
    switch (what)
    {
    case NOTIFICATION_PROCESS: {
        ImGui::Begin("C++ module");
        ImGui::Text("hello");
        ImGui::End();
    }
    break;
    }
}
