#include "mynode.h"
#include "core/config/engine.h"
#include "core/io/resource_loader.h"
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
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    switch (what)
    {
    case NOTIFICATION_PROCESS:
        ImGui::Begin("C++ module", nullptr, ImGuiWindowFlags_AlwaysAutoResize);
        ImGui::Text("hello");
        ImGui::Image(_img, {_iconSize, _iconSize});
        ImGui::DragFloat("size", &_iconSize, 1.f, 32.f, 512.f);
        ImGui::End();
        break;

    case NOTIFICATION_READY:
        _img = ResourceLoader::load("res://icon.svg");
        break;
    }
}
